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

### Phase 1 — "The inbox watches the system, and knows when it's blind" (all client-side/UI, no wire changes)

| # | Slice | Job | Size |
|---|---|---|---|
| 1.1 | **Errors-in-window issue class**: high-severity inbox rows from `fleet.topics[].errors`/`statusCounts` ("`payments:capture` — `unauthorized` ×12 in the last 24h"); inbox windowed to 24h independent of the fleet picker; **includes reserved topics** (a failing `mesh:aggregate` must be reportable) | 2 | S–M |
| 1.2 | **Unattributed-failing-traffic leg**: failing flows carrying no Benzene topic (the scheduled-rule-400 class) surface as their own inbox row | 2 | S |
| 1.3 | **Feed-health line** on every live surface: "last successful poll Ns ago · last observed event Xm ago"; red on poll failure; "no telemetry has ever arrived — check exporter/OTLP endpoint" when topics are declared but nothing was ever observed; **suppress silent-topic/retirement issues in the blind state** (blindness must never become retirement evidence) | 2 | S–M |
| 1.4 | **"Last error at \<time\>"** per topic (strip + fleet rows) — post-fix verification becomes one glanceable timestamp instead of counter-archaeology | 2/3 | S |
| 1.5 | **Copy/papercut sweep**: plane-correct empty states (no "ring buffer" on the composite plane), "connecting…" never a permanent state, Unhealthy tile counts stale/unknown | 1–3 | S |

### Phase 2 — One front door, one traffic picture

| # | Slice | Job | Size |
|---|---|---|---|
| 2.1 | **Benzene-semantic rendering rule** (D7): collapse/label non-Benzene spans in service lists + waterfalls; codify in the UI CLAUDE.md | 1 | S |
| 2.2 | **Front-door rebuild**: merge `#fleet` landing into the estate; order = needs-you strip → traffic picture → recent flows; catalog/value/edge-table demoted to nav; shared range picker surfaces on the front door | 1+2 | M |
| 2.3 | **Graph live encoding** (un-defer): edge weight = volume, red = error rate, over the shared window | 1 | S–M |
| 2.4 | **Topic → failing-flows pivot**: topic on `TraceSummary` + failed/topic filter on recent flows, linked from every error count (error counts stop being dead-end text) | 2 | M |
| 2.5 | **Provenance absorption pass**: single Traffic column, honesty one layer down, window printed in the column header text (after 2.2 so it lands on the new shape) | 1–3 | S–M |

### Phase 3 — The WHY (pipeline + wire; the mesh finally explains)

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
