# Benzene.Cache.Redis

## What this package does
Redis-backed implementation of the `Benzene.Cache.Core` abstractions, using StackExchange.Redis for
distributed caching shared across instances.

## Key types/interfaces
- `RedisCacheService` - **abstract** `ICacheService`. You subclass it and implement
  `GetConfigurationOptionsAsync()` (returns a StackExchange.Redis `ConfigurationOptions`). Holds a
  lazily-established `IConnectionMultiplexer`; `CanConnectAsync()` issues a `PING`. Factory methods
  build the concrete entry/action types below.
- `RedisCacheEntry<T>` (internal) - `CacheEntry<T>` over a single key. `Get`/`Set`/`Invalidate` map
  to `StringGetAsync` / `StringSetAsync` (with TTL) / `KeyDeleteAsync`.
- `RedisMultiKeyActions<T>` (internal) - write/invalidate the same value across several keys.
- `RedisWildcardActions` (internal) - invalidate by pattern via a `KEYS <pattern>` scan then batched
  `KeyDeleteAsync`.
- `IRedisConnectionFactory` / `RedisConnectionFactory` - the `ConnectionMultiplexer.ConnectAsync`
  seam (overridable for tests).

## When to use this package
- When you need caching shared across multiple instances backed by Redis.

## Deliberate boundaries (NOT shipped)
- **No cluster-specific handling.** The client talks to whatever endpoint(s) the
  `ConfigurationOptions` you supply describe; there is no cluster-aware sharding logic in this
  package.
- **No atomic / conditional operations.** Operations are `StringGet`/`StringSet`/`KeyDelete` (plus a
  `KEYS`-based wildcard delete). There is no `SETNX`/Lua/transaction-based atomicity here — so this
  is not a distributed-lock or single-flight primitive (cache-aside stampede caveat lives in
  `Benzene.Cache.Core`).
- The wildcard invalidation uses `KEYS`, which scans the keyspace — use with care on large Redis
  instances.

## Important conventions
- Configuration is supplied as `ConfigurationOptions` from your `GetConfigurationOptionsAsync()`
  override, **not** as a bare connection string on this package's API.
- `DefaultCacheLifespan` defaults to 5 minutes (override in your subclass); `SetEntryValueAsync`
  applies it as the TTL when no explicit expiry is passed.
- Redis errors on get/set/invalidate are caught and logged (returning a miss / `false`) rather than
  thrown, so a Redis outage degrades gracefully.

## Dependencies on other Benzene packages
- **Benzene.Cache.Core** - the cache abstractions and base-class layering
- **Benzene.Diagnostics** - `IProcessTimerFactory`
- **StackExchange.Redis** - the Redis client
