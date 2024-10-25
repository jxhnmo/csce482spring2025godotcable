using Godot;
using System;

public partial class RigidBody3d : RigidBody3D
{
	// Movement and rotation speed settings
	private float movementSpeed = 20.0f;  // Stronger forward/backward force
	private float rotationSpeed = 30.0f;  // Stronger rotation force for tank-like turning

	// The maximum angle (in radians) the tank can tilt before becoming immobile
	private float maxTiltAngle = Mathf.Pi / 4;  // 45 degrees (you can adjust this)
	
	public override void _PhysicsProcess(double delta)
	{
		// Check if the tank is tipped over past the allowed threshold
		if (!IsUpright())
		{
			// If the tank is tipped over, do not allow any movement
			return;
		}

		// Get the forward direction (Z axis in Godot is forward)
		Vector3 forwardDir = -GlobalTransform.Basis.Z.Normalized();
		Vector3 rightDir = GlobalTransform.Basis.X.Normalized();

		// Movement vector initialization
		Vector3 force = Vector3.Zero;

		// Moving forward
		if (Input.IsActionPressed("move_forward"))
		{
			force += forwardDir * movementSpeed;  // Apply forward force
		}

		// Moving backward
		if (Input.IsActionPressed("move_backward"))
		{
			force -= forwardDir * movementSpeed;  // Apply backward force
		}

		// Apply the force (central force moves it forward or backward)
		ApplyCentralForce(force);

		// Tank-style turning
		// Turning left: Apply backward force to the left side and forward force to the right side
		if (Input.IsActionPressed("move_left"))
		{
			ApplyCentralForce(-rightDir * rotationSpeed);  // Left side (backward)
			ApplyCentralForce(forwardDir * rotationSpeed); // Right side (forward)
		}

		// Turning right: Apply forward force to the left side and backward force to the right side
		if (Input.IsActionPressed("move_right"))
		{
			ApplyCentralForce(rightDir * rotationSpeed);  // Left side (forward)
			ApplyCentralForce(-forwardDir * rotationSpeed); // Right side (backward)
		}
	}

	// Function to check if the tank is upright
	private bool IsUpright()
	{
		// Get the current up vector (Basis.Y is the up vector in Godot)
		Vector3 upDir = GlobalTransform.Basis.Y.Normalized();

		// Check the angle between the current up vector and the world up vector (Vector3.Up)
		float angleBetween = upDir.AngleTo(Vector3.Up);

		// If the angle exceeds the maximum allowed tilt, the tank is considered tipped over
		return angleBetween < maxTiltAngle;
	}
}
