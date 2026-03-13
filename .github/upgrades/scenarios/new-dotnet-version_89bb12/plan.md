# .NET 10 Upgrade Plan — HomeAutomation Solution

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Migration Strategy](#2-migration-strategy)
3. [Detailed Dependency Analysis](#3-detailed-dependency-analysis)
4. [Detailed Execution Steps](#4-detailed-execution-steps)
5. [Project-by-Project Migration Plans](#5-project-by-project-migration-plans)
   - [HomeAutomation.Database](#51-homeautomationdatabase)
   - [HomeAutomation.Client](#52-homeautomationclient)
   - [HomeAutomation.Core](#53-homeautomationcore)
   - [HomeAutomation](#54-homeautomation)
6. [Package Update Reference](#6-package-update-reference)
7. [Breaking Changes Catalog](#7-breaking-changes-catalog)
8. [Testing & Validation Strategy](#8-testing--validation-strategy)
9. [Risk Management](#9-risk-management)
10. [Complexity & Effort Assessment](#10-complexity--effort-assessment)
11. [Source Control Strategy](#11-source-control-strategy)
12. [Success Criteria](#12-success-criteria)

---

## 1. Executive Summary

### Scenario

Upgrade all projects in the **HomeAutomation** solution from `.NET 9.0` to `.NET 10.0 (LTS)`.

### Selected Strategy

**All-At-Once Strategy** — All 4 projects upgraded simultaneously in a single coordinated operation.

**Rationale:**
- Small solution (4 projects) — well within All-At-Once threshold
- Homogeneous codebase — all projects currently target `net9.0`
- Simple, clear dependency structure — maximum depth of 3 levels, no circular dependencies
- Low total codebase size — 8,050 lines of code across 110 files
- All NuGet packages have known target versions available
- No test projects to complicate phasing

### Scope

| Project | Type | Current | Target | Files | LOC |
|---|---|---|---|---|---|
| `HomeAutomation.Database` | ClassLibrary | net9.0 | net10.0 | 45 | 4,918 |
| `HomeAutomation.Client` | Blazor WebAssembly | net9.0 | net10.0 | 4 | 5 |
| `HomeAutomation.Core` | ClassLibrary | net9.0 | net10.0 | 45 | 2,590 |
| `HomeAutomation` | ASP.NET Core (Blazor Server) | net9.0 | net10.0 | 42 | 537 |

**Total:** 4 projects · 136 files · 8,050 LOC · 14 NuGet packages

### Key Findings

| Category | Count | Impact |
|---|---|---|
| 🔴 Mandatory issues | 7 | Target framework changes (4) + binary-breaking API changes (3) |
| 🟡 Potential issues | 33 | Source incompatible APIs (8) + behavioral changes (16) + package upgrades (9) |
| 🔵 Security vulnerability | 1 | `MimeKit` 4.13.0 → 4.15.1 — address during upgrade |
| ✅ Compatible packages | 5 | No changes required |

### ⚠️ Security Vulnerability

The following package contains a known security vulnerability and **must** be upgraded:

| Package | Current Version | Fixed Version | Project |
|---|---|---|---|
| `MimeKit` | 4.13.0 | 4.15.1 | `HomeAutomation.Core` |

This will be addressed as part of the atomic upgrade operation.

---

## 2. Migration Strategy

### Approach: All-At-Once

All 4 projects are upgraded in a **single coordinated atomic operation**. There are no intermediate states, no multi-targeting, and no project-by-project incremental phases.

### Justification

| Factor | Value | Implication |
|---|---|---|
| Project count | 4 | Well below the 30-project threshold for incremental |
| Current framework | All `net9.0` | Homogeneous — no mixed-framework complexity |
| Total LOC | 8,050 | Small codebase, low risk surface |
| Dependency depth | 3 levels | Simple, clear chain |
| Circular dependencies | None | No blocking dependency issues |
| Package availability | All packages have known `net10.0` versions | No compatibility blockers |
| Test projects | None | No test phasing concerns |

### Execution Phases

| Phase | Description |
|---|---|
| **Phase 0 — Prerequisites** | Validate .NET 10 SDK installation |
| **Phase 1 — Atomic Upgrade** | Update all `TargetFramework` properties, all package references, restore, build and fix all compilation errors |
| **Phase 2 — Validation** | Build the complete solution with 0 errors, run manual smoke test |

### Parallel vs Sequential

Within the atomic upgrade, project file updates and package reference updates can be performed in any order. However, when resolving **compilation errors**, follow the dependency order:
1. `HomeAutomation.Database` — fix first (no project dependencies)
2. `HomeAutomation.Client` — fix in parallel with Database (no project dependencies)
3. `HomeAutomation.Core` — fix after Database is clean
4. `HomeAutomation` — fix last (depends on all others)

---

## 3. Detailed Dependency Analysis

### Dependency Graph

```
HomeAutomation  (root — no dependants)
├── HomeAutomation.Client      (leaf — no project dependencies)
├── HomeAutomation.Core
│   └── HomeAutomation.Database  (leaf — no project dependencies)
└── HomeAutomation.Database    (leaf — no project dependencies)
```

### Project Levels

| Level | Projects | Description |
|---|---|---|
| Level 0 (Leaves) | `HomeAutomation.Database`, `HomeAutomation.Client` | No project dependencies; safe to update first |
| Level 1 | `HomeAutomation.Core` | Depends on `HomeAutomation.Database` only |
| Level 2 (Root) | `HomeAutomation` | Depends on all three; updated last |

### Critical Path

`HomeAutomation.Database` → `HomeAutomation.Core` → `HomeAutomation`

`HomeAutomation.Client` is independent of the core chain and can be updated in parallel with any level.

### Circular Dependencies

None detected. The dependency graph is a clean directed acyclic graph (DAG).

### Migration Order Rationale

Following the **bottom-up** rule: leaf nodes must be updated before projects that depend on them. In the All-At-Once approach all projects are updated in a single operation, but the dependency order is critical when fixing compilation errors — errors in `HomeAutomation.Database` or `HomeAutomation.Client` must be resolved before addressing errors in `HomeAutomation.Core`, which must be resolved before `HomeAutomation`.

---

## 4. Detailed Execution Steps

### Step 0 — Validate Prerequisites

Verify that the .NET 10 SDK is installed on the machine:
```sh
dotnet --list-sdks
```
Expected: an SDK with version `10.x.x` is listed. If not, install from https://dot.net/download before proceeding.

### Step 1 — Update All Project TargetFramework Properties

Change `<TargetFramework>net9.0</TargetFramework>` to `<TargetFramework>net10.0</TargetFramework>` in all four project files simultaneously:

- `HomeAutomation.Database\HomeAutomation.Database.csproj`
- `HomeAutomation.Client\HomeAutomation.Client.csproj`
- `HomeAutomation.Core\HomeAutomation.Core.csproj`
- `HomeAutomation\HomeAutomation.csproj`

See [§5 Project-by-Project Migration Plans](#5-project-by-project-migration-plans) for per-project file details.

### Step 2 — Update All NuGet Package References

Update all 9 packages requiring version changes across all projects. See [§6 Package Update Reference](#6-package-update-reference) for the complete matrix.

Key updates:
- Microsoft platform packages `9.0.8` → `10.0.5` (8 packages, 3 projects)
- `MimeKit` `4.13.0` → `4.15.1` (1 project — **security fix**)

### Step 3 — Restore Dependencies

```sh
dotnet restore HomeAutomation.sln
```

Verify no unresolved packages remain before building.

### Step 4 — Build Solution and Fix All Compilation Errors

```sh
dotnet build HomeAutomation.sln
```

Address all compilation errors produced by the framework and package upgrades. Follow the dependency order when fixing errors (Database/Client → Core → HomeAutomation). See [§7 Breaking Changes Catalog](#7-breaking-changes-catalog) for the full list of anticipated code changes.

Rebuild to verify all fixes are complete:
```sh
dotnet build HomeAutomation.sln
```

**Expected outcome:** Solution builds with **0 errors**.

### Step 5 — Validate

- Confirm build output reports 0 errors and review any warnings
- Start the application and verify basic functionality loads correctly

---

## 5. Project-by-Project Migration Plans

### 5.1 HomeAutomation.Database

#### Current State
- **Framework:** `net9.0`
- **Project Kind:** ClassLibrary
- **Dependencies:** None (leaf node)
- **Dependants:** `HomeAutomation`, `HomeAutomation.Core`
- **Files:** 45 | **LOC:** 4,918
- **Risk Level:** 🟢 Low

#### Target State
- **Framework:** `net10.0`
- **Packages updated:** 1 (`Microsoft.Extensions.Hosting.Abstractions`)
- **API issues:** 0

#### Migration Steps

1. **Update TargetFramework** in `HomeAutomation.Database\HomeAutomation.Database.csproj`:
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

2. **Update Package References** in `HomeAutomation.Database\HomeAutomation.Database.csproj`:
   | Package | From | To |
   |---|---|---|
   | `Microsoft.Extensions.Hosting.Abstractions` | 9.0.8 | 10.0.5 |

3. **Expected Breaking Changes:** None — 0 API issues detected.

4. **Compatible Packages (no change):**
   - `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.4 ✅

#### Validation Checklist
- [ ] `HomeAutomation.Database` builds without errors
- [ ] `HomeAutomation.Database` builds without warnings
- [ ] No unresolved package references

---

### 5.2 HomeAutomation.Client

#### Current State
- **Framework:** `net9.0`
- **Project Kind:** Blazor WebAssembly (AspNetCore)
- **Dependencies:** None (leaf node)
- **Dependants:** `HomeAutomation`
- **Files:** 4 | **LOC:** 5
- **Risk Level:** 🟢 Low

#### Target State
- **Framework:** `net10.0`
- **Packages updated:** 1 (`Microsoft.AspNetCore.Components.WebAssembly`)
- **API issues:** 0

#### Migration Steps

1. **Update TargetFramework** in `HomeAutomation.Client\HomeAutomation.Client.csproj`:
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

2. **Update Package References** in `HomeAutomation.Client\HomeAutomation.Client.csproj`:
   | Package | From | To |
   |---|---|---|
   | `Microsoft.AspNetCore.Components.WebAssembly` | 9.0.8 | 10.0.5 |

3. **Expected Breaking Changes:** None — 0 API issues detected.

#### Validation Checklist
- [ ] `HomeAutomation.Client` builds without errors
- [ ] `HomeAutomation.Client` builds without warnings
- [ ] Blazor WASM project outputs correctly

---

### 5.3 HomeAutomation.Core

#### Current State
- **Framework:** `net9.0`
- **Project Kind:** ClassLibrary
- **Dependencies:** `HomeAutomation.Database`
- **Dependants:** `HomeAutomation`
- **Files:** 45 | **LOC:** 2,590
- **Risk Level:** 🟡 Medium (binary-incompatible APIs, source-incompatible APIs, security vulnerability)

#### Target State
- **Framework:** `net10.0`
- **Packages updated:** 6 (5 Microsoft.Extensions.* + MimeKit security fix)
- **API issues to resolve:** 25+ (3 binary-incompatible, 8 source-incompatible, 14 behavioral)

#### Migration Steps

1. **Update TargetFramework** in `HomeAutomation.Core\HomeAutomation.Core.csproj`:
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

2. **Update Package References** in `HomeAutomation.Core\HomeAutomation.Core.csproj`:
   | Package | From | To | Notes |
   |---|---|---|---|
   | `Microsoft.Extensions.Configuration.Abstractions` | 9.0.8 | 10.0.5 | Framework alignment |
   | `Microsoft.Extensions.Configuration.Binder` | 9.0.8 | 10.0.5 | Framework alignment |
   | `Microsoft.Extensions.Hosting.Abstractions` | 9.0.8 | 10.0.5 | Framework alignment |
   | `Microsoft.Extensions.Http` | 9.0.8 | 10.0.5 | Framework alignment |
   | `Microsoft.Extensions.Logging.Abstractions` | 9.0.8 | 10.0.5 | Framework alignment |
   | `MimeKit` | 4.13.0 | 4.15.1 | **🔴 Security vulnerability fix** |

3. **Expected Breaking Changes — Must Fix:**

   **🔴 Binary Incompatible — `ConfigurationBinder` API changes**
   - File: `HomeAutomation.Core\ScheduledJobs\CalculateBatteryChargingScheduleJob.cs`
   - `ConfigurationBinder.GetValue<T>(IConfiguration, string)` — method signature changed in .NET 10. Review all calls to `configuration.GetValue<T>(...)` and update as required by the new overload resolution.
   - `ConfigurationBinder.Get<T>(IConfiguration)` — binary-incompatible change (2 occurrences). Review all calls to `configuration.Get<T>()` and update accordingly.
   - Reference: https://go.microsoft.com/fwlink/?linkid=2262679

   **🟡 Source Incompatible — `TimeSpan` factory methods**
   - File: `HomeAutomation.Core\ScheduledJobs\CalculateBatteryChargingScheduleJob.cs` (lines 103, 109)
   - File: `HomeAutomation.Core\ScheduledJobs\Base\ScheduledJobHandler.cs` (line 17)
   - `TimeSpan.FromHours(double)` — new integer overloads introduced in .NET 10 create ambiguity. All 6 call sites passing `double` literals or `double` variables must use an explicit `double` cast or switch to the `double` overload explicitly:
     ```csharp
     // Before (ambiguous in .NET 10):
     TimeSpan.FromHours(configuration.GetValue<double>("Key", 10))
     // After (explicit):
     TimeSpan.FromHours((double)configuration.GetValue<double>("Key", 10))
     ```
   - `TimeSpan.FromSeconds(long)` — same ambiguity issue for the 2 call sites in `ScheduledJobHandler.cs`:
     ```csharp
     // Before:
     TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(interval)
     // After (if interval is int/long, cast to double):
     TimeSpan.FromSeconds((double)30), TimeSpan.FromSeconds((double)interval)
     ```
   - Reference: https://go.microsoft.com/fwlink/?linkid=2262679

4. **Behavioral Changes — Review Required:**

   **`System.Net.Http.HttpContent` (11 occurrences)**
   - Multiple files use `HttpContent` reading APIs (e.g., `response.Content.ReadFromJsonAsync<T>(...)` in `ImportSunDataScheduleJob.cs` line 21).
   - Behavioral change in .NET 10: verify response content reading behavior is consistent. Test HTTP-dependent jobs in a real environment after upgrade.

   **`System.Uri` constructor (3 occurrences)**
   - File: `HomeAutomation.Core\ScheduledJobs\CalculateBatteryChargingScheduleJob.cs` (line 164)
   - `new Uri(string)` and `request.Headers.Referrer = new Uri(...)` — URI parsing behavior changed in .NET 10. Verify that empty strings or special characters in the `EnergyPrices:Referrer` configuration value are handled correctly.

5. **Compatible Packages (no change):**
   - `SlackNet` 0.17.3 ✅
   - `SlackNet.Extensions.DependencyInjection` 0.17.3 ✅
   - `SmtpServer` 11.0.0 ✅

#### Validation Checklist
- [ ] `HomeAutomation.Core` builds without errors
- [ ] `HomeAutomation.Core` builds without warnings
- [ ] `TimeSpan.FromHours` and `TimeSpan.FromSeconds` call sites reviewed and updated
- [ ] `ConfigurationBinder.GetValue<T>` and `ConfigurationBinder.Get<T>` call sites reviewed and updated
- [ ] `HttpContent` usage reviewed for behavioral changes
- [ ] `System.Uri` usage reviewed for behavioral changes
- [ ] `MimeKit` updated to 4.15.1 (security fix applied)

---

### 5.4 HomeAutomation

#### Current State
- **Framework:** `net9.0`
- **Project Kind:** ASP.NET Core with Blazor Server + Blazor WebAssembly hosting
- **Dependencies:** `HomeAutomation.Database`, `HomeAutomation.Core`, `HomeAutomation.Client`
- **Dependants:** None (root application)
- **Files:** 42 | **LOC:** 537
- **Risk Level:** 🟢 Low (2 behavioral changes only)

#### Target State
- **Framework:** `net10.0`
- **Packages updated:** 2
- **API issues to review:** 2 (behavioral changes only)

#### Migration Steps

1. **Update TargetFramework** in `HomeAutomation\HomeAutomation.csproj`:
   ```xml
   <TargetFramework>net10.0</TargetFramework>
   ```

2. **Update Package References** in `HomeAutomation\HomeAutomation.csproj`:
   | Package | From | To |
   |---|---|---|
   | `Microsoft.AspNetCore.Components.WebAssembly.Server` | 9.0.8 | 10.0.5 |
   | `Microsoft.EntityFrameworkCore.Design` | 9.0.8 | 10.0.5 |

3. **Behavioral Changes — Review Required:**

   **`UseExceptionHandler(IApplicationBuilder, string, bool)` — `Program.cs` line 58**
   ```csharp
   // Current:
   app.UseExceptionHandler("/Error", createScopeForErrors: true);
   ```
   Behavioral change in ASP.NET Core 10: verify that exception handling still routes to `/Error` as expected and that the `createScopeForErrors: true` behavior is consistent with .NET 10 semantics.

   **`AddHttpClient(IServiceCollection)` — `Program.cs` line 20**
   ```csharp
   // Current:
   builder.Services.AddHttpClient();
   ```
   Behavioral change in .NET 10: `AddHttpClient()` default configuration may have changed. Verify that all named/typed HTTP clients registered elsewhere in the application still resolve correctly.

4. **Compatible Packages (no change):**
   - `MudBlazor` 8.11.0 ✅

#### Validation Checklist
- [ ] `HomeAutomation` builds without errors
- [ ] `HomeAutomation` builds without warnings
- [ ] `UseExceptionHandler` behavior verified in runtime
- [ ] `AddHttpClient` registration behavior verified
- [ ] Application starts successfully

---

## 6. Package Update Reference

### Packages Requiring Update (9 total)

| Package | Current | Target | Projects Affected | Reason |
|---|---|---|---|---|
| `Microsoft.AspNetCore.Components.WebAssembly` | 9.0.8 | 10.0.5 | `HomeAutomation.Client` | Framework version alignment |
| `Microsoft.AspNetCore.Components.WebAssembly.Server` | 9.0.8 | 10.0.5 | `HomeAutomation` | Framework version alignment |
| `Microsoft.EntityFrameworkCore.Design` | 9.0.8 | 10.0.5 | `HomeAutomation` | Framework version alignment |
| `Microsoft.Extensions.Configuration.Abstractions` | 9.0.8 | 10.0.5 | `HomeAutomation.Core` | Framework version alignment |
| `Microsoft.Extensions.Configuration.Binder` | 9.0.8 | 10.0.5 | `HomeAutomation.Core` | Framework version alignment |
| `Microsoft.Extensions.Hosting.Abstractions` | 9.0.8 | 10.0.5 | `HomeAutomation.Core`, `HomeAutomation.Database` | Framework version alignment |
| `Microsoft.Extensions.Http` | 9.0.8 | 10.0.5 | `HomeAutomation.Core` | Framework version alignment |
| `Microsoft.Extensions.Logging.Abstractions` | 9.0.8 | 10.0.5 | `HomeAutomation.Core` | Framework version alignment |
| `MimeKit` | 4.13.0 | **4.15.1** | `HomeAutomation.Core` | **🔴 Security vulnerability fix** |

### Compatible Packages (no change required — 5 total)

| Package | Version | Projects | Status |
|---|---|---|---|
| `MudBlazor` | 8.11.0 | `HomeAutomation` | ✅ Compatible |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 9.0.4 | `HomeAutomation.Database` | ✅ Compatible |
| `SlackNet` | 0.17.3 | `HomeAutomation.Core` | ✅ Compatible |
| `SlackNet.Extensions.DependencyInjection` | 0.17.3 | `HomeAutomation.Core` | ✅ Compatible |
| `SmtpServer` | 11.0.0 | `HomeAutomation.Core` | ✅ Compatible |

> **Note:** `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.4 is marked compatible with net10.0 by the assessment. Monitor for a `10.x` release if runtime issues arise with EF Core interactions.

---

## 7. Breaking Changes Catalog

All breaking changes are confined to **`HomeAutomation.Core`** and **`HomeAutomation`**.

### 7.1 Binary-Incompatible Changes (Must Fix — Compilation Will Fail)

#### `ConfigurationBinder.GetValue<T>` — `HomeAutomation.Core`

| Detail | Value |
|---|---|
| API | `Microsoft.Extensions.Configuration.ConfigurationBinder.GetValue<T>(IConfiguration, string)` |
| File | `HomeAutomation.Core\ScheduledJobs\CalculateBatteryChargingScheduleJob.cs` |
| Line | 151 |
| Category | 🔴 Binary Incompatible |
| Occurrences | 1 |

**Current code:**
```csharp
configuration.GetValue<string>("EnergyPrices:APIUrl") + date.ToString("yyyy-MM-dd")
```
**Action:** Review the method signature changes for `GetValue<T>` in .NET 10. The overload resolution has changed; update the call site to match the new API if compilation fails.

---

#### `ConfigurationBinder.Get<T>` — `HomeAutomation.Core`

| Detail | Value |
|---|---|
| API | `Microsoft.Extensions.Configuration.ConfigurationBinder.Get<T>(IConfiguration)` |
| Category | 🔴 Binary Incompatible |
| Occurrences | 2 |

**Action:** Locate all usages of `configuration.Get<T>()` in `HomeAutomation.Core` and update to match the new .NET 10 method signature. Reference: https://go.microsoft.com/fwlink/?linkid=2262679

---

### 7.2 Source-Incompatible Changes (Must Fix — Will Not Compile)

#### `TimeSpan.FromHours(double)` — `HomeAutomation.Core`

| Detail | Value |
|---|---|
| API | `System.TimeSpan.FromHours(System.Double)` |
| Files | `HomeAutomation.Core\ScheduledJobs\CalculateBatteryChargingScheduleJob.cs` |
| Lines | 103 (×3), 109 (×3) |
| Category | 🟡 Source Incompatible |
| Occurrences | 6 |

**Root Cause:** .NET 10 introduces new integer overloads for `TimeSpan.FromHours(int)`. When passing a `double` from `GetValue<double>(...)`, the compiler cannot resolve the correct overload and produces an error.

**Action:** Add an explicit `double` cast at all 6 call sites:
```csharp
// Before:
TimeSpan.FromHours(configuration.GetValue<double>("EnergyCalculation:DayChargingStartHour", 10))

// After:
TimeSpan.FromHours((double)configuration.GetValue<double>("EnergyCalculation:DayChargingStartHour", 10))
```

---

#### `TimeSpan.FromSeconds(long)` — `HomeAutomation.Core`

| Detail | Value |
|---|---|
| API | `System.TimeSpan.FromSeconds(System.Int64)` |
| File | `HomeAutomation.Core\ScheduledJobs\Base\ScheduledJobHandler.cs` |
| Line | 17 |
| Category | 🟡 Source Incompatible |
| Occurrences | 2 |

**Root Cause:** Same new-overload ambiguity as `FromHours`. The integer/long literal `30` and the `interval` variable are ambiguous between `FromSeconds(double)` and new integer overloads.

**Current code:**
```csharp
_timer = new Timer(ProcessTimer, cancellationToken, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(interval));
```
**Action:** Use explicit `double` cast or use the `TimeSpan.FromSeconds(double)` approach:
```csharp
_timer = new Timer(ProcessTimer, cancellationToken, TimeSpan.FromSeconds((double)30), TimeSpan.FromSeconds((double)interval));
```

---

### 7.3 Behavioral Changes (Review Required — Will Compile, May Behave Differently)

#### `System.Net.Http.HttpContent` — `HomeAutomation.Core`

| Detail | Value |
|---|---|
| API | `System.Net.Http.HttpContent` |
| File | `HomeAutomation.Core\ScheduledJobs\ImportSunDataScheduleJob.cs` |
| Line | 21 |
| Category | 🔵 Behavioral Change |
| Occurrences | 11 |

**Current code:**
```csharp
var data = await response.Content.ReadFromJsonAsync<SunDataResponse>(cancellationToken);
```
**Action:** `HttpContent` reading APIs may have changed default encoding or buffering behavior in .NET 10. Test all scheduled jobs that make HTTP calls after the upgrade and verify that response deserialization produces expected results.

---

#### `System.Uri` and `Uri` constructor — `HomeAutomation.Core`

| Detail | Value |
|---|---|
| API | `System.Uri`, `System.Uri..ctor(System.String)` |
| File | `HomeAutomation.Core\ScheduledJobs\CalculateBatteryChargingScheduleJob.cs` |
| Line | 164 |
| Category | 🔵 Behavioral Change |
| Occurrences | 3 |

**Current code:**
```csharp
request.Headers.Referrer = new Uri(configuration.GetValue("EnergyPrices:Referrer", ""));
```
**Action:** URI parsing rules have changed in .NET 10. If `EnergyPrices:Referrer` can be an empty string or relative URI, validate that the behavior is still correct. Consider guarding with a null/empty check before constructing the `Uri`.

---

#### `UseExceptionHandler(IApplicationBuilder, string, bool)` — `HomeAutomation`

| Detail | Value |
|---|---|
| API | `Microsoft.AspNetCore.Builder.ExceptionHandlerExtensions.UseExceptionHandler` |
| File | `HomeAutomation\Program.cs` |
| Line | 58 |
| Category | 🔵 Behavioral Change |
| Occurrences | 1 |

**Current code:**
```csharp
app.UseExceptionHandler("/Error", createScopeForErrors: true);
```
**Action:** Verify that the exception handler middleware routes errors to `/Error` correctly in .NET 10. The `createScopeForErrors` parameter behavior may have changed. Test error pages in the running application.

---

#### `AddHttpClient(IServiceCollection)` — `HomeAutomation`

| Detail | Value |
|---|---|
| API | `Microsoft.Extensions.DependencyInjection.HttpClientFactoryServiceCollectionExtensions.AddHttpClient` |
| File | `HomeAutomation\Program.cs` |
| Line | 20 |
| Category | 🔵 Behavioral Change |
| Occurrences | 1 |

**Current code:**
```csharp
builder.Services.AddHttpClient();
```
**Action:** Verify that all HTTP clients in the application resolve correctly after the upgrade. Default `HttpClient` configuration (timeouts, retry policies, handlers) may have changed in .NET 10.

---

## 8. Testing & Validation Strategy

### No Automated Test Projects

The solution contains no test projects. Validation relies on build success and manual runtime checks.

### Build Validation

After the atomic upgrade is complete:
```sh
dotnet build HomeAutomation.sln
```
**Pass criteria:** 0 errors, 0 warnings (or all warnings reviewed and accepted).

### Manual Runtime Validation

After successful build, start the application and verify:

| Area | What to Check |
|---|---|
| Application startup | Application starts without exceptions in the console |
| Blazor UI | Main UI loads in the browser without errors |
| Exception handling | Navigate to a non-existent route and confirm the `/Error` page displays correctly |
| HTTP client usage | Trigger any feature that calls an external HTTP API and verify the response is processed correctly |
| Scheduled jobs | Monitor logs after startup to confirm `ScheduledJobHandler` initializes timers without errors |
| Energy calculation job | Trigger `CalculateBatteryChargingScheduleJob` and verify `TimeSpan` calculations produce correct results |
| HTTP content reading | Trigger `ImportSunDataScheduleJob` and verify `ReadFromJsonAsync` deserializes correctly |
| URI handling | Verify `EnergyPrices:Referrer` configuration value produces a valid `Uri` instance |
| Database connectivity | Verify EF Core migrations and queries execute successfully against the PostgreSQL database |

### EF Core Migration Verification

After the upgrade, run a quick DB health check:
```sh
dotnet ef database update --context DefaultContext --project HomeAutomation.Database --startup-project HomeAutomation
```
Confirm no migration errors occur.

---

## 9. Risk Management

### Risk Summary Table

| Project | Risk Level | Key Risk | Mitigation |
|---|---|---|---|
| `HomeAutomation.Database` | 🟢 Low | None — no API issues | Straightforward package + framework update |
| `HomeAutomation.Client` | 🟢 Low | None — no API issues | Straightforward package + framework update |
| `HomeAutomation.Core` | 🟡 Medium | Binary-incompatible `ConfigurationBinder` APIs; source-incompatible `TimeSpan` factory overloads | Follow §7 Breaking Changes Catalog exactly; fix compilation errors before proceeding |
| `HomeAutomation` | 🟢 Low | Behavioral changes in `UseExceptionHandler` and `AddHttpClient` | Manual runtime verification of error handling and HTTP client resolution |

### Security Risk

| Package | Vulnerability | Severity | Fix |
|---|---|---|---|
| `MimeKit` 4.13.0 | Known security vulnerability | 🔴 Critical | Upgrade to 4.15.1 as part of atomic upgrade — do not defer |

### Contingency Plans

**If `ConfigurationBinder` binary-incompatible errors cannot be resolved automatically:**
- Consult the official .NET 10 breaking changes documentation: https://go.microsoft.com/fwlink/?linkid=2262679
- As a temporary workaround, use `IConfiguration["key"]` direct indexer access instead of `GetValue<T>` while investigating the new API surface.

**If `TimeSpan` overload ambiguity persists after adding explicit casts:**
- Switch to `new TimeSpan(hours: 0, minutes: 0, seconds: (int)interval)` constructors as an alternative.

**If `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.4 exhibits runtime issues with .NET 10:**
- Check the Npgsql release feed for a `10.x` compatible version and update `HomeAutomation.Database` accordingly.

**If the build produces unexpected additional breaking changes not listed in this plan:**
- These are likely in the 13 files marked with incidents in `HomeAutomation.Core` that were not fully enumerated due to the 20-item query limit.
- Follow the same pattern: consult https://go.microsoft.com/fwlink/?linkid=2262679 and apply the smallest code change necessary.

---

## 10. Complexity & Effort Assessment

### Per-Project Complexity

| Project | Complexity | LOC | API Issues | Package Changes | Driver |
|---|---|---|---|---|---|
| `HomeAutomation.Database` | 🟢 Low | 4,918 | 0 | 1 | Framework + 1 package update only |
| `HomeAutomation.Client` | 🟢 Low | 5 | 0 | 1 | Minimal project, 1 package update only |
| `HomeAutomation.Core` | 🟡 Medium | 2,590 | 25+ | 6 | Binary + source-incompatible API fixes required |
| `HomeAutomation` | 🟢 Low | 537 | 2 | 2 | Behavioral changes only — no code fixes expected |

### Phase Complexity

| Phase | Complexity | Description |
|---|---|---|
| Phase 0 — Prerequisites | 🟢 Low | SDK validation only |
| Phase 1 — Atomic Upgrade | 🟡 Medium | Driven by `HomeAutomation.Core` API fixes |
| Phase 2 — Validation | 🟢 Low | Manual runtime checks, no test infrastructure |

### Resource Guidance

- A developer familiar with .NET upgrade patterns and the `HomeAutomation.Core` scheduled jobs codebase (particularly `CalculateBatteryChargingScheduleJob.cs`) should handle the `TimeSpan` and `ConfigurationBinder` fixes.
- The remaining projects require no specialist knowledge beyond basic .NET project file editing.

---

## 11. Source Control Strategy

### Branch Strategy

Per user preference, all upgrade changes are committed directly to the **`main`** branch. No separate upgrade branch is used.

### Commit Strategy

**Single commit** — Prefer committing all upgrade changes (project file updates, package reference updates, and code fixes) in one atomic commit once the solution builds with 0 errors:

```
chore: upgrade solution from .NET 9 to .NET 10

- Updated TargetFramework to net10.0 in all 4 projects
- Updated Microsoft platform packages 9.0.8 → 10.0.5
- Updated MimeKit 4.13.0 → 4.15.1 (security fix)
- Fixed TimeSpan.FromHours / FromSeconds overload ambiguity (HomeAutomation.Core)
- Fixed ConfigurationBinder.GetValue / Get binary-incompatible API usage (HomeAutomation.Core)
```

If the upgrade spans multiple sessions or requires intermediate save points, use descriptive WIP commits and squash before finalizing.

### Review Process

- Review `HomeAutomation.Core` changes carefully — this project contains all binary and source-incompatible API fixes
- Verify the `MimeKit` version bump is present in the commit (security fix)
- Confirm all 4 `TargetFramework` changes are included

---

## 12. Success Criteria

### Technical Criteria

- [ ] All 4 projects have `<TargetFramework>net10.0</TargetFramework>`
- [ ] All 9 recommended package updates applied (versions match §6 Package Update Reference)
- [ ] `MimeKit` updated to `4.15.1` — security vulnerability resolved
- [ ] `dotnet build HomeAutomation.sln` produces **0 errors**
- [ ] No unresolved NuGet package references
- [ ] `TimeSpan.FromHours` and `TimeSpan.FromSeconds` call sites updated (8 total in `HomeAutomation.Core`)
- [ ] `ConfigurationBinder.GetValue<T>` and `Get<T>` call sites updated (3 total in `HomeAutomation.Core`)

### Quality Criteria

- [ ] Application starts without runtime exceptions
- [ ] Blazor UI renders correctly in browser
- [ ] Exception handling (`/Error`) works as expected
- [ ] Scheduled jobs initialize and run without errors
- [ ] EF Core database connectivity verified
- [ ] HTTP client registrations resolve correctly
- [ ] `Uri` construction with configuration values behaves correctly

### Process Criteria

- [ ] All-At-Once Strategy applied — single atomic upgrade operation
- [ ] Changes committed to `main` branch with descriptive commit message
- [ ] Breaking changes addressed as catalogued in §7
- [ ] No packages deferred or skipped without explicit justification
