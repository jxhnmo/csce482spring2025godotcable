using Godot;
using System;

public partial class WorldViewport : SubViewport
{
	public override void _Ready()
	{
		// var container = GetParent<SubViewportContainer>();

		// // Initial size
		// Size = (Vector2I)container.Size;
		// Size2DOverrideStretch = true;

		// // Sync on resize
		// container.Resized += () =>
		// {
		// 	Size = (Vector2I)container.Size;
		// };
	}
}
