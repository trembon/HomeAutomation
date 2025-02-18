namespace HomeAutomation.Base.Enums;

[Flags]
public enum TelldusDeviceMethods
{
    TurnOn = 1,
    TurnOff = 2,
    Bell = 4,
    Toggle = 8,
    Dim = 16,
    Learn = 32,
    Execute = 64,
    Up = 128,
    Down = 256,
    Stop = 512
}
