# Centralized Error Handling Across a Benzene Pipeline

Catch unhandled exceptions in one place, log them consistently, and map them onto a sensible response for every transport instead of letting each handler roll its own `try`/`catch`.

## Problem Statement

Individual message handlers throwing unhandled exceptions leads to inconsistent behavior across transports: an ASP.NET Core/API Gateway request might blow up with an unstructured 500, while an SQS consumer might crash the whole batch instead of failing just the one message. You want:

- A single place to catch exceptions thrown anywhere downstream in a pipeline
- Consistent logging of the exception (so you're not relying on every handler author to remember to log)
- A transport-appropriate response: an HTTP-based transport should return a proper status code and body; a batch transport like SQS should report only the failed message back to the queue, not the whole batch

## Prerequisites

- A Benzene service with at least one pipeline built via `IMiddlewarePipelineBuilder<TContext>` (any transport)
- `Benzene.Core.Middleware` (already a dependency of `Benzene.Core.MessageHandlers` and every transport package — no extra install needed for the middleware itself)

## Installation

No new package is required — `UseExceptionHandler<TContext>()` lives in `Benzene.Core.Middleware` (`Benzene.Core.Middleware.Extensions`), which every Benzene transport package already depends on. If your project doesn't already reference it transitively:

```bash
dotnet add package Benzene.Core.Middleware --prerelease
```

For the HTTP example below you'll also use `Benzene.Results` (`ErrorPayload`, `BenzeneResultStatus`) and `Benzene.Http` (`DefaultHttpStatusCodeMapper`), both of which are already dependencies of any HTTP-based transport package (`Benzene.AspNet.Core`, `Benzene.Aws.Lambda.ApiGateway`, ...).

## What `UseExceptionHandler` actually does

```csharp
public static IMiddlewarePipelineBuilder<TContext> UseExceptionHandler<TContext>(
    this IMiddlewarePipelineBuilder<TContext> app, Action<TContext, Exception> onException)
```

This registers an `ExceptionHandlerMiddleware<TContext>` that wraps everything registered **after** it in a `try`/`catch`. Its exact behavior, read straight from `src/Benzene.Core.Middleware/ExceptionHandlerMiddleware.cs`:

```csharp
public async Task HandleAsync(TContext context, Func<Task> next)
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unhandled exception caught in middleware pipeline");
        onException(context, ex);
    }
}
```

Two things worth calling out precisely, because this middleware's behavior changed and older assumptions about it no longer hold:

1. **It always logs at `Error` level before calling your callback** — `"Unhandled exception caught in middleware pipeline"`, with the exception attached. This is not optional and does not depend on whether your `onException` callback logs anything itself. The logger is resolved via `UseExceptionHandler`'s wiring: `resolver.TryGetService<ILoggerFactory>()?.CreateLogger("Benzene") ?? NullLogger.Instance` — so if no `ILoggerFactory` is registered at all, logging silently no-ops via `NullLogger`, but `UsingBenzene(...)` registers a default logging setup for you, so this only bites you if you built the container yourself without calling `AddLogging()`.
2. **It does not rethrow.** Once caught, the exception stops propagating up the pipeline unless your `onException` callback itself throws — in which case the *original* exception is still logged first, and then whatever your callback throws propagates to whatever called this pipeline.

This is verified directly by `test/Benzene.Core.Test/Core/Core/MiddlewareBuilder/MiddlewareTest.cs`:

```csharp
[Fact]
public async Task ExceptionHandler_CaughtException_IsLogged()
{
    // ...
    builder.UseExceptionHandler((_, _) => { });
    builder.Use((_, _) => throw new Exception("Test"));
    // ...
    await pipeline.HandleAsync(new object(), resolver);

    var entry = Assert.Single(fakeLoggerFactory.Collector.Entries.Where(x => x.Level == LogLevel.Error));
    Assert.Equal("Unhandled exception caught in middleware pipeline", entry.Message);
    Assert.Equal("Test", entry.Exception.Message);
}

[Fact]
public async Task ExceptionHandler_ExceptionRethrownByHandler_IsStillLogged()
{
    // ...
    builder.UseExceptionHandler((_, ex) => throw ex);
    builder.Use((_, _) => throw new Exception("Test"));
    // ...
    await Assert.ThrowsAsync<Exception>(() => pipeline.HandleAsync(new object(), resolver));

    Assert.Single(fakeLoggerFactory.Collector.Entries.Where(x => x.Level == LogLevel.Error));
}
```

Because the middleware only wraps what comes *after* it, **placement matters**: add `.UseExceptionHandler(...)` as early as possible in the pipeline you want protected — before `.UseMessageHandlers()` and before any other middleware whose exceptions you want caught. Anything registered before it is unprotected.

There's also an important gap between this and how Benzene normally reports handler failures: when a handler returns `BenzeneResult.UnexpectedError(...)` (an *unsuccessful result*, not a thrown exception), the framework's own response pipeline (`DefaultResponsePayloadMapper`/`HttpStatusCodeResponseHandler`, see `src/Benzene.Core.MessageHandlers/Response/DefaultResponsePayloadMapper.cs` and `src/Benzene.Http/HttpStatusCodeResponseHandler.cs`) automatically serializes an `ErrorPayload` and maps the status to an HTTP code. A raw *thrown* exception caught by `UseExceptionHandler` bypasses that pipeline entirely — nothing builds a response for you. Your `onException` callback is responsible for constructing whatever response shape you want.

## Step-by-Step Implementation

### 1. HTTP transport (API Gateway) — map to a 500 response

```csharp
using System.Text.Json;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Core.Middleware;
using Benzene.Results;

app.UseApiGateway(apiGatewayApp => apiGatewayApp
    .UseExceptionHandler((ApiGatewayContext context, Exception exception) =>
    {
        context.EnsureResponseExists();
        context.ApiGatewayProxyResponse.StatusCode = 500; // matches DefaultHttpStatusCodeMapper's UnexpectedError -> 500
        context.ApiGatewayProxyResponse.Headers["content-type"] = "application/json";
        context.ApiGatewayProxyResponse.Body = JsonSerializer.Serialize(
            new ErrorPayload(BenzeneResultStatus.UnexpectedError, new[] { "An unexpected error occurred." }));
    })
    .UseCorrelationId()
    .UseMessageHandlers());
```

`500` here isn't an arbitrary choice — it's the same value `DefaultHttpStatusCodeMapper` (`src/Benzene.Http/DefaultHttpStatusCodeMapper.cs`) maps `BenzeneResultStatus.UnexpectedError` to for handlers that fail normally, so a thrown exception and a handler explicitly returning `BenzeneResult.UnexpectedError()` end up looking the same to the client. `ErrorPayload` (`Benzene.Results`) is the same payload shape `DefaultResponsePayloadMapper` uses for unsuccessful results, so the JSON body is consistent with the framework's own error responses too — you're just building it by hand because the exception path doesn't run that mapper for you.

The same pattern applies to `Benzene.AspNet.Core`'s `AspNetContext` (set `context.HttpContext.Response.StatusCode` and write the body) if you're hosting in ASP.NET Core instead of Lambda.

### 2. SQS transport — report only the failed message

```csharp
using Benzene.Aws.Lambda.Sqs;
using Benzene.Core.Middleware;

app.UseSqs(sqsApp => sqsApp
    .UseExceptionHandler((SqsMessageContext context, Exception exception) =>
    {
        context.IsSuccessful = false;
    })
    .UseCorrelationId()
    .UseMessageHandlers());
```

This relies on how `SqsApplication.HandleAsync` (`src/Benzene.Aws.Lambda.Sqs/SqsApplication.cs`) processes each record:

```csharp
try
{
    using (var scope = serviceResolverFactory.CreateScope())
    {
        // ...
        await _pipeline.HandleAsync(context, scope);
    }

    if (context.IsSuccessful.HasValue && !context.IsSuccessful.Value)
    {
        batchItemFailures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = context.SqsMessage.MessageId });
    }
}
catch (Exception ex)
{
    // logs "Processing SQS message {messageId} failed" and adds a BatchItemFailure anyway
}
```

Without `UseExceptionHandler`, a thrown exception is still caught — but one layer up, by `SqsApplication` itself, which logs its own `"Processing SQS message {messageId} failed"` error and adds the failure. Adding `UseExceptionHandler` inside the pipeline intercepts the exception first: `ExceptionHandlerMiddleware` logs `"Unhandled exception caught in middleware pipeline"`, your callback sets `context.IsSuccessful = false`, and the exception never reaches `SqsApplication`'s own `catch` — `SqsApplication` sees a pipeline call that returned normally with `IsSuccessful == false` and adds the batch item failure through its normal (non-exception) branch. The net effect for SQS is the same either way (the message ends up in `BatchItemFailures` so only it gets retried/DLQ'd, not the whole batch) — the difference `UseExceptionHandler` buys you is a single, customizable place to decide what "failed" means and to attach any extra logging/telemetry, instead of relying on `SqsApplication`'s hardcoded fallback message.

The resulting `SQSBatchResponse` (assuming a single failing record in the batch) serializes to:

```json
{ "batchItemFailures": [ { "itemIdentifier": "some-message-id" } ] }
```

### 3. Logged output

Both examples above produce the same shape of log line (same middleware, same fixed message), because both go through `ExceptionHandlerMiddleware`:

```
[Error] Benzene: Unhandled exception caught in middleware pipeline
System.InvalidOperationException: boom
   at ...
```

(`"Benzene"` is the fixed logger category `UseExceptionHandler` requests via `CreateLogger("Benzene")`.)

## Testing

Unit-test `onException` wiring the same way this repo does, without needing a real transport — build a minimal pipeline over a plain `object` (or your real context type) and assert on both the callback's side effect and the log entry, following `test/Benzene.Core.Test/Core/Core/MiddlewareBuilder/MiddlewareTest.cs`:

```csharp
[Fact]
public async Task ExceptionHandler_CaughtException()
{
    var services = new ServiceCollection();
    var container = new MicrosoftBenzeneServiceContainer(services);
    var builder = new MiddlewarePipelineBuilder<object>(container);

    var caught = false;
    builder.UseExceptionHandler((_, _) => caught = true);
    builder.Use((_, _) => throw new Exception("Test"));

    var pipeline = builder.Build();
    using var factory = new MicrosoftServiceResolverFactory(services);
    using var resolver = factory.CreateScope();

    await pipeline.HandleAsync(new object(), resolver);

    Assert.True(caught);
}
```

For the SQS-specific fallback behavior (what happens with *no* `UseExceptionHandler` at all, i.e. relying purely on `SqsApplication`'s own catch), see `test/Benzene.Core.Test/Aws/Sqs/SqsApplicationExceptionLoggingTest.cs` — it asserts a thrown exception during pipeline execution still produces exactly one `BatchItemFailure` and one `Error`-level log line containing the message ID, via `SqsApplication`'s own try/catch rather than `ExceptionHandlerMiddleware`. That test is a good template if you want to verify the "no explicit exception handler configured" case for your own pipeline.

## Troubleshooting

### Exceptions thrown before `UseExceptionHandler` aren't caught

**Solution:** `UseExceptionHandler` only wraps middleware registered *after* it in the same pipeline builder chain. Move it to be one of the first calls (right after transport-level concerns like `.UseCorrelationId()` if you want correlation IDs on file for the error, but before `.UseMessageHandlers()` and anything else you want protected).

### The exception isn't logged

**Solution:** Confirm an `ILoggerFactory` is actually registered. `UsingBenzene(...)` calls `services.AddLogging()` for you so this is rarely an issue with the standard DI setup — but if you constructed Benzene from a raw, already-built `IServiceProvider` (see [Monitoring — Logging](../monitoring.md#logging)) or a custom container without wiring logging defaults, `UseExceptionHandler` falls back to `NullLogger.Instance` and the log call is a no-op.

### HTTP responses come back with the wrong status/no body

**Solution:** Remember the automatic `ErrorPayload`/status-code mapping (`DefaultResponsePayloadMapper`, `HttpStatusCodeResponseHandler`) only runs for handlers that *return* an unsuccessful `IBenzeneResult` — it never runs for a caught exception. Your `onException` callback has to set the status code and body itself, as in the API Gateway example above.

### An SQS message keeps failing the whole batch, not just itself

**Solution:** Check you're setting `context.IsSuccessful = false` (or letting the exception propagate up to `SqsApplication`'s own catch) rather than swallowing the exception and leaving `IsSuccessful` at its default `null` — `SqsApplication` only adds a `BatchItemFailure` when `IsSuccessful.HasValue && !IsSuccessful.Value`, or when an exception escapes the pipeline entirely.

## Variations

### Different callback per transport

Nothing requires a single shared `onException` — as shown above, the HTTP and SQS pipelines each get their own callback tailored to that transport's response shape. There's no single "global" registration point across transports; `UseExceptionHandler` is added per pipeline (per `app.UseApiGateway(...)`, `app.UseSqs(...)`, etc.), so "global" here means "consistently applied to every pipeline you configure," not one process-wide handler.

### Combine with `UseRetry`

`Benzene.Resilience`'s `UseRetry(...)` (see [Common Middleware — UseRetry](../common-middleware.md#useretry)) retries on any exception except `OperationCanceledException` by default. Put it *inside* `UseExceptionHandler` (i.e. `UseExceptionHandler` registered first, `UseRetry` after) so retries happen first and `UseExceptionHandler` only catches the exception that survives all retry attempts.

### Re-throwing after logging

If you want the exception to still propagate (e.g. to let a Lambda invocation fail and be retried by the platform itself, rather than being converted to a graceful response), just rethrow in the callback: `.UseExceptionHandler((_, ex) => throw ex)`. The exception is still logged first — see the `ExceptionHandler_ExceptionRethrownByHandler_IsStillLogged` test above — it just isn't swallowed afterward.

## Further Reading

- [Common Middleware — UseExceptionHandler](../common-middleware.md#useexceptionhandler) — reference documentation for this middleware
- [Middleware — `.UseExceptionHandler()`](../middleware.md#useexceptionhandler) — pipeline-construction-level reference
- [Common Middleware — UseRetry](../common-middleware.md#useretry) — pairing retries with centralized error handling
- [Monitoring & Diagnostics — Logging](../monitoring.md#logging) — how Benzene's `ILogger`/`ILoggerFactory` resolution works
- [Handling SQS Message Failures](handling-sqs-failures.md) — DLQ and retry patterns for the SQS transport specifically
