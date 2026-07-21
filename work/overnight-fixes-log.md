# Overnight bug-hunt-and-fix log (2026-07-20 → morning)

Autonomous loop: find real correctness bug → reproduce with a failing test → fix → full build+test →
commit+push to main. Adversarial verification: no fix ships without a test that fails before and
passes after. Staying clear of the actively-churning #29/#30 cloud series (Aws/Azure/Kafka/Grpc/
Clients/RabbitMq/SelfHost.Http) to avoid collisions with other sessions.

**Tally so far: 27 cycles shipped** (each failing-test-first + full-suite green). Coverage swept via
parallel adversarial hunters across Core/Results/Abstractions, serialization/validation/message-handlers,
mesh (collector/aggregator/reporting/tempo/contracts/ui), cache/resilience/DI, HTTP/CORS, CLI/codegen,
observability/serializers, auth/oauth2/versioning, health-checks, idempotency/saga, and cloud-service.
Auth (Auth.Core/OAuth2/Basic) verified security-sound. Remaining candidates are all in the deferred
list below (design decisions, browser-mitigated JS hardening, or memory-model fixes with no
deterministic repro) — nothing left that meets the failing-test-first bar without a maintainer decision.

## Deferred / noted (not fixed — need a decision, a real repro, or are intentional)
- **`Utils.GetTypes` swallows `ReflectionTypeLoadException`** (3 copies: `Benzene.Core.MessageHandlers/
  Utils.cs`, `.../Helper/Utils.cs`, `Benzene.Core/Helper/Utils.cs`) — `catch { return Type.EmptyTypes; }`
  drops ALL of an assembly's types if one type fails to load, making every handler in it undiscoverable.
  A `ReflectionTypeLoadException`-specific catch returning `ex.Types.Where(t => t != null)` is strictly
  better, but a clean failing test needs a real partially-loadable assembly (hard to synthesize), and it
  may be an intentional defensive default — left for a maintainer call.
- **`VersionSelector` ordinal fallback** (`"9" > "10"` lexicographically) — DOCUMENTED and intentional
  (deterministic, culture-independent; versions are opaque strings). Not a bug.
- **`MeshAggregator.BuildTopicEntry` SchemaMismatch folds Response into the compare string** without the
  request-side "no schema ⇒ no signal" guard, so consumer A (`Request=X, Response=null`) vs B (`Request=X,
  Response=Y`) flags a mismatch though inbound payloads are identical. GENUINELY AMBIGUOUS — the property's
  second doc sentence says "request/response", and a mixed one-way/two-way set could be a real divergence.
  Left for a maintainer call (behaviour decision, not a clear bug).
- **`MeshAggregator` structural-edge dedup key** `$"{client} {server}"` can collide if a service name
  contains a space. Service names are normally identifiers → latent/low; a `HashSet<(string,string)>` or a
  non-name separator would fix it. Deferred (no realistic repro).
- **`DynamoDbHealthCheck` verdict is driven only by the DescribeTable HTTP 200**, ignoring `TableStatus`,
  so a table in `INACCESSIBLE_ENCRYPTION_CREDENTIALS` (KMS key disabled → all I/O fails), `DELETING`,
  `ARCHIVED`, etc. reports healthy. Arguably wrong for a readiness probe, BUT the class doc + package
  CLAUDE.md explicitly document "healthy on a 200", and which `TableStatus` values should fail is a
  policy decision (ACTIVE-only? allow UPDATING?). Touches the AWS SDK enum. Left for a maintainer call.
- **`CachingHealthCheckProcessor` cache key is just the sorted check `Type` set** (`string.Join(",",
  types)`), so two probes (e.g. liveness vs readiness) sharing the singleton processor with the same
  type-multiset but different check instances/URLs can serve each other's cached result for up to the TTL.
  Real keying flaw but bounded (requires opting into caching AND colliding type-sets); a correct fix needs
  probe identity threaded into the key, which isn't cleanly available at that layer. Deferred.
- **`AwsLambdaBenzeneTestHost.SendEventAsync` leaks an X-Ray segment on the exception path** —
  `AWSXRayRecorder.Instance.BeginSegment("Test")` then `FunctionHandlerAsync(...)` then `EndSegment()`, with
  no `try/finally`. If the handler throws (unhandled), the global recorder's "Test" segment is left open, so
  every later `.BuildHost()` test stacks on a dangling segment (order-dependent, can throw under
  `ContextMissingStrategy.RUNTIME_ERROR`). Correct fix is a `finally` around `EndSegment()`. Deferred: latent
  (no in-suite test drives an unhandled throw through the host), and a robust failing test couples to
  AWS X-Ray global recorder internals (`GetEntity()`/`EntityNotAvailableException`), which is environment-
  brittle — not a clean failing-test-first fix. Noted for a maintainer.
- **`MessageBuilderExtensions.AsRawHttpRequest` emits LF, not CRLF** (`AppendLine` uses `Environment.NewLine`
  = `\n` on Linux), violating RFC 7230 for a raw wire-format HTTP request. Dead code — no caller anywhere in
  `src/`, `test/`, or `examples/` — so not fixed (nothing to reproduce against).
- **`CloudServiceDescriptorSource._descriptor` uses non-volatile double-checked locking** — read lock-free
  in `Get`'s fast path and in `TryGet()`, written under `_gate`. On a weak memory model (ARM64/Graviton,
  common for AWS-hosted .NET) the reference publish can become visible before the descriptor's own field
  writes, exposing a partially-constructed descriptor to a concurrent first-invocation reader; benign on
  x86/x64. The canonical one-word fix is `private volatile MeshServiceDescriptor? _descriptor;`. Deferred
  only because it can't be failing-test-first verified (the race won't reproduce on x64 CI, and the type is
  `internal` with no `InternalsVisibleTo` to the test assembly) — a maintainer can apply the keyword trivially.
- **`mesh-ui.html` sets `specUrl`/`healthUrl` anchor `href` from the self-reported manifest without a
  scheme allow-list**, so a hostile service reporting `"specUrl":"javascript:…"` yields a clickable
  script-executing link. Browser-mitigated (anchors are `target="_blank"`; modern Chromium/Firefox block
  `javascript:` navigation into a new tab), and the sibling `specUiLink()` already round-trips through
  `new URL(...)`. A one-line `^https?:`/relative allow-list would harden it, but it's client-side JS with
  no test harness in the repo (no failing-test-first possible), so deferred rather than shipping untested.
- **`BenzeneResultExtensions.IsSuccess()`** returns true only for `Ok`, disagreeing with
  `IBenzeneResult.IsSuccessful` (true for all six success statuses). No production caller in `src/`; likely
  an intentional narrow `Ok`-alias. Left alone (would need a maintainer decision on intended semantics).

## Cycle log

(newest first)

### Cycle 29 — Kafka header getters threw on duplicate header keys (`ToDictionary`) — three sites
- **Bug:** `KafkaMessageHeadersGetter<TKey,TValue>` (Kafka.Core inbound), `KafkaSendMessageHeadersGetter`
  (Kafka.Core outbound), and `Azure.Function.Kafka.KafkaMessageHeadersGetter` all built their dictionary
  with `.ToDictionary(x => x.Key, ...)`. Kafka headers are an ordered list that legitimately permits
  repeated keys, so `ToDictionary` threw `ArgumentException` on the second occurrence — making a valid
  consumed record unprocessable (dropped/at-most-once under `CatchHandlerExceptions`, or a poison record
  under commit-on-success). The sibling RabbitMq/gRPC getters already use a last-wins indexer.
- **Repro:** three new tests (Kafka.Core inbound + outbound in `KafkaCoreMappersTest`, Azure in
  `KafkaGettersTest`) — each threw `ArgumentException` on a duplicate `trace` header pre-fix.
- **Fix:** build the dictionary with a last-wins indexer loop in all three, matching RabbitMq/gRPC (and
  the Cycle 28 AWS Lambda Kafka fix). Full core suite green (1913). (Found by three parallel transport hunts.)

### Cycle 28 — AWS Lambda Kafka header getter dropped all headers but the first (`KafkaMessageHeadersGetter`)
- **Bug:** `context.KafkaEventRecord.Headers.FirstOrDefault()` took only element [0] of the header list. In
  the AWS MSK/Kafka Lambda wire format each record header is a SEPARATE single-entry element in that list
  (`IList<IDictionary<string,byte[]>>`, preserving Kafka's ordered multimap), so a record with N headers
  surfaced only the first — every header after it (very commonly `traceparent`, which follows
  app/correlation headers) was silently dropped. This broke W3C trace continuation, schema-version routing,
  correlation, and any handler header read on the Kafka Lambda transport. The doc-comment ("first header
  batch") and the single-element test helper both encoded the same misconception, so CI stayed green.
- **Repro:** new `KafkaGettersTest.HeadersGetter_MultipleHeaderEntries_DecodesAllOfThem` — `traceparent`
  (second entry) was `KeyNotFound` pre-fix.
- **Fix:** flatten every list element (last-wins indexer, which also stops a duplicate Kafka key from
  throwing) and add the synthetic `topic` entry; preserve the empty-list → empty-dict behaviour. Full core
  suite green (1909). (Found — and independently corroborated — by two parallel transport hunts.)

### Cycle 27 — mesh trace read traceparent/correlation-id case-sensitively, breaking cross-language join (`Mesh.Wire/Extensions.UseMeshTrace`)
- **Bug:** the trace middleware read `traceparent` and `x-correlation-id` with plain `TryGetValue`. The
  envelope headers deserialize into an ordinal `Dictionary<string,string>`, so a canonically-cased key —
  exactly what a cross-language mesh participant sends (a Go service's `net/http` canonicalizes to
  `Traceparent`) — was missed: the service started a fresh trace instead of joining the caller's, silently
  breaking distributed tracing across the fleet (this package's whole reason for existing). Same §2
  case-insensitive-read violation fixed in Cycles 16/17, and inconsistent with `W3CTraceContextExtensions`,
  which already reads `traceparent` case-insensitively.
- **Repro:** two new `ExtensionsTest` cases — a `Traceparent` header didn't join, and an `X-Correlation-Id`
  header wasn't captured, pre-fix.
- **Fix:** a `TryGetHeader` helper (fast path + case-insensitive fallback scan) for both reads. 170 mesh +
  129 conformance tests green. (Found by a cross-cutting sibling-pattern sweep after the package hunts.)

### Cycle 26 — test header builders threw on duplicate keys and clobbered/aliased headers (`Benzene.Testing`)
- **Bug:** `MessageBuilder.WithHeader`/`HttpBuilder.WithHeader` used `Dictionary.Add`, so setting a header
  twice (a default then an override) threw `ArgumentException` instead of last-wins like a real header map.
  `HttpBuilder.WithHeaders` assigned the caller's dictionary by reference — dropping any earlier `WithHeader`
  values, coupling later mutations to the caller's collection, and throwing on a read-only source — diverging
  from the additive-copy sibling `MessageBuilderExtensions.WithHeaders`. Latent (no current call site hits the
  buggy branch) but a real defect in a shipped test-helper API.
- **Repro:** new `BenzeneTestBuildersTest` (4 cases) — duplicate-key set threw, and `WithHeaders` dropped a
  prior header / aliased the caller dict, pre-fix.
- **Fix:** `WithHeader` overwrites via the indexer (last-wins); `HttpBuilder.WithHeaders` merges into the
  builder's own dictionary (additive, last-wins), matching the sibling. Suite 1908. (Found by a parallel
  Testing/Tools hunt; AspNet.Core hunt came back clean.)

### Cycle 25 — Fleet UI middleware matched its path case-sensitively (`MeshFleetUiMiddleware`)
- **Bug:** `NormalizePath` ended with `trimmed.TrimEnd('/')` but — unlike the two sibling middlewares
  (`MeshUiMiddleware`, `SpecUiMiddleware`), which both end with `.ToLowerInvariant()` — omitted the final
  lowercasing. So a canonically-cased request (`/MESH-FLEET-UI`, or a configured path with different case)
  didn't match `_path` and fell through to `next()` (a 404), even though the sibling UI pages serve
  regardless of path case (a convention pinned by `SpecUiMiddlewareTest`'s `[InlineData("/spec-ui",
  "/SPEC-UI")]`). No `MeshFleetUiMiddlewareTest` existed, which is why it slipped through.
- **Repro:** new `MeshFleetUiMiddlewareTest` — case/trailing-slash-insensitive matching cases failed pre-fix.
- **Fix:** lowercase the normalized path, matching the siblings. 168 mesh tests green. (Found by a parallel
  mesh-UI/tools hunt; that hunt also noted a client-side JS hardening item — see deferred.)

### Cycle 24 — versioned enum nullability change corrupted the value or threw at startup (`CasterFuncBuilder`)
- **Bug:** the payload caster's `IsEnum` guard returned true only when both sides were non-nullable enums or
  both nullable enums, missing the *mixed* case; and the equal-underlying-type value block only rescues the
  *same* CLR enum type. Per the schema convention, `V1.OrderStatus` and `V2.OrderStatus` are distinct
  per-version CLR types, so the routine "make the enum field optional/required" evolution produced a
  different-typed enum with a nullability change that hit neither guard and fell through to class-mapping:
  downcast (`V2.Status?`→`V1.Status`) came back `default(0)` — silent corruption on the response path
  (`CastingResponsePayloadMapper`); upcast (`V1.Status`→`V2.Status?`) threw `Expression.Constant(null,
  <non-nullable enum>)` `ArgumentException` at caster-build (startup).
- **Repro:** three new `CasterFactoryTest` cases with two distinct enum CLR types — upcast (threw at Build
  pre-fix), downcast (returned default pre-fix), and a null-source guard.
- **Fix:** `IsEnum` now matches an enum-or-nullable-enum on both sides (any nullability combo), and
  `CreateEnumExpression` is nullability-aware — a lifted `Convert` for →nullable/both, and a
  `HasValue ? (T)value : default` branch for nullable→non-nullable so a null never throws (mirroring the
  value-type block). 49 versioning tests green; suite 1904. (Found by a parallel auth/validation hunt,
  which also cleared Auth.Core/OAuth2/DataAnnotations as correct.)

### Cycle 23 — Prometheus client threw on a valid-but-unexpected JSON body (`PrometheusQueryClient.QueryAsync`)
- **Bug:** the parse was wrapped in `catch (JsonException)`, but `JsonException` only covers syntactically
  invalid JSON. Once the body is valid JSON, the strongly-typed `JsonElement` accessors (`GetString`,
  `GetArrayLength`, indexer) throw `InvalidOperationException` on a wrong `ValueKind` — e.g. `{"status":1}`,
  a non-array `value`, or a numeric value element. The class doc + CLAUDE.md explicitly promise a
  "malformed/unexpected body" surfaces as an empty result, but these escaped, faulting the whole
  `mesh:topology` build (one bad query defeats the "one bad query shouldn't block the rest" design).
  Reachable when the configured Prometheus URL returns 200 with valid JSON of an unexpected shape
  (misconfigured URL, a proxy's JSON error object, a Thanos/Cortex/Mimir/VictoriaMetrics variant).
- **Repro:** new `PrometheusQueryClientTest.QueryAsync_ValidJsonUnexpectedShape_ReturnsEmpty` (3 cases) —
  threw `InvalidOperationException` pre-fix, return empty post-fix.
- **Fix:** `catch (Exception ex) when (ex is JsonException or InvalidOperationException)`. 8 client tests +
  163 mesh tests green. (Found by a parallel mesh reporting/tempo hunt.)

### Cycle 22 — CLI help name/description indentation was silently dropped (`HelpGenerator`)
- **Bug (low severity, cosmetic):** the intended indentation of the command name/description was written
  as `$"{  name}"` / `$"{    description}"` — the spaces sit *inside* the interpolation braces, where
  they're part of the (whitespace-insignificant) expression and discarded. Both printed flush-left,
  misaligned with the indented `"  Parameters"` / `"    --…"` lines below.
- **Repro:** new `HelpGeneratorTest.Generate_IndentsNameAndDescription` — no leading spaces pre-fix.
- **Fix:** move the spaces outside the braces (`$"  {name}"` / `$"    {description}"`). 7 help tests green.

### Cycle 21 — Avro serializer overflowed on `ulong` values above `long.MaxValue` (`AvroDatumConverter`)
- **Bug:** the schema generator maps `ulong` to Avro `long` (documented `ulong→long`), but `ToDatum`'s
  Long case used `Convert.ToInt64`, which throws `OverflowException` for a `ulong` in the upper half of
  its range (> `long.MaxValue`) on serialize; the reverse `long→ulong` via `Convert.ChangeType` would also
  overflow on the resulting negative long. The same bug class as the earlier `uint` fix, which stopped at
  32 bits and left the top half of `ulong` broken.
- **Repro:** new `AvroSerializerTest.RoundTrips_ULong_AboveInt64Max` (`10_000_000_000_000_000_000UL`) —
  threw `OverflowException` on serialize pre-fix, round-trips post-fix.
- **Fix:** bit-reinterpret the full 64-bit range — `ToAvroLong` uses `unchecked((long)u)` for a `ulong`,
  and `ConvertPrimitive` reverses it with `unchecked((ulong)l)` for a `ulong` target; `uint`/`long` stay on
  the plain Convert path. 24 Avro tests green (incl. the uint regression); suite 1900. (Found by a parallel
  observability/serializer hunt.)

### Cycle 20 — metrics middleware NRE'd on a null message result (`MetricsExtensions.UseBenzeneMetrics`)
- **Bug:** the result-tag computation ran `context is IHasMessageResult r ? (r.MessageResult.IsSuccessful ...)`.
  `IHasMessageResult.MessageResult` is a settable, nullable property (several contexts leave it null until
  a handler sets one). A non-throwing completion that set no result (e.g. a short-circuit, or no matching
  handler) made `r.MessageResult.IsSuccessful` throw `NullReferenceException` — inside the recording
  `finally`, replacing the real successful outcome with a spurious exception escaping the metrics layer.
- **Repro:** new `BenzeneMetricsTest.UseBenzeneMetrics_NullMessageResult_RecordsMissingWithoutThrowing` —
  NRE pre-fix; post-fix the message is recorded once, tagged `result="<missing>"`.
- **Fix:** a `{ MessageResult: not null }` property pattern, so a null result falls to the existing
  `"<missing>"` sentinel like a context that carries no result signal at all. 3 metrics tests green;
  suite 1899. (Found by a parallel observability hunt.)

### Cycle 19 — API Gateway `resource` leaked segments after a path parameter (`ApiGatewayBuilderV1.BuildVerb`)
- **Bug:** the emitted VTL `resource` is meant to be the static path prefix up to the first path
  parameter (`TakeWhile(x => !x.Contains("{"))`). But a parameter part (ASP.NET `TemplateParser`) has
  `Text == null`, so the segment flattened to `""` and was dropped by the `Where(!IsNullOrEmpty)` *before*
  the `TakeWhile` ran — leaving only literal segments, none containing `{`, so `TakeWhile` never stopped
  (dead code). A literal segment after a parameter (`users/{id}/orders`) leaked in, producing
  `/users/orders/` instead of `/users/`. Trailing-parameter routes (the only shape in the golden files)
  happened to be correct, hiding it. Reachable via `build --output api-gateway`.
- **Repro:** new `LambdaOpenApiBuilderTest.BuildVerb_LiteralSegmentAfterParameter_ResourceStopsAtTheParameter`
  (`/users/orders/` pre-fix, `/users/` post-fix) + a trailing-parameter regression guard.
- **Fix:** `TakeWhile` over the original segments using each segment's `IsParameter` flag, so the prefix
  truly stops at the first parameter. Both golden files unchanged; 47 ApiGateway tests green; suite 1898.
  (Found by a parallel CLI/codegen hunt.)

### Cycle 18 — CLI option default discarded for a value-less flag (`Parsing/Extensions.GetValue`)
- **Bug:** `GetValue` returned `source.Attributes.TryGetValue(key, out var value) ? value : NotNull(default)`.
  The attribute dictionary is `IDictionary<string, string?>`, and a bare value-less flag (`--format`) is
  stored as `key -> null`. `TryGetValue` then returns `true` with `value == null`, so the method returned
  `null` — bypassing the configured default despite a non-null return type — and `PayloadMapper.Map` set
  that `null` straight into a `string` payload property (e.g. `Format` got `null` instead of `"json"`).
  The `NotNull` guard only covered the missing-key branch.
- **Repro:** new `ArgumentExtensionsTest.GetValue_KeyPresentWithNullValue_ReturnsDefault` — returned null
  pre-fix, returns the default post-fix (plus present-value and missing-key regression guards).
- **Fix:** fall back to the default when the found value is null, not only when the key is absent. 303
  CLI/parser tests green; full suite 1896. (Found by a parallel CLI/codegen hunt.)

### Cycle 17 — idempotency key strategy missed a canonically-cased header (`HeaderOrBodyHashIdempotencyKeyStrategy`)
- **Bug:** same class as Cycle 16 — `headers.TryGetValue(_options.HeaderName, ...)` (default
  `idempotency-key`) is case-sensitive via the dictionary comparer, but wire-contracts.md §2 requires
  case-insensitive header reads regardless of comparer. A caller sending `Idempotency-Key` (canonical
  casing) against an ordinal header dict was missed, so the strategy silently fell through to
  body-hashing — ignoring the caller's *explicit* key. That both loses dedup of two different requests
  the caller tagged with the same key, and wrongly dedups two identical bodies tagged with different keys.
- **Repro:** new `HeaderOrBodyHashIdempotencyKeyStrategyTest.Uses_HeaderKey_RegardlessOfHeaderKeyCasing`
  — returned a body hash pre-fix, returns the header key post-fix.
- **Fix:** a `TryGetHeader` helper (fast path + case-insensitive scan), matching Cycle 16 and
  `HeaderMessageVersionGetter`. 25 idempotency tests green; full suite 1893.

### Cycle 16 — content negotiation missed canonically-cased headers (`AcceptHeaderMediaFormatBase`)
- **Bug:** `CanRead`/`CanWrite` looked up `content-type`/`accept` with a plain `IDictionary.TryGetValue`
  and hard-coded lowercase keys, so a header dictionary with an ordinal (case-sensitive) comparer and a
  canonically-cased key (`Content-Type`, `Accept`) missed. wire-contracts.md §2 requires header keys to be
  case-insensitive on read regardless of the dictionary's comparer, and the sibling
  `HeaderMessageVersionGetter` deliberately implements exactly that (with a comment citing the same rule).
  Effect: a client sending `Content-Type: application/xml` (or `Accept:`) against a case-sensitive header
  dict silently fell back to the default JSON format instead of the negotiated one.
- **Repro:** two new `MediaFormatNegotiatorTest` cases — `SelectRead`/`SelectWrite` with a canonically-cased
  key in the default (ordinal) dictionary — returned `application/json` pre-fix, `application/xml` post-fix.
- **Fix:** a `TryGetHeader` helper that tries the fast path then falls back to a case-insensitive scan,
  matching the version getter. 39 media-format tests green; full suite 1890. (Found by a parallel
  serialization/validation hunt.)

### Cycle 15 — mesh collector aborted a whole trace batch on a null status (`MeshCollectorStore.AddEvents`)
- **Bug:** `topic.StatusCounts[traceEvent.Status]` / `GetValueOrDefault(traceEvent.Status)` used the
  status as a `Dictionary<string,long>` key without coalescing. `MeshTraceEvent.Status` is a non-nullable
  `string` at compile time, but a wire payload with `"status": null` deserializes to an actual null
  (NRT isn't runtime-enforced), and a null dictionary key throws `ArgumentNullException`. The line just
  above (`IsSuccess(traceEvent.Status)`) is deliberately null-tolerant, so the author anticipated null —
  the counts line was missed. The exception propagates out of the single-locked loop, aborting the batch
  mid-way (partial, non-transactional mutation) and returning an error, violating the §6 "no missing feed
  ever fails ingestion" rule. Reachable from the `mesh:traces` handler over an untrusted transport.
- **Repro:** new `MeshCollectorStoreTest.AddEvents_EventWithNullStatus_IsAcceptedAndCountedAsFailure` —
  threw `ArgumentNullException` pre-fix; post-fix the event is accepted and counted as a failure.
- **Fix:** coalesce `traceEvent.Status ?? string.Empty` before the key path (mirroring `TopicVersion ??
  string.Empty` one line up). 160 mesh + 129 conformance tests green. (Found by a parallel mesh hunt.)

### Cycle 14 — request enrichment threw on Guid/enum/Nullable properties (`DictionaryUtils.GetValue`)
- **Bug:** `GetValue` coerced enricher-supplied string values to the DTO property type with
  `Convert.ChangeType`, which only supports the IConvertible primitives. A `Guid` (e.g. a route `{id}`
  onto `Guid Id`), an `enum` from a string, or any `Nullable<T>` target (`int?`, `Guid?`, …) threw
  `InvalidCastException`, surfacing as an unhandled error for the whole message. Guid ids and nullable
  fields on request DTOs are extremely common; only string→string / string→int were covered, so the
  gap was untested. Reachable via `EnrichingRequestMapper` (the default request mapper) fed by the real
  enrichers (ApiGateway/AspNet route, query, header, claim values).
- **Repro:** four new `DictionaryUtilsTest` cases — Guid, Guid?, int?, enum from string — all threw
  `InvalidCastException` pre-fix.
- **Fix:** unwrap `Nullable<T>` to its underlying type, parse `Guid`/`enum` explicitly, and only then
  fall back to `Convert.ChangeType`; the compiled setter boxes the value back to the declared property
  type. Full core suite green (1888). (Found by a parallel serialization/validation hunt.)

### Cycle 13 — Autofac adapter ignored the supplied instance in `AddScoped(instance)`/`AddTransient(instance)`
- **Bug:** both instance overloads called `RegisterType<TImplementation>()` (which tells Autofac to
  *construct a new* object), discarding the caller-supplied `implementation`. The `IBenzeneServiceContainer`
  contract documents them as "using an existing instance", the Microsoft adapter honours it
  (`AddScoped(_ => implementation)`), and Autofac's own singleton overload does it right
  (`RegisterInstance`). Resolving returned a different object (losing the caller's configured state) —
  or threw `DependencyResolutionException` if the type had no Autofac-resolvable constructor. A silent
  cross-adapter divergence: identical Benzene code behaved correctly on Microsoft DI and wrongly on Autofac.
- **Repro:** new `ServiceContainerInstanceRegistrationTest` — `Assert.Same(instance, resolved)` for scoped
  and transient, on both adapters. Autofac cases failed pre-fix (different object); Microsoft cases pass
  as the parity guard.
- **Fix:** register the captured instance via `Register(_ => implementation)` with the matching lifetime.
  Full core suite green. (Found by a parallel DI/cache/resilience hunt.)

### Cycle 12 — CLI command splitter crashed on an unterminated quote (`CommandSplitter`)
- **Bug:** `Split`'s quote branch did `i++` then read `args[i]` without a bounds check, so a command
  string with a missing closing quote (`command -name "value one`) ran the inner loop past the end of
  the string → `IndexOutOfRangeException`, crashing the CLI on malformed user input. Two secondary
  defects: the unconditional final-word flush appended a spurious empty `""` token whenever the input
  ended in a quoted argument or a trailing space (existing tests only used inputs ending in a bare
  token, so they never hit any of these).
- **Repro:** three new `CommandSplitterTest` cases — unterminated quote (threw pre-fix), quoted-final
  arg and trailing-space (each emitted a trailing `""` pre-fix).
- **Fix:** the inner loop breaks and flushes on `i >= args.Length` instead of indexing past the end; the
  final flush is gated on `currentWord.Any()` so a already-flushed word doesn't append `""`. Reachable
  from `ConsoleApplication.ExecuteAsync(string)`. 293 CLI/parser tests green; full suite 1879.

### Cycle 11 — markdown builder crashed on inline-object arrays and dropped referenced-array fields (`MarkdownTypeBuilder`)
- **Bug:** `MapProperty`'s array branch guarded with `if (Items.Reference != null || Items.Reference.ReferenceV2 == reference)`.
  Two defects in one contradictory condition: (a) the `||` short-circuits on `Items.Reference != null`, so
  EVERY array of a referenced object (`List<TenantDto>`) collapsed to `{...}[]` and its item fields never
  rendered — even when it wasn't a cycle; (b) for an inline-object array (`Items.Reference == null`,
  reached via the enclosing `Items.Type == "object"` test), the second operand dereferenced the null
  `Items.Reference.ReferenceV2` → `NullReferenceException`, crashing the whole doc build.
- **Repro:** two new `MarkdownTypeBuilderTest` cases — `BuildType_ArrayOfReferencedObjects_ExpandsItemProperties`
  (collapsed to `{...}[]` pre-fix, expands `TenantDto`'s fields post-fix) and
  `BuildType_ArrayOfInlineObjects_DoesNotThrow` (NRE pre-fix, expands the inline field post-fix). New model
  `TenantListDto`. No existing golden model has an object array, so the 6 golden files are unaffected.
- **Fix:** `&&` instead of `||`, mirroring the sibling single-object branch (`Reference.ReferenceV2 == reference`):
  collapse to `{...}[]` only on a genuine reference cycle; otherwise expand the item schema. Full core suite
  green (1872).

### Cycle 10 — markdown doc property keys diverged from the wire for acronym names (`CodeGenHelpers.Camelcase`)
- **Bug:** the same acronym-lowercasing algorithm fixed in Cycle 2's `ExamplePayloadBuilder`, but here it
  IS on a production path — `CodeGenHelpers.Camelcase` is called by the Markdown doc builders
  (`MarkdownTypeBuilder`, `LambdaServiceMarkdownBuilder`) to render property keys. It lowercased the whole
  leading run of capitals, so `IPAddress` → `ipaddress`, whereas the runtime serializer (STJ
  `JsonNamingPolicy.CamelCase`) yields `ipAddress` (keeps the capital before a lowercase). Any
  acronym-prefixed property (`IPAddress`, `IOStream`, `URLPath`, …) was documented with a key that binds
  to null against its own service. (Earlier wrongly logged as "unused" — the grep missed the
  `CodeGenHelpers.Camelcase(...)` static-call form.)
- **Repro:** `CodeGenHelpersTest.Camelcase_MatchesJsonNamingPolicy` — `IDValue`→`idvalue`/`IPAddress`→
  `ipaddress` pre-fix, `idValue`/`ipAddress` post-fix. No CodeGen test model has a 2+-leading-capital
  property, so the golden markdown files are unaffected (27 codegen/markdown tests green).
- **Fix:** replaced the hand-rolled logic with `JsonNamingPolicy.CamelCase.ConvertName` (exactly the
  runtime serializer's policy), matching the Cycle 2 fix. Full core suite green (1870).

### Cycle 9 — backward-compat gate let breaking EVENT changes pass as warnings (`SchemaCompatibilityRules`)
- **Bug:** `DefaultFor` special-cased only `SchemaDirection.Response` for the consumer-side rules; `Event`
  fell into the producer (Request) branch. But the client CONSUMES events (the service produces them),
  so `Event` is consumer-side like `Response`. Result: removing a property from an event payload was
  classified `Warning` (not `Breaking`), so `EnsureBackwardCompatible` didn't throw and a genuinely
  breaking event-contract change passed the CI gate. (And the inverse false-positives: a new required
  event field was flagged Breaking instead of Compatible, etc.)
- **Repro:** `SchemaCompatibilityRulesTest.DefaultFor_Event_MatchesTheResponseConsumerSide` (4 cases) —
  wrong pre-fix, correct post-fix; a Request regression theory confirms the producer side is unchanged.
- **Fix:** the four consumer-side branches now test `direction != SchemaDirection.Request` (Response +
  Event), so Event shares the Response rules. 21 compatibility tests green.

### Cycle 8 — JSON-schema validation threw on a malformed body instead of rejecting it (`JsonSchemaMiddleware`)
- **Bug:** `JsonDocument.Parse(body)` was unguarded. A `null` body and a schema-failing body both return
  `ValidationError`, but a syntactically-invalid body (`"{"`, `"not json"`, `""`) threw `JsonException`
  that escaped the pipeline as an internal error - the most clearly-invalid input was the only one that
  crashed. The sibling `IsJsonValidator` already guards the identical parse.
- **Repro:** `ValidationTest_MalformedJsonBody_ReturnsValidationError` — threw pre-fix, returns
  ValidationError post-fix.
- **Fix:** parse inside try/catch(JsonException); a parse failure is treated as a validation failure like
  the null/non-conforming cases. 8 JsonSchema tests green.

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
