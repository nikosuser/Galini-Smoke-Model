using OSGeo.GDAL;
using OSGeo.OSR;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Galini_C_
{
    internal class FOFEM
    {
        public static void usage()

        {
            Console.WriteLine("usage: gdaldatasetwrite {dataset name}");
            System.Environment.Exit(-1);
        }

        public static void save(string[] args)
        {

            if (args.Length < 1) usage();

            int bXSize, bYSize;
            int w, h;

            w = 100;
            h = 100;

            if (args.Length > 1)
                w = int.Parse(args[1]);

            if (args.Length > 2)
                h = int.Parse(args[2]);

            bXSize = w;
            bYSize = 1;

            //try 
            {
                /* -------------------------------------------------------------------- */
                /*      Register driver(s).                                             */
                /* -------------------------------------------------------------------- */
                Gdal.AllRegister();

                /* -------------------------------------------------------------------- */
                /*      Get driver                                                      */
                /* -------------------------------------------------------------------- */
                Driver drv = Gdal.GetDriverByName("GTiff");

                if (drv == null)
                {
                    Console.WriteLine("Can't get driver.");
                    System.Environment.Exit(-1);
                }

                Console.WriteLine("Using driver " + drv.LongName);

                /* -------------------------------------------------------------------- */
                /*      Open dataset.                                                   */
                /* -------------------------------------------------------------------- */
                Dataset ds = drv.Create(args[0], w, h, 3, DataType.GDT_Byte, null);

                if (ds == null)
                {
                    Console.WriteLine("Can't create " + args[0]);
                    System.Environment.Exit(-1);
                }

                /* -------------------------------------------------------------------- */
                /*      Preparing the data in a byte buffer.                            */
                /* -------------------------------------------------------------------- */

                byte[] buffers = new byte[w * h * 3];

                for (int i = 0; i < w; i++)
                {
                    for (int j = 0; j < h; j++)
                    {
                        buffers[(i * w + j)] = (byte)(256 - i * 256 / w);
                        buffers[w * h + (i * w + j)] = 0;
                        buffers[2 * w * h + (i * w + j)] = (byte)(i * 256 / w);
                    }
                }

                int[] iBandMap = { 1, 2, 3 };
                ds.WriteRaster(0, 0, w, h, buffers, w, h, 3, iBandMap, 0, 0, 0);

                ds.FlushCache();

            }
        }
        public static void saveFCCStiff(string inputFile, string outputFile)
        {

            //-----------------------------------------------------------------------------------------------------

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

            /* -------------------------------------------------------------------- */
            /*      Get raster band                                                 */
            /* -------------------------------------------------------------------- */
            for (int iBand = 1; iBand <= ds.RasterCount; iBand++)
            {
                Band band = ds.GetRasterBand(iBand);
                Console.WriteLine("Band " + iBand + " :");
                Console.WriteLine("   DataType: " + band.DataType);
                Console.WriteLine("   Size (" + band.XSize + "," + band.YSize + ")");
                Console.WriteLine("   PaletteInterp: " + band.GetRasterColorInterpretation().ToString());

                for (int iOver = 0; iOver < band.GetOverviewCount(); iOver++)
                {
                    Band over = band.GetOverview(iOver);
                    Console.WriteLine("      OverView " + iOver + " :");
                    Console.WriteLine("         DataType: " + over.DataType);
                    Console.WriteLine("         Size (" + over.XSize + "," + over.YSize + ")");
                    Console.WriteLine("         PaletteInterp: " + over.GetRasterColorInterpretation().ToString());
                }
            }
            Band band0 = ds.GetRasterBand(0);
            int width = band0.XSize;
            int height = band0.YSize;
            int size = width * height;
            double min = 0.00;
            double max = 0.00;
            double mean = 0.00;
            double stddev = 0.00;

            var stats = band0.GetStatistics(1, 0, out min, out max, out mean, out stddev);

            //Console.WriteLine($"Statistics retrieved and returned a result of {stats}");
            Console.WriteLine($"X : {width} Y : {height} SIZE: {size}");
            Console.WriteLine($"MIN : {min} MAX : {max} MEAN : {mean} STDDEV : {stddev}");
            DataType type = band0.DataType;
            Console.WriteLine($"Data Type : {type}");

            float gtMean = 0; //cut
            float ltMean = 0; //fill

            float[] data = new float[size];
            double[,] dataMatrix = new double[width,height]; 
            var dataArr = band0.ReadRaster(0, 0, width, height, data, width, height, 0, 0);

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    dataMatrix[i, j] = (double)data[i * width + j];
                }
            }

            double[,] fuel_FCCS = DispersionModelling.SBtoFCCS(dataMatrix);

            Driver drv_tiff = Gdal.GetDriverByName("GTiff");
            string[] options = null;
            Dataset tiffOutput = drv_tiff.CreateCopy(outputFile, ds, 0, options, null, null);
            tiffOutput.SetProjection(ds.GetProjection());

            tiffOutput.FlushCache();
            tiffOutput.Dispose();

        }

        public static void runFOFEM(string simulationPath, string fofemInputFileLoc, string fccsFuelfilename, double[,] fuelMoisture) { 

            fofemInputFileLoc = simulationPath + fofemInputFileLoc;

            // Delete existing input file if it exists
            if (File.Exists(fofemInputFileLoc))
            {
                File.Delete(fofemInputFileLoc);
            }
            if (Directory.Exists(simulationPath + "/FOFEMOutput/"))
            {
                Directory.Delete(simulationPath + "/FOFEMOutput/");
            }
            Directory.CreateDirectory(simulationPath + "/FOFEMOutput/");

            double thousandHourMoisture = new double();
            if (fuelMoisture.GetLength(1) < 6)
            {
                Console.WriteLine("1000-Hour dead fuel moisture not provided, using arbitrarily chosen value of 8%");
                thousandHourMoisture = 8;
            }
            else
            {
                thousandHourMoisture = fuelMoisture[1, 6];
            }
            string[] FOFEMinputfile = {$"FCCS_Layer_File: "+ fccsFuelfilename.ToString(),
            "FCCS_Layer_Number: 1",
            "FOFEM_Percent_Foliage_Branch_Consumed: 75.0",
            "FOFEM_Region: I",
            "FOFEM_Season: Summer",
            $"FOFEM_10_Hour_FM: " + fuelMoisture[1,2].ToString(),
            $"FOFEM_1000_Hour_FM: " + thousandHourMoisture.ToString(),
            $"FOFEM_Duff_FM: " + fuelMoisture[1,4].ToString(),
            "FOFEM_FLAMING_PM25: ",
            "FOFEM_FLAMING_PM10: ",
            "FOFEM_SMOLDERING_PM25: ",
            "FOFEM_SMOLDERING_PM10: " };
            // Writelines to the FOFEM input file
            Helpers.AppendLinesToFile(fofemInputFileLoc, FOFEMinputfile); 
            

            Console.WriteLine("FOFEM Input File Saved, Starting FOFEM!");



            // Execute FOFEM command
            string externalProgram = simulationPath + "/bin/TestSpatialFOFEM.exe";
            string outputFolder = simulationPath + "/FOFEMoutput/";
            string commandFOFEM = $"'{simulationPath}/setEnv.bat' & '{externalProgram}' '{fofemInputFileLoc}' '{outputFolder}'";
            Helpers.ExecuteCommand(commandFOFEM);

            Console.WriteLine("FOFEM Complete");


        }
    }
}
