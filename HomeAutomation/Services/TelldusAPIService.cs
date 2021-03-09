using HomeAutomation.Base.Enums;
using HomeAutomation.Base.Extensions;
using HomeAutomation.Entities;
using HomeAutomation.Entities.Enums;
using HomeAutomation.Models.Telldus;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace HomeAutomation.Services
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

        private object queueLock = new object();
        private Queue<QueueItem> sendQueue;
        private bool isQueueProcessing = false;

        private class QueueItem
        {
            public int DeviceID { get; set; }

            public TelldusDeviceMethods Command { get; set; }

            public string ControllerUrl { get; set; }

            public override string ToString()
            {
                return $"ID: {DeviceID} - {Command}";
            }
        }

        public TelldusAPIService(IConfiguration configuration)
        {
            this.configuration = configuration;
            this.httpClient = new HttpClient();

            this.sendQueue = new Queue<QueueItem>();
        }

        public Task<IEnumerable<DeviceModel>> GetDevices()
        {
            string[] telldusDevices = configuration.GetSection("Telldus:APIURL").Get<string[]>();

            List<DeviceModel> results = new List<DeviceModel>();
            Parallel.ForEach(telldusDevices, baseUrl =>
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}devices/");

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
        
        public Task<bool> SendCommand(int id, TelldusDeviceMethods command)
        {
            lock (queueLock)
            {
                // if same device is already in queue, remove it
                if(sendQueue.Any(i => i.DeviceID == id))
                    sendQueue = sendQueue.Where(i => i.DeviceID != id).ToQueue();

                string[] telldusControllers = configuration.GetSection("Telldus:APIURL").Get<string[]>();
                for(int i = 0; i < telldusControllers.Length; i++)
                {
                    QueueItem item = new QueueItem { DeviceID = id, Command = command, ControllerUrl = telldusControllers[i] };

                    // add one first to queue, so it will be executed next
                    var list = sendQueue.ToList();
                    list.Insert(i, item);
                    sendQueue = list.ToQueue();

                    // and then the other last in the queue, so total 2 tries per controller
                    sendQueue.Enqueue(item);
                }

                if (!isQueueProcessing)
                {
                    isQueueProcessing = true;
                    Task.Run(ExecuteSendCommand);
                }
            }

            return Task.FromResult(true);
        }

        private async Task ExecuteSendCommand()
        {
            // get the item from the queue
            QueueItem item = null;
            lock (queueLock)
            {
                item = sendQueue.Dequeue();
            }

            // send the request to the controller
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{item.ControllerUrl}devices/{item.DeviceID}/send/{item.Command}");

            // wait for an OK
            var sendResult = await httpClient.SendAsync(request);
            sendResult.EnsureSuccessStatusCode();

            // when done sending command, check if more is in the queue and then continue processing
            lock (queueLock)
            {
                if(sendQueue.Count > 0)
                {
                    // get count of controllers
                    int controllerCount = configuration.GetSection("Telldus:APIURL").Get<string[]>().Length;

                    // calculate delay, should be about 3 second delay between each controller command send
                    int delay = 3000 / controllerCount;

                    Task.Delay(delay).ContinueWith(t => ExecuteSendCommand());
                }
                else
                {
                    isQueueProcessing = false;
                }
            }
        }

        public Task<TelldusDeviceMethods> GetLastCommand(int id)
        {
            string[] telldusDevices = configuration.GetSection("Telldus:APIURL").Get<string[]>();

            List<TelldusDeviceMethods> results = new List<TelldusDeviceMethods>();
            Parallel.ForEach(telldusDevices, baseUrl =>
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}devices/{id}/lastcommand");

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
