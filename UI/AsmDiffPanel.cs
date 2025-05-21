using Godot;
using System;
using DecompMeDesktop.Core.CodeEditor;
using DecompMeDesktop.Core;

namespace DecompMeDesktop.UI;

public partial class AsmDiffPanel : HSplitContainer
{
	[Export] private TextEdit _targetEdit;
	[Export] private TextEdit _currentEdit;
	[Export] private RichTextLabel _richTextLabel;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_targetEdit.GetVScrollBar().SelfModulate = Colors.Transparent;
		_targetEdit.GetVScrollBar().MouseFilter = MouseFilterEnum.Ignore;

		_currentEdit.SyntaxHighlighter = new AsmHighlighter();

		var mirrorHighlighter = new MirrorHighlighter();
		mirrorHighlighter.SourceHighlighter = (AsmHighlighter)_currentEdit.SyntaxHighlighter;
		_targetEdit.SyntaxHighlighter = mirrorHighlighter;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_targetEdit.ScrollVertical = _currentEdit.ScrollVertical;
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton button)
		{
			if (button.ButtonIndex == MouseButton.WheelDown)
			{
				_targetEdit.ScrollVertical += 1.5;
				_currentEdit.ScrollVertical += 1.5;
				GetViewport().SetInputAsHandled();
			}
			else if (button.ButtonIndex == MouseButton.WheelUp)
			{
				_targetEdit.ScrollVertical -= 1.5;
				_currentEdit.ScrollVertical -= 1.5;
				GetViewport().SetInputAsHandled();
			}

		}
	}

	public void SetTargetText(string text) => _targetEdit.Text = text;
	public void SetCurrentText(string text) => _currentEdit.Text = text;

	public void ClearText()
	{
		_targetEdit.Text = string.Empty;
		_currentEdit.Text = string.Empty;
	}

	public void SetScore(int score, int maxScore)
	{
		var missingScore = maxScore - score;
		_richTextLabel.Text = $"[b]Compilation[/b] {missingScore} ({Utils.GetMatchPercentage(score, maxScore)})";
		_richTextLabel.TooltipText = $"{score}/{maxScore}";
	}
}
