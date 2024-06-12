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

builder.Services.AddTransient<ISunDataService, SunDataService>();

builder.Services.AddScheduleJob<ImportSunDataScheduleJob>();

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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(HomeAutomation.Client._Imports).Assembly);

app.Run();
