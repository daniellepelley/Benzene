# Monitoring & Diagnostics

Benzene includes built-in support for common monitoring and diagnostic patterns, ensuring your services are observable and easy to debug. Detailed information on core middleware can be found in the [Common Middleware](common-middleware) section.

## Correlation IDs

Correlation IDs allow you to trace a single request as it moves through various components of your system.

### Usage

To enable correlation IDs in your pipeline, use the `UseCorrelationId()` middleware.

```csharp
app.UseCorrelationId();
```

By default, this will:
1. Look for a correlation ID in the incoming message headers (e.g., `x-correlation-id`, `correlation-id`).
2. If found, it will register an `ICorrelationId` with that value; if not, it will generate a new one (GUID).
3. Make `ICorrelationId` resolvable via dependency injection for the rest of the request.

To also add the correlation ID to structured logs, see [Common Middleware](common-middleware) — `.UseLogResult(x => x.WithCorrelationId())`.

## Timers

Timers allow you to measure the duration of different parts of your pipeline.

### Usage

```csharp
app.UseTimer("my-application");
```

This will log the duration of the pipeline execution under the specified name.

## Logging

Benzene logs through [`Microsoft.Extensions.Logging`](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging) (`ILogger<T>`). There is no Benzene-specific logger to configure: whatever logging providers your host sets up (console, Serilog, log4net, Application Insights, ...) automatically receive Benzene's framework logs and your handlers' logs alike.

`UsingBenzene(...)` calls `services.AddLogging()` for you, so `ILogger<T>` always resolves — with no providers configured, log calls are simply no-ops. Configure providers the standard .NET way:

```csharp
services.AddLogging(x => x.AddConsole());
// or with Serilog's Microsoft.Extensions.Logging provider:
services.AddLogging(x => x.AddSerilog());
```

Your message handlers just take a logger via constructor injection:

```csharp
public class CreateOrderMessageHandler : IMessageHandler<CreateOrderMessage, OrderDto>
{
    private readonly ILogger<CreateOrderMessageHandler> _logger;

    public CreateOrderMessageHandler(ILogger<CreateOrderMessageHandler> logger)
    {
        _logger = logger;
    }
    // ...
}
```

### Structured log scopes

The pipeline middleware `.UseLogResult(...)` / `.UseLogContext(...)` attach structured properties (correlation ID, topic, transport, AWS request ID, ...) to the logging scope for the duration of the request, using `ILogger.BeginScope`:

```csharp
app.UseLogResult(x => x
    .WithCorrelationId()
    .WithTopic()
    .WithTransport());
```

Scope properties flow to any provider that supports scopes — for the console provider enable `IncludeScopes`; Serilog's provider maps scopes to its `LogContext` automatically.

### Autofac

The Autofac integration registers fallbacks so `ILogger<T>` always resolves. To enable real logging, register a logger factory — your registration always wins over the fallback:

```csharp
containerBuilder.RegisterInstance(LoggerFactory.Create(x => x.AddConsole()))
    .As<ILoggerFactory>();
```

> Note: if you construct Benzene from an already-built `IServiceProvider`, Benzene cannot add logging defaults — configure `AddLogging()` on the host yourself (ASP.NET Core and the generic host always do).

### Datadog Integration

Benzene provides a `Benzene.Datadog` package for easier integration with Datadog monitoring:

```csharp
services.UsingBenzene(x => x.AddDatadog());
```

## Distributed Tracing

### AWS X-Ray

For AWS Lambda environments, you can use the `Benzene.Aws.XRay` package to integrate with AWS X-Ray for distributed tracing.

### OpenTelemetry

Benzene provides a `Benzene.OpenTelemetry` package for standardized distributed tracing and metrics:

```csharp
services.UsingBenzene(x => x.AddOpenTelemetry());
```
