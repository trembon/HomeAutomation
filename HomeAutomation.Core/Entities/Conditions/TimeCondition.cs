using HomeAutomation.Core.Entities;
using HomeAutomation.Core.Services;
using HomeAutomation.Entities.Enums;

namespace HomeAutomation.Entities.Conditions
{
    public class TimeCondition : Condition
    {
        public ScheduleMode Mode { get; set; }

        public TimeSpan? Time { get; set; }

        public CompareType Compare { get; set; }

        public override Task<bool> Check(ConditionExecutionArguments arguments)
        {
            DateTime compareDateTime = DateTime.Today;

            if(Mode == ScheduleMode.Time)
            {
                compareDateTime = compareDateTime.Add(Time ?? new TimeSpan(0));
            }
            else
            {
                var service = arguments.GetService<ISunDataService>();
                var sunData = service.GetLatest();

                if(Mode == ScheduleMode.Sunrise)
                {
                    compareDateTime = compareDateTime.Add(sunData.Sunrise.TimeOfDay);
                }
                else
                {
                    compareDateTime = compareDateTime.Add(sunData.Sunset.TimeOfDay);
                }

                if (Time.HasValue)
                    compareDateTime = compareDateTime.Add(Time.Value);
            }

            bool result = false;
            if(Compare == CompareType.GreaterThan)
            {
                result = DateTime.Now > compareDateTime;
            }
            else if (Compare == CompareType.LesserThan)
            {
                result = compareDateTime > DateTime.Now;
            }

            return Task.FromResult(result);
        }
    }
}
