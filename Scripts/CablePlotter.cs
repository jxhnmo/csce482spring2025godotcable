using System.Collections.Generic;
using Godot;

public interface CablePlotter
{
	public void Generate(float nodeMass, Vector2[] initialPoints, float actualLength, List<(int nodeIndex, Vector2 force)> extraForces = null);
	public void HidePlot();
	public void ShowPlot();
	public bool GetHidden();
	public string GetPlotName();
	public Color GetColor();
	public float GetProgress();
	public Vector2[] GetFinalPoints();
}
