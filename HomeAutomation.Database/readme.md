# Add migrations

## Default context

```
dotnet ef migrations add <migration_name> --context DefaultContext --project HomeAutomation.Database --startup-project HomeAutomation --output-dir Migrations/DefaultMigrations --prefix-output
```

## Log context

```
dotnet ef migrations add <migration_name> --context LogContext --project HomeAutomation.Database --startup-project HomeAutomation --output-dir Migrations/LogMigrations --prefix-output
```