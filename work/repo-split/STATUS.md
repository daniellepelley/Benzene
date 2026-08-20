# Repo split — execution status

Live status of the autonomous execution of [`../repo-split-plan.md`](../repo-split-plan.md).

## Phase 1 — Stand up benzene-dotnet — DONE ✅

**Published:** `daniellepelley/benzene-dotnet` `main` = commit `c60c93f` (3388 files). The repo
already existed (the maintainer had created it, along with sibling `benzene-go` and
`benzene-typescript`); the earlier `403` was the integration lacking repo-*creation* rights, not a
wrong name. Regenerated from current benzene HEAD, re-verified (`Benzene.sln` builds standalone,
0 errors; conformance 134/0), committed as the maintainer (clean-snapshot history, no AI trailers),
and pushed. The `deploy-website.yml` benzene-dotnet checkout will now succeed on its next run.

Original blocker (now resolved) was recorded here: repo creation returned `403 Resource not
accessible by integration`; the maintainer had already created the repo.

**What is done and verified:**
- Full move/stay split executed into a staging tree (`git archive` clean snapshot — tracked files
  only, no `bin/obj`, no history).
- Conformance fixtures vendored to `test/conformance-fixtures/` (+ `SPEC_VERSION`, README); the
  `Benzene.Conformance.Test.csproj` fixture path repointed at the vendored dir; the source comment
  updated. This is the **only** code change in the entire MOVE.
- `conformance-drift-check` workflow added (fetches canonical fixtures from benzene, fails on drift).
- Standalone `README.md` / `AGENTS.md` / `CLAUDE.md` written for the .NET-port repo; `docs/index.md`
  spec section replaced with a pointer to the canonical spec in benzene.
- **Verified `dotnet build Benzene.sln` → `Build succeeded, 0 Error(s)`** in the staged tree — proves
  the .NET port is self-contained, nothing references files left behind in benzene.
- **Verified `dotnet test Benzene.Conformance.Test` → 134 passed, 0 failed** against the vendored
  fixtures. The rest of `test/` is byte-identical to benzene (already green in its CI).

**Artifacts (all committed under `work/repo-split/`):**
- `populate-benzene-dotnet.sh` — the exact, reproducible recipe (re-runnable; ends with the push
  commands).
- `overlay/` — the files that differ from their benzene originals (repo docs, drift-check workflow,
  index.md, vendored-fixtures README).

**To finish Phase 1 once the repo exists** (either run the script, or the two push lines):
```
BENZENE=/home/user/Benzene STAGE=/home/user/benzene-dotnet-staging \
  work/repo-split/populate-benzene-dotnet.sh
git -C /home/user/benzene-dotnet-staging remote add origin \
  https://github.com/daniellepelley/benzene-dotnet.git
git -C /home/user/benzene-dotnet-staging push -u origin main
```
(The staging tree at `/home/user/benzene-dotnet-staging` is ephemeral to this container; the script
regenerates it deterministically from benzene HEAD.)

## Phase 2 — Website generator to multi-source — DONE & VERIFIED
The generator now renders a **manifest of doc sources** instead of a single `docs/` tree:
- `DocSource` model; `.NET` docs under `/dotnet/docs/`, spec under `/docs/specification/`.
- Per-source nav (each from its own nav file); links resolved by absolute disk path so cross-source
  links work; self-check spans all sources.
- Cross-language docs hub at `/docs/index.html` (leads with the spec, a card per language); a no-JS
  language switcher atop the docs sidebar.
- The `.NET` source root is `--dotnet-docs <path>` (a benzene-dotnet checkout); with no flag it falls
  back to benzene's own `docs/`, so the generator runs locally before the split lands.
- **Verified:** 92 pages from 2 sources, zero broken links — both from benzene's own `docs/` and from
  the staged benzene-dotnet tree via `--dotnet-docs` (the exact CI multi-checkout path).

## Phase 3 — Flip deploy-website to multi-checkout — DONE (dev deploy not exercised here)
- `deploy-website.yml` checks out benzene-dotnet's `main` and feeds its docs to the generator; the
  checkout is **best-effort**, so until the repo exists the generator falls back to benzene's own
  `docs/` and the site keeps building. `repository_dispatch: dotnet-docs-updated` triggers a rebuild
  on a benzene-dotnet docs push; `notify-website.yml` (in the benzene-dotnet overlay) fires it.
- Old-path **redirects**: the generator emits a stub at each `.NET` page's pre-split path
  (`/docs/*.html` → `/dotnet/docs/*.html`); collisions with the hub / spec pages are skipped.
  **Verified:** 92 pages + 76 redirects, self-check green.
- Not done here: the actual `aws s3 sync` to the dev bucket + the on-`dev.benzene.app` visual check —
  needs the deploy credentials/environment. The generator output is verified locally instead.

## Phase 4 — Cutover (remove migrated content from benzene) — DONE ✅
Removed the .NET port from benzene now that it lives (and passes CI) in benzene-dotnet:
- Deleted `src/ test/ examples/ templates/ benchmarks/ deploy/ tools/`, the two `.sln`s,
  `Directory.Build.props`, `version.txt`, `CHANGELOG.md`, `VERSIONING.md`,
  `DOCUMENTATION_WRITER_SETUP.md`, all .NET `docs/*` (kept `docs/specification/**`), and every .NET
  CI workflow (kept `deploy-website.yml` / `promote-website.yml`).
- Rewrote `README.md` / `AGENTS.md` / `CLAUDE.md` as the cross-language home (spec + website +
  "pick your language" table).
- `deploy-website.yml`: the benzene-dotnet docs checkout is now **required** (no longer best-effort);
  the generator errors clearly if the .NET docs aren't provided (the marketing pages link into them).
- Kept (best-endeavors, cleanup later): `work/`, `.claude/`, `blog/`, governance files.
- **Verified:** the website generator still builds from the post-cutover benzene — 94 pages from 4
  sources, zero broken links (`.NET` via `--dotnet-docs`, Go/TS via `--source`, spec + hub).

## Phase 5 — Polish (real sibling languages, switcher UX) — DONE ✅
All three sibling ports are wired as real language sections, not placeholders. `deploy-website.yml`
checks out **`benzene-go`**, **`benzene-typescript`** and **`benzene-python`** and feeds each to the
generator through its `wire_lang` helper as
`--source <id>::<Label>::<id>/docs::<path>::<repoBlobUrl>` — a full docs section when the port has a
`docs/index.md`, a landing page from its `README.md` otherwise — so a port graduates from landing
page to full section by adding docs, with no workflow change.

**Python was not in this phase's original scope** (it did not exist when the phase was written) and
is wired on the same footing as the other two.

The language switcher atop the docs sidebar carries all of them, and the same list drives the
cross-language hub at `/docs/`.

**Verified 2026-08-20:** a local build with the exact CI flags renders 242 pages (+84 redirects)
from 7 sources — spec + hub + .NET + Go + TypeScript + Python — with the broken-link self-check
green.

---

**Phases 0–5 are complete. This document is a record of how the repositories got their present
shape, not a plan with work left in it.**
