﻿using HomeAutomation.Base.Enums;
using HomeAutomation.Core.Models;
using HomeAutomation.Database.Entities;
using HomeAutomation.Entities.Enums;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeAutomation.Core.Services;

public interface ITelldusAPIService
{
    event Action<TelldusEventModel> TelldusEventReceived;

    event Action<TelldusEventModel> TelldusRawEventReceived;

    Task<bool> SendCommand(int id, TelldusDeviceMethods command);

    Task<IEnumerable<TelldusDeviceModel>> GetDevices();

    Task<TelldusDeviceMethods> GetLastCommand(int id);

    void SendLogMessage(string message);

    void SendLogMessage(string message, DateTime timestamp);

    void SendRawLogMessage(string message);

    DeviceEvent ConvertCommandToEvent(TelldusDeviceMethods command);

    TelldusDeviceMethods? ConvertStateToCommand(DeviceState state);
}

public class TelldusAPIService(IConfiguration configuration) : ITelldusAPIService
{
    private readonly HttpClient httpClient = new();
    private readonly IConfiguration configuration = configuration;

    public event Action<TelldusEventModel> TelldusEventReceived;

    public event Action<TelldusEventModel> TelldusRawEventReceived;

    public async Task<IEnumerable<TelldusDeviceModel>> GetDevices()
    {
        string baseUrl = configuration.GetSection("Telldus:APIUrl").Get<string>();

        HttpRequestMessage request = new(HttpMethod.Get, $"{baseUrl}devices/");

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TelldusDeviceModel[]>();
    }

    public async Task<bool> SendCommand(int id, TelldusDeviceMethods command)
    {
        string baseUrl = configuration.GetSection("Telldus:APIUrl").Get<string>();

        // send the request to the controller
        HttpRequestMessage request = new(HttpMethod.Post, $"{baseUrl}devices/{id}/send/{command}");

        // wait for an OK
        var result = await httpClient.SendAsync(request);

        return result.IsSuccessStatusCode;
    }

    public async Task<TelldusDeviceMethods> GetLastCommand(int id)
    {
        string baseUrl = configuration.GetSection("Telldus:APIUrl").Get<string>();

        HttpRequestMessage request = new(HttpMethod.Get, $"{baseUrl}devices/{id}/lastcommand");

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TelldusDeviceMethods>();
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
        switch (command)
        {
            case TelldusDeviceMethods.TurnOn: return DeviceEvent.On;
            case TelldusDeviceMethods.TurnOff: return DeviceEvent.Off;

            default: return DeviceEvent.Unknown;
        }
    }

    public TelldusDeviceMethods? ConvertStateToCommand(DeviceState state)
    {
        switch (state)
        {
            case DeviceState.On: return TelldusDeviceMethods.TurnOn;
            case DeviceState.Off: return TelldusDeviceMethods.TurnOff;
        }
        return null;
    }
}
