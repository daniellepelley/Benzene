# Benzene

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Benzene is a hexagonal (ports-and-adapters) architecture for message-driven services: you write a
message handler once, against a topic, and run it behind any transport — HTTP, queues, streams,
serverless functions — with cross-cutting concerns (logging, correlation IDs, validation, health
checks) composed as middleware rather than scattered through your code.**

Benzene is defined by a **language-neutral specification**, so it isn't one library — it's a design
that each language implements as its own idiomatic port. This repo is the **cross-language home**: the
specification and the project website. The actual implementations live in per-language repos.

## The specification

The spec is the source of truth every port implements — concepts, wire contracts, transport bindings,
the mesh contracts, the Cloud Service Profile, and a porting guide, plus language-neutral
**conformance fixtures** every implementation runs to prove it conforms.

➡️ **[Read the specification](docs/specification/README.md)**

## Pick your language

| Language | Repo | Status |
|----------|------|--------|
| **.NET** | [benzene-dotnet](https://github.com/daniellepelley/benzene-dotnet) | The reference implementation — full docs, examples, templates, mesh tooling |
| **Go** | [benzene-go](https://github.com/daniellepelley/benzene-go) | Early port |
| **TypeScript** | [benzene-typescript](https://github.com/daniellepelley/benzene-typescript) | Early port |
| **Python** | [benzene-python](https://github.com/daniellepelley/benzene-python) | Early port |

Each port is a translation of the same spec into that language's idioms — same topics, same wire
contracts, same conformance fixtures.

## The website

[benzene.app](https://benzene.app) is built from this repo. Its generator (`website/`) stitches the
spec here together with each language port's own docs (checked out from its repo) into one site: a
language switcher, a cross-language docs hub, and a per-language section for each port. See
[`website/README.md`](website/README.md).

## What's in this repo

- `docs/specification/**` — the language-neutral specification (the cross-language source of truth)
- `docs/capabilities.md` — the consolidated cross-port capability matrix: what each language port
  does, deliberately doesn't, and where they diverge (descriptive, built from each port's own
  capability record)
- `website/` — the static-site generator for benzene.app
- `blog/` — the project blog
- `work/` — planning and design notes (actioned plans live in `work/archive/`)

The .NET implementation that used to live here moved to
[benzene-dotnet](https://github.com/daniellepelley/benzene-dotnet); see
`work/archive/repo-split-plan.md` for that split.

## Contributing

Contributions to the spec and the website are welcome — see [CONTRIBUTING.md](./CONTRIBUTING.md). For
a specific language implementation, contribute in that language's repo.

## License

MIT — see [LICENSE](./LICENSE).
