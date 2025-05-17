using Godot;
using System;
using ClangSharp;
using ClangSharp.Interop;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using System.Diagnostics;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Reactive.Linq;
using System.Linq;

public partial class CppCodeEdit : CodeEdit
{
	private ILanguageClient _languageClient;
	private Process _clangdProcess;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//AsmDiffer.CheckAllDependenciesAsync();

		SyntaxHighlighter = new CppHighlighter();

		CodeCompletionPrefixes = [".", ">"];

		CodeCompletionRequested += () =>
		{
			GD.Print("requested!");
		};

		StartClangd();
		TextChanged += () =>
		{
			//var line = GetCaretLine();
			//var column = GetCaretColumn();

			//string virtualFile = "C:\\test.cpp";

			//_languageClient.TextDocument.DidOpenTextDocument(new DidOpenTextDocumentParams
			//{
			//	TextDocument = new TextDocumentItem
			//	{
			//		Uri = virtualFile,
			//		LanguageId = "cpp",
			//		Version = 1,
			//		Text = this.Text
			//	}
			//});

			//var semanticTokens = _languageClient.TextDocument.RequestSemanticTokens(
			//new SemanticTokensParams
			//{
			//	TextDocument = new TextDocumentIdentifier
			//	{
			//		Uri = virtualFile,
			//	}
			//});



			//var semanticData = semanticTokens.GetAwaiter().GetResult().Data;
			//int character = 0;
			//int semLine = 0;
			//for (int i = 0; i < semanticData.Count(); i += 5)
			//{
			//	semLine += semanticData[i];
			//	character = (semanticData[i] == 0) ? character + semanticData[i + 1] : semanticData[i + 1];

			//	int length = semanticData[i + 2];
			//	int tokenType = semanticData[i + 3];
			//	int tokenModifiers = semanticData[i + 4];

			//	//GD.Print($"semiLine: {semLine} character: {character} length: {length} tokenType: {tokenType} tokenModifiers: {tokenModifiers}");
			//}

			//foreach (var data in )
			//{
			//	GD.Print($"data is: {data}");
			//}

			//var completion = _languageClient.RequestCompletion(new CompletionParams
			//{
			//	//TextDocument = new TextDocumentIdentifier { Uri = "C:\\dev\\github\\gta2_re\\Source\\debug.hpp" },
			//	TextDocument = new TextDocumentIdentifier { Uri = virtualFile },
			//	Position = new Position(line, column),
			//	Context = new CompletionContext { TriggerKind = CompletionTriggerKind.Invoked }
			//});

			//foreach (var item in completion.GetAwaiter().GetResult().Items)
			//{
			//	if (item.Kind == CompletionItemKind.Method || item.Kind == CompletionItemKind.Function)
			//	{
			//		AddCodeCompletionOption(CodeCompletionKind.Function, item.Label, item.InsertText);
			//	}
			//	else if (item.Kind == CompletionItemKind.Field)
			//	{
			//		AddCodeCompletionOption(CodeCompletionKind.Member, item.Label, item.InsertText);
			//	}
			//	else if (item.Kind == CompletionItemKind.TypeParameter || item.Kind == CompletionItemKind.Variable)
			//	{
			//		AddCodeCompletionOption(CodeCompletionKind.Variable, item.Label, item.InsertText);
			//	}
			//	else if (item.Kind == CompletionItemKind.Class || item.Kind == CompletionItemKind.Struct)
			//	{
			//		AddCodeCompletionOption(CodeCompletionKind.Class, item.Label, item.InsertText);
			//	}
			//}
			
			//UpdateCodeCompletionOptions(true);

		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _Input(InputEvent @event)
	{
		if (Input.IsKeyPressed(Key.Period))
		{
			EmitSignalCodeCompletionRequested();
		}
	}

	private async Task StartClangd()
	{
		string clangdPath = "C:\\Users\\mouzedrift\\Downloads\\clangd_20.1.0\\bin\\clangd.exe";

		_clangdProcess = new Process();
		_clangdProcess.StartInfo.FileName = clangdPath;
		_clangdProcess.StartInfo.Arguments = "--log=verbose";
		_clangdProcess.StartInfo.RedirectStandardInput = true;
		_clangdProcess.StartInfo.RedirectStandardOutput = true;
		_clangdProcess.StartInfo.UseShellExecute = false;
		_clangdProcess.StartInfo.CreateNoWindow = true;

		_clangdProcess.Start();

		_languageClient = await LanguageClient.From(
			new LanguageClientOptions()
			.WithInput(_clangdProcess.StandardOutput.BaseStream)
			.WithOutput(_clangdProcess.StandardInput.BaseStream)
			.WithInitializationOptions(new { }));

		GD.Print("Clangd started!");
	}

	//private void ParseCode()
	//{
	//	CXIndex index = CXIndex.Create();
	//	CXTranslationUnit translationUnit;

	//	CXErrorCode error = CXTranslationUnit.TryParse(index, "C:\\dev\\github\\gta2_re\\Source\\debug.hpp", Array.Empty<string>(), Array.Empty<CXUnsavedFile>(), CXTranslationUnit_Flags.CXTranslationUnit_SingleFileParse, out translationUnit);
	//	if (error != CXErrorCode.CXError_Success)
	//	{
	//		GD.Print("Error parsing file.");
	//		return;
	//	}
		
	//	CXCursor cursor = translationUnit.Cursor;
	//	unsafe
	//	{
	//		cursor.VisitChildren(VisitClass, new CXClientData(IntPtr.Zero));
	//	}

	//	translationUnit.Dispose();
	//	index.Dispose();
	//}

	//private unsafe CXChildVisitResult VisitClass(CXCursor cursor, CXCursor parent, void* client_data)
	//{
	//	if (cursor.Kind == CXCursorKind.CXCursor_ClassDecl || cursor.Kind == CXCursorKind.CXCursor_StructDecl)
	//	{
	//		GD.Print($"class is: {cursor.Spelling}");
	//		cursor.VisitChildren(VisitFields, new CXClientData(IntPtr.Zero));
	//		cursor.VisitChildren(VisitMethods, new CXClientData(IntPtr.Zero));
	//		return CXChildVisitResult.CXChildVisit_Recurse;
	//	}
	//	return CXChildVisitResult.CXChildVisit_Recurse;
	//}

	//private unsafe CXChildVisitResult VisitFields(CXCursor cursor, CXCursor parent, void* client_data)
	//{
	//	if (cursor.Kind == CXCursorKind.CXCursor_FieldDecl)
	//	{
	//		GD.Print($"field is: {cursor.GetPrettyPrinted(cursor.PrintingPolicy)}");
	//		return CXChildVisitResult.CXChildVisit_Recurse;
	//	}
	//	return CXChildVisitResult.CXChildVisit_Recurse;
	//}

	//private unsafe CXChildVisitResult VisitMethods(CXCursor cursor, CXCursor parent, void* client_data)
	//{
	//	if (cursor.Kind == CXCursorKind.CXCursor_CXXMethod)
	//	{
	//		cursor.Location.GetSpellingLocation(out CXFile file, out uint line, out uint column, out uint offset);
	//		GD.Print($"method is: {cursor.GetPrettyPrinted(cursor.PrintingPolicy)}");
	//		GD.Print($"file: {file.Name} line: {line} column: {column} offset: {offset}");
	//		return CXChildVisitResult.CXChildVisit_Recurse;
	//	}
	//	return CXChildVisitResult.CXChildVisit_Recurse;
	//}
}
