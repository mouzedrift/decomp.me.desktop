using Godot;
using DecompMeDesktop.Core;
using System;
using System.Collections.Generic;
using static DecompMeDesktop.Core.DecompMeApi;
using System.Linq;

namespace DecompMeDesktop.UI;
public partial class ScratchListPage : Node
{
	private readonly PackedScene SCRATCH_CARD = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/ScratchCard.tscn");
	private readonly PackedScene SCRATCH_PAGE = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/ScratchPage.tscn");

	private VBoxContainer _scratchCardContainer;
	private Button _showMoreButton;
	private Label _scratchCountLabel;
	private Label _githubUserCountLabel;
	private Label _asmCountLabel;
	private Timer _statsUpdateTimer;
	private VBoxContainer _yourScratchesHBox;

	private DecompMeApi.ScratchList _latestScratchList;
	private DecompMeApi.JsonRequest<DecompMeApi.ScratchList> _scratchListRequest;
	private DecompMeApi.JsonRequest<DecompMeApi.Stats> _statsRequest;
	private DecompMeApi.JsonRequest<DecompMeApi.ScratchList> _yourScratchesRequest;
	private List<DecompMeApi.PresetName> _presetNameCache = [];
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_scratchCardContainer = GetNode<VBoxContainer>("ScrollContainer/VBoxContainer/VBoxContainer/MarginContainer2/HBoxContainer2/HBoxContainer2/ScratchCards");
		_showMoreButton = GetNode<Button>("ScrollContainer/VBoxContainer/VBoxContainer/MarginContainer2/HBoxContainer2/HBoxContainer2/ScratchCards/ShowMoreButton");
		_scratchCountLabel = GetNode<Label>("ScrollContainer/VBoxContainer/VBoxContainer/HBoxContainer/ScratchCountLabel");
		_githubUserCountLabel = GetNode<Label>("ScrollContainer/VBoxContainer/VBoxContainer/HBoxContainer/GitHubUserCountLabel");
		_asmCountLabel = GetNode<Label>("ScrollContainer/VBoxContainer/VBoxContainer/HBoxContainer/AsmCountLabel");
		_statsUpdateTimer = GetNode<Timer>("StatsUpdateTimer");
		_yourScratchesHBox = GetNode<VBoxContainer>("ScrollContainer/VBoxContainer/VBoxContainer/MarginContainer2/HBoxContainer2/HBoxContainer2/MarginContainer/YourScratches");

		_showMoreButton.Visible = false;

		_scratchListRequest = DecompMeApi.Instance.RequestScratchList();
		_scratchListRequest.DataReceived += () =>
		{
			PopulateScratchCards(_scratchListRequest.Data);
			_scratchListRequest.QueueFree();
			_scratchListRequest = null;
		};

		if (DecompMeApi.Instance.CurrentUser != null)
		{
			_yourScratchesRequest = DecompMeApi.Instance.RequestUserScratches(DecompMeApi.Instance.CurrentUser);
			_yourScratchesRequest.DataReceived += () =>
			{
				PopulateYourScratches();
				_yourScratchesRequest.QueueFree();
				_yourScratchesRequest = null;
			};
		}
		else
		{
			DecompMeApi.Instance.UserReady += () =>
			{
				_yourScratchesRequest = DecompMeApi.Instance.RequestUserScratches(DecompMeApi.Instance.CurrentUser);
				_yourScratchesRequest.DataReceived += () =>
				{
					PopulateYourScratches();
					_yourScratchesRequest.QueueFree();
					_yourScratchesRequest = null;
				};
			};
		}

		RequestStats();
		_statsUpdateTimer.Timeout += RequestStats;

		_showMoreButton.Pressed += NextPage;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void PopulateYourScratches()
	{
		foreach (var scratch in _yourScratchesRequest.Data.results)
		{
			var yourScratchesItem = ResourceLoader.Load<PackedScene>("uid://ccu3o4odbtxox").Instantiate<YourScratchesTextItem>();
			yourScratchesItem.SetFunctionName(scratch.name);
			yourScratchesItem.SetMatchPercentage(Utils.GetMatchPercentage(scratch.score, scratch.max_score));
			yourScratchesItem.LinkLabelPressed += ()  =>
			{
				var url = $"{DecompMeApi.ApiUrl}/scratch/{scratch.slug}";
				var request = DecompMeApi.Instance.RequestScratch(url);
				request.DataReceived += () =>
				{
					var scratchPage = SCRATCH_PAGE.Instantiate<ScratchPage>();
					scratchPage.Init(request.Data);
					SceneManager.Instance.ChangeScene(scratchPage);
					request.QueueFree();
				};
			};
			_yourScratchesHBox.AddChild(yourScratchesItem);
		}
	}

	private void PopulateScratchCards(DecompMeApi.ScratchList scratchList)
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
			else if (!_presetNameCache.Any(p => p.id == scratch.preset))
			{
				var presetRequest = DecompMeApi.Instance.RequestPresetName(scratch.preset.Value);
				presetRequest.DataReceived += () =>
				{
					card.SetPresetName(presetRequest.Data.name);
					_presetNameCache.Add(presetRequest.Data);
					presetRequest.QueueFree();
				};
			}
			else
			{
				var presetName = _presetNameCache.Where(p => p.id == scratch.preset).First().name;
				card.SetPresetName(presetName);
			}

			card.SetPlatformImage(scratch.platform);
			card.SetFunctionName(scratch.name, scratch.slug);
			card.SetUsername(scratch.GetOwnerName());
			card.SetTimestamp(scratch.last_updated);
			card.SetMatchPercentage(Utils.GetMatchPercentage(scratch.score, scratch.max_score));

			_scratchCardContainer.AddChild(card);
		}

		_showMoreButton.Visible = true;
		_scratchCardContainer.MoveChild(_showMoreButton, -1);
		_latestScratchList = scratchList;
	}

	private void PopulateStats(DecompMeApi.Stats stats)
	{
		_scratchCountLabel.Text = $"{stats.scratch_count:N0} scratches created";
		_githubUserCountLabel.Text = $"{stats.github_user_count:N0} users signed up";
		_asmCountLabel.Text = $"{stats.asm_count:N0} asm globs submitted";
	}

	private void NextPage()
	{
		_scratchListRequest = DecompMeApi.Instance.RequestNextScratchList(_latestScratchList.next);
		_scratchListRequest.DataReceived += () =>
		{
			PopulateScratchCards(_scratchListRequest.Data);
			_scratchListRequest.QueueFree();
			_scratchListRequest = null;
		};
	}

	private void RequestStats()
	{
		// the previous stats request still hasn't finished...
		if (_statsRequest != null)
		{
			return;
		}

		_statsRequest = DecompMeApi.Instance.RequestStats();
		_statsRequest.DataReceived += () =>
		{
			PopulateStats(_statsRequest.Data);
			_statsRequest.QueueFree();
			_statsRequest = null;
		};
	}
}
