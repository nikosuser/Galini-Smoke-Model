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

namespace Galini_C_
{
    internal class Program
    {

        public static void Main(string[] args)
        {
            //----------------------------INPUT VARIABLES--------------------------------------

            double[] burningPoint = [12, 12];       //coordinates of point on fire (about fire domain)
            double scaleFactor = 5;                 //Scale factor between fire domain and smoke domain
            double[] fireDomainDims = [20, 30];     //Total fire domain size 
            double smokeTemp = 200;                 //Smoke exit temperature (C)
            double exitVelocity = 10;               //Smoke upwards velocity (m/s)              (??)
            double windVelocity = 15;               //wind magnitude (km/h)
            double WindAngle = 70;                  //wind angle (wind vector starting from the vertical downwards, anticlockwise)
            double cellsize = 30;                   //size of each landscape point (m)
            double emissionMassFlowRate = 2000;     //emission species mass flow rate (m3/s)    (??)
            double stackDiameter = cellsize;        //effective smoke stack diameter
            double atmosphericP = 1;                //ambient pressure (bar)
            double atmosphericTemp = 300;           //Ambient temperature (K)

            //in future: enforce consistent units!

            //--------------------------------------------------------------------------------
            var DispersionModelling = new DispersionModelling();
            
            double[] dispCoeffOut = DispersionModelling.GetDispersionCoefficients("day", "rural", "strong", "majority", "pessimistic", 25, 3);
            double[,] topDownRaster = DispersionModelling.DispersionModel_topDownConcentration(burningPoint, scaleFactor, fireDomainDims, smokeTemp, exitVelocity, windVelocity, WindAngle, dispCoeffOut, cellsize, emissionMassFlowRate, stackDiameter, atmosphericP, atmosphericTemp);
            //double[,] driverLevelDensity = DispersionModelling.dispersionModel_driverLevel([12, 12], scaleFactor, [20, 30], 350, 10, 3, 70, dispCoeffOut, 30, 5);

            WriteMatrixToCSV(topDownRaster, System.IO.Directory.GetCurrentDirectory() + "/output.csv");
        }
        public static void WriteMatrixToCSV(double[,] matrix, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                int rows = matrix.GetLength(0);
                int cols = matrix.GetLength(1);

                for (int y = 0; y < cols; y++)
                {
                    string[] row = new string[cols];
                    for (int x = 0; x < rows; x++)
                    {
                        row[x] = matrix[x, y].ToString();
                    }
                    writer.WriteLine(string.Join(",", row));
                }
            }
        }
    }
}
