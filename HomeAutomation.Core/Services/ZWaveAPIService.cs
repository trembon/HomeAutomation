using HomeAutomation.Base.Enums;
using HomeAutomation.Entities.Devices;
using HomeAutomation.Entities.Enums;
using HomeAutomation.Models.ZWave;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace HomeAutomation.Core.Services
{
    public interface IZWaveAPIService
    {
        Task<IEnumerable<NodeModel>> GetNodes();

        Task<bool> SendCommand(byte id, ZWaveCommandClass command, object value);

        ZWaveCommandClass? ConvertStateToCommand(DeviceState state, out object value);

        DeviceEvent ConvertParameterToEvent(Type deviceType, ZWaveEventParameter parameter, object value);
    }

    public class ZWaveAPIService : IZWaveAPIService
    {
        private readonly HttpClient httpClient;
        private readonly IConfiguration configuration;

        public ZWaveAPIService(IConfiguration configuration)
        {
            this.httpClient = new HttpClient();
            this.configuration = configuration;
        }

        public async Task<IEnumerable<NodeModel>> GetNodes()
        {
            string baseUrl = configuration.GetSection("ZWave:APIUrl").Get<string>();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}nodes/");

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<NodeModel>>();
        }

        public async Task<bool> SendCommand(byte id, ZWaveCommandClass command, object parameter)
        {
            string baseUrl = configuration.GetSection("ZWave:APIUrl").Get<string>();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}nodes/{id}/send/{command}?parameter={parameter}");

            var sendResult = await httpClient.SendAsync(request);
            sendResult.EnsureSuccessStatusCode();

            return true; // TODO: check for real response
        }

        public ZWaveCommandClass? ConvertStateToCommand(DeviceState state, out object value)
        {
            switch (state)
            {
                case DeviceState.On:
                    value = 255;
                    return ZWaveCommandClass.Basic;

                case DeviceState.Off:
                    value = 0;
                    return ZWaveCommandClass.Basic;
            }

            value = null;
            return null;
        }

        public DeviceEvent ConvertParameterToEvent(Type deviceType, ZWaveEventParameter parameter, object value)
        {
            if(value is JsonElement je)
            {
                switch (je.ValueKind)
                {
                    case JsonValueKind.String:
                        value = je.GetString();
                        break;

                    case JsonValueKind.Number:
                        if (je.TryGetInt32(out int intValue))
                        {
                            value = intValue;
                        }
                        else if (je.TryGetDecimal(out decimal decimalValue))
                        {
                            value = decimalValue;
                        }
                        break;

                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        value = je.GetBoolean();
                        break;

                    default:
                        value = null;
                        break;
                }
            }

            if(deviceType == typeof(MotionSensorDevice))
            {
                if (parameter == ZWaveEventParameter.SensorMotion)
                    return DeviceEvent.Motion;

                if (parameter == ZWaveEventParameter.AlarmGeneric)
                    return DeviceEvent.Off;
            }

            switch (value)
            {
                case int intValue:
                    if (parameter == ZWaveEventParameter.Basic)
                        return intValue == 255 ? DeviceEvent.On : DeviceEvent.Off;

                    if (parameter == ZWaveEventParameter.SwitchBinary)
                        return intValue == 255 ? DeviceEvent.On : DeviceEvent.Off;

                    break;

                case byte byteValue:
                    if (parameter == ZWaveEventParameter.Basic)
                        return byteValue == 255 ? DeviceEvent.On : DeviceEvent.Off;

                    if (parameter == ZWaveEventParameter.SwitchBinary)
                        return byteValue == 255 ? DeviceEvent.On : DeviceEvent.Off;

                    break;
            }

            return DeviceEvent.Unknown;
        }
    }
}
