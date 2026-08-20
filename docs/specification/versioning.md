# Payload Schema Versioning

**Status: implemented.** Both axes ship: handler-version dispatch (`[Message(topic, version)]` +
`IVersionSelector`) and payload schema casting (`Benzene.Core.Versioning` — `ICaster`/`SchemaCaster`,
the request/response casting decorators, `UsePayloadVersionCasting<TContext>()`). The version signal is
read per transport by `IMessageVersionGetter<TContext>` (HTTP: a `/v{version}` route segment with a
`benzene-version` header fallback; every other transport: the `benzene-version` header); it is written
outbound via the `SendAsync(topic, request, version)` / `SendMessageAsync(topic, message, version)`
overloads (`MessageVersionHeaders.Default`). HTTP apps opt into the `/v{version}` route convention with
`AddHttpVersioning()`. The mesh reconciles produced-vs-consumed versions per topic
(`MeshTopicCatalog.VersionCompatibility`, rendered on the Mesh UI topic page). Runnable end-to-end in
`examples/Mesh` (payments-api: `/v1`|`/v2` routes + response downcast) and `examples/K8sMesh`
(orders→payments: v1 request upcast to a single v2 handler over the envelope).

**§5 (Mechanism C — side-by-side service versions) is specified, not implemented.** Its identity
layer is now part of the mesh model (mesh.md §2.4, and §5.5 below), pinned by
`conformance/mesh-service-version-cases.json`; what remains unbuilt is any collector honoring it and
the version-aware outbound routing of §5.4. The §1 axes table marks it accordingly.

## 1. Purpose and scope

A long-lived service inevitably needs to change the shape of a topic's request or response
payload. Without a versioning story, that forces either a breaking change (every producer and
consumer redeployed in lockstep) or a new topic per shape (permanent proliferation). This document
defines Benzene's answer: **producers may publish any payload schema version a topic still
accepts, and a service may keep serving whichever versions it declares support for**, without
staged/lockstep releases.

There are **three independent axes** of "version" in Benzene, easy to conflate and important to
keep separate. Note that they sit at different levels — the first two are *inside* one deployment,
the third is *across* deployments:

| Axis | Answers | Scope | Mechanism |
|---|---|---|---|
| **Handler version** | "Which implementation of this topic's behavior runs?" | Within a service | Shipped: `(topic, version)` → handler, `[Message(topic, version)]`, `IVersionSelector` (core-concepts.md §2, §9) — §3 |
| **Payload schema version** | "What shape is this request/response wire payload?" | Within a service | Shipped: schema casting — §4 |
| **Service version** | "Which immutable release of this service is serving this?" | Across deployments of one service | **Specified, unimplemented**: identity in mesh.md §2.4, routing still open — §5 |

The third is the layer beneath a service's *name*: a name identifies which service, a service
version identifies **which release of it is running**. It is what makes two deliberately co-deployed
builds of one service distinguishable rather than looking like one service that keeps changing its
mind (§5.1).

Note the third axis is an **entity**, not a shape. A service version *has* a contract; it is not
*defined by* one. Two service versions may serve byte-identical topics and payload schemas and still
be different versions — a rewrite, a bug fix, a swapped downstream dependency — which is why its
identity cannot be derived from the contract (§5.2).

A service MAY use any axis alone, or combine them — a handler-version bump for a genuine behavior
change, payload-schema casting absorbing compatible data-shape drift, and a service-version
side-by-side deployment for a change too large to absorb in either. Section 6 gives the full
comparison.

All three are **transport-neutral concepts** per this spec's one rule (README.md): they belong
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

**The route-parameter side has no equivalent answer, and that asymmetry is an open decision** (§7):
the header names are a configurable list precisely because a pre-existing `version` can mean
something else, but the HTTP route parameter is a fixed `version` with no override and no reserved-
name warning — the same collision, unaddressed on the other carrier.

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
isn't a header/route parameter at all) exactly as any other extension point permits. Because the
fallback list is an **application-wide** contract (the same regardless of transport, unlike a
transport's topic attribute/property key), it is set in **one place** rather than per transport:
`services.AddMessageVersionHeaderNames("schema-version", ...)` registers a
`MessageVersionHeaderNames` override that every transport's version getter resolves at message-handle
time (each transport registers its getter via `AddHeaderMessageVersionGetter<TContext>()`, the HTTP
transports via their `HttpMessageVersionGetterBase` subclasses). Registration order relative to the
transport `UseXxx`/`AddXxx` calls does not matter, and when no override is registered every getter
falls back to `HeaderMessageVersionGetter<TContext>.DefaultHeaderNames`. Because every
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
`DeferredRequestMapper<TContext>.GetRequest<TRequest>()` calls
`IRequestMapper<TContext>.GetBody<TRequest>(context)` to materialize the request. A casting request
mapper is a **decorator** around the existing one:

1. Read the incoming version via `IMessageVersionGetter<TContext>` (§2.3) and the topic via the
   existing `IMessageTopicGetter<TContext>`.
2. If no version was signalled, or the topic has no registered schema casters, delegate straight to
   the inner `IRequestMapper<TContext>` unchanged — **zero overhead, zero behavior change** for any
   topic that doesn't opt in (design-principles.md §1's "never require it" rule).
3. Otherwise, look up the caster keyed by **`(topic, incomingVersion, the resolved handler's
   request type)`** — see §4.1.2 for why the target is the request *type* rather than a canonical
   version string. The returned caster's `FromType` **is** the incoming payload's CLR shape — no
   separate version-to-type registry is needed; it falls out of the casters already registered for
   the topic. *(Informative, .NET: `ISchemaCasters.TryGetSchemaCaster(topic, fromSchema, toType)` in
   `Benzene.Core.Versioning.Schemas`, called with `typeof(TRequest)`.)*
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
V3→V4, V4→V5 instead of V1→V2→V3→V4→V5" scenario this design needs to support: if V1→V2,
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

#### 4.1.2 More than one canonical version for a topic

A topic may have **more than one live canonical version** — more than one handler registered for
the same topic, each declaring a different request type. It is rare, but the shape has always
allowed it (`PayloadSchemaVersions.ToSchemas` is an array), and a lookup keyed only by
`(topic, fromVersion)` would be ambiguous the moment it happened.

**The disambiguation rule is normative: resolve the caster whose target type is exactly the
resolved handler's request type.** Routing has already picked the handler before the request is
materialized (§4.1's opening paragraph), so its request type is known and is the only correct
target — the message is being cast *for that handler*, not for the topic in the abstract. A lookup
keyed by `(topic, fromVersion, targetType)` is therefore unambiguous by construction, whatever
`ToSchemas` contains, and no iteration or tie-break over candidate canonical versions is needed on
this path.

*(Informative, .NET: `SchemaCasters` builds a dictionary keyed by
`(Topic, FromSchema, ToType)` and the request mapper passes `typeof(TRequest)`.)*

**Open — the duplicate-key tie-break.** The rule above is unambiguous for a *well-formed* caster
set. It says nothing about a **malformed** one: two casters registered for the same
`(topic, fromVersion, targetType)`. The .NET implementation resolves this with **first registration
wins** (a `TryAdd` on the lookup dictionary, commented "a well-formed set never has one"), which is
silent — the second registration is discarded with no error and no warning, and registration order
across DI modules is not something an application controls closely. Settle whether that is the
intended normative behavior, or whether a duplicate should **fail fast at registration** the way a
duplicate topic registration does. Failing fast is the better fit for a condition the code itself
describes as never happening in a well-formed set; the argument against is that it turns a
previously-tolerated startup into a crash. Until this is decided, a port MUST NOT rely on either
behavior.

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
- **Bespoke request mappers (gRPC)**: `UsePayloadVersionCasting<TContext>` wraps the framework-default
  `MultiSerializerOptionsRequestMapper<TContext>` on the request side, which is not gRPC's real mapper.
  A transport with a bespoke request mapper re-points the request side at *its own* mapper via
  `UsePayloadVersionRequestCasting<TContext, TInnerRequestMapper>()`; the decorator still reads the wire
  body through that mapper (so protobuf-JSON bridging runs) before upcasting. For gRPC this is packaged
  as `Benzene.Grpc.Versioning`'s `AddGrpcPayloadVersioning(...)` — same caster-declaration surface as
  `AddPayloadVersioning`, request side only (gRPC writes its response straight to protobuf via its result
  setter, so there is no response payload mapper to downcast). The response side of the serializer-based
  transports wraps the universal `DefaultResponsePayloadMapper<TContext>` and is unaffected.

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

## 5. Mechanism C — side-by-side service versions

**Status: proposed.** Mechanisms A and B both keep **one deployment** of a service and absorb the
version difference inside it. Mechanism C is the other axis entirely: leave the old deployment
running, deploy the new one **alongside** it, and route each message to whichever deployment speaks
its version. Nothing is replaced until the operator decides to retire it.

Good fit when the change is too large or too risky to absorb by casting: a rewritten
implementation, a genuine behavior change (not just a data-shape change), a migration that needs
independent rollback, or a gradual cutover with real traffic on both versions at once. It is the
deployment-shaped answer to the same problem §3 and §4 answer in code.

### 5.1 The missing identity layer

Today a service's identity in the mesh is its **name**. That is one layer short. A name identifies
*which* service; it does not identify *which release of it is running*, and two deliberately
co-deployed versions of `payments` are indistinguishable from one `payments` that keeps changing its
mind. Three layers are needed, not two:

| Layer | Identifies | Lifetime | Wire field (mesh.md §2) |
|---|---|---|---|
| **Service** | the logical service — `payments` | permanent | `service` (REQUIRED) |
| **Service version** | one **immutable release** of that service — its behavior *and* its contract | added over time, never mutated | `serviceVersion`, else §5.2 |
| **Instance** | one running process | ephemeral, replaced constantly | `instanceId` |

The middle layer is the new concept, and it is an **entity**, not a shape. A service version *has* a
contract — a set of topics, produced topics and payload schemas, frozen for that version's lifetime —
but it is not *defined by* that contract. Over time a service **accumulates** service versions rather
than overwriting one.

That distinction is load-bearing, because **two service versions may serve identical contracts**. A
rewrite, a corrected calculation, a swapped downstream dependency — all change what the service
*does* while changing nothing about the topics or payload shapes it declares. Such a change is a
genuine new service version, and it is one of the most common reasons to run side by side at all: a
behavior change is exactly the kind of risk an operator wants to cut over gradually. Any scheme that
identifies a service version by its contract collapses precisely this case.

Note what this is *not*: the `serviceVersion` field already exists on the wire (mesh.md §2). What
does not exist is any **identity meaning** attached to it — today it is descriptive metadata a
collector may display and nothing keys on. This mechanism promotes it to part of the key, so no new
wire field is required for the declared case.

### 5.2 Version identity is extrinsic

**A service version's identity cannot be derived from its contract.** §5.1's point is the whole
reason: two versions may declare byte-identical topics and schemas and still be different releases,
so any content-derived value — including `descriptorHash` — is a *structural* fingerprint, never an
*entity* identity. Using one as identity silently merges a behavior-only change into its
predecessor, which is the exact scenario side-by-side deployment exists to serve.

Identity must therefore come from outside the contract. It resolves in this order:

1. **Declared.** A non-empty `serviceVersion` on the descriptor is the identity, and is the only
   source that is both stable and human-meaningful — it is what an operator writes in a routing rule
   and names in a rollback. Operators **SHOULD** declare one for every release, and a port **SHOULD**
   make it easy to (a build-time constant, a substituted environment variable).
2. **Substrate revision.** Where the platform itself assigns an immutable per-release identifier, a
   port **MAY** read it as the fallback: a published AWS Lambda version, a Kubernetes ReplicaSet, a
   Cloud Run revision, an Azure deployment slot. This is legitimate precisely because it is
   *extrinsic* — the substrate mints a new one per deployment, not per contract change. Which
   identifier (if any) is available is per-platform, so this is **informative, not normative**, and
   is read the same way `placement` already is (mesh.md §2).
3. **Neither.** The service has exactly **one** service version, and side-by-side is unavailable to
   it. This is not an error and **MUST NOT** be reported as one — it is the status quo, and every
   existing single-deployment service lands here unchanged.

A port **MUST NOT** synthesize an identity from a value that changes independently of releases — a
process start time, a random id per boot, an instance id. Those mint a fresh "version" per replica
or per restart, shattering one release into many phantom siblings.

> **Why case 3 cannot be improved on.** Two replicas of one deployment and two side-by-side versions
> with identical contracts are, to a collector, the same observation: several instances reporting one
> service name and one contract. Without an extrinsic identifier there is genuinely nothing to tell
> them apart, and a spec that claimed otherwise would be inventing a distinction the data does not
> contain. Declaring `serviceVersion` is what supplies the missing information.

### 5.3 Immutability, and what drift still means

A service version is **immutable**: once `(service, version)` has been registered with a contract,
that pair's contract does not change. Everything a collector needs to police that already exists —
it is `descriptorHash`, re-scoped from the service to the `(service, version)` pair:

| Version identity | Contract | Meaning |
|---|---|---|
| same | same | Same version, same contract — ordinary multi-instance. Not drift. |
| **same** | **different** | **Contract drift** — the declared version is lying. Two builds claim one version and disagree about its contract. This is the case the existing rule (mesh.md §5) already catches, and it MUST keep being caught. |
| **different** | different | **Side-by-side versions, contract changed.** Siblings, both valid. Consumers of the changed topics may need to migrate. Today this reads as drift; under this mechanism it MUST NOT. |
| **different** | **same** | **Side-by-side versions, behavior-only change.** Siblings, both valid, and — usefully — *no consumer needs to migrate*, because nothing about the wire contract moved. |

The last two rows are the fix to the identity problem; the second is why a contract fingerprint must
survive as a check rather than being replaced by the label, since a declared version is an
*assertion* and the fingerprint is what verifies it.

The fourth row is worth more than it first appears: "these two versions differ, and their contracts
are identical" is precisely the signal that tells a consumer team it has nothing to do. Reporting it
requires comparing a **contract-only** fingerprint across versions — which `descriptorHash` as
currently defined cannot do, because `serviceVersion` participates in it (mesh.md §2.2), so two
declared versions always hash differently even when their contracts are identical. Resolving that
(narrow the hash, or publish a second contract-only digest beside it) is a normative change to a
conformance-pinned definition and is recorded in §7 rather than decided here.

### 5.4 Routing a message to the right service version

The enabling decision was already made in §2: **the version travels as transport metadata, never
inside the payload.** Metadata is the one layer infrastructure routers can see, so most transports
can route by version with no framework involvement at all. Had the version lived in the body (as in
the source project §2 describes departing from), almost nothing below could work.

| Transport | Version-based routing | How |
|---|---|---|
| HTTP / Kubernetes | Yes, natively | `/v{version}` path or `benzene-version` header rule at the ingress/gateway — ordinary canary machinery |
| SNS | Yes | Subscription **filter policies** on the message attribute the version header maps to; one queue per version |
| EventBridge | Yes | Rules pattern-match the embedded header (transport-bindings.md's `_benzeneHeaders`) |
| Bare queue (SQS, etc.) | **No** — a queue has no routing layer | Front it with a topic/bus that has one, **or** route producer-side (below) |
| Direct invocation (Lambda invoke, gRPC) | Producer-side only | The caller chooses the callee, so the outbound route must be version-aware |

Two placements, and an implementation **MAY** offer either or both:

- **Infrastructure-side** (preferred where available). Producers publish one topic with a version
  header and stay entirely ignorant of how many versions are deployed. Cutover is a routing-rule
  change with no redeploy of anything.
- **Producer-side.** The outbound route resolves `(topic, version) → destination` rather than
  `topic → destination`. This is the only option for bare queues and direct invocation. *(Informative,
  .NET: this is a real gap today — `OutboundRoutingBuilder.Route` keys on topic alone, even though
  `SendAsync(topic, request, version)` already stamps the header. A version-aware route key is the
  one framework change Mechanism C needs.)*

Whichever placement is used, the **absent-version default (§2.2) MUST be encoded in the routing
rule**: a message with no version signal routes to the topic's default version — by convention the
oldest still accepted — so producers that predate versioning keep reaching the deployment that can
still serve them, rather than silently landing on the newest.

### 5.5 What this requires of the mesh *(normative changes to mesh.md)*

Mechanism C cannot be adopted without three amendments to mesh.md. **They are now made** — the
identity layer is part of the mesh model, pinned by `conformance/mesh-service-version-cases.json` —
but **no port implements them yet**, so this remains a proposal until at least one collector does:

1. **§4 — the collector keys the catalog by `(service, serviceVersion)`, not `service`.**
   Re-registration replaces wholesale *within a version*, not across the service. Without this, v2
   registering deletes v1's entry, which is precisely the current failure.
2. **§5 — a descriptor-hash mismatch is drift only within one version.** Two versions of one
   service reporting different hashes is the expected, correct state (§5.3, row 3), not a mismatch
   to surface.
3. **§2 — `serviceVersion`'s meaning is promoted** from descriptive metadata to part of the
   identity, with §5.2's resolution order stated normatively — including that a service with no
   version identity available has exactly one, which is what keeps every existing single-deployment
   service conformant without change.

A collector that has not adopted these degrades predictably rather than dangerously: it sees
repeated re-registration of one service whose contract keeps changing, which is exactly today's
behavior — wrong, but not new.

One presentation consequence was settled in the same pass and one is still open. Settled: a topic's
producer/consumer graph stays keyed by service name, not by `(service, serviceVersion)` — two live
versions both declaring a topic contribute one edge, and the version breakdown lives on the service
view (mesh.md §4). Open: "which versions of this service are live, and which is taking traffic" is
the natural place to *observe* a cutover, but nothing yet renders it — and the cutover itself is a
routing change the mesh does not perform.

### 5.6 Degradation

| Requires | Why | Degradation when declined |
|---|---|---|
| A version identity for the service (§5.2) | Two releases are otherwise the same entity | The service has one service version; side-by-side is unavailable to it. Not an error — the status quo for every service today |
| A version signal on the message (§2) | Routing has nothing to discriminate on without one | Everything routes to the topic's default version (§2.2) — i.e. exactly single-deployment behavior |
| A routing layer that can read it (§5.4) | Something has to act on the signal | Only one version can be live per topic; the mechanism is unavailable, but nothing breaks |
| Version-aware mesh identity (§5.5) | Siblings are otherwise read as one service overwriting itself | Deployment and routing still work; the **catalog** misreports, showing drift where there is none |

Note the third row: Mechanism C's *runtime* behavior does not depend on the mesh at all. The mesh
changes are what make a side-by-side estate **legible**, not what make it function.

### 5.7 Sharp edges

These are constraints on the pattern, not gaps in it, and an implementation guide **MUST** state
them — each one is a live production hazard that routing alone does not address:

- **Event fan-out double-processing.** Side-by-side is clean for *commands*, which have one intended
  recipient. For a subscribed *event*, if two versions both subscribe, the event is processed twice —
  duplicate side effects, not a duplicate read. Exactly one version MUST own a given event
  subscription at a time, and that ownership moves at cutover.
- **Shared state.** Two versions writing one store relocates the breaking change from the wire to
  the database rather than removing it. Side-by-side deployment addresses *contract* compatibility;
  the store still requires expand/contract discipline, and no routing scheme substitutes for it.
- **Unbounded accumulation.** Versions are immutable and additive, so without a retirement policy an
  estate grows service versions forever. Retirement is an operator decision the mesh should make
  visible (which versions still receive traffic) but MUST NOT make automatically.

## 6. Choosing between the mechanisms

| | §3 Handler-version dispatch | §3.1 Casting-handler sugar | §4 Transparent casting | §5 Side-by-side versions |
|---|---|---|---|---|
| **Deployments per service** | One | One | One | **One per live version** |
| New framework code required | None (already shipped) | None (application-level) | Request/response mapper decorators + §4.4 redesign (both shipped) | Version-aware outbound routing (§5.4) + mesh identity (§5.5) — **neither shipped** |
| Handler code duplication | Real, per version | One thin forwarding handler per retired version | None — one handler, always the latest | None — each deployment only knows its own version |
| Where the version difference is absorbed | In the handler set | Explicit, in the forwarding handler | Implicit, in registered `ISchemaCaster`s | **Nowhere in code** — in the routing layer and the deployment topology |
| Rollback granularity | Redeploy the service | Redeploy the service | Redeploy the service | **Per version, independently** |
| Good fit when | Versions genuinely behave differently, not just shaped differently | A quick bridge for one or two retired versions | Many long-lived producer versions, pure data-shape drift, want zero per-version handler code | A rewrite, a risky migration, or a gradual cutover with real traffic on both at once |
| Main cost | Duplicate handler code | A forwarding handler per retired version | Casters must exist in both directions (§4.2.1) | Running, observing and eventually retiring N deployments; the §5.7 hazards |

The mechanisms **compose**, and the useful combination is C with A or B rather than either alone: a
side-by-side deployment handles the genuine behavior change, while casting inside each deployment
absorbs shape-only drift, so C's deployment count tracks *behavioral* versions rather than every
schema revision. C alone, with no casting, means a new deployment for every payload change — which
is where an estate accumulates versions fastest and §5.7's retirement problem bites soonest.

## 7. Open decisions

Mechanisms A and B are implemented (§3, §4), so these are no longer questions "for the
implementation pass" — the pass happened. What remains are decisions still owed on shipped
behavior, plus one verification gap. Each says what state it is in.

- **~~Multiple simultaneous canonical versions per topic.~~ RESOLVED — promoted to
  [§4.1.2](#412-more-than-one-canonical-version-for-a-topic).** The implementation adopted the
  candidate rule this bullet floated: the lookup keys on the resolved handler's request type, so a
  topic with several live canonical versions is unambiguous by construction. One sub-question
  survives the promotion and is recorded there, still open: whether **first registration wins** on a
  duplicate key is the intended normative tie-break or an implementation accident that should fail
  fast at registration instead.
- **OPEN — HTTP route-parameter naming collision.** An application with a domain route parameter
  literally named `version`, for unrelated reasons, makes the default HTTP `IMessageVersionGetter`
  read a domain value as a payload schema version. Neither of the resolutions this bullet offered
  exists: the .NET getter hard-codes the name as a `const` with no override, and §2.1's table calls
  it "conventionally named `version`" with no reserved-name warning attached.

  **The decision, concretely — pick one:**
  1. **Reserve the name.** Add `version` to the reserved-names set alongside the `/benzene/` path
     prefix (design-principles.md §5.1), and say in §2.1 that a route declaring a `version`
     parameter for any other purpose is a conflict the application must resolve. Cheapest; makes
     the collision a documented rule rather than a silent misread, but takes a plain English word
     away from every application that uses HTTP routing.
  2. **Make it configurable.** A settable parameter name on the HTTP version getter, defaulting to
     `version`. Costs an option and a place for two halves of an application to disagree, but takes
     nothing away.
  3. **Both** — configurable, with the default reserved and documented as such.

  **The strongest argument is the asymmetry with the header carrier.** §2.1 already answers this
  exact collision on headers — the fallback name list "MUST be configurable" for the stated reason
  that a pre-existing `version` header can mean something else — and then leaves the identical
  collision on the route parameter unaddressed. Whatever is ruled, the two carriers should answer
  it the same way, which points at option 3 (the shape the rest of the spec uses: a default steer,
  always overridable — design-principles.md's extension-point catalog). The argument against is
  that it is the most work for a collision nobody has yet reported. This needs a ruling before 1.0
  either way, because option 1 alone is a breaking change afterwards.
- **UNCONFIRMED — content negotiation vs version negotiation interplay.** Both decorators are now
  real, which was the precondition this bullet set ("confirm no ordering dependency once both
  decorators are real"). **Nothing records that confirmation as having been done.** The reasoning
  for why they should be orthogonal still holds on inspection — format selection reads
  `content-type`/`accept`, version selection reads `benzene-version`/route, and they share no state
  — but that is an argument, not a check. It is cheap to close: a test that exercises a versioned
  cast and a non-default media format on the same message, in both decorator orders.
- **OPEN — no version-casting conformance fixtures.** The canonical set has no casting sibling to
  `envelope-cases.json`, and the "once implemented" precondition this bullet set has fired: §4 is
  shipped. So **casting-chain composition is verified only against the .NET reference** — its
  shortest-path composition, its shortcut preference, its degradation when no caster exists. That is
  precisely the failure mode `conformance/README.md`'s own rationale warns about: a port can pass
  every existing fixture and still be unable to exchange a versioned message, because nothing
  neutral pins the behavior. Adding the fixture is a spec change of its own — it needs a canonical
  caster set to sit alongside the canonical handler set — and every port then has to claim or
  decline it, which is why it is listed here rather than done in passing.

Specific to Mechanism C (§5):

- **A contract-only fingerprint (§5.3, row 4).** Reporting "these two versions differ but their
  contracts are identical" — the signal that tells a consumer team it has nothing to do — needs a
  digest over the contract *alone*. `descriptorHash` cannot serve: `serviceVersion` participates in
  it (mesh.md §2.2), so two declared versions always hash differently even when their contracts
  match. Two candidates, both normative changes to a conformance-pinned definition and so needing
  their own cross-port pass: **narrow** `descriptorHash` by adding `serviceVersion` to its exclusion
  list (cleaner, but changes an existing hash's value in every port), or **add** a second
  contract-only digest alongside it (purely additive, at the cost of two hashes to explain). The
  first also has a subtlety worth checking: with `serviceVersion` excluded, the hash no longer
  changes on a version bump, so anything today relying on "hash changed ⇒ redeploy" would need to
  key on the version identity instead.
- **Mixed identity sources in one estate.** §5.2 resolves per service, so one service may key on a
  declared `1.4.2` while another keys on a substrate revision, and a third has no version identity at
  all. That is intended (a port cannot force declaration), but a catalog rendering all three needs a
  presentation rule — most likely: mark a substrate-derived identity as such rather than letting it
  read like a name someone chose, and render a service with no version identity exactly as services
  are rendered today, with no empty "version" affordance implying something is missing.
- **What counts as the "default version" for absent-version routing.** §2.2 says the topic's
  default is by convention the oldest still accepted, and §5.4 requires routing rules to encode
  it. Nothing currently *declares* which version that is — it is a convention held in an operator's
  head and duplicated into a routing rule. Candidate: an explicit marker on the descriptor, so the
  mesh can show which version absent-version traffic actually lands on, and flag the case where two
  live versions both believe they are the default.
- **Where the version identity is applied.** §5.5 keys the collector by `(service, serviceVersion)`,
  but every existing read model, artifact document and UI surface is keyed by service name alone
  (`services/{name}.json`, the topic graph's node identity, issue fingerprints — mesh.md §4.1 —
  which include `service` but not its version). Settle whether those become version-keyed, stay
  service-keyed with version as an attribute, or split; doing this piecemeal per surface is how the
  two identities drift apart.
- **Retirement, and evidence for it.** §5.7 requires retirement to be an operator decision, and
  mesh.md §4.2's declared-vs-observed layer already distinguishes a declared edge that no trace has
  exercised. Confirm that signal is sufficient to answer "is anything still calling v1?" — it is the
  question the whole mechanism eventually turns on, and the one an operator will not retire without.
- **Conformance fixtures for sibling versions.** The collector cases currently register one
  descriptor per service. Add cases pinning each row of §5.3's table: two descriptors differing only
  in `serviceVersion` produce two catalog entries rather than one overwriting the other (rows 3–4);
  two differing in contract under one declared version still report drift (row 2); and — the case
  that motivated this mechanism's redesign — **two differing only in `serviceVersion`, with
  byte-identical topics and schemas, still produce two entries** (row 4). That last one is the
  regression guard against re-deriving identity from contract content.
