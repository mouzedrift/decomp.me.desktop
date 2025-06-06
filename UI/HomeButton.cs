using DecompMeDesktop.Core;
using Godot;
using System;

namespace DecompMeDesktop.UI;

public partial class HomeButton : HBoxContainer
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb)
		{
			if (mb.ButtonIndex == MouseButton.Left && mb.Pressed)
			{
				SceneManager.GotoHomepage();
			}
		}
	}
}
