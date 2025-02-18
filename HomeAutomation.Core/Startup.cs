using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SlackNet.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeAutomation.Core;

public static class Startup
{
    public static void AddSlackClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSlackNet(c => c.UseApiToken(configuration["Slack:Token"]));
    }
}
