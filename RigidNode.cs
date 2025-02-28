using Godot;
using System;

public partial class RigidNode : RopeNode
{
	private RigidBody2D _rigidBody;
	private CollisionShape2D _collisionShape;

	public RigidNode(float mass = 1.0f, bool isFixed = false)
	{
		_rigidBody = new RigidBody2D
		{
			Mass = mass,
			GravityScale=2f
		};

		
		_collisionShape = new CollisionShape2D();
		_rigidBody.AddChild(_collisionShape);

		var shape = new CircleShape2D { Radius = 5 };
		_collisionShape.Shape = shape;

		SetFixed(isFixed);
	}

	public override void _Ready()
	{
		_rigidBody.GlobalPosition = GlobalPosition;
		GetParent().CallDeferred("add_child", _rigidBody);;
	}
	
	public override void _PhysicsProcess(double delta)
	{
		Position = _rigidBody.Position;
		Rotation = _rigidBody.Rotation;
	}

	public override void AddVelocity(Vector2 deltaV)
	{
		_rigidBody.LinearVelocity += deltaV;
	}
	
	public override void ApplyForce(Vector2 force)
	{
		if (!GetFixed())
		{
			_rigidBody.ApplyCentralImpulse(force * Engine.PhysicsTicksPerSecond);
		}
	}

	
	public override Vector2 GetVelocity()
	{
		return _rigidBody.LinearVelocity;
	}

	public override void SetFixed(bool fixedState)
	{
		if (fixedState)
		{
			_rigidBody.Freeze = true;
			_rigidBody.LinearVelocity = Vector2.Zero;
			_rigidBody.AngularVelocity = 0f;
		}
		else
		{
			_rigidBody.Freeze = false;
		}
	}

	public override void SetMass(float mass)
	{
		_rigidBody.Mass = mass;
	}

	public override bool GetFixed()
	{
		return _rigidBody.Freeze == true;
	}

	public override float GetMass()
	{
		return _rigidBody.Mass;
	}
}
