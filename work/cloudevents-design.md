# Benzene CloudEvents Support — Design Proposal

**Status: design proposal, not a committed plan.** This records the approach and open questions for
a `Benzene.CloudEvents` integration so the design can be agreed before an implementation plan is
written. It is a design doc in the spirit of `work/auth-middleware-design.md` / `work/saga-design.md`,
not a `docs/plans/*` implementation plan.

## Why

[CloudEvents](https://cloudevents.io/) (a CNCF spec) is increasingly the lingua franca for
event metadata across ecosystems — Azure Event Grid emits it, Knative and Dapr are built on it,
Google Cloud's Functions Framework delivers it (`ICloudEventFunction<TData>` — which Benzene's
`Benzene.GoogleCloud.Functions.PubSub` already sits behind), and many HTTP/Kafka event producers
now speak the CloudEvents HTTP/Kafka bindings. A Benzene service that can consume and produce
CloudEvents natively interoperates with all of them without a bespoke adapter per source.

The conceptual fit is strong: Benzene's own envelope (`BenzeneMessage`: `topic` / `headers` / `body`)
is essentially a subset of a CloudEvent (`type` / context-attributes+extensions / `data`). Benzene
already does CloudEvent-shaped work in spots (Event Grid, the GCP PubSub CloudEvent trigger) without
a shared abstraction. And the official C# SDK — [`CloudNative.CloudEvents`](https://github.com/cloudevents/sdk-csharp)
(2.x, actively maintained, protocol bindings for HTTP + pluggable JSON/Avro/Protobuf event
formatters) — means we don't hand-roll parsing or encoding, which aligns with Benzene's
serializer-agnostic model.

## The mapping

A CloudEvent ⇆ a Benzene message. The core correspondence:

| CloudEvents attribute | Benzene concept | Notes |
|---|---|---|
| `type` | **topic** | The routing key — exactly how Event Grid's `eventType` and EventBridge's `detail-type` already map to topic. |
| `data` | **body** | The domain payload, decoded via the negotiated `ISerializer`. |
| `datacontenttype` | `content-type` header | Feeds media-format negotiation. |
| `dataschema` **or** a version extension (e.g. `benzeneversion`) | **payload schema version** | Ties directly into the versioning work (`docs/specification/versioning.md`): the `benzene-version` signal expressed as a CloudEvent (extension) attribute. |
| extension attributes | **headers** | CloudEvents extensions are the natural home for Benzene's flat header dictionary (correlation, trace, tenant, version). |
| `id` | correlation / message id | Map to a Benzene correlation header if present, else generate. |
| `source`, `subject` | *(no direct Benzene equivalent)* | Surface as reserved `cloudevents-source` / `cloudevents-subject` headers (mirrors how EventBridge exposes `source` as metadata). Open question below. |

## Shape of the integration

Two directions, both reusing the SDK's formatters/bindings rather than reimplementing them:

- **Inbound** — a generic `CloudEventContext` + getters (`IMessageTopicGetter` → `type`,
  `IMessageBodyGetter` → `data`, `IMessageHeadersGetter` → context attributes + extensions,
  `IMessageVersionGetter` → the version attribute). Because it's keyed on the CloudEvent envelope,
  **one adapter serves any transport that carries CloudEvents** — the HTTP binding (structured or
  binary content mode), Event Grid, PubSub, or Kafka with CE headers — rather than a per-transport
  reimplementation.
- **Outbound** — format a Benzene message as a CloudEvent for publishing: a transport/formatter that
  plugs into `OutboundRoutingBuilder`/`IBenzeneMessageSender`, emitting structured or binary content
  mode over the target binding. Reuses the SDK's `JsonEventFormatter` / Avro / Protobuf formatters so
  it stays serializer-agnostic.

Package: `Benzene.CloudEvents` (core mapping + getters), depending on `CloudNative.CloudEvents` and
the relevant format package(s). Transport-specific glue (e.g. a first-class HTTP CloudEvents binding)
can be thin extensions on top.

## Relationship to the spec

Benzene's `docs/specification/wire-contracts.md` defines the native `BenzeneMessage` envelope.
CloudEvents is a second, industry-standard envelope Benzene can speak. Worth proposing a
`docs/specification/` section that documents the **normative CloudEvents binding** — the attribute
↔ concept mapping above — so that cross-language Benzene ports and external producers have one
agreed contract. This is the same "a concept belongs in the spec before/with the code" rule the
versioning work followed.

## Open questions

- **`source` / `subject`** have no Benzene equivalent. Reserved headers (as above) is the low-risk
  default, but confirm whether any routing/telemetry should key off `source` (EventBridge treats it
  as pure metadata — likely the same here).
- **Content mode** — structured (the whole CloudEvent is the body, one JSON object) vs binary
  (attributes in transport headers, `data` in the body). Binary is the better fit for Benzene's
  existing header/body split; support both, default per binding.
- **Format adapter vs transport binding** — is this one reusable format adapter (preferred, keyed on
  the envelope) or a set of per-transport bindings? Leaning: a reusable core + thin per-binding
  extensions, so HTTP/Kafka/EventGrid don't each reimplement the mapping.
- **Version attribute name** — reuse `dataschema` (standard, URI-typed) or a dedicated
  `benzeneversion` extension (simpler, matches the `benzene-version` header). Leaning: a documented
  extension, with `dataschema` honored if present.
- **Overlap with existing CloudEvent touchpoints** — Event Grid and GCP PubSub already parse
  CloudEvent-shaped payloads their own way; decide whether they migrate onto this shared adapter or
  stay independent (migration is a follow-up, not a prerequisite).

## Next step

If the approach is agreed, promote this into a `docs/plans/cloudevents-plan.md` implementation plan
(inbound adapter first, then outbound formatter, then the HTTP binding and spec section).
