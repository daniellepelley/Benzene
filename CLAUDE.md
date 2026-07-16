# Benzene — Project Guide for Claude Code

## What this is
Benzene is a C# middleware-based library supporting hexagonal
(ports-and-adapters) architecture. It provides a pipeline of
middleware components that wrap calls to "ports" (interfaces
representing external boundaries — DB, HTTP, queues, etc).

## Structure
- `src/` — library source
- `test/` — unit/integration tests
- `benchmarks/` — BenchmarkDotNet micro-benchmarks (compile-checked via `Benzene.sln`, but not run
  as part of CI — see `benchmarks/Benzene.Benchmarks/README.md`)
- `templates/` — `dotnet new` starter-project templates, packaged as one NuGet template pack
  (`Benzene.Templates`); own `templates/Benzene.Templates.sln` for local dev, verified by
  `.github/workflows/build-templates.yml`, not part of `Benzene.sln` — see `templates/README.md`
- `deploy/` — independently-versioned, independently-built deployable artifacts (Docker-packaged,
  not NuGet-packaged) with their own release lifecycle — e.g. `deploy/Mesh/Benzene.Mesh.Host`, a
  config-driven Mesh Aggregator+UI for `docker-compose`; own `.sln` per artifact, not part of
  `Benzene.sln`/`Benzene.Examples.sln` — see `deploy/Mesh/README.md`
- `examples/` — sample usage projects
- `docs/` — documentation
- `website/` — static marketing + docs site generator (deploys `docs/`/`README.md` to S3), its
  own standalone project, not part of `Benzene.sln` — see `website/README.md`
- `Benzene.sln` — main library solution
- `Benzene.Examples.sln` — examples solution
- `.github/workflows/` — CI

## Before making changes
- Read existing middleware implementations in `src/` first and
  follow their exact pattern (naming, constructor shape, async
  conventions) rather than inventing a new style.
- Check `test/` for the existing test conventions (framework,
  naming, arrange/act/assert style) before writing new tests.
- Rebase from `main` before making any changes.

## Conventions (verify against actual code, then keep this updated)
- Language: C#, target framework(s) — confirm from .csproj files
- Testing framework — confirm from test project references
- Async/await used throughout for I/O-bound operations
- Middleware components follow a consistent interface for
  wrapping port calls in the pipeline
- Context types (`TContext`) stay pure — describe the transport message's
  shape only. For a middleware-to-later-step handoff scoped to one specific
  pipeline (e.g. a per-queue override), use a small scoped DI-registered
  holder instead of adding a marker interface/property to the context — see
  `src/Benzene.Abstractions.Middleware/CLAUDE.md`'s "Context purity" section
  and the `PresetTopicHolder` example in `Benzene.Core.MessageHandlers`

## Do NOT
- Do not modify `Benzene.sln` / `Benzene.Examples.sln` structure
  without explicit approval
- Do not add new NuGet dependencies without asking first
- Do not change public API signatures on existing middleware
  without flagging it as a breaking change
- Do not skip or disable existing tests to make a build pass

## Workflow expectations
- Use Plan Mode for any non-trivial feature: propose a plan,
  wait for approval, then implement
- Run the full test suite before considering a task complete
- Keep commits scoped to one logical change
