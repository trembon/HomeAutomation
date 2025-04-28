using HomeAutomation.Database.Entities;
using HomeAutomation.Database.Enums;

namespace HomeAutomation.Entities.Devices;

public class SensorDevice : Device
{
    public Dictionary<SensorValueKind, SensorValueEntity> LatestValues { get; }

    public SensorDevice()
    {
        this.LatestValues = new Dictionary<SensorValueKind, SensorValueEntity>();
    }
}
