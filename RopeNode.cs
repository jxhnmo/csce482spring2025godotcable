using Godot;
using System;
using System.Collections.Generic;

public abstract partial class RopeNode : Node2D
{
	public abstract void AddVelocity(Vector2 deltaV);
	public abstract void SetFixed(bool fixedState);
	public abstract void SetMass(float mass);
	public abstract Vector2 GetVelocity();
	public abstract bool GetFixed();
	public abstract float GetMass();
	public abstract void ApplyForce(Vector2 force);
	
	public override void _Draw()
	{
		Color color = GetFixed() ? Colors.Red : Colors.White;
		DrawCircle(Vector2.Zero, 5, color);
	}
}
