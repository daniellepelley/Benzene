# Benzene Specification

The language-neutral definition of Benzene — the concepts, wire contracts, and conformance fixtures
every language port implements. New here? Start with the [overview](README.md), then work through the
documents below.

- **The Core Specification**
  - [Overview](README.md) — how the spec is organized and the two conformance levels (Core vs. the Cloud Service Profile)
  - [Design Principles](design-principles.md) — "opinionated but optional": the adoption ladder, and the rule that every steer must be replaceable
  - [Core Concepts](core-concepts.md) — the abstract model: pipeline, context, message handler, topic, result, lifecycle
  - [Wire Contracts](wire-contracts.md) — the message envelope, header conventions, the status vocabulary, and its per-protocol (HTTP/gRPC) mappings
  - [Transport Bindings](transport-bindings.md) — what a transport adapter must satisfy, with every existing binding as a worked example
  - [Mesh Contracts](mesh.md) — service self-description, trace events, and collector topics for fleet-wide visibility
  - [Payload Schema Versioning](versioning.md) — versioning a topic's request/response shape independently of handler versioning
  - [Porting Guide](porting-guide.md) — concept-vs-idiom mapping and suggested order for implementing Benzene in another language
  - [Port Quality Standards](port-quality-standards.md) — the Definition of Done for a language port: the DX-champion-in-the-loop workflow and the quality gates a unit of port work must clear
  - [Conformance Fixtures](conformance/README.md) — the language-neutral test fixtures every implementation runs to prove conformance
- **The Cloud Service Profile**
  - [Cloud Service Profile](cloud-service-profile.md) — the named conformance target (requirements R1–R8) that guarantees mesh, Spec UI, and fleet tooling work on a service with no per-service negotiation
