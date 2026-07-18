# Entity Framework Core Integration

Use EF Core for data access in a Benzene service — injected into your handlers the standard way —
and add a database health check with `Benzene.HealthChecks.EntityFramework`.

## Problem Statement

You want to:
- Access a database from message handlers using EF Core
- Keep data access behind a port so handlers stay testable and portable
- Expose a health check that verifies the database connection (and, optionally, that migrations are
  applied)

## Prerequisites

- A Benzene service
- EF Core and a provider (`Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.SqlServer`, …)
- `Benzene.HealthChecks.EntityFramework` for the health check

```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL      # example provider
dotnet add package Benzene.HealthChecks.EntityFramework --prerelease
```

## Step-by-Step Implementation

### 1. Define your DbContext

A standard EF Core `DbContext`:

```csharp
public class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
}
```

### 2. Keep data access behind a port

Rather than injecting `DbContext` straight into handlers, put it behind a repository interface (a
"port"). This keeps handlers ignorant of EF and easy to test:

```csharp
public interface IOrderRepository
{
    Task<IBenzeneResult<OrderDto>> GetAsync(string id);
}

public class OrderRepository : IOrderRepository
{
    private readonly OrdersDbContext _db;
    public OrderRepository(OrdersDbContext db) => _db = db;

    public async Task<IBenzeneResult<OrderDto>> GetAsync(string id)
    {
        var order = await _db.Orders.FindAsync(id);
        return order is null
            ? BenzeneResult.NotFound<OrderDto>()
            : BenzeneResult.Ok(order.ToDto());
    }
}
```

### 3. Register EF Core and the repository

In `ConfigureServices`, register the `DbContext` and your repository:

```csharp
services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(configuration["DB_CONNECTION_STRING"]));

services.AddScoped<IOrderRepository, OrderRepository>();

services.UsingBenzene(x => x
    .AddMessageHandlers(typeof(GetOrderHandler).Assembly));
```

Handlers then depend only on `IOrderRepository` — no EF types leak into your logic.

### 4. Add a database health check

`Benzene.HealthChecks.EntityFramework` provides checks keyed on your `DbContext`:

- `DatabaseConnectionHealthCheck<TDbContext>` — verifies the database is reachable.
- `DatabaseHealthCheck<TDbContext>` — verifies connectivity and that a target migration is applied.

Add one to your [health-check](../health-checks.md) set:

```csharp
var healthChecks = new IHealthCheck[]
{
    new DatabaseConnectionHealthCheck<OrdersDbContext>(dbContext)
};
```

(Resolve the `DbContext` from the scope, or register the check via a `DatabaseHealthCheckFactory<OrdersDbContext>`.)

## Testing

Because handlers depend on `IOrderRepository`, unit-test them with a mocked repository (see
[Mocking External Dependencies](mocking-dependencies.md)) — no database needed. For repository
tests, use EF Core's in-memory or SQLite provider, or point at a real database in Docker via
[`WithConfiguration`](../testing-benzene.md) for an integration test.

```csharp
var repository = new Mock<IOrderRepository>();
repository.Setup(x => x.GetAsync("123")).ReturnsAsync(BenzeneResult.Ok(new OrderDto { Id = "123" }));

var host = new AwsLambdaBenzeneTestHost(
    BenzeneTestHost.Create<StartUp>()
        .WithServices(s => s.AddScoped(_ => repository.Object))
        .BuildAwsLambdaHost());
```

## Troubleshooting

### `DbContext` lifetime errors on Lambda

**Problem**: `DbContext` disposed / concurrency errors under load.

**Solution**: `DbContext` is scoped — Benzene creates a scope per message, so a scoped `DbContext`
is correct. Don't cache a `DbContext` in a singleton. On serverless, keep connection pools small and
consider connection resilience (`EnableRetryOnFailure`).

### Cold-start latency from the first query

**Problem**: The first request after a cold start is slow.

**Solution**: The initial connection and model build happen on first use. See
[Lambda Cold Start Optimization](lambda-cold-start-optimization.md) for reducing cold-start cost.

## Variations

### Add caching

Layer [Redis caching](redis-caching.md) in the repository so hot reads skip the database.

### Migrations

Run EF migrations as part of deployment rather than at startup on serverless hosts; use
`DatabaseHealthCheck<TDbContext>` with a target migration to detect drift.

## Further Reading

- [Health Checks](../health-checks.md) - the health-check pipeline
- [Redis Caching](redis-caching.md) - caching reads in front of EF
- [Message Handlers](../message-handlers.md) - keeping handlers thin and port-based
- [Package Reference](../reference/packages.md#health-checks) - the EF health-check package
