# Benzene Specification

The language-neutral definition of Benzene — the concepts, wire contracts, and conformance fixtures
every language port implements. Start with the [overview](README.md): it explains the tiers and the
stability labels.

- [Overview](README.md) — the tiers (Core, Modules, Profile, Guidance), stability labels and the spec version
- **Core**
  - [Core Concepts](core-concepts.md) — the abstract model: topic and version selection, handler, pipeline, result, scope, steers, outbound
  - [Wire Contracts](wire-contracts.md) — the envelope, problem details, headers, the status vocabulary and its mappings, reserved topics
  - [Transport Bindings](transport-bindings.md) — the binding contract, settlement, the envelope endpoint and default paths, with worked examples
  - [Versioning](versioning.md) — the version header, absence, dispatch and payload casting
- **Modules**
  - [Mesh](mesh.md) — Stable: service self-description, trace events, collector topics and degradation
  - [Contract Document](contract-document.md) — Stable: the self-description at `benzene:spec`, its derivation, naming and hashes, and client generation
  - [Service Versions](service-versions.md) — Provisional (.NET): service-version identity and order
  - [MCP](mcp.md) — Provisional (.NET): a service's handlers as tools for AI clients
  - [Claim Check](claim-check.md) — Provisional (.NET): large payloads passed by reference
- **Profile**
  - [Cloud Service Profile](cloud-service-profile.md) — requirements R1–R8 for a fully tool-operable service
- **Conformance**
  - [Conformance Fixtures](conformance/README.md) — the fixtures every implementation runs, and which each claim requires
- **Guidance**
  - [Design Principles](../guides/design-principles.md) — "opinionated but optional": the adoption ladder and why every steer is replaceable
  - [Porting Guide](../guides/porting-guide.md) — concept versus idiom, and a suggested porting order
  - [Port Quality Standards](../guides/port-quality-standards.md) — the Definition of Done for a language port
