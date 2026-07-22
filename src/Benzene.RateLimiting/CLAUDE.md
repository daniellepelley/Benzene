# Benzene.RateLimiting

## What this package does
Best-effort, **per-instance** rate limiting as pipeline middleware, built directly on
`System.Threading.RateLimiting`'s abstract `RateLimiter`. Its purpose is protection, not traffic
shaping: endpoints a service can't avoid exposing publicly (health checks, spec) shouldn't be a
free denial-of-service or serverless-cost-amplification vector. A rejected message short-circuits
with a `TooManyRequests` result (→ HTTP 429 via the standard status mapping).

**Honesty rule (keep in every doc):** the limit is per service instance — a fleet of N instances
admits up to N× the configured rate, and serverless scale-out multiplies it further. Authoritative
rate limiting belongs at the gateway (API Gateway, APIM, ingress) in front of all instances. This
package documents that loudly (`docs/rate-limiting.md`); never present it as a hard guarantee.

## Key types
- `RateLimitingMiddleware<TContext> : IMiddleware<TContext>` (Name `"RateLimiting"`) - attempts
  `RateLimiter.AttemptAcquire(cost)` (never queues; protection wants immediate rejection). The
  acquired lease is disposed **after** `next()`, so a concurrency-style limiter's permits release
  correctly (a no-op for window/bucket limiters). A cost the limiter could never grant
  (`ArgumentOutOfRangeException`, e.g. a payload bigger than the whole bucket) is a rejection, not
  an error. Rejection attaches the topic's handler definition (same pattern as
  `Benzene.JsonSchema`) so the `ErrorPayload` body is written; includes the limiter's retry-after
  metadata in the message when present.
- `Extensions` - pipeline entry points; call **before** the middleware to protect
  (`UseHealthCheck`/`UseSpec`/`UseMessageHandlers`):
  - `UseRateLimiting(RateLimiter)` - bring-your-own limiter (fixed/sliding window, token bucket,
    concurrency, partitioned, custom), 1 permit per message. The caller owns the limiter's
    lifetime/disposal.
  - `UseRateLimiting(RateLimiter, Func<IServiceResolver, TContext, int>)` - BYO limiter + BYO
    per-message permit cost.
  - `UseFixedWindowRateLimiting(permitLimit, window)` - N messages per window.
  - `UseTokenBucketRateLimiting(tokenLimit, tokensPerPeriod, replenishmentPeriod)` - smoothed
    message rate with bursts.
  - `UsePayloadSizeRateLimiting(maxBurstBytes, bytesPerPeriod, replenishmentPeriod)` - token
    bucket where each message costs its body's UTF-8 byte size (bodyless costs 1): a
    bytes-per-second budget. A single payload larger than `maxBurstBytes` is always rejected.

## Dependencies
- `Benzene.Abstractions.Pipelines`, `Benzene.Core.MessageHandlers`, `Benzene.Core.Middleware`.
- NuGet: **System.Threading.RateLimiting** (the abstraction this package is deliberately shaped
  around, per the design request - BYO means any of its limiters plug in).

## Important conventions
- The limiter instance is shared across every message on the pipeline (and across pipelines if the
  caller passes the same instance) - that's the point; costs/permits are process-wide.
- No queuing (`QueueLimit = 0` on all built-ins): a protective limiter that queues just moves the
  resource exhaustion into memory.
- Rejection status is `BenzeneResultStatus.TooManyRequests` (already in the status vocabulary,
  mapped to HTTP 429 by `DefaultHttpStatusCodeMapper`).

## Tests
- `test/Benzene.Core.Test/Plugins/RateLimiting/RateLimitingPipelineTest.cs` - pass-through under
  the limit, 429 + message over it, payload-size budget spend + oversized-payload rejection, BYO
  concurrency limiter lease release, BYO cost function.
