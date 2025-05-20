using Godot;
using System;
using DecompMeDesktop.Core.CodeEditor;

namespace DecompMeDesktop.UI;

public partial class CompilerOutputPanel : TabContainer
{
	private TextEdit _errorsTextEdit;
	private TextEdit _compilerOutputTextEdit;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_errorsTextEdit = GetNode<TextEdit>("Errors/ErrorsTextEdit");
		_compilerOutputTextEdit = GetNode<TextEdit>("Compiler Output/CompilerOutputTextEdit");
		_compilerOutputTextEdit.SyntaxHighlighter = new CompilerOutputHighlighter();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void SetCompilerOutput(string text)
	{
		_compilerOutputTextEdit.Text = text;
	}

}
