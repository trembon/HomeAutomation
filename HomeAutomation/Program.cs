using HomeAutomation.Client.Pages;
using HomeAutomation.Components;
using HomeAutomation.Database.Extensions;
using HomeAutomation.ScheduledJobs;
using HomeAutomation.Core.Extensions;
using HomeAutomation.Core.Services;
using MudBlazor.Services;
using HomeAutomation.Base.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "APP_");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddControllers();
builder.Services.AddMudServices();
builder.Services.AddHttpClient();

builder.Services.AddDefaultDatabaseContext(builder.Configuration.GetConnectionString("Default")!);

builder.Logging.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DatabaseLoggerProvider>());
if (!builder.Environment.IsDevelopment())
    builder.Logging.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SlackLoggerProvider>());

builder.Services.AddSingleton<ITuyaAPIService, TuyaAPIService>();
builder.Services.AddSingleton<IZWaveAPIService, ZWaveAPIService>();
builder.Services.AddSingleton<ITelldusAPIService, TelldusAPIService>();

builder.Services.AddSingleton<IJsonDatabaseService, JsonDatabaseService>();
builder.Services.AddSingleton<IEvaluateConditionService, EvaluateConditionService>();

builder.Services.AddTransient<ISunDataService, SunDataService>();
builder.Services.AddTransient<ITriggerService, TriggerService>();
builder.Services.AddTransient<ISensorValueService, SensorValueService>();
builder.Services.AddTransient<INotificationService, NotificationService>();
builder.Services.AddTransient<IActionExecutionService, ActionExecutionService>();

builder.Services.AddScheduleJob<CleanupLogScheduleJob>();
builder.Services.AddScheduleJob<ImportSunDataScheduleJob>();
builder.Services.AddScheduleJob<ImportWeatherDataScheduledJob>();
builder.Services.AddScheduleJob<TriggerScheduledJob>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.ApplyDatabaseMigrations();
app.Services.GetRequiredService<IJsonDatabaseService>().Initialize();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(HomeAutomation.Client._Imports).Assembly);

app.MapControllers();

app.Run();