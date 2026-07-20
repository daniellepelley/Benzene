# Overnight bug-hunt-and-fix log (2026-07-20 → morning)

Autonomous loop: find real correctness bug → reproduce with a failing test → fix → full build+test →
commit+push to main. Adversarial verification: no fix ships without a test that fails before and
passes after. Staying clear of the actively-churning #29/#30 cloud series (Aws/Azure/Kafka/Grpc/
Clients/RabbitMq/SelfHost.Http) to avoid collisions with other sessions.

## Cycle log

(newest first)

### Cycle 1 — value-type cache miss-as-hit (`CacheEntry<T>.LazyLoadAsync`)
- **Bug:** for an unconstrained value-type `T`, a cold-cache read returns `default(T)` and `default(T) != null`
  (boxed) is always true, so `LazyLoadAsync` treated the MISS as a hit — returning e.g. `0m`/`Guid.Empty`
  and never reading the database (a permanent silent miss). Reference-type `T` unaffected, so it was latent.
- **Repro:** `LazyLoadAsync_ValueType_CacheMiss_CallsDatabaseFuncAndReturnsTheDbValue` — failed pre-fix
  (DB func never called), passes post-fix.
- **Fix:** `CacheEntry.cs` now reads via `TryReadEntryAsync()` returning `(bool Found, T? Value)` and gates
  the hit on `found && cacheValue is not null` — presence-based, so value-type misses read the DB while
  reference-type behavior (incl. the deferred negative-caching/penetration semantics) is unchanged.
- Full core suite green (1845). Commit: cache fix.
