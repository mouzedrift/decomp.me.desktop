using Godot;
using System;
using System.Threading.Tasks;

namespace DecompMeDesktop.UI;

public partial class TaskProgressBar : HBoxContainer
{
	public Func<Task<bool>> WorkFunc { get; set; }
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
		_checkTextureRect.Texture = ResourceLoader.Load<Texture2D>("uid://qhubu8k8d4n0");
		_checkTextureRect.SelfModulate = Colors.White;
		_progressBar.Indeterminate = false;
		_progressBar.Value = 100;
	}

	public void MarkAsFailed()
	{
		_checkTextureRect.Texture = ResourceLoader.Load<Texture2D>("uid://dg20e33s7h7x1");
		_checkTextureRect.SelfModulate = Colors.White;
		_progressBar.Indeterminate = false;
		_progressBar.Value = 0;
	}
}
