using Godot;
using System;

public partial class ExternalForce : Control
{
	[Export] public NodePath NodeBoxPath;
	[Export] public NodePath XBoxPath;
	[Export] public NodePath YBoxPath;
	[Export] public NodePath RemovePath;
	private SpinBox nodeBox;
	private SpinBox xBox;
	private SpinBox yBox;
	private Button remove;

	public override void _Ready()
	{
		nodeBox = GetNode<SpinBox>(NodeBoxPath);
		xBox = GetNode<SpinBox>(XBoxPath);
		yBox = GetNode<SpinBox>(YBoxPath);
		remove = GetNode<Button>(RemovePath);
	}

	public void SetMaxIndex(int index) {
		nodeBox.MaxValue = index;
	}

	public Vector2 GetForce()
	{
		return new Vector2((float)xBox.Value, (float)yBox.Value);
	}

	public int GetNodeIndex()
	{
		return (int)nodeBox.Value;
	}

	public void SetRemoveAction(Action action) {
		remove.Pressed += action;
	}

}
