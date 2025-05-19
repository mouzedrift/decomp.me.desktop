using Godot;
using Godot.Collections;
using System.Text.RegularExpressions;

namespace DecompMeDesktop.Core.CodeEditor;

public partial class CppHighlighter : CodeHighlighter
{
	// Visual Studio 2022 Dark Theme Colors
	private static readonly Color BlueColor = Color.FromHtml("#569CD6");
	private static readonly Color TypeColor = Color.FromHtml("#4EC9B0");
	private static readonly Color PreprocessorColor = new Color(0.8f, 0.8f, 0.8f);    // Light gray
	private static readonly Color PinkColor = new Color("#C586C0");

	private Godot.Collections.Dictionary<string, Color> KeywordColorsInternal = new Godot.Collections.Dictionary<string, Color>
	{
		{ "alignas", BlueColor},
		{ "and", BlueColor},
		{ "and_eq", BlueColor},
		{ "asm", BlueColor},
		{ "atomic_cancel", BlueColor},
		{ "atomic_commit", BlueColor},
		{ "atomic_noexcept", BlueColor},
		{ "auto", BlueColor},
		{ "bitand", BlueColor},
		{ "bitor", BlueColor},
		{ "bool", BlueColor},
		{ "break", PinkColor},
		{ "case", PinkColor},
		{ "catch", PinkColor},
		{ "char", BlueColor},
		{ "char8_t", BlueColor},
		{ "char16_t", BlueColor},
		{ "char32_t", BlueColor},
		{ "class", BlueColor},
		{ "compl", BlueColor},
		{ "concept", BlueColor},
		{ "const", BlueColor},
		{ "consteval", BlueColor},
		{ "constexpr", BlueColor},
		{ "constinit", BlueColor},
		{ "const_cast", BlueColor},
		{ "continue", PinkColor},
		{ "contract_assert", BlueColor},
		{ "co_wait", PinkColor},
		{ "co_returnn", PinkColor},
		{ "co_yield", PinkColor},
		{ "decltype", BlueColor},
		{ "default", PinkColor},
		{ "delete", BlueColor},
		{ "do", PinkColor},
		{ "double", BlueColor},
		{ "dynamic_cast", BlueColor},
		{ "else", PinkColor},
		{ "enum", BlueColor},
		{ "explicit", BlueColor},
		{ "export", BlueColor},
		{ "extern", BlueColor},
		{ "false", BlueColor},
		{ "float", BlueColor},
		{ "for", PinkColor},
		{ "friend", BlueColor},
		{ "goto", PinkColor},
		{ "if", PinkColor},
		{ "inline", BlueColor},
		{ "int", BlueColor},
		{ "long", BlueColor},
		{ "mutable", BlueColor},
		{ "namespace", BlueColor},
		{ "new", BlueColor},
		{ "noexcept", BlueColor},
		{ "not", BlueColor},
		{ "not_eq", BlueColor},
		{ "nullptr", BlueColor},
		{ "operator", BlueColor},
		{ "or", BlueColor},
		{ "or_eq", BlueColor},
		{ "private", BlueColor},
		{ "protected", BlueColor},
		{ "public", BlueColor},
		{ "reflexpr", BlueColor},
		{ "register", BlueColor},
		{ "reinterpret_cast", BlueColor},
		{ "requires", BlueColor},
		{ "return", PinkColor},
		{ "short", BlueColor},
		{ "signed", BlueColor},
		{ "sizeof", BlueColor},
		{ "static", BlueColor},
		{ "static_assert", BlueColor},
		{ "static_cast", BlueColor},
		{ "struct", BlueColor},
		{ "switch", PinkColor},
		{ "synchronized", BlueColor},
		{ "template", BlueColor},
		{ "this", BlueColor},
		{ "thread_local", BlueColor},
		{ "throw", PinkColor},
		{ "true", BlueColor},
		{ "try", PinkColor},
		{ "typedef", BlueColor},
		{ "typeid", BlueColor},
		{ "typename", BlueColor},
		{ "union", BlueColor},
		{ "unsigned", BlueColor},
		{ "using", BlueColor},
		{ "virtual", BlueColor},
		{ "void", BlueColor},
		{ "volatile", BlueColor},
		{ "wchar_t", BlueColor},
		{ "while", PinkColor},
		{ "xor", BlueColor},
		{ "xor_eq", BlueColor}
	}; 
	
	public CppHighlighter()
	{
		SymbolColor = Color.FromHtml("#D4D4D4");
		NumberColor = Color.FromHtml("#B5CEA8");
		FunctionColor = Color.FromHtml("#DCDCAA");
		MemberVariableColor = Color.FromHtml("#9CDCFE");

		ColorRegions["//"] = Color.FromHtml("#6A9955");
		AddColorRegion("\"", "\"", Color.FromHtml("#CE9178"));
		AddColorRegion("/*", "*/", Color.FromHtml("#6A9955"));
		AddColorRegion("//", "", Color.FromHtml("#6A9955"));

		KeywordColors = [];
		foreach (var keyword in KeywordColorsInternal)
		{
			KeywordColors[keyword.Key] = keyword.Value;
		}
	}
}
