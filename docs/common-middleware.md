# Common Middleware

## UseCorrelationId

This will attempt to pick up a Correlation Id from the message, usually this will be on the header as “x-correlation-id” or “correlation-id”, but this can be set in the middleware itself. It will create an instance of ICorrelationId which can then be injected into classes if you need to access the correlation id. The correlation id can be added to structured logs to help with tracing.

```csharp
.UseCorrelationId()
```

## UseTimer

This opens a named [`Activity`](monitoring#tracing) span around the rest of the pipeline. Every
middleware already gets its own `Activity` automatically via `AddDiagnostics()` — `UseTimer` is for
naming a specific stage explicitly.

```csharp
.UseTimer("benzene-message-application")
```

## UseBenzeneMetrics

This records `benzene.messages.processed` (a count) and `benzene.message.duration` (in
milliseconds) for the wrapped pipeline stage, tagged by `topic`, `transport`, and `result`
(`success`/`failure`, where the context implements `IHasMessageResult`). Unlike the automatic
per-middleware `Activity` spans from `AddDiagnostics()`, this is once-per-message granularity and
must be added explicitly around the stage you want measured. See
[OpenTelemetry](monitoring#opentelemetry) for exporting these to a real backend.

```csharp
.UseBenzeneMetrics()
```

## UseHealthCheck
This will allow healthchecks to be accessed using the topic added. By default “healthcheck” will always access the healthchecks on a service, but you might want call multiple healthchecks from outside the service so this give you the options to have a topic called something like “<service-name>:healthcheck”.

```csharp
.UseHealthCheck(healthCheckTopic, healthCheckBuilder)
```

## UseSpec
This allows you to query schemas from the service such as openapi and asyncapi. It is essential that this is added if you want to use the Command line tools to generate code.

```csharp
.UseSpec("spec")
```

## UseMessageHandlers
This is the middleware that will route the raw message to a message handler by pulling out the topic and deserializing the payload. You can add additional middleware to the message router such as validation and permissions.

```csharp
.UseMessageHandlers(router => router
    .UseFluentValidation())
```

## UseFluentValidation
This adds FluentValidation to the pipeline. It will attempt to find a validator for the request type, and if it finds a validation failure it will return a validation failure before it even hits the message handler.

```csharp
.UseMessageHandlers(router => router
    .UseFluentValidation())
```
