using Godot;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace DecompMeDesktop.Core;

public partial class Globals : Node
{
	public static Globals Instance { get; private set; }
	public ILanguageClient LanguageClient { get; private set; }
	public Process ClangdProcess { get; private set; }

	private static readonly List<string> LinuxRequirements = new List<string>()
	{
		"python3-pip",
		"python3-venv",
		"binutils",
		"binutils-mingw-w64-i686"
	};

	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			GlobalCache.SaveCache();
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		Instance = this;

		AppDirs.CreateDirectories();

		GlobalCache.LoadCache();

		var window = GetTree().Root.GetWindow();
		var width = ProjectSettings.GetSetting("display/window/size/viewport_width").AsInt32();
		var height = ProjectSettings.GetSetting("display/window/size/viewport_height").AsInt32();
		var minSize = new Vector2I(width, height);

		if (window.Size <= minSize)
		{
			window.MinSize = minSize;
		}

		Utils.CopyBinFiles();

		await StartClangdAsync();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private async Task StartClangdAsync()
	{
		ClangdProcess = new Process();
		ClangdProcess.StartInfo.FileName = "clangd";
		ClangdProcess.StartInfo.Arguments = "--background-index --all-scopes-completion";
		ClangdProcess.StartInfo.RedirectStandardInput = true;
		ClangdProcess.StartInfo.RedirectStandardOutput = true;
		ClangdProcess.StartInfo.UseShellExecute = false;
		ClangdProcess.StartInfo.CreateNoWindow = true;

		ClangdProcess.Start();

		var options = new LanguageClientOptions();
		options.WithInput(ClangdProcess.StandardOutput.BaseStream)
		.WithOutput(ClangdProcess.StandardInput.BaseStream)
		.WithInitializationOptions(new { }).OnPublishDiagnostics(diagnosticsParams =>
		{
			//foreach (var diag in diagnosticsParams.Diagnostics)
			//{
			//	GD.Print($"{diag.Source} {diag.Severity}: {diag.Message} at {diag.Range.Start.Line}:{diag.Range.Start.Character}");
			//}
		});

		LanguageClient = await OmniSharp.Extensions.LanguageServer.Client.LanguageClient.From(options);
		GD.Print("Clangd started!");
	}

	private static async Task<string> RunPythonAsync(string command)
	{
		using var process = Utils.StartProcess("wsl", $"{/*_pythonExePath*/command} {command}");
		if (process == null)
		{
			return string.Empty;
		}

		string output = await process.StandardOutput.ReadToEndAsync();
		string error = await process.StandardError.ReadToEndAsync();
		await process.WaitForExitAsync();
		return output.Trim();
	}

	public static Dictionary<string, string> ParseAsmDifferJson(string json)
	{
		var root = JsonDocument.Parse(json).RootElement;

		var rows = root.GetProperty("rows");

		string baseText = string.Empty;
		string currentText = string.Empty;
		foreach (var row in rows.EnumerateArray())
		{
			bool basePresent = false;
			if (row.TryGetProperty("base", out JsonElement baseElem))
			{
				basePresent = true;
				foreach (var text in baseElem.GetProperty("text").EnumerateArray())
				{
					baseText += text.GetProperty("text").GetString();
				}
				baseText += "\n";
			}

			bool currentPresent = false;
			if (row.TryGetProperty("current", out JsonElement currentElem))
			{
				currentPresent = true;
				foreach (var text in currentElem.GetProperty("text").EnumerateArray())
				{
					currentText += text.GetProperty("text").GetString();
				}
				currentText += "\n";
			}

			if (basePresent != currentPresent)
			{
				if (!basePresent)
				{
					baseText += "\n";
				}
				else if (!currentPresent)
				{
					currentText += "\n";
				}
			}
		}
		return new Dictionary<string, string>() { { "base", baseText }, { "current", currentText } };
	}

	public static async Task<string> RunAsmDiffAsync(string symbol)
	{
		Process process = new Process();
		var globalBinDir = ProjectSettings.GlobalizePath(AppDirs.Bin);
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			process = Utils.StartProcess("wsl", $"{Utils.GetPython3Path()} diff.py -o --no-pager --format json -f obj.o {symbol}", globalBinDir);
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			process = Utils.StartProcess(Utils.GetPython3Path(), $"diff.py -o --no-pager --format json -f obj.o {symbol}", globalBinDir);
		}

		if (process == null)
		{
			GD.Print("Failed to start diff.py process");
			return null;
		}

		string diffJson = await process.StandardOutput.ReadToEndAsync();
		//string error = await process.StandardError.ReadToEndAsync();

		await process.WaitForExitAsync();
		process.Dispose();
		return diffJson;
	}
}
