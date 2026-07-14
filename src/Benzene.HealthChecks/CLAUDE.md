# Benzene.HealthChecks

## What this package does
Message-handler middleware that runs a registered set of `Benzene.HealthChecks.Core` health checks
and reports the aggregated outcome. Wires into a Benzene middleware pipeline via `.UseHealthCheck(...)`
(or the Kubernetes-style `.UseLivenessCheck(...)`/`.UseReadinessCheck(...)`) on a matching *message
topic* (e.g. `BenzeneMessage`/HTTP-over-message-handlers), not a dedicated ASP.NET Core route or
HTTP-endpoint attribute - there is no built-in `/health` route and no HTTP-endpoint-attribute
discovery in this package itself. An HTTP transport that maps message-handler results to HTTP
responses (see e.g. `Benzene.AspNet.Core`) is what turns this middleware's result into an actual HTTP
response - and that mapping DOES reflect health status in the HTTP status code (200 healthy, 503
unhealthy, via `HealthCheckProcessor`), not just the JSON body. See `docs/kubernetes-health-checks.md`
for the full Kubernetes wiring guide.

## Key types/interfaces

### Middleware wiring (`Extensions.cs`)
- `.UseHealthCheck(topic, params IHealthCheck[])` / `.UseHealthCheck(topic, Action<IHealthCheckBuilder>)`
  / `.UseHealthCheck(topic, IHealthCheckBuilder)` - registers middleware that, for any incoming
  message whose topic matches `topic` or `Constants.DefaultHealthCheckTopic` ("healthcheck"), runs
  every registered check and sets the aggregated `HealthCheckResponse` as the message result; other
  topics fall through to `next()` unchanged
- `.UseLivenessCheck(...)` / `.UseReadinessCheck(...)` - same 3 overload shapes, but respond ONLY to
  `Constants.DefaultLivenessTopic`/`DefaultReadinessTopic` ("liveness"/"readiness") - deliberately do
  NOT also match `DefaultHealthCheckTopic`, so registering both in one pipeline doesn't have one
  silently shadow the other on a shared fallback topic. Share the same underlying middleware
  (`UseHealthCheckMiddleware`, private) as `.UseHealthCheck(...)`.
- `HealthCheckBuilder : IHealthCheckBuilder` - collects health checks (DI-resolved types, inline
  factory functions) and, via `IHealthCheckFinder`, DI-registered `IHealthCheck` implementations
- `IHealthCheckFinder`/`HealthCheckFinder` - resolves every `IHealthCheck` registered directly in DI
  (as opposed to ones added inline through the builder)

### Execution
- `HealthCheckProcessor.PerformHealthChecksAsync` - runs every check wrapped in
  `TimeOutHealthCheck(ExceptionHandlingHealthCheck(check))` (10s hardcoded timeout, not configurable;
  exceptions become a failed result instead of propagating) and aggregates into a `HealthCheckResponse`
- `TimeOutHealthCheck` - enforces the 10s timeout via `Task.WhenAny`; note the inner check keeps
  running in the background after a timeout is reported (it isn't cancelled)
- `ExceptionHandlingHealthCheck` - catches any exception from the wrapped check and reports it as a
  failed result instead of throwing
- `FailedHealthCheck`/`InlineHealthCheck`/`SimpleHealthCheck` - small `IHealthCheck` helpers (a
  fixed-failure stub, a func-backed wrapper, and an always-healthy default, respectively)
- `HealthCheckNamer` - dedupes health check names in the aggregated response when multiple checks
  share the same `Type`

## When to use this package
- When exposing an aggregated health status through Benzene's existing message-handler pipeline
  (e.g. a `healthcheck` topic reachable the same way any other message handler is)
- As the execution engine underneath an HTTP-facing health endpoint built with a separate transport
  package - this package doesn't provide that HTTP surface itself

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Http** - HTTP abstractions (message-topic routing, not HTTP endpoint attributes)
- **Benzene.HealthChecks.Core** - Health check core

## Important conventions
- `IsHealthy` on the aggregated response is `true` unless at least one check reports
  `HealthCheckStatus.Failed` - a `Warning` result does not make the whole response unhealthy
- Every check gets a 10-second timeout and exception isolation automatically; individual
  `IHealthCheck` implementations don't need to implement either themselves
- `.UseHealthCheck(topic, ...)` responds to `Constants.DefaultHealthCheckTopic` ("healthcheck") in
  addition to whatever topic is passed; `.UseLivenessCheck`/`.UseReadinessCheck` do not
- `HealthCheckProcessor.PerformHealthChecksAsync` maps the aggregate result to
  `BenzeneResultStatus.Ok`/`ServiceUnavailable` (HTTP 200/503 via the standard status code mapper),
  not just an `isHealthy` body field - this matters for any consumer (Kubernetes probes, load
  balancer target-health checks) that only inspects the status code
