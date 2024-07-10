using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Base.Enums
{
    public enum ZWaveControllerStatus : byte
    {
        Connected = 0,
        Disconnected = 1,
        Initializing = 2,
        Ready = 3,
        Error = 4
    }
}
