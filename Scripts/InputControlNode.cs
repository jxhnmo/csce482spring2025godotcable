using Godot;
using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Data.SqlTypes;

public partial class InputControlNode : Control
{
	public static InputControlNode Instance { get; private set; } = null;
	private bool isReady;
	private Coordinator coordinator;
	private CablePlotter[] plotters;
	public string SavePath = 
		OperatingSystem.IsWindows()
			? $"C:/Users/{System.Environment.UserName}/Desktop/"
			: $"/Users/{System.Environment.UserName}/Desktop/";
	[Export] public NodePath CoordinatorPath;
	[Export] public NodePath ControlPath;
	[Export] public NodePath ExternalForcesPath;
	[Export] public NodePath AddForcePath;
	[Export] public NodePath ControlDynamicsPath;
	[Export] public NodePath StatsPath;
	[Export] public NodePath GeneratePath;
	[Export] public NodePath RecenterPath;
	[Export] public NodePath SavePathInput;
	[Export] public NodePath SaveButtonPath;


	public InputControlNode() {
		Instance = this;
		isReady = false;
	}

	public override void _Ready()
	{
		allStats = new();
		isReady = true;
		coordinator = GetNode<Coordinator>(CoordinatorPath);
		if (coordinator.IsReady) {
			prepareInput();
		} else {
			coordinator.Ready += prepareInput;
		}
	}


	private void prepareInput() {

		var forceTarget = GetNode<Control>(ExternalForcesPath);
		PackedScene packed = GD.Load<PackedScene>("res://ExternalForce.tscn");
		GetNode<Button>(AddForcePath).Pressed += () => 
		{
			ExternalForce newForce = packed.Instantiate<ExternalForce>();
			forceTarget.AddChild(newForce);
			Coordinator.Instance.RegisterExternalForce(newForce);
		};
		
		// Start point
		GetNode<SpinBox>($"{ControlPath}/HBoxContainerStart/StartX").ValueChanged += val => coordinator.SetStartPointX((float)val);
		GetNode<SpinBox>($"{ControlPath}/HBoxContainerStart/StartY").ValueChanged += val => coordinator.SetStartPointY((float)val);
		coordinator.SetStartPointX((float)GetNode<SpinBox>($"{ControlPath}/HBoxContainerStart/StartX").Value);
		coordinator.SetStartPointY((float)GetNode<SpinBox>($"{ControlPath}/HBoxContainerStart/StartY").Value);

		// End point
		GetNode<SpinBox>($"{ControlPath}/HBoxContainerEnd/EndX").ValueChanged += val => coordinator.SetEndPointX((float)val);
		GetNode<SpinBox>($"{ControlPath}/HBoxContainerEnd/EndY").ValueChanged += val => coordinator.SetEndPointY((float)val);
		coordinator.SetEndPointX((float)GetNode<SpinBox>($"{ControlPath}/HBoxContainerEnd/EndX").Value);
		coordinator.SetEndPointY((float)GetNode<SpinBox>($"{ControlPath}/HBoxContainerEnd/EndY").Value);

		// Mass
		GetNode<SpinBox>($"{ControlPath}/HBoxContainerMass/Mass").ValueChanged += val => coordinator.SetMass((float)val);
		coordinator.SetMass((float)GetNode<SpinBox>($"{ControlPath}/HBoxContainerMass/Mass").Value);

		// Length
		GetNode<SpinBox>($"{ControlPath}/HBoxContainerLength/SpinBoxLength").ValueChanged += val => coordinator.SetLength((float)val);
		coordinator.SetLength((float)GetNode<SpinBox>($"{ControlPath}/HBoxContainerLength/SpinBoxLength").Value);

		// Segments
		GetNode<SpinBox>($"{ControlPath}/HBoxContainerSegments/SpinBoxSegments").ValueChanged += val => coordinator.SetSegmentCount((int)val);
		coordinator.SetSegmentCount((int)GetNode<SpinBox>($"{ControlPath}/HBoxContainerSegments/SpinBoxSegments").Value);

		//output directory
		try {
			var pathInput = GetNode<LineEdit>(SavePathInput);
			pathInput.Text = SavePath; // Set the initial path
			pathInput.TextChanged += OnSavePathChanged; // Hook up the handler
		} catch (Exception e) {
			GD.PrintErr("SavePathInput failed: " + e.Message);
		}
		
		// Save button programming
		GetNode<Button>(SaveButtonPath).Pressed += saveAllStats;


		// Dynamic copies of visibility checkboxes:
		var checkboxContainer = GetNode<Container>($"{ControlPath}/VisibilityChecksContainer");
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

		GetNode<Button>(GeneratePath).Pressed += () => {clearStatistics(); coordinator.GeneratePlots();};
		GetNode<Button>(RecenterPath).Pressed += coordinator.ResetCamera;
		coordinator.GeneratePlots();
	}

	private void clearStatistics()
	{
		allStats.Clear();
		var container = GetNode<VBoxContainer>(StatsPath);
		foreach (Node child in container.GetChildren())
			child.QueueFree();
	}

	private List<(string, Godot.Collections.Dictionary<string, string>)> allStats;

	public void StatisticsCallback(CablePlotter caller, Godot.Collections.Dictionary<string, string> stats)
	{
		if (!isReady) {
			Ready += () => StatisticsCallback(caller, stats);
			return;
		}

		allStats.Add((caller.GetPlotName(), stats));

		var container = GetNode<VBoxContainer>(StatsPath);

		var richText = new RichTextLabel
		{
			FocusMode = FocusModeEnum.None,
			BbcodeEnabled = true,
			ScrollActive = false,
			AutowrapMode = TextServer.AutowrapMode.Off,
			FitContent = true,
			SizeFlagsHorizontal = SizeFlags.Expand,
		};
		richText.SetSelectionEnabled(true);
		var style = new StyleBoxEmpty();
		richText.AddThemeStyleboxOverride("focus", style);

		// Title
		richText.AppendText($"[b]{caller.GetPlotName()}[/b]\n");

		// Begin table with 2 columns
		richText.AppendText("[table=2]");

		foreach (var pair in stats)
		{
			richText.AppendText($"[cell][left]{pair.Key}[/left][/cell]");
			richText.AppendText($"[cell][right]{pair.Value}[/right][/cell]");
		}

		richText.AppendText("[/table]");

		container.AddChild(richText);

		var separator = new HSeparator
		{
			SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
		};
		container.AddChild(separator);
	}

	private void OnSavePathChanged(string text)
	{
		SavePath = text;
	}


	public void AddDoubleField(string labelText, double initialValue, Action<double> onChanged)
	{
		var container = GetNode<VBoxContainer>(ControlDynamicsPath);
		var box = new VBoxContainer();
		var label = new Label
		{
			Text = labelText,
			CustomMinimumSize = new Vector2(100, 0)
		};
		var input = new SpinBox
		{
			MinValue = -1e12,
			MaxValue = 1e12,
			Step = 0.1,
			Value = initialValue,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};

		input.ValueChanged += val => onChanged((double)val);
		onChanged(initialValue);

		box.AddChild(label);
		box.AddChild(input);
		container.AddChild(box);
	}


	public void ShowAlert(string title, string message)
	{
		var alert = new AcceptDialog
		{
			DialogText = message,
			Title = title,
		};

		AddChild(alert);
		alert.PopupCentered(); // Center it on the screen
	}

	private void saveAllStats()
	{
		try
		{
			// Abort if there's nothing to save
			if (allStats == null || allStats.Count == 0)
			{
				ShowAlert("Nothing to Save", "There are no statistics available to export.");
				return;
			}

			// Abort if path is missing or invalid
			if (string.IsNullOrEmpty(SavePath) || !System.IO.Directory.Exists(SavePath))
			{
				ShowAlert("Download Fail", $"Path not found or incorrect: '{SavePath}'. Please select an existing folder.");
				return;
			}

			// Ensure trailing separator
			if (!SavePath.EndsWith("/") && !SavePath.EndsWith("\\"))
			{
				SavePath += Path.DirectorySeparatorChar;
			}

			foreach ((string name, var statsDict) in allStats)
			{
				string title = Regex.Replace(name, @"\s", string.Empty);
				string fullPath = Path.Combine(SavePath, $"{title}.csv");
				saveStatsToCSV(statsDict, fullPath);
			}

			ShowAlert("Download Complete", $"Files saved to:\n{SavePath}");
		}
		catch (Exception ex)
		{
			ShowAlert("Download Error", $"An error occurred while saving stats:\n\n{ex.Message}");
		}
	}


	private void saveStatsToCSV(Godot.Collections.Dictionary<string, string> statsDict, string filePath)
	{
		string directory = Path.GetDirectoryName(filePath);

		// Ensure the directory exists
		if (!Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}

		bool fileExists = File.Exists(filePath);

		using (var writer = new StreamWriter(filePath, append: true))
		{
			if (!fileExists)
			{
				writer.WriteLine("Timestamp," + string.Join(",", statsDict.Keys));
			}

			string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
			string csvLine = timestamp + "," + string.Join(",", statsDict.Values);
			writer.WriteLine(csvLine);
		}
	}
}
