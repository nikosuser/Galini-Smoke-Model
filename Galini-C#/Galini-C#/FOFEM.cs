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
    internal class Fofem
    {
        public static void Usage()
        {
            Console.WriteLine("usage: gdaldatasetwrite {dataset name}");
            System.Environment.Exit(-1);
        }

        public static void Save(string[] args)
        {

            if (args.Length < 1) Usage();

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
        /// <summary>
        /// Save FCCS fuel map to geoTIFF 
        /// </summary>
        /// <param name="outputFile">FCCS fuel model raster file save location</param>
        public static void SaveFccStiff(string outputFile, double[,] fccs)
        {
            Helpers.SaveGeoTiff(outputFile, fccs);
        }
        /// <summary>
        /// Convert an SB40 fuel map to FCCS. Conversion is based on a dictionary in the accompanying paper. 
        /// </summary>
        /// <param name="sBfuels">Scott and Burgan 40 fuel model map (int[,])</param>
        /// <returns>the FCCS fuel map raster (int[,]</returns>
        public static int[,] SBtoFccs(int[,] sBfuels)
        {
            Dictionary<int, int> sBtoFccSarray = new Dictionary<int, int>()
            {
            { 0, 0 },
            { 91, 0 },
            { 92, 0 },
            { 93, 1244 },
            { 98, 0 },
            { 99, 0 },
            { 100, 49 },
            { 101, 519 },
            { 102, 66 },
            { 103, 66 },
            { 104, 131 },
            { 105, 131 },
            { 106, 66 },
            { 107, 318 },
            { 108, 175 },
            { 120, 401 },
            { 121, 56 },
            { 122, 308 },
            { 123, 560112 },
            { 124, 445 },
            { 140, 49 },
            { 141, 69 },
            { 142, 52 },
            { 143, 36 },
            { 144, 69 },
            { 145, 210 },
            { 146, 470 },
            { 147, 154 },
            { 148, 1470313 },
            { 149, 154 },
            { 160, 10 },
            { 161, 224 },
            { 162, 156 },
            { 163, 156 },
            { 164, 59 },
            { 165, 2 },
            { 180, 49 },
            { 181, 154 },
            { 182, 283 },
            { 183, 110 },
            { 184, 305 },
            { 185, 364 },
            { 186, 154 },
            { 187, 228 },
            { 188, 90 },
            { 189, 467 },
            { 200, 1090412 },
            { 201, 48 },
            { 202, 1100422 },
            { 203, 4550432 },
            { -9999, -9999 }
            };
            
            int[,] fccSfuels = new int[sBfuels.GetLength(0), sBfuels.GetLength(1)];
            for (int i = 0; i < sBfuels.GetLength(0); i++)
            {
                for (int j = 0; j< sBfuels.GetLength(1); j++)
                {
                    fccSfuels[i, j] = sBtoFccSarray[sBfuels[i, j]];
                }
            }

            return fccSfuels;
        }
        /// <summary>
        /// Run the TestSpatialFOFEM.exe program by creating its input file and running an internal command.
        /// </summary>
        /// <param name="simulationPath">Path to save the FOFEM outputs (string)</param>
        /// <param name="fofemInputFileLoc">Name of the FOFEM input file (arbitrary) (string)</param>
        /// <param name="fccsFuelfilename">Full path of the FCCS fuel map raster (string) </param>
        /// <param name="fuelMoisture">Fuel moistures of each fuel, same as a FARSITE/FLAMMAP .fms file (double[,])</param>
        public static void RunFofem(string simulationPath, string fofemInputFileLoc, string fccsFuelfilename, double[,] fuelMoisture) { 

            fofemInputFileLoc = simulationPath + fofemInputFileLoc;

            // Delete existing input file if it exists
            if (File.Exists(fofemInputFileLoc))
            {
                File.Delete(fofemInputFileLoc);
            }
            if (Directory.Exists(simulationPath + "/FOFEMOutput/"))
            {
                Directory.Delete(simulationPath + "/FOFEMOutput/",true);
            }
            Directory.CreateDirectory(simulationPath + "/FOFEMOutput/");

            double thousandHourMoisture = new double();
            if (fuelMoisture.GetLength(1) < 6)
            {
                Console.WriteLine("1000-Hour dead fuel moisture not provided, using arbitrarily chosen value of 16%");
                thousandHourMoisture = 16;
            }
            else
            {
                thousandHourMoisture = fuelMoisture[1, 6];
            }


            string[] fofeMinputfile = {$"FCCS_Layer_File: {simulationPath}/{fccsFuelfilename}",
            "FCCS_Layer_Number: 1",
            "FOFEM_Percent_Foliage_Branch_Consumed: 75.0",
            "FOFEM_Region: I",
            "FOFEM_Season: Summer",
            $"FOFEM_10_Hour_FM: " + fuelMoisture[1,2].ToString(),
            $"FOFEM_1000_Hour_FM: " + thousandHourMoisture.ToString(),
            $"FOFEM_Duff_FM: " + fuelMoisture[1,4].ToString(),
            "FOFEM_SMOLDERING_CO2: ",
            "FOFEM_SMOLDERING_CO: ",
            "FOFEM_SMOLDERING_CH4: ",
            "FOFEM_SMOLDERING_NOX: ",
            "FOFEM_SMOLDERING_SO2: ",
            "FOFEM_SMOLDERING_PM25: ",
            "FOFEM_SMOLDERING_PM10: ",            
            "FOFEM_FLAMING_CO2: ",
            "FOFEM_FLAMING_CO: ",
            "FOFEM_FLAMING_CH4: ",
            "FOFEM_FLAMING_NOX: ",
            "FOFEM_FLAMING_SO2: ",
            "FOFEM_FLAMING_PM25: ",
            "FOFEM_FLAMING_PM10: ",
            "FOFEM_TOTAL_FUEL_CONSUMED: ",
            };
            // Writelines to the FOFEM input file
            Helpers.AppendLinesToFile(fofemInputFileLoc, fofeMinputfile); 
            
            Console.WriteLine("FOFEM Input File Saved, Starting FOFEM!");

            // Execute FOFEM command
            string externalProgram = simulationPath + "/bin/TestSpatialFOFEM.exe";
            string outputFolder = simulationPath + "/FOFEMoutput/";
            string commandFofem = $" {simulationPath}/setEnv.bat && {externalProgram} {fofemInputFileLoc} {outputFolder}";
            commandFofem = commandFofem.Replace("\\", "/");
            Helpers.ExecuteCommand(commandFofem);

            Console.WriteLine("FOFEM Complete");


        }
    }
}
