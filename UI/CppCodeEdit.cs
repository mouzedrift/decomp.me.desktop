using DecompMeDesktop.Core;
using DecompMeDesktop.Core.CodeEditor;
using Godot;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.IO;
using System.Runtime.CompilerServices;
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

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (_codeType == CodeType.None)
		{
			GD.PushError($"CppCodeEdit type can't be set to none!");
		}

		_savedText = Text;
		SyntaxHighlighter = new CppHighlighter();
		TextChanged += OnTextChanged;
	}

	public void DidOpenTextDocument(string scratchDir)
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
	}

	private async void OnTextChanged()
	{
		var line = GetCaretLine();
		var column = GetCaretColumn();

		var languageClient = Globals.Instance.LanguageClient.TextDocument;


		//var semanticTokens = await languageClient.TextDocument.RequestSemanticTokens(
		//new SemanticTokensParams
		//{
		//	TextDocument = new TextDocumentIdentifier
		//	{
		//		Uri = _virtualFilePath,
		//	}
		//});


		//int character = 0;
		//int semLine = 0;
		//for (int i = 0; i < semanticTokens.Data.Length; i += 5)
		//{
		//	semLine += semanticTokens.Data[i];
		//	character = (semanticTokens.Data[i] == 0) ? character + semanticTokens.Data[i + 1] : semanticTokens.Data[i + 1];

		//	int length = semanticTokens.Data[i + 3];
		//	int tokenModifiers = semanticTokens.Data[i + 4];

		//	GD.Print($"semiLine: {semLine} character: {character} length: {length} tokenModifiers: {tokenModifiers}");
		//}


		languageClient.DidChangeTextDocument(new DidChangeTextDocumentParams
		{
			TextDocument = new OptionalVersionedTextDocumentIdentifier
			{
				Uri = _virtualFilePath,
				Version = ++_docVersion
			},
			ContentChanges = new Container<TextDocumentContentChangeEvent>(
				new TextDocumentContentChangeEvent
				{
					Text = _codeType == CodeType.Source ? TextWithContext() : Text,
				}
			)
		});

		_completeToken?.Cancel();
		_completeToken?.Dispose();

		_completeToken = new CancellationTokenSource();

		try
		{
			var completion = await languageClient.RequestCompletion(new CompletionParams
			{
				TextDocument = new TextDocumentIdentifier { Uri = _virtualFilePath },
				Position = new Position(line, column),
				Context = new CompletionContext { TriggerKind = CompletionTriggerKind.TriggerForIncompleteCompletions },
			}, _completeToken.Token);

			foreach (CompletionItem item in completion.Items)
			{
				GD.Print(item.ToString());
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
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _Input(InputEvent @event)
	{
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
}
