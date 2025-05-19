using Godot;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using DecompMeDesktop.Core;
using DecompMeDesktop.Core.Compilers;
using System.Linq;

namespace DecompMeDesktop.UI;

public partial class CompilerManagerWindow : Window
{
	private VBoxContainer _win32Tab;
	private List<CompilerCheckBox> _allCompilerCheckBoxes = [];
	private List<CompilerCheckBox> _compilersToInstall = [];
	private List<CompilerCheckBox> _compilersToUninstall = [];
	private Dictionary<string, bool> _checkBoxStartStates = [];
	private HttpRequest _lastRequest;
	private Button _installButton;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		CloseRequested += QueueFree;

		_win32Tab = GetNode<VBoxContainer>("MarginContainer/VBoxContainer/TabContainer/Win32/VBoxContainer");
		_installButton = GetNode<Button>("MarginContainer/VBoxContainer/InstallButton");

		foreach (var compiler in Compilers.Win32Compilers)
		{
			CompilerCheckBox checkBox = new CompilerCheckBox();
			checkBox.Compiler = compiler;
			_win32Tab.AddChild(checkBox);
			_allCompilerCheckBoxes.Add(checkBox);
		}

		foreach (var compiler in _allCompilerCheckBoxes)
		{
			compiler.Toggled += (bool toggledOn) =>
			{
				_installButton.Disabled = !IsAnyCheckBoxDirty();

				if (toggledOn)
				{
					if (toggledOn != _checkBoxStartStates[compiler.Compiler.Version])
					{
						_compilersToInstall.Add(compiler);
					}
					else
					{
						_compilersToUninstall.Remove(compiler);
					}
				}
				else
				{
					if (toggledOn != _checkBoxStartStates[compiler.Compiler.Version])
					{

						_compilersToUninstall.Add(compiler);
					}
					else
					{
						_compilersToInstall.Remove(compiler);
					}
				}
			};
		}
		_installButton.Pressed += OnInstallButtonPressed;

		InitUI();
	}

	// handle installing and uninstalling compilers
	private void OnInstallButtonPressed()
	{
		if (_compilersToInstall.Count <= 0 && _compilersToUninstall.Count <= 0)
		{
			GD.Print("No compilers selected to install/uninstall");
			return;
		}

		string installCompilers = string.Join("\n", _compilersToInstall.Select(c => c.Compiler.Version));
		string uninstallCompilers = string.Join("\n", _compilersToUninstall.Select(c => c.Compiler.Version));
		string dialogText = string.Empty;
		if (_compilersToInstall.Count > 0)
		{
			dialogText += "Installing:\n" + installCompilers + "\n\n";
		}

		if (_compilersToUninstall.Count > 0)
		{
			dialogText += "Uninstalling:\n" + uninstallCompilers;
		}

		ConfirmationDialog confirmDialog = new ConfirmationDialog();
		confirmDialog.GetLabel().HorizontalAlignment = HorizontalAlignment.Center;
		confirmDialog.DialogText = dialogText;
		AddChild(confirmDialog);
		confirmDialog.PopupCentered();
		confirmDialog.Show();

		confirmDialog.Confirmed += () =>
		{
			if (_compilersToInstall.Count > 0)
			{
				StartCompilerDownloads();
			}

			if (_compilersToUninstall.Count > 0)
			{
				UninstallCompilers();
			}
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void StartCompilerDownloads()
	{
		_lastRequest = new HttpRequest();
		AddChild(_lastRequest);

		_lastRequest.Request(_compilersToInstall[0].Compiler.DownloadUrl);
		GD.Print($"Downloading compiler: {_compilersToInstall[0].Compiler.DownloadUrl}...");
		_lastRequest.RequestCompleted += OnRequestCompleted;
	}

	private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		_lastRequest.QueueFree();
		_lastRequest = null;
		var compiler = _compilersToInstall[0];
		var compilerDir = Path.Combine(Globals.CompilersPath, compiler.Compiler.Platform, compiler.Compiler.Version);
		Directory.CreateDirectory(compilerDir);

		foreach (var header in headers)
		{
			GD.Print(header);
		}

		var headerDict = Utils.ParseHeaders(headers);
		if (headerDict["Content-Type"] == "application/zip")
		{
			ExtractZip(body, compilerDir);
		}
		else if (headerDict["Content-Type"] == "application/gzip" ||
				headerDict["Content-Type"] == "application/octet-stream")
		{
			ExtractTarGz(body, compilerDir);
		}
		else
		{
			// Fallback: save raw file
			string filePath = Path.Combine(compilerDir, compiler.Name);
			File.WriteAllBytes(filePath, body);
		}

		_compilersToInstall.RemoveAt(0);
		if (_compilersToInstall.Count <= 0)
		{
			AcceptDialog acceptDialog = new AcceptDialog();
			acceptDialog.GetLabel().HorizontalAlignment = HorizontalAlignment.Center;
			acceptDialog.DialogText = $"All compilers have been installed/uninstalled successfully.";
			AddChild(acceptDialog);
			acceptDialog.PopupCentered();
			acceptDialog.Show();
			return;
		}

		StartCompilerDownloads();
	}

	private void ExtractZip(byte[] body, string targetDir)
	{
		using var zipStream = new MemoryStream(body);
		using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
		foreach (var entry in archive.Entries)
		{
			string entryPath = Path.Combine(targetDir, entry.FullName);

			// Handle directory entries
			if (string.IsNullOrEmpty(entry.Name))
			{
				Directory.CreateDirectory(entryPath);
				continue;
			}

			Directory.CreateDirectory(Path.GetDirectoryName(entryPath)!);
			using var entryStream = entry.Open();
			using var outputStream = new FileStream(entryPath, FileMode.Create, System.IO.FileAccess.Write);
			entryStream.CopyTo(outputStream);
		}
	}

	private void ExtractTarGz(byte[] body, string targetDir)
	{
		using var gzipStream = new GZipInputStream(new MemoryStream(body));
		using var tarArchive = TarArchive.CreateInputTarArchive(gzipStream);
		tarArchive.ExtractContents(targetDir);
	}

	private void InitUI()
	{
		foreach (var checkBox in _allCompilerCheckBoxes)
		{
			bool isInstalled = checkBox.Compiler.IsInstalled();
			checkBox.SetPressedNoSignal(isInstalled);
			_checkBoxStartStates.Add(checkBox.Compiler.Version, isInstalled);
		}

		_installButton.Disabled = !IsAnyCheckBoxDirty();
	}

	private void UninstallCompilers()
	{
		foreach (var compiler in _compilersToUninstall)
		{
			compiler.Compiler.Uninstall();

		}
		_compilersToUninstall.Clear();
	}

	private bool IsAnyCheckBoxDirty()
	{
		foreach (var compiler in _allCompilerCheckBoxes)
		{
			if (compiler.ButtonPressed != _checkBoxStartStates[compiler.Compiler.Version])
			{
				return true;
			}
		}

		return false;
	}
}
