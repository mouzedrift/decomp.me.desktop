using Godot;
using System.IO;

namespace DecompMeDesktop.Core;

public class AppDirs
{
	public const string Scratches = "user://scratches";
	public const string PythonVenv = "user://venv";
	public const string Bin = "user://bin";
	public const string Compilers = "user://compilers";

	public static void CreateDirectories()
	{
		Directory.CreateDirectory(ProjectSettings.GlobalizePath(Scratches));
		Directory.CreateDirectory(ProjectSettings.GlobalizePath(PythonVenv));
		Directory.CreateDirectory(ProjectSettings.GlobalizePath(Bin));
		Directory.CreateDirectory(ProjectSettings.GlobalizePath(Compilers));
	}
};
