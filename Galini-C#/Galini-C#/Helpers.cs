using OSGeo.GDAL;
using OSGeo.OSR;
using System.Diagnostics;
using System.Globalization;

namespace Galini_C_
{
    public class Helpers
    {
        /// <summary>
        /// Get the elevation value of a RAWS weather file
        /// </summary>
        /// <param name="filePath">Path to selected RAWS file (string)</param>
        /// <returns>The elevation value (double)</returns>
        public static double GetRawsElevation(string filePath)
        {       
            try
            {
                foreach (string line in File.ReadLines(filePath))
                {
                    if (line.StartsWith("RAWS_ELEVATION:"))
                    {
                        string elevationStr = line.Split(':')[1].Trim();
                        if (double.TryParse(elevationStr, out double elevation))
                        {
                            return elevation;
                        }
                        else
                        {
                            Console.WriteLine("Failed to convert RAWS_ELEVATION to double.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            return 0;
        }
        /// <summary>
        /// Save raster to geoTiff file
        /// </summary>
        /// <param name="outputPath">Full path and name of target geoTIFF file (string)</param>
        /// <param name="dataArray">Matrix to be saved (double[,])</param>
        public static void SaveGeoTiff(string outputPath,double[,] dataArray)
        {
            GdalConfiguration.ConfigureGdal();
            Gdal.AllRegister();

            // Define the dimensions of the array
            int rows = dataArray.GetLength(0);
            int cols = dataArray.GetLength(1);

            // Create a new GeoTIFF file
            Driver driver = Gdal.GetDriverByName("GTiff");
            Dataset dataset = driver.Create(outputPath, cols, rows, 1, DataType.GDT_Float64, null);

            // Create a Spatial Reference (optional)
            SpatialReference srs = new SpatialReference("");
            srs.SetWellKnownGeogCS("WGS84");
            string wkt;
            srs.ExportToWkt(out wkt, null);
            dataset.SetProjection(wkt);

            // Set the GeoTransform (optional, defines the position and pixel size)
            double[] geoTransform = { 0, 1, 0, 0, 0, -1 }; // Example values
            dataset.SetGeoTransform(geoTransform);

            // Get the band and write the data
            Band band = dataset.GetRasterBand(1);
            double[] dataBuffer = new double[rows * cols];
            Buffer.BlockCopy(dataArray, 0, dataBuffer, 0, rows * cols * sizeof(double));
            band.WriteRaster(0, 0, cols, rows, dataBuffer, cols, rows, 0, 0);

            // Flush data to disk and clean up
            band.FlushCache();
            dataset.FlushCache();
            dataset.Dispose();

            Console.WriteLine("GeoTIFF file created successfully.");
        }
        /// <summary>
        /// Save raster in geoTIFF file by copying the projection information of another geoTIFF file.
        /// </summary>
        /// <param name="inputFile">Path to the sample geotiff file (string)</param>
        /// <param name="outputFile">Path and name of the output file (string)</param>
        /// <param name="data">Matrix to be saved (float[,])</param>
        public static void SaveGeoTifFfileByCopy(string inputFile, string outputFile, float[,] data)
        {
            GdalConfiguration.ConfigureGdal();
            Gdal.AllRegister();
            Dataset ds = Gdal.Open(inputFile, Access.GA_ReadOnly);
            Driver drv = ds.GetDriver();

            Band band0 = ds.GetRasterBand(1);
            int width = band0.XSize;
            int height = band0.YSize;
            int size = width * height;
            double min = 0.00;
            double max = 0.00;
            double mean = 0.00;
            double stddev = 0.00;

            var stats = band0.GetStatistics(1, 0, out min, out max, out mean, out stddev);

            Driver drvTiff = Gdal.GetDriverByName("GTiff");
            string[] options = null;
            Dataset tiffOutput = drvTiff.CreateCopy(outputFile, ds, 0, options, null, null);
            Band outputBand = tiffOutput.GetRasterBand(1);

            float[] linearData = new float[width * height];

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    linearData[i * width + j] = data[i, j];
                }
            }
            outputBand.WriteRaster(0, 0, band0.XSize, band0.YSize, linearData, band0.XSize, band0.YSize, 0, 0);

            tiffOutput.SetProjection(ds.GetProjection());

            Console.WriteLine("Output dataset parameters:");
            Console.WriteLine("  Projection: " + tiffOutput.GetProjectionRef());
            Console.WriteLine("  RasterCount: " + tiffOutput.RasterCount);
            Console.WriteLine("  RasterSize (" + tiffOutput.RasterXSize + "," + tiffOutput.RasterYSize + ")");

            tiffOutput.FlushCache();
            tiffOutput.Dispose();
        }
        
        /// <summary>
        /// Read a single-band geoTIFF raster file and get the raster. 
        /// </summary>
        /// <param name="inputFile">Full path to the geoTIFF file (string)</param>
        /// <returns>The matrix contained in the geoTIFF file without header information (double[,])</returns>
        public static double[,] ReadGeoTifFfile(string inputFile)
        {
            GdalConfiguration.ConfigureGdal();
            Gdal.AllRegister();

            /* -------------------------------------------------------------------- */
            /*      Open dataset.                                                   */
            /* -------------------------------------------------------------------- */
            Dataset ds = Gdal.Open(inputFile, Access.GA_ReadOnly);

            if (ds == null)
            {
                Console.WriteLine("Can't open fuel file, null Dataset input");
                System.Environment.Exit(-1);
            }

            Console.WriteLine("Raster dataset parameters:");
            Console.WriteLine("  Projection: " + ds.GetProjectionRef());
            Console.WriteLine("  RasterCount: " + ds.RasterCount);
            Console.WriteLine("  RasterSize (" + ds.RasterXSize + "," + ds.RasterYSize + ")");

            /* -------------------------------------------------------------------- */
            /*      Get driver                                                      */
            /* -------------------------------------------------------------------- */
            Driver drv = ds.GetDriver();

            if (drv == null)
            {
                Console.WriteLine("Can't get driver.");
                System.Environment.Exit(-1);
            }

            Console.WriteLine("Using driver " + drv.LongName);

            Band band0 = ds.GetRasterBand(1);
            int width = band0.XSize;
            int height = band0.YSize;
            int size = width * height;
            double min = 0.00;
            double max = 0.00;
            double mean = 0.00;
            double stddev = 0.00;

            var stats = band0.GetStatistics(1, 0, out min, out max, out mean, out stddev);

            DataType type = band0.DataType;

            float[] data = new float[size];
            double[,] dataMatrix = new double[width, height];
            var dataArr = band0.ReadRaster(0, 0, width, height, data, width, height, 0, 0);

            double[,] output = new double[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    output[i, j] = data[i * width + j];
                }
            }
            ds.FlushCache();
            ds.Dispose();
            return output;
        }
        
        /// <summary>
        /// Run a python script (currently not used)
        /// </summary>
        /// <param name="scriptPath">Path to python script (string)</param>
        /// <returns>Errors generated by script execution (script)</returns>
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
        /// <summary>
        /// In the gaussian dispersion model, the isocontour of a specific concentration will form a teardrop-like shape. Finding the maximum dimensions of this teardrop is useful for certain calculations.
        /// </summary>
        /// <param name="u">Wind velocity in m/s (double)</param>
        /// <param name="thr">Smoke concentration threshold in g/m3 (double)</param>
        /// <param name="q">Smoke injection mass flow rate in g/s (double)</param>
        /// <param name="dispCoeff">The parameters to calculate dispersion coefficients using Casal (double[7]).</param>
        /// <param name="steadyStateHeight">The smoke injection height in m (double)</param>
        /// <param name="xorYorZ">Specify whether the maximum dimensions desired is the "X", "Y" or "Z" value (string)</param>
        /// <returns>The value of the maximum dimensions of this concentration's contour (double)</returns>
        public double FindMax(double u, double thr, double q, double[] dispCoeff, double steadyStateHeight, string xorYorZ)
        {
            double pointCurrent = 0;
            double point = -1;
            double x = 0;
            double step = 30;
            double curconc = 2 * thr;

            double dispCoeffY = 0;
            double dispCoeffZ = 0;
            double choice = 0;

            if (xorYorZ == "x")
            {
                while (curconc > thr)
                {
                    pointCurrent += step;
                    dispCoeffY = dispCoeff[0] * pointCurrent * Math.Pow(1 + dispCoeff[1] * pointCurrent, dispCoeff[2]);
                    dispCoeffZ = dispCoeff[3] * pointCurrent * Math.Pow(1 + dispCoeff[4] * pointCurrent, dispCoeff[5]);
                    curconc = (1 + Math.Exp(-4 * Math.Pow(steadyStateHeight, 2) / (2 * Math.Pow(dispCoeffZ, 2)))) * q / (Math.Sqrt(2 * Math.PI) * u * dispCoeffZ * dispCoeffY);
                }
            }
            else
            {
                if (xorYorZ == "y")
                {
                    choice = 1;
                }
                else if (xorYorZ == "z")
                {
                    choice = 0;
                }
                while (pointCurrent > point)
                {
                    x += step;
                    point = pointCurrent;
                    dispCoeffY = dispCoeff[0] * x * Math.Pow(1 + dispCoeff[1] * x, dispCoeff[2]);
                    dispCoeffZ = dispCoeff[3] * x * Math.Pow(1 + dispCoeff[4] * x, dispCoeff[5]);
                    pointCurrent = Math.Sqrt(-Math.Log(Math.Sqrt(2 * Math.PI) * u * thr * dispCoeffZ * dispCoeffY / q) * 2) * (Math.Pow(dispCoeffY, choice) * Math.Pow(dispCoeffZ, 1 - choice));
                }
            }
            return pointCurrent;
        }

        /// <summary>
        /// Read asc geofile and get its contained raster
        /// </summary>
        /// <param name="rootPath">Folder containing the file (string)</param>
        /// <param name="whichFile">Specified .asc file (include .asc file extention) (string)</param>
        /// <param name="skipLines">Skip header lines (int, 6 by default)</param>
        /// <returns>The raster contained in the .asc (double[,])</returns>
        /// <exception cref="FileNotFoundException"> The file rootpath/whichFile was not found</exception>
        public static double[,] GetAscFile(string rootPath, string whichFile, int skipLines = 6)
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
        /// <summary>
        /// Save matrix as csv
        /// </summary>
        /// <param name="matrix">Matrix to be saved (double[,])</param>
        /// <param name="filePath">Saved filename (including file extention .csv) (string)</param>
        public static void WriteMatrixToCsv(double[,] matrix, string filePath)
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
        /// <summary>
        /// Run command in internal command line
        /// </summary>
        /// <param name="command">full command (string)</param>
        public static void ExecuteCommand(string command)
        {
            Console.WriteLine(command);
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
        /// <summary>
        /// Get the header information of an .asc geofile
        /// </summary>
        /// <param name="rootPath">The folder containing the file (string)</param>
        /// <param name="file">The name of the asc file (including .asc) (string)</param>
        /// <returns>The parameters in the header (double[6])</returns>
        /// <exception cref="FileNotFoundException">File was not found in rootPath/file</exception>
        /// <exception cref="FormatException">The selected file does not have any data</exception>
        public static double[] GetAscHeader(string rootPath, string file)
        {
            string filePath = rootPath + file;
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"The file {filePath} does not exist.");

            double[] headerLines = new double[6];
            using (StreamReader reader = new StreamReader(filePath))
            {
                for (int i = 0; i < 6; i++)
                {
                    string line = reader.ReadLine();
                    if (line == null)
                        throw new FormatException($"Unexpected end of file while reading the header at line {i + 1}.");
                
                    headerLines[i] = double.Parse(line.Trim().Split(" ")[^1]); // Store the entire line in the array
                }
            }
            return headerLines;
        }
    }
}
