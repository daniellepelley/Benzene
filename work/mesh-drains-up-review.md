# Mesh Drains-Up Review — 2026-07-25

**Trigger (maintainer, verbatim intent):** "mesh-ui and otel/x-ray tracing is a bit of a mess. Start
from first principles: the user doesn't care about the tech issues, they just want to know what
traffic is going across the different services and topics and most importantly if there are any
issues with the system they need to look into, exactly what those issues are and the best way to
resolve them. Add that value and mesh is a winning product."

Synthesis of three parallel reviews: the mesh product owner (product shape), the observability
product owner (data feeds), and the DX champion (an operator-journey walkthrough traced through the
actual `mesh-ui.html` code paths). This document is the working plan; the three source reviews'
substance is folded in here.

---

## 1. The bar: three user jobs, in priority order

1. **Traffic** — what's flowing across my services and topics, now and over a window.
2. **Issues** — is there anything I need to look into? Surfaced *to* me; I shouldn't have to hunt.
3. **Resolution** — exactly what is each issue, and the best way to resolve it.

This is a deliberate reprioritization of `work/mesh-ui-product-vision.md`'s outcome ordering
(which put "understand the domain" first). Comprehension/catalog becomes the supporting cast.
Recorded as a deviation, not silently absorbed.

## 2. Verdict

| Job | State | One-line evidence |
|---|---|---|
| 1. Traffic | **Partial, scattered** | The data exists but is smeared across ~6 surfaces on two landing pages; the one glanceable answer (a flow map with live volume/error on the edges) is exactly the twice-deferred item; numbers wear caveat badges instead of being one best-available figure. |
| 2. Issues | **Frame exists, watching the wrong signals** | Every issue-inbox leg is a catalog/metadata divergence (drift, mismatch, staleness, undeclared consumers). **Not one leg is failing traffic.** A system throwing errors all night says "All clear". The live-tested proof: the `mesh:aggregate` scheduled-rule 400 — a real production-shaped failure — was invisible on every surface (and is a *reserved* topic, excluded from the inbox by design). |
| 3. Resolution | **Essentially unserved** | The mesh reports *that* something failed (a red bar, a status word), never *why* + *what to do* — even though the pipeline knows the exception class, validation failures, and wire status at the moment of failure, and most issue classes have a small statically-knowable remedy set. |

The cross-cutting operator finding: **every honest signal in the product is a noun (counts,
statuses, divergences) and the operator's questions are verbs (what broke, why, since when, is it
fixed).**

## 3. Structural diagnosis

**D1 — Accretion without a front door.** P1–P6, F1–F3, merge phases A–F, live slices 1–3: each
disciplined and shipped, none organized around the user's opening question. Two landing pages
(estate catalog + `#fleet`), eight views, and neither landing leads with "here's your traffic;
here's what needs you."

**D2 — The inbox triages the estate's paperwork, not its behavior.** `collectIssues` /
`collectLiveIssues` have zero error-derived classes. Sharpened by the mesh PO: the missing
*inputs* matter before any lifecycle machinery — adding failing-traffic legs delivers more of job 2
than severity/lifecycle work, and lifecycle needs state (a vessel decision), so it sequences later.

**D3 — No feed in the system is issue-shaped.** Health/heartbeats exist only on the push-collector
plane (the composite X-Ray/CloudWatch plane the reference deployment uses has health = permanently
`unknown`). Error signal = windowed aggregate buckets with no identity, no lifecycle, no
classification, no first/last-seen. `hashMatches` is the one true issue semantic and it's
heartbeat-plane-only.

**D4 — Jobs 2/3 cannot be served by more backend enrichment.** Every recent fix (benzene.service
stamping, ms ordering, annotation reading) made the backend plane *less wrong about transport
facts*; no amount of enrichment makes X-Ray/Tempo carry **failure semantics the pipeline never
emitted**. Generic APM stops at "here's the error span" because it sits outside the pipeline.
Benzene doesn't — end-to-end pipeline ownership is the asymmetric advantage, currently unused.

**D5 — Blindness is indistinguishable from quiet — and becomes false evidence.** Swallowed poll
failures (`loadFleet`'s empty catch), no last-poll/last-event freshness anywhere, "nothing observed
yet" rendered identically for a dead feed and a quiet system. Worse: a broken exporter makes
`collectLiveIssues` file "no traffic observed — evidence toward retiring it" for **every declared
topic** — the UI actively argues a blind estate is unused. A broken metrics export is
indistinguishable from "no traffic".

**D6 — The honesty machinery leaked into the user's face.** Dual declared/observed columns, plane
chips, sentence-length window badges, `(no outcome recorded)` buckets, `—` cells. Each ruling
individually defensible; cumulatively the UI narrates its own data pipeline and outsources
synthesis to the reader. The mesh PO revises their own 2026-07-25 "adjacent everywhere" presentation
ruling: divergence's home is the **issue inbox**; primary surfaces show one best-available number
with provenance one affordance deep.

**D7 — No product-quality bar for backend-mapped data (new, normative).** Infra handler names as
services, out-of-order flows, wrong empty-state copy ("aged out of the ring buffer" on a plane with
no ring) all share one root: raw backend artifacts reached the screen without a mapping rule.
**Rule adopted: the mesh renders the Benzene-semantic view of the estate; backend artifacts
(segments, ADOT handler names, infra spans) never surface as first-class entities unless no Benzene
signal exists — and then explicitly labelled as infrastructure.**

## 4. The architecture ruling (feeds)

> **Backends tell you what moved; the pipeline tells you what's wrong.**

The current architecture asks backends to do both and then apologizes per-caveat. Ruling (hybrid):

- **Backend-read (unchanged):** traffic counts (usage feed: metrics → CloudWatch/App Insights —
  unsampled, correct), flows/topology (trace sources), and drill-in **evidence** (waterfalls,
  correlation). The counts-from-metrics / flows-from-traces / never-counts-from-sampled-traces
  split stays.
- **Mesh-native (new):** the **issue feed** — emitted by the pipeline itself, which uniquely knows
  topic, service, wire status, handler, exception class, validation errors at the moment of
  failure. Plus health/heartbeats and descriptors (already mesh-native).

No new runtime tier: the issue feed rides the same normative sender rule as `mesh:traces`
(async, non-blocking, lossy, never harms the invocation — spec §4), landing on the collector's
ingest plane or as an artifact next to `usage.json` on the aggregator plane. Sparse by
construction: emitted on failure only, fingerprint-deduped at source (count + last-seen updates,
not per-occurrence) — immune to sampling bias because dedup happens where events are complete.
Absent feed degrades to today exactly (`MissingFeeds += "issues"`).

### Issue-feed contract (minimum, bloat-guarded)

Per issue: `fingerprint` (stable identity: service, topic@version, classification, exception type
or status — never message text/ids/timestamps), `classification` (**closed** vocabulary:
`exception` / `validation` / `config-wiring` / `dependency` / `contract-drift`), `service`,
`topic`, `version`, `transport`, `status` (wire vocabulary verbatim), `exceptionType` (CLR type
name, **type not message** — privacy + fingerprint stability), `count`, `firstSeen`, `lastSeen`,
`exemplarTraceIds` (≤3 — the bridge to the evidence plane), `resolutionHint` (a **key into a
bounded catalog**, not free text — the pipeline states what it knows; the catalog owns the prose).

Explicitly rejected: stack traces (trace plane's job), payloads/headers (privacy), per-occurrence
events (volume), free-text remediation (drifts, unlocalizable), severity scoring (derivable
downstream). If a field can be derived downstream or fetched via the exemplar, it stays out.

## 5. The winning shape

**One front door** (the estate landing rebuilt; the separate `#fleet` landing merges in),
top-to-bottom = the three jobs:

1. **"Needs you" strip** — the issue inbox promoted to the top, failing-traffic legs first.
   All-clear is a proud, quiet state — and *trustworthy*, because feed health is asserted, not
   assumed.
2. **The traffic picture** — the topology graph finally carrying live volume (edge weight) and
   error rate (color) over the shared window, plus headline numbers.
3. **Recent flows** — newest first, failures pinned/filterable, real service names, infra spans
   collapsed.

**The core loop:** front door → "3 issues need you" → **issue detail page** (the one genuinely new
surface: what it is, the evidence — affected flows, status mix, example waterfall, schema pair,
health data bag — and "what this usually means / how to fix it") → drill-ins as *evidence*, not
destinations.

**Promoted:** issue inbox; graph-with-live-encoding; waterfall-as-evidence.
**Demoted to secondary navigation:** service/topic catalog browsing, value & deprecation (a
quarterly tool, not a daily one), discussion, compose, the topology edge table.

## 6. STOP list

1. **Stop shipping new estate sections/surfaces** until the front door and issue detail exist.
   The accretion pattern is the disease.
2. **Stop the everywhere-adjacent declared/observed double columns.** One "Traffic" column, best
   available signal; provenance behind a hover/detail affordance; divergence lives in the inbox.
   (Revises the 2026-07-25 presentation ruling; the reconciliation *classes* stay.)
3. **Stop sentence-length honesty badges on primary numbers.** Wire contract
   (`countsWindowed`/`MissingFeeds`) untouched; rendering moves one layer down.
4. **Stop rendering backend infra artifacts as fleet entities** (D7 rule, normative).
5. **Stop `(no outcome recorded)` / `<missing>` chips on primary surfaces** — data-quality
   footnote only.
6. **Don't build yet:** issue lifecycle (seen/resolved), trends/history, notifications — each
   needs state, i.e. an explicit vessel decision (`Benzene.Mesh.Host` or a collector endpoint),
   named when its slice comes. Static floor stays the degradation target.

## 7. Roadmap

Phases ship independently; each slice moves one job. Sizes: S < half-day, M ≈ a day, L = multi-day.

### Phase 1 — "The inbox watches the system, and knows when it's blind" — **SHIPPED 2026-07-25**

> All five slices below shipped in one pass (see `src/Benzene.Mesh.Ui/CLAUDE.md`'s dated block for the
> implementation detail). One deviation from "no wire changes": `TraceSummary.topic` was added (additive,
> null-omitted — the flow's entry topic), pulled forward from Phase 2.4 because per-topic "last failing
> flow" and the unattributed-failure class both need flow→topic attribution; populated by the store,
> X-Ray enriched rows, and Jaeger. Verified by a Playwright smoke harness (failing/blind/down/static
> scenarios) + the .NET mesh & conformance suites.

| # | Slice | Job | Size |
|---|---|---|---|
| 1.1 | **Errors-in-window issue class**: high-severity inbox rows from `fleet.topics[].errors`/`statusCounts` ("`payments:capture` — `unauthorized` ×12 in the last 24h"); inbox windowed to 24h independent of the fleet picker; **includes reserved topics** (a failing `mesh:aggregate` must be reportable) | 2 | S–M |
| 1.2 | **Unattributed-failing-traffic leg**: failing flows carrying no Benzene topic (the scheduled-rule-400 class) surface as their own inbox row | 2 | S |
| 1.3 | **Feed-health line** on every live surface: "last successful poll Ns ago · last observed event Xm ago"; red on poll failure; "no telemetry has ever arrived — check exporter/OTLP endpoint" when topics are declared but nothing was ever observed; **suppress silent-topic/retirement issues in the blind state** (blindness must never become retirement evidence) | 2 | S–M |
| 1.4 | **"Last error at \<time\>"** per topic (strip + fleet rows) — post-fix verification becomes one glanceable timestamp instead of counter-archaeology | 2/3 | S |
| 1.5 | **Copy/papercut sweep**: plane-correct empty states (no "ring buffer" on the composite plane), "connecting…" never a permanent state, Unhealthy tile counts stale/unknown | 1–3 | S |

### Phase 2 — One front door, one traffic picture — **SHIPPED 2026-07-25**

> All five slices shipped (see `src/Benzene.Mesh.Ui/CLAUDE.md`). Notes: the traffic map's composite-plane
> derivation is declared routes (topics.json producer→consumer) carrying the window's live per-topic
> counts — fleet topics on that plane have no consumer/provider dimensions to derive observed edges from;
> the map's subtitle names which derivation is showing. Demotion of value/topology = collapsed-by-default
> disclosures (a full nav overhaul wasn't needed for the front-door ordering). 2.4's `TraceSummary.topic`
> had already shipped with Phase 1.

| # | Slice | Job | Size |
|---|---|---|---|
| 2.1 | **Benzene-semantic rendering rule** (D7): collapse/label non-Benzene spans in service lists + waterfalls; codify in the UI CLAUDE.md | 1 | S |
| 2.2 | **Front-door rebuild**: merge `#fleet` landing into the estate; order = needs-you strip → traffic picture → recent flows; catalog/value/edge-table demoted to nav; shared range picker surfaces on the front door | 1+2 | M |
| 2.3 | **Graph live encoding** (un-defer): edge weight = volume, red = error rate, over the shared window | 1 | S–M |
| 2.4 | **Topic → failing-flows pivot**: topic on `TraceSummary` + failed/topic filter on recent flows, linked from every error count (error counts stop being dead-end text) | 2 | M |
| 2.5 | **Provenance absorption pass**: single Traffic column, honesty one layer down, window printed in the column header text (after 2.2 so it lands on the new shape) | 1–3 | S–M |

### Phase 3 — The WHY (pipeline + wire; the mesh finally explains)

> **LIVE-FIRE VALIDATION 2026-07-25 (against the deployed AwsMesh estate, phases 1–3.3 iteration).**
> Driven with a real browser against the live API (via a local relay; Chromium can't cross the sandbox
> proxy's TLS interception). **Confirmed working on real data:** the 400 Terraform fix (mesh:aggregate
> 360/360 success in 6h; the 2,514 bad-requests are pre-fix history inside the 24h window), real
> service names on flows/topics, `TraceSummary.topic`, windowed counts, the inbox catching the real
> failure window + two genuine schema mismatches + three unhealthy services, the issue page naming the
> actual cause (bad-request → "producer sending a malformed payload — e.g. a scheduled rule"), and the
> pivot answering an honest "0 of 20 — failed only" post-fix. **Four live-fire defects found & fixed:**
> (1) epoch-zero `lastSeen` serialized on composite rows read as "stale for two millennia" and lit the
> Unhealthy tile → `ServiceSummary`/`TopicSummary`/`ServiceView.LastSeen` now nullable/omitted
> (breaking-additive on the read views) + a UI pre-2001 sanity floor; (2) the inbox's 24h view flapped
> under backend throttling (fetch isolation returning success with an empty topics slice) → hold-last-
> good caching (an empty slice is never cached) + shared-range fallback + inbox poll 30s→60s; (3) a
> topic getter's `"<missing>"` sentinel was stamped as a real `benzene.topic` → 2.7k phantom mesh
> flows on a topic named `<missing>` → the decorator now treats the sentinel as unresolved; (4) the
> mesh Lambda's own spans lacked `benzene.service` (no `UseBenzeneCloudService` there) → its flows
> showed as `EventBridgeLambdaHandler` → the AwsMesh mesh Startup now calls `SetApplicationInfo`.
> Remaining live observation: ~half the newest flows are summary-plane fallbacks (in-flight traces
> enriched before X-Ray has their spans) — correctly labelled "infrastructure", noted for Phase 4.

> **LIVE-FEEDBACK ITERATION 2026-07-25 (maintainer, using the deployed estate).** Four asks, all
> shipped UI-side (see `src/Benzene.Mesh.Ui/CLAUDE.md` for mechanics): (1) **"recent routes jump
> around"** — root-caused to X-Ray eventual consistency (each poll answers a varying trace subset;
> enriched rows land late and flip a flow's start precision) × a full table rebuild per 5s poll; fixed
> with a rolling client-side hold-and-upgrade map (never downgrade, prune to 60 newest, reset on range
> change), proven by a churn-mode mock (each poll returns a DIFFERENT single flow — the UI holds both,
> stable order). (2) **Started datetime column** on flows (local wall-clock, ISO in the tooltip,
> absent ≠ epoch). (3) **Copy-to-clipboard buttons** (trace ids full-length, correlation ids,
> topic/service titles, issue subjects — stopPropagation so copying never navigates). (4) **Benzene
> utility traffic hidden by default** (flows/tiles/map; "show benzene traffic" checkbox; hidden count
> always stated; pivot-to-utility-topic bypasses; inbox deliberately unfiltered). Interim name list in
> `isUtilityTraffic` until the **benzene-prefix rename** (user-deferred single task, next). Live run:
> 12/12 assertions incl. the new ones; live finding: X-Ray's 20-row recency cap is ~95% mesh's own 5s
> polling — the filter makes that visible honestly ("19 benzene flows hidden"); backend-side exclusion
> of mesh traces from the recent-flows query is the real fix, natural after the rename.

> **BUG-FIX + UX ROUND 2026-07-25 (live exploratory pass × mesh-PO review).** The PO's ranked 10 all
> shipped (see `src/Benzene.Mesh.Ui/CLAUDE.md` for mechanics): utility vocabulary completed to the
> full `ReservedTopics` list + `ping` + catalog-`reserved` rows (the bare `mesh` topic was rendering
> as user traffic); hold-map prunes hidden classes first (benzene's polling could evict the user's
> held flows in ~5min); crowd-out honesty on the saturated recent-flows window; `<missing>` never
> raw ("(no topic recorded)" / "(no outcome recorded)"); service-strip flow counts match what the
> destination shows; stack-prefix de-emphasis; tile-exclusion tooltips; day unit + relative-age
> tooltips; topic-cell link+copy; "Live traffic" nav rename; topics-header wrap (the 760px overflow).
> **PO ruling recorded — successful uninstrumented flows hide by default:** under D7 these rows carry
> zero Benzene semantics (no topic, no status, no waterfall — a dead click), so hiding is correct
> PROVIDED the hidden count is always stated and names "uninstrumented" distinctly from "benzene" —
> the two populations have different remedies (backend exclusion after the rename vs "instrument this
> service"), and the note+checkbox is itself the right cue for a genuinely-uninstrumented user
> service. Failing uninstrumented flows always stay visible (the 1.2 class carve-out). Same interim
> caveat as the utility filter: superseded by backend exclusion after the rename. Also fixed:
> expanding a no-Benzene-events flow answered a red "not-found" error → now a neutral honest note.
> Live re-verified 13/13 (incl. prefix de-emphasis) + 53 mock assertions; the live 1h window at
> verification time genuinely contained ONLY utility topics (user demo traffic quiet + `<missing>`
> still produced at ~380/h by the pre-fix deployed backend — will stop on next deploy), which
> exercised the crowd-out empty state on real data: tiles 0/0 with the exclusion tooltip, "20 of 20
> flows are benzene's own (hidden)".

> **COST ROUND 2026-07-25 (maintainer: "reduce the backend traffic so the demo doesn't rack up
> cost").** Three levers, no product-shape change: (1) AwsMesh `aggregate_schedule` default
> `rate(1 minute)` → `rate(15 minutes)` — each pass invokes the mesh Lambda plus spec+healthcheck
> calls to all six services, all X-Ray-traced and EMF-metered (~20k invocations + ~35k traces/day
> idle at 1-min); (2) UI poll cadence — fleet 5s→15s, inbox 60s→5min (the inbox's 24h window makes
> every poll a full-day `GetTraceSummaries` scan, X-Ray's per-trace-scanned billing's worst case);
> (3) hidden-tab pause — a backgrounded tab makes ZERO backend queries, with an immediate
> both-planes poll on return (verified: 0 queries over 33s hidden, 2 within 1.5s of return).
> Applies on next `terraform apply` + redeploy; the schedule stays a variable for anyone wanting
> near-live freshness back.

> **SERVICE-PAGE FILTER 2026-07-25 (maintainer: the service page "is showing mostly benzene utility
> traffic… the same benzene utility filter should apply here").** The benzene filter is now ONE
> global state across the whole page: the service page's functional map hides utility/reserved rows
> (header counts what's shown) and its usage panel excludes utility-topic entries — chips sum over
> the visible set, the excluded volume is stated ("9.9k messages on benzene utility topics
> excluded"), and an all-utility feed renders a statement, never a false "no traffic observed".
> Every filtered section carries "N benzene topics hidden · show", which flips the same
> `flShowUtility` the estate checkbox drives (both stay in sync). Rationale recorded per the
> maintainer: benzene traffic is assumed to be working correctly — when it isn't, the (unfiltered)
> issue inbox is what says so. Live-verified on the reported page; 56 mock + 13 live assertions.

> **TALLY ROUND 2026-07-25 (maintainer: "all the numbers need to add up… filters across the whole
> estate, not on individual controls — otherwise the user won't have confidence and it won't add
> value").** Root cause of the reported mismatch: the estate Traffic cell falls back to the usage
> feed (its own ~24h baked window) while the topic page's headline read only the live plane in the
> picked range — "22" beside "not observed". PO rulings implemented in full (sticky estate-wide
> filter bar under the topbar on every view; the catalog's separate toggle retired into the ONE
> global state; the normative tally rule — one shared `topicTraffic()` source + plane token +
> window label traveling with every number; usage-feed numbers never re-windowed, labeled inline on
> drill-ins; the inbox's fixed 24h window and never-filtered exemption stated loudly at both ends;
> the "like they don't exist" sweep incl. the Services tile and the value view's Removed tier).
> Windowable usage feed filed in `work/service-mesh-roadmap-1.0.md` as the data-layer fix.
> Live-verified on the reported case (order:placed: estate 22 = topic-page 22, both planes stated);
> 61 mock + 13 live assertions.

> **3.2 COMPLETE 2026-07-25** — backend (below) plus the UI merge (feed-wins inbox rows with the
> windowed-count/feed-detail field split, fingerprint `#issue:` ids, detail-page enrichment with
> classification guide + registered resolution-hint prose + exemplar waterfall, "pipeline-reported"
> provenance one affordance deep) and the push-plane example wiring (`examples/Mesh`'s `EnvelopeHost`
> adds `UseMeshIssues` inside `UseMeshTrace`). Composite-plane degradation proven byte-identical to
> phase 1. Remaining from the whole review: **Phase 4** (read-side probes + the live X-Ray
> verification harness) and the named deferrals (AwsMesh issue-store vessel; Go collector parity;
> Tempo recent-flows enrichment).
>
> **3.2 BACKEND SHIPPED 2026-07-25 (spec + emitter + collector; UI merge is the remaining slice).**
> Joint PO rulings (observability + mesh, recorded here as amendments to §4's contract):
> (a) **`unclassified` joins the closed vocabulary** — an honest sixth value beats a lying fallback;
> classification is a normative PRECEDENCE table (validation statuses → exception-type-present →
> config-wiring incl. `unauthorized` and the empty-status wiring gap → dependency → `unexpected-error`
> → unclassified), `contract-drift` reserved for catalog/heartbeat-derived issues.
> (b) **Counts are DELTAS on the wire** (occurrences since the previous flush) — the only semantics a
> lossy sender can merge restart-proof, with no instance identity needed.
> (c) **Fingerprint recipe is normative** (first 16 bytes of SHA-256 over
> `service|topic|version|classification|discriminator`, lowercase hex; transport excluded).
> (d) **Empty batches are the liveness assertion** (30s interval) — quiet-wired vs unwired is
> distinguishable; feed absence marks `ServiceSummary.missingFeeds += "issues"` only when the service
> has failing traffic to explain.
> (e) Spec placement: **§4.1 now** (optional topic, DRAFT-spec bake-room; claims-gated
> `mesh-issue-cases.json` fixtures shipped with the .NET implementation; **Go reference parity is a
> named deferral**). Issues survive redeploys; resolution-by-silence; no lifecycle state (STOP-6).
> Bonus gap-close: push-plane `UseMeshTrace` now populates `MeshTraceEvent.ExceptionType` via the new
> scoped `MessageErrorState`.
> **AwsMesh vessel follow-up (named, deferred):** the composite plane has no long-running collector;
> candidate vessels for its issue store — DynamoDB (per-fingerprint atomic adds, no CAS loop),
> `deploy/Mesh`'s `Benzene.Mesh.Host` as a standing collector beside the Lambdas, or S3
> conditional-put CAS (least favored). Until then the plane marks `missingFeeds: ["issues"]` and the
> UI keeps its client-derived rows (byte-identical phase-1 degradation).

> **3.3 SHIPPED 2026-07-25 (client-derived first cut, deliberately ahead of 3.2).** The issue detail
> page works over the client-derived issue classes today — diagnosis/remediation catalog in the HTML,
> composed evidence (inline failing waterfall with 3.1's exception type, pivot/entity deep-links),
> honest "not currently detected" for a resolved bookmark. Built before the wire feed because it's
> assembly over data already flowing; 3.2's feed later enriches it (fingerprints as stable ids,
> first/last-seen, exemplar trace ids, pipeline-classified causes) without changing the page's shape.
>
> **3.1 SHIPPED 2026-07-25.** `benzene.exception.type` on the topic-bearing span (decorator catch for
> propagating exceptions; `ActivityExceptionTag` walk-up for handler-converted ones — the common case),
> `MeshTraceEvent.ExceptionType` as a spec-§3 **optional/additive** field (flagged: additive wire
> change; push-plane `UseMeshTrace` doesn't populate it yet — absence is spec-legal), read by all three
> trace-store mappers, rendered on failed waterfall legs ("service-unavailable ·
> System.Net.Http.HttpRequestException"). Type name only, never message/stack; span-only, never a
> metric tag. 3.2 (issue feed) and 3.3 (issue detail page) remain.

| # | Slice | Job | Size |
|---|---|---|---|
| 3.1 | **`benzene.exception.type` on the error span** (`ActivityMiddlewareDecorator` — today only status + message). Span-only, never a metric tag (cardinality). Failed waterfall rows immediately answer "why" | 3 | S |
| 3.2 | **Mesh-native issue feed** (§4 contract): spec section + pipeline emitter (fingerprint dedup at source, spec-§4 lossy/non-blocking) + collector ingest + aggregator artifact variant + `MissingFeeds` degradation | 2+3 | L |
| 3.3 | **Issue detail page** (`#issue:<fingerprint>`): per-class diagnosis + remediation catalog (prose ships in the HTML — static-floor safe) + composed evidence deep-links (example failing waterfall, schema pair, health data bag, correlation pivot) | 3 | M |

### Phase 4 — The chain diagnoses itself

| # | Slice | Job | Size |
|---|---|---|---|
| 4.1 | **Read-side probes**: traces-without-benzene-tags ("exporter attribute mapping missing"), annotation-vs-metadata landing ("correlation search will return nothing — annotation indexing not configured"), metric-never-existed vs zero-in-window, source fetch failures as named feed-health rows (not silent empty slices) | 2 | M |
| 4.2 | **Live verification harness** for the X-Ray path: scripted seed-and-assert (emit known traffic, assert annotation/metadata landing, tag names, id validity) — converts the standing "shipped-but-unverified" caveats into a repeatable check | — | M |

**Deferred (gated on a vessel decision):** issue lifecycle (seen/resolved), traffic trends,
notifications. **Deferred (known, cosmetic-relative):** Tempo recent-flows enrichment parity.

## 8. Constraints & caveats

- Everything in Phases 1–2 is reorganization + client-side derivation over feeds already flowing
  through `IMeshFleetReadModel`/`IMeshUsageSource`/`IMeshTraceSource` — no spec widening, no wire
  change, no static-floor break.
- Phase 3.2 is the one contract addition; it follows the existing wire conventions and adds **zero
  required service emissions** (the feed is optional, degradation-normative).
- Thresholds (error-rate, staleness, inbox window) are UI knobs like `STALE_AFTER_MS`, never
  contract values.
- Standing honesty caveat: the composite plane is live-verified only through the maintainer's own
  AwsMesh testing; Tempo and Jaeger adapters remain shipped-but-unverified against real backends
  (Phase 4.2 is the retirement path for that asterisk).

## 9. Deviations recorded

1. **Outcome reprioritization**: traffic/issues/resolution over comprehension-first
   (supersedes the vision doc's ordering for roadmap purposes).
2. **Presentation-ruling revision**: adjacent declared/observed dual rendering is no longer the
   default on primary surfaces; divergence's home is the inbox (the reconciliation classes stay).
3. **D7 Benzene-semantic rendering rule** adopted as normative.

> **COST ROUND 2 — 2026-07-25 (maintainer: AWS free-tier alert at 85%, 966,865 / 1,000,000 traces).**
> The alerting dimension was `Global-XRay-TracesAccessed` — traces **retrieved or scanned**, i.e. the
> Mesh UI's own queries, NOT the demo's recorded traffic. `GetTraceSummaries` scans every trace in the
> queried window, so the widest window on the page (the 24h issue inbox) was the dominant consumer.
> Three levers shipped:
> 1. **Emit side:** `trace_sample_rate` (default 0.2) → `OTEL_TRACES_SAMPLER_ARG`, applied with a
>    PARENT-based ratio sampler so a transaction is sampled or dropped whole (never half a flow in the
>    mesh). Cuts both free-tier dimensions at once — fewer traces recorded is also fewer traces for
>    every future scan to walk.
> 2. **Query side:** `FleetQuery.IncludeFlows` (additive wire cost hint, DIM-threaded so no implementer
>    breaks) — the inbox's 24h poll now asks for counts only and the composite plane skips the trace
>    source entirely. Flow-derived issue evidence reads the range-windowed poll (`flowFleet`), which is
>    what its wording already claimed.
> 3. **Lifecycle:** a `destroy-aws-mesh-example.yml` workflow (typed DESTROY confirmation, same remote
>    state as the deploy) that also empties the artifacts bucket and deletes the implicit
>    `/aws/lambda/benzene-mesh-*` log groups Terraform doesn't own — so "stop paying" is one run.
> The AwsMesh README gained a cost section stating both free-tier dimensions and the three knobs; the
> surprising one is that the retrieved dimension grows with **how much you look**, not how much traffic
> exists. Also fixed: the traffic map stayed blank for a poll when `topics.json` landed after the first
> fleet poll (only the fleet side re-rendered) — visible once the cadence went to 15s.
