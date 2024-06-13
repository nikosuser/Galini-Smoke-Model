using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Galini_C_
{
    public class DisModelNEW
    {
        static double Erf(double x)
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

        public double[] XYPlumeD(double[] BP, double[] TP, double WindAngle) //smoke domain
        {
            double theta = (180 - WindAngle) * (Math.PI / 180); //180?

            // Calculate the vector AB
            double ABx = Math.Cos(theta);
            double ABy = Math.Sin(theta);

            // Calculate the vector AP
            double APx = TP[0] - BP[0];
            double APy = TP[1] - BP[1];

            // Dot product of AP and AB
            double dotProduct = APx * ABx + APy * ABy;

            // Magnitude squared of AB
            double AB_mag_squared = ABx * ABx + ABy * ABy;

            // Projection factor
            double projectionFactor = dotProduct / AB_mag_squared;
            double _vx = projectionFactor * ABx;
            double _vy = projectionFactor * ABy;

            // Calculate the intersection point (foot of the perpendicular)
            double intersectionX = BP[0] + _vx;
            double intersectionY = BP[1] + _vy;

            // Calculate the perpendicular vector
            double perpenVX = intersectionX - TP[0];
            double perpenVY = intersectionY - TP[1];

            // calculate x,y in smoke plume
            double _x = Math.Sqrt(_vx * _vx + _vy * _vy);
            double _y = Math.Sqrt(perpenVX * perpenVX + perpenVY * perpenVY);

            return [_x, _y];
        }

        public double FindH(double smokeTemp, double exitVelocity, double windVelocity)
        {
            double stackDiameter = 20; // simulated stack diameter(m)
            double atmosphericP = 1; // atmospheric pressure(bar)
            double atmosphericTemp = 300; // Ambient temperature(K)
            double a = 1; // ground reflection coefficient, usual conservative value is 1

            double steadyStateHeight = (exitVelocity * stackDiameter / windVelocity) *
                                   (1.5 + 2.68 * atmosphericP * stackDiameter *
                                   (smokeTemp + 273 - atmosphericTemp) / (smokeTemp + 273));

            return steadyStateHeight;
        }
        //topDownRaster
        public double[,] dispersionModel1(double[] BPfireD, double scaleSize, double[] fireD, double smokeTemp, double exitVelocity, double windVelocity, double WindAngle, double[] dispCoeff, double steps, double emissionMassFlowRate)
        {
            double steadyStateHeight = FindH(smokeTemp, exitVelocity, windVelocity);
            double theta = (180 - WindAngle) * (Math.PI / 180);
            double WindAngle_rad = (WindAngle) * (Math.PI / 180);

            double w = fireD[0];
            double l = fireD[1];
            double[] smokeD = [scaleSize * w, scaleSize * l];
            double[] BPsmokeD = [scaleSize * w / 2 - (w/2 - BPfireD[0]), scaleSize * l / 2 - (l / 2 - BPfireD[1])];

            int rows = (int)smokeD[0];
            int cols = (int)smokeD[1];

            double[,] topDownRaster = new double[rows, cols];

            for (int x = 0; x < rows; x++)
            {
                for (int y = 0; y < cols; y++)
                {
                    if (WindAngle < 180)
                    {
                        if (y > BPsmokeD[1] && x > (BPsmokeD[0] - (y - BPsmokeD[1])/Math.Tan(WindAngle_rad)))
                        {
                            topDownRaster[x, y] = 0;
                        }
                        else if (y <= BPsmokeD[1] && x > (BPsmokeD[0] - (y - BPsmokeD[1])/ Math.Tan(WindAngle_rad)))
                        {
                            topDownRaster[x, y] = 0;
                        }
                        else
                        {
                            double[] XYplume = XYPlumeD(BPsmokeD, [x, y], WindAngle);

                            double _x = XYplume[0] * steps;
                            double _y = XYplume[1] * steps;

                            double dispersionCoefficientY = dispCoeff[0] * _x * Math.Pow(1 + dispCoeff[1] * _x, dispCoeff[2]);
                            double dispersionCoefficientZ = dispCoeff[3] * _x * Math.Pow(1 + dispCoeff[4] * _x, dispCoeff[5]);

                            double termA = (Math.PI / 2) * (emissionMassFlowRate / (Math.Sqrt(2 * Math.PI) * windVelocity * dispersionCoefficientZ * dispersionCoefficientY)) *
                                           Math.Exp(-Math.Pow(_y, 2) / (2 * Math.Pow(dispersionCoefficientY, 2)));
                            double zMax = 300000;
                            double zMin = 0;
                            double termB = Erf((zMax - steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2))) +
                                           Erf((zMax + steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2)));

                            double termC = Erf((zMin - steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2))) +
                                           Erf((zMin + steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2)));

                            topDownRaster[x, y] = termA * dispersionCoefficientZ * (termB - termC);
                        }
                    }
                    else
                    {

                        if (y > BPsmokeD[1] && x < (BPsmokeD[0] - Math.Abs(Math.Tan(theta) * y)))
                        {
                            topDownRaster[x, y] = 0;
                        }
                        if (y < BPsmokeD[1] && x < (BPsmokeD[0] +Math.Abs(Math.Tan(theta) * y)))
                        {
                            topDownRaster[x, y] = 0;
                        }
                        else
                        {
                            double[] XYplume = XYPlumeD(BPsmokeD, [x, y], WindAngle);

                            double _x = XYplume[0] * steps;
                            double _y = XYplume[1] * steps;

                            double dispersionCoefficientY = dispCoeff[0] * _x * Math.Pow(1 + dispCoeff[1] * _x, dispCoeff[2]);
                            double dispersionCoefficientZ = dispCoeff[3] * _x * Math.Pow(1 + dispCoeff[4] * _x, dispCoeff[5]);

                            double termA = (Math.PI / 2) * (emissionMassFlowRate / (Math.Sqrt(2 * Math.PI) * windVelocity * dispersionCoefficientZ * dispersionCoefficientY)) *
                                           Math.Exp(-Math.Pow(_y, 2) / (2 * Math.Pow(dispersionCoefficientY, 2)));
                            double zMax = 300000;
                            double zMin = 0;
                            double termB = Erf((zMax - steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2))) +
                                           Erf((zMax + steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2)));

                            double termC = Erf((zMin - steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2))) +
                                           Erf((zMin + steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2)));

                            topDownRaster[x, y] = termA * dispersionCoefficientZ * (termB - termC);
                        }
                    }
                    

                }

            }
            return topDownRaster;
        }

        // driverLevelDensity
         public double[,] dispersionModel2(double[] BPfireD, double scaleSize, double[] fireD, double smokeTemp, double exitVelocity, double windVelocity, double WindAngle, double[] dispCoeff, double steps, double emissionMassFlowRate)
        {
            double steadyStateHeight = FindH(smokeTemp, exitVelocity, windVelocity);

            double w = fireD[0];
            double l = fireD[1];
            double[] smokeD = [scaleSize * w, scaleSize * l];
            double[] BPsmokeD = [scaleSize * w / 2 - (w / 2 - BPfireD[0]), scaleSize * l / 2 - (l / 2 - BPfireD[1])];

            int rows = (int)smokeD[0];
            int cols = (int)smokeD[1];

            double[,] driverLevelDensity = new double[rows, cols];

            for (int x = 1; x < rows; x++)
            {
                for (int y = 1; y < cols; y++)
                {
                    double[] XYplume = XYPlumeD(BPsmokeD, [x,y], WindAngle);

                    double _x = XYplume[0] * steps;
                    double _y = XYplume[1] * steps;

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
