using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

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
}
