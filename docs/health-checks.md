# Health Checks

A service is likely to depend on other resources to be able to function properly. This might be a database, a storage location or another service. Healthchecks allow you to easily check that a service has access to everything it needs and everything is in the right state for the service to operate properly.

 
Healthchecks can be added to a middleware pipeline using the .UseHealthCheck() extension method.

```csharp 
.UseHealthCheck("healthcheck", x => x
    .AddHealthCheck<SimpleHealthCheck>()
    .AddHealthCheck(new SimpleHealthCheck())
    .AddHealthCheckFactory(new SimpleHealthCheckFactory())
    .AddHealthCheck(_ => new SimpleHealthCheck())
    .AddHealthCheck(resolver => resolver.GetService<SimpleHealthCheck>())
    .AddHealthCheck("inline", _ => Task.FromResult(new HealthCheckResult(false)))
    .AddHealthCheck("inline", _ => new HealthCheckResult(false))
    .AddHealthCheck(async _ => await Task.FromResult(new HealthCheckResult(false)))
    .AddHealthCheck(_ => new HealthCheckResult(false)));
 ```

There are a variety of ways a specific healthcheck can be added, either using a class that inherits from IHealthCheck, by a health check factory or a inline lambda function.

The cleanest way of adding a healthcheck is probably by creating each healthcheck as it’s own class.

### Example
The following example is for a simple health check to check that a service can connect to a database.

It is best practice to give each health a unique name, this is so used to report back the success or failure of the health check.

The HealthCheckResult object contains a Dictionary where any meta data from the health check can be set. In this case a “CanConnect” field has been set.

```csharp 
public class DatabaseConnectionHealthCheck<TDbContext> : IHealthCheck where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    public string Name => "DatabaseConnection";
    
    public DatabaseConnectionHealthCheck(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> ExecuteAsync()
    {
        var canConnect = await TryConnect(_dbContext);
        return new HealthCheckResult(canConnect, new Dictionary<string, object>
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