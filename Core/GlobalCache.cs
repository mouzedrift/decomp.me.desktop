using System.Collections.Generic;

namespace DecompMeDesktop.Core;

public static class GlobalCache
{
	private static readonly Dictionary<int, string> _presetNames = [];

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
}
