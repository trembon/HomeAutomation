using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Entities.Devices;

public class CameraDevice : Device
{
    public string URL { get; set; }

    public string ThumbnailURL { get; set; }
}
