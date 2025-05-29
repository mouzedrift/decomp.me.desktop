using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace DecompMeDesktop.Core;

public partial class DecompMeApi : Node
{
	public static DecompMeApi Instance {  get; private set; }
	public const string ApiUrl = "https://decomp.me/api";
	public const int DefaultPageSize = 20;
	public const int MaxPageSize = 100;
	public const bool DebugRequests = false;

	public User CurrentUser { get; private set; }
	[Signal] public delegate void UserReadyEventHandler();

	private JsonRequest<User> _userRequestNode;
	public enum ScratchRequestType
	{
		Url,
		Slug
	}

#nullable enable
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

	public partial class JsonRequest<T> : HttpRequest
	{
		public T Data { get; private set; }
		[Signal] public delegate void DataReceivedEventHandler();

		public override void _Ready()
		{
			RequestCompleted += (long result, long responseCode, string[] headers, byte[] body) =>
			{
				if (DebugRequests)
				{
					GD.Print($"HTTP result: {result} response code: {responseCode}");
				}

				bool isJson = false;
				foreach (var header in headers)
				{
					if (header == "Content-Type: application/json")
					{
						isJson = true;
						break;
					}
				}

				if (!isJson)
				{
					GD.Print("JsonRequest content is not application/json");
					GD.Print(body.GetStringFromUtf8());
					QueueFree();
					return;
				}

				string jsonStr = body.GetStringFromUtf8();
				Data = JsonSerializer.Deserialize<T>(jsonStr);

				SaveCookie(headers);

				EmitSignal(SignalName.DataReceived);
			};
		}

		public new Godot.Error Request(string url, string[] customHeaders = null, HttpClient.Method method = HttpClient.Method.Get, string requestData = "")
		{
			customHeaders = AddHeader(customHeaders, "Accept: application/json");
			customHeaders = AddCookie(customHeaders);
			var error = base.Request(url, customHeaders, method, requestData);
			if (error != Error.Ok)
			{
				GD.PrintErr($"{method} {url} failed with error: {error}");
			}

			if (DebugRequests)
			{
				GD.Print($"{method} {url}");
			}
			return error;
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;

		SceneManager.Instance.PreSceneChange += () =>
		{
			QueueFreeAllRequests();
		};

		_userRequestNode = RequestUser();
		_userRequestNode.DataReceived += () =>
		{
			CurrentUser = _userRequestNode.Data;
			GD.Print($"Logged in as {CurrentUser.username}, anon={CurrentUser.is_anonymous}");
			_userRequestNode.QueueFree();
			_userRequestNode = null;
			EmitSignal(SignalName.UserReady);
		};
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

	public JsonRequest<ScratchListItem> RequestScratch(string input, ScratchRequestType requestType = ScratchRequestType.Url)
	{
		var httpRequest = new JsonRequest<ScratchListItem>();
		AddChild(httpRequest);
		string url = requestType == ScratchRequestType.Url ? input : $"{ApiUrl}/scratch/{input}";
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

	public JsonRequest<ScratchList> RequestUserScratches(User user, int pageSize = DefaultPageSize)
	{
		var httpRequest = new JsonRequest<ScratchList>();
		AddChild(httpRequest);
		
		if (!user.is_anonymous.GetValueOrDefault() && user.github_id != null)
		{
			httpRequest.Request($"{ApiUrl}/users/{user.username}/scratches?page_size={pageSize}&ordering=-creation_time");
		}
		else
		{
			httpRequest.Request($"{ApiUrl}/user/scratches?page_size={pageSize}&ordering=-creation_time");
		}

		return httpRequest;
	}

	public JsonRequest<ScratchListItem> ForkScratch(ScratchListItem scratch)
	{
		var httpRequest = new JsonRequest<ScratchListItem>();
		AddChild(httpRequest);
		httpRequest.Request($"{ApiUrl}/scratch/{scratch.slug}/fork", ["Content-Type: application/json"], HttpClient.Method.Post, JsonSerializer.Serialize(scratch));
		return httpRequest;
	}

	// returns the claimed scratch
	public void ClaimScratch(ScratchListItem scratch)
	{
		if (scratch.claim_token == null || scratch.slug == null)
		{
			GD.PrintErr("Cannot claim scratch!");
			return;
		}

		var httpRequest = new HttpRequest();
		AddChild(httpRequest);
		var claimJson = JsonSerializer.Serialize(new { token = scratch.claim_token });
		httpRequest.Request($"{ApiUrl}/scratch/{scratch.slug}/claim", AddCookie(["Content-Type: application/json"]), HttpClient.Method.Post, claimJson);
		httpRequest.RequestCompleted += (long result, long responseCode, string[] headers, byte[] body) =>
		{
			SaveCookie(headers);
			httpRequest.QueueFree();
		};
	}

	public void DeleteScratch(ScratchListItem scratch)
	{
		var httpRequest = new HttpRequest();
		AddChild(httpRequest);
		httpRequest.Request($"{ApiUrl}/scratch/{scratch.slug}", AddCookie(["Content-Type: application/json"]), HttpClient.Method.Delete);
		httpRequest.RequestCompleted += (long result, long responseCode, string[] headers, byte[] body) =>
		{
			SaveCookie(headers);
			httpRequest.QueueFree();
		};
	}

	public void UpdateScratch(ScratchListItem scratch)
	{
		var httpRequest = new HttpRequest();
		AddChild(httpRequest);
		httpRequest.Request($"{ApiUrl}/scratch/{scratch.slug}", AddCookie(["Content-Type: application/json"]), HttpClient.Method.Patch, JsonSerializer.Serialize(scratch));
		httpRequest.RequestCompleted += (long result, long responseCode, string[] headers, byte[] body) =>
		{
			SaveCookie(headers);
			httpRequest.QueueFree();
		};
	}

	public JsonRequest<PresetName> RequestPresetName(int id)
	{
		var httpRequest = new JsonRequest<PresetName>();
		AddChild(httpRequest);
		httpRequest.Request($"{ApiUrl}/preset/{id}/name");
		return httpRequest;
	}

	public JsonRequest<List<SearchResult>> RequestSearch(string search, int pageSize = 5)
	{
		VerifyPageSize(pageSize);
		var httpRequest = new JsonRequest<List<SearchResult>>();
		AddChild(httpRequest);

		string requestStr = $"{ApiUrl}/search?";
		requestStr += $"search={search}";
		if (search != "")
		{
			requestStr += $"&page_size={pageSize}";
		}

		httpRequest.Request(requestStr);
		return httpRequest;
	}

	public HttpRequest RequestScratchZip(string slug)
	{
		var httpRequest = new HttpRequest();
		AddChild(httpRequest);
		httpRequest.Request($"{ApiUrl}/scratch/{slug}/export");
		return httpRequest;
	}

	public JsonRequest<CompileResult> CompileScratch(ScratchListItem scratch)
	{
		var httpRequest = new JsonRequest<CompileResult>();
		AddChild(httpRequest);
		httpRequest.Request($"{ApiUrl}/scratch/{scratch.slug}/compile", ["Content-Type: application/json"], HttpClient.Method.Post, JsonSerializer.Serialize(scratch));
		return httpRequest;
	}

	private static void VerifyPageSize(int pageSize)
	{
		if (pageSize > MaxPageSize)
		{
			GD.PushWarning($"Page size cannot be larger than {MaxPageSize}");
		}
	}

	private void QueueFreeAllRequests()
	{
		foreach (var child in GetChildren())
		{
			if (_userRequestNode != null && child == _userRequestNode)
			{
				continue;
			}

			if (child is HttpRequest request)
			{
				request.CancelRequest();
				request.QueueFree();
			}
		}
	}
}
