# Benzene Specification (Draft)

**Status: DRAFT v0.1 — extracted from the .NET implementation, which remains the single normative
reference while this document matures.**

## Purpose

This directory defines Benzene's **portable core**: the concepts, semantics, and wire contracts
that any implementation of Benzene — in any language, on any cloud vendor — must honor. It exists
so that:

1. **Design decisions outlive their C# encoding.** The .NET implementation is full of C# idioms
   (attributes, generics, MS DI, `IAsyncEnumerable`). This spec records what each feature *means*,
   separately from how C# happens to express it.
2. **A future port is a translation of a design, not a rewrite.** An implementation in another
   language implements these documents, not the C# API shape.
3. **Cross-language interop works from day one.** Two Benzene services in different languages
   interoperate through the wire contracts in this spec (message envelope, headers, status
   vocabulary, trailers) — that, not API similarity, is the point of a multi-language Benzene.

## Documents

| Document | Contents |
|---|---|
| [design-principles.md](design-principles.md) | The "opinionated but optional" strategy: the adoption ladder (middleware-only and in-process use are first-class), what each capability requires and how it degrades, the extension-point catalog (every convention overridable on both sides of the wire), and the default service standard (`/benzene/`-prefixed well-known surfaces) |
| [core-concepts.md](core-concepts.md) | The abstract model: pipeline, context, message handler, topic, result, lifecycle, registration |
| [wire-contracts.md](wire-contracts.md) | Everything that crosses a process boundary: the message envelope, header conventions, the status vocabulary and its per-protocol mappings, the health check response format |
| [transport-bindings.md](transport-bindings.md) | What a transport adapter is and the contract every binding must satisfy, with the existing bindings as worked examples |
| [mesh.md](mesh.md) | The optional mesh module's wire contracts: service self-description (descriptor + derived payload schemas + contract hash), semantic trace events, collector topics, heartbeats, and the normative degradation rules. Reference implementation: the Go port's `mesh`/`meshd` packages |
| [porting-guide.md](porting-guide.md) | Concept-vs-idiom mapping for implementers in other languages, and the conformance-testing approach |
| [conformance/](conformance/README.md) | Language-neutral test fixtures (status vocabulary, mapping tables, envelope cases) that every implementation runs; the .NET reference runner is `test/Benzene.Conformance.Test/` |

## Conformance language

The key words MUST, MUST NOT, SHOULD, and MAY are used as described in
[RFC 2119](https://www.rfc-editor.org/rfc/rfc2119). Sections marked *(informative)* describe the
.NET implementation for illustration and are not requirements.

## Versioning

This spec is versioned independently of the .NET packages. While in draft (0.x), wire contracts may
still change; from 1.0, changes to anything in [wire-contracts.md](wire-contracts.md) follow
semver — a breaking change to an on-the-wire format is a major version.

## The one design rule

Every new Benzene feature must answer: **is this a Benzene concept or a language idiom?**

- Concepts (a status, a header convention, a pipeline behavior, an envelope field) belong in this
  spec *before or alongside* the code that implements them.
- Idioms (attribute-based discovery, a DI container adapter, an accessor pattern) stay in the
  implementation and its per-package docs, and MUST always have a spec-level, idiom-free
  equivalent (e.g. attribute scanning is sugar over explicit registration — the explicit path is
  the concept; the scan is the idiom).
