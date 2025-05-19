using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

namespace DecompMeDesktop.Core.Compilers;

public interface ICompiler
{
	public string Platform { get; }
	public string Version { get; }
	public string DownloadUrl { get; }

	public bool IsInstalled()
	{
		var compilerPath = Path.Combine(Globals.CompilersPath, Platform, Version);
		return Directory.Exists(compilerPath) && Directory.GetFiles(compilerPath).Length >= 0;
	}

	public void Uninstall()
	{
		if (!IsInstalled())
		{
			return;
		}

		var compilerPath = Path.Combine(Globals.CompilersPath, Platform, Version);
		Directory.Delete(compilerPath, true);
	}

	public void UpdateEnvironment(StringDictionary env);
}

public class MSVCCompiler(string version, string downloadUrl) : ICompiler
{
	public string Platform { get; private set; } = "win32";
	public string Version { get; private set; } = version;
	public string DownloadUrl { get; private set; } = downloadUrl;

	public void UpdateEnvironment(StringDictionary env)
	{
		string binPath = Path.Combine(Globals.CompilersPath, Platform, Version, "Bin");
		env["PATH"] = $"{binPath};" + env["PATH"];
		env["INCLUDE"] = Path.Combine(Globals.CompilersPath, Platform, Version, "Include");
	}
}

public class Compilers
{
	public static readonly List<MSVCCompiler> Win32Compilers = new List<MSVCCompiler>()
	{
		new MSVCCompiler("msvc4.0", "https://github.com/itsmattkc/MSVC400/archive/821e942fd95bd16d01649401de7943ef87ae9f54.zip"),
		new MSVCCompiler("msvc4.1", "https://github.com/decompme/compilers/releases/download/compilers/msvc4.1.tar.gz"),
		new MSVCCompiler("msvc4.2", "https://github.com/itsmattkc/MSVC420/archive/df2c13aad74c094988c6c7e784234c2e778a0e91.zip"),
		new MSVCCompiler("msvc6.0", "https://github.com/OmniBlade/decomp.me/releases/download/msvcwin9x/msvc6.0.tar.gz"),
		new MSVCCompiler("msvc6.3", "https://github.com/OmniBlade/decomp.me/releases/download/msvcwin9x/msvc6.3.tar.gz"),
		new MSVCCompiler("msvc6.4", "https://github.com/OmniBlade/decomp.me/releases/download/msvcwin9x/msvc6.4.tar.gz"),
		new MSVCCompiler("msvc6.5", "https://github.com/OmniBlade/decomp.me/releases/download/msvcwin9x/msvc6.5.tar.gz"),
		new MSVCCompiler("msvc6.5pp", "https://github.com/OmniBlade/decomp.me/releases/download/msvcwin9x/msvc6.5pp.tar.gz"),
		new MSVCCompiler("msvc6.6", "https://github.com/OmniBlade/decomp.me/releases/download/msvcwin9x/msvc6.6.tar.gz"),
		new MSVCCompiler("msvc7.0", "https://github.com/roblabla/MSVC-7.0-Portable/releases/download/release/msvc7.0.tar.gz"),
		new MSVCCompiler("msvc7.1", "https://github.com/OmniBlade/decomp.me/releases/download/msvcwin9x/msvc7.0.tar.gz"),
		new MSVCCompiler("msvc8.0", "https://github.com/widberg/msvc8.0/archive/d6c4aa208c8345c78a9f68ba6ef911ee94c6a6e1.zip"),
		new MSVCCompiler("msvc8.0p", "https://github.com/widberg/msvc8.0/archive/52c8293f8b8d6441c594cf096542290c17a4d70e.zip"),
	};

	public static List<ICompiler> GetAllCompilers()
	{
		List<ICompiler> allCompilers = [];
		allCompilers.AddRange(Win32Compilers);

		return allCompilers;
	}

	public static bool IsCompilerInstalled(string version)
	{
		foreach (var compiler in GetAllCompilers())
		{
			if (compiler.Version == version && compiler.IsInstalled())
			{
				return true;
			}
		}
		return false;
	}

	public static ICompiler GetCompiler(string version)
	{
		foreach (var compiler in GetAllCompilers())
		{
			if (compiler.Version == version && compiler.IsInstalled())
			{
				return compiler;
			}
		}
		return null;
	}
}
