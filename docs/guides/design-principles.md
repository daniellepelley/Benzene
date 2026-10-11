# Design Principles: Opinionated but Optional

**Guidance (informative).** The strategy behind the spec. The rules it motivates are normative where
the spec states them: degradation, shorthands and inference in
[core-concepts](../specification/core-concepts.md) §11, configurable names in
[wire-contracts](../specification/wire-contracts.md) §2, default paths in
[transport-bindings](../specification/transport-bindings.md) §1.

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
demanding a rewrite — but it creates an obligation: **any capability that relies on a steer
degrades gracefully when the steer is declined** (section 3), and **every convention is
configurable on both sides of the wire** (section 4). Both are normative in the spec
(core-concepts §11, wire-contracts §2).

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
| 5. Meshed | The mesh module ([mesh.md](../specification/mesh.md)) | Everything above, plus fleet visibility: live topology, health, schemas, and flows derived from what services actually do |

The steers compound: rung 3 is why rung 5 can derive schemas, rung 4 is why a collector can find
a fleet. But the dependencies only ever point *down* the ladder — a rung-2 service still
participates in the mesh's trace feed (reduced, per [mesh.md](../specification/mesh.md) §6), and a rung-1 pipeline
is still real Benzene.

The top of the ladder has a name: a service at rungs 2–5 in full is a **Benzene Cloud Service**
([cloud-service-profile.md](../specification/cloud-service-profile.md)) — the conformance target fleet tooling is
allowed to assume. The ladder describes the journey and keeps every rung first-class; the
profile marks the destination for services that opt in to being fully tool-operable.

## 3. Capabilities and what they require

Because handlers are optional, every capability must be honest about whether it needs them.

**Works with middleware alone (rungs 1–2):**

| Capability | Notes |
|---|---|
| Middleware pipeline, context, result/status model | The core; depends on nothing above rung 1 |
| Transport adapters / hosting | Adapters run a pipeline, not a handler registry |
| Health-check interception | Reserved-topic middleware ([wire-contracts.md](../specification/wire-contracts.md)); answers before routing, so no handlers needed |
| Mesh trace feed | Trace middleware observes invocations, not handlers; a middleware-only service appears in the mesh with real traffic ([mesh.md](../specification/mesh.md) §3) |
| Wire-envelope hosting | The envelope carries a topic; what the pipeline does with it is the service's business |
| String status vocabulary | Statuses are pipeline-level, set by middleware or handlers alike |

**Requires message handlers (rung 3):**

| Capability | Why | Degradation when declined |
|---|---|---|
| Derived spec (topics + payload schemas) | Derivation reads the registry of `(topic, TRequest, TResponse)` registrations; middleware declares none of that | No spec output; the service is documented by hand or not at all |
| Mesh descriptor (`benzene:mesh` reserved topic, schemas, `descriptorHash`) | Same registry dependency ([mesh.md](../specification/mesh.md) §2) | Service appears in the mesh via traces/heartbeats with `missingFeeds: ["descriptor"]` — reduced, never rejected ([mesh.md](../specification/mesh.md) §6) |
| HTTP endpoint attributes / route sugar | Sugar over handler registration | Routes are declared explicitly instead |
| Codegen clients | Generated from the derived spec | Clients are written by hand |

The rule behind this table is normative in [core-concepts](../specification/core-concepts.md) §11:
a capability that depends on a steer states its degradation, and the degradation reduces the
capability, never breaks the service or the fleet tooling around it. The mesh's degradation rules
([mesh.md](../specification/mesh.md) §6) are the worked example: every feed independent, a missing
feed marks the service reduced, ingestion and queries never fail on partial data.

## 4. Extension points: every convention, both sides

Every convention that crosses the wire has a producer and a consumer, and both ship the same
default and expose the same override — normatively, every reserved name is configurable in one
place and applies in both directions ([wire-contracts](../specification/wire-contracts.md) §2). A
convention you can only override on one side is a trap, not a convention.

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
[transport-bindings.md](../specification/transport-bindings.md) for the full adapter contract):

| Extension point | Default steer | Override mechanism |
|---|---|---|
| Topic getter (per transport) | Transport-specific well-known location (SQS: `topic` message attribute; HTTP: route/envelope field; see each binding) | Replace the topic-getter registration for that context type |
| Headers getter | Transport's native header/attribute mechanism | Replace the headers-getter registration |
| Request mapping / serialization | JSON, media-format negotiated | Replace the request-mapper registration |
| Result setter | Transport's native response shape per [transport-bindings.md](../specification/transport-bindings.md) | Replace the result-setter registration |
| Outbound client conventions | Mirror of the consumer defaults (e.g. `ISqsClient` tags `topic`) | Implement the client interface |
| Status vocabulary | The string statuses of [wire-contracts.md](../specification/wire-contracts.md) | **Statuses are strings precisely so you can add your own.** Custom statuses flow through the pipeline, the envelope, and the mesh untouched; only protocol mappings (e.g. HTTP status codes) need a mapping entry for them, and unmapped statuses take the documented fallback |
| Status readers (mesh) | Read the transport's Benzene status field | Implement the status-reader interface for your context type |
| DI container | The implementation's first-party or platform container | The container abstraction ([core-concepts.md](../specification/core-concepts.md) §8) — bring your own |
| Handler discovery | Attribute/reflection scanning where idiomatic | Explicit registration is the concept; scanning is sugar ([README](../specification/README.md), "the one design rule") |
| Version getter (per transport) | The `benzene-version` header (HTTP: a `version` route parameter where the route declares one, else the header) — [versioning.md](../specification/versioning.md) §2 | Rename the header in one place, or replace the version-getter registration for that context type |

Advice for feature authors:

- Name a new convention's default in the spec, make it overridable via a replaceable component (not
  a fork of the pipeline), and ship the override on both producer and consumer sides in the same
  release.
- Tolerate unknown values in a new status, header, or attribute convention — reject-on-unknown
  breaks the extensibility promise.
- Give sugar (attributes, scanning, builder shorthands) an explicit, idiom-free equivalent (see
  §4.1, which covers the obligations that run the *other* way).

### 4.1 The shorthand ladder: what a steer costs in code

Section 1 says every steer can be declined. This section says the other half: **taking a steer
must be cheap, and the cheap path must not cost you the ability to see what it did.** The three
testable rules — a shorthand composed from the public explicit form, inference verified before the
first message, the explicit form named in the shorthand's documentation — are normative in
[core-concepts](../specification/core-concepts.md) §11; this section is the reasoning.

Both failures are real, and a framework usually has one or the other:

| Failure | What the user experiences |
|---|---|
| Too much ceremony | They write the framework's job — building hosts, wiring transports, constructing pipelines. Intent drowns in plumbing, and every service re-derives the same twenty lines slightly differently. |
| Too much magic | They cannot predict what will happen, cannot override it, cannot drop a level when they need to, and find out it was wrong at message time in production. |

The way out is not a compromise between them. It is a **ladder**, and the framework owes both ends
of it.

**Every capability MUST have an explicit form.** Every step visible, nothing inferred. This is the
contract; the shorthand is a convenience over it.

**Every capability a service needs *routinely* MUST have a shorthand.** A capability that exists
only in explicit form is unfinished, not minimal. The existing rule ("sugar must have an explicit
equivalent") is only half a policy — on its own it permits a framework with no sugar at all.

**A shorthand MUST be composed from the public explicit form, never parallel to it.** This is the
whole anti-magic guarantee, and it is testable three ways:

1. A user could have written the shorthand themselves, in their own code, using only public API.
2. From any rung you can drop **exactly one level** down and keep going — never zero, never all the
   way to the bottom.
3. Every rung you land on is public, documented API — not an internal type you were not meant to see.

A shorthand that can do something no composition of public API can do has taken a capability
hostage. That is the line between *convenience* and *magic*, and it does not move.

**The price of a convention is a start-up check.** Inference — assembly or module scanning,
attribute/decorator discovery, convention-over-configuration, "it just found it" — is permitted
exactly to the degree that it is **verified before any message is handled**, and the failure
**names what was looked for, where, and what to add**. The cost of magic was never the inference;
it was *finding out late*. A convention that can first fail on the message path has not paid for
itself.

**The ladder MUST be visible from the top.** A shorthand's documentation MUST name the explicit
form it composes. An escape hatch nobody can find is, from the user's seat, indistinguishable from
no escape hatch — and the user will conclude the framework cannot do the thing.

**What a steer should cost:** declaration, not wiring. A service's own code should read as *what it
handles, what it talks to, and what it needs* — and contain approximately nothing else. Building a
host, mounting a transport, and assembling a pipeline are the framework's work, not the service's.

#### Examples are where this is proved

Example and sample code is the only place these claims are actually tested, so it is held to a
stricter rule than production code:

- **Every line is domain or intent.** A line that is neither is one of two things: a missing
  shorthand — which is a **framework** bug, not an example bug — or a deliberate demonstration of
  the explicit form, which MUST say so in a comment. There is no third category, and "that is just
  the setup you have to write" is the first category wearing a disguise.
- **Duplicated plumbing across examples is a framework bug.** When two examples hand-roll the same
  adapter, that is a missing seam, and the count is the evidence: the second copy is a signal, the
  third is a backlog item. Copying it a fourth time is choosing not to fix it.

## 5. The default service standard

A Benzene service exposes a standard set of well-known HTTP surfaces under one `/benzene/` prefix,
so that people and tools can walk up to any service in the fleet and find the same things in the
same places. `/admin/` was considered and rejected: many domains have their own "admin" concept,
and the collision is exactly the ambiguity the prefix exists to remove. The prefix also gives
operators one knob — expose or protect every framework surface with a single path rule (the
"descriptor endpoints not provisioned" scenario of [mesh.md](../specification/mesh.md) §6 becomes
`deny /benzene/spec*` at the gateway).

The paths themselves are normative, in
[transport-bindings](../specification/transport-bindings.md) §1 ("Default HTTP paths"); the
[Cloud Service Profile](../specification/cloud-service-profile.md)'s R7 makes them a MUST.
