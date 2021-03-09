using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Base.Enums
{
    public enum ZWaveNodeQueryStatus : uint
    {
        NodeAddReady = 18944,
        NodeAddStarted = 18945,
        NodeAddDone = 18946,
        NodeAddFailed = 18948,
        NodeRemoveReady = 19200,
        NodeRemoveStarted = 19201,
        NodeRemoveDone = 19202,
        NodeRemoveFailed = 19204,
        NeighborUpdateStarted = 23041,
        NeighborUpdateDone = 23042,
        NeighborUpdateFailed = 23043,
        NodeAdded = 65281,
        NodeRemoved = 65282,
        NodeUpdated = 65283,
        Error = 65518,
        Timeout = 65535
    }
}
