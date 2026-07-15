# website

## What this does
Generates Benzene's static marketing + documentation website: a hand-built landing page plus one
page per `docs/*.md` file, for deployment to S3 as plain static files (no server runtime). Not
part of `Benzene.sln` — same precedent as `templates/` and `benchmarks/`, it's a standalone
project with its own build/run flow, documented in `website/README.md`.

## Key types (`generator/`)
- `SiteBuilder` - the orchestrator. Discovers docs pages, builds the nav, rewrites links, renders
  (docs pages + the separately hand-built marketing home), and runs a self-check. See its `Run()`
  method for the full pipeline in order.
- **The marketing home page (`index.html`) is hand-authored**, not derived from README.md's
  markdown - `MarketingContent` holds the copy (hero tagline, feature cards, quickstart snippets,
  platform list, loosely kept in sync with README.md's messaging by hand), `ArchitectureDiagram`
  draws the hexagon-with-six-adapters SVG diagram, and `Layout.RenderMarketingPage` assembles the
  hero/features/code/diagram/platforms sections. A card/hero layout isn't something plain markdown
  can express well, so README.md itself is no longer crawled or rendered as a page at all.
- `Logo` - the brand mark: a hexagon with an inscribed ring, the standard chemistry shorthand for
  an aromatic ring (benzene's own structure) - used identically as the favicon, header icon, and
  hero graphic, via `currentColor` so it themes with light/dark mode.
- **Docs page discovery is not a hardcoded list**: every `*.md` under `docs/` is included, except
  `docs/plans/` (internal roadmap docs) and `docs/DOCUMENTATION_QUICK_REFERENCE.md` (contributor
  cheat-sheet). `docs/specification/conformance/*.json` is naturally excluded too, since only
  `*.md` files are picked up.
- **Navigation comes from `docs/index.md` itself** (`NavTreeBuilder.BuildFromIndexPage`) - it
  parses index.md's own nested bullet list via Markdig's AST (not regex), so index.md stays the
  single source of truth for both docs-home content and the sidebar. Any crawled docs page not
  already reachable from that tree still gets a sidebar entry, appended under a synthesized
  "More" group (`SiteBuilder.AppendOrphanedDocsPages`) - so nothing under `docs/` is ever silently
  unreachable from the site, even if `index.md` hasn't been updated to link it yet.
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
