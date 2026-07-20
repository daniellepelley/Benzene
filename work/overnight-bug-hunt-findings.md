# Overnight bug hunt — findings ledger

Fan-out of read-only hunters across subsystems; each candidate verified with a failing test locally
before any fix (dotnet 10 available; Docker not). Status key: ✅ fixed+tested+committed · 🔧 to fix ·
🔎 verify-carefully (risky/core) · 🚩 flag for maintainer (design/contract call, not fixed unilaterally).

## Fixed (12 — each with a regression test, full Benzene.Core.Test suite green: 1548 passed)
- ✅ **`As<TOutput>()` flipped success→failure** (High). Overload-resolution accident bound a projected
  success to the failure constructor (IsSuccessful=false → 200 with error body, misread by
  saga/idempotency/batch escalation).
- ✅ **SQS partial-batch-failure `List` data race** (High). Shared `List.Add` from concurrent WhenAll
  continuations → dropped failure ids → silent message loss. Now return-from-task + build after WhenAll.
- ✅ **NewtonsoftJson camel-cased dictionary KEYS** (High). `ProcessDictionaryKeys=true` corrupted
  free-form keys and the round-trip never restored them. Now property names only.
- ✅ **Versioning: nullability change dropped value** (High). `int↔int?`, enum↔nullable-enum fell to
  class-mapping → `default`. Added a lifted-conversion branch.
- ✅ **Versioning: array-element-type change threw at startup** (Medium). Arrays excluded from the
  enumerable path. Now handled (GetElementType + ToArray).
- ✅ **W3C `isRemote:false`** (Medium). Inbound remote parent parsed as local → mis-sampling per hop.
- ✅ **UseBenzeneMetrics dropped metrics on exception** (Medium). No try/finally → throwing requests
  never counted. Now recorded in finally, result=failure on throw.
- ✅ **SNS null MessageAttributes NRE** (Medium). Headers path made null-safe like the topic path.
- ✅ **HealthCheckNamer duplicate name → whole probe 500** (Medium). Generated name now reserved.
- ✅ **IsDoubleGuid → wrong OpenAPI schema** (Medium). Copy-paste of the IsNumeric case.
- ✅ **VersionSelector culture-sensitive fallback** (Low). Now `StringComparer.Ordinal`.
- ✅ **Saga step leaked an earlier attempt's exception across retries** (Low). Reset per run.

## Remaining — clean but not done this pass (need heavier real-server/adapter test infra)
- 🔧 **SelfHost `RawUrl` in `HttpRequest.Path`** (Medium-High) — query leaks into Path; one-line fix
  (`AbsolutePath`) but needs an HttpListener test harness.
- 🔧 **AspNetResponseAdapter `Headers.Add` throws on dup key / writes body on 204** (Medium/Low) —
  AspNet adapter tests are commented out; needs that harness revived.
- 🔧 **Cache Redis KEYS glob injection** (Medium) — escape metacharacters.
- 🔧 **Idempotency key delimiter injection** (Low) — length-prefix the hash input.
- 🔧 **UrlMatcher.RemoveParts global Replace** (Low-Med) — routing; deferred to avoid a routing regression.
- 🔧 **ContextDictionaryBuilder duplicate keys throw** (Low) — last-wins.
- 🔧 **Avro Dictionary round-trip / uint overflow** (Medium) — Avro map schema + wider unsigned mapping.

## Verify carefully (risky / core semantics — left for review; fix only with strong tests)
- 🔎 **MiddlewarePipeline eager middleware instantiation** (High claim, `MiddlewarePipeline.cs:43`).
  Claims all middleware constructed up-front → breaks short-circuit, `IBenzeneInvocation` ctor-injection,
  exception-handler coverage. Verify with a test before changing the hot path.
- 🔎 **Route precedence: `{param}` can shadow a literal route** (Medium-High, `RouteFinder.cs`). Verify;
  fix = sort by specificity (behavior change).
- 🔎 **AddMessageHandlers `TryAddSingleton` finder lock-in** (Medium, `Core.MessageHandlers/DI/Extensions.cs`).
  no-arg then typed overload → typed finder dropped → 404s. Verify + fix registration.
- 🔎 **HTTP CORS headers set after response finalized on real servers** (High, `CorsMiddleware.cs:63`).
  Works on buffered API Gateway, throws on ASP.NET/self-host. Verify with a self-host test.
- 🔎 **Cache value-type `T` miss-as-hit** (High-if-used/latent, `CacheEntry.cs:32`). `default(T)!=null`
  always true for value types → DB never consulted. Fix needs `where T:class` or presence tracking.
- 🔎 **MiddlewareRouter value-type request null-check always false** (Low, latent). Constrain `T:class`.

## Flag for maintainer (design/contract calls — not fixing unilaterally)
- 🚩 **Outbound SQS/SNS return `Ok`; the other 5 fire-and-forget transports return `Accepted`**
  (Medium inconsistency). Breaks a transport-agnostic `IsOk()`/`IsAccepted()` check. Which side is
  canonical is a contract decision.
- 🚩 **Cache null-payload penetration** (Medium). Success-with-null never effectively cached → DB hit
  every call. Negative-caching policy is a decision.
- 🚩 **Versioning: unknown incoming version deserialized as canonical** (Medium). Passthrough vs
  fail-fast for a known-versioned topic is a decision.

## Test-infra note (not a product bug)
- The W3C trace-context test classes (`W3CTraceContextTest`, `EventHubW3CTraceContextTest`,
  `KafkaW3CTraceContextTest`) each attach a process-global `ActivityListener` to the "Benzene"
  source and use `Assert.Single(activities, …)`. Under parallel class execution they can capture
  each other's activities and flake. Not observed in a normal full-suite run (only when forced
  concurrent via a filter), but worth isolating each to a unique ActivitySource or a shared
  non-parallel collection.

## Auth
- No security/bypass/privilege-escalation bug found — auth is solid. One low, fail-closed asymmetry:
  the `scope` claim isn't JSON-array-parsed while `scp` is (wrongly denies, never wrongly grants).

## Second pass (follow-up) — 12 more fixed, remainder are API/design decisions

Fixed this pass (each reproduced with a failing test first, then fixed; full suite green: 1577):
- ✅ **Middleware pipeline resolved every middleware up front** (High). Deferred DI resolution into the
  chain closure, so a short-circuited/never-reached middleware isn't constructed and UseExceptionHandler
  can cover a downstream construction failure. (`MiddlewarePipeline`)
- ✅ **Route precedence: a `{param}` route could shadow a literal** (Medium-High). Order routes by
  ascending parameter-segment count. (`RouteFinder`)
- ✅ **UrlMatcher corrupted a param value overlapping a segment literal** (Low-Med). Position-anchored
  extraction instead of global String.Replace.
- ✅ **CORS headers set after the response was finalized on real servers** (High). Set them before next().
- ✅ **Self-host put the query string in HttpRequest.Path** (Medium-High). Use Url.AbsolutePath.
- ✅ **AspNet adapter threw on a duplicate header + wrote a body on 204** (Medium/Low). Append + 204 guard.
- ✅ **Redis prefix invalidation didn't escape glob metacharacters** (Medium).
- ✅ **Idempotency body-hash key could collide across distinct topic triples** (Low). Length-prefix.
- ✅ **Log-scope build threw on a duplicate context key** (Low). Last-wins.
- ✅ **Avro overflowed on uint > int.MaxValue** (Medium). Map uint to Avro long.
- ✅ **test:** stopped the W3C trace-context tests flaking under parallel execution (shared collection).

### Verified reproducible but NOT fixed (needs a design decision / public-API change — flagged)
- 🚩 **AddMessageHandlers `TryAddSingleton` finder lock-in** (Medium). Confirmed reproducible, but a
  naive fix (aggregate both finders) surfaces the test assembly's **duplicate topics** that the current
  cross-finder dedup silently absorbs, breaking 50 tests. The no-arg overload's own XML doc says it's
  *deliberately* reflection-free, so aggregating the two is a design change (dedup semantics), not a
  drop-in. Reverted; needs maintainer input.
- 🚩 **Cache value-type `T` miss-as-hit** (High-if-used, latent). Fix needs `where T : class` (a
  source-breaking public-API change) or presence-tracking (an interface change). No in-repo caller uses
  a value-type payload today. Left for a maintainer API decision.
- 🚩 **MiddlewareRouter value-type request null-check always false** (Low, latent). Needs
  `where TRequest : class` (public-API constraint). No in-repo value-type router. Flagged.
- 🚩 **Avro Dictionary/map round-trip + extreme `ulong` > long.MaxValue** (Medium). Needs a bidirectional
  Avro map-schema change (schema + datum + reverse). Niche serializer; deferred as a scoped follow-up.
- 🚩 (still open, design/contract) Outbound SQS/SNS `Ok` vs siblings' `Accepted`; cache null-payload
  negative-caching policy; versioning unknown-version passthrough; auth `scope`-as-JSON-array asymmetry
  (low, fails-closed).
