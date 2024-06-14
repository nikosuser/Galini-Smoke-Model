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


namespace Galini_C_
{
    internal class Program
    {
        // read asc file
        private static double[,] GetAscFile(string rootPath, string whichFile, int skipLines = 6)
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
   


        public static void Main(string[] args)
        {
            //----------------------------INPUT VARIABLES--------------------------------------

            double windVelocity = 15;               //wind magnitude (km/h)
            double WindAngle = 30;                  //wind angle (wind vector starting from the vertical downwards, anticlockwise)
            double cellsize = 30;                   //size of each landscape point (m)
            double emissionMassFlowRate = 2000;     //emission species mass flow rate (m3/s)    (??)
            double stackDiameter = cellsize;        //effective smoke stack diameter
            double atmosphericP = 1;                //ambient pressure (bar)
            double atmosphericTemp = 300;           //Ambient temperature (K)

            double time = 60; //mins
            double burning_period = 10;

            double scaleFactor = 2;                 //Scale factor between fire domain and smoke domain


            // read burning point matrix with time
            string rootPath = System.IO.Directory.GetCurrentDirectory();
            string whichFile = "arrivalTime.asc";
            double[,] burningPointMatrix_time = GetAscFile(rootPath, whichFile);

            int width_fire = burningPointMatrix_time.GetLength(0);
            int length_fire = burningPointMatrix_time.GetLength(1);

            double[,] burningPointMatrix = new double[width_fire, length_fire];       //coordinates of points on fire (about fire domain)


            double[] fireDomainDims = [width_fire, length_fire];     //Total fire domain size 

            double[,] smokeTemp = new double[width_fire, length_fire];                 //Smoke exit temperature (C) 
            double[,] exitVelocity = new double[width_fire, length_fire];               //Smoke upwards velocity (m/s)          (??)


            // read burning points at the specific time
            
            for (int i = 0; i < width_fire; i++)
            {
                for (int j = 0; j < length_fire; j++)
                {
                    if ( burningPointMatrix_time[i, j] <= time && burningPointMatrix_time[i, j] >= (time - burning_period))
                    {
                        burningPointMatrix[i, j] = 1;
                        smokeTemp[i, j] = 200;
                        exitVelocity[i, j] = 10;
                    }
                        /*switch (burningPointMatrix[i, j])
                    {
                        case 1:
                            smokeTemp[i, j] = 200;
                            exitVelocity[i, j] = 10;
                            break;
                        case 2:
                            smokeTemp[i, j] = 100;
                            exitVelocity[i, j] = 5;
                            break;
                    }*/
                }
            }



            //in future: enforce consistent units!

            //--------------------------------------------------------------------------------
            var DispersionModelling = new DispersionModelling();

            Console.WriteLine(DispersionModelling.FindInjectionHeight_Andersen(30, 20, 100000, -6.5, -9.8, 1700, 20));



            //double[] dispCoeffOut = DispersionModelling.GetDispersionCoefficients("day", "rural", "strong", "majority", "pessimistic", 25, 3);
            //double[,] topDownRaster = DispersionModelling.DispersionModel_topDownConcentration(burningPointMatrix, scaleFactor, fireDomainDims, smokeTemp, exitVelocity, windVelocity, WindAngle, dispCoeffOut, cellsize, emissionMassFlowRate, stackDiameter, atmosphericP, atmosphericTemp);
            //double[,] driverLevelDensity = DispersionModelling.dispersionModel_driverLevel([12, 12], scaleFactor, [20, 30], 350, 10, 3, 70, dispCoeffOut, 30, 5);

            string filePath = "/output.csv";
            WriteMatrixToCSV(burningPointMatrix, System.IO.Directory.GetCurrentDirectory() + "/burningMatrix.csv");
            //WriteMatrixToCSV(topDownRaster, System.IO.Directory.GetCurrentDirectory() + filePath);
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
    }
}
