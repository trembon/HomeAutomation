using HomeAutomation.Base.Enums;
using HomeAutomation.Core.Models;
using HomeAutomation.Database.Entities;
using HomeAutomation.Entities.Enums;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeAutomation.Core.Services
{
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

        public Task<IEnumerable<TelldusDeviceModel>> GetDevices()
        {
            string[] telldusDevices = configuration.GetSection("Telldus:APIURL").Get<string[]>();

            List<TelldusDeviceModel> results = new();
            Parallel.ForEach(telldusDevices, baseUrl =>
            {
                HttpRequestMessage request = new(HttpMethod.Get, $"{baseUrl}devices/");

                var sendTask = httpClient.SendAsync(request);
                sendTask.Wait();

                HttpResponseMessage response = sendTask.Result;
                response.EnsureSuccessStatusCode();

                var readTask = response.Content.ReadFromJsonAsync<TelldusDeviceModel[]>();
                readTask.Wait();
                results.AddRange(readTask.Result);
            });

            return Task.FromResult(results.Distinct());
        }

        public async Task<bool> SendCommand(int id, TelldusDeviceMethods command)
        {
            List<Task<HttpResponseMessage>> tasks = new();

            string[] telldusControllers = configuration.GetSection("Telldus:APIURL").Get<string[]>();
            for (int i = 0; i < telldusControllers.Length; i++)
            {
                // send the request to the controller
                HttpRequestMessage request = new(HttpMethod.Post, $"{telldusControllers[i]}devices/{id}/send/{command}");

                // wait for an OK
                var task = httpClient.SendAsync(request);
                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks);
            return results.All(x => x.IsSuccessStatusCode);
        }

        public Task<TelldusDeviceMethods> GetLastCommand(int id)
        {
            string[] telldusDevices = configuration.GetSection("Telldus:APIURL").Get<string[]>();

            List<TelldusDeviceMethods> results = new();
            Parallel.ForEach(telldusDevices, baseUrl =>
            {
                HttpRequestMessage request = new(HttpMethod.Get, $"{baseUrl}devices/{id}/lastcommand");

                var sendTask = httpClient.SendAsync(request);
                sendTask.Wait();

                HttpResponseMessage response = sendTask.Result;
                response.EnsureSuccessStatusCode();

                var readTask = response.Content.ReadFromJsonAsync<TelldusDeviceMethods>();
                readTask.Wait();
                results.Add(readTask.Result);
            });

            return Task.FromResult(results.FirstOrDefault());
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
}
