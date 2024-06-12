using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Galini_C_
{
    public class GDC
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
                        case 'A':case 'B':
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
                        case 'E': case 'F':
                            dispersionCoefficientY = [0.11, 0.0004, -0.5];
                            dispersionCoefficientZ = [0.08, 0.0015, -0.5];
                            P = 0.30;
                            break;
                    }
                    break;
            }

            return [dispersionCoefficientY[0], dispersionCoefficientY[1], dispersionCoefficientY[2], dispersionCoefficientZ[0], dispersionCoefficientZ[1], dispersionCoefficientZ[2], P];
        }

    }
}
