# README
## Godot Cable Modelling
2D modelling of cable physics in Godot 4 Game Engine. This project simulates a realistic environment where 2D cables are suspended between two points, interacting with external and internal forces. The aim is to achieve realistic cable dynamics and interactions.

## Table of Contents
- [Requirements](#requirements)
- [External Dependencies](#external-dependencies)
- [Environmental Variables/Files](#environmental-variablesfiles)
- [Installation and Setup](#installation-and-setup)
- [Deployment](#deployment)
- [Usage](#usage)
- [Features](#features)
- [Documentation](#documentation)
- [Credits and Acknowledgments](#credits-and-acknowledgments)
- [License](#license)
- [Third-Party Libraries](#third-party-libraries)
- [Contact Information](#contact-information)

## Requirements
This project has been tested with the following software versions:
- **Operating System**: Windows, MacOS
- **Godot Engine**: 4.0 or later
- **Godot.NET.Sdk**: 4.3.0

## External Dependencies
- **Git**: [Download the latest version](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git)

## Environmental Variables/Files
None were used in this project.

## Installation and Setup

### Method 1: Using Godot 4 .NET for Development
1. Ensure you have dotnet enabled Godot 4 installed on your system. You can download it from the [official Godot website](https://godotengine.org/download).
2. Open Godot and navigate to the project directory.
3. Import the project by selecting the `project.godot` file.
4. Run the project within Godot to start the simulation.

### Method 2: Downloading the Release from GitHub
1. Go to the [GitHub releases page](https://github.com/jxhnmo/csce482spring2025godotcable/releases).
2. Download the appropriate release for your operating system:
   - For Windows: `CableModelsWindows.zip`
   - For Mac: `CableModelsMac.zip`
3. Extract the downloaded zip file to a desired location on your computer.
4. Run the executable file to start the simulation.

## Deployment
Deployment is not required for this project as it can be run directly using the methods described above.

## Usage
To use the project, follow these steps:
1. Launch the application using one of the methods described in the Installation and Setup section.
2. Use the interface to simulate cable dynamics.
3. Adjust parameters to see different cable behaviors.

## Features
- Initial structure generation from start, end, mass, length, segments
- FEM deformation
- Mass-Spring deformation simulation
- External force appliction
- Time analysis
- File output
- Final deformation comparison

## Documentation
- [Godot 4 Documentation](https://docs.godotengine.org/en/stable/)
- [Capstone Summary Video](https://youtu.be/0oB9SjARxZM)
- [LICENSE](LICENSE)

## Credits and Acknowledgments
- [Non-linear Finite Element Analysis of 2D Catenary & Cable Structures](https://www.engineeringskills.com/course/non-linear-finite-element-analysis-of-2d-catenary-and-cable-structures) for FEM deformation solving.
- [GPT-4o](https://chatgpt.com/share/68085649-c824-8009-8f35-fd783b98124a) for information on time-stepped, explicit numerical simulation using the semi-implicit Euler method for a Mass-Spring system.
- [GPT-4o](https://chatgpt.com/share/68085649-c824-8009-8f35-fd783b98124a) for other non-core features such as camera controls and helper matrix functions.

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Third-Party Libraries
Just Godot 4 and dotnet SDKs.

## Contact Information
For questions or support, contact:
- Thomas Holt (prefered): tmholt02@gmail.com 682-319-4595
- John Mo: johnmo@tamu.edu
- Jordan Daryanani: jordandary@tamu.edu
- Jonathan Zhao: jonz64@tamu.edu
- Mustafa Tekin: mt37863@tamu.edu
