# Architecture Reference

Quick-access map of every layer for contributors and AI coding agents. Read this to understand where things live before touching code.

---

## Solution Layout

```
HomeAutomation.sln
├── HomeAutomation/                    # Blazor host — UI + API surface
│   ├── Program.cs                     # DI wiring, middleware pipeline
│   ├── Components/
│   │   ├── Pages/                     # Feature pages (Devices, Integrations, Logs)
│   │   ├── Components/                # Shared MudBlazor dialogs
│   │   └── Layout/                    # MainLayout, NavMenu
│   └── Webhooks/                      # ASP.NET Core API controllers + request models
│       ├── TelldusController.cs       # POST webhooks/telldus/...
│       ├── ZWaveController.cs         # POST webhooks/zwave/...
│       ├── TuyaController.cs          # POST webhooks/tuya/...
│       ├── VerisureController.cs      # POST webhooks/verisure/...
│       └── FusionSolarController.cs   # POST webhooks/fusionsolar/...
│
├── HomeAutomation.Core/               # Business logic — no HTTP, no EF migrations
│   ├── Services/                      # Interface + implementation in same file
│   │   ├── ActionExecutionService.cs  # Dispatches actions
│   │   ├── TriggerService.cs          # Evaluates + fires triggers
│   │   ├── EvaluateConditionService.cs
│   │   ├── NotificationService.cs     # Slack (SlackNet)
│   │   ├── SunDataService.cs
│   │   ├── TelldusAPIService.cs       # Singleton — HTTP + SSE
│   │   ├── ZWaveAPIService.cs         # Singleton — HTTP + SSE
│   │   ├── TuyaAPIService.cs          # Singleton — HTTP
│   │   ├── VerisureAPIService.cs      # Singleton — HTTP
│   │   ├── FusionSolarService.cs      # Singleton — HTTP
│   │   └── EmailReceiveService.cs     # SMTP listener for IP cameras
│   ├── ScheduledJobs/
│   │   ├── Base/
│   │   │   ├── IScheduledJob.cs
│   │   │   ├── ScheduledJobAttribute.cs
│   │   │   └── ScheduledJobHandler.cs # IHostedService wrapper
│   │   ├── TriggerScheduledJob.cs
│   │   ├── ImportSunDataScheduleJob.cs
│   │   ├── ImportWeatherDataScheduledJob.cs
│   │   ├── CleanupLogScheduleJob.cs
│   │   ├── SummarizeSolarGenerationScheduleJob.cs
│   │   └── CalculateBatteryChargingScheduleJob.cs
│   ├── Logging/
│   │   ├── DatabaseLogger.cs / DatabaseLoggerProvider.cs
│   │   └── SlackLogger.cs / SlackLoggerProvider.cs
│   ├── Enums/                         # ZWave-specific enums
│   ├── Extensions/                    # Helper extensions (reflection, color, events)
│   ├── Models/                        # API response models (per-integration)
│   └── Startup.cs                     # AddRepositories(), AddSlackClient()
│
├── HomeAutomation.Database/           # EF Core — entities, repositories, migrations
│   ├── DefaultContext.cs              # DbContext + IDesignTimeDbContextFactory
│   ├── Entities/
│   │   ├── BaseEntity.cs              # abstract — int Id
│   │   ├── DeviceEntity.cs
│   │   ├── TriggerEntity.cs           # implements IConditionedEntity
│   │   ├── ActionEntity.cs            # implements IConditionedEntity
│   │   ├── ConditionEntity.cs
│   │   ├── ActionDeviceEntity.cs      # join: Action ↔ Device
│   │   ├── TriggerActionEntity.cs     # join: Trigger ↔ Action
│   │   ├── SensorValueEntity.cs
│   │   ├── SunDataEntity.cs
│   │   ├── EnergyPricingEntity.cs
│   │   ├── SolarGenerationSummaryEntity.cs
│   │   ├── WeatherForecastEntity.cs
│   │   ├── LogEntity.cs
│   │   └── MailMessageEntity.cs
│   ├── Repositories/
│   │   ├── Repository.cs              # IRepository / IRepository<T> / Repository<T>
│   │   ├── DeviceRepository.cs        # IDeviceRepository / DeviceRepository
│   │   └── TriggerRepository.cs       # ITriggerRepository / TriggerRepository
│   ├── Interfaces/
│   │   └── IConditionedEntity.cs
│   ├── Enums/                         # All domain enums
│   ├── Converters/                    # DateTimeUTCConverter, NullableDateTimeUTCConverter
│   ├── Migrations/                    # EF Core migrations
│   └── Startup.cs                     # AddDefaultDatabaseContext(), ApplyDatabaseMigrations()
│
└── HomeAutomation.Client/             # Blazor WebAssembly — client-only components
```

---

## Automation Execution Flow

### Device-event triggered flow
```
Integration bridge  →  POST webhooks/{integration}
  ZWaveController / TelldusController / etc.
    IDeviceRepository.Get(source, sourceId)
    ITriggerService.FireTriggersFromDevice(device, event)
      ITriggerRepository — load triggers matching device + event
      IEvaluateConditionService.MeetConditions(trigger)       ← guards time windows
      IActionExecutionService.Execute(actionId, source)
        IEvaluateConditionService.MeetConditions(action)
        ActionKind.DeviceEvent  → Integration API service
        ActionKind.SendMessage  → INotificationService.SendToSlack
        ActionKind.SendSnapshot → INotificationService.SendToSlack (with image)
```

### Scheduled trigger flow
```
ScheduledJobHandler<TriggerScheduledJob>  (every 600 s)
  TriggerScheduledJob.Execute(currentExecution, lastExecution)
    ITriggerRepository.GetScheduledTriggers()
    For each trigger: CalculateTriggerTime(at, mode, sunData)
    ITriggerService.FireTriggers(matchedTriggers)
      → same execution path as device-event flow from IActionExecutionService
```

---

## Key Relationships (EF Core)

```
TriggerEntity  ──< TriggerActionEntity >──  ActionEntity
TriggerEntity  ──< ConditionEntity
ActionEntity   ──< ConditionEntity
ActionEntity   ──< ActionDeviceEntity >──  DeviceEntity
DeviceEntity   ──< SensorValueEntity
```

- `TriggerEntity.ListenOnDeviceId` → FK to `DeviceEntity` (for DeviceState triggers)
- All many-to-many relations use explicit join entities

---

## Adding a New Integration

1. Add enum value to `DeviceSource` in `HomeAutomation.Database/Enums/DeviceSource.cs`.
2. Create `I{Name}APIService` + `{Name}APIService` in `HomeAutomation.Core/Services/` (same file). Register as **singleton** in `Program.cs`.
3. Create `{Name}Controller` in `HomeAutomation/Webhooks/` routing `webhooks/{name}`. Inject `IDeviceRepository`, `ITriggerService`, and the new API service.
4. Add webhook request models under `HomeAutomation/Webhooks/Models/{Name}/`.
5. Handle `DeviceSource.{Name}` inside `ActionExecutionService.ExecuteDeviceEventAction`.
6. (Optional) Add a Blazor page under `HomeAutomation/Components/Pages/Integrations/`.

## Adding a New Scheduled Job

1. Create a class in `HomeAutomation.Core/ScheduledJobs/` implementing `IScheduledJob`.
2. Decorate with `[ScheduledJob(intervalInSeconds)]` — `DelayInSeconds` defaults to 30.
3. Use primary constructor for DI dependencies.
4. Done — auto-discovered and registered by `ScheduledJobExtensions.AddScheduleJobs()`.

## Adding a New Repository

1. Create `I{Name}Repository` extending `IRepository<TEntity>` and `{Name}Repository` extending `Repository<TEntity>` in the **same file** under `HomeAutomation.Database/Repositories/`.
2. Done — auto-discovered and registered by `Startup.AddRepositories()` via reflection.

---

## DI Registration Summary

| Category | Lifetime | Registration |
|---|---|---|
| Integration API services | Singleton | Manual in `Program.cs` |
| Business services | Transient | Manual in `Program.cs` |
| Repositories | Transient | Auto via `Startup.AddRepositories()` |
| Scheduled jobs | Transient + `IHostedService` | Auto via `AddScheduleJobs()` |
| Loggers | Singleton | Manual in `Program.cs` |

---

## Conventions Cheat-Sheet

| Rule | Detail |
|---|---|
| Primary constructors | Used everywhere — no `this.x = x` assignments |
| Interface + impl | Always in the **same `.cs` file** |
| CancellationToken | Every `async` method must accept one |
| SourceId | Always `string` — parse to numeric type at call site |
| Read-only EF queries | Always use `AsNoTracking()` |
| DateTime storage | UTC; converted by `DateTimeUTCConverter` globally in `OnModelCreating` |
| Logging pattern | `"Domain.Operation :: {entity} :: Status:{status}"` |
| EF config | Per-entity `IEntityTypeConfiguration<T>` class in the **same file** as the entity |
| Nullable | Enabled project-wide — never suppress without reason |
