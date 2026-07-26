using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Core.Enums;

public enum ZWaveDiscoveryStatus : uint
{
    DiscoveryStart = 513,
    DiscoveryEnd = 514,
    DiscoveryError = 515
}
