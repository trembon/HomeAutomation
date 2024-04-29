using System;
using System.ComponentModel.DataAnnotations;

namespace HomeAutomation.Database
{
    public class MailMessage
    {
        [Key]
        [Required]
        public int ID { get; set; }

        public string MessageID { get; set; }

        public string DeviceSource { get; set; }

        public string DeviceSourceID { get; set; }

        public byte[] EmlData { get; set; }

        public DateTime Timestamp { get; set; }

        public MailMessage()
        {
            Timestamp = DateTime.UtcNow;
        }
    }
}
