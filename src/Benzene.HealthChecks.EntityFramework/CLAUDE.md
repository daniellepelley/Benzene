# Benzene.HealthChecks.EntityFramework

## What this package does
Two `IHealthCheck` implementations for an EF Core `DbContext`: a plain connectivity check, and a
stricter one that also verifies the database's applied migrations. Neither executes an arbitrary test
query - both check connectivity via `DbContext.Database.CanConnectAsync()` and (for the migration
variant) `GetAppliedMigrationsAsync()`.

## Key types/interfaces
- `DatabaseConnectionHealthCheck<TDbContext>` - connectivity only; result `Data` includes `CanConnect`
  and `Error` (the connection exception's message, if any)
- `DatabaseHealthCheck<TDbContext>` - connectivity AND schema: healthy only if the connection succeeds
  AND the configured target migration is the LAST applied migration (not merely present among applied
  migrations) - a database that's reachable but hasn't yet had a newer migration applied (or has a
  newer one than expected) reports unhealthy; result `Data` includes `CanConnect`, `AppliedMigrations`,
  `TargetMigration`, `MigrationMatch` (drives pass/fail), `MigrationContains`, and `Error`
- `DatabaseHealthCheckFactory<TDbContext>` - factory for `DatabaseHealthCheck<TDbContext>`, resolving
  `TDbContext` from DI each time the check runs; no equivalent factory exists for
  `DatabaseConnectionHealthCheck<TDbContext>` today (construct it directly if needed)

## When to use this package
- `DatabaseConnectionHealthCheck` - simple "is the database reachable" check
- `DatabaseHealthCheck` - stricter check for deployments that need to confirm the expected migration
  is actually live before reporting healthy (e.g. after a rollout)

## Dependencies on other Benzene packages
- **Benzene.HealthChecks.Core** - Health check core
- **Microsoft.EntityFrameworkCore** - EF Core

## Important conventions
- No timeout of its own - relies on the aggregator's timeout wrapper if run through
  `Benzene.HealthChecks`, or the `DbContext`'s own command/connection timeout configuration
- Connection failures are caught and reported as a failed result with the exception message in
  `Data["Error"]`, not thrown
