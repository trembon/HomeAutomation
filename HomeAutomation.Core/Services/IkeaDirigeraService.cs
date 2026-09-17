using HomeAutomation.Core.Models;
using HomeAutomation.Database.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Globalization;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace HomeAutomation.Core.Services;

public interface IIkeaDirigeraService
{
    event Action<IkeaDirigeraEventModel>? EventReceived;

    Task<IEnumerable<IkeaDirigeraDeviceModel>> GetDevices(CancellationToken cancellationToken = default);

    Task<bool> SendAction(string deviceId, object payload, CancellationToken cancellationToken = default);

    IReadOnlyList<object> ConvertStateToActions(DeviceEvent state, DeviceKind deviceKind, Dictionary<string, string> parameters);

    IAsyncEnumerable<IkeaDirigeraEventModel> ListenToEvents(CancellationToken cancellationToken = default);
}

public class IkeaDirigeraService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<IkeaDirigeraService> logger) : IIkeaDirigeraService
{
    private const int ColorTemperatureMin = 2202;
    private const int ColorTemperatureMax = 6535;

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

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
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

    public async Task<bool> SendAction(string deviceId, object payload, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        var httpClient = httpClientFactory.CreateClient(nameof(IkeaDirigeraService));
        string token = GetToken();

        using var request = new HttpRequestMessage(HttpMethod.Patch, BuildUrl($"devices/{deviceId}"))
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("IkeaDirigera.Action :: Device:{deviceId}, StatusCode:{statusCode}, Payload:{payload}", deviceId, (int)response.StatusCode, JsonSerializer.Serialize(payload));

        response.EnsureSuccessStatusCode();
        return true;
    }

    public IReadOnlyList<object> ConvertStateToActions(DeviceEvent state, DeviceKind deviceKind, Dictionary<string, string> parameters)
    {
        if (state != DeviceEvent.On && state != DeviceEvent.Off)
            return [];

        List<object> actions =
        [
            CreateAction(new Dictionary<string, object>
            {
                ["isOn"] = state == DeviceEvent.On
            })
        ];

        if (state == DeviceEvent.On && deviceKind == DeviceKind.Lightbulb)
            AddLightActions(actions, parameters);

        return actions;
    }

    private static void AddLightActions(List<object> actions, Dictionary<string, string> parameters)
    {
        if (parameters.TryGetValue("color", out string? hexColor) && !string.IsNullOrWhiteSpace(hexColor))
        {
            Color color = ParseHexColor(hexColor);
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            actions.Add(CreateAction(new Dictionary<string, object>
            {
                ["colorHue"] = (double)color.GetHue(),
                ["colorSaturation"] = max == 0 ? 0d : 1d - (double)min / max
            }));
            actions.Add(CreateAction(new Dictionary<string, object>
            {
                ["lightLevel"] = Math.Clamp((int)Math.Round(max / 255d * 100, MidpointRounding.AwayFromZero), 1, 100)
            }));
            return;
        }

        if (parameters.TryGetValue("temperature", out string? temperature) && !string.IsNullOrWhiteSpace(temperature))
        {
            int percentage = ParsePercentage(temperature, "temperature");
            actions.Add(CreateAction(new Dictionary<string, object>
            {
                ["colorTemperature"] = ColorTemperatureMin + (int)Math.Round(
                    (ColorTemperatureMax - ColorTemperatureMin) * percentage / 100d,
                    MidpointRounding.AwayFromZero)
            }));
        }

        if (parameters.TryGetValue("brightness", out string? brightness) && !string.IsNullOrWhiteSpace(brightness))
        {
            actions.Add(CreateAction(new Dictionary<string, object>
            {
                ["lightLevel"] = Math.Max(1, ParsePercentage(brightness, "brightness"))
            }));
        }
    }

    private static object[] CreateAction(Dictionary<string, object> attributes)
    {
        return [new { attributes }];
    }

    private static Color ParseHexColor(string value)
    {
        string hexColor = value.Trim();
        if (hexColor.Length != 7 || hexColor[0] != '#' || hexColor.Skip(1).Any(c => !Uri.IsHexDigit(c)))
            throw new ArgumentException("Color must be a hexadecimal value in the format #RRGGBB.", nameof(value));

        return ColorTranslator.FromHtml(hexColor);
    }

    private static int ParsePercentage(string value, string parameterName)
    {
        if (!int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int percentage) || percentage < 0 || percentage > 100)
            throw new ArgumentException($"{parameterName} must be an integer between 0 and 100.", parameterName);

        return percentage;
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

            Dictionary<string, object> attributeDictionary = [];
            if (root.TryGetProperty("data", out JsonElement data) && data.TryGetProperty("attributes", out JsonElement attributes) && attributes.ValueKind == JsonValueKind.Object)
            {
                foreach (var attribute in attributes.EnumerateObject())
                {
                    string key = attribute.Name;
                    if (attribute.Value.ValueKind == JsonValueKind.String)
                    {
                        string? value = attribute.Value.GetString();
                        attributeDictionary[key] = value ?? "";
                    }
                    else if (attribute.Value.ValueKind == JsonValueKind.Number)
                    {
                        attributeDictionary[key] = attribute.Value.GetDecimal();
                    }
                    else if (attribute.Value.ValueKind == JsonValueKind.True || attribute.Value.ValueKind == JsonValueKind.False)
                    {
                        attributeDictionary[key] = attribute.Value.GetBoolean();
                    }
                }
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
