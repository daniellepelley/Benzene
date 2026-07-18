# Polly Resilience Pipelines (circuit breaker, timeout, hedging, fallback)

Run a Benzene middleware pipeline (or an outbound port call) through your own
[Polly](https://www.pollydocs.org/) `ResiliencePipeline`, so you get circuit breaker / timeout /
hedging / fallback / rate limiting without Benzene wrapping or hiding Polly behind its own
abstraction.

## Problem Statement

`Benzene.Resilience` ships exactly one resilience pattern in-box: retry with exponential backoff
(`RetryMiddleware<TContext>` / `.UseRetry(...)`) — see [Resilience](../resilience.md). It
deliberately does **not** ship a circuit breaker, timeout, or bulkhead, and does not depend on Polly,
so it stays the zero-dependency option for callers who only want retry.

Everything else — circuit breaker, timeout, hedging, fallback, rate limiting — comes from the sibling
**`Benzene.Resilience.Polly`** package. It takes a `Polly.Core` dependency in exchange for the whole
toolkit, and it *exposes* Polly rather than wrapping it: you build a `ResiliencePipeline` with exactly
the strategies you want and hand it to `.UseResiliencePipeline(...)`. Benzene gives Polly a clean
place to plug into the pipeline; it does not re-abstract the strategy surface (retry strategies,
circuit-breaker state, hedging, rate limiting) that's the reason to reach for Polly in the first
place.

> Prefer to own the ~15 lines yourself instead of taking the dependency? The
> [DIY alternative](#appendix-diy-without-the-package) at the end shows the hand-rolled middleware —
> the package is that same bridge, packaged, tested, and with the outcome-aware failure handling
> below added.

## Prerequisites

- A Benzene middleware pipeline (any transport) built with `IMiddlewarePipelineBuilder<TContext>`.
- Familiarity with building a Polly `ResiliencePipeline` via `ResiliencePipelineBuilder` — this
  cookbook doesn't re-teach Polly itself; see the [Polly docs](https://www.pollydocs.org/) for the
  full strategy catalogue (retry, circuit breaker, timeout, hedging, rate limiter, fallback).

## Installation

```bash
dotnet add package Benzene.Resilience.Polly
```

It brings in `Polly.Core` (Polly's modern strategy-pipeline API, `ResiliencePipeline` /
`ResiliencePipelineBuilder`) transitively, plus `Benzene.Abstractions.Middleware` and
`Benzene.Core.Middleware`.

## Step-by-Step Implementation

### 1. Build a pipeline with the strategies you need

Compose whatever Polly strategies your service needs — timeout and circuit breaker in this example:

```csharp
using Polly;
using Polly.CircuitBreaker;

var pipeline = new ResiliencePipelineBuilder()
    .AddTimeout(TimeSpan.FromSeconds(5))
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(30),
        MinimumThroughput = 10,
        BreakDuration = TimeSpan.FromSeconds(15),
    })
    .Build();
```

### 2. Wire it into the pipeline

`.UseResiliencePipeline(...)` comes in four overloads — pass a prebuilt `ResiliencePipeline`, or
build it inline with an `Action<ResiliencePipelineBuilder>`, each optionally with the `isFailure`
predicate covered below:

```csharp
using Benzene.Resilience.Polly;

// Bring your own prebuilt pipeline...
app.UseSqs(sqsApp => sqsApp
    .UseResiliencePipeline(pipeline)
    .UseMessageHandlers(router => router.UseFluentValidation()));

// ...or configure it inline:
app.UseSqs(sqsApp => sqsApp
    .UseResiliencePipeline(builder => builder
        .AddTimeout(TimeSpan.FromSeconds(5))
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions { /* ... */ }))
    .UseMessageHandlers());
```

Register a stateful `ResiliencePipeline` **once** and reuse it — a circuit breaker's open/closed
state lives on the instance, so building a fresh one per message would defeat it. The prebuilt-pipeline
overload is the one to use when you want to share a single instance across pipeline builds (e.g. hold
it as a singleton in DI and pass it in).

`.UseResiliencePipeline(...)` is fully generic, like every other Benzene middleware, so it works on
any pipeline context — inbound transport contexts and `OutboundContext` alike.

## Outcome awareness: retrying a returned failure result

Benzene reports domain failure two ways: a **thrown exception**, or an **unsuccessful
`IBenzeneResult`** left on the context (not thrown) — see
[Message Result](../message-result.md). Polly's strategies fire on exceptions, so by default a
returned failure *result* is invisible to them.

Pass an `isFailure` predicate to bridge the two. After the pipeline runs, if the predicate returns
`true`, the middleware throws an internal `BenzeneFailureResultException` that Polly can treat as a
handled outcome — **but only if you configure the pipeline to handle it**:

```csharp
using Benzene.Resilience.Polly;
using Polly;
using Polly.Retry;

app.UseResiliencePipeline<MyMessageContext>(
    builder => builder.AddRetry(new RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder().Handle<BenzeneFailureResultException>(),
        MaxRetryAttempts = 3,
    }),
    isFailure: ctx => ctx.MessageResult?.IsSuccessful == false);
```

The sentinel **never escapes**: once the pipeline finishes (retries exhausted, breaker open, …), it's
swallowed and the last unsuccessful result remains on the context — identical to running with no
resilience middleware. A **real** exception is never wrapped and propagates normally. With no
`isFailure` (the default), only thrown exceptions drive the strategies. This is the one thing the DIY
middleware in the appendix doesn't give you for free.

## Outbound clients: the same middleware, no extra work

Because `.UseResiliencePipeline(...)` is fully generic, it works on an outbound route exactly the way
`Benzene.Resilience`'s `.UseRetry(...)` does (see
[Clients — Outbound middleware](../clients.md#outbound-middleware)). This is the higher-value case —
Benzene's whole thesis is wrapping port calls:

```csharp
services.UsingBenzene(x => x.AddOutboundRouting(routing => routing
    .Route("order:create", pipeline => pipeline
        .UseSqs(queueUrl)
        .UseResiliencePipeline(circuitBreakerPipeline))));
```

## Cancellation caveat

Benzene's middleware pipeline does not thread a `CancellationToken` through
`IMiddleware<TContext>.HandleAsync(TContext context, Func<Task> next)` — no middleware anywhere in
Benzene carries one. The middleware passes the token Polly threads through `ExecuteAsync`, so Polly's
own timeout strategy still works (it races the delegate against its own internal token), but where the
transport has no deadline this is effectively `CancellationToken.None` — a Polly-cancelled execution
can't cooperatively cancel the Benzene pipeline underneath it beyond however `next()` itself responds
to an `OperationCanceledException` propagating back up.

## Testing

`ResiliencePipeline` is a real object you can construct directly in a test — no need to spin up your
whole host to exercise the middleware:

```csharp
using Benzene.Resilience.Polly;
using Polly;
using Polly.Timeout;

var pipeline = new ResiliencePipelineBuilder()
    .AddTimeout(TimeSpan.FromMilliseconds(50))
    .Build();
var middleware = new PollyResilienceMiddleware<object>(pipeline);

await Assert.ThrowsAsync<TimeoutRejectedException>(() =>
    middleware.HandleAsync(new object(), () => Task.Delay(TimeSpan.FromSeconds(1))));
```

See `test/Benzene.Core.Test/Resilience/PollyResilienceMiddlewareTest.cs` for the full set (retry,
exception propagation, and the outcome-aware failure-result path).

## Troubleshooting

**I want retry AND circuit breaker together.**
Compose both strategies into the same `ResiliencePipeline` via `ResiliencePipelineBuilder`
(`.AddRetry(...).AddCircuitBreaker(...)`) rather than stacking `Benzene.Resilience`'s `RetryMiddleware`
on top of `.UseResiliencePipeline(...)` — Polly's own strategies are designed to compose correctly
with each other (e.g. a circuit breaker sees retries as part of one logical call), which two
independent middleware wrapping each other might not get right.

**Should I use `Benzene.Resilience`'s `UseRetry` or Polly's retry strategy?**
Use `UseRetry` when you want retry only and no extra dependency; use `Benzene.Resilience.Polly` when
you want anything more, or already standardize on Polly. Don't stack both on the same call.

**My returned failure result isn't being retried.**
Supplying `isFailure` isn't enough on its own — the Polly pipeline must also be configured to handle
`BenzeneFailureResultException` (`ShouldHandle = new PredicateBuilder().Handle<BenzeneFailureResultException>()`).
Without that, the sentinel the middleware throws isn't a handled outcome, so no strategy fires.

## Appendix: DIY without the package

If you'd rather not take the `Polly.Core` dependency through `Benzene.Resilience.Polly`, the
exception-only bridge is genuinely small — this is what the package's core does, minus the
outcome-aware `isFailure` handling:

```csharp
using Benzene.Abstractions.Middleware;
using Polly;

public class PollyMiddleware<TContext> : IMiddleware<TContext>
{
    private readonly ResiliencePipeline _pipeline;

    public PollyMiddleware(ResiliencePipeline pipeline) => _pipeline = pipeline;

    public string Name => nameof(PollyMiddleware<TContext>);

    public Task HandleAsync(TContext context, Func<Task> next)
        => _pipeline.ExecuteAsync(async _ => await next()).AsTask();
}

public static class PollyExtensions
{
    public static IMiddlewarePipelineBuilder<TContext> UsePolly<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, ResiliencePipeline pipeline)
        => app.Use(_ => new PollyMiddleware<TContext>(pipeline));
}
```

You still `dotnet add package Polly.Core` for the `ResiliencePipeline` type — the only thing you save
is the thin `Benzene.Resilience.Polly` layer (and you give up its four overloads and the outcome-aware
failure bridge). For most teams the package is the better trade; the DIY route is here for those who
want zero Benzene-owned surface between their code and Polly.

## See Also

- [Resilience](../resilience.md) — Benzene's own retry-with-backoff middleware
- [Middleware](../middleware.md)
- [Capability Matrix](../capability-matrix.md)
- [Polly documentation](https://www.pollydocs.org/)
