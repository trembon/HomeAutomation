using HomeAutomation.Base.Extensions;
using HomeAutomation.Base.Logging;
using HomeAutomation.Database;
using HomeAutomation.Hubs;
using HomeAutomation.ScheduledJobs;
using HomeAutomation.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using SmtpServer;
using SmtpServer.Storage;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HomeAutomation
{
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
                options.AddProvider(new DatabaseLoggerProvider().Configure(logger => logger.UseSqlite(Configuration.GetConnectionString("Logging"))));
            });

            // register database contexts
            services.AddDbContext<DefaultContext>(options => options.UseSqlite(Configuration.GetConnectionString("Default")), ServiceLifetime.Transient);
            services.AddDbContext<LogContext>(options => options.UseSqlite(Configuration.GetConnectionString("Logging")), ServiceLifetime.Transient);

            services.AddScoped<ISunDataService, SunDataService>();
            services.AddScoped<ITriggerService, TriggerService>();
            services.AddScoped<IActionExecutionService, ActionExecutionService>();
            services.AddScoped<IEvaluateConditionService, EvaluateConditionService>();

            services.AddSingleton<IZWaveAPIService, ZWaveAPIService>();
            services.AddSingleton<IMessageStore, EmailReceiveService>();
            services.AddSingleton<ITelldusAPIService, TelldusAPIService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IJsonDatabaseService, JsonDatabaseService>();

            // add quartz after all services
            services.AddQuartz();

            var options = new SmtpServerOptionsBuilder()
                .ServerName(Configuration["SMTP:ServerName"])
                .Port(Configuration.GetValue<int>("SMTP:Port"))
                .Build();

            services.AddSingleton(x => new SmtpServer.SmtpServer(options, x));
        }

        public async void Configure(IApplicationBuilder app)
        {
            app.ApplicationServices.GetService<IJsonDatabaseService>().Initialize();

            app.ApplicationServices.GetService<DefaultContext>().Database.Migrate();
            app.ApplicationServices.GetService<LogContext>().Database.Migrate();

            if (HostingEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                await MockDatabase(app.ApplicationServices.GetService<DefaultContext>());
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
                    q.CreateScheduleJob<ImportSunDataScheduleJob>(s => s.WithSimpleSchedule(x => x.WithIntervalInHours(2).RepeatForever()).StartAt(DateTimeOffset.Now.AddSeconds(30)));
                    q.CreateScheduleJob<CleanupLogScheduleJob>(s => s.WithCronSchedule("0 0 3 1/1 * ? *").StartNow());
                });
            }

            if (Configuration.GetValue<bool>("SMTP:Enabled"))
            {
                await app.ApplicationServices.GetService<SmtpServer.SmtpServer>().StartAsync(CancellationToken.None);
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
