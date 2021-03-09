using HomeAutomation.Database.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Database
{
    public class SensorValue
    {
        [Key]
        [Required]
        public int ID { get; set; }
        
        [Required]
        public int TellstickID { get; set; }

        [Required]
        public SensorValueType Type { get; set; }

        public string Value { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
