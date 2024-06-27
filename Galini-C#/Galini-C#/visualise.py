import matplotlib.pyplot as plt
import numpy as np

# Path to the CSV file
csv_file_path = 'output.csv'

# Read the CSV file into a 2D numpy array
output = np.loadtxt(csv_file_path, delimiter=',')

# Generate x and y coordinates
x = np.arange(output.shape[1])
y = np.arange(output.shape[0])

# Create a meshgrid
X, Y = np.meshgrid(x, y)

# Create the contour plot
plt.contourf(X, Y, output)
plt.colorbar()  # Add a color bar to show the color scale

# Reverse the y-axis
plt.gca().invert_yaxis()
# Move x-axis label to the top
plt.tick_params(axis='x', top=True, bottom=False, labeltop=True, labelbottom=False)

# Add labels and title
plt.xlabel('X-axis')
plt.ylabel('Y-axis')
plt.title('Contour Plot of output')

# Save the plot as a PNG file
plt.savefig('output.png')
