using Godot;
using System;

public partial class YourScratchesTextItem : HBoxContainer
{
	[Export] private Label _functionLabel;
	[Export] private Label _matchPercentageLabel;

	public void SetFunctionName(string name) => _functionLabel.Text = name;
	public void SetMatchPercentage(string percentage) => _matchPercentageLabel.Text = percentage;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

}
