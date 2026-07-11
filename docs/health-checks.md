# Health Checks

A service is likely to depend on other resources to be able to function properly. This might be a database, a storage location or another service. Healthchecks allow you to easily check that a service has access to everything it needs and everything is in the right state for the service to operate properly.

Healthchecks can be added to a middleware pipeline using the `.UseHealthCheck()` extension method.

```csharp
.UseHealthCheck("healthcheck", x => x
    .AddHealthCheck<SimpleHealthCheck>()
    .AddHealthCheck(resolver => resolver.GetService<SimpleHealthCheck>())
    .AddHealthCheck("inline", _ => true)
    .AddHealthCheck("inline", async _ => await Task.FromResult(true))
    .AddHealthCheck(_ => true))
```

There are a variety of ways a specific healthcheck can be added: as a class that inherits from `IHealthCheck` (resolved from DI with `AddHealthCheck<T>()` or a factory function), or as an inline function returning a `bool` or `Task<bool>`.

The cleanest way of adding a healthcheck is probably by creating each healthcheck as it's own class.

### Example
The following example is for a simple health check to check that a service can connect to a database.

Each health check reports its own `Type` (a name used to identify it in the result), which is set on the `HealthCheckResult` it returns.

The `HealthCheckResult` also carries a `Data` dictionary where any metadata from the health check can be set. In this case a “CanConnect” field has been set.

```csharp
public class DatabaseConnectionHealthCheck<TDbContext> : IHealthCheck where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    public string Type => "DatabaseConnection";

    public DatabaseConnectionHealthCheck(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IHealthCheckResult> ExecuteAsync()
    {
        var canConnect = await TryConnect(_dbContext);
        return HealthCheckResult.CreateInstance(canConnect, Type, new Dictionary<string, object>
        {
            { "CanConnect", canConnect },
        });
    }

    private static async Task<bool> TryConnect(DbContext dbContext)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }
}
```

### Running a health check
You can then send it a healthcheck request using the JSON below.

```json
{
  "topic": "healthcheck"
}
```

The output of the healthcheck will be outputted in the UI.
