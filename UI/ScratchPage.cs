using DecompMeDesktop.Core;
using Godot;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace DecompMeDesktop.UI;

public partial class ScratchPage : Control
{
	private Stopwatch _stopwatch = new Stopwatch();
	private HttpRequest _httpRequest;
	private AsmDiffPanel _asmDiffWindow;
	private DecompMeApi.ScratchListItem _scratch;
	private string _scratchDir;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_asmDiffWindow = GetNode<AsmDiffPanel>("VBoxContainer/HSplitContainer/HBoxContainer/AsmDiffWindow");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void Populate(DecompMeApi.ScratchListItem scratch)
	{
		_scratch = scratch;

		_scratchDir = Globals.ScratchRoot.PathJoin(scratch.slug);
		if (!Directory.Exists(_scratchDir))
		{
			Directory.CreateDirectory(_scratchDir);
		}

		GetNode<Label>("VBoxContainer/Header/UsernameLabel").Text = scratch.GetOwnerName();
		GetNode<Label>("VBoxContainer/Header/FunctionNameLabel").Text = scratch.name;
		GetNode<Label>("VBoxContainer/Header/TimestampLabel").Text = scratch.GetLastUpdatedTime();

		string scoreStr = $"Score: {scratch.score} ({scratch.GetMatchPercentage()})";
		GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/TabContainer/About/ScoreRichTextLabel").Text = scoreStr;
		GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/TabContainer/About/OwnerRichTextLabel").Text = $"Owner: {scratch.GetOwnerName()}";
		GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/TabContainer/About/ForkOfTextLabel2").Text = $"Fork of: {scratch.parent}"; // TODO
		GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/TabContainer/About/PlatformRichTextLabel3").Text = $"Platform: {scratch.platform}";
		GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/TabContainer/About/PresetRichTextLabel4").Text = $"Preset: {scratch.preset}"; // TODO
		GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/TabContainer/About/CreatedRichTextLabel5").Text = $"Created: {scratch.GetCreationTime()}";
		GetNode<RichTextLabel>("VBoxContainer/HSplitContainer/TabContainer/About/ModifiedRichTextLabel6").Text = $"Modified: {scratch.GetLastUpdatedTime()}";

		GetNode<CodeEdit>("VBoxContainer/HSplitContainer/TabContainer/Source Code/CodeEdit").Text = scratch.source_code;
		GetNode<CodeEdit>("VBoxContainer/HSplitContainer/TabContainer/Context/CodeEdit").Text = scratch.context;

		_stopwatch.Start();
		_httpRequest = DecompMeApi.Instance.RequestScratchZip(scratch.slug);
		_httpRequest.RequestCompleted += OnZipRequestCompleted;
	}

	private async void OnZipRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		_stopwatch.Stop();
		GD.Print($"zip request + download took {_stopwatch.ElapsedMilliseconds}ms");
		_httpRequest.QueueFree();

		using var memoryStream = new MemoryStream(body);
		using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

		var asmTarget = archive.GetEntry("target.s");
		bool asmTargetAvailable = asmTarget != null;
		if (!asmTargetAvailable)
		{
			GD.Print("Could not find target.s in exported scratch zip, falling back to target.o");
		}

		var objTarget = archive.GetEntry("target.o");
		if (objTarget == null)
		{
			GD.Print("Could not find target.o in exported scratch zip");
			return;
		}

		var objCurrent = archive.GetEntry("current.o");
		if (objCurrent == null)
		{
			GD.Print("Could not find current.o in exported scratch zip");
			return;
		}

		if (asmTargetAvailable)
		{
			using var asmTargetReader = new StreamReader(asmTarget.Open());
			string targetAsm = asmTargetReader.ReadToEnd();
			File.WriteAllText(_scratchDir.PathJoin("target.s"), targetAsm);
		}

		using var objCurrentMs = new MemoryStream();
		using var objTargetMs = new MemoryStream();
		objCurrent.Open().CopyTo(objCurrentMs);
		objTarget.Open().CopyTo(objTargetMs);

		File.WriteAllBytes(_scratchDir.PathJoin("target.o"), objTargetMs.ToArray());

		var expectedDir = Path.Combine(Globals.BinPath, "expected");
		if (!Directory.Exists(expectedDir))
		{
			Directory.CreateDirectory(expectedDir);
		}

		var expectedObjPath = Path.Combine(expectedDir, "obj.o");
		File.Copy(_scratchDir.PathJoin("target.o"), expectedObjPath, true);

		var ourObjPath = Path.Combine(Globals.BinPath, "obj.o");
		File.WriteAllBytes(ourObjPath, objCurrentMs.ToArray());

		var json = await Globals.RunAsmDiffAsync(_scratch.name);
		var diffs = Globals.ParseAsmDifferJson(json);

		_asmDiffWindow.SetTargetText(diffs["base"]);
		_asmDiffWindow.SetCurrentText(diffs["current"]);
	}
}
