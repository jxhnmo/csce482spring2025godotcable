using Godot;
using System;

public partial class main : Node
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		AddChild(RopeFactory.CreateRope(5.0f, 200.0f, 2, new Vector2(300, 100)));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
