using Godot;
using OmniSharp.Extensions.JsonRpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using static DecompMeDesktop.Core.DecompMeApi;
using static System.Net.WebRequestMethods;

namespace DecompMeDesktop.Core;

public partial class DecompMeApi : Node
{
	public static DecompMeApi Instance {  get; private set; }
	public const string ApiUrl = "https://decomp.me/api";
	public const int DefaultPageSize = 20;
	public const int MaxPageSize = 100;

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
		public User? owner { get; set; }
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

	public class User
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
		public User? owner { get; set; }
	}

	public class Stats
	{
		public int? asm_count { get; set; }
		public int? scratch_count { get; set; }
		public int? github_user_count { get; set; }
	}

#nullable disable

	public partial class JsonRequest<T> : HttpRequest
	{
		public T Data { get; private set; }
		[Signal] public delegate void DataReceivedEventHandler();

		public override void _Ready()
		{
			RequestCompleted += (long result, long responseCode, string[] headers, byte[] body) =>
			{
				string jsonStr = body.GetStringFromUtf8();
				Data = JsonSerializer.Deserialize<T>(jsonStr);

				foreach (var header in headers)
				{
					if (header.StartsWith("Set-Cookie:"))
					{
						var sessionId = header.Split(';')[0].Split('=')[1];
						if (!SettingsManager.Instance.HasSectionKey("Cookie", "session_id"))
						{
							SettingsManager.Instance.SetValue("Cookie", "session_id", sessionId);
							SettingsManager.Instance.Save();
						}
					}
				}

				EmitSignal(SignalName.DataReceived);
			};
		}

		public new Error Request(string url, string[] customHeaders = null, HttpClient.Method method = HttpClient.Method.Get, string requestData = "")
		{
			if (SettingsManager.Instance.HasSectionKey("Cookie", "session_id"))
			{
				var sessionId = SettingsManager.Instance.GetValue("Cookie", "session_id").AsString();
				var cookieHeader = $"Cookie: sessionid={sessionId}";
				if (customHeaders != null)
				{
					customHeaders = customHeaders.Append(cookieHeader).ToArray();
				}
				else
				{
					customHeaders = [cookieHeader];
				}
			}

			return base.Request(url, customHeaders, method, requestData);
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
	}

	public JsonRequest<ScratchList> RequestScratchList(string search = "", int pageSize = DefaultPageSize)
	{
		VerifyPageSize(pageSize);
		var httpRequest = new JsonRequest<ScratchList>();
		AddChild(httpRequest);

		string requestStr = $"{ApiUrl}/scratch?";
		requestStr += $"page_size={pageSize}&";
		if (search != "")
		{
			requestStr += $"search={search}";
		}

		GD.Print(requestStr);
		httpRequest.Request(requestStr);
		return httpRequest;
	}

	public JsonRequest<ScratchList> RequestNextScratchList(string url, int pageSize = DefaultPageSize)
	{
		VerifyPageSize(pageSize);
		var httpRequest = new JsonRequest<ScratchList>();
		AddChild(httpRequest);
		httpRequest.Request($"{url}&page_size={pageSize}");
		return httpRequest;
	}

	public JsonRequest<ScratchListItem> RequestScratch(string url)
	{
		var httpRequest = new JsonRequest<ScratchListItem>();
		AddChild(httpRequest);
		httpRequest.Request(url);
		return httpRequest;
	}

	public JsonRequest<Stats> RequestStats()
	{
		var httpRequest = new JsonRequest<Stats>();
		AddChild(httpRequest);
		httpRequest.Request($"{ApiUrl}/stats");
		return httpRequest;
	}

	public JsonRequest<User> RequestUser()
	{
		var httpRequest = new JsonRequest<User>();
		AddChild(httpRequest);
		httpRequest.Request($"{ApiUrl}/user");
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
		httpRequest.Request($"{ApiUrl}/scratch/{slug}/export");
		return httpRequest;
	}

	private static void VerifyPageSize(int pageSize)
	{
		if (pageSize > MaxPageSize)
		{
			GD.PushWarning($"Page size cannot be larger than {MaxPageSize}");
		}
	}
}
