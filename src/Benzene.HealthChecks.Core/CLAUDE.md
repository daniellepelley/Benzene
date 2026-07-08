# Benzene.HealthChecks.Core

## What this package does
Core health check abstractions and implementations for Benzene. Provides infrastructure for health check endpoints, health status aggregation, and readiness/liveness probes for containerized applications.

## Key types/interfaces

### Health Check Infrastructure
- `IHealthCheck` - Health check interface
- Health check aggregation
- Health status types (Healthy, Degraded, Unhealthy)
- Health check context

## When to use this package
- When implementing health check endpoints
- For Kubernetes readiness/liveness probes
- For monitoring application health
- Foundation for specific health checks

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions

## Important conventions
- Health checks return status and description
- Checks can be aggregated
- Supports degraded state
- Timeout support for checks
- Used by HTTP health check endpoints
