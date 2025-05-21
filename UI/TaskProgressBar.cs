using Godot;
using System;

namespace DecompMeDesktop.UI;

public partial class TaskProgressBar : HBoxContainer
{
	private Label _taskLabel;
	private ProgressBar _progressBar;
	private TextureRect _checkTextureRect;

	// Called when the node enters the scene tree for the first time.

	public override void _Ready()
	{
		_taskLabel = GetNode<Label>("TaskLabel");
		_progressBar = GetNode<ProgressBar>("ProgressBar");
		_checkTextureRect = GetNode<TextureRect>("CheckTextureRect");

		SetDefaultState();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void SetTaskDescription(string description)
	{
		_taskLabel.Text = description;
	}

	public void SetDefaultState()
	{
		_checkTextureRect.SelfModulate = Colors.Transparent;
		_progressBar.Indeterminate = false;
		_progressBar.Value = 0;
	}

	public void ShowProgress()
	{
		_checkTextureRect.SelfModulate = Colors.Transparent;
		_progressBar.Indeterminate = true;
		_progressBar.Value = 0;
	}

	public void MarkAsDone()
	{
		_checkTextureRect.SelfModulate = Colors.White;
		_progressBar.Indeterminate = false;
		_progressBar.Value = 100;
	}
}
