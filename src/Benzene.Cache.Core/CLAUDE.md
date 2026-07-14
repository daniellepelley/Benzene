# Benzene.Cache.Core

## What this package does
Core caching abstractions and implementations for Benzene. Provides cache interface, cache middleware, and utilities for caching message handler results and HTTP responses.

## Key types/interfaces

### Cache Infrastructure
- `ICache<T>` - Cache interface
- Cache middleware
- Cache key generation
- TTL and expiration support

## When to use this package
- When implementing caching in Benzene
- For caching message handler results
- For HTTP response caching
- Foundation for cache providers

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions

## Important conventions
- Generic cache interface
- TTL-based expiration
- Key generation strategies
- Middleware for automatic caching
- Provider-agnostic
- `CacheHealthCheck<TCacheService>` - an `IHealthCheck` verifying `ICacheService.CanConnectAsync()`;
  result `Data` includes `CanConnect` and `Error` (the exception's type name, not its message - not a
  connection string or other secret); result `Dependencies` includes one
  `HealthCheckDependency("Cache", typeof(TCacheService).Name)`
