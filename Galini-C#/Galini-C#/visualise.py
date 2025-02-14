import matplotlib.pyplot as plt
from matplotlib import animation
import numpy as np
import os.path
import glob
import csv
import re

def read_csv_matrix(filename):
    """
    Reads a CSV file and returns a 2D list (matrix) of floats.
    
    Args:
        filename (str): Path to the CSV file.
        
    Returns:
        list of list of float: 2D matrix representing the CSV data.
    """
    matrix = []
    with open(filename, newline='') as csvfile:
        reader = csv.reader(csvfile, delimiter=',')
        for row in reader:
            # Convert each value to float; remove float conversion if not needed.
            matrix.append([float(value) for value in row])
    return matrix

########## INPUT PARAMETERS #################

rootFolder = "D:/galiniInput/Output/"

fileNames = ["driverLevelDensity.csv",
             "flamingAmount.csv",
             "smolderingAmount.csv",
             "injectionHeight.csv",
             "subgridTime.csv",
             "topDownRaster.csv"]

animationSaveName = rootFolder + "results.mp4"

#%%

debug=False
if os.path.isfile(rootFolder + "0" + fileNames[2]):
    debug=True
    

files = glob.glob(f"{rootFolder}*{fileNames[0]}")
inputs = []

for file in files:
    inputs.append(read_csv_matrix(file))
    
def extract_number(filename):
    # This regex looks for one or more digits after a backslash.
    m = re.search(r'\\(\d+)', filename)
    return int(m.group(1)) if m else 0

files = sorted(files, key=extract_number)

inputsDebug = np.zeros([len(fileNames),len(files),len(inputs[0]),len(inputs[0])])

if debug:
    for i in range(0,len(fileNames)):
        files = glob.glob(f"{rootFolder}*{fileNames[i]}")
        files = sorted(files, key=extract_number)
        for j in range(0,len(files)):
            inputsDebug[i,j,:,:]=read_csv_matrix(files[j])

#%% 
# for timestep in range(0,len(inputsDebug[0])):
#     if debug:
#         fig, ax = plt.subplots(2,3, figsize = [12,8])
#         for i in range(0,len(fileNames)):
#             im = ax[int(i/3),int(i%3)].imshow(inputsDebug[i,timestep,:,:],aspect='equal',
#             cmap='viridis')
#             cbar = plt.colorbar(im, ax=ax[int(i/3),int(i%3)])
#             ax[int(i/3),int(i%3)].set_title(f"{fileNames[i]} Frame {timestep*10}")
#     plt.show()
#%%
# Create the figure and subplots (2 rows, 3 columns)
fig, axs = plt.subplots(2, 3)
fig.set_figheight(10)
fig.set_figwidth(20)

# Flatten the axes array for easier iteration
axs = axs.flatten()

colorbarTitles = ['PM2.5 Concentration (μg/m3)','','','Injection Height ASL (m)','Subgrid Time (s)','PM2.5 Column Concentration (μg/m2)']
colorbarColors = ['viridis','Reds','Greys','gist_earth','YlGn','viridis']

# Create a list to hold the image objects for each subplot
ims = []
# Initialize each subplot with the first timestep
for i in range(len(fileNames)):
    im = axs[i].imshow(inputsDebug[i, 0, :, :],
                       vmin=0, vmax=np.max(inputsDebug[i]),
                       interpolation='none',
                       aspect='equal',
                       cmap=colorbarColors[i])
    axs[i].set_title(f"{fileNames[i]} Time 0 min")
    # Add a colorbar to each subplot
    if i == 0 or i == 3 or i == 4 or i == 5:
        cbar = fig.colorbar(im, ax=axs[i],fraction=0.046, pad=0.04)
        cbar.set_label(colorbarTitles[i], rotation=270,labelpad=15)
    ims.append(im)

def update(frame):
    """Update function for animation."""
    for i in range(len(fileNames)):
        # Update the image data for subplot i with the current timestep 'frame'
        ims[i].set_array(inputsDebug[i, frame, :, :])
        axs[i].set_title(f"{fileNames[i]} Time {frame*30} min")
    return ims

# Create the animation
anim = animation.FuncAnimation(
    fig,
    update,
    frames=len(inputsDebug[0]),
    interval=300,  # time between frames in milliseconds
    blit=False
)

writervideo = animation.FFMpegWriter(fps=30) 
anim.save(animationSaveName, writer=writervideo)
    