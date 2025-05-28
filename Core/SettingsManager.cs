using Godot;

namespace DecompMeDesktop.Core;

public partial class SettingsManager : Node
{
	public static SettingsManager Instance { get; private set; }
	private const string SettingsPath = "user://settings.cfg";
	private ConfigFile _config;

	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			SetValue("Video", "window_resolution", GetWindow().Size);
			SetValue("Video", "window_position", GetWindow().Position);
			SetValue("Video", "window_screen", GetWindow().CurrentScreen);
			GD.Print("saving config...");
			Save();
		}
	}

	public override void _Ready()
	{
		Instance = this;

		_config = new ConfigFile();
		if (FileAccess.FileExists(SettingsPath))
		{
			_config.Load(SettingsPath);
		}

		SetValueOptional("Setup", "dependencies_installed", false);

		var scaleFactor = CalculateHidpiScaleFactor();
		SetValueOptional("General", "scale_factor", scaleFactor);

		SetValueOptional("CodeEditor", "clangd_enabled", true);
		SetValueOptional("CodeEditor", "auto_completion", true);
		SetValueOptional("CodeEditor", "syntax_highlighting", true);

		SetValueOptional("Video", "vsync", true);
		SetValueOptional("Video", "window_resolution", GetWindow().Size);
		SetValueOptional("Video", "window_position", GetWindow().Position);
		SetValueOptional("Video", "window_screen", GetWindow().CurrentScreen);
		SetValueOptional("Video", "max_fps", 0);

		GetTree().Root.ContentScaleFactor = _config.GetValue("General", "scale_factor").AsSingle();

		ApplyVideoSettings();
	}

	private void SetValueOptional(string section, string key, Variant value)
	{
		if (!HasSectionKey(section, key))
		{
			_config.SetValue(section, key, value);
		}
	}

	public void ApplyVideoSettings()
	{
		bool vsync = _config.GetValue("Video", "vsync").AsBool();
		Vector2I resolution = _config.GetValue("Video", "window_resolution").AsVector2I();
		Vector2I windowPos = _config.GetValue("Video", "window_position").AsVector2I();
		int windowScreen = _config.GetValue("Video", "window_screen").AsInt32();

		DisplayServer.WindowSetVsyncMode(vsync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);

		if (DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Windowed)
		{
			GetWindow().Size = resolution;
			GetWindow().Position = windowPos;
			GetWindow().CurrentScreen = windowScreen;

			//CenterWindow(GetWindow().CurrentScreen);
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
