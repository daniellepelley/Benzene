# Overnight bug-hunt-and-fix log (2026-07-20 → morning)

Autonomous loop: find real correctness bug → reproduce with a failing test → fix → full build+test →
commit+push to main. Adversarial verification: no fix ships without a test that fails before and
passes after. Staying clear of the actively-churning #29/#30 cloud series (Aws/Azure/Kafka/Grpc/
Clients/RabbitMq/SelfHost.Http) to avoid collisions with other sessions.

## Deferred / noted (not fixed — need a decision, a real repro, or are intentional)
- **`Utils.GetTypes` swallows `ReflectionTypeLoadException`** (3 copies: `Benzene.Core.MessageHandlers/
  Utils.cs`, `.../Helper/Utils.cs`, `Benzene.Core/Helper/Utils.cs`) — `catch { return Type.EmptyTypes; }`
  drops ALL of an assembly's types if one type fails to load, making every handler in it undiscoverable.
  A `ReflectionTypeLoadException`-specific catch returning `ex.Types.Where(t => t != null)` is strictly
  better, but a clean failing test needs a real partially-loadable assembly (hard to synthesize), and it
  may be an intentional defensive default — left for a maintainer call.
- **`VersionSelector` ordinal fallback** (`"9" > "10"` lexicographically) — DOCUMENTED and intentional
  (deterministic, culture-independent; versions are opaque strings). Not a bug.
- **`CodeGenHelpers.Camelcase`** has the same acronym-lowercasing quirk fixed in `ExamplePayloadBuilder`,
  but is unused by any production path (only its own unit test) — no active bug.

## Cycle log

(newest first)

### Cycle 7 — HTTP route parameter values were lowercased (`UrlMatcher.SplitPath` / `CompiledRoutePath`)
- **Bug:** `SplitPath` lowercased the whole incoming path (`.ToLowerInvariant()`) so literal matching
  could compare both sides folded. But the same lowercased segments were the source of extracted
  parameter values, so `/users/JohnDoe` on `/users/{id}` handed the handler `id = "johndoe"` — corrupting
  case-sensitive ids, slugs, base64/hex tokens, and uppercase GUIDs against case-sensitive stores.
- **Repro:** two new `FindWithParameters_ValueOverlapsSegmentLiteral` cases (`JohnDoe`, `AbC-123`) —
  returned lowercased pre-fix, verbatim post-fix.
- **Fix:** `SplitPath` no longer folds case; `CompiledRoutePath.Match` compares literals/prefix/suffix
  with `StringComparison.OrdinalIgnoreCase` against the original-case segment. Matching stays
  case-insensitive (identical behavior); only the extracted value is now preserved. 137 route/HTTP/
  CORS/pipeline tests green.

### Cycle 6 — route with a literal prefix/suffix matched an empty param value (`CompiledRoutePath`)
- **Bug:** for a parameter with a literal prefix and/or suffix (`/example-{id}-foo`, `/x{id}`,
  `/{id}-foo`), a URL that supplied no value for the parameter (`/example--foo`, `/x`, `/-foo`) still
  matched — the empty extracted value hit a `continue` that skipped adding the param but let the segment
  count as a match. The handler was then dispatched with the required route parameter absent (bound to
  null/default), instead of a 404.
- **Repro:** three new `UrlMatcherTest.DoesNotMatchPath` cases — returned an empty dict pre-fix, return
  null post-fix. All existing match/no-match cases unchanged (49 route tests green).
- **Fix:** `return null` on an empty extracted value (a required param with no value is not a match).
  `RouteFinder` and `UrlMatcher.MatchUrl` both go through `CompiledRoutePath.Match`, so both are fixed.

### Cycle 5 — XML responses declared `encoding="utf-16"` but shipped as UTF-8 (`Benzene.Xml.XmlSerializer`)
- **Bug:** `Serialize` wrote via a `StringWriter` (always UTF-16), so `XmlWriter` stamped
  `<?xml ... encoding="utf-16"?>` into the declaration. The body is returned as a string and transmitted
  as UTF-8 like every other body, so the declaration contradicted the actual bytes; a conformant XML
  client honoring the declaration fails to parse the response ("no Unicode byte order mark"). Benzene's
  own string round-trip masked it (Deserialize reads chars from a StringReader, ignoring the declaration).
- **Repro:** `Serialize_DeclaresUtf8_SoTheUtf8WireBytesParse` — pre-fix the declaration said utf-16 and
  `XmlDocument.Load` over the UTF-8 bytes threw; post-fix it declares utf-8 and parses.
- **Fix:** a `Utf8StringWriter : StringWriter` overriding `Encoding => Encoding.UTF8`, so the declaration
  matches the UTF-8 wire bytes. Existing round-trip/caching/null tests unchanged. Core suite green.

### Cycle 4 — spec build crashes on a validation rule for a non-schema member (`OpenApiValidationSchemaBuilder`)
- **Bug:** `schema.Properties[validationSchema.Key]` (unguarded indexer) threw `KeyNotFoundException`
  when a FluentValidation `RuleFor` targeted a member that isn't a serialized schema property (e.g. a
  `[JsonIgnore]` property, or a rule keyed on a non-property member). That failed the ENTIRE spec build
  (a 500 on the `spec` endpoint), not just that one rule.
- **Repro:** `AddSchema_ValidationRuleForAMemberNotInTheSchema_DoesNotThrow` (+ mixed real/ghost case) —
  threw pre-fix, passes post-fix; happy-path decoration test confirms real keys still apply.
- **Fix:** `TryGetValue(key, out property)` + `continue` on miss. Full core suite green.

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
