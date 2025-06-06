using DecompMeDesktop.Core.Network;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DecompMeDesktop.Core;

public static class DecompMeApi
{
	public const string ApiUrl = "https://decomp.me/api";
	public const int DefaultPageSize = 20;
	public const int MaxPageSize = 100;

	public static User CurrentUser { get; set; }
	[Signal] public delegate void UserReadyEventHandler();

	public enum ScratchRequestType
	{
		Url,
		Slug
	}

#nullable enable

	public class ClaimResult
	{
		public bool success { get; set; }
	}

	public class CompileResult
	{
		public object diff_output { get; set; }
		public string compiler_output { get; set; }
		public bool success { get; set; }
	}

	public class SearchResult
	{
		public string? type { get; set; }
		public object? item { get; set; }
	}

	public class LibraryItem
	{
		public string name { get; set; }
		public string version { get; set; }
	}

	public class PresetName
	{
		public int? id { get; set; }
		public string? name { get; set; }
	}

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
		public string? claim_token { get; set; }
		public string? name { get; set; }
		public string? description { get; set; }
		public int? score { get; set; }
		public int? max_score { get; set; }
		public bool? match_override { get; set; }
		public string? parent { get; set; }
		public List<LibraryItem>? libraries { get; set; }
		public string? diff_label { get; set; }
		public List<string>? diff_flags { get; set; }

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

	public static string[] AddHeader(string[] headers, string headerToAdd)
	{
		if (headers != null)
		{
			headers = headers.Append(headerToAdd).ToArray();
		}
		else
		{
			headers = [headerToAdd];
		}
		return headers;
	}

	public static string[] AddCookie(string[] customHeaders)
	{
		if (SettingsManager.Instance.HasSectionKey("Cookie", "session_id"))
		{
			var sessionId = SettingsManager.Instance.GetValue("Cookie", "session_id").AsString();
			var cookieHeader = $"Cookie: sessionid={sessionId}";
			customHeaders = AddHeader(customHeaders, cookieHeader);
		}
		return customHeaders;
	}

	public static void SaveCookie(string[] headers)
	{
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
	}

	public static async Task<ScratchList> RequestScratchListAsync(Node parent, string search = "", int pageSize = DefaultPageSize)
	{
		VerifyPageSize(pageSize);
		var httpRequest = new JsonHttpRequest<ScratchList>();
		parent.AddChild(httpRequest);

		string requestStr = $"{ApiUrl}/scratch?";
		requestStr += $"page_size={pageSize}&";
		if (search != "")
		{
			requestStr += $"search={search}";
		}

		return await httpRequest.RequestAsync(requestStr);
	}

	public static async Task<ScratchList> RequestNextScratchListAsync(Node parent, string url, int pageSize = DefaultPageSize)
	{
		VerifyPageSize(pageSize);
		var httpRequest = new JsonHttpRequest<ScratchList>();
		parent.AddChild(httpRequest);
		return await httpRequest.RequestAsync($"{url}&page_size={pageSize}");
	}

	public static async Task<ScratchListItem> RequestScratchAsync(Node parent, string input, ScratchRequestType requestType = ScratchRequestType.Url)
	{
		var httpRequest = new JsonHttpRequest<ScratchListItem>();
		parent.AddChild(httpRequest);
		string url = requestType == ScratchRequestType.Url ? input : $"{ApiUrl}/scratch/{input}";
		return await httpRequest.RequestAsync(url);
	}

	public static async Task<Stats> RequestStatsAsync(Node parent)
	{
		var httpRequest = new JsonHttpRequest<Stats>();
		parent.AddChild(httpRequest);
		return await httpRequest.RequestAsync($"{ApiUrl}/stats");
	}

	public static async Task<User> RequestUserAsync(Node parent)
	{
		var httpRequest = new JsonHttpRequest<User>();
		parent.AddChild(httpRequest);
		return await httpRequest.RequestAsync($"{ApiUrl}/user");
	}

	public static async Task<ScratchList> RequestUserScratchesAsync(Node parent, User user, int pageSize = DefaultPageSize)
	{
		var httpRequest = new JsonHttpRequest<ScratchList>();
		parent.AddChild(httpRequest);

		if (!user.is_anonymous.GetValueOrDefault() && user.github_id != null)
		{
			return await httpRequest.RequestAsync($"{ApiUrl}/users/{user.username}/scratches?page_size={pageSize}&ordering=-creation_time");
		}
		else
		{
			return await httpRequest.RequestAsync($"{ApiUrl}/user/scratches?page_size={pageSize}&ordering=-creation_time");
		}
	}

	public static async Task<ScratchListItem> ForkScratchAsync(Node parent, ScratchListItem scratch)
	{
		var httpRequest = new JsonHttpRequest<ScratchListItem>();
		parent.AddChild(httpRequest);
		return await httpRequest.RequestAsync($"{ApiUrl}/scratch/{scratch.slug}/fork", null, HttpClient.Method.Post, JsonSerializer.Serialize(scratch));
	}

	public static async Task<ClaimResult> ClaimScratchAsync(Node parent, ScratchListItem scratch)
	{
		if (scratch.claim_token == null || scratch.slug == null)
		{
			GD.PrintErr("Cannot claim scratch!");
			return null;
		}

		var httpRequest = new JsonHttpRequest<ClaimResult>();
		parent.AddChild(httpRequest);
		var claimJson = JsonSerializer.Serialize(new { token = scratch.claim_token });
		return await httpRequest.RequestAsync($"{ApiUrl}/scratch/{scratch.slug}/claim", null, HttpClient.Method.Post, claimJson);
	}

	public static async Task DeleteScratchAsync(Node parent, ScratchListItem scratch)
	{
		var httpRequest = new ManagedHttpRequest();
		parent.AddChild(httpRequest);
		await httpRequest.RequestAsync($"{ApiUrl}/scratch/{scratch.slug}", null, HttpClient.Method.Delete);
	}

	public static async Task UpdateScratchAsync(Node parent, ScratchListItem scratch)
	{
		var httpRequest = new ManagedHttpRequest();
		parent.AddChild(httpRequest);
		await httpRequest.RequestAsync($"{ApiUrl}/scratch/{scratch.slug}", ["Content-Type: application/json"], HttpClient.Method.Patch, JsonSerializer.Serialize(scratch));
	}

	public static async Task<PresetName> RequestPresetNameAsync(Node parent, int id)
	{
		var httpRequest = new JsonHttpRequest<PresetName>();
		parent.AddChild(httpRequest);
		return await httpRequest.RequestAsync($"{ApiUrl}/preset/{id}/name");
	}

	public static async Task<List<SearchResult>> RequestSearchAsync(Node parent, string search, int pageSize = 5)
	{
		VerifyPageSize(pageSize);
		var httpRequest = new JsonHttpRequest<List<SearchResult>>();
		parent.AddChild(httpRequest);

		string requestStr = $"{ApiUrl}/search?";
		requestStr += $"search={search}";
		if (search != "")
		{
			requestStr += $"&page_size={pageSize}";
		}

		return await httpRequest.RequestAsync(requestStr);
	}

	public static async Task<ManagedHttpRequest.HttpResponse> RequestScratchZipAsync(Node parent, string slug)
	{
		var httpRequest = new ManagedHttpRequest();
		parent.AddChild(httpRequest);
		return await httpRequest.RequestAsync($"{ApiUrl}/scratch/{slug}/export");
	}

	public static async Task<CompileResult> CompileScratchAsync(Node parent, ScratchListItem scratch)
	{
		var httpRequest = new JsonHttpRequest<CompileResult>();
		parent.AddChild(httpRequest);
		return await httpRequest.RequestAsync($"{ApiUrl}/scratch/{scratch.slug}/compile", ["Content-Type: application/json"], HttpClient.Method.Post, JsonSerializer.Serialize(scratch));
	}

	private static void VerifyPageSize(int pageSize)
	{
		if (pageSize > MaxPageSize)
		{
			GD.PushWarning($"Page size cannot be larger than {MaxPageSize}");
		}
	}
}
