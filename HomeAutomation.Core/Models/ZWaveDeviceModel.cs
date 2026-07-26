using HomeAutomation.Core.Enums;

namespace HomeAutomation.Core.Models;

public class ZWaveDeviceModel
{
    public byte ID { get; set; }

    public NodeCapabilities ProtocolInfo { get; set; } = new();

    public ManufacturerSpecificInfo ManufacturerSpecific { get; set; } = new();

    public List<NodeCommandClass> SupportedCommands { get; set; } = [];

    public class NodeCapabilities
    {
        public byte BasicType { get; set; }
        public byte GenericType { get; set; }
        public byte SpecificType { get; set; }

        public override string ToString()
        {
            return $"Basic: {BasicType}, Generic: {GenericType}, Specific: {SpecificType}";
        }
    }

    public class ManufacturerSpecificInfo
    {
        public string ManufacturerId { get; set; } = string.Empty;
        public string TypeId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"Manufacturer: {ManufacturerId}, Type: {TypeId}, Product: {ProductId}";
        }
    }

    public class NodeCommandClass
    {
        public byte Id { get; set; }
        public int Version { get; set; }
        public ZWaveCommandClass CommandClass { get; }
    }
}
