# Handle migrations

## Add migrations

```
dotnet ef migrations add <migration_name> --context DefaultContext --project HomeAutomation.Database --startup-project HomeAutomation
```

## Remove latest migration

```
dotnet ef migrations remove --context DefaultContext --project HomeAutomation.Database --startup-project HomeAutomation
```

## Update DB to latest migration

```
dotnet ef database update --context DefaultContext --project HomeAutomation.Database --startup-project HomeAutomation
```

## Generate SQL from migrations

```
dotnet ef migrations script --context DefaultContext --project HomeAutomation.Database --startup-project HomeAutomation
```

## Build migrations bundle

```
dotnet ef migrations bundle --self-contained --project HomeAutomation.Database --startup-project HomeAutomation --output ../.build-migrations/efBundle.exe --force
```