using Godot;
using System;

public partial class ScratchListPage : Node
{
	private readonly PackedScene SCRATCH_CARD = ResourceLoader.Load<PackedScene>("res://Scenes/ScratchCard.tscn");

	private VBoxContainer _scratchCardContainer;
	private Button _showMoreButton;

	private DecompMeApi.ScratchList _latestScratchList;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_scratchCardContainer = GetNode<VBoxContainer>("ScrollContainer/MarginContainer/VBoxContainer");
		_showMoreButton = GetNode<Button>("ScrollContainer/MarginContainer/VBoxContainer/ShowMoreButton");

		_showMoreButton.Visible = false;

		DecompMeApi.Instance.RequestScratchList();
		DecompMeApi.Instance.DataReceived += (Variant type) =>
		{
			if (DecompMeApi.IsType(DecompMeApi.RequestType.ScratchList, type))
			{
				Populate(DecompMeApi.Instance.GetData<DecompMeApi.ScratchList>());
			}
		};

		_showMoreButton.Pressed += NextPage;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void Populate(DecompMeApi.ScratchList scratchList)
	{
		foreach (var scratch in scratchList.results)
		{
			var card = SCRATCH_CARD.Instantiate<ScratchCard>();
			card.SetFunctionName(scratch.name, scratch.slug);
			card.SetPresetName(scratch.preset.ToString()); // TODO
			card.SetUsername(scratch.owner != null ? scratch.owner.username : "No Owner");
			card.SetTimestamp(scratch.last_updated);
			card.SetMatchPercentage(scratch.GetMatchPercentage());

			_scratchCardContainer.AddChild(card);
		}

		_showMoreButton.Visible = true;
		_scratchCardContainer.MoveChild(_showMoreButton, -1);
		_latestScratchList = scratchList;
	}

	private void NextPage()
	{
		DecompMeApi.Instance.Request(_latestScratchList.next);
	}
}
