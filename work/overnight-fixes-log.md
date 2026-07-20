# Overnight bug-hunt-and-fix log (2026-07-20 → morning)

Autonomous loop: find real correctness bug → reproduce with a failing test → fix → full build+test →
commit+push to main. Adversarial verification: no fix ships without a test that fails before and
passes after. Staying clear of the actively-churning #29/#30 cloud series (Aws/Azure/Kafka/Grpc/
Clients/RabbitMq/SelfHost.Http) to avoid collisions with other sessions.

## Cycle log

(newest first)

### Cycle 3 — BenzeneMessage bypassed the configurable version getter (`BenzeneMessageGetter.GetTopic`)
- **Bug:** `GetTopic` baked the raw `"version"` header into the topic version. `MessageRouter` treats a
  topic-getter version as a deliberate preset override and skips `IMessageVersionGetter`, so BenzeneMessage
  never used the configurable, priority-ordered header getter (default `benzene-version` > `version` >
  `x-version`). A message with both `benzene-version` and `version` routed to the wrong handler version,
  and an app that narrows the header list (docs/specification/versioning.md §2.1) was silently defeated.
  Inconsistent with every other transport's topic getter (SQS/SNS return version-less topics).
- **Repro:** `BenzeneMessageVersionRoutingTest.BenzeneVersionHeaderWinsOverVersionHeader` — routed to `1`
  (version header) pre-fix, routes to `2` (benzene-version) post-fix. Single-`version`-header and no-header
  cases unchanged (also tested).
- **Fix:** `GetTopic` returns a version-less `Topic(id)`, deferring version resolution to the router's
  version getter. Core suite (1850) + conformance (129) green.

### Cycle 2 — example/test-payload camelCase diverges from the wire for acronym names (`ExamplePayloadBuilder`)
- **Bug:** `ExamplePayloadBuilder.CamelCase` lowercased the whole leading run of capitals, so `IPAddress`
  → `ipaddress`, but the service deserializes with STJ `JsonNamingPolicy.CamelCase` which yields
  `ipAddress` (keeps the capital before a lowercase). The generated example/test payload — documented as
  "the exact shape a caller POSTs" — had keys that bind to null against its own service. Hits any
  acronym-prefixed property (`IPAddress`, `IOStream`, `URLPath`, …). Simple names were unaffected, so
  golden-file tests didn't catch it.
- **Repro:** `Build_AcronymPrefixedProperty_UsesSameCamelCaseAsTheRuntimeSerializer` — failed pre-fix
  (`ipaddress`), passes post-fix.
- **Fix:** replaced the hand-rolled logic with `JsonNamingPolicy.CamelCase.ConvertName` (exactly the
  runtime serializer's policy). 54 example/test-payload/spec tests pass, incl. all golden files.
- **Noted (not fixed):** `CodeGen.Core/CodeGenHelpers.Camelcase` has the identical algorithm but is
  unused by any production path (only its own unit test references it) — no active wire-key bug, left
  alone.

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
