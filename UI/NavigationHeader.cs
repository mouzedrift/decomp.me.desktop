using DecompMeDesktop.Core;
using Godot;
using System;

namespace DecompMeDesktop.UI;

public partial class NavigationHeader : Node
{
	private Button _newScratchButton;
	private Button _settingsButton;

	private readonly PackedScene SettingsPage = ResourceLoader.Load<PackedScene>("uid://dq8qy1aik7870");

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_newScratchButton = GetNode<Button>("HBoxContainer/HBoxContainer2/NewScratchButton");
		_settingsButton = GetNode<Button>("HBoxContainer/HBoxContainer2/SettingsButton");

		_settingsButton.Pressed += () => SceneManager.Instance.ChangeScene(SettingsPage.Instantiate());
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
