# Benzene.Resilience

## What this package does
Provides a single retry middleware for the Benzene pipeline: `RetryMiddleware<TContext>`, added
via `.UseRetry(...)`. Retry uses **exponential backoff only**. There is **no Polly dependency** —
this package is pure Benzene middleware over `Benzene.Abstractions.Middleware`.

## Key types/interfaces
- **`RetryMiddleware<TContext>`** — re-invokes the downstream pipeline (`next`) on failure. Retries
  on a thrown exception (default: any exception except `OperationCanceledException`) and/or on a
  context predicate. Constructor knobs:
  - `numberOfRetries` (default 3)
  - `initialDelay` (default 200ms)
  - `backoffFactor` (default 2.0 — delay is multiplied by this each attempt)
  - `maxDelay` (`TimeSpan?`, default `null` = uncapped) — caps the actual sleep duration each
    attempt; the underlying exponential growth used to compute the *next* attempt's delay is left
    uncapped, so later attempts still compound off the true curve (matches AWS's documented "full
    jitter" algorithm: `sleep = random(0, min(cap, base * factor^attempt))`)
  - `shouldRetry` (`Func<Exception, bool>`) — decide retry from the exception
  - `shouldRetryContext` (`Func<TContext, bool>`) — decide retry from the resulting context (default
    `false`, i.e. no retry on a "successful-but-failed" context)
  - `jitter` (`Func<TimeSpan, TimeSpan>`, default `null` = no jitter / identity) — transforms the
    capped delay into the actual sleep duration. `RetryMiddleware.FullJitter(Random? random = null)`
    (the non-generic companion class) is a ready-made "full jitter" implementation
    (`random(0, delay)`) you can pass straight in — spreads out retries from many callers that
    backed off at the same moment instead of them all retrying in lockstep
  - `delay` (`Func<TimeSpan, Task>`) — override the delay mechanism (default `Task.Delay`), useful
    for tests
- **`RetryMiddleware.FullJitter(...)`** — static helper on the non-generic `RetryMiddleware` class
  (not `RetryMiddleware<TContext>` — it needs no `TContext`, matching the `Task`/`Task<T>` pattern).
- **`Extensions.UseRetry<TContext>(...)`** — pipeline-builder extension registering the middleware
  with the same parameters.

## When to use this package
- When you want a downstream pipeline step retried on transient failure with exponential backoff.

## Deliberate boundaries (this package)
- **This package is retry-only, and stays that way.** No circuit breaker, timeout, bulkhead, hedging,
  or fallback here, and **no Polly dependency** — that keeps `Benzene.Resilience` the zero-dependency
  option for callers who only want retry.
- **The full toolkit lives in the sibling `Benzene.Resilience.Polly`.** For circuit breaker / timeout
  / hedging / fallback / rate limiting, use `.UseResiliencePipeline(...)` from that package, which
  runs the pipeline through a Polly v8 `ResiliencePipeline`. It also bridges Benzene's
  result-on-context failure model to Polly's outcome model via an optional `isFailure` predicate —
  see `src/Benzene.Resilience.Polly/CLAUDE.md`. Pick this package (`.UseRetry`) for retry with no
  extra dependency; pick `Benzene.Resilience.Polly` for anything more.

## Important conventions
- **Retry re-invokes the whole downstream pipeline.** Do not place it on an inbound context that has
  already written a response (e.g. an inbound HTTP context) — a re-invocation would run those steps
  again. It is intended for outbound/port calls that are safe to re-run.
- `OperationCanceledException` is not retried by default (respects cancellation).

## Dependencies on other Benzene packages
- **Benzene.Abstractions.Middleware** — `IMiddleware<TContext>`, `IMiddlewarePipelineBuilder<TContext>`
- **Benzene.Core.Middleware** — middleware pipeline implementation
