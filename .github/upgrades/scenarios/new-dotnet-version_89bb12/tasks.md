# HomeAutomation .NET 10.0 Upgrade Tasks

## Overview

This document tracks the execution of the HomeAutomation solution upgrade from .NET 9.0 to .NET 10.0. All four projects will be upgraded simultaneously in a single atomic operation.

**Progress**: 1/2 tasks complete (50%) ![0%](https://progress-bar.xyz/50)

---

## Tasks

### [✓] TASK-001: Verify prerequisites *(Completed: 2026-03-13 09:48)*
**References**: Plan §4 Step 0

- [✓] (1) Verify .NET 10 SDK is installed (`dotnet --list-sdks` shows version 10.x.x)
- [✓] (2) .NET 10 SDK installation confirmed (**Verify**)

---

### [▶] TASK-002: Atomic framework and dependency upgrade
**References**: Plan §4 Steps 1-4, Plan §5 Project-by-Project Migration Plans, Plan §6 Package Update Reference, Plan §7 Breaking Changes Catalog

- [✓] (1) Update `<TargetFramework>` to `net10.0` in all 4 project files per Plan §5.1-5.4 (HomeAutomation.Database, HomeAutomation.Client, HomeAutomation.Core, HomeAutomation)
- [✓] (2) All project files updated to net10.0 (**Verify**)
- [✓] (3) Update all 9 package references per Plan §6 Package Update Reference (8 Microsoft packages 9.0.8→10.0.5, MimeKit 4.13.0→4.15.1)
- [✓] (4) All package references updated (**Verify**)
- [✓] (5) Restore all dependencies (`dotnet restore HomeAutomation.sln`)
- [✓] (6) All dependencies restored successfully (**Verify**)
- [✓] (7) Build solution and fix all compilation errors per Plan §7 Breaking Changes Catalog (focus: ConfigurationBinder binary-incompatible APIs in HomeAutomation.Core, TimeSpan.FromHours/FromSeconds source-incompatible overloads in HomeAutomation.Core; review behavioral changes for HttpContent, Uri, UseExceptionHandler, AddHttpClient)
- [✓] (8) Solution builds with 0 errors (**Verify**)
- [▶] (9) Commit all changes with message: "chore: upgrade solution from .NET 9 to .NET 10 - Updated TargetFramework to net10.0 in all 4 projects - Updated Microsoft platform packages 9.0.8 → 10.0.5 - Updated MimeKit 4.13.0 → 4.15.1 (security fix) - Fixed TimeSpan.FromHours/FromSeconds overload ambiguity (HomeAutomation.Core) - Fixed ConfigurationBinder API binary-incompatible usage (HomeAutomation.Core)"

---





