# Benzene.HealthChecks

## What this package does
Health check endpoint implementation for Benzene. Provides HTTP endpoints for health checks, integrating with Benzene.HealthChecks.Core to expose health status via REST API.

## Key types/interfaces

### Health Check Endpoints
- Health check HTTP endpoint
- Readiness endpoint
- Liveness endpoint
- Health check middleware

## When to use this package
- When exposing health check endpoints
- For Kubernetes health probes
- For load balancer health checks
- For application monitoring

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Http** - HTTP abstractions
- **Benzene.HealthChecks.Core** - Health check core

## Important conventions
- Standard /health endpoint
- Returns 200 OK if healthy
- Returns 503 Service Unavailable if unhealthy
- JSON response with health details
- Separate readiness and liveness endpoints
