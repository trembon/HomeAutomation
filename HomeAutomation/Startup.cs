using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HomeAutomation.Base.Extensions;
using HomeAutomation.Database;
using HomeAutomation.Hubs;
using HomeAutomation.Base.Logging;
using HomeAutomation.ScheduledJobs;
using HomeAutomation.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quartz;
using SmtpServer;
using SmtpServer.Storage;

namespace HomeAutomation
{
    // TODO: build memory logging that is readable on the web
    // TODO: build memory db to present telldus raw events on web
    // TODO: telldus retry count in config
    // TODO: add info logging to view on the website

    public class Startup
    {
        public IConfiguration Configuration { get; }

        public IWebHostEnvironment HostingEnvironment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            HostingEnvironment = env;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting(options => {
                options.AppendTrailingSlash = true;
                options.LowercaseUrls = true;
            });

            services.AddControllersWithViews();
            services.AddSignalR();

            services.AddLogging(options =>
            {
                options.AddConfiguration(Configuration.GetSection("Logging"));
                options.AddConsole();

                if (!HostingEnvironment.IsDevelopment())
                    options.AddProvider(new SlackLoggerProvider(Configuration["Slack:Token"]));

                options.Services.AddSingleton<ILoggerProvider, HubLoggerProvider>();
            });

            // register database contexts
            services.AddDbContext<DefaultContext>(options => options.UseSqlite(Configuration.GetConnectionString("Default")), ServiceLifetime.Transient);

            services.AddScoped<ISunDataService, SunDataService>();

            services.AddSingleton<ITriggerService, TriggerService>();
            services.AddSingleton<IZWaveAPIService, ZWaveAPIService>();
            services.AddSingleton<IMessageStore, EmailReceiveService>();
            services.AddSingleton<ITelldusAPIService, TelldusAPIService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IJsonDatabaseService, JsonDatabaseService>();
            services.AddSingleton<IActionExecutionService, ActionExecutionService>();

            // add quartz after all services
            services.AddQuartz();

            var options = new SmtpServerOptionsBuilder()
                .ServerName(Configuration["SMTP:ServerName"])
                .Port(Configuration.GetValue<int>("SMTP:Port"))
                .Build();

            services.AddSingleton(x => new SmtpServer.SmtpServer(options, x));
        }

        public async void Configure(IApplicationBuilder app, DefaultContext context, IJsonDatabaseService memoryEntitiesService, SmtpServer.SmtpServer smtpServer)
        {
            memoryEntitiesService.Initialize();
            context.Database.Migrate();

            if (HostingEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                await MockDatabase(context);
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapHub<LogHub>("/logHub");
            });

            if (Configuration.GetValue<bool>("Scheduler:Enabled"))
            {
                app.UseQuartz(q =>
                {
                    q.CreateScheduleJob<TriggerScheduledJob>(s => s.WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever()).StartAt(DateTimeOffset.Now.AddSeconds(20)));
                    q.CreateScheduleJob<ImportWeatherDataScheduledJob>(s => s.WithSimpleSchedule(x => x.WithIntervalInMinutes(60).RepeatForever()).StartAt(DateTimeOffset.Now.AddSeconds(20)));
                });
            }

            if (Configuration.GetValue<bool>("SMTP:Enabled"))
            {
                await smtpServer.StartAsync(CancellationToken.None);
            }
        }

        private async Task MockDatabase(DefaultContext context)
        {
            bool containsSunData = await context.SunData.AnyAsync();

            if (!containsSunData)
            {
                SunData sunData = new SunData();
                sunData.Date = DateTime.Today.AddDays(-1);
                sunData.Sunrise = sunData.Date.Add(new TimeSpan(7, 38, 0));
                sunData.Sunset = sunData.Date.Add(new TimeSpan(17, 57, 0));
                await context.AddAsync(sunData);
            }

            await context.SaveChangesAsync();
        }
    }
}
