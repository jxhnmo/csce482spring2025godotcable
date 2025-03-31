using Godot;
using System;

public partial class InputControlNode : Control
{
    [Export] public NodePath CoordinatorPath;
    private Coordinator coordinator;

    public override void _Ready()
    {
        coordinator = GetNode<Coordinator>(CoordinatorPath);

        // Start point
        GetNode<SpinBox>("HBoxContainerStart/StartX").ValueChanged += val => coordinator.SetStartPointX((float)val);
        GetNode<SpinBox>("HBoxContainerStart/StartY").ValueChanged += val => coordinator.SetStartPointY((float)val);

        // End point
        GetNode<SpinBox>("HBoxContainerEnd/EndX").ValueChanged += val => coordinator.SetEndPointX((float)val);
        GetNode<SpinBox>("HBoxContainerEnd/EndY").ValueChanged += val => coordinator.SetEndPointY((float)val);

        // Mass
        GetNode<SpinBox>("HBoxContainerMass/Mass").ValueChanged += val => coordinator.SetMass((float)val);

        // Length
        GetNode<SpinBox>("HBoxContainerLength/SpinBoxLength").ValueChanged += val => coordinator.SetLength((float)val);

        // Segments
        GetNode<SpinBox>("HBoxContainerSegments/SpinBoxSegments").ValueChanged += val => coordinator.SetSegmentCount((int)val);

        // Generate Plots Button
        GetNode<Button>("GeneratePlots").Pressed += () => coordinator.GeneratePlots();

        // Recenter Camera Button
        GetNode<Button>("RecenterCamera").Pressed += () => coordinator.ResetCamera();
    }
}