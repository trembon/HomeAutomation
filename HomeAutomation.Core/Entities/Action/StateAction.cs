using HomeAutomation.Base.Enums;
using HomeAutomation.Core.Entities;
using HomeAutomation.Core.Services;
using HomeAutomation.Database.Enums;
using HomeAutomation.Entities.Enums;

namespace HomeAutomation.Entities.Action;

public class StateAction : Action
{
    public DeviceState State { get; set; }

    public Dictionary<string, string> Parameters { get; set; }

    public override Task Execute(IActionExecutionArguments arguments)
    {
        List<Task> sendCommandTasks = new(arguments.Devices.Count());
        foreach (var device in arguments.Devices)
        {
            if (device.Source == DeviceSource.Telldus)
            {
                var telldusAPIService = arguments.GetService<ITelldusAPIService>();
                var command = telldusAPIService.ConvertStateToCommand(State);

                if (command.HasValue)
                {
                    var task = telldusAPIService.SendCommand(int.Parse(device.SourceID), command.Value);
                    sendCommandTasks.Add(task);
                }
            }

            if (device.Source == DeviceSource.ZWave)
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
                var dpsData = tuyaAPIService.ConvertStateToDPS(State, device.GetType(), Parameters);

                if (dpsData.Any())
                {
                    var task = tuyaAPIService.SendCommand(device.SourceID, dpsData);
                    sendCommandTasks.Add(task);
                }
            }
        }

        return Task.WhenAll(sendCommandTasks);
    }
}
