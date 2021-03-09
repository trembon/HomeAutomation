using HomeAutomation.Models.Actions;
using HomeAutomation.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HomeAutomation.Entities.Action
{
    public class DelayAction : Action
    {
        public int[] Actions { get; set; }

        public TimeSpan Delay { get; set; }

        public override Task Execute(ActionExecutionArguments arguments)
        {
            if(Actions != null)
            {
                // TODO: add possibility to cancel delay, for example if motion is detected again before device is turned off, keep it running for x more minutes
                var scopeFactory = arguments.GetService<IServiceScopeFactory>();
                _ = Task.Delay(Delay).ContinueWith(task => ExecuteDelayedActions(scopeFactory, this));
            }

            return Task.CompletedTask;
        }

        private static async void ExecuteDelayedActions(IServiceScopeFactory scopeFactory, DelayAction delayAction)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var executionService = scope.ServiceProvider.GetService<IActionExecutionService>();
                foreach (var action in delayAction.Actions)
                    await executionService.Execute(action, delayAction);
            }
        }

        public override string ToSourceString()
        {
            return $"delay ({Delay})";
        }
    }
}
