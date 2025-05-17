using Godot;
using Godot.Collections;
using System;

public partial class MirrorHighlighter : SyntaxHighlighter
{
	public AsmHighlighter SourceHighlighter { get; set; }

	public MirrorHighlighter()
	{

	}

	public override Dictionary _GetLineSyntaxHighlighting(int line)
	{
		if (SourceHighlighter == null)
		{
			return new Dictionary();
		}

		//GetTextEdit().GetLine(line).Length;
		var highlighting = SourceHighlighter.GetLineSyntaxHighlighting(line);
		return highlighting;
	}

	private void ColorRange(int start, int end, Color color, ref Dictionary dict)
	{
		dict[start] = new Dictionary() { { "color", color } };
		dict[end] = new Dictionary() { { "color", Colors.White } };
	}
}
