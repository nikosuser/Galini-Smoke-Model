using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Reflection.Metadata;
using Microsoft.VisualBasic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using static IronPython.SQLite.PythonSQLite;


namespace Galini_C_
{
    public class DispersionModelling
    {
        public static double[] GetDispersionCoefficients(string dayOrNight, string surfaceRoughness, string insolation, string nightOvercast, string stabilityMode, double windVelocityDirectionVariation, double windVelocity)
        {
            //Method that determine atmospheric stability class under different weather conditons and find corresponding values used to dispersion coefficients y and z
            //7 Inputs:
            //string dayOrNight ("day", "night"),
            //string surfaceRoughness ("rural", "urban"),
            //string insolation ("strong", "moderate", "slight"),
            //string nightOvercast ("majority","minority"),
            //string stabilityMode ("optimistic", "pessimistic"),
            //double windVelocityDirectionVariation,
            //double windVelocity(m/s)
            //Return a 1*7 double array, 0:3 are values to calculate dispersion coefficient y, 3:6 are values to calculate dispersion coeffienct z, the last value is P

            char atmoStabilityClassByDirection = 'O';

            if (windVelocityDirectionVariation < 3.5)
            {
                atmoStabilityClassByDirection = 'F';
            }
            else if (windVelocityDirectionVariation >= 3.5 && windVelocityDirectionVariation < 7.5)
            {
                atmoStabilityClassByDirection = 'E';
            }
            else if (windVelocityDirectionVariation >= 7.5 && windVelocityDirectionVariation < 12.5)
            {
                atmoStabilityClassByDirection = 'D';
            }
            else if (windVelocityDirectionVariation >= 12.5 && windVelocityDirectionVariation < 17.5)
            {
                atmoStabilityClassByDirection = 'C';
            }
            else if (windVelocityDirectionVariation >= 17.5 && windVelocityDirectionVariation < 22.5)
            {
                atmoStabilityClassByDirection = 'B';
            }
            else if (windVelocityDirectionVariation >= 22.5)
            {
                atmoStabilityClassByDirection = 'A';
            }


            char atmoStabilityClassByIrradiance = 'O';

            switch (dayOrNight)
            {
                case "day":
                    if (windVelocity < 2)
                    {
                        switch (insolation)
                        {
                            case "strong":
                                atmoStabilityClassByIrradiance = 'A';
                                break;
                            case "moderate":
                                atmoStabilityClassByIrradiance = 'A';
                                break;
                            case "slight":
                                atmoStabilityClassByIrradiance = 'B';
                                break;
                        }
                    }
                    else if (windVelocity >= 2 && windVelocity < 3)
                    {
                        switch (insolation)
                        {
                            case "strong":
                                atmoStabilityClassByIrradiance = 'B';
                                break;
                            case "moderate":
                                atmoStabilityClassByIrradiance = 'B';
                                break;
                            case "slight":
                                atmoStabilityClassByIrradiance = 'C';
                                break;
                        }
                    }
                    else if (windVelocity >= 3 && windVelocity < 5)
                    {
                        switch (insolation)
                        {
                            case "strong":
                                atmoStabilityClassByIrradiance = 'B';
                                break;
                            case "moderate":
                                atmoStabilityClassByIrradiance = 'C';
                                break;
                            case "slight":
                                atmoStabilityClassByIrradiance = 'C';
                                break;
                        }
                    }
                    else if (windVelocity >= 5 && windVelocity < 6)
                    {
                        switch (insolation)
                        {
                            case "strong":
                                Console.WriteLine("ERROR: Atmospheric Stability mismatch: too strong wind for strong insolation!");
                                break;
                            case "moderate":
                                atmoStabilityClassByIrradiance = 'C';
                                break;
                            case "slight":
                                atmoStabilityClassByIrradiance = 'D';
                                break;
                        }
                    }
                    else if (windVelocity >= 6)
                    {
                        switch (insolation)
                        {
                            case "strong":
                                Console.WriteLine("ERROR: Atmospheric Stability mismatch: too strong wind for strong insolation!");
                                break;
                            case "moderate":
                                atmoStabilityClassByIrradiance = 'D';
                                break;
                            case "slight":
                                atmoStabilityClassByIrradiance = 'D';
                                break;
                        }
                    }
                    break;
                case "night":
                    switch (nightOvercast)
                    {
                        case "majority":
                            if (windVelocity < 3)
                            {
                                atmoStabilityClassByIrradiance = 'E';
                            }
                            else
                            {
                                atmoStabilityClassByIrradiance = 'D';
                            }
                            break;

                        case "minority":
                            if (windVelocity < 3)
                            {
                                atmoStabilityClassByIrradiance = 'F';
                            }
                            else if (windVelocity >= 3 && windVelocity < 5)
                            {
                                atmoStabilityClassByIrradiance = 'E';
                            }
                            else
                            {
                                atmoStabilityClassByIrradiance = 'D';
                            }
                            break;
                    }
                    break;
            }

            char atmoStabilityClass = 'O';
            char[] stability = [atmoStabilityClassByIrradiance, atmoStabilityClassByDirection];
            Array.Sort(stability);

            if (atmoStabilityClassByIrradiance == atmoStabilityClassByDirection)
            {
                atmoStabilityClass = atmoStabilityClassByDirection;
            }
            else if (stabilityMode.Equals("pessimistic"))
            {
                atmoStabilityClass = stability[1];
            }
            else if (stabilityMode.Equals("optimistic"))
            {
                atmoStabilityClass = stability[0];
            }


            double[] dispersionCoefficientY = new double[3];
            double[] dispersionCoefficientZ = new double[3];
            double P = 0;

            switch (surfaceRoughness)
            {
                case "rural":
                    switch (atmoStabilityClass)
                    {
                        case 'A':
                            dispersionCoefficientY = [0.22, 0.0001, -0.5];
                            dispersionCoefficientZ = [0.2, 0, 0];
                            P = 0.07;
                            break;
                        case 'B':
                            dispersionCoefficientY = [0.16, 0.0001, -0.5];
                            dispersionCoefficientZ = [0.12, 0, 0];
                            P = 0.07;
                            break;
                        case 'C':
                            dispersionCoefficientY = [0.11, 0.001, -0.5];
                            dispersionCoefficientZ = [0.08, 0.0002, -0.5];
                            P = 0.10;
                            break;
                        case 'D':
                            dispersionCoefficientY = [0.08, 0.0001, -0.5];
                            dispersionCoefficientZ = [0.06, 0.0015, -0.5];
                            P = 0.15;
                            break;
                        case 'E':
                            dispersionCoefficientY = [0.06, 0.0001, -0.5];
                            dispersionCoefficientZ = [0.03, 0.0003, -1];
                            P = 0.35;
                            break;
                        case 'F':
                            dispersionCoefficientY = [0.04, 0.001, -0.5];
                            dispersionCoefficientZ = [0.016, 0.0003, -1];
                            P = 0.55;
                            break;
                    }
                    break;
                case "urban":
                    switch (atmoStabilityClass)
                    {
                        case 'A':
                        case 'B':
                            dispersionCoefficientY = [0.32, 0.0004, -0.5];
                            dispersionCoefficientZ = [0.24, 0.0001, 0.5];
                            P = 0.15;
                            break;
                        case 'C':
                            dispersionCoefficientY = [0.22, 0.0004, -0.5];
                            dispersionCoefficientZ = [0.2, 0, 0];
                            P = 0.20;
                            break;
                        case 'D':
                            dispersionCoefficientY = [0.16, 0.0004, -0.5];
                            dispersionCoefficientZ = [0.20, 0.0003, -0.5];
                            P = 0.25;
                            break;
                        case 'E':
                        case 'F':
                            dispersionCoefficientY = [0.11, 0.0004, -0.5];
                            dispersionCoefficientZ = [0.08, 0.0015, -0.5];
                            P = 0.30;
                            break;
                    }
                    break;
            }

            return [dispersionCoefficientY[0], dispersionCoefficientY[1], dispersionCoefficientY[2], dispersionCoefficientZ[0], dispersionCoefficientZ[1], dispersionCoefficientZ[2], P];
        }

        private static double Erf(double x)
        {
            // Method to calculate Erf(x)
            // 1 Input:
            // double x
            // Return double Erf(x)

            // constants
            double a1 = 0.254829592;
            double a2 = -0.284496736;
            double a3 = 1.421413741;
            double a4 = -1.453152027;
            double a5 = 1.061405429;
            double p = 0.3275911;

            // Save the sign of x
            int sign = 1;
            if (x < 0)
                sign = -1;
            x = Math.Abs(x);

            // A&S formula 7.1.26
            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

            return sign * y;
        }

        public static int SBtoFCCS(int fuel)
        {
            // Method that convert SB to FCCS
            // 1 Input:
            // fuel.asc (vegetaton exists in landscae in format Scott and Burgan
            // Return vegetation in format FCCS

            Dictionary<int, int> SBtoFCCSarray = new Dictionary<int, int>()
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

            return SBtoFCCSarray[fuel];
        }

        public static double[,] SBtoFCCS(double[,] fuel_SB)
        {
            // Method that convert SB to FCCS
            // 1 Input:
            // fuel.asc (vegetaton exists in landscae in format Scott and Burgan
            // Return vegetation in format FCCS

            double[,] SBtoFCCSarray = new double[,]
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

            int w = fuel_SB.GetLength(0);
            int l = fuel_SB.GetLength(1);
            double[,] fuel_FCCS = new double[w, l];

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < l; j++)
                {
                    for (int k = 0; k < SBtoFCCSarray.GetLength(0); k++)
                    {
                        if (fuel_SB[i, j] == SBtoFCCSarray[k, 0])
                        {
                            fuel_FCCS[i, j] = SBtoFCCSarray[k, 1];
                        }
                    }
                }
            }

            return fuel_FCCS;
        }

        public static double[] GetCoordsAboutWindAxis(double[] burningPoint, double[] targetPoint, double windAngle)
        {
            // Method that get target point dimensions in relation to the burning point and the wind direction, so can use its coordinates directly in smoke plume
            // 3 Inputs:
            // 1*2 double array (x, y) of burning point,
            // 1*2 double array (x, y) of target point,
            // double wind angle in degree
            // Return a 1*2 double array (x, y) of target point relative to burning point and wind angle in smoke plume's coordinate system

            // rotate the point ...
            double px = targetPoint[0];
            double py = targetPoint[1];

            // about the point.
            double cx = burningPoint[0];
            double cy = burningPoint[1];

            //mess with the wind to get it to point right
            double theta = (windAngle) * Math.PI / 180;

            //rotate the point (do not reproject about the stable point)
            double p_x = Math.Cos(theta) * (px - cx) - Math.Sin(theta) * (py - cy);
            double p_y = Math.Sin(theta) * (px - cx) + Math.Cos(theta) * (py - cy);

            // Output the new coordinates
            return [p_x, p_y];

        }

        private static double FindInjectionHeight(double smokeTemp, double exitVelocity, double windVelocity, double stackDiameter, double atmosphericP, double atmosphericTemp)
        {
            // Method that calculate injection height
            // 6 Inputs are double smoke temperature, double exit velocity, double wind velocity, double stack diameter, double atmospheic pressure, double atmospheric temperature
            // Return injection height

            double a = 1; // ground reflection coefficient, usual conservative value is 1

            double steadyStateHeight = (exitVelocity * stackDiameter / windVelocity) *
                                   (1.5 + 2.68 * atmosphericP * stackDiameter *
                                   (smokeTemp + 273 - atmosphericTemp) / (smokeTemp + 273));

            return steadyStateHeight;
        }

        public static double FindInjectionHeight_Andersen(double cellsize, double T_amb, double P_amb, double environemntalLapseRate, double dryAdiabaticLapseRate, double firelineIntensity, double ROS)
        {
            T_amb = T_amb + 273;                                            //C to K
            environemntalLapseRate = environemntalLapseRate / 1000;         //C/km to C/m
            dryAdiabaticLapseRate = dryAdiabaticLapseRate / 1000;           //C/km to C/m
            firelineIntensity = firelineIntensity * 1000;                   //kW/m to W/m
            ROS = ROS / 60;                                                 //m/min to m/s

            double Q_supplied = firelineIntensity * cellsize / ROS;

            double tolerance = 0.1;
            int maxIterations = 100;
            int iteration = 0;

            double guess1 = 0;
            double guess2 = 2000;

            double newGuess = 0;

            double Q_required1 = AndersenRequiredEnergy(guess1, cellsize, T_amb, P_amb, environemntalLapseRate, dryAdiabaticLapseRate, firelineIntensity, ROS);
            double Q_required2 = AndersenRequiredEnergy(guess2, cellsize, T_amb, P_amb, environemntalLapseRate, dryAdiabaticLapseRate, firelineIntensity, ROS);


            while (Math.Abs(1 - (Q_supplied / Q_required2)) > tolerance && iteration < maxIterations)
            {
                // Secant Method formula
                newGuess = guess2 - (Q_required2 - Q_supplied) * (guess2 - guess1) / (Q_required2 - Q_required1);

                // Update guesses and required energy
                guess1 = guess2;
                Q_required1 = Q_required2;

                guess2 = newGuess;
                Q_required2 = AndersenRequiredEnergy(guess2, cellsize, T_amb, P_amb, environemntalLapseRate, dryAdiabaticLapseRate, firelineIntensity, ROS);

                iteration++;
            }
            return newGuess;
        }

        private static double AndersenRequiredEnergy(double injectionHeight, double cellsize, double T_amb, double P_amb, double environemntalLapseRate, double dryAdiabaticLapseRate, double firelineIntensity, double ROS)
        {
            return 0.5 * P_amb * cellsize * cellsize * injectionHeight *
                Math.Log(1 + (injectionHeight * (environemntalLapseRate - dryAdiabaticLapseRate)) / T_amb) *
                (1 - Math.Pow((1 + environemntalLapseRate * injectionHeight / T_amb), (-9.81 / (environemntalLapseRate * 287.05))));

        }

        public static double TopDownRaster(double _x, double _y, double dispersionCoefficientY, double dispersionCoefficientZ, double emissionMassFlowRate, double windVelocity, double steadyStateHeight)
        {   //  Method that calculate top-down smoke concentration at one target point due to one burning/smoldering point
            // 7 Inputs:
            // double x,y coordinates in metre on smoke domain
            // double y,z dispersion coefficients
            // double emission mass flow rate,
            // double wind velocity,
            // double injection height
            // Return top-down smoke concentration at one target point due to one burning/smoldering point

            double termA = Math.Sqrt(Math.PI / 2) * (emissionMassFlowRate / (Math.Sqrt(2 * Math.PI) * windVelocity * dispersionCoefficientZ * dispersionCoefficientY)) *
                            Math.Exp(-Math.Pow(_y, 2) / (2 * Math.Pow(dispersionCoefficientY, 2)));

            double zMax = 300000;
            double zMin = 0;        //Hardcoded values instead of integrating 

            double termB = Erf((zMax - steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2))) +
                            Erf((zMax + steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2)));

            double termC = Erf((zMin - steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2))) +
                            Erf((zMin + steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2)));

            return termA * dispersionCoefficientZ * (termB - termC);
        }


        public static double DriverLevelDensity(double _x, double _y, double dispersionCoefficientY, double dispersionCoefficientZ, double emissionMassFlowRate, double windVelocity, double steadyStateHeight)
        {   //  Method that calculate driver-level (1m) smoke concentration at one target point due to one burning/smoldering point
            // 7 Inputs:
            // double x,y coordinates in metre on smoke domain
            // double y,z dispersion coefficients
            // double emission mass flow rate,
            // double wind velocity,
            // double injection height
            // Return driver-level smoke concentration at one target point due to one burning/smoldering point

            double z = 1.0; //Driver level height
            double term1 = emissionMassFlowRate / (Math.Sqrt(2 * Math.PI) * windVelocity * dispersionCoefficientZ * dispersionCoefficientY);
            double term2 = Math.Exp(-Math.Pow(_y, 2) / (2 * Math.Pow(dispersionCoefficientY, 2)));
            double term3 = Math.Exp(-Math.Pow(z - steadyStateHeight, 2) / (2 * Math.Pow(dispersionCoefficientZ, 2)));
            double term4 = Math.Exp(-Math.Pow(z + steadyStateHeight, 2) / (2 * Math.Pow(dispersionCoefficientZ, 2)));

            return term1 * term2 * (term3 + term4);
        }

        public static (double[,], double[,]) DispersionModel(Dictionary<string, double> config,
                                                                double[,] burningPoint_fireDomain,
                                                                double[,] firelineIntensity,
                                                                double[,] ROS,
                                                                double[,] flamingEmissionsFlowrate,
                                                                double[,] smolderingEmissionsFlowrate,
                                                                double[] fireDomainDims,
                                                                double[] dispCoeff
                                                                )
        {
            //Method that calculates the total top-down & driver-level smoke concentration for a landscape. 

            //Returns the total top-down & driver-level smoke concentration in the smoke domain.

            double windAngle = config["windAngle"];
            double windVelocity = config["windVelocity"];
            double scaleFactor = config["scaleFactor"];
            double cellsize_Fire = config["cellsize_fire"];
            double cellsize_Smoke = config["cellsize_smoke"];
            double T_amb = config["atmosphericTemp"];
            double P_amb = config["atmosphericPressure"];
            double environmentalLapseRate = config["environmentalLapseRate"];
            double dryAdiabaticLapseRate = config["dryAdiabaticLapseRate"];


            if (windAngle == 0)
            {
                windAngle = 0.01;
            }
            if (windAngle == 180)
            {
                windAngle = 180 + 0.01;
            }
            double WindAngle_rad = (90 - windAngle) * (Math.PI / 180);
            double steadyStateHeight;

            double w = fireDomainDims[0];
            double l = fireDomainDims[1];
            double[] smokeDomainDims = [scaleFactor * w * cellsize_Fire / cellsize_Smoke, scaleFactor * l * cellsize_Fire / cellsize_Smoke];

            int rows = (int)smokeDomainDims[0];
            int cols = (int)smokeDomainDims[1];

            double[,] topDownRaster = new double[rows, cols];
            double[,] driverLevelDensity = new double[rows, cols];

            //setup progress bars
            int totalBurningCells = 0;
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < l; j++)
                {
                    // check if the point is producing smoke (burning or smoldering)
                    if (burningPoint_fireDomain[i, j] != 0)
                    {
                        totalBurningCells++;
                    }
                }
            }

            int count = 0;
            int progress = 1;
            Console.WriteLine("Calculating Total Top-Down Concentration");
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < l; j++)
                {
                    // check if the point is producing smoke (burning or smoldering)
                    if (burningPoint_fireDomain[i, j] != 0)
                    {
                        count++;
                        if (100 * count / totalBurningCells >= progress * 5)
                        {
                            if (progress % 2 == 0) { Console.Write(" *"); }
                            else { Console.Write(" |"); }

                            progress++;
                        }


                        steadyStateHeight = FindInjectionHeight_Andersen(cellsize_Fire, T_amb, P_amb, environmentalLapseRate, dryAdiabaticLapseRate, firelineIntensity[i, j], ROS[i, j]);

                        double[] burningPoint_smokeDomain = [(scaleFactor * w / 2 - (w / 2 - i)) * cellsize_Fire / cellsize_Smoke, (scaleFactor * l / 2 - (l / 2 - j)) * cellsize_Fire / cellsize_Smoke];

                        // define a boundary line y = kx + b, where no smoke spread to the side opposing wind direction
                        double k = -Math.Tan(WindAngle_rad);
                        double b = burningPoint_smokeDomain[1] - burningPoint_smokeDomain[0] * k;

                        switch (burningPoint_fireDomain[i, j])
                        {
                            case 1: // burning

                                if (windAngle >= 90 && windAngle <= 270) // wind pointing upwards, in y direction, smoke spreads from boundary line to top of domain (y = cols)
                                {
                                    for (int x = 0; x < rows; x++)
                                    {
                                        int Ybound = (int)(k * x + b); // boundary line in terms of y, changing with x
                                        // for certain x, Ybound becomes negative, out of smoke domain, reset to zero
                                        if (Ybound < 0) { Ybound = 0; }

                                        for (int y = Ybound; y < cols; y++)
                                        {
                                            double[] XYplume = GetCoordsAboutWindAxis(burningPoint_smokeDomain, [x, y], windAngle); //get point dimensions in relation to the burning point (0,0) and the wind direction (x axis)

                                            //when the target point is the burning point
                                            //XYplume is zero, the zero occurs in the denominator of TopDownRaster function, TopDownRaster function died, so just add 0 instead of running the function.
                                            if (XYplume[0] == 0 && XYplume[1] == 0)
                                            {
                                                topDownRaster[x, y] += 0;
                                                driverLevelDensity[x, y] += 0;
                                            }

                                            else
                                            {
                                                double _x = XYplume[0] * cellsize_Smoke;
                                                double _y = XYplume[1] * cellsize_Smoke;      //convert to meters

                                                double dispersionCoefficientY = dispCoeff[0] * _x * Math.Pow(1 + dispCoeff[1] * _x, dispCoeff[2]);
                                                double dispersionCoefficientZ = dispCoeff[3] * _x * Math.Pow(1 + dispCoeff[4] * _x, dispCoeff[5]);      //get s_y, s_z for this point

                                                topDownRaster[x, y] += TopDownRaster(_x, _y, dispersionCoefficientY, dispersionCoefficientZ, flamingEmissionsFlowrate[i, j], windVelocity, steadyStateHeight);
                                                driverLevelDensity[x, y] += DriverLevelDensity(_x, _y, dispersionCoefficientY, dispersionCoefficientZ, flamingEmissionsFlowrate[i, j], windVelocity, steadyStateHeight);
                                            }
                                        }
                                    }
                                }
                                else // wind pointing downwards, in y direction, smoke spreads from boundary line to bottom of domain (y=0)
                                {
                                    for (int x = 0; x < rows; x++)
                                    {
                                        int Ybound = (int)(k * x + b); // boundary line in terms of y, changing with x
                                        // for certain x, Ybound becomes too large out of smoke domain, reset to the top of domain
                                        if (Ybound > cols)
                                        {
                                            Ybound = cols;
                                        }

                                        for (int y = 0; y < Ybound; y++)
                                        {
                                            double[] XYplume = GetCoordsAboutWindAxis(burningPoint_smokeDomain, [x, y], windAngle); //get point dimensions in relation to the burning point (0,0) and the wind direction (x axis)
                                            //when the target point is burning point, same as above
                                            if (XYplume[0] == 0 && XYplume[1] == 0)
                                            {
                                                topDownRaster[x, y] += 0;
                                                driverLevelDensity[x, y] += 0;
                                            }

                                            else
                                            {
                                                double _x = XYplume[0] * cellsize_Smoke;
                                                double _y = XYplume[1] * cellsize_Smoke;      //convert to meters

                                                double dispersionCoefficientY = dispCoeff[0] * _x * Math.Pow(1 + dispCoeff[1] * _x, dispCoeff[2]);
                                                double dispersionCoefficientZ = dispCoeff[3] * _x * Math.Pow(1 + dispCoeff[4] * _x, dispCoeff[5]);      //get s_y, s_z for this point

                                                topDownRaster[x, y] += TopDownRaster(_x, _y, dispersionCoefficientY, dispersionCoefficientZ, flamingEmissionsFlowrate[i, j], windVelocity, steadyStateHeight);
                                                driverLevelDensity[x, y] += DriverLevelDensity(_x, _y, dispersionCoefficientY, dispersionCoefficientZ, flamingEmissionsFlowrate[i, j], windVelocity, steadyStateHeight);
                                            }
                                        }
                                    }
                                }
                                break;

                            case 2: // smoldering

                                if (windAngle >= 90 && windAngle <= 270) // wind pointing upwards, in y direction, smoke spreads from boundary line to top of domain (y = cols)
                                {
                                    for (int x = 0; x < rows; x++)
                                    {
                                        int Ybound = (int)(k * x + b); // boundary line in terms of y, changing with x
                                        // for certain x, Ybound becomes negative, out of smoke domain, reset to zero
                                        if (Ybound < 0) { Ybound = 0; }

                                        for (int y = Ybound; y < cols; y++)
                                        {
                                            double[] XYplume = GetCoordsAboutWindAxis(burningPoint_smokeDomain, [x, y], windAngle); //get point dimensions in relation to the burning point (0,0) and the wind direction (x axis)

                                            //when the target point is the smoldering point
                                            //XYplume is zero, the zero occurs in the denominator of TopDownRaster function, TopDownRaster function died, so just add 0 instead of running the function.
                                            if (XYplume[0] == 0 && XYplume[1] == 0)
                                            {
                                                topDownRaster[x, y] += 0;
                                                driverLevelDensity[x, y] += 0;
                                            }

                                            else
                                            {
                                                double _x = XYplume[0] * cellsize_Smoke;
                                                double _y = XYplume[1] * cellsize_Smoke;      //convert to meters

                                                double dispersionCoefficientY = dispCoeff[0] * _x * Math.Pow(1 + dispCoeff[1] * _x, dispCoeff[2]);
                                                double dispersionCoefficientZ = dispCoeff[3] * _x * Math.Pow(1 + dispCoeff[4] * _x, dispCoeff[5]);      //get s_y, s_z for this point

                                                topDownRaster[x, y] += TopDownRaster(_x, _y, dispersionCoefficientY, dispersionCoefficientZ, smolderingEmissionsFlowrate[i, j], windVelocity, steadyStateHeight);
                                                driverLevelDensity[x, y] += DriverLevelDensity(_x, _y, dispersionCoefficientY, dispersionCoefficientZ, smolderingEmissionsFlowrate[i, j], windVelocity, steadyStateHeight);
                                            }
                                        }
                                    }
                                }
                                else // wind pointing downwards, in y direction, smoke spreads from boundary line to bottom of domain (y=0)
                                {
                                    for (int x = 0; x < rows; x++)
                                    {
                                        int Ybound = (int)(k * x + b); // boundary line in terms of y, changing with x
                                        // for certain x, Ybound becomes too large out of smoke domain, reset to the top of domain
                                        if (Ybound > cols)
                                        {
                                            Ybound = cols;
                                        }

                                        for (int y = 0; y < Ybound; y++)
                                        {
                                            double[] XYplume = GetCoordsAboutWindAxis(burningPoint_smokeDomain, [x, y], windAngle); //get point dimensions in relation to the burning point (0,0) and the wind direction (x axis)

                                            //when the target point is the smoldering point
                                            if (XYplume[0] == 0 && XYplume[1] == 0)
                                            {
                                                topDownRaster[x, y] += 0;
                                                driverLevelDensity[x, y] += 0;
                                            }

                                            else
                                            {
                                                double _x = XYplume[0] * cellsize_Smoke;
                                                double _y = XYplume[1] * cellsize_Smoke;      //convert to meters

                                                double dispersionCoefficientY = dispCoeff[0] * _x * Math.Pow(1 + dispCoeff[1] * _x, dispCoeff[2]);
                                                double dispersionCoefficientZ = dispCoeff[3] * _x * Math.Pow(1 + dispCoeff[4] * _x, dispCoeff[5]);      //get s_y, s_z for this point

                                                topDownRaster[x, y] += TopDownRaster(_x, _y, dispersionCoefficientY, dispersionCoefficientZ, smolderingEmissionsFlowrate[i, j], windVelocity, steadyStateHeight);
                                                driverLevelDensity[x, y] += DriverLevelDensity(_x, _y, dispersionCoefficientY, dispersionCoefficientZ, smolderingEmissionsFlowrate[i, j], windVelocity, steadyStateHeight);
                                            }
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
            }

            Console.WriteLine(" ");
            return (topDownRaster, driverLevelDensity);
        }
    }
}