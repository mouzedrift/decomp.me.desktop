using Godot;
using System;

namespace DecompMeDesktop.UI;

public partial class MinMaxHSlider : VBoxContainer
{
	[Export] private string _title = "Title";
	[Export] private int _minValue = 100;
	[Export] private int _maxValue = 2000;
	private int _value = 500;

	[Export]
	public int Value
	{ 
		get => _value; 
		set
		{
			_value = Mathf.Clamp(value, _minValue, _maxValue);
			_hslider.Value = _value;
		}
	}

	private Label _titleLabel;
	private Label _currentValueLabel;
	private Label _minValueLabel;
	private Label _maxValueLabel;
	private HSlider _hslider;

	public HSlider GetHSlider() => _hslider;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_titleLabel = GetNode<Label>("TitleLabel");
		_currentValueLabel = GetNode<Label>("HBoxContainer/CurrentValueLabel");
		_minValueLabel = GetNode<Label>("HBoxContainer/MinValueLabel");
		_maxValueLabel = GetNode<Label>("HBoxContainer/MaxValueLabel");
		_hslider = GetNode<HSlider>("HBoxContainer/HSlider");

		_hslider.ValueChanged += OnValueChanged;
		Value = _value;
		_hslider.MinValue = _minValue;
		_hslider.MaxValue = _maxValue;

		UpdateText();
	}

	private void OnValueChanged(double value)
	{
		_currentValueLabel.Text = $"{(int)value}ms";
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint())
		{
			UpdateText();
		}
	}

	private void UpdateText()
	{
		_titleLabel.Text = _title;
		_currentValueLabel.Text = $"{_value}ms";
		_minValueLabel.Text = $"{_minValue}ms";
		_maxValueLabel.Text = $"{_maxValue}ms";
	}
}
