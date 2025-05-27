using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DecompMeDesktop.Core.CodeEditor;

public class ColorRange
{
	public int ColumnStart;
	public int ColumnEnd;
	public Color Color;
}

// Visual Studio 2022 Dark Theme Colors for C++
public class ColorTheme
{
	public static Color Default { get; } = Color.FromHtml("#C8C8C8");
	public static Color LocalVariable { get; } = Color.FromHtml("#9CDCFE");
	public static Color Function { get; } = Color.FromHtml("#DCDCAA");
	public static Color Parameter { get; } = Color.FromHtml("#9A9A9A");
	public static Color Macro { get; } = Color.FromHtml("#BEB7FF");
	public static Color Type { get; } = Color.FromHtml("#4EC9B0");
	public static Color Class { get; } = Color.FromHtml("#4EC9B0");
	public static Color Enum { get; } = Color.FromHtml("#4EC9B0");
	public static Color EnumMember { get; } = Color.FromHtml("#B8D7A3");
	public static Color Blue { get; } = Color.FromHtml("#569CD6");
	public static Color Pink { get; } = Color.FromHtml("#C586C0");
	public static Color Preprocessor { get; } = new Color(0.8f, 0.8f, 0.8f);
	public static Color Number { get; } = Color.FromHtml("#B5CEA8");
	public static Color Comment { get; } = Color.FromHtml("#6A9955");
	public static Color String { get; } = Color.FromHtml("#CE9178");
}

public partial class CppHighlighter : SyntaxHighlighter
{
	private static Godot.Collections.Dictionary<string, Color> KeywordColorsInternal = new Godot.Collections.Dictionary<string, Color>
	{
		{ "alignas", ColorTheme.Blue},
		{ "and", ColorTheme.Blue},
		{ "and_eq", ColorTheme.Blue},
		{ "asm", ColorTheme.Blue},
		{ "atomic_cancel", ColorTheme.Blue},
		{ "atomic_commit", ColorTheme.Blue},
		{ "atomic_noexcept", ColorTheme.Blue},
		{ "auto", ColorTheme.Blue},
		{ "bitand", ColorTheme.Blue},
		{ "bitor", ColorTheme.Blue},
		{ "bool", ColorTheme.Blue},
		{ "break", ColorTheme.Pink},
		{ "case", ColorTheme.Pink},
		{ "catch", ColorTheme.Pink},
		{ "char", ColorTheme.Blue},
		{ "char8_t", ColorTheme.Blue},
		{ "char16_t", ColorTheme.Blue},
		{ "char32_t", ColorTheme.Blue},
		{ "class", ColorTheme.Blue},
		{ "compl", ColorTheme.Blue},
		{ "concept", ColorTheme.Blue},
		{ "const", ColorTheme.Blue},
		{ "consteval", ColorTheme.Blue},
		{ "constexpr", ColorTheme.Blue},
		{ "constinit", ColorTheme.Blue},
		{ "const_cast", ColorTheme.Blue},
		{ "continue", ColorTheme.Pink},
		{ "contract_assert", ColorTheme.Blue},
		{ "co_wait", ColorTheme.Pink},
		{ "co_returnn", ColorTheme.Pink},
		{ "co_yield", ColorTheme.Pink},
		{ "decltype", ColorTheme.Blue},
		{ "default", ColorTheme.Pink},
		{ "delete", ColorTheme.Blue},
		{ "do", ColorTheme.Pink},
		{ "double", ColorTheme.Blue},
		{ "dynamic_cast", ColorTheme.Blue},
		{ "else", ColorTheme.Pink},
		{ "enum", ColorTheme.Blue},
		{ "explicit", ColorTheme.Blue},
		{ "export", ColorTheme.Blue},
		{ "extern", ColorTheme.Blue},
		{ "false", ColorTheme.Blue},
		{ "float", ColorTheme.Blue},
		{ "for", ColorTheme.Pink},
		{ "friend", ColorTheme.Blue},
		{ "goto", ColorTheme.Pink},
		{ "if", ColorTheme.Pink},
		{ "inline", ColorTheme.Blue},
		{ "int", ColorTheme.Blue},
		{ "long", ColorTheme.Blue},
		{ "mutable", ColorTheme.Blue},
		{ "namespace", ColorTheme.Blue},
		{ "new", ColorTheme.Blue},
		{ "noexcept", ColorTheme.Blue},
		{ "not", ColorTheme.Blue},
		{ "not_eq", ColorTheme.Blue},
		{ "nullptr", ColorTheme.Blue},
		{ "operator", ColorTheme.Blue},
		{ "or", ColorTheme.Blue},
		{ "or_eq", ColorTheme.Blue},
		{ "private", ColorTheme.Blue},
		{ "protected", ColorTheme.Blue},
		{ "public", ColorTheme.Blue},
		{ "reflexpr", ColorTheme.Blue},
		{ "register", ColorTheme.Blue},
		{ "reinterpret_cast", ColorTheme.Blue},
		{ "requires", ColorTheme.Blue},
		{ "return", ColorTheme.Pink},
		{ "short", ColorTheme.Blue},
		{ "signed", ColorTheme.Blue},
		{ "sizeof", ColorTheme.Blue},
		{ "static", ColorTheme.Blue},
		{ "static_assert", ColorTheme.Blue},
		{ "static_cast", ColorTheme.Blue},
		{ "struct", ColorTheme.Blue},
		{ "switch", ColorTheme.Pink},
		{ "synchronized", ColorTheme.Blue},
		{ "template", ColorTheme.Blue},
		{ "this", ColorTheme.Blue},
		{ "thread_local", ColorTheme.Blue},
		{ "throw", ColorTheme.Pink},
		{ "true", ColorTheme.Blue},
		{ "try", ColorTheme.Pink},
		{ "typedef", ColorTheme.Blue},
		{ "typeid", ColorTheme.Blue},
		{ "typename", ColorTheme.Blue},
		{ "union", ColorTheme.Blue},
		{ "unsigned", ColorTheme.Blue},
		{ "using", ColorTheme.Blue},
		{ "virtual", ColorTheme.Blue},
		{ "void", ColorTheme.Blue},
		{ "volatile", ColorTheme.Blue},
		{ "wchar_t", ColorTheme.Blue},
		{ "while", ColorTheme.Pink},
		{ "xor", ColorTheme.Blue},
		{ "xor_eq", ColorTheme.Blue}
	};
	private System.Collections.Generic.Dictionary<int, List<ColorRange>> _lineColorCache = new();
	private System.Collections.Generic.Dictionary<int, List<ColorRange>> _lineColorQueue = new();

	Regex _keywordRegex = new Regex(@"\b(" + string.Join("|", KeywordColorsInternal.Keys.Select(Regex.Escape)) + @")\b", RegexOptions.Compiled);

	public override Dictionary _GetLineSyntaxHighlighting(int line)
	{
		Dictionary dict = new Dictionary();
		if (_lineColorCache.TryGetValue(line, out var ranges))
		{
			// godot requires the nested dicts to be sorted starting from the lowest value
			ranges.Sort((a, b) => a.ColumnStart.CompareTo(b.ColumnStart));
			foreach (var range in ranges)
			{
				dict[range.ColumnStart] = new Dictionary { ["color"] = range.Color };
				if (range.ColumnStart < range.ColumnEnd && range.ColumnEnd != 0)
				{
					dict[range.ColumnEnd] = new Dictionary { ["color"] = ColorTheme.Default };
				}
			}
		}
		return dict;
	}

	public override void _ClearHighlightingCache()
	{
		_lineColorCache.Clear();
	}

	//public override void _UpdateCache()
	//{
	//	GD.Print("update cache");
	//	foreach (var kvp in _lineColorQueue)
	//	{
	//		_lineColorCache[kvp.Key] = kvp.Value;
	//	}
	//	_lineColorQueue.Clear();
	//}


	public void ColorKeywords()
	{
		bool inMultilineComment = false;
		for (int i = 0; i < GetTextEdit().GetLineCount(); i++)
		{
			string line = GetTextEdit().GetLine(i);
			var lineCommentIdx = line.IndexOf("//");
			var multilineCommentStartIdx = line.IndexOf("/*");
			if (multilineCommentStartIdx != -1 || inMultilineComment)
			{
				inMultilineComment = true;
				var multilineCommentEndIdx = line.IndexOf("*/");
				if (multilineCommentEndIdx != -1)
				{
					AddColorRange(i, 0, multilineCommentEndIdx + 2, ColorTheme.Comment);
					inMultilineComment = false;
				}
				else
				{
					AddColorRange(i, multilineCommentStartIdx, 0, ColorTheme.Comment);
					continue;
				}
			}
			else if (lineCommentIdx != -1)
			{
				AddColorRange(i, lineCommentIdx, 0, ColorTheme.Comment);
			}

			foreach (Match match in _keywordRegex.Matches(line))
			{
				var name = match.Value;
				if (!KeywordColorsInternal.TryGetValue(name, out var color))
				{
					continue;
				}
				
				if (lineCommentIdx != -1 && lineCommentIdx <= match.Index)
				{
					break;
				}

				AddColorRange(i, match.Index, match.Index + name.Length, color);
			}
		}
	}

	public void AddColorRange(int line, int columnStart, int columnEnd, Color color)
	{
		if (!_lineColorCache.TryGetValue(line, out var ranges))
		{
			ranges = new List<ColorRange>();
			_lineColorCache[line] = ranges;
		}

		ranges.Add(new ColorRange
		{
			ColumnStart = columnStart,
			ColumnEnd = columnEnd,
			Color = color
		});
	}

	public static Color TokenTypeToColor(string tokenType)
	{
		return tokenType switch
		{
			"function" => ColorTheme.Function,
			"method" => ColorTheme.Function,
			"parameter" => ColorTheme.Parameter,
			//"variable" => ColorTheme.LocalVariable,
			"macro" => ColorTheme.Macro,
			"type" => ColorTheme.Type,
			"class" => ColorTheme.Class,
			"enum" => ColorTheme.Enum,
			"enumMember" => ColorTheme.EnumMember,
			"comment" => ColorTheme.Comment,
			_ => Colors.Crimson
		};
	}
}
