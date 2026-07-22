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
- `.UseContractsCheck(...)` - same 3 overload shapes again, responding ONLY to
  `Constants.DefaultContractsTopic` ("contracts"). A **diagnostic** topic for consumer-side
  contract-drift / downstream-provider checks (`Benzene.Clients.HealthChecks`'s `AddContractCheck`),
  deliberately separate from the liveness/readiness **probes**: a contract check calls a downstream
  service and reports drift, so wiring it into a probe would let one struggling dependency restart or
  de-route otherwise-healthy pods. Wire it to monitoring/the mesh, never a Kubernetes probe - see
  `docs/kubernetes-health-checks.md` and `work/client-health-checks-design.md`.
- `HealthCheckBuilder : IHealthCheckBuilder` - collects health checks (DI-resolved types, inline
  factory functions) and, via `IHealthCheckFinder`, DI-registered `IHealthCheck` implementations.
  `GetHealthChecks(resolver, includeDependencyChecks)` selects the probe scope: when `false` the
  dependency-category checks are dropped (the probe-safe set).
- `IHealthCheckFinder`/`HealthCheckFinder` - resolves DI-registered checks. `FindHealthChecks()` returns
  the checks registered as plain `IHealthCheck` (the probe-safe set); `FindDependencyHealthChecks()`
  returns the dependency-category checks (`IDependencyHealthCheck`), **de-duplicated by `DedupKey`**. The
  two sets are disjoint: a check registered under `IDependencyHealthCheck` is not returned when the
  container resolves `IEnumerable<IHealthCheck>`, so it never leaks onto a probe.

### Dependency registration category (§3.2 - the one-way door)
- `IDependencyHealthCheck`, its `DependencyHealthCheck` wrapper, and the `AddDependencyHealthCheck`
  registration seam all live in **`Benzene.HealthChecks.Core`** (so client packages auto-wire via their
  existing lightweight `.Core` reference, not a dependency on this middleware package). The wrapper
  delegates `Type`/`Tags`/`IsNonCritical`/`Ttl`/`Timeout`/`ExecuteAsync` unchanged and carries `DedupKey`.
- `AddDependencyHealthCheck(this IBenzeneServiceContainer, Func<IServiceResolver, IHealthCheck> factory,
  string? dedupKey = null)` is the config-time seam Phase 1 client extensions call via
  `app.Register(x => x.AddDependencyHealthCheck(...))`.
- Harvest matrix: **only** `.UseHealthCheck` (the general/deep layer) includes the dependency category;
  `.UseLivenessCheck`, `.UseReadinessCheck` **and** `.UseContractsCheck` all exclude it. A dependency
  check is shared-fate, so gating any k8s probe on it would fail every replica at once on a transient
  blip - liveness restart-storms, readiness pulls the whole Service (a degradation becomes an outage).
  Dependency reachability is surfaced on the deep `healthcheck` layer for monitoring/mesh instead. A
  developer can still add a dependency check to readiness explicitly if they've reasoned it safe.

### Execution
- `IHealthCheckProcessor`/`HealthCheckProcessor` - the injectable execution engine (was a static class).
  Runs every check wrapped in `TimeOutHealthCheck(ExceptionHandlingHealthCheck(check))` and aggregates
  into a `HealthCheckResponse`. The per-check timeout is **configurable** via the constructor
  (`new HealthCheckProcessor(TimeSpan)`, default 10s); the middleware resolves `IHealthCheckProcessor`
  from DI, registered by the builder with `TryAddSingleton` so a consumer can register their own
  (e.g. a different timeout) and have it win. Each check is **timed** and its duration stamped onto the
  result (`IHealthCheckResult.Duration`). A static `PerformHealthChecksAsync(topic, checks)` shim
  remains for source-compatibility (the `topic` arg is unused). **Per-check overrides are honoured:** a
  check's `IHealthCheck.Timeout` replaces the processor-wide timeout for that check, and a non-critical
  check (`IsNonCritical == true`) has a `Failed` result downgraded to `Warning` so it never flips the
  probe unhealthy (§3.4) — **except a persistent `Failed`** (`IHealthCheckResult.IsPersistent`, which
  `HealthCheckError.Classify` sets for an authorization/permission denial), which is exempt from the
  downgrade and stays `Failed` even for a non-critical dependency check, so a deterministic IAM
  misconfiguration surfaces as unhealthy rather than being softened to a Warning (§3.9).
- `CachingHealthCheckProcessor` - an opt-in `IHealthCheckProcessor` decorator that caches the
  aggregated result for a TTL (keyed by the set of check `Type`s, so liveness/readiness cache
  independently), so a probe polling every few seconds doesn't re-run every check and re-hit every
  dependency. Register it as the `IHealthCheckProcessor` wrapping a `HealthCheckProcessor`; do NOT use
  it for a liveness probe that must reflect the instant state.
  Each check's `Dependencies` (see `Benzene.HealthChecks.Core`) survives this aggregation - the
  processor explicitly rebuilds a `HealthCheckResult` per check, and threads `Dependencies` through
  that rebuild alongside `Status`/`Type`/`Data`. Known limitation: if `TimeOutHealthCheck`/
  `ExceptionHandlingHealthCheck` themselves have to synthesize a fallback result (a hard timeout or an
  unhandled exception from the inner check), that fallback result has no `Dependencies` - there's no
  result to read them from once the inner check hasn't returned one. A check's own internal
  timeout/failure handling (e.g. `SqsHealthCheck`'s own 10s send timeout, distinct from this outer
  wrapper) does not have this limitation, since the check still constructs its own result.
- `TimeOutHealthCheck` - enforces the 10s timeout via `Task.WhenAny`; note the inner check keeps
  running in the background after a timeout is reported (it isn't cancelled)
- `ExceptionHandlingHealthCheck` - catches any exception from the wrapped check and reports it as a
  failed result instead of throwing; an `OperationCanceledException` is reported distinctly as
  `Error=Cancelled` (a cancelled/shutting-down check is not a broken dependency).
- **Cancellation** - a scoped `ICancellationTokenAccessor` (`Benzene.Abstractions.DI`, default impl
  `Benzene.Core.CancellationTokenAccessor`) is registered by the builder so any check can resolve it
  and observe the ambient token (e.g. `HttpPingHealthCheck` passes it to `GetAsync`). The pipeline
  does not thread a `CancellationToken` (a repo-wide convention - it rides on scoped accessors, like
  gRPC's `IGrpcServerCallAccessor`). The accessor is registered scoped in `AddBenzene` (universally
  available); `Benzene.AspNet.Core`'s `UseHttp` seeds it from `HttpContext.RequestAborted` per request.
  Other transports (workers, Lambda) can seed it the same way; until a given transport does, it
  defaults to `None`.
- `FailedHealthCheck`/`InlineHealthCheck`/`SimpleHealthCheck` - small `IHealthCheck` helpers (a
  fixed-failure stub, a func-backed wrapper, and an always-healthy default, respectively)
- `MemoryHealthCheck` (`AddMemoryCheck(maximumBytes, warningBytes?)`) - a host self-check on the
  process working set (`Environment.WorkingSet`, the physical memory a container/host OOM-killer
  watches); `Failed` at/above the maximum, optional `Warning` at/above a soft ceiling, else `Ok`.
  Includes managed-heap size and the GC-reported memory limit in the result data. Lives here rather
  than in a dedicated package (unlike `Benzene.HealthChecks.Disk`/`.Tcp`) because it has no external
  dependency - just `Environment`/`GC`. Covered by
  `test/Benzene.Core.Test/HealthChecks/MemoryHealthCheckTest.cs`.
- `ShutdownState` + `ShutdownReadinessHealthCheck` (`AddShutdownReadinessCheck(shutdownState)`) -
  graceful-shutdown drain support. `ShutdownState` is a one-way latch; `LinkTo(CancellationToken)`
  trips it from the host's shutdown signal (e.g. `IHostApplicationLifetime.ApplicationStopping`, a
  worker stop token). The check returns `Failed` once the latch is set, so a **readiness** probe
  (`.UseReadinessCheck(...)`) flips to unhealthy during shutdown and a load balancer / Kubernetes
  stops routing new traffic while in-flight work drains — **liveness** must NOT use it (a draining
  instance is healthy, not broken). Dependency-free (BCL `CancellationToken` only), caller-owned
  state (no DI registration needed). Covered by
  `test/Benzene.Core.Test/HealthChecks/ShutdownReadinessHealthCheckTest.cs`.
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
- Every check gets a configurable timeout (default 10s) and exception isolation automatically;
  individual `IHealthCheck` implementations don't need to implement either themselves
- Checks run **concurrently** (one `Task.WhenAll`), each resolved from the same per-request
  `IServiceResolver` scope. A check must therefore not share mutable scoped state with another check -
  most notably, don't have two checks resolve and use the same scoped, non-thread-safe EF `DbContext`
  concurrently. Register a distinct context/resource per check, or make the check self-contained.
  (`IServiceResolver` has no child-scope API, so the engine can't isolate each check in its own scope.)
- `.UseHealthCheck(topic, ...)` responds to `Constants.DefaultHealthCheckTopic` ("healthcheck") in
  addition to whatever topic is passed; `.UseLivenessCheck`/`.UseReadinessCheck` do not
- `HealthCheckProcessor.PerformHealthChecksAsync` maps the aggregate result to
  `BenzeneResultStatus.Ok`/`ServiceUnavailable` (HTTP 200/503 via the standard status code mapper),
  not just an `isHealthy` body field - this matters for any consumer (Kubernetes probes, load
  balancer target-health checks) that only inspects the status code
