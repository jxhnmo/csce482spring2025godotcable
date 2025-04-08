using System;
using System.Runtime.CompilerServices;
using Godot;

public interface CablePlotter
{
	// public void Generate(Vector2 startPoint, Vector2 endPoint, float mass, float length, int segmentCount);
	public void Generate(float nodeMass, Vector2[] initialPoints);
	public void HidePlot();
	public void ShowPlot();
	public String GetPlotName();
	public Color GetColor();
	// public float GetProgress();
	// public Vector2[] GetFinalPoints();
	// Dictionary<string, string> GetStatistics();
}
