using DecompMeDesktop.Core;
using DecompMeDesktop.Core.Compilers;
using Godot;
using Nerdbank.Streams;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Environment = System.Environment;

namespace DecompMeDesktop.UI;

public partial class ScratchPage : Control
{
	private Stopwatch _stopwatch = new Stopwatch();
	private HttpRequest _httpRequest;
	private AsmDiffPanel _asmDiffWindow;
	private DecompMeApi.ScratchListItem _scratch;
	private string _scratchDir;
	private CppCodeEdit _ctxCodeEdit;
	private CppCodeEdit _srcCodeEdit;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_asmDiffWindow = GetNode<AsmDiffPanel>("VBoxContainer/HSplitContainer/HBoxContainer/AsmDiffWindow");
		_ctxCodeEdit = GetNode<CppCodeEdit>("VBoxContainer/HSplitContainer/TabContainer/Context/CodeEdit");
		_srcCodeEdit = GetNode<CppCodeEdit>("VBoxContainer/HSplitContainer/TabContainer/Source Code/CodeEdit");

		_ctxCodeEdit.SaveRequested += SaveCode;
		_srcCodeEdit.SaveRequested += SaveCode;
	}

	private async void SaveCode()
	{
		File.WriteAllText(_scratchDir.PathJoin("ctx.c"), _ctxCodeEdit.Text);
		string srcCode = "#include \"ctx.c\"\n" + _srcCodeEdit.Text;
		File.WriteAllText(_scratchDir.PathJoin("code.c"), srcCode);

		GD.Print("code saved!");

		await Compile();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override async void _Input(InputEvent @event)
	{
	}

	public void Populate(DecompMeApi.ScratchListItem scratch)
	{
		_scratch = scratch;

		_scratchDir = Globals.ScratchRoot.PathJoin(scratch.slug);
		if (!Directory.Exists(_scratchDir))
		{
			Directory.CreateDirectory(_scratchDir);
		}

		GetNode<Label>("VBoxContainer/Header/UsernameLabel").Text = scratch.GetOwnerName();
		GetNode<Label>("VBoxContainer/Header/FunctionNameLabel").Text = scratch.name;
		GetNode<Label>("VBoxContainer/Header/TimestampLabel").Text = scratch.GetLastUpdatedTime();

		string scoreStr = $"Score: {scratch.score} ({scratch.GetMatchPercentage()})";
		GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/TabContainer/About/ScoreRichTextLabel").Text = scoreStr;
		GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/TabContainer/About/OwnerRichTextLabel").Text = $"Owner: {scratch.GetOwnerName()}";
		GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/TabContainer/About/ForkOfTextLabel2").Text = $"Fork of: {scratch.parent}"; // TODO
		GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/TabContainer/About/PlatformRichTextLabel3").Text = $"Platform: {scratch.platform}";
		GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/TabContainer/About/PresetRichTextLabel4").Text = $"Preset: {scratch.preset}"; // TODO
		GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/TabContainer/About/CreatedRichTextLabel5").Text = $"Created: {scratch.GetCreationTime()}";
		GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/TabContainer/About/ModifiedRichTextLabel6").Text = $"Modified: {scratch.GetLastUpdatedTime()}";

		GetNode<CodeEdit>("VBoxContainer/HSplitContainer/TabContainer/Source Code/CodeEdit").Text = scratch.source_code;
		GetNode<CodeEdit>("VBoxContainer/HSplitContainer/TabContainer/Context/CodeEdit").Text = scratch.context;

		_stopwatch.Start();
		_httpRequest = DecompMeApi.Instance.RequestScratchZip(scratch.slug);
		_httpRequest.RequestCompleted += OnZipRequestCompleted;
	}

	private async void OnZipRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		_stopwatch.Stop();
		GD.Print($"zip request + download took {_stopwatch.ElapsedMilliseconds}ms");
		_httpRequest.QueueFree();

		using var memoryStream = new MemoryStream(body);
		using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

		var asmTarget = archive.GetEntry("target.s");
		bool asmTargetAvailable = asmTarget != null;
		if (!asmTargetAvailable)
		{
			GD.Print("Could not find target.s in exported scratch zip, falling back to target.o");
		}

		var targetObjEntry = archive.GetEntry("target.o");
		if (targetObjEntry == null)
		{
			GD.Print("Could not find target.o in exported scratch zip");
			return;
		}

		var currentObjEntry = archive.GetEntry("current.o");
		if (currentObjEntry == null)
		{
			GD.Print("Could not find current.o in exported scratch zip");
			return;
		}

		var codeEntry = archive.GetEntry("code.c");
		if (currentObjEntry == null)
		{
			GD.Print("Could not find code.c in exported scratch zip");
			return;
		}

		var ctxEntry = archive.GetEntry("ctx.c");
		if (currentObjEntry == null)
		{
			GD.Print("Could not find ctx.c in exported scratch zip");
			return;
		}

		if (asmTargetAvailable)
		{
			using var asmTargetReader = new StreamReader(asmTarget.Open());
			string targetAsm = asmTargetReader.ReadToEnd();
			File.WriteAllText(_scratchDir.PathJoin("target.s"), targetAsm);
		}

		using var objCurrentMs = new MemoryStream();
		using var objTargetMs = new MemoryStream();
		currentObjEntry.Open().CopyTo(objCurrentMs);
		targetObjEntry.Open().CopyTo(objTargetMs);

		File.WriteAllBytes(_scratchDir.PathJoin("target.o"), objTargetMs.ToArray());

		var expectedDir = Path.Combine(Globals.BinPath, "expected");
		if (!Directory.Exists(expectedDir))
		{
			Directory.CreateDirectory(expectedDir);
		}

		var expectedObjPath = Path.Combine(expectedDir, "obj.o");
		File.Copy(_scratchDir.PathJoin("target.o"), expectedObjPath, true);

		var ourObjPath = Path.Combine(Globals.BinPath, "obj.o");
		File.WriteAllBytes(ourObjPath, objCurrentMs.ToArray());

		var ctxFilePath = Path.Combine(_scratchDir, "ctx.c");
		ctxEntry.ExtractToFile(ctxFilePath, true);
		var codeFilePath = Path.Combine(_scratchDir, "code.c");
		codeEntry.ExtractToFile(codeFilePath, true);

		string sourceCode = File.ReadAllText(codeFilePath);
		sourceCode = "#include \"ctx.c\"\n" + sourceCode;
		File.WriteAllText(codeFilePath, sourceCode);
	}

	private async Task Compile()
	{
		var compiler = Compilers.GetCompiler(_scratch.compiler);
		var psi = new ProcessStartInfo
		{
			FileName = "C:\\Users\\mouzedrift\\AppData\\Roaming\\Godot\\app_userdata\\decomp.me.desktop\\compilers\\win32\\msvc6.4\\Bin\\CL.EXE",
			Arguments = "/c code.c " + _scratch.compiler_flags,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = _scratchDir
		};

		compiler.UpdateEnvironment(psi.EnvironmentVariables);
		GD.Print(psi.EnvironmentVariables["PATH"]);

		var process = Process.Start(psi);

		var stdoutTask = process.StandardOutput.ReadToEndAsync();
		var stderrTask = process.StandardError.ReadToEndAsync();

		await process.WaitForExitAsync();

		string stdout = await stdoutTask;
		string stderr = await stderrTask;

		GD.Print("STDOUT:\n" + stdout);
		GD.Print("STDERR:\n" + stderr);

		if (process.ExitCode == 0)
		{
			string src = _scratchDir.PathJoin("code.obj");
			string dst = Globals.BinPath.PathJoin("obj.o");
			File.Copy(src, dst, true);

			var json = await Globals.RunAsmDiffAsync(_scratch.name);
			var diffs = Globals.ParseAsmDifferJson(json);

			_asmDiffWindow.SetTargetText(diffs["base"]);
			_asmDiffWindow.SetCurrentText(diffs["current"]);
		}
	}
}
