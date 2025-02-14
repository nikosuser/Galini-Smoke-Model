using System;
using System.Collections.Generic;
using System.IO;

namespace Galini_C_
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            //----------------------------INPUT VARIABLES--------------------------------------
            //string? rootPath = Environment.GetEnvironmentVariable("ROOT_PATH");
            string rootPath =
                Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            if (rootPath == null)
            {
                throw new DirectoryNotFoundException("The root directory could not be found. Set it as an environment variable.");
            }
            const int timeSteps = 30; //minutes per time step

            const bool debug = true;
            
            const string weatherFile = "/weather.wxs";
            const string fccsOutputFileName = "/fuel_fccs.tif";
            const string fofemInputFileName = "/FOFEM.txt";

            if (Directory.Exists(rootPath + "/Output/"))
            {
                Directory.Delete(rootPath + "/Output/", true);
            }
            Directory.CreateDirectory(rootPath + "/Output/");
            if (!File.Exists(rootPath + fccsOutputFileName))
            {
                File.Delete(rootPath + fccsOutputFileName);
            }

            double[,] weatherInput = Helpers.GetAscFile(rootPath, "/weather.wxs", 4);
            double[,] arrivalTime = Helpers.GetAscFile(rootPath, "/arrivalTime.asc", 6);
            double[,] ros = Helpers.GetAscFile(rootPath, "/rateofspread.asc", 6);
            double[,] fuelMoisture = Helpers.GetAscFile(rootPath, "/moisture.fms", 0);
            double[,] elevation= Helpers.GetAscFile(rootPath, "/elevation.asc", 6);
            double[,] firelineIntensity = Helpers.GetAscFile(rootPath, "/firelineIntensity.asc", 6);

            double[] rosHeader = Helpers.GetAscHeader(rootPath, "/rateofspread.asc");
            double[] elevationHeader = Helpers.GetAscHeader(rootPath, "/rateofspread.asc");
            double[] rasterDelta = [(rosHeader[2] - elevationHeader[2])/rosHeader[4],(rosHeader[3] - elevationHeader[3])/rosHeader[4]];
            
            Dictionary<string, double> config = new Dictionary<string, double>()
            {
                {"windVelocity", -1 },
                {"windAngle", -1 },
                {"atmosphericTemp", 20 },
                {"atmosphericPressure", 100000 },
                {"cellsize", elevationHeader[4] },
                {"environmentalLapseRate", -6.5 },
                {"dryAdiabaticLapseRate", -9.8 },
                {"RAWSelevation", Helpers.GetRawsElevation(rootPath + weatherFile) }
            };

            Fofem.RunFofem(rootPath, fofemInputFileName, fccsOutputFileName, fuelMoisture);

            double[] fireDomainDims = [arrivalTime.GetLength(0), arrivalTime.GetLength(1)];                        //Total fire domain size 
            double[,] burningPointMatrix = new double[(int)fireDomainDims[0], (int)fireDomainDims[1]];       //coordinates of points on fire (about fire domain)
            double[,] flamingTime = new double[(int)fireDomainDims[0], (int)fireDomainDims[1]];
            double[,] smolderingTime = new double[(int)fireDomainDims[0], (int)fireDomainDims[1]];  //awaiting for update?
            double[,] flamingEmissions = Helpers.ReadGeoTifFfile(rootPath + "/FOFEMoutput/_Flaming PM10.tif");
            double[,] smolderingEmissions = Helpers.ReadGeoTifFfile(rootPath + "/FOFEMoutput/_Smoldering PM10.tif");
            
            //outputs are in pounds per acre, so we convert them to micrograms per square meter

            for (int i=0; i<fireDomainDims[0]; i++)
            {
                for (int j = 0; j < fireDomainDims[1]; j++)
                {
                    //burningPointMatrix[i,j] = (arrivalTime[i,j] <= flamingTime[i,j] + smolderingTime[i,j] + config["cellsize_fire"] / ROS[i,j]) ? 1 : 0; 
                    flamingEmissions[i, j] = (float)(112085 * flamingEmissions[i, j]);
                    smolderingEmissions[i, j] = (float)(112085 * smolderingEmissions[i, j]);

                    //fill times with dummy variables for now
                    flamingTime[i, j] = 15;
                    smolderingTime[i, j] = 120;
                }
            }

            int totalWeatherInputs = weatherInput.GetLength(0)-1;
            DateTime fireEndTime = new DateTime((int)weatherInput[totalWeatherInputs, 0], (int)weatherInput[totalWeatherInputs, 1], (int)weatherInput[totalWeatherInputs, 2], (int)(weatherInput[totalWeatherInputs, 3] / 100), (int)(weatherInput[totalWeatherInputs, 3] % 100), 0);
            DateTime fireStartTime = new DateTime((int)weatherInput[0, 0], (int)weatherInput[0, 1], (int)weatherInput[0, 2], (int)(weatherInput[0, 3] / 100), (int)(weatherInput[0, 3] % 100), 0);

            int totalSteps = (int)((fireEndTime - fireStartTime).TotalMinutes / timeSteps);

            //----------------------    interpolate wind values -------------------------
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
            
            //-----------------------   Start simulating smoke  -------------------------

            
            for (int totalTime = 0; totalTime < (fireEndTime - fireStartTime).TotalMinutes; totalTime += timeSteps)
            {
                int burningPoints = 0;
                Console.WriteLine($"Elapsed Minutes: {totalTime} of {(fireEndTime - fireStartTime).TotalMinutes}");
                
                double[,] subgridTime = new double[(int)fireDomainDims[0], (int)fireDomainDims[1]];
                for (int i = 0; i < fireDomainDims[0]; i++)
                {
                    for (int j = 0; j < fireDomainDims[1]; j++)
                    {
                        if (arrivalTime[i, j] >= 0 && arrivalTime[i,j]<=totalTime)
                        {
                            burningPointMatrix[i, j] = 1;
                            burningPoints++;
                            subgridTime[i,j] = totalTime - arrivalTime[i, j];
                            subgridTime[i, j] = (subgridTime[i, j] >= 0) ? subgridTime[i, j] : 0; 
                        }
                    }
                }
                Console.WriteLine($"Burning Points: {burningPoints}");

                DispersionModelling gaussian = new DispersionModelling(rasterDelta, elevation, config, ros,
                    firelineIntensity, flamingEmissions, smolderingEmissions, flamingTime, smolderingTime);
                
                gaussian.GetStabilityClass("day",  "moderate", "majority", "pessimistic", 25, config["windVelocity"]);
                gaussian.GetDispersionCoefficients("rural");
                
                gaussian.currentlyBurning = burningPointMatrix;
                gaussian.SubgridTime = subgridTime;
                
                gaussian.Config["windVelocity"] = windMagPerStep[(int)(totalTime / timeSteps)];
                gaussian.Config["windAngle"] = windDirPerStep[(int)(totalTime / timeSteps)];
                
                gaussian.DispersionModel(debug);
                
                gaussian.SaveTimestep(rootPath+"/Output/",totalTime,debug);

            }
            //string scriptPath = rootPath + "/visualise.py";
            //string result = Helpers.RunPythonScript(scriptPath);
        }  
    }
}
   

