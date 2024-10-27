using Godot;
using System;

public partial class RigidBody3d : RigidBody3D
{
	// Cameras
	private Camera3D thirdPersonCamera;
	private Camera3D firstPersonCamera;
	private bool isThirdPersonActive = true;  // Initially start with the third-person camera

	// Movement and rotation speed settings
	private float movementSpeed = 20.0f;
	private float rotationSpeed = 30.0f;
	private float maxTiltAngle = Mathf.Pi / 4;

	public override void _Ready()
	{
		// Find the cameras in the scene
		thirdPersonCamera = GetNode<Camera3D>("Camera3D"); // This is the third-person camera
		firstPersonCamera = GetNode<Camera3D>("FirstPersonCamera3D"); // This should be the new first-person camera

		// Set initial active camera (disable first-person at start)
		thirdPersonCamera.Current = true;
		firstPersonCamera.Current = false;
	}

	public override void _PhysicsProcess(double delta)
	{
		// Toggle camera on key press
		if (Input.IsActionJustPressed("toggle_camera"))
		{
			ToggleCamera();
		}

		// Check if the tank is tipped over
		if (!IsUpright())
		{
			return;
		}

		// Movement logic as before
		Vector3 forwardDir = -GlobalTransform.Basis.Z.Normalized();
		Vector3 rightDir = GlobalTransform.Basis.X.Normalized();
		Vector3 force = Vector3.Zero;

		if (Input.IsActionPressed("move_forward"))
		{
			force += forwardDir * movementSpeed;
		}
		if (Input.IsActionPressed("move_backward"))
		{
			force -= forwardDir * movementSpeed;
		}

		ApplyCentralForce(force);

		if (Input.IsActionPressed("move_left"))
		{
			ApplyCentralForce(-rightDir * rotationSpeed);
			ApplyCentralForce(forwardDir * rotationSpeed);
		}
		if (Input.IsActionPressed("move_right"))
		{
			ApplyCentralForce(rightDir * rotationSpeed);
			ApplyCentralForce(-forwardDir * rotationSpeed);
		}
	}

	private bool IsUpright()
	{
		Vector3 upDir = GlobalTransform.Basis.Y.Normalized();
		float angleBetween = upDir.AngleTo(Vector3.Up);
		return angleBetween < maxTiltAngle;
	}

	private void ToggleCamera()
	{
		isThirdPersonActive = !isThirdPersonActive;

		// Switch cameras
		thirdPersonCamera.Current = isThirdPersonActive;
		firstPersonCamera.Current = !isThirdPersonActive;
	}
}
