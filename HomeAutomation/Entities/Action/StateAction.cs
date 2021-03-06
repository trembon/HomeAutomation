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

        public override async Task Execute(ActionExecutionArguments arguments)
        {
            foreach(var device in arguments.Devices)
            {
                if(device.Source == DeviceSource.Telldus)
                {
                    var telldusAPIService = arguments.GetService<ITelldusAPIService>();
                    var command = telldusAPIService.ConvertStateToCommand(State);

                    if(command.HasValue)
                        await arguments.GetService<ITelldusAPIService>().SendCommand(int.Parse(device.SourceID), command.Value);
                }

                if(device.Source == DeviceSource.ZWave)
                {
                    var zwaveAPIService = arguments.GetService<IZWaveAPIService>();
                    ZWaveCommandClass? command = zwaveAPIService.ConvertStateToCommand(State, out object parameter);

                    if (command.HasValue)
                        await arguments.GetService<IZWaveAPIService>().SendCommand(byte.Parse(device.SourceID), command.Value, parameter);
                }
            }
        }
    }
}
