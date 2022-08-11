using HomeAutomation.Entities.Devices;
using HomeAutomation.Entities.Enums;
using HomeAutomation.Models.Tuya;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace HomeAutomation.Services
{
    public interface ITuyaAPIService
    {
        Task<IEnumerable<DeviceModel>> GetDevices();

        Task<bool> SendCommand(string deviceId, int propertyId, object value);

        int? ConvertStateToPropertyId(DeviceState state, Type deviceType, out object value);

        DeviceEvent ConvertPropertyToEvent(int propertyId, Type deviceType, object value);
    }

    public class TuyaAPIService : ITuyaAPIService
    {
        private readonly HttpClient httpClient;
        private readonly IConfiguration configuration;

        public TuyaAPIService(IConfiguration configuration)
        {
            this.httpClient = new HttpClient();
            this.configuration = configuration;
        }

        public DeviceEvent ConvertPropertyToEvent(int propertyId, Type deviceType, object value)
        {
            switch (deviceType.Name)
            {
                case nameof(LightbulbDevice):
                    if (propertyId == 20)
                    {
                        return Convert.ToBoolean(value) ? DeviceEvent.On : DeviceEvent.Off;
                    }
                    break;

                case nameof(PowerSwitchDevice):
                    if (propertyId == 1)
                    {
                        return Convert.ToBoolean(value) ? DeviceEvent.On : DeviceEvent.Off;
                    }
                    break;
            }

            return DeviceEvent.Unknown;
        }

        public int? ConvertStateToPropertyId(DeviceState state, Type deviceType, out object value)
        {
            switch (deviceType.Name)
            {
                case nameof(LightbulbDevice):
                    if (state == DeviceState.On)
                    {
                        value = true;
                        return 20;
                    }
                    if (state == DeviceState.Off)
                    {
                        value = false;
                        return 20;
                    }
                    break;

                case nameof(PowerSwitchDevice):
                    if (state == DeviceState.On)
                    {
                        value = true;
                        return 1;
                    }
                    if (state == DeviceState.Off)
                    {
                        value = false;
                        return 1;
                    }
                    break;
            }

            value = null;
            return null;
        }

        public async Task<IEnumerable<DeviceModel>> GetDevices()
        {
            string baseUrl = configuration.GetSection("Tuya:APIUrl").Get<string>();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}devices/");

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<DeviceModel>>();
        }

        public async Task<bool> SendCommand(string deviceId, int propertyId, object value)
        {
            string baseUrl = configuration.GetSection("Tuya:APIUrl").Get<string>();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}devices/{deviceId}/send")
            {
                Content = JsonContent.Create(new { dps = propertyId.ToString(), set = value })
            };

            var sendResult = await httpClient.SendAsync(request);
            sendResult.EnsureSuccessStatusCode();

            return true; // TODO: check for real response
        }
    }
}
