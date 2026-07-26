# Benzene (.NET) — Project Guide for AI Coding Agents

## What this is
This is the **.NET port of Benzene** — a C# middleware-based library supporting hexagonal
(ports-and-adapters) architecture. It provides a pipeline of middleware components that wrap calls
to "ports" (interfaces representing external boundaries — DB, HTTP, queues, etc).

Benzene is defined by a **language-neutral specification** that does **not** live here — it is the
cross-language source of truth in the [`benzene`](https://github.com/daniellepelley/Benzene) repo
(`docs/specification/**`), alongside the project website. This repo is one language implementation
of that spec. Other languages are separate ports of the same spec. When a `docs/` page or code
comment here refers to `docs/specification/...`, that path is in the `benzene` repo.

## Structure
- `src/` — library source
- `test/` — unit/integration tests
  - `test/conformance-fixtures/` — a **vendored snapshot** of the language-neutral conformance
    fixtures from `benzene` (`docs/specification/conformance/*.json`). Do **not** edit these here;
    the canonical copy lives in `benzene` and the `conformance-drift-check` workflow fails if this
    snapshot drifts. `SPEC_VERSION` records the source commit.
- `benchmarks/` — BenchmarkDotNet micro-benchmarks (compile-checked via `Benzene.sln`, not run in CI)
- `templates/` — `dotnet new` starter-project templates, packaged as one NuGet template pack
  (`Benzene.Templates`); own `templates/Benzene.Templates.sln`, verified by `build-templates.yml`
- `deploy/` — independently-versioned deployable artifacts (Docker-packaged) — e.g.
  `deploy/Mesh/Benzene.Mesh.Host`; own `.sln`, not part of `Benzene.sln`
- `tools/` — .NET tooling (`Benzene.Descriptor`)
- `examples/` — sample usage projects (`Benzene.Examples.sln`)
- `docs/` — **.NET** documentation (how to use Benzene in C#). The language-neutral spec is in
  `benzene`, not here.
- `Benzene.sln` — main library solution
- `Benzene.Examples.sln` — examples solution
- `.github/workflows/` — CI

## Relationship to the `benzene` repo (the split)
- **`benzene`** holds the cross-language material: the spec definition (`docs/specification/**`) and
  the website that renders every language's docs into per-language sections.
- **`benzene-dotnet`** (this repo) holds the entire .NET port. `git clone && dotnet test` works with
  no submodule and no dependency on `benzene` at build time.
- The only coupling is the vendored conformance fixtures (above), guarded by CI drift-check.
- The website builds out of `benzene` and consumes this repo's `docs/**` by CI checkout of `main`.
  See the split plan in the `benzene` repo (`work/repo-split-plan.md`).

## Dev environment
- Requires .NET 10 (see `.csproj` `TargetFramework`s; a few packages also target `net6.0`/
  `netstandard2.0` for backward compatibility, buildable under the .NET 10 SDK).
- No `global.json` pins a specific SDK patch — match whatever `.github/workflows/build-benzene.yml`'s
  `actions/setup-dotnet` step installs (currently `10.0.x`).
- `dotnet build Benzene.sln` / `dotnet test test/Benzene.Core.Test/Benzene.Test.csproj` are the local
  build/test commands. If `dotnet` isn't available in your environment, say so and fall back to CI
  (`.github/workflows/build-benzene.yml`) as the verification loop rather than guessing.

## Before making changes
- Read existing middleware implementations in `src/` first and follow their exact pattern (naming,
  constructor shape, async conventions) rather than inventing a new style.
- Check `test/` for the existing test conventions before writing new tests.
- Rebase from `main` before making any changes.
- If a change affects an observable contract the spec defines (wire format, status vocabulary, mesh
  shapes), that is a **spec change** — it belongs in the `benzene` repo's `docs/specification/**`
  first, and the conformance fixtures re-vendored here.

## Conventions (verify against actual code, then keep this updated)
- Language: C#, target framework(s) — confirm from .csproj files
- Async/await used throughout for I/O-bound operations
- Middleware components follow a consistent interface for wrapping port calls in the pipeline
- Context types (`TContext`) stay pure — describe the transport message's shape only. For a
  middleware-to-later-step handoff scoped to one pipeline, use a small scoped DI-registered holder
  instead of adding a marker to the context — see `src/Benzene.Abstractions.Middleware/CLAUDE.md`.

## Do NOT
- Do not modify `Benzene.sln` / `Benzene.Examples.sln` structure without explicit approval
- Do not add new NuGet dependencies without asking first
- Do not change public API signatures on existing middleware without flagging it as breaking
- Do not skip or disable existing tests to make a build pass
- Do not edit `test/conformance-fixtures/**` to make a test pass — fix the code, or change the spec
  in `benzene` and re-vendor

## Workflow expectations
- Plan-first for any non-trivial feature: propose a plan, wait for approval, then implement
- Run the full test suite before considering a task complete
- Keep commits scoped to one logical change

## More detail, per package
Every `src/<Package>/` directory has its own `CLAUDE.md` with that package's specific intent, key
types, and conventions — read the relevant one(s) before working in that package.
