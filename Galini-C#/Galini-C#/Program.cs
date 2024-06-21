
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

        public static void Main(string[] args)
        {
            //----------------------------INPUT VARIABLES--------------------------------------

            DateTime smokeTime = new DateTime(2024, 5, 30, 14, 00, 00);

            string weatherFile = "weather.wxs";
            string arrivalTimeFile = "arrivalTime.asc";
            string rateOfSpreadFile = "rateofspread.asc";
            string firelineIntensityFile = "firelineintensity.asc";

            Dictionary<string, double> config = new Dictionary<string, double>()
            {
                {"windVelocity", -1 },
                {"windAngle", -1 },
                {"atmosphericTemp", -1 },
                {"atmosphericPressure", 100000 },
                {"cellsize_fire", 30 },
                {"cellsize_smoke", 45 },
                {"scaleFactor", 3 },
                {"environmentalLapseRate", -6.5 },
                {"dryAdiabaticLapseRate", -9.8 }
            };

            double burning_period = 10;             //turn to matrix

            string rootPath = Directory.GetCurrentDirectory();

            double[,] weatherInput = GetAscFile(rootPath, weatherFile, 4);
            double[,] arrivalTime = GetAscFile(rootPath, arrivalTimeFile, 6);
            double[,] ROS = GetAscFile(rootPath, rateOfSpreadFile, 6);
            double[,] firelineIntensity = GetAscFile(rootPath, firelineIntensityFile, 6);

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
                if (Math.Abs((int)(smokeTime - inputRowTime).TotalMinutes)<50)
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

            WriteMatrixToCSV(burningPointMatrix, System.IO.Directory.GetCurrentDirectory() + "/burningMatrix.csv");

            DispersionModelling galini = new DispersionModelling();

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

            string filePath = "/output.csv";
            
            WriteMatrixToCSV(topDownRaster, System.IO.Directory.GetCurrentDirectory() + filePath);
            

        }
    }
}

