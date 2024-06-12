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
//using GDC;

namespace Galini_C_
{
    internal class Program
    {

        public static void Main(string[] args)
        {
            var dispCoeffoutput = new GDC();
            double[] dispCoeffOut = dispCoeffoutput.GetDispersionCoefficients("day", "rural", "strong", "majority", "pessimistic", 25, 3);
            var output = new DisModel();
            double[,] topDownRaster = output.dispersionModel1(1e-5, 350, 10, 3, dispCoeffOut, 3, 5);
            double[,] driverLevelDensity = output.dispersionModel2(1e-5, 350, 10, 3, dispCoeffOut, 3, 5);
            Console.WriteLine("jj");
            OutputFile(topDownRaster, "C:\\Users\\sx1022\\Documents\\GitHub\\Galini-Smoke-Model/test.txt");
        }

        public static void OutputFile(double[,] boundary, string PerilOutput)      //output variable to a new text file, shamelessly stolen
        {
            using (var sw = new StreamWriter(PerilOutput))  //beyond here the code has been shamelessly stolen
            {
                for (int i = 0; i < boundary.GetLength(1); i++)   //for all elements in the output array
                {
                    for (int j = 0; j < boundary.GetLength(0); j++)
                    {
                        sw.Write(boundary[j, i] + " ");       //write the element in the file
                    }
                    sw.Write("\n");
                }
                sw.Flush();                                 //i dont really know
                sw.Close();                                 //close opened output text file
            }
        }
    }
}
