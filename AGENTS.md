# Benzene — Project Guide for AI Coding Agents

## What this is
This is the **cross-language home** of Benzene, a hexagonal (ports-and-adapters) architecture for
message-driven services. It holds two things:

1. **The language-neutral specification** (`docs/specification/**`) — the source of truth every
   language port implements: concepts, wire contracts, transport bindings, mesh contracts, the Cloud
   Service Profile, a porting guide, and language-neutral **conformance fixtures**.
2. **The website** (`website/`) — the generator for [benzene.app](https://benzene.app), which stitches
   the spec here together with each language port's own docs into one multi-language site.

**This repo contains no language implementation.** The .NET port (and the code/tests/examples that
used to live here) is in [benzene-dotnet](https://github.com/daniellepelley/benzene-dotnet); Go is in
[benzene-go](https://github.com/daniellepelley/benzene-go); TypeScript is in
[benzene-typescript](https://github.com/daniellepelley/benzene-typescript). The split is recorded in
`work/repo-split-plan.md`.

## Structure
- `docs/specification/` — the spec (Markdown) + `conformance/*.json` fixtures. The **canonical** copy;
  each language repo vendors a snapshot of the fixtures and CI-checks it against this one.
- `website/` — the static-site generator (a .NET console app using Markdig) + demos + assets. This is
  the only .NET project in the repo; it's a build tool, not a shipped package. See `website/CLAUDE.md`.
- `blog/` — the project blog (Markdown).
- `work/` — planning/design notes, including the repo-split plan/manifest/status.
- `.github/workflows/` — `deploy-website.yml` (build + publish to dev) and `promote-website.yml`
  (dev → live). The language-implementation CI lives in each language repo, not here.

## The specification is the product here
- A change to an **observable contract** (wire format, status vocabulary, mesh shapes, the Cloud
  Service Profile) is a **spec change**: make it in `docs/specification/**`, update the conformance
  fixtures, and expect every language port to re-vendor and re-verify. Don't change a fixture to match
  one implementation's quirk — the fixture is the neutral truth.
- Keep the spec taut: it should cover what a conforming service must do and no more.

## Working on the website
- Requires .NET 10. Run from the repo root:
  `dotnet run --project website/generator -- --out website/dist`.
- Multi-source: the `.NET` docs come from a benzene-dotnet checkout via `--dotnet-docs <path>`; extra
  languages via `--source id::Label::urlPrefix::path[::navFile][::landing][::<repoBlobUrl>]`. With no
  flags it builds the spec + hub only. See `website/CLAUDE.md` for the full model.
- `website/dist/` is gitignored; CI regenerates it. The broken-link self-check fails the build.

## Conventions
- Markdown for spec/blog; the website generator is C#.
- Repo-relative paths in the generator are forward-slash strings regardless of host OS.

## Do NOT
- Do not add a language implementation here — it belongs in that language's repo.
- Do not edit a conformance fixture to make one implementation pass — fix the implementation, or
  change the spec deliberately.
- Do not break the website generator's build or its broken-link self-check.

## Workflow expectations
- Plan-first for non-trivial spec or website changes.
- Keep commits scoped to one logical change.
