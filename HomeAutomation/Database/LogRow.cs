﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Database
{
    public class LogRow
    {
        [Key]
        [Required]
        public int ID { get; set; }

        [Required]
        public LogLevel Level { get; set; }

        public string Category { get; set; }

        public int EventID { get; set; }

        public DateTime Timestamp { get; set; }

        public string Message { get; set; }

        public string Exception { get; set; }

        public LogRow()
        {
            Timestamp = DateTime.UtcNow;
        }
    }
}
