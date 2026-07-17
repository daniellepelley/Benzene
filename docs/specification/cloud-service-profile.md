# Benzene Cloud Service Profile

**Status: DRAFT v0.1 — the level names ("Benzene Core", "Benzene Cloud Service Profile") are
settled; requirement ids (R1–R8) may still be reorganized while in draft.**

## 1. Purpose

The [Benzene Core Specification](README.md) is deliberately open: middleware-only services,
in-process pipelines, and services with no health checks or spec endpoint are all first-class,
conforming ways to use Benzene ([design-principles.md](design-principles.md) §2). That openness
is an asset for adoption — and a problem for fleet tooling. The mesh, the Spec UI, codegen
clients, and cross-service operability all depend on surfaces that a Core-level service is free
not to have.

This profile resolves that tension without giving up the openness. It is a **named conformance
target layered on top of the core**: a service that claims it guarantees the full set of
operational surfaces that fleet tooling assumes, so that mesh, the Spec UI, and every other
cross-service tool work on **every** profiled service with no per-service negotiation.

The profile defines **no new wire contracts**. Everything it requires is already specified in
the core documents — this document only changes the RFC 2119 requirement level (SHOULD/MAY →
MUST) of existing, individually-optional capabilities, for services that claim the profile.

### 1.1 The bargain *(informative)*

The profile is opt-in in both directions, and each side of the bargain is unconditional:

- **Meet all of it, get all of it.** A service that satisfies every requirement in §2 gets the
  full mesh and visibility offering — it appears in the fleet view with live health, its derived
  spec and schemas, its topology edges, and every tool works against it with no per-service
  setup. Within the profile, the requirements are MUSTs precisely so this promise never comes
  with an asterisk.
- **Skip it, lose only dashboard.** A service that doesn't claim the profile still runs, still
  interoperates over the wire contracts, and still shows up in the mesh to whatever extent its
  feeds allow ([mesh.md](mesh.md) §6) — it just has missing bits of dashboard: no schemas
  without a descriptor, unknown health without heartbeats, no edges without traces. Nothing
  breaks; the picture is simply reduced.

This makes the profile the recommended shape for **domain services running in a cloud** — the
services a team operates as a fleet and wants full visibility over — while Benzene itself
remains fully usable off-profile, in whichever way fits, for everything that isn't that: an
in-process pipeline inside a larger app, a middleware-only edge, a deliberate out-of-spec
deployment.

### 1.2 The two conformance levels

| Level | Claim | What it means |
|---|---|---|
| **Benzene Core** | "This is a Benzene implementation/service" | Conforms to the core documents as applicable to what it uses. Every capability is optional; the adoption ladder ([design-principles.md](design-principles.md) §2) applies in full. |
| **Benzene Cloud Service** | "Fleet tooling can rely on this service" | Benzene Core, plus every requirement in §2 of this document. Corresponds to rungs 2–5 of the adoption ladder. |

Core-level conformance is a property of an *implementation* (a language port) or a *service*;
the Cloud Service profile is a property of a **service** (a deployed application). A port
supports the profile when a service built on it can satisfy §2 out of the box.

## 2. Requirements

A service claiming the Benzene Cloud Service profile MUST satisfy all of the following. Each
requirement names the core-spec section that defines the behavior — the definitions live there,
not here.

### R1 — Hosted middleware pipeline

The service MUST run its traffic through the middleware pipeline
([core-concepts.md](core-concepts.md) §4) hosted behind at least one transport binding
([transport-bindings.md](transport-bindings.md)). An in-process-only pipeline (adoption ladder
rung 1) cannot claim the profile — there is nothing for fleet tooling to reach.

### R2 — Message handlers via the registry

The service's topics MUST be served by message handlers registered through the handler registry
([core-concepts.md](core-concepts.md) §3, §9), so that the registry is the complete truth of
what the service serves. This is the load-bearing steer: R5 (derived spec) and R6 (descriptor)
are projections of the registry and cannot exist without it.

Middleware may still do anything middleware can do — interception, cross-cutting behavior,
short-circuiting — but *routable application topics* MUST go through handlers.

### R3 — Health checks

The service MUST intercept the reserved `healthcheck` topic and respond with the health check
response format ([wire-contracts.md](wire-contracts.md) §5, [core-concepts.md](core-concepts.md)
§10). Where the service has an HTTP surface, the aggregate MUST also be exposed at its
`/benzene/health` default ([design-principles.md](design-principles.md) §5.2).

### R4 — Wire-envelope invocability

The service MUST be invokable via the Benzene message envelope
([wire-contracts.md](wire-contracts.md) §1) on at least one of its transports. Where the service
has an HTTP surface, the envelope endpoint MUST default to `/benzene/invoke`
([design-principles.md](design-principles.md) §5.2). This is the surface collectors and generic
tooling use to reach any service without knowing its native transport shapes.

### R5 — Derived spec

The service MUST derive its spec document from the handler registry and expose it — over HTTP at
the `/benzene/spec` default ([design-principles.md](design-principles.md) §5.2). Hand-maintained
spec documents do not satisfy this requirement: the point of the profile is that the spec is
true because it is derived ([design-principles.md](design-principles.md) §3).

### R6 — Mesh service-side feeds

The service MUST provision the four service-side mesh feeds of [mesh.md](mesh.md):

- the reserved `mesh` topic serving the ServiceDescriptor (mesh §1–§2),
- registration (`mesh:register`) on startup (mesh §4),
- heartbeats (`mesh:heartbeat`) (mesh §5),
- the trace feed (`mesh:traces`) with one TraceEvent per routed invocation (mesh §3–§4).

Implementing a *collector* is NOT required — the profile makes every service a good mesh
citizen, not a mesh host.

The sender-behavior rules of mesh §4 apply unchanged: no feed may ever fail, slow, or block the
service's own traffic. See §4 below for how runtime degradation interacts with the profile
claim.

### R7 — Default service standard paths

The well-known HTTP surfaces the service exposes MUST default to their
[design-principles.md](design-principles.md) §5.2 paths under the `/benzene/` prefix. For a
Core-level service §5 is a SHOULD; under this profile it is a MUST-by-default. Every path
remains configurable per deployment (the extension-point rule of
[design-principles.md](design-principles.md) §4 is not suspended by this profile), but a
deployment that relocates or blocks a surface accepts the documented degradation for that
surface (§4 below) — fleet tooling is entitled to assume the defaults.

### R8 — Trace context propagation

The service MUST join inbound W3C trace context per [core-concepts.md](core-concepts.md) §10 and
mesh §3, and its outbound Benzene clients MUST forward `traceparent` built from the current
invocation's span (promoted from the SHOULD of mesh §3). Consumer edges in the mesh are derived
from trace parentage; a profiled fleet where propagation is optional would have holes in its
topology through no fault of the collector.

## 3. What stays optional, even under the profile

The profile mandates capabilities, not idioms or extras. The following remain optional for a
profiled service:

| Still optional | Why |
|---|---|
| The UIs (`/benzene/spec-ui`, `/benzene/mesh-ui`, `/benzene/fleet-ui`) | Human conveniences; tooling consumes the machine surfaces (R4–R6) |
| Implementing a mesh collector | R6 is service-side only; collectors are separate services |
| Any specific transport binding | R1/R4 require *at least one* binding; which ones is the service's business |
| Handler discovery idiom | Attribute scanning vs explicit calls is an idiom ([core-concepts.md](core-concepts.md) §9); R2 requires the registry, not a discovery style |
| Convention overrides | Every §4 extension point of [design-principles.md](design-principles.md) stays replaceable; the profile constrains defaults and presence, not customizability |
| Custom statuses, extra topics, application middleware | The profile is a floor, not a ceiling |

## 4. Degradation vs. non-conformance

The mesh degradation rules ([mesh.md](mesh.md) §6) and the profile claim answer different
questions, and both stay in force:

- **Runtime degradation is not a conformance failure.** An unreachable collector, a full trace
  buffer dropping events, a transient health-check failure — a profiled service degrades exactly
  as mesh §6 specifies and keeps its claim. The profile requires the capabilities to be
  *provisioned*, not that the network never fails.
- **Deliberate omission is what the profile rules out.** A service that never installs health
  checks, never derives a descriptor, or serves topics outside the registry is a perfectly good
  **Benzene Core** service — it just isn't a **Benzene Cloud Service**, and fleet tooling makes
  no promises about it beyond the reduced rendering of mesh §6.
- **Deployment-time exposure control sits in between.** Blocking `/benzene/spec*` at a gateway
  pending a security review ([design-principles.md](design-principles.md) §5.1) is an operations
  decision about a *deployment*, not a gap in the *service*. The service remains
  profile-conformant; that deployment presents to fleet tooling as reduced, per mesh §6, for
  exactly the blocked surfaces.

## 5. Conformance testing

- **Benzene Core** (implementation-level): pass `status-vocabulary.json`, the mapping-table
  fixtures for each protocol the port binds, and `envelope-cases.json` — see
  [conformance/](conformance/README.md).
- **Benzene Cloud Service** (profile support): additionally pass `mesh-descriptor-cases.json`
  and `mesh-trace-cases.json`. (`mesh-collector-cases.json` remains collector-only and is not
  part of the profile.)
- A per-requirement (R1–R8) machine-checkable profile checklist — assertable against a running
  service rather than a runner — is planned; until it exists, R3–R7's observable surfaces
  (health response shape, envelope round-trip, spec and descriptor presence, default paths) are
  verified in the live-interop form described in
  [porting-guide.md §3](porting-guide.md#3-conformance-testing).

## 6. Relationship to the adoption ladder *(informative)*

The profile is not a new idea grafted onto Benzene — it is a **name for the top of the ladder**
that [design-principles.md](design-principles.md) §2 already defines. The ladder describes the
journey (each rung optional, stop anywhere); the profile marks the destination fleet tooling can
rely on:

| Adoption ladder rung | Profile requirement |
|---|---|
| 2. Hosted middleware-only | R1 |
| 3. Message handlers | R2, R5 |
| 4. Default service standard | R3, R4, R7 |
| 5. Meshed | R6, R8 |

Nothing about the ladder changes for Core-level services: every rung below the profile remains
first-class and indefinitely inhabitable. The profile exists so that "we run Benzene Cloud
Services" is a complete, precise statement of what an operator, a teammate, or a tool will find.

## 7. Naming note *(informative)*

"Profile" follows established spec practice (e.g. WS-I Basic Profile): a base specification
stays maximally permissive, and a named profile layers interoperability requirements on top for
those who opt in. The base level is referred to as the **Benzene Core Specification** to keep
the two unambiguous — "conforms to Benzene" is Core; "conforms to the Cloud Service Profile" is
this document.
