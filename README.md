# In Tank 3D Perception

This project involves designing and testing a simulation of the Movex M-27 mini dozer for hazardous environments, specifically focusing on nuclear waste tank cleaning. The simulation aims to analyze the traversability of the mini dozer, determining the conditions under which the robot might tip over as it moves within a nuclear waste tank. The analysis utilizes Jolt Physics and Godot for accurate simulations, and terrain data is randomly generated due to the unavailability of LiDAR data. The primary objective is to ensure safe and effective movement of the robot in hazardous environments by understanding its stability under different conditions.

# Project Overview
This simulation focuses on robotic movement within the unique and hazardous environment of nuclear tanks. We aim to evaluate different robot models for their ability to traverse the challenging terrain and obstructions found in these tanks. The current proof of concept utilizes the Movex M27 mini dozer, a compact and robust robotic platform.

This code has been run and tested using the following internal and external components

- Tools
    - Godot 4.3
    - Jolt Physics Engine
    - Blender
    - Jira

- Environment
    - MacOS Sonoma 14.5
    - TODO Victor Env
    - TODO Josh Env
    - MacOS Sequoia 15.1

- Language / Runtimes
    - Python 3.12
    - GDScript

- Libraries
    - Jolt Physics Library
    - MatPlotLib
    - pandas
    - os

- External Dependencies
    - None

# Installation

<!-- How do I download/use the tool -->

### Example:

- Install python > 3.12
- Install Godot 4.3
    - <a href="https://godotengine.org/download/macos/">Godot for Mac</a><br />
    - <a href="https://godotengine.org/download/windows/">Godot for Windows</a><br />
- Install Jolt
    - Download <a href="https://godotengine.org/asset-library/asset/1918">Jolt Physics Library</a>
    - Extract the Jolt files into your project directory
    - Start or restart Godot
    - Open project settings
    - Enable Advanced Settings
    - Go to "Physics" then to "3D"
    - Change "Physics Engine" to "JoltPhysics3D"
    - Restart Godot
- Install Blender (Optional, only necessary if performing point cloud conversions)
- Download codebase
- Unzip the codebase
- Open Godot 4.3
- Navigate to codebase directory through Godot 4.3
- Open project

## Execute Code

Once the project is open in Godot, simply press the play button in the top right hand corner

# Documentation

A robust description of our project can be found in the Final Report

# Our Team

### Example:
- Jeffrey Cheung
- Yuki Janvier
- Victor Pham
- Joshua Roberts

## References

- TODO

## Resources
<a href="https://godotengine.org/download/macos/">Godot for Mac</a><br />
<a href="https://godotengine.org/download/windows/">Godot for Windows</a><br />
<a href="https://docs.godotengine.org/en/stable/getting_started/first_3d_game/index.html">Learn 3D Game Dev in Godot</a><br />
<a href="https://docs.godotengine.org/en/stable/tutorials/3d/introduction_to_3d.html#d-viewport">Working in 3D Space (Camera Movement)</a><br />
