using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Coordinator : Node
{
	public static Coordinator Instance { get; private set;}
	public bool IsReady { get; private set; } = false;
	[Export] public GridlineController GridController;
	[Export] public Node2D WorldRoot;
	private Vector2 startPoint;
	private Vector2 endPoint;
	private float mass;
	private float length;
	private int segmentCount;
	private PackedScene cablePackedScene;

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

	public void SetMass(float value) => mass = value;
	public void SetLength(float value) => length = value;
	public void SetSegmentCount(int value) {
		segmentCount = value;
		foreach (var force in externalForces) {
			force.SetMaxIndex(segmentCount - 1);
		}
	}

	public float GetStartPointX() => startPoint.X;
	public float GetStartPointY() => startPoint.Y;
	public float GetEndPointX() => endPoint.X;
	public float GetEndPointY() => endPoint.Y;

	public Vector2 GetStartPoint() => startPoint;
	public Vector2 GetEndPoint() => endPoint;

	public float GetMass() => mass;
	public float GetLength() => length;
	public int GetSegmentCount() => segmentCount;

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
		Vector2[] initalPoints = InitialCurve.Make(startPoint, endPoint, mass, length, segmentCount);
		float nodeMass = mass / segmentCount;
		var addedForces = createExtraForcesList();
		foreach (CablePlotter plotter in plotters)
		{
			plotter.Generate(nodeMass, initalPoints, length, addedForces);
		}
		/*
		cablePackedScene = GD.Load<PackedScene>("res://Mass Spring/cable.tscn");
		var cableInstance = cablePackedScene.Instantiate() as Cable;
		cableInstance.Initialize(
			startX: MetersToWorldX(startPoint.X), startY: MetersToWorldY(startPoint.Y), endX: MetersToWorldX(endPoint.X), endY: MetersToWorldY(endPoint.Y),
			mass: mass, length: MetersToWorldX(length), segments: segmentCount
		);
		WorldRoot.AddChild(cableInstance);
		*/
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
		AddPlotter(new RawPlotter("Initial Plot", new Color(.9f, .9f, 0)));
		AddPlotter(new FEMLine(new Color(0, 0, 1)));
		
		
		
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
