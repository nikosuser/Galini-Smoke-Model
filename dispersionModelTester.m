
xRes=100;
xMax=6000;
wind=6;
ROS=3;
windVelocityDirectionVariation=1;
exitVelocity=4;
smokeTemp=800;
tolerance=10^-6;

dayOrNight='day';
surfaceRoughness='rural';
insolation='moderate';
nightOvercast='minority';
stabilityMode='optimistic';

stackDiameter = 15; %simulated stack diameter (m)
atmosphericP = 1; %atmospheric pressure (bar)
atmosphericTemp = 300; %Ambient temperature (K)
a = 1; %ground reflection coefficient, usual conservative value is 1

emissionMassFlowRate = 0.3*ROS; %Emission mass flow rate (kg/s)

if emissionMassFlowRate <=0
    emissionMassFlowRate=0.1;
end

yMax=300;

ySlices=16;
zSlices=16;

%%
    if windVelocityDirectionVariation < 3.5
        atmoStabilityClassByDirection = 'F';
    elseif windVelocityDirectionVariation >= 3.5 && windVelocityDirectionVariation < 7.5
        atmoStabilityClassByDirection = 'E';
    elseif windVelocityDirectionVariation >= 7.5 && windVelocityDirectionVariation < 12.5
        atmoStabilityClassByDirection = 'D';
    elseif windVelocityDirectionVariation >= 12.5 && windVelocityDirectionVariation < 17.5
        atmoStabilityClassByDirection = 'C';
    elseif windVelocityDirectionVariation >= 17.5 && windVelocityDirectionVariation < 22.5
        atmoStabilityClassByDirection = 'B';
    elseif windVelocityDirectionVariation >= 22.5
        atmoStabilityClassByDirection = 'A';
    end
    
    switch dayOrNight
        case 'day'
            if wind < 2 
                switch insolation
                    case 'strong'
                        atmoStabilityClassByIrradiance = 'A';
                    case 'moderate'
                        atmoStabilityClassByIrradiance = 'A';
                    case 'slight'
                        atmoStabilityClassByIrradiance = 'B';
                end
            elseif wind >= 2 && wind < 3
                switch insolation
                    case 'strong'
                        atmoStabilityClassByIrradiance = 'B';
                    case 'moderate'
                        atmoStabilityClassByIrradiance = 'B';
                    case 'slight'
                        atmoStabilityClassByIrradiance = 'C';
                end
            elseif wind >= 3 && wind < 5
                switch insolation
                    case 'strong'
                        atmoStabilityClassByIrradiance = 'B';
                    case 'moderate'
                        atmoStabilityClassByIrradiance = 'C';
                    case 'slight'
                        atmoStabilityClassByIrradiance = 'C';
                end
            elseif wind >= 5 && wind < 6
                switch insolation
                    case 'strong'
                        disp('ERROR: Atmospheric Stability mismatch: too strong wind for strong insolation!');
                    case 'moderate'
                        atmoStabilityClassByIrradiance = 'C';
                    case 'slight'
                        atmoStabilityClassByIrradiance = 'D';
                end
            elseif wind >= 6
                switch insolation
                    case 'strong'
                        disp('ERROR: Atmospheric Stability mismatch: too strong wind for strong insolation!');
                    case 'moderate'
                        atmoStabilityClassByIrradiance = 'D';
                    case 'slight'
                        atmoStabilityClassByIrradiance = 'D';
                end
            end
        case 'night'
            switch nightOvercast
                case 'majority'
                    if wind < 3
                        atmoStabilityClassByIrradiance = 'E';
                    else
                        atmoStabilityClassByIrradiance = 'D';
                    end
                case 'minority'
                    if wind < 3
                        atmoStabilityClassByIrradiance = 'F';
                    elseif wind >= 3 && wind < 5
                        atmoStabilityClassByIrradiance = 'E';
                    else
                        atmoStabilityClassByIrradiance = 'D';
                    end
            end
    end
    
    
    if atmoStabilityClassByIrradiance == atmoStabilityClassByDirection
        atmoStabilityClass = atmoStabilityClassByDirection;
    elseif strcmp(stabilityMode,'pessimistic')
        atmoStabilityClass = char(max(atmoStabilityClassByDirection, atmoStabilityClassByIrradiance));
    elseif strcmp(stabilityMode,'optimistic')
        atmoStabilityClass = char(min(atmoStabilityClassByDirection, atmoStabilityClassByIrradiance));
    end
    switch surfaceRoughness
        case'rural'
            switch atmoStabilityClass
                case 'A'
                    dispersionCoefficientY = @(x) 0.22*x*(1+0.0001*x)^-0.5; 
                    dispersionCoefficientZ = @(x) 0.20*x;
                case 'B'
                    dispersionCoefficientY = @(x) 0.16*x*(1+0.0001*x)^-0.5; 
                    dispersionCoefficientZ = @(x) 0.12*x;
                case 'C'
                    dispersionCoefficientY = @(x) 0.11*x*(1+0.0001*x)^-0.5; 
                    dispersionCoefficientZ = @(x) 0.08*x*(1+0.0002*x)^-0.5;
                case 'D'
                    dispersionCoefficientY = @(x) 0.08*x*(1+0.0001*x)^-0.5; 
                    dispersionCoefficientZ = @(x) 0.06*x*(1+0.0015*x)^-0.5;
                case 'E'
                    dispersionCoefficientY = @(x) 0.06*x*(1+0.0001*x)^-0.5; 
                    dispersionCoefficientZ = @(x) 0.03*x*(1+0.0003*x)^-1;
                case 'F'
                    dispersionCoefficientY = @(x) 0.04*x*(1+0.0001*x)^-0.5; 
                    dispersionCoefficientZ = @(x) 0.016*x*(1+0.0003*x)^-1;
            end
        case 'urban'
            switch atmoStabilityClass
                case {'A' 'B'}
                    dispersionCoefficientY = @(x) 0.32*x*(1+0.0004*x)^-0.5; 
                    dispersionCoefficientZ = @(x) 0.24*x*(1+0.0001*x)^0.5; 
                case 'C'
                    dispersionCoefficientY = @(x) 0.22*x*(1+0.0004*x)^-0.5; 
                    dispersionCoefficientZ = @(x) 0.20*x;
                case 'D'
                    dispersionCoefficientY = @(x) 0.16*x*(1+0.0004*x)^-0.5; 
                    dispersionCoefficientZ = @(x) 0.20*x*(1+0.0003*x)^-0.5;
                case {'E' 'F'}
                    dispersionCoefficientY = @(x) 0.11*x*(1+0.0004*x)^-0.5; 
                    dispersionCoefficientZ = @(x) 0.08*x*(1+0.0015*x)^-0.5;
            end
    end

%%
steadyStateHeight = (exitVelocity*stackDiameter/wind)*(1.5+2.68*atmosphericP*stackDiameter*(smokeTemp-atmosphericTemp)/smokeTemp); %steady state plume height (meters)

term1=@(x) emissionMassFlowRate/(2*pi()*wind*dispersionCoefficientZ(x)*dispersionCoefficientY(x));
%term2=@(x,y) exp(((-(y).^2))/(2*dispersionCoefficientY(x).^2));
%term3=@(x,z) exp(((z-steadyStateHeight)^2)/(2*dispersionCoefficientZ(x)^2));
%term4=@(x,z) a*exp(((z+steadyStateHeight).^2)/(2*dispersionCoefficientZ(x).^2));

smokespacePos = @(x,y) steadyStateHeight + sqrt( -(  log(tolerance./term1(x)) + y.^2./dispersionCoefficientY(x).^2) .* 2 .* dispersionCoefficientZ(x) .^ 2);
smokespaceNeg = @(x,y) steadyStateHeight -sqrt( -(  log(tolerance./term1(x)) + y.^2./dispersionCoefficientY(x).^2) .* 2 .* dispersionCoefficientZ(x) .^ 2);

figure
fsurf(smokespacePos,[0 4000 -200 200]);
hold on;
fsurf(smokespaceNeg,[0 4000 -200 200]);
axis equal