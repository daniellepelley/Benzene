# Benzene.HealthChecks.Http

## What this package does
HTTP endpoint health check implementation for Benzene. Provides health checks for external HTTP dependencies, verifying availability of downstream services and APIs.

## Key types/interfaces

### HTTP Health Checks
- HTTP endpoint connectivity check
- Response status validation
- Timeout and retry support

## When to use this package
- When verifying downstream HTTP services
- For checking external API availability
- To monitor service dependencies
- For startup dependency validation

## Dependencies on other Benzene packages
- **Benzene.HealthChecks.Core** - Health check core

## Important conventions
- Checks HTTP endpoint availability
- Validates response status codes
- Configurable timeout
- Can check multiple endpoints
