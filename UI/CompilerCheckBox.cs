using Godot;
using System;
using DecompMeDesktop.Core;
using DecompMeDesktop.Core.Compilers;


public partial class CompilerCheckBox : CheckBox
{
	private Compilers.ICompiler _compiler;
	public Compilers.ICompiler Compiler 
	{ 
		get
		{
			return _compiler; 
		}
		set
		{
			Text = value.Version;
			TooltipText = value.DownloadUrl;
			_compiler = value;
		}
	}
	// Called when the node enters the scene tree for the first time.

	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
