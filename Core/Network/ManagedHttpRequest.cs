using Godot;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DecompMeDesktop.Core.Network;

public partial class ManagedHttpRequest : HttpRequest
{
	public class HttpResponse
	{
		public HttpRequest.Result Result { get; set; }
		public long ResponseCode { get; set; }
		public string[] Headers { get; set; }
		public byte[] Body { get; set; }
	}

	public const bool DebugRequests = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

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

	public async Task<HttpResponse> RequestAsync(string url, string[] customHeaders = null, HttpClient.Method method = HttpClient.Method.Get, string requestData = "")
	{
		var tcs = new TaskCompletionSource<HttpResponse>();

		void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
		{
			RequestCompleted -= OnRequestCompleted;
			QueueFree();

			SaveCookie(headers);

			var response = new HttpResponse
			{
				Result = (HttpRequest.Result)result,
				ResponseCode = responseCode,
				Headers = headers,
				Body = body
			};

			tcs.SetResult(response);
		}

		RequestCompleted += OnRequestCompleted;

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
		
		return await tcs.Task;
	}
}

public partial class JsonHttpRequest<T> : ManagedHttpRequest
{
	public new async Task<T> RequestAsync(string url, string[] customHeaders = null, HttpClient.Method method = HttpClient.Method.Get, string requestData = "")
	{
		customHeaders = AddHeader(customHeaders, "Accept: application/json");
		if (method == HttpClient.Method.Post || method == HttpClient.Method.Patch)
		{
			customHeaders = AddHeader(customHeaders, "Content-Type: application/json");
		}

		var result = await base.RequestAsync(url, customHeaders, method, requestData);

		bool isJson = false;
		foreach (var header in result.Headers)
		{
			if (header == "Content-Type: application/json")
			{
				isJson = true;
				break;
			}
		}

		if (!isJson)
		{
			GD.Print("JsonHttpRequest content is not application/json");
			GD.Print(result.Body.GetStringFromUtf8());
			return default;
		}

		string jsonStr = result.Body.GetStringFromUtf8();
		return JsonSerializer.Deserialize<T>(jsonStr);
	}
}