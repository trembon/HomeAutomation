using HomeAutomation.Core.Models;
using HomeAutomation.Database.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace HomeAutomation.Core.Services;

public interface IIkeaDirigeraService
{
    event Action<IkeaDirigeraEventModel> EventReceived;

    Task<IEnumerable<IkeaDirigeraDeviceModel>> GetDevices(CancellationToken cancellationToken = default);

    Task<bool> SendAction(string deviceId, Dictionary<string, object?> payload, CancellationToken cancellationToken = default);

    Dictionary<string, object?> ConvertStateToAction(DeviceEvent state);

    IAsyncEnumerable<IkeaDirigeraEventModel> ListenToEvents(CancellationToken cancellationToken = default);
}

public class IkeaDirigeraService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<IkeaDirigeraService> logger) : IIkeaDirigeraService
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public event Action<IkeaDirigeraEventModel>? EventReceived;

    private string GetToken()
    {
        return configuration.GetValue<string>("IkeaDirigera:Token") ?? throw new InvalidOperationException("IkeaDirigera:Token is missing.");
    }

    public async Task<IEnumerable<IkeaDirigeraDeviceModel>> GetDevices(CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient(nameof(IkeaDirigeraService));
        string token = GetToken();

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUrl("devices"));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseData = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions, cancellationToken);
        if (responseData.ValueKind != JsonValueKind.Array)
            return [];

        List<IkeaDirigeraDeviceModel> devices = [];
        foreach (var item in responseData.EnumerateArray())
        {
            string id = TryGetString(item, "id") ?? string.Empty;
            string type = TryGetString(item, "type") ?? string.Empty;
            string name = TryGetNestedString(item, ["attributes", "customName"])
                ?? TryGetNestedString(item, ["attributes", "deviceName"])
                ?? id;

            bool isReachable = TryGetNestedBool(item, ["attributes", "isReachable"]) ?? true;

            if (!string.IsNullOrWhiteSpace(id))
            {
                devices.Add(new IkeaDirigeraDeviceModel
                {
                    Id = id,
                    Type = type,
                    Name = name,
                    IsReachable = isReachable
                });
            }
        }

        return devices;
    }

    public async Task<bool> SendAction(string deviceId, Dictionary<string, object?> payload, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        var httpClient = httpClientFactory.CreateClient(nameof(IkeaDirigeraService));
        string token = GetToken();

        using var request = new HttpRequestMessage(HttpMethod.Patch, BuildUrl($"devices/{deviceId}"))
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return true;
    }

    public Dictionary<string, object?> ConvertStateToAction(DeviceEvent state)
    {
        if (state == DeviceEvent.On)
        {
            return new Dictionary<string, object?>
            {
                ["attributes"] = new Dictionary<string, object?>
                {
                    ["isOn"] = true
                }
            };
        }

        if (state == DeviceEvent.Off)
        {
            return new Dictionary<string, object?>
            {
                ["attributes"] = new Dictionary<string, object?>
                {
                    ["isOn"] = false
                }
            };
        }

        return [];
    }

    public async IAsyncEnumerable<IkeaDirigeraEventModel> ListenToEvents([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string token = GetToken();

        using ClientWebSocket socket = new();
        socket.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;
        socket.Options.SetRequestHeader("Authorization", $"Bearer {token}");

        string eventUrl = BuildEventUrl();
        await socket.ConnectAsync(new Uri(eventUrl), cancellationToken);

        byte[] buffer = new byte[8 * 1024];
        using MemoryStream messageBuffer = new();

        while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            messageBuffer.SetLength(0);
            WebSocketReceiveResult result;

            do
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                    yield break;

                await messageBuffer.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
            }
            while (!result.EndOfMessage);

            string payload = Encoding.UTF8.GetString(messageBuffer.ToArray());
            var mapped = TryParseEvent(payload);
            if (mapped is not null)
            {
                EventReceived?.Invoke(mapped);
                yield return mapped;
            }
        }
    }

    private IkeaDirigeraEventModel? TryParseEvent(string payload)
    {
        try
        {
            using var jsonDocument = JsonDocument.Parse(payload);
            JsonElement root = jsonDocument.RootElement;

            string? deviceId = TryGetNestedString(root, ["data", "id"])
                ?? TryGetNestedString(root, ["attributes", "id"])
                ?? TryGetString(root, "id");

            Dictionary<string, string?> attributeDictionary = [];
            if (root.TryGetProperty("data", out JsonElement data) && data.TryGetProperty("attributes", out JsonElement attributes) && attributes.ValueKind == JsonValueKind.Object)
            {
                attributeDictionary = attributes
                    .EnumerateObject()
                    .ToDictionary(x => x.Name, x => x.Value.GetString());
            }

            DateTime timestamp = DateTime.UtcNow;
            string? ts = TryGetString(root, "time") ?? TryGetNestedString(root, ["data", "lastSeen"]);

            if (!string.IsNullOrWhiteSpace(ts) && DateTime.TryParse(ts, out DateTime parsedDate))
                timestamp = parsedDate;

            return new IkeaDirigeraEventModel
            {
                DeviceId = deviceId,
                Attributes = attributeDictionary,
                Timestamp = timestamp,
                RawPayload = payload
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "IkeaDirigera.ParseEvent failed");
            return null;
        }
    }

    private string BuildUrl(string path)
    {
        string? configuredIP = configuration["IkeaDirigera:IP"];
        if (string.IsNullOrWhiteSpace(configuredIP))
            throw new InvalidOperationException("IkeaDirigera:IP is missing.");

        return $"https://{configuredIP}:8443/v1/{path}";
    }

    private string BuildEventUrl()
    {
        string apiUrl = BuildUrl("events");
        if (apiUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return "wss://" + apiUrl[8..];

        if (apiUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            return "ws://" + apiUrl[7..];

        return apiUrl;
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
            return property.GetString();

        return null;
    }

    private static string? TryGetNestedString(JsonElement element, string[] path)
    {
        JsonElement current = element;
        foreach (string segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
                return null;
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
    }

    private static bool? TryGetNestedBool(JsonElement element, string[] path)
    {
        JsonElement current = element;
        foreach (string segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
                return null;
        }

        return current.ValueKind == JsonValueKind.True || current.ValueKind == JsonValueKind.False
            ? current.GetBoolean()
            : null;
    }
}
