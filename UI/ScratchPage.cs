using DecompMeDesktop.Core;
using DecompMeDesktop.Core.Compilers;
using Godot;
using Nerdbank.Streams;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using static DecompMeDesktop.Core.DecompMeApi;
using static System.Net.Mime.MediaTypeNames;
using Environment = System.Environment;

namespace DecompMeDesktop.UI;

public partial class ScratchPage : Control
{
	private Stopwatch _stopwatch = new Stopwatch();
	private HttpRequest _httpRequest;
	private AsmDiffPanel _asmDiffWindow;
	private DecompMeApi.ScratchListItem _originalScratch;
	private DecompMeApi.ScratchListItem _currentScratch;
	private string _scratchDir;
	private CppCodeEdit _ctxCodeEdit;
	private CppCodeEdit _srcCodeEdit;
	private CompilerOutputPanel _compilerOutputPanel;
	private bool _compilerRunning = false;
	private Button _newButton;
	private Button _saveButton;
	private Button _forkButton;
	private Button _deleteButton;
	private Button _compileButton;
	private Timer _recompileTimer;
	private HttpRequest _forkRequest;
	private ICompiler _currentCompiler;
	private CheckButton _compileLocallyCheckButton;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	private void ForkCurrentScratch()
	{
		var forkRequest = DecompMeApi.Instance.ForkScratch(_currentScratch);
		forkRequest.DataReceived += () =>
		{
			DecompMeApi.Instance.ClaimScratch(forkRequest.Data);
			_currentScratch = forkRequest.Data;
			forkRequest.QueueFree();
		};
	}

	private void SaveScratch()
	{
		SaveCode();

		if (DecompMeApi.Instance.CurrentUser.id != _currentScratch.owner.id)
		{
			ForkCurrentScratch();
		}
		else
		{
			DecompMeApi.Instance.UpdateScratch(_currentScratch);
		}
	}

	private void SaveCode(bool saveToDisk = true)
	{
		if (saveToDisk)
		{
			if (_ctxCodeEdit.RequestSave(_scratchDir))
			{
				_currentScratch.context = _ctxCodeEdit.Text;
			}

			if (_srcCodeEdit.RequestSave(_scratchDir))
			{
				_currentScratch.source_code = _srcCodeEdit.Text;
			}
		}
		else
		{
			_currentScratch.context = _ctxCodeEdit.Text;
			_currentScratch.source_code = _srcCodeEdit.Text;
		}

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override async void _Input(InputEvent @event)
	{
		if (Input.IsActionJustPressed("scratch_save"))
		{
			SaveScratch();
		}
		else if (Input.IsActionJustPressed("code_compile"))
		{
			await CompileAsync();
		}
	}

	public void Init(DecompMeApi.ScratchListItem scratch)
	{
		Ready += () =>
		{
			_asmDiffWindow = GetNode<AsmDiffPanel>("VBoxContainer/HSplitContainer/HBoxContainer/VSplitContainer/AsmDiffWindow");
			_ctxCodeEdit = GetNode<CppCodeEdit>("VBoxContainer/HSplitContainer/TabContainer/Context/CodeEdit");
			_srcCodeEdit = GetNode<CppCodeEdit>("VBoxContainer/HSplitContainer/TabContainer/Source Code/CodeEdit");
			_compilerOutputPanel = GetNode<CompilerOutputPanel>("VBoxContainer/HSplitContainer/HBoxContainer/VSplitContainer/CompilerOutputPanel");
			_newButton = GetNode<Button>("VBoxContainer/Header/HBoxContainer/NewButton");
			_saveButton = GetNode<Button>("VBoxContainer/Header/HBoxContainer/SaveButton");
			_forkButton = GetNode<Button>("VBoxContainer/Header/HBoxContainer/ForkButton");
			_deleteButton = GetNode<Button>("VBoxContainer/Header/HBoxContainer/DeleteButton");
			_compileButton = GetNode<Button>("VBoxContainer/Header/HBoxContainer/CompileButton");
			_recompileTimer = GetNode<Timer>("RecompileTimer");
			_compileLocallyCheckButton = GetNode<CheckButton>("VBoxContainer/Header/HBoxContainer/CheckButton");

			_originalScratch = Utils.DeepCopy(scratch);
			_currentScratch = scratch;


			var localScratchDir = AppDirs.Scratches.PathJoin(scratch.slug);
			_scratchDir = ProjectSettings.GlobalizePath(localScratchDir);
			Directory.CreateDirectory(_scratchDir);
			File.WriteAllText(_scratchDir.PathJoin("code.c"), "#include \"ctx.c\"\n#include \"src.c\"");

			GetNode<Label>("VBoxContainer/Header/HBoxContainer/UsernameLabel").Text = scratch.GetOwnerName();
			GetNode<Label>("VBoxContainer/Header/HBoxContainer/FunctionNameLabel").Text = scratch.name;
			GetNode<Label>("VBoxContainer/Header/HBoxContainer/TimestampLabel").Text = scratch.GetLastUpdatedTime();

			string scoreStr = $"Score: {scratch.score} ({Utils.GetMatchPercentage(_currentScratch.score, _currentScratch.max_score)})";
			GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/TabContainer/About/ScoreRichTextLabel").Text = scoreStr;
			GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/TabContainer/About/OwnerRichTextLabel").Text = $"Owner: {scratch.GetOwnerName()}";
			GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/TabContainer/About/ForkOfTextLabel2").Text = $"Fork of: {scratch.parent}"; // TODO
			GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/TabContainer/About/PlatformRichTextLabel3").Text = $"Platform: {scratch.platform}";
			GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/TabContainer/About/PresetRichTextLabel4").Text = $"Preset: {scratch.preset}"; // TODO
			GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/TabContainer/About/CreatedRichTextLabel5").Text = $"Created: {scratch.GetCreationTime()}";
			GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/TabContainer/About/ModifiedRichTextLabel6").Text = $"Modified: {scratch.GetLastUpdatedTime()}";

			_ctxCodeEdit.InitAsync(_scratchDir, scratch.context);
			_ctxCodeEdit.TextChanged += () =>
			{
				_recompileTimer.Start();
			};
			_srcCodeEdit.InitAsync(_scratchDir, scratch.source_code);
			_srcCodeEdit.TextChanged += () =>
			{
				_recompileTimer.Start();
			};

			_saveButton.Pressed += SaveScratch;
			_compileButton.Pressed += async () =>
			{
				await CompileAsync();
			};

			_recompileTimer.Timeout += async () =>
			{
				GD.Print("auto recompile triggered");
				await CompileAsync();
			};

			_forkButton.Pressed += () =>
			{
				ForkCurrentScratch();
			};

			_deleteButton.Pressed += () =>
			{
				DecompMeApi.Instance.DeleteScratch(_currentScratch);
			};

			_currentCompiler = Compilers.GetCompiler(_currentScratch.compiler);
			_compileLocallyCheckButton.ButtonPressed = _currentCompiler != null;
			if (_currentCompiler == null)
			{
				Utils.CreateAcceptDialog(this, $"Compiler {_currentScratch.compiler} is not installed!\nFalling back to online compiler.");
			}

			_stopwatch.Start();
			_httpRequest = DecompMeApi.Instance.RequestScratchZip(scratch.slug);
			_httpRequest.RequestCompleted += OnZipRequestCompleted;
		};
	}

	private async void OnZipRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		_stopwatch.Stop();
		GD.Print($"zip request + download took {_stopwatch.ElapsedMilliseconds}ms");
		_httpRequest.QueueFree();
		_httpRequest = null;

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

		var globalBinDir = ProjectSettings.GlobalizePath(AppDirs.Bin);
		var expectedDir = Path.Combine(globalBinDir, "expected");
		if (!Directory.Exists(expectedDir))
		{
			Directory.CreateDirectory(expectedDir);
		}

		var expectedObjPath = Path.Combine(expectedDir, "obj.o");
		File.Copy(_scratchDir.PathJoin("target.o"), expectedObjPath, true);

		var ourObjPath = Path.Combine(globalBinDir, "obj.o");
		File.WriteAllBytes(ourObjPath, objCurrentMs.ToArray());

		var ctxFilePath = Path.Combine(_scratchDir, "ctx.c");
		ctxEntry.ExtractToFile(ctxFilePath, true);
		var codeFilePath = Path.Combine(_scratchDir, "src.c");
		codeEntry.ExtractToFile(codeFilePath, true);

		await CompileAsync();
	}

	private async Task CompileAsync()
	{
		if (_compileLocallyCheckButton.ButtonPressed)
		{
			SaveCode(true);
			await CompileLocallyAsync();
		}
		else
		{
			SaveCode(false);
			CompileOnline();
		}
	}

	private void CompileOnline()
	{
		var request = DecompMeApi.Instance.CompileScratch(_currentScratch);
		request.DataReceived += () =>
		{
			var root = JsonDocument.Parse(request.Data.diff_output.ToString()).RootElement;
			var diffs = Globals.ParseAsmDifferJson(root.ToString());
			_asmDiffWindow.SetTargetText(diffs["base"]);
			_asmDiffWindow.SetCurrentText(diffs["current"]);
			_asmDiffWindow.SetScore(root.GetProperty("current_score").GetInt32(), root.GetProperty("max_score").GetInt32());
			_compilerOutputPanel.SetCompilerOutput(request.Data.compiler_output);
			request.QueueFree();
		};
	}

	private async Task CompileLocallyAsync()
	{
		if (_currentCompiler == null)
		{
			Utils.CreateAcceptDialog(this, $"Compiler {_currentScratch.compiler} is not installed!");
			return;
		}

		if (_compilerRunning)
		{
			GD.Print("already compiling...");
			return;
		}

		var psi = new ProcessStartInfo()
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = _scratchDir
		};

		// directly executing cl.exe doesn't work for some reason
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			psi.FileName = "cmd.exe";
			psi.Arguments = $"/c {_currentCompiler.Command} code.c {_currentScratch.compiler_flags}";
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			psi.FileName = $"wine";
			psi.Arguments = $"cmd.exe /c \"{_currentCompiler.Command} code.c {_currentScratch.compiler_flags}\"";
		}
		_currentCompiler.UpdateEnvironment(psi.EnvironmentVariables);

		_compilerRunning = true;

		var process = Process.Start(psi);

		await process.WaitForExitAsync();

		var stdout = await process.StandardOutput.ReadToEndAsync();
		if (process.ExitCode == 0)
		{
			string src = _scratchDir.PathJoin("code.obj");
			string dst = ProjectSettings.GlobalizePath(AppDirs.Bin.PathJoin("obj.o"));
			File.Copy(src, dst, true);

			var json = await Globals.RunAsmDiffAsync(_currentScratch.name);
			var diffs = Globals.ParseAsmDifferJson(json);

			var root = JsonDocument.Parse(json).RootElement;
			_asmDiffWindow.SetTargetText(diffs["base"]);
			_asmDiffWindow.SetCurrentText(diffs["current"]);
			_asmDiffWindow.SetScore(root.GetProperty("current_score").GetInt32(), root.GetProperty("max_score").GetInt32());
		}

		_compilerOutputPanel.SetCompilerOutput(stdout);
		_compilerRunning = false;
		process.Dispose();
	}
}
