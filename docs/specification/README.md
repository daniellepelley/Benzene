# Benzene Specification

The canonical, language-neutral definition of Benzene, which every language port implements. The
version of the specification is in [`VERSION`](VERSION); each release is tagged `spec-x.y.z`, and a
port's conformance fixtures pin the release they implement ([conformance](conformance/README.md)).

## Tiers

| Tier | What it is | Stability |
|---|---|---|
| **Core** | What every implementation honours for the capabilities it uses | Frozen under semver from 1.0 |
| **Modules** | Optional, self-contained additions; an implementation that offers one offers it in exactly this shape | **Stable**, or **Provisional** until a second port implements it (the label names the ports that do) |
| **Profile** | The Cloud Service Profile: a named conformance target that raises chosen SHOULDs and MAYs to MUSTs for deployed services | Frozen under semver from 1.0, like Core |
| **Guidance** | Informative: strategy, porting advice, quality standards | Unversioned; lives in [`docs/guides/`](../guides/index.md) |

**Optional capabilities.** Core also names a few capabilities a port need not offer — versioned
handlers and payload casting, W3C trace context, and health checks
([core-concepts.md](core-concepts.md) §10). One that does offers it in exactly the shape the spec
defines. (Reading the version header and selecting a handler by it is not optional.)

"Conforms to Benzene" means Core. "Is a Benzene Cloud Service" means Core plus the profile.

### Core

| Document | Contents |
|---|---|
| [core-concepts.md](core-concepts.md) | The abstract model: topic and version selection, handler, pipeline, result, context, application definition, scope, optional capabilities, steers, outbound |
| [wire-contracts.md](wire-contracts.md) | Everything that crosses a process boundary: the envelope, problem details, headers, the status vocabulary and its HTTP/gRPC mappings, the health response, serialization, reserved topics |
| [transport-bindings.md](transport-bindings.md) | The contract every binding satisfies — including settlement, the envelope endpoint and the default `/benzene/` paths — with the existing bindings as worked examples |
| [versioning.md](versioning.md) | The version header, absence, dispatch and payload casting |

### Modules

| Document | Stability | Contents |
|---|---|---|
| [mesh.md](mesh.md) | Stable | Service self-description, trace events, collector topics, heartbeats and degradation |
| [contract-document.md](contract-document.md) | Stable | The self-description at `benzene:spec`: requests, events and sends, the derivation and naming rules, the two cross-port hashes, and the client-generation rules |
| [service-versions.md](service-versions.md) | Provisional: .NET | Service-version identity and order, for releases deployed side by side |
| [mcp.md](mcp.md) | Provisional: .NET | A binding (claimed per binding, documented like a module): a service's handlers as tools for AI clients |
| [claim-check.md](claim-check.md) | Provisional: .NET | Large payloads passed by reference to an external store |

### Profile

| Document | Contents |
|---|---|
| [cloud-service-profile.md](cloud-service-profile.md) | Requirements R1–R8 — hosted pipeline, registry-served handlers, health checks, envelope invocability, derived spec, mesh feeds, default paths, trace propagation — what stays optional, and how runtime degradation differs from non-conformance |

### Conformance

[conformance/](conformance/README.md) holds the language-neutral fixtures every implementation runs,
and states which fixtures each claim — Core, per binding, the profile, per module — requires.

## Conformance language

The key words MUST, MUST NOT, SHOULD, and MAY are used as described in
[RFC 2119](https://www.rfc-editor.org/rfc/rfc2119). Sections marked *(informative)* illustrate and
are not requirements. While the specification is 0.x, contracts may still change; from 1.0, a
breaking change to Core or the profile is a major version.

## The one design rule

Every new Benzene feature must answer: **is this a Benzene concept or a language idiom?**

- Concepts (a status, a header convention, a pipeline behavior, an envelope field) belong in this
  spec *before or alongside* the code that implements them.
- Idioms (attribute-based discovery, a DI container adapter, an accessor pattern) stay in the
  implementation and its per-package docs, and MUST always have a spec-level, idiom-free
  equivalent (e.g. attribute scanning is sugar over explicit registration — the explicit path is
  the concept; the scan is the idiom).
