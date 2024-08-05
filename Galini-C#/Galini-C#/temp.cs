using static IronPython.SQLite.PythonSQLite;

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
                    }
                    else
                    {
                        double _x = XYplume[0] * cellsize_Smoke;
                        double _y = XYplume[1] * cellsize_Smoke;      //convert to meters

                        double dispersionCoefficientY = dispCoeff[0] * _x * Math.Pow(1 + dispCoeff[1] * _x, dispCoeff[2]);
                        double dispersionCoefficientZ = dispCoeff[3] * _x * Math.Pow(1 + dispCoeff[4] * _x, dispCoeff[5]);      //get s_y, s_z for this point

                        topDownRaster[x, y] += TopDownRaster(maxHeight, _x, _y, dispersionCoefficientY, dispersionCoefficientZ, flamingEmissionsFlowrate[i, j], windVelocity, steadyStateHeight);
                        driverLevelDensity[x, y] += DriverLevelDensity(elevationSmokeDomain[x, y], elevationSmokeDomain[(int)burningPoint_smokeDomain[0], (int)burningPoint_smokeDomain[1]], _x, _y, dispersionCoefficientY, dispersionCoefficientZ, flamingEmissionsFlowrate[i, j], windVelocity, steadyStateHeight);
                        smokeBelowTerrain[x, y] += TopDownRaster(elevationSmokeDomain[x, y], _x, _y, dispersionCoefficientY, dispersionCoefficientZ, flamingEmissionsFlowrate[i, j], windVelocity, steadyStateHeight);


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
                if (Ybound > cols) {Ybound = cols;}

                for (int y = 0; y < Ybound; y++)
                {
                    double[] XYplume = GetCoordsAboutWindAxis(burningPoint_smokeDomain, [x, y], windAngle); //get point dimensions in relation to the burning point (0,0) and the wind direction (x axis)
                                                                                                            //when the target point is burning point, same as above
                    if (XYplume[0] == 0 && XYplume[1] == 0)
                    {
                    }

                    else
                    {
                        double _x = XYplume[0] * cellsize_Smoke;
                        double _y = XYplume[1] * cellsize_Smoke;      //convert to meters

                        double dispersionCoefficientY = dispCoeff[0] * _x * Math.Pow(1 + dispCoeff[1] * _x, dispCoeff[2]);
                        double dispersionCoefficientZ = dispCoeff[3] * _x * Math.Pow(1 + dispCoeff[4] * _x, dispCoeff[5]);      //get s_y, s_z for this point

                        topDownRaster[x, y] += TopDownRaster(maxHeight, _x, _y, dispersionCoefficientY, dispersionCoefficientZ, flamingEmissionsFlowrate[i, j], windVelocity, steadyStateHeight);
                        driverLevelDensity[x, y] += DriverLevelDensity(elevationSmokeDomain[x, y], _x, _y, dispersionCoefficientY, dispersionCoefficientZ, flamingEmissionsFlowrate[i, j], windVelocity, steadyStateHeight);
                        smokeBelowTerrain[x, y] += TopDownRaster(elevationSmokeDomain[x, y], _x, _y, dispersionCoefficientY, dispersionCoefficientZ, flamingEmissionsFlowrate[i, j], windVelocity, steadyStateHeight);
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

                        topDownRaster[x, y] += TopDownRaster(maxHeight, _x, _y, dispersionCoefficientY, dispersionCoefficientZ, smolderingEmissionsFlowrate[i, j], windVelocity, steadyStateHeight);
                        driverLevelDensity[x, y] += DriverLevelDensity(elevationSmokeDomain[x, y], _x, _y, dispersionCoefficientY, dispersionCoefficientZ, flamingEmissionsFlowrate[i, j], windVelocity, steadyStateHeight);
                        smokeBelowTerrain[x, y] += TopDownRaster(elevationSmokeDomain[x, y], _x, _y, dispersionCoefficientY, dispersionCoefficientZ, flamingEmissionsFlowrate[i, j], windVelocity, steadyStateHeight);
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

                        topDownRaster[x, y] += TopDownRaster(maxHeight, _x, _y, dispersionCoefficientY, dispersionCoefficientZ, smolderingEmissionsFlowrate[i, j], windVelocity, steadyStateHeight);
                        driverLevelDensity[x, y] += DriverLevelDensity(elevationSmokeDomain[x, y], _x, _y, dispersionCoefficientY, dispersionCoefficientZ, flamingEmissionsFlowrate[i, j], windVelocity, steadyStateHeight);
                        smokeBelowTerrain[x, y] += TopDownRaster(elevationSmokeDomain[x, y], _x, _y, dispersionCoefficientY, dispersionCoefficientZ, flamingEmissionsFlowrate[i, j], windVelocity, steadyStateHeight);
                    }
                }
            }
        }
        break;
}