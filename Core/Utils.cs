using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text.Json;
using static System.Formats.Asn1.AsnWriter;

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
		catch
		{
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
}
