using HomeAutomation.Entities.Devices;
using HomeAutomation.Entities.Triggers;
using HomeAutomation.Models.Actions;
using HomeAutomation.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Entities.Action
{
    public class MessageAction : Action
    {
        public string Channel { get; set; }

        public string Message { get; set; }

        public override Task Execute(IActionExecutionArguments arguments)
        {
            var notificationService = arguments.GetService<INotificationService>();
            return notificationService.SendToSlack(Channel, GetProcessedMessage(arguments));
        }

        protected string GetProcessedMessage(IActionExecutionArguments arguments)
        {
            string deviceString = string.Join(", ", arguments.Devices.Select(d => d.Name));

            string message = Message.Replace("{devices}", deviceString);
            message = message.Replace("{source}", arguments.Source.ToSourceString());
            message = message.Replace("{now}", DateTime.Now.ToString());

            return message;
        }
    }
}
