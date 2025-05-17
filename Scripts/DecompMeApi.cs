using Godot;
using OmniSharp.Extensions.JsonRpc;
using System;
using System.Collections.Generic;
using System.Text.Json;
using static DecompMeApi;


public partial class DecompMeApi : HttpRequest
{
	public static DecompMeApi Instance;

	#nullable enable
	public class ScratchList
	{
		public string? next { get; set; }
		public string? previous { get; set; }
		public List<ScratchListItem>? results { get; set; }
	}

	public class ScratchListItem
	{
		public string? slug { get; set; }
		public ScratchOwner? owner { get; set; }
		public string? source_code { get; set; }
		public string? context { get; set; }
		public string? language { get; set; }
		public string? last_updated { get; set; }
		public string? creation_time { get; set; }
		public string? platform { get; set; }
		public string? compiler { get; set; }
		public int? preset { get; set; }
		public string? name { get; set; }
		public string? description { get; set; }

		public int? score { get; set; }
		public int? max_score { get; set; }
		public bool? match_override { get; set; }
		public string? parent { get; set; }
		public List<string>? libraries { get; set; }

		public string GetMatchPercentage()
		{
			float? percentage = 100f - ((float)score / max_score) * 100f;
			return $"{percentage:0.00}%";
		}

		public string GetCreationTime()
		{
			var creation = DateTime.Parse(creation_time);
			return Utils.FormatRelativeTime(DateTime.Now - creation);
		}

		public string GetLastUpdatedTime()
		{
			var lastUpdated = DateTime.Parse(last_updated);
			return Utils.FormatRelativeTime(DateTime.Now - lastUpdated);
		}
	}

	public class ScratchOwner
	{
		public bool? is_anonymous { get; set; }
		public int? id { get; set; }
		public bool? is_online { get; set; }
		public bool? is_admin { get; set; }
		public string? username { get; set; }
		public int? github_id { get; set; }
		public List<double>? frog_color { get; set; }

		public string GetGitHubAvatarUrl()
		{
			return $"https://avatars.githubusercontent.com/u/{github_id}";
		}
	}
	#nullable disable

	public enum RequestType
	{
		ScratchList,
		Scratch,
		Profile
	}

	private RequestType _requestType;
	private object _data;

	[Signal] public delegate void DataReceivedEventHandler(Variant requestType);

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;

		RequestCompleted += OnRequestCompleted;

		Utils.CopyBinFile();
	}

	public void RequestScratchList(string search = "")
	{
		_requestType = RequestType.ScratchList;
		if (search != "")
		{
			Request($"https://decomp.me/api/scratch?search={search}");
		}
		else
		{
			Request("https://decomp.me/api/scratch");
		}
	}

	public void RequestScratch(string url)
	{
		_requestType = RequestType.Scratch;
		Request(url);
	}

	public T GetData<T>()
	{
		return (T)_data;
	}

	public static bool IsType(RequestType requestType, Variant variantType)
	{
		return (int)requestType == variantType.AsInt32();
	}

	private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		string jsonStr = body.GetStringFromUtf8();
		if (_requestType == RequestType.ScratchList)
		{
			_data = JsonSerializer.Deserialize<ScratchList>(jsonStr);
		}
		else if (_requestType == RequestType.Scratch)
		{
			_data = JsonSerializer.Deserialize<ScratchListItem>(jsonStr);
		}
		else
		{
			return;
		}

		EmitSignal(SignalName.DataReceived, (int)_requestType);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public HttpRequest RequestScratchZip(string slug)
	{
		var httpRequest = new HttpRequest();
		AddChild(httpRequest);
		httpRequest.Request($"https://decomp.me/api/scratch/{slug}/export");
		return httpRequest;
	}
}
