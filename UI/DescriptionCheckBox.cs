using Godot;
using System;
using System.Text.RegularExpressions;

namespace DecompMeDesktop.UI;

public partial class DescriptionCheckBox : HBoxContainer
{
	[Export] private string _title;
	[Export] private string _description;

	private Label _titleLabel;
	private Label _descriptionLabel;

	public CheckBox GetCheckBox() => GetNode<CheckBox>("CheckBox");

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_titleLabel = GetNode<Label>("VBoxContainer/TitleLabel");
		_descriptionLabel = GetNode<Label>("VBoxContainer/DescriptionLabel");

		_descriptionLabel.CustomMinimumSize = new Vector2(Size.X, 0f);

		_titleLabel.Text = _title != "" ? _title : _titleLabel.Text;
		_descriptionLabel.Text = _description != "" ? _description : _titleLabel.Text;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
