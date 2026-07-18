# Resilience


> **Boundary:** `Benzene.Resilience` ships retry-with-backoff only (zero third-party dependencies). Circuit breaker, timeout, hedging, fallback, and rate limiting come from the sibling `Benzene.Resilience.Polly` package, which runs *your* Polly `ResiliencePipeline` as middleware — see [Polly Resilience Pipelines](cookbooks/polly-resilience.md) and the [Capability Matrix](capability-matrix.md).

Benzene's built-in resilience support is a single, custom (not Polly-based) middleware — `RetryMiddleware<TContext>` — added to a pipeline via the `.UseRetry(...)` extension from `Benzene.Resilience`. For the full resilience toolkit, add `Benzene.Resilience.Polly` (see [Beyond retry](#beyond-retry-the-full-polly-toolkit) below).

## Overview

`Benzene.Resilience` currently implements exactly one resilience pattern: **retry with exponential backoff**. There is no circuit breaker, timeout, or bulkhead middleware in the package today, and it does not depend on [Polly](https://github.com/App-vNext/Polly) — `RetryMiddleware<TContext>` is a small, hand-rolled loop over `Func<Task> next()`. (If you've seen references elsewhere describing this package as Polly-backed with circuit-breaker/timeout/bulkhead support, that describes aspirational scope, not the current source — verified against `src/Benzene.Resilience/*.cs`, which contains only `RetryMiddleware.cs` and `Extensions.cs`.)

Because it's a normal Benzene middleware (`IMiddleware<TContext>`), `UseRetry(...)` slots into a pipeline like any other `.Use*()` call and wraps everything *after* it in the chain — including message handlers, validation, or nested middleware — re-running that entire sub-pipeline on failure.

`RetryMiddleware<TContext>` supports retrying on two independent conditions:

- **An exception was thrown** by `next()` — checked with a `shouldRetry` predicate (default: retry everything except `OperationCanceledException`).
- **`next()` completed without throwing, but the context still represents a failure** — checked with a `shouldRetryContext` predicate you supply (default: never retry a non-throwing completion). This is useful because many Benzene pipelines represent failure as a result on the context object rather than as a thrown exception.

### Outbound client retry uses this same middleware

Retrying an outbound call — e.g. a `Benzene.Clients` route that returns `ServiceUnavailable` — is not
a separate mechanism. `.UseRetry(...)` works on `OutboundContext` unmodified, since `RetryMiddleware<TContext>`
is fully generic:

```csharp
services.UsingBenzene(x => x.AddOutboundRouting(routing => routing
    .Route("order:create", pipeline => pipeline
        .UseSqs(queueUrl)
        .UseRetry(3, shouldRetryContext: ctx => ((IBenzeneResult)ctx.Response).IsServiceUnavailable()))));
```

Put `.UseRetry(...)` *after* the transport middleware in the pipeline (outermost in the chain) so a
failed attempt retries the whole send beneath it, including any header-stamping middleware. See
[Clients — Outbound middleware](clients.md#outbound-middleware) for the full reference.

## Prerequisites

- A Benzene middleware pipeline (any transport — AWS Lambda, Azure Functions, ASP.NET Core, etc.) built with `IMiddlewarePipelineBuilder<TContext>`.

## Installation

```
dotnet add package Benzene.Resilience
```

`Benzene.Resilience` depends only on `Benzene.Abstractions.Middleware` and `Benzene.Core.Middleware` — no third-party packages (in particular, no Polly).

## Basic Usage

Add `.UseRetry()` at the point in the pipeline you want retried. Everything after it in the chain is re-run on each retry:

```csharp
app.UseBenzeneMessage(benzeneMessageApp => benzeneMessageApp
    .UseRetry(numberOfRetries: 3)
    .UseMessageHandlers());
```

With the defaults (`numberOfRetries: 3`, `initialDelay: 200ms`, `backoffFactor: 2.0`), a handler that keeps throwing will be attempted up to 4 times total (the original attempt plus 3 retries), waiting 200ms, then 400ms, then 800ms between attempts.

## Configuration

`UseRetry<TContext>(...)` (and the underlying `RetryMiddleware<TContext>` constructor) accept:

| Parameter | Default | Purpose |
| --- | --- | --- |
| `numberOfRetries` | `3` | Number of *additional* attempts after the first. Total possible invocations of `next()` is `numberOfRetries + 1`. |
| `initialDelay` | `TimeSpan.FromMilliseconds(200)` | Delay before the *first* retry. |
| `backoffFactor` | `2.0` | Multiplier applied to the delay after each retry (exponential backoff). `1.0` gives a constant delay; `1` retry has no effect on backoff since there's nothing after it. |
| `maxDelay` | `null` (uncapped) | `TimeSpan?` — caps the actual sleep each attempt. The exponential growth used to compute the *next* attempt's delay is left uncapped, so later attempts still compound off the true curve — only the sleep is capped, matching AWS's documented "full jitter" algorithm (`sleep = random(0, min(cap, base * factor^attempt))`). |
| `shouldRetry` | retry everything except `OperationCanceledException` | `Func<Exception, bool>` — called when `next()` throws, while attempts remain. Return `false` to let a specific exception propagate immediately without retrying. |
| `shouldRetryContext` | `_ => false` (never) | `Func<TContext, bool>` — called after `next()` **completes successfully** (no exception), while attempts remain. Return `true` to retry anyway based on state in `TContext` (e.g. a result object indicating a soft failure). |
| `jitter` | `null` (no jitter — identity function) | `Func<TimeSpan, TimeSpan>` — transforms the capped delay into the actual sleep. `RetryMiddleware.FullJitter()` (static, on the non-generic `RetryMiddleware` class) is a ready-made "full jitter" implementation (`random(0, delay)`) that spreads out retries from many callers that backed off simultaneously, instead of them all retrying in lockstep. |
| `delay` | `Task.Delay` | `Func<TimeSpan, Task>` — the actual wait between attempts. Overriding this (e.g. to a no-op) is how the package's own tests run retry scenarios instantly — see `test/Benzene.Core.Test/Resilience/RetryMiddlewareTest.cs`. |

Retry accounting is shared across both failure modes: once `numberOfRetries` attempts have been used up (whether from thrown exceptions, a non-satisfied `shouldRetryContext`, or a mix of both), the middleware stops — an exhausted exception-based retry rethrows the last exception; an exhausted context-based retry simply returns (no exception to throw).

## Advanced Usage

### Retrying based on context state instead of (or in addition to) exceptions

If your pipeline represents failure via the context rather than throwing, supply `shouldRetryContext`:

```csharp
app.UseRetry<MyMessageContext>(
    numberOfRetries: 3,
    shouldRetryContext: context => context.Result?.Status == BenzeneResultStatus.ServiceUnavailable);
```

This re-runs the wrapped pipeline whenever the handler returns a `ServiceUnavailable`-style result without throwing, in addition to the default exception-based retry behavior.

### Narrowing which exceptions are retried

```csharp
app.UseRetry<MyMessageContext>(
    numberOfRetries: 3,
    shouldRetry: ex => ex is HttpRequestException or TimeoutException);
```

Anything not matched by `shouldRetry` propagates immediately on the first occurrence, bypassing further retries.

### Tuning backoff

```csharp
app.UseRetry<MyMessageContext>(
    numberOfRetries: 5,
    initialDelay: TimeSpan.FromMilliseconds(50),
    backoffFactor: 3.0);
```

Delays for this configuration would be 50ms, 150ms, 450ms, 1350ms, 4050ms across the five retries.

### Capping delay and adding jitter

For a service with many concurrent callers that might all back off in lockstep (a "thundering herd"
retrying at the same moment), cap the maximum sleep and randomize it:

```csharp
app.UseRetry<MyMessageContext>(
    numberOfRetries: 5,
    initialDelay: TimeSpan.FromMilliseconds(100),
    backoffFactor: 2.0,
    maxDelay: TimeSpan.FromSeconds(10),
    jitter: RetryMiddleware.FullJitter());
```

Uncapped growth would reach 100ms, 200ms, 400ms, 800ms, 1600ms — none of which exceed the 10s cap in
this example, so `maxDelay` only matters once the exponential curve would otherwise exceed it (e.g.
with more retries or a larger `backoffFactor`). `FullJitter()` then randomizes each (possibly capped)
delay to somewhere between zero and that value, rather than sleeping the exact computed duration.

### Testing pipelines that use `UseRetry`

Pass a no-op `delay` function to avoid real waits in tests, as `RetryMiddlewareTest` does:

```csharp
var middleware = new RetryMiddleware<object>(numberOfRetries: 3, delay: _ => Task.CompletedTask);
```

## Examples

See [Advanced Usage](#advanced-usage) above for context-based retry, exception filtering, and backoff tuning examples. `test/Benzene.Core.Test/Resilience/RetryMiddlewareTest.cs` has additional worked examples covering exhausted retries, `OperationCanceledException` handling, and backoff timing assertions.

## Troubleshooting

**A retried operation seems to run more times than `numberOfRetries` suggests.**
`numberOfRetries` is the number of *retries*, not the total attempt count — total attempts are `numberOfRetries + 1`.

**`OperationCanceledException` isn't being retried.**
This is the default `shouldRetry` behavior — cancellation is treated as intentional, not transient, and is deliberately excluded. Pass a custom `shouldRetry` predicate if you need to retry on cancellation (e.g. for a self-imposed timeout you want to retry past).

**Retries seem to keep happening even though my handler "succeeded."**
Check whether you passed a `shouldRetryContext` predicate — if your context always satisfies it (e.g. a bug in the predicate, or a result field that's never populated the way you expect), the middleware will keep retrying successful, non-throwing calls until `numberOfRetries` is exhausted, then return normally without an exception (so the failure may not be obvious from a stack trace).

**I need a circuit breaker / timeout / bulkhead, not just retry.**
Not implemented in `Benzene.Resilience` — deliberately (see the boundary note at the top of this
page). Use the sibling `Benzene.Resilience.Polly` package, which runs a Polly `ResiliencePipeline`
as middleware — see [Beyond retry](#beyond-retry-the-full-polly-toolkit) below and the
[Polly Resilience Pipelines](cookbooks/polly-resilience.md) cookbook.

## Beyond retry: the full Polly toolkit

For circuit breaker, timeout, hedging, fallback, or rate limiting, add the sibling
`Benzene.Resilience.Polly` package. It wraps the rest of the pipeline in a
[Polly v8](https://www.pollydocs.org/) `ResiliencePipeline` — so the full Polly strategy set applies
to whatever `next` wraps (a handler dispatch on an inbound pipeline, or a port/service call on an
outbound one), without Benzene hiding Polly behind its own abstraction:

```
dotnet add package Benzene.Resilience.Polly
```

```csharp
using Benzene.Resilience.Polly;
using Polly;

app.UseResiliencePipeline<MyMessageContext>(builder => builder
    .AddTimeout(TimeSpan.FromSeconds(5))
    .AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(30),
        MinimumThroughput = 10,
        BreakDuration = TimeSpan.FromSeconds(15),
    }));
```

`Benzene.Resilience` stays the zero-dependency retry-only option; `Benzene.Resilience.Polly` is the
opt-in for the rest. The Polly package also bridges Benzene's dual failure model — a returned
(non-throwing) failure `IBenzeneResult` can be treated as a Polly-handled outcome via an `isFailure`
predicate, so it can drive retry/breaker/fallback too. See the
[Polly Resilience Pipelines](cookbooks/polly-resilience.md) cookbook for the full walkthrough,
including the outcome-aware bridge, outbound-client usage, and the cancellation caveat.

## See Also

- [Polly Resilience Pipelines](cookbooks/polly-resilience.md) — the full Polly toolkit as middleware
- [Middleware](middleware.md)
- [Common Middleware](common-middleware.md)
- [Caching](caching.md)
