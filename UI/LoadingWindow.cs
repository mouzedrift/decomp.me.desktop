using Godot;
using System;
using System.Threading.Tasks;

public partial class LoadingWindow : Window
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public async Task ExecuteAsync(Node parent, string title, Func<Task> work)
	{
		bool isInTree = false;
		foreach (var child in parent.GetChildren())
		{
			if (child == this)
			{
				isInTree = true;
			}
		}

		if (!isInTree)
		{
			parent.AddChild(this);
		}

		Title = title;
		await work();
	}
}
