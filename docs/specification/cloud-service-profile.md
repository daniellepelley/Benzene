# Benzene Cloud Service Profile

**Profile.** Frozen under semver from 1.0, like Core.

## 1. Purpose

The [Benzene Core Specification](README.md) is deliberately open: middleware-only services,
in-process pipelines, and services with no health checks or spec endpoint are all first-class,
conforming ways to use Benzene ([the adoption ladder](../guides/design-principles.md#2-the-adoption-ladder)). That openness
is an asset for adoption — and a problem for fleet tooling. The mesh, the Spec UI, codegen
clients, and cross-service operability all depend on surfaces that a Core-level service is free
not to have.

This profile resolves that tension without giving up the openness. It is a **named conformance
target layered on top of the core**: a service that claims it guarantees the operational surfaces
any tool can **pull** from the service itself — a hosted pipeline, registry-served handlers,
health, envelope invocability, its derived contract document and the default paths — so that the
Spec UI, codegen, probes and every other cross-service tool work on **every** profiled service
with no per-service negotiation, and with no collector. Joining a mesh, which needs a collector,
is the separate claim of a **meshed service** ([mesh.md](mesh.md) §7).

The profile defines **no new wire contracts**. Everything it requires is already specified in
the core documents — this document only changes the RFC 2119 requirement level (SHOULD/MAY →
MUST) of existing, individually-optional capabilities, for services that claim the profile.

### 1.1 The bargain *(informative)*

The profile is opt-in in both directions, and each side of the bargain is unconditional:

- **Meet all of it, get all of it.** A service that satisfies every requirement in §2 is fully
  tool-operable — its health, its derived contract document and schemas, and its envelope are
  where every tool expects them, with no per-service setup; a meshed service additionally
  appears in the fleet view ([mesh.md](mesh.md) §7.1). Within the profile, the requirements are
  MUSTs precisely so this promise never comes with an asterisk.
- **Skip it, lose only tooling.** A service that doesn't claim the profile still runs and still
  interoperates over the wire contracts — tools just find less: no derived document, so no
  schemas or generated clients; no health topic, so unknown health; no envelope, so no generic
  invocation. Nothing breaks; the picture is simply reduced.

This makes the profile the recommended shape for **domain services running in a cloud** — the
services a team operates as a fleet and wants full visibility over — while Benzene itself
remains fully usable off-profile, in whichever way fits, for everything that isn't that: an
in-process pipeline inside a larger app, a middleware-only edge, a deliberate out-of-spec
deployment.

### 1.2 The two conformance levels

| Level | Claim | What it means |
|---|---|---|
| **Benzene Core** | "This is a Benzene implementation/service" | Conforms to the core documents as applicable to what it uses. Every capability is optional; the [adoption ladder](../guides/design-principles.md#2-the-adoption-ladder) applies in full. |
| **Benzene Cloud Service** | "Fleet tooling can rely on this service" | Benzene Core, plus every requirement in §2 of this document. Corresponds to rungs 2–4 of the adoption ladder. |

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
what the service serves. This is the load-bearing steer: R5 (derived spec), and the mesh
registration built from it, are projections of the registry and cannot exist without it.

Middleware may still do anything middleware can do — interception, cross-cutting behavior,
short-circuiting — but *routable application topics* MUST go through handlers.

### R3 — Health checks

The service MUST intercept the reserved `benzene:healthcheck` topic and respond with the health check
response format ([wire-contracts.md](wire-contracts.md) §5, [core-concepts.md](core-concepts.md)
§10). Where the service has an HTTP surface, the aggregate MUST also be exposed at its
`/benzene/health` default ([transport-bindings.md](transport-bindings.md) §1, "Default HTTP paths").

### R4 — Wire-envelope invocability

The service MUST be invokable via the Benzene message envelope
([wire-contracts.md](wire-contracts.md) §1) on at least one of its transports. Where the service
has an HTTP surface, the envelope endpoint MUST default to `/benzene/invoke`
([transport-bindings.md](transport-bindings.md) §1, "Default HTTP paths"). This is the surface collectors and generic
tooling use to reach any service without knowing its native transport shapes.

### R5 — Derived spec

The service MUST derive its spec document from the handler registry and expose it: by
intercepting the reserved `benzene:spec` topic in its pipeline and answering it with that document
wherever the transport carries a reply ([contract-document.md](contract-document.md) §7), and,
where the service has an HTTP surface, at the `/benzene/spec` default ([transport-bindings.md](transport-bindings.md) §1, "Default HTTP paths"). Hand-maintained
spec documents do not satisfy this requirement: the point of the profile is that the spec is
true because it is derived ([core-concepts.md](core-concepts.md) §11). The served document's
shape is [contract-document.md](contract-document.md) — the same format language-neutral client
generators parse.

### R7 — Default service standard paths

The well-known HTTP surfaces the service exposes MUST default to their
[transport-bindings.md](transport-bindings.md) §1 default paths under the `/benzene/` prefix. For a
Core-level service these defaults are a SHOULD (transport-bindings §1); under this profile they are a MUST-by-default. Every path
remains configurable per deployment (the extension-point rule of
[wire-contracts.md](wire-contracts.md) §2 and [core-concepts.md](core-concepts.md) §11 are not suspended by this profile), but a
deployment that relocates or blocks a surface accepts the documented degradation for that
surface (§4 below) — fleet tooling is entitled to assume the defaults.

### R6 and R8 *(moved)*

The mesh feeds (formerly R6) and trace context propagation (formerly R8) are the mesh module's own
conformance claim, a **meshed service** ([mesh.md](mesh.md) §7, M1 and M2). The ids R6 and R8
are not reused.

## 3. What stays optional, even under the profile

The profile mandates capabilities, not idioms or extras. The following remain optional for a
profiled service:

| Still optional | Why |
|---|---|
| The UIs (`/benzene/spec-ui`, `/benzene/mesh-ui`, `/benzene/fleet-ui`) | Human conveniences; tooling consumes the machine surfaces (R3–R5) |
| Joining a mesh | The meshed-service claim ([mesh.md](mesh.md) §7) is separate: it needs a collector, and the profile does not |
| Any specific transport binding | R1/R4 require *at least one* binding; which ones is the service's business |
| Handler discovery idiom | Attribute scanning vs explicit calls is an idiom ([core-concepts.md](core-concepts.md) §9); R2 requires the registry, not a discovery style |
| Convention overrides | Every configurable name and replaceable default ([wire-contracts.md](wire-contracts.md) §2, [core-concepts.md](core-concepts.md) §8) stays replaceable; the profile constrains defaults and presence, not customizability |
| Custom statuses, extra topics, application middleware | The profile is a floor, not a ceiling |

## 4. Degradation vs. non-conformance

The mesh degradation rules ([mesh.md](mesh.md) §6) and the profile claim answer different
questions, and both stay in force:

- **Runtime degradation is not a conformance failure.** A transient health-check failure, a slow
  dependency — a profiled service degrades and keeps its claim (and a meshed service's
  unreachable collector or full trace buffer degrades exactly as mesh §6 specifies). The profile requires the capabilities to be
  *provisioned*, not that the network never fails.
- **Deliberate omission is what the profile rules out.** A service that never installs health
  checks, never derives its contract document, or serves topics outside the registry is a perfectly good
  **Benzene Core** service — it just isn't a **Benzene Cloud Service**, and fleet tooling makes
  no promises about it beyond the reduced rendering of mesh §6.
- **Deployment-time exposure control sits in between.** Blocking `/benzene/spec*` at a gateway
  pending a security review ([transport-bindings.md](transport-bindings.md) §1) is an operations
  decision about a *deployment*, not a gap in the *service*. The service remains
  profile-conformant; that deployment presents to fleet tooling as reduced, per mesh §6, for
  exactly the blocked surfaces.

## 5. Conformance testing

- **Benzene Core** (implementation-level): pass the Core fixtures listed in
  [conformance/README.md](conformance/README.md) ("Which fixtures a given conformance claim
  requires").
- **Benzene Cloud Service** (profile support): additionally pass the contract-document producer
  groups ([contract-document.md](contract-document.md) §8). The meshed-service claim's fixtures
  are [mesh.md](mesh.md) §7.2's.
- **Self-assessment.** A service MAY self-assess against this profile in its contract document's
  `profile` field ([contract-document.md](contract-document.md) §1): `name` is `"cloud-service"`,
  and `missing` lists the ids of §2 (`R1`–`R5`, `R7`) its own wiring knows it does not meet. A
  consumer ignores an id it does not recognise.
- *(Informative.)* A port MAY offer a wiring-time **self-check** that evaluates a service's setup
  against §2 and fills the `profile` field, and an external **probe** that audits any service
  over HTTP. Neither is
  required by the wire contract; .NET ships both (`Benzene.CloudService` and
  `Benzene.CloudService.Probe` — see the
  [benzene-dotnet docs](https://github.com/daniellepelley/benzene-dotnet/tree/main/docs)).

## 6. Relationship to the adoption ladder *(informative)*

The profile is not a new idea grafted onto Benzene — it is a **name for the top of the ladder**
that [the adoption ladder](../guides/design-principles.md#2-the-adoption-ladder) already defines. The ladder describes the
journey (each rung optional, stop anywhere); the profile marks the destination fleet tooling can
rely on:

| Adoption ladder rung | Profile requirement |
|---|---|
| 2. Hosted middleware-only | R1 |
| 3. Message handlers | R2, R5 |
| 4. Default service standard | R3, R4, R7 |
| 5. Meshed | — (the meshed-service claim, [mesh.md](mesh.md) §7) |

Nothing about the ladder changes for Core-level services: every rung below the profile remains
first-class and indefinitely inhabitable. The profile exists so that "we run Benzene Cloud
Services" is a complete, precise statement of what an operator, a teammate, or a tool will find;
"and they are meshed" adds the fleet view.

## 7. Naming note *(informative)*

"Profile" follows established spec practice (e.g. WS-I Basic Profile): a base specification
stays maximally permissive, and a named profile layers interoperability requirements on top for
those who opt in. The base level is referred to as the **Benzene Core Specification** to keep
the two unambiguous — "conforms to Benzene" is Core; "conforms to the Cloud Service Profile" is
this document.
