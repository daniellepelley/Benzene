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

### Phase 1 — Stand up `benzene-dotnet` (additive; `benzene` untouched)
- [ ] Create the empty `benzene-dotnet` repo.
- [ ] Copy the "MOVES" set from the manifest as a clean snapshot (working tree, no history rewrite).
- [ ] Vendor the conformance fixtures: `test/conformance-fixtures/*.json` + `SPEC_VERSION`; point
      `ConformanceFixtures.cs` at the vendored copy instead of `docs/specification/conformance/`.
- [ ] Add the drift-check workflow (fetch canonical fixtures from `benzene`, diff, fail on drift).
- [ ] Port the .NET CI workflows; confirm `dotnet build Benzene.sln` + `dotnet test` are green in
      the new repo with **no** dependency on `benzene`.
- [ ] Rewrite `benzene-dotnet`'s `README.md`/`AGENTS.md`/`CLAUDE.md` for a standalone .NET repo.
- [ ] **Do not remove anything from `benzene` yet** — both repos build in parallel during migration.

### Phase 2 — Evolve the website generator to multi-source
- [ ] Replace `SiteBuilder`'s single hardcoded `docs/` root with a **manifest of doc sources**
      (`{ language, label, urlPrefix, checkoutPath }[]`), one entry per language repo + the spec.
- [ ] Nav: keep `docs/index.md` as the cross-language headline/spec nav; build a **per-language nav**
      from each language checkout's own `docs/index.md`, rendered under its `/<lang>/` section.
- [ ] Link rewriting / self-check / demo copy made source-aware (per-source output subtree; broken
      internal link across sources still fails the build).
- [ ] Language **dropdown** + a demo "switcher" so the same demo can be viewed per language.
- [ ] Build entirely from local checkouts first (fixture dirs standing in for the language repos) so
      the generator change is verifiable before wiring real cross-repo checkout.

### Phase 3 — Flip deploy to multi-checkout; verify on dev
- [ ] `deploy-website.yml`: check out `benzene` + each language repo's `main`, run the multi-source
      generator, sync to the **dev** bucket.
- [ ] Verify `dev.benzene.app`: headline + spec render from `benzene`; `/dotnet/*` renders from
      `benzene-dotnet`; language dropdown works; old `/docs/*` paths redirect.
- [ ] Add the cross-repo trigger so a push to `benzene-dotnet`'s `main` rebuilds the site.

### Phase 4 — Remove migrated content from `benzene`; cut over
- [ ] Delete the "MOVES" set from `benzene` (keep only the "STAYS" set per the manifest).
- [ ] Rewrite `benzene`'s `README.md` as the cross-language landing (what Benzene is + the spec +
      "pick your language"), and its `AGENTS.md`/`CLAUDE.md` for a spec-and-website repo.
- [ ] Reduce `docs/index.md` to the cross-language headline + spec + language-section links.
- [ ] Keep the `/docs/*` → `/dotnet/docs/*` redirects in place.
- [ ] Promote dev → live once verified.

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
