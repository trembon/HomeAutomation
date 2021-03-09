using HomeAutomation.Base.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Models.ZWave
{
    public class NodeModel
    {
        public byte ID { get; set; }

        public NodeCapabilities ProtocolInfo { get; set; }

        public ManufacturerSpecificInfo ManufacturerSpecific { get; set; }

        public List<NodeCommandClass> SupportedCommands { get; set; }

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
            public string ManufacturerId { get; set; }
            public string TypeId { get; set; }
            public string ProductId { get; set; }

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
}
