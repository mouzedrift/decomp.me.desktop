using DecompMeDesktop.Core;
using DecompMeDesktop.Core.CodeEditor;
using Godot;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DecompMeDesktop.UI;

public partial class CppCodeEdit : CodeEdit
{
	private string _savedText;
	public enum CodeType
	{
		None,
		Context,
		Source
	}

	[Export] private CodeType _codeType;
	private string _virtualFilePath;
	private CancellationTokenSource _completeToken;
	private int _docVersion = 1;
	private CppHighlighter _cppHighlighter = new CppHighlighter();
	private int _lastLineCount;
	private bool _ready = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SyntaxHighlighter = _cppHighlighter;
		LinesEditedFrom += OnLinesEditedFrom;
	}

	private async void OnLinesEditedFrom(long fromLine, long toLine)
	{
		if (!_ready)
		{
			return;
		}

		GD.Print($"on lines edited from {fromLine}-{toLine}");

		var line = _codeType == CodeType.Source ? GetCaretLine() + 1 : GetCaretLine();
		var column = GetCaretColumn();

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
		})
		});

		UpdateSyntaxHighlighting((int)fromLine, (int)toLine);

		_completeToken?.Cancel();
		_completeToken?.Dispose();

		_completeToken = new CancellationTokenSource();

		try
		{
			var completion = await Globals.Instance.LanguageClient.TextDocument.RequestCompletion(new CompletionParams
			{
				TextDocument = new TextDocumentIdentifier { Uri = _virtualFilePath },
				Position = new Position(line, column),
				Context = new CompletionContext { TriggerKind = CompletionTriggerKind.TriggerForIncompleteCompletions },
			}, _completeToken.Token);

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
				else if (item.Kind == CompletionItemKind.TypeParameter ||
					item.Kind == CompletionItemKind.Variable)
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
			}

			UpdateCodeCompletionOptions(true);
		}
		catch (TaskCanceledException)
		{

		}

		_lastLineCount = GetLineCount();
	}

	public async Task InitAsync(string scratchDir, string text)
	{
		// NOTE: LanguageId is overridden by the file extension of the file
		if (_codeType == CodeType.Context)
		{
			//_virtualFilePath = scratchDir.PathJoin("ctx__virtualfile__.hpp");
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
		CodeCompletionPrefixes = [".", ">"];

		_savedText = text;
		Text = text;
		_lastLineCount = GetLineCount();

		//await UpdateSyntaxHighlightingAsync();
		_ready = true;
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		QueueRedraw();
	}

	public override void _Input(InputEvent @event)
	{
		if (Input.IsKeyPressed(Key.F2))
		{
			GD.Print("cleared");
			_cppHighlighter.CallDeferred(SyntaxHighlighter.MethodName.ClearHighlightingCache);
			_cppHighlighter.ClearHighlightingCache();
			_cppHighlighter.UpdateCache();
			QueueRedraw();
		}

		//if (Input.IsKeyPressed(Key.Period))
		//{
		//	EmitSignalCodeCompletionRequested();
		//}
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

	private void UpdateSyntaxHighlighting(int startLine, int endLine)
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
		var lineDiff = GetLineCount() - _lastLineCount;
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
		_cppHighlighter.ColorKeywords();
	}
}
