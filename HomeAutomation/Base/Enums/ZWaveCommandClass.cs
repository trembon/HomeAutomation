using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Base.Enums
{
    public enum ZWaveCommandClass : byte
    {
        NotSet = 0,
        Basic = 32,
        SwitchBinary = 37,
        SwitchMultilevel = 38,
        SwitchAll = 39,
        SceneActivation = 43,
        SensorBinary = 48,
        SensorMultilevel = 49,
        Meter = 50,
        ThermostatHeating = 56,
        ThermostatMode = 64,
        ThermostatOperatingState = 66,
        ThermostatSetPoint = 67,
        ThermostatFanMode = 68,
        ThermostatFanState = 69,
        ClimateControlSchedule = 70,
        ThermostatSetBack = 71,
        Crc16Encapsulated = 86,
        CentralScene = 91,
        MultiInstance = 96,
        DoorLock = 98,
        UserCode = 99,
        Irrigation = 107,
        Configuration = 112,
        Alarm = 113,
        ManufacturerSpecific = 114,
        NodeNaming = 119,
        Battery = 128,
        Clock = 129,
        Hail = 130,
        WakeUp = 132,
        Association = 133,
        Version = 134,
        MultiCmd = 143,
        Security = 152,
        SensorAlarm = 156,
        SilenceAlarm = 157
    }
}
