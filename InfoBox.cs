using Godot;
using System;

public partial class InfoBox : Control
{
	public override void _Ready()
	{
		Button closeButton = GetNode<Button>("GreyBackground/CenterContainer/MainPanel/MarginContainer/VBoxContainer/HBoxContainer/CloseButton");
		closeButton.Pressed += Hide;
		Show();
	}
}
