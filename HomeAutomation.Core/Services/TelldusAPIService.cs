using HomeAutomation.Base.Enums;
using HomeAutomation.Entities.Enums;
using HomeAutomation.Models.Telldus;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace HomeAutomation.Core.Services
{
    public interface ITelldusAPIService
    {
        Task<bool> SendCommand(int id, TelldusDeviceMethods command);

        Task<IEnumerable<DeviceModel>> GetDevices();

        Task<TelldusDeviceMethods> GetLastCommand(int id);

        DeviceEvent ConvertCommandToEvent(TelldusDeviceMethods command);

        TelldusDeviceMethods? ConvertStateToCommand(DeviceState state);
    }

    public class TelldusAPIService : ITelldusAPIService
    {
        private readonly HttpClient httpClient;
        private readonly IConfiguration configuration;

        public TelldusAPIService(IConfiguration configuration)
        {
            this.configuration = configuration;
            this.httpClient = new HttpClient();
        }

        public Task<IEnumerable<DeviceModel>> GetDevices()
        {
            string[] telldusDevices = configuration.GetSection("Telldus:APIURL").Get<string[]>();

            List<DeviceModel> results = new();
            Parallel.ForEach(telldusDevices, baseUrl =>
            {
                HttpRequestMessage request = new(HttpMethod.Get, $"{baseUrl}devices/");

                var sendTask = httpClient.SendAsync(request);
                sendTask.Wait();

                HttpResponseMessage response = sendTask.Result;
                response.EnsureSuccessStatusCode();

                var readTask = response.Content.ReadAsAsync<IEnumerable<DeviceModel>>();
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

                var readTask = response.Content.ReadAsAsync<TelldusDeviceMethods>();
                readTask.Wait();
                results.Add(readTask.Result);
            });

            return Task.FromResult(results.FirstOrDefault());
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
