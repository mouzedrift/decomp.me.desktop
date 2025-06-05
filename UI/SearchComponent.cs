using DecompMeDesktop.Core;
using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using static DecompMeDesktop.Core.DecompMeApi;
using static Godot.HttpRequest;

namespace DecompMeDesktop.UI;

public partial class SearchComponent : LineEdit
{
	private class SearchItem(string type, string data)
	{
		public string Type { get; set; } = type;
		public string Data { get; set; } = data;
	}

	private readonly PackedScene SCRATCH_PAGE = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/ScratchPage.tscn");

	private PopupMenu _popupMenu;
	private Timer _searchTimer;
	private Dictionary<int, SearchItem> _searchResults = [];
	private bool _mouseOnSearchBar;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_popupMenu = GetNode<PopupMenu>("PopupMenu");
		_searchTimer = GetNode<Timer>("SearchTimer");

		TextChanged += (string newText) =>
		{
			if (!string.IsNullOrWhiteSpace(newText))
			{
				_searchTimer.Start();
			}
		};

		_searchTimer.Timeout += async () =>
		{
			var result = await DecompMeApi.RequestSearchAsync(this, Text);
			ClearAllItems();
			if (result.Count > 0)
			{
				PopulateSearchMenu(result);
			}
			else
			{
				_popupMenu.AddItem("No search results");
			}

			if (!_popupMenu.Visible)
			{
				_popupMenu.Show();
			}
			PositionSearchMenu();
		};

		_popupMenu.IndexPressed += OnIndexPressed;
		_popupMenu.WindowInput += OnWindowInput;

		MouseExited += () => _mouseOnSearchBar = false;
		MouseEntered += () => _mouseOnSearchBar = true;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left && !_mouseOnSearchBar)
			{
				ReleaseFocus();
			}
		}
	}

	// the LineEdit can no longer be focused and receives no inputs when the window is focused so instead we'll update the text here.
	// annoying but the focus methods and properties dont do anything
	private void OnWindowInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			if (keyEvent.Keycode == Key.Down || keyEvent.Keycode == Key.Up)
			{
				return;
			}

			char typedChar = (char)keyEvent.Unicode;
			if (keyEvent.Keycode == Key.Backspace)
			{
				if (GetSelectedText().Length > 1)
				{
					Text = Text.Replace(GetSelectedText(), "");
					EmitSignalTextChanged(Text);
				}
				else
				{
					DeleteCharAtCaret();
				}
			}
			else if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.A)
			{
				SelectAll();
			}
			else if (keyEvent.Keycode == Key.Left)
			{
				CaretColumn = Mathf.Max(0, CaretColumn - 1);
			}
			else if (keyEvent.Keycode == Key.Right)
			{
				CaretColumn = Mathf.Min(Text.Length, CaretColumn + 1);
			}
			else
			{
				InsertTextAtCaret(typedChar.ToString());
				EmitSignalTextChanged(Text);
			}
		}
	}

	private void PopulateSearchMenu(List<DecompMeApi.SearchResult> results)
	{
		for (int i = 0; i < results.Count; i++)
		{
			var result = results[i];
			string json = string.Empty;
			if (result.item is JsonElement element)
			{
				json = element.GetRawText();
			}

			if (result.type == "scratch")
			{
				var scratch = JsonSerializer.Deserialize<ScratchListItem>(json);
				_popupMenu.AddIconItem(Utils.GetPlatformIcon(scratch.platform), $"{scratch.name} {Utils.GetMatchPercentage(scratch)}", i);
				_searchResults.Add(i, new SearchItem(result.type, scratch.slug));
			}
			else if (result.type == "user")
			{
				var user = JsonSerializer.Deserialize<User>(json);
				_popupMenu.AddIconItem(null, user.username, i); // TODO: use user avatar as icon
				_searchResults.Add(i, new SearchItem(result.type, user.username));
			}
			else if (result.type == "preset")
			{
				var preset = JsonSerializer.Deserialize<PresetListItem>(json);
				_popupMenu.AddIconItem(Utils.GetPlatformIcon(preset.platform), $"{preset.name} {preset.num_scratches} scratches", i);
				_searchResults.Add(i, new SearchItem(result.type, preset.name));
			}
			else
			{
				throw new NotSupportedException($"Search result type {result.type} is not supported!");
			}

		}
	}

	private async void OnIndexPressed(long index)
	{
		if (_searchResults.TryGetValue((int)index, out SearchItem item))
		{
			if (item.Type == "scratch")
			{
				var request = await DecompMeApi.RequestScratchAsync(this, item.Data, ScratchRequestType.Slug);
				var scratchPage = SCRATCH_PAGE.Instantiate<ScratchPage>();
				scratchPage.Init(request);
				SceneManager.Instance.ChangeScene(scratchPage);
			}
			else
			{
				throw new NotSupportedException($"Redirecting to {item.Type} is not supported (yet)!");
			}
		}
	}

	private void PositionSearchMenu()
	{
		_popupMenu.Size = new Vector2I((int)Size.X, 0);
		_popupMenu.Position = new Vector2I(GetWindow().Position.X + (int)GlobalPosition.X, GetWindow().Position.Y + Mathf.CeilToInt(Size.Y));
	}

	private void ClearAllItems()
	{
		_searchResults.Clear();
		_popupMenu.Clear();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
