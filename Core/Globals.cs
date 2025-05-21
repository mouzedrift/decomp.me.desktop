using Godot;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
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

	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		Instance = this;

		AppDirs.CreateDirectories();

		Utils.CopyBinFiles();

		await StartClangdAsync();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private async Task StartClangdAsync()
	{
		// TODO: dont hardcode
		/*
		string clangdPath = "C:\\Users\\mouzedrift\\Downloads\\clangd_20.1.0\\bin\\clangd.exe";

		ClangdProcess = new Process();
		ClangdProcess.StartInfo.FileName = clangdPath;
		ClangdProcess.StartInfo.Arguments = "--log=verbose";
		ClangdProcess.StartInfo.RedirectStandardInput = true;
		ClangdProcess.StartInfo.RedirectStandardOutput = true;
		ClangdProcess.StartInfo.UseShellExecute = false;
		ClangdProcess.StartInfo.CreateNoWindow = true;

		ClangdProcess.Start();

		LanguageClient = await OmniSharp.Extensions.LanguageServer.Client.LanguageClient.From(
			new LanguageClientOptions()
			.WithInput(ClangdProcess.StandardOutput.BaseStream)
			.WithOutput(ClangdProcess.StandardInput.BaseStream)
			.WithInitializationOptions(new { }));

		GD.Print("Clangd started!");
		*/
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
			process = Utils.StartProcess($"{Utils.GetPython3Path()} diff.py", $"-o --no-pager --format json -f obj.o {symbol}", globalBinDir);
		}

		if (process == null)
		{
			GD.Print("Failed to start diff.py process");
			return null;
		}

		string diffJson = await process.StandardOutput.ReadToEndAsync();
		string error = await process.StandardError.ReadToEndAsync();

		await process.WaitForExitAsync();

		return diffJson;
	}
}
