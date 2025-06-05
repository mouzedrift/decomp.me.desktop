using DecompMeDesktop.Core;
using Godot;
using System;

namespace DecompMeDesktop.UI;

public partial class ScratchCard : PanelContainer
{
	[Export] private LinkLabel _functionNameLabel;
	public void SetPlatformImage(string platformName)
	{
		var iconTexture = Utils.GetPlatformIcon(platformName);
		GetNode<TextureRect>("MarginContainer/VBoxContainer/HBoxContainer/MarginContainer/PlatformTextureRect").Texture = iconTexture;
	}

	public void SetFunctionName(string name, string scratchSlug = "")
	{
		_functionNameLabel.Text = name;
		_scratchSlug = scratchSlug;
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

	private readonly PackedScene SCRATCH_PAGE = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/ScratchPage.tscn");
	private string _scratchSlug;

	// Called when the node enters the scene tree for the first time.

	public override void _Ready()
	{
		_functionNameLabel.Pressed += async () =>
		{
			var url = $"{DecompMeApi.ApiUrl}/scratch/{_scratchSlug}";
			var scratch = await DecompMeApi.RequestScratchAsync(this, url);
			var scratchPage = SCRATCH_PAGE.Instantiate<ScratchPage>();
			scratchPage.Init(scratch);
			SceneManager.Instance.ChangeScene(scratchPage);
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
