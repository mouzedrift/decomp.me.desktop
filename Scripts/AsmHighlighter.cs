using Godot;
using Godot.Collections;
using System;

public partial class AsmHighlighter : SyntaxHighlighter
{
	public AsmHighlighter()
	{

	}

	public override Dictionary _GetLineSyntaxHighlighting(int line)
	{
		var dict = new Dictionary();
		var lineStr = GetTextEdit().GetLine(line);
		int lineEnd = lineStr.Length;

		if (lineStr.StartsWith('>'))
		{
			ColorRange(0, lineEnd, Color.FromHtml("#45BD00"), ref dict);
		}
		else if (lineStr.StartsWith('<'))
		{
			ColorRange(0, lineEnd, Color.FromHtml("#C82829"), ref dict);
		}
		else if (lineStr.StartsWith('|'))
		{
			ColorRange(0, lineEnd, Color.FromHtml("#6D6DFF"), ref dict);
		}
		else if (lineStr.StartsWith('i'))
		{
			ColorRange(0, lineEnd, Color.FromHtml("#6D6DFF"), ref dict);
		}
		else if (lineStr.StartsWith('r'))
		{
			ColorRange(0, lineEnd, Color.FromHtml("#AA8B00"), ref dict);
		}
		return dict;
	}

	private void ColorRange(int start, int end, Color color, ref Dictionary dict)
	{
		dict[start] = new Dictionary() { { "color", color } };
		//dict[end] = new Dictionary() { { "color", Colors.White } };
	}
}
