using Godot;
using System;
using System.Runtime.CompilerServices;

namespace DecompMeDesktop.UI;

public partial class SettingsPage : VBoxContainer
{
	private Button _accountButton;
	private Button _appearanceButton;
	private Button _editorButton;
	private Button _compilersButton;
	private readonly PackedScene CompilerManagerWindow = ResourceLoader.Load<PackedScene>("uid://uqbucagyx7yy");
	private Control _activeCategory;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_accountButton = GetNode<Button>("MarginContainer/HBoxContainer2/HBoxContainer/SideBar/AccountButton");
		_appearanceButton = GetNode<Button>("MarginContainer/HBoxContainer2/HBoxContainer/SideBar/AppearanceButton");
		_editorButton = GetNode<Button>("MarginContainer/HBoxContainer2/HBoxContainer/SideBar/EditorButton");
		_compilersButton = GetNode<Button>("MarginContainer/HBoxContainer2/HBoxContainer/SideBar/CompilersButton");

		_accountButton.Pressed += () =>
		{
			SwitchPage("AccountCategory");
		};

		_editorButton.Pressed += () =>
		{
			SwitchPage("EditorCategory");
		};

		_compilersButton.Pressed += () =>
		{
			var compilerManager = CompilerManagerWindow.Instantiate<CompilerManagerWindow>();
			AddChild(compilerManager);
		};

		SwitchPage("EditorCategory");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void SwitchPage(string pageToShow)
	{
		Control pagesParent = GetNode<Control>("MarginContainer/HBoxContainer2/HBoxContainer/CategoryContent");
		foreach (Control child in pagesParent.GetChildren())
		{
			child.Visible = child.Name == pageToShow;
			if (child.Visible)
			{
				_activeCategory = child;
			}
		}
	}
}
