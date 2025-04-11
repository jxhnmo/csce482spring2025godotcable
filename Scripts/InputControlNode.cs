using Godot;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;

public partial class InputControlNode : Control
{
	public static InputControlNode Instance { get; private set; } = null;
	[Export] public NodePath CoordinatorPath;
	private Coordinator coordinator;
	private CablePlotter[] plotters;
	private string controlPath = "TabContainer/Controls/MarginContainer/VBoxContainer/";
	private string statsPath = "TabContainer/Statistics/ScrollContainer/MarginContainer/VBoxContainer/";

	public override void _Ready()
	{
		Instance = this;
		coordinator = GetNode<Coordinator>(CoordinatorPath);
		if (coordinator.IsReady) {
			PrepareInput();
		} else {
			coordinator.Ready += PrepareInput;
		}
	}

	private void PrepareInput() {
		
		// Start point
		GetNode<SpinBox>($"{controlPath}HBoxContainerStart/StartX").ValueChanged += val => coordinator.SetStartPointX((float)val);
		GetNode<SpinBox>($"{controlPath}HBoxContainerStart/StartY").ValueChanged += val => coordinator.SetStartPointY((float)val);
		coordinator.SetStartPointX((float)GetNode<SpinBox>($"{controlPath}HBoxContainerStart/StartX").Value);
		coordinator.SetStartPointY((float)GetNode<SpinBox>($"{controlPath}HBoxContainerStart/StartY").Value);

		// End point
		GetNode<SpinBox>($"{controlPath}HBoxContainerEnd/EndX").ValueChanged += val => coordinator.SetEndPointX((float)val);
		GetNode<SpinBox>($"{controlPath}HBoxContainerEnd/EndY").ValueChanged += val => coordinator.SetEndPointY((float)val);
		coordinator.SetEndPointX((float)GetNode<SpinBox>($"{controlPath}HBoxContainerEnd/EndX").Value);
		coordinator.SetEndPointY((float)GetNode<SpinBox>($"{controlPath}HBoxContainerEnd/EndY").Value);

		// Mass
		GetNode<SpinBox>($"{controlPath}HBoxContainerMass/Mass").ValueChanged += val => coordinator.SetMass((float)val);
		coordinator.SetMass((float)GetNode<SpinBox>($"{controlPath}HBoxContainerMass/Mass").Value);

		// Length
		GetNode<SpinBox>($"{controlPath}HBoxContainerLength/SpinBoxLength").ValueChanged += val => coordinator.SetLength((float)val);
		coordinator.SetLength((float)GetNode<SpinBox>($"{controlPath}HBoxContainerLength/SpinBoxLength").Value);

		// Segments
		GetNode<SpinBox>($"{controlPath}HBoxContainerSegments/SpinBoxSegments").ValueChanged += val => coordinator.SetSegmentCount((int)val);
		coordinator.SetSegmentCount((int)GetNode<SpinBox>($"{controlPath}HBoxContainerSegments/SpinBoxSegments").Value);

		// Dynamic copies of visibility checkboxes:
		var checkboxContainer = GetNode<Container>($"{controlPath}VisibilityChecksContainer");
		foreach (Node child in checkboxContainer.GetChildren()) {
			child.QueueFree(); // Remove sample containers from tree.
		}
		plotters = coordinator.GetPlotters();
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

		GetNode<Button>($"{controlPath}GeneratePlots").Pressed += () => {clearStatistics(); coordinator.GeneratePlots();};
		GetNode<Button>($"{controlPath}RecenterCamera").Pressed += coordinator.ResetCamera;
		coordinator.GeneratePlots();
	}

	private void clearStatistics()
	{
		var container = GetNode<VBoxContainer>(statsPath);
		foreach (Node child in container.GetChildren())
			child.QueueFree();
	}

	public void StatisticsCallback(CablePlotter caller, Dictionary<string, string> stats)
	{
		var container = GetNode<VBoxContainer>(statsPath);

		var richText = new RichTextLabel
		{
			FocusMode = FocusModeEnum.None,
			BbcodeEnabled = true,
			ScrollActive = false,
			AutowrapMode = TextServer.AutowrapMode.Word,
			FitContent = true,
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		richText.SetSelectionEnabled(true);
		var style = new StyleBoxEmpty();
		richText.AddThemeStyleboxOverride("focus", style);

		richText.AppendText($"[b]{caller.GetPlotName()}[/b]\n");
		foreach (var pair in stats)
			richText.AppendText($"{pair.Key}: {pair.Value}\n");

		container.AddChild(richText);

		var separator = new HSeparator
		{
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		container.AddChild(separator);
	}


}
