# Contributing to Benzene

Thanks for your interest in contributing. Benzene is pre-1.0 (see `version.txt`) and still
evolving quickly, so please read this file — and `AGENTS.md` — before sending a change.

## Getting set up

- Requires the .NET 10 SDK (see individual `.csproj` `TargetFramework`s; a few packages also
  target `net6.0`/`netstandard2.0` for backward compatibility, but build fine under the .NET 10
  SDK). No `global.json` pins an exact patch version — match whatever
  `.github/workflows/build-benzene.yml`'s `actions/setup-dotnet` step installs.
- Clone the repo and open `Benzene.sln` (the main library solution). `Benzene.Examples.sln`,
  `templates/Benzene.Templates.sln`, and the per-artifact solutions under `deploy/` are separate —
  see `AGENTS.md`'s Structure section for what's part of what.
- Build: `dotnet build Benzene.sln`
- Test: `dotnet test test/Benzene.Core.Test/Benzene.Test.csproj` — the primary, fastest local test
  loop. CI (`build-benzene.yml`) additionally runs `Benzene.Grpc.Test`, `Benzene.Mesh.Test`, and
  `Benzene.Conformance.Test` in the same job, plus `Benzene.Aws.Tests`/`Benzene.Integration.Test`
  in separate jobs that spin up Docker-based emulation (SQS/SNS/DynamoDB/Service Bus/Event Hubs, via
  FluentDocker) — run those locally too if your change touches AWS/Azure transports.

## Before you start

- **Read `AGENTS.md` in full** — it's the canonical project guide (Claude Code reads it via
  `CLAUDE.md`'s pointer, but it applies to any contributor or AI coding tool equally). It covers
  repo structure, conventions, and what not to do.
- **Read the package's own `CLAUDE.md`** — every `src/<Package>/` directory has one describing
  that package's specific intent, key types, and conventions. Match its existing patterns (naming,
  constructor shape, async conventions) rather than inventing a new style.
- **Check `test/` for existing test conventions** in the area you're touching (framework, naming,
  arrange/act/assert style) before writing new tests.
- **Rebase from `main`** before starting any change.

## Making a change

- Use a plan-first approach for any non-trivial feature: propose what you intend to do before
  writing the implementation, especially if it touches more than one package or changes a public
  API shape.
- Keep commits scoped to one logical change.
- Do not modify `Benzene.sln` / `Benzene.Examples.sln` structure without discussing it first.
- Do not add new NuGet dependencies without a strong reason — raise it first.
- Do not change public API signatures on existing middleware without flagging it as a breaking
  change (see `CHANGELOG.md`'s `**BREAKING:**`-prefixed entries for the expected style, and
  `docs/migration-alpha-to-1.0.md` for how breaking renames are documented for consumers).
- Do not skip or disable existing tests to make a build pass.
- Prefer editing existing files/patterns over introducing new abstractions — see AGENTS.md's "no
  premature abstraction" convention, illustrated in `work/batch-failure-handling.md`'s "Why this
  wasn't built as one shared abstraction" section.

## Submitting

- Run the full test suite (`dotnet test test/Benzene.Core.Test/Benzene.Test.csproj`, plus
  `Benzene.Grpc.Test`/`Benzene.Mesh.Test`/`Benzene.Conformance.Test` and any Docker-based suites
  relevant to your change) before opening a PR — CI runs the same set and will block on failures.
- Update the relevant package `CLAUDE.md`(s) and add a `CHANGELOG.md` entry under `## [Unreleased]`
  in the same PR — documentation drift is treated as part of the change, not a follow-up.
- Describe *why* the change is needed, not just what it does — PR descriptions and commit messages
  are the record that survives once the code itself is refactored.

## Questions

Open an issue, or start a discussion, before investing significant effort in a large or
architecturally significant change — Benzene's maintainers would rather weigh in early than ask
for a rewrite after the fact.
