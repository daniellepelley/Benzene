# Correlation Ids

> `UseCorrelationId()` is obsolete in favor of automatic [W3C `traceparent` propagation](monitoring#w3c-trace-context) for cross-service correlation. The `correlationId`-style header remains supported here as a legacy fallback and is still emitted to log scopes via `WithCorrelationId()`.

This will create a correlation Id for the current request, either copying from the message headers if a correlation Id has been passed in, or by creating a new correlation Id if there is not an existing correlation Id.

```csharp
app.UseBenzeneMessage(benzeneMessageApp => benzeneMessageApp
   .UseCorrelationId()
);
```

By default this checks `x-correlation-id`, then `correlation-id`, then the legacy `correlationId` header (matched case-insensitively, first match wins). Pass an explicit header name to check only that one instead: `.UseCorrelationId("my-header")`.

The Correlation Id can be added to the logging scope (via `ILogger.BeginScope`) using the following.

```csharp
.UseLogResult(x => x.WithCorrelationId());
```