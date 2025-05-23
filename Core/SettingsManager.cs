using Godot;

namespace DecompMeDesktop.Core;

public partial class SettingsManager : Node
{
	public static SettingsManager Instance { get; private set; }
	private const string SettingsPath = "user://settings.cfg";
	private ConfigFile _config;

	public override void _Ready()
	{
		Instance = this;

		_config = new ConfigFile();
		if (!FileAccess.FileExists(SettingsPath))
		{
			var scaleFactor = CalculateHidpiScaleFactor();

			_config.SetValue("General", "scale_factor", scaleFactor);
			//_config.SetValue("Video", "vsync", true);
			//_config.SetValue("Video", "resolution", new Vector2I(1280, 720));

			_config.Save(SettingsPath);
		}
		else
		{
			_config.Load(SettingsPath);
		}

		GetTree().Root.ContentScaleFactor = _config.GetValue("General", "scale_factor").AsSingle();

		//ApplyVideoSettings();
	}

	public void ApplyVideoSettings()
	{
		bool vsync = _config.GetValue("Video", "vsync").AsBool();
		Vector2I resolution = _config.GetValue("Video", "resolution").AsVector2I();

		DisplayServer.WindowSetVsyncMode(vsync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);

		if (DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Windowed)
		{
			DisplayServer.WindowSetSize(resolution);
			CenterWindow(GetWindow().CurrentScreen);
		}
	}

	private void CenterWindow(int screen)
	{
		var currentScreenPos = DisplayServer.ScreenGetPosition(screen);
		var currentScreenSize = DisplayServer.ScreenGetSize(screen);

		var windowSize = GetWindow().Size;

		Vector2I centeredPos = currentScreenPos + (currentScreenSize - windowSize) / 2;
		GetWindow().Position = centeredPos;
	}

	public void SetValue(string section, string key, Variant value)
	{
		_config.SetValue(section, key, value);
	}

	public Variant GetValue(string section, string key)
	{
		return _config.GetValue(section, key);
	}

	public bool HasSectionKey(string section, string key)
	{
		return _config.HasSectionKey(section, key);
	}

	public void Save()
	{
		_config.Save(SettingsPath);
	}

	private static float CalculateHidpiScaleFactor()
	{
		float defaultDpi = 96f;
		float scaleFactor = (float)DisplayServer.ScreenGetDpi() / defaultDpi;
		return scaleFactor;
	}
}
