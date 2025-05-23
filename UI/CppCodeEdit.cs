using DecompMeDesktop.Core;
using DecompMeDesktop.Core.CodeEditor;
using Godot;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.IO;

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

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (_codeType == CodeType.None)
		{
			GD.PushError($"CppCodeEdit type can't be set to none!");
		}

		_savedText = Text;
		//AsmDiffer.CheckAllDependenciesAsync();

		SyntaxHighlighter = new CppHighlighter();

		CodeCompletionPrefixes = [".", ">"];

		CodeCompletionRequested += () =>
		{
			GD.Print("requested!");
		};

		TextChanged += OnTextChanged;
	}

	private async void OnTextChanged()
	{
		/*
		var line = GetCaretLine();
		var column = GetCaretColumn();

		string virtualFile = "C:\\test.cpp";

		var languageClient = Globals.Instance.LanguageClient;
		languageClient.TextDocument.DidOpenTextDocument(new DidOpenTextDocumentParams
		{
			TextDocument = new TextDocumentItem
			{
				Uri = virtualFile,
				LanguageId = "cpp",
				Version = 1,
				Text = this.Text
			}
		});

		var semanticTokens = await languageClient.TextDocument.RequestSemanticTokens(
		new SemanticTokensParams
		{
			TextDocument = new TextDocumentIdentifier
			{
				Uri = virtualFile,
			}
		});


		int character = 0;
		int semLine = 0;
		for (int i = 0; i < semanticTokens.Data.Length; i += 5)
		{
			semLine += semanticTokens.Data[i];
			character = (semanticTokens.Data[i] == 0) ? character + semanticTokens.Data[i + 1] : semanticTokens.Data[i + 1];

			int length = semanticTokens.Data[i + 3];
			int tokenModifiers = semanticTokens.Data[i + 4];

			GD.Print($"semiLine: {semLine} character: {character} length: {length} tokenModifiers: {tokenModifiers}");
		}

		var completion = await languageClient.RequestCompletion(new CompletionParams
		{
			//TextDocument = new TextDocumentIdentifier { Uri = "C:\\dev\\github\\gta2_re\\Source\\debug.hpp" },
			TextDocument = new TextDocumentIdentifier { Uri = virtualFile },
			Position = new Position(line, column),
			Context = new CompletionContext { TriggerKind = CompletionTriggerKind.Invoked }
		});

		foreach (var item in completion.Items)
		{
			if (item.Kind == CompletionItemKind.Method || item.Kind == CompletionItemKind.Function)
			{
				AddCodeCompletionOption(CodeCompletionKind.Function, item.Label, item.InsertText);
			}
			else if (item.Kind == CompletionItemKind.Field)
			{
				AddCodeCompletionOption(CodeCompletionKind.Member, item.Label, item.InsertText);
			}
			else if (item.Kind == CompletionItemKind.TypeParameter || item.Kind == CompletionItemKind.Variable)
			{
				AddCodeCompletionOption(CodeCompletionKind.Variable, item.Label, item.InsertText);
			}
			else if (item.Kind == CompletionItemKind.Class || item.Kind == CompletionItemKind.Struct)
			{
				AddCodeCompletionOption(CodeCompletionKind.Class, item.Label, item.InsertText);
			}
		}

		UpdateCodeCompletionOptions(true);
		*/
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

		_savedText = Text;
		if (_codeType == CodeType.Context)
		{
			File.WriteAllText(scratchDir.PathJoin("ctx.c"), Text);
			return true;

		}
		else if (_codeType == CodeType.Source)
		{
			string srcCode = "#include \"ctx.c\"\n" + Text;
			File.WriteAllText(scratchDir.PathJoin("code.c"), srcCode);
			return true;
		}

		return false;
	}

	public bool IsDirty()
	{
		return _savedText != Text;
	}
}
