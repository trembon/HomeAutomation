using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Database
{
    public class SunData
    {
        [Key]
        [Required]
        public int ID { get; set; }
        
        public DateTime Date { get; set; }
        
        public DateTime Sunset { get; set; }

        public DateTime Sunrise { get; set; }
    }
}
