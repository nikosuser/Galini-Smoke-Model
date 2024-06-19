using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using Microsoft.VisualBasic;


namespace Galini_C_
{
    internal class Program
    {
        // read asc file
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
        // write csv file
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

            double windVelocity = 20;               // initialize wind magnitude (km/h)
            double WindAngle = 180;                  //initialize wind angle (wind vector starting from the vertical downwards, anticlockwise)
            double atmosphericTemp = 300;                 //initialize Ambient temperature (K)
            double cellsize_Fire = 30;                   //size of each landscape point (m)
            double cellsize_Smoke = 50;

            double emissionMassFlowRate = 2000;     //emission species mass flow rate (m3/s)    (??)
            double stackDiameter = cellsize_Fire;        //effective smoke stack diameter
            double atmosphericP = 1;                //ambient pressure (bar)
            

            double time = 60; //mins
            double burning_period = 10;

            double scaleFactor = 2;                 //Scale factor between fire domain and smoke domain


            // Read the input matrix from the weather.wxs file
            string filePath1 ="weather.wxs";
            string rootPath = System.IO.Directory.GetCurrentDirectory();
            double[,] Weather_variables = GetAscFile(rootPath, filePath1, 4);
            // read burning point matrix with time
            string whichFile = "arrivalTime.asc";
            double[,] burningPointMatrix_time = GetAscFile(rootPath, whichFile, 6);

            // ask for the specific date and time
            double Year = 2024;
            double Month = 5;
            double Day = 30;
            double Hour = 1200;
           /* Console.WriteLine("Input the year");
            double Year = Convert.ToDouble(Console.ReadLine());
            Console.WriteLine("Input the month");
            double Month = Convert.ToDouble(Console.ReadLine());
            Console.WriteLine("Input the day");
            double Day = Convert.ToDouble(Console.ReadLine());
            Console.WriteLine("Input the hour");
            double Hour = Convert.ToDouble(Console.ReadLine());*/

           // read the corresponding variables 
            for (int i = 0; i < Weather_variables.GetLength(0); i++)
            {
                if (Weather_variables[i,0] == Year && Weather_variables[i, 1] == Month && Weather_variables[i, 2] == Day && Weather_variables[i, 3] == Hour)
                {
                    atmosphericTemp = Weather_variables[i, 4];
                    windVelocity = Weather_variables[i, 7];
                    WindAngle = Weather_variables[i, 8];
                    break;
                }
            }

            //
            var DispersionModelling = new DispersionModelling();

            int width_fire = burningPointMatrix_time.GetLength(0);
            int length_fire = burningPointMatrix_time.GetLength(1);

            double width_smoke = scaleFactor * width_fire * cellsize_Fire / cellsize_Smoke;
            double length_smoke = scaleFactor * length_fire * cellsize_Fire / cellsize_Smoke;
            double[,] burningPointMatrix_smoke_time = new double[(int)width_smoke, (int)length_smoke];
            double[,] burningPointMatrix_smoke = new double[(int)width_smoke, (int)length_smoke];

            double[] XburningPoint_fireToSmokeDomian_inMeter = new double[width_fire];
            double[] YburningPoint_fireToSmokeDomian_inMeter = new double[length_fire];


            for (int i = 0; i < width_smoke; i++)
            {
                double x = i * cellsize_Smoke;

                for (int j = 0; j < length_smoke; j++)
                {
                    double y = j * cellsize_Smoke;

                    for (int a = 0; a < (width_fire - 1); a++)
                    {
                        XburningPoint_fireToSmokeDomian_inMeter[a] = (scaleFactor * width_fire  / 2 - (width_fire / 2 - a)) * cellsize_Fire;
                        double x1 = XburningPoint_fireToSmokeDomian_inMeter[a];
                        double x2 = XburningPoint_fireToSmokeDomian_inMeter[a + 1];

                        for (int b = 0; b < (length_fire - 1); b++)
                        {
                            YburningPoint_fireToSmokeDomian_inMeter[b] = (scaleFactor * length_fire / 2 - (length_fire / 2 - b)) * cellsize_Fire;
                            double y1 = XburningPoint_fireToSmokeDomian_inMeter[b];
                            double y2 = XburningPoint_fireToSmokeDomian_inMeter[b + 1];

                            double Q11 = burningPointMatrix_time[a, b];
                            double Q12 = burningPointMatrix_time[a, b + 1];
                            double Q21 = burningPointMatrix_time[a + 1, b];
                            double Q22 = burningPointMatrix_time[a + 1, b + 1];

                            if (x >= x1 && x <= x2
                                && y >= y1 && y <= y2)
                            {
                                burningPointMatrix_smoke_time[i, j] = DispersionModelling.Interpolate(x, y, x1, y1, x2, y2, Q11, Q12, Q21, Q22);

                                if (burningPointMatrix_time[i, j] <= time && burningPointMatrix_time[i, j] >= (time - burning_period))
                                {
                                    burningPointMatrix_smoke[i, j] = 1;
                                }
                            }
                        }
                    }
                }
            }
            WriteMatrixToCSV(burningPointMatrix_smoke, System.IO.Directory.GetCurrentDirectory() + "/burningMatrixSmoke.csv");
            

            //in future: enforce consistent units!

            //--------------------------------------------------------------------------------
            /*
            var DispersionModelling = new DispersionModelling();

            Console.WriteLine(DispersionModelling.FindInjectionHeight_Andersen(30, 20, 100000, -6.5, -9.8, 1700, 20));



            double[] dispCoeffOut = DispersionModelling.GetDispersionCoefficients("day", "rural", "strong", "majority", "pessimistic", 25, 3);
            double[,] topDownRaster = DispersionModelling.DispersionModel_topDownConcentration(burningPointMatrix, scaleFactor, fireDomainDims, smokeTemp, exitVelocity, windVelocity, WindAngle, dispCoeffOut, cellsize_Fire, emissionMassFlowRate, stackDiameter, atmosphericP, atmosphericTemp);
            //double[,] driverLevelDensity = DispersionModelling.dispersionModel_driverLevel([12, 12], scaleFactor, [20, 30], 350, 10, 3, 70, dispCoeffOut, 30, 5);

            string filePath = "/output.csv";
            WriteMatrixToCSV(burningPointMatrix, System.IO.Directory.GetCurrentDirectory() + "/burningMatrix.csv");
            WriteMatrixToCSV(topDownRaster, System.IO.Directory.GetCurrentDirectory() + filePath);
            */

        }
        
    }
}
