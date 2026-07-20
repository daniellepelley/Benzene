# Health Checks

Health checks let a service report whether it — and the things it depends on (a database, a
downstream HTTP API, a queue) — are in a state where it can do its job.

## Overview

A service built on Benzene almost always depends on other resources: a database, a storage
location, or another service. Health checks let you verify that a service has access to everything
it needs and is in the right state to operate.

In Benzene, a health check isn't a special HTTP-only concept bolted onto the framework — it's just
another topic in the middleware pipeline. `.UseHealthCheck(topic, ...)` adds a middleware that
intercepts messages for that topic (and the default `"healthcheck"` topic), runs your registered
`IHealthCheck`s, and returns an aggregated `IHealthCheckResponse`. Because it's ordinary
middleware, it works identically across every transport Benzene supports — AWS Lambda (API Gateway,
SNS, SQS, Kafka), Azure Functions, ASP.NET Core, or a self-hosted worker — with no special endpoint
plumbing beyond the same fluent `.Use*()` calls you use everywhere else.

## Installation

| Package | What it adds |
| --- | --- |
| `Benzene.HealthChecks.Core` | The abstractions: `IHealthCheck`, `IHealthCheckResult`, `IHealthCheckResponse<T>`, `HealthCheckStatus`, `IHealthCheckBuilder`, `IHealthCheckFactory`. Pulled in transitively by the packages below. |
| `Benzene.HealthChecks` | The processor, `HealthCheckBuilder`, the `.UseHealthCheck(...)` middleware, and the built-in `SimpleHealthCheck`/`InlineHealthCheck`/`FailedHealthCheck` checks. |
| `Benzene.HealthChecks.Http` | `HttpPingHealthCheck` — pings a downstream HTTP URL. |
| `Benzene.HealthChecks.EntityFramework` | `DatabaseConnectionHealthCheck<TDbContext>` and `DatabaseHealthCheck<TDbContext>` for EF Core. |

Add `Benzene.HealthChecks` to any project that wires up a pipeline; add `Benzene.HealthChecks.Http`
and/or `Benzene.HealthChecks.EntityFramework` only where you need those specific checks.

## Basic usage

```csharp
using Benzene.HealthChecks;

app.UseHealthCheck("healthcheck", x => x
    .AddHealthCheck<SimpleHealthCheck>()
    .AddHealthCheck(resolver => resolver.GetService<SimpleHealthCheck>())
    .AddHealthCheck("inline", _ => true)
    .AddHealthCheck("inline", async _ => await Task.FromResult(true))
    .AddHealthCheck(_ => true));
```

Send it a health check request the same way you'd send any other message — for a `BenzeneMessage`-based
transport (SNS/SQS/Kafka), that's a message with the matching topic:

```json
{
  "topic": "healthcheck"
}
```

The response is a JSON payload describing whether the service is healthy overall and the result of
each individual check (see [Response format](#response-format) below).

## Core concepts

### `IHealthCheck`

```csharp
public interface IHealthCheck
{
    string Type { get; }
    Task<IHealthCheckResult> ExecuteAsync();
}
```

`Type` is the name used to identify the check in the response — it's the value used as (or to
derive) the check's key in the response dictionary. Everything else about the interface is
intentionally minimal: no context object is passed in, so a check gets whatever it needs (a
`DbContext`, an `HttpClient`, ...) via constructor injection instead.

### `IHealthCheckResult` / `HealthCheckResult`

```csharp
public interface IHealthCheckResult
{
    string Status { get; }
    string Type { get; }
    IDictionary<string, object> Data { get; }
}
```

`Data` is a free-form metadata bag — put whatever's useful for diagnosing a failure in there (e.g.
`"CanConnect"`, `"Url"`, `"StatusCode"`). `HealthCheckResult` implements the interface and exposes
static factory helpers instead of a public constructor for the common cases:

```csharp
HealthCheckResult.CreateInstance(bool success);                                   // Type = "Unknown", empty Data
HealthCheckResult.CreateInstance(bool success, string type);
HealthCheckResult.CreateInstance(bool success, string type, IDictionary<string, object> data);
HealthCheckResult.CreateInstance(Task<bool> success, string type);                // async overload, returns Task<IHealthCheckResult>
HealthCheckResult.CreateWarning(string type);
HealthCheckResult.CreateWarning(string type, IDictionary<string, object> data);
```

`CreateInstance` maps `success` to `HealthCheckStatus.Ok` or `HealthCheckStatus.Failed`;
`CreateWarning` always produces `HealthCheckStatus.Warning`.

### `HealthCheckStatus`

Three string constants, used as the `Status` value:

| Constant | Value | Effect on `IsHealthy` |
| --- | --- | --- |
| `HealthCheckStatus.Ok` | `"ok"` | Healthy |
| `HealthCheckStatus.Warning` | `"warning"` | Still counted as healthy |
| `HealthCheckStatus.Failed` | `"failed"` | Flips the aggregate response to unhealthy |

Only `Failed` affects the aggregate result — a check that reports `Warning` shows up in the
response so you can see it, but doesn't take the whole service down.

### `IHealthCheckResponse<T>` / `HealthCheckResponse`

```csharp
public interface IHealthCheckResponse<THealthCheckResult> where THealthCheckResult : IHealthCheckResult
{
    bool IsHealthy { get; }
    IDictionary<string, THealthCheckResult> HealthChecks { get; }
}
```

This is the payload returned by `.UseHealthCheck(...)` — `IsHealthy` is `true` only if every
registered check's `Status` is not `Failed`; `HealthChecks` maps a (de-duplicated, see
[naming](#result-naming-and-deduplication) below) name to each check's result.

## Registering health checks: `IHealthCheckBuilder`

```csharp
public interface IHealthCheckBuilder
{
    IHealthCheckBuilder AddHealthCheck<THealthCheck>() where THealthCheck : class, IHealthCheck;
    IHealthCheckBuilder AddHealthCheck(Func<IServiceResolver, IHealthCheck> func);
    IHealthCheck[] GetHealthChecks(IServiceResolver resolver);
}
```

There are two fundamentally different ways a check gets registered, and it matters which one you
pick:

- **`AddHealthCheck<THealthCheck>()`** registers `THealthCheck` as a **scoped** `IHealthCheck` in
  the DI container, and is discovered at request time via `IHealthCheckFinder`
  (`HealthCheckFinder`, which resolves `IEnumerable<IHealthCheck>`). Because of that, it also picks
  up *any* `IHealthCheck` registered directly in DI by other code — not just the ones added through
  this specific `.UseHealthCheck()` call.
- Every other `AddHealthCheck(...)` overload (instance, factory function, inline delegate) is scoped
  to *this* builder only — it's stored in a private list and turned into an `IHealthCheck` when
  `GetHealthChecks(resolver)` runs, without touching DI at all.

`Benzene.HealthChecks.Core` adds these builder extensions on top of the two core methods:

```csharp
builder.AddHealthCheck(IHealthCheck healthCheck);                      // wraps an existing instance
builder.AddHealthChecks(params IHealthCheck[] healthChecks);           // several existing instances at once
builder.AddHealthCheckFactory(IHealthCheckFactory healthCheckFactory); // e.g. HttpPingHealthCheckFactory, DatabaseHealthCheckFactory<T>
```

`Benzene.HealthChecks` adds inline overloads for one-off checks that don't need their own class,
covering sync/async and `bool`/`IHealthCheckResult` return types, with or without an explicit
`Type`:

```csharp
builder.AddHealthCheck("my-check", resolver => true);
builder.AddHealthCheck("my-check", async resolver => await SomeAsyncCheckAsync());
builder.AddHealthCheck("my-check", resolver => HealthCheckResult.CreateWarning("my-check"));
builder.AddHealthCheck(resolver => true);                              // Type defaults to "inline"
```

All of the inline overloads build an `InlineHealthCheck` under the hood — you rarely construct
`InlineHealthCheck` yourself.

## Built-in health checks

### `SimpleHealthCheck` (`Benzene.HealthChecks`)

Always succeeds. `Type` is `"Simple"`. Useful as a smoke test or a placeholder while you build out
real checks.

### `InlineHealthCheck` (`Benzene.HealthChecks`)

Wraps a `Func<Task<IHealthCheckResult>>` with an optional explicit `Type` (defaults to
`string.Empty`, which the response namer then treats as `"HealthCheck"` — see
[naming](#result-naming-and-deduplication)). This is what every inline `AddHealthCheck(...)`
overload above constructs for you.

### `FailedHealthCheck` (`Benzene.HealthChecks`)

Wraps an `Exception`. `Type` is `"Failed"`; `ExecuteAsync()` always returns a `Failed` result with
the exception's message in `Data["Exception"]`. `Extensions.BuildHealthCheck(Func<IHealthCheck>)`
uses this to turn a check *construction* failure (e.g. a factory that throws) into a reportable
result instead of an unhandled exception.

### `TimeOutHealthCheck` / `ExceptionHandlingHealthCheck` (internal safety net)

These two are `internal` — you never construct them yourself, but it's worth knowing they run
around *every* check automatically. `HealthCheckProcessor.PerformHealthChecksAsync` wraps each
`IHealthCheck` in `ExceptionHandlingHealthCheck` (catches any exception thrown by `ExecuteAsync()`
and turns it into a `Failed` result with `Data["Exception"]`) and then `TimeOutHealthCheck` (a
hard-coded 10 second timeout — if the check hasn't completed by then, it returns a `Failed` result
with `Data["Error"] = "Timed Out"` instead of waiting indefinitely). Neither timeout nor exception
handling is currently configurable; every check gets the same 10 second budget and the same
catch-all.

### `HttpPingHealthCheck` (`Benzene.HealthChecks.Http`)

`GET`s a URL and reports healthy only on a `200 OK` response (any other status code, including other
2xx codes, is `Failed`). `Type` is `"HttpPing"`; `Data` includes `Url` and `StatusCode`.

```csharp
using Benzene.HealthChecks.Http;

app.UseHealthCheck("healthcheck", x => x
    .AddHttpPing("https://downstream-service/health"));
```

`AddHttpPing(url)` registers an `HttpPingHealthCheckFactory`, which resolves a plain `HttpClient`
from the resolver at check time — make sure one is registered (e.g. via
`services.AddHttpClient()`/`IHttpClientFactory`, or a manual `HttpClient` registration).

### Entity Framework Core health checks (`Benzene.HealthChecks.EntityFramework`)

Two checks, both generic over your `DbContext` type and resolving it via constructor injection —
neither has a dedicated `.AddHealthCheck<T>()`-style extension method, so register them either as a
DI-resolved type or via their factory:

- **`DatabaseConnectionHealthCheck<TDbContext>`** — `Type` is `"DatabaseConnection"`. Just calls
  `dbContext.Database.CanConnectAsync()`; `Data["CanConnect"]` reports the result (and
  `Data["Error"]` the exception message, if connecting threw).

  ```csharp
  app.UseHealthCheck("healthcheck", x => x
      .AddHealthCheck<DatabaseConnectionHealthCheck<MyDbContext>>());
  ```

- **`DatabaseHealthCheck<TDbContext>`** — `Type` is `"Database"`. Checks connectivity *and*
  compares applied migrations against a target migration name you supply. It's only healthy if
  `CanConnectAsync()` succeeds **and** the *last* applied migration exactly matches
  `targetMigration` (`MigrationMatch`). `Data` also reports the full `AppliedMigrations` list and
  `MigrationContains` (whether the target appears anywhere in the list, not necessarily last) —
  useful if you want looser matching in your own aggregation. Register it via
  `DatabaseHealthCheckFactory<TDbContext>`, since it needs the target migration name at
  construction time:

  ```csharp
  using Benzene.HealthChecks.EntityFramework;

  app.UseHealthCheck("healthcheck", x => x
      .AddHealthCheckFactory(new DatabaseHealthCheckFactory<MyDbContext>("20220809094008_V8")));
  ```

## Wiring into the pipeline: `.UseHealthCheck()`

### Message-topic based (every transport)

Defined in `Benzene.HealthChecks.Extensions`, generic over `TContext`, so it works on any pipeline
(`BenzeneMessage`, SNS, SQS, Kafka, Azure Functions, ASP.NET Core, self-hosted workers):

```csharp
app.UseHealthCheck(topic, params IHealthCheck[] healthChecks);
app.UseHealthCheck(topic, Action<IHealthCheckBuilder> action);
app.UseHealthCheck(topic, IHealthCheckBuilder builder);
```

The middleware checks the incoming message's topic against **both** the `topic` you passed in and
the constant default topic `"healthcheck"` (`Constants.DefaultHealthCheckTopic`) — so even if you
wire it up under a custom topic like `"orders-service:healthcheck"`, it's still reachable via the
plain `"healthcheck"` topic too. If neither matches, the middleware calls `next()` and gets out of
the way.

```csharp
app.UseHealthCheck("orders-service:healthcheck", x => x
    .AddHealthCheck<SimpleHealthCheck>()
    .AddHealthCheckFactory(new DatabaseHealthCheckFactory<OrdersDbContext>("20220809094008_V8")));
```

### HTTP method + path based (raw HTTP transports)

Two packages that deal with raw HTTP requests directly — `Benzene.SelfHost.Http` and
`Benzene.Aws.Lambda.ApiGateway` — additionally expose `UseHealthCheck` overloads that match on HTTP
verb and path instead of (or in addition to) a message topic, since those pipelines see the raw
request before any topic has been resolved from it:

```csharp
// Benzene.SelfHost.Http / Benzene.Aws.Lambda.ApiGateway
app.UseHealthCheck(method, path, params IHealthCheck[] healthChecks);
app.UseHealthCheck(topic, method, path, params IHealthCheck[] healthChecks);
app.UseHealthCheck(method, path, Action<IHealthCheckBuilder> action);
app.UseHealthCheck(topic, method, path, Action<IHealthCheckBuilder> action);
app.UseHealthCheck(topic, method, path, IHealthCheckBuilder builder);
```

```csharp
apiGatewayApp.UseHealthCheck("healthcheck", "POST", "/healthcheck", healthChecks);
```

These match when the request's HTTP method and path equal `method`/`path` exactly (method comparison
is case-insensitive); when they don't match, the middleware falls through to `next()` the same way
the topic-based version does. ASP.NET Core doesn't currently have an equivalent method+path overload
— use the topic-based `.UseHealthCheck(topic, ...)` there (an ASP.NET Core request's topic is
already resolved via routing before it reaches this middleware, so there's no raw path left to match
on at this point in the pipeline).

### Kubernetes-style liveness/readiness: `.UseLivenessCheck()` / `.UseReadinessCheck()`

`.UseHealthCheck()` runs one undifferentiated set of checks. For Kubernetes (or any platform with a
liveness-vs-readiness distinction), `Benzene.HealthChecks` also provides two purpose-built
convenience methods — see [Kubernetes Health Checks](kubernetes-health-checks.md) for the full guide,
including which checks belong in which and example probe YAML. Short version:

```csharp
// Benzene.HealthChecks — topic-based, every transport
app.UseLivenessCheck(x => x.AddHealthCheck<ProcessResponsiveCheck>());
app.UseReadinessCheck(x => x.AddHealthCheckFactory(new DatabaseHealthCheckFactory<MyDbContext>("...")));
```

Unlike `.UseHealthCheck()`, these do **not** also respond to `Constants.DefaultHealthCheckTopic` —
each only matches its own topic (`Constants.DefaultLivenessTopic`/`DefaultReadinessTopic`), so
registering both in the same pipeline doesn't have one silently shadow the other on a shared fallback
topic.

`Benzene.SelfHost.Http` and `Benzene.Aws.Lambda.ApiGateway` additionally expose HTTP-path versions,
defaulting to the conventional Kubernetes probe paths:

```csharp
// Benzene.SelfHost.Http / Benzene.Aws.Lambda.ApiGateway
app.UseLivenessCheck(checks);   // GET /livez
app.UseReadinessCheck(checks);  // GET /readyz
app.UseLivenessCheck("/custom/live/path", checks);  // path override
```

Both variants (topic-based and HTTP-path-based) run through the same `HealthCheckProcessor`, so an
unhealthy result maps to HTTP 503 the same way `.UseHealthCheck()`'s does — see
[HTTP status codes](#http-status-codes) below.

### gRPC (grpc.health.v1)

Package: `Benzene.Grpc.AspNet`. This one is different in kind from everything above: instead of
routing a message topic or HTTP path through `.UseHealthCheck()`, it bridges Benzene's health checks
onto the *standard* [grpc.health.v1](https://github.com/grpc/grpc/blob/master/doc/health-checking.md)
protocol, so any generic gRPC health-checking tool (`grpc_health_probe`, Kubernetes gRPC liveness
probes, `grpcurl`) can query it without knowing anything about Benzene:

```csharp
services.AddBenzeneGrpc(o => o.EnableHealthChecks = true);
services.AddScoped<IHealthCheck, DatabaseHealthCheck>();   // Benzene.HealthChecks.Core.IHealthCheck

app.MapGrpcService<GreeterService>();
app.MapBenzeneGrpcHealthService();
```

`BenzeneHealthCheckBridge` is an ASP.NET Core `Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck`
(not the same interface as above — note the different namespace) registered as the `"benzene"` check;
it resolves every `Benzene.HealthChecks.Core.IHealthCheck` from the container directly (plain
`services.AddScoped<IHealthCheck, T>()`, no `IHealthCheckBuilder` involved) and aggregates them:
unhealthy if any failed, degraded if any warned, healthy otherwise. A gRPC `Check`/`Watch` call then
reflects that aggregate as `SERVING`/`NOT_SERVING` per the standard protocol.

To split liveness from readiness over gRPC (so a liveness probe doesn't run your external-dependency
checks), set `LivenessCheckTypes`/`ReadinessCheckTypes` on `BenzeneGrpcOptions` — Benzene then publishes
named `"liveness"`/`"readiness"` grpc.health.v1 services reporting only those check `Type`s, alongside
the overall `""` service. See [Kubernetes Health Checks](kubernetes-health-checks.md#grpc-livenessreadiness-split).

Both health checks and reflection are off by default (`EnableHealthChecks`/`EnableReflection` on
`BenzeneGrpcOptions`) — see [gRPC Setup](getting-started-grpc.md#10-health-checks-and-reflection-d8)
for the full walkthrough.

## Response format

The response payload is a `HealthCheckResponse` (`IHealthCheckResponse<HealthCheckResult>`),
serialized through whatever `ISerializer` your app uses. By default that's a camelCase naming
policy, which applies to the actual C# properties on `HealthCheckResponse`/`HealthCheckResult`
(`IsHealthy`, `HealthChecks`, `Status`, `Type`, `Data`) but *not* to the contents of the `Data`
dictionary itself, since a naming policy only rewrites declared properties, not arbitrary
`IDictionary<string, object>` keys. For example:

```json
{
  "isHealthy": true,
  "healthChecks": {
    "Simple": { "status": "ok", "type": "Simple", "data": {} },
    "Database": {
      "status": "ok",
      "type": "Database",
      "data": {
        "CanConnect": true,
        "AppliedMigrations": ["20220101000000_Init", "20220809094008_V8"],
        "TargetMigration": "20220809094008_V8",
        "MigrationMatch": true,
        "MigrationContains": true,
        "Error": null
      }
    }
  }
}
```

That's why `data`'s inner keys stay `PascalCase` (`CanConnect`, `AppliedMigrations`, ...) exactly as
the built-in checks set them, while everything around them follows your serializer's naming policy.
This example assumes the default `System.Text.Json`-based `ISerializer` (`Benzene.Core.MessageHandlers`),
whose `PropertyNamingPolicy` doesn't touch dictionary contents; if your app instead wires up
`Benzene.NewtonsoftJson`'s serializer, check its `CamelCasePropertyNamesContractResolver`
configuration — Newtonsoft's camel-casing can be configured to rewrite dictionary keys too, which
would camelCase `Data`'s contents as well.

If a check's `status` is `ok`/`warning`, `isHealthy` stays `true`; a single `failed` check flips the
whole response to `false` even if every other check succeeded.

### HTTP status codes

Over an HTTP transport, the response's HTTP status code reflects `isHealthy`, not just its body:
`200` when healthy, `503 Service Unavailable` when not (mapped by `DefaultHttpStatusCodeMapper`,
same as everywhere else in Benzene). This matters because most health-check consumers — a
Kubernetes HTTP probe, a load balancer target-health check — only look at the status code, not the
response body, so a check whose body says `"isHealthy": false` but whose status code is still `200`
would be silently treated as healthy by those consumers. `HealthCheckProcessor.PerformHealthChecksAsync`
handles this for every `.UseHealthCheck()`/`.UseLivenessCheck()`/`.UseReadinessCheck()` variant, over
every transport, with no extra configuration needed.

`Data["Error"]`/`Data["Exception"]` entries (populated when a check fails with an exception, or
times out) report the exception's *type name* (e.g. `"SqlException"`), not its message — some
ADO.NET providers embed connection details in exception messages, and this response can flow out to
whatever calls the health check with no built-in authorization. See
[Privacy & Data Handling](privacy-and-data-handling.md) for the full reasoning.

### Result naming and deduplication

Each entry's key in `healthChecks` comes from `HealthCheckNamer`, run against the `Type` on the
*executed* `HealthCheckResult` — not necessarily the `IHealthCheck` instance's own `Type` property:

- On success, that's whatever `Type` the check's `ExecuteAsync()` actually put on the result it
  returned. For well-behaved checks (all the built-in ones) that matches the check's own `Type`
  property, but it isn't required to — e.g. the untyped `AddHealthCheck(resolver => true)` /
  `AddHealthCheck(resolver => Task<bool>)` overloads hardcode the executed result's `Type` to
  `"inline"` even though the underlying `InlineHealthCheck` instance's own `Type` is empty, while
  `AddHealthCheck(resolver => IHealthCheckResult)` defers entirely to whatever `Type` your returned
  `IHealthCheckResult` carries (e.g. bare `HealthCheckResult.CreateInstance(success)` defaults to
  `"Unknown"`).
- On exception or timeout, the internal `ExceptionHandlingHealthCheck`/`TimeOutHealthCheck` wrappers
  build their fallback result using the wrapped check's own `Type` property (`_inner.Type`) — so a
  named class-based check keeps its name even when it fails or times out.
- An empty/`null` `Type` on the executed result is named `"HealthCheck"`.
- If a name has already been used, `HealthCheckNamer` appends `-2`, `-3`, ... to keep keys unique
  rather than one check's result silently overwriting another's — e.g. two `SimpleHealthCheck`s in
  the same call show up as `"Simple"` and `"Simple-2"`.

## Example: a custom health check class

The cleanest way to add a non-trivial check is as its own class, which is also the shape
`AddHealthCheck<T>()`/DI resolution expects:

```csharp
using Benzene.HealthChecks.Core;
using Microsoft.EntityFrameworkCore;

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

(This is, in fact, exactly what `Benzene.HealthChecks.EntityFramework`'s own
`DatabaseConnectionHealthCheck<TDbContext>` looks like — you only need to write your own version of
this if you want different `Data`/behavior than the shipped one.)

## Troubleshooting

- **A check never seems to time out, it just reports `Failed` after ~10 seconds.** That's
  `TimeOutHealthCheck` — every check gets a hard-coded 10 second budget with no override today. If a
  check is slow by design, make sure its own internal timeout (e.g. an `HttpClient.Timeout`, a DB
  command timeout) is comfortably shorter than 10 seconds so you get a meaningful `Data["Error"]`
  from the check itself rather than the generic `"Timed Out"`.
- **Two checks with the same `Type` and I can't tell them apart in the response.** See
  [naming](#result-naming-and-deduplication) — look for the `-2`/`-3` suffix `HealthCheckNamer`
  appends, or give each check a distinct `Type`.
- **My health check topic doesn't seem reachable from the outside.** Remember `.UseHealthCheck()`
  also always responds to the plain `"healthcheck"` topic in addition to whatever topic you passed
  in, and make sure the middleware is registered before anything upstream of it would otherwise
  swallow that topic.
- **`AddHealthCheck<T>()` doesn't pick up my check.** Confirm `T` is registered (or resolvable) as
  scoped in the same DI container the pipeline is built from — `AddHealthCheck<T>()` only adds a DI
  registration; it doesn't create the instance itself.

## See Also

- [Common Middleware](common-middleware.md) — `.UseHealthCheck()` alongside the other pipeline
  middleware
- [Middleware](middleware.md) — how middleware ordering and inline middleware work in general
- [Monitoring & Diagnostics](monitoring.md) — tracing, logging, and metrics for the rest of your pipeline
- [gRPC Setup](getting-started-grpc.md) — the grpc.health.v1 bridge
