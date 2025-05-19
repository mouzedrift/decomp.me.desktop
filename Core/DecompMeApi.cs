using Godot;
using OmniSharp.Extensions.JsonRpc;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace DecompMeDesktop.Core;

public partial class DecompMeApi : Node
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
		public string? compiler_flags { get; set; }
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

		public string GetOwnerName()
		{
			return owner != null ? owner.username : "No Owner";
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
	public class PresetListItem
	{
		public int? id { get; set; }
		public string? name { get; set; }
		public string? platform { get; set; }
		public string? compiler { get; set; }
		public string? assembler_flags { get; set; }
		public string? compiler_flags { get; set; }
		public List<string>? diff_flags { get; set; } // TODO: type not confirmed 
		public string? decompiler_flags { get; set; }
		public List<string>? libraries { get; set; } // TODO: type not confirmed 
		public int? num_scratches { get; set; }
		public ScratchOwner? owner { get; set; }
	}

#nullable disable

	public partial class ScratchListItemRequest : HttpRequest
	{
		public ScratchListItem Data { get; private set; }
		[Signal] public delegate void DataReceivedEventHandler();

		public override void _Ready()
		{
			RequestCompleted += (long result, long responseCode, string[] headers, byte[] body) =>
			{
				string jsonStr = body.GetStringFromUtf8();
				Data = JsonSerializer.Deserialize<ScratchListItem>(jsonStr);
				EmitSignal(SignalName.DataReceived);
			};
		}
	}

	public partial class ScratchListRequest : HttpRequest
	{
		public ScratchList Data { get; private set; }
		[Signal] public delegate void DataReceivedEventHandler();

		public override void _Ready()
		{
			RequestCompleted += (long result, long responseCode, string[] headers, byte[] body) =>
			{
				string jsonStr = body.GetStringFromUtf8();
				Data = JsonSerializer.Deserialize<ScratchList>(jsonStr);
				EmitSignal(SignalName.DataReceived);
			};
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
	}

	public ScratchListRequest RequestScratchList(string search = "", int pageSize = 20)
	{
		var httpRequest = new ScratchListRequest();
		AddChild(httpRequest);
		if (search != "")
		{
			httpRequest.Request($"https://decomp.me/api/scratch?search={search}&page_size={pageSize}");
		}
		else
		{
			httpRequest.Request($"https://decomp.me/api/scratch?page_size={pageSize}");
		}

		return httpRequest;
	}

	public ScratchListRequest RequestNextScratchList(string url, int pageSize = 20)
	{
		var httpRequest = new ScratchListRequest();
		AddChild(httpRequest);
		httpRequest.Request($"{url}&page_size={pageSize}");
		return httpRequest;
	}

	public ScratchListItemRequest RequestScratch(string url)
	{
		var httpRequest = new ScratchListItemRequest();
		AddChild(httpRequest);
		httpRequest.Request(url);
		return httpRequest;
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
