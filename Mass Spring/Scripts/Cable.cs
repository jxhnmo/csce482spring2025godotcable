using Godot;
using System;
using System.Collections.Generic;

public partial class Cable : Node2D
{
	[Export] 
	private float startXPosition = 200.0f;
	[Export]
	private float startYPosition = 400.0f;
	[Export]
	private float endXPosition = 600.0f;
	[Export]
	private float endYPosition = 200.0f;
	[Export]
	private float bias = 0.1f;
	[Export]
	private float softness = 0.01f;
	[Export]
	private float mass = 100.0f;
	[Export]
	private float length = 500.0f;
	[Export]
	private int numSegments = 10;
		
	public void Initialize(
		float startX, float startY, float endX, float endY,
		float mass, float length, int segments)
	{
		this.startXPosition = startX;
		this.startYPosition = startY;
		this.endXPosition = endX;
		this.endYPosition = endY;
		this.mass = mass;
		this.length = length;
		this.numSegments = segments;
	}
	
	public float GetStartXPosition() => startXPosition;
	public void SetStartXPosition(float value) => startXPosition = value;

	public float GetStartYPosition() => startYPosition;
	public void SetStartYPosition(float value) => startYPosition = value;

	public float GetEndXPosition() => endXPosition;
	public void SetEndXPosition(float value) => endXPosition = value;

	public float GetEndYPosition() => endYPosition;
	public void SetEndYPosition(float value) => endYPosition = value;

	public float GetBias() => bias;
	public void SetBias(float value) => bias = value;

	public float GetSoftness() => softness;
	public void SetSoftness(float value) => softness = value;

	public float GetMass() => mass;
	public void SetMass(float value) => mass = value;

	public float GetLength() => length;
	public void SetLength(float value) => length = value;

	public int GetNumSegments() => numSegments;
	public void SetNumSegments(int value) => numSegments = value;
	
	private bool StaticCableEnd = true;
	
	private PackedScene CableSegmentPackedScene;
	private CableSegment CableStart;
	private CableSegment CableEnd;
	private PinJoint2D CableStartPinJoint;
	private PinJoint2D CableEndPinJoint;
	
	private List<Vector2> CablePointsLine2D;
	private Line2D Line2DNode;
	private List<CableSegment> CableSegments = new List<CableSegment>();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		CablePointsLine2D = new List<Vector2>();
		CableSegmentPackedScene = GD.Load<PackedScene>("res://Mass Spring/cable_segment.tscn");
		Line2DNode = GetNode<Line2D>("Line2D");
		CableStart = GetNode<CableSegment>("CableStart");
		CableStart.GlobalPosition = new Vector2(startXPosition, startYPosition);
		CableEnd = GetNode<CableSegment>("CableEnd");
		CableEnd.GlobalPosition = CableStart.GlobalPosition + new Vector2(length, 0);
		CableStart.Cable = this;
		CableEnd.Cable = this;
		CableStartPinJoint = GetNode<PinJoint2D>("CableStart/PinJoint2D");
		CableEndPinJoint = GetNode<PinJoint2D>("CableEnd/PinJoint2D");

		SpawnCable();
		
		//CableEnd.GlobalPosition = new Vector2(endXPosition, endYPosition);
		var tween = CreateTween();
		tween.TweenProperty(CableEnd, "global_position", new Vector2(endXPosition, endYPosition), 1);
	}

	public void SpawnCable()
	{
		Vector2 cableStartPos = CableStart.GlobalPosition;
		Vector2 cableEndPos = CableEnd.GlobalPosition;
		//GD.Print(cableStartPos, cableEndPos);
		
		Vector2 direction = (cableEndPos - cableStartPos).Normalized();
		var interval = length / numSegments;
		var segmentMass = mass / numSegments;
		var rotationAngle = (float)(direction.Angle() - Math.PI / 2);
		CableStart.IndexInArray = 0;
		Vector2 currentPos = cableStartPos;
		CableSegment latestSegment = CableStart;

		CableSegments.Clear();
		CableSegments.Add(latestSegment);

		for (int i = 0; i < numSegments; i++)
		{
			currentPos += direction * interval;
			//GD.Print(currentPos);
			Console.WriteLine(currentPos);
			latestSegment = AddCableSegment(latestSegment, i + 1, rotationAngle, currentPos, segmentMass);
			CableSegments.Add(latestSegment);

			var jointPos = latestSegment.GetNode<PinJoint2D>("PinJoint2D").GlobalPosition;

			if (jointPos.DistanceTo(cableEndPos) < interval)
			{
				break;
			}
		}
		ConnectCableParts(CableEnd, latestSegment);
		CableEnd.Rotation = rotationAngle;
		CableSegments.Add(CableEnd);

		if (StaticCableEnd)
		{
			CableStart.Freeze = true;
			CableEnd.Freeze = true;
		}
		CableEnd.IndexInArray = numSegments;
	}

	private static void ConnectCableParts(CableSegment a, CableSegment b)
	{
		PinJoint2D pinJoint = a.GetNode("PinJoint2D") as PinJoint2D;
		pinJoint.NodeA = a.GetPath();
		pinJoint.NodeB = b.GetPath();
	}

	public CableSegment AddCableSegment(Node previousSegment, int id, float rotationAngle, Vector2 position, float segmentMass)
	{
		PinJoint2D pinJoint = previousSegment.GetNode("PinJoint2D") as PinJoint2D;

		var segment = CableSegmentPackedScene.Instantiate() as CableSegment;
		segment.GlobalPosition = position;
		segment.Rotation = rotationAngle;
		segment.Cable = this;
		segment.IndexInArray = id;
		segment.Mass = segmentMass;

		AddChild(segment);
		pinJoint.NodeA = previousSegment.GetPath();
		pinJoint.NodeB = segment.GetPath();
		pinJoint.Bias = bias;
		pinJoint.Softness = softness;
		return segment;
	}

	private void UpdateLine2DCable()
	{
		CablePointsLine2D.Clear();
		CablePointsLine2D.Add(CableStart.Position);
		//GD.Print(CableStart.Position);

		foreach (var segment in CableSegments)
		{
			CablePointsLine2D.Add(segment.Position);
			//GD.Print(segment.Position);
		}
		CablePointsLine2D.Add(CableEnd.Position);
		Line2DNode.Points = CablePointsLine2D.ToArray();
		
		//GD.Print(CableEnd.Position);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		UpdateLine2DCable();
	}
}
