# Benzene.Cache.Redis

## What this package does
Redis cache implementation for Benzene. Provides distributed caching using Redis, implementing Benzene.Cache.Core abstractions for scalable, shared caching across instances.

## Key types/interfaces

### Redis Cache
- Redis implementation of `ICache<T>`
- Connection string configuration
- Serialization for Redis storage
- Distributed cache support

## When to use this package
- When you need distributed caching
- For multi-instance deployments
- When using Redis infrastructure
- For scalable caching

## Dependencies on other Benzene packages
- **Benzene.Cache.Core** - Cache abstractions
- **StackExchange.Redis** - Redis client

## Important conventions
- Configure Redis connection string
- Serialization to Redis format
- Supports TTL expiration
- Works with Redis cluster
- Atomic operations supported
