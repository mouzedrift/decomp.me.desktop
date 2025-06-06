using DecompMeDesktop.UI;
using Godot;

namespace DecompMeDesktop.Core;

public partial class SceneManager : Node
{
	private static readonly PackedScene ScratchListPage = ResourceLoader.Load<PackedScene>("uid://dgrhdqs4p8wr3");
	private static readonly PackedScene SettingsPage = ResourceLoader.Load<PackedScene>("uid://dq8qy1aik7870");
	private static readonly PackedScene ScratchPage = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/ScratchPage.tscn");

	public static SceneManager Instance { get; private set; }
	[Signal] public delegate void PostSceneChangeEventHandler();
	[Signal] public delegate void PreSceneChangeEventHandler();

	private Node _nextScene;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void ChangeScene(Node scene)
	{
		EmitSignal(SignalName.PreSceneChange);
		_nextScene = scene;
		CallDeferred(MethodName.DeleteCurrentScene);
	}

	private void DeleteCurrentScene()
	{
		GetTree().CurrentScene.Free();
		GetTree().Root.AddChild(_nextScene);
		GetTree().CurrentScene = _nextScene;

		_nextScene = null;
		EmitSignal(SignalName.PostSceneChange);
	}

	public static void GotoHomepage()
	{
		Instance.ChangeScene(ScratchListPage.Instantiate());
	}

	public static void GotoSettings()
	{
		Instance.ChangeScene(SettingsPage.Instantiate());
	}

	public static void GotoScratchPage(DecompMeApi.ScratchListItem scratch)
	{
		var scratchPage = ScratchPage.Instantiate<ScratchPage>();
		scratchPage.Init(scratch);
		Instance.ChangeScene(scratchPage);
	}
}
