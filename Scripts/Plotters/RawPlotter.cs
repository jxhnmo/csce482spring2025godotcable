using Godot;
using System;
using System.Collections.Generic;

public partial class RawPlotter : Node2D, CablePlotter
{
	private Line2D line;
	private bool show = false;
	private String plotName = null;
	private Vector2[] meterPoints;

	public String GetPlotName() {
		return plotName;
	}

	public RawPlotter(String plotName) {
		this.plotName = plotName;
	}

	public override void _Ready()
	{
		if (plotName == null) {
			plotName = "Raw";
		}
		line = new Line2D
		{
			Width = 4,
			DefaultColor = new Color(0.2f, 0.8f, 1.0f),
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

	public void Generate(Vector2 startPoint, Vector2 endPoint, float mass, float arcLength, int segmentCount)
	{
		if (line == null) {
			Ready += () => Generate(startPoint, endPoint, mass, arcLength, segmentCount);
			return;
		}
		
		meterPoints = InitialCurve.Make(startPoint, endPoint, mass, arcLength, segmentCount);
		Vector2[] worldPoints = new Vector2[meterPoints.Length];

		for (int i = 0; i < meterPoints.Length; i++) {
			worldPoints[i] = Coordinator.MetersToWorld(meterPoints[i]);
		}

		line.Points = worldPoints;
	}
}
