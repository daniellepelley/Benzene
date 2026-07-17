# Middleware Reference

A complete catalogue of the middleware Benzene ships, what each does, and the package it lives
in. For the concept of how the pipeline works, see [Middleware](../middleware); for prose on
the most common steps, see [Common Middleware](../common-middleware).

## Two pipeline levels

Benzene has two places you add middleware, and it matters which one you're on:

1. **The transport pipeline** — `IMiddlewarePipelineBuilder<TContext>`. This is the outer
   pipeline configured inside a transport block (`UseHttp(...)`, `UseApiGateway(...)`, and so
   on). Cross-cutting steps like correlation IDs, enrichment, retries, and health checks go
   here, and it's terminated by `UseMessageHandlers()`.
2. **The message-handler router** — `IMessageRouterBuilder`. This is the inner pipeline
   configured inside `UseMessageHandlers(router => router.UseX())`. Steps that run *per
   handler, after routing and deserialization* — validation, filters — go here.

```csharp
app.UseBenzene(benzene => benzene
    .UseHttp(http => http           // ── transport pipeline (IMiddlewarePipelineBuilder<AspNetContext>)
        .UseBenzeneEnrichment()
        .UseMessageHandlers(router => router   // ── message router (IMessageRouterBuilder)
            .UseFluentValidation())));
```

Order matters: each step wraps everything added after it, so put cross-cutting concerns
(correlation, logging, metrics, retry) before `UseMessageHandlers()`.

---

## Transport / entry-point middleware

These select the event source and open its sub-pipeline. Install the matching
[package](packages#hosts--transports); each is documented with a full walkthrough elsewhere.

| Step | Context | Package | Purpose |
|---|---|---|---|
| `UseHttp(...)` | `AspNetContext` | `Benzene.AspNet.Core`, `Benzene.Azure.Function.AspNet` | Handle HTTP requests. See [ASP.NET Core](../asp-net-core). |
| `UseApiGateway(...)` | `AwsEventStreamContext` | `Benzene.Aws.Lambda.ApiGateway` | Handle API Gateway events. See [AWS Lambda Setup](../getting-started-aws). |
| `UseSqs(...)` | `AwsEventStreamContext` | `Benzene.Aws.Lambda.Sqs` | Handle SQS queue events. |
| `UseSns(...)` | `AwsEventStreamContext` | `Benzene.Aws.Lambda.Sns` | Handle SNS notification events. |
| `UseS3(...)` | `AwsEventStreamContext` | `Benzene.Aws.Lambda.S3` | Handle S3 bucket-notification events. |
| `UseKafka(...)` | `AwsEventStreamContext` / Azure | `Benzene.Aws.Lambda.Kafka`, `Benzene.Azure.Function.Kafka` | Handle Kafka records. |
| `UseEventHub(...)` | `EventHubContext` | `Benzene.Azure.Function.EventHub` | Handle Azure Event Hub events. |
| `UseApiGatewayCustomAuthorizer(...)` | `AwsEventStreamContext` | `Benzene.Aws.Lambda.ApiGateway` | Handle API Gateway custom-authorizer (Lambda authorizer) invocations. |
| `UseWorker(...)` / `UseAwsLambda(...)` | host builders | `Benzene.SelfHost` / `Benzene.Aws.Lambda.Core` | Open the platform-neutral event pipeline for a self-hosted worker or AWS Lambda host. |

---

## Message routing

### `UseMessageHandlers()`

**Package:** `Benzene.Core.MessageHandlers` (transitive via every host). The terminal step of a
transport pipeline: it pulls the topic off the message, deserializes the payload, routes to the
matching handler, and serializes the result back. Optionally configures the message router.

```csharp
// Discover handlers in all loaded assemblies:
.UseMessageHandlers()

// Restrict discovery to specific assemblies or types:
.UseMessageHandlers(typeof(MyHandler).Assembly)
.UseMessageHandlers(typeof(MyHandler), typeof(OtherHandler))

// Configure the message router (validation, filters, …):
.UseMessageHandlers(router => router
    .UseFluentValidation()
    .UseFilters(typeof(MyFilter).Assembly))
```

---

## Cross-cutting middleware (transport pipeline)

All of these extend `IMiddlewarePipelineBuilder<TContext>` and are added before
`UseMessageHandlers()`.

### `UseW3CTraceContext()`

**Package:** `Benzene.Diagnostics`. Propagates the W3C `traceparent`/`tracestate` trace context
across service boundaries — the recommended cross-service correlation mechanism. See
[Monitoring](../monitoring#w3c-trace-context).

```csharp
.UseW3CTraceContext()
```

### `UseBenzeneEnrichment()`

**Package:** `Benzene.Diagnostics`. Attaches `invocationId`, `traceId`, `spanId`, `topic`,
`transport`, and `handler` to the logging scope, and tags the current `Activity`. Each key is
omitted if its backing service isn't registered. `invocationId` requires `UseBenzeneInvocation()`
on this or an outer pipeline.

```csharp
.UseBenzeneEnrichment()
```

### `UseBenzeneMetrics()`

**Package:** `Benzene.Diagnostics`. Records `benzene.messages.processed` (count) and
`benzene.message.duration` (ms) for the wrapped stage, tagged by `topic`, `transport`, and
`result`. Export via [OpenTelemetry](../monitoring#opentelemetry).

```csharp
.UseBenzeneMetrics()
```

### `UseTimer(...)`

**Package:** `Benzene.Diagnostics`. Opens a named `Activity` span around the rest of the
pipeline, or invokes a callback with the elapsed milliseconds.

```csharp
.UseTimer("benzene-message-application")   // named Activity span
.UseTimer((context, elapsedMs) => { /* record elapsed */ })
```

### `UseBenzeneInvocation()`

**Package:** `Benzene.Core.Middleware` (transport-specific overloads in the host packages).
Establishes the per-invocation context (invocation ID and transport metadata) that enrichment
and diagnostics read from. Add it once near the top of the pipeline.

```csharp
.UseBenzeneInvocation()
```

### `UseExceptionHandler(Action<TContext, Exception> onException)`

**Package:** `Benzene.Core.Middleware`. Catches exceptions thrown further down the pipeline and
runs your callback (logging via the `Benzene` logger by default). Use it to translate failures
into a controlled response.

```csharp
.UseExceptionHandler((context, ex) => logger.LogError(ex, "Unhandled"))
```

### `UseLogContext(...)` / `UseLogResult(...)`

**Package:** `Benzene.Core.Middleware`. Build a structured logging context from the message
(`UseLogContext`) or from the result (`UseLogResult`) via an `ILogContextBuilder<TContext>`.

```csharp
.UseLogContext(log => log.Add("topic", ctx => ctx.MessageTopic))
.UseLogResult(log => log.Add("status", ctx => ctx.MessageResult.Status))
```

### `UseRetry(...)`

**Package:** `Benzene.Resilience`. Wraps the rest of the pipeline in a retry policy with
exponential backoff.

```csharp
.UseRetry()                       // 3 retries, 2.0x backoff
.UseRetry(
    numberOfRetries: 5,
    initialDelay: TimeSpan.FromMilliseconds(200),
    backoffFactor: 2.0,
    shouldRetry: ex => ex is TimeoutException)
```

| Parameter | Default | Purpose |
|---|---|---|
| `numberOfRetries` | `3` | Maximum retry attempts. |
| `initialDelay` | `null` | Delay before the first retry. |
| `backoffFactor` | `2.0` | Multiplier applied to the delay each attempt. |
| `shouldRetry` | `null` | Predicate on the exception — retry only when it returns true. |
| `shouldRetryContext` | `null` | Predicate on the context — retry based on the message/result. |
| `delay` | `null` | Custom delay implementation (override the default `Task.Delay`). |

### `UseHealthCheck(...)`

**Package:** transport packages (e.g. `Benzene.Aws.Lambda.ApiGateway`) plus
`Benzene.HealthChecks`. Exposes health checks on a topic (default `healthcheck`), optionally
bound to an HTTP method/path. See [Health Checks](../health-checks).

```csharp
// HTTP-bound, explicit checks:
.UseHealthCheck("GET", "/health", new MyHealthCheck())

// Custom topic + builder:
.UseHealthCheck("my-service:healthcheck", builder => builder.AddCheck(...))
```

### `UseCors(CorsSettings corsSettings)`

**Package:** `Benzene.Http`. Applies CORS headers to HTTP responses. Requires an HTTP context.

```csharp
.UseCors(new CorsSettings
{
    AllowedDomains = ["https://app.example.com"],
    AllowedHeaders = ["Content-Type", "Authorization"],
})
```

| `CorsSettings` property | Purpose |
|---|---|
| `AllowedDomains` | Origins allowed to call the API (`"*"` allows all — avoid in production). |
| `AllowedHeaders` | Headers echoed in `Access-Control-Allow-Headers`. |

### `UseSpec(string topic = "spec")`

**Package:** `Benzene.Schema.OpenApi`. Exposes the service's OpenAPI/AsyncAPI schema on a topic
so it can be queried at runtime. **Required for the code-generation CLI to introspect the
service.** See [OpenAPI Specification](../spec).

```csharp
.UseSpec()
.UseSpec("my-service:spec")
```

### `UseJsonSchema()`

**Package:** `Benzene.JsonSchema`. Validates incoming messages against a JSON Schema before they
reach the handler.

```csharp
.UseJsonSchema()
```

### `UseXml()`

**Package:** `Benzene.Xml`. Adds XML serialization support so requests/responses can be handled
as XML in addition to JSON.

```csharp
.UseXml()
```

### `UseAvro(Action<AvroOptions>? configure = null)`

**Package:** `Benzene.Avro`. Adds Apache Avro binary serialization (`application/avro`) so
requests/responses can be handled as Avro in addition to JSON — schemas are inferred by
reflection by default, or registered explicitly per type via `configure`. See
[Package Reference](packages#serialization).

```csharp
.UseAvro()
```

### `UseMessagePack()`

**Package:** `Benzene.MessagePack`. Adds MessagePack binary serialization
(`application/msgpack`) so requests/responses can be handled as MessagePack in addition to JSON.
See [Package Reference](packages#serialization).

```csharp
.UseMessagePack()
```

---

## Message-router middleware

These extend `IMessageRouterBuilder` and run **inside** `UseMessageHandlers(router => ...)`,
per handler, after routing and deserialization.

### `UseFluentValidation(...)`

**Package:** `Benzene.FluentValidation`. Finds a FluentValidation validator for the request type
and short-circuits with a validation failure before the handler runs. See
[Fluent Validation](../fluent-validation).

```csharp
.UseMessageHandlers(router => router
    .UseFluentValidation())                              // scan loaded assemblies
.UseMessageHandlers(router => router
    .UseFluentValidation(typeof(MyValidator).Assembly))  // specific assemblies
```

### `UseDataAnnotationsValidation()`

**Package:** `Benzene.DataAnnotations`. Validates the request using
`System.ComponentModel.DataAnnotations` attributes. See [Data Annotations](../data-annotations).

```csharp
.UseMessageHandlers(router => router
    .UseDataAnnotationsValidation())
```

### `UseFilters(...)`

**Package:** `Benzene.Core.MessageHandlers` (`Benzene.Core.MessageHandlers.Filters`). Runs
filter components around handler execution — the place for cross-cutting per-handler concerns
such as authorization.

```csharp
.UseMessageHandlers(router => router
    .UseFilters(typeof(MyFilter).Assembly))
```

### `UseBroadcastEvent()`

**Package:** `Benzene.Extras`. Broadcasts an event message to multiple matching handlers rather
than routing it to a single one — useful for in-process fan-out.

```csharp
.UseMessageHandlers(router => router
    .UseBroadcastEvent())
```

---

## Outbound client middleware

For sending messages *out* to other services, configured on a client pipeline
(`IMiddlewarePipelineBuilder<...SendMessageContext>` / `IBenzeneClientContext<...>`). See the
[client packages](packages#outbound-messaging-clients).

| Step | Package | Sends via |
|---|---|---|
| `UseHttpClient()` / `UseHttp(...)` | `Benzene.Client.Http` | An outbound HTTP request. |
| `UseSqsClient()` / `UseSqs(...)` | `Benzene.Aws.Sqs` | An SQS queue. |
| `UseSnsClient()` / `UseSns(...)` | `Benzene.Clients.Aws` | An SNS topic. |
| `UseAwsLambdaClient()` / `UseAwsLambda(...)` | `Benzene.Clients.Aws` | A direct AWS Lambda invoke. |
| `UseKafkaClient()` / `UseKafka(...)` | `Benzene.Kafka.Core` | A Kafka topic. |

---

## See also

- [Middleware](../middleware) — the pipeline concept.
- [Common Middleware](../common-middleware) — narrative on the most-used steps.
- [Package Reference](packages) — which package each step ships in.
- [Monitoring & Diagnostics](../monitoring) — the observability middleware in context.
