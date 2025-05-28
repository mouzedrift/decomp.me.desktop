using DecompMeDesktop.Core;
using DecompMeDesktop.Core.CodeEditor;
using Godot;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Godot.HttpRequest;

namespace DecompMeDesktop.UI;

public partial class CppCodeEdit : CodeEdit
{
	class SearchResult
	{
		public int Line { get; set; }
		public int ColumnStart { get; set; }
		public int ColumnEnd { get; set; }
	}
	private string _savedText;
	public enum CodeType
	{
		None,
		Context,
		Source
	}

	[Export] private CodeType _codeType;
	private LineEdit _searchLineEdit;
	private Button _nextButton;
	private Button _previousButton;
	private LineEdit _replaceLineEdit;
	private Button _replaceButton;
	private Button _replaceAllButton;
	private Button _closeButton;
	private CheckBox _matchCaseCheckBox;
	private CheckBox _regexpCheckBox;
	private CheckBox _byWordCheckBox;
	private PanelContainer _searchBoxParent;
	private string _virtualFilePath;
	private CancellationTokenSource _completeToken;
	private int _docVersion = 1;
	private CppHighlighter _cppHighlighter = new CppHighlighter();
	private bool _ready = false;
	private bool _autocompleteKeyPressed = false;
	Vector2I _searchPos;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_searchLineEdit = GetNode<LineEdit>("PanelContainer/MarginContainer/VBoxContainer/HBoxContainer/SearchLineEdit");
		_nextButton = GetNode<Button>("PanelContainer/MarginContainer/VBoxContainer/HBoxContainer/NextButton");
		_previousButton = GetNode<Button>("PanelContainer/MarginContainer/VBoxContainer/HBoxContainer/PreviousButton");
		_replaceLineEdit = GetNode<LineEdit>("PanelContainer/MarginContainer/VBoxContainer/HBoxContainer2/ReplaceLineEdit");
		_replaceButton = GetNode<Button>("PanelContainer/MarginContainer/VBoxContainer/HBoxContainer2/ReplaceButton");
		_replaceAllButton = GetNode<Button>("PanelContainer/MarginContainer/VBoxContainer/HBoxContainer2/ReplaceAllButton");
		_closeButton = GetNode<Button>("PanelContainer/MarginContainer/VBoxContainer/HBoxContainer/CloseButton");
		_matchCaseCheckBox = GetNode<CheckBox>("PanelContainer/MarginContainer/VBoxContainer/HBoxContainer/MatchCaseCheckBox");
		_regexpCheckBox = GetNode<CheckBox>("PanelContainer/MarginContainer/VBoxContainer/HBoxContainer/RegexpCheckBox");
		_byWordCheckBox = GetNode<CheckBox>("PanelContainer/MarginContainer/VBoxContainer/HBoxContainer/ByWordCheckBox");
		_searchBoxParent = GetNode<PanelContainer>("PanelContainer");

		SyntaxHighlighter = _cppHighlighter;
		LinesEditedFrom += OnLinesEditedFrom;

		_nextButton.Pressed += () => SearchNext();
		_previousButton.Pressed += () => SearchPrevious();

		_closeButton.Pressed += _searchBoxParent.Hide;

		_replaceAllButton.Pressed += () =>
		{
			StringBuilder stringBuilder = new StringBuilder(Text);
			stringBuilder.Replace(_searchLineEdit.Text, _replaceLineEdit.Text);
			Text = stringBuilder.ToString();
		};

		_replaceButton.Pressed += () =>
		{
			var result = SearchNext();
			if (result != null)
			{
				var oldLineStr = GetLine(result.Line);
				var newLineStr = oldLineStr.Remove(result.ColumnStart, result.ColumnEnd - result.ColumnStart).Insert(result.ColumnStart, _replaceLineEdit.Text);
				SetLine(result.Line, newLineStr);
			}
		};

		_searchLineEdit.TextChanged += (string text) =>
		{
			var searchText = _searchLineEdit.Text;
			var result = Search(searchText, GetSearchFlags(), 0, 0);
			if (result.X == -1 || result.Y == -1)
			{
				return;
			}

			var searchResult = new SearchResult
			{
				Line = result.Y,
				ColumnStart = result.X,
				ColumnEnd = result.X + searchText.Length
			};
			SelectSearchResult(searchResult);
		};
	}


	private void SelectSearchResult(SearchResult searchResult)
	{
		ScrollVertical = GetScrollPosForLine(searchResult.Line);
		Select(searchResult.Line, searchResult.ColumnStart, searchResult.Line, searchResult.ColumnEnd);
	}

	private SearchResult SearchPrevious()
	{
		var searchText = _searchLineEdit.Text;
		if (searchText.Length <= 0)
		{
			return null;
		}

		var result = Search(searchText, GetSearchFlags(true), _searchPos.Y, _searchPos.X);
		if (result.X == -1 || result.Y == -1)
		{
			return null;
		}

		GD.Print($"found {searchText} at line {result.Y} column {result.X}");
		if (result.X - searchText.Length >= 0)
		{
			_searchPos = new Vector2I(result.X - searchText.Length, result.Y);
		}
		else if (result.Y - 1 >= 0)
		{
			_searchPos = new Vector2I(result.X, result.Y - 1);
		}

		var searchResult = new SearchResult
		{
			Line = result.Y,
			ColumnStart = result.X,
			ColumnEnd = result.X + searchText.Length
		};
		SelectSearchResult(searchResult);
		return searchResult;
	}

	private SearchResult SearchNext()
	{
		var searchText = _searchLineEdit.Text;
		if (searchText.Length <= 0)
		{
			return null;
		}

		var result = Search(searchText, GetSearchFlags(), _searchPos.Y, _searchPos.X);
		if (result.X == -1 || result.Y == -11)
		{
			return null;
		}

		GD.Print($"found {searchText} at line {result.Y} column {result.X}");
		if (result.X < result.X + searchText.Length)
		{
			_searchPos = new Vector2I(result.X + searchText.Length, result.Y);
		}
		else if (result.Y + 1 < GetLineCount())
		{
			_searchPos = new Vector2I(result.X, result.Y + 1);
		}
		ScrollVertical = GetScrollPosForLine(result.Y);
		Select(result.Y, result.X, result.Y, result.X + searchText.Length);

		var searchResult = new SearchResult
		{
			Line = result.Y,
			ColumnStart = result.X,
			ColumnEnd = result.X + searchText.Length
		};
		SelectSearchResult(searchResult);
		return searchResult;
	}

	private uint GetSearchFlags(bool searchBackwards = false)
	{
		SearchFlags flags = 0;
		if (_matchCaseCheckBox.ButtonPressed)
		{
			flags |= SearchFlags.MatchCase;
		}
		if (_byWordCheckBox.ButtonPressed)
		{
			flags |= SearchFlags.WholeWords;
		}
		if (searchBackwards)
		{
			flags |= SearchFlags.Backwards;
		}
		return (uint)flags;
	}

	private void DidChangeTextDocument()
	{
		Globals.Instance.LanguageClient.TextDocument.DidChangeTextDocument(new DidChangeTextDocumentParams
		{
			TextDocument = new OptionalVersionedTextDocumentIdentifier
			{
				Uri = _virtualFilePath,
				Version = ++_docVersion
			},
			ContentChanges = new Container<TextDocumentContentChangeEvent>(
			new TextDocumentContentChangeEvent
			{
				Text = _codeType == CodeType.Source ? TextWithContext() : this.Text
			})});
	}

	private async void OnLinesEditedFrom(long fromLine, long toLine)
	{
		DidChangeTextDocument();
		UpdateSyntaxHighlighting();

		if (!_autocompleteKeyPressed || !_ready)
		{
			return;
		}

		var line = _codeType == CodeType.Source ? GetCaretLine() + 1 : GetCaretLine();
		var column = GetCaretColumn() + 1;

		_completeToken?.Cancel();
		_completeToken?.Dispose();

		_completeToken = new CancellationTokenSource();

		try
		{
			var completion = Globals.Instance.LanguageClient.TextDocument.RequestCompletion(new CompletionParams
			{
				TextDocument = new TextDocumentIdentifier { Uri = _virtualFilePath },
				Position = new Position(line, column),
				Context = new CompletionContext { TriggerKind = CompletionTriggerKind.Invoked }
			}, _completeToken.Token).GetAwaiter().GetResult();

			foreach (CompletionItem item in completion.Items)
			{
				if (item.Kind == CompletionItemKind.Method || item.Kind == CompletionItemKind.Function)
				{
					AddCodeCompletionOption(CodeCompletionKind.Function, item.Label, item.InsertText, null, ResourceLoader.Load("uid://d0pdc43hxgxlc"));
				}
				else if (item.Kind == CompletionItemKind.Field || item.Kind == CompletionItemKind.Property)
				{
					AddCodeCompletionOption(CodeCompletionKind.Member, item.Label, item.InsertText, null, ResourceLoader.Load("uid://bsek7qnira33g"));
				}
				else if (item.Kind == CompletionItemKind.TypeParameter || item.Kind == CompletionItemKind.Variable)
				{
					AddCodeCompletionOption(CodeCompletionKind.Variable, item.Label, item.InsertText, null, ResourceLoader.Load("uid://bkomvyb0gmooc"));
				}
				else if (item.Kind == CompletionItemKind.Class || item.Kind == CompletionItemKind.Struct)
				{
					AddCodeCompletionOption(CodeCompletionKind.Class, item.Label, item.InsertText, null, ResourceLoader.Load("uid://d0pdc43hxgxlc"));
				}
				else if (item.Kind == CompletionItemKind.Enum)
				{
					AddCodeCompletionOption(CodeCompletionKind.Enum, item.Label, item.InsertText, null, ResourceLoader.Load("uid://ccyhcr7dpcvom"));
				}
				else
				{
					continue;
				}
				GD.Print(item.ToString());
			}

			UpdateCodeCompletionOptions(true);
		}
		catch (TaskCanceledException)
		{
		}
	}

	public async Task InitAsync(string scratchDir, string text)
	{
		// NOTE: LanguageId is overridden by the file extension of the file
		if (_codeType == CodeType.Context)
		{
			_virtualFilePath = scratchDir.PathJoin("ctx__virtualfile__.hpp");
		}
		else if (_codeType == CodeType.Source)
		{
			_virtualFilePath = scratchDir.PathJoin("src__virtualfile__.cpp");
		}

		Globals.Instance.LanguageClient.TextDocument.DidOpenTextDocument(new DidOpenTextDocumentParams
		{
			TextDocument = new TextDocumentItem
			{
				Uri = _virtualFilePath,
				LanguageId = "cpp",
				Version = _docVersion,
				Text = _codeType == CodeType.Source ? TextWithContext() : this.Text
			}
		});

		if (_codeType == CodeType.None)
		{
			GD.PushError($"CppCodeEdit type can't be set to none!");
		}

		_savedText = text;
		Text = text;

		UpdateSyntaxHighlighting();
		_ready = true;
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (Input.IsActionJustPressed("code_search"))
		{
			_searchBoxParent.Show();
		}

		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			char pressedKey = (char)keyEvent.Unicode;
			GD.Print("pressed: " + pressedKey + " " + keyEvent.Unicode);
			if (char.IsLetterOrDigit(pressedKey) ||
				pressedKey == '.' || pressedKey == '>' || pressedKey == '_' || keyEvent.Keycode == Key.Shift)
			{
				_autocompleteKeyPressed = true;
			}
			else if (keyEvent.Keycode == Key.Down || keyEvent.Keycode == Key.Up ||
					keyEvent.Keycode == Key.Enter || keyEvent.Keycode == Key.Tab)
			{
				_autocompleteKeyPressed = false;
			}
			else
			{
				GD.Print("cancel completion");
				CancelCodeCompletion();
				_autocompleteKeyPressed = false;
			}
		}
	}

	public override void _Input(InputEvent @event)
	{

	}

	public bool RequestSave(string scratchDir)
	{
		if (!IsDirty())
		{
			return false;
		}

		bool saved = false;
		_savedText = Text;
		if (_codeType == CodeType.Context)
		{
			File.WriteAllText(scratchDir.PathJoin("ctx.c"), Text);
			saved = true;
		}
		else if (_codeType == CodeType.Source)
		{
			File.WriteAllText(scratchDir.PathJoin("src.c"), Text);
			saved = true;
		}

		if (saved)
		{
			Globals.Instance.LanguageClient.TextDocument.DidSaveTextDocument(new DidSaveTextDocumentParams
			{
				Text = _codeType == CodeType.Source ? TextWithContext() : Text,
				TextDocument = new TextDocumentIdentifier
				{
					Uri = _virtualFilePath
				}
			});
		}

		return saved;
	}

	public bool IsDirty()
	{
		return _savedText != Text;
	}

	public string TextWithContext()
	{
		return "#include \"ctx.c\"\n" + Text;
	}

	private void UpdateSyntaxHighlighting()
	{
		var semanticTokens = Globals.Instance.LanguageClient.TextDocument.RequestSemanticTokensFull(
		new SemanticTokensParams
		{
			TextDocument = new TextDocumentIdentifier
			{
				Uri = _virtualFilePath,
			}
		}).GetAwaiter().GetResult(); // i wish i could make this async but godot's syntaxhighlighter just wont play nicely

		int currentLine = 0;
		int currentColumn = 0;

	    var legend = Globals.Instance.LanguageClient.TextDocument.ServerSettings.Capabilities.SemanticTokensProvider.Legend;
		_cppHighlighter.ClearHighlightingCache();
		for (int i = 0; i < semanticTokens.Data.Length; i += 5)
		{
			int deltaLine = semanticTokens.Data[i];
			int deltaStart = semanticTokens.Data[i + 1];
			int length = semanticTokens.Data[i + 2];
			int tokenType = semanticTokens.Data[i + 3];
			int tokenModifiers = semanticTokens.Data[i + 4];

			currentLine += deltaLine;
			currentColumn = (deltaLine == 0) ? currentColumn + deltaStart : deltaStart;

			string tokenTypeName = legend.TokenTypes.ElementAt(tokenType);

			Color color = CppHighlighter.TokenTypeToColor(tokenTypeName);

			_cppHighlighter.AddColorRange(_codeType == CodeType.Source ? (currentLine - 1): currentLine, currentColumn, currentColumn + length, color);
		}
	}
}
