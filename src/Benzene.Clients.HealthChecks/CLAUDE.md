# Benzene.Clients.HealthChecks

## What this package does
Health check client implementation for Benzene services. Provides clients for checking health of remote Benzene services, useful for service monitoring and health check aggregation.

## Key types/interfaces

### Health Check Client
- Client for calling health endpoints
- Health status parsing
- Remote service health validation

## When to use this package
- When checking health of remote services
- For health check aggregation
- For monitoring distributed systems
- For startup dependency validation

## Dependencies on other Benzene packages
- **Benzene.Clients** - Client abstractions
- **Benzene.HealthChecks.Core** - Health check types

## Important conventions
- Calls /health endpoints
- Parses health check responses
- Timeout support
- Works with standard health check format
