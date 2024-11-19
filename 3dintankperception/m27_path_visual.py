import matplotlib.pyplot as plt
import pandas as pd
import os

appdata_path = os.path.expandvars(r'%APPDATA%')
full_path = os.path.join(appdata_path, 'Godot', 'app_userdata', '3dInTankPerception', 'm27_log.csv')

data = pd.read_csv('full_path')

x = data['Position_X']
y = data['Position_Y']
z = data['Position_Z']


fig = plt.figure()
ax = fig.add_subplot(111, projection='3d')
ax.scatter(x, y, z)

ax.set_xlabel('X')
ax.set_ylabel('Y')
ax.set_zlabel('Z')
plt.title('M27 Path')


plt.show()
    