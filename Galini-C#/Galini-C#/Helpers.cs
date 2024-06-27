using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Galini_C_
{
    public class Helpers
    {
        public static string RunPythonScript(string scriptPath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "python", // Use "python3" on some systems, or the full path to the Python interpreter
                Arguments = scriptPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(error))
                {
                    return $"Error: {error}";
                }

                return output;
            }
        }

        public double FindMax(double U, double Thr, double Q, double[] dispCoeff, double steadyStateHeight, string XorYorZ)
        {
            double point_current = 0;
            double point = -1;
            double x = 0;
            double step = 30;
            double curconc = 2 * Thr;

            double dispCoeffY = 0;
            double dispCoeffZ = 0;
            double choice = 0;

            if (XorYorZ == "x")
            {
                while (curconc > Thr)
                {
                    point_current += step;
                    dispCoeffY = dispCoeff[0] * point_current * Math.Pow(1 + dispCoeff[1] * point_current, dispCoeff[2]);
                    dispCoeffZ = dispCoeff[3] * point_current * Math.Pow(1 + dispCoeff[4] * point_current, dispCoeff[5]);
                    curconc = (1 + Math.Exp(-4 * Math.Pow(steadyStateHeight, 2) / (2 * Math.Pow(dispCoeffZ, 2)))) * Q / (Math.Sqrt(2 * Math.PI) * U * dispCoeffZ * dispCoeffY);
                }
            }
            else
            {
                if (XorYorZ == "y")
                {
                    choice = 1;
                }
                else if (XorYorZ == "z")
                {
                    choice = 0;
                }
                while (point_current > point)
                {
                    x += step;
                    point = point_current;
                    dispCoeffY = dispCoeff[0] * x * Math.Pow(1 + dispCoeff[1] * x, dispCoeff[2]);
                    dispCoeffZ = dispCoeff[3] * x * Math.Pow(1 + dispCoeff[4] * x, dispCoeff[5]);
                    point_current = Math.Sqrt(-Math.Log(Math.Sqrt(2 * Math.PI) * U * Thr * dispCoeffZ * dispCoeffY / Q) * 2) * (Math.Pow(dispCoeffY, choice) * Math.Pow(dispCoeffZ, 1 - choice));
                }
            }

            return point_current;


        }

        public static double[,] GetAscFile(string rootPath, string whichFile, int skipLines)
        {
            string filePath = rootPath + whichFile;

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
            Console.WriteLine($"Output {filePath} saved!");
        }

        public static void AppendLinesToFile(string filePath, string[] lines)
        {
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                foreach (var line in lines)
                {
                    writer.WriteLine(line);
                }
            }
        }

        public static void ExecuteCommand(string command)
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
                process.StartInfo.WorkingDirectory = "d:";
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
    }
}
