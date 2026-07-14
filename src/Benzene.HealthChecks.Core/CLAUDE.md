# Benzene.HealthChecks.Core

## What this package does
Core health check abstractions: the `IHealthCheck` contract, its result/response types, and the
builder interface used to register a set of checks. Pure abstractions/simple implementations only -
no execution engine (timeout/exception handling, message-handler wiring), no HTTP endpoint, and no
dedicated readiness/liveness concept; those live in `Benzene.HealthChecks` (execution + message-topic
middleware) and whatever HTTP transport package maps a result to an HTTP response.

## Key types/interfaces
- `IHealthCheck` - `Type` (the check's identifier) + `ExecuteAsync()` returning an `IHealthCheckResult`
- `IHealthCheckResult`/`HealthCheckResult` - `Status`, `Type`, `Data` (arbitrary diagnostic dictionary);
  `HealthCheckResult` supplies `CreateInstance`/`CreateWarning` factory methods for the common cases
- `HealthCheckStatus` - the three actual status string constants: `Ok` ("ok"), `Warning` ("warning"),
  `Failed` ("failed") - not "Healthy/Degraded/Unhealthy"
- `IHealthCheckResponse<T>`/`HealthCheckResponse` - the aggregated `IsHealthy` + per-check results
- `IHealthCheckBuilder`/`HealthCheckBuilderExtensions` - registration surface for a set of checks
  (DI-resolved type, resolver-function, instance, or `IHealthCheckFactory`)
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
