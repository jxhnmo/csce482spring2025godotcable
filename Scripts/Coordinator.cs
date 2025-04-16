using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public partial class Coordinator : Node
{
	public static Coordinator Instance { get; private set;}
	public bool IsReady { get; private set; } = false;
	[Export] public GridlineController GridController;
	[Export] public Node2D WorldRoot;
	private Vector2 startPoint;
	private Vector2 endPoint;
	private float massPerMeter;
	private float length;
	private int segmentCount;

	public Coordinator() {
		Instance = this;
		externalForces = new List<ExternalForce>();
	}

	private List<CablePlotter> plotters = new List<CablePlotter>();

	public void SetStartPointX(float value) => GD.Print(startPoint.X = value);
	public void SetStartPointY(float value) => startPoint.Y = value;
	public void SetEndPointX(float value) => endPoint.X = value;
	public void SetEndPointY(float value) => endPoint.Y = value;

	public void SetStartPoint(Vector2 value) => startPoint = value;
	public void SetEndPoint(Vector2 value) => endPoint = value;

	public void SetMass(float value) => massPerMeter = value;
	public void SetLength(float value) => length = value;
	public void SetSegmentCount(int value) {
		segmentCount = value;
		foreach (var force in externalForces) {
			force.SetMaxIndex(segmentCount - 1);
		}
	}

	private List<ExternalForce> externalForces;

	public void RegisterExternalForce(ExternalForce force) {
		externalForces.Add(force);
		force.SetRemoveAction(() => RemoveExternalForce(force));
		force.SetMaxIndex(segmentCount - 1);
	}

	public void RemoveExternalForce(ExternalForce force) {
		externalForces.Remove(force);
		force.QueueFree();
	}

	private List<(int nodeIndex, Vector2 force)> createExtraForcesList() {
		return  externalForces
			.Select(f => (f.GetNodeIndex(), f.GetForce()))
			.ToList();
	}


public void GeneratePlots()
{
	GD.Print("GeneratePlots()...");
	var mass = massPerMeter * length;
	Vector2[] initalPoints = InitialCurve.Make(startPoint, endPoint, mass, length, segmentCount);
	float nodeMass = mass / segmentCount;
	var addedForces = createExtraForcesList();

	foreach (CablePlotter plotter in plotters)
	{
		plotter.Generate(nodeMass, initalPoints, length, addedForces);
	}

	if (plotters.Count < 2)
	{
		GD.Print("Not enough plotters to compare.");
		return;
	}

	ThreadPool.QueueUserWorkItem(_ =>
	{
		try
		{
			// Wait for all plotters to finish
			bool allDone;
			do
			{
				allDone = plotters.All(p => p.GetProgress() >= 1f);
				if (!allDone)
					Thread.Sleep(100);
			} while (!allDone);

			var statsDict = new Godot.Collections.Dictionary<string, string>();

			for (int i = 0; i < plotters.Count; i++)
			{
				for (int j = i + 1; j < plotters.Count; j++)
				{
					string nameA = plotters[i].GetPlotName();
					string nameB = plotters[j].GetPlotName();

					Vector2[] resultA = plotters[i].GetFinalPoints();
					Vector2[] resultB = plotters[j].GetFinalPoints();

					if (resultA.Length != resultB.Length)
					{
						GD.PrintErr($"Mismatch in point count between '{nameA}' and '{nameB}'");
						continue;
					}

					double mse = 0.0;
					for (int k = 0; k < resultA.Length; k++)
					{
						float dx = resultA[k].X - resultB[k].X;
						float dy = resultA[k].Y - resultB[k].Y;
						mse += dx * dx + dy * dy;
					}
					mse /= resultA.Length;

					string key = $"MSE: {nameA} vs {nameB} (m^2)";
					string value = $"{mse:F6}";

					statsDict[key] = value;
					GD.Print($"{key} = {value}");
				}
			}

			CallDeferred(nameof(postStatistics), statsDict);

			// You can send this statsDict elsewhere if needed:
			// InputControlNode.Instance.StatisticsCallback(this, statsDict);

		}
		catch (Exception ex)
		{
			GD.PrintErr($"Comparison error: {ex.Message}");
		}
	});
}

	protected void postStatistics(Godot.Collections.Dictionary<string, string> statsDict) {
		var dummy = new RawPlotter("Plot Comparisons", new Color(0,0,0));
		InputControlNode.Instance.StatisticsCallback(dummy, statsDict);
	}


	public void AddPlotter(CablePlotter plotter)
	{
		if (plotter is Node node)
		{
			WorldRoot.AddChild(node);
		}

		if (!plotters.Contains(plotter))
		{
			plotters.Add(plotter);
		}
	}

	public void ClearPlotters()
	{
		plotters.Clear();
	}

	public void SetVisible(int index, bool visible)
	{
		if (index < 0 || index >= plotters.Count)
		{
			GD.PrintErr($"SetVisible: Index {index} out of range for plotters list.");
			return;
		}

		if (visible) {
			plotters[index].ShowPlot();
		}
		else {
			plotters[index].HidePlot();
		}
	}

	public void ResetCamera() => GridController.ResetCamera();

	public CablePlotter[] GetPlotters() {
		if (!IsReady) {
			throw new Exception("Cannot call GetPlotters before Coordinator is ready.");
		}
		return plotters.ToArray();
	}

	public override void _Ready()
	{
		AddPlotter(new RawPlotter("Initial Plot", new Color(0.9f, 0.9f, 0)));
		AddPlotter(new FEMLine(new Color(0, 0, 1)));
		AddPlotter(new MassSpringCable("Mass Spring", new Color(0.7f, 0, 0.7f)));
		// AddPlotter(new FEMLineNewer(new Color(0.7f, 0, 0.7f)));
		IsReady = true;
	}

	public const float PixelsPerMeter = 75.0f;

	public static Vector2 WorldToMeters(Vector2 worldPos)
	{
		return new Vector2(
			worldPos.X / PixelsPerMeter,
			-worldPos.Y / PixelsPerMeter
		);
	}

	public static float WorldToMetersX(float x) => x / PixelsPerMeter;
	public static float WorldToMetersY(float y) => -y / PixelsPerMeter;

	public static Vector2 MetersToWorld(Vector2 meterPos)
	{
		return new Vector2(
			meterPos.X * PixelsPerMeter,
			-meterPos.Y * PixelsPerMeter
		);
	}

	public static float MetersToWorldX(float x) => x * PixelsPerMeter;
	public static float MetersToWorldY(float y) => -y * PixelsPerMeter;
}
