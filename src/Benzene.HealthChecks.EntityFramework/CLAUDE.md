# Benzene.HealthChecks.EntityFramework

## What this package does
Entity Framework health check implementation for Benzene. Provides health checks for database connectivity using Entity Framework Core, verifying database availability and query execution.

## Key types/interfaces

### EF Health Checks
- Database connectivity health check
- Query execution health check
- DbContext health check

## When to use this package
- When using Entity Framework Core
- For database health monitoring
- To verify database connectivity
- For startup health validation

## Dependencies on other Benzene packages
- **Benzene.HealthChecks.Core** - Health check core
- **Microsoft.EntityFrameworkCore** - EF Core

## Important conventions
- Checks database connectivity
- Can execute test queries
- Timeout configurable
- Works with any EF Core DbContext
