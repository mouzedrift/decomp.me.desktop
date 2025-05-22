using Godot;

namespace DecompMeDesktop.Core;

public partial class SceneManager : Node
{
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
}
