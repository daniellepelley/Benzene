# Benzene.HealthChecks.Core

## What this package does
Core health check abstractions: the `IHealthCheck` contract, its result/response types, and the
builder interface used to register a set of checks. Pure abstractions/simple implementations only -
no execution engine (timeout/exception handling, message-handler wiring), no HTTP endpoint, and no
dedicated readiness/liveness concept; those live in `Benzene.HealthChecks` (execution + message-topic
middleware) and whatever HTTP transport package maps a result to an HTTP response.

## Key types/interfaces
- `IHealthCheck` - `Type` (the check's identifier) + `ExecuteAsync()` returning an `IHealthCheckResult`,
  plus four **default interface members** (source/binary-compatible for the 20+ existing implementers,
  which keep the defaults) so a check can describe its own routing/criticality/cost instead of having
  them decided processor-wide: `Tags` (open-string labels for filtering/MEL predicate mapping; **not**
  the probe-separation axis - that's topic + the readiness category), `IsNonCritical` (default `false`;
  `true` downgrades a `Failed` to `Warning` during aggregation so a non-critical dep degrades rather
  than de-services - §3.4), `Ttl` (per-check cache hint; reserved - the current caching processor
  caches the aggregate, not per check), `Timeout` (per-check timeout overriding the processor-wide one).
  `HealthCheckProcessor` honours `IsNonCritical` and `Timeout`. **Polarity note:** the criticality flag
  is `IsNonCritical` (default false = critical), *not* `IsCritical` (default true), so it **fails safe** -
  any Moq/DI proxy over `IHealthCheck` returns `default(bool)` == false == critical, rather than
  silently turning a failing dependency non-fatal. (Deviation from design §3.5's `IsCritical => true`,
  made because that polarity is fail-open and breaks every `Mock<IHealthCheck>` in the suite.)
- `IDependencyHealthCheck : IHealthCheck` - the **dependency registration category** (§3.2): a DI
  service-type marker (not a context marker) that auto-wired client dependency checks register under.
  These are harvested by the general (`healthcheck`) probe **only** - never `liveness`, `readiness` or
  `contracts`. The reasoning is shared-fate: every replica runs the same check against the same
  downstream, so gating a Kubernetes probe on it would fail all replicas at once on a transient blip -
  liveness would restart-storm the fleet, readiness would pull every pod from the Service (zero
  endpoints, a total outage from a mere degradation). So dependency reachability lives on the deep
  `healthcheck` layer (monitoring / mesh / humans), which triggers no automated k8s action. Carries a
  `DedupKey` (default `Type`) to collapse duplicate registrations. The wrapper `DependencyHealthCheck`
  and the registration seam `AddDependencyHealthCheck` live **here in Core** (not the middleware
  package), so a client package can auto-wire using only its existing lightweight `.Core` reference.
  **The category is non-critical by default:** `DependencyHealthCheck` forces `IsNonCritical => true`, so
  a failing auto-wired check *degrades* the deep report to a `Warning` (still visible per-dependency)
  rather than flipping the aggregate unhealthy / the endpoint to 503 — that layer is monitoring, not a
  probe, so a downstream blip must not turn it red, and it keeps a healthcheck endpoint green in
  integration tests where the real dependency isn't reachable. **Exception (§3.9):** a **persistent**
  failure (`IHealthCheckResult.IsPersistent`, which `HealthCheckError.Classify` sets for an
  authorization/permission denial) is *exempt* from this downgrade and stays `Failed` (unhealthy) even
  here — a missing IAM permission / bad credential is a deterministic misconfiguration that won't
  self-heal, so it must show red. This is safe *because the deep `healthcheck` layer is advisory*:
  `liveness`/`readiness` exclude dependency checks (harvest matrix above), so a dependency red — the same
  status the Mesh UI renders — can never de-service a pod or de-register a load balancer target; it tells a
  human the estate is not wired up as expected. So the red is a true signal, not a false alarm to suppress.
  (`healthCheck: false` stops probing a dependency you don't want monitored at all — not a workaround for the
  advisory red.) A caller who wants a dependency to be fatal on a *probe* adds an explicit critical check.
- `IHealthCheckResult`/`HealthCheckResult` - `Status`, `Type`, `Data` (arbitrary diagnostic dictionary),
  `Dependencies` (structured `HealthCheckDependency[]` describing the external resources the check
  verifies - e.g. a specific queue, database, or downstream service; a default interface member on
  `IHealthCheckResult`, defaulting to empty, so existing implementers stay source/binary compatible);
  `HealthCheckResult` supplies `CreateInstance`/`CreateWarning` factory methods for the common cases,
  each with a `Dependencies`-accepting overload
- `HealthCheckDependency` - `Kind` (an open string category, e.g. `"Queue"`/`"Database"`/`"Http"`/
  `"Lambda"`/`"StateMachine"`/`"Cache"` - not an enum, so new dependency kinds don't require a shared
  type) + `Name` (the specific resource identifier - never a connection string or other secret)
- `HealthCheckStatus` - the three actual status string constants: `Ok` ("ok"), `Warning` ("warning"),
  `Failed` ("failed") - not "Healthy/Degraded/Unhealthy"
- `IHealthCheckResponse<T>`/`HealthCheckResponse` - the aggregated `IsHealthy` + per-check results
- `IHealthCheckBuilder`/`HealthCheckBuilderExtensions` - registration surface for a set of checks
  (DI-resolved type, resolver-function, instance, or `IHealthCheckFactory`). `GetHealthChecks` has a
  scope-aware overload `GetHealthChecks(resolver, bool includeDependencyChecks)` (a DIM defaulting to the
  include-everything behaviour) so a liveness/readiness harvest can drop the dependency-category checks.
- `IHealthCheckFactory` - builds a check from constructor arguments not themselves resolved from DI
  (e.g. a URL, a target migration name) - see `Benzene.HealthChecks.Http`/`.EntityFramework`'s factories
- `HealthCheckError` - the shared failure-classification policy (§3.4/§3.9). `Classify(type, exception,
  dependencies, errorCode?, statusCode?, data?)` returns a **persistent** `Failed`
  (`IHealthCheckResult.IsPersistent`) for an authorization/permission denial — detected by *meaning*
  (HTTP 401/403 **or** a known authorization error code, via `IsAuthorizationFailure`, so an
  `AccessDeniedException` surfaced as HTTP 400 still classifies) — and a transient `Failed` otherwise,
  enriching `Data` with the exception **type** plus the SDK's `ErrorCode`/`StatusCode` when supplied -
  **never the exception message** (secret-safety). The persistent flag makes the authorization case
  escape the non-critical downgrade (`HealthCheckProcessor`), so a real IAM break shows unhealthy even
  for an auto-wired dependency check rather than being softened to a Warning (a reversal of the earlier
  §3.9 rule, which made a permission error a Warning so a least-privilege publisher stayed green).
  Cloud-SDK exception types (`AmazonServiceException`, Azure's `RequestFailedException`) are extracted by the
  *caller* in its own client package and passed in, so this core helper stays cloud-agnostic.

## When to use this package
- Implementing a custom `IHealthCheck` for a new dependency type
- Referenced transitively by any package that registers or runs health checks - rarely used standalone

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions

## Important conventions
- `Warning` is a real, distinct status from `Failed` - whether it counts as "healthy" is a decision
  made by the aggregator (`Benzene.HealthChecks.HealthCheckProcessor`), not by this package
- No timeout/exception handling is implemented here - see `Benzene.HealthChecks`'s
  `TimeOutHealthCheck`/`ExceptionHandlingHealthCheck` decorators for that
