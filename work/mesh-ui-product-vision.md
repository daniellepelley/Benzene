# Benzene Mesh UI — Product Vision & Roadmap

> Living doc owned by `mesh-product-owner`. Convention: append dated update
> blocks at the top (oldest→newest) that flag deviations rather than rewriting
> history. Cross-reference `work/service-mesh-roadmap-1.0.md` (same owner)
> by section number when a UI need depends on the data layer.

---

> **2026-07-22 (latest) P3 SHIPPED — the topology graph, on both planes:**
> - **Artifact plane (`mesh-ui.html`):** a node-link SVG graph now renders above the existing
>   topology edge table (the table stays — the graph answers "what's the shape of the estate",
>   the table answers "sort me by error rate"). Hand-rolled, self-contained SVG: deterministic
>   layered left-to-right layout (longest-path layering with a cycle guard, nodes sorted by name
>   within a layer — no physics, no randomness, stable across reloads). Nodes carry the
>   manifest's health status on their stroke (healthy/unhealthy/unreachable; dashed for a
>   participant not in the manifest) and **click through to the service page** — the graph is a
>   full member of the three-entity link closure (keyboard: Enter/Space, `role="link"`).
>   Edge width tracks √(req/min), red = error rate ≥ 5%, tooltips carry the exact numbers;
>   backward edges (cycles) arc over the top, and edges that skip intermediate layers bow
>   underneath them so they stay visible when endpoints share a row.
> - **Collector plane (`mesh-fleet-ui.html`):** the same graph, but over **derived** edges — the
>   fleet has no `topology.json`, so consumer→provider edges are aggregated client-side from the
>   topic catalog's providers/consumers lists (invocations/errors summed per pair, topics listed
>   in the tooltip). Node strokes reuse the fleet health vocabulary incl. the P2 staleness
>   downgrade (stale = amber dashed); the section hides itself entirely when no edges can be
>   derived yet. Nodes are informational (tooltip), not clickable — the fleet view has no
>   service page to link to (yet); that's the artifact plane's job today.
> - Both graphs share the no-dependency floor: no chart/graph library, no layout engine, inline
>   CSS classes for theming (light + dark verified).
> - Verified in a real browser (Playwright + Chromium): 29 artifact-plane checks and 21 fleet
>   checks green, zero console errors — node/edge counts, err-edge thresholds (18% flags, 2.4%
>   doesn't), per-status node strokes, graph-node → service-page navigation round trip, and the
>   edge-less service correctly absent from the fleet graph.
> - Remaining roadmap: P4 usage analytics (gated on the C.1 usage-feed standard), P5
>   value/deprecation view, P6 discussion/annotations.
>
> ---
>
> **2026-07-22 (later still) P2 SHIPPED — flow view + fleet staleness:**
> - **Flow view:** the collector's conformance-tested `mesh:query:trace`/`TraceView` is finally
>   surfaced — every "Recent flows" row in `mesh-fleet-ui.html` expands an inline traced
>   waterfall (per-event time-positioned bars, wire-vocabulary success-class coloring, parentage
>   indentation, per-trace caching, poll-rebuild survival, ring-buffer-aged-out empty state).
>   Self-contained CSS, no chart library — the static/no-dependency floor holds on the collector
>   plane too.
> - **Fleet staleness:** the 2026-07-20 ruling's pending collector-plane half is done — "Last
>   seen" column + health mark downgraded to "◌ stale" past a 90s UI knob (a few missed
>   heartbeats), never a contract value.
> - Verified against a stub collector speaking the envelope contract (Playwright + Chromium,
>   light + dark): 12 checks green, zero console errors, including indentation depths, the
>   failed-span coloring, cache single-fetch, and open-waterfall poll survival.
> - P3 (topology graph over collector-derived edges) is next.
>
> ---
>
> **2026-07-22 (later) P1 SHIPPED + usage-feed requirement refined by the owner:**
> - **P1 (three-entity exploration model) is built and verified.** `#service:<name>` page +
>   generic hash router + full link closure, exactly per §B below; the topic page's embedded
>   service cards became compact linked rows; unknown-service deep links degrade to a placeholder
>   page. Verified in a real browser (Playwright + Chromium over the demo fixtures): estate →
>   service → topic → service round trip, browser Back/Forward, direct deep links, Escape,
>   topology-cell links, light + dark — all green, zero console errors. `website/demos/mesh/`
>   refreshed (and gained a hand-authored, contract-shaped `topics.json` so the demo now
>   showcases all three entities).
> - **Requirement C.1 (usage per topic + transport) refined by the owner:** usage reporting is
>   deliberately **not** part of the Cloud Service spec — it is not the service's request/response
>   surface but an **observability concern**: each service emits, per handled message, metrics
>   with a **standard metadata set** (at minimum topic, transport, status). That metadata standard
>   is the load-bearing piece: it's what lets **adapters** (Application Insights, CloudWatch, an
>   OTel collector, …) extract the same usage signal from different backends and feed it to the
>   mesh. Where a backend's data is missing part of the standard (e.g. no transport dimension),
>   the Mesh UI **degrades gracefully** — it shows what it can, and surfaces the data gap as a
>   visible data-quality note (not on the primary screen, but findable) rather than failing or
>   silently pretending. Explicitly: this adds **no new required endpoints** to a service — the
>   Cloud Service Profile's surface (spec/health/…) is untouched. Routed: metadata standard +
>   emission → `observability-product-owner` (with mesh PO co-owning the standard's field set);
>   backend adapters + ingestion → mesh data layer (collector path); UI presentation +
>   degradation rules → here (P4).
>
> ---
>
> **2026-07-22 three-entity exploration model — current-state review + revised roadmap
> (mesh-product-owner):** The owner's direction: three first-class entities — **Estate, Service,
> Topic** — each with its own maximally-informative page, every mention of another entity a
> click-through. This block records what was verified in source, the gap analysis, the data
> requirements filed, and the re-sequenced roadmap. The three-entity model is Phase 1 by owner
> priority; the 2026-07-20 pressure-test's build order (flow view → topology graph) slots in
> behind it, unchanged in substance.
>
> **A. Current state (verified against `src/Benzene.Mesh.Ui/mesh-ui.html`, 1500 lines, and
> `Benzene.Mesh.Contracts` shapes — not assumed):**
> - **Estate page (`#main-view`) exists and is the hub:** stats bar, issue inbox
>   (`renderIssues()`, incl. the shipped `snapshotAtUtc` staleness derivation), searchable
>   service-card list, topics table (filter, utilities toggle, composite AsyncAPI download +
>   Studio deep-link), topology edge table.
> - **Topic page exists and is deep-linkable:** `#topic:<id>` full view swap
>   (`renderTopicPage`), hash is the single source of truth (browser Back/Forward work — roadmap
>   §10.14/§10.15). Per version: payload schema trees + validation chips, schema-mismatch
>   banner/badges, status badges, HTTP mappings, and producers/consumers rendered as **embedded
>   full service cards** (accordion + lazy health detail inline).
> - **There is no Service page.** A "service" today is an estate-page card:
>   `goToService(name)` *clears the hash*, scrolls to the card and flashes it — so navigating
>   to a service from anywhere **loses deep-linkability and leaves the topic context**. The
>   card's expanded body shows health-check detail only. The "topics" button is a search jump
>   (pre-fills the topics filter), not an entity view.
> - **Cross-link audit — what links vs. dead-ends:** topic-table producer/consumer chips →
>   `goToService` (scroll+flash, not a page) ✓; issue-inbox rows → `goToService` / `#topic:` ✓;
>   service card → filtered topics table ✓ (search, not entity). **Dead-ends:** the topology
>   table's Client/Server cells are plain text (verified `sortAndRenderEdges()` — no links at
>   all); topic-page producer/consumer cards navigate nowhere (detail is embedded, not
>   addressable); no way to share/bookmark "look at this service."
>
> **B. Three-entity design (Phase 1 spine).** Extend the proven hash convention:
> `#service:<encodeURIComponent(name)>` alongside `#topic:<id>`, one generic hash router
> replacing the topic-only `syncTopicPageFromHash`/`clearTopicHash` pair; `#main-view`,
> `#topic-page`, and the new `#service-page` mutually exclusive, hash = source of truth, so
> Back/Forward/bookmarks keep working. **Service page content — all from data already
> shipped in the artifacts** (this phase needs zero contract/spec change):
> - *Identity & state* (from `manifest.json` row): name, owning team, status badge, drift
>   badge, transports chips, `snapshotAtUtc` freshness (reuse the inbox's 24h derivation),
>   spec/health/spec-ui external links.
> - *About* (from `services/{name}.json`): `fetchedAtUtc`, last fetch `error`, full
>   health-check detail (checks, dependencies — move the accordion body here), drift evidence
>   (`specHash` vs `previousSpecHash`), and the service's own `info.title`/`info.description`/
>   `info.version` parsed client-side from the verbatim `specJson` (verified:
>   `EventServiceDocument` serializes `OpenApiInfo`; **verify rendering against a real spec
>   payload during build** — presence of a populated `description` is convention, not
>   guaranteed).
> - *Topics consumed / produced* (derived from `topics.json` by filtering
>   `consumers[].service` / `producers[].service`): per row — topic id (**links `#topic:`**),
>   version, payload-schema presence, HTTP mappings, status/mismatch badges. This is the
>   functional map, the page's centerpiece per the merged brief — health detail sits below it,
>   not above.
> - *Position in topology* (from `topology.json`, edges where `client`/`server` == name):
>   "calls" / "called by" lists with the existing rate/latency columns, neighbor names
>   **linking `#service:`**. Degrades to hidden exactly like the estate topology section —
>   per the 2026-07-20 pressure-test this file is Tempo-gated and usually absent, and Tempo
>   metric names remain **unverified against a real backend**.
> - *Link closure* (the rest of Phase 1): topology-table Client/Server cells → `#service:`;
>   topic-page producers/consumers become compact linked rows (status badge + name + team →
>   `#service:`), replacing the embedded full cards — the service page is now the canonical
>   depth, no duplicated accordion state (unknown services keep the "not in this fleet's
>   manifest" non-link placeholder); estate card name → `#service:` (card keeps its accordion
>   as the quick-glance affordance); issue-inbox service rows → `#service:` (making triage
>   links shareable); service page → back to estate. Quality bar unchanged: Playwright
>   light+dark verification, empty states for every absent artifact, no new dependencies,
>   static floor untouched.
>
> **C. Data requirements filed (routed, not assumed):**
> 1. **Usage per topic + per transport** (service page "usage" section, topic page ditto, and
>    the estate value view all want it): **not produced anywhere today**. Requirement stands
>    with `observability-product-owner` (signal production, OTel/collector path) and the mesh
>    data layer (ingestion/aggregation). Phase-1 pages ship without a usage section rather
>    than with a mocked one.
> 2. **Drift substance ("what changed")**: snapshot carries only the hash pair — a service
>    page can prove *that* the contract changed, not *what*. Requirement on the aggregator
>    (mesh data layer, roadmap Phase 4 field-level compatibility; check
>    `Benzene.Schema.OpenApi/Compatibility` first). Aggregator-derived — **no Cloud Service
>    spec widening needed**.
> 3. **Per-topic transport bindings**: the topic page can only show HTTP mappings plus each
>    participant's *service-level* transports (must be labeled as such). Deliberately **not**
>    filing a spec addition — §10.16 already scoped declared per-topic bindings down once
>    (tautness), and the usage feed (req. 1) answers the better question ("over which
>    transports is it *actually* exercised"). Revisit only if req. 1 lands and still leaves
>    the gap.
> 4. **Structural topology edges**: `TopologyEdgeSource.Structural` is defined but produced
>    by nothing (2026-07-20 pressure-test) — the service page's topology section inherits
>    that hole. Pre-existing open item, unchanged; verified consumer edges live on the
>    collector plane.
>
> **D. Revised roadmap (supersedes the sequencing below and the 2026-07-20 build order's
> position, not its content):**
> - **P1 — Three-entity exploration model** (owner priority; static plane; all data shipped;
>   no spec change): `#service:` page + hash router + full link closure per §B.
> - **P2 — Flow view** (traced waterfall over the collector's `mesh:query:trace`/`TraceView`
>   — built and conformance-tested, not yet surfaced; collector plane, self-contained). Also
>   fold in the pending fleet-ui staleness derivation (UI-only follow-up from the roadmap's
>   2026-07-20 staleness ruling).
> - **P3 — Topology graph** (node-link, self-contained SVG; collector-derived edges are the
>   verified source; artifact-plane `topology.json` stays the degraded fallback). Enriches
>   P1's service-page topology section when present.
> - **P4 — Usage analytics** (gated on data req. 1; Tempo names unverified — flag on every
>   estimate). Adds usage sections to all three entity pages, not a separate dashboard.
> - **P5 — Value & deprecation view** (usage + observed consumers + drift substance, data
>   reqs. 1–2): the estate-level "defend a deprecation" ranking.
> - **P6 — Discussion & annotations** (backend + auth; vessel decision per "The hard
>   constraint" section — static explorer keeps working without it).

---

> **2026-07-22 ownership merge:** `mesh-ui-product-owner` has been merged into
> `mesh-product-owner` — one owner now covers the whole mesh product, data
> packages through UI. References to `mesh-ui-product-owner` in older update
> blocks below are historical. The merged role's brief sharpens the product
> mission: the estate review is for users, business people, business analysts,
> and product owners; the functional map (topics consumed/produced, payloads,
> versions) is the most vital part with health present but not the
> centerpiece; usage means how often topics are exercised **and over which
> transports**, fed by OpenTelemetry/collector metrics; and the owner is now
> also guardian of the Cloud Service spec — full coverage of the product's
> needs with a deliberately small, taut surface area.

---

> **2026-07-20 near-term pressure-test (mesh-ui-product-owner):** critical review of the
> three near-term items against verified source. Key findings that change sequencing:
> - **Two data planes, not one.** The static `/mesh-ui` reads aggregator *artifacts*
>   (`manifest`/`topics`/`topology`/`asyncapi.json`); the live `/fleet-ui` polls the
>   *collector* (`mesh:query:*` → `FleetView`/`TraceView`). They have different models and
>   different health vocabularies (`unhealthy`/`unreachable` vs `degraded`/`unknown`). Each
>   near-term feature must pick a plane, and the choice decides its data honesty.
> - **`topology.json` is entirely Tempo-gated.** `TopologyEdgeSource` only has `Tempo`
>   (produced) and `Structural` (defined, produced by *nothing*). No Tempo wired → the file is
>   absent → an artifact-plane graph has zero edges. Tempo edges are also still UNVERIFIED
>   against a real backend. The collector's trace-parentage consumer edges (real, conformance-
>   tested, no Tempo) populate `FleetView`, NOT `topology.json` — so a *verified* graph lives on
>   the collector plane, not the static one.
> - **Issue inbox is the shippable-now item:** 4 of 5 legs (unhealthy, unreachable, drift,
>   schema-mismatch) are already in the static artifacts; pure client-side reduction, no backend,
>   no graph lib. Only **staleness** is missing — there is still no `MeshServiceStatus.Stale`.
> - **Flow view's real data already exists** as the collector's `mesh:query:trace` (`TraceView`),
>   built and conformance-tested but not yet surfaced in the UI; a trace waterfall is self-
>   contained (no graph lib). AsyncAPI `reply`/operations give the *designed* shape only.
> - **Revised build order: Issue inbox → Flow view (traced waterfall, collector plane) →
>   Topology graph (collector-derived edges, self-contained SVG layout).** Full assessment and
>   filed data requirements returned to the launching agent this pass.

---

## Vision
Make the Benzene Mesh UI the place a team **understands, discusses, and improves**
a platform built on Benzene — an industry-leading product for developers *and*
product owners, not a JSON viewer. Success is measured in time-to-understanding
and decisions-made-in-the-UI, not widgets shipped.

## The two audiences
- **Developers** — debug flows, find the failing/slow hop, see a topic's
  contract, understand who they'll break by changing it.
- **Product owners** — understand the domain in business terms, see what's used
  and valuable vs. dormant, defend a deprecation, and steer the roadmap.

The product must serve both without forcing either to think like the other.

## The six outcomes (the backlog is whatever blocks these)
1. **Understand the domain** — services, ownership, the business capability each
   topic represents, how it fits together.
2. **See the message flows** — call/event topology end to end, request→reply and
   pub/sub shape, traceable paths.
3. **Spot the issues** — failing/slow/drifting/stale services & contracts as
   *problems to act on*.
4. **See usage** — hot vs. cold topics/flows, traffic and error trends over time.
5. **Judge value** — what adds value and is used vs. **deprecation candidates**,
   with evidence a PO can defend.
6. **Discuss it** — annotate/comment/thread on a service, topic, flow, or
   incident, so the UI is where the team *decides*.

## Where we are today (verify before quoting; see mesh roadmap)
- **`/mesh-ui`** static catalog explorer: service cards (health + drift), per-topic
  pages (payload schema + validation rules + schema-mismatch highlighting),
  topology **table**, composite AsyncAPI download + Studio deep-link.
- **`/fleet-ui`** live Fleet view over `Benzene.Mesh.Collector` (health +
  reduced-feed markers, observed-consumer catalog, recent flows).
- Both: single self-contained HTML, no CDN, no build, no external requests —
  statically hostable.

Maps to outcomes: (1) partial, (2) partial (table, no graph, no end-to-end path),
(3) partial (health + drift, no issue triage), (4) none, (5) none, (6) none.

## The hard constraint
`Benzene.Mesh.Ui` is self-contained / no-CDN / no-build / statically-hostable, and
that floor is non-negotiable. Outcomes 4–6 (usage history, value analysis,
discussion) need a **backend and state** a static file can't provide. Design rule:
progressive enhancement — the static explorer always works with zero dependencies;
backend-powered capabilities layer on when present and degrade cleanly when not.
Candidate vessels, to be chosen *with* `mesh-product-owner`:
- Enhancement layer in the existing pages that feature-detects a backend endpoint.
- A hosted companion app in `deploy/Mesh/Benzene.Mesh.Host`.
- New collector/aggregator contracts+endpoints for usage history / annotations.

## Roadmap (sequenced by outcome; each item = "question it answers → data it needs")

### Near term — deepen understanding & flows (mostly static, low data risk)
- **Interactive topology graph** (outcome 2): node-link view with health/traffic
  encoding, replacing/augmenting the table. Data: existing `topology.json`.
  (Mesh roadmap: "Topology graph visualization" open item.)
- **End-to-end flow view** (outcome 2): follow a request across services incl.
  request→reply and event fan-out, using the AsyncAPI 3.0 operations+reply model.
  Data: existing composite `asyncapi.json` + topology.
- **Issue inbox** (outcome 3): ✅ **SHIPPED** in `mesh-ui.html` (`renderIssues()`) — a
  severity-grouped, link-out triage list (Needs attention / Warnings / For review) over the static
  artifacts: unhealthy/unreachable + schema-mismatch (high), contract drift (medium),
  deprecation-candidate/gap (low). Reserved topics excluded; verified light+dark via Playwright.
  **Staleness** ✅ now derived: the `mesh-product-owner` ruled (roadmap 2026-07-20) it's a read-time
  UI derivation over a raw timestamp, **not** a `Stale` status. `manifest.json` gained per-row
  `snapshotAtUtc`; the inbox flags a service stale when it's past a 24h freshness window
  (`STALE_AFTER_MS`), and only shows the "pending data" note for an older manifest with no timestamps.
  Verified via Playwright (stale service surfaces, fresh ones don't, no-timestamp manifest still notes
  pending).

### Mid term — usage & value (needs a data layer; drive requirements out)
- **Usage analytics** (outcome 4): per-topic/flow traffic + error trends over
  time. Data requirement → `observability-product-owner` + `mesh-product-owner`
  (usage history persistence; Tempo metric-name convention is UNVERIFIED against a
  real backend — flag on every estimate).
- **Value & deprecation view** (outcome 5): combine usage + consumers + drift into
  a "value vs. deprecation-candidate" ranking a PO can defend. Data: usage history
  + observed consumers + contract compatibility (mesh roadmap Phase 4 field-level
  compatibility — check `Benzene.Schema.OpenApi/Compatibility` first).

### Longer term — collaboration (needs backend + auth; crosses the constraint)
- **Discussion & annotations** (outcome 6): threaded comments/annotations on
  services, topics, flows, incidents. Explicitly backend-backed — decide vessel
  with `mesh-product-owner`; keep static explorer working without it.

## Industry bar (keep current via WebSearch/WebFetch)
Benchmark against Datadog service maps, Grafana/Kibana, Moesif / API-analytics,
AsyncAPI Studio, and Backstage software catalogs. Lead on: contract-aware,
message-flow-native comprehension tied directly to the running Benzene mesh, for a
**mixed developer+PO** audience. Deliberately don't compete on: general-purpose
metrics dashboards or full APM.

## Open questions
- Right vessel for backend-powered features (enhancement layer vs. companion app)?
- Where does usage history live and who produces it (collector vs. external
  metrics store)?
- Deprecation signal: derive from usage alone, or require explicit lifecycle
  metadata on topics?
- Identity/auth model for discussion — out of scope for the static floor, required
  for outcome 6.

---

**Status:** vision established; near-term items map to existing data, mid/long-term
items are gated on data-layer and backend decisions to be driven into the owning POs.
