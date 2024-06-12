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
    public class DisModel
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

        public double[] FindxyzH(double threshold, double smokeTemp, double exitVelocity, double windVelocity, double[] dispCoeff, double steps, double emissionMassFlowRate)
        {
            double[] xyzH = new double[5];
            double stackDiameter = 20; // simulated stack diameter(m)
            double atmosphericP = 1; // atmospheric pressure(bar)
            double atmosphericTemp = 300; // Ambient temperature(K)
            double a = 1; // ground reflection coefficient, usual conservative value is 1


            double steadyStateHeight = (exitVelocity * stackDiameter / windVelocity) *
                                   (1.5 + 2.68 * atmosphericP * stackDiameter *
                                   (smokeTemp + 273 - atmosphericTemp) / (smokeTemp + 273));

            var findmax = new FindMaximum();

            //xMax, yMax, zMin, zMax
            xyzH[0] = 2 * findmax.FindMax(windVelocity, threshold, emissionMassFlowRate, dispCoeff, steadyStateHeight, "x");
            xyzH[1] = 4 * findmax.FindMax(windVelocity, threshold, emissionMassFlowRate, dispCoeff, steadyStateHeight, "y");
            xyzH[2] = steadyStateHeight - findmax.FindMax(windVelocity, threshold, emissionMassFlowRate, dispCoeff, steadyStateHeight, "z");
            xyzH[3] = steadyStateHeight + findmax.FindMax(windVelocity, threshold, emissionMassFlowRate, dispCoeff, steadyStateHeight, "z");
            xyzH[4] = steadyStateHeight;

            return xyzH;
        }

        //topDownRaster
        public double[,] dispersionModel1(double threshold, double smokeTemp, double exitVelocity, double windVelocity, double[] dispCoeff, double steps, double emissionMassFlowRate)
        {
            double[] xyzH = FindxyzH(threshold, smokeTemp, exitVelocity, windVelocity, dispCoeff, steps, emissionMassFlowRate);

            double xMax = xyzH[0];
            double yMax = xyzH[1];
            double zMin = xyzH[2];
            double zMax = xyzH[3];
            double steadyStateHeight = xyzH[4];

            int rows = (int)Math.Ceiling(xMax / steps);
            int cols = (int)Math.Ceiling(yMax / steps);

            double[,] topDownRaster = new double[rows, cols];

            for (int x = 1; x < rows; x++)
            {
                for (int y = 1; y < cols; y++)
                {
                    double dispersionCoefficientY = dispCoeff[0] * x * Math.Pow(1 + dispCoeff[1] * x, dispCoeff[2]);
                    double dispersionCoefficientZ = dispCoeff[3] * x * Math.Pow(1 + dispCoeff[4] * x, dispCoeff[5]); ;

                    double termA = (Math.PI / 2) * (emissionMassFlowRate / (Math.Sqrt(2 * Math.PI) * windVelocity * dispersionCoefficientZ * dispersionCoefficientY)) *
                                   Math.Exp(-Math.Pow(y * steps, 2) / (2 * Math.Pow(dispersionCoefficientY, 2)));

                    double termB = Erf((zMax - steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2))) +
                                   Erf((zMax + steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2)));

                    double termC = Erf((zMin - steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2))) +
                                   Erf((zMin + steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2)));

                    topDownRaster[x, y] = termA * dispersionCoefficientZ * (termB - termC); //topDownRaster

                  
                }

            }
            return topDownRaster;
        }

        // driverLevelDensity
        public double[,] dispersionModel2(double threshold, double smokeTemp, double exitVelocity, double windVelocity, double[] dispCoeff, double steps, double emissionMassFlowRate)
        {
            double[] xyzH = FindxyzH(threshold, smokeTemp, exitVelocity, windVelocity, dispCoeff, steps, emissionMassFlowRate);

            double xMax = xyzH[0];
            double yMax = xyzH[1];
            double zMin = xyzH[2];
            double zMax = xyzH[3];
            double steadyStateHeight = xyzH[4];

            int rows = (int)Math.Ceiling(xMax / steps);
            int cols = (int)Math.Ceiling(yMax / steps);

            double[,] topDownRaster = new double[rows, cols];
            double[,] driverLevelDensity = new double[rows, cols];

            for (int x = 1; x < rows; x++)
            {
                for (int y = 1; y < cols; y++)
                {
                    double dispersionCoefficientY = dispCoeff[0] * x * Math.Pow(1 + dispCoeff[1] * x, dispCoeff[2]);
                    double dispersionCoefficientZ = dispCoeff[3] * x * Math.Pow(1 + dispCoeff[4] * x, dispCoeff[5]); ;


                    if (zMin < 1)
                    {
                        double z = 1.0;
                        double term1 = emissionMassFlowRate / (Math.Sqrt(2 * Math.PI) * windVelocity * dispersionCoefficientZ * dispersionCoefficientY);
                        double term2 = Math.Exp(-Math.Pow(y * steps, 2) / (2 * Math.Pow(dispersionCoefficientY, 2)));
                        double term3 = Math.Exp(-Math.Pow(z - steadyStateHeight, 2) / (2 * Math.Pow(dispersionCoefficientZ, 2)));
                        double term4 = Math.Exp(-Math.Pow(z + steadyStateHeight, 2) / (2 * Math.Pow(dispersionCoefficientZ, 2)));

                        driverLevelDensity[x, y] = term1 * term2 * (term3 + term4); //driverLevelDensity 
                    }
                }

            }
            return topDownRaster;

        }
    }
}
