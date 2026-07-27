# website

## What this does
Generates Benzene's static marketing + documentation website: a hand-built landing page plus the
docs, for deployment to S3 as plain static files (no server runtime). Not part of `Benzene.sln` —
same precedent as `templates/` and `benchmarks/`, it's a standalone project with its own build/run
flow, documented in `website/README.md`.

**Multi-source (post repo-split):** the site is no longer a single `docs/` tree. It stitches
together several **doc sources** (`DocSource`) — each language port's docs plus the cross-language
specification — each with its own docs root on disk and its own output URL prefix. `.NET` docs
render under `/dotnet/docs/…`, the spec under `/docs/specification/…`, and a generated
cross-language hub at `/docs/index.html` leads with the spec and points at each language. A no-JS
language switcher (a `<details>` disclosure) sits atop the docs sidebar. The `.NET` source's docs
root is `--dotnet-docs <path>` (a `benzene-dotnet` checkout); it is **required** — the `.NET` docs
are the reference section and the marketing/front pages link into them, so a run without it errors.
(Before the split's cutover a local run fell back to benzene's own `docs/`; the `.NET` code has since
moved out to `benzene-dotnet`, so that fallback is gone.) See `work/repo-split-plan.md`.

## Key types (`generator/`)
- `SiteBuilder` - the orchestrator. For every `DocSource` it discovers that source's pages (rooted at
  the source's docs dir, output under its URL prefix), builds that source's nav, rewrites links
  (resolved by absolute disk path, so cross-source links work), renders each page under its source's
  nav + a language switcher, writes the cross-language docs hub + the hand-built marketing home, and
  runs a self-check. See its `Run()` method for the full pipeline in order.
- `DocSource` - one documentation source: `Id`/`Label`/`UrlPrefix`/`DocsRootDisk`/`NavFile`/
  `IsLanguage`/excludes. The spec is a source with `IsLanguage=false` (leads the hub, not in the
  switcher); each language port is `IsLanguage=true`. Built in `Program.cs`; extra languages can be
  added with a repeatable `--source id=Label=urlPrefix=path[=navFile]` arg.
- **The marketing page advertises only the languages this run actually built.** `MarketingContent.
  Languages` is the full catalogue (id, label, install line, snippet, docs path); `SiteBuilder`
  passes the ids of the wired `DocSource`s and `Layout.RenderMarketingPage` filters the catalogue to
  those. This matters because the sibling-port checkouts in CI are best-effort: without the filter, a
  port whose repo failed to check out still gets a "get started" tab linking to docs that were never
  generated, and the broken-link self-check then fails the **whole** site build. Adding a language
  means a `MarketingContent.Languages` entry, its `#gs-<id>` rules in `assets/site.css`, and a
  `wire_lang` line in `deploy-website.yml` — the entry stays dormant until the port's docs exist.
- **The marketing home page (`index.html`) is hand-authored**, not derived from README.md's
  markdown - `MarketingContent` holds the copy (hero tagline, feature cards, quickstart snippets,
  platform list, loosely kept in sync with README.md's messaging by hand), `ArchitectureDiagram`
  draws the hexagon-with-six-adapters SVG diagram, and `Layout.RenderMarketingPage` assembles the
  hero/features/code/diagram/platforms sections. A card/hero layout isn't something plain markdown
  can express well, so README.md itself is no longer crawled or rendered as a page at all.
- **Value-themed marketing sub-pages** (`why.html`, `architecture.html`, `operations.html`) are
  also hand-authored: content lives in `MarketingPages` (a small data model of pages → sections →
  cards), rendered by `Layout.RenderValuePage` reusing the home page's `.section`/`.feature-card`
  shell + a `.page-hero`. They broaden the site past a developer-only audience (architects,
  DevOps/SRE, decision-makers) **without** audience-labeled pages - the framing is value/theme, not
  job title. Rationale and audience analysis: `work/website-audience-plan.md`; messaging pillars
  and honesty gates: `work/website-marketing-aims.md`. `SiteBuilder.Run()` writes one file per
  `MarketingPages.All` entry and includes each in the broken-link self-check. Every claim links to
  a real docs page/demo; anything partial or pre-1.0 is said so (retry-only resilience, in-memory
  idempotency, partial trace-propagation, no perf numbers).
- `Logo` - the brand mark: a hexagon with an inscribed ring, the standard chemistry shorthand for
  an aromatic ring (benzene's own structure) - used identically as the favicon, header icon, and
  hero graphic, via `currentColor` so it themes with light/dark mode.
- **Docs page discovery is not a hardcoded list**: within each source, every `*.md` under its docs
  root is included, minus that source's excludes (the `.NET` source excludes `specification/` — the
  spec is its own source — and `plans/` and `DOCUMENTATION_QUICK_REFERENCE.md`). `*.json` fixtures
  are naturally excluded since only `*.md` files are picked up.
- **Navigation is per-source**, each built from that source's own nav file (`NavTreeBuilder.
  BuildFromIndexPage`): `index.md` for a language port and for the spec (the spec's `README.md` is
  a prose overview whose document index is in Markdown tables the nav builder can't read, so
  `docs/specification/index.md` supplies the sidebar). It parses the
  file's nested bullet list via Markdig's AST (not regex), so the source's index stays the single
  source of truth for its own sidebar. Any page in a source not reachable from its nav still gets a
  sidebar entry under a synthesized "More" group (`SiteBuilder.AppendOrphanedPages`) — nothing is
  silently unreachable. Nav hrefs resolve against the **global** disk index, so a nav bullet that
  points cross-source (e.g. the `.NET` index still listing a spec page) resolves correctly.
- **Link rewriting** (`SiteBuilder.RewriteLinks`): every `LinkInline` in every page is resolved
  against the crawled page set and rewritten to a relative output `href`. Only image extensions
  (`SiteBuilder.WebAssetExtensions`) get vendored into the output as static assets - docs link to
  plenty of other real repo files too (SAM templates, `docker-compose.yaml`, test `.cs` files),
  and those are deliberately left as unresolved relative links rather than being copied into the
  published site.
- `RepoPaths` - all path math (resolving a markdown link's href against its containing page,
  computing the relative href between two output files). Repo-relative paths are always
  forward-slash strings throughout, regardless of host OS.
- `Layout` - the shared HTML shell (header/sidebar/footer, one hand-written `assets/site.css`, no
  JS framework, no Node/npm build step) for both the marketing home page and docs pages.
- **Self-check**: after generation, every internal `href` the tool emitted is checked against the
  actual output tree; a broken one is a non-zero exit, not a silent gap.
- **Live UI demos (`website/demos/`)**: `SiteBuilder.CopyDemos()` copies this directory verbatim
  into `<out>/demos/**` - it is pre-built static HTML + JSON, not markdown, so it bypasses the
  docs-page renderer entirely (unlike `WebAssetExtensions`, which only vendors images actually
  linked from a crawled doc page). `ResolveDemoAsset` lets both `docs/index.md`'s nav tree and
  regular markdown links resolve an href like `../demos/mesh/index.html` to a real published path
  without that path existing as a docs page.
  - `website/demos/mesh/` - a copy of `src/Benzene.Mesh.Ui/mesh-ui.html` (as `index.html`) plus
    hand-authored `manifest.json`/`services/*.json`/`topology.json` fixtures, schema-matched to
    `Benzene.Mesh.Contracts` and modeled on `examples/Mesh`'s three-service demo (healthy /
    unhealthy+contract-drift / unreachable, plus the `FakePrometheus`-driven topology edges) - see
    `examples/Mesh/README.md` for what each fixture value is illustrating. The page finds these by
    its own default boot sequence (`./manifest.json`, then `services/{name}.json`/`topology.json`
    resolved relative to that), so no query param or build step is needed.
  - `website/demos/spec/` - a copy of `src/Benzene.Spec.Ui/spec-ui.html` only, no fixture JSON -
    it renders its own built-in embedded sample spec when opened with no `?url=`.
  - **These fixtures do not auto-refresh.** They were hand-authored (no local .NET SDK was
    available to actually run `examples/Mesh/run.sh` and capture its real output) against the
    exact contract types and demo-service source at the time of writing. If `Benzene.Mesh.Contracts`'
    shape or the `examples/Mesh` demo's health checks/topology data change, this directory needs a
    manual refresh to match - it is not verified against them by any test or build step.

## When to use this package
- Never referenced by any Benzene library or example - it only reads `README.md`/`docs/` and
  writes `website/dist/`.

## Dependencies
- **Markdig** - the only NuGet dependency, build-tool-only (not shipped in any published package).

## Important conventions
- Run from the repo root (`dotnet run --project website/generator -- --out website/dist`) -
  `docs/` and `README.md` are located relative to the current working directory.
- `website/dist/` is gitignored; nothing under it is committed. CI regenerates it fresh each run.
- If `docs/index.md`'s nav structure changes, no code here needs to change - it's parsed at
  generation time. If a new doc page should be excluded from the public site the way
  `docs/plans/` is, add it to `SiteBuilder.ExcludedDirPrefixes`/`ExcludedFiles`.
