
using OSGeo.GDAL;
using OSGeo.OSR;
using System.Diagnostics;
using System.Globalization;
using System.IO;


namespace Galini_C_
{
    internal class Program
    {
        private static double[,] GetAscFile(string rootPath, string whichFile, int skipLines)
        {
            string filePath = Path.Combine(rootPath, whichFile);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The file {filePath} does not exist.");
            }

            List<double[]> matrix = new List<double[]>();

            try
            {
                Console.WriteLine("Trying to load file: " + whichFile);
                using (StreamReader reader = new StreamReader(filePath))
                {
                    for (int i = 0; i < skipLines; i++) // Skip header lines
                    {
                        reader.ReadLine();
                    }

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Split the line into elements and convert them to doubles
                        string[] elements = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        double[] row = Array.ConvertAll(elements, s => double.Parse(s, CultureInfo.InvariantCulture));
                        matrix.Add(row);
                    }
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"Error: File '{filePath}' not found.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: An unexpected error occurred - {e.Message}");
            }

            Console.WriteLine("Done loading file: " + whichFile);

            // Convert the list of rows to a 2D array
            if (matrix.Count == 0)
            {
                return new double[0, 0];
            }

            int rowCount = matrix.Count;
            int colCount = matrix[0].Length;
            double[,] resultMatrix = new double[rowCount, colCount];

            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < colCount; j++)
                {
                    resultMatrix[i, j] = matrix[i][j];
                }
            }

            return resultMatrix;
        }

        public static void WriteMatrixToCSV(double[,] matrix, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                int rows = matrix.GetLength(0);
                int cols = matrix.GetLength(1);

                for (int x = 0; x < cols; x++)
                {
                    string[] row = new string[cols];
                    for (int y = 0; y < rows; y++)
                    {
                        row[y] = matrix[x, y].ToString();
                    }
                    writer.WriteLine(string.Join(",", row));
                }
            }
        }

        private static void CreateGeoTIFF(string outputFile, double[,] data, double[] geotransform, string epsgCode)
        {
            Gdal.AllRegister();

            int ySize = data.GetLength(0);
            int xSize = data.GetLength(1);
            int bandCount = 1; // Number of bands, change as needed

            Driver driver = Gdal.GetDriverByName("GTiff");
            Dataset outputDataset = driver.Create(outputFile, xSize, ySize, bandCount, DataType.GDT_Float32, null);

            outputDataset.SetGeoTransform(geotransform);

            SpatialReference srs = new SpatialReference("");
            srs.ImportFromEPSG(int.Parse(epsgCode.Split(':')[1])); // Extract the EPSG code number
            string wkt;
            srs.ExportToWkt(out wkt);
            outputDataset.SetProjection(wkt);

            Band band = outputDataset.GetRasterBand(1);
            float[] buffer = new float[xSize * ySize];
            Buffer.BlockCopy(data, 0, buffer, 0, buffer.Length * sizeof(float));
            band.WriteRaster(0, 0, xSize, ySize, buffer, xSize, ySize, 0, 0);

            band.FlushCache();
            outputDataset.FlushCache();
            outputDataset.Dispose();
            driver.Dispose();
        }

        private static void AppendLinesToFile(string filePath, string[] lines)
        {
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                foreach (var line in lines)
                {
                    writer.WriteLine(line);
                }
            }
        }

        private static void ExecuteCommand(string command)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c " + command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = processStartInfo })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                Console.WriteLine("Standard Output:");
                Console.WriteLine(output);

                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine("Standard Error:");
                    Console.WriteLine(error);
                }

                Console.WriteLine($"Process exited with code: {process.ExitCode}");
            }
        }


        public static void Main(string[] args)
        {
            //----------------------------INPUT VARIABLES--------------------------------------

            DateTime smokeTime = new DateTime(2024, 5, 30, 13, 00, 00);

            string weatherFile = "weather.wxs";
            string arrivalTimeFile = "arrivalTime.asc";
            string rateOfSpreadFile = "rateofspread.asc";
            string firelineIntensityFile = "firelineintensity.asc";
            string fuelSBFile = "fuel.asc";

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

            double[,] weatherInput = GetAscFile(rootPath, weatherFile, 4);
            double[,] arrivalTime = GetAscFile(rootPath, arrivalTimeFile, 6);
            double[,] ROS = GetAscFile(rootPath, rateOfSpreadFile, 6);
            double[,] firelineIntensity = GetAscFile(rootPath, firelineIntensityFile, 6);
            double[,] fuelMoisture = GetAscFile(rootPath, fuelSBFile, 0);
            double[,] fuel_SB = GetAscFile(rootPath, fuelSBFile, 6);
            

            double[] fireDomainDims = [arrivalTime.GetLength(0), arrivalTime.GetLength(1)];                        //Total fire domain size 
            double[,] burningPointMatrix = new double[(int)fireDomainDims[0], (int)fireDomainDims[1]];       //coordinates of points on fire (about fire domain)
            double[,] flamingTime = burningPointMatrix;
            double[,] smolderingTime = burningPointMatrix;
            double[,] flamingEmissions = burningPointMatrix;
            double[,] smolderingEmissions = burningPointMatrix;
            double[,] flamingEmissionsFlowrate = burningPointMatrix;
            double[,] smolderingEmissionsFlowrate = burningPointMatrix;

            // read the corresponding variables 
            DateTime fireStartTime = new DateTime();

            for (int i = 0; i < weatherInput.GetLength(0); i++)
            {
                DateTime inputRowTime = new DateTime((int)weatherInput[i, 0], (int)weatherInput[i, 1], (int)weatherInput[i, 2], (int)(weatherInput[i, 3] / 100), (int)(weatherInput[i, 3] % 100), 0);

                if (i == 0)
                {
                    fireStartTime = inputRowTime;
                }
                if (Math.Abs((int)(smokeTime - inputRowTime).TotalMinutes)<30)
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
                    flamingTime[i,j] = 10;
                    smolderingTime[i,j] = 180;
                    flamingEmissions[i, j] = 2000;
                    smolderingEmissions[i, j] = 8000;
                    if (arrivalTime[i, j] <= targetArrivalTime && arrivalTime[i, j] >= (targetArrivalTime - burning_period))
                    {
                        burningPointMatrix[i, j] = 1;
                    }
                    else { burningPointMatrix[i, j] = 0; }
                    if (flamingTime[i, j] != 0)
                    {
                        flamingEmissionsFlowrate[i, j] = flamingEmissions[i, j] / flamingTime[i, j];
                    }
                    if (smolderingTime[i, j] != 0)
                    {
                        smolderingEmissionsFlowrate[i, j] = smolderingEmissions[i, j] / smolderingTime[i, j];
                    }
                }
            }

            //WriteMatrixToCSV(burningPointMatrix, System.IO.Directory.GetCurrentDirectory() + "/burningMatrix.csv");




            DispersionModelling galini = new DispersionModelling();

            double[,] fuel_FCCS = galini.SBtoFCCS(fuel_SB);



            string simulationPath = System.IO.Directory.GetCurrentDirectory(); 
            string outputFile = Path.Combine(simulationPath, "FCCS_GALINI.tif");
            string epsgCode = "EPSG:26986";
            string fofemInputFileLoc = "C:\\Users\\sx1022\\OneDrive - Imperial College London\\Documents\\FB\\TestSpatialFOFEM\\SampleData/FOFEM_GALINI.txt";

          
            // Example geodata
            double[] geodata = new double[] { 0, 1, 0, 0, 0, -1 }; // Replace with actual geotransform values

            // 1. Write GeoTIFF file
            CreateGeoTIFF(outputFile, fuel_FCCS, geodata, epsgCode);
            Console.WriteLine("FOFEM FCCS Fuel Map Saved!");

            // 2. Delete existing input file if it exists
            if (File.Exists(fofemInputFileLoc))
            {
                File.Delete(fofemInputFileLoc);
            }

            // 3. Write lines to the FOFEM input file
            AppendLinesToFile(fofemInputFileLoc, new string[]
            {
            $"FCCS_Layer_File: {outputFile}",
            "FCCS_Layer_Number: 1",
            "FOFEM_Percent_Foliage_Branch_Consumed: 75.0",
            "FOFEM_Region: I",
            "FOFEM_Season: Summer",
            $"FOFEM_10_Hour_FM: {fuelMoisture[1,3]}",
            $"FOFEM_1000_Hour_FM: {fuelMoisture[1,7]}",
            $"FOFEM_Duff_FM: {fuelMoisture[1,5]}",
            "FOFEM_FLAMING_PM25: ",
            "FOFEM_FLAMING_PM10: ",
            "FOFEM_SMOLDERING_PM25: ",
            "FOFEM_SMOLDERING_PM10: "
            });

            Console.WriteLine("FOFEM Input File Saved, Starting FOFEM!");

            // 4. Execute FOFEM command
            string commandFOFEM = @"C:\Users\nikos\Downloads\FB\FB\bin\TestSpatialFOFEM C:\Users\sx1022\OneDrive - Imperial College London\Documents\FB\TestSpatialFOFEM\SampleData\FOFEM_GALINI.txt C:\Users\sx1022\OneDrive - Imperial College London\Documents\FB\TestSpatialFOFEM\SampleData\OutputGALINI\";
            ExecuteCommand(commandFOFEM);

            Console.WriteLine("FOFEM Complete");




            double[] dispCoeffs = galini.GetDispersionCoefficients("day", "rural", "strong", "majority", "pessimistic", 25, 3);
            double[,] topDownRaster = galini.DispersionModel_topDownConcentration(config,
                                                                burningPointMatrix,
                                                                firelineIntensity,
                                                                ROS,
                                                                flamingEmissionsFlowrate,
                                                                smolderingEmissionsFlowrate,
                                                                fireDomainDims,
                                                                dispCoeffs);
            //double[,] driverLevelDensity = DispersionModelling.dispersionModel_driverLevel([12, 12], scaleFactor, [20, 30], 350, 10, 3, 70, dispCoeffOut, 30, 5);

            WriteMatrixToCSV(fuel_FCCS, System.IO.Directory.GetCurrentDirectory() + "/fuel_FCCS.csv");

            WriteMatrixToCSV(topDownRaster, System.IO.Directory.GetCurrentDirectory() +  "/output.csv");
            

        }
    }
}

