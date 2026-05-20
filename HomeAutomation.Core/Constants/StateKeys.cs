using HomeAutomation.Database.Enums;

namespace HomeAutomation.Core.Constants;

public static class StateKeys
{
    public static string DeviceState(int deviceId) => $"device:{deviceId}:state";

    public static string SensorValue(int deviceId, SensorValueKind valueKind) => $"sensor:{deviceId}:{valueKind}";

    public static string CustomState(string name) => $"state:{name}";
}
