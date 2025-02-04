import os
import pandas as pd
import matplotlib.pyplot as plt

# Define the path to the CSV file based on user
appdata_path = os.path.expandvars(r'%APPDATA%')
full_path = os.path.join(appdata_path, 'Godot', 'app_userdata', '3dInTankPerception', 'm27_log.csv')

# Read the CSV file with comma as the separator, skipping the first row (it's always wrong)
df = pd.read_csv(full_path, sep=',', skiprows=[1])

print("Available columns:")
print(df.columns)

# Extract position data using correct column names
x = df['Position_X']
y = df['Position_Y']
z = df['Position_Z']

# Create a 3D scatter plot
fig = plt.figure(figsize=(10, 8))
ax = fig.add_subplot(111, projection='3d')


scatter = ax.scatter(x, y, z, c=z, cmap='viridis')

ax.set_xlabel('X Position')
ax.set_ylabel('Y Position')
ax.set_zlabel('Z Position')
ax.set_title('3D Position Scatter Plot')


plt.show()

