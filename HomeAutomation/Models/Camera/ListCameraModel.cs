using HomeAutomation.Entities;
using HomeAutomation.Entities.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Models.Camera
{
    public class ListCameraModel
    {
        public List<CameraDevice> Cameras { get; set; }
    }
}
