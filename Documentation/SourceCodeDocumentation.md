# Source Code Documentation

This document provides an overview of the key classes and components in the Cable Models codebase.

## Core Components

### CablePlotter Interface
The `CablePlotter` interface defines the contract for different cable plotting implementations:

- `Generate()` - Creates the cable plot using provided node mass and initial points
- `HidePlot()/ShowPlot()` - Controls plot visibility 
- `GetPlotName()` - Returns the name of this plot
- `GetColor()` - Returns the plot's display color
- `GetProgress()` - Returns generation progress (0-1)
- `GetFinalPoints()` - Returns the final calculated points

### Plot Implementations

#### RawPlotter
Basic plotter that directly displays the input points with a specified name and color.

#### FEMLine 
Finite element method implementation that calculates cable shape using physics simulation.

### Coordinator
Central coordinator class that manages plot generation and visibility:

- Maintains list of plotters
- Triggers plot generation
- Controls individual plot visibility
- Interfaces with InitialCurve for starting points
- `ResetCamera()` - Resets the camera to the default position
- `Instance` - Singleton instance of the Coordinator

### InitialCurve
Generates initial cable curve points based on:
- Start/end points
- Mass
- Arc length 
- Number of segments

### Input Controls
The `InputControlNode` class programs and adds the UI controls and user input, connecting to the Coordinator to trigger actions.

- `StatisticsCallback()` - Handles statistics from the Coordinator
- `AddDoubleField(labelText: string, initialValue: double, onChanged: Action<double>)` - Adds a UI field for double input with a label and change handler
- `ShowAlert(title: string, message: string)` - Displays an alert dialog with the specified title and message
- `Instance` - Singleton instance of the InputControlNode

## Scene Structure

### User Controls Scene
Main control panel containing:
- Plot generation controls
- Camera controls  
- Force input controls
- Statistics display
- Save functionality

### External Force Scene
UI component for adding external force vectors:
- Node selection
- X/Y force components
- Remove button

## Class Diagram
See `diagram.puml` for a complete view of class relationships and dependencies.
