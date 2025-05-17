using Godot;
using System;

public partial class ScratchCard : PanelContainer
{
	[Export] private RichTextLabel _functionNameLabel;
	public void SetPlatformImage(Texture2D texture) => GetNode<TextureRect>("MarginContainer/VBoxContainer/HBoxContainer/PlatformTextureRect").Texture = texture;
	public void SetFunctionName(string name, string scratchSlug = "")
	{
		if (scratchSlug != "")
		{
			_functionNameLabel.Text = $"[b][url=https://decomp.me/api/scratch/{scratchSlug}]{name}[/url][/b]";
		}
		else
		{
			_functionNameLabel.Text = name;
		}
	}
	public void SetPresetName(string name) => GetNode<Label>("MarginContainer/VBoxContainer/HBoxContainer2/PresetNameLabel").Text = name;
	public void SetMatchPercentage(string percent) => GetNode<Label>("MarginContainer/VBoxContainer/HBoxContainer2/MatchPercentageLabel").Text = percent;
	public void SetUsername(string username) => GetNode<Label>("MarginContainer/VBoxContainer/HBoxContainer/UsernameLabel").Text = username;
	public void SetUserAvatar(Texture2D texture) => GetNode<TextureRect>("MarginContainer/VBoxContainer/HBoxContainer/UserAvatarTextureRect").Texture = texture;
	public void SetTimestamp(string timestamp)
	{
		var timeSpan = DateTime.Now - DateTime.Parse(timestamp);
		GetNode<Label>("MarginContainer/VBoxContainer/HBoxContainer2/TimestampLabel").Text = Utils.FormatRelativeTime(timeSpan);
	}

	private readonly PackedScene SCRATCH_PAGE = ResourceLoader.Load<PackedScene>("res://Scenes/ScratchPage.tscn");


	// Called when the node enters the scene tree for the first time.

	public override void _Ready()
	{
		_functionNameLabel.MetaHoverStarted += (Variant meta) => _functionNameLabel.SelfModulate = new Color("#58a6ff");
		_functionNameLabel.MetaHoverEnded += (Variant meta) => _functionNameLabel.SelfModulate = Colors.White;

		_functionNameLabel.MetaClicked += (Variant meta) =>
		{
			DecompMeApi.Instance.RequestScratch(meta.AsString());
			DecompMeApi.Instance.DataReceived += (Variant requestType) =>
			{
				if (DecompMeApi.IsType(DecompMeApi.RequestType.Scratch, requestType))
				{
					var scratchPage = SCRATCH_PAGE.Instantiate<ScratchPage>();
					scratchPage.Populate(DecompMeApi.Instance.GetData<DecompMeApi.ScratchListItem>());
					SceneManager.Instance.ChangeScene(scratchPage);
				}
			};

			GD.Print($"meta clicked: {meta.AsString()}");
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
