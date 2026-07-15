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
- `examples/` — sample usage projects
- `docs/` — documentation
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
