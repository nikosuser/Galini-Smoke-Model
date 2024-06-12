using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Galini_C_
{
    public class FindMaximum
    {
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
    } 
}
