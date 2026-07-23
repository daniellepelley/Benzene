# Benzene outbound model — making it introspectable and symbolic (plan)

**Status:** design proposal / plan — no code changes accompany this. Follow-on from
`work/benzene-clients-redesign-plan.md` (whose 4 steps shipped) and `work/deployment-descriptor-design.md`.
This solves the thing the clients redesign explicitly deferred in its §3: *"the topic → transport
binding is external configuration … stays explicitly authored,"* with no introspectable record of it.

**Why now:** the deployment-descriptor tool (`tools/Benzene.Descriptor`) needs, per produced topic, the
**transport kind** and a **destination reference**. Today it recovers `transportKind` by *fragile
reflection* into the built routing table, and cannot recover the destination at all — because the
current model keeps neither in an introspectable form, and the destination reaches Benzene as an
already-resolved string (its config/env-var name lost). This plan fixes the model so the tool reads a
clean read-model instead of reflecting, and so a symbolic destination survives to the descriptor.

---

## 1. Current shape (verified)

- `IBenzeneMessageSender.SendAsync<TReq,TResp>(topic, request, headers?)` — business-logic surface.
- `services.AddOutboundRouting(routing => routing.Route("shipping:book", p => p.UseSqs(queueUrl)))` —
  one `IMiddlewarePipeline<OutboundContext>` per topic. `AddOutboundRouting` registers
  `DefaultBenzeneMessageSender` (holds `IReadOnlyDictionary<string, IMiddlewarePipeline<OutboundContext>>`)
  and `OutboundRoutingTopics` (the topic *set* only).
- `UseSqs(string queueUrl)` / `UseSns(string topicArn)` / `UseEventBridge(source, eventBusName)` etc.
  `app.Convert(new OutboundSqsContextConverter(queueUrl, …), …)`. The transport is implied by the
  converter type; the destination is a **resolved string** captured in a private converter field
  (`_queueUrl`/`_topicArn`/`_eventBusName`).

Two consequences that make the model unfit for the descriptor:

1. **No read-model.** Nothing exposes `{topic → (transport, destination)}`. `OutboundRoutingTopics`
   gives topics only; transport + destination are buried in private fields inside lazily-built
   pipeline middleware. (The descriptor tool reflects into `DefaultBenzeneMessageSender._routes` →
   pipeline → `ContextConverterMiddleware<OutboundContext, TOut>` → `TOut` name for the transport. It
   deliberately does **not** attempt the destination.)

2. **The destination is resolved, not symbolic.** Every real caller writes
   `Environment.GetEnvironmentVariable("SHIPPING_QUEUE_URL")` (or `config["…"]`) and passes the *value*
   to `UseSqs(value)`. Benzene never sees the key `SHIPPING_QUEUE_URL`, so a descriptor can't emit a
   destination reference an IaC generator could bind — the very thing the descriptor→IaC story needs.

**Prior art in-repo:** `examples/AwsMesh/Shared/OutboundSend.cs` already models the missing shape by
hand — `record OutboundSend(string Topic, Type MessageType, OutboundTransport Transport, string TargetEnvVar)`
— and uses it to drive *both* the spec's produced-events *and* the runtime route. That userland
duplication is the signal for what the framework should offer.

## 2. Goals

1. **Introspectable read-model** — a DI-registered `{topic → OutboundRoute}` populated at registration,
   read directly (no reflection) by the descriptor tool, `ValidateOutboundRouting`, and the mesh.
2. **Symbolic destinations** — retain the *reference* (config/env-var key, or a logical name), resolved
   to a value at runtime. The descriptor emits the reference; the operator's IaC binds the physical
   resource. Benzene never needs to know the physical name.
3. **Cloud-agnostic transport naming** — each route carries a transport *name* string (`sqs`, `sns`,
   `eventbridge`, `servicebus`, …), declared by the transport, not inferred from a type name.
4. **One source of truth for a produced event** — a produced topic's spec declaration and its outbound
   route should be one statement, not two hand-correlated ones (kill the `OutboundSend` duplication).
5. **Preserve the shipped runtime** — still middleware-based, still `SendAsync(topic, …)`; additive
   first, cutover second (pre-1.0, same staged discipline as the clients redesign).

## 3. Proposed model

### 3.1 `DestinationRef` — a symbolic destination (the crux)

```csharp
namespace Benzene.Clients;

public sealed record DestinationRef(string Kind, string Value)
{
    public static DestinationRef Config(string key)  => new("config", key);   // resolved via IConfiguration[key]
    public static DestinationRef Literal(string val) => new("literal", val);  // used as-is
    // (extensible: "secret", "ssm", … resolved by a registered IDestinationResolver)
}
```

- **Runtime:** the transport converter no longer takes a raw string; it takes a `DestinationRef` and
  resolves it via an injected `IDestinationResolver` (default: `IConfiguration` lookup for `config`,
  passthrough for `literal`). Same send behaviour, one indirection.
- **Introspection:** the *reference* (`config:SHIPPING_QUEUE_URL`) is retained verbatim — that's what
  the descriptor emits as `destinationRef`.
- **Back-compat:** keep `UseSqs(string)` as sugar for `UseSqs(DestinationRef.Literal(string))`, so
  existing call sites compile unchanged (they just show up as `literal` destinations — honest).

### 3.2 `OutboundRouteCatalog` — the read-model

```csharp
public sealed record OutboundRoute(string Topic, string Transport, DestinationRef Destination, Type? PayloadType);

public interface IOutboundRouteCatalog { IReadOnlyCollection<OutboundRoute> Routes { get; } }
```

`AddOutboundRouting(...)` registers a populated `IOutboundRouteCatalog` singleton alongside the sender.
Each transport `Use*` reports its `(transport, destination)` for the route being built; `Build()`
collects them keyed by topic. The descriptor tool then replaces its reflection inspector with a direct
`resolver.GetService<IOutboundRouteCatalog>()`.

**How the transport reports itself** (kills the reflection *and* the type-name inference): each outbound
converter/middleware declares its transport name — e.g. an `IOutboundTransport { string Transport { get; } DestinationRef Destination { get; } }` marker on the converter — and `Route` collects it from the
pipeline it just built. The transport name is authored by the transport package (SQS says `"sqs"`),
not derived from `SqsSendMessageContext`.

### 3.3 Unified declaration (optional, higher-value)

Fold produced-event declaration and route wiring into one call, so the framework offers what
`OutboundSend` does by hand:

```csharp
routing.Produces<TakePaymentRequest>("shipping:book").ToSqs(DestinationRef.Config("SHIPPING_QUEUE_URL"));
```

This single statement (a) adds the spec `events` entry (today `AddResponseEventDeclarations`), (b) wires
the runtime route, and (c) records the catalog entry with `PayloadType`. One source of truth →
descriptor `produces[]` is complete and can never drift from the runtime route.

## 4. Phasing (each independently committable + CI-gated)

**Phase 1 — read-model, non-breaking.** Add `OutboundRoute`/`IOutboundRouteCatalog`; have the existing
`Use*` extensions report `(transport, destination-as-literal)`; register the populated catalog.
Transport name declared by each transport package. *No API break.* Immediately lets the descriptor tool
drop its reflection inspector and read `transportKind` from the catalog.

**Phase 2 — symbolic destinations.** Introduce `DestinationRef` + `IDestinationResolver`; add
`UseSqs(DestinationRef)` overloads (string overload → `Literal`); converters resolve at runtime. Catalog
records the symbolic ref. *Additive.* Descriptor gains `destinationRef`.

**Phase 3 — unified `Produces<T>(topic).ToXxx(dest)` declaration.** Collapse
`AddResponseEventDeclarations` + `AddOutboundRouting` for produced topics into one statement; migrate the
AwsMesh example off its hand-rolled `OutboundSend`. *Additive; old two-call form stays until cutover.*

**Phase 4 — descriptor tool switch-over.** Replace `OutboundRouteInspector` (reflection) with a direct
`IOutboundRouteCatalog` read; add `destinationRef` to `produces[]`; delete the reflection code and its
"spike-grade" caveats. Update `work/deployment-descriptor-design.md`.

**Phase 5 — cutover (optional, breaking).** Once nothing uses the resolved-string `UseSqs(string)` for
real destinations, consider making `DestinationRef` the only overload. Pre-1.0; `CHANGELOG` +
`docs/migration-alpha-to-1.0.md`, matching the clients-redesign precedent.

## 5. Open questions (decide before Phase 2)

1. **Resolution timing.** Resolve `DestinationRef` per-send (injected `IConfiguration`, supports config
   reload) or once at build (matches today's eager string)? *Lean: per-send via `IDestinationResolver`,
   retaining the symbolic ref for introspection.*
2. **Transport name source.** A marker interface on the converter (`IOutboundTransport.Transport`) vs. a
   parameter on `Route`/`Use*`. *Lean: marker on the converter — the transport owns its own name, and it
   composes with custom pipelines.*
3. **Fan-out.** Keep one route (one transport, one destination) per topic — SNS/EventBridge fan out
   downstream — or allow multiple destinations per topic? *Lean: one-per-topic now; note multi-destination
   as future.*
4. **Scope vs. `IBenzeneMessageClient` concrete clients.** The `*BenzeneMessageClient` transport classes
   also hold destinations but are a separate, load-bearing lower layer (per the clients redesign's Step-4
   correction). *Lean: scope this to outbound **routing** only; leave the concrete clients alone.*
5. **`DestinationRef.Kind` set.** Ship `config`/`literal` first; `secret`/`ssm`/`keyvault` via
   `IDestinationResolver` later, or bake an enum now? *Lean: open string + resolver, cloud-agnostic.*

## 6. What this deliberately does NOT change

- `IBenzeneMessageSender.SendAsync(topic, …)` business-logic surface — unchanged.
- The middleware-per-route runtime — unchanged in shape; the converter gains a `DestinationRef`.
- Physical resource names — still never in Benzene; the descriptor emits a *reference*, the operator's
  IaC binds it. That boundary is the point (see `work/deployment-descriptor-design.md`).

## 7. Landing check (definition of "fit for purpose")

- The descriptor tool reads `{topic → transport, destinationRef}` from a public read-model, no reflection.
- A produced event's spec entry, runtime route, and descriptor entry come from one declaration.
- `destinationRef` is symbolic (`config:SHIPPING_QUEUE_URL`), so an IaC generator can bind the queue
  without Benzene knowing its ARN.
- Existing services compile unchanged through Phases 1–3.
