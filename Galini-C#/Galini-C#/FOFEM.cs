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
        public static void saveFCCStiff(string inputFile, string outputFile, double[,] SBfuels)
        {

            //-----------------------------------------------------------------------------------------------------
            /*
            double[,] data = Helpers.ReadGeoTIFFfile(inputFile);

            for (int i = 0; i < data.GetLength(0); i++)
            {
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    data[i, j] = (float)DispersionModelling.SBtoFCCS((int)SBfuels[i, j]);
                }
            }
            */
            Helpers.SaveGeoTIFF(outputFile, SBfuels);

        }

        public static void runFOFEM(double[,] SBfuels, string simulationPath, string fofemInputFileLoc, string fccsFuelfilename, double[,] fuelMoisture) { 

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

            double[,] FCCSfuels = new double[SBfuels.GetLength(0), SBfuels.GetLength(1)];
            for (int i = 0; i < SBfuels.GetLength(0); i++)
            {
                for (int j = 0; j< SBfuels.GetLength(1); j++)
                {
                    FCCSfuels[i, j] = DispersionModelling.SBtoFCCS((int)SBfuels[i, j]);
                }
            }

            saveFCCStiff("fuel.asc", simulationPath + fccsFuelfilename, FCCSfuels);

            string[] FOFEMinputfile = {$"FCCS_Layer_File: "+ fccsFuelfilename,
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
            Helpers.AppendLinesToFile(fofemInputFileLoc, FOFEMinputfile); 
            
            Console.WriteLine("FOFEM Input File Saved, Starting FOFEM!");

            // Execute FOFEM command
            string externalProgram = simulationPath + "/bin/TestSpatialFOFEM.exe";
            string outputFolder = simulationPath + "/FOFEMoutput/";
            string commandFOFEM = $" {simulationPath}/setEnv.bat && {externalProgram} {fofemInputFileLoc} {outputFolder}";
            commandFOFEM = commandFOFEM.Replace("\\", "/");
            Helpers.ExecuteCommand(commandFOFEM);

            Console.WriteLine("FOFEM Complete");


        }
    }
}
