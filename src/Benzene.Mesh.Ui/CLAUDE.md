# Benzene.Mesh.Ui

> **2026-07-25 COST ROUND 2 — the inbox poll no longer scans a day of traces.** The account hit 85% of
> the X-Ray free tier on `Global-XRay-TracesAccessed` — traces **retrieved/scanned**, i.e. the UI's own
> queries, not the demo's traffic. `GetTraceSummaries` scans every trace in the queried window, so the
> 24h inbox poll was the most expensive call on the page. It now sends the additive wire cost hint
> `includeFlows: false` (`FleetQuery.IncludeFlows`; the composite read model skips the trace source
> entirely, the in-memory ring ignores it — flows were always a legally-empty slice). The inbox reasons
> over counts, which is all it needs; the flow-derived "uninstrumented failing traffic" class and
> `lastFailingFlowAgeMs` read the range-windowed poll instead (`flowFleet`), matching what their wording
> already claimed ("in the current window"). Also fixed here: whichever of `topics.json` / the first
> fleet poll landed **second** must trigger the traffic-map render — at the 15s cadence the map was
> blank for up to a poll (it re-rendered only from the fleet side).

> **2026-07-25 TALLY ROUND — estate-wide filters, and every number reconciles (maintainer + PO ruling).**
> The maintainer's confidence rule: "if the numbers don't add up, the user won't believe the system."
> - **Estate-wide filter bar** (`#estate-filters`, sticky under the topbar, on EVERY view): the ONE
>   time range (`#fl-range`, moved out of the Live traffic section; mounts only with a live endpoint)
>   and the ONE benzene toggle (`#fl-show-utility`, always mounts; relabeled "show benzene topics"),
>   plus a "filters apply estate-wide" scope note. No per-section filter controls remain — the
>   catalog's separate `#topics-show-utils` checkbox is retired (supersedes the earlier separate-planes
>   split); `renderTopicRows` reads the global `flShowUtility` via `setShowUtility`.
> - **The tally rule (normative):** one number = one shared source + one plane token + one window
>   label wherever it appears. `topicTraffic(topicId, version)` → `{count, plane: live|usage,
>   windowLabel}` is THE per-topic traffic source; the estate table's Traffic cell and the topic
>   page's headline both render it (`topicTrafficSpan`) so they cannot diverge by construction. The
>   usage fallback's baked window is stated INLINE on the drill-in ("observed · usage feed, its own
>   window 22 · no live traffic in last 1 hour") — partially supersedes drains-up 2.5's tooltip-only
>   provenance for the drill-in; the estate cell keeps tooltip-one-affordance-deep. The range picker
>   never applies to usage-feed numbers (they can't be re-windowed client-side — a windowable usage
>   feed is filed in `work/service-mesh-roadmap-1.0.md` as the data-layer fix).
> - **Deliberate window/filter exceptions are labeled at both ends:** the inbox keeps its fixed 24h
>   watch and is never benzene-filtered — when the global range ≠ 24h it says "inbox always watches
>   the last 24h — not affected by the time filter" (`#issues-window-note`), and an inbox row on a
>   utility topic carries a "benzene" chip naming the exemption.
> - **"Like they don't exist" sweep:** Services tile now counts health-known services plus names seen
>   in VISIBLE non-infra flows only (benzene-only/backend names never inflate it); the value view's
>   Removed tier skips utility topics; map/tiles/flows/catalog/service-page already filtered. Bypass
>   surfaces stay: explicit topic-page navigation, by-id trace/correlation lookups, pivots.

> **2026-07-25 SERVICE-PAGE FILTER — the benzene filter is ONE global state, applied to the drill-ins.**
> Maintainer report: `#service:benzene-mesh-payments` showed mostly benzene utility traffic (a declared
> `spec` consumed row with "717 obs" beside silent domain topics; a usage panel reading "spec 9.8k ·
> payments:capture 11"). The default view of a service is now its DOMAIN contract:
> - `buildServiceTopicList` hides utility rows (`isUtilityTraffic` OR `reserved`) by default; the
>   header count counts what's shown ("Consumes (2)").
> - `buildServiceUsageSection` excludes utility-topic entries from the panel, so every chip row sums
>   over what's visible (internal consistency), with the excluded volume stated below ("9.9k messages
>   on benzene utility topics excluded"). All-utility usage renders a statement, never a lying
>   "no traffic observed" empty panel.
> - `flShowUtility` is now one global state with `setShowUtility(on)`: each filtered section carries a
>   `utilityHiddenNote(count, noun)` — "N benzene topics hidden · show" (tooltip: benzene's plumbing
>   is assumed working; the issue inbox reports failures regardless) — and toggling anywhere syncs the
>   estate checkbox and re-renders the open view. Topic pages for utility topics remain reachable by
>   explicit navigation (the pivot-bypass rule).

> **2026-07-25 COST ROUND — polling is a cost knob on the composite plane.** Every live poll fans out
> to REAL backend queries (X-Ray `GetTraceSummaries` — billed per trace **scanned** — + CloudWatch
> `GetMetricData` + a traced Lambda invocation), so cadence is money, not just freshness:
> - `FLEET_POLL_MS` 5s → **15s** (still ahead of X-Ray's eventual consistency; range changes and
>   pivots re-poll immediately regardless).
> - `INBOX_POLL_MS` 60s → **5 min** — the inbox's 24h window makes it the widest scan on the page,
>   and a 24h view doesn't need minute freshness (hold-last-good keeps it stable between polls).
> - **Hidden-tab pause:** `loadFleet`/`loadInboxFleet` no-op while `document.hidden`; a
>   `visibilitychange` handler polls both planes immediately on return, so the reader never sees
>   stale data because of the pause. A tab left open in the background now costs zero.
> The standing-traffic half lives in `examples/AwsMesh/deploy/variables.tf`: `aggregate_schedule`
> default `rate(1 minute)` → `rate(15 minutes)` (each pass = mesh Lambda + spec/healthcheck fan-out
> to every service, all traced + metered).

> **2026-07-25 BUG-FIX + UX ROUND SHIPPED (live exploratory pass + mesh-PO review, 10 items + ruling).**
> - **Utility vocabulary completed:** `isUtilityTraffic` now mirrors `ReservedTopics.DefaultIds`
>   (`healthcheck`/`liveness`/`readiness`/`spec`/`test-payloads`/`mesh`/`invoke`/`report`) plus the
>   transport probe topic `ping`, and is also **data-driven** (any `topics.json` row marked
>   `reserved`) — the bare `mesh` descriptor topic and health `ping`s no longer render as user
>   traffic. Utility topics are likewise excluded from the **undeclared-topic inbox class** (no more
>   "Topic observed, not in catalog · healthcheck" noise; failing traffic on them still files).
> - **Successful uninstrumented flows hide by default (PO ruling):** zero Benzene semantics, dead
>   drill-in — hidden with the utility traffic but **counted separately** ("2 benzene + 1
>   uninstrumented flows hidden" — different remedies: backend exclusion after the rename vs "add
>   Benzene middleware"). Failing uninstrumented flows ALWAYS stay visible (1.2 evidence). Expanding
>   a flow whose trace has no Benzene events now answers a **neutral honest note**, not a red error.
> - **Hold-map prune priority:** `flKnownFlows` eviction goes hidden classes → failing-uninstrumented
>   → attributed user flows last (`flHoldRank`), so benzene's ~12 polling flows/min can never churn
>   the user's held flows out of the 60-slot map. Display order stays purely by time.
> - **Crowd-out honesty:** when ≥75% of the backend's answered flows are hidden classes, the note
>   says "N of M flows are benzene's own (hidden)" and the empty state says user flows may be
>   **crowded out of the backend's recent-flows window, not absent** (tiles are authoritative); a
>   pivot-filtered empty list gets its own "no flows match this filter" text.
> - **`<missing>` sentinel never renders raw** (`topicLabel`): inbox rows/issue pages say "(no topic
>   recorded)" with a why-line; the topic live strip folds it into the usage panel's "(no outcome
>   recorded) N" wording. Subject copy buttons are suppressed for prose/sentinel subjects.
> - **Shared stack prefix de-emphasized** (`commonSvcPrefix`/`svcNameSpan`, display-only): when all
>   services share a deployment prefix (`benzene-mesh-*`), it renders muted (`.svc-prefix`) on estate
>   cards, flow Services cells, and traffic-map nodes; full names stay in tooltips/copy/filter/ids.
> - Also: flows **Topic cell** is now a `#topic:` link + copy button (stopPropagation); tile
>   tooltips state the utility exclusion (inbox is never filtered — numbers can't silently disagree);
>   service-strip flow count counts what the destination shows ("3 recent flows (+ 12 benzene)");
>   `flAge` gained a day unit; Started tooltip carries relative age; `humanAge` floors at minutes
>   ("moments"/"5 minutes", no more "0 hours ago"); top-bar button renamed **"Live traffic"**
>   (hash `#fleet` kept for bookmarks); catalog toggle reworded "show benzene topics"; the topics
>   header wraps at narrow widths (was the page's only horizontal overflow at 760px); compose's
>   local `copyBtn` var renamed (shadowed the global factory).

> **2026-07-25 LIVE-FEEDBACK ITERATION SHIPPED — stable flows, Started column, copy buttons, benzene
> traffic hidden by default.** Four maintainer asks from using the deployed estate:
> - **Flow-list stability:** recent flows are held in a rolling client-side map (`flKnownFlows`/
>   `flMergeFlows`, cap `FL_KNOWN_MAX` 60 by newest start, deterministic tie-break) merged on every
>   poll — X-Ray's eventual consistency returns a varying subset per poll and enriched rows land late,
>   which made a per-poll rebuild visibly jump. Once seen, a trace is only **upgraded** in place
>   (never downgraded to a thinner summary); the map resets on a range change (`flApplyRange` — a new
>   window is a new list). Tiles' service buckets read the merged list too.
> - **Started column:** the flows table gains a Started datetime (local wall-clock via `flStamp` —
>   time-only today, day+time older; full ISO in the cell title; pre-2001 floor shared with `flAgeMs`,
>   absent ≠ epoch). Table is now 7 columns (detail `colSpan` follows).
> - **Copy buttons:** `copyBtn(text, label)` (reusing `copyText`) — an inline ⧉ with ✓/✕ feedback,
>   `stopPropagation` load-bearing (they sit inside clickable rows/links). On: flow trace-id cells
>   (copies the FULL id, the cell shows 16 chars), waterfall head trace id + correlation id,
>   topic/service page titles, issue-page subject.
> - **Utility filter:** `isUtilityTraffic()` (= `isReservedTopicId` + interim literal names
>   `healthcheck`/`spec`/`<missing>` until the planned benzene-prefix rename) hides benzene's own
>   plumbing from the traffic picture by default — flows, tiles (Topics/Invocations/Errors), and the
>   traffic map. A "show benzene traffic" checkbox (`#fl-show-utility`, display-only re-render) shows
>   all; the hidden count is stated (`#fl-utility-note`, and the empty state says "N benzene flows
>   hidden", never "nothing observed"). **Pivot bypass:** an explicit pivot to a utility topic (an
>   issue's evidence link) skips the filter. The issue **inbox is deliberately NOT filtered** — a
>   failing `mesh:aggregate` stays reportable (the phase-1 rule). Live finding worth knowing: on the
>   AwsMesh estate the X-Ray recent-flows cap (20) is dominated by the mesh's own 5s polling, so with
>   the filter on few user flows remain visible — the honest note covers it; the real fix is
>   backend-side exclusion, natural after the benzene-prefix rename (deferred task).

> **2026-07-25 DRAINS-UP 3.2 UI MERGE SHIPPED — pipeline-reported issues in the inbox, feed-wins.**
> The inbox now renders `FleetView.issues` (the `mesh:issues` feed, spec §4.1) windowed client-side on
> `lastSeen` ≤ 24h. **Feed wins and suppresses** the client-derived "Failing traffic" row for the same
> topic (one best-available row — the absorption ruling); each field takes its best source: the
> occurrence figure stays the WINDOWED topic error count, while classification (rendered as the kind
> chip), exception type, first/last seen and exemplars come from the feed. The normative
> **fingerprint becomes the stable `#issue:` id** (`issueId()` prefers `i.feed.fingerprint`). The
> detail page enriches accordingly: a "pipeline-reported" provenance chip (tooltip explains the
> moment-of-failure source), `CLASSIFICATION_GUIDE` (per-vocabulary diagnosis), a facts line
> (exception/status/count/first/last), `RESOLUTION_HINTS` (the spec's registered keys: `no-handler`,
> `deserialization`), the status-keyed fix, and the newest **exemplar trace's waterfall inline**
> (`appendTraceWaterfall`, shared with the observed-flow path). Topics without feed coverage keep the
> client-derived row unchanged; no feed at all (the composite plane, `missingFeeds: ["issues"]`) is
> **byte-identical to phase 1** — proven by the smoke harness's pre-existing scenarios passing
> untouched (now 40 assertions incl. the 6 feed-merge cases).

> **2026-07-25 DRAINS-UP 3.3 SHIPPED — the issue detail page (client-derived first cut).** The core
> loop's destination: an inbox row now opens `#issue:<kind|subject>` (`renderIssuePage`, section
> `#issue-page`) — **what the issue means** (`ISSUE_GUIDE`, a per-kind diagnosis catalog shipped in the
> HTML — static-floor safe), **what to do** (bounded likely-cause lists; failing-traffic rows get
> status-keyed guidance from `FAILING_STATUS_GUIDE`: `unauthorized` → IAM/rotated credentials,
> `bad-request` → producer sending a malformed payload, `validation-error` → contract break,
> `not-found` → routing/deploy mismatch, `exception`/`service-unavailable` → see the exception type on
> the example flow, etc.), and **evidence** — deep-link buttons (the failing-flows pivot, the
> topic/service pages) plus, for failing traffic, the **newest observed failing flow's waterfall
> inline** (via `mesh:query:trace` + the shared `flTraceCache`), whose failed leg carries the 3.1
> `exceptionType`. Issue ids resolve against the latest client-side derivation (`lastIssues`), so a
> bookmark to a fixed issue answers **"Issue not currently detected"** — the honest post-fix state.
> Entity pages remain one click away; the drill-ins are evidence, not destinations. Covered by the
> smoke harness (34 assertions: what/what-to-do, status-keyed guidance, inline WHY waterfall,
> evidence pivot round-trip, undetected-bookmark state).

> **2026-07-25 DRAINS-UP PHASE 2 SHIPPED — one front door, one traffic picture, evidence-first.**
> Second phase of `work/mesh-drains-up-review.md` (slices 2.1–2.5):
> - **Front door (2.2):** the separate `#fleet-page` is GONE — the live plane lives on the landing page
>   as `#traffic-section` (between the issue inbox and the catalog), ordered per the three jobs:
>   needs-you strip → live traffic (range picker, feed health, tiles) → traffic map → recent flows →
>   trace/correlation lookups → the catalog. `#fleet` deep-links (old bookmarks, the nav button, the
>   pivots) show the estate scrolled to the traffic section; `isEstateView()` treats `#fleet` as estate;
>   `renderFleetInto()` refreshes the traffic section on every estate poll. Mounts only with a live
>   endpoint (static floor: the section never appears). The **value & deprecation** and declared
>   **topology** sections are demoted: collapsed by default behind `.sec-toggle` show/hide buttons.
> - **Traffic map (2.3):** `flTrafficEdges(fleet)` — observed consumer→provider edges when the plane
>   carries them (push collector), else **declared routes carrying live counts** (topics.json
>   producer→consumer structure joined with the window's per-topic invocations/errors) — which is what
>   makes the map light up on the composite (AwsMesh) plane, where fleet topics have no
>   consumer/provider dimensions. Edge width = √volume, red = ≥5% failing (the existing encoding);
>   `#fl-topo-sub` says which derivation is in play.
> - **D7 rendering rule (2.1):** backend infra names are never first-class. A flow row that mapped no
>   Benzene span (`flIsInfraRow`: `events === 0 && !topic`) gets an "infrastructure" chip on its
>   services cell (`.fl-infra-chip`), and the Services tile excludes names known only from such rows.
> - **Failing-flows pivot (2.4):** recent flows gain a **Topic** column (`TraceSummary.topic`) and a
>   pivot filter (`flFlowFilter`/`flPivotToFailingFlows` + the `#fl-flow-filter` bar with row counts and
>   a clear button). Error counts are never dead ends: the inbox's failing-traffic rows and the topic
>   strip's error count land on that topic's failing flows (evidence-first core loop).
> - **Provenance absorption (2.5):** the estate topics table's two count columns (Usage / Observed) are
>   now ONE **Traffic** column — best-available number per row (live preferred, usage-feed fallback),
>   plane + window one affordance deep (header token + per-cell tooltips). The counts-cumulative badge
>   is a compact marker with the full sentence in its tooltip; the usage panel's `(no outcome recorded)`
>   chip moved into the data-quality footnotes (still reconciling By-status vs By-transport).
> - Playwright smoke harness extended to 28 assertions (front door, pivot round-trips, infra labeling,
>   declared-route traffic map, single Traffic column, static floor) — ad-hoc, not in CI.

> **2026-07-25 DRAINS-UP PHASE 1 SHIPPED — the inbox watches the system, and knows when it's blind.**
> First implementation phase of `work/mesh-drains-up-review.md` (slices 1.1–1.5), closing the review's
> headline defect: a failing system could say "All clear" because every inbox class was catalog paperwork.
> - **Failing-traffic issue class (1.1):** `collectLiveIssues()` now files a high-severity "Failing
>   traffic" row per topic with `errors > 0` — count, total, the failing-status mix
>   (`failingStatusMix`, top 3 non-success tokens across both planes' vocabularies), and "last failing
>   flow Xm ago" when derivable. Derived from a **dedicated 24h inbox window** (`INBOX_WINDOW`/
>   `loadInboxFleet`, 30s cadence — `INBOX_POLL_MS`) independent of the fleet picker, so an overnight
>   failure greets the morning check; falls back to the shared-range view until the first inbox poll
>   lands. Honest on the push plane: when `window.countsWindowed === false` the row says "(cumulative —
>   this plane can't window counts)". **Reserved topics are deliberately included** — a failing
>   `mesh:aggregate` must be reportable (the AwsMesh scheduled-rule 400 was the motivating invisible
>   failure).
> - **Unattributed-failing-traffic class (1.2):** failed flows whose trace carries no Benzene span
>   (`events === 0 && !topic`) file one "Failing traffic without Benzene instrumentation" row — the
>   infra-level / rejected-before-topic-resolved failure class. Gated on the plane attributing spans at
>   all (some flow has `events > 0 || topic`), so a summary-only backend (Tempo) never false-flags.
> - **Feed health (1.3):** `feedHealthState()`/`renderFeedHealth()` — the line that distinguishes
>   "quiet" from "blind". `loadFleet` now records success/failure timestamps and observed activity
>   (`recordFeedActivity`; counts prove traffic even when no flow row is in view). States: poll failing
>   → red "live plane unreachable … the live data shown is stale"; connected-but-nothing-ever-observed
>   while topics are declared (`feedIsBlind()`) → amber "check the exporter / OTLP endpoint wiring";
>   ok → "live · polled Xs ago · last activity Ym ago" (fleet page always; the estate line only mounts
>   when something is wrong). **Blind-state suppression:** silent-but-declared issues are skipped when
>   blind — a broken exporter must never read as retirement evidence. "Connecting to the live mesh…"
>   is no longer a permanent state (`liveConnectingText()` on the fleet status + both live strips).
> - **Last failing flow (1.4):** `lastFailingFlowAgeMs(topicId)` — from the flow lists' new
>   `TraceSummary.topic` field (additive, `Benzene.Mesh.Collector` Views; populated by the store,
>   X-Ray enriched rows, and Jaeger) — rendered on the topic live strip and the failing-traffic rows.
>   Worded "last failing flow" (observed), never "last error": composite flows are sampled/capped.
> - **Copy sweep (1.5):** plane-correct not-found copy on the trace/correlation lookups and the empty
>   waterfall (no more "ring buffer" wording on planes with no ring); the fleet Unhealthy tile now
>   counts stale heartbeats (not unknown — the composite plane is health-unknown by design).
> - Verified by a Playwright smoke harness against the real page + a mock envelope (failing/blind/
>   down/static scenarios, 17 assertions) — ad-hoc, not in CI.

> **2026-07-25 (live-across-surfaces, slice 1): live divergences in the estate issue inbox.** The landing
> page's issue inbox (`renderIssues`/`collectIssues`) now also surfaces live-plane divergences via
> `collectLiveIssues()` — the reconciliation through-line from `work/mesh-ui-product-vision.md` (2026-07-25):
> **declared is the spine, observed sits adjacent, the divergence is the product.** Four classes derived from
> the live `FleetView` against the declared catalog: **observed-but-undeclared** consumer (high — a live caller
> no descriptor declares; the estate echo of the topic page's gap callout), **undeclared topic** observed live
> (medium), **heartbeat degraded** (high) and **heartbeat stale** (medium, `FL_STALE_MS`) from the live health
> plane, and **silent-but-declared** (low — a declared domain topic with no observed traffic *in the current
> window*; worded as deprecation evidence, never "unused"). Each renders with a `LIVE` provenance chip
> (`.issue-live`) so an observed divergence is never mistaken for a declared fact. **Honesty state 1 is
> load-bearing:** with no `fleetEndpoint()` (or before the first poll) `collectLiveIssues()` returns `[]` — the
> live layer does not mount, and the inbox reads exactly as on a static-only deploy (Playwright-verified as its
> own case). Topic reconciliation is skipped until `topics.json` loads (else every observed consumer would
> false-flag as undeclared). The estate inbox re-renders on each fleet poll only when on the estate view
> (`isEstateView()`), leaving the rest of the landing page undisturbed by the background poll.
>
> **Slice 2 SHIPPED 2026-07-25 — estate observed column + service-card heartbeat dot.** The provenance
> visual-token vocabulary is now defined once and reused: **declared renders plain; observed renders in the
> `.obs-count` token** (a live dot + `--ok` accent), with `.obs-th-chip`/`.obs-th-window` on the column header
> and `.hb-dot` (health-coloured) on the cards — so a reader never guesses which figure is declared vs observed.
> The estate topics table gains an **Observed** column (`topics-observed-th`) **adjacent to — never merged with**
> the declared `usage.json` Usage column: they're different planes (aggregator snapshot vs live poll), shown
> side by side, never summed; disagreement is signal, not a bug. Per-cell: a live count in the observed token,
> or **`—` when not observed / stats-absent** (absent ≠ zero). The header states the window/plane
> (`fleetRangeLabel()` when `countsWindowed`, else "cumulative"), carrying the 2026-07-24 count-plane honesty
> onto the estate. Service cards gain a live **heartbeat dot** (`liveHealthState`/`applyHeartbeatDot`: healthy/
> degraded/stale; no dot when unknown — absent heartbeat ≠ unhealthy). Honesty state 1: the observed column and
> the dots mount only with `fleetEndpoint()` + a polled fleet — a static-only deploy renders exactly as before
> (Playwright-verified both paths). On the fleet poll the estate refreshes the Observed column via
> `renderTopicRows()` and updates the heartbeat dots **in place** (`refreshServiceCardHeartbeats()`) so an
> expanded card isn't collapsed.
>
> **Slice 3 SHIPPED 2026-07-25 — weave the live data inline on the drill-in pages; retire the appended
> sections.** The Phase-C "Live activity"/"Observed (live)" titled sections are gone; their contents
> redistribute per the reconciliation rule. Each page gets a compact **live strip** (`.live-strip`, in the
> header region) folding the live-only facts — service: heartbeat health / last-seen / instances / observed
> totals / a recent-flows link; topic: observed / errors / avg-ms / status-mix — refreshed in place on the
> poll. Declared rows carry **inline observed markers** (`.obs-marker`, the observed token): the service
> functional-map rows via `topicObservedMarker` (a live count or muted "silent"), the topic's declared
> consumer rows via `consumerObservedMarker` ("observed" vs "silent"). Heartbeat health shows **beside the
> pulled health-check** in `renderServiceAbout` (the two health planes together). The one divergence that
> can't be woven inline — **observed-but-undeclared** consumers, absent from the declared list by definition —
> stays a **loud `.live-gap` callout** on the topic page (the reconciliation pattern's centerpiece). Poll
> refresh (`refreshOpenLiveSection`) re-fills the strip and re-renders the stable containers
> (`sp-topics-section` / `tp-versions`) so the inline markers stay current without rebuilding the page (no
> lost scroll / re-fetched snapshot). Honesty state 1 throughout: every strip builder returns null and every
> marker helper returns null without `fleetEndpoint()` — the drill-in pages render exactly as on a static-only
> deploy (Playwright-verified both paths, service + topic).

> **2026-07-24 (merge, phase D): a time-range picker on the live plane.** The Fleet view gains a Grafana-style
> time-range control (`.fl-range` / `flWireRangePicker`) — presets 5m/15m/1h/6h/24h/7d, All time, and a custom
> absolute from/to (`datetime-local` → ISO). **One shared range** (`fleetRange`, default `now-1h`) drives every
> live surface: it rides the fleet poll body (`fleetQueryBody` folds `window` into `mesh:query:fleet` and
> `mesh:query:correlation` — **not** `mesh:query:trace`, a by-id lookup), so the Fleet landing view AND the
> per-entity live sections it feeds re-window at once (changing it re-polls immediately and re-runs an open
> correlation lookup). "All time" / a custom range with no lower bound sends **no** `window`, reproducing the
> pre-picker unfiltered behavior. The picker only exists on the live plane (inside `#fleet-page`, which only
> shows with an envelope endpoint) — the static catalog is untouched. **Honesty:** when the response's
> `MeshWindow.countsWindowed` is false (`flCountsNote`), the count tiles carry a "counts are cumulative from
> {countsSince}, not filtered to {range} — flows are windowed, counters aren't" badge rather than a blanked
> number: a windowed count that can't honor the window is a real number answering a *different* window, not the
> "—" of a genuinely-absent dimension. Backend contract + the collector-vs-composite plane split is in
> `src/Benzene.Mesh.Collector/CLAUDE.md` (Phase D); per-surface range overrides are a deferred fast-follow.

> **2026-07-24 (merge, phase E): the Fleet plane is folded into `UseMeshUi` — one page, not two.**
> The live Fleet data (health, observed-vs-declared consumers, recent flows, a Fleet landing view) is
> no longer a separate `mesh-fleet-ui.html` page — it's enriched into the catalog `mesh-ui.html` in
> place ("the catalog is the spine, the live data enriches it"; phases A/B/C did the page work). The
> wiring caught up here: `MeshUiPage.GetHtml(string? manifestUrl, string? envelopeUrl)` injects a
> `data-fleet-url` alongside `data-manifest-url` (same `<html>`-attribute mechanism), the page's Fleet
> plane feature-detects on it (`?fleet=`/`data-fleet-url`), and `UseMeshUi(path, manifestUrl,
> envelopeUrl)` grew the optional third parameter that threads it through `MeshUiMiddleware`. With
> `envelopeUrl` null (the default) the page is the static catalog viewer exactly as before — the Fleet
> plane stays dormant, so a plain static-host deploy is unaffected. `UseMeshFleetUi` /
> `MeshFleetUiPage` / `MeshFleetUiMiddleware` / `mesh-fleet-ui.html` are now `[Obsolete]` (phase F
> migrates the remaining example callers and deletes them); the AwsMesh example already wires
> `.UseMeshUi("/mesh-ui", "manifest.json", "/benzene/invoke")` instead of the separate fleet page.
>
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
  - `GetHtml(string? manifestUrl, string? envelopeUrl)` — additionally injects a `data-fleet-url`
    when `envelopeUrl` is set, so the page's live **Fleet plane** feature-detects it and enriches the
    catalog with `mesh:query:*` data polled from that wire-envelope endpoint. `envelopeUrl` null →
    the static catalog viewer, Fleet plane dormant (the folded-in successor to the retired
    `MeshFleetUiPage`).
- `MeshUiMiddleware<TContext> : IMiddleware<TContext> where TContext : IHttpContext` — transport-
  agnostic HTTP middleware, same short-circuit shape as `Benzene.Spec.Ui`'s `SpecUiMiddleware`. Two
  ctors: `(path, manifestUrl, …)` and `(path, manifestUrl, envelopeUrl, …)` (the latter wires the
  live Fleet plane).
- `MeshUiExtensions.UseMeshUi<TContext>(this IMiddlewarePipelineBuilder<TContext>, path = "/mesh-ui", manifestUrl = "manifest.json", envelopeUrl = null)`
  — registers the middleware on any Benzene HTTP pipeline. This is a **secondary convenience**,
  not the primary deployment path (see below). Pass `envelopeUrl` (e.g. `"/benzene/invoke"`) on a
  mesh host that also serves a `Benzene.Mesh.Collector` to fold the live Fleet plane into the catalog.
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
