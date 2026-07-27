# Code Generation

Code generation in Benzene is not a feature of any one language — it is a consequence of the
**Cloud Service Profile**'s self-description ([cloud-service-profile.md](../specification/cloud-service-profile.md))
and the **mesh descriptor** ([mesh.md](../specification/mesh.md) §2). A conforming service already
knows, at runtime, the topics it handles, the request and response payload shapes for each, the
broadcast events it emits, and its validation rules. Anything you can generate from that
self-description, you can generate from *any* Benzene service, in *any* language, without the
generator knowing which language produced it.

This guide describes the concept and the artifacts. **How to invoke a generator** — the package
name, the command, the API — is language-specific and lives in each port's own docs.

## The one idea: generate *from the description*, not from the code

A Benzene service exposes its own contract in a language-neutral form:

- the **derived spec** (the reserved `spec` operation) — the service's topics, payloads, and events;
- the **mesh descriptor** (the reserved `mesh` topic) — the same contract plus a stable
  `descriptorHash` that changes only when the observable contract changes.

Both are wire artifacts defined by the specification, so a generator consumes them the same way
whether the service is written in .NET, Go, TypeScript, or Python. The generated output tracks what the
service *actually* exposes, so it can never drift from the implementation the way a hand-maintained
schema or diagram does.

## What can be generated

| Artifact | Generated from | What it's for |
|---|---|---|
| **OpenAPI** | the derived spec (HTTP-mapped topics) | REST tooling, API gateways, doc portals, HTTP client generators |
| **AsyncAPI** | the derived spec (broadcast events + message topics) | event-driven tooling and message-contract documentation |
| **Client SDKs** | the derived spec / OpenAPI | typed callers for another service's topics, without hand-writing request/response types |
| **Infrastructure** | the service's declared topics and hosting settings | provisioning the transports a service consumes/produces (e.g. an event-source per topic) as infrastructure-as-code |

Infrastructure generation is worth calling out because it is often assumed to be language-bound and
is not: a queue, a topic subscription, or an HTTP route is described by *what the service handles*,
not by the language of the handler. The generated infrastructure (for example Terraform) is the
same shape regardless of the port; only the deployment package it points at differs.

## Why this is language-neutral

- The **inputs** are spec-defined wire artifacts (the derived spec, the mesh descriptor), not
  language types.
- The **outputs** are ecosystem-standard formats (OpenAPI, AsyncAPI) or infrastructure formats
  (Terraform, and others), none of which are tied to a runtime language.
- A generator is therefore a consumer of the specification, and a service in any conforming language
  is a valid source for it.

## Where the language-specific parts live

The *how* is idiomatic per language and is documented in each port's repo:

- **.NET** — [benzene-dotnet](https://github.com/daniellepelley/benzene-dotnet)
- **Go** — [benzene-go](https://github.com/daniellepelley/benzene-go)
- **TypeScript** — [benzene-typescript](https://github.com/daniellepelley/benzene-typescript)
- **Python** — [benzene-python](https://github.com/daniellepelley/benzene-python)

Look there for the package that runs the generator, the command-line or API to invoke it, and the
exact settings each generator accepts. This guide stays with the concept: the description exists, it
is language-neutral, and generation is the act of turning it into an artifact.

## See also

- [Cloud Service Profile](../specification/cloud-service-profile.md) — the self-description a
  generator consumes (the derived spec, R5)
- [Mesh Contracts](../specification/mesh.md) — the service descriptor and its contract hash
- [Wire Contracts](../specification/wire-contracts.md) — the envelope and status vocabulary the
  generated clients speak
