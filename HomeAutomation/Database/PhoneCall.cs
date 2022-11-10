using HomeAutomation.Database.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace HomeAutomation.Database
{
    public class PhoneCall
    {
        [Key]
        [Required]
        public int ID { get; set; }

        public string Number { get; set; }

        public PhoneCallType Type { get; set; }

        public TimeSpan Length { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
