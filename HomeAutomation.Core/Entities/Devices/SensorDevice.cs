using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;

namespace HomeAutomation.Entities.Devices;

public class SensorDevice : Device
{
    public Dictionary<SensorValueType, SensorValue> LatestValues { get; }

    public SensorDevice()
    {
        this.LatestValues = new Dictionary<SensorValueType, SensorValue>();
    }
}
