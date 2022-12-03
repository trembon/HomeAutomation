using HomeAutomation.Base.Extensions;
using HomeAutomation.Entities.Devices;
using HomeAutomation.Entities.Enums;
using HomeAutomation.Models.Tuya;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace HomeAutomation.Services
{
    public interface ITuyaAPIService
    {
        Task<IEnumerable<DeviceModel>> GetDevices();

        Task<bool> SendCommand(string deviceId, Dictionary<int, object> dps);

        Dictionary<int, object> ConvertStateToDPS(DeviceState state, Type deviceType, Dictionary<string, string> parameters);

        DeviceEvent ConvertPropertyToEvent(Type deviceType, Dictionary<int, object> dps);
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

        public DeviceEvent ConvertPropertyToEvent(Type deviceType, Dictionary<int, object> dps)
        {
            switch (deviceType.Name)
            {
                case nameof(LightbulbDevice):
                    if (dps.ContainsKey(20))
                    {
                        return Convert.ToBoolean(dps[20]) ? DeviceEvent.On : DeviceEvent.Off;
                    }
                    break;

                case nameof(PowerSwitchDevice):
                    if (dps.ContainsKey(1))
                    {
                        return Convert.ToBoolean(dps[1]) ? DeviceEvent.On : DeviceEvent.Off;
                    }
                    break;
            }

            return DeviceEvent.Unknown;
        }

        public Dictionary<int, object> ConvertStateToDPS(DeviceState state, Type deviceType, Dictionary<string, string> parameters)
        {
            Dictionary<int, object> result = new();

            switch (deviceType.Name)
            {
                case nameof(LightbulbDevice):
                    if (state == DeviceState.On)
                    {
                        result[20] = true;
                        result[22] = 1000; // brightness: 10-1000 (1-100%)
                        //result["23"] = 1000; // temperature: 0-1000 (0-100%)
                        if (parameters.TryGetValue("color", out string hexColor))
                        {
                            result[21] = "color";
                            result[24] = ColorTranslator.FromHtml(hexColor).ToHSVString();
                        }
                        else
                        {
                            result[21] = "white";
                        }
                    }
                    if (state == DeviceState.Off)
                    {
                        result[20] = false;
                        result[21] = "white";
                    }
                    break;

                case nameof(PowerSwitchDevice):
                    if (state == DeviceState.On)
                    {
                        result[1] = true;
                    }
                    if (state == DeviceState.Off)
                    {
                        result[1] = false;
                    }
                    break;
            }

            return result;
        }

        public async Task<IEnumerable<DeviceModel>> GetDevices()
        {
            string baseUrl = configuration.GetSection("Tuya:APIUrl").Get<string>();
            HttpRequestMessage request = new(HttpMethod.Get, $"{baseUrl}devices/");

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<DeviceModel>>();
        }

        public async Task<bool> SendCommand(string deviceId, Dictionary<int, object> dps)
        {
            string baseUrl = configuration.GetSection("Tuya:APIUrl").Get<string>();
            HttpRequestMessage request = new(HttpMethod.Post, $"{baseUrl}devices/{deviceId}/send")
            {
                Content = JsonContent.Create(new { data = dps })
            };

            var sendResult = await httpClient.SendAsync(request);
            sendResult.EnsureSuccessStatusCode();

            return true; // TODO: check for real response
        }
    }
}
