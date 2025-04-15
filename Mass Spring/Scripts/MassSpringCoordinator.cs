using Godot;
using System;

public partial class MassSpringCoordinator : Node
{
	public static MassSpringCoordinator Instance { get; private set;}
	public bool IsReady { get; private set; } = false;
	[Export] public GridlineController GridController;
	[Export] public Node2D WorldRoot;
	private Vector2 startPoint;
	private Vector2 endPoint;
	private float mass;
	private float length;
	private int segmentCount;
	private PackedScene cablePackedScene;

	public MassSpringCoordinator() {
		Instance = this;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
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
