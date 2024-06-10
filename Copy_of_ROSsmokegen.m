clear all;
close all;

%y,x

%surfaceRoughness = 'urban'; %urban / rural
%inversionHeight = 600; %m above ground where inversion layer occurs.
%insolation = 'moderate'; % strong / moderate / slight sun irradiance, inverse to cloud coverage.
%nightOvercast = 'majority'; % majority / minority : more or less than 50% nighttime overcast (one hour before and after sunset and sunrise)
%dayOrNight = 'day'; % day / night Day or Night Time
%stabilityMode = 'pessimistic'; %optimistic / pessimistic wrt choosing between two mismatching atmospheric stability calculations. Optimistic will choose unstable conditions

maximumPlumeLength = 6000; %Maximum simulation length in line with plume evolution (m)
lengthwiseResolution = 50; %lengthways plume slice distance (m)

rateofSpread = readmatrix(strcat('C:\Users\nikos\OneDrive\Desktop\smoke\RateofSpread.txt'));

IgnitionNode = [433,479];
cellsize=30;
timestep=60;
windAngle = +15-270;
windVelocity = 4;
exitVelocity=7;

stackDiameter=15;

windVelocityDirectionVariation=2;
dayOrNight='day';
surfaceRoughness='rural';
insolation='moderate';
nightOvercast='minority';
stabilityMode='optimistic';

smokeTemp=800;
tolerance=10^-6;

stackDiameter = 15; %simulated stack diameter (m)
atmosphericP = 1; %atmospheric pressure (bar)
atmosphericTemp = 300; %Ambient temperature (K)
a = 1; %ground reflection coefficient, usual conservative value is 1



activatedNodes = zeros(length(rateofSpread(:,1)),length(rateofSpread(1,:)));
activatedNodes(rateofSpread==-9999)=4;
activatedNodes(IgnitionNode(1),IgnitionNode(2))=2;

% 0: Not on fire yet
% 1: On fire
% 2: Transmissive
% 3: Extinguished
% 4: Unaffected

BurningNodes = 1;

residencyTime=(30./rateofSpread).*60; %how long the fire takes to travel through a cell(until it becomes transmissive) seconds.

currentTime=0;
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

notFound=1;
searchingX = 1;
searchingY = 1;

while notFound
    if sum(rateofSpread(:,searchingX)~=-9999)>0
        minimumX=searchingX;
        notFound=0;
    end
    searchingX=searchingX+1;
end

notFound=1;

while notFound
    if sum(rateofSpread(searchingY,:)~=-9999)>0
        minimumY=searchingY;
        notFound=0;
    end
    searchingY=searchingY+1;
end

notFound=1;
searchingX = length(rateofSpread(1,:));
searchingY = length(rateofSpread(:,1));

while notFound
    if sum(rateofSpread(:,searchingX)~=-9999)>0
        maximumX=searchingX;
        notFound=0;
    end
    searchingX=searchingX-1;
end

notFound=1;

while notFound
    if sum(rateofSpread(searchingY,:)~=-9999)>0
        maximumY=searchingY;
        notFound=0;
    end
    searchingY=searchingY-1;
end

activatedNodes = activatedNodes(minimumY:maximumY,minimumX:maximumX);
residencyTime = residencyTime(minimumY:maximumY,minimumX:maximumX);
%%
neighborPatternX = [-1,0,1];
neighborPatternY = [-1,0,1];

liveMap = figure;

rotateByZ = [cosd(windAngle) -sind(windAngle) 0; sind(windAngle) cosd(windAngle) 0; 0 0 1];

startedSmoking=zeros(length(activatedNodes(:,1)),length(activatedNodes(1,:)));

figure('units','normalized','outerposition',[0 0 1 1])
smokeCloud = [0,0,0];
burningCells=[0,0,0];

while BurningNodes>0
    for x=2:length(activatedNodes(1,:))-1
        for y=2:length(activatedNodes(:,1))-1
            ROS=rateofSpread(y+minimumY,x+minimumX);
            switch activatedNodes(y,x)
                case 0
                case 1
                    residencyTime(y,x)=residencyTime(y,x)-timestep;
                    if ~startedSmoking(y,x)
                        emissionMassFlowRate = 0.3*ROS; %Emission mass flow rate (kg/s)
                        steadyStateHeight = (exitVelocity*stackDiameter/windVelocity)*(1.5+2.68*atmosphericP*stackDiameter*(smokeTemp-atmosphericTemp)/smokeTemp); %steady state plume height (meters)
                        term1=@(x) ROS*emissionMassFlowRate/(2*pi()*windVelocity*dispersionCoefficientZ(x)*dispersionCoefficientY(x));
                        smokespacePos = @(x,y) steadyStateHeight + sqrt( -(  log(tolerance./term1(x)) + y.^2./dispersionCoefficientY(x).^2) .* 2 .* dispersionCoefficientZ(x) .^ 2);
                        smokespaceNeg = @(x,y) steadyStateHeight -sqrt( -(  log(tolerance./term1(x)) + y.^2./dispersionCoefficientY(x).^2) .* 2 .* dispersionCoefficientZ(x) .^ 2);
                        subplot(1,2,1)
                        fsurf(smokespacePos,[0 4000 -200 200]);
                        hold on;
                        fsurf(smokespaceNeg,[0 4000 -200 200]);
                        axis equal
                        %smokeCloud=[smokeCloud;dispersionModel(10^-6,ROS,800,verticalSmokeSpeed,windVelocity, maximumPlumeLength, dispersionCoefficientZ, dispersionCoefficientY,lengthwiseResolution)*rotateByZ+[x*cellsize,y*cellsize,0]];
                    end
                    if residencyTime(y,x)<=0
                        activatedNodes(y,x)=2;
                    end
                    startedSmoking(y,x)=1;
                case 2
                    for i=1:3
                        for j=1:3
                            if activatedNodes(y+neighborPatternY(i),x+neighborPatternX(j))==0 && rateofSpread(y+neighborPatternY(i)+minimumY,x+neighborPatternX(j)+minimumX)~=-9999
                                activatedNodes(y+neighborPatternY(i),x+neighborPatternX(j))=1;
                                BurningNodes=BurningNodes+1;
                                burningCells=[burningCells;[x+neighborPatternX(j),y+neighborPatternY(i),0].*cellsize];
                            end 
                        end
                    end
                    residencyTime(y,x)=residencyTime(y,x)-timestep;
                    if residencyTime(y,x)<=-300
                        activatedNodes(y,x)=3;
                    end
                case 3
                    %stopSmokeStack
                    BurningNodes=BurningNodes-1;
                    activatedNodes(y,x)=5;

                    
                    
                    
                    %removeSmokeCloud=dispersionModel(10^-6,ROS,800,verticalSmokeSpeed,windVelocity, maximumPlumeLength, dispersionCoefficientZ, dispersionCoefficientY,lengthwiseResolution)*rotateByZ+[x*cellsize,y*cellsize,0];
                    
                    %[ia,ib]=ismember(smokeCloud,removeSmokeCloud,'rows');
                    %smokeCloud(ia,:)=[];
                    %clear ia ib
                case 4
                case 5
                    
                    
                    
                    
                    %smokeCloud=[smokeCloud;dispersionModel(10^7,rateofSpread(y,x)/3,400,verticalSmokeSpeed/3,windVelocity, maximumPlumeLength, dispersionCoefficientZ, dispersionCoefficientY,lengthwiseResolution)*rotateByZ+[x*cellsize,y*cellsize,0]];
                    
                    activatedNodes(y,x)=6;
            end
        end
    end
    if length(smokeCloud)>1
        smokeCloud(1,:)=[];
    end

%     while length(smokeCloud(:,1))>50000
%         smokeCloud(1:2:length(smokeCloud(:,1)),:)=[];
%     end

    currentTime=currentTime+timestep;
    
    subplot(1,2,1)
    image(activatedNodes,'CDataMapping','scaled');
    axis([0 112 0 152]);
    pause(0.05);
end









%%



