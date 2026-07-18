# Polly Resilience Pipelines (circuit breaker, timeout, bulkhead)

Drop your own [Polly](https://www.pollydocs.org/) `ResiliencePipeline` into a Benzene middleware
pipeline, so you get circuit breaker / timeout / bulkhead / hedging without Benzene wrapping or
hiding Polly behind its own abstraction.

## Problem Statement

`Benzene.Resilience` ships exactly one resilience pattern in-box: retry with exponential backoff
(`RetryMiddleware<TContext>` / `.UseRetry(...)`) — see [Resilience](../resilience.md). It
deliberately does **not** ship a circuit breaker, timeout, or bulkhead middleware, and does not
depend on Polly. This is a considered boundary, not an oversight (see the
[Capability Matrix](../capability-matrix.md)): where a best-in-class library already exists, Benzene's
job is to give it a clean place to plug into the pipeline, not to re-implement or wrap it — wrapping
Polly behind a `Benzene.Resilience.Polly` package would hide the exact configuration surface (retry
strategies, circuit-breaker state, hedging, rate limiting) that's the reason to reach for Polly in
the first place.

The good news: the integration is genuinely small. This cookbook is the whole thing — there's no
missing package, just ~15 lines of middleware you own and can change freely.

## Prerequisites

- A Benzene middleware pipeline (any transport) built with `IMiddlewarePipelineBuilder<TContext>`.
- Familiarity with building a Polly `ResiliencePipeline` via `ResiliencePipelineBuilder` — this
  cookbook doesn't re-teach Polly itself; see the [Polly docs](https://www.pollydocs.org/) for the
  full strategy catalogue (retry, circuit breaker, timeout, hedging, rate limiter, fallback).

## Installation

```bash
dotnet add package Polly.Core
```

`Polly.Core` (not the older `Polly` v7 package) is Polly's modern strategy-pipeline API
(`ResiliencePipeline`/`ResiliencePipelineBuilder`) — this cookbook targets it.

## Step-by-Step Implementation

### 1. Write the middleware

A Polly `ResiliencePipeline` executes a delegate; a Benzene `IMiddleware<TContext>` wraps a
delegate (`next`). Bridging the two is the entire integration:

```csharp
using Benzene.Abstractions.Middleware;
using Polly;

public class PollyMiddleware<TContext> : IMiddleware<TContext>
{
    private readonly ResiliencePipeline _pipeline;

    public PollyMiddleware(ResiliencePipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public string Name => nameof(PollyMiddleware<TContext>);

    public Task HandleAsync(TContext context, Func<Task> next)
    {
        return _pipeline.ExecuteAsync(async _ => await next()).AsTask();
    }
}
```

```csharp
public static class PollyExtensions
{
    public static IMiddlewarePipelineBuilder<TContext> UsePolly<TContext>(
        this IMiddlewarePipelineBuilder<TContext> app, ResiliencePipeline pipeline)
    {
        return app.Use(new PollyMiddleware<TContext>(pipeline));
    }
}
```

That's it — `PollyMiddleware<TContext>` is fully generic, like every other Benzene middleware, so it
works on any pipeline context (inbound transport contexts, `OutboundContext` for outbound clients,
anything).

### 2. Build a pipeline with the strategies you need

Compose whatever Polly strategies your service needs — circuit breaker, timeout, and retry, in this
example (retry here is Polly's own retry strategy, an alternative to `Benzene.Resilience`'s
`RetryMiddleware`, not layered on top of it — pick one retry mechanism, not both, for the same call):

```csharp
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

var pipeline = new ResiliencePipelineBuilder()
    .AddTimeout(TimeSpan.FromSeconds(5))
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(30),
        MinimumThroughput = 10,
        BreakDuration = TimeSpan.FromSeconds(15)
    })
    .Build();
```

### 3. Wire it into the pipeline

```csharp
app.UseSqs(sqsApp => sqsApp
    .UsePolly(pipeline)
    .UseMessageHandlers(router => router.UseFluentValidation()));
```

Register the `ResiliencePipeline` once (it's stateful — a circuit breaker's open/closed state lives
on the instance) and reuse it across pipeline builds, typically as a singleton in DI:

```csharp
services.AddSingleton(pipeline);
services.AddScoped<PollyMiddleware<SqsMessageContext>>();
```

then resolve it via `app.Use<PollyMiddleware<SqsMessageContext>>()` if you want DI-managed
construction instead of the inline `.UsePolly(pipeline)` shown above.

## Outbound clients: the same middleware, no extra work

Because `PollyMiddleware<TContext>` is fully generic, it works on an outbound route exactly the way
`Benzene.Resilience`'s `RetryMiddleware<TContext>` does (see
[Clients — Outbound middleware](../clients.md#outbound-middleware)):

```csharp
services.UsingBenzene(x => x.AddOutboundRouting(routing => routing
    .Route("order:create", pipeline => pipeline
        .UseSqs(queueUrl)
        .UsePolly(pipeline: circuitBreakerPipeline))));
```

## Cancellation caveat

Benzene's middleware pipeline does not thread a `CancellationToken` through
`IMiddleware<TContext>.HandleAsync(TContext context, Func<Task> next)` — no middleware anywhere in
Benzene carries one (verified across every `IMiddleware<TContext>` in the framework). The example
above therefore calls `_pipeline.ExecuteAsync(async _ => await next())` with an implicit
`CancellationToken.None`. This means Polly's own timeout strategy still works (it races the
delegate against its own internal token, independent of any caller-supplied one), but a
Polly-cancelled execution cannot cooperatively cancel the Benzene pipeline underneath it beyond
however `next()` itself responds to `OperationCanceledException` propagating back up.

## Testing

`ResiliencePipeline` is a real object you can construct directly in a test — no need to spin up your
whole host to exercise `PollyMiddleware<TContext>`:

```csharp
var pipeline = new ResiliencePipelineBuilder()
    .AddTimeout(TimeSpan.FromMilliseconds(50))
    .Build();
var middleware = new PollyMiddleware<object>(pipeline);

await Assert.ThrowsAsync<TimeoutRejectedException>(() =>
    middleware.HandleAsync(new object(), () => Task.Delay(TimeSpan.FromSeconds(1))));
```

## Troubleshooting

**I want retry AND circuit breaker together.**
Compose both strategies into the same `ResiliencePipeline` via `ResiliencePipelineBuilder`
(`.AddRetry(...).AddCircuitBreaker(...)`) rather than stacking `Benzene.Resilience`'s
`RetryMiddleware` on top of `PollyMiddleware` — Polly's own strategies are designed to compose
correctly with each other (e.g. a circuit breaker sees retries as part of one logical call), which
two independent middleware wrapping each other might not get right.

**Should I use `Benzene.Resilience.RetryMiddleware` or Polly's retry strategy?**
Either — they don't conflict as long as you don't stack them on the same call. `RetryMiddleware` is
zero-dependency and matches Benzene's other context-aware knobs (`shouldRetryContext`, for retrying
a non-throwing "successful but failed" result — Polly's retry strategy works from thrown exceptions
and a result-predicate too, so both can do this; pick whichever fits the rest of your pipeline's
style).

## See Also

- [Resilience](../resilience.md) — Benzene's own retry-with-backoff middleware
- [Middleware](../middleware.md)
- [Capability Matrix](../capability-matrix.md)
- [Polly documentation](https://www.pollydocs.org/)
