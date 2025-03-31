using Godot;
using System;

public partial class CameraDragZoom : Camera2D
{
	private bool isDragging = false;
	private Vector2 previousMouseScreenPos;

	private Vector2 homePosition;
	private Vector2 homeZoom;

	[Export] public float ZoomStep = 0.1f;
	[Export] public float MinZoom = 0.2f;
	[Export] public float MaxZoom = 5f;

	public override void _Ready()
	{
		// Save initial camera state
		homePosition = Position;
		homeZoom = Zoom;
	}


	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left)
			{
				if (mouseButton.Pressed)
				{
					isDragging = true;
					previousMouseScreenPos = GetViewport().GetMousePosition();
				}
				else
				{
					isDragging = false;
				}
			}

			if (mouseButton.ButtonIndex == MouseButton.WheelUp)
				ApplyZoom(+ZoomStep);
			else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
				ApplyZoom(-ZoomStep);
		}

		if (@event is InputEventMouseMotion && isDragging)
		{
			Vector2 currentMouseScreenPos = GetViewport().GetMousePosition();
			Vector2 screenDelta = currentMouseScreenPos - previousMouseScreenPos;
			Position -= screenDelta / Zoom;
			previousMouseScreenPos = currentMouseScreenPos;
		}
	}

	private void ApplyZoom(float zoomChange)
	{
		float zoomAvg = (Zoom.X + Zoom.Y) * 0.5f;
		float scaledStep = zoomChange * zoomAvg;

		float targetZoomX = Zoom.X + scaledStep;
		float targetZoomY = Zoom.Y + scaledStep;

		float clampedZoomX = Mathf.Clamp(targetZoomX, MinZoom, MaxZoom);
		float clampedZoomY = Mathf.Clamp(targetZoomY, MinZoom, MaxZoom);

		Zoom = new Vector2(clampedZoomX, clampedZoomY);
	}

	public void ResetCamera()
	{
		Position = homePosition;
		Zoom = homeZoom;
	}
}
