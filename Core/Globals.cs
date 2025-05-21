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

	private static string _pythonVenv { get; set; }
	private static string _pythonExePath { get; set; }
	private static string _pythonVenvPipPath { get; set; }

	private static readonly List<string> PythonRequirements = new List<string>()
	{
		"colorama",
		"watchdog",
		"levenshtein",
		"cxxfilt",
		"dataclasses",
	};

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

		await CheckAllDependenciesAsync();
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

	public static async Task CheckAllDependenciesAsync()
	{
		var globalPythonVenvDir = ProjectSettings.GlobalizePath(AppDirs.PythonVenv);

		_pythonVenv = globalPythonVenvDir;
		_pythonExePath = globalPythonVenvDir + "/bin" + "/python3";
		_pythonVenvPipPath = globalPythonVenvDir + "/bin" + "/pip3";

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			_pythonVenv = await ToWslPathAsync(_pythonVenv);
			_pythonExePath = await ToWslPathAsync(_pythonExePath);
			_pythonVenvPipPath = await ToWslPathAsync(_pythonVenvPipPath);
		}

		GD.Print("Python venv: " + _pythonVenv);
		GD.Print("Python exe: " + _pythonExePath);
		GD.Print("pip exe: " + _pythonVenvPipPath);

		if (await IsPythonInstalledAsync())
		{
			await CreatePythonVenvAsync();
			var missingPythonPackages = await GetMissingPythonPackagesAsync();
			await InstallPythonPackagesAsync(missingPythonPackages);
			GD.Print("All requirements met.");
		}
		else
		{
			GD.PrintErr("Python version >= 3.6 is required.");
		}
	}

	private static async Task<bool> IsPythonInstalledAsync()
	{
		using var process = StartProcess("wsl", "python3 --version");
		if (process == null)
		{
			GD.PrintErr("Error checking Python requirements");
			return false;
		}

		string output = await process.StandardOutput.ReadToEndAsync();
		string error = await process.StandardError.ReadToEndAsync();
		await process.WaitForExitAsync();

		string versionOutput = !string.IsNullOrWhiteSpace(output) ? output : error;
		var match = Regex.Match(versionOutput, @"Python (\d+)\.(\d+)\.(\d+)");
		if (match.Success)
		{
			int major = int.Parse(match.Groups[1].Value);
			int minor = int.Parse(match.Groups[2].Value);
			GD.Print($"Python version {major}.{minor} is installed.");
			return major > 3 || (major == 3 && minor >= 6);
		}

		return false;
	}

	private static async Task<List<string>> GetMissingPythonPackagesAsync()
	{
		using var process = StartProcess("wsl", $"{_pythonExePath} -m pip list --format=freeze");
		if (process == null)
		{
			GD.Print("Error checking Python requirements");
			return [];
		}

		string output = await process.StandardOutput.ReadToEndAsync();
		await process.WaitForExitAsync();

		var installedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
		{
			var parts = line.Split(new[] { "==" }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length > 0)
			{
				installedPackages.Add(parts[0].ToLower());
			}
		}

		List<string> missingPackages = [];
		foreach (var requirement in PythonRequirements)
		{
			if (!installedPackages.Contains(requirement.ToLower()))
			{
				GD.Print($"Missing Python package: {requirement}");
				missingPackages.Add(requirement);
			}
		}

		return missingPackages;
	}

	private static async Task<bool> InstallPythonPackagesAsync(List<string> packages)
	{
		if (packages == null || packages.Count == 0)
		{
			return true;
		}

		// Combine all package names into one string
		string joinedPackages = string.Join(" ", packages);

		using var process = StartProcess("wsl", $"{_pythonExePath} -m pip install {joinedPackages}");
		if (process == null)
		{
			GD.PrintErr("Error installing Python packages");
			return false;
		}

		string output = await process.StandardOutput.ReadToEndAsync();
		string error = await process.StandardError.ReadToEndAsync();
		await process.WaitForExitAsync();

		if (!string.IsNullOrWhiteSpace(error))
		{
			GD.PrintErr("pip error:\n" + error);
		}

		return process.ExitCode == 0;
	}

	private static async Task<bool> CreatePythonVenvAsync()
	{
		Process process = new Process();
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			process = StartProcess("wsl", $"python3 -m venv {_pythonVenv}");
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			process = StartProcess("python3", $"-m venv {_pythonVenv}");
		}

		if (process == null)
		{
			GD.PrintErr("Failed to create python venv!");
			return false;
		}

		string output = await process.StandardOutput.ReadToEndAsync();
		string error = await process.StandardError.ReadToEndAsync();

		await process.WaitForExitAsync();

		GD.Print($"Successfully created python venv at: {_pythonVenv}");
		return true;
	}

	// TODO: convert the path without starting wsl
	// input: C:\\dir1\\dir2
	// output: /mnt/c/dir1/dir2
	private static async Task<string> ToWslPathAsync(string windowsPath)
	{
		using var process = StartProcess("wsl", $"wslpath {windowsPath}");
		if (process == null)
		{
			return string.Empty;
		}

		string wslPath = await process.StandardOutput.ReadToEndAsync();
		string error = await process.StandardError.ReadToEndAsync();
		await process.WaitForExitAsync();
		return wslPath.Trim();
	}

	private static async Task<string> RunPythonAsync(string command)
	{
		using var process = StartProcess("wsl", $"{_pythonExePath} {command}");
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
			process = StartProcess("wsl", $"{_pythonExePath} diff.py -o --no-pager --format json -f obj.o {symbol}", globalBinDir);
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			process = StartProcess($"{_pythonExePath} diff.py", $"-o --no-pager --format json -f obj.o {symbol}", globalBinDir);
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

	private static Process StartProcess(string fileName, string args = "", string workingDirectory = "")
	{
		try
		{
			var psi = new ProcessStartInfo
			{
				FileName = fileName,
				Arguments = args,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				WorkingDirectory = workingDirectory
			};

			return Process.Start(psi);
		}
		catch
		{
		}

		return null;
	}
}
