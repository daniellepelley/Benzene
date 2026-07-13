# Rich Structured Logging with Serilog

Wire Serilog into a Benzene service and see Benzene's built-in scope enrichment (correlation ID, topic, transport, ...) show up as structured properties in a JSON sink.

## Problem Statement

You want Serilog's structured sinks (JSON console output, Seq, Elasticsearch, ...) instead of the plain-text console logger, and you want the request-scoped properties Benzene already attaches — correlation ID, topic, transport, processing time — to appear as first-class fields on every log event, not just interpolated into a message string.

**Before you start installing packages, the important thing to know is what Benzene *doesn't* provide here:** there is no `Benzene.Serilog` NuGet package. One existed at an earlier point in this project's history (`SerilogBenzeneLogAppender`, `SerilogBenzeneLogContext`), but it was deleted when Benzene moved its entire logging surface onto plain `Microsoft.Extensions.Logging` (`ILogger<T>`/`ILoggerFactory`) — the commit that removed it notes the old Serilog registration was "broken and unused." Nothing in current `src/` references a Benzene-specific Serilog package, and it isn't in `Benzene.sln`.

That turns out not to matter: Benzene logs exclusively through `ILogger<T>`, and its scope-enrichment middleware (`UseLogResult`/`UseLogContext`/`UseBenzeneEnrichment`) attaches properties via the standard `ILogger.BeginScope`. Serilog's own `Microsoft.Extensions.Logging` provider (`Serilog.Extensions.Logging`) maps `BeginScope` state onto its `LogContext` automatically — no Benzene-specific glue required. This cookbook is entirely "plug the standard Serilog provider into the standard `AddLogging` call" — there's no Benzene-side extension point beyond that.

## Prerequisites

- A Benzene service (AWS Lambda, Azure Functions, ASP.NET Core, or a plain worker) already using `services.UsingBenzene(...)`
- Familiarity with [Monitoring & Diagnostics — Logging](../monitoring.md#logging), which documents the `ILogger`/scope model this cookbook builds on

## Installation

```bash
dotnet add package Serilog
dotnet add package Serilog.Extensions.Logging
dotnet add package Serilog.Sinks.Console
```

`Serilog.Extensions.Logging` is what provides `AddSerilog()` on `ILoggingBuilder` — that's the entire integration surface between Serilog and Benzene. Add `Serilog.Sinks.Seq` instead of (or alongside) `Serilog.Sinks.Console` if you're shipping to Seq rather than stdout.

## Step-by-Step Implementation

### 1. Configure Serilog's static logger

Do this once at startup, before `ConfigureServices` builds the container. `Enrich.FromLogContext()` is the piece that makes Benzene's `BeginScope` properties show up — without it, Serilog ignores the ambient `LogContext` entirely and your correlation ID/topic/transport properties will be silently dropped.

```csharp
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger();
```

`JsonFormatter` ships in the base `Serilog` package (`Serilog.Formatting.Json`), so this needs no extra sink beyond `Serilog.Sinks.Console` for the console writer itself. For Seq, swap the sink:

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();
```

(This is the same shape used in this repo's own AWS example, `examples/Aws/Benzene.Examples.Aws/Logging/Extensions.cs` — it configures `Log.Logger` with `Enrich.FromLogContext()` and a JSON-formatting console sink, just with a hand-rolled `ITextFormatter` instead of Serilog's built-in `JsonFormatter`.)

### 2. Bridge Serilog into `Microsoft.Extensions.Logging`

This is the one line that actually connects Serilog to Benzene — Benzene only ever calls `ILogger<T>`, so whatever provider `AddLogging` wires up is what receives Benzene's logs:

```csharp
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddLogging(x => x.AddSerilog());

    services.UsingBenzene(x => x
        .AddBenzene()
        .AddMessageHandlers(typeof(CreateOrderMessage).Assembly));
}
```

This matches [Monitoring — Logging](../monitoring.md#logging) exactly: `UsingBenzene(...)` already calls `services.AddLogging()` for you, so `ILogger<T>` always resolves; `AddSerilog()` is just the provider you plug into that call. No Benzene-specific registration exists or is needed.

### 3. Turn on Benzene's scope enrichment

Add `UseLogResult(...)` (or `UseLogContext(...)` if you don't want the extra `"BenzeneResult"` summary line) early in the pipeline you want enriched:

```csharp
app.UseApiGateway(apiGatewayApp => apiGatewayApp
    .UseCorrelationId()
    .UseLogResult(x => x
        .WithCorrelationId()
        .WithTopic()
        .WithTransport())
    .UseMessageHandlers());
```

- `WithCorrelationId()` (`Benzene.Diagnostics`, needs `.UseCorrelationId()` earlier in the pipeline to populate `ICorrelationId`) — adds `correlationId`
- `WithTopic()` (`Benzene.Core.MessageHandlers`) — adds `topic` (`"<missing>"` if unresolvable)
- `WithTransport()` (`Benzene.Core.MessageHandlers`) — adds `transport`, the current `ICurrentTransport.Name`

Or, for the portable one-call version that also adds `invocationId`/`traceId`/`spanId`/`handler` on every platform:

```csharp
app.UseBenzeneEnrichment();
```

Both attach their properties via `ILogger.BeginScope(...)`, which is exactly what Serilog's provider watches for.

### 4. See it in the sink

With the wiring above, a request produces a `BenzeneResult` summary line (from `UseLogResult`) plus every other log statement made during that request, all carrying the same scope properties. Console output with `JsonFormatter` looks like:

```json
{"Timestamp":"2026-07-13T09:14:02.1170000Z","Level":"Information","MessageTemplate":"BenzeneResult","Properties":{"correlationId":"5c9e2b1a-...","topic":"order:create","transport":"apiGateway","processTime":42}}
```

And a handler's own `_logger.LogInformation("Creating order for {CustomerId}", request.CustomerId);` call made inside that same request gets the identical `correlationId`/`topic`/`transport` properties attached, because it runs inside the same `BeginScope`:

```json
{"Timestamp":"2026-07-13T09:14:02.0810000Z","Level":"Information","MessageTemplate":"Creating order for {CustomerId}","Properties":{"CustomerId":"cust-123","correlationId":"5c9e2b1a-...","topic":"order:create","transport":"apiGateway"}}
```

## Testing

There's no Serilog-specific test in this repository — Serilog's own `SerilogLoggerProvider` mapping `BeginScope` state onto `LogContext` is Serilog's behavior to verify, not Benzene's. What Benzene's own test suite verifies is that `UseLogResult`/`UseLogContext` actually populate the scope dictionary correctly, using a `FakeLoggerFactory` (from `Microsoft.Extensions.Diagnostics.Testing`) instead of a real provider — see `test/Benzene.Core.Test/Core/Core/Logging/UseLogContextTest.cs`:

```csharp
[Fact]
public async Task CorrelationId_AddedToLogContextForDurationOfRequest()
{
    SetUp(x => x.WithCorrelationId());

    await _host.SendBenzeneMessageAsync(MessageBuilder.Create(Defaults.Topic, new ExampleRequestPayload()));

    Assert.Contains(_fakeLoggerFactory.Collector.ScopeDictionaries, x => x.ContainsKey("correlationId"));
}
```

To verify your own Serilog wiring end to end, run the service locally with the console sink from Step 1 and confirm the `correlationId`/`topic`/`transport` keys appear in the JSON `Properties` object for a real request — if they don't, the fix is almost always Step 1's `Enrich.FromLogContext()`.

## Troubleshooting

### Scope properties (`correlationId`, `topic`, `transport`, ...) aren't in the output

**Solution:** Confirm `.Enrich.FromLogContext()` is in your `LoggerConfiguration`. Without it, Serilog never reads the ambient `LogContext` that `BeginScope` pushes onto, so the properties are silently dropped rather than erroring.

### Logs aren't reaching Serilog at all

**Solution:** Confirm `services.AddLogging(x => x.AddSerilog());` is actually called. `UsingBenzene(...)` registers a default (no-op) `ILoggerFactory` for you if nothing else does — if `AddSerilog()` never runs, Benzene's `ILogger<T>` calls succeed but go nowhere.

### Old references to `Benzene.Serilog` / `SerilogBenzeneLogContext`

**Solution:** That package was removed. If you're looking at an older fork, an old blog post, or a stale example that references `Benzene.Serilog`, `IBenzeneLogAppender`, or `IBenzeneLogContext`, those types no longer exist — replace them with the plain `Microsoft.Extensions.Logging` + `AddSerilog()` wiring in this cookbook.

### `correlationId` is missing even though `WithCorrelationId()` is configured

**Solution:** `WithCorrelationId()` reads from `ICorrelationId`, which is only populated if `.UseCorrelationId()` (or `AddCorrelationId()`) ran earlier in the same pipeline and the incoming request actually carried an `x-correlation-id`/`correlation-id`/`correlationId` header. No header, no value — `WithCorrelationId()` won't fabricate one.

## Variations

### Per-handler `ILogger<T>` calls alongside scope enrichment

Nothing changes about how you write handler-level logging — inject `ILogger<T>` as normal ([Monitoring — Logging](../monitoring.md#logging)) and every call automatically inherits whatever scope is active:

```csharp
public class CreateOrderMessageHandler(ILogger<CreateOrderMessageHandler> logger)
    : IMessageHandler<CreateOrderMessage, OrderDto>
{
    public async Task<IBenzeneResult<OrderDto>> HandleAsync(CreateOrderMessage message)
    {
        logger.LogInformation("Creating order for {CustomerId}", message.CustomerId);
        // ...
    }
}
```

### Adding static enrichers instead of (or alongside) scopes

Serilog's own enrichers (`Enrich.WithMachineName()`, `Enrich.WithEnvironmentName()`, a custom `ILogEventEnricher`) work independently of anything Benzene does — they're configured on `LoggerConfiguration` in Step 1 and apply to every event regardless of pipeline scope.

### Application Insights instead of console/Seq

Swap the sink in Step 1 for `Serilog.Sinks.ApplicationInsights` — see [Logging to Application Insights — Using Serilog Instead](logging-application-insights.md#using-serilog-instead) for a worked example combining both.

## Further Reading

- [Monitoring & Diagnostics — Logging](../monitoring.md#logging) — the `ILogger`/scope model this cookbook is built on
- [Common Middleware — UseLogResult / UseLogContext](../common-middleware.md#uselogresult--uselogcontext) — full reference for the `.With*()` builder extensions
- [Common Middleware — UseBenzeneEnrichment](../common-middleware.md#usebenzeneenrichment) — the portable, one-call alternative to hand-composing `.With*()` calls
- [Logging to Application Insights](logging-application-insights.md) — the Application Insights equivalent of this cookbook, including a Serilog-based variation
- [Serilog documentation](https://serilog.net/) / [Serilog.Extensions.Logging](https://github.com/serilog/serilog-extensions-logging)
