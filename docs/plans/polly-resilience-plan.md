# Benzene Polly Resilience Integration Plan

## Context

`Benzene.Resilience` today is a single homegrown middleware — `RetryMiddleware<TContext>` plus a
`.UseRetry(...)` extension (`src/Benzene.Resilience/`). It does exponential-backoff retry with
`shouldRetry(Exception)` / `shouldRetryContext(TContext)` predicates and nothing else: no circuit
breaker, timeout, bulkhead/concurrency limit, hedging, fallback, or rate limiting. Its only
dependencies are `Benzene.Abstractions.Middleware` and `Benzene.Core.Middleware`.

[Polly v8](https://www.pollydocs.org/) is the de-facto .NET resilience standard and now the engine
underneath Microsoft's own `Microsoft.Extensions.Resilience` / `Microsoft.Extensions.Http.Resilience`
packages. Its `ResiliencePipeline` — "execute a callback through a chain of strategies" — is an
almost exact structural match for Benzene's middleware model (`IMiddleware<TContext>.HandleAsync(context,
next)` wraps `next`). Polly is BSD-3-licensed (free/OSS), so — unlike MediatR/AutoMapper — there is
no licensing trap.

Goal: give Benzene the full resilience toolkit as pipeline middleware, without discarding the
zero-dependency `UseRetry` for users who want no extra dependency.

## Verified facts this plan relies on

- **The middleware seam is a plain wrap.** `IMiddleware<TContext>.HandleAsync(TContext context,
  Func<Task> next)` — a Polly middleware calls `pipeline.ExecuteAsync(_ => next(), ...)`. This is
  the same `.Use(_ => new XxxMiddleware(...))` registration shape `UseRetry` already uses
  (`src/Benzene.Resilience/Extensions.cs`).
- **Two distinct application points, both already exist:**
  1. *Inbound* — around handler dispatch, on any transport's `IMiddlewarePipelineBuilder<TContext>`
     (where `UseRetry` sits today).
  2. *Outbound* — around a port/service call. Benzene's outbound send now runs through a per-topic
     pipeline built by `OutboundRoutingBuilder` behind `IBenzeneMessageSender`
     (`src/Benzene.Clients/`); resilience belongs on that pipeline for "calling another service."
     This is the higher-value case — Benzene's whole thesis is "wrap port calls in the pipeline."
- **Benzene's failure model is dual: exceptions *and* unsuccessful results.** A handler reports
  domain failure as an unsuccessful `IBenzeneResult` (status on the context), not an exception
  (`core-concepts.md` §5). Polly v8's retry/circuit-breaker/fallback are outcome-aware via
  `ShouldHandle` predicates, but Benzene middleware's `next()` returns `void` — the outcome lives on
  the context afterwards. So an outcome-aware Polly middleware must read the result off the context
  after `next()` and surface it to Polly. `RetryMiddleware` already models exactly this split with
  its `shouldRetryContext(TContext)` predicate — the Polly version generalizes it.
- **HttpClient is a special, already-solved case.** `Benzene.Client.Http` builds on `HttpClient`;
  the Microsoft-blessed path there is `Microsoft.Extensions.Http.Resilience`
  (`AddStandardResilienceHandler()`), which is Polly v8 under the hood.

## Design

### New package: `Benzene.Resilience.Polly`

Keep `Benzene.Resilience` (homegrown, zero-dependency retry) exactly as-is. Add a sibling
`Benzene.Resilience.Polly` referencing `Polly.Core` (8.x) + `Benzene.Abstractions.Middleware` +
`Benzene.Core.Middleware`. It provides:

- **`PollyResilienceMiddleware<TContext>` : `IMiddleware<TContext>`** — wraps `next()` in a Polly
  `ResiliencePipeline`. The pipeline is supplied at construction (a prebuilt `ResiliencePipeline`,
  or built once from an `Action<ResiliencePipelineBuilder>`), so per-message overhead is just
  `ExecuteAsync`.
- **Extensions** on `IMiddlewarePipelineBuilder<TContext>`:
  - `.UseResiliencePipeline(ResiliencePipeline pipeline)` — bring your own fully-configured Polly
    pipeline (retry + circuit breaker + timeout + …).
  - `.UseResiliencePipeline(Action<ResiliencePipelineBuilder> configure)` — configure inline.
  - Thin convenience wrappers mirroring the Polly strategy set where useful
    (`.UseCircuitBreaker(...)`, `.UseTimeout(...)`) — but the escape hatch above is the contract;
    the wrappers are sugar.
- **Outcome awareness (Phase 2).** An optional `Func<TContext, bool> isFailure` lets the middleware
  treat an unsuccessful `IBenzeneResult` on the context as a Polly-handled outcome (retry it, trip
  the breaker on it, fall back from it) — not just thrown exceptions. Implemented by inspecting the
  context after `next()` and throwing a private sentinel that the pipeline's `ShouldHandle` matches,
  or by using `ResiliencePipeline<Outcome>` — the exact mechanism is a Phase-2 implementation
  decision. Default off, so Phase 1 behaves purely on exceptions (matching most transports' natural
  "handler threw" path).

### HttpClient resilience (Phase 3)

For `Benzene.Client.Http`, adopt `Microsoft.Extensions.Http.Resilience`'s
`AddStandardResilienceHandler()` on the underlying `HttpClient` registration rather than routing
HTTP retries through the Benzene middleware layer — it is the Microsoft-standard, handles the
`HttpResponseMessage`-outcome semantics correctly, and keeps HTTP transport concerns at the HTTP
layer. Expose it via a small `Benzene.Client.Http` opt-in extension.

## Scope

**In:** the `Benzene.Resilience.Polly` package (middleware + extensions), outcome-aware failure
handling, HttpClient resilience wiring for `Benzene.Client.Http`, unit tests, a resilience cookbook,
and a package `CLAUDE.md`.

**Out (for now):** removing or rewriting the homegrown `Benzene.Resilience.UseRetry` (it stays as
the zero-dependency option); a bespoke rate-limiting feature (Polly's rate limiter covers it, and
.NET has `System.Threading.RateLimiting` natively); replacing `Microsoft.Extensions.Resilience`'s
own abstractions (we consume Polly directly, we don't re-abstract it).

## Phases

1. **Core Polly middleware** — `PollyResilienceMiddleware<TContext>` + `.UseResiliencePipeline(...)`
   (exception-based). Unit tests: retry/circuit-breaker/timeout around a failing `next()`; a passing
   `next()` runs once.
2. **Outcome-aware** — `isFailure(TContext)` so an unsuccessful `IBenzeneResult` is a handled
   outcome. Tests over a handler returning a failure *result* (not throwing).
3. **HttpClient resilience** — `Microsoft.Extensions.Http.Resilience` wiring in
   `Benzene.Client.Http`.
4. **Docs** — `docs/cookbooks/resilience-with-polly.md`, package `CLAUDE.md`, and a cross-link from
   `Benzene.Resilience`'s `CLAUDE.md` ("basic retry here; full toolkit in `Benzene.Resilience.Polly`").

## Open questions

- **Where does resilience most belong by default** — the inbound handler pipeline, the outbound
  `OutboundRoutingBuilder` pipeline, or both? (Leaning: document both; ship examples for the outbound
  case first, since that's the port-call scenario.)
- **Outcome-to-Polly bridge** — sentinel-exception vs `ResiliencePipeline<T>` (Phase 2). Both work;
  pick the one that keeps `next()`'s `void` shape and avoids allocating on the success path.
- **Cancellation** — Benzene middleware has no cancellation parameter (`core-concepts.md` §4);
  Polly wants a `CancellationToken`. Resolve from the invocation scope / context accessor, or pass
  `CancellationToken.None` where the transport has no deadline (document the choice).
