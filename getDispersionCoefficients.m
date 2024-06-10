function [P,dispersionCoefficientY,dispersionCoefficientZ] = getDispersionCoefficients(dayOrNight,surfaceRoughness,insolation,nightOvercast,stabilityMode,windVelocityDirectionVariation,windVelocity)
   
    
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
            if windVelocity < 2 
                switch insolation
                    case 'strong'
                        atmoStabilityClassByIrradiance = 'A';
                    case 'moderate'
                        atmoStabilityClassByIrradiance = 'A';
                    case 'slight'
                        atmoStabilityClassByIrradiance = 'B';
                end
            elseif windVelocity >= 2 && windVelocity < 3
                switch insolation
                    case 'strong'
                        atmoStabilityClassByIrradiance = 'B';
                    case 'moderate'
                        atmoStabilityClassByIrradiance = 'B';
                    case 'slight'
                        atmoStabilityClassByIrradiance = 'C';
                end
            elseif windVelocity >= 3 && windVelocity < 5
                switch insolation
                    case 'strong'
                        atmoStabilityClassByIrradiance = 'B';
                    case 'moderate'
                        atmoStabilityClassByIrradiance = 'C';
                    case 'slight'
                        atmoStabilityClassByIrradiance = 'C';
                end
            elseif windVelocity >= 5 && windVelocity < 6
                switch insolation
                    case 'strong'
                        disp('ERROR: Atmospheric Stability mismatch: too strong wind for strong insolation!');
                    case 'moderate'
                        atmoStabilityClassByIrradiance = 'C';
                    case 'slight'
                        atmoStabilityClassByIrradiance = 'D';
                end
            elseif windVelocity >= 6
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
                    if windVelocity < 3
                        atmoStabilityClassByIrradiance = 'E';
                    else
                        atmoStabilityClassByIrradiance = 'D';
                    end
                case 'minority'
                    if windVelocity < 3
                        atmoStabilityClassByIrradiance = 'F';
                    elseif windVelocity >= 3 && windVelocity < 5
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
                    %dispersionCoefficientY = 0.22.*lengthwaysSpace.*(1+0.0001.*lengthwaysSpace).^-0.5; 
                    %dispersionCoefficientZ = 0.20.*lengthwaysSpace;
                    dispersionCoefficientY=[0.22,0.0001,-0.5];
                    dispersionCoefficientZ=[0.2,0,0];
                    P=0.07;
                case 'B'
                    %dispersionCoefficientY = 0.16.*lengthwaysSpace.*(1+0.0001.*lengthwaysSpace).^-0.5; 
                    %dispersionCoefficientZ = 0.12.*lengthwaysSpace;
                    dispersionCoefficientY=[0.16,0.0001,-0.5];
                    dispersionCoefficientZ=[0.12,0,0];
                    P=0.07;
                case 'C'
                    %dispersionCoefficientY = 0.11.*lengthwaysSpace.*(1+0.0001.*lengthwaysSpace).^-0.5; 
                    %dispersionCoefficientZ = 0.08.*lengthwaysSpace.*(1+0.0002.*lengthwaysSpace).^-0.5;
                    dispersionCoefficientY=[0.11,0.001,-0.5];
                    dispersionCoefficientZ=[0.08,0,0002,-0.5];
                    P=0.10;
                case 'D'
                    %dispersionCoefficientY = 0.08.*lengthwaysSpace.*(1+0.0001.*lengthwaysSpace).^-0.5; 
                    %dispersionCoefficientZ = 0.06.*lengthwaysSpace.*(1+0.0015.*lengthwaysSpace).^-0.5;
                    dispersionCoefficientY=[0,08,0.0001,-0.5];
                    dispersionCoefficientZ=[0.06,0.0015,-0.5];
                    P=0.15;
                case 'E'
                    %dispersionCoefficientY = 0.06.*lengthwaysSpace.*(1+0.0001.*lengthwaysSpace).^-0.5; 
                    %dispersionCoefficientZ = 0.03.*lengthwaysSpace.*(1+0.0003.*lengthwaysSpace).^-1;
                    dispersionCoefficientY=[0.06,0.0001,-0.5];
                    dispersionCoefficientZ=[0.03,0.0003,-1];
                    P=0.35;
                case 'F'
                    %dispersionCoefficientY = 0.04.*lengthwaysSpace.*(1+0.0001.*lengthwaysSpace).^-0.5; 
                    %dispersionCoefficientZ = 0.016.*lengthwaysSpace.*(1+0.0003.*lengthwaysSpace).^-1;
                    dispersionCoefficientY=[0.04,0.001,-0.5];
                    dispersionCoefficientZ=[0.016,0.0003,-1];
                    P=0.55;
            end
        case 'urban'
            switch atmoStabilityClass
                case {'A' 'B'}
                    %dispersionCoefficientY = 0.32.*lengthwaysSpace.*(1+0.0004.*lengthwaysSpace).^-0.5; 
                    %dispersionCoefficientZ = 0.24.*lengthwaysSpace.*(1+0.0001.*lengthwaysSpace).^0.5; 
                    dispersionCoefficientY=[0.32,0.0004,-0.5];
                    dispersionCoefficientZ=[0.24,0.0001,0.5];
                    P=0.15;
                case 'C'
                    %dispersionCoefficientY = 0.22.*lengthwaysSpace.*(1+0.0004.*lengthwaysSpace).^-0.5; 
                    %dispersionCoefficientZ = 0.20.*lengthwaysSpace;
                    dispersionCoefficientY=[0.22,0.0004,-0.5];
                    dispersionCoefficientZ=[0.2,0,0];
                    P=0.20;
                case 'D'
                    %dispersionCoefficientY = 0.16.*lengthwaysSpace.*(1+0.0004.*lengthwaysSpace).^-0.5; 
                    %dispersionCoefficientZ = 0.20.*lengthwaysSpace.*(1+0.0003.*lengthwaysSpace).^-0.5;
                    dispersionCoefficientY=[0.16,0.0004,-0.5];
                    dispersionCoefficientZ=[0.20,0.0003,-0.5];
                    P=0.25;
                case {'E' 'F'}
                    %dispersionCoefficientY = 0.11.*lengthwaysSpace.*(1+0.0004.*lengthwaysSpace).^-0.5; 
                    %dispersionCoefficientZ = 0.08.*lengthwaysSpace.*(1+0.0015.*lengthwaysSpace).^-0.5;
                    dispersionCoefficientY=[0.11,0.0004,-0.5];
                    dispersionCoefficientZ=[0.08,0.0015,-0.5];
                    P=0.30;
            end
    end
end