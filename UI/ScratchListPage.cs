using Godot;
using DecompMeDesktop.Core;
using System.Threading.Tasks;
using System.Text.Json;

namespace DecompMeDesktop.UI;
public partial class ScratchListPage : Node
{
	private readonly PackedScene SCRATCH_CARD = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/ScratchCard.tscn");

	private VBoxContainer _scratchCardContainer;
	private Button _showMoreButton;
	private Label _scratchCountLabel;
	private Label _githubUserCountLabel;
	private Label _asmCountLabel;
	private Timer _statsUpdateTimer;
	private VBoxContainer _yourScratchesHBox;

	private DecompMeApi.ScratchList _latestScratchList;
	
	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		_scratchCardContainer = GetNode<VBoxContainer>("ScrollContainer/VBoxContainer/VBoxContainer/MarginContainer2/HBoxContainer2/HBoxContainer2/ScratchCards");
		_showMoreButton = GetNode<Button>("ScrollContainer/VBoxContainer/VBoxContainer/MarginContainer2/HBoxContainer2/HBoxContainer2/ScratchCards/ShowMoreButton");
		_scratchCountLabel = GetNode<Label>("ScrollContainer/VBoxContainer/VBoxContainer/HBoxContainer/ScratchCountLabel");
		_githubUserCountLabel = GetNode<Label>("ScrollContainer/VBoxContainer/VBoxContainer/HBoxContainer/GitHubUserCountLabel");
		_asmCountLabel = GetNode<Label>("ScrollContainer/VBoxContainer/VBoxContainer/HBoxContainer/AsmCountLabel");
		_statsUpdateTimer = GetNode<Timer>("StatsUpdateTimer");
		_yourScratchesHBox = GetNode<VBoxContainer>("ScrollContainer/VBoxContainer/VBoxContainer/MarginContainer2/HBoxContainer2/HBoxContainer2/MarginContainer/YourScratches");

		_showMoreButton.Visible = false;

		if (DecompMeApi.CurrentUser == null)
		{
			var user = await DecompMeApi.RequestUserAsync(this);
			DecompMeApi.CurrentUser = user;
		}

		GD.Print($"Logged in as {DecompMeApi.CurrentUser.username}, anon={DecompMeApi.CurrentUser.is_anonymous}");
		GD.Print(JsonSerializer.Serialize(DecompMeApi.CurrentUser));
		var scratchList = await DecompMeApi.RequestScratchListAsync(this);
		await PopulateScratchCardsAsync(scratchList);

		if (DecompMeApi.CurrentUser != null)
		{
			var userScratches = await DecompMeApi.RequestUserScratchesAsync(this, DecompMeApi.CurrentUser);
			PopulateYourScratches(userScratches);
		}

		await RequestStatsAsync();
		_statsUpdateTimer.Timeout += async () =>
		{
			await RequestStatsAsync();
		};

		_showMoreButton.Pressed += async () =>
		{
			await NextPageAsync();
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void PopulateYourScratches(DecompMeApi.ScratchList scratches)
	{
		foreach (var scratch in scratches.results)
		{
			var yourScratchesItem = ResourceLoader.Load<PackedScene>("uid://ccu3o4odbtxox").Instantiate<YourScratchesTextItem>();
			yourScratchesItem.SetFunctionName(scratch.name);
			yourScratchesItem.SetMatchPercentage(Utils.GetMatchPercentage(scratch.score, scratch.max_score));
			yourScratchesItem.LinkLabelPressed += async ()  =>
			{
				var url = $"{DecompMeApi.ApiUrl}/scratch/{scratch.slug}";
				var result = await DecompMeApi.RequestScratchAsync(this, url);
				SceneManager.GotoScratchPage(result);
			};
			_yourScratchesHBox.AddChild(yourScratchesItem);
		}
	}

	private async Task PopulateScratchCardsAsync(DecompMeApi.ScratchList scratchList)
	{
		foreach (var scratch in scratchList.results)
		{
			var card = SCRATCH_CARD.Instantiate<ScratchCard>();
			if (scratch.preset == null)
			{
				// TODO: use the compiler translation instead https://github.com/decompme/decomp.me/blob/aaf1eb94e7160b33b44bd7f24765992f09e16798/frontend/src/lib/i18n/locales/en/compilers.json
				// additionally clicking the name should bring you to the preset page
				card.SetPresetName(scratch.compiler); 
			}
			else if (GlobalCache.TryGetPresetName(scratch.preset.Value, out string presetName))
			{
				card.SetPresetName(presetName);
			}
			else
			{
				var presetRequest = await DecompMeApi.RequestPresetNameAsync(this, scratch.preset.Value);
				card.SetPresetName(presetRequest.name);
				GlobalCache.AddPresetName(scratch.preset.Value, presetRequest.name);
			}

			card.SetPlatformImage(scratch.platform);
			card.SetFunctionName(scratch.name, scratch.slug);
			card.SetUsername(scratch.GetOwnerName());
			card.SetTimestamp(scratch.last_updated);
			card.SetMatchPercentage(Utils.GetMatchPercentage(scratch.score, scratch.max_score));

			_scratchCardContainer.AddChild(card);
			_scratchCardContainer.MoveChild(_showMoreButton, -1);
		}

		_showMoreButton.Visible = true;
		_latestScratchList = scratchList;
	}

	private void PopulateStats(DecompMeApi.Stats stats)
	{
		_scratchCountLabel.Text = $"{stats.scratch_count:N0} scratches created";
		_githubUserCountLabel.Text = $"{stats.github_user_count:N0} users signed up";
		_asmCountLabel.Text = $"{stats.asm_count:N0} asm globs submitted";
	}

	private async Task NextPageAsync()
	{
		var scratchList = await DecompMeApi.RequestNextScratchListAsync(this, _latestScratchList.next);
		await PopulateScratchCardsAsync(scratchList);
	}

	private async Task RequestStatsAsync()
	{
		// TODO: check if a request is still going...
		var stats = await DecompMeApi.RequestStatsAsync(this);
		PopulateStats(stats);
	}
}
