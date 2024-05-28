namespace HomeAutomation.Core.Models
{
    public class TuyaDeviceModel
    {
        public string ID { get; set; }

        public string IP { get; set; }

        public string ProductKey { get; set; }

        public bool IsConnected { get; set; }
    }
}
