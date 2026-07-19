# Benzene.Resilience.Polly

## What this package does
Runs a Benzene middleware pipeline (or an outbound port call) through a [Polly v8](https://www.pollydocs.org/)
`ResiliencePipeline`, so the full Polly strategy set — retry, circuit breaker, timeout, hedging,
fallback, rate limiter — applies to whatever `next` wraps. It is a **sibling** to `Benzene.Resilience`,
not a replacement: that package stays the zero-dependency homegrown retry; this one takes a
`Polly.Core` (8.x) dependency in exchange for the whole toolkit. See the
[Capability Matrix](../../docs/capability-matrix.md) for how the two resilience options relate.

The seam is a plain wrap: `IMiddleware<TContext>.HandleAsync(context, next)` calls
`pipeline.ExecuteAsync(_ => next(), ...)`. The `ResiliencePipeline` is supplied ready-built, so the
per-message cost is just `ExecuteAsync`.

## Key types/interfaces
- **`PollyResilienceMiddleware<TContext>`** — wraps `next()` in a supplied `ResiliencePipeline`.
  Constructor takes the pipeline plus an optional `Func<TContext, bool>? isFailure` (see outcome
  awareness below).
- **`Extensions.UseResiliencePipeline<TContext>(...)`** — four pipeline-builder overloads:
  - `(ResiliencePipeline pipeline)` — bring your own fully-configured Polly pipeline.
  - `(ResiliencePipeline pipeline, Func<TContext, bool> isFailure)` — same, but outcome-aware.
  - `(Action<ResiliencePipelineBuilder> configure)` — build the pipeline inline.
  - `(Action<ResiliencePipelineBuilder> configure, Func<TContext, bool> isFailure)` — inline +
    outcome-aware.
- **`BenzeneFailureResultException`** — the internal sentinel that bridges Benzene's result-on-context
  failure model to Polly's outcome model. Never escapes the middleware (see below).

## Outcome awareness (the dual failure model)
Benzene reports domain failure two ways: a **thrown exception**, or an **unsuccessful
`IBenzeneResult`** left on the context (not thrown) — see `docs/specification/core-concepts.md` §5.
Polly's `next()` here returns `void`, so the middleware can't see a failure *result* the way it sees
an exception.

The bridge: pass an `isFailure` predicate (e.g. `ctx => ctx.MessageResult?.IsSuccessful == false`).
After `next()` runs, if the predicate returns `true` the middleware throws
`BenzeneFailureResultException`, which the Polly pipeline treats as a handled outcome — **only if you
configure the pipeline to handle it**:

```csharp
app.UseResiliencePipeline(builder => builder.AddRetry(new RetryStrategyOptions
{
    ShouldHandle = new PredicateBuilder().Handle<BenzeneFailureResultException>(),
    MaxRetryAttempts = 3,
}), isFailure: ctx => ctx.MessageResult?.IsSuccessful == false);
```

The sentinel **never escapes**: once the pipeline finishes (retries exhausted / breaker open), the
middleware swallows it and the last unsuccessful result remains on the context — identical to running
with no resilience middleware. A **real** exception is never wrapped and propagates normally. When
`isFailure` is `null` (the default), only thrown exceptions drive the strategies.

## When to use this package
- You want more than exponential-backoff retry (circuit breaker, timeout, hedging, fallback, rate
  limiting), or you already standardize on Polly elsewhere.
- Highest-value placement is the **outbound** `OutboundRoutingBuilder` pipeline ("calling another
  service") — Benzene's whole thesis is wrapping port calls — but it works on any inbound
  `IMiddlewarePipelineBuilder<TContext>` too.

Prefer `Benzene.Resilience`'s `.UseRetry(...)` when you want retry only and no extra dependency.

## Important conventions
- **Resilience re-invokes the whole downstream pipeline.** As with `RetryMiddleware`, do not place it
  on an inbound context that has already written a response — a re-run would repeat those steps. It's
  intended for outbound/port calls that are safe to re-run.
- **Cancellation.** Benzene middleware has no `CancellationToken` parameter; the middleware passes
  the token Polly threads through `ExecuteAsync`. Where the transport has no deadline this is
  effectively `CancellationToken.None`.
- The `isFailure` path costs one sentinel `throw`/`catch` per failed attempt only — the success path
  allocates nothing beyond Polly's own `ExecuteAsync` state tuple.

## Dependencies
- **Polly.Core** (8.x) — the resilience engine (BSD-3-licensed; also the engine under
  `Microsoft.Extensions.Resilience`).
- **Benzene.Abstractions.Middleware** — `IMiddleware<TContext>`, `IMiddlewarePipelineBuilder<TContext>`.
- **Benzene.Core.Middleware** — middleware pipeline implementation.

## Coverage
`test/Benzene.Core.Test/Resilience/PollyResilienceMiddlewareTest.cs`: passing `next` runs once;
throw-then-succeed retries; an always-throwing `next` propagates the real exception; a failure
*result* + `isFailure` retries; retries-exhausted swallows the sentinel and leaves the failure result
on the context; without `isFailure` a failure result does not retry.
