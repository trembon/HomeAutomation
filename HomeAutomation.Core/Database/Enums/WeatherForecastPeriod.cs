using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Database.Enums
{
    public enum WeatherForecastPeriod
    {
        /// <summary>
        /// 00:00-06:00
        /// </summary>
        Night = 0,
        /// <summary>
        /// 06:00-12:00
        /// </summary>
        Morning = 1,
        /// <summary>
        /// 12:00-18:00
        /// </summary>
        Day = 2,
        /// <summary>
        /// 18:00-00:00
        /// </summary>
        Evening = 3
    }
}
