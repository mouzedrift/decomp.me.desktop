using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DecompMeDesktop.UI;

public partial class PlatformsScroller : Control
{
	private readonly Vector2I TargetTextureSize = new Vector2I(128, 128);
	private readonly Vector2I TargetSpacerSize = new Vector2I(48, 48);

	private readonly List<string> _platformPaths = new List<string>()
	{
		{"res://Assets/Images/Platforms/Png/msdos.png"},
		{"res://Assets/Images/Platforms/Png/irix.png"},
		{"res://Assets/Images/Platforms/Png/win32.png"},
		{"res://Assets/Images/Platforms/Png/macosx.png"},
		{"res://Assets/Images/Platforms/Png/n64.png"},
		{"res://Assets/Images/Platforms/Png/gba.png"},
		{"res://Assets/Images/Platforms/Png/gc_wii.png"},
		{"res://Assets/Images/Platforms/Png/nds.png"},
		{"res://Assets/Images/Platforms/Png/ps1.png"},
		{"res://Assets/Images/Platforms/Png/ps2.png"},
		{"res://Assets/Images/Platforms/Png/psp.png"},
		{"res://Assets/Images/Platforms/Png/n3ds.png"},
		{"res://Assets/Images/Platforms/Png/switch.png"},
		{"res://Assets/Images/Platforms/Png/saturn.png"},
		{"res://Assets/Images/Platforms/Png/dreamcast.png"}
	};

	[Export] private HBoxContainer _hboxContainer;
	[Export] private SubViewport _subViewport;
	[Export] private TextureRect _renderTarget;

	private int _newImageIdx = 0;
	private bool _init = false;
	private Vector2I _targetViewportSize = Vector2I.Zero;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		CustomMinimumSize = TargetTextureSize;

		InsertAllImages();
		
		RenderingServer.FramePostDraw += () =>
		{
			if (!_init)
			{
				var targetSize = (Vector2I)_hboxContainer.Size;
				_subViewport.Size = targetSize;

				_renderTarget.Texture = _subViewport.GetTexture();

				_init = true;
			}
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void InsertAllImages()
	{
		for (int i = 0; i < _platformPaths.Count; i++)
		{
			AddNextImage();
			AddSpacer();
		}
	}

	private void AddNextImage()
	{
		var imagePath = _platformPaths[_newImageIdx];
		_newImageIdx = (_newImageIdx + 1) % _platformPaths.Count;

		TextureRect texRect = new TextureRect();
		texRect.Texture = ResourceLoader.Load<Texture2D>(imagePath);
		texRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		texRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		texRect.CustomMinimumSize = TargetTextureSize;

		_targetViewportSize.X += TargetTextureSize.X;
		_targetViewportSize.Y = TargetTextureSize.Y;

		_hboxContainer.AddChild(texRect);
	}

	private void AddSpacer()
	{
		Control spacer = new Control();
		_hboxContainer.AddChild(spacer);
		spacer.CustomMinimumSize = TargetSpacerSize;
		_subViewport.Size += new Vector2I(TargetSpacerSize.X, 0);
	}
}
