using HomeAutomation.Core.Enums;
using HomeAutomation.Core.Extensions;
using HomeAutomation.Core.Models;
using HomeAutomation.Database.Enums;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace HomeAutomation.Core.Services;

public interface ITelldusAPIService
{
    event Action<TelldusEventModel>? TelldusEventReceived;

    event Action<TelldusEventModel>? TelldusRawEventReceived;

    Task<bool> SendCommand(int id, TelldusDeviceMethods command, CancellationToken cancellationToken = default);

    Task<IEnumerable<TelldusDeviceModel>> GetDevices(CancellationToken cancellationToken = default);

    Task<TelldusDeviceMethods> GetLastCommand(int id, CancellationToken cancellationToken = default);

    void SendLogMessage(string message);

    void SendLogMessage(string message, DateTime timestamp);

    void SendRawLogMessage(string message);

    DeviceEvent ConvertCommandToEvent(TelldusDeviceMethods command);

    TelldusDeviceMethods? ConvertStateToCommand(DeviceEvent state);
}

public class TelldusAPIService(IConfiguration configuration, IHttpClientFactory httpClientFactory) : ITelldusAPIService
{
    public event Action<TelldusEventModel>? TelldusEventReceived;

    public event Action<TelldusEventModel>? TelldusRawEventReceived;

    public async Task<IEnumerable<TelldusDeviceModel>> GetDevices(CancellationToken cancellationToken = default)
    {
        Uri requestUri = new(configuration.GetRequiredHttpBaseUri("Telldus:APIUrl"), "devices/");
        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
        HttpClient httpClient = httpClientFactory.CreateClient(nameof(TelldusAPIService));
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength == 0)
            return [];

        return await response.Content.ReadFromJsonAsync<TelldusDeviceModel[]>(cancellationToken) ?? [];
    }

    public async Task<bool> SendCommand(int id, TelldusDeviceMethods command, CancellationToken cancellationToken = default)
    {
        Uri requestUri = new(configuration.GetRequiredHttpBaseUri("Telldus:APIUrl"), $"devices/{id}/send/{command}");
        using HttpRequestMessage request = new(HttpMethod.Post, requestUri);
        HttpClient httpClient = httpClientFactory.CreateClient(nameof(TelldusAPIService));
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

        return response.IsSuccessStatusCode;
    }

    public async Task<TelldusDeviceMethods> GetLastCommand(int id, CancellationToken cancellationToken = default)
    {
        Uri requestUri = new(configuration.GetRequiredHttpBaseUri("Telldus:APIUrl"), $"devices/{id}/lastcommand");
        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
        HttpClient httpClient = httpClientFactory.CreateClient(nameof(TelldusAPIService));
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TelldusDeviceMethods>(cancellationToken);
    }

    public void SendLogMessage(string message)
    {
        SendLogMessage(message, DateTime.Now);
    }

    public void SendLogMessage(string message, DateTime timestamp)
    {
        TelldusEventReceived?.Invoke(new TelldusEventModel { Message = message, Timestamp = timestamp });
    }

    public void SendRawLogMessage(string message)
    {
        TelldusRawEventReceived?.Invoke(new TelldusEventModel { Message = message, Timestamp = DateTime.Now });
    }

    public DeviceEvent ConvertCommandToEvent(TelldusDeviceMethods command)
    {
        return command switch
        {
            TelldusDeviceMethods.TurnOn => DeviceEvent.On,
            TelldusDeviceMethods.TurnOff => DeviceEvent.Off,
            _ => DeviceEvent.Unknown
        };
    }

    public TelldusDeviceMethods? ConvertStateToCommand(DeviceEvent state)
    {
        return state switch
        {
            DeviceEvent.On => TelldusDeviceMethods.TurnOn,
            DeviceEvent.Off => TelldusDeviceMethods.TurnOff,
            _ => null
        };
    }
}
