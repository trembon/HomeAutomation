using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Core.Enums;

public enum ZWaveHealStatus : uint
{
    HealStart = 513,
    HealEnd = 514,
    HealError = 515
}
