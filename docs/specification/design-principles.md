# Design Principles: Opinionated but Optional

**Status: DRAFT v0.1 — this document is strategy, not wire format. Its defaults are normative
(an implementation that ships a different default is non-conforming); its override points are
equally normative (an implementation that ships a default without an override is also
non-conforming).**

## 1. The principle

Benzene is **opinionated but optional**. Every layer of the framework has a recommended,
consistent, well-documented default — the *steer* — and every steer can be declined or replaced
without giving up the layers below it.

The clearest example is message handlers. Handlers are to Benzene what controllers are to
ASP.NET: the steer, the shape most services should use, the thing the tooling understands best —
and **ultimately optional**. A Benzene service can be:

- a middleware pipeline with no message handlers at all, all behavior written as middleware;
- a pipeline invoked **in process** — not hosted behind any transport — as a composition
  mechanism inside a larger application;
- a conventional hosted service with handlers, topics, and derived spec;
- all the way up to a fully meshed service implementing the default service standard (section 5).

Each of those is a first-class, supported way to use Benzene, not a degraded mode. This
flexibility is an asset — it is what makes Benzene fit inside an existing codebase instead of
demanding a rewrite — but it creates an obligation this document exists to state: **any
capability that relies on a steer MUST degrade gracefully when the steer is declined** (section
3), and **every convention MUST be configurable on both sides of the wire** (section 4).

### The two rules

1. **Steer to the default.** Ship one recommended way to do each thing. Documentation, examples,
   and tooling assume the default. Consistency across the fleet is itself a feature — the mesh,
   the spec generator, and cross-service clients all work *because* most services take the steer.
2. **Never require it.** Every default is a replaceable component, not a hard-coded behavior.
   Declining a steer costs you exactly the capabilities that depend on it (stated per capability
   in section 3), never the layers underneath.

## 2. The adoption ladder

Benzene adoption is a ladder, not a gate. Each rung adds a steer and unlocks capabilities; no
rung is mandatory, and a service can stop climbing at any rung and stay there indefinitely.

| Rung | What you use | What you get |
|---|---|---|
| 1. In-process pipeline | The middleware pipeline invoked directly from your own code — no host, no transport | Composition, cross-cutting middleware, the result/status model |
| 2. Hosted middleware-only | A transport adapter running a pipeline with no message handlers | Everything above, plus transport neutrality, health-check interception, mesh trace feed |
| 3. Message handlers | `handle : TRequest -> Result<TResponse>` functions routed by topic | Everything above, plus topic routing, request mapping, **derived** spec/descriptor (topics + schemas), codegen clients |
| 4. Default service standard | The well-known surfaces of section 5 | Everything above, plus fleet-wide operability: any tool or teammate finds spec, health, and envelope endpoints in the same place on every service |
| 5. Meshed | The mesh module ([mesh.md](mesh.md)) | Everything above, plus fleet visibility: live topology, health, schemas, and flows derived from what services actually do |

The steers compound: rung 3 is why rung 5 can derive schemas, rung 4 is why a collector can find
a fleet. But the dependencies only ever point *down* the ladder — a rung-2 service still
participates in the mesh's trace feed (reduced, per [mesh.md](mesh.md) §6), and a rung-1 pipeline
is still real Benzene.

## 3. Capabilities and what they require

Because handlers are optional, every capability must be honest about whether it needs them.

**Works with middleware alone (rungs 1–2):**

| Capability | Notes |
|---|---|
| Middleware pipeline, context, result/status model | The core; depends on nothing above rung 1 |
| Transport adapters / hosting | Adapters run a pipeline, not a handler registry |
| Health-check interception | Reserved-topic middleware ([wire-contracts.md](wire-contracts.md)); answers before routing, so no handlers needed |
| Mesh trace feed | Trace middleware observes invocations, not handlers; a middleware-only service appears in the mesh with real traffic ([mesh.md](mesh.md) §3) |
| Wire-envelope hosting | The envelope carries a topic; what the pipeline does with it is the service's business |
| String status vocabulary | Statuses are pipeline-level, set by middleware or handlers alike |

**Requires message handlers (rung 3):**

| Capability | Why | Degradation when declined |
|---|---|---|
| Derived spec (topics + payload schemas) | Derivation reads the registry of `(topic, TRequest, TResponse)` registrations; middleware declares none of that | No spec output; the service is documented by hand or not at all |
| Mesh descriptor (`mesh` reserved topic, schemas, `descriptorHash`) | Same registry dependency ([mesh.md](mesh.md) §2) | Service appears in the mesh via traces/heartbeats with `missingFeeds: ["descriptor"]` — reduced, never rejected ([mesh.md](mesh.md) §6) |
| HTTP endpoint attributes / route sugar | Sugar over handler registration | Routes are declared explicitly instead |
| Codegen clients | Generated from the derived spec | Clients are written by hand |

This table is the normative pattern for future features: a new capability that depends on
handlers (or any other steer) MUST list itself here with its degradation behavior, and that
behavior MUST reduce the capability, never break the service or the fleet tooling around it. The
mesh's degradation rules ([mesh.md](mesh.md) §6) are the worked example: every feed independent,
a missing feed marks the service reduced, ingestion and queries never fail on partial data.

## 4. Extension points: every convention, both sides

Every convention that crosses the wire has a producer and a consumer, and **both MUST ship the
same default and both MUST expose the override**. A convention you can only override on one side
is a trap, not a convention.

The worked example — SQS topic resolution *(informative, .NET)*:

- **Consumer side:** `SqsConsumerMessageTopicGetter` reads the SQS message's `topic` message
  attribute. It is registered as a replaceable `IMessageTopicGetter<SqsConsumerMessageContext>`
  service — register your own implementation and the pipeline resolves topics from wherever
  your messages carry them (a body field, a queue-per-topic convention, an EventBridge
  envelope). The same pattern holds per transport: the Lambda SQS binding's
  `SqsMessageTopicGetter` is likewise a replaceable DI registration.
- **Client side:** `SqsMessageClient` (behind the `ISqsClient` interface) tags each published
  message with the same `topic` attribute. Publishing under a different convention means
  implementing `ISqsClient` — the interface, not the attribute name, is the contract.

Declining the steer on one side obliges you to decline it identically on the other; the framework
makes both declinations the same size.

The catalog of extension points every transport binding carries (see
[transport-bindings.md](transport-bindings.md) for the full adapter contract):

| Extension point | Default steer | Override mechanism |
|---|---|---|
| Topic getter (per transport) | Transport-specific well-known location (SQS: `topic` message attribute; HTTP: route/envelope field; see each binding) | Replace the topic-getter registration for that context type |
| Headers getter | Transport's native header/attribute mechanism | Replace the headers-getter registration |
| Request mapping / serialization | JSON, media-format negotiated | Replace the request-mapper registration |
| Result setter | Transport's native response shape per [transport-bindings.md](transport-bindings.md) | Replace the result-setter registration |
| Outbound client conventions | Mirror of the consumer defaults (e.g. `ISqsClient` tags `topic`) | Implement the client interface |
| Status vocabulary | The string statuses of [wire-contracts.md](wire-contracts.md) | **Statuses are strings precisely so you can add your own.** Custom statuses flow through the pipeline, the envelope, and the mesh untouched; only protocol mappings (e.g. HTTP status codes) need a mapping entry for them, and unmapped statuses take the documented fallback |
| Status readers (mesh) | Read the transport's Benzene status field | Implement the status-reader interface for your context type |
| DI container | The implementation's first-party or platform container | The container abstraction ([core-concepts.md](core-concepts.md) §8) — bring your own |
| Handler discovery | Attribute/reflection scanning where idiomatic | Explicit registration is the concept; scanning is sugar ([README](README.md), "the one design rule") |

Rules for feature authors:

- A new convention MUST name its default in the spec, MUST be overridable via a replaceable
  component (not a fork of the pipeline), and MUST ship the override on both producer and
  consumer sides in the same release.
- A new status, header, or attribute convention MUST tolerate unknown values from services that
  extended the vocabulary — reject-on-unknown breaks the extensibility promise.
- Sugar (attributes, scanning, builder shorthands) MUST have an explicit, idiom-free equivalent.

## 5. The default service standard

In the spirit of section 1, a Benzene service SHOULD expose a standard set of well-known
surfaces, so that people and tools can walk up to any service in the fleet and find the same
things in the same places. Like everything else in this document: each surface is optional and
each location is configurable — but the defaults below are the steer, and fleet tooling assumes
them.

### 5.1 The `/benzene/` prefix

All framework-provided HTTP surfaces live under a single well-known prefix, **`/benzene/`**, so
that it is immediately clear — in a URL, a log line, a gateway config, a firewall rule — that a
path is framework infrastructure and not a domain endpoint. `/admin/` was considered and
rejected: many domains have their own "admin" concept, and the collision is exactly the
ambiguity the prefix exists to remove. `/benzene/` is self-identifying and collision-free.

The prefix also gives operators one knob: expose or protect all framework surfaces with a single
path rule (the security-driven "descriptor endpoints not provisioned" scenario of
[mesh.md](mesh.md) §6 becomes `deny /benzene/spec*` at the gateway, with the mesh degrading
exactly as specified).

### 5.2 Well-known HTTP surfaces

| Path | Surface | Notes |
|---|---|---|
| `/benzene/invoke` | Wire-envelope endpoint (`{topic, headers, body}` in, `{statusCode, headers, body}` out per [wire-contracts.md](wire-contracts.md)) | The service-to-service and collector-query surface |
| `/benzene/spec` | The derived spec document | Requires rung 3 (section 3) |
| `/benzene/health` | Health check (the reserved `healthcheck` topic's response shape, over HTTP) | Liveness/readiness variants MAY nest beneath it (`/benzene/health/live`, `/benzene/health/ready`) |
| `/benzene/spec-ui` | Human-readable spec browser | Optional UI |
| `/benzene/mesh-ui` | Mesh artifact viewer | Optional UI |
| `/benzene/fleet-ui` | Live fleet view (collector-hosted) | Optional UI; collectors are ordinary Benzene services, so the standard applies to them too |

Reserved *topics* (`healthcheck`, `mesh`, `mesh:*`) are already namespaced by their own
registries ([wire-contracts.md](wire-contracts.md), [mesh.md](mesh.md) §1/§4) and take no
prefix — the prefix is an HTTP-surface concern.

### 5.3 Conformance and migration

- New framework-provided HTTP surfaces MUST default to a `/benzene/`-prefixed path.
- Every path MUST remain configurable per service; the prefix is the steer, not a cage.
- *(informative)* Pre-existing .NET defaults that predate this standard (`/mesh-ui`,
  `/spec-ui`) keep their current defaults for compatibility and are migration candidates for
  the 1.0 release; the Fleet view and wire-envelope defaults (`/benzene/fleet-ui`,
  `/benzene/invoke`), which shipped after this standard, already follow it.
