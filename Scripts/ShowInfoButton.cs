using Godot;
using System;

public partial class ShowInfoButton : Button
{
	[Export]
	public NodePath InfoBoxPath;

	private Control infoBox;

	public override void _Ready()
	{
		if (InfoBoxPath != null && HasNode(InfoBoxPath))
		{
			infoBox = GetNode<Control>(InfoBoxPath);
			Pressed += OnButtonPressed;
		}
		else
		{
			GD.PrintErr("ShowInfoButton: InfoBoxPath is not set or invalid.");
		}
	}

	private void OnButtonPressed()
	{
		if (infoBox != null)
			infoBox.Show();
	}
}
