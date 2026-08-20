> ARCHIVED 2026-08-20: actioned — Phases 1–4 done; this repo carries no src/. Phase 5 remainder extracted to `work/remaining-issues-plan.md`.

# Repo split & multi-language website — plan

**Status:** planning (not yet actioned). This is the tracked, phase-by-phase checklist.
The exact file-level move/stay split lives in the companion **[repo-split-manifest.md](repo-split-manifest.md)** —
review that before Phase 1 creates anything in the new repo.

## Goal

Split Benzene into two repos so ".NET" is just *one* language port among several to come:

- **`benzene`** (this repo) — the cross-language home: the language-neutral **spec definition**
  (`docs/specification/**`) and the **website** that renders every language's docs into per-language
  sections. Headline = what Benzene is + the spec; drill-in = per-language docs; a language dropdown
  shows "the same demo in .NET or TypeScript", extensible as languages are added.
- **`benzene-dotnet`** (new) — the entire .NET port: `src/`, `test/`, `examples/`, `templates/`,
  `benchmarks/`, `deploy/`, `tools/`, the two solutions, and every ".NET how-to" doc. Plain
  `git clone && dotnet test` works with no submodules.

## Locked decisions

1. **New repo name:** `benzene-dotnet`.
2. **Cross-repo mechanics — no submodules in the language repo.**
   - **Website** consumes each language's docs by **CI checkout** of that repo's `main` (a
     multi-checkout in the deploy workflow), *not* submodules. The main `benzene` repo pulling in
     language repos this way is fine; a language repo submoduling `benzene` would be a smell for
     anyone cloning it, so we don't.
   - **Conformance fixtures (option 1):** `benzene-dotnet` carries a **vendored snapshot** of
     `docs/specification/conformance/*.json` (committed under `test/conformance-fixtures/` + a
     `SPEC_VERSION` marker) so `dotnet test` is self-contained. A **CI drift-check** fetches the
     canonical fixtures from `benzene` and fails if they differ — the snapshot can never silently
     rot against the spec.
3. **URL layout:** per-language docs under a `/dotnet/…` prefix; `301`/redirect the old
   `/docs/*.html` paths to their new `/dotnet/docs/*` home so existing links survive.
4. **Freshness:** the site tracks each language repo's `main` live (rebuilt on push via a
   cross-repo trigger), not pinned tags.
5. **Docs taxonomy:** "how to *use* the spec in .NET" is language-specific and moves
   (`docs/spec.md`); the "actual **spec definition**" is cross-language and stays
   (`docs/specification/**`).
6. **History:** `benzene-dotnet` is seeded from a **clean snapshot** (not `git filter-repo`), with
   history authored under the maintainer. On both repos, going forward, commits are the maintainer's
   own reviewed work — no AI co-author/session trailers.

## Phases

### Phase 0 — Decisions & manifest ✅
- [x] Lock decisions 1–6 (above).
- [x] Write this plan.
- [x] Write the file-level move/stay manifest (`repo-split-manifest.md`).
- [x] Resolve the ambiguous items as best-endeavors defaults (duplication OK; a cleanup/review pass
      follows the split — getting every call perfect up front is not a priority).
- [ ] Maintainer's final go-ahead to start Phase 1.

### Phase 1 — Stand up `benzene-dotnet` (additive; `benzene` untouched) ✅ done — `main` @ c60c93f
- [x] Create the empty `benzene-dotnet` repo. (Already existed — maintainer-created.)
- [x] Copy the "MOVES" set from the manifest as a clean snapshot (working tree, no history rewrite).
- [x] Vendor the conformance fixtures: `test/conformance-fixtures/*.json` + `SPEC_VERSION`; point
      the conformance test project at the vendored copy instead of `docs/specification/conformance/`.
- [x] Add the drift-check workflow (fetch canonical fixtures from `benzene`, diff, fail on drift).
- [x] Port the .NET CI workflows; confirmed `dotnet build Benzene.sln` (0 errors) + conformance
      (134/0) green in the new tree with **no** dependency on `benzene`.
- [x] Rewrite `benzene-dotnet`'s `README.md`/`AGENTS.md`/`CLAUDE.md` for a standalone .NET repo.
- [x] **Nothing removed from `benzene`** — both repos build in parallel during migration.

### Phase 2 — Evolve the website generator to multi-source ✅ (done & verified)
- [x] Replace `SiteBuilder`'s single hardcoded `docs/` root with a **manifest of doc sources**
      (`DocSource`: id/label/urlPrefix/docsRoot/navFile/isLanguage), one entry per language + the spec.
- [x] Nav: cross-language docs **hub** at `/docs/index.html` (headline + spec + a card per language);
      **per-language nav** from each source's own nav file, under its `/<lang>/docs/` section.
- [x] Link rewriting (by absolute disk path → cross-source safe) / self-check / demo copy made
      source-aware (broken internal link across sources still fails the build).
- [x] Language **switcher** (no-JS `<details>`) atop the docs sidebar. (Demo per-language switcher
      deferred to Phase 5 — needs a second language's demo variant.)
- [x] Built + verified from local checkouts first: 92 pages / 2 sources / zero broken links, from
      both benzene's own `docs/` and a staged benzene-dotnet tree via `--dotnet-docs`.

### Phase 3 — Flip deploy to multi-checkout; verify on dev ✅ (deploy step not exercised here)
- [x] `deploy-website.yml`: check out `benzene` + benzene-dotnet's `main` (best-effort; falls back to
      benzene's own `docs/` until the repo exists), run the multi-source generator, sync to **dev**.
- [~] Verify `dev.benzene.app`: generator output verified locally (headline + spec + `/dotnet/*` +
      switcher + redirects). The on-dev visual check needs deploy creds — left for you / the next run.
- [x] Cross-repo trigger: `repository_dispatch: dotnet-docs-updated` on benzene + `notify-website.yml`
      shipped in the benzene-dotnet overlay to fire it.
- [x] Old `/docs/*` → `/dotnet/docs/*` redirects emitted by the generator (76 stubs, self-checked).

### Phase 4 — Remove migrated content from `benzene`; cut over ✅ (done; promote pending)
- [x] Delete the "MOVES" set from `benzene` (kept the "STAYS" set per the manifest).
- [x] Rewrite `benzene`'s `README.md` as the cross-language landing, and its `AGENTS.md`/`CLAUDE.md`
      for a spec-and-website repo.
- [x] `docs/index.md` removed (the cross-language docs hub is generated at `/docs/index.html`).
- [x] `/docs/*` → `/dotnet/docs/*` redirects still emitted by the generator.
- [x] Deploy workflow: benzene-dotnet docs checkout now required; generator errors if absent.
- [ ] Promote dev → live once verified on `dev.benzene.app` (needs deploy creds / a live run).

### Phase 5 — Polish
- [ ] TypeScript placeholder language section (proves the multi-language shape end-to-end with a
      second entry, even before a real TS port exists).
- [ ] Language switcher UX pass; per-language "edit this page" links point at the right repo.
- [ ] Document the new cross-repo release/verify loop in both repos' guides.

## Cross-repo couplings to keep honest

| Coupling | Canonical home | Consumer | Guard |
|----------|----------------|----------|-------|
| Conformance fixtures | `benzene` `docs/specification/conformance/*.json` | `benzene-dotnet` vendored `test/conformance-fixtures/` | CI drift-check fails on divergence |
| Docs rendering | each language repo's `docs/**` | `benzene` website generator | CI checkout of `main`; broken-link self-check |
| Spec definition | `benzene` `docs/specification/**` | every language port (porting-guide) | manual — porting guide references canonical spec |

## Commit-attribution cleanup (both repos, going forward)

Per the maintainer's direction, commits on `benzene` and `benzene-dotnet` should read as the
maintainer's own reviewed-and-committed work. Going forward: author as the maintainer and **omit**
AI co-author / session trailers. (Historic rewrite of already-pushed commits is a separate,
explicitly-authorized step if wanted — not folded into this split silently.)
