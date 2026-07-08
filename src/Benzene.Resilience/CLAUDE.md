# Benzene.Resilience

## What this package does
Resilience patterns for Benzene using Polly. Provides middleware for retry, circuit breaker, timeout, and bulkhead patterns. Enables fault-tolerant applications with configurable resilience policies.

## Key types/interfaces

### Resilience Patterns
- Retry middleware with configurable strategies
- Circuit breaker middleware
- Timeout middleware
- Bulkhead isolation middleware
- Polly policy integration

## When to use this package
- When you need retry logic
- For circuit breaker pattern
- When calling unreliable external services
- For timeout enforcement
- For bulkhead isolation

## Dependencies on other Benzene packages
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.Middleware** - Middleware implementations
- **Polly** - Resilience library

## Important conventions
- Configure policies in middleware
- Retry on transient failures
- Circuit breaker prevents cascading failures
- Timeouts prevent hanging requests
- Bulkhead isolates resources
- Policies can be combined
