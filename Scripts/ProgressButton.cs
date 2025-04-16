using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ProgressButton : Button
{
	[Export] public NodePath LoadingPanelPath;
	[Export] public NodePath ProgressBarContainerPath;
	[Export] public float PollRate = 0.2f; // Interval to check progress
	[Export] public float CompleteDelay = 1.0f; // Delay after all bars are full

	private Control loadingPanel;
	private VBoxContainer barContainer;
	private Timer pollTimer;
	private Timer completeDelayTimer;

	private CablePlotter[] plotters;
	private Dictionary<CablePlotter, ProgressBar> bars;

	public override void _Ready()
	{
		loadingPanel = GetNode<Control>(LoadingPanelPath);
		barContainer = GetNode<VBoxContainer>(ProgressBarContainerPath);

		loadingPanel.Visible = false;

		pollTimer = new Timer
		{
			WaitTime = PollRate,
			OneShot = false,
			Autostart = false
		};
		AddChild(pollTimer);
		pollTimer.Timeout += UpdateProgress;

		completeDelayTimer = new Timer
		{
			WaitTime = CompleteDelay,
			OneShot = true,
			Autostart = false
		};
		AddChild(completeDelayTimer);
		completeDelayTimer.Timeout += FinishReset;

		this.Pressed += OnPressed;

		plotters = Coordinator.Instance.GetPlotters();
		bars = new Dictionary<CablePlotter, ProgressBar>();
	}

	private void OnPressed()
	{
		Disabled = true;
		loadingPanel.Visible = true;

		foreach (Node child in barContainer.GetChildren())
			child.QueueFree();

		bars.Clear();

		foreach (var plotter in plotters)
		{
			if (plotter.GetHidden())
				continue; // Skip hidden plotters

			var bar = new ProgressBar
			{
				MinValue = 0,
				MaxValue = 1,
				Value = 0,
				SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.Fill
			};

			var label = new Label
			{
				Text = plotter.GetPlotName(),
				SizeFlagsHorizontal = SizeFlags.Expand
			};

			var container = new HBoxContainer();
			container.AddChild(label);
			container.AddChild(bar);
			barContainer.AddChild(container);

			bars[plotter] = bar;
		}

		if (bars.Count == 0)
		{
			GD.Print("No visible plotters to track.");
			FinishReset();
			return;
		}

		pollTimer.Start();
	}

	private void UpdateProgress()
	{
		bool allDone = true;

		foreach (var (plotter, bar) in bars)
		{
			float progress = Mathf.Clamp(plotter.GetProgress(), 0f, 1f);
			bar.Value = progress;

			if (progress >= 1f)
				bar.Modulate = new Color(0.2f, 0.9f, 0.2f); // Green
			else
				allDone = false;
		}

		if (allDone)
		{
			pollTimer.Stop();
			completeDelayTimer.Start();
		}
	}

	private void FinishReset()
	{
		loadingPanel.Visible = false;
		this.Disabled = false;
	}
}
