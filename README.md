# Introduction

<!-- What does your tool/codebase do? -->


# Project Environment

<!-- What did you use to develop the tool -->

This code has been run and tested using the following internal and external components

- Tools
    - Examples:
        - Docker version ???
        - Git Hub
        - VS Code
        - Simplecov
        - Jira
        - Others

- Environment
    - Examples:
        - Ubuntu version ???
        - MacOS version ???
        - Docker Engine version ???
        - Docker container version ???

- Language / Runtimes
    - Examples:
        - Ruby version ___
        - Python version ___
        - NodeJS version ___

- Libraries
    - Examples:
        - Rails version ___
        - Numpy version ___
        - React version ___

- External Dependencies
    - Examples:
        - Github Actions for CI/CD
        - Amazon AWS
        - Heroku
        - A custom server running ((this)) tool from ((this)) codebase

# Installation

<!-- How do I download/use the tool -->

### Example:

- Install python > 3.8
- Download the codebase from github
- Unzip the codebase
- cd to the codebase in a terminal
- run `pip install -e .`
- run `demo_tool --version` to ensure the installation worked

# Building & Development

<!-- How can I modify the tool and get it to work? -->

## Installation

- Install the latest version of docker
- Install python > 3.8
- Install ruby > 2.7 but < 3.0

## Environmental Variables/Files

### Example:

During development, the project path needs to be added to `PYTHON_PATH`

For deployment, we have environment variables setup for Authentication.
The tutorial can be found here: https://medium.com/craft-academy/encrypted-credentials-in-ruby-on-rails-9db1f36d8570

You will need to setup your own auth env vars to deploy on your own server.

## Execute Code

### Example:

Run the following code in Powershell if using windows or the terminal using Linux/Mac

`cd project-sprint-3-zlp-interviewer`

`docker run --rm -it --volume "$(pwd):/rails_app" -e DATABASE_USER=test_app -e DATABASE_PASSWORD=test_password -p 3000:3000 dmartinez05/ruby_rails_postgresql:latest`


Install the app

`bundle install && rails webpacker:install && rails db:create && db:migrate`


Run the app
`rails server --binding:0.0.0.0`


The application can be seen using a browser and navigating to http://localhost:3000/

## Tests

### Example:

An RSpec test suite is available and can be ran using:

`rspec spec/`

You can run all the test cases by running. This will run both the unit and integration tests.
`rspec .`

## Deployment

### Example:

1. Start docker and work in the terminal
3. Switch to the deploy branch
4. Login and open the Heroku Dashboard for monitoring the deployment
5. Select "Main" in Review Apps. Choose the deployment branch. 
6. In the terminal, merge the main branch into the deployment branch then push
7. Heroku should automatically deploy once the code is pushed

# Documentation

### Example:

Our product and sprint backlog can be found in Jira, with organization name [NAME] and project name [NAME]

# Our Team

### Example:
- Bob Thicket
- Foo Bar
- Sam Eeee

## References

- https://www.w3schools.com/howto/howto_js_filter_table.asp

# In Tank 3D Perception

This project involves designing and testing a simulation of the Movex M-27 mini dozer for hazardous environments, specifically focusing on nuclear waste tank cleaning. The simulation aims to analyze the traversability of the mini dozer, determining the conditions under which the robot might tip over as it moves within a nuclear waste tank. The analysis utilizes Jolt Physics and Godot for accurate simulations, and terrain data is randomly generated due to the unavailability of LiDAR data. The primary objective is to ensure safe and effective movement of the robot in hazardous environments by understanding its stability under different conditions.

# Project Overview
This simulation focuses on robotic movement within the unique and hazardous environment of nuclear tanks. We aim to evaluate different robot models for their ability to traverse the challenging terrain and obstructions found in these tanks. The current proof of concept utilizes the Movex M27 mini dozer, a compact and robust robotic platform.

## Key Objectives:
- Simulate realistic traversal scenarios based on known conditions of Hanford tanks.
- Perform traversability analysis of the robot in complex and constrained environments.

# Requirements
Before setting up the project, ensure you have the following installed:
- Godot Engine 4.0+ (required for running the simulation)
- <a href="https://godotengine.org/asset-library/asset/1918">Jolt Physics Library</a> (integrated into Godot for this project)

# Set up
To incorporate Jolt into Godot:
- Extract the Jolt files into your project directory
- Start or restart Godot
- Open project settings
- Enable Advanced Settings
- Go to "Physics" then to "3D"
- Change "Physics Engine" to "JoltPhysics3D"
- Restart Godot

## Resources
<a href="https://godotengine.org/download/macos/">Godot for Mac</a><br />
<a href="https://godotengine.org/download/windows/">Godot for Windows</a><br />
<a href="https://docs.godotengine.org/en/stable/getting_started/first_3d_game/index.html">Learn 3D Game Dev in Godot</a><br />
<a href="https://docs.godotengine.org/en/stable/tutorials/3d/introduction_to_3d.html#d-viewport">Working in 3D Space (Camera Movement)</a><br />