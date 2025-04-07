using Godot;
using System;
using System.Collections.Generic;

public partial class ParabolaPlotter : Node2D, CablePlotter
{
	private Line2D line;
	private bool show = false;

	public String GetPlotName() {
		return "Parabola";
	}

	public override void _Ready()
	{
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

	public void Generate(Vector2 startMeters, Vector2 endMeters, float mass, float length, int segments)
	{
		if (line == null)
		{
			GD.Print("Line was null... postponing execution");
			Ready += () => Generate(startMeters, endMeters, mass, length, segments);
			return;
		}
		else
		{
			GD.Print($"startMeters: {startMeters}\nendMeters: {endMeters}\nmass: {mass}\nlength: {length}\nsegments: {segments}");
		}

		line.ClearPoints();
		List<Vector2> points = new();

		Vector2 localEnd = endMeters - startMeters;
		float d = localEnd.X;
		float y1 = localEnd.Y;

		// === Solve parabola coefficients ===
		// y(x) = a*x^2 + b*x + c  where c = 0 (startMeters.Y is 0 in local space)
		// b = (y1 - a*d^2) / d
		// L = ∫₀^d sqrt(1 + (dy/dx)^2) dx

		float ArcLengthForA(float a)
		{
			float b = (y1 - a * d * d) / d;
			float Integrand(float x)
			{
				float dydx = 2 * a * x + b;
				return Mathf.Sqrt(1 + dydx * dydx);
			}

			int steps = 100;
			float sum = 0f;
			float dx = d / steps;
			for (int i = 0; i < steps; i++)
			{
				float x = i * dx;
				sum += Integrand(x) * dx;
			}

			return sum;
		}

		float aMin = -10f, aMax = 10f;
		float a = 0f;
		for (int i = 0; i < 100; i++)
		{
			float mid = (aMin + aMax) / 2f;
			float arc = ArcLengthForA(mid);

			if (Mathf.Abs(arc - length) < 0.0001f)
			{
				a = mid;
				break;
			}

			if (arc > length)
				aMax = mid;
			else
				aMin = mid;

			a = mid;
		}

		float bFinal = (y1 - a * d * d) / d;
		float cFinal = 0f;

		for (int i = 0; i <= segments; i++)
		{
			float t = (float)i / segments;
			float x = t * d;
			float y = a * x * x + bFinal * x + cFinal;

			Vector2 localPoint = new Vector2(x, y);
			Vector2 worldPoint = startMeters + localPoint;

			points.Add(Coordinator.MetersToWorld(worldPoint));
		}

		line.Points = points.ToArray();
	}
}
