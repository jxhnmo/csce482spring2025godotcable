using Godot;
using System;

public partial class GridlineController : Node2D
{
	[Export] public NodePath WorldRootPath;
	private Node2D worldRoot;

	private bool isDragging = false;
	private Vector2 previousMouseScreenPos;

	private Vector2 homePosition;
	private Vector2 homeScale;

	[Export] public float ZoomStep = 0.1f;
	[Export] public float MinZoom = 0.2f;
	[Export] public float MaxZoom = 5f;
	[Export] public float GridSpacing = 1.0f; // in meters
	[Export] public Color GridColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
	[Export] public Color TextColor = new Color(1f, 1f, 1f);

	public override void _Ready()
	{
		worldRoot = GetNode<Node2D>(WorldRootPath);
		homePosition = worldRoot.Position;
		homeScale = worldRoot.Scale;

		SetProcessInput(true);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left)
			{
				isDragging = mouseButton.Pressed;
				previousMouseScreenPos = GetViewport().GetMousePosition();
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

			worldRoot.Position += screenDelta;
			previousMouseScreenPos = currentMouseScreenPos;
		}

		QueueRedraw();
	}

	private void ApplyZoom(float zoomChange)
	{
		Vector2 viewportSize = GetViewportRect().Size;
		Vector2 mouseViewportPos = GetViewport().GetMousePosition();
		Vector2 mouseScreenOffset = mouseViewportPos - viewportSize / 2f;

		Vector2 scaleBefore = worldRoot.Scale;
		Vector2 centerWorld = -worldRoot.Position;
		Vector2 mouseWorldBefore = (centerWorld + mouseScreenOffset) / scaleBefore;

		float zoomAvg = (scaleBefore.X + scaleBefore.Y) * 0.5f;
		float zoomStep = zoomChange * zoomAvg;

		float newZoomX = Mathf.Clamp(scaleBefore.X + zoomStep, MinZoom, MaxZoom);
		float newZoomY = Mathf.Clamp(scaleBefore.Y + zoomStep, MinZoom, MaxZoom);
		Vector2 scaleAfter = new Vector2(newZoomX, newZoomY);

		Vector2 mouseScreenOffsetBefore = mouseWorldBefore * scaleBefore;
		Vector2 mouseScreenOffsetAfter  = mouseWorldBefore * scaleAfter;

		Vector2 offsetDelta = mouseScreenOffsetAfter - mouseScreenOffsetBefore;

		worldRoot.Scale = scaleAfter;
		worldRoot.Position -= offsetDelta;
	}

	public void ResetCamera()
	{
		worldRoot.Position = homePosition;
		worldRoot.Scale = homeScale;
		QueueRedraw();
	}

	public override void _Draw()
	{
		Vector2 viewportSize = GetViewportRect().Size;
		Vector2 scale = worldRoot.Scale;

		// Position of worldRoot in screenspace
		Vector2 centerWorld = -worldRoot.Position;

		// Get screen bounds in world space
		Vector2 topLeftScreen = - viewportSize / 2f;
		Vector2 bottomRightScreen =  viewportSize / 2f;
		Vector2 topLeftWorld = (centerWorld + topLeftScreen) / scale;
		Vector2 bottomRightWorld = (centerWorld + bottomRightScreen) / scale;
		Vector2 topLeftMeters = Coordinator.WorldToMeters(topLeftWorld);
		Vector2 bottomRightMeters = Coordinator.WorldToMeters(bottomRightWorld);

		// Round bounds outward for full meter coverage
		int startX = Mathf.FloorToInt(topLeftMeters.X);
		int endX = Mathf.CeilToInt(bottomRightMeters.X);
		int startY = Mathf.FloorToInt(bottomRightMeters.Y);
		int endY = Mathf.CeilToInt(topLeftMeters.Y);

		// Draw vertical lines every 1m
		for (int x = startX; x <= endX; x++)
		{
			Vector2 worldStart = Coordinator.MetersToWorld(new Vector2(x, startY));
			Vector2 worldEnd   = Coordinator.MetersToWorld(new Vector2(x, endY));

			Vector2 screenStart = worldStart * scale - centerWorld;
			Vector2 screenEnd   = worldEnd   * scale - centerWorld;

			if (x != 0)
				DrawLine(screenStart, screenEnd, GridColor, 1.0f);
			else
				DrawLine(screenStart, screenEnd, Colors.Green, 1.0f); // Y axis
		}

		// Draw horizontal lines every 1m
		for (int y = startY; y <= endY; y++)
		{
			Vector2 worldStart = Coordinator.MetersToWorld(new Vector2(startX, y));
			Vector2 worldEnd   = Coordinator.MetersToWorld(new Vector2(endX, y));

			Vector2 screenStart = worldStart * scale - centerWorld;
			Vector2 screenEnd   = worldEnd   * scale - centerWorld;

			if (y != 0)
				DrawLine(screenStart, screenEnd, GridColor, 1.0f);
			else
				DrawLine(screenStart, screenEnd, Colors.Red, 1.0f); // X axis
		}

		// --- Cursor Position in Meters ---
		Vector2 mouseViewportPos = GetViewport().GetMousePosition();
		Vector2 mouseScreenPos = mouseViewportPos - viewportSize / 2f;
		if (
			mouseScreenPos.X < topLeftScreen.X || 
			mouseScreenPos.X > bottomRightScreen.X || 
			mouseScreenPos.Y < topLeftScreen.Y || 
			mouseScreenPos.Y > bottomRightScreen.Y
		) return;
		Vector2 worldPos = (centerWorld + mouseScreenPos) / scale;
		Vector2 metersPos = Coordinator.WorldToMeters(worldPos);

		// Format nicely
		string cursorText = $"Cursor: {metersPos.X:F2} m, {metersPos.Y:F2} m";

		// Pick a font
		var font = ThemeDB.FallbackFont;
		Vector2 margin = new Vector2(10, -10);
		Vector2 drawPos = new Vector2(margin.X + topLeftScreen.X, margin.Y + bottomRightScreen.Y);
		DrawString(font, drawPos, cursorText, HorizontalAlignment.Left, -1, 16, TextColor);
	}

}
