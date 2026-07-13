# Migration Guide: Alpha → 1.0

Benzene's alpha releases (`0.x.x-alpha`) did not follow strict semver, and several
renames happened without a corresponding migration note. This guide collects the
API changes you're most likely to hit upgrading an alpha-era service to 1.0.

If you find something not covered here, please open an issue — this list was
compiled from what surfaced while auditing docs and examples for 1.0, not from a
complete diff of every alpha release.

## Naming changes

| Alpha | 1.0 |
|---|---|
| `UseDirectMessage(...)` | `UseBenzeneMessage(...)` |
| `DirectMessageRequest` / `DirectMessageResponse` | `BenzeneMessageRequest` / `BenzeneMessageResponse` |
| `DirectMessageContext` | `BenzeneMessageContext` |
| `UseProcessDirectMessageResponse()` | Removed — response mapping now happens automatically inside `UseBenzeneMessage(...)` |
| `UseMessageRouter(...)` | `UseMessageHandlers(...)` |
| `IServiceResult<T>` / `HandlerResult` | `IBenzeneResult<T>` / `BenzeneResult` (static factory: `BenzeneResult.Ok(...)`, `.NotFound<T>()`, etc.) |
| `UseElementsLogContext()` | `UseLogResult(x => x.WithCorrelationId())` or `UseLogContext(...)` |
| `TestAwsLambdaStartUp<TStartUp>` | `AwsLambdaBenzeneTestStartUp<TStartUp>` |
| `testHost.SendEventAsync<T>(...)` | `testHost.SendBenzeneMessageAsync(...)` (built via `MessageBuilder.Create(topic, message)`) |
| `Benzene.Core.MiddlewareBuilder` (namespace) | `Benzene.Core.Middleware` |
| `AwsEventStreamPipelineBuilder` | `MiddlewarePipelineBuilder<AwsEventStreamContext>` |

## Logging: IBenzeneLogger → Microsoft.Extensions.Logging

The custom logging stack has been removed in favour of `Microsoft.Extensions.Logging`.
Framework and handler code now inject `ILogger<T>`; structured log context flows via
`ILogger.BeginScope`. `UsingBenzene(...)` calls `AddLogging()` for you, so loggers
always resolve — configure providers the standard .NET way.

| Alpha | 1.0 |
|---|---|
| `IBenzeneLogger` | `ILogger<T>` / `ILogger` (Microsoft.Extensions.Logging) |
| `BenzeneLogLevel` | `LogLevel` (values map 1:1) |
| `BenzeneLogger.NullLogger` | `NullLogger.Instance` / `NullLogger<T>.Instance` |
| `IBenzeneLogAppender` | `ILoggerProvider` (register via `AddLogging(x => x.Add...())`) |
| `IBenzeneLogContext.Create(props)` | `ILogger.BeginScope(props)` |
| `AddMicrosoftLogger()` | Removed — `AddLogging()` is called by `UsingBenzene` |
| `AddSerilog()` (Benzene.Serilog) | `AddLogging(x => x.AddSerilog())` (Serilog's MEL provider) |
| `AddLog4Net()` (Benzene.Log4Net) | log4net's MEL provider (`Microsoft.Extensions.Logging.Log4Net.AspNetCore`) |
| `ILogContextBuilder.CreateForRequest/CreateForResponse` | `BuildRequestScope`/`BuildResponseScope` (only relevant to custom middleware; the `With*`/`OnRequest`/`OnResponse` fluent API is unchanged) |

The packages `Benzene.Microsoft.Logging`, `Benzene.Serilog` and `Benzene.Log4Net`
no longer exist. `UseLogResult(...)`/`UseLogContext(...)` and the enrichment
extensions (`WithCorrelationId`, `WithTopic`, `WithTransport`, `WithHeaders`,
`WithRequestId`, `WithApplication`, `WithHttp`) are source-compatible.

## Message results

The old `IMessageResult` carried `Topic`, `Status`, `Errors`, `Payload`, and
`MessageHandlerDefinition` directly. It's been replaced by `IMessageHandlerResult`
(and `IMessageHandlerResult<TResponse>`), which wraps an `IBenzeneResult` instead
of exposing those fields directly:

```csharp
// Alpha
public string Map(TContext context, ISerializer serializer)
{
    var messageResult = context.MessageResult;
    return messageResult.IsSuccessful
        ? serializer.Serialize(messageResult.Payload)
        : serializer.Serialize(new ErrorPayload(messageResult.Status, messageResult.Errors));
}

// 1.0
public string Map(TContext context, IMessageHandlerResult messageHandlerResult, ISerializer serializer)
{
    return messageHandlerResult.BenzeneResult.IsSuccessful
        ? serializer.Serialize(messageHandlerResult.MessageHandlerDefinition.ResponseType, messageHandlerResult.BenzeneResult.PayloadAsObject)
        : serializer.Serialize(new ErrorPayload(messageHandlerResult.BenzeneResult.Status, messageHandlerResult.BenzeneResult.Errors));
}
```

If you had a custom `IResponsePayloadMapper<TContext>`, see
`Benzene.Core.MessageHandlers.Response.DefaultResponsePayloadMapper` for the
current reference implementation.

## Bug fixes that change behavior

Two bugs fixed during 1.0 prep change runtime behavior if you were relying on
the old (incorrect) behavior:

- **`TryAddSingleton(Type)`** previously called `AddScoped` internally, so
  "singleton" registrations were silently scoped instead. It now genuinely
  registers a singleton. If something in your app depended on getting a new
  instance per scope from a `TryAddSingleton` call, it will now get one shared
  instance.
- **`Extensions.Split()`** passed the wrong variable to the branch pipeline
  builder. Split pipelines now branch correctly; if your split branch appeared
  to silently no-op before, it should now actually execute.

## Removed: `AddScoped<T>(T instance)` extension

`BenzeneServiceContainerExtensions.AddScoped<T>(T instance)` — the overload
with "don't register if already registered" semantics — has been removed. It
was unreachable dead code: `IBenzeneServiceContainer` declares its own
`AddScoped<T>(T instance)` member with the identical signature, so normal call
syntax always resolved to that (unconditional) method instead. If you want
Try-semantics for an existing instance, check `IsTypeRegistered<T>()` yourself
before calling `AddScoped`.

## Unified hosting: `BenzeneStartUp` / `IBenzeneApplicationBuilder`

1.0 introduces a platform-neutral application definition. Instead of a
platform-specific StartUp type per host, you derive **one**
`BenzeneStartUp` (`Benzene.Microsoft.Dependencies`) and run it on whichever
host(s) you need:

```csharp
public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration() => ...;

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.UsingBenzene(x => x.AddMessageHandlers(typeof(StartUp).Assembly));
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseHttp(httpApp => httpApp.UseCorrelationId().UseMessageHandlers());
    }
}
```

| Platform | Host entry point |
|---|---|
| AWS Lambda | `public class Function : AwsLambdaHost<StartUp> { }` (`Benzene.Aws.Lambda.Core`) |
| Azure Functions (isolated worker) | `hostBuilder.UseBenzene<StartUp>()` (`Benzene.Azure.Function.Core`) |
| Generic Worker (`Microsoft.Extensions.Hosting`) | `hostBuilder.UseBenzene<StartUp>()` (`Benzene.HostedService`) |
| ASP.NET Core | `builder.UseBenzene<StartUp>()` + `app.UseBenzene()` (`Benzene.AspNet.Core`) |

This is **additive**, not a forced migration: the pre-1.0 `AwsLambdaStartUp`
/ `AwsLambdaBenzeneTestStartUp<TStartUp>` path and its existing tests keep
working unchanged. `BenzeneStartUp` is the recommended path for new services
and the only way to share one StartUp across multiple hosts.

Testing gets a matching unified entry point — see
[`BenzeneTestHost`](#new-unified-test-host-benzenetesthost) below.

## HTTP verb: `UseHttp` (ASP.NET Core, Azure Functions) — `UseApiGateway` unchanged (AWS)

As part of the hosting unification, the HTTP-trigger verb converges on one
name, **`UseHttp(...)`**, on the two platforms that share a request/response
HTTP shape:

| Alpha / early 1.0 | 1.0 |
|---|---|
| `UseAspNet(...)` (ASP.NET Core) | `UseHttp(...)` — `Benzene.AspNet.Core.BenzeneExtensions.UseHttp`, on both `IAspApplicationBuilder` and `IBenzeneApplicationBuilder` |
| (Azure Functions ASP.NET-style trigger, in-process worker) | `UseHttp(...)` — `Benzene.Azure.Function.AspNet.DependencyInjectionExtensions.UseHttp`, rewritten onto the isolated worker (see below) |

**`UseApiGateway(...)` on AWS Lambda's API Gateway integration is
unchanged** — it was *not* folded into this rename. It remains its own verb
(`Benzene.Aws.Lambda.ApiGateway.Extensions.UseApiGateway`), because API
Gateway's context type (`ApiGatewayContext`) doesn't share a concrete HTTP
shape with ASP.NET Core's `HttpContext` or Azure's `HttpRequest`. Don't
search-and-replace `UseApiGateway` → `UseHttp` when migrating an AWS app.

## Azure Functions: package rename + isolated worker rewrite

Two related, compounding breaking changes for Azure Functions users:

1. **Packages renamed** to align with the `Benzene.Aws.Lambda.<Transport>`
   convention:

   | Alpha | 1.0 |
   |---|---|
   | `Benzene.Azure.Core` | `Benzene.Azure.Function.Core` |
   | `Benzene.Azure.AspNet` | `Benzene.Azure.Function.AspNet` |
   | `Benzene.Azure.EventHub` | `Benzene.Azure.Function.EventHub` |
   | `Benzene.Azure.Kafka` | `Benzene.Azure.Function.Kafka` |
   | (each package's matching `*.TestHelpers`) | renamed the same way |

   `Benzene.AspNet.Core` (the separate ASP.NET Core integration, unrelated
   to Azure Functions) was deliberately left untouched.

2. **Rewritten from the in-process worker model to the isolated worker
   model.** `Microsoft.Azure.WebJobs`/`IWebJobsStartup` are gone; Azure now
   runs on `Microsoft.Azure.Functions.Worker.*`. `AzureFunctionStartUp` (the
   one WebJobs-coupled type) is deleted in favor of the platform-neutral
   `BenzeneStartUp`. Kafka's binding type changes from `KafkaEventData<string>`
   to the isolated worker's `KafkaRecord` (`byte[] Key`/`Value` instead of
   `string`). If you have a custom `KafkaContext`/`KafkaApplication`/message
   getter, it needs updating for the new binding type.

   New host wiring: `IHostBuilder.UseBenzene<TStartUp>()`
   (`Benzene.Azure.Function.Core`). See the hosting table above.

## Logging & tracing infrastructure: Datadog/Zipkin/X-Ray deleted, OpenTelemetry rebuilt

Benzene's tracing story moved onto `System.Diagnostics.Activity` as the one
shared representation of a trace span, superseding the old
`TimerMiddleware`/`IProcessTimer`-per-vendor approach:

- **`Benzene.Datadog`, `Benzene.Zipkin`, and `Benzene.Aws.XRay` are deleted
  entirely** — packages, `AddDatadog()`/`AddZipkin()`/`UseXRayTracing()`
  extension methods, the lot. Each was a standalone timer backend wrapping
  one vendor SDK. Every pipeline now gets a real `Activity` span
  automatically (see below), and any of these three backends can be
  reached through the OpenTelemetry exporter of your choice instead of a
  dedicated Benzene package per vendor.
- **`Benzene.Diagnostics`'s `AddDiagnostics()`** now registers
  `ActivityMiddlewareWrapper` (replacing the old `TimerMiddleware`/timer
  wrapper stack), which automatically wraps *every* middleware in *every*
  pipeline in an `Activity` span tagged with topic/transport/handler info —
  no explicit call needed per middleware. `ActivityProcessTimer`/
  `ActivityProcessTimerFactory` is the new default `IProcessTimerFactory`
  (`UseTimer("name")` call sites keep working, now backed by `Activity`
  instead of a vendor SDK).
- **`Benzene.OpenTelemetry`'s `AddOpenTelemetry()`** (an
  `IBenzeneServiceContainer` extension) **is removed**, replaced by
  **`AddBenzeneInstrumentation()`** — extension methods on OpenTelemetry's
  own `TracerProviderBuilder` and `MeterProviderBuilder`, wiring Benzene's
  `ActivitySource`/`Meter` (both named `"Benzene"`,
  `Benzene.Diagnostics.BenzeneDiagnostics`) into your own OTel pipeline
  instead of Benzene owning provider setup:

  ```csharp
  // Alpha / early 1.0
  services.UsingBenzene(x => x.AddOpenTelemetry());

  // 1.0
  services.AddOpenTelemetry()
      .WithTracing(x => x.AddBenzeneInstrumentation())
      .WithMetrics(x => x.AddBenzeneInstrumentation());
  ```

| Alpha | 1.0 |
|---|---|
| `Benzene.Datadog` / `AddDatadog()` | Deleted — export `Activity` spans to Datadog via an OTel exporter |
| `Benzene.Zipkin` / `AddZipkin()` | Deleted — export `Activity` spans to Zipkin via an OTel exporter |
| `Benzene.Aws.XRay` / `UseXRayTracing()` | Deleted — export `Activity` spans to X-Ray via an OTel exporter |
| `TimerMiddleware`/`TimerMiddlewareWrapper` (as the auto-wrap-every-middleware mechanism) | `ActivityMiddlewareWrapper`/`ActivityProcessTimer`, registered by `AddDiagnostics()` |
| `Benzene.OpenTelemetry.AddOpenTelemetry()` | `TracerProviderBuilder`/`MeterProviderBuilder.AddBenzeneInstrumentation()` |

## Correlation ID: `UseCorrelationId()` obsoleted, default header bug fixed

`UseCorrelationId()` (`Benzene.Diagnostics.Correlation`) is now marked
`[Obsolete]` — it's superseded by automatic W3C `traceparent` propagation
(`UseW3CTraceContext()`, below) for cross-service correlation — but it's
still fully functional and not going away.

Along the way, a real default-header bug was fixed: the no-argument overload
previously checked for the literal header key `"correlationId"` only. It now
checks, case-insensitively, in order: `x-correlation-id`, `correlation-id`,
then `correlationId` (kept last as a legacy fallback). If you were sending
`X-Correlation-Id` and it was silently ignored before, it will now be picked
up.

## New: `UseBenzeneEnrichment()`, `UseBenzeneMetrics()`, W3C trace context

Several new, portable middleware/decorator additions in `Benzene.Diagnostics`
and `Benzene.Clients`:

- **`UseBenzeneEnrichment()`** — one middleware that attaches `invocationId`,
  `traceId`/`spanId`, `topic`, `transport`, and `handler` to the logging
  scope (`ILogger.BeginScope`) and tags the current `Activity` with
  `benzene.invocationId`, the same way on every platform. Each key degrades
  gracefully (simply omitted) when its backing service isn't registered for
  the current pipeline scope.
- **`UseBenzeneMetrics()`** — records message-processed count and duration,
  tagged by topic/transport/result, on the shared `"Benzene"` `Meter`.
- **`UseW3CTraceContext()`** — inbound middleware that reads
  `traceparent`/`tracestate` (case-insensitively) and starts the pipeline's
  root `Activity` with the parsed remote context as its parent, so traces
  continue across services instead of each hop starting a disconnected one.
  Must be the first middleware added. Currently wired for HTTP-based
  transports only (ASP.NET Core, Azure Functions' ASP.NET-style trigger, API
  Gateway) — SQS/SNS/Kafka/Event Hub inbound extraction is follow-up work.
- **`WithW3CTraceContext()`** (`Benzene.Clients.TraceContext`) — the
  matching outbound `ClientBuilder` decorator, stamping `Activity.Current`'s
  `traceparent`/`tracestate` onto outgoing requests.

## Obsolete: AWS-only `WithRequestId()`/`WithApplication()` log-context extensions

`Benzene.Aws.Lambda.Core.LogContextBuilderExtensions.WithRequestId()` and
`WithApplication()` are now `[Obsolete]`, superseded by the portable
`UseBenzeneEnrichment()` (above), which covers the same information on every
platform instead of just AWS Lambda. Both still work.

## Bug fix: outbound client header forwarding

None of the outbound client context converters — HTTP, SQS, SNS, Kafka —
actually forwarded `IBenzeneClientRequest.Headers` onto the real wire
request. Header-based client decorators (e.g. `WithCorrelationId()` on a
`ClientBuilder`, or the new `WithW3CTraceContext()`) populated a headers
dictionary that never reached the other side. This is now fixed for all
four converters. (`AwsLambdaBenzeneMessageClient` was already correct — it
embeds headers in its own envelope. The lower-level `UseAwsLambda()`/
`LambdaContextConverter` path has no header-like concept to forward into and
remains documented as such.) If you were relying on the old (broken)
behavior — e.g. asserting that a header decorator had no effect — that
assumption no longer holds.

## New: unified `BenzeneTestHost`

`Benzene.Testing.BenzeneTestHost.Create<TStartUp>()` replaces ad hoc,
per-platform test wiring for `BenzeneStartUp`-based apps: it runs
`GetConfiguration` → applies `WithConfiguration(...)` overrides →
`ConfigureServices` → applies `WithServices(...)` overrides, then hands off
to a platform-specific `Build*()` bridge (`BuildAwsLambdaHost()`,
`BuildAzureFunctionApp()`, ...). This is purely additive — the pre-1.0,
`AwsLambdaStartUp`-specific `AwsLambdaBenzeneTestStartUp<TStartUp>` keeps
working and is documented as the "legacy" path in
[Testing Benzene](testing-benzene) for the many existing tests/examples still
built on it.

## Other notable behavior changes

- **`ExceptionHandlerMiddleware<TContext>`'s constructor gains an `ILogger`
  parameter** and now logs unhandled exceptions at `Error` level before
  invoking the configured callback. Previously, exceptions were swallowed
  silently unless the app author logged manually in the callback. If you
  construct `ExceptionHandlerMiddleware` directly rather than through
  `UseExceptionHandler(...)`, update the call site.
- **`DefaultJsonSchemaProvider` (`Benzene.JsonSchema`) is now implemented.**
  It previously returned `null` unconditionally, so `UseJsonSchema()`
  silently validated nothing unless you supplied a custom
  `IJsonSchemaProvider`. It now generates and caches a real JSON schema
  (draft 2020-12, camelCase) from each handler's request type. If you were
  relying on `UseJsonSchema()` being a no-op, requests that don't match
  their handler's request shape will now be rejected.
- **New `BENZ001` compile diagnostic**: the source generator now reports a
  compile *error* for duplicate topics across handlers (previously
  undetected at compile time). If your app has two handlers mapped to the
  same topic (and not disambiguated by version), it will now fail to build.

## Target framework

Benzene 1.0 targets **.NET 10**. See [VERSIONING.md](../VERSIONING.md) for the
ongoing target framework support policy.
