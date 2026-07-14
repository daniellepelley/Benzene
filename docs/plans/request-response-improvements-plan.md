# Request/Response Improvements Plan

## Context

`work/request-response-design-review.md` (2026-07-14) assessed the request-mapping and
response-writing design in `Benzene.Core.MessageHandlers` against its goals — multi-format
serialization, performance, and future HTML templating — and found the architecture sound but
in need of consolidation: content negotiation is scattered across both sides of the pipe and
keyed on the wrong header for responses; response rendering is forced to masquerade as payload
serialization; the string-based `ISerializer` caps both throughput and format flexibility at
text; and the hot path re-does reflection and rebuilds static lookups per message. This plan
turns that review into four executable phases. Finding IDs (D1–D5, C1–C7, P1–P7) refer to the
review document.

Phases must run in order; each compiles, passes the full suite, and merges independently.
Phase 1 is non-breaking. Phases 2–4 contain breaking changes, acceptable under the current
release posture (pre-1.0, releasable-state-not-releasing, no `[Obsolete]` shims).

## Verified facts this plan relies on

- **Hot-path costs are real and localized:** `MessageHandlerFactory.Create` runs
  `GetType().GetMethod("CreateMessageHandlerByType")` + `MakeGenericMethod` + `Invoke` per
  message; `MessageHandlerDefinitionLookUp` re-aggregates all finders and re-runs the
  GroupBy-dedup per `FindHandler`; header getters build a fresh dictionary per `GetHeaders`
  call (`ApiGatewayMessageHeadersGetter` runs a multi-operator LINQ chain each time).
- **`IMessageHandlersList`** (`Benzene.Abstractions.MessageHandlers`) has a single member,
  `Add(IMessageHandlerDefinition)`; the concrete `MessageHandlersList` is a singleton. Runtime
  handler registration exists, so any definition cache needs invalidation.
- **Blast radius of the format unification (phase 2):**
  - `ISerializerOption<TContext>` implementations: `Benzene.Xml` (`XmlSerializerOption`) and
    `Benzene.Extras/Request` (`SerializerOption`, `SerializerOptionBase`,
    `InlineSerializerOption`, plus `RequestMapBuilder` — the only `IRequestMapBuilder`
    implementation).
  - `ISerializationResponseHandler`/`IBodySerializer` implementations outside core:
    `Benzene.Xml` only.
  - `IResponseHandler<TContext>` registrations: `Benzene.AspNet.Core`,
    `Benzene.Aws.Lambda.ApiGateway`, `Benzene.Azure.Function.AspNet`, `Benzene.SelfHost.Http`
    (each: `HttpStatusCodeResponseHandler` + a body handler), `Benzene.Xml` (open generic),
    and the `BenzeneMessage` path (`ResponseBodyHandler`).
  - The two body-writing schemes disagree today: ApiGateway registers the negotiating
    `ResponseHandler<JsonSerializationResponseHandler<…>,…>`, while AspNet.Core,
    Azure.Function.AspNet, and SelfHost.Http register the non-negotiating
    `ResponseBodyHandler` (single DI serializer, hardcoded JSON content type) — XML responses
    only actually work on ApiGateway today.
- **The empty-body `Activator.CreateInstance<TRequest>()` fallback in `RequestMapper` is
  load-bearing** (review C2 re-assessed): HTTP GET endpoints have no body and rely on
  `EnrichingRequestMapper` + route-parameter enrichers to populate the request — and
  `EnrichingRequestMapper` skips enrichment when the inner mapper returns null. The fallback
  must stay; it gets documented, not changed.
- `AddBenzene()` registers `JsonSerializer` (concrete) as a singleton; `AddXml()` registers
  `XmlSerializer` as a singleton — both are resolvable for phase 1's C4 fix.
- Status-code writing (`HttpStatusCodeResponseHandler`, `IHttpStatusCodeMapper` in
  `Benzene.Http`) is already a separate response handler — untouched by this plan.

## ⚠️ FLAGS — approved by approving this plan

- **No new NuGet dependencies. No new projects.** All work lands in existing projects
  (`Benzene.Abstractions.*`, `Benzene.Core.MessageHandlers`, `Benzene.Core.Messages`,
  `Benzene.Xml`, `Benzene.NewtonsoftJson`, `Benzene.Extras`, plus the four HTTP-ish transport
  packages' DI registrations). An actual HTML/Razor package is explicitly **out of scope**
  (future work; phase 3 only builds the seam and proves it with a test-only renderer).
- **Breaking changes (clean break, no `[Obsolete]`):** listed per phase below. Phase 1 has
  none. CHANGELOG entries required for every breaking item.

## Design decisions (final)

- **R1 — Media-type comparison is parameter- and case-tolerant.** One helper
  (`MediaType.Matches(string headerValue, string mediaType)`, in `Benzene.Core.Messages`)
  strips `;`-parameters, trims, compares `OrdinalIgnoreCase`. Every content-type comparison in
  the repo goes through it.
- **R2 — Hot-path caches are static-compiled, version-invalidated.** Factory dispatch is
  cached as compiled open-instance delegates keyed by `(handlerType, requestType,
  responseType)` (static `ConcurrentDictionary`, expression-tree built, so scoped factory
  instances share delegates but still resolve handlers from their own scope). The definition
  lookup is backed by a singleton index (`Dictionary<string, IMessageHandlerDefinition[]>` by
  topic id) rebuilt when a version stamp on `MessageHandlersList` changes.
- **R3 — One media-format registration drives both directions** (replaces D2's three-part
  registration). New abstraction in `Benzene.Abstractions.MessageHandlers`:

  ```csharp
  public interface IMediaFormat<TContext>
  {
      string ContentType { get; }                                   // what it produces
      bool CanRead(TContext context, IServiceResolver resolver);    // request content-type
      bool CanWrite(TContext context, IServiceResolver resolver);   // Accept header
      ISerializer GetSerializer(IServiceResolver resolver);
  }
  ```

  A scoped `IMediaFormatNegotiator<TContext>` evaluates registered formats **once per
  message** (memoizing both decisions), exposing `SelectRead()` and `SelectWrite()`; read
  falls back to the default format (JSON), write falls back to the read format, then the
  default. This also subsumes P3: negotiation is the main repeated `GetHeaders` caller today.
- **R4 — Response format negotiates on `Accept`, falling back to request content-type.**
  (`AcceptHeaderMediaFormatBase` implements `CanWrite` on `accept` — token match within the
  header value, `*/*` treated as no-preference — and `CanRead` on `content-type`, both via
  R1's helper.)
- **R5 — One response-handler interface.** `IResponseHandler<TContext>` gains
  `ValueTask HandleAsync(TContext, IMessageHandlerResult)`; `ISyncResponseHandler` /
  `IAsyncResponseHandler` and the container's type-switch are deleted. Sync implementations
  return `default`.
- **R6 — The write side is a renderer, not a serializer.** New abstraction:

  ```csharp
  public interface IResponseRenderer<TContext>
  {
      bool CanRender(TContext context, IMessageHandlerResult result, IServiceResolver resolver);
      Task RenderAsync(TContext context, IMessageHandlerResult result,
                       IBenzeneResponseAdapter<TContext> response);
  }
  ```

  One built-in implementation, `SerializerResponseRenderer<TContext>`, wraps the media-format
  negotiator (covers JSON/XML/Newtonsoft — i.e. everything today). HTML templating later
  implements `IResponseRenderer` directly — async, topic-aware (the result carries the
  `MessageHandlerDefinition`), owning its error-page mapping — with **zero core changes**.
  Renderers are evaluated in registration order; first `CanRender` wins; the serializer
  renderer registers last as the catch-all.
- **R7 — Raw payloads carry their content type.** New
  `IRawContentMessage : IRawStringMessage { string ContentType { get; } }` in
  `Benzene.Abstractions.Messages`. The payload mapper already passes `IRawStringMessage`
  content through; the renderer additionally honors `IRawContentMessage.ContentType` when
  setting the response content type (fixes "raw HTML stamped as application/json").
- **R8 — Byte-oriented serialization is additive.** New
  `IPayloadSerializer : ISerializer` in `Benzene.Abstractions.Serialization` adding
  `void Serialize(Type type, object payload, IBufferWriter<byte> writer)` and
  `object? Deserialize(Type type, ReadOnlySpan<byte> payload)`. String-based `ISerializer`
  remains the universal fallback; the request mapper and serializer renderer use the byte path
  only when the serializer implements `IPayloadSerializer` **and** the context's body getter
  exposes bytes (new optional `IMessageBodyBytesGetter<TContext>`). Binary-only formats
  implement `IPayloadSerializer` and throw `NotSupportedException` from the string members —
  documented contract.
- **R9 — Enricher precedence stays earlier-wins; the docs change, not the behavior.**
  (`IRequestEnricher`/`EnrichingRequestMapper` XML docs corrected to match
  `DictionaryUtils.MapOnto`; C3.) Similarly the empty-body `Activator` fallback stays and gets
  documented as load-bearing (C2).

---

## Phase 1 — Correctness + hot-path fixes (non-breaking)

All in `Benzene.Core.MessageHandlers`, `Benzene.Core.Messages`, and `Benzene.Xml`; no public
API removed or reshaped.

1. **R1 media types** — add `MediaType` helper (`Benzene.Core.Messages/Helper/MediaType.cs`)
   with `Matches(string headerValue, string mediaType)`; use it in
   `HeaderContextPredicate.Check` (add an optional "compare as media type" mode or a
   `MediaTypeHeaderContextPredicate` subclass — decide by whichever avoids changing existing
   equality semantics for non-content-type headers: **subclass**, and point
   `XmlContentTypeHeaderContextPredicate` at it) and in `XmlSerializationResponseHandler`
   (replacing its private `KeyEquals` copy with the helper). (C1, partial de-dup of the
   `KeyEquals` clone.)
2. **C4 DI-resolved serializers** — `JsonSerializationResponseHandler` ctor-injects the
   singleton `JsonSerializer`; `XmlSerializationResponseHandler` ctor-injects `XmlSerializer`;
   both stop newing instances per response.
3. **C7** — delete the commented `IRawJsonMessage` block in `DefaultResponsePayloadMapper`.
4. **P1 factory cache** — in `MessageHandlerFactory`, replace per-call
   `GetMethod`/`MakeGenericMethod`/`Invoke` with a static
   `ConcurrentDictionary<(Type, Type, Type), Func<MessageHandlerFactory, ITopic, IMessageHandler>>`
   of expression-compiled open-instance delegates.
5. **P2 definition index** — add `int Version { get; }` to `MessageHandlersList` (concrete
   class only, not the interface — non-breaking), incremented on `Add`. New singleton
   `MessageHandlerDefinitionIndex` builds `Dictionary<string, IMessageHandlerDefinition[]>`
   (topic id → versions) from all finders, rebuilding when any `MessageHandlersList.Version`
   changes; `MessageHandlerDefinitionLookUp` consults the index instead of re-aggregating.
   **Implementation must verify** every in-repo `IMessageHandlerDefinition` DI registration is
   singleton; if any scoped registration exists, exclude `DependencyMessageHandlersFinder`
   from the index and keep it as a per-call overlay.
6. **P5 enricher costs** — `EnrichingRequestMapper` returns the mapped request untouched when
   no enrichers are registered; `DictionaryUtils.Enrich` uses a static-generic cache of
   compiled property setters (`Action<T, object?>` per settable property, expression-built
   once per `T`); `MapOnto` switches to `StringComparer.OrdinalIgnoreCase` dictionaries
   instead of `ToLowerInvariant()` per key. **R9**: fix the `EnrichingRequestMapper` /
   `IRequestEnricher` XML docs to say earlier-wins; document the empty-body `Activator`
   fallback on `RequestMapper.GetBody` as intentional (route-parameter-only requests).
7. **P4 mapper allocation** — `MultiSerializerOptionsRequestMapper` pre-builds the
   default-serializer mapper once per (scoped) instance and caches option-selected mappers in
   a small per-instance dictionary keyed by the selected `ISerializer`.
8. **P6** — `Benzene.Xml.XmlSerializer` caches `System.Xml.Serialization.XmlSerializer`
   instances in a static `ConcurrentDictionary<Type, …>`.

Tests (`test/Benzene.Core.Test/Core/…`, following existing conventions): `MediaType` theory
(exact, parameters, casing, mismatches); XML selected when request sends
`application/xml; charset=utf-8` end-to-end (regression for C1); factory returns per-scope
handler instances across repeated dispatches (cache must not leak scope); runtime
`MessageHandlersList.Add` after first dispatch is found by the next (index invalidation);
enricher setter cache round-trip incl. type conversion; existing suite green untouched.

Acceptance: no public-surface diffs (`git diff` on `Benzene.Abstractions.*` empty except XML
docs); full suite green.

## Phase 2 — Media-format unification (D2 + D3 + D5; breaking)

1. **New abstractions** (`Benzene.Abstractions.MessageHandlers`): `IMediaFormat<TContext>`
   (R3), `IMediaFormatNegotiator<TContext>` (scoped, memoizing; R3/R4). New in
   `Benzene.Core.MessageHandlers`: `MediaFormatNegotiator<TContext>`,
   `AcceptHeaderMediaFormatBase<TContext>` (R4, built on R1), `JsonMediaFormat<TContext>`
   (default format, wraps the singleton `JsonSerializer`).
2. **Request side** — `MultiSerializerOptionsRequestMapper` reimplemented over
   `IMediaFormatNegotiator.SelectRead()`; `ISerializerOption<TContext>`,
   `SerializerOptionBase` (both copies — core and Extras), `InlineSerializerOption`,
   `IRequestMapBuilder`, and `RequestMapBuilder` are **deleted**; `Benzene.Extras` gains an
   `InlineMediaFormat<TContext>` (predicate + serializer + content type) as the replacement
   for inline registration.
3. **Response side** — one `SerializationResponseHandler<TContext>` (asks the negotiator for
   the write format, writes body + content type unless a body is already set) replaces
   `ResponseHandler<T,TContext>`, `ISerializationResponseHandler<TContext>`,
   `JsonSerializationResponseHandler`, `IBodySerializer`, `BodySerializer<TContext>`, and
   `ResponseBodyHandler` — all **deleted**. The four HTTP-ish transports and the
   `BenzeneMessage` path register the unified handler; **this fixes AspNet.Core /
   Azure.Function.AspNet / SelfHost.Http silently not supporting XML today.**
4. **R5 single handler interface** — reshape `IResponseHandler<TContext>`; delete
   `ISyncResponseHandler`/`IAsyncResponseHandler`; simplify `ResponseHandlerContainer`; update
   `HttpStatusCodeResponseHandler` + all implementations.
5. **`Benzene.Xml`** — becomes one `XmlMediaFormat<TContext>` + `AddXml()` registration;
   `XmlSerializationResponseHandler`, `XmlResponseHandler`, `XmlSerializerOption`,
   `XmlContentTypeHeaderContextPredicate`, and the mutable static `Settings` class (C5) are
   deleted; content-type strings become ctor parameters with defaults.
   `Benzene.NewtonsoftJson` gains a `NewtonsoftJsonMediaFormat` if it registered an option
   (verify at implementation; today it appears to ship only the serializer).
6. **Breaking-changes CHANGELOG entry** covering: `ISerializerOption`/`IRequestMapBuilder`
   family removed (replaced by `IMediaFormat`), `ISerializationResponseHandler`/
   `IBodySerializer`/`ResponseBodyHandler` removed (replaced by
   `SerializationResponseHandler`), `IResponseHandler` reshaped (R5), `Benzene.Xml.Settings`
   removed, response format now honors `Accept` (behavioral).

Tests: negotiator matrix (`content-type` × `accept` × registered formats → selected
read/write formats, including "Accept: application/xml with JSON request body" — impossible
before, D3); registration order no longer matters (JSON-then-XML and XML-then-JSON produce
identical behavior — the regression that motivated D2); one `GetHeaders` call per message
across the whole negotiation (instrumented fake getter — closes P3); all four HTTP transports
produce XML when asked (new coverage for the three that couldn't).

## Phase 3 — Renderer seam (D4 + R6 + R7; breaking only for phase-2 internals)

1. `IResponseRenderer<TContext>` (R6) in `Benzene.Abstractions.MessageHandlers.Response`;
   phase 2's `SerializationResponseHandler` becomes `SerializerResponseRenderer<TContext>`
   (registered last, catch-all); a thin `RendererResponseHandler<TContext>` (an
   `IResponseHandler`) walks registered renderers, first `CanRender` wins.
2. `IRawContentMessage` (R7) in `Benzene.Abstractions.Messages` + honor it in the serializer
   renderer (body passthrough + content type).
3. Error rendering moves inside the renderer: the serializer renderer keeps the
   `DefaultResponsePayloadMapper` `ErrorPayload` behavior verbatim; the renderer contract
   documents that non-serializer renderers own their error representation.
4. **Proof, not product:** a test-only `FakeHtmlRenderer` (in `test/Benzene.Core.Test`)
   asserting: `Accept: text/html` selects it over JSON; it renders asynchronously; a failed
   result renders its error page (not `ErrorPayload` JSON); a handler returning
   `IRawContentMessage` with `text/html` is delivered with that content type. A real
   `Benzene.Html.*` package is future work and out of scope.
5. Docs: `docs/serialization.md` (or nearest existing page — verify at implementation)
   rewritten around formats + renderers; spec (`docs/specification/`) amended if it prescribes
   response-format selection (verify; the HTTP binding section mentions content types).

## Phase 4 — Byte-oriented serialization (D1 + P7; additive)

1. `IPayloadSerializer` (R8) in `Benzene.Abstractions.Serialization`;
   `IMessageBodyBytesGetter<TContext>` in `Benzene.Abstractions.Messages.Mappers`.
2. `Benzene.Core.MessageHandlers.Serialization.JsonSerializer` implements
   `IPayloadSerializer` via `Utf8JsonWriter`/`Utf8JsonReader` (no behavior change on the
   string members).
3. `RequestMapper` prefers the byte path when the selected serializer is an
   `IPayloadSerializer` and an `IMessageBodyBytesGetter<TContext>` is registered for the
   context; `SerializerResponseRenderer` mirrors this on the write side where the response
   adapter can accept bytes (add `SetBody(TContext, ReadOnlyMemory<byte>)` as a
   default-interface member on `IBenzeneResponseAdapter` that falls back to UTF-8 string —
   keeps every existing adapter compiling).
4. Wire one transport end-to-end as the reference: `BenzeneMessageContext` (already
   envelope-owned, simplest body access), proving the string path and byte path produce
   byte-identical JSON in tests.
5. Explicitly deferred from this phase: converting the other transports' body getters, any
   binary format package (Protobuf/MessagePack), and async/streaming serialization — each is
   its own follow-up once a consumer exists.

## Verification

Per phase: full `dotnet test` via CI on push to `main` (no local SDK in the dev sandbox —
extra care on overload resolution and namespace ambiguity, per this repo's established
practice). Phase 2 additionally: `Benzene.Examples.sln` builds; grep-gates that the deleted
types (`ISerializerOption`, `IBodySerializer`, `ISyncResponseHandler`,
`IAsyncResponseHandler`, `ResponseBodyHandler`, `Benzene.Xml.Settings`) have zero remaining
references. The conformance suite (`test/Benzene.Conformance.Test`) must stay green
throughout — the wire contracts (envelope, error payload, statuses) are not touched by any
phase, and the suite proves it.

## Execution notes

- Follow existing file style: file-scoped namespaces, one type per file, full XML docs
  (`GenerateDocumentationFile` is on; the repo holds a zero-CS1591 line).
- Decisions R1–R9 are final; the flagged breaking changes were approved with this plan.
- Work on branch `claude/benzene-grpc-design-plan-1z2hb8`; commit per phase (phase 1 may be
  two commits: correctness, then perf); standard merge flow to `main` after each phase; watch
  CI and fix forward.
- Phase boundaries are merge points — later phases may be re-planned after earlier ones land
  without invalidating this document.
