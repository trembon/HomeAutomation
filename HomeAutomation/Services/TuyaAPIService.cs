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

                        // color: HSV value of the wanted color in hex values
                        // hue 0 - 360, saturation 0 - 360, value 0 - 1000
                        // ex: 00DC004B004E > 00DC|004B|004E > hue|saturation|value
                        if (parameters.TryGetValue("color", out string hexColor))
                        {
                            result[21] = "colour";
                            result[24] = ColorTranslator.FromHtml(hexColor).ToHSVString();
                        }
                        else
                        {
                            result[21] = "white";

                            // brightness and temperature is only applicable for white light

                            // brightness: 10-1000 (1-100%)
                            // if not specified, always go for 100%
                            if (parameters.TryGetValue("brightness", out string brightness))
                            {
                                result[22] = int.Parse(brightness) * 10;
                            }
                            else
                            {
                                result[22] = 1000;
                            }

                            // temperature: 0-1000 (0-100%)
                            // if not specified, always go for 100%
                            if (parameters.TryGetValue("temperature", out string temperature))
                            {
                                result[22] = int.Parse(temperature) * 10;
                            }
                            else
                            {
                                result[22] = 1000;
                            }
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
