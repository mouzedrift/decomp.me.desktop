using Godot;
using System;

namespace DecompMeDesktop.UI;
public partial class CompilerOptionButton : Button
{
	[Export] private MarginContainer _vboxContainer;
	[Export] private Label _optionName;
	[Export] private Label _optionDescription;
	[Export] private CheckBox _checkbox;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SyncButtonSize();
		_vboxContainer.Resized += () =>
		{
			SyncButtonSize();
		};

		Toggled += (bool toggledOn) =>
		{
			GD.Print("toggled");
			_checkbox.ButtonPressed = toggledOn;
		};

		_optionName.Text = "/TP";
		_optionDescription.Text = "Compile as C++";
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void SyncButtonSize()
	{
		CustomMinimumSize = _vboxContainer.Size;
	}
}
