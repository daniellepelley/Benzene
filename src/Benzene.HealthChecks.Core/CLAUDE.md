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
- `IReadinessHealthCheck : IHealthCheck` - the **readiness registration category** (§3.2): a DI
  service-type marker (not a context marker) that auto-wired client dependency checks register under,
  so the general (`healthcheck`) and `readiness` probes harvest them but `liveness`/`contracts` don't -
  a downstream blip must never restart the pod. Carries a `DedupKey` (default `Type`) used to collapse
  duplicate registrations of the same dependency. The concrete registration wrapper lives in
  `Benzene.HealthChecks` (`ReadinessHealthCheck` + `AddReadinessHealthCheck`).
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
  scope-aware overload `GetHealthChecks(resolver, bool includeReadinessScoped)` (a DIM defaulting to the
  include-everything behaviour) so a liveness harvest can drop the readiness-category checks.
- `IHealthCheckFactory` - builds a check from constructor arguments not themselves resolved from DI
  (e.g. a URL, a target migration name) - see `Benzene.HealthChecks.Http`/`.EntityFramework`'s factories

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
