# Resilience

Benzene's resilience support is a single, custom (not Polly-based) middleware — `RetryMiddleware<TContext>` — added to a pipeline via the `.UseRetry(...)` extension from `Benzene.Resilience`.

## Overview

`Benzene.Resilience` currently implements exactly one resilience pattern: **retry with exponential backoff**. There is no circuit breaker, timeout, or bulkhead middleware in the package today, and it does not depend on [Polly](https://github.com/App-vNext/Polly) — `RetryMiddleware<TContext>` is a small, hand-rolled loop over `Func<Task> next()`. (If you've seen references elsewhere describing this package as Polly-backed with circuit-breaker/timeout/bulkhead support, that describes aspirational scope, not the current source — verified against `src/Benzene.Resilience/*.cs`, which contains only `RetryMiddleware.cs` and `Extensions.cs`.)

Because it's a normal Benzene middleware (`IMiddleware<TContext>`), `UseRetry(...)` slots into a pipeline like any other `.Use*()` call and wraps everything *after* it in the chain — including message handlers, validation, or nested middleware — re-running that entire sub-pipeline on failure.

`RetryMiddleware<TContext>` supports retrying on two independent conditions:

- **An exception was thrown** by `next()` — checked with a `shouldRetry` predicate (default: retry everything except `OperationCanceledException`).
- **`next()` completed without throwing, but the context still represents a failure** — checked with a `shouldRetryContext` predicate you supply (default: never retry a non-throwing completion). This is useful because many Benzene pipelines represent failure as a result on the context object rather than as a thrown exception.

### This is not the same thing as `RetryBenzeneMessageClient` / `.WithRetry(n)`

Benzene also has a retry mechanism in `Benzene.Clients` — `RetryBenzeneMessageClient`, wired up via `ClientBuilder.WithRetry(int numberOfRetries)`. It is a **separate, unrelated implementation** that solves a different problem:

| | `Benzene.Resilience` (`UseRetry`) | `Benzene.Clients` (`WithRetry`) |
| --- | --- | --- |
| Applies to | A middleware pipeline stage (server-side, inbound) | An outbound `IBenzeneMessageClient` (client-side, calling another service) |
| Retries on | Thrown exceptions and/or a custom context predicate | A `ServiceUnavailable` result status specifically |
| Backoff | Exponential, configurable (`initialDelay`, `backoffFactor`) | None — retries immediately in a loop |
| Retry count semantics | `numberOfRetries` *additional* attempts after the first (default 3 → up to 4 total calls) | `numberOfRetries` *total* attempts (default 3) |

Don't reach for `Benzene.Resilience` if you're just retrying an outbound client call that returns `ServiceUnavailable` — use `.WithRetry(n)` on the `ClientBuilder` for that. Use `Benzene.Resilience` when you want retry behavior *inside* a pipeline, with backoff, driven by exceptions and/or your own notion of "did this actually succeed."

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
    .UseCorrelationId()
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
| `shouldRetry` | retry everything except `OperationCanceledException` | `Func<Exception, bool>` — called when `next()` throws, while attempts remain. Return `false` to let a specific exception propagate immediately without retrying. |
| `shouldRetryContext` | `_ => false` (never) | `Func<TContext, bool>` — called after `next()` **completes successfully** (no exception), while attempts remain. Return `true` to retry anyway based on state in `TContext` (e.g. a result object indicating a soft failure). |
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
Not currently implemented in `Benzene.Resilience`. You'd need to bring your own (e.g. wrap the relevant middleware call with Polly directly, or write a custom `IMiddleware<TContext>`) until such a middleware exists in this package.

## See Also

- [Middleware](middleware)
- [Common Middleware](common-middleware)
- [Caching](caching)
