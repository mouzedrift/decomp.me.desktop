using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public partial class PlatformsScroller : ScrollContainer
{
	private readonly List<string> _platformPaths = new List<string>()
	{
		{"res://Images/Platforms/msdos.svg"},
		{"res://Images/Platforms/irix.svg"},
		{"res://Images/Platforms/win32.svg"},
		{"res://Images/Platforms/macosx.svg"},
		{"res://Images/Platforms/n64.svg"},
		{"res://Images/Platforms/gba.svg"},
		{"res://Images/Platforms/gc_wii.svg"},
		{"res://Images/Platforms/nds.svg"},
		{"res://Images/Platforms/ps1.svg"},
		{"res://Images/Platforms/ps2.svg"},
		{"res://Images/Platforms/psp.svg"},
		{"res://Images/Platforms/n3ds.svg"},
		{"res://Images/Platforms/switch.svg"},
		{"res://Images/Platforms/saturn.svg"},
		{"res://Images/Platforms/dreamcast.svg"}
	};

	[Export] private HBoxContainer _hboxContainer;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ScrollHorizontal = (int)GetHScrollBar().MaxValue;
		InsertImages();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		ScrollHorizontal += (int)(-250f * delta);
		if (ScrollHorizontal <= 0)
		{
			ScrollHorizontal = (int)GetHScrollBar().MaxValue;
		}
	}

	private void InsertImages()
	{
		foreach (var imagePath in _platformPaths)
		{
			TextureRect texRect = new TextureRect();
			texRect.Texture = ResourceLoader.Load<Texture2D>(imagePath);
			texRect.ExpandMode = TextureRect.ExpandModeEnum.KeepSize;
			texRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
			texRect.SetSize(new Vector2(48, 48));
			texRect.CustomMinimumSize = new Vector2(48, 48);
			texRect.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
			texRect.SizeFlagsVertical = SizeFlags.ShrinkCenter;
			_hboxContainer.AddChild(texRect);
		}
	}
}
