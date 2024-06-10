function [topDownRaster,driverLevelDensity] = dispersionModel(P,threshold,smokeTemp,exitVelocity,windVelocity,dispCoeffY, dispCoeffZ,steps,emissionMassFlowRate)

%% INPUT PARAMETERS

stackDiameter = 20; %simulated stack diameter (m)
atmosphericP = 1; %atmospheric pressure (bar)
atmosphericTemp = 300; %Ambient temperature (K)
a = 1; %ground reflection coefficient, usual conservative value is 1

%% Find Max and Min dimensions of smoke cloud
steadyStateHeight = (exitVelocity*stackDiameter/windVelocity)*(1.5+2.68*atmosphericP*stackDiameter*(smokeTemp+273-atmosphericTemp)/(smokeTemp+273)); %steady state plume height (meters)

xMax=2*findMax(windVelocity,threshold,emissionMassFlowRate,dispCoeffY,dispCoeffZ,steadyStateHeight,'x');
yMax=4*findMax(windVelocity,threshold,emissionMassFlowRate,dispCoeffY,dispCoeffZ,steadyStateHeight,'y');
zMin=steadyStateHeight - findMax(windVelocity,threshold,emissionMassFlowRate,dispCoeffY,dispCoeffZ,steadyStateHeight,'z');
zMax=steadyStateHeight + findMax(windVelocity,threshold,emissionMassFlowRate,dispCoeffY,dispCoeffZ,steadyStateHeight,'z');

%F=9.81*exitVelocity*stackDiameter.^2*()
%%

topDownRaster = zeros(ceil(xMax/steps),ceil(yMax/steps));
driverLevelDensity = zeros(ceil(xMax/steps),ceil(yMax/steps));

for x=1:ceil(xMax/steps)
    for y=1:ceil(yMax/steps)
        dispersionCoefficientY = dispCoeffY(1)*x*steps*(1+dispCoeffY(2)*x*steps)^dispCoeffY(3);
        dispersionCoefficientZ = dispCoeffZ(1)*x*steps*(1+dispCoeffZ(2)*x*steps)^dispCoeffZ(3);

        termA = (pi()/2)*(emissionMassFlowRate/(2*pi()*windVelocity*dispersionCoefficientZ*dispersionCoefficientY))*exp(-(y*steps).^2/(2*dispersionCoefficientY.^2));
        termB = erf((zMax-steadyStateHeight)./(dispersionCoefficientZ*sqrt(2)))+erf((zMax+steadyStateHeight)/dispersionCoefficientZ*sqrt(2));
        termC = erf((zMin-steadyStateHeight)./(dispersionCoefficientZ*sqrt(2)))+erf((zMin+steadyStateHeight)/dispersionCoefficientZ*sqrt(2));
        topDownRaster(x,y) = termA*dispersionCoefficientZ*(termB-termC);
        if zMin < 1
            z=1;
            term1=emissionMassFlowRate/(2*pi()*windVelocity*dispersionCoefficientZ*dispersionCoefficientY);
            term2=exp((-((y*steps)^2))/(2*dispersionCoefficientY.^2));
            term3=exp((-((z-steadyStateHeight)^2))/(2*dispersionCoefficientZ.^2));
            term4=a*exp((-((z+steadyStateHeight)^2))/(2*dispersionCoefficientZ.^2));
            driverLevelDensity(x,y)=term1*term2*(term3+term4);
        end

    end
end

end

