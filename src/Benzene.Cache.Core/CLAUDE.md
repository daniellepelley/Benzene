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
- `CacheInvalidateActions` / `CacheWriteActions<T>` / `CacheEntry<T>` - the abstract base-class
  layering every concrete cache entry (e.g. `Benzene.Cache.Redis`'s `RedisCacheEntry<T>`) builds
  on, each adding write-through behavior on top of the last: `CacheInvalidateActions` (delete +
  `WriteThroughInvalidateAsync` - invalidate only when `modifyDatabaseFunc`'s result is
  successful) → `CacheWriteActions<T>` (adds `SetValueAsync` (JSON-serializes via the shared
  `Benzene.Core.MessageHandlers.Serialization.JsonSerializer`) + three `WriteThroughAsync`
  overloads, the simplest defaulting the cache action from the result's `BenzeneResultStatus` -
  `Ok`/`Created`/`Accepted`/`Updated` → `Set`, `Deleted` → `Invalidate`, anything else → `None`) →
  `CacheEntry<T>` (adds `GetValueAsync` - swallows and logs a read exception rather than
  propagating, so a cache outage degrades to a miss - and `LazyLoadAsync`, which only writes back
  to the cache on a cache miss whose `databaseReadFunc` result is successful). A concrete subclass
  implements only 4 protected members: `Logger`, `ProcessTimerFactory`, `KeyDescription`, and the
  3 `*EntryAsync` primitives (`Get`/`Set`/`Invalidate`) the layers above call.

## Tests
- `test/Benzene.Core.Test/Cache/CacheHealthCheckTest.cs` - `CacheHealthCheck<TCacheService>`.
- `test/Benzene.Core.Test/Cache/CacheEntryTest.cs` - the `CacheInvalidateActions`/
  `CacheWriteActions<T>`/`CacheEntry<T>` layering, via a `FakeCacheEntry<T>` test double backed by
  an in-memory dictionary (no Redis/network dependency - `Benzene.Cache.Redis`'s
  `RedisCacheEntry<T>` was the only prior concrete subclass and had no dedicated tests either).
  Covers: `GetValueAsync` hit/miss/underlying-read-throws (swallowed, logged, returns default);
  `SetValueAsync`/`InvalidateAsync`; `LazyLoadAsync`'s hit-skips-database-call vs.
  miss-calls-database-and-writes-back-only-on-success branches; all three `WriteThroughAsync`
  overloads (default `BenzeneResultStatus`-derived action mapping for `Ok`/`Deleted`/`NotFound`,
  a custom cache-value mapping, and a custom cache-action mapping); and
  `WriteThroughInvalidateAsync`'s successful-vs-unsuccessful-result branches.
