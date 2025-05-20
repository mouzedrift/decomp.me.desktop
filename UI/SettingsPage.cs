using Godot;
using System;

namespace DecompMeDesktop.UI;

public partial class SettingsPage : VBoxContainer
{
	private Button _accountButton;
	private Button _appearanceButton;
	private Button _editorButton;
	private Button _compilersButton;
	private readonly PackedScene CompilerManagerWindow = ResourceLoader.Load<PackedScene>("uid://uqbucagyx7yy");

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_accountButton = GetNode<Button>("MarginContainer/VBoxContainer/AccountButton");
		_appearanceButton = GetNode<Button>("MarginContainer/VBoxContainer/AppearanceButton");
		_editorButton = GetNode<Button>("MarginContainer/VBoxContainer/EditorButton");
		_compilersButton = GetNode<Button>("MarginContainer/VBoxContainer/CompilersButton");

		_compilersButton.Pressed += () =>
		{
			var compilerManager = CompilerManagerWindow.Instantiate<CompilerManagerWindow>();
			AddChild(compilerManager);
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
