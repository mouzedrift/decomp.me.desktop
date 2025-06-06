using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace DecompMeDesktop.Core;

public class Utils
{
	public static string FormatRelativeTime(TimeSpan timeSpan)
	{
		if (timeSpan.TotalMinutes < 1)
		{
			return "just now";
		}
		else if (timeSpan.TotalMinutes < 60)
		{
			return $"{(int)timeSpan.TotalMinutes} minute(s) ago";
		}
		else if (timeSpan.TotalHours < 24)
		{
			return $"{(int)timeSpan.TotalHours} hour(s) ago";
		}
		else if (timeSpan.TotalDays < 7)
		{
			return $"{(int)timeSpan.TotalDays} day(s) ago";
		}
		else if (timeSpan.TotalDays < 30)
		{
			return $"{(int)(timeSpan.TotalDays / 7)} week(s) ago";
		}
		else if (timeSpan.TotalDays < 365)
		{
			return $"{(int)(timeSpan.TotalDays / 30)} month(s) ago";
		}
		else
		{
			return $"{(int)(timeSpan.TotalDays / 365)} year(s) ago";
		}
	}

	public static void CopyBinFiles()
	{
		var binDir = DirAccess.Open("res://Assets/Bin");
		var error = binDir.ListDirBegin();
		if (error != Error.Ok)
		{
			GD.PrintErr($"Could not open resDir: {error}");
			return;
		}
		
		foreach (var fileName in binDir.GetFiles())
		{
			var src = binDir.GetCurrentDir().PathJoin(fileName);
			var dst = AppDirs.Bin.PathJoin(fileName);

			var globalFileDst = ProjectSettings.GlobalizePath(dst);
			if (File.Exists(globalFileDst))
			{
				continue;
			}

			error = DirAccess.CopyAbsolute(src, dst);
			if (error != Error.Ok)
			{
				GD.PrintErr($"Could not copy file {src} to {dst}: {error}");
				continue;
			}

			GD.Print($"Copied file from {src} to {dst}");
		}
	}

	public static Dictionary<string, string> ParseHeaders(string[] headers)
	{
		var headerDict = headers
			.Select(h => h.Split(new[] { ':' }, 2))
			.Where(parts => parts.Length == 2)
			.ToDictionary(
			parts => parts[0].Trim(),
			parts => parts[1].Trim(),
			StringComparer.OrdinalIgnoreCase);
		return headerDict;
	}

	public static ConfirmationDialog CreateConfirmationDialog(Node parent, string dialogText)
	{
		ConfirmationDialog confirmDialog = new ConfirmationDialog();
		confirmDialog.GetLabel().HorizontalAlignment = HorizontalAlignment.Center;
		confirmDialog.DialogText = dialogText;
		parent.AddChild(confirmDialog);
		confirmDialog.PopupCentered();
		confirmDialog.Show();
		return confirmDialog;
	}

	public static AcceptDialog CreateAcceptDialog(Node parent, string dialogText)
	{
		AcceptDialog acceptDialog = new AcceptDialog();
		acceptDialog.GetLabel().HorizontalAlignment = HorizontalAlignment.Center;
		acceptDialog.DialogText = dialogText;
		parent.AddChild(acceptDialog);
		acceptDialog.PopupCentered();
		acceptDialog.Show();
		return acceptDialog;
	}

	public static string ToWinePath(string windowsPath)
	{
		throw new NotImplementedException();
	}

	public static Process StartProcess(string fileName, string args = "", string workingDirectory = "")
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
		catch (Exception ex)
		{
			GD.Print(ex.Message);
		}

		return null;
	}

	public static string ToWslPath(string windowsPath)
	{
		windowsPath = Path.GetFullPath(windowsPath);

		char driveLetter = char.ToLower(windowsPath[0]);
		if (windowsPath[1] != ':' || windowsPath[2] != '\\')
		{
			GD.PrintErr("Invalid Windows path format.");
			return windowsPath;
		}

		string pathWithoutDrive = windowsPath.Substring(2).Replace('\\', '/');

		return $"/mnt/{driveLetter}{pathWithoutDrive}";
	}

	public static string GetPython3Path()
	{
		var globalPythonVenv = ProjectSettings.GlobalizePath(AppDirs.PythonVenv);
		var python3Path = Path.Join(globalPythonVenv, "bin", "python3");
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return ToWslPath(python3Path);
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			return python3Path;
		}

		return "";
	}

	public static string GetPip3Path()
	{
		var globalPythonVenv = ProjectSettings.GlobalizePath(AppDirs.PythonVenv);
		var python3Path = Path.Join(globalPythonVenv, "bin", "pip3");
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return ToWslPath(python3Path);
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			return python3Path;
		}

		return "";
	}

	public static string GetMatchPercentage(int? score, int? maxScore)
	{
		float? percentage = 100f - ((float)score / maxScore) * 100f;
		if (percentage >= 100f)
		{
			return "100%";
		}
		return $"{percentage:0.00}%";
	}

	public static string GetMatchPercentage(DecompMeApi.ScratchListItem scratch)
	{
		return GetMatchPercentage(scratch.score, scratch.max_score);
	}

	public static T DeepCopy<T>(T obj)
	{
		T copy = default;
		try
		{
			var json = JsonSerializer.Serialize(obj);
			copy = JsonSerializer.Deserialize<T>(json);
		}
		catch (Exception ex)
		{
			GD.PrintErr(ex.Message);
		}

		return copy;
	}

	public static Texture2D GetPlatformIcon(string platformName)
	{
		return platformName switch
		{
			"dreamcast" => ResourceLoader.Load<Texture2D>("uid://beopy5d1n44j1"),
			"gba" => ResourceLoader.Load<Texture2D>("uid://d1ulihwptl40j"),
			"gc_wii" => ResourceLoader.Load<Texture2D>("uid://duo0k71l0r2tj"),
			"irix" => ResourceLoader.Load<Texture2D>("uid://dhv01x8dbmph7"),
			"macosx" => ResourceLoader.Load<Texture2D>("uid://c6nmnekvq0ais"),
			"msdos" => ResourceLoader.Load<Texture2D>("uid://c5bljl08pvu46"),
			"n3ds" => ResourceLoader.Load<Texture2D>("uid://ni2mujbnmckf"),
			"n64" => ResourceLoader.Load<Texture2D>("uid://d0rhv7t0rncr3"),
			"nds_arm9" => ResourceLoader.Load<Texture2D>("uid://cy20ub1v0k2ji"),
			"switch" => ResourceLoader.Load<Texture2D>("uid://dm4frdybyo5tm"),
			"ps1" => ResourceLoader.Load<Texture2D>("uid://dnvdwxif85cn3"),
			"ps2" => ResourceLoader.Load<Texture2D>("uid://b1s84rsbv828g"),
			"psp" => ResourceLoader.Load<Texture2D>("uid://cp23ndfa2ys0b"),
			"saturn" => ResourceLoader.Load<Texture2D>("uid://cn76qvj15jndx"),
			"win32" => ResourceLoader.Load<Texture2D>("uid://dkxy8fhnqwxnd"),
			_ => ResourceLoader.Load<Texture2D>("uid://d2iapnf8j0gmd") // unknown icon
		};
	}

	public static LoadingWindow CreateLoadingWindow(Node parent)
	{
		var loadingWindow = ResourceLoader.Load<PackedScene>("uid://bxcbmvfofmuod").Instantiate<LoadingWindow>();
		parent.AddChild(loadingWindow);
		return loadingWindow;
	}
}
