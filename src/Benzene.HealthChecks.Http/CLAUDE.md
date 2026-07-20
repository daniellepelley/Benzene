# Benzene.HealthChecks.Http

## What this package does
A single `IHealthCheck` implementation (`HttpPingHealthCheck`) that verifies a downstream HTTP
dependency by issuing a GET request. Deliberately minimal: no built-in retry, and no timeout of its
own beyond whatever the injected `HttpClient` is configured with (`Benzene.HealthChecks`'s
`TimeOutHealthCheck` decorator applies a 10s timeout at the aggregation level regardless of this
package - see that package's CLAUDE.md).

## Key types/interfaces
- `HttpPingHealthCheck` - GETs a fixed URL; healthy only on a 200 OK response (any other status code,
  including other 2xx codes, is reported unhealthy); result `Data` includes the checked `Url` and the
  response's `StatusCode`; result `Dependencies` includes one `HealthCheckDependency("Http", url)`
- `HttpPingHealthCheckFactory` - resolves an `HttpClient` from DI each time the check runs, rather
  than capturing one at registration time
- `Extensions.AddHttpPing(builder, url)` - registration helper on `IHealthCheckBuilder`

## When to use this package
- Verifying a downstream HTTP service/API is reachable, as one check among others in a
  `Benzene.HealthChecks`-driven aggregate

## Dependencies on other Benzene packages
- **Benzene.HealthChecks.Core** - Health check core

## Important conventions
- No retry logic - a single GET, once
- No independent timeout - relies on the injected `HttpClient`'s own configuration (or the
  aggregator's timeout wrapper, if run through `Benzene.HealthChecks`)
- Only a 200 OK exact match counts as healthy - a 204 No Content or 3xx redirect is reported unhealthy
- **Requires an `HttpClient` in DI**: the factory resolves a bare `HttpClient` (`GetService<HttpClient>()`),
  which this package does NOT register - the consumer must register one (e.g. via `AddHttpClient()` /
  `IHttpClientFactory`, or `AddSingleton<HttpClient>()`), or the check throws on first run.
- The reported `Url`/`Dependency` have any basic-auth **userinfo stripped** (`https://user:pass@host`
  → `https://host`) - the report can flow out with no authorization, so credentials must not leak. The
  request itself still uses the full URL.
- Observes the ambient cancellation token via `ICancellationTokenAccessor` (resolved by the factory,
  optional) - passes it to `GetAsync`, so a cancelled scope aborts the request.
