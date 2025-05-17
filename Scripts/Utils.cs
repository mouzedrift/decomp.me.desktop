using Godot;
using System;
using System.IO;
using System.Linq;

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

	public static void CopyBinFile()
	{
		string binFolder = ProjectSettings.GlobalizePath("user://Bin");
		if (!Directory.Exists(binFolder))
		{
			Directory.CreateDirectory(binFolder);
		}

		var resDir = DirAccess.Open("res://Bin");
		if (resDir.ListDirBegin() != Error.Ok)
		{
			GD.Print("Could not open resDir");
			return;
		}

		foreach (var file in resDir.GetFiles())
		{
			var fileResPath = resDir.GetCurrentDir().PathJoin(file);

			using var resFile = Godot.FileAccess.Open(fileResPath, Godot.FileAccess.ModeFlags.Read);
			GD.Print(resFile.GetPath());
			if (resFile.GetError() != Error.Ok)
			{
				GD.Print($"Failed to open source file: {fileResPath}");
				continue;
			}

			var destPath = binFolder.PathJoin(file);
			if (Godot.FileAccess.FileExists(destPath))
			{
				continue;
			}

			using var actualFile = Godot.FileAccess.Open(destPath, Godot.FileAccess.ModeFlags.Write);
			if (actualFile.GetError() != Error.Ok)
			{
				GD.Print($"Failed to open destination file: {binFolder}");
				continue;
			}

			var content = resFile.GetBuffer((int)resFile.GetLength());
			actualFile.StoreBuffer(content);

			GD.Print($"Copied file from {fileResPath} to {binFolder}");
		}
	}
}
