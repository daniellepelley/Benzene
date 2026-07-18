# Benzene.Cache.Core

## What this package does
Provider-agnostic caching abstractions plus a cache-aside / write-through base-class layering that
concrete providers (e.g. `Benzene.Cache.Redis`) extend. This package contains **no cache middleware
and no HTTP response caching** — it is a set of interfaces + abstract base classes your handler code
calls directly, plus a health check. Values are JSON-serialized via the shared
`Benzene.Core.MessageHandlers.Serialization.JsonSerializer`.

## Key types/interfaces
- `ICacheService` - marker service exposing only `CanConnectAsync()` (a provider connection is
  supplied by the concrete implementation, e.g. `RedisCacheService`)
- `ICacheEntry<T>` / `ICacheWriteActions<T>` / `ICacheInvalidateActions` - the read/write/invalidate
  contracts a concrete single-/multi-key cache entry implements
- `CacheUpdateAction` enum - `None` / `Set` / `Invalidate`
- `CacheHealthCheck<TCacheService>` + `CacheHealthCheckFactory<TCacheService>` and the
  `IHealthCheckBuilder.AddCacheHealthCheck<TCacheService>()` extension

## When to use this package
- When implementing a cache provider (subclass the `CacheEntry<T>` layering)
- When your handler needs cache-aside (`LazyLoadAsync`) or write-through (`WriteThroughAsync`) around
  a database read/modify

## Deliberate boundaries (NOT shipped)
- **No stampede / single-flight / dogpile protection.** `CacheEntry<T>.LazyLoadAsync` is a plain
  cache-aside: on a miss every concurrent caller runs `databaseReadFunc` and writes back. There is
  no lock or in-flight coalescing (see `CacheEntry.cs`).
- No cache middleware, no automatic key generation, no HTTP response caching.

## Dependencies on other Benzene packages
- **Benzene.Core.MessageHandlers** - the shared JSON `ISerializer`
- **Benzene.Core** / **Benzene.Results** / **Benzene.Abstractions.Results** - `IBenzeneResult`/status
- **Benzene.Diagnostics** - `IProcessTimerFactory` timing scopes
- **Benzene.HealthChecks.Core** - health-check contracts

## Important conventions
- Read failures degrade to a miss: `CacheEntry<T>.GetValueAsync` swallows and logs a read exception
  rather than propagating, so a cache outage doesn't fail the request.
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
