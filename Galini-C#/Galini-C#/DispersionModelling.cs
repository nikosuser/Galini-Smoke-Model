
using System.Diagnostics;

namespace Galini_C_
{
    public class DispersionModelling
    {
        private double[,] _elevation;
        public Dictionary<string, double> Config;
        public double[,] currentlyBurning;
        private double[,] _ros;
        private double[,] _firelineIntensity;
        private double[,] _flamingEmissions;
        private double[,] _smolderingEmissions;
        private double[] _dispCoeff;
        private char _atmoStabilityIndex;
        private double[,] _flamingTime;
        private double[,] _smolderingTime;
        public double[,] SubgridTime;
        
        double[,] _topDownRaster;
        double[,] _driverLevelDensity;
        double[,] _fireActivePoints;
        double[,] _smolderActivePoints;
        double[,] _injectionHeight;
        double[,] _subgridOut;

        private double _windAngle;
        private double _windVelocity;
        private double _cellsize;
        private double _tAmb;
        private double _pAmb;
        private double _environmentalLapseRate;
        private double _dryAdiabaticLapseRate;
        private double[] _rasterDelta;
        /// <summary>
        /// Main method constructor. Use this method first. 
        /// </summary>
        /// <param name="rasterDelta">The separation between the Lower Left corners of the fuel and elevation arrays [X,Y] (double[2])</param>
        /// <param name="elevation">The elevation raster (double[A,B])</param>
        /// <param name="config">a Dictionary(string, double) with simulation parameters. Must contain: "windAngle" (deg), "windVelocity" (m/s), "cellsize" (m), "atmosphericTemp" (C), "atmosphericPressure" (Pa), "environmentalLapseRate" (C/m), "dryAdiabaticLapseRate" (C/m).   </param>
        /// <param name="ros">The rate of spread magnitude (double[X,Y])</param>
        /// <param name="firelineIntensity">The fireline intensity of the wildfire in kW/m (double[A,B])</param>
        /// <param name="flamingEmissions">The total emissions during flaming (double[A,B])</param>
        /// <param name="smolderingEmissions">The total emissions during smoldering (double[A,B])</param>
        /// <param name="flamingTime">The total flaming time (double[A,B])</param>
        /// <param name="smolderingTime">The total smoldering time (double[A,B](_)</param>
        public DispersionModelling(double[] rasterDelta,
               double[,] elevation,
               Dictionary<string, double> config,
               double[,] ros,
               double[,] firelineIntensity,
               double[,] flamingEmissions,
               double[,] smolderingEmissions,
               double[,] flamingTime,
               double[,] smolderingTime)
        {
            this._rasterDelta = rasterDelta;
            this._elevation = elevation;
            this.Config=config;
            this._ros=ros;
            this._firelineIntensity=firelineIntensity;
            this._flamingEmissions = flamingEmissions;
            this._smolderingEmissions = smolderingEmissions;
            this._flamingTime = flamingTime;
            this._smolderingTime = smolderingTime;
            
            this._windAngle = this.Config["windAngle"];
            this._windVelocity = this.Config["windVelocity"];
            this._cellsize = this.Config["cellsize"];
            this._tAmb = this.Config["atmosphericTemp"];
            this._pAmb = this.Config["atmosphericPressure"];
            this._environmentalLapseRate = this.Config["environmentalLapseRate"];
            this._dryAdiabaticLapseRate = this.Config["dryAdiabaticLapseRate"];
        }
        /// <summary>
        /// Get the gaussian dispersion coefficients based on the atmospheric stability index and the surface roughness. This method returns seven parameters used to calculate sigma_y and sigma_z according to Casal
        /// </summary>
        /// <param name="surfaceRoughness">Describes the surface roughness of the area, either "rural" or "urban" (string)</param>
        /// <saves>The seven factors to calculate the dispersion factors sigma_y and sigma_z (double[7])</saves>
        public void GetDispersionCoefficients(string surfaceRoughness)
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

            double[] dispersionCoefficientY = new double[3];
            double[] dispersionCoefficientZ = new double[3];
            double p = 0;

            switch (surfaceRoughness)
            {
                case "rural":
                    switch (this._atmoStabilityIndex)
                    {
                        case 'A':
                            dispersionCoefficientY = [0.22, 0.0001, -0.5];
                            dispersionCoefficientZ = [0.2, 0, 0];
                            p = 0.07;
                            break;
                        case 'B':
                            dispersionCoefficientY = [0.16, 0.0001, -0.5];
                            dispersionCoefficientZ = [0.12, 0, 0];
                            p = 0.07;
                            break;
                        case 'C':
                            dispersionCoefficientY = [0.11, 0.001, -0.5];
                            dispersionCoefficientZ = [0.08, 0.0002, -0.5];
                            p = 0.10;
                            break;
                        case 'D':
                            dispersionCoefficientY = [0.08, 0.0001, -0.5];
                            dispersionCoefficientZ = [0.06, 0.0015, -0.5];
                            p = 0.15;
                            break;
                        case 'E':
                            dispersionCoefficientY = [0.06, 0.0001, -0.5];
                            dispersionCoefficientZ = [0.03, 0.0003, -1];
                            p = 0.35;
                            break;
                        case 'F':
                            dispersionCoefficientY = [0.04, 0.001, -0.5];
                            dispersionCoefficientZ = [0.016, 0.0003, -1];
                            p = 0.55;
                            break;
                    }
                    break;
                case "urban":
                    switch (this._atmoStabilityIndex)
                    {
                        case 'A':
                        case 'B':
                            dispersionCoefficientY = [0.32, 0.0004, -0.5];
                            dispersionCoefficientZ = [0.24, 0.0001, 0.5];
                            p = 0.15;
                            break;
                        case 'C':
                            dispersionCoefficientY = [0.22, 0.0004, -0.5];
                            dispersionCoefficientZ = [0.2, 0, 0];
                            p = 0.20;
                            break;
                        case 'D':
                            dispersionCoefficientY = [0.16, 0.0004, -0.5];
                            dispersionCoefficientZ = [0.20, 0.0003, -0.5];
                            p = 0.25;
                            break;
                        case 'E':
                        case 'F':
                            dispersionCoefficientY = [0.11, 0.0004, -0.5];
                            dispersionCoefficientZ = [0.08, 0.0015, -0.5];
                            p = 0.30;
                            break;
                    }
                    break;
            }

            this._dispCoeff =  [dispersionCoefficientY[0], dispersionCoefficientY[1], dispersionCoefficientY[2], dispersionCoefficientZ[0], dispersionCoefficientZ[1], dispersionCoefficientZ[2], p];
        }
        /// <summary>
        /// Get atmospheric stability index according to criteria set out in Casal
        /// </summary>
        /// <param name="dayOrNight">string specifying whether it is "day" or "night"</param>
        /// <param name="insolation">string describing the sun radiative power, "strong", "moderate", or "slight"</param>
        /// <param name="nightOvercast">string specifying whether the night sky (if dayOrNight is set to "night") is "majority" or "minority" covered by clouds</param>
        /// <param name="stabilityMode">This method uses two methods to estimate the atmospheric stability. "pessimistic" will return the more unstable of the two values and "optimistic" will return the more stable of the two. </param>
        /// <param name="windDirectionVariation">The hourly variation in wind direction in degrees (double)</param>
        /// <param name="windVelocity">The current average wind velocity in m/s (double)</param>
        /// <saves>The current Atmospheric Stability Class from A to F (char)</saves>
        public void GetStabilityClass(string dayOrNight, string insolation, string nightOvercast, string stabilityMode, double windDirectionVariation, double windVelocity)
        {
            char atmoStabilityClassByDirection = 'O';

            if (windDirectionVariation < 3.5)
            {
                atmoStabilityClassByDirection = 'F';
            }
            else if (windDirectionVariation >= 3.5 && windDirectionVariation < 7.5)
            {
                atmoStabilityClassByDirection = 'E';
            }
            else if (windDirectionVariation >= 7.5 && windDirectionVariation < 12.5)
            {
                atmoStabilityClassByDirection = 'D';
            }
            else if (windDirectionVariation >= 12.5 && windDirectionVariation < 17.5)
            {
                atmoStabilityClassByDirection = 'C';
            }
            else if (windDirectionVariation >= 17.5 && windDirectionVariation < 22.5)
            {
                atmoStabilityClassByDirection = 'B';
            }
            else if (windDirectionVariation >= 22.5)
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

            this._atmoStabilityIndex = atmoStabilityClass;
        }
        /// <summary>
        /// Adjust the wind velocity at a target height due to the atmospheric boundary layer, following Hellmann's parameters.
        /// </summary>
        /// <param name="windSpeed">Wind velocity above the ground in m/s (double)</param>
        /// <param name="rawsElevation">Elevation of wind velocity measurement (usually 10m) in m (double)</param>
        /// <param name="sourceElevation">The elevation of the smoke source (injection height) in m (double)</param>
        /// <param name="atmoStabilityClass">Atmospheric stability class ranging from A to F (char)</param>
        /// <returns>The adjusted wind value for a target height in m/s (double)</returns>
        private static double HellmannWindAdjust(double windSpeed, double rawsElevation, double sourceElevation, char atmoStabilityClass)
        {
            float hellmannExponent = 0;
            switch (atmoStabilityClass) {
                case 'A':
                case 'B':
                    hellmannExponent = 0.27f;
                    break;
                case 'C':
                case 'D':
                    hellmannExponent = 0.34f;
                    break;
                case 'E':
                case 'F':
                    hellmannExponent = 0.60f;
                    break;
            }

            return windSpeed * MathF.Pow((float)(sourceElevation / rawsElevation),hellmannExponent);
        }
        /// <summary>
        /// Calculate the error function of a number
        /// </summary>
        /// <param name="x">Input numnber (double)</param>
        /// <returns>Error function of x (double)</returns>
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
        /// <summary>
        /// Rotates 'p1' about 'p2' by 'angle' degrees clockwise.
        /// </summary>
        /// <param name="targetPoint">Point to be rotated</param>
        /// <param name="burningPoint">Point to rotate around</param>
        /// <param name="windAngle">Angle in degrees to rotate clockwise</param>
        /// <returns>The rotated point</returns>
        public static double[] GetCoordsAboutWindAxis(int[] burningPoint, double[] targetPoint, double windAngle)
        {

            double radians = (windAngle)*Math.PI/180;
            double sin = Math.Sin(radians);
            double cos = Math.Cos(radians);

            double p1X = targetPoint[0];
            double p1Y = targetPoint[1];
            double p2X = burningPoint[0];
            double p2Y = burningPoint[1];

            // Translate point back to origin
            p1X -= p2X;
            p1Y -= p2Y;

            // Rotate point
            double xnew = p1X * cos - p1Y * sin;
            double ynew = p1X * sin + p1Y * cos;

            // Translate point back
            double[] newPoint = [-ynew, -xnew];
            return newPoint;
        }
        /// <summary>
        /// Find the injection height based on the Briggs plume rise equations (shamelessly stolen from VSMOKE and Lavdas
        /// </summary>
        /// <param name="atmoStabilityIndex">Pasquill Guilford stability index (char)</param>
        /// <param name="sourceElevation">Elevation of source point in m (double)</param>
        /// <param name="firelineIntensity">Fireline Intensity, as derived from Byram's equation, in kW/m (double)</param>
        /// <param name="windVelocity">the wind velocity at the height of the fire (near ground level) (double)</param>
        /// <param name="tAmb">Ambient temperature in C (double)</param>
        /// <param name="cellsize">Size of the raster cell in m (double)</param>
        /// <returns>The injecion height of smoke (added to the elevation of the source point) in m (double)</returns>
        private static double FindInjectionHeight_Briggs(char atmoStabilityIndex, double sourceElevation, double firelineIntensity, double windVelocity, double tAmb, double cellsize)
        {
            double F = (9.81 / (3.14 * 1.005 * 1.225)) * (firelineIntensity * cellsize / (tAmb + 273));

            double dtheta = 0;
            switch (atmoStabilityIndex)
            {
                case 'A':   //extremely unstable
                case 'B':
                case 'C':
                case 'D':   //neutral
                    break;
                case 'E':
                    dtheta = 0.01;
                    break;
                case 'F':   //moderately stable
                    dtheta = 0.02;
                    break;
            }

            double S = 9.80665 * dtheta / (tAmb + 273);
            double hFLW = 5*Math.Pow(F,0.25)*Math.Pow(S,-3/8);
            double hFS = 2.4 * Math.Pow(F / (windVelocity * 0.278 * S), 0.333);
            double hFNUS;
            if (F<=51.6)
            {
                hFNUS = 21.425 * Math.Pow(F, 0.75) * Math.Pow(windVelocity * 0.278, -1);
            }
            else
            {
                hFNUS = 38.710*Math.Pow(F,0.6)*Math.Pow(windVelocity * 0.278,-1);
            }

            double output = 0;
            if (dtheta > 0.001)
            {
                output = new [] {hFS, hFLW, hFNUS}.Min();
            }
            else
            {
                output = hFNUS;
            }
            
            return output + sourceElevation;
        }
        
        /// <summary>
        /// Find the injection height of the smoke 
        /// </summary>
        /// <param name="sourceElevation">The elevation of the smoke source point in m (double)</param>
        /// <param name="smokeTemp">The temperature of the smoke approximately 1m above the fire in C (double)</param>
        /// <param name="exitVelocity">The upwards velocity of the smoke approximately 1m above the fire in m/s (double)</param>
        /// <param name="windVelocity">The wind speed at injection height in m/s (double). Finding this value required an iterative solution.</param>
        /// <param name="stackDiameter">The cell size of the pixel representing the smoke source point in m (double)</param>
        /// <param name="pAmb">Atmospheric pressure at ground level in bar (double)</param>
        /// <param name="tAmb">Atmospheric temperature at ground level in C (double)</param>
        /// <returns></returns>
        private static double FindInjectionHeight_Holland(double sourceElevation, double smokeTemp, double exitVelocity, double windVelocity, double stackDiameter, double pAmb, double tAmb)
        {
            double a = 1; // ground reflection coefficient, usual conservative value is 1

            double steadyStateHeight = sourceElevation + (exitVelocity * stackDiameter / windVelocity) *
                                   (1.5 + 2.68 * pAmb * stackDiameter *
                                   (smokeTemp - tAmb) / (smokeTemp + 273));

            return steadyStateHeight;
        }
        /// <summary>
        /// Find the smoke injection height according to Andersen
        /// </summary>
        /// <param name="sourceHeight">Height of the smoke source in m (double)</param>
        /// <param name="cellsize">Size of the pixel represented by the source point in m (double)</param>
        /// <param name="tAmb">Ambient ground level temperature in C (double)</param>
        /// <param name="pAmb">Ambient ground level pressure in bar (double)</param>
        /// <param name="environemntalLapseRate">Current environmental Lapse Rate in C/m (double)</param>
        /// <param name="dryAdiabaticLapseRate">Adiabatic Lapse Rate in C/m (double)</param>
        /// <param name="heatPua">Wildfire heat released per unit area in W/m2 (double)</param>
        /// <returns>The predicted injection height in m (double)</returns>
        public static double FindInjectionHeight_Andersen(double sourceHeight, double cellsize, double tAmb, double pAmb, double environemntalLapseRate, double dryAdiabaticLapseRate, double heatPua)
        {
            tAmb = tAmb + 273;                                            //C to K
            environemntalLapseRate = environemntalLapseRate / 1000;         //C/km to C/m
            dryAdiabaticLapseRate = dryAdiabaticLapseRate / 1000;           //C/km to C/m
            heatPua = heatPua * 1000;                                       //kJ/m2 to J/m2

            double qSupplied = heatPua * (cellsize * cellsize) ;

            double tolerance = 100;
            int maxIterations = 100;
            int iteration = 0;

            double guess1 = 0;
            double guess2 = 200;

            double newGuess = 0;

            double qRequired1 = AndersenRequiredEnergy(guess1, cellsize, tAmb, pAmb, environemntalLapseRate, dryAdiabaticLapseRate);
            double qRequired2 = AndersenRequiredEnergy(guess2, cellsize, tAmb, pAmb, environemntalLapseRate, dryAdiabaticLapseRate);


            while (Math.Abs(qSupplied - qRequired2) > tolerance && iteration < maxIterations)
            {
                // Secant Method formula
                newGuess = guess2 - (qRequired2 - qSupplied) * (guess2 - guess1) / (qRequired2 - qRequired1);

                // Update guesses and required energy
                guess1 = guess2;
                qRequired1 = qRequired2;

                guess2 = newGuess;
                qRequired2 = AndersenRequiredEnergy(guess2, cellsize, tAmb, pAmb, environemntalLapseRate, dryAdiabaticLapseRate);

                iteration++;
            }
            return newGuess + sourceHeight;
        }
        /// <summary>
        /// Find required energy to lift a parcel of air to a specific height, to use in the Andersen injection height calculation
        /// </summary>
        /// <param name="targetHeight">The target height for the air parcel to be lifted to in m (double)</param>
        /// <param name="cellsize">The size of the individual cell represented by this point in m (double)</param>
        /// <param name="tAmb">Ambient ground level temperature in C (double)</param>
        /// <param name="pAmb">Ambient ground level pressure in bar (double)</param>
        /// <param name="environemntalLapseRate">Current environmental Lapse Rate in C/m (double)</param>
        /// <param name="dryAdiabaticLapseRate">Adiabatic Lapse Rate in C/m (double)</param>
        /// <returns></returns>
        private static double AndersenRequiredEnergy(double targetHeight, double cellsize, double tAmb, double pAmb, double environemntalLapseRate, double dryAdiabaticLapseRate)
        {
            //energy in J/kg
            double energyPum = -0.5 * 1001 * dryAdiabaticLapseRate * targetHeight *
                Math.Log(1 + (targetHeight * (environemntalLapseRate - dryAdiabaticLapseRate)) / tAmb);
            //mass in kg
            double totalSmokeMass = (pAmb*cellsize*cellsize/9.81)*(1 - Math.Pow((1 + environemntalLapseRate * targetHeight / tAmb), (-9.81 / (environemntalLapseRate * 287.05))));

            return  energyPum * totalSmokeMass;
        }
        /// <summary>
        /// Method to calculate the total smoke concentration above a specific point.
        /// </summary>
        /// <param name="height">The maximum height to be included in this calculation in m (double)</param>
        /// <param name="x">The X coordinate of the target point (along wind direction) in m (double)</param>
        /// <param name="y">The Y coordinate of the target point (perpendicular to wind direction) in m (double)</param>
        /// <param name="dispersionCoefficientY">The sigma_y dispersion coefficient (double)</param>
        /// <param name="dispersionCoefficientZ">The sigma_z dispersion coefficient (double)</param>
        /// <param name="emissionMassFlowRate">The mass flow rate of the emissions in g/s (double)</param>
        /// <param name="windVelocity">The velocity of the wind at the injection height in m/s (double)</param>
        /// <param name="steadyStateHeight">The injection / steady state height of the smoke in meters (double)</param>
        /// <returns>The total smoke concentration above the target point, in μg/m2 (double)</returns>
        public static double TopDownRaster(double height, double x, double y, double dispersionCoefficientY, double dispersionCoefficientZ, double emissionMassFlowRate, double windVelocity, double steadyStateHeight)
        {
            double termA = Math.Sqrt(Math.PI / 2) * (emissionMassFlowRate / (Math.Sqrt(2 * Math.PI) * windVelocity * dispersionCoefficientZ * dispersionCoefficientY)) *
                            Math.Exp(-Math.Pow(y, 2) / (2 * Math.Pow(dispersionCoefficientY, 2)));

            double zMax = height;
            double zMin = 0;        //Hardcoded values instead of integrating 

            double termB = Erf((zMax - steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2))) +
                            Erf((zMax + steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2)));

            double termC = Erf((zMin - steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2))) +
                            Erf((zMin + steadyStateHeight) / (dispersionCoefficientZ * Math.Sqrt(2)));

            double output = termA * dispersionCoefficientZ * (termB - termC);

            return output;
        }
        /// <summary>
        /// Method to calculate the smoke concentration (in μg/m3) at driver level (1m above ground level) at a specific point. This method assumes the source point is at (0,0) and the target point's coordinates are in relation to the direction of the wind.
        /// </summary>
        /// <param name="targetHeight">The height of the target point in m (double)</param>
        /// <param name="sourceHeight">The height of the source point in m (double)</param>
        /// <param name="x">The X coordinate of the target point (along wind direction) in m (double)</param>
        /// <param name="y">The Y coordinate of the target point (perpendicular to wind direction) in m (double)</param>
        /// <param name="dispersionCoefficientY">The sigma_y dispersion coefficient (double)</param>
        /// <param name="dispersionCoefficientZ">The sigma_z dispersion coefficient (double)</param>
        /// <param name="emissionMassFlowRate">The mass flow rate of the emissions in g/s (double)</param>
        /// <param name="windVelocity">The velocity of the wind at the injection height in m/s (double)</param>
        /// <param name="steadyStateHeight">The injection / steady state height of the smoke in meters (double)</param>
        /// <returns>The concentration of smoke at the target point in μg/m3 (double)</returns>
        public static double DriverLevelDensity(double targetHeight, double sourceHeight, double x, double y, double dispersionCoefficientY, double dispersionCoefficientZ, double emissionMassFlowRate, double windVelocity, double steadyStateHeight)
        {   
            double h = targetHeight - sourceHeight;
            //h = (h > steadyStateHeight) ? steadyStateHeight : h;

            if (h > steadyStateHeight)
            {
                h = steadyStateHeight;
                targetHeight = h;
            }

            double z = 1.0 + targetHeight ; //Driver level height
            double term1 = emissionMassFlowRate / (2.5 * windVelocity * dispersionCoefficientZ * dispersionCoefficientY);
            double term2 = Math.Exp(-(y*y) / (2 * dispersionCoefficientY * dispersionCoefficientY));
            double term3 = Math.Exp(-(z - steadyStateHeight)*(z - steadyStateHeight) / (2 * dispersionCoefficientZ * dispersionCoefficientZ));
            double term4 = Math.Exp(-(z + steadyStateHeight - 2 * h)*(z + steadyStateHeight - 2 * h) / (2 * dispersionCoefficientZ * dispersionCoefficientZ));

            double output = term1 * term2 * (term3 + term4);
            
            return term1 * term2 * (term3 + term4);
        }

        /// <summary>
        /// Main entry point to calculate the current smoke concentration
        /// </summary>
        /// <param name="debug">Boolean to calculate additional outputs, such as total smoke concentration. Enabling this significantly increases computational time.</param>
        public void DispersionModel(bool debug)
        {
            double maxHeight = 300000;
            double[] steadyStateHeight = [100,100]; //initial guess
            double[] smokeTemp = [200, 75];
            double[] exitVelocity = [2, 0.4];
            double[] firelineIntensityConversionFactor = [1f, 0.3f];

            if (_windAngle == 0 || _windAngle == 90 || _windAngle == 180 || _windAngle == 270) {_windAngle += 0.1;}  //solve some divide by zero errors. 
            _windAngle -= 90;
            double windAngleRad = ( _windAngle) * (Math.PI / 180);
            
            int rows = this._elevation.GetLength(0);
            int cols = this._elevation.GetLength(1);

            InitialiseOutputs();

            Console.WriteLine("Getting smoke emitting properties");
            //Find all the gaussian model parameters of each point emitting smoke (arrays are [flaming,smoldering])
            // i and j below are in ROS raster coordinates
            int totalBurningCells = 0;
            List<double> burningElevations = new List<double>();
            List<double[]> injectionMassFlowrates = new List<double[]>();
            List<double[]> steadyStateHeights = new List<double[]>();
            List<int[]> burningCoords = new List<int[]>();
            List<double[]> phases = new List<double[]>();
            for (int i = 0; i < this._ros.GetLength(0); i++)
            {
                for (int j = 0; j < this._ros.GetLength(1); j++)
                {
                    // check if the point is producing smoke (burning or smoldering)
                    if (this.currentlyBurning[i, j] != 0)
                    {
                        totalBurningCells++;
                        int[] elevCoords = [i - (int)_rasterDelta[0], j - (int)_rasterDelta[1]];
                        double[] currentPhases = AdjustForSubgrid(i, j);
                        phases.Add(currentPhases);
                        burningCoords.Add(elevCoords);
                        burningElevations.Add(this._elevation[elevCoords[0],elevCoords[1]]);
                        injectionMassFlowrates.Add([
                            this._flamingEmissions[i, j] / this._flamingTime[i, j],
                            this._smolderingEmissions[i, j] / this._smolderingEmissions[i, j]
                        ]);
                        
                        for (int phase = 0; phase<=1; phase++)
                        {
                            steadyStateHeight[phase] = 0f;
                            if (currentPhases[phase] > 0)
                            {
                                if (phase == 0)
                                {
                                    this._fireActivePoints[elevCoords[0],elevCoords[1]] = 1;
                                }
                                else
                                {
                                    this._smolderActivePoints[elevCoords[0],elevCoords[1]] = 1;
                                }
                                steadyStateHeight[phase] = FindInjectionHeight_Briggs(_atmoStabilityIndex,this._elevation[i, j],
                                    this._firelineIntensity[i, j] * firelineIntensityConversionFactor[phase], this._windVelocity,
                                    this._tAmb, this._cellsize);
                                //double delta = 11;
                                //while (Math.Abs(delta) > 3)
                                //{
                                    //steadyStateHeight[phase] = FindInjectionHeight_Holland(this._elevation[i, j], smokeTemp[phase], exitVelocity[phase], _windVelocity * 0.277778, this._cellsize, _pAmb / 100000, _tAmb);
                                    //_windVelocity = HellmannWindAdjust(Config["windVelocity"], Config["RAWSelevation"], steadyStateHeight[phase], _atmoStabilityIndex);
                                    //delta = steadyStateHeight[phase] - FindInjectionHeight_Holland(_elevation[i, j], smokeTemp[phase], exitVelocity[phase], _windVelocity * 0.277778, _cellsize, _pAmb / 100000, _tAmb);
                                //}
                                _injectionHeight[i, j] = steadyStateHeight[phase];
                            }
                        }
                        steadyStateHeights.Add(steadyStateHeight);
                    }
                }
            }

            int count = 0;
            float output = 0;
            Console.WriteLine("Calculating Smoke Concentration");
            
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            //below i and j are smoke / elevation domain coordinates
            Parallel.For(0, this._elevation.GetLength(0), i =>
            {
                for (int j = 0; j < this._elevation.GetLength(1); j++)
                {
                    // check if the point is producing smoke (burning or smoldering)

                    count++;
                    this._subgridOut[i, j] = this.SubgridTime[i, j];

                    for (int burnPoint = 0; burnPoint < burningElevations.Count; burnPoint++)
                    {
                        for (int phase = 0; phase <= 1; phase++)
                        {
                            if (phases[burnPoint][phase] != 0)
                            {
                                output = (float)burningElevations[burnPoint];
                                double[] xYplume =
                                    GetCoordsAboutWindAxis(burningCoords[burnPoint], [i, j],
                                        _windAngle); //get point dimensions in relation to the burning point (0,0) and the wind direction (x axis)
                                
                                //when the target point is the burning point
                                //XYplume is zero, the zero occurs in the denominator of TopDownRaster function, TopDownRaster function died, so just add 0 instead of running the function.
                                if (!(xYplume[0] == 0 && xYplume[1] == 0) && xYplume[0]>=0)
                                {
                                    double x = xYplume[0] * _cellsize;
                                    double y = xYplume[1] * _cellsize; //convert to meters

                                    double dispersionCoefficientY =
                                        _dispCoeff[0] * x * Math.Pow(1 + _dispCoeff[1] * x, _dispCoeff[2]);
                                    double dispersionCoefficientZ =
                                        _dispCoeff[3] * x *
                                        Math.Pow(1 + _dispCoeff[4] * x, _dispCoeff[5]); //get s_y, s_z for this point
                                    
                                    if (debug)
                                    {
                                        _topDownRaster[i, j] += TopDownRaster(maxHeight, x, y, dispersionCoefficientY,
                                            dispersionCoefficientZ, injectionMassFlowrates[burnPoint][phase],
                                            _windVelocity,
                                            steadyStateHeights[burnPoint][phase]);
                                    }

                                    _driverLevelDensity[i, j] += 0.6*DriverLevelDensity(this._elevation[i, j],
                                        burningElevations[burnPoint], x, y, dispersionCoefficientY,
                                        dispersionCoefficientZ, injectionMassFlowrates[burnPoint][phase], _windVelocity,
                                        steadyStateHeights[burnPoint][phase]) + 0.4*DriverLevelDensity(this._elevation[i, j],
                                        burningElevations[burnPoint], x, y, dispersionCoefficientY,
                                        dispersionCoefficientZ, injectionMassFlowrates[burnPoint][phase], _windVelocity,
                                        burningElevations[burnPoint]);
                                }
                            }
                        }
                        //Console.Write($"\r ---- {100 * count / (this._elevation.GetLength(0)*this._elevation.GetLength(1))}% done, current point [ {i}, {j} ], Injection Height: {(output-_elevation[i, j]).ToString(".0")} meters                     ");
                    }
                }
            });
            stopwatch.Stop();
            Console.WriteLine($"Timestep Complete. Elapsed Time: {stopwatch.Elapsed}");
            Console.WriteLine("============================");
            Console.WriteLine(" ");
        }

        /// <summary>
        /// Get subgrid scaling value for current timestep
        /// </summary>
        /// <param name="i">1st coordinate of point</param>
        /// <param name="j">2nd coordinate of point</param>
        /// <returns>2-element array of scaling factors [flaming, smoldering]</returns>
        private double[] AdjustForSubgrid(int i, int j)
        {
            double fireTime = this._flamingTime[i, j];
            double smolderTime = this._smolderingTime[i, j];
            double ros = this._ros[i, j];
            double cell = this._cellsize;
            double currentTime = this.SubgridTime[i, j];
            
            double maxFlamingOutputFraction = Math.Clamp(ros * fireTime / cell, 0, 1);
            double maxSmolderingOutputFraction = Math.Clamp(ros * smolderTime / cell, 0, 1);
            double flaming = 0;
            double smoldering = 0;

            if (ros < 0) 
            {
                return [0, 0];
            }

            if (currentTime <= 0)
            {
                return [0, 0];
            }
            else if (currentTime <= fireTime)
            {
                flaming =  currentTime / fireTime;
            }
            else if (currentTime <= cell / ros) 
            {
                flaming =  1;
            }
            else if (currentTime <= fireTime + cell / ros)
            {
                flaming = 1 - (currentTime - cell/ros) / fireTime;
            }

            if (currentTime <= fireTime)
            {
                smoldering = 0;
            }
            else if (currentTime <= fireTime + cell / ros)
            {
                smoldering = (currentTime - fireTime) / (cell/ros);
            }
            else if (currentTime <= fireTime + smolderTime)
            {
                smoldering = 1;
            }
            else if (currentTime <= fireTime + smolderTime + cell / ros)
            {
                smoldering = 1 - (currentTime - fireTime - smolderTime) / (cell / ros);
            }

            return [flaming * maxFlamingOutputFraction, smoldering * maxSmolderingOutputFraction];
        }
        /// <summary>
        /// Set the dimensions for the output rasters
        /// </summary>
        private void InitialiseOutputs()
        {
            int rows = this._elevation.GetLength(0);
            int cols = this._elevation.GetLength(1);
            this._topDownRaster = new double[rows, cols];
            this._driverLevelDensity = new double[rows, cols];
            this._fireActivePoints = new double[rows, cols];
            this._smolderActivePoints = new double[rows, cols];
            this._injectionHeight = new double[rows, cols];
            this._subgridOut = new double[rows, cols];
        }
        /// <summary>
        /// Save the current smoke simulation timestep in csv format.
        /// </summary>
        /// <param name="path">The folder where the outputs will be saved (ending with /)</param>
        /// <param name="totalTime">The current elapsed simulation time (to append to output file name)</param>
        /// <param name="debug">Boolean to control whether additional rasters will be saved to files (if True)</param>
        public void SaveTimestep(string path, int totalTime, bool debug = false)
        {
            Helpers.WriteMatrixToCsv(this._driverLevelDensity, path + totalTime.ToString() + "driverLevelDensity.csv");
            if (debug)
            {
                Helpers.WriteMatrixToCsv(_topDownRaster, path + totalTime.ToString() + "topDownRaster.csv");
                Helpers.WriteMatrixToCsv(_fireActivePoints, path + totalTime.ToString() + "flamingAmount.csv");
                Helpers.WriteMatrixToCsv(_smolderActivePoints, path + totalTime.ToString() + "smolderingAmount.csv");
                Helpers.WriteMatrixToCsv(_injectionHeight, path + totalTime.ToString() + "injectionHeight.csv");
                Helpers.WriteMatrixToCsv(_subgridOut, path + totalTime.ToString() + "subgridTime.csv");
            }
            Console.WriteLine("Outputs Saved!");
            Console.WriteLine(("============================"));
        }
    }
}