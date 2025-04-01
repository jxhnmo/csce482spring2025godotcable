using Godot;
using System;

public partial class InputControlNode : Control
{
    [Export] public NodePath CoordinatorPath;
    private Coordinator coordinator;

    public override void _Ready()
    {
        coordinator = GetNode<Coordinator>(CoordinatorPath);
        if (coordinator.IsReady) {
            PrepareInput();
        } else {
            coordinator.Ready += PrepareInput;
        }
    }

    private void PrepareInput() {
        // Start point
        GetNode<SpinBox>("HBoxContainerStart/StartX").ValueChanged += val => coordinator.SetStartPointX((float)val);
        GetNode<SpinBox>("HBoxContainerStart/StartY").ValueChanged += val => coordinator.SetStartPointY((float)val);
        coordinator.SetStartPointX((float)GetNode<SpinBox>("HBoxContainerStart/StartX").Value);
        coordinator.SetStartPointY((float)GetNode<SpinBox>("HBoxContainerStart/StartY").Value);

        // End point
        GetNode<SpinBox>("HBoxContainerEnd/EndX").ValueChanged += val => coordinator.SetEndPointX((float)val);
        GetNode<SpinBox>("HBoxContainerEnd/EndY").ValueChanged += val => coordinator.SetEndPointY((float)val);
        coordinator.SetEndPointX((float)GetNode<SpinBox>("HBoxContainerEnd/EndX").Value);
        coordinator.SetEndPointY((float)GetNode<SpinBox>("HBoxContainerEnd/EndY").Value);

        // Mass
        GetNode<SpinBox>("HBoxContainerMass/Mass").ValueChanged += val => coordinator.SetMass((float)val);
        coordinator.SetMass((float)GetNode<SpinBox>("HBoxContainerMass/Mass").Value);

        // Length
        GetNode<SpinBox>("HBoxContainerLength/SpinBoxLength").ValueChanged += val => coordinator.SetLength((float)val);
        coordinator.SetLength((float)GetNode<SpinBox>("HBoxContainerLength/SpinBoxLength").Value);

        // Segments
        GetNode<SpinBox>("HBoxContainerSegments/SpinBoxSegments").ValueChanged += val => coordinator.SetSegmentCount((int)val);
        coordinator.SetSegmentCount((int)GetNode<SpinBox>("HBoxContainerSegments/SpinBoxSegments").Value);

        // Checkbox Visibility
        GetNode<CheckBox>("HBoxContainerParabola/CheckBoxParabola").Toggled += pressed => coordinator.SetVisible(0, pressed);
        coordinator.SetVisible(0, GetNode<CheckBox>("HBoxContainerParabola/CheckBoxParabola").ButtonPressed);
        GetNode<CheckBox>("HBoxContainerAbs/CheckBoxAbs").Toggled += pressed => coordinator.SetVisible(1, pressed);
        coordinator.SetVisible(1, GetNode<CheckBox>("HBoxContainerAbs/CheckBoxAbs").ButtonPressed);
        GetNode<CheckBox>("HBoxContainerFEM/CheckBoxFEM").Toggled += pressed => coordinator.SetVisible(2, pressed);
        coordinator.SetVisible(2, GetNode<CheckBox>("HBoxContainerFEM/CheckBoxFEM").ButtonPressed);

        // Generate Plots Button
        GetNode<Button>("GeneratePlots").Pressed += () => coordinator.GeneratePlots();

        // Recenter Camera Button
        GetNode<Button>("RecenterCamera").Pressed += () => coordinator.ResetCamera();

        // Generate Plots with initial parameters
        coordinator.GeneratePlots();
    }
}