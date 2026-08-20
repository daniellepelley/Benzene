# Benzene CloudEvents Support — Design Proposal

**Status: OVERTAKEN BY AN IMPLEMENTATION. A decision is owed.** benzene-go has shipped a complete
CloudEvents 1.0 ↔ Benzene binding that **no specification section pins and no conformance fixture
covers**, and whose concrete choices contradict this document in several places. Read §0 before
anything below it: the rest of this page is a proposal that was never ruled on, and Go's package is
currently the only thing that defines what a Benzene CloudEvent looks like.

---

## 0. What shipped, where it diverges, and what has to be decided

### 0.1 What shipped

benzene-go, `cloudevents/{cloudevents.go,http.go}` (plus tests and an example), verified on `main`
at `b44d53c`. It is not a sketch: JSON structured representation, both HTTP content modes
(structured `application/cloudevents+json` and binary `ce-*` headers), attribute validation,
`data_base64` handling, an `http.Handler` entry point, and an ack contract of 204 for a success
status / 500 for a non-success one / 400 for a delivery that is not a valid CloudEvent
(`http.go:16`, `:27-30`, `:70-73`). It takes no third-party dependency.

No other port ships anything comparable. .NET, TypeScript and Python each parse CloudEvent-shaped
payloads in the two places that force it — Event Grid and the GCP PubSub CloudEvent trigger — which
is the pre-existing situation §"Overlap with existing CloudEvent touchpoints" describes, not a
binding.

**And `grep -ri cloudevent docs/` in this repository returns nothing.** There is no spec section, no
fixture, and no row in any conformance table.

### 0.2 Where Go diverges from this document

| Concern | This document proposes | benzene-go ships |
|---|---|---|
| Context attributes → headers | Only `source`/`subject` get reserved header names, spelled `cloudevents-source` / `cloudevents-subject` — they are the two with no Benzene equivalent | **Every** context attribute becomes a `ce-`-prefixed header: `ce-id`, `ce-source`, `ce-subject`, `ce-specversion`, `ce-time`, `ce-datacontenttype`, `ce-dataschema` (`cloudevents.go:11-16`, `:175-181`) |
| Extension attributes → headers | Map onto the Benzene header dictionary directly — "the natural home for Benzene's flat header dictionary (correlation, trace, tenant, version)" | Prefixed too: extension `foo` → header `ce-foo` (`cloudevents.go:183`) |
| `datacontenttype` | Becomes the `content-type` header, feeding media-format negotiation | Becomes `ce-datacontenttype`. Go has no media-format negotiator at all (it is JSON-only), so nothing reads it there — but the mapping is the cross-language contract, and as written a port that *does* negotiate would not see the declared content type |
| `id` | Map to a Benzene correlation header if present, else generate | Becomes `ce-id`; correlation is untouched |
| Payload schema version | Open question — `dataschema` or a `benzeneversion` extension, leaning to the extension with `dataschema` honoured | Neither. `dataschema` becomes `ce-dataschema` and an extension named `benzeneversion` becomes `ce-benzeneversion`; **nothing produces the `benzene-version` header the versioning spec requires** (versioning.md §2.1), so payload schema version does not survive a CloudEvents hop at all |
| Outbound headers | Extensions are where Benzene's headers travel | **All unprefixed wire headers are dropped** — only `ce-<legalname>` headers map back to attributes/extensions (`cloudevents.go:206-211`, `:228-235`). `benzene-version`, correlation and trace headers therefore do not leave a Go service over CloudEvents. Documented as deliberate lossiness in the package doc, but it is deliberate *against* this design |
| Content mode | Support both, default per binding | Both inbound; outbound (`FromRequest` + `MarshalJSON`) is structured only |
| Shape of the integration | A reusable core keyed on the envelope + thin per-binding extensions | **Matches.** `ToRequest`/`FromRequest` are transport-neutral; `Handler` is the thin HTTP binding |

Two of those rows are not stylistic. **The `ce-` prefix on extensions plus the outbound drop means a
Benzene header cannot round-trip through CloudEvents**, and **no version signal survives**, which
takes versioning.md and the header conventions of wire-contracts.md §2 off the table for any
CloudEvents hop. The header-naming row is the one that decides the wire format for every other port.

### 0.3 The decision this now owes

`AGENTS.md` names this situation exactly: "A change to an observable contract … is a spec change …
Don't change a fixture to match one implementation's quirk." Go's binding is an observable contract
being defined by one implementation. There are two coherent outcomes and no third:

1. **Go's `ce-` scheme becomes normative.** It is defensible on its own terms — one uniform rule
   covering every attribute and extension rather than a short list of special cases, reversible
   without a lookup table, and it mirrors the CloudEvents HTTP binding's own `ce-` header
   convention, which is what most producers and consumers already speak. Choosing it means writing
   the spec section (attribute ↔ concept mapping, both content modes, the ack contract, the
   round-trip rules) and adding a `cloudevents-cases.json` fixture, then deciding per port whether
   the claim is required or conditional. It also means ruling explicitly on the three consequences
   above: whether `datacontenttype` should feed negotiation after all, whether `benzene-version`
   must be produced from some attribute, and whether the outbound drop of unprefixed headers stands.
2. **Go changes to match a spec written first.** The path this document assumed, and the one
   `AGENTS.md` prefers in general. The cost is now real rather than theoretical — Go's package is
   released, tested and documented — but the tag has not shipped, so it is still the cheapest it
   will ever be.

**This document does not take that decision**, and the spec section is deliberately not written
here. What is recorded is that the decision exists, that it is being made by default every day it is
left, and that the default answer is outcome 1 without anyone having chosen it.

The open questions in the section of the same name below are all live still — but note that Go has
already answered four of them in code (`source`/`subject` naming, content mode, format adapter vs
transport binding, and the version attribute), so they are no longer open in the sense of "nothing
depends on the answer yet".

---

**Status of everything below: design proposal, not a committed plan, written before Go's binding
existed.** It records the approach and open questions for a `Benzene.CloudEvents` integration so the
design could be agreed before an implementation plan was written. It is a design doc in the spirit
of `work/auth-middleware-design.md` / `work/saga-design.md`, not a `docs/plans/*` implementation
plan.

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

*Superseded by §0.3 — the next step is the maintainer's ruling on whose scheme is normative, not a
plan. Kept for the ordering it assumes, which is still right whichever way the ruling goes: the spec
section is part of the work, not a follow-up to it.*

If the approach is agreed, promote this into a `docs/plans/cloudevents-plan.md` implementation plan
(inbound adapter first, then outbound formatter, then the HTTP binding and spec section).
