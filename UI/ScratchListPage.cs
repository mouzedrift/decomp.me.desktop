using Godot;
using DecompMeDesktop.Core;

namespace DecompMeDesktop.UI;
public partial class ScratchListPage : Node
{
	private readonly PackedScene SCRATCH_CARD = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/ScratchCard.tscn");

	private VBoxContainer _scratchCardContainer;
	private Button _showMoreButton;

	private DecompMeApi.ScratchList _latestScratchList;
	private DecompMeApi.ScratchListRequest _request;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_scratchCardContainer = GetNode<VBoxContainer>("ScrollContainer/VBoxContainer");
		_showMoreButton = GetNode<Button>("ScrollContainer/VBoxContainer/ShowMoreButton");

		_showMoreButton.Visible = false;

		_request = DecompMeApi.Instance.RequestScratchList();
		_request.DataReceived += () =>
		{
			Populate(_request.Data);
			_request.QueueFree();
			_request = null;
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
			card.SetUsername(scratch.GetOwnerName());
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
		_request = DecompMeApi.Instance.RequestNextScratchList(_latestScratchList.next);
		_request.DataReceived += () =>
		{
			Populate(_request.Data);
			_request.QueueFree();
			_request = null;
		};
	}
}
