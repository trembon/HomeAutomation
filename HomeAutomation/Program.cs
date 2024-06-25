using HomeAutomation.Client.Pages;
using HomeAutomation.Components;
using HomeAutomation.Database.Extensions;
using HomeAutomation.ScheduledJobs;
using HomeAutomation.Core.Extensions;
using HomeAutomation.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddDefaultDatabaseContext(builder.Configuration.GetConnectionString("Default")!);
builder.Services.AddLoggingDatabaseContext(builder.Configuration.GetConnectionString("Logging")!);

builder.Services.AddSingleton<IJsonDatabaseService, JsonDatabaseService>();

builder.Services.AddTransient<ISunDataService, SunDataService>();
builder.Services.AddTransient<ITriggerService, TriggerService>();
builder.Services.AddTransient<INotificationService, NotificationService>();

builder.Services.AddScheduleJob<CleanupLogScheduleJob>();
builder.Services.AddScheduleJob<ImportSunDataScheduleJob>();
builder.Services.AddScheduleJob<ImportWeatherDataScheduledJob>();
builder.Services.AddScheduleJob<PhoneCallLogScheduleJob>();
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

app.Run();
