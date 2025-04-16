using Godot;
using System;
using System.Collections.Generic;

public partial class RawPlotter : Node2D, CablePlotter
{
	private Line2D line;
	private bool show = false;
	private string plotName;
	private Color lineColor;
	private Vector2[] meterPoints;

	public RawPlotter(string plotName, Color lineColor) {
		this.plotName = plotName;
		this.lineColor = lineColor;
	}

	public Vector2[] GetFinalPoints() {
		return (Vector2[])meterPoints.Clone();
	}

	public override void _Ready()
	{
		line = new Line2D {
			Width = 4,
			DefaultColor = lineColor,
			Antialiased = true
		};

		AddChild(line);
		line.Visible = show;
	}

	public void HidePlot() {
		show = false;
		if (line != null && line.IsInsideTree()) {
			line.Hide();
		}
	}

	public void ShowPlot() {
		show = true;
		if (line != null && line.IsInsideTree()) {
			line.Show();
		}
	}

	public string GetPlotName() {
		return plotName;
	}

	public float GetProgress() {
		return 1f;
	}

	public bool GetHidden() {
		return !show;
	}

	public Color GetColor() {
		return lineColor;
	}


	public void Generate(float nodeMass, Vector2[] meterPoints, float actualLength, List<(int nodeIndex, Vector2 force)> extraForces = null)
	{
		if (line == null) {
			Ready += () => Generate(nodeMass, meterPoints, actualLength, extraForces);
			return;
		}
		
		this.meterPoints = meterPoints;
		Vector2[] worldPoints = new Vector2[meterPoints.Length];

		for (int i = 0; i < meterPoints.Length; i++) {
			worldPoints[i] = Coordinator.MetersToWorld(meterPoints[i]);
		}

		line.Points = worldPoints;
	}
}
