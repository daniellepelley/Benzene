# Benzene.Mesh.Ui

> **2026-07-23 (Fleet view): absent ≠ zero — reduced stats render "—", not "0".**
> `mesh-fleet-ui.html` gains `isAbsent(row, dim)`/`statCell(row, value, dim, class)`: a stat dimension a
> row itself marks genuinely absent (via `missingFeeds`) renders **`—`**, not the non-nullable `0` it
> carries on the wire. This is the UI half of the composite fleet reader (`CompositeMeshFleetReadModel`,
> `work/otel-fleet-adapter-scope.md` inc 3): a backend-composed fleet (X-Ray traces + CloudWatch usage)
> supplies **topic** counts but not **per-service** counts (CloudWatch has no service dimension) nor a
> **duration** (CloudWatch has none) — so service rows mark `stats` and topic rows mark `duration`, and
> those cells show `—` instead of a fabricated `0` that reads as "observed none". Also: the top
> **Invocations/Errors tiles now sum over topics, not services** — topic counts are the per-message truth
> on both planes (they match the service sums on the push collector, which counts the same events), and on
> the composite plane services carry no counts while topics do, so the old service-sum would have shown
> `0` beside a populated topic table (the "numbers don't add up" trap again). Collector-plane rows are
> unaffected (their `missingFeeds` never name a stat dimension — every dimension is observed there).
>
> **2026-07-23 (Fleet view): "Look up a trace by ID" box.** `mesh-fleet-ui.html` gains a direct
> trace-id lookup box (above "Recent flows"), the sibling of the correlation lookup: paste a trace id
> → POSTs `mesh:query:trace` through the same `ENVELOPE_URL` → renders that flow's waterfall via the
> **existing** `buildWaterfall(view)`. It surfaces the trace waterfall as its own window rather than
> only via clicking one of the last ~20 recent flows, so a trace still in the collector's ring but off
> the recent list is reachable. `not-found` → an honest "aged out of the ring buffer" empty state;
> empty id is a client-side no-op. Reuses the `.corr-box`/`.corr-results` styles and the no-dependency
> floor (vanilla JS, one `fetch`); no new read-model (the collector's `mesh:query:trace`/`TraceView`
> were already built + conformance-tested, just previously reachable only by row-click).
>
> **2026-07-23 (usage panel): "By status" reconciles with "By transport" via a neutral bucket.**
> The usage panel (`buildUsagePanel`) computed "By transport" over all entries but "By status" over
> only real statuses (excluding the `result=<missing>` no-outcome sentinel), so the two rows silently
> disagreed by the `<missing>` count — the "numbers don't add up" an owner reported (91 by transport
> vs 29 by status). Fixed by **relabeling, not hiding**: the raw `<missing>` token is still never
> rendered as a status, but its count is now folded into a neutral **`(no outcome recorded)`** chip
> appended to the "By status" row (with a `title` tooltip explaining it's messages with no recorded
> success/failure outcome and that the fix is backend-side), so the status chips sum to the same total
> as the transport row. When a feed carries *only* `<missing>`/null statuses, no "By status" row is
> shown at all (the missing-`status` data-quality footnote covers it) — so there's never a lone bucket
> and never two disagreeing totals. This supersedes the earlier "just hide `<missing>`" mechanism,
> whose intent (don't show the ugly sentinel) is kept while its bug (dropping the count broke
> cross-row integrity) is fixed. Normative rule in `docs/mesh-usage-feed.md` §3; mirrored in
> `website/demos/mesh/index.html`. Making `<missing>` actually reach zero remains a backend concern
> (the pipeline recording a `MessageResult`); this keeps the panel honest regardless. Showing the real
> wire status (`Accepted`/`Ignored`/…) instead of the `success`/`failure` class is a **separate,
> deferred** metric-vocabulary change (a `benzene.messages.processed` contract change), not this.
>
> **2026-07-23 (Fleet view): "Trace a transaction" — correlation-id lookup + failed-flow pivot.**
> `mesh-fleet-ui.html` gains a **correlation-id lookup box** above "Recent flows": enter a business
> correlation id (from a ticket/log) → POSTs `mesh:query:correlation` through the same `ENVELOPE_URL`
> → renders every matching flow (a correlation id can span multiple traces) as a labelled block via
> the **existing** `buildWaterfall(view)` — no new event-rendering code. `NotFound` → an honest empty
> state that also names the ring-buffer-aging / no-header-set reasons; the box carries a one-line note
> that correlation ids exist only for flows whose entry set the `x-correlation-id` header (the mesh
> never fabricates one). **Failed-flow pivot:** when an expanded waterfall's events carry a
> `correlationId`, the `wf-head` shows it as a "find all flows that carried this correlation id" button
> that drives the same lookup — so an investigator who opened a failed flow reaches every related flow
> in one click ("surface it from a reported failure"). Collector-plane only, by design: the static
> `mesh-ui.html` / AwsMesh artifact plane has no live ring and gets an X-Ray/CloudWatch deep-link
> instead (a separate, still-deferred item). Reuses the no-dependency floor (vanilla JS, one `fetch`).
>
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
>
> **2026-07-22 (P4 of the vision doc's roadmap): usage analytics on all three entity pages.**
> `mesh-ui.html` now fetches `usage.json` (the aggregator's merge of every registered
> `IMeshUsageSource` adapter's report - the full standard is `docs/mesh-usage-feed.md`) via the
> same `resolveUrl()` precedence as the other artifacts. Sections, not a separate dashboard:
> a **Usage column** on the estate's topics table (total observed messages per topic row, `–`
> when unexercised), a **usage panel on the topic page** (total + window + per-source
> attribution, chip rows split by transport and by status), and a **usage section on the service
> page** directly under the functional map (the service's own entries when the feed attributes
> per service; otherwise clearly-labeled fleet-wide counts for the topics it handles). The
> degradation rules are normative: artifact absent → every usage surface hidden ("no feed
> wired"); present with empty entries → the explicit "feed is wired, no traffic observed" state
> (deprecation evidence, not an error); a dimension null across the panel's entries → a
> data-quality footnote inside the panel naming the gap (findable, off the primary screen, fix is
> adapter-side) - counts are never invented and a missing dimension is never guessed.
>
> **2026-07-22 (P5 of the vision doc's roadmap): the value & deprecation view.**
> A new estate section (`#value-section`, `renderValueView()`) - the "defend a deprecation"
> ranking: every domain topic tiered by the evidence available for retiring it, with the evidence
> spelled out on each row (this view argues from data, it never decides). Tiers: **Removed since
> the previous run** (`MeshTopicCatalog.RemovedTopics` - a retirement that just completed, or a
> disappearance to confirm), **Retirement candidates** (no declared consumers, and/or zero
> observed usage while a usage feed is wired), **Verify externally** (`gap` topics - their
> producer is outside this fleet's declarations by definition, so fleet data alone can't defend
> retiring them), **No retirement signal**. Least-used first within a tier. Honesty rule: with no
> usage feed wired the header says "structural evidence only" and disuse is never claimed. Rows
> carry the run-over-run **change badges** (`MeshTopicEntry.Changes`, hover for the description),
> and the topic page renders the same changes as full "what changed" lines above the payload
> panel - the aggregator's catalog-diff drift substance surfaced. Also fixed here: the service
> page's spec links had rotted when the estate cards moved to `meshSpecUiHref` (the removed
> `specUiLink` was still referenced, throwing on every service-page render post-merge) - the
> service page now uses the same mesh-hosted spec / raw / health link set as the cards.
>
> **2026-07-22 (P6 of the vision doc's roadmap): discussion & annotations.**
> Topic and service pages carry a **Discussion** section, built as the "hard constraint" vessel
> ruling: the **read path is static** - `buildDiscussionSection` renders `annotations.json`
> (fetched via the usual `resolveUrl()` precedence, the same artifact store as `manifest.json`) -
> and only the **write path** needs a live endpoint: posting goes through the aggregator host's
> `mesh:annotations:add` over the wire envelope (the same POST shape the Fleet view speaks),
> feature-detected via `?annotations=<envelope-url>` or a `data-annotations-url` attribute on the
> document root. Degradation ladder: no artifact + no endpoint → no trace of the feature (the
> static floor); artifact only → threads render with an explicit "read-only" note; endpoint
> present → composer (name + note, client-side required check, `Created`/`Ok` accepted, the
> response's authoritative thread folded into the local cache so the new note survives
> navigation). Notes render newest-first via `textContent` only (no HTML injection path), with
> `humanAge` timestamps. Identity is self-declared by design - the composer says so in-line, and
> access control is the fronting gateway's job (the RateLimiting boundary ruling).
>
> **2026-07-22 (F1 + F2 of the maintainer-feedback triage): version display + value-view RAG.**
> - **F1 — "unversioned" is implied, not labelled.** The three sites that rendered the literal
>   `unversioned` fallback for a topic with no version (the service-page consumed/produced list
>   `buildServiceTopicList`, the topic-page version header `buildTopicPageVersionSection`, and the
>   value-view row `buildValueRow`) now render **nothing** where the version chip would be - absence
>   of a version *is* the signal, not a noise word competing with real version strings. Display-only:
>   the value view's `usageEntriesForTopic(topic, version || null)` join key is unchanged. The estate
>   topics **table** keeps its neutral `–` cell (a table column's standard "n/a" placeholder, not the
>   `unversioned` label the feedback objected to).
> - **F2 — value & deprecation as RAG.** `renderValueView`'s existing four tiers now carry a scan
>   colour: **red** = *Retirement candidates*, **amber** = *Verify externally*, **green** = *No
>   retirement signal*, and a **distinct muted grey "gone"** = *Removed since the previous run* (a
>   past-tense fact, not a live proposal - deliberately NOT sharing red with Candidates). Pure visual
>   encoding of what P5 already computes: no new tier logic, no new data, the "structural evidence
>   only" honesty header is untouched. **Colour is never the only signal** - each tier header carries
>   a distinct SHAPE glyph (`▲ ◆ ● ○` via `vdTierHeader`/`RAG_GLYPH`, `aria-hidden`) and keeps its
>   text label, and the row edge is an `inset` box-shadow, so the reading survives colour-blindness
>   and forced-colors/monochrome. Palette reuses the health-badge design tokens
>   (`--req`/`--m-put`/`--ok`/`--ink-faint`) - no new colours, verified in light and dark.
>
> **2026-07-22 (F3a of the maintainer-feedback triage, first cut): compose test payload (copy-only,
> toggle-gated static floor).** A fourth entity view (`#compose:<topic>`, `renderComposePage`) joins
> Estate/Topic/Service in the same hash router (`showView` now lists `compose-page`; back returns to
> the launching topic, Escape too). It builds a **raw benzene-message envelope** — `{ topic, headers,
> body }` where `body` is the payload serialized as a string, matching `BenzeneMessageRequest` — from
> the topic version's **inlined** inbound schema (`inboundSchema` = `messageSchema` ?? `requestSchema`;
> the response isn't inbound), entirely in-browser via `exampleFromSchema`/`exampleString` (a
> deterministic example generator honouring `example`/`default`/`enum` then type+format with
> length/range hints — no randomness, mirroring the C# `ExamplePayloadBuilder` intent). The user
> picks a **payload** (the topic's versions) and a **transport** (raw + `supportedTransportsForTopic`,
> the union of consuming services' manifest `transports` + `http` when HTTP mappings exist), edits the
> **headers** and **payload fields** as JSON (per-field parse errors shown inline), and **copies** the
> assembled envelope (`copyText`: `navigator.clipboard` with an `execCommand` fallback). **Nothing is
> sent** — this is the copy half only.
> - **Toggle (decision: URL/attribute feature-detect):** `composeEnabled()` is off unless `?compose`
>   is in the query or `data-compose-enabled` is on `<html>`, so the affordance (the topic-page
>   "Compose test payload" button) and the `#compose:` route are entirely absent in a production
>   deploy that doesn't opt in. A `#compose:` bookmark with the toggle off falls back to the estate.
> - **Architecture ruling honoured (do NOT dress transports in the static UI):** the C# SQS/SNS/
>   API-Gateway/Service-Bus envelope builders can't run here, so this ships **vessel #2** — the
>   always-available raw-envelope skeleton. Choosing a non-raw transport shows the raw envelope plus
>   an honest note that the transport-specific wire dressing is served by the host `UseTestPayloads()`
>   endpoint (**a documented follow-up**, not yet wired) — no fabricated dressing, no AWS-only-and-
>   called-done. Code-registered custom payloads (`SuppliedSchemaCatalog`) are likewise a host-fed
>   follow-up; the floor offers the schema-derived default per version.
> - **Follow-ups (not in this cut):** the host `UseTestPayloads()` introspect-and-dress endpoint +
>   the runtime-clean core / `Benzene.*.TestPayloads.Aws` package split (`work/runtime-test-payloads-
>   plan.md`), Azure transport dressing, and the feature-detected fetch of host-dressed payloads.
>
> **2026-07-22 (F3b-revised case 2a: Spec-UI "Try it" deep-link — the §10.7-clean live-HTTP answer).**
> Each service's link row (estate card + service page) gains a **"try it ↗"** deep-link to the
> service's **own** Spec UI (`specUiTryItHref` → the service origin's `/spec-ui`, `UseSpecUi()`'s
> default path, derived from `specUrl`'s origin). The live send is the service's *own same-origin*
> "Try it" (`Benzene.Spec.Ui`) — **the mesh never calls the service itself**, so this needs no §10.7
> exception (§10.7 explicitly blesses live dispatch scoped to a service's own self-hosted Spec UI).
> Gated behind the **same compose toggle** as the payload composer (`composeEnabled()` — a live-
> testing affordance, off in a production deploy by default) and shown only for HTTP-reachable
> services (`svcIsHttpReachable`: `transports` includes `http`, or an older manifest with no transport
> info, best-effort). `safeHttpUrl`-validated. It requires the target service to host its own Spec UI
> (optional today; recommended as part of the service standard — a docs follow-up). Queue/stream
> transports stay F3a compose+copy only; the Lambda direct-invoke (browser-can't-`Invoke`) host-proxy
> path is the separate, gated F3b case (1).

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
- `MeshSpecUiPage` / `MeshSpecUiMiddleware<TContext>` / `UseMeshSpecUi<TContext>(path =
  "/mesh-spec-ui.html", manifestUrl = "manifest.json")` — the **mesh-hosted per-service Spec UI**
  (page: `mesh-spec-ui.html`), the target of `mesh-ui.html`'s per-service *spec* link. It renders a
  single service's Benzene spec — the same Swagger-style view as `Benzene.Spec.Ui`'s `spec-ui.html` —
  but reads the spec the aggregator already captured into the **same-origin** `services/{name}.json`
  snapshot (`MeshServiceSnapshot.specJson`), unwrapping it client-side. So a mesh service only ever
  serves its spec as **JSON** (the Cloud Service contract) — it never has to host any HTML, and there
  is no cross-origin fetch. Opened as `mesh-spec-ui.html?service=<name>&manifest=<url>&mesh=<backUrl>`.
  The default served path ends in `.html` on purpose: that one relative link resolves whether the mesh
  UI is a static file next to the artifacts or served from a pipeline at `/mesh-ui` (the page then
  answers at `/mesh-spec-ui.html`). It has no "try it" (that would be cross-origin to the service) and
  no load dialog — it's a fixed, read-only view of one captured spec, with a "‹ Mesh" back link.

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
  service cards. Each card's link row is: **spec** (opens the mesh-hosted `mesh-spec-ui.html` for
  that service — the mesh renders the spec itself, so the service needs no UI of its own), **raw**
  (the service's raw `specUrl` JSON — the Cloud Service contract), **health** (`healthUrl`), and a
  **topics** button; plus — when the manifest entry's `transports` is non-empty — a `.svc-transports`
  chip row of every transport that service is wired to receive messages over. (The old "spec ui" link
  that *derived* a `/spec-ui` URL on the service's own host was removed: it wrongly assumed the
  service hosts HTML, which the Cloud Service contract does not require — the mesh hosts the spec UI
  now.) Expanding a card
  lazily fetches that service's `services/{name}.json` (resolved relative to the manifest's own
  URL, via `resolveUrl()`) and renders its health-check detail: per check, its name, `type`, a
  status badge (`ok`/`warning`/`failed` via `checkBadgeClass` - `warning` is a distinct amber tier
  from `failed`'s red, mirroring the `Benzene.HealthChecks.Core` model where a degraded but non-fatal
  signal — contract drift, or a non-critical dependency blip — reports `warning`, not `failed`; note a
  401/403 permission failure is *not* a warning but a persistent `failed`, a deterministic IAM
  misconfiguration), and dependency chips. For any **non-ok** check it also
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
