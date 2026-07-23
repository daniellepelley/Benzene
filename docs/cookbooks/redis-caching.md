# Cache Handler Responses with Redis

Use `Benzene.Cache.Redis` to cache expensive reads behind a message handler, and keep the cache
correct on writes and deletes.

## Problem Statement

You have a message handler that reads from a slow or rate-limited downstream (a database, a
third-party API) on every request. You want to:

- Serve repeated reads from Redis instead of hitting the downstream every time
- Automatically refresh the cache when the underlying data changes
- Explicitly invalidate the cache when a record is deleted
- Do all of this without hand-writing "check cache, then call downstream, then update cache"
  boilerplate in every handler

This cookbook builds a small product catalog on top of the caching abstractions described in
[Caching](../caching.md) — read that first if you haven't; it's the reference doc for
`Benzene.Cache.Core`/`Benzene.Cache.Redis` and covers every member in detail. This cookbook is the
worked example on top of it: a full read/write/invalidate cycle wired into real message handlers.

## Prerequisites

- A Benzene service using `Benzene.Core.MessageHandlers` (message handlers returning
  `IBenzeneResult<T>`) — see [Getting Started: Benzene on AWS Lambda](../getting-started-aws.md)
  if you don't have one yet.
- A reachable Redis instance (a local Docker container is enough for development — see
  [Testing](#testing) below).
- An `IProcessTimerFactory` registered in DI. This is a **hard constructor dependency** of
  `RedisCacheService` — DI resolution fails without it. See [Installation](#installation).

## Installation

```bash
dotnet add package Benzene.Cache.Core --prerelease
dotnet add package Benzene.Cache.Redis --prerelease
```

`Benzene.Cache.Redis` pulls in `Benzene.Cache.Core` and `StackExchange.Redis` transitively — no
other NuGet dependencies are added.

You also need something registering `IProcessTimerFactory` (from `Benzene.Diagnostics.Timers`).
The normal way is `Benzene.Diagnostics`'s `AddDiagnostics()`:

```bash
dotnet add package Benzene.Diagnostics --prerelease
```

If you don't want `Benzene.Diagnostics`'s automatic per-middleware tracing, register
`DebugTimerFactory` (or another `IProcessTimerFactory` implementation) directly instead — see
[Step 2](#2-register-services) below. Either way, skipping this step entirely means your cache
service subclass can't be constructed; see [Troubleshooting](#troubleshooting).

## Step-by-Step Implementation

### 1. Define the port and a Redis-backed cache service

The repository/API you're caching in front of is a port — an interface your handlers depend on,
implemented by whatever actually talks to your database:

```csharp
using Benzene.Abstractions.Results;

public interface IProductRepository
{
    Task<IBenzeneResult<Product>> GetAsync(Guid productId);
    Task<IBenzeneResult<Product>> UpdateAsync(Guid productId, UpdateProductRequest request);
    Task<IBenzeneResult<Void>> DeleteAsync(Guid productId);
}

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public decimal PriceInCents { get; set; }
}
```

Subclass `RedisCacheService` and expose a typed `ICacheEntry<T>` for the key(s) you want to cache.
`GetConfigurationOptionsAsync()` is the only member you're required to implement — everything else
(serialization, TTL, lazy-load/write-through orchestration) is handled for you by the base class:

```csharp
using Benzene.Cache.Core;
using Benzene.Cache.Redis;
using Benzene.Diagnostics.Timers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

public class ProductCacheService : RedisCacheService
{
    private readonly IConfiguration _configuration;

    public ProductCacheService(
        ILogger<RedisCacheService> logger,
        IProcessTimerFactory processTimerFactory,
        IRedisConnectionFactory connectionFactory,
        IConfiguration configuration)
        : base(logger, processTimerFactory, connectionFactory)
    {
        _configuration = configuration;
        StartConnection(); // fail fast / warm the connection on construction
    }

    // Override the default 5-minute TTL used by SetValueAsync calls that don't pass expireIn.
    public override TimeSpan DefaultCacheLifespan => TimeSpan.FromMinutes(10);

    protected override Task<ConfigurationOptions> GetConfigurationOptionsAsync()
    {
        return Task.FromResult(ConfigurationOptions.Parse(
            _configuration["Redis:ConnectionString"] ?? "localhost:6379"));
    }

    public ICacheEntry<Product> GetProductEntry(Guid productId) =>
        CreateCacheEntry<Product>($"product:{productId}");
}
```

### 2. Register services

```csharp
using Benzene.Cache.Redis;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.UsingBenzene(x => x
        .AddBenzene()
        .AddDiagnostics() // registers ActivityProcessTimerFactory as IProcessTimerFactory
        .AddMessageHandlers(typeof(GetProductMessageHandler).Assembly)
        .AddHttpMessageHandlers());

    services.AddScoped<IRedisConnectionFactory, RedisConnectionFactory>();
    services.AddScoped<ProductCacheService>();
    services.AddScoped<IProductRepository, SqlProductRepository>();
}
```

If you don't want `AddDiagnostics()`'s tracing behavior, register a timer factory directly instead:

```csharp
services.AddScoped<IProcessTimerFactory>(_ => new DebugTimerFactory());
```

### 3. Read-through: check the cache before doing expensive work

`LazyLoadAsync` checks the cache first; on a miss, it calls your `databaseReadFunc`, and — if that
result `IsSuccessful` — stores the payload back in the cache before returning it:

```csharp
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Http;

[Message("products:get")]
[HttpEndpoint("GET", "/products/{productId}")]
public class GetProductMessageHandler : IMessageHandler<GetProductRequest, Product>
{
    private readonly ProductCacheService _cache;
    private readonly IProductRepository _products;

    public GetProductMessageHandler(ProductCacheService cache, IProductRepository products)
    {
        _cache = cache;
        _products = products;
    }

    public Task<IBenzeneResult<Product>> HandleAsync(GetProductRequest message)
    {
        var entry = _cache.GetProductEntry(message.ProductId);

        return entry.LazyLoadAsync(() => _products.GetAsync(message.ProductId));
    }
}

public class GetProductRequest
{
    public Guid ProductId { get; set; }
}
```

The first request for a given `productId` misses, calls `IProductRepository.GetAsync`, and caches
the result. Every request after that (until the 10-minute TTL from `DefaultCacheLifespan` expires)
is served straight from Redis without touching `IProductRepository` at all.

### 4. Write-through: keep the cache in sync on updates

`WriteThroughAsync` runs your write, then updates the cache based on the result's
`BenzeneResultStatus` — `ok`/`created`/`updated`/`accepted` sets the cache to the new payload,
`deleted` invalidates it, anything else leaves the cache untouched:

```csharp
[Message("products:update")]
[HttpEndpoint("PUT", "/products/{productId}")]
public class UpdateProductMessageHandler : IMessageHandler<UpdateProductRequest, Product>
{
    private readonly ProductCacheService _cache;
    private readonly IProductRepository _products;

    public UpdateProductMessageHandler(ProductCacheService cache, IProductRepository products)
    {
        _cache = cache;
        _products = products;
    }

    public Task<IBenzeneResult<Product>> HandleAsync(UpdateProductRequest message)
    {
        var entry = _cache.GetProductEntry(message.ProductId);

        return entry.WriteThroughAsync(() => _products.UpdateAsync(message.ProductId, message));
    }
}

public class UpdateProductRequest
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = "";
    public decimal PriceInCents { get; set; }
}
```

Because `IProductRepository.UpdateAsync` returns `IBenzeneResult<Product>` directly, the
single-argument `WriteThroughAsync(Func<Task<TResult>> modifyDatabaseFunc)` overload is all you
need — it reads the payload straight off the result. If your write method's return type isn't
`IBenzeneResult<T>` (e.g. it returns a wrapper type), use the
`WriteThroughAsync(modifyDatabaseFunc, getCacheValue)` overload instead (see
[Advanced Usage in Caching](../caching.md#advanced-usage)).

### 5. Invalidation: clear the cache on delete

`WriteThroughInvalidateAsync` runs your write and invalidates the cache only if the result
`IsSuccessful` — there's no `Set` path, since a delete has nothing to cache:

```csharp
using Benzene.Abstractions.Results;

[Message("products:delete")]
[HttpEndpoint("DELETE", "/products/{productId}")]
public class DeleteProductMessageHandler : IMessageHandler<DeleteProductRequest, Void>
{
    private readonly ProductCacheService _cache;
    private readonly IProductRepository _products;

    public DeleteProductMessageHandler(ProductCacheService cache, IProductRepository products)
    {
        _cache = cache;
        _products = products;
    }

    public Task<IBenzeneResult<Void>> HandleAsync(DeleteProductRequest message)
    {
        var entry = _cache.GetProductEntry(message.ProductId);

        return entry.WriteThroughInvalidateAsync(() => _products.DeleteAsync(message.ProductId));
    }
}

public class DeleteProductRequest
{
    public Guid ProductId { get; set; }
}
```

You can also invalidate directly, outside of a write, by calling `entry.InvalidateAsync()` — useful
if something other than this service changed the underlying record (e.g. a batch job, or another
service writing to the same database).

### 6. Wire up the pipeline

```csharp
public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
{
    app.UseAwsLambda(eventPipeline => eventPipeline
        .UseApiGateway(apiGatewayApp => apiGatewayApp
            .UseMessageHandlers()));
}
```

Nothing here is cache-specific — caching in Benzene isn't a middleware you add to a pipeline (see
[Caching: Overview](../caching.md#overview)). `ProductCacheService` is consumed directly by the
handlers above through constructor injection, the same as any other dependency.

## Testing

Benzene's own Redis cache tests
([`test/Benzene.Core.Test/Cache/Redis/RedisCacheServiceTest.cs`](../../test/Benzene.Core.Test/Cache/Redis/RedisCacheServiceTest.cs))
don't talk to a real Redis instance — they fake `IRedisConnectionFactory` with a `Mock<IDatabase>`
(see
[`Cache/Redis/Mocks/MockConnectionFactory.cs`](../../test/Benzene.Core.Test/Cache/Redis/Mocks/MockConnectionFactory.cs)),
so `StringGetAsync`/`StringSetAsync`/`KeyDeleteAsync` calls are asserted directly with Moq instead
of hitting the network. This is the fastest way to unit test your own cache-entry logic (TTLs,
which key gets touched, lazy-load vs. write-through branching) without any infrastructure:

```csharp
var connectionFactory = new MockConnectionFactory();
connectionFactory.DataBaseMock
    .Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<bool>(), When.Always, CommandFlags.None))
    .ReturnsAsync(true)
    .Verifiable();

var service = new ProductCacheService(NullLogger<RedisCacheService>.Instance, new DebugTimerFactory(), connectionFactory, configuration);
var entry = service.GetProductEntry(productId);

var result = await entry.WriteThroughAsync(() => Task.FromResult(BenzeneResult.Updated(product)));

connectionFactory.DataBaseMock.Verify();
```

(`MockConnectionFactory` above is internal to Benzene's test project — copy the pattern rather than
referencing it directly; it's a thin `IRedisConnectionFactory` wrapping `Mock<IDatabase>` and
`Mock<IConnectionMultiplexer>`.)

For a true integration test against a real Redis — confirming your `GetConfigurationOptionsAsync()`
connection string actually works, TTLs really expire, `KEYS`-based invalidation really matches —
run Redis via Docker Compose. Benzene's own Azure example already does this for its integration
tests ([`examples/Azure/docker-compose.yaml`](../../examples/Azure/docker-compose.yaml)):

```yaml
services:
  redis:
    image: redis
    ports:
      - '6379:6379'
```

Then point a test at the real `RedisConnectionFactory` instead of a mock:

```csharp
services.AddScoped<IRedisConnectionFactory, RedisConnectionFactory>();
services.AddScoped<IProcessTimerFactory>(_ => new DebugTimerFactory());
services.AddScoped<ProductCacheService>();
```

and exercise `GetProductEntry(...)`/`LazyLoadAsync`/`WriteThroughAsync` against `localhost:6379` the
same way the handler does. Run `docker compose up -d redis` before the test run (locally or as a
CI step) and tear it down afterward; there's no Testcontainers dependency in Benzene's own test
suite for this today, so a plain Compose file (or your own Testcontainers setup, if you already use
one elsewhere) is the way to go.

## Troubleshooting

**DI fails to resolve my cache service with a missing-service error for `IProcessTimerFactory`.**
You skipped [Installation](#installation)'s timer factory registration. Add
`services.UsingBenzene(x => x.AddDiagnostics())`, or register `DebugTimerFactory` (or another
`IProcessTimerFactory`) directly.

**`GetValueAsync()`/reads always return `default`/miss, even though I know the key exists.**
Read errors are caught and logged, not thrown, so a connectivity or serialization problem silently
looks like a cache miss. Check your logs for "Error occurred when trying to read from cache" /
"Error getting value from cache" — usually a bad connection string from
`GetConfigurationOptionsAsync()`, or a Redis instance that isn't reachable from where the code is
running (e.g. a container that can't resolve `localhost`).

**Stale data after an update.** Confirm the write path actually goes through `WriteThroughAsync`
(or an explicit `InvalidateAsync()`/`SetValueAsync()`) — if something else writes to the same
underlying store without going through `ProductCacheService`, the cache has no way to know the
value changed. Also double-check the key: `GetProductEntry` builds the key from `productId`, so a
write and a read against two different derived keys (e.g. a typo in the prefix) will never see each
other.

**`InvalidateAsync()` returns `false` even though I expect the key to exist.** For a single-key
`ICacheEntry<T>`, `RedisCacheEntry<T>.InvalidateEntryAsync` returns whatever
`IDatabase.KeyDeleteAsync` reports — `false` if the key was already gone. For prefix/wildcard
invalidation, `RedisWildcardActions` only returns `true` if at least one key was actually deleted,
and swallows connection errors (logged as a warning) rather than throwing.

## Variations

### Multiple keys for one value

If the same product needs to be looked up by both its ID and an external SKU, use
`CreateMultiKeyActions` instead of `CreateCacheEntry` so a single `SetValueAsync`/`InvalidateAsync`
call updates every key:

```csharp
public ICacheWriteActions<Product> GetProductMultiKeyActions(Guid productId, string sku) =>
    CreateMultiKeyActions<Product>(new[] { $"product:{productId}", $"product:sku:{sku}" });
```

### Bulk invalidation

To invalidate every cached product at once (e.g. after a bulk import), use `CreatePrefixActions`:

```csharp
public ICacheInvalidateActions GetAllProductsInvalidation() => CreatePrefixActions("product:");
```

This runs a Redis `KEYS product:*` scan — an O(N) operation over the whole keyspace — so avoid
calling it often against a large, busy production Redis instance. See
[Caching: Advanced Usage](../caching.md#advanced-usage) for the full details and a
custom-cache-action-mapping example.

### Health checks

Wire `AddCacheHealthCheck<ProductCacheService>()` into a `UseHealthCheck(...)` endpoint so a
monitoring system can confirm Redis connectivity without hitting a real product handler — see
[Caching: Health Check](../caching.md#health-check) and [Health Checks](../health-checks.md).

## Further Reading

- [Caching](../caching.md) — the full reference for `Benzene.Cache.Core`/`Benzene.Cache.Redis`,
  including every member of `RedisCacheService`/`ICacheEntry<T>` and the write-through/invalidate
  semantics used above
- [Monitoring & Diagnostics](../monitoring.md) — `IProcessTimerFactory`/`AddDiagnostics()` in
  depth
- [Health Checks](../health-checks.md) — wiring `AddCacheHealthCheck<TCacheService>()` into a
  pipeline
- [Message Handlers](../message-handlers.md) — `IBenzeneResult<T>`/`BenzeneResultStatus`
- [Testing Benzene](../testing-benzene.md) — `BenzeneTestHost` for exercising the handlers above
  end to end
