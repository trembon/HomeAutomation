using HomeAutomation.Base.Enums;
using HomeAutomation.Base.Extensions;
using HomeAutomation.Entities.Enums;
using HomeAutomation.Models.Actions;
using HomeAutomation.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Entities.Action
{
    public class StateAction : Action
    {
        public DeviceState State { get; set; }

        public override Task Execute(IActionExecutionArguments arguments)
        {
            List<Task> sendCommandTasks = new(arguments.Devices.Count());
            foreach(var device in arguments.Devices)
            {
                if(device.Source == DeviceSource.Telldus)
                {
                    var telldusAPIService = arguments.GetService<ITelldusAPIService>();
                    var command = telldusAPIService.ConvertStateToCommand(State);

                    if (command.HasValue)
                    {
                        var task = telldusAPIService.SendCommand(int.Parse(device.SourceID), command.Value);
                        sendCommandTasks.Add(task);
                    }
                }

                if(device.Source == DeviceSource.ZWave)
                {
                    var zwaveAPIService = arguments.GetService<IZWaveAPIService>();
                    ZWaveCommandClass? command = zwaveAPIService.ConvertStateToCommand(State, out object parameter);

                    if (command.HasValue)
                    {
                        var task = zwaveAPIService.SendCommand(byte.Parse(device.SourceID), command.Value, parameter);
                        sendCommandTasks.Add(task);
                    }
                }

                if (device.Source == DeviceSource.Tuya)
                {
                    var tuyaAPIService = arguments.GetService<ITuyaAPIService>();
                    int? propertyId = tuyaAPIService.ConvertStateToPropertyId(State, device.GetType(), out object value);

                    if (propertyId.HasValue)
                    {
                        var task = tuyaAPIService.SendCommand(device.SourceID, propertyId.Value, value);
                        sendCommandTasks.Add(task);
                    }
                }
            }

            return Task.WhenAll(sendCommandTasks);
        }
    }
}
