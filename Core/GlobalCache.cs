using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace DecompMeDesktop.Core;

public static class GlobalCache
{
	private static Dictionary<int, string> _presetNames = [];

	public static bool TryGetPresetName(int id, out string name)
	{
		return _presetNames.TryGetValue(id, out name);
	}

	public static void AddPresetName(int id, string name)
	{
		if (!_presetNames.ContainsKey(id))
		{
			_presetNames[id] = name;
		}
	}

	public static void ClearPresetNames()
	{
		_presetNames.Clear();
	}

	public static void SaveCache()
	{
		var presetJson = JsonSerializer.Serialize(_presetNames);
		FileAccess.Open(AppDirs.Cache.PathJoin("presets.json"), FileAccess.ModeFlags.Write).StoreString(presetJson);
	}

	public static void LoadCache()
	{
		var presetsJsonPath = AppDirs.Cache.PathJoin("presets.json");
		if (FileAccess.FileExists(presetsJsonPath))
		{
			var presetJson = FileAccess.Open(presetsJsonPath, FileAccess.ModeFlags.Read).GetAsText();
			_presetNames = JsonSerializer.Deserialize<Dictionary<int, string>>(presetJson);
		}
	}
}
