# GitHub Copilot Instructions — HomeAutomation

## Project Overview
A private home automation system built with **C# 14 / .NET 10**, **Blazor** (Interactive Server + WebAssembly hybrid), and **PostgreSQL** via **Entity Framework Core**. The core automation model is: **Trigger → [Conditions] → Actions → Devices**.

## Solution Structure
| Project | Role |
|---|---|
| `HomeAutomation` | Blazor host app — UI pages, webhook controllers, `Program.cs` |
| `HomeAutomation.Core` | Business logic — services, scheduled jobs, logging providers |
| `HomeAutomation.Database` | Data layer — EF Core entities, repositories, migrations, enums |
| `HomeAutomation.Client` | Blazor WebAssembly client assembly |

## Automation Domain Model
```
Trigger  ──(fires)──►  [Conditions checked]  ──(met)──►  Action  ──(targets)──►  Device(s)
```
- **`TriggerEntity`** — `TriggerKind.DeviceState` (reacts to a device event) or `TriggerKind.Scheduled` (time-based, evaluated by `TriggerScheduledJob`).
- **`ConditionEntity`** — Currently only `ConditionKind.Time`; compares current time against `Specified`, `Sunrise`, or `Sunset` (`TimeMode` enum) using `CompareKind`.
- **`ActionEntity`** — `ActionKind.DeviceEvent` (send command to devices), `ActionKind.SendMessage` (Slack text), `ActionKind.SendSnapshot` (camera snapshot to Slack).
- **`DeviceEntity`** — A physical device; identified by `DeviceSource` (integration) + `SourceId` (string, even for numeric IDs).

### Key Enums
- `DeviceSource`: `ONVIF=0`, `Telldus=1`, `ZWave=2`, `Tuya=3`, `Verisure=4`, `FusionSolar=5`
- `DeviceKind`: `Camera`, `Lightbulb`, `MotionSensor`, `PowerSwitch`, `Remote`, `Sensor`, `Alarm`, `MagneticSensor`
- `DeviceEvent`: `Unknown=0`, `On`, `Off`, `Motion`, `Partial`
- `ActionKind`: `DeviceEvent=0`, `SendMessage=1`, `SendSnapshot=2`
- `TriggerKind`: `DeviceState=0`, `Scheduled=1`
- `ConditionKind`: `Time=0`
- `TimeMode`: `Specified=0`, `Sunrise=1`, `Sunset=2`

## Device Integrations
Each integration has a singleton API service and a corresponding webhook controller (or scheduled import).

| Integration | Service Interface | Webhook Route | Notes |
|---|---|---|---|
| Telldus (433 MHz) | `ITelldusAPIService` | `webhooks/telldus` | Duplicate-request guard (`Lock`) |
| Z-Wave (868 MHz) | `IZWaveAPIService` | `webhooks/zwave` | Fires SSE events via `ZWaveEventReceived` |
| Tuya (local/offline) | `ITuyaAPIService` | `webhooks/tuya` | DPS-based commands |
| Verisure (alarm) | `IVerisureAPIService` | `webhooks/verisure` | Maps state strings to `DeviceEvent` |
| FusionSolar (solar) | `IFusionSolarService` | `webhooks/fusionsolar` | Battery charging logic in scheduled job |
| ONVIF (cameras) | — | — | Devices only, no dedicated API service |

## Repository Pattern
- `IRepository<TEntity>` — base interface with `Get(id)`, `AddAndSave(entity)`, `Save()`, and `Table` (DbSet).
- `Repository<TEntity>` — generic EF Core implementation.
- Specialized repositories (e.g., `IDeviceRepository`, `ITriggerRepository`) extend the base and are defined alongside their implementations in the same file.
- **Auto-registration**: `Startup.AddRepositories()` scans assemblies via reflection — no manual registration needed for new repositories.
- `IConditionedEntity` — interface implemented by `ActionEntity` and `TriggerEntity`; signals that the entity has a `Conditions` collection.

## Scheduled Jobs
- Implement `IScheduledJob` → `Task Execute(DateTime currentExecution, DateTime? lastExecution, CancellationToken)`.
- Decorated with `[ScheduledJob(intervalInSeconds, DelayInSeconds = 30)]`.
- **Auto-registered** by `ScheduledJobExtensions.AddScheduleJobs()` — just add the attribute.
- Run as `IHostedService` via `ScheduledJobHandler<T>` (creates a new DI scope per execution).

| Job | Interval | Purpose |
|---|---|---|
| `TriggerScheduledJob` | 600 s | Fires scheduled triggers (sunrise/sunset/time) |
| `ImportSunDataScheduleJob` | 7200 s | Fetches sunrise/sunset from sunrise-sunset.org |
| `ImportWeatherDataScheduledJob` | — | Fetches weather forecast |
| `CleanupLogScheduleJob` | — | Purges old log entries |
| `SummarizeSolarGenerationScheduleJob` | — | Summarises daily solar generation |
| `CalculateBatteryChargingScheduleJob` | 3600 s | Plans battery charge windows from energy pricing |

## Service Layer
### DI Lifetimes
- **Singleton**: `ITelldusAPIService`, `IZWaveAPIService`, `ITuyaAPIService`, `IVerisureAPIService`, `IFusionSolarService` — hold long-lived HTTP clients and event streams.
- **Transient**: repositories, `ITriggerService`, `IActionExecutionService`, `INotificationService`, `IEvaluateConditionService`, `ISunDataService`.

### Core Service Responsibilities
- `IActionExecutionService` — resolves action type and dispatches to send-message, send-snapshot, or device-event logic; resolves singleton API services from `IServiceProvider` at call time.
- `ITriggerService` — evaluates conditions and calls `IActionExecutionService` for each trigger's actions.
- `IEvaluateConditionService` — checks all conditions on an `IConditionedEntity`; uses `ISunDataService` for sunrise/sunset comparisons.
- `INotificationService` — wraps `SlackNet` for text and file uploads to Slack channels.

## Logging
- `DatabaseLoggerProvider` / `DatabaseLogger` — writes `LogEntity` rows to PostgreSQL (all environments).
- `SlackLoggerProvider` — posts errors to Slack (non-development only).
- Log category pattern: `"Domain.Operation :: {entity} :: Status:{status}"` (e.g., `"Action.Execute :: MyAction :: Status:Disabled"`).

## Configuration (`appsettings.json`)
```
ConnectionStrings:Default       PostgreSQL connection string
Slack:Token                     Slack bot token (SlackNet)
Telldus:APIUrl                  TelldusCoreWrapper WebAPI base URL
Telldus:IgnoreDuplicateWebhooksInSeconds
ZWave:APIUrl                    ZWaveLib WebAPI base URL
Tuya:APIUrl                     tuya-local-api base URL
Verisure:APIUrl                 Verisure bridge API base URL
FusionSolar:APIUrl              FusionSolar bridge API base URL
EnergyPrices:APIUrl / Referrer  Spot-price API
Forecasts:SunDataUrl            sunrise-sunset.org template URL (lat/lng placeholders)
Forecasts:WeatherUrl            yr.no weather XML URL
Forecasts:Lat / Lng             Home coordinates
SMTP:Enabled / ServerName / Port  Built-in SMTP server for IP camera emails
```
All keys are overridable via environment variables prefixed `APP_`.

## UI (Blazor)
- **MudBlazor** component library for all UI components.
- Render modes: Interactive Server + Interactive WebAssembly (`HomeAutomation.Client` assembly).
- Pages live in `HomeAutomation/Components/Pages/` grouped by feature (Devices, Integrations, Logs).
- Shared dialogs in `HomeAutomation/Components/Components/`.

## Coding Conventions
- **Primary constructors** everywhere (no `this.field = parameter` assignments).
- Interface and implementation class defined in the **same file**.
- `async` methods always accept `CancellationToken cancellationToken`.
- `DeviceEntity.SourceId` is always a `string`, even for numeric IDs — parse at call site.
- `ActionDeviceEntity` is the join table between `ActionEntity` and `DeviceEntity`.
- `TriggerActionEntity` is the join table between `TriggerEntity` and `ActionEntity`.
- EF Core `AsNoTracking()` on read-only queries.
- All `DateTime` columns stored as UTC via `DateTimeUTCConverter` / `NullableDateTimeUTCConverter`.
- `BaseEntity` — all entities inherit this; provides `int Id`.
- No comments unless explaining non-obvious logic.
- Nullable reference types enabled (`<Nullable>enable</Nullable>`).
