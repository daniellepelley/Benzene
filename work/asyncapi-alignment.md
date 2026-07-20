# AsyncAPI alignment — is Benzene using AsyncAPI correctly?

**Status:** Stage 1 (correct the 2.0 output) **and Stage 2 (3.0 migration)** implemented. Only F5
(reply-channel gating) remains an open follow-up.
**Scope:** `Benzene.Schema.OpenApi`'s `AsyncApi/` builder (per-service `spec?type=asyncapi`) and, downstream,
`Benzene.Mesh.Aggregator`'s `AsyncApiCompositor` (the fleet composite).

## TL;DR
Benzene emits **structurally valid but semantically inverted** AsyncAPI 2.x. The `publish`/`subscribe`
operations are the wrong way round on every channel, so any AsyncAPI consumer reads a Benzene service
backwards (treats its inputs as things it publishes and its outputs as things it consumes). There are
also two smaller correctness issues and a strategic question about moving to AsyncAPI 3.0.

## How AsyncAPI 2.x actually defines publish/subscribe
From the v2.6.0 spec (Channel Item Object), quoted verbatim:

- **`subscribe`**: "a definition of the SUBSCRIBE operation, which defines the messages **produced by the
  application and sent to the channel**."
- **`publish`**: "a definition of the PUBLISH operation, which defines the messages **consumed by the
  application** from the channel."

They are written from **the application's perspective**, and it is famously counter-intuitive:
- `subscribe` ⇒ the app **SENDS / produces**.
- `publish` ⇒ the app **RECEIVES / consumes**.

(AsyncAPI 3.0 dropped these two verbs precisely because they confused everyone, replacing them with an
explicit `action: send` / `action: receive` on top-level operations.)

## What Benzene emits today
`AsyncApiDocumentBuilder.AddMessageHandlerDefinition` (src/Benzene.Schema.OpenApi/AsyncApi/AsyncApiDocumentBuilder.cs):
for a handler on topic `order:create` with request `CreateOrder` / response `OrderCreated`, the real
serialized output is:

```json
"channels": {
  "order:create":               { "subscribe": { "message": { "payload": { "$ref": ".../CreateOrder" } } } },
  "order:create:benzeneResult": { "publish":   { "message": { "payload": { "$ref": ".../OrderCreated" } } } }
}
```

- `order:create` carries the **request the service receives** — but it's under `subscribe`, which per
  spec means "messages the app **sends**". The service does not send `CreateOrder`; it receives it.
  **Should be `publish`.**
- `order:create:benzeneResult` carries the **reply the service sends** — but it's under `publish`, which
  means "messages the app **receives**". **Should be `subscribe`.**

The same inversion applies to `AddBroadcastEventDefinition` and `AddMessageSenderDefinition`: both model
messages the service **sends outbound** (egress / fire-and-forget events), and both emit them under
`publish` — which says the service *receives* them. **Both should be `subscribe`.**

Net effect: a codegen tool pointed at a Benzene service would scaffold a publisher for its inputs and a
subscriber for its outputs — the exact opposite of what the service does.

## Findings

### F1 — publish/subscribe inverted everywhere (Critical, correctness)
As above. Every operation Benzene emits picks the wrong verb. Confirmed against the v2.6.0 spec text and
against live builder output. Fix is a straight swap in four places in `AsyncApiDocumentBuilder`:
- handled request channel: `Subscribe` → `Publish`
- `:benzeneResult` reply channel: `Publish` → `Subscribe`
- broadcast event channel: `Publish` → `Subscribe`
- message-sender channel: `Publish` → `Subscribe`

Downstream: `AsyncApiCompositor` already tags/namespaces both `publish` and `subscribe`, so it needs no
logic change, but the Mesh + builder tests that assert `["subscribe"]` on a handled topic must flip, and
the composite golden expectations update.

### F2 — literal "Summary"/"Description" placeholder strings shipped (Bug)
`AddBroadcastEventDefinition` and `AddMessageSenderDefinition` set `Summary = "Summary"` and
`Description = "Description"` on the operation (AsyncApiDocumentBuilder.cs:139-140, 186-187). These
literal placeholders end up in the published spec. Should be dropped (or filled with something real).

### F3 — missing document-root metadata (Gap)
Per-service docs have no `id`, no `defaultContentType`, no `servers`. `defaultContentType:
"application/json"` is trivially correct (every Benzene body is JSON) and the library supports it on the
document. `id` (e.g. `urn:benzene:service:<name>`) is easy. `servers` is genuinely per-deployment (the
broker/endpoint URLs) and Benzene doesn't know them at spec-build time — best left to the host to supply,
or omitted. (The Mesh composite already sets `id`/`defaultContentType`; this brings the per-service docs
in line.)

### F4 — AsyncAPI 2.0 vs 3.0 (Strategic direction)
The current dependency, **AsyncAPI.NET 4.1.0**, is **2.0-only**: its `AsyncApiVersion` enum exposes only
`AsyncApi2_0`, and `AsyncApiDocument` has no top-level `operations` (it's the 2.x channel-nested model).
Emitting 3.0 would require switching to the maintained **ByteBard.AsyncAPI.NET** fork (3.0 support, still
pre-release) — a **new NuGet dependency** (needs approval per AGENTS.md) — or hand-writing 3.0 JSON.

Why 3.0 is a better fit for Benzene specifically:
- `action: send`/`receive` is unambiguous — the F1 confusion cannot happen.
- Native **`reply`** object models Benzene's request→`:benzeneResult` reply as *one* operation with a
  reply, instead of two loosely-related channels joined only by a `:benzeneResult` naming convention.
- Operations are top-level and reference channels, which composes more cleanly in the Mesh compositor
  (no publish/subscribe-per-channel gymnastics).

### F5 — should every handled topic have a reply channel at all? (Modeling question)
Benzene emits an `:benzeneResult` reply channel for **every** handler. But most Benzene transports
(SNS, SQS, EventBridge, Kafka, Service Bus…) are fire-and-forget — there is no reply on the wire. The
reply only really exists for the synchronous benzene-message / HTTP-invoke / gRPC paths. Emitting a reply
channel for a pure pub/sub consumer over-describes the contract. Options: always emit (today), never
emit, or emit only when a request/reply-capable transport is wired. Ties into F4 (3.0 `reply` makes this
cleaner either way).

## Proposed plan (staged)

**Stage 1 — correctness on 2.0, no new dependency (DONE):**
1. ✅ F1: swapped the publish/subscribe assignments in `AsyncApiDocumentBuilder` (handler request → `publish`,
   `:benzeneResult` reply → `subscribe`, broadcast/event/sender → `subscribe`).
2. ✅ F2: removed the "Summary"/"Description" placeholders.
3. ✅ F3 (partial): added `id` (`urn:benzene:service:<title>`) + `defaultContentType` (`application/json`)
   to the per-service document. `servers` still deliberately omitted (per-deployment, unknown at build time).
4. ✅ Added `Operations_UseTheCorrectAsyncApiPerspective` builder test; existing round-trip/compositor tests
   still pass (the compositor is verb-agnostic). Composite still validates against the real AsyncAPI.NET
   reader with 0 errors.

**Stage 2 — 3.0 migration (DONE):**
- ✅ Swapped `AsyncAPI.NET` 4.1.0 → **`ByteBard.AsyncAPI.NET` 3.0.0** (2.0-only → 3.0 model+serializer;
  test project uses `ByteBard.AsyncAPI.NET.Readers`).
- ✅ `Mapper` now targets ByteBard's `AsyncApiJsonSchema`; `AsyncApiDocumentBuilder` emits 3.0 —
  channels (`address` + `messages`), top-level operations with `action: receive/send`, and the native
  `reply` object for a handler's request→reply channel (address `<topic>:response` by default,
  configurable via `AsyncApiSpecOptions.ResponseTopicSuffix` — replaces the old internal-looking
  `:benzeneResult`). Channel/operation/message map keys sanitized to `^[A-Za-z0-9.\-_]+$` (topic kept
  in `address`). The Mesh `AsyncApiCompositor` drops reserved topics operation-first (by request-channel
  address) and keeps only channels a surviving operation references, so it's suffix-agnostic.
- ✅ `AsyncApiCompositor` rewritten to namespace + merge the 3.0 structure (channels, top-level
  operations, channel-scoped message refs, schema refs; reserved-topic + operation filtering; unused-
  schema pruning; per-operation service tags). Composite emits `asyncapi: 3.0.0`.
- ✅ Verified end-to-end against the real `ByteBard.AsyncAPI.NET.Readers` reader (parses + resolves
  fully) on both a single service and a two-service composite. **Caveat:** ByteBard's
  `AsyncApiOperationRules` "messages MUST be a subset of the referenced channel's messages" validation
  is a false positive — it also fails the AsyncAPI spec's own request/reply example — so it's not a
  signal; the documents are structurally correct and all refs resolve.
- Serializes as `3.1.0` (ByteBard's latest 3.x patch), which also clears Studio's "use the latest
  version" recommendation.

## Remaining open question
- **F5:** Benzene still emits a `reply` (and its `<topic>:response` channel) for **every** handler, even
  fire-and-forget pub/sub consumers with no reply on the wire. Decide whether to gate the `reply` on a
  request/reply-capable transport (benzene-message / HTTP-invoke / gRPC) being wired. Needs transport
  info at spec-build time; not done.
