using Godot;
using System;
using System.Collections.Generic;

public partial class AbsPloter : Node2D, CablePloter
{
	private Line2D line;
	private bool show = false;

	public override void _Ready()
	{
		line = new Line2D
		{
			Width = 4,
			DefaultColor = new Color(1.0f, 0.4f, 0.2f),
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

	public void Generate(Vector2 startMeters, Vector2 endMeters, float mass, float length, int segments)
	{
		if (line == null) {
			Ready += () => Generate(startMeters, endMeters, mass, length, segments);
			return;
		}

		line.ClearPoints();
		List<Vector2> points = new();

		Vector2 delta = endMeters - startMeters;
		float totalDistance = delta.Length();

		// If length <= direct distance, draw a straight line
		if (length <= totalDistance)
		{
			for (int i = 0; i <= segments; i++)
			{
				float t = (float)i / segments;
				Vector2 p = startMeters.Lerp(endMeters, t);
				points.Add(Coordinator.MetersToWorld(p));
			}

			line.Points = points.ToArray();
			return;
		}

		float d = delta.X;
		float halfDistance = totalDistance / 2f;

		// Solve for h such that arc length equals 'length'
		float halfLength = length / 2f;
		float h = Mathf.Sqrt(halfLength * halfLength - halfDistance * halfDistance);

		// ABS-style sagging shape
		for (int i = 0; i <= segments; i++)
		{
			float t = (float)i / segments;

			Vector2 p = startMeters.Lerp(endMeters, t);

			// Apply vertical sag
			float arcOffset = -h * (1f - 2f * Math.Abs(t - 0.5f)); // downward triangle
			p += new Vector2(0, arcOffset);

			points.Add(Coordinator.MetersToWorld(p));
		}

		line.Points = points.ToArray();
	}


}
