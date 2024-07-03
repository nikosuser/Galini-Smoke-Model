import matplotlib.pyplot as plt
import numpy as np

# Path to the CSV file
csv_file_path1 = 'topDownRaster.csv'
csv_file_path2 = 'driverLevelDensity.csv'

# Read the CSV file into a 2D numpy array
output1 = np.loadtxt(csv_file_path1, delimiter=',')
output2 = np.loadtxt(csv_file_path1, delimiter=',')

# Generate x and y coordinates
x = np.arange(output1.shape[1])
y = np.arange(output1.shape[0])

# Create a meshgrid
X, Y = np.meshgrid(x, y)

# Create the contour plot
plt.subplot(1,2,1)
plt.contourf(X, Y, output1)
plt.colorbar()  # Add a color bar to show the color scale

# Reverse the y-axis
plt.gca().invert_yaxis()
# Move x-axis label to the top
plt.tick_params(axis='x', top=True, bottom=False, labeltop=True, labelbottom=False)

# Add labels and title
plt.xlabel('X-axis')
plt.ylabel('Y-axis')
plt.title('TopDownRaster')


# Generate x and y coordinates
x = np.arange(output2.shape[1])
y = np.arange(output2.shape[0])

# Create a meshgrid
X, Y = np.meshgrid(x, y)

# Create the contour plot
plt.subplot(1,2,2)
plt.contourf(X, Y, output2)
plt.colorbar()  # Add a color bar to show the color scale

# Reverse the y-axis
plt.gca().invert_yaxis()
# Move x-axis label to the top
plt.tick_params(axis='x', top=True, bottom=False, labeltop=True, labelbottom=False)

# Add labels and title
plt.xlabel('X-axis')
plt.ylabel('Y-axis')
plt.title('DriverLevelDensity')

plt.suptitle('output')
# Save the plot as a PNG file
plt.savefig('output.png')
