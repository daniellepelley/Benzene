# Benzene.Mesh.Ui

> **2026-07-16:** this package now ships a second page: `MeshFleetUiPage`/`MeshFleetUiMiddleware`/
> `UseMeshFleetUi(path, envelopeUrl)` - the **Fleet view**, the live counterpart to the
> artifact-driven explorer below. It polls a `Benzene.Mesh.Collector`'s `mesh:query:fleet` topic
> through a wire-envelope endpoint and renders the derived fleet (services with health and
> reduced-feed markers, topic catalog with observed consumers, recent flows). Same embedded-HTML
> pattern (`mesh-fleet-ui.html`, attribute-injected config, no JS framework); see
> `examples/Mesh/run.sh` for it running against live services.
>
> **2026-07-22 (P2 of the vision doc's roadmap): the Fleet view now has the flow view + staleness.**
> - **Flow view (traced waterfall):** each "Recent flows" row is clickable (button + Enter/Space on
>   the row) and expands an inline waterfall of the flow's events, fetched from the collector's
>   `mesh:query:trace` through the same envelope endpoint. One row per handled message: service +
>   `topic@version` label, a time-positioned bar (offset = start within the flow, width = duration),
>   colored by the status's **wire-vocabulary success class** (`Ok`/`Created`/`Accepted`/`Updated`/
>   `Deleted`/`Ignored` = success; everything else - and unknown statuses - render as failure,
>   matching the collector's own error counting), with parentage shown by indenting children under
>   their parent span (cycle-guarded, capped at depth 8 visually). A trace is immutable once
>   captured, so the `TraceView` is cached per trace id (a transient fetch failure is not cached);
>   the open waterfall survives the 2s poll's table rebuild, and an empty `events` answer renders
>   the "aged out of the ring buffer" note. Self-contained CSS bars - no chart/graph library.
> - **Staleness (the roadmap's 2026-07-20 ruling, collector-plane half):** a new "Last seen" column
>   renders each service's heartbeat age, and a service whose `lastSeen` exceeds `STALE_AFTER_MS`
>   (90s default - a few missed heartbeats; a JS knob, deliberately not a contract value) has its
>   health mark downgraded to "◌ stale" (amber) - an old "healthy" verdict is not a current one.
>
> **2026-07-22 (P3 of the vision doc's roadmap): both pages now render a topology graph.**
> - **`mesh-ui.html`:** a node-link SVG graph above the topology edge table (the table stays -
>   the graph answers "what's the shape", the table answers "sort me by error rate"). Hand-rolled
>   SVG, no graph/layout library: deterministic layered left-to-right layout (longest-path
>   layering, cycle-guarded; nodes sorted by name within a layer - stable across reloads). Nodes
>   are stroked by the manifest's health status (dashed = not in the manifest) and are full
>   members of the three-entity link closure - click/Enter/Space navigates to `#service:<name>`.
>   Edge width tracks √(req/min), red = error rate ≥ 5%, `<title>` tooltips carry exact numbers;
>   cycles arc over the top, layer-skipping edges bow underneath intermediate columns.
> - **`mesh-fleet-ui.html`:** the same graph over **derived** edges - no `topology.json` exists
>   on the collector plane, so consumer→provider edges are aggregated client-side from the fleet
>   topic catalog's providers/consumers (invocations/errors summed per pair). Node strokes reuse
>   the fleet health vocabulary including the P2 staleness downgrade; the section hides itself
>   when no edges can be derived. Fleet nodes are tooltip-only (no service page exists on this
>   plane to link to).

## What this package does
Serves a self-contained, catalog-style web viewer for a **Benzene service mesh** - the
`manifest.json`/`services/{name}.json` artifacts produced by `Benzene.Mesh.Aggregator`. It shows
every registered service's health status and contract-drift flag at a glance, with a per-service
drill-down into health check detail.

This package renders the catalog; it does **not** generate it. Generation lives in
`Benzene.Mesh.Aggregator`. It mirrors `Benzene.Spec.Ui`'s exact shape and philosophy, one level up
(catalog-of-services rather than catalog-of-topics).

## Key types
- `MeshUiPage` — transport-agnostic accessor for the viewer HTML.
  - `GetHtml()` — the page as-is (falls back to an embedded sample manifest, or a `?url=` query param).
  - `GetHtml(string manifestUrl)` — injects a `data-manifest-url` onto the document root so the
    page fetches and renders that manifest on load.
- `MeshUiMiddleware<TContext> : IMiddleware<TContext> where TContext : IHttpContext` — transport-
  agnostic HTTP middleware, same short-circuit shape as `Benzene.Spec.Ui`'s `SpecUiMiddleware`.
- `MeshUiExtensions.UseMeshUi<TContext>(this IMiddlewarePipelineBuilder<TContext>, path = "/mesh-ui", manifestUrl = "manifest.json")`
  — registers the middleware on any Benzene HTTP pipeline. This is a **secondary convenience**,
  not the primary deployment path (see below).

## Primary deployment target: a static file host, not a Benzene pipeline
Unlike `Benzene.Spec.Ui` (which is served by the exact service whose spec it shows),
`Benzene.Mesh.Aggregator`'s output is typically generated by one process and consumed from
wherever it's published (local disk, blob storage, a CDN) - there's usually no single "the mesh
service" to serve this page from. The realistic deployment is: copy `mesh-ui.html` into the same
directory/bucket the aggregator writes `manifest.json`/`services/*.json` to, and serve all of it
as static files. `MeshUiMiddleware`/`UseMeshUi` exist for the secondary case where you do want to
serve it from a live Benzene app (local demo, or an aggregator host self-serving its dashboard).

## The viewer (`mesh-ui.html`)
- A single self-contained HTML file (inline CSS + vanilla JS, no external requests), embedded as
  a resource (`LogicalName` `Benzene.Mesh.Ui.mesh-ui.html`). Reuses `Benzene.Spec.Ui`'s exact CSS
  design-token block (light/dark theming) for visual consistency across Benzene's UI family.
- Below the stats bar, an **issue inbox** (`#issues-section`, `renderIssues()`) promotes the fleet's
  scattered problem signals into one severity-grouped, actionable worklist — the "what do I need to
  act on now" landing surface. It's a pure client-side reduction over the same static artifacts the
  page already reads (no backend): **Needs attention** = unhealthy/unreachable services + topic
  schema-mismatch; **Warnings** = contract drift; **For review** = topic `deprecation-candidate`/`gap`.
  Each row shows the owning team (when present) and links out — service rows call `goToService`,
  topic rows set the `#topic:` hash — reusing the existing navigation. Reserved (utility) topics are
  excluded. It re-renders from both `render()` (service legs) and `renderTopics()` (topic legs join
  once `topics.json` loads). **Staleness** is derived here client-side (the `mesh-product-owner` ruled
  it a read-time derivation, not a `Stale` status): a service is flagged stale when its
  `manifest.json` `snapshotAtUtc` is older than `STALE_AFTER_MS` (24h default) — a `medium` issue,
  since freshness is orthogonal to health. The "pending data" note only shows for an older manifest
  that carries no `snapshotAtUtc` at all (`freshnessKnown()` false). All-clear renders a check-mark
  empty state.
- Renders a stats bar (total/healthy/unhealthy/unreachable/drift counts) and a searchable list of
  service cards (name, status badge, drift badge, links to the service's raw `specUrl`/
  `healthUrl`, a best-effort "View Spec UI" link derived from `specUrl`, and - when the manifest
  entry's `transports` is non-empty - a `.svc-transports` chip row of every transport that
  service is wired to receive messages over). Expanding a card
  lazily fetches that service's `services/{name}.json` (resolved relative to the manifest's own
  URL, via `resolveUrl()`) and renders its health-check detail: per check, its name, `type`, a
  status badge (`ok`/`warning`/`failed` via `checkBadgeClass` - `warning` is a distinct amber tier
  from `failed`'s red, mirroring the `Benzene.HealthChecks.Core` model where a 401/403 permission
  blip degrades to `warning`, not `failed`), and dependency chips. For any **non-ok** check it also
  renders the check's `data` bag as a key/value **root-cause** block - the "why" behind the
  warning/failure (e.g. `Error`/`ErrorCode`/`StatusCode` from the shared classification policy) - so
  a reader doesn't have to leave the mesh to find out what's wrong. An ok check stays a single clean
  line (no detail needed); a non-ok check whose `data` is empty degrades to a "No further detail
  reported by this check." note (population is per-check, not guaranteed). Data keys are shown
  verbatim - the aggregator camelCases property names but not dictionary keys, and the underlying
  classification policy deliberately reports only non-sensitive discriminators (exception *type*,
  code, status), never the exception message.
  `resolveUrl()` first resolves `manifestUrl` itself against `location.href` before resolving the
  relative path against *that* - `manifestUrl` is very often relative on its own (a bare filename,
  or root-relative like `/artifacts/manifest.json`, the common case for an aggregator host
  self-serving its dashboard), and the `URL()` constructor's `base` argument must already be
  absolute or it throws.
- Loads a manifest from, in precedence order: `?url=` query param → `data-manifest-url` on the
  document root → a relative fetch of `manifest.json` (so the plain embedded page works unmodified
  when copied next to the aggregator's output, with no query param or attribute needed) → embedded
  sample. Theme-aware (light/dark), with a "Load manifest" dialog.
- After every manifest load, also fetches `topics.json` (the aggregator's cross-service topic
  catalog) via the same `resolveUrl()` precedence and renders it as a table (topic, domain-vs-utility
  badge, owning-service chips, HTTP mappings) with a "show utilities" toggle that hides the reserved
  Benzene topics by default. Missing `topics.json` hides the section silently, same as topology.
- The topics section header links the aggregator's composite **`asyncapi.json`** (the fleet's
  merged AsyncAPI 3.0 document): a download link plus, when the resolved artifact URL is absolute,
  an "open in Studio" deep-link to `https://studio.asyncapi.com/?url=…`. Populated in `renderTopics`
  via the same `resolveUrl()` model as the other artifacts.
- **Three-entity exploration model (2026-07-22, P1 of the vision doc's revised roadmap):** the page
  now has three first-class, hash-deep-linkable views — **Estate** (`#main-view`), **Topic**
  (`#topic:<id>`), and **Service** (`#service:<name>`) — mutually exclusive, with `location.hash` as
  the single source of truth (one generic `syncViewFromHash()` router; browser Back/Forward and
  bookmarks work across all three). The **service page** (`renderServicePage`) is maximally
  informative from data already shipped: identity/badges/team/freshness + transports + external
  links (manifest), the **functional map as the centerpiece** — topics consumed/produced with
  version/status/mismatch badges and the service's own HTTP mappings, derived by filtering
  `topics.json` — then About & health (snapshot time, fetch error, drift hash-pair evidence, the
  spec's own `info.description`/`version` parsed best-effort from the verbatim `specJson`, full
  health-check detail via the shared `renderHealthChecks`), and its topology position
  (calls/called-by with rate/error/p50, from `topology.json`). Every section degrades
  independently: no `topics.json` → explicit empty state; no edges → section hidden; unknown
  service name (stale bookmark / out-of-fleet participant) → placeholder page.
  **Full link closure:** estate card names, topic-page producer/consumer rows (now compact linked
  rows — the embedded full cards are gone; the service page is the canonical depth), topology
  table client/server cells, and issue-inbox service rows all navigate to `#service:`; every topic
  id links to `#topic:`; `goToService` now navigates to the service page (the old scroll+flash
  card behavior is retired).
- The per-topic drill-in page (`#topic:<id>`) renders each version's **payload schema** — a "Payload"
  panel showing the Request/Response (or Message) structure with a property tree and validation-rule
  chips (`format`, `enum`, `minLength`/`maxLength`, `minimum`/`maximum`, `pattern`, `nullable`,
  required `*`), the same rendering `Benzene.Spec.Ui` gives per topic. The schema comes inlined from
  `topics.json` (`MeshTopicEntry.RequestSchema`/`ResponseSchema`/`MessageSchema`), so the renderer
  (`renderSchemaTree`) expands nested objects inline rather than resolving `$ref`s. When the
  aggregator flags `SchemaMismatch` (two consumers of the same topic+version declaring different
  payloads — a likely contract error), it's **highlighted**: a red "schema mismatch" badge in the
  topics table's Status column and on the topic-page version header, plus an explanatory banner above
  the payload panel. All of this renders only when the schema/flag is present, so an older
  `topics.json` without them degrades to the previous producers/consumers-only view.
- After every manifest load, also fetches `topology.json` via the same `resolveUrl()` precedence
  (relative to `manifestUrl`) and renders it as a sortable table (client, server, source badge,
  req/min, error rate, p50/p95/p99 latency) below the service list. Any fetch failure - 404,
  network error, malformed JSON - just hides the section silently rather than showing an error,
  since a missing `topology.json` is the expected common case (any deployment that hasn't wired up
  `Benzene.Mesh.Tracing.Tempo` or another topology source).

## When to use this package
- To give a service mesh a browsable catalog dashboard alongside the aggregator's generated JSON.
- Static hosting is the turnkey path - just publish `mesh-ui.html` next to the artifacts. Any HTTP
  transport can also serve it via `UseMeshUi`/`MeshUiPage.GetHtml(...)` directly.

## Dependencies
- `Benzene.Http` (project reference) — for the transport-agnostic HTTP abstractions used by
  `MeshUiMiddleware`/`MeshUiExtensions`. `MeshUiPage` alone has no Benzene dependencies at all.

## Conventions
- Keep the viewer dependency-free and self-contained (no CDN/webfont/script references) so it
  works offline and behind strict CSPs, matching `Benzene.Spec.Ui`'s convention.
- Topology rendering is **both** a node-link SVG graph and the flat sortable edge table beneath
  it - they are two views over the same `topology.json` edges (shape vs. sortable detail), so
  keep them in sync when the edge contract changes. The graph is hand-rolled SVG under the same
  no-dependency floor as everything else here: never introduce a chart/graph/layout library.
