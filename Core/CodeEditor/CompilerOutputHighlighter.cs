using Godot;
using Godot.Collections;
using System;

namespace DecompMeDesktop.Core.CodeEditor;

public partial class CompilerOutputHighlighter : SyntaxHighlighter
{
	public CompilerOutputHighlighter()
	{

	}

	public override Dictionary _GetLineSyntaxHighlighting(int line)
	{
		var dict = new Dictionary();
		var lineStr = GetTextEdit().GetLine(line);
		int lineEnd = lineStr.Length;

		if (lineStr.Contains(") : error"))
		{
			ColorRange(0, lineEnd, Color.FromHtml("#C82829"), ref dict);
		}
		else if (lineStr.Contains(") : warning"))
		{
			ColorRange(0, lineEnd, Colors.Yellow, ref dict);
		}

		return dict;
	}

	private void ColorRange(int start, int end, Color color, ref Dictionary dict)
	{
		dict[start] = new Dictionary() { { "color", color } };
		//dict[end] = new Dictionary() { { "color", Colors.White } };
	}
}
