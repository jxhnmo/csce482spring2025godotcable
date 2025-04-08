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

		// Dynamic copies of visibility checkboxes:
		var checkboxContainer = GetNode<Container>("VisibilityChecksContainer");
		foreach (Node child in checkboxContainer.GetChildren()) {
			child.QueueFree(); // Remove sample containers from tree.
		}
		var plotters = coordinator.GetPlotters();
		for (int i = 0; i < plotters.Length; i++)
		{
			int index = i;
			var plotter = plotters[index];

			var hbox = new HBoxContainer();

			var centerCont = new CenterContainer 
			{
				SizeFlagsVertical = Control.SizeFlags.Expand | Control.SizeFlags.Fill
			};

			var colorRect = new ColorRect
			{
				Color = plotter.GetColor(),
				CustomMinimumSize = new Vector2(16, 16),
				SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
				SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin
			};

			var checkbox = new CheckBox
			{
				Text = plotter.GetPlotName(),
				ButtonPressed = true
			};

			checkbox.Toggled += pressed => coordinator.SetVisible(index, pressed);
			coordinator.SetVisible(index, checkbox.ButtonPressed);

			centerCont.AddChild(colorRect);
			hbox.AddChild(centerCont);
			hbox.AddChild(checkbox);
			checkboxContainer.AddChild(hbox);
		}

		// Generate Plots Button
		GetNode<Button>("GeneratePlots").Pressed += () => coordinator.GeneratePlots();

		// Recenter Camera Button
		GetNode<Button>("RecenterCamera").Pressed += () => coordinator.ResetCamera();

		// Generate Plots with initial parameters
		coordinator.GeneratePlots();
	}
}
