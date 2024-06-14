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

namespace Galini_C_
{
    public class DispersionModelling
    {

        public double[] GetDispersionCoefficients(string dayOrNight, string surfaceRoughness, string insolation, string nightOvercast, string stabilityMode, double windVelocityDirectionVariation, double windVelocity)
        {
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

        public static double[] getCoordsAboutWindAxis(double[] burningPoint, double[] targetPoint, double windAngle)
        {
            // rotate the point ...
            double px = targetPoint[0];
            double py = targetPoint[1];

            // about the point.
            double cx = burningPoint[0];
            double cy = burningPoint[1];

            //mess with the wind to get it to point right
            double theta = (90-windAngle) * Math.PI / 180;

            //rotate the point (do not reproject about the stable point)
            double p_x = Math.Cos(theta) * (px - cx) - Math.Sin(theta) * (py - cy) ;
            double p_y = Math.Sin(theta) * (px - cx) + Math.Cos(theta) * (py - cy) ;

            // Output the new coordinates
            return [p_x, p_y];

        }
        private double FindInjectionHeight(double smokeTemp, double exitVelocity, double windVelocity, double stackDiameter, double atmosphericP, double atmosphericTemp)
        {
            double a = 1; // ground reflection coefficient, usual conservative value is 1

            double steadyStateHeight = (exitVelocity * stackDiameter / windVelocity) *
                                   (1.5 + 2.68 * atmosphericP * stackDiameter *
                                   (smokeTemp + 273 - atmosphericTemp) / (smokeTemp + 273));

            return steadyStateHeight;
        }

        public double FindInjectionHeight_Andersen(double cellsize, double T_amb, double P_amb, double environemntalLapseRate, double dryAdiabaticLapseRate, double firelineIntensity, double ROS)
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
                Console.WriteLine(iteration);
            }
            return newGuess;
        }

        private double AndersenRequiredEnergy(double injectionHeight, double cellsize, double T_amb, double P_amb, double environemntalLapseRate, double dryAdiabaticLapseRate, double firelineIntensity, double ROS)
        {
            return 0.5 * P_amb * cellsize * cellsize * injectionHeight * 
                Math.Log(1 + (injectionHeight * (environemntalLapseRate - dryAdiabaticLapseRate)) / T_amb) * 
                (1 - Math.Pow((1 + environemntalLapseRate * injectionHeight / T_amb), (-9.81 / (environemntalLapseRate * 287.05))));
            
        }

        public double[,] DispersionModel_topDownConcentration(double[,] burningPoint_fireDomain, double smokeDomainScaleFactor, double[] fireDomainDims, double[,] smokeTemp, double[,] exitVelocity, double windVelocity, double WindAngle, double[] dispCoeff, double cellsize, double emissionMassFlowRate, double stackDiameter, double atmosphericP, double atmosphericTemp)
        {
            ///Method that calculates the total top-down smoke concentration for a landscape. 
            ///Returns the smoke domain.
            
            double WindAngle_rad = (WindAngle) * (Math.PI / 180);
            double steadyStateHeight;

            double w = fireDomainDims[0];
            double l = fireDomainDims[1];
            double[] smokeDomainDims = [smokeDomainScaleFactor * w, smokeDomainScaleFactor * l];
            
            int rows = (int)smokeDomainDims[0];
            int cols = (int)smokeDomainDims[1];

            double[,] topDownRaster = new double[rows, cols];

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < l; j++)
                {
                    if (burningPoint_fireDomain[i, j] != 0)
                    {
                        steadyStateHeight = FindInjectionHeight(smokeTemp[i, j], exitVelocity[i, j], windVelocity, stackDiameter, atmosphericP, atmosphericTemp);
                        Console.WriteLine(i.ToString() + ", " + j.ToString());

                        switch (burningPoint_fireDomain[i, j])
                        {
                            case 1: // burning
                                
                                for (int x = 0; x < rows; x++)
                                {
                                    for (int y = 0; y < cols; y++)
                                    {
                                        double[] burningPoint_smokeDomain = [smokeDomainScaleFactor * w / 2 - (w / 2 - i), smokeDomainScaleFactor * l / 2 - (l / 2 - j)];

                                        if (WindAngle < 180 && x > (burningPoint_smokeDomain[0] - (burningPoint_smokeDomain[1] - y) / Math.Tan(WindAngle_rad)))   //check if target point is "behind" the plume when the wind vector is pointing to the left
                                        {
                                            topDownRaster[x, y] += 0;
                                        }
                                        else if (WindAngle >= 180 && x < (burningPoint_smokeDomain[0] - (burningPoint_smokeDomain[1] - y) / Math.Tan(WindAngle_rad)))    //as above but if the wind is pointing to the right
                                        {
                                            topDownRaster[x, y] += 0;
                                        }
                                        else
                                        {
                                            double[] XYplume = getCoordsAboutWindAxis(burningPoint_smokeDomain, [x, y], WindAngle); //get point dimensions in relation to the burning point (0,0) and the wind direction (x axis)

                                            if ( burningPoint_smokeDomain[0] == Convert.ToDouble(x) && burningPoint_smokeDomain[1] == Convert.ToDouble(y) )
                                            {
                                                topDownRaster[x, y] += 0;
                                            }
                                            else
                                            {
                                                double _x = XYplume[0] * cellsize;
                                                double _y = XYplume[1] * cellsize;      //convert to meters

                                                double dispersionCoefficientY = dispCoeff[0] * _x * Math.Pow(1 + dispCoeff[1] * _x, dispCoeff[2]);
                                                double dispersionCoefficientZ = dispCoeff[3] * _x * Math.Pow(1 + dispCoeff[4] * _x, dispCoeff[5]);      //get s_y, s_z for this point

                                                double termA = (Math.PI / 2) * (emissionMassFlowRate / (Math.Sqrt(2 * Math.PI) * windVelocity * dispersionCoefficientZ * dispersionCoefficientY)) *
                                                               Math.Exp(-Math.Pow(_y, 2) / (2 * Math.Pow(dispersionCoefficientY, 2)));

                                                double zMax = 300000;
                                                double zMin = 0;        //Hardcoded values instead of integrating 

                                                double termB = Erf((zMax - steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2))) +
                                                               Erf((zMax + steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2)));

                                                double termC = Erf((zMin - steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2))) +
                                                               Erf((zMin + steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2)));

                                                topDownRaster[x, y] += termA * dispersionCoefficientZ * (termB - termC);
                                            }
                                            
                                        }
                                    }
                                }
                                break;

                            case 2: // smoldering
                              
                                for (int x = 0; x < rows; x++)
                                {
                                    for (int y = 0; y < cols; y++)
                                    {
                                        double[] burningPoint_smokeDomain = [smokeDomainScaleFactor * w / 2 - (w / 2 - i), smokeDomainScaleFactor * l / 2 - (l / 2 - j)];

                                        if (WindAngle < 180 && x > (burningPoint_smokeDomain[0] - (burningPoint_smokeDomain[1] - y) / Math.Tan(WindAngle_rad)))   //check if target point is "behind" the plume when the wind vector is pointing to the left
                                        {
                                            topDownRaster[x, y] += 0;
                                        }
                                        else if (WindAngle >= 180 && x < (burningPoint_smokeDomain[0] - (burningPoint_smokeDomain[1] - y) / Math.Tan(WindAngle_rad)))    //as above but if the wind is pointing to the right
                                        {
                                            topDownRaster[x, y] += 0;
                                        }
                                        else
                                        {
                                            double[] XYplume = getCoordsAboutWindAxis(burningPoint_smokeDomain, [x, y], WindAngle); //get point dimensions in relation to the burning point (0,0) and the wind direction (x axis)

                                            double _x = XYplume[0] * cellsize;
                                            double _y = XYplume[1] * cellsize;      //convert to meters

                                            double dispersionCoefficientY = dispCoeff[0] * _x * Math.Pow(1 + dispCoeff[1] * _x, dispCoeff[2]);
                                            double dispersionCoefficientZ = dispCoeff[3] * _x * Math.Pow(1 + dispCoeff[4] * _x, dispCoeff[5]);      //get s_y, s_z for this point

                                            double termA = (Math.PI / 2) * (emissionMassFlowRate / (Math.Sqrt(2 * Math.PI) * windVelocity * dispersionCoefficientZ * dispersionCoefficientY)) *
                                                           Math.Exp(-Math.Pow(_y, 2) / (2 * Math.Pow(dispersionCoefficientY, 2)));

                                            double zMax = 300000;
                                            double zMin = 0;        //Hardcoded values instead of integrating 

                                            double termB = Erf((zMax - steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2))) +
                                                           Erf((zMax + steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2)));

                                            double termC = Erf((zMin - steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2))) +
                                                           Erf((zMin + steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2)));

                                            topDownRaster[x, y] += termA * dispersionCoefficientZ * (termB - termC);
                                        }
                                    }
                                }
                                break;
                        }
                    }
                    
                }
            }

            return topDownRaster;
        }

        // driverLevelDensity
         public double[,] dispersionModel_driverLevel(double[] burningPoint_fireDomain, double smokeDomainScaleFactor, double[] fireDomainDims, double smokeTemp, double exitVelocity, double windVelocity, double WindAngle, double[] dispCoeff, double cellsize, double emissionMassFlowRate, double stackDiameter, double atmosphericP, double atmosphericTemp)
        {
            double steadyStateHeight = FindInjectionHeight(smokeTemp, exitVelocity, windVelocity, stackDiameter, atmosphericP, atmosphericTemp);

            double w = fireDomainDims[0];
            double l = fireDomainDims[1];
            double[] smokeD = [smokeDomainScaleFactor * w, smokeDomainScaleFactor * l];
            double[] BPsmokeD = [smokeDomainScaleFactor * w / 2 - (w / 2 - burningPoint_fireDomain[0]), smokeDomainScaleFactor * l / 2 - (l / 2 - burningPoint_fireDomain[1])];

            int rows = (int)smokeD[0];
            int cols = (int)smokeD[1];

            double[,] driverLevelDensity = new double[rows, cols];

            for (int x = 0; x < rows; x++)
            {
                for (int y = 0; y < cols; y++)
                {
                    double[] XYplume = getCoordsAboutWindAxis(BPsmokeD, [x,y], WindAngle);

                    double _x = XYplume[0] * cellsize;
                    double _y = XYplume[1] * cellsize;

                    double dispersionCoefficientY = dispCoeff[0] * _x * Math.Pow(1 + dispCoeff[1] * _x, dispCoeff[2]);
                    double dispersionCoefficientZ = dispCoeff[3] * _x * Math.Pow(1 + dispCoeff[4] * _x, dispCoeff[5]); 

                    double z = 1.0;
                    double term1 = emissionMassFlowRate / (Math.Sqrt(2 * Math.PI) * windVelocity * dispersionCoefficientZ * dispersionCoefficientY);
                    double term2 = Math.Exp(-Math.Pow(_y, 2) / (2 * Math.Pow(dispersionCoefficientY, 2)));
                    double term3 = Math.Exp(-Math.Pow(z - steadyStateHeight, 2) / (2 * Math.Pow(dispersionCoefficientZ, 2)));
                    double term4 = Math.Exp(-Math.Pow(z + steadyStateHeight, 2) / (2 * Math.Pow(dispersionCoefficientZ, 2)));

                    driverLevelDensity[x, y] = term1 * term2 * (term3 + term4); 
                }

            }
            return driverLevelDensity;

        }
    }
}
