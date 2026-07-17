# Payload Schema Versioning

**Status: DRAFT v0.1 — design proposal, not yet implemented.** Nothing in this document is
shipped; it exists so the design is agreed before code is written. Once implementation starts,
this document graduates alongside it and this notice is removed.

## 1. Purpose and scope

A long-lived service inevitably needs to change the shape of a topic's request or response
payload. Without a versioning story, that forces either a breaking change (every producer and
consumer redeployed in lockstep) or a new topic per shape (permanent proliferation). This document
defines Benzene's answer: **producers may publish any payload schema version a topic still
accepts, and a service may keep serving whichever versions it declares support for**, without
staged/lockstep releases.

There are **two independent axes** of "version" in Benzene, easy to conflate and important to
keep separate:

| Axis | Answers | Existing mechanism |
|---|---|---|
| **Handler version** | "Which implementation of this topic's behavior runs?" | Already shipped: `(topic, version)` → handler, `[Message(topic, version)]`, `IVersionSelector` (core-concepts.md §2, §9) |
| **Payload schema version** | "What C# shape is this request/response wire payload?" | **New** — this document |

A service MAY use either axis alone, or both together (e.g. a handler-version bump for a genuine
behavior change, with payload-schema casting absorbing compatible data-shape drift so most schema
evolution never needs a new handler version at all). Section 4 gives the full comparison.

Both axes are **transport-neutral concepts** per this spec's one rule (README.md): they belong
here, before or alongside code, in whatever language a Benzene implementation is written in. The
.NET shapes below are illustrative — marked *(informative)*.

## 2. The version header

Version travels as **metadata, not payload**. This is the core requirement driving the whole
design: unlike the source project this feature was adapted from (which read a schema-version
field out of the JSON body itself), Benzene's payload MUST remain exactly what the handler's
declared type serializes to, in whatever format is negotiated (JSON, XML, MessagePack, Avro, or
any future `IMediaFormat`) — because the body is opaque bytes to everything except the selected
serializer, a version signal embedded *in* the body would only ever work for one format. Reading
version off headers keeps versioning orthogonal to serialization, which is the whole point of
generalizing beyond the JSON-only source project.

### 2.1 Wire representation

| Transport shape | Carrier | Notes |
|---|---|---|
| HTTP | A route parameter, conventionally named `version` (e.g. `/v{version}/orders/{id}`, mirroring the existing `/orders/{id}` route-parameter mechanism — transport-bindings.md §2 HTTP, `HttpTopicRoute.Parameters`) | Falls back to the header fallback list below if the matched route declares no `version` parameter, so a service can support both a path-versioned and a header-versioned surface for the same topic without duplicating routes. |
| Every other transport (queues, gRPC, direct invocation, the `BenzeneMessage` envelope) | A header, resolved from an **ordered, configurable fallback list of header names** — default `benzene-version`, then `version`, then `x-version`; the first of these present in the header dictionary wins | Same case-insensitive-on-read, lower-case-on-write rule as every other header in that table. |

Why a fallback list rather than one fixed name: `benzene-version` is the unambiguous, collision-free
default (same reasoning as the `/benzene/` HTTP prefix, design-principles.md §5.1), but plenty of
producers already emit a plain `version`/`x-version` header for their own purposes (API/client SDK
version, not payload schema version) before ever adopting Benzene — the fallback list lets a
service opt into recognizing those without forcing every producer to rename a header first. Because
that is also exactly how it can go wrong — a pre-existing `version` header meaning something else
entirely would be silently misread as the payload schema version — **the fallback list MUST be
configurable**: an application with its own conflicting use of `version` restricts the list to
`["benzene-version"]` only, and one with a different existing convention entirely (say
`schema-version`) replaces the list wholesale. This is the same "default steer, always overridable"
shape as everything else in design-principles.md §4, applied to the list contents rather than to the
getter as a whole — see §2.3.

This adds one row to wire-contracts.md §2's header table:

| Header | Direction | Meaning |
|---|---|---|
| `benzene-version` | inbound (request), outbound (response) | The payload schema version. Absent (and no configured fallback header present either) means "the topic's default version" — see §2.2. `version`/`x-version` are recognized fallback names by default (§2.1), not separate headers with distinct meaning. |

### 2.2 Default version and absence

A message with no version signal (no route parameter, no `benzene-version` header) is treated as
the topic's **default version** — by convention the oldest version the topic still accepts, so
that pre-versioning producers keep working unmodified after versioning is turned on for a topic.
This mirrors the existing rule for handler-version dispatch (core-concepts.md §2: "when a message
arrives without a version, the unversioned handler... handles it") — the same absent-means-default
principle, applied consistently across both axes.

### 2.3 The extension point: `IMessageVersionGetter<TContext>` *(informative, .NET)*

Extracting the version is a **new, replaceable extension point**, following the same
producer/consumer, default/override shape every existing convention uses (design-principles.md
§4):

```csharp
namespace Benzene.Abstractions.Messages.Mappers;

public interface IMessageVersionGetter<TContext>
{
    // Empty/null means "no version signalled" (§2.2), not an error.
    string? GetVersion(TContext context);
}
```

This is deliberately the same shape as the already-shipped `IMessageTopicGetter<TContext>`
(`Benzene.Abstractions.Messages.Mappers`) — same namespace, same one-method extraction contract,
same "null means absent, not an error" rule. Every transport binding registers a default
implementation (HTTP: route parameter then the header fallback list; every other transport: the
header fallback list only), replaceable exactly like a topic getter is.

The header fallback list (§2.1) is a constructor/options parameter on the default implementation —
e.g. `new HeaderMessageVersionGetter<TContext>(headersGetter, headerNames: ["benzene-version",
"version", "x-version"])` — not a hard-coded scan order, so an application can narrow, reorder, or
fully replace it via its own DI registration without writing a new `IMessageVersionGetter<TContext>`
from scratch, while still being free to replace the whole getter (e.g. for a version signal that
isn't a header/route parameter at all) exactly as any other extension point permits. Because every
transport already registers an `IMessageHeadersGetter<TContext>` mapping its native metadata onto
the flat header dictionary (wire-contracts.md §2), one generic `HeaderMessageVersionGetter<TContext>`
built against that (not the native transport type) can serve as the default for every transport
except HTTP — which layers the route-parameter check in front of the same generic header fallback,
rather than needing its own from-scratch header scan.

**This closes a real gap in the current implementation, independent of which mechanism (§3 or §4)
a service adopts**: every existing `IMessageTopicGetter<TContext>` implementation constructs
`new Topic(id)` — never `new Topic(id, version)` — so `ITopic.Version` is always empty coming off
the wire today, even though `IVersionSelector`/`MessageAttribute`'s dispatch-by-version machinery
(§3) has been fully wired on the lookup side since it shipped. Nothing currently populates the
producer side of that contract. `IMessageVersionGetter<TContext>` is that missing producer;
`MessageRouter<TContext>` combines its output with the existing topic getter's output into one
`ITopic(id, version)` before calling `IMessageHandlerDefinitionLookUp.FindHandler`, which is the
one-line change §3 needs.

## 3. Mechanism A — handler-version dispatch (multiple handlers)

**Status: mostly shipped.** Register two handlers for the same topic, one per version, e.g.:

```csharp
[Message("order:create", "V1")]
public class CreateOrderV1MessageHandler : IMessageHandler<CreateOrderRequestV1, CreateOrderResponseV1> { ... }

[Message("order:create", "V2")]
public class CreateOrderV2MessageHandler : IMessageHandler<CreateOrderRequestV2, CreateOrderResponseV2> { ... }
```

`IVersionSelector` (default: exact match, else highest available version — `VersionSelector.cs`)
picks between them per message once §2.3 wires the incoming version onto `ITopic`. This is exactly
the "much simpler" option: no casting, no new abstractions, real duplicate handler code — but that
duplication is usually thin, because the divergent part is normally just request/response shape
and mapping to a shared internal service/domain call, not the business logic itself.

### 3.1 Sugar: a casting handler

A team that wants only *one* real implementation (the latest) but still needs to accept an older
producer can get there today, with **zero framework changes**, by writing a small V1 handler that
upcasts and delegates:

```csharp
[Message("order:create", "V1")]
public class CreateOrderV1MessageHandler : IMessageHandler<CreateOrderRequestV1, CreateOrderResponseV1>
{
    private readonly CreateOrderV2MessageHandler _v2; // or a shared internal service either calls

    public async Task<IBenzeneResult<CreateOrderResponseV1>> HandleAsync(CreateOrderRequestV1 request, ...)
    {
        var v2Request = _caster.Cast(request); // ICaster<V1,V2> from Benzene.Core.Versioning (§4.4)
        var v2Result = await _v2.HandleAsync(v2Request, ...);
        return v2Result.Map(_downcaster.Cast); // ICaster<V2,V1>
    }
}
```

This is application-level composition over already-shipped pieces
(`Benzene.Core.Versioning.CasterBuilder.CasterFactory<TFrom,TTo>` for the cast, ordinary DI for the
delegation) — it needs no new abstraction in the message-handlers packages at all, which is why
it's framed as sugar rather than a third mechanism: it is mechanism A, with the duplicate code
shrunk to one small forwarding handler per retired version. **This is the recommended starting
point** for a team not yet ready to adopt §4's fully transparent casting, and MAY be documented as
a cookbook (`docs/cookbooks/`) rather than framework code once implemented.

## 4. Mechanism B — transparent payload casting (single handler)

**Status: implemented** (`Benzene.Core.Versioning`, opt-in per transport via
`UsePayloadVersionCasting<TContext>()`). One handler serves a topic, written
against the **latest** schema version. Producers on older (or newer) versions are transparently
upcast (or downcast) at the edges of the pipeline; the handler never sees any version but its own.

### 4.1 Request path

Hooks into `IRequestMapper<TContext>` (`Benzene.Core.MessageHandlers.Request`), which already sits
exactly at the right seam: `MessageRouter<TContext>` resolves the topic and handler definition
first (so `messageHandlerDefinition.RequestType` — the canonical/latest shape — is known) *before*
`RequestMapperThunk<TContext>.GetRequest<TRequest>()` calls
`IRequestMapper<TContext>.GetBody<TRequest>(context)` to materialize the request. A casting request
mapper is a **decorator** around the existing one:

1. Read the incoming version via `IMessageVersionGetter<TContext>` (§2.3) and the topic via the
   existing `IMessageTopicGetter<TContext>`.
2. If no version was signalled, or the topic has no registered schema casters, delegate straight to
   the inner `IRequestMapper<TContext>` unchanged — **zero overhead, zero behavior change** for any
   topic that doesn't opt in (design-principles.md §1's "never require it" rule).
3. Otherwise, look up the caster for `(incomingVersion, canonicalVersion, topic)` via the
   already-shipped `ISchemaCasters.GetSchemaCaster(...)` (`Benzene.Core.Versioning.Schemas`). The
   returned `ISchemaCaster.FromType` **is** the incoming payload's CLR shape — no separate
   version-to-type registry is needed; it falls out of the casters already registered for the topic.
4. Deserialize the raw body as `FromType` using the **negotiated `ISerializer`** (not a JSON-specific
   path — `ISerializer.Deserialize(Type, string)`, or `IPayloadSerializer.Deserialize(Type,
   ReadOnlySpan<byte>)` on the byte-oriented path `RequestMapper<TContext>` already prefers when
   available). This is what makes the mechanism serializer-agnostic: MessagePack, XML, and Avro all
   already implement `ISerializer`/`IPayloadSerializer` with the same `Type`-parameterized shape.
5. Invoke the caster (`ICaster<TFrom,TTo>.Cast`, dispatched via reflection the same way the
   existing `PayloadDeserializer`/`SchemaCastDefinitionsExpander` already do) to upcast into
   `TRequest`, and return that.

#### 4.1.1 Long-lived version back-catalogs and shortcut casters

A service supporting many still-live versions (e.g. currently on V5 but still accepting producers
as old as V1) does not need a direct V1→V5 caster registered, and does not always chain step by
step through every intervening version either. `ISchemaCasters.GetSchemaCaster(...)` is backed by
`SchemaCastDefinitionsExpander`, already shipped and unchanged by this proposal (§4.4), which
resolves any requested `(from, to, topic)` pair by:

1. Reusing a directly-registered caster for that exact pair if one exists.
2. Otherwise, finding the shortest path (fewest composed casters) between the two versions over
   whatever casters *are* registered for the topic, via breadth-first search
   (`SchemaCastDefinitionsExpander.GetChain`), and composing them with `CompositeCaster<TFrom,
   TIntermediate,TTo>`.

Because it is breadth-first, a **shortcut caster is automatically preferred over a longer chain
through intermediate versions whenever both exist** — exactly the "V1→V3 direct, so use V1→V3,
V3→V4, V4→V5 instead of V1→V2→V3→V4→V4→V5" scenario this design needs to support: if V1→V2,
V2→V3, V3→V4, V4→V5, **and** V1→V3 are all registered, resolving V1→V5 composes `[V1→V3 (direct),
V3→V4, V4→V5]` (3 casters), never revisiting V3 via the longer V1→V2→V3 route, because BFS marks a
version visited — and therefore never reconsiders it — the first time any edge reaches it, which
for V3 happens on the direct edge in the same BFS layer the V1→V2 edge is explored (both are
one hop from V1). **No change is needed for this to work — it is already exhaustively covered by
the existing `SchemaCastDefinitionsExpanderTest` shortcut-preference test.** Registering fewer
shortcut casters still works (the full step-by-step chain is the fallback), and registering *more*
shortcuts only ever shortens future chains — there is no scenario where adding a shortcut caster
makes an existing resolution worse or ambiguous, since exact-pair reuse (step 1 above) and shortest-
path composition (step 2) are both deterministic given a fixed set of registered casters.

### 4.2 Response path

Symmetric, and hooks into `IResponsePayloadMapper<TContext>`
(`Benzene.Core.MessageHandlers.Response`) the same way — again a decorator, not a replacement:

1. The handler has already produced its result in the **canonical** response type
   (`messageHandlerDefinition.ResponseType`) — the handler is never aware any casting happens.
2. Read the version again via `IMessageVersionGetter<TContext>` against the same `context` the
   decorator already has (no cross-request-to-response state needed — the getter is a pure,
   idempotent read of the immutable context, called twice rather than threaded through, keeping this
   simpler than the `PresetTopicHolder` pattern `Benzene.Core.MessageHandlers` uses elsewhere for a
   genuinely different problem — a topic *override*, not a repeatable read).
3. **Default: symmetric versioning** — respond in the same version the request declared, so a V1
   producer always gets a V1 response back without needing a separate "Accept-Version" negotiation.
   This default MUST be overridable (design-principles.md §4's rule) for services that want
   asymmetric negotiation; the override point is replacing this decorator's registration.
4. Downcast the canonical payload via `ISchemaCasters.TryGetSchemaCaster(topic, canonicalType,
   requestedVersion, out caster)` (reverse direction from §4.1) and serialize the result as
   `caster.ToType` with the negotiated `ISerializer` — again format-agnostic.

#### 4.2.1 As implemented *(informative, .NET)*

- **Opt-in per transport**: `services.UsePayloadVersionCasting<TContext>()` wraps that context's
  `IRequestMapper<TContext>`/`IResponsePayloadMapper<TContext>` with `CastingRequestMapper<TContext>`
  / `CastingResponsePayloadMapper<TContext>`. Call it **after** the transport's own registration
  (`UseHttp`/`AddSqs`/… + `AddMessageHandlers`), so the closed decorator registrations win, and pair
  it with `RegisterSchemaCastDefinitions` + `RegisterPayloadSchemaVersions`.
- **Type-keyed lookup, not a version-string pair**: neither decorator ever knows both version
  strings — the request side has `(topic, incomingVersion, TRequest)`, the response side
  `(topic, ResponseType, requestedVersion)`. Two `TryGetSchemaCaster` overloads on `ISchemaCasters`
  match on one version string + one CLR `Type`, backed by O(1) indexes built once on the singleton.
  This is also what resolves the "multiple canonical versions per topic" open question below:
  matching by the handler's actual request/response `Type` sidesteps ever needing a canonical
  version string.
- **Register both directions.** The upcast (request) and downcast (response) are *different* casters:
  V1→V2 does not give you V2→V1. `RegisterPayloadSchemaVersions`'s expander only generates the
  `FromSchemas → ToSchemas` direction, so **symmetric response casting requires the reverse pairs to
  exist too** — the simplest way is to list every live version in *both* `FromSchemas` and
  `ToSchemas` (the expander then composes every needed pair, up and down, reusing direct casters and
  chaining where none exists). A topic that only ever upcasts requests and doesn't downcast responses
  needs only the forward direction.
- **Known limitation**: the request decorator wraps the framework-default
  `MultiSerializerOptionsRequestMapper<TContext>`; a transport that registers a bespoke request
  mapper (e.g. gRPC's protobuf-JSON one) is not wrapped on the request side. The response side wraps
  the universal `DefaultResponsePayloadMapper<TContext>` and is unaffected.

### 4.3 Degradation

Per design-principles.md §3's normative pattern, this capability's requirement and degradation:

| Requires | Why | Degradation when declined |
|---|---|---|
| `Benzene.Core.Versioning` schema casters registered for the topic (§4.4) | The decorators no-op without a registered `ISchemaCasters` entry for `(topic, incoming/target version)` | The request/response mapper decorators pass through unchanged — behaves exactly as an unversioned topic; not an error |
| `IMessageVersionGetter<TContext>` returning a real signal | Casting only ever triggers for a version that differs from the canonical one | A topic with no version signalled always takes the canonical path — the same "absent means default" rule as §2.2 |

### 4.4 Required redesign of `Benzene.Core.Versioning` — done

The package as imported (see its `CLAUDE.md`) was built for a prior project whose wire format put
the schema version and topic *inside* the JSON body (`IPayloadFields.GetSchemaVersion(JsonElement)`
/ `GetTopic(JsonElement)`), and `PayloadDeserializer.Deserialize<T>(JsonElement json)` was written
directly against `System.Text.Json`. Both assumptions are wrong for Benzene and MUST be removed
before this mechanism can be wired up generally:

- **Remove** `IPayloadFields`, `IPayloadSchemaVersionLookUp`, `PayloadSchemaVersionLookUp`, and the
  `JsonElement`-typed `IPayloadDeserializer`/`PayloadDeserializer` — version and topic now come from
  `IMessageVersionGetter<TContext>`/`IMessageTopicGetter<TContext>` on the context (§2.3), never
  from inside the body.
- **Replace** the JSON-specific deserialization step with one against `ISerializer`/
  `IPayloadSerializer` (`Benzene.Abstractions.Serialization`), keyed by the `Type` the resolved
  `ISchemaCaster.FromType`/`.ToType` already carries (§4.1 step 3–4, §4.2 step 4) — the same
  `Type`-parameterized shape every serializer (JSON, XML, MessagePack, Avro) already implements, so
  no per-format branching is needed in the versioning package itself.
- **Keep unchanged**: `ICaster<TFrom,TTo>`, `CasterFactory<TFrom,TTo>`/`CasterFuncBuilder` (the
  property-mapping compiler), `ISchemaCaster(s)`, `SchemaCastDefinitionsExpander` (chain
  composition), and the `SchemaCastersBuilder`/`RegisterSchemaCastDefinitions`/
  `RegisterPayloadSchemaVersions` DI registration surface — none of that is JSON-coupled; it operates
  on CLR types and delegates throughout. This is the majority of the package by volume and needs no
  change for this mechanism.

## 5. Choosing between the mechanisms

| | §3 Handler-version dispatch | §3.1 Casting-handler sugar | §4 Transparent casting |
|---|---|---|---|
| New framework code required | None (already shipped) | None (application-level) | Request/response mapper decorators + §4.4 redesign (both shipped) |
| Handler code duplication | Real, per version | One thin forwarding handler per retired version | None — one handler, always the latest |
| Where casting logic lives | N/A (no casting) | Explicit, in the forwarding handler | Implicit, in registered `ISchemaCaster`s |
| Good fit when | Versions genuinely behave differently, not just shaped differently | A quick bridge for one or two retired versions | Many long-lived producer versions, pure data-shape drift, want zero per-version handler code |
| Both MAY be combined | — | — | A handler-version bump handles genuine behavior changes; casting absorbs shape-only drift within a handler version |

## 6. Open questions for the implementation pass

- **Multiple simultaneous canonical versions per topic.** §4.1 assumes one canonical version per
  topic (the resolved handler's declared type). A topic with more than one live canonical version
  (rare, but the already-shipped `PayloadSchemaVersions.ToSchemas` is an array, suggesting the
  original design considered it) needs an explicit disambiguation rule before implementation —
  candidate: match the schema caster whose `ToType` equals `messageHandlerDefinition.RequestType`
  exactly, iterating `ToSchemas` until found.
- **HTTP route-parameter naming collision.** If an application already has a domain route parameter
  literally named `version` for unrelated reasons, the default HTTP `IMessageVersionGetter` would
  misfire. Needs either a documented reserved-name warning (similar to `/benzene/` prefix
  reasoning, design-principles.md §5.1) or a configurable parameter name.
- **Content negotiation vs version negotiation interplay.** §4.2's symmetric-by-default response
  versioning and the existing `IMediaFormatNegotiator<TContext>` (format: JSON/XML/...) are
  orthogonal today; confirm no ordering dependency once both decorators are real (format selection
  reads `content-type`/`accept`, version selection reads `benzene-version`/route — no shared state).
- **Conformance fixtures.** Once implemented, add version-casting cases to
  `docs/specification/conformance/` (envelope-cases.json sibling) so other-language ports can
  verify their casting chain composition against the same fixtures as the .NET reference.
