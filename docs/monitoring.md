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

Benzene integrates with common logging frameworks. Each integration package registers its own `IBenzeneLogContext`/`IBenzeneLogAppender` implementation; the actual logging middleware in your pipeline is still the framework-agnostic `.UseLogResult()` / `.UseLogContext()` from [Common Middleware](common-middleware).

### Serilog Integration

To use Serilog, add the `Benzene.Serilog` package and register it with your service container:

```csharp
services.UsingBenzene(x => x.AddSerilog());
```

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
