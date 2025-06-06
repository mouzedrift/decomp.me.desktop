using DecompMeDesktop.Core;
using Godot;
using System;

namespace DecompMeDesktop.UI;

public partial class NavigationHeader : Node
{
	private Button _newScratchButton;
	private Button _settingsButton;
	private Button _userButton;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_newScratchButton = GetNode<Button>("HBoxContainer/HBoxContainer2/NewScratchButton");
		_settingsButton = GetNode<Button>("HBoxContainer/HBoxContainer2/SettingsButton");
		_userButton = GetNode<Button>("HBoxContainer/HBoxContainer2/UserButton");

		_userButton.Pressed += () =>
		{

		};

		_settingsButton.Pressed += SceneManager.GotoSettings;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
