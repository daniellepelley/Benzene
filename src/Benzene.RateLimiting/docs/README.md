# Rate Limiting
Best-effort request rate limiting as Benzene middleware, built on .NET's standard
`System.Threading.RateLimiting` abstractions. Use it to stop publicly reachable utility endpoints —
health checks, the spec endpoint — from being a free denial-of-service vector or, on serverless
plans, a way for a stranger to run up your bill.

### The honest caveat, first
This limits **per service instance**. Run three instances and you admit up to 3× the configured
rate; let a serverless platform scale out and the multiplier grows with it. That makes this a
brake on abuse, not an exact science — **authoritative rate limiting belongs at the gateway**
(API Gateway, Azure API Management, your ingress) in front of every instance. Use this package as
defense-in-depth for endpoints that would otherwise ship with no protection at all.

### Integration with Benzene
Add it to the pipeline **before** whatever it should protect. A message the limiter rejects
short-circuits with a `TooManyRequests` result — HTTP 429 through the standard status mapping —
including the limiter's retry-after hint when available.

```csharp
app.UseBenzeneMessage(x => x
    .UseFixedWindowRateLimiting(60, TimeSpan.FromMinutes(1))   // 60 requests/minute
    .UseHealthCheck()
    .UseSpec()
    .UseMessageHandlers());
```

Limit by **payload size** instead of request count — a bytes-per-second budget via a token bucket:

```csharp
.UsePayloadSizeRateLimiting(
    maxBurstBytes: 256 * 1024,          // the most admissible at once
    bytesPerPeriod: 64 * 1024,          // sustained 64 KiB/second
    replenishmentPeriod: TimeSpan.FromSeconds(1))
```

Or bring your own `RateLimiter` — anything derived from the abstract class plugs in, with an
optional per-message permit cost:

```csharp
.UseRateLimiting(new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions { ... }))
.UseRateLimiting(myPartitionedLimiter, (resolver, context) => CostOf(context))
```
