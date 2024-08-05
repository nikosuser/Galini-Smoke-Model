using OSGeo.GDAL;
using OSGeo.OSR;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System;
using System.Drawing;
using System.Windows.Forms;
using IronPython.Compiler.Ast;

namespace Galini_C_
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            //----------------------------INPUT VARIABLES--------------------------------------
            string rootPath = @"C:\GaliniData";
            string caseStudy = "/FlatPoint_actual";
            int timeSteps = 1; //minuts per timestep

            if (Directory.Exists(rootPath + caseStudy + "/Output/"))
            {
                Directory.Delete(rootPath + caseStudy + "/Output/", true);
            }
            Directory.CreateDirectory(rootPath + caseStudy + "/Output/");

            string weatherFile = caseStudy + "/weather.wxs";
            string arrivalTimeFile = caseStudy + "/arrivalTime.asc";
            string rateOfSpreadFile = caseStudy + "/rateofspread.asc";
            //string heatPUAFile = caseStudy + "/heatpua.asc";
            string fuelSBFile = caseStudy + "/fuel.asc";
            string fuelMoistureFile = caseStudy + "/moisture.fms";
            string fccs_outputFileName = "/fuel_fccs.tif";
            string fofemInputFileName = "/FOFEM.txt";
            string elevationHeight = caseStudy + "/elevation.asc";

            if (!File.Exists(rootPath + caseStudy + fccs_outputFileName))
            {
                File.Delete(rootPath + caseStudy + fccs_outputFileName);
            }

            Dictionary<string, double> config = new Dictionary<string, double>()
            {
                {"windVelocity", -1 },
                {"windAngle", -1 },
                {"atmosphericTemp", -1 },
                {"atmosphericPressure", 100000 },
                {"cellsize_fire", 30 },
                {"cellsize_smoke", 30 },
                {"scaleFactor", 1 },
                {"environmentalLapseRate", -6.5 },
                {"dryAdiabaticLapseRate", -9.8 },
                {"RAWSelevation", Helpers.GetRawsElevation(rootPath + weatherFile) }
            };

            double[,] weatherInput = Helpers.GetAscFile(rootPath, weatherFile, 4);
            double[,] arrivalTime = Helpers.GetAscFile(rootPath, arrivalTimeFile, 6);
            double[,] ROS = Helpers.GetAscFile(rootPath, rateOfSpreadFile, 6);
            //double[,] heatPUA = Helpers.GetAscFile(rootPath, heatPUAFile, 6);
            double[,] fuelMoisture = Helpers.GetAscFile(rootPath, fuelMoistureFile, 0);
            double[,] fuel_SB = Helpers.GetAscFile(rootPath, fuelSBFile, 6);
            double[,] elevation= Helpers.GetAscFile(rootPath, elevationHeight, 6);

            FOFEM.runFOFEM(fuel_SB, rootPath, fofemInputFileName, fccs_outputFileName, fuelMoisture);

            double[] fireDomainDims = [arrivalTime.GetLength(0), arrivalTime.GetLength(1)];                        //Total fire domain size 
            double[,] burningPointMatrix = new double[(int)fireDomainDims[0], (int)fireDomainDims[1]];       //coordinates of points on fire (about fire domain)
            double[,] flamingTime = new double[(int)fireDomainDims[0], (int)fireDomainDims[1]];
            double[,] smolderingTime = new double[(int)fireDomainDims[0], (int)fireDomainDims[1]];  //awaiting for update?
            double[,] flamingEmissions = Helpers.ReadGeoTIFFfile(rootPath + "/FOFEMoutput/_Flaming PM10.tif");
            double[,] smolderingEmissions = Helpers.ReadGeoTIFFfile(rootPath + "/FOFEMoutput/_Smoldering PM10.tif");
            
            //outputs are in pounds per acre, so we convert them to grams per square meter

            for (int i=0; i<fireDomainDims[0]; i++)
            {
                for (int j = 0; j < fireDomainDims[1]; j++)
                {
                    //burningPointMatrix[i,j] = (arrivalTime[i,j] <= flamingTime[i,j] + smolderingTime[i,j] + config["cellsize_fire"] / ROS[i,j]) ? 1 : 0; 
                    flamingEmissions[i, j] = (float)(0.112085 * flamingEmissions[i, j]);
                    smolderingEmissions[i, j] = (float)(0.112085 * smolderingEmissions[i, j]);

                    //fill times with dummy variables for now
                    flamingTime[i, j] = 15;
                    smolderingTime[i, j] = 120;
                }
            }

            int totalWeatherInputs = weatherInput.GetLength(0)-1;
            DateTime fireEndTime = new DateTime((int)weatherInput[totalWeatherInputs, 0], (int)weatherInput[totalWeatherInputs, 1], (int)weatherInput[totalWeatherInputs, 2], (int)(weatherInput[totalWeatherInputs, 3] / 100), (int)(weatherInput[totalWeatherInputs, 3] % 100), 0);
            DateTime fireStartTime = new DateTime((int)weatherInput[0, 0], (int)weatherInput[0, 1], (int)weatherInput[0, 2], (int)(weatherInput[0, 3] / 100), (int)(weatherInput[0, 3] % 100), 0);

            int totalSteps = (int)((fireEndTime - fireStartTime).TotalMinutes / timeSteps);
            double[] windMagPerStep = new double[totalSteps];
            double[] windDirPerStep = new double[totalSteps];
            for (int step = 0; step < totalSteps; step++)
            {
                int index1 = 0;
                int index2 = 0;
                DateTime hour1 = new DateTime();
                DateTime hour2 = new DateTime();
                DateTime smokeTime = fireStartTime.AddMinutes(step*timeSteps);
                for (int i = 0; i < weatherInput.GetLength(0); i++)
                {
                    DateTime inputRowTime = new DateTime((int)weatherInput[i, 0], (int)weatherInput[i, 1], (int)weatherInput[i, 2], (int)(weatherInput[i, 3] / 100), (int)(weatherInput[i, 3] % 100), 0);

                    if ((int)(smokeTime - inputRowTime).TotalMinutes < 60)
                    {
                        hour1 = inputRowTime.AddHours(1);
                        hour2 = inputRowTime;
                        index1 = i + 1;
                        index2 = i;

                        double hourProgress = (smokeTime - hour2).TotalMinutes / (hour1 - hour2).TotalMinutes;
                        windMagPerStep[step] = weatherInput[index2, 7] + hourProgress * (weatherInput[index1, 7] - weatherInput[index2, 7]);
                
                        double windDirShift = (weatherInput[index1, 8] - weatherInput[index2, 8]);
                        windDirShift = (windDirShift < 0) ? windDirShift + 360 : windDirShift;
                        bool clockwiseShift = (windDirShift % 180 == windDirShift) ? true : false;
                        if (clockwiseShift)
                        {
                            windDirPerStep[step] = (hourProgress * windDirShift + weatherInput[index2, 8]) % 360;
                        }
                        else
                        {
                            windDirShift = (360 - windDirShift) % 180;
                            windDirPerStep[step] = (weatherInput[index2, 8] - hourProgress * windDirShift) % 360;
                            windDirPerStep[step] = (windDirPerStep[step] < 0) ? windDirPerStep[step] + 360 : windDirPerStep[step];
                        }

                        break;
                    }
                }

            }


            for (int totalTime = 0; totalTime < (fireEndTime - fireStartTime).TotalMinutes; totalTime = totalTime + timeSteps)
            {
                Console.WriteLine($"Elapsed Minutes: {totalTime} of {(fireEndTime - fireStartTime).TotalMinutes}");
                DateTime smokeTime = fireStartTime.AddMinutes(totalTime);

                double[,] subgridTime = new double[(int)fireDomainDims[0], (int)fireDomainDims[1]];
                double targetArrivalTime = (smokeTime - fireStartTime).TotalMinutes;
                for (int i = 0; i < fireDomainDims[0]; i++)
                {
                    for (int j = 0; j < fireDomainDims[1]; j++)
                    {
                        if (arrivalTime[i, j] >= 0)
                        {
                            burningPointMatrix[i, j] = 1;
                            subgridTime[i,j] = totalTime - arrivalTime[i, j];
                            subgridTime[i, j] = (subgridTime[i, j] >= 0) ? subgridTime[i, j] : 0; 
                        }
                    }
                }
                char atmoStabilityIndex = DispersionModelling.GetStabilityClass("day", "rural", "moderate", "majority", "pessimistic", 25, config["windVelocity"]);
                double[] dispCoeffs = DispersionModelling.GetDispersionCoefficients("rural", atmoStabilityIndex);

                Console.WriteLine($"Atmospheric Stability Class {atmoStabilityIndex}");

                config["windVelocity"] = windMagPerStep[(int)(totalTime / timeSteps)];
                config["windAngle"] = windDirPerStep[(int)(totalTime / timeSteps)];

                double[,] topDownRaster;
                double[,] driverLevelDensity;
                double[,] fireActivePoints;
                double[,] smolderActivePoints;
                double[,] injectionHeight;
                double[,] subgridOut;
                (topDownRaster, driverLevelDensity, fireActivePoints, smolderActivePoints, injectionHeight, subgridOut) = DispersionModelling.DispersionModel(elevation,
                                                                    config,
                                                                    burningPointMatrix,
                                                                    ROS,
                                                                    flamingEmissions,
                                                                    smolderingEmissions,
                                                                    fireDomainDims,
                                                                    dispCoeffs,
                                                                    atmoStabilityIndex,
                                                                    flamingTime,
                                                                    smolderingTime,
                                                                    subgridTime);

                Helpers.WriteMatrixToCSV(topDownRaster, rootPath + caseStudy + "/Output/" + totalTime.ToString() + "topDownRaster.csv");
                Helpers.WriteMatrixToCSV(driverLevelDensity, rootPath + caseStudy + "/Output/" + totalTime.ToString() + "driverLevelDensity.csv");
                Helpers.WriteMatrixToCSV(fireActivePoints, rootPath + caseStudy + "/Output/" + totalTime.ToString() + "flamingAmount.csv");
                Helpers.WriteMatrixToCSV(smolderActivePoints, rootPath + caseStudy + "/Output/" + totalTime.ToString() + "smolderingAmount.csv");
                Helpers.WriteMatrixToCSV(injectionHeight, rootPath + caseStudy + "/Output/" + totalTime.ToString() + "injectionHeight.csv");
                Helpers.WriteMatrixToCSV(subgridOut, rootPath + caseStudy + "/Output/" + totalTime.ToString() + "subgridTime.csv");
            }
            
            //string scriptPath = rootPath + "/visualise.py";
            //string result = Helpers.RunPythonScript(scriptPath);

        }  
    }
}
   

