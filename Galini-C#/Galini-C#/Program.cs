
using OSGeo.GDAL;
using OSGeo.OSR;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Galini_C_
{
    internal class Program
    {

        public static void Main(string[] args)
        {
            //----------------------------INPUT VARIABLES--------------------------------------

            DateTime smokeTime = new DateTime(2024, 5, 30, 13, 00, 00);

            string weatherFile = "/weather.wxs";
            string arrivalTimeFile = "/arrivalTime.asc";
            string rateOfSpreadFile = "/rateofspread.asc";
            string firelineIntensityFile = "/firelineintensity.asc";
            string fuelSBFile = "/fuel.asc";
            string fuelMoistureFile = "/moisture.fms";
            string fccs_outputFileName = "/fuel_fccs.tif";
            string fofemInputFileName = "/FOFEM.txt";

            Dictionary<string, double> config = new Dictionary<string, double>()
            {
                {"windVelocity", -1 },
                {"windAngle", -1 },
                {"atmosphericTemp", -1 },
                {"atmosphericPressure", 100000 },
                {"cellsize_fire", 30 },
                {"cellsize_smoke", 45 },
                {"scaleFactor", 2 },
                {"environmentalLapseRate", -6.5 },
                {"dryAdiabaticLapseRate", -9.8 }
            };

            double burning_period = 10;             //turn to matrix

            string rootPath = Directory.GetCurrentDirectory();

            double[,] weatherInput = Helpers.GetAscFile(rootPath, weatherFile, 4);
            double[,] arrivalTime = Helpers.GetAscFile(rootPath, arrivalTimeFile, 6);
            double[,] ROS = Helpers.GetAscFile(rootPath, rateOfSpreadFile, 6);
            double[,] firelineIntensity = Helpers.GetAscFile(rootPath, firelineIntensityFile, 6);
            double[,] fuelMoisture = Helpers.GetAscFile(rootPath, fuelMoistureFile, 0);
            double[,] fuel_SB = Helpers.GetAscFile(rootPath, fuelSBFile, 6);

            FOFEM.runFOFEM(rootPath, fofemInputFileName, fccs_outputFileName, fuelMoisture);

            double[] fireDomainDims = [arrivalTime.GetLength(0), arrivalTime.GetLength(1)];                        //Total fire domain size 
            double[,] burningPointMatrix = new double[(int)fireDomainDims[0], (int)fireDomainDims[1]];       //coordinates of points on fire (about fire domain)
            double[,] flamingTime = new double[(int)fireDomainDims[0], (int)fireDomainDims[1]];
            double[,] smolderingTime = new double[(int)fireDomainDims[0], (int)fireDomainDims[1]];  //awaiting for update?
            float[,] flamingEmissions = Helpers.ReadGeoTIFFfile(rootPath + "/FOFEMoutput/_Flaming PM10.tif");
            float[,] smolderingEmissions = Helpers.ReadGeoTIFFfile(rootPath + "/FOFEMoutput/_Smoldering PM10.tif");
            double[,] flamingEmissionsFlowrate = new double[(int)fireDomainDims[0], (int)fireDomainDims[1]];
            double[,] smolderingEmissionsFlowrate = new double[(int)fireDomainDims[0], (int)fireDomainDims[1]];

            //outputs are in pounds per acre, so we convert them to grams per square meter

            for (int i=0; i<fireDomainDims[0]; i++)
            {
                for (int j = 0; j < fireDomainDims[1]; j++)
                {
                    flamingEmissions[i, j] = (float)(0.112085 * flamingEmissions[i, j]);
                    smolderingEmissions[i, j] = (float)(0.112085 * smolderingEmissions[i, j]);
                }
            }

            // read the corresponding variables 
            DateTime fireStartTime = new DateTime();

            for (int i = 0; i < weatherInput.GetLength(0); i++)
            {
                DateTime inputRowTime = new DateTime((int)weatherInput[i, 0], (int)weatherInput[i, 1], (int)weatherInput[i, 2], (int)(weatherInput[i, 3] / 100), (int)(weatherInput[i, 3] % 100), 0);

                if (i == 0)
                {
                    fireStartTime = inputRowTime;
                }
                if (Math.Abs((int)(smokeTime - inputRowTime).TotalMinutes) < 30)
                {
                    config["atmosphericTemp"] = weatherInput[i, 4];
                    config["windVelocity"] = weatherInput[i, 7];
                    config["WindAngle"] = weatherInput[i, 8];
                    break;
                }
            }

            double targetArrivalTime = (smokeTime - fireStartTime).TotalMinutes;
            for (int i = 0; i < fireDomainDims[0]; i++)
            {
                for (int j = 0; j < fireDomainDims[1]; j++)
                {
                    flamingTime[i, j] = 10;
                    smolderingTime[i, j] = 180;
                    flamingEmissions[i, j] = 2000;
                    smolderingEmissions[i, j] = 8000;
                    if (arrivalTime[i, j] <= targetArrivalTime && arrivalTime[i, j] >= (targetArrivalTime - burning_period))
                    {
                        burningPointMatrix[i, j] = 1;
                    }
                    else { burningPointMatrix[i, j] = 0; }
                    if (flamingTime[i, j] + 1 != 0)
                    {
                        flamingEmissionsFlowrate[i, j] = flamingEmissions[i, j] / (1+flamingTime[i, j]);
                    }
                    if (smolderingTime[i, j] + 1 != 0)
                    {
                        smolderingEmissionsFlowrate[i, j] = smolderingEmissions[i, j] / (1+smolderingTime[i, j]);
                    }
                }
            }
            double[] dispCoeffs = DispersionModelling.GetDispersionCoefficients("day", "rural", "strong", "majority", "pessimistic", 25, 3);

            double[,] topDownRaster;
            double[,] driverLevelDensity;
            (topDownRaster, driverLevelDensity) = DispersionModelling.DispersionModel(config,
                                                                burningPointMatrix,
                                                                firelineIntensity,
                                                                ROS,
                                                                flamingEmissionsFlowrate,
                                                                smolderingEmissionsFlowrate,
                                                                fireDomainDims,
                                                                dispCoeffs);

         

            Helpers.WriteMatrixToCSV(topDownRaster, System.IO.Directory.GetCurrentDirectory() + "/topDownRaster.csv");
            Helpers.WriteMatrixToCSV(driverLevelDensity, System.IO.Directory.GetCurrentDirectory() + "/driverLevelDensity.csv");
            
            string scriptPath = System.IO.Directory.GetCurrentDirectory() + "/visualise.py";
            string result = Helpers.RunPythonScript(scriptPath);

        }  
    }
}
   

