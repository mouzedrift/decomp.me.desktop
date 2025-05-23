using Godot;
using System;

namespace DecompMeDesktop.UI;

public partial class YourScratchesTextItem : HBoxContainer
{
	[Export] private LinkLabel _functionLabel;
	[Export] private Label _matchPercentageLabel;

	[Signal] public delegate void LinkLabelPressedEventHandler();

	public void SetFunctionName(string name) => _functionLabel.Text = name;
	public void SetMatchPercentage(string percentage) => _matchPercentageLabel.Text = percentage;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_functionLabel.Pressed += () => EmitSignal(SignalName.LinkLabelPressed);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

}
