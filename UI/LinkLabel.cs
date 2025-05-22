using Godot;
using System;

namespace DecompMeDesktop.UI;

public partial class LinkLabel : Label
{
	[Signal] public delegate void PressedEventHandler();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		LabelSettings = new LabelSettings()
		{
			FontSize = 14,
			OutlineSize = 1
		};
		MouseDefaultCursorShape = CursorShape.PointingHand;
		MouseFilter = MouseFilterEnum.Stop;

		MouseEntered += () => SelfModulate = new Color("#58a6ff");
		MouseExited += () => SelfModulate = Colors.White;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb)
		{
			if (mb.ButtonIndex == MouseButton.Left && mb.Pressed)
			{
				EmitSignal(SignalName.Pressed);
			}
		}
	}
}
