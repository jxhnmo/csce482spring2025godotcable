using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

public interface CablePlotter
{
	// public void Generate(Vector2 startPoint, Vector2 endPoint, float mass, float length, int segmentCount);
	public void Generate(float nodeMass, Vector2[] initialPoints, float actualLength);
	public void HidePlot();
	public void ShowPlot();
	public String GetPlotName();
	public Color GetColor();
	// public float GetProgress();
	// public Vector2[] GetFinalPoints();
	protected static Action<CablePlotter, Dictionary<string, string>> statisticsCallback;

	public static void SetStatisticsCallback(Action<CablePlotter, Dictionary<string, string>> callback)
	{
		statisticsCallback = callback;
	}

}
