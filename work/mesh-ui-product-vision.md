# Benzene Mesh UI — Product Vision & Roadmap

> Living doc owned by `mesh-ui-product-owner`. Convention: append dated update
> blocks at the top (oldest→newest) that flag deviations rather than rewriting
> history. Cross-reference `work/service-mesh-roadmap-1.0.md` (owned by
> `mesh-product-owner`) by section number when a UI need depends on the data layer.

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
- **Issue inbox** (outcome 3): promote failing/unreachable/drifting/stale services
  and schema mismatches into a triage list with severity, not scattered badges.
  Data: existing manifest/health/drift + **staleness** (mesh roadmap "Staleness
  representation" — OPEN; file the requirement).

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
