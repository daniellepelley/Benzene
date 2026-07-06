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
1. Look for a correlation ID in the incoming message headers (e.g., `CorrelationId`, `x-correlation-id`).
2. If found, it will set it in the `IBenzeneContext`.
3. If not found, it will generate a new one (GUID).
4. Ensure the correlation ID is included in logs and outgoing requests.

## Timers

Timers allow you to measure the duration of different parts of your pipeline.

### Usage

```csharp
app.UseTimer("my-application");
```

This will log the duration of the pipeline execution under the specified name.

## Logging

Benzene integrates with common logging frameworks.

### Serilog Integration

To use Serilog, add the `Benzene.Serilog` package and use the middleware:

```csharp
app.UseSerilog();
```

### Datadog Integration

Benzene provides a `Benzene.Datadog` package for easier integration with Datadog monitoring.

## Distributed Tracing

### AWS X-Ray

For AWS Lambda environments, you can use the `Benzene.Aws.XRay` package to integrate with AWS X-Ray for distributed tracing.

### OpenTelemetry

Benzene has emerging support for OpenTelemetry to provide standardized distributed tracing and metrics.

```csharp
// Example (refer to Benzene.OpenTelemetry for latest usage)
app.UseOpenTelemetry();
```
