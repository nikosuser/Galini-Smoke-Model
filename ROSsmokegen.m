clear all;
close all;

%y,x

%CURRENT PROBLEM: the smoke data is saved in a 2D array, which means it
%cannot take negative y values. For calculating the smoke cloud itself its
%fine because its symmetrical. Question is how to do it in the overall
%cloud in the loop below. 

%surfaceRoughness = 'urban'; %urban / rural
%inversionHeight = 600; %m above ground where inversion layer occurs.
%insolation = 'moderate'; % strong / moderate / slight sun irradiance, inverse to cloud coverage.
%nightOvercast = 'majority'; % majority / minority : more or less than 50% nighttime overcast (one hour before and after sunset and sunrise)
%dayOrNight = 'day'; % day / night Day or Night Time
%stabilityMode = 'pessimistic'; %optimistic / pessimistic wrt choosing between two mismatching atmospheric stability calculations. Optimistic will choose unstable conditions


%% FARSITE FILE LOCATION

simulationPath = "D:\CyprusGIS\Arakapas\25m/";

rateofSpread = readmatrix(strcat(simulationPath, 'rateofspread.asc'),'FileType','text','NumHeaderLines',6);
fuels = readmatrix(strcat(simulationPath, 'ArakapasFuels[25].asc'),'FileType','text','NumHeaderLines',6);
intensity = readmatrix(strcat(simulationPath, 'reactionintensity.asc'),'FileType','text','NumHeaderLines',6);
fuelMoisture = readmatrix(strcat(simulationPath, 'fuelMoistures.fms'),'FileType','text');
hourlyWeather = readmatrix(strcat(simulationPath, 'arakapasHourlyWeather.wxs'),'FileType','text','NumHeaderLines',4);

[ROScontent,geodata] = readgeoraster(strcat(simulationPath, 'rateofspread.asc'));

ignitionTime = [2021,7,2,1300]; %Year,month,day,time
IgnitionNode = [433,479];
cellsize = 25;

fprintf("FARSITE Input Parameters Set!\n")

%% VALUES THAT NEED MORE STUDY / MAGIC NUMBERS!

     %maximumPlumeLength = 6000; %Maximum simulation length in line with plume evolution (m)
verticalSmokeSpeedFlaming = 3;
verticalSmokeSpeedSmoldering = 1;
smokeTempFlaming = 120;
smokeTempSmoldering = 60;

minimumParticleDensity = 10^-4;

fprintf("Magic Numbers Set!\n")

%% STABILITY VALUES - WEATHER INPUTS

windVelocityDirectionVariation=2;
dayOrNight='day';
surfaceRoughness='rural';
insolation='moderate';
nightOvercast='minority';
stabilityMode='optimistic';

windAngle = -30;
windVelocity = 1;

fprintf("Weather Stability Parameters Set!\n")

%% VALUES FROM THE SIMULATION
fid_ROS = fopen(strcat(simulationPath, 'rateofspread.asc'));

tline=fgetl(fid_ROS);
values_str = strsplit(tline, ' ');
ncols=values_str(end);
tline=fgetl(fid_ROS);
values_str = strsplit(tline, ' ');
nrows=values_str(end);

clear tline values_str

timestep = 1;               %minute
dispersionModelSteps = cellsize;

fprintf("Raster metadata Set!\n")

%% FOFEM PREPARATION INCLUDING GEOTIFF CONVERSIONS

SBtoFCCSarray = [0,0;
    91,0;
    92,0;
    93,1244;
    98,0;
    99,0;
    100,49;
    101,519;
    102,66;
    103,66;
    104,131;
    105,131;
    106,66;
    107,318;
    108,175;
    120,401;
    121,56;
    122,308;
    123,560112;
    124,445;
    140,49;
    141,69;
    142,52;
    143,36;
    144,69;
    145,210;
    146,470;
    147,154;
    148,1470313;
    149,154;
    160,10;
    161,224;
    162,156;
    163,156;
    164,59;
    165,2;
    180,49;
    181,154;
    182,283;
    183,110;
    184,305;
    185,364;
    186,154;
    187,228;
    188,90;
    189,467;
    200,1090412;
    201,48;
    202,1100422;
    203,4550432
    -9999,-9999];

SBtoFCCS = dictionary(SBtoFCCSarray(:,1),SBtoFCCSarray(:,2));

fuelsFOFEM = SBtoFCCS(fuels);

geotiffwrite(strcat(simulationPath, 'FCCS_GALINI.tif'),fuelsFOFEM,geodata, "CoordRefSysCode","EPSG:26986")

fprintf("FOFEM FCCS Fuel Map Saved!\n")

fofemInputFileLoc = "C:\Users\nikos\Downloads\FB\FB\TestSpatialFOFEM\SampleData\FOFEM_GALINI.txt";
if isfile(fofemInputFileLoc)
    delete(fofemInputFileLoc)
end

writelines("FCCS_Layer_File: " + strcat(simulationPath, 'FCCS_GALINI.tif'),fofemInputFileLoc,WriteMode="append")
writelines("FCCS_Layer_Number: 1 ",fofemInputFileLoc,WriteMode="append")
writelines("FOFEM_Percent_Foliage_Branch_Consumed: 75.0 ",fofemInputFileLoc,WriteMode="append")
writelines("FOFEM_Region: I ",fofemInputFileLoc,WriteMode="append")
writelines("FOFEM_Season: Summer",fofemInputFileLoc,WriteMode="append")
writelines("FOFEM_10_Hour_FM: "+num2str(fuelMoisture(1,3)),fofemInputFileLoc,WriteMode="append")
writelines("FOFEM_1000_Hour_FM: "+num2str(fuelMoisture(1,7)),fofemInputFileLoc,WriteMode="append")
writelines("FOFEM_Duff_FM: "+num2str(fuelMoisture(1,5)),fofemInputFileLoc,WriteMode="append")
writelines("FOFEM_FLAMING_PM25: ",fofemInputFileLoc,WriteMode="append")
writelines("FOFEM_FLAMING_PM10: ",fofemInputFileLoc,WriteMode="append")
writelines("FOFEM_SMOLDERING_PM25: ",fofemInputFileLoc,WriteMode="append")
writelines("FOFEM_SMOLDERING_PM10: ",fofemInputFileLoc,WriteMode="append")

fprintf("FOFEM Input File Saved, Starting FOFEM!\n")

commandFOFEM = "C:\Users\nikos\Downloads\FB\FB\bin\TestSpatialFOFEM C:\Users\nikos\Downloads\FB\FB\TestSpatialFOFEM\SampleData\FOFEM_GALINI.txt C:\Users\nikos\Downloads\FB\FB\TestSpatialFOFEM\SampleData\OutputGALINI\";
system(commandFOFEM);

fprintf("FOFEM Complete\n")

%% FOFEM FOR EMISSIONS -- HARDCODED VALUES FOR NOW

% fofem values are in lb/acre

fofemOutput = "C:\Users\nikos\Downloads\FB\FB\TestSpatialFOFEM\SampleData\OutputGALINI\";
[flamingPM10,geodata] = readgeoraster(strcat(fofemOutput, '_Flaming PM10.tif'));
[flamingPM25,geodata] = readgeoraster(strcat(fofemOutput, '_Flaming PM25.tif'));
[smolderingPM10,geodata] = readgeoraster(strcat(fofemOutput, '_Smoldering PM10.tif'));
[smolderingPM25,geodata] = readgeoraster(strcat(fofemOutput, '_Smoldering PM25.tif'));

flamingPM10=flamingPM10*0.112085*(cellsize.^2); %Convert from lb/acre to total emitted grams.
flamingPM25=flamingPM25*0.112085*(cellsize.^2);
smolderingPM10=smolderingPM10*0.112085*(cellsize.^2);
smolderingPM25=smolderingPM25*0.112085*(cellsize.^2);

flamingPM10(fuels==-9999)=-9999;
flamingPM25(fuels==-9999)=-9999;
smolderingPM10(fuels==-9999)=-9999;
smolderingPM25(fuels==-9999)=-9999;

allPresentFuels = unique(fuels)';

flamingTime = ones(size(smolderingPM25))*10;            %Minutes
smoulderingTime = ones(size(smolderingPM25))*60;

totalFlamingEmissions = flamingPM10 + flamingPM25;
totalSmoulderingEmissions = smolderingPM10 + smolderingPM25;

massFlowRateFlaming = totalFlamingEmissions./flamingTime;
massFlowRateSmoldering = totalSmoulderingEmissions./smoulderingTime;

fprintf("FOFEM Mass Flow Rate Calculations Complete!\n")

%Center of smokeDomain is user Defined, the fire will be placed with the
%center being in the top right corner?

%% RASTER PREPARATION - CROP UNBURNED AREAS

% 0: Not on fire yet
% 1: On fire
% 2: Transmissive
% 3: Extinguished
% 4: Unaffected

notFound = 1;
searchingX = 1;
searchingY = 1;

BurningNodes = 1;

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

activatedNodes = zeros(length(rateofSpread(:,1)), length(rateofSpread(1,:)));
activatedNodes(rateofSpread == -9999) = 5;
activatedNodes(IgnitionNode(1), IgnitionNode(2)) = 2;

rateofSpreadCut = rateofSpread(minimumY:maximumY,minimumX:maximumX);
fuels = fuels(minimumY:maximumY,minimumX:maximumX);
activatedNodes = activatedNodes(minimumY:maximumY,minimumX:maximumX);
massFlowRateFlaming = massFlowRateFlaming(minimumY:maximumY,minimumX:maximumX);
massFlowRateSmoldering = massFlowRateSmoldering(minimumY:maximumY,minimumX:maximumX);

emissionsModelTrack = zeros(length(rateofSpreadCut(:,1)), length(rateofSpreadCut(1,:)));
emissionsModelTrack(rateofSpreadCut == -9999) = 4;
isAlight = zeros(length(rateofSpreadCut(:,1)), length(rateofSpreadCut(1,:)));
elapsedTime = zeros(length(rateofSpreadCut(:,1)), length(rateofSpreadCut(1,:)));

clear maximumX maximumY minimumX minimumY notFound searchingX searchingY IgnitionNode

fprintf("Raster Cropping Complete!\n")

%% ATMOSPHERIC STABILITY CALCULATIONS

[P,dispersionCoefficientY,dispersionCoefficientZ] = getDispersionCoefficients(dayOrNight,surfaceRoughness,insolation,nightOvercast,stabilityMode,windVelocityDirectionVariation,windVelocity);
   
fprintf("Dispersion Coefficients Set!\n")

%% SIM PREPARATION
neighborPatternX = [-1,0,1];
neighborPatternY = [-1,0,1];

liveMap = figure;

rotateByZ = [cosd(90-windAngle) -sind(90-windAngle) ; sind(90-windAngle) cosd(90-windAngle) ];

burningCells = [0,0];

residencyTime=cellsize./rateofSpreadCut;

TopDownRaster(10*inf_lin([length(activatedNodes(1, :)),length(activatedNodes(:, 1))])) = 0;
DriverLevelDensity(10*inf_lin([length(activatedNodes(1, :)),length(activatedNodes(:, 1))])) = 0;

fprintf("Output Rasters Instantiated, Main Loop Starting!\n")

%% MAIN LOOP
% 0: Not on fire yet
% 1: On fire
% 2: Transmissive
% 3: Extinguished
% 4: Unaffected

pointsForPlotting=[0,0];
totaltime=0;

while BurningNodes>0
    totaltime=totaltime+1;
    for x = 2:length(activatedNodes(1, :)) - 1
        for y = 2:length(activatedNodes(:, 1)) - 1
            
            emissionsIndex = find(allPresentFuels==fuels(y,x));
            elapsedTime(y,x)=elapsedTime(y,x)+timestep*isAlight(y,x);

            %% Wildfire Tracking Part

            switch activatedNodes(y, x)
                case 0
                    %nonfuel
                case 1
                    isAlight(y,x)=1;
                    if emissionsModelTrack(y, x) == 0
                        pointsForPlotting = [pointsForPlotting;[x,y]];
                        emissionsModelTrack(y, x) = 1;
                    end

                    if elapsedTime(y,x) >= residencyTime(y, x)
                        activatedNodes(y, x) = 2;
                    end
                    
                    if elapsedTime(y,x)>=smoulderingTime(y,x)
                        emissionsModelTrack(y, x) = 3;
                    elseif elapsedTime(y,x)>=flamingTime(y,x)
                        emissionsModelTrack(y, x) = 2;
                    end
                case 2
                    for i=1:3
                        for j=1:3
                            if activatedNodes(y+neighborPatternY(i),x+neighborPatternX(j))==0 && rateofSpreadCut(y+neighborPatternY(i),x+neighborPatternX(j))~=-9999
                                activatedNodes(y+neighborPatternY(i),x+neighborPatternX(j))=1;
                                BurningNodes=BurningNodes+1;
                                burningCells=[burningCells;[x+neighborPatternX(j),y+neighborPatternY(i)].*cellsize];
                            end 
                        end
                    end
                    activatedNodes(y, x) = 3;
                    
                    if elapsedTime(y,x)>=smoulderingTime(y,x)
                        emissionsModelTrack(y, x) = 3;
                    elseif elapsedTime(y,x)>=flamingTime(y,x)
                        emissionsModelTrack(y, x) = 2;
                    end
                case 3
                    if elapsedTime(y,x)>=smoulderingTime(y,x)
                        emissionsModelTrack(y, x) = 3;
                        activatedNodes(y, x)=4;
                    elseif elapsedTime(y,x)>=flamingTime(y,x)
                        emissionsModelTrack(y, x) = 2;
                        
                    end
                case 4
                    isAlight(y,x)=0;
            end

%% Smoke model part

            switch emissionsModelTrack(y, x)
                case 0
                case 1
                    [currentTopDownRaster,currentDriverLevelDensity]=dispersionModel(P,minimumParticleDensity,smokeTempFlaming,verticalSmokeSpeedFlaming,windVelocity, dispersionCoefficientZ, dispersionCoefficientY,dispersionModelSteps,massFlowRateFlaming(y,x));
                    
                    for smokex=1:length(currentTopDownRaster(:,1))
                        for smokey=1:length(currentTopDownRaster(1,:))
                            if currentTopDownRaster(smokex,smokey) > minimumParticleDensity                                 
                                modifiedDims = inf_lin((ceil([smokex,smokey]*rotateByZ+[x,y]))');
                                TopDownRaster(modifiedDims) = TopDownRaster(modifiedDims) + currentTopDownRaster(smokex,smokey);%addToSmokeCloud(TopDownRaster, [modifiedDims(2),modifiedDims(1),currentTopDownRaster(smokex,smokey)]);
                                DriverLevelDensity(modifiedDims) = DriverLevelDensity(modifiedDims) + currentDriverLevelDensity(smokex,smokey);
                                modifiedDims = ceil(modifiedDims - 2 .* [0,smokey] * rotateByZ);
                                TopDownRaster(modifiedDims) = TopDownRaster(modifiedDims) + currentTopDownRaster(smokex,smokey); %TopDownRaster = addToSmokeCloud(TopDownRaster, [modifiedDims(2),modifiedDims(1),currentTopDownRaster(smokex,smokey)]);
                                DriverLevelDensity(modifiedDims) = DriverLevelDensity(modifiedDims) + currentDriverLevelDensity(smokex,smokey);
                            end
%                             if currentDriverLevelDensity(smokex,smokey) > minimumParticleDensity
%                                 modifiedDims = ceil([smokex,smokey]*rotateByZ+[x,y]);
%                                 DriverLevelDensity = addToSmokeCloud(DriverLevelDensity, [modifiedDims(2),modifiedDims(1),currentDriverLevelDensity(smokex,smokey)]);
%                                 modifiedDims = ceil(modifiedDims - 2 .* [0,smokey] * rotateByZ);
%                                 DriverLevelDensity = addToSmokeCloud(DriverLevelDensity, [modifiedDims(2),modifiedDims(1),currentDriverLevelDensity(smokex,smokey)]);
%                             end
                        end
                    end
                case 2
                    clear currentTopDownRaster currentDriverLevelDensity;

                    [currentTopDownRaster,currentDriverLevelDensity]=dispersionModel(P,minimumParticleDensity,smokeTempSmoldering,verticalSmokeSpeedSmoldering,windVelocity, dispersionCoefficientZ, dispersionCoefficientY,dispersionModelSteps,massFlowRateSmoldering(y,x));
                    for smokex=1:length(currentTopDownRaster(:,1))
                        for smokey=1:length(currentTopDownRaster(1,:))
                            if currentTopDownRaster(smokex,smokey) > minimumParticleDensity
                                modifiedDims = inf_lin(ceil([smokex,smokey]*rotateByZ+[x,y]));
                                TopDownRaster(modifiedDims) = TopDownRaster(modifiedDims) + currentTopDownRaster(smokex,smokey);%addToSmokeCloud(TopDownRaster, [modifiedDims(2),modifiedDims(1),currentTopDownRaster(smokex,smokey)]);
                                DriverLevelDensity(modifiedDims) = DriverLevelDensity(modifiedDims) + currentDriverLevelDensity(smokex,smokey);
                                modifiedDims = ceil(modifiedDims - 2 .* [0,smokey] * rotateByZ);
                                TopDownRaster(modifiedDims) = TopDownRaster(modifiedDims) + currentTopDownRaster(smokex,smokey); %TopDownRaster = addToSmokeCloud(TopDownRaster, [modifiedDims(2),modifiedDims(1),currentTopDownRaster(smokex,smokey)]);
                                DriverLevelDensity(modifiedDims) = DriverLevelDensity(modifiedDims) + currentDriverLevelDensity(smokex,smokey);
                            end
%                             if currentTopDownRaster(smokex,smokey) > minimumParticleDensity
%                                 modifiedDims = ceil([smokex,smokey]*rotateByZ+[x,y]);
%                                 DriverLevelDensity = addToSmokeCloud(DriverLevelDensity, [modifiedDims(2),modifiedDims(1),currentDriverLevelDensity(smokex,smokey)]);
%                                 modifiedDims = ceil(modifiedDims - 2 .* [0,smokey] * rotateByZ);
%                                 DriverLevelDensity = addToSmokeCloud(DriverLevelDensity, [modifiedDims(2),modifiedDims(1),currentDriverLevelDensity(smokex,smokey)]);
%                             end
                        end
                    end

                    [C,pointIndex,ib] = intersect(pointsForPlotting,[x,y],'rows');
                    pointsForPlotting(pointIndex,:) = [];
                case 3

                case 4
            end
        end
    end
    D = 1;
    E=1;
    for x = 0:length(activatedNodes(1, :)) - 1
        for y = 0:length(activatedNodes(:, 1)) - 1
            if TopDownRaster(inf_lin([x,y]))>minimumParticleDensity
                displayTopDownRaster(D,:)=[x,y,TopDownRaster(inf_lin([x,y]))];
                D=D+1;
            end
            if DriverLevelDensity(inf_lin([x,y]))>minimumParticleDensity
                displayDriverLevelRaster(E,:)=[x,y,DriverLevelDensity(inf_lin([x,y]))];
                E=E+1;
            end
        end
    end

    hold off
    
    tiledlayout(1,2)
    totaltime
    tile1 = nexttile(1);

    im = imagesc(emissionsModelTrack,'CDataMapping','scaled');
    im.AlphaData = 0.5;
    hold on;
    scatter(displayTopDownRaster(:,1),displayTopDownRaster(:,2),12,displayTopDownRaster(:,3),'filled');
    %scatter(pointsForPlotting(:,1),pointsForPlotting(:,2),'.','red');
    hold off;
    colormap(tile1,hot);

    tile2 = nexttile(2);
    jm = imagesc(emissionsModelTrack);%,'CDataMapping','scaled');
    jm.AlphaData = 1;
    hold on;
    if exist("displayDriverLevelRaster")==1
        s = scatter(displayDriverLevelRaster(:,1),displayDriverLevelRaster(:,2),12,displayDriverLevelRaster(:,3),'filled');
        s.AlphaData=displayDriverLevelRaster(:,3);
        s.MarkerFaceAlpha = 'flat';
    end
    colormap(tile2,"lines");
    %colorbar('Ticks',0:4,'TickLabels',{'Fuel','Flaming','Smouldering','Extinguished','Nonfuel'});

    pause(0.01)

    clear TopDownRaster DriverLevelDensity

    TopDownRaster(10*inf_lin([length(activatedNodes(1, :)),length(activatedNodes(:, 1))])) = 0;
    DriverLevelDensity(10*inf_lin([length(activatedNodes(1, :)),length(activatedNodes(:, 1))])) = 0;

end

%%

function [Loc] = inf_lin(coords)

    x=coords(1);
    y=coords(2);

    if abs(x)>abs(y)
        m=abs(x);
    else
        m=abs(y);
    end
    Loc=4*m*(m-1)+1;
    
    if y==m
        if x>0
            X=x;
        else
            X=-x;
        end
    elseif y==-m
        X=3*m+abs(x-m);
    elseif x==m
        X=m+abs(y-m);
    elseif x==-m
        X=5*m+abs(m+y);
    end
    
    Loc=Loc+X;

end

function pointCloud = addToSmokeCloud(currentCloud,currentPoint)
    
    [~,ia,~]=intersect(currentCloud(:,1:2),currentPoint(1:2),'rows');
    if isempty(ia)
        pointCloud = [currentCloud;currentPoint];
    else
        pointCloud = currentCloud;
        pointCloud(ia,3)=currentCloud(ia,3)+currentPoint(3);
    end

end



