using DecompMeDesktop.Core;
using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DecompMeDesktop.UI;

public partial class RequirementsCheckerPage : Control
{
	private readonly PackedScene TaskProgressbar = ResourceLoader.Load<PackedScene>("uid://coqxme8u46wvb");
	private readonly PackedScene ScratchListPage = ResourceLoader.Load<PackedScene>("uid://dgrhdqs4p8wr3");

	private VBoxContainer _taskList;
	private TaskProgressBar _checkPythonTask;
	private TaskProgressBar _createVenvTask;
	private TaskProgressBar _getMissingDepsTask;
	private TaskProgressBar _installDepsTask;

	private static readonly List<string> PythonRequirements = new List<string>()
	{
		"colorama",
		"watchdog",
		"levenshtein",
		"cxxfilt",
		"dataclasses",
	};

	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		_taskList = GetNode<VBoxContainer>("VBoxContainer2/VBoxContainer");

		_checkPythonTask = AddTask("Check for existing python installation...");
		_createVenvTask = AddTask("Creating python venv...");
		_getMissingDepsTask = AddTask("Retrieving missing dependencies...");
		_installDepsTask = AddTask("Installing missing dependencies...");

		await CheckAllDependenciesAsync();
		SceneManager.Instance.ChangeScene(ScratchListPage.Instantiate());
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private TaskProgressBar AddTask(string description)
	{
		var task = TaskProgressbar.Instantiate<TaskProgressBar>();
		_taskList.AddChild(task);
		task.SetTaskDescription(description);
		return task;
	}

	public async Task CheckAllDependenciesAsync()
	{
		var globalPythonVenvDir = ProjectSettings.GlobalizePath(AppDirs.PythonVenv);
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

	private async Task<bool> IsPythonInstalledAsync()
	{
		_checkPythonTask.ShowProgress();
		Process process = null;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			process = Utils.StartProcess("wsl", "python3 --version");

		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			process = Utils.StartProcess("python3", "--version");
		}

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

			var isCorrectVersion = major > 3 || (major == 3 && minor >= 6);
			if (isCorrectVersion)
			{
				_checkPythonTask.MarkAsDone();
			}
			return isCorrectVersion;
		}

		return false;
	}

	private async Task<List<string>> GetMissingPythonPackagesAsync()
	{
		_getMissingDepsTask.ShowProgress();
		var pythonExe = ProjectSettings.GlobalizePath(AppDirs.PythonVenv);
		Process process = null;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			process = Utils.StartProcess("wsl", $"{Utils.GetPython3Path()} -m pip list --format=freeze");
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			process = Utils.StartProcess(Utils.GetPython3Path(), "-m pip list --format=freeze");
		}

		if (process == null)
		{
			GD.Print("Error checking Python requirements");
			return [];
		}

		string output = await process.StandardOutput.ReadToEndAsync();
		await process.WaitForExitAsync();
		process.Dispose();

		var installedPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
		{
			var parts = line.Split("==", StringSplitOptions.RemoveEmptyEntries);
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

		_getMissingDepsTask.MarkAsDone();
		return missingPackages;
	}

	private async Task<bool> InstallPythonPackagesAsync(List<string> packages)
	{
		if (packages == null || packages.Count == 0)
		{
			_installDepsTask.MarkAsDone();
			return true;
		}

		_installDepsTask.ShowProgress();
		// Combine all package names into one string
		string joinedPackages = string.Join(" ", packages);
		Process process = null;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			process = Utils.StartProcess("wsl", $"{Utils.GetPython3Path()} -m pip install {joinedPackages}");
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			process = Utils.StartProcess(Utils.GetPython3Path(), $"-m pip install {joinedPackages}");
		}

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

		var ok = process.ExitCode == 0;
		GD.Print(ok);
		if (ok)
		{
			_installDepsTask.MarkAsDone();
		}

		process.Dispose();
		return ok;
	}

	private async Task<bool> CreatePythonVenvAsync()
	{
		_createVenvTask.ShowProgress();
		var globalPythonVenvDir = ProjectSettings.GlobalizePath(AppDirs.PythonVenv);
		Process process = new Process();
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			process = Utils.StartProcess("wsl", $"python3 -m venv {Utils.ToWslPath(globalPythonVenvDir)}");
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			process = Utils.StartProcess("python3", $"-m venv {globalPythonVenvDir}");
		}

		if (process == null)
		{
			GD.PrintErr("Failed to create python venv!");
			return false;
		}

		string output = await process.StandardOutput.ReadToEndAsync();
		string error = await process.StandardError.ReadToEndAsync();

		await process.WaitForExitAsync();

		GD.Print($"Successfully created python venv at: {globalPythonVenvDir}");
		_createVenvTask.MarkAsDone();
		return true;
	}
}
