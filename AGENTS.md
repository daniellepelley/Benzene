# Benzene — Project Guide for AI Coding Agents

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

## Dev environment
- Requires .NET 10 (see `.csproj` `TargetFramework`s; a few packages also target `net6.0`/
  `netstandard2.0` for backward compatibility, buildable fine under the .NET 10 SDK).
- No `global.json` pins a specific SDK patch version — match whatever `.github/workflows/
  build-benzene.yml`'s `actions/setup-dotnet` step installs (currently `10.0.x`) if you need to be precise.
- `dotnet build Benzene.sln` / `dotnet test test/Benzene.Core.Test/Benzene.Test.csproj` are the
  local build/test commands. A local .NET SDK is **not guaranteed** in every agent environment —
  if `dotnet` isn't available, say so plainly and fall back to CI
  (`.github/workflows/build-benzene.yml`) as the verification loop instead of guessing whether
  something compiles.

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
- Use a plan-first approach for any non-trivial feature: propose a plan,
  wait for approval, then implement
- Run the full test suite before considering a task complete
- Keep commits scoped to one logical change

## More detail, per package
Every `src/<Package>/` directory has its own `CLAUDE.md` with that package's specific intent, key
types, and conventions — read the relevant one(s) before working in that package. This root file
only covers what applies repo-wide.
