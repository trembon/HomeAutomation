﻿using HomeAutomation.Database.Enums;

namespace HomeAutomation.Webhooks.Models.Telldus;

public class SensorUpdatesModel
{
    public int SensorID { get; set; }

    public SensorValueType Type { get; set; }

    public DateTime Timestamp { get; set; }

    public string Value { get; set; }
}
