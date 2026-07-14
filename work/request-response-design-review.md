# Request/Response Design Review — Benzene.Core.MessageHandlers

**Date:** 2026-07-14
**Scope:** the request-mapping and response-writing machinery in
`Benzene.Core.MessageHandlers` (`Request/`, `Response/`, `Serialization/`, `MessageRouter`,
`MessageHandlerFactory`, `RequestMapperThunk`) plus the abstractions they implement
(`Benzene.Abstractions.MessageHandlers.Request/Response`, `ISerializer`) and the two
non-default serializer packages (`Benzene.Xml`, `Benzene.NewtonsoftJson`). Assessed against the
design's stated goals: multi-serialization flexibility (JSON, XML, others), performance, and
future extensibility to HTML templating.

---

## 1. The design as it stands

**Request path** (per message):

```
MessageRouter<TContext>
  → IMessageGetter.GetTopic → IMessageHandlerDefinitionLookUp.FindHandler
  → IMessageHandlerFactory.Create(definition)            [reflection: MakeGenericMethod]
  → MessageHandler<TReq,TResp>.HandlerAsync(IRequestMapperThunk)
      → thunk.GetRequest<TReq>()                          [defers typing until handler known]
          → IRequestMapper<TContext>.GetBody<TReq>
              = MultiSerializerOptionsRequestMapper<TContext, JsonSerializer>
                  → first ISerializerOption<TContext> whose IContextPredicate matches,
                    else DI-resolved default serializer
                  → new RequestMapper (IRequestContext short-circuit | body string
                    via IMessageBodyGetter | ISerializer.Deserialize | empty body →
                    Activator.CreateInstance)
                  → new EnrichingRequestMapper (IRequestEnricher dictionaries folded
                    onto the request via reflection)
```

**Response path** (response-writing transports):

```
IMessageHandlerResultSetter<TContext>
  → ResponseIfHandledMessageHandlerResultSetter (skip when topic = <missing>)
      → IResponseHandlerContainer (ordered IResponseHandler<TContext>s,
        sync/async dispatched by runtime type-check)
          → e.g. HttpStatusCodeResponseHandler
          → e.g. ResponseHandler<XmlSerializationResponseHandler,TCtx>
              → ISerializationResponseHandler (per-format: check applicability,
                skip if body already set)
                  → IBodySerializer (BodySerializer binds context + payload mapper)
                      → IResponsePayloadMapper (success payload | ErrorPayload;
                        IRawStringMessage escape hatch)
                          → ISerializer
      → IBenzeneResponseAdapter.FinalizeAsync
```

## 2. What the design gets right

These are genuine strengths and should be preserved through any rework:

- **`IRequestMapperThunk` is the key insight.** The router stays fully transport- and
  type-agnostic; typing is deferred to the one place that knows `TRequest`, with no reflection
  in the mapping itself. This is what let `IAsyncEnumerable<T>` streaming handlers (gRPC) work
  without touching core.
- **Serializer selection as data** (`ISerializerOption` + `IContextPredicate`) rather than
  hardcoded if/else — new formats are additive registrations (`AddXml()` is proof).
- **`IBenzeneResponseAdapter`** is the right write-side seam: headers/status/content-type/body
  as a transport-agnostic surface, with `FinalizeAsync` for buffered transports.
- **The response-handler chain composes** — status-code writing and body writing are separate
  handlers, which is exactly the separation HTML templating will also need.
- **`IRequestContext<TRequest>` short-circuit** gives pre-typed transports (gRPC) a zero-cost
  path around string serialization.
- **Error payloads go through the same negotiated serializer** (`ErrorPayload` via
  `DefaultResponsePayloadMapper`), so XML clients get XML errors.

## 3. Design-level findings (ranked)

### D1 — `ISerializer` is string-based; that is the ceiling on both performance and format flexibility

`string Serialize(...)` / `Deserialize(string)` forces every body through a UTF-16 string.
Consequences:

- Modern serializers are UTF-8-first (`System.Text.Json` serializes to `Utf8JsonWriter`);
  every message pays a UTF-8→UTF-16→UTF-8 round trip, plus large-body allocations (LOH risk).
- **Binary formats (Protobuf, MessagePack, Avro) cannot be represented at all** — a string is
  not a valid carrier for arbitrary bytes. The XML doc on `ISerializer` says "JSON, XML,
  Protobuf, etc." but the contract cannot deliver the "Protobuf, etc." part; the gRPC family
  had to build its own `IGrpcMessageAdapter` partly for this reason.
- The contract is sync-only, which blocks true streaming (relevant to HTML below).

**Recommendation:** introduce a byte-oriented contract alongside (not instead of)
`ISerializer` — e.g. `IPayloadSerializer` with
`void Serialize(Type, object, IBufferWriter<byte>)` and
`object? Deserialize(Type, ReadOnlySpan<byte>)` — and adapt string-based serializers onto it.
`IMessageBodyGetter<TContext>` would gain a sibling that exposes body bytes
(`ReadOnlyMemory<byte>`), with the string-based getters adapted. This is the deepest change
proposed here and the one that decides whether "supports a host of serialisations" includes
binary. It can be done additively: the request mapper prefers the byte path when both sides
support it, and falls back to strings otherwise.

### D2 — Content negotiation is split across the request and response sides and duplicated per format

Today a format registers **three** things that don't know about each other:

| Concern | Mechanism | Selection logic |
|---|---|---|
| Request deserialization | `ISerializerOption<TContext>` | first predicate match, else default |
| Response serialization | `ISerializationResponseHandler<TContext>` | each handler re-checks headers itself |
| Response fallback | `JsonSerializationResponseHandler` | runs "if body not already set" |

The response handlers coordinate only through the implicit *"skip if a body has already been
written"* contract plus DI registration order. That is fragile (register the JSON fallback
before the XML handler and XML silently stops working), duplicated
(`XmlSerializationResponseHandler` has its own private `KeyEquals` copy of
`DictionaryUtils.KeyEquals`), and asymmetric (`ResponseBodyHandler` — used by AspNet.Core —
ignores the whole scheme and unconditionally writes with the single DI `ISerializer` and a
hardcoded JSON content type).

**Recommendation:** unify around a single **media-format registration**:

```csharp
public interface IMediaFormat<TContext>
{
    string ContentType { get; }                         // what it produces
    bool CanRead(TContext context);                     // request-side applicability
    bool CanWrite(TContext context);                    // response-side applicability (Accept)
    ISerializer Serializer { get; }                     // or IPayloadSerializer per D1
}
```

One registration per format drives *both* directions; a small negotiation service picks the
read format from the request content type and the write format from `Accept` (falling back to
the request's format, then the default). `AddXml()` becomes one line, ordering stops mattering,
and the "body already set" convention disappears. The existing types
(`XmlSerializerOption` + `XmlSerializationResponseHandler` + `XmlResponseHandler` +
`ResponseHandler<T,TContext>` + `IBodySerializer` + `BodySerializer<TContext>`) collapse from
six types per format to roughly two.

### D3 — Response format is negotiated on the *request's* `content-type`, never on `Accept`

`XmlSerializationResponseHandler` decides whether to write XML by reading the **inbound**
`content-type` header. A client that sends JSON and wants XML back — or, critically for the
HTML ambition, a browser that submits a form (`application/x-www-form-urlencoded`) and wants
`text/html` back — cannot express that. Request format and response format are independent
dimensions in HTTP; the design currently fuses them. This falls out of D2's negotiation
service for free (`CanWrite` checks `Accept`).

### D4 — HTML templating: possible today, but through the wrong-shaped hole

What HTML rendering needs per response: the model (payload), *which view to render* (derivable
from topic / handler definition — both already available on `IMessageHandlerResult`), the
context (culture, auth), async rendering, and an error page path. Assessment against today's
seams:

- `ISerializationResponseHandler<TContext>` is **already at the right level** — an
  `HtmlSerializationResponseHandler` gets the context and the full `IMessageHandlerResult`,
  can check `Accept: text/html`, and can ignore the passed `IBodySerializer` entirely and
  render a template instead. So nothing structurally blocks HTML.
- But HTML is **not a serializer** — a template renderer needs the topic and context, and
  `ISerializer.Serialize(Type, object)` can express neither. Forcing HTML in as an
  `ISerializerOption` would be a category error; the design should not pretend response
  rendering is always payload serialization.
- `ISerializationResponseHandler.HandleAsync` is **sync** (`void`), but every real template
  engine (Razor and friends) renders asynchronously.
- Error results map to a serialized `ErrorPayload` — an HTML client needs an error *page*;
  there is no seam for "error rendering by format".
- The `IRawStringMessage` escape hatch (handler returns pre-rendered content) half-works:
  `DefaultResponsePayloadMapper` passes the raw string through, but `ResponseBodyHandler`
  then stamps `application/json` on it. A raw HTML response today would be delivered with the
  wrong content type.

**Recommendation:** in the D2 model, make the write side a **renderer**, not a serializer:

```csharp
public interface IResponseRenderer<TContext>
{
    string ContentType { get; }
    bool CanWrite(TContext context, IMessageHandlerResult result);
    Task RenderAsync(TContext context, IMessageHandlerResult result,
                     IBenzeneResponseAdapter<TContext> response);
}
```

Serialization formats get one generic `SerializerResponseRenderer` wrapper (closing the D2
loop); HTML gets an `HtmlTemplateRenderer` that selects a template by topic/definition,
renders async, and owns its error-page mapping. This is the single change that makes the
"even HTML templating" goal a first-class citizen rather than a workaround. Complementary
small fix: replace/augment `IRawStringMessage` with a content-type-aware
`IRawContentMessage { string Content; string ContentType; }`.

### D5 — The sync/async response-handler split is awkward

`IResponseHandler<TContext>` is an empty marker; the container type-switches to
`ISyncResponseHandler` (whose method is named `HandleAsync` yet returns `void`) or
`IAsyncResponseHandler`, and **silently ignores** any handler implementing neither. A single
interface returning `ValueTask` (cheap for sync implementations) removes the marker, the
type-switch, the silent-ignore hole, and the misnamed sync `HandleAsync`.

## 4. Correctness findings

- **C1 — Content-type matching is exact string equality.** `HeaderContextPredicate` and
  `XmlSerializationResponseHandler` compare `headers["content-type"] == "application/xml"`.
  Real HTTP traffic routinely carries parameters (`application/json; charset=utf-8`) and
  differing case — both fail today, silently falling back to JSON. Media-type comparison
  needs to strip parameters and compare case-insensitively. This is the most likely
  to-bite-in-production bug in the whole area.
- **C2 — Empty body silently becomes `Activator.CreateInstance<TRequest>()`.**
  (`RequestMapper.GetBody`.) A handler cannot distinguish "no body" from "empty object", and
  a required-field request passes mapping with all-default properties, deferring the failure
  to validation (if any is registered). Consider returning `null` and letting the existing
  bad-request path in `MessageHandler` respond, or making the fallback opt-in.
- **C3 — Doc/behaviour mismatch on enricher precedence.** `EnrichingRequestMapper`'s remarks
  say "later enrichers can overwrite values set by earlier ones", but `DictionaryUtils.MapOnto`
  only adds keys that are missing (or whose current value is default) — i.e. *earlier* wins.
- **C4 — `JsonSerializationResponseHandler` / `XmlSerializationResponseHandler` new up their
  serializers** (`new JsonSerializer()`, `new XmlSerializer()`) instead of resolving from DI —
  a user who registers customized serializer options gets them on the request side but not on
  these response paths. `ResponseBodyHandler` (DI serializer) and these two disagree today.
- **C5 — `Benzene.Xml.Settings` is mutable global static state** (`ContentTypeKey/Value`
  settable process-wide) — a testing/thread-safety smell; should be per-registration options.
- **C6 — `IRequestMapBuilder.UseDefault` precedence is documented as
  "check the implementation"** — an abstraction whose contract says *implementation-defined*
  for its central question isn't yet a contract. Worth pinning down (or folding into D2's
  format registry, which subsumes it).
- **C7 — Dead code:** commented-out `IRawJsonMessage` block in `DefaultResponsePayloadMapper`.

## 5. Performance findings (against the stated performance goal)

Ordered by measured-likelihood of mattering; none of these matter at low traffic, all matter
at Lambda/gRPC throughput:

- **P1 — `MessageHandlerFactory.Create` does uncached reflection per message:**
  `GetType().GetMethod("CreateMessageHandlerByType")` + `MakeGenericMethod` + `Invoke` on
  every dispatch. The definition set is static after startup — cache a compiled
  `Func<ITopic, IMessageHandler>` per definition (build once via `MakeGenericMethod` +
  `Expression.Lambda`, store on/keyed by the definition).
- **P2 — `MessageHandlerDefinitionLookUp` re-aggregates every finder, re-runs the
  GroupBy-dedup, and linear-scans on every `FindHandler` call.** `CacheMessageHandlersFinder`
  caches *reflection*, but the aggregation and matching re-run per message, allocating arrays
  each time. Build a `Dictionary<(id, version)>` once (invalidate on `IMessageHandlersList`
  mutation) — this compounds with P1 since both run per message.
- **P3 — `GetHeaders` allocates a fresh dictionary per call, and is called repeatedly per
  message:** once per serializer-option predicate, again by the XML response handler, again
  by any header-reading enricher/middleware (the ApiGateway getter runs a five-operator LINQ
  chain each time). Memoize extracted headers per context (a context-items cache slot), or
  give predicates a cheaper "get one header" seam.
- **P4 — `MultiSerializerOptionsRequestMapper.GetMapper` allocates two mapper objects per
  message** (`RequestMapper` + `EnrichingRequestMapper`) although both are pure functions of
  (serializer, getter, enrichers). Cache one composed mapper per selected serializer.
- **P5 — `DictionaryUtils.Enrich` is reflection-per-request:** `typeof(T).GetProperties()`,
  per-property LINQ scans of the enrichment dictionary (O(props × keys) with allocations),
  `PropertyInfo.SetValue`, plus `ToLowerInvariant()` string allocations per key in `MapOnto`.
  Cache a per-type compiled setter map (static generic cache of `Action<T, object>` from
  expression trees) and use `StringComparer.OrdinalIgnoreCase` dictionaries instead of
  lowercasing keys. Note the fast path: when no enrichers are registered,
  `EnrichingRequestMapper` still runs the aggregate — skip it outright on empty.
- **P6 — `Benzene.Xml.XmlSerializer` constructs `System.Xml.Serialization.XmlSerializer(type)`
  per call.** The BCL caches the generated assembly for this constructor so it's not
  catastrophic, but a `ConcurrentDictionary<Type, XmlSerializer>` is one line and removes
  repeated cache-lookup/lock overhead.
- **P7 — string-based bodies throughout** — see D1; the UTF-8↔UTF-16 round trip is the
  structural allocation floor no local fix removes.

## 6. Suggested sequencing

1. **Quick, non-breaking fixes** (hours): C1 media-type comparison, C4 DI-resolved
   serializers, C7 dead code, P1 factory delegate cache, P2 lookup dictionary, P6 XML
   serializer cache, P5 enricher fast-path. None change public API.
2. **Format unification (D2 + D3 + D5)** (a plan-doc-sized piece): `IMediaFormat` /
   negotiation service, single-interface response handlers, collapse the per-format type
   stack. Breaking for anyone implementing `ISerializationResponseHandler` directly — which
   is only `Benzene.Xml` and the two JSON handlers in-repo — and pre-1.0 breaking changes are
   currently acceptable.
3. **Renderer seam (D4)** rides on 2: `IResponseRenderer` + `SerializerResponseRenderer` +
   `IRawContentMessage`; HTML templating then lands later as a normal package
   (`Benzene.Html.Razor`?) with zero core changes.
4. **Byte-oriented serialization (D1)** last — additive, largest payoff for gRPC/binary
   formats and large payloads, and the format registry from step 2 gives it a natural home.

## 7. Bottom line

The bones are right: the thunk-deferred typing, predicate-selected serializers, the
response-adapter seam, and composable response handlers are exactly the shapes a
multi-format, multi-transport core needs, and HTML templating is *already possible* through
`ISerializationResponseHandler` without touching core. What needs work is consolidation and
honesty about categories: negotiation logic is scattered across both sides of the pipe and
keyed on the wrong header for responses; "response rendering" is currently forced to
masquerade as "payload serialization", which is precisely the seam HTML will strain; the
string-based `ISerializer` quietly caps both throughput and the "host of serialisations"
ambition at text formats; and the hot path re-does reflection and re-builds static lookups on
every message. All of it is fixable additively or within the accepted pre-1.0 breaking-change
window, and none of it requires abandoning the current architecture.
