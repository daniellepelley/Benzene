# Benzene Mesh UI — Product Vision & Roadmap

> Living doc owned by `mesh-product-owner`. Convention: append dated update
> blocks at the top (oldest→newest) that flag deviations rather than rewriting
> history. Cross-reference `work/service-mesh-roadmap-1.0.md` (same owner)
> by section number when a UI need depends on the data layer.

---

> **2026-07-25 DRAINS-UP REVIEW — the three-job reprioritization. See `work/mesh-drains-up-review.md`
> (the working plan; supersedes this doc's outcome ordering for roadmap purposes).** Maintainer ask:
> start from first principles — the user wants (1) what traffic is flowing across services/topics,
> (2) what issues need their attention, (3) exactly what each issue is and how to resolve it.
> Three-review synthesis (mesh PO + observability PO + DX operator walkthrough) concluded: job 2's
> inbox watches the estate's *paperwork* (drift/mismatch/staleness), not its *behavior* (no
> failing-traffic issue class — a failing system can say "All clear"); job 3 is essentially unserved
> (the mesh says *that*, never *why* + *what to do*); blindness is indistinguishable from quiet and
> even becomes false retirement evidence. Architecture ruling: **backends tell you what moved; the
> pipeline tells you what's wrong** — a mesh-native, fingerprint-deduped issue feed (pipeline-emitted,
> spec-§4 lossy/non-blocking, `MissingFeeds`-degradable) serves jobs 2/3; trace backends remain the
> traffic + evidence plane. Deviations recorded there: (a) traffic/issues/resolution over
> comprehension-first; (b) the 2026-07-25 "declared/observed adjacent everywhere" *presentation*
> ruling is revised — divergence's home is the inbox, primary surfaces show one best-available number
> with provenance one affordance deep (the reconciliation classes stay); (c) a normative
> Benzene-semantic rendering rule: backend infra artifacts never surface as first-class fleet
> entities. A STOP list (notably: no new estate surfaces until the front door + issue detail exist)
> and a 4-phase roadmap live in the review doc.

> **2026-07-25 FIXED: flows show real service names on both ends (the `orders-api → ApiGatewayLambdaHandler`
> bug).** A maintainer saw one real service name and one AWS/Lambda infra name in a Fleet flow. Root cause:
> the topic-bearing span didn't carry the emitting service's own name, so the X-Ray mapper fell back to the
> segment name (the ADOT handler name on Lambda). Fixed by a new **`benzene.service`** span attribute
> (mesh-PO + observability-PO approved; sourced from `IApplicationInfo.Name`, fed canonically by
> `UseBenzeneCloudService` → `SetApplicationInfo`), which the X-Ray/Tempo/Jaeger mappers prefer over the
> backend's segment/resource/process name. Data-layer detail in `work/otel-fleet-adapter-scope.md` §6(c).
> **Known residual:** the Fleet **recent-flows** list on the X-Ray/Tempo *summary* planes still shows the
> backend's names (summaries carry no span attributes, and we don't fan out a fetch per row) — the drill-in
> waterfall/correlation show real names, and Jaeger recent-flows (full traces) do too. A documented gap.

> **2026-07-25 VISION: live data alongside declared across every surface — reconciliation as the through-line.**
> Maintainer ask: "that [fleet] data on the mesh-ui and on the service and topic pages to give a live data view
> as well as the current information." Ruling (mesh-product-owner): **declared is the spine; observed sits
> adjacent, never replacing or summed into it; the divergence is the product.** Four reconciliation classes,
> named consistently everywhere: **silent-but-declared** (in catalog, no observed traffic in window →
> deprecation evidence), **observed-but-undeclared** (traffic with no catalog entry → catalog gap),
> **unhealthy** (heartbeat health bad), **stale** (heartbeat past freshness). Two registers: **the estate
> expresses divergence as issues** (bounded, severity-ranked, actionable); **the drill-in expresses it in
> place** (inline markers + the loud gap callout). Honesty three-states (hard acceptance criteria): **(1) no
> endpoint → the live layer does not mount at all** (no empty observed columns, no "—" implying missing data —
> the table/cards render exactly as today; feature-detect `envelopeUrl`); (2) endpoint but nothing observed →
> **"—", never 0** (absent ≠ zero); (3) endpoint + observation → value with the cumulative-vs-windowed plane
> badge (`countsWindowed`/`countsSince`, per the 2026-07-24 data-layer contract in
> `work/service-mesh-roadmap-1.0.md`). Estate live planes (usage.json declared column vs `currentFleet`
> observed) stay **adjacent, labelled by provenance, never blended** — disagreement is signal, not a bug.
> **This deliberately un-defers** the Phase-C "estate topics-table live indicator" deferral. Phase order:
> **Slice 1** — the four classes as live rows in the issue inbox (highest value, reuses `renderIssues()`,
> satisfies the core ask); **Slice 2** — estate topics-table observed column + service-card heartbeat dot
> (badged, absent≠zero, adjacent-not-merged), gated on a one-time provenance visual-token vocabulary;
> **Slice 3** — weave the drill-in pages (inline observed markers, header-fold live-only facts, keep the topic
> gap callout, retire the appended "Live activity"/"Observed (live)" sections). **Explicitly NOT this
> increment:** topology-graph live encoding, full live value-ranking, any plane blending/refiltering, any wire
> or spec change. **Slice 1 SHIPPED 2026-07-25:** `collectLiveIssues()` derives the four classes from the
> live `FleetView` against the declared catalog, merged into the inbox with a `LIVE` provenance chip; the
> static-floor no-endpoint path (no rows mount) is Playwright-verified as its own case. Topic reconciliation
> is skipped until `topics.json` loads (else every observed consumer would false-flag as undeclared).
> **Slice 2 SHIPPED 2026-07-25:** the provenance visual-token vocabulary is defined once (declared plain vs
> the `.obs-count` observed token) and reused; the estate topics table gains an **Observed** column adjacent to
> (never merged with) the declared usage.json column — "—" when unobserved, live count otherwise, header stating
> the window/plane — and the service cards gain a live **heartbeat dot**. Both mount only with a live endpoint
> (static floor renders as before; Playwright-verified).
> **Slice 3 SHIPPED 2026-07-25 — the increment is complete.** The drill-in pages are woven: the Phase-C
> titled live sections are retired in favour of a compact header **live strip** (live-only facts, refreshed on
> poll), **inline observed markers** on the declared functional-map / consumer rows (count or "silent"),
> **heartbeat health beside the pulled health-check**, and the **observed-but-undeclared gap kept as a loud
> callout** (the one divergence that can't be woven inline). Static floor honoured (no strip, no markers
> without an endpoint; Playwright-verified service + topic). All three surfaces — estate, service, topic — now
> show the live truth alongside the declared, reconciliation as the through-line. Deferred as scoped:
> topology-graph live encoding, full live value-ranking.

> **2026-07-24 SHIPPED: the Fleet plane folded into the Mesh UI + a time-range picker (Phases A–F).**
> The standalone `mesh-fleet-ui.html` is gone: the live Fleet plane is now enriched into `mesh-ui.html`
> itself (`UseMeshUi(path, manifestUrl, envelopeUrl)` — the catalog is the spine, the live data merges in
> as a Fleet landing view + per-entity live sections). **Phase D** adds the time-range control the owner
> asked for: Grafana relative grammar (`now-5m`/`now-1h`/`now-7d`), presets 5m/15m/1h/6h/24h/7d + All time
> + custom absolute, default 1h, **one shared range** driving every live surface, applied **server-side**
> on `mesh:query:fleet`/`correlation` (a trace lookup is by id — no window). The honesty ruling held: a
> windowed count that can't honor the window is badged "cumulative from {countsSince}, not filtered to
> {range}", never blanked (that's the `MissingFeeds` "—" channel) and never silently refiltered. Data-layer
> half — the `MeshTimeRange`/`MeshWindow` wire types and the `countsWindowed`/`countsSince` self-description
> across the two planes — is in `work/service-mesh-roadmap-1.0.md` (same date). **Caveat:** the composite
> (X-Ray + CloudWatch) plane's flows honor the picked window, but its counts still cover the usage feed's
> own baked window (the CloudWatch/App-Insights adapters are single-window by design) — threading the picked
> window into `IMeshUsageSource` so composite counts honor it is the documented fast-follow, and none of the
> composite-plane range behavior is verified against a live AWS backend yet (correct by API shape only).
> Deferred: per-surface range overrides (one shared range for now).

> **2026-07-24 SHIPPED: the composite count-windowing fast-follow (the caveat above, closed).** The AWS/Azure
> plane's counts now honor the picked window too — the CloudWatch/App-Insights adapters query their backend over
> the selected range (the picked window threads into `IMeshUsageSource` as a resolved `MeshUsageWindow`), so the
> "cumulative from …" badge disappears on that plane and the tiles track the picker. It stays honest when a
> non-windowable source (the cumulative collector feed) is mixed in — the badge returns, because the union of a
> windowed and a cumulative feed isn't windowed. UI needed no change (the badge already keys on `countsWindowed`).
> Data-layer detail (the `MeshUsageWindow` port change, the returned-window honored check) is in
> `work/service-mesh-roadmap-1.0.md`, same date. **Cost:** a wider range now drives the usage query too —
> negligible on CloudWatch, real on Azure Log Analytics; pair with the idle-poll pause if it bites. Still
> API-shape-verified only, not against a live backend.

> **2026-07-23 SCOPED: Fleet UI backed by an OTel trace store.** Beyond the push-collector, the Fleet
> view can read traces/correlation/recent-flows from Grafana Tempo/X-Ray/Jaeger (scope:
> `work/otel-fleet-adapter-scope.md`). UI honesty items when trace-backed: a "Backed by: <backend> —
> traces only" banner, `Health=unknown`+`MissingFeeds` reduced rows (health/descriptor/stats absent from
> traces), and — the one gap to close — render "—" not "0" for stat fields when `MissingFeeds` has
> `stats` (absent ≠ zero; traces are sampled, counts stay the usage feed).
> **2026-07-23 usage "By status" now itemizes failures (mesh-product-owner).**
> Following the reconcile-the-totals fix, the owner asked for the *failure* breakdown to show the real
> cause, not a single `failure` chip: "a load of `not-found` might be fine; a load of `unauthorized`
> points to a wider problem." Delivered by the data-layer `result`-tag change (see
> `work/service-mesh-roadmap-1.0.md`, same date): successes stay one `success` bucket, failures carry
> their real status. **No UI change was needed** — `buildUsagePanel` already groups by `status` and
> renders each value as a chip, and the just-shipped `(no outcome recorded)` bucket still folds
> `<missing>`, so "By status" now reads e.g. `success 29 · not-found 40 · unauthorized 2 · (no outcome
> recorded) 20` and reconciles with "By transport". Optional future polish (not done): tint failure
> chips distinctly from `success`.
> **2026-07-23 Fleet view: "Trace a transaction" — correlation-id lookup (mesh-product-owner).**
> Ships the owner's "trace a transaction by correlation id" story on the **collector/live plane**
> (`mesh-fleet-ui.html`): a lookup box that resolves a business correlation id to every flow that
> carried it — services, topics, per-leg success/error — via the new `mesh:query:correlation`
> (data-layer block in `work/service-mesh-roadmap-1.0.md`, same date). Deliberately a **box on the
> existing page, not a separate hash-routed screen** (that page has no router; adding one is
> gold-plating for a first cut) — reuses the P2 waterfall renderer per matching trace. The "surface it
> from a reported failure" half is a **failed-flow pivot**: an expanded flow's `correlationId` is a
> one-click "find related flows" action inside the waterfall, right where an investigator already is —
> with **no** `FleetView`/`TraceSummary` widening. Degradation is honest: correlation ids exist only
> for flows whose entry set `x-correlation-id` (the mesh never fabricates one), and an aged-out id
> reuses the ring-buffer note. **Collector-plane only:** the static `mesh-ui.html`/AwsMesh plane has no
> live ring; its X-Ray/CloudWatch correlation deep-link stays a **separate, deferred** item. Shipped
> .NET-side; cross-language conformance pinning is a fast-follow.
> **2026-07-23 topology table now shows real data on attributable edges (mesh-product-owner).**
> Owner reported the main-page topology table read as "entirely empty". It wasn't missing rows — the
> structural edges render — but every metric cell was blank because the aggregator never attributed
> usage onto edges. Fixed data-side (see `work/service-mesh-roadmap-1.0.md` 2026-07-23 block): edges
> now carry a usage-derived req/min + error rate where a topic's traffic attributes to that specific
> edge unambiguously (single-producer rule); percentiles stay blank (no latency in the feed). **UI
> note / follow-up:** a blank metric cell now means one of two things — "no usage feed wired" (whole
> table blank) or "traffic can't be attributed to this specific link" (some rows blank while others
> show numbers). The latter needs an **empty-cell affordance** (e.g. a `title` tooltip: "traffic can't
> be attributed to this link — needs the per-consumer usage dimension") so a blank reads as *designed*,
> not *broken* — currently the cell just shows "–". Coordinate with `dx-champion`. The lever to fill
> the remaining AwsMesh fan-out rows is the per-consumer usage dimension (an adapter follow-up), not a
> UI change.
> **2026-07-22 (latest) FEEDBACK TRIAGE — three maintainer asks turned into requirements
> (mesh-product-owner). No shipping code changed; this is PO triage + written requirements. The
> P1–P6 roadmap remains complete; these are the next backlog items, sized and sequenced.
> **Maintainer answers incorporated 2026-07-22:** F1, F2 (Removed = distinct grey, not red), and
> F3a are APPROVED. The live-dispatch ask was NOT authorized as the queue-injection version that
> reopened §10.7; the maintainer steered it to a narrower direct-to-consumer / Swagger-for-HTTP
> direction captured as **F3b-revised** (explore-and-design, §10.7 NOT reopened, still pending a
> ruling before any build).**
>
> **Raw feedback (verbatim):** (1) "Unversioned should be implied not expressly mentioned." (2)
> "Value and depreciation should have green, amber and red." (3) "Should be able to send in demo
> payloads but this should be feature toggleable, as in users will not want that on production.
> The payloads will be for different payloads, so should be able to choice supported payloads.
> Should be able to build them from topic, headers and payload fields, and construct those into an
> SQS for instance. There will be the ability to define custom payloads in the code somewhere.
> Sending payload might be it's own screen."
>
> ---
>
> **F1 — "Unversioned" is implied, not labelled. SIZE: SMALL. PRIORITY: P7 (do first — trivial,
> pure polish). APPROVED (maintainer confirmed 2026-07-22).**
> - **User & job:** every audience. Reading the estate/topic/value views, an unversioned topic
>   currently reads `unversioned` as if that were a version string — noise that competes with the
>   real signal (which topics *do* carry versions, and drift between them).
> - **Verified current state:** `mesh-ui.html` renders `t.version || "unversioned"` in three
>   places — the estate topics table (`renderTopicRows`, ~line 1454), the topic-page version
>   header (`renderTopicPage`, ~line 1654), and the value-view row (`buildValueRow`, ~line 1920).
>   The literal `"unversioned"` is the fallback whenever `MeshTopicEntry.Version` is empty/null.
> - **Requirement / acceptance criteria:**
>   - When a topic has a version, render the version (unchanged).
>   - When a topic has no version, render **nothing** where the version chip would be — no
>     `unversioned` label, no empty pill box. Absence of a version *is* the signal.
>   - Applies to all three render sites; the topic-page header must still render cleanly (no
>     dangling separator/`@`) with the chip omitted.
>   - The value-view `usageEntriesForTopic(t.topic, t.version || null)` join key is unchanged —
>     this is a **display-only** change; `null`/empty version still keys usage correctly.
>   - Playwright: a fixture topic with no version shows no `unversioned` text in estate table,
>     topic page, and value view; a versioned topic still shows its version; light + dark.
> - **Decision-framework note:** no spec impact, no data change, static floor untouched. Pure
>   time-to-understanding win (noise reduction). This is the cheapest item in the whole doc.
>
> ---
>
> **F2 — Value & deprecation as RAG (green / amber / red). SIZE: SMALL–MEDIUM. PRIORITY: P8.
APPROVED (maintainer confirmed 2026-07-22), with the Removed-tier ruling below.**
> - **User & job:** the product owner defending a deprecation ("can I retire `order:legacy-export`
>   this quarter?"). Today the value view (`renderValueView`, P5) already tiers every domain topic
>   — *Retirement candidates* / *Verify externally* / *No retirement signal*, plus *Removed since
>   the previous run* — but the tiers are **text headers with no colour encoding** (verified:
>   `VD_TIERS` labels + `vd-group-h`/`vd-group-sub` classes; the only coloured badge on a row is
>   the neutral `t-status-deprecation-candidate` chip, `chip-bg`/`chip-ink` — not RAG). A PO can't
>   scan the estate and see red/amber/green at a glance.
> - **Requirement / acceptance criteria:**
>   - Map the **existing** four tiers to a RAG scale (no new data, no new tier logic — this is a
>     visual encoding of what P5 already computes):
>     - **Red** = *Retirement candidates* (strongest disuse evidence — a live proposal to act on).
>     - **Grey / "gone" (DISTINCT from red — maintainer ruling 2026-07-22)** = *Removed since the
>       previous run*. It is past-tense fact, not a live proposal, so it gets its own muted
>       gone/grey treatment rather than sharing red with Candidates. Keep it visually calm (it's a
>       record, not an alarm) but still clearly a distinct tier.
>     - **Amber** = *Verify externally* (`gap` topics — fleet data alone can't defend retiring
>       them; needs a human check outside the fleet).
>     - **Green** = *No retirement signal* (actively used, or no evidence of disuse).
>   - **Colour is never the only signal (accessibility, table stakes per the quality bar):** keep
>     the tier text label; add a non-colour cue (a leading status glyph/shape or a text status
>     word) so the RAG reading survives colour-blindness and monochrome/high-contrast. Reuse the
>     existing design-token palette (the health badges already have red/amber/green tokens —
>     `statusBadgeClass`, the `warning` amber tier) rather than introducing new colours; verify in
>     light **and** dark and under forced-colors/strict-CSP.
>   - **Honesty rule preserved (P5):** with no usage feed wired, the header still says "structural
>     evidence only" and *disuse is never claimed*. RAG must not turn a structural-only "no
>     declared consumers" into a confident red "unused" — when the feed is absent, a candidate row
>     is amber-with-caveat, not red, OR the header's structural-only banner stays load-bearing and
>     the row text keeps "no usage feed to check against". Do not let colour overstate certainty
>     the data can't support. (This is the one place F2 has real subtlety — resolve it toward the
>     P5 honesty ruling, not toward a prettier traffic light.)
> - **Decision-framework note:** no spec impact, no new data, static floor untouched. Small unless
>   the feed-absent honesty nuance is done properly, which nudges it to S–M. Consider extending the
>   same RAG vocabulary to the issue inbox severity groups later for consistency — noted, not
>   scoped here.
> - **Sub-decision (RESOLVED, maintainer 2026-07-22):** *Removed* gets its own distinct gone/grey
>   treatment, separate from red *Retirement candidates*. No open questions remain on F2.
>
> ---
>
> **F3 — Send demo payloads. This is TWO capabilities, and they must be split — one is
> static-safe and near-ready, the other reopens a settled security decision. Read the split before
> the sizing.**
>
> The feedback bundles: *compose a message from topic + headers + payload fields, dress it for a
> transport (their example: SQS), choose among supported payloads incl. custom ones defined in
> code* — **and** *actually send it into the mesh, feature-toggled off in prod*. The composition
> half is static-floor-compatible and reuses machinery that already exists. The **send** half
> cannot be done from the static UI at all (a browser cannot put a message on SQS without a
> server-side proxy holding cloud credentials) and directly contradicts roadmap §10.7 item 1,
> which **de-scoped live multi-protocol dispatch from the centralized/aggregated view on
> blast-radius grounds**, restricting any live "reach into a system" affordance to a single
> service's *own* self-hosted Spec UI. So:
>
> **F3a — Compose & copy a transport-dressed payload (the "build it into an SQS" half).
> SIZE: MEDIUM. PRIORITY: P9. Static-floor-safe. APPROVED (maintainer confirmed 2026-07-22 — keep
> as-is regardless of F3b's direction; compose+copy is valuable on its own and covers the
> queue/stream transports that F3b-revised excludes).**
> - **User & job:** a developer (and a technical BA) validating a service — "give me a correctly
>   shaped SQS/SNS/API-Gateway/raw-envelope message for `order:placed` so I can paste it into the
>   AWS CLI / console / Lambda Test Tool and exercise the handler," without hand-authoring envelope
>   boilerplate or reading C#.
> - **This already mostly exists — do not rebuild it (roadmap §10.2):** `Benzene.CodeGen.
>   LambdaTestTool`'s `LambdaTestFilesBuilder` already dresses per-topic example payloads as
>   `benzene-message` / `sns` / `sqs` / `api-gateway` envelopes, off the **deterministic**
>   `Benzene.Schema.OpenApi.Examples.ExamplePayloadBuilder` (`DefaultExampleBuilders`) — the same
>   generator the spec embeds. `work/runtime-test-payloads-plan.md` already designed the runtime,
>   opt-in, introspect-and-dress endpoint (`UseTestPayloads()`), split by the transports a service
>   is actually wired to (`EventServiceDocument.Transports`).
> - **"Supported payloads" discovery = schema-derived defaults + code-registered custom ones:**
>   - *Schema-derived default:* generated by `ExamplePayloadBuilder` from the topic's own schema —
>     the mesh already carries these inlined per (topic, version) as `MeshTopicEntry.RequestSchema`/
>     `ResponseSchema`/`MessageSchema`, so the UI can render a field skeleton **with zero backend**.
>   - *Custom payloads "defined in code":* map to the existing BYO-schema seam —
>     `SuppliedSchemaCatalog` / `AddSuppliedSchemas` (see
>     `work/complex-payloads-byo-schema-plan.md`). A code-registered example is the natural sibling
>     of a code-registered schema. Requirement: a "supported payloads" list per topic = the
>     schema-derived default **plus** any code-registered named examples, the user picks one.
> - **Where the logic lives (architecture ruling — do NOT put envelope-dressing in the static
>   UI):** the C# envelope builders can't run in `mesh-ui.html`. Two acceptable vessels, pick one:
>   1. **Artifact/endpoint on the host** (preferred): the aggregator/`deploy/Mesh/Benzene.Mesh.Host`
>      publishes or serves the dressed example payloads (the `UseTestPayloads()` design), and the
>      static UI *displays + copies* them — feature-detected exactly like annotations/usage, so the
>      static floor holds when absent.
>   2. **Client-side skeleton only** (degraded fallback): the UI generates a raw-envelope JSON
>      skeleton from the inlined `MeshTopicEntry` schema it already has, and offers copy — no
>      SQS/SNS dressing (that stays host-side). Ship this as the always-available floor even if (1)
>      isn't wired.
> - **Dependency discipline:** transport-dressing must not pull AWS test-helper packages into a
>   service's runtime or into `Contracts`/`Ui`. Adopt `runtime-test-payloads-plan.md`'s
>   recommendation 1(c): a runtime-clean core + AWS dressing in a separate opt-in
>   `Benzene.*.TestPayloads.Aws` package. Azure (Service Bus / Event Hub) dressing is a documented
>   follow-up, **not** silently shipped AWS-only-and-called-done (honesty convention).
> - **Acceptance criteria:** per non-reserved topic, list supported payloads (schema default +
>   code-registered customs); pick a transport from the ones that topic actually supports
>   (intersection of the service's `Transports` + `HttpMappings` for `api-gateway`); render the
>   dressed message; **copy** (not send). Static floor: with no host endpoint, the UI still offers
>   the raw-envelope skeleton from inlined schema. "Its own screen": yes — a dedicated compose view
>   (`#compose:<topic>` hash, consistent with the three-entity router) rather than bolted onto the
>   catalog.
> - **Spec impact:** none. Everything needed (topic schemas, transports, HTTP mappings) is already
>   in the spec / already fetched by the aggregator. No Cloud Service spec widening. Taut.
>
> **F3b (SUPERSEDED — the queue-injection framing below was NOT authorized).** The original F3b
> asked to reverse §10.7 and let the centralized UI inject messages into shared infrastructure
> (SQS/SNS). The maintainer (2026-07-22) **did not authorize that** — §10.7 stands as-is for
> queue/stream injection — and instead steered to a narrower, explore-and-design direction
> captured as **F3b-revised** below. The queue-injection posture is retained here only as the
> rejected baseline the new direction is measured against; it is not on the backlog.
>
> **F3b-revised — DIRECT-TO-CONSUMER dispatch + Swagger-for-HTTP. STATUS: EXPLORE & DESIGN
> (NOT build, NOT approved to build). §10.7 is NOT reopened — this is a *candidate that might
> clear its bar*, pending an explicit maintainer ruling. Maintainer words (2026-07-22): "the
> payloads would be sent straight to the consumer, such as the Lambda and not to the SQS. this
> might take more thinking about. A possible other solution for http is to provide a wired in
> swagger interface."**
>
> This splits by transport into three cleanly-separated cases — that partition is the key product
> insight, because it decides which transports can ever get a live-send and which stay compose+copy
> (F3a) only:
>
> **(1) Direct-invokable transports — Lambda direct `Invoke`, HTTP, BenzeneMessage — CANDIDATE
> that plausibly clears §10.7's bar. SIZE: MEDIUM–LARGE.**
> - **Why the blast-radius calculus genuinely changes vs. the rejected queue version:**
>   - **The access path already exists and is already trusted.** The aggregator *already* reaches
>     each service directly to interrogate it — spec/health via Lambda `Invoke` or HTTPS. "Invoke
>     the target service directly with a chosen payload" reuses the *same* access grant (same
>     `lambda:InvokeFunction` action / same HTTP POST to the service's own invoke endpoint the
>     Fleet view already speaks) — it changes the *payload*, not the *permission*. **No new
>     credential type** (notably: no queue-write / `sqs:SendMessage` grant, which the rejected
>     version required).
>   - **It targets exactly one known service**, the one the mesh is already talking to — not a
>     shared queue that fans out to arbitrary other systems. §10.7's specific objection ("reaches
>     into 'different systems' from a central, aggregated view") is about unbounded fan-out into
>     shared infra; direct-to-consumer is bounded to a single declared endpoint.
> - **Residual blast radius (state it honestly — it is NOT zero):** the invoked handler runs *for
>   real* and executes real side-effects (DB writes, downstream calls, possibly its own publishes
>   to SQS/SNS as part of handling). So fan-out isn't eliminated — it's one hop removed and
>   *mediated by real handler logic* rather than raw infrastructure injection. This is materially
>   smaller and more predictable than queue injection, but it is "a real handler ran with test
>   data," which is exactly why it must stay off production.
> - **Posture (lighter than the rejected version, but still gated):**
>   - **Toggle still required:** opt-in registration **and** an explicit `AllowInProduction`/env
>     gate (`runtime-test-payloads-plan.md` decision 3). Off by default, loudly — because real
>     side-effects execute. The *credential* posture is lighter (reuses the existing invoke path,
>     no new queue-write creds), but the *side-effect* posture is unchanged, so the gate stays.
>   - **Vessel:** for **Lambda direct-invoke** and **BenzeneMessage**, still
>     `deploy/Mesh/Benzene.Mesh.Host` — a browser cannot perform a Lambda `Invoke`, so it goes
>     through a host proxy that reuses the aggregator's existing invoke path. For **HTTP**, see
>     case (2): the browser can POST directly given CORS/auth, which is the Swagger option and may
>     need no host proxy at all.
>   - **Static floor:** unchanged — `Benzene.Mesh.Ui` feature-detects the host dispatch endpoint
>     and degrades to F3a compose+copy when absent; default "not present" = off in prod by
>     construction.
> - **My PO read: this candidate plausibly clears the bar the queue version didn't**, because it
>   reuses an already-trusted access path, is bounded to one known service, and adds no new
>   credential type. I am **recommending** it to the maintainer as clearing §10.7's intent — but
>   NOT treating §10.7 as reopened, and NOT building, until the maintainer rules, because the
>   residual "real handler side-effects" risk is a real product judgment call, and they said "this
>   might take more thinking about."
>
> **(2) HTTP transport — "wired-in Swagger" — this is the HTTP-shaped live-send answer, and it may
> need NO §10.7 exception at all. SIZE: SMALL (deep-link) to MEDIUM (centralized cross-origin).**
> - **Building block already exists:** `Benzene.Spec.Ui`'s `spec-ui.html` already has a live
>   "Try it" (`tryItBlock`) that POSTs the raw envelope **same-origin** to the service that serves
>   it. The mesh's own `mesh-spec-ui.html` deliberately has **no** "Try it" — its CLAUDE.md states
>   this is exactly *because* calling the service would be cross-origin from the mesh. So the live
>   HTTP-call capability is already built; the only question is where it's hosted relative to the
>   service's origin.
> - **Two framings, with very different §10.7 implications:**
>   - **(2a) Deep-link to the service's own self-hosted Spec UI — §10.7-CLEAN BY CONSTRUCTION,
>     RECOMMENDED.** The mesh links out to each HTTP service's own `UseSpecUi()` "Try it." This is
>     *literally* where §10.7 said live dispatch belongs ("scoped to a single service's own
>     self-hosted Spec UI, where 'this page can reach this one service' is unremarkable"). Zero new
>     blast radius, no reopening, no centralized credential. Cost: the service must host its own
>     Spec UI (optional today) and the mesh must know its base URL (a link, not a fetch). This is
>     the cheapest live-HTTP answer and needs no maintainer security ruling.
>   - **(2b) Centralized cross-origin Swagger that calls the service from the dashboard —
>     HEAVIER, separately decided.** A Swagger UI hosted in the mesh that POSTs cross-origin to the
>     service. This needs the target service to serve **CORS** headers allow-listing the dashboard
>     origin on its invoke endpoint (roadmap §10.5 already flagged CORS as the prerequisite for
>     centralized cross-service calls) **and** the browser to carry the service's **auth** (bearer
>     injection; cookies don't cross origin cleanly). This reintroduces exactly the cross-origin +
>     auth coupling `mesh-spec-ui.html` and §10.5 were cautious about. Viable, but it's a real
>     decision, not a free win — and (2a) delivers the same user job without it.
> - **Does Swagger subsume F3a for HTTP? No — it complements it.** Swagger/Try-it is the *live
>   send* for HTTP; F3a (compose + transport-dress + copy) still has independent value: it works
>   with zero backend, produces copy-paste artifacts for CLI/CI/scripts, and covers the
>   queue/stream transports Swagger can't. Keep both.
>
> **(3) Queue/stream transports — SQS, SNS, Event Hub, Kinesis, Event Grid — OUT OF SCOPE for any
> live send.** "Send straight to the consumer" does not apply to shared infrastructure; these are
> precisely what §10.7 excluded and the maintainer did not authorize. For these, the answer stays
> **F3a compose + copy only** (dress the payload, copy it, paste into the CLI/console). This is the
> honest boundary of the direct-to-consumer model.
>
> **Convergence note:** for HTTP, case (1)'s "send straight to consumer" and case (2)'s Swagger are
> the *same* operation (POST to the service's HTTP endpoint). The genuinely *new* capability in (1)
> is the **Lambda direct-invoke** (and BenzeneMessage) path a browser can't perform — that's the
> piece that needs the host proxy and the §10.7 judgment. HTTP is best served by (2a).
>
> **Open decisions the maintainer still owns (F3b-revised):**
> 1. **Does direct-to-consumer clear §10.7's bar?** My recommendation: yes for case (1) — it
>    reuses an already-trusted access path, is bounded to one service, adds no new credential type.
>    But the residual "real handler side-effects execute" risk is a product call only the
>    maintainer makes. Ruling needed before any build. §10.7 remains NOT reopened until then.
> 2. **Posture for case (1):** confirm opt-in registration **+** `AllowInProduction`/env gate (my
>    recommendation), or a lighter posture given the lighter credential footprint? My steer: keep
>    both gates — side-effects, not credentials, are the reason.
> 3. **Swagger framing:** (2a) deep-link to the service's own Spec UI (recommended, §10.7-clean,
>    cheap) vs. (2b) centralized cross-origin Swagger (needs CORS + browser-carried auth). If (2a),
>    do we make `UseSpecUi()` a recommended part of the service standard so the deep-link target
>    reliably exists? If (2b), that CORS/auth prerequisite needs writing into `design-principles.md`
>    §5 (as §10.5 already anticipated).
> 4. **Scope of the first design cut:** HTTP-only via (2a) first (smallest, §10.7-clean, ships
>    value immediately), with Lambda direct-invoke (case 1) as a follow-on once its §10.7 ruling
>    lands? My recommendation: yes — sequence (2a) → (1-Lambda), F3a in parallel.
> - **If the maintainer approves a build:** case (1) Lambda/BenzeneMessage dispatch is a
>   `deploy/Mesh/Benzene.Mesh.Host` feature and gets its own design doc (cross-ref §10.2/§10.7 +
>   `runtime-test-payloads-plan.md`); case (2a) is a `Benzene.Mesh.Ui` deep-link + a service-URL
>   the mesh already has; case (2b) is a host + CORS/auth design.
>
> **Cross-reference:** the data-layer / packages side of F3 (runtime `UseTestPayloads()` endpoint,
> transport-dressing package split, direct-to-consumer host dispatch endpoint, Swagger wiring) is
> recorded in `work/service-mesh-roadmap-1.0.md` (dated block at top, and §10.2/§10.7) and
> `work/runtime-test-payloads-plan.md`. F1/F2 are UI-only and live here.
>
> ---
>
> **2026-07-22 P6 SHIPPED — discussion & annotations. The 2026-07-22 roadmap (P1–P6)
> is complete.**
> - **The vessel ruling (the "hard constraint" decision, now made):** discussion is split
>   across the two halves the architecture already had. The **read path is a static artifact**
>   — `annotations.json`, published into the same `IMeshArtifactStore` as `manifest.json`, so
>   any static host serves recorded discussion with zero backend. The **write path is a
>   dogfooded handler** — `mesh:annotations:add` (`POST /mesh/annotations`) on the aggregator
>   host, `mesh:report`'s exact opt-in shape, spoken to by the explorer through the wire
>   envelope and **feature-detected** (`?annotations=` / `data-annotations-url`). Degradation
>   ladder: no artifact + no endpoint → the feature leaves no trace (the static floor,
>   untouched); artifact only → read-only threads with the state explicitly labeled; endpoint →
>   composer. Of the three candidate vessels, this is "enhancement layer in the existing pages"
>   — no companion app, no new collector contract, nothing added to the Cloud Service spec.
> - **The identity ruling (the open question, now answered):** authoring is **self-declared
>   display names**; authenticating who may post — and verifying who they are — belongs to the
>   gateway in front of the annotations endpoint. This is the `Benzene.RateLimiting` boundary
>   ruling applied to writes: Benzene ships the mechanism and says so plainly (the composer
>   carries the caveat in-line), the deployment's edge owns access control. The mesh packages
>   stay identity-free; the handler enforces shape only (required fields, 200/80/4000 bounds).
> - **Contracts:** `MeshAnnotation`/`MeshAnnotationLog`/`MeshAnnotationRequest`/
>   `MeshAnnotationThread`; entity ids reuse the explorer's own model (`service:<name>`,
>   `topic:<id>`). Durability note: notes are the one artifact that can't be regenerated from
>   the fleet, so a corrupt log is parked to a timestamped sibling, never silently discarded.
> - **UI:** Discussion sections on the topic and service pages — the decisions the evidence
>   provokes recorded next to the evidence. The demo now shows the full arc on
>   `order:legacy-export`: deprecation-candidate badge + zero observed usage (P5's evidence)
>   with the retirement decision thread beneath it (P6's record).
> - Verified: 10 new unit tests (publisher round-trip/corruption-parking, handler
>   validation/bounds/thread response — 211 Mesh tests green) and 62 Playwright checks
>   including a stub write path over the envelope (composer feature-detection, post → thread
>   re-render, cache survival across navigation, required-field guard), zero console errors.
> - **Roadmap status: P1 (three-entity model), P2 (flow view + staleness), P3 (topology
>   graphs), P4 (usage feed), P5 (value & deprecation), P6 (discussion) — all shipped.**
>   Follow-ups parked, not planned: threaded replies/resolution states on notes, field-level
>   per-service spec diffing (P5's scope ruling), metrics-backend usage adapters (App
>   Insights/CloudWatch, need their SDKs), structural-vs-observed topology edge merging.
>
> ---
>
> **2026-07-22 (earlier) P5 SHIPPED — the value & deprecation view, and data requirement 2
> closed at topic granularity:**
> - **Drift substance (req. 2):** the aggregator now diffs each run's catalog against its own
>   previous `topics.json` (the snapshot read-back pattern, catalog-wide) and annotates what
>   changed: `MeshTopicEntry.Changes` (`topic-added`/`schema-changed` with the changed side
>   named/`producers-changed`/`consumers-changed` with `+`/`-` deltas) plus
>   `MeshTopicCatalog.RemovedTopics` for topics that vanished entirely. First run claims
>   nothing; reserved churn never flagged. **Scope ruling recorded:** req. 2 is closed at topic
>   granularity — the roadmap's "check `Schema.OpenApi/Compatibility` first" was checked, and
>   deliberately not used: the comparer needs the typed `EventServiceDocument` model, which
>   cross-language (Go-emitted) specs aren't guaranteed to round-trip; the aggregator stays on
>   its best-effort JSON-level convention. Field-level per-service diff (the service page's
>   "what changed inside this service's contract") remains open as a follow-up, now clearly a
>   nice-to-have rather than a gate.
> - **The view (estate-level, the roadmap's "defend a deprecation" ranking):** every domain
>   topic tiered by retirement evidence, evidence spelled out per row — the view argues from
>   data, it never decides. Tiers: Removed since the previous run / Retirement candidates (no
>   declared consumers, and/or zero observed usage while a feed is wired) / Verify externally
>   (`gap` topics — fleet data alone can't defend retiring something fed from outside the
>   fleet) / No retirement signal. Least-used first within a tier; rows carry status badges,
>   change badges, producer/consumer counts, and observed volume; everything links through to
>   the topic page, which now renders the change lines in full above its payload panel.
>   Honesty rule: with no usage feed wired the header says "structural evidence only" — disuse
>   is never claimed without the feed that could prove it.
> - **Also fixed:** the service page's spec links had rotted in the `mesh-spec-ui` merge (the
>   removed `specUiLink` was still referenced — every service-page render threw). Caught by
>   this phase's browser verification; the service page now shares the estate card's
>   mesh-hosted spec / raw / health link set.
> - Verified: 7 new aggregator diff tests (201 Mesh tests green) and 56 Playwright checks
>   against the refreshed demo (topics.json now carries `changes` + `removedTopics` fixtures),
>   zero console errors.
> - Remaining roadmap: P6 discussion/annotations (backend + auth vessel decision per "The hard
>   constraint" — the static explorer must keep working without it).
>
> ---
>
> **2026-07-22 (earlier) P4 SHIPPED — usage analytics, and data requirement 1 closed:**
> - **The C.1 usage feed now exists end to end.** The emission half turned out to already be
>   shipped: `Benzene.Diagnostics`' `UseBenzeneMetrics()` emits `benzene.messages.processed` /
>   `benzene.message.duration` per handled message, tagged `topic`/`transport`/`result` — exactly
>   the owner's standard metadata set. That tag set is now documented as **the** metric metadata
>   standard in `docs/mesh-usage-feed.md` and flagged as a published contract in the Diagnostics
>   package docs. Per the owner's ruling it stays observability-side: no Cloud Service spec
>   widening, no new required endpoints on any service.
> - **Ingestion:** `MeshUsage`/`MeshUsageEntry` (`usage.json`) + the `IMeshUsageSource` port in
>   `Benzene.Mesh.Contracts` (zero-I/O port, `IMeshReportPublisher` precedent — adapters depend
>   on Contracts alone). `MeshAggregator` polls all registered sources per run (concurrent with
>   the service polling, per-source 10s timeout, a throwing source never fails the run), merges
>   reports (per-entry `source` attribution, `TopologyEdge` precedent) and publishes `usage.json`
>   only when a source reported — absence still means "no feed wired", empty entries means "feed
>   wired, nothing observed". Not a defined-but-produced-by-nothing contract: the first adapter
>   ships too — `CollectorUsageSource` bridges a co-hosted collector's cumulative per-topic
>   stats as (topic, version, status) entries, transport/service honestly absent (the trace wire
>   shape has no transport; that dimension is the metrics-backend adapters' job — App Insights/
>   CloudWatch adapters need their SDKs, so they ship as their own packages later).
> - **UI (usage sections on all three entity pages, not a separate dashboard):** estate topics
>   table gains a Usage column (`–` for unexercised topics); topic page gains a usage panel
>   (total/window/source, split-by-transport and split-by-status chip rows); service page gains
>   a Usage section directly under the functional map (service-attributed entries, or
>   clearly-labeled fleet-wide counts for its topics when the feed can't attribute). Degradation
>   per the owner's ruling: missing artifact hides everything; missing dimensions become a
>   data-quality footnote inside the panel (findable, off the primary screen); an unexercised
>   topic renders the explicit "feed wired, no traffic observed" state — which is precisely the
>   deprecation evidence P5 will rank on.
> - Verified: 8 new unit tests (aggregator merge/timeout/absence semantics, collector bridge
>   dimensions) — 181 Mesh tests green — and 42 Playwright checks against the refreshed demo
>   (which now ships a two-source `usage.json`: a transport-rich "cloudwatch" feed + a
>   collector-shaped feed, so every degradation path is visible), zero console errors.
> - Remaining roadmap: P5 value/deprecation view (usage + observed consumers + drift substance —
>   data req. 2, drift substance, is now the only gating input), P6 discussion/annotations.
>
> ---
>
> **2026-07-22 (earlier) P3 SHIPPED — the topology graph, on both planes:**
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

---

## 2026-08-10 — the React/Redux rebuild: what the port cost, and what it bought

`mesh-ui.html` — five thousand hand-written lines — has been retired in favour of
[benzene-ui](https://github.com/daniellepelley/benzene-ui), a React + Redux Toolkit component
library built to one rule: **components hold no state; the UI is a function of the store**. The rule
is enforced mechanically (`src/components/architecture.test.ts`), not described in a README. The
build vendors two self-contained HTML pages down the chain
`benzene-ui/build → Benzene/mesh-ui → website/demos + Benzene.{Mesh,Spec}.Ui`, checked by CI in two
repos.

Two things are worth recording, because both are the kind of thing a rebuild does silently.

### The visual system was a casualty, and nothing caught it

The port carried the components across and left the theme behind. The token set went from roughly
thirty semantic colours to nine, and the stylesheet ended up with **no `html` or `body` rule at
all** — so the product shipped as a dark box floating on a browser-default white page, in Times New
Roman. Every one of the two hundred-odd tests passed, because every test asserted on text content
and the text was fine.

The fix was a base layer and a restored token set. The lesson is the check, not the fix:

- `src/theme/theme.test.ts` parses the stylesheet and refuses to let the foundation go missing
  again — body/html backgrounds, a page font, `color-scheme`, form-control typography, a focus ring,
  colour-token parity between light and dark, and no hardcoded hex outside the token blocks.
- `npm run shots` renders every page in a real browser and writes a contact sheet. Deliberately not
  a visual-regression test — no baselines, nothing to fail. A screenshot diff on a page under active
  design churns and gets ignored, which is worse than no check. This just makes *looking* cheap, and
  a person spots a wrong-looking page in about a second where a test suite never will.

**Automated tests cannot tell you a page looks wrong.** Any future port should assume the theme is
the part that will be dropped, and should budget a look at it.

### The functional map was missing, and had been for a while

The estate page listed *flagged* topics only. So the product's first question — **what do these
services actually do** — could only be answered by opening every service in turn and assembling the
map by hand. That is a capability gap, not a styling one, and it predates the rebuild.

Now closed: a **topic catalog** on the front door, one row per topic — producers, consumers, HTTP
routes, status, traffic — sortable, filterable by topic *or* service name, with Benzene's own
utility topics held back until asked for like every other traffic surface. It subsumes the old
"topics needing attention" list, since every flagged topic is a row in it with its status; keeping a
second surface for the same rows was the sort of duplication that grew the page this replaced to
five thousand lines.

Two honesty rules carried into it, both load-bearing:

- Traffic with no usage feed renders `—`, never `0`, and sorts *below* zero. A column of invented
  zeroes would tell a reader the whole estate is unused.
- A filtered table states `n of N`, so a narrow view is never mistaken for the estate.

### Also in this pass

A real `DataTable` primitive (the library had none — every list was flex rows with wrapping chips,
so nothing lined up and a reader scanning a column was reading, not scanning); collapsible sections
that remember what a reader put away; the service-card disclosure filled in, having previously
opened an empty box; filters moved to sit with the list they filter, rather than in a global header
where the service filter was present on every page and did something on only one; and a three-state
theme toggle — light, dark, or follow the system — because "follow the system" is a real answer and
a two-state switch overrides a preference the reader already gave their OS.

### Postscript — the arrival flash, and the bug behind it

The last item on the review list was a status-change flash on the service cards. It turned out to be
blocked on a defect, not on taste: **nothing refreshed the declared plane.** `manifestRefreshed` had
no dispatcher, so the manifest was fetched once at page load and never again. A dashboard left open
showed the statuses it had when the tab opened, for as long as the tab stayed open, under a
"generated" timestamp that never moved — and nothing on the page said so. There was no status change
for a flash to fire on, because statuses did not change.

The published artifacts now refresh together on a sixty-second timer — together, because one
aggregator run publishes them under one `generatedAtUtc`, and refreshing the manifest alone would put
fresh statuses under a stale map. A failed refresh keeps the last good manifest and says nothing: a
transient fetch failure is not news about the estate, and the stale timestamp in the header is what
tells the reader how old this is.

The flash is one 1.4-second settle in the card's own RAG colour — never a pulse, never a repeat. A
card that keeps moving is motion in the place alarms live, and readers learn to look away from that.
The colour makes it say *which way* it moved; a service going red and a service recovering are
different news. It is empty on first load by design: a wall of flashing cards on arrival says
nothing, because "everything is new" and "the page just opened" are the same picture.

Worth noting how the defect surfaced. It was not found by a test or a review of the store — it was
found by asking what the animation would actually fire on. **A feature nobody could build on top of
was the evidence that the thing underneath was not running.**

---

## 2026-08-16 — PRODUCT REFINEMENT on the first user-feedback round

Input: `work/mesh-feedback-round-2026-08-16.md` (eight personas, `work/mesh-user-personas.md`, harness
built from `benzene-ui` `3a61f05`). This block **deviates from** the "two audiences" framing above and
**re-ranks** the six outcomes. It does not rewrite them; read them as history, and this as what
replaced them.

Everything below that asserts current behaviour was checked in source before it entered the backlog.
Where the evidence pack is wrong, §0 says so — the pack will be read again and must not be believed
uncritically.

### §0 — Corrections to the evidence pack (verified in source; re-scope, don't delete)

The round found real defects. It also mis-scoped four findings, two of them the round's own
headlines. Ranking by user harm requires fixing the scope first.

1. **Ownership is NOT absent from the model.** `MeshServiceRegistryEntry.OwningTeam` →
   `MeshManifestEntry.OwningTeam` exists (`Benzene.Mesh.Contracts/MeshManifestEntry.cs:61`,
   populated at `Benzene.Mesh.Aggregator/MeshAggregator.cs:89`) and **the UI already renders it**
   (`ServiceCard.tsx:67`, `ServicePage.tsx:70`). It is absent from
   `benzene-ui/contracts/artifacts/manifest.json` — the fixture the round ran on. Only
   `manifest.minimal.json` carries it. Re-scoped: from *"missing capability"* to *"missing from the
   demo estate, and one free-text string is too thin to answer 'who do I wake'."* Still a top-three
   backlog item; a different item than the one reported.
2. **Version skew is SHIPPED, not missing.** Theme 4 — the round's "sharpest single finding" — is
   largely a fixture artifact. `MeshTopicVersionCompatibility` (produced/consumed/producedNotConsumed,
   with the upcaster caveat written into the type), the `VersionCompatibility` component, the
   `selectVersionCompatibility` selector and `contracts/artifacts/topics.versioned.json` all exist —
   and that fixture *literally encodes* `payment:capture` produced at v2, consumed at v1, exactly the
   go/no-go the developer said the product couldn't show. The round ran against `topics.json`, where
   every domain topic has `version: ""` and there is no `versionCompatibility` block. Re-scoped: from
   *SEVERE product gap* to *SEVERE demo-surface gap*, plus one real, small reconciliation gap (the
   live plane carries `"version": "1"` while the declared plane renders `—`; declared-blank should
   defer to live-with-provenance, not to punctuation). **This is the largest re-ranking in the round.**
3. **`missingFeeds` is rendered — on one surface of five.** `TopicLiveStrip.tsx:98` renders it as
   *"not supplied by this plane: …"*, which is why a DOM search for the string "missing" found
   nothing. Service-level `missingFeeds` (`shipping-api: [health, traces]`) is genuinely rendered
   nowhere. Re-scoped: from *"the mesh knows its blind spots and never says"* to *"it says so once,
   in words nobody searches for, on the one surface where the reader already suspected."* The
   severity is unchanged; the fix is smaller than reported.
4. **The window picker is a control-honesty defect, not a data gap.** The wire self-describes
   (`window.countsWindowed: false` + `countsSince`) and the store already reads it
   (`selectors.ts:932`). The caption is *honest*; the picker sitting above it is not. No data
   requirement, no spec question — the fix is that a control which cannot govern the numbers must
   say so or not be offered.

Confirmed exactly as reported, and it is the round's most important finding:

5. **The Value page manufactures deletion evidence from an absent row.** Verified in
   `benzene-ui/src/store/selectors.ts` — `feedWired` is `s.catalog.usage != null` (line 646: *the
   artifact exists at all*), and `totalFor()` (line 663) sums matching rows, so a topic with **zero
   rows** yields `0`, which line 675 turns into the evidence string *"no traffic observed while the
   usage feed is wired."* Estate-scoped evidence rendered as a topic-scoped claim. `MeshUsage`
   carries no coverage declaration, so the aggregator cannot currently distinguish the two either.
   **Ranked #1 in this refinement**, above everything, because it is the only finding that can
   authorise an irreversible production action from data that does not exist.

### §1 — The audience model: two audiences is retired

**Deviation.** "The two audiences" (Developers / Product owners) is withdrawn as a prioritisation
model. It survives only as a *readership floor*, restated and widened in §1.3.

It failed for a specific reason worth recording: it assumed each audience brings one job. Eight roles
brought four, they collided across job titles (the architect and the delivery owner wanted the same
retirement evidence; the developer and the QA wanted the same schema constraints), and — decisively —
**the product's worst failures were not any audience's job. They were a property all eight needed and
none of them got.** A model that cannot express "the thing that hurt everyone" is the wrong model.

#### §1.1 Four jobs, replacing two audiences

Each job names the decision it unblocks. Ranked by how badly the round showed them served.

| Job | Bringers | The decision | Round verdict |
|---|---|---|---|
| **Decide the estate's future** | delivery owner, architect, security reviewer, PO | retire / invest / approve / sign off | **actively unsafe** (§0.5) |
| **Change safely** | developer, QA, architect | ship or hold this payload change; who do I tell | **half-served** — precise blast radius, no contact, no diff substance |
| **Comprehend** | BA, architect, joining developer | what does this system already do, before I write a requirement | **structurally served, semantically empty** |
| **Attribute** | production support, platform engineer | is it us or them; whose is this | **served at the seam, correctly narrow** |

#### §1.2 The guarantee that outranks all four: provenance

Every number, every empty state, every green tile states what it was derived from and what it could
not see. This is not a feature; it is the precondition for all four jobs, and the round is unanimous
evidence for it: Theme 1 (absence as good news), Theme 7 (numbers disagreeing), Theme 8 (an inert
control), Theme 9 (two clocks), Theme 11 (unstated scope of claim) are all one defect wearing five
costumes. It is also **already normative in the specification** — `mesh.md` §2 says of `degraded`
that *"an empty array asserts 'this service calls nothing,' which a port that cannot yet know that
has no right to assert."* The spec has the discipline. The product drops it at the render.

Standing rule, effective now: **no surface may present an absence as a measurement, and no claim may
be rendered at a wider scope than its evidence.**

#### §1.3 The readership floor (widened)

No view may require reading C#. **Widened by this round:** no view may require knowing Benzene's
internal vocabulary either. The BA logged *plane*, *reserved*, *raw (benzene-message)*, *exemplar
traces*, and *collector / aggregator / usage feed* — *"three words for what I think is one thing"* —
as blockers, and broke her producer/consumer mental model on `producers: none`. Implementation
vocabulary on a reader-facing surface is the same defect as a stack trace on a landing page
(`System.TimeoutException` was the round's most prominent landing-page text).

#### §1.4 Two roles are served as guests, deliberately

**Production support and QA are not first-class users of this product and will not be optimised for.**
Their own verdicts say so — *"YES, second tab, not first"* and *"YES for reading, NO for testing."*
Mesh serves them **only at the contract seam**: who else is on this topic, what shape is the payload,
what version. Their tools stay PagerDuty, Splunk, Postman, the CI suite. This is a ranking decision
with teeth — it is why the Test Console does not get assertions (§4) and why the flow drill-down does
not become a trace viewer (§5.7).

#### §1.5 One role the vision never named: the security reviewer

New first-class audience, with a requirement none of the others have: **the product must evidence its
own controls.** Their meta-finding — *"a control I cannot verify from the product is a control the
next reviewer will not credit"* — is a product requirement, not a security chore. `Benzene.Mesh.Dispatch`
has genuinely good controls (default-deny in Production, unset environment treated as Production,
three independent opt-ins, registry-bounded targeting, a confirmation that resets after every send —
all verified in `MeshDispatchGate.cs` / `Extensions.cs`) and **not one of them is visible from the UI.**

### §2 — The six outcomes, re-scored

| # | Outcome | Verdict |
|---|---|---|
| 0 | **Know what this view cannot see** | **NEW — added as a precondition, not a seventh item.** Nothing above it ships until it holds on that surface. |
| 1 | Understand the domain | **Claimed, hollow.** Structure real; meaning absent. No topic has a description; one of three services has a sentence; search matches names only, so `email` returns "no match" while `customerEmail` is a field on two topics. Outcome 1 already promised *ownership* — hollow per §0.1. |
| 2 | See the message flows | **Met as a map, hollow as a path.** The map is the thing nobody else's tool has. The instance-level drill-down is *advertised and absent* — "7 events" that is not a link, unclickable exemplar trace ids, `#trace/<id>` silently bouncing to fleet. An advertised non-affordance is an outcome-0 violation, not a missing feature. |
| 3 | Spot the issues | **Met, and the strongest surface in the product.** The only thing the architect said survives forty services — *"because it's a queue, not a canvas."* Protect it. |
| 4 | See usage | **Claimed, unsafe.** Two planes, two windows, two vocabularies, one inert control. `412 failed` beside `×486`; `observed 0` directly above `5,207 calls`. Delivery owner: *"a dashboard I have to caveat is a dashboard I don't open."* |
| 5 | Judge value | **Claimed, dangerous.** §0.5. |
| 6 | Discuss it | **Met, and undervalued — the surprise of the round.** Priya's one comment distinguishing expected drift (`PAY-118`) from the real issue *"carries more contract-health signal than every automated indicator on the page combined"* (architect). Outcome 6 was sequenced last and lowest; it out-performed four automated outcomes. **Promoted**: annotations are not decoration, they are the estate's only channel for human judgement the contract cannot derive — which is also where §5.3 sends data classification. |

Nothing is missing from the list. Outcome 0 is added, outcome 6 is promoted, and outcome 4's "trends
over time" half is deferred (§6) rather than pursued.

### §3 — Contradictions, resolved rather than averaged

**3.1 The Value page: "epistemic honesty" (architect) vs "manufactures a deletion" (platform engineer).**
The platform engineer wins outright; the architect was right about the *principle* and wrong about
this *instance*. The string is honest about the feed and dishonest about the topic. **Resolution: a
claim's scope must match its evidence's scope.** "The feed is wired" is estate-scoped; "no traffic
observed" is topic-scoped; joining them asserts per-topic coverage the artifact never carried. Three
states, not two — *covered and measured zero* / *not covered by any feed* / *no feed at all* — and
only the first is retirement evidence. The second moves the row to **Verify externally (amber)**,
which is what that tier is for. The UI-only half ships in R1; the artifact half (§5.1) follows. The
counter-evidence the round flagged as worth protecting — the strapline downgrade, `errors unknown`
instead of `0.0%` — is the correct behaviour generalised, not an exception.

**3.2 Dispatch: "protect these controls" (security) vs "I can't test with it" (QA) vs the console's own
"bookmark this as a production runbook step".** Security's posture wins outright, and the runbook copy
is **withdrawn, not reconciled** — it contradicts `MeshDispatchGate.IsAllowed` (`!IsProduction ||
AllowInProduction`) in the same product. Both cannot be right; the gate is right. QA's ask is granted
at the *contract* level only: show the declared response schema and return `x-correlation-id`. It is
refused at the *harness* level: no assertions, no collections, no CI. See §1.4.

**3.3 "Screenshot only, won't drive it live" (architect) vs "yes, live, over a stakeholder's shoulder"
(BA, delivery owner).** Not a contradiction — it is the clock. A 31-day-old snapshot with no age, no
warning and no styling is what makes a live demo unsafe. Resolution: snapshot age is ranked in R1
above almost everything, because it is cheap and it converts a MAYBE into a YES.

**3.4 "The empty topology reads as decoupling" (platform engineer) vs "the graph dies at node six"
(architect).** Both true, pulling opposite ways. Resolution: **the node-link graph is not the scaling
answer and will not be made into one.** Fix its empty state now (honesty); cap it as a small-estate
affordance. The surfaces that scale are the catalog table (faceting, pagination, *"every topic with
more than one producer"*) and the issue queue. This is a deliberate under-investment in the most
demo-friendly widget in the product.

**3.5 "Show me more" (all eight) vs "stay small" (all eight).** Resolved by §4.

### §4 — What mesh will NOT do (explicit product position)

The personas drew this boundary unprompted and near-unanimously. It is now a product position, and it
bounds the backlog.

**The seam mesh owns:** *what this estate declares, what is actually running, and whether they match* —
because mesh is the only thing that knows what a topic, a payload and a version are.

Mesh will not become:

1. **A monitoring system.** Platform engineer: *"the moment there's a chart with a threshold on it,
   mesh is a worse Grafana and I'll stop trusting both."* Derived rule: **no chart with a threshold
   on it, ever.** No alerting, no paging, no time-series exploration.
2. **An incident tool.** Lifecycle, comms and the rota stay in PagerDuty. Mesh names the contract
   facts an incident needs; it does not run the incident.
3. **A test runner.** No assertions, no collections, no CI integration, no test-case management.
4. **The authority on who may call what.** Security reviewer: *"Mesh should tell me a route exists;
   it should not become the authority on who may use it."* This also rules out mesh-side policy,
   allow-lists and per-caller entitlement views.
5. **The keeper of intent or target state.** ADRs, the *why*, and the 18-month picture stay with the
   architect. Mesh reports what is, never what was meant.
6. **A trace or log store.** *(Added by this refinement, and it is the non-obvious one.)* The round's
   most-requested feature — one real example of a failure — will **not** be built as a trace viewer.
   Three personas left for Splunk/CloudWatch at exactly that point, and the honest constraint is that
   the live plane's `traces[]` carries summary fields only. Mesh's answer is a **hand-off**: a
   copyable correlation id and an optional configured deep link out. Building the viewer would take
   mesh across boundary 1 by a different door.

**The Test Console is demoted** from headline capability to non-production diagnostic. It produced four
of the round's seven shipped-code defects, its copy contradicts its own gate, and it dispatches to the
wrong service (§5.8). It shipped on 2026-08-15/16 while outcome 0 was still unbuilt — a feature that
reaches into running services landed before the product could say what it could not see. Sequencing
lesson, recorded: **provenance is not a polish phase.**

### §5 — Decision-framework rulings on the major asks

Spec-coverage summary: **one spec addition approved, two rejected, one deferred.** Every other ask is
served from signal the estate already emits or from aggregator-side derivation.

**5.1 Absence honesty (Themes 1, 8, 9, 11) — APPROVE, top of the backlog.**
*Job:* all four. *Data:* on the wire today — `missingFeeds` and `degraded` (mesh.md §2/§6),
`window.countsWindowed`/`countsSince`, `snapshotAtUtc`. **No spec change; this is implementing honesty
the spec already mandates.** One genuine gap: `MeshUsage` has no per-topic *coverage* declaration, so
"the feed didn't cover this topic" is currently underivable. Ruling: **derive in the aggregator, not
from services** — `usage.json` gains a coverage/scope statement per source. That is an aggregator
artifact (`mesh.md` §9 explicitly leaves the aggregator's artifacts unconstrained), **not** a Cloud
Service spec change and not a conformance-fixture change. Until it lands, the UI must say *"not
present in the usage feed"* and must not tier on it.

**5.2 Ownership metadata — the first big spec question. REJECT the spec addition; APPROVE a registry widening.**
*Job:* all four; #1 ask for three personas. Production support: *"the single gap between mesh being a
nice tool and mesh being* the *tool."*
- **Ruling: ownership does NOT go into the ServiceDescriptor or the Cloud Service Profile.** Ownership
  is organisational and deployment truth, not service self-description. A service cannot derive its
  own on-call rota from its handler registry, and R5's whole principle is *"the spec is true because
  it is derived"* — putting `owner` in a derived document buys a hand-maintained string in the one
  document whose value is that nothing in it is hand-maintained. It also decays faster than services
  redeploy. Widening `mesh.md` §2 here would be the least taut change available: every port, every
  language, every profiled service pays, for a field that is wrong the week after a reorg.
- **Approved instead:** widen the **mesh registry** — already operator-supplied, already outside the
  spec — from a single `OwningTeam` string to a capped **contact block**: team, contact URI
  (channel/email), repo URL, runbook URL. Four optional fields, no rota schema, no schedule, no paging
  integration (boundary §4.2). Populate it from the `Benzene.Mesh.Discovery.*` providers where the
  answer already lives — AWS tags, Azure tags, Kubernetes labels.
- **Industry bar:** Backstage already owns exactly this problem at 3,000+ companies, with ownership
  declared in-repo and harvested. Mesh **federates** ownership; it does not become a catalog. The
  differentiator is that mesh can attach a contact to a *topic-level* blast radius, which Backstage
  cannot compute — and that only works if the contact is cheap to import, not another thing to author.
- **And render what already exists.** Per §0.1 the string is plumbed end to end and invisible only
  because the demo fixture omits it.

**5.3 Field-level data classification — the second big spec question. REJECT as a normative spec addition.**
*Job:* decide (security sign-off), comprehend (BA).
- **Ruling: no `pii` / `classification` field in `contract-document.md` or the Cloud Service Profile.**
  Four reasons, stated so this is not re-litigated: (i) **it is already expressible** — the document's
  schemas are OpenAPI 3.0 Schema Objects, so `description`, `title` and `x-` vendor extensions are
  legal today; making a taxonomy normative buys nothing a convention can't. (ii) **There is no neutral
  vocabulary** — PII / PCI / PHI / GDPR special category are jurisdiction- and organisation-specific;
  pinning one in a conformance fixture would be the least defensible thing in the spec. (iii) **An
  optional, hand-maintained classification is wrong exactly when it matters**, and a reviewer who
  cannot trust it is worse off than one reading field names — which is the §1.2 defect in a new
  costume. (iv) **The reviewer did not ask for it.** Their sign-off was YES WITH CONDITIONS and none of
  the three conditions was classification; all three were mesh-side (§5.6).
- **Also rejected: inferring classification from `format`.** A guessed PII flag that is absent for an
  unformatted `customerEmail` field is precisely "absence renders as good news." If we cannot be sure,
  we show the field name and let the human decide.
- **Approved instead:** ship the honest derived half the reviewer already praised — the
  contract-derived data-flow map, which fields cross which topic to which service — completed by
  §5.4's dropped constraints. Then treat classification as an **annotation** (outcome 6): human,
  org-specific, revisable, attached to an entity, correctable when wrong. That is what the P6
  vessel is for, it costs the spec nothing, and it puts the assertion where it can be argued with.
- **Revisit trigger, written down so it is a decision and not a refusal:** if three independent
  adopters need machine-readable classification to gate a pipeline, revisit — as an `x-` convention
  documented in `contract-document.md` as *tolerated, non-normative*. Never a MUST.

**5.4 Payload constraints dropped (Theme 5) — APPROVE, no data work at all.**
*Job:* change safely (QA's negative cases, developer's validation errors). Verified: `SchemaTree.tsx`
renders name / type / format / required / enum and silently drops `pattern`, `minimum`, `maximum`,
`minLength`, `maxLength` — while `orders:create`'s artifact carries `pattern: ^[A-Z]{3}-[0-9]{4}$` and
`quantity` 1–99, **and the usage feed counts 94 validation errors on that topic**. The developer's line
stands as written: *"the UI is hiding the cause of the failures it's counting."* Everything needed is
already in `topics.json`. Pure render fix, highest insight-per-line-of-code in the round.

**5.5 Drift and mismatch substance (Theme 3) — APPROVE, derived in the aggregator.**
*Job:* change safely. A hash pair is *"a change-detection primitive presented as a finding"*; a bare
`schemaMismatch: bool` *"is a rumour with a border-radius."* Half of this is already shipped and
unrendered on the round's fixture — `MeshTopicChange{kind, description}` gives topic-level "what
changed". The remaining half is real: the aggregator holds **both consumers' inlined schemas** at the
moment it sets the boolean, so it can emit the differing paths and does not. Ruling: **derive it in
the aggregator**; `topics.json` gains mismatch detail. No spec change, no fixture change, no new signal
from any service. This was parked as a P5 "nice-to-have"; the round promotes it — QA is being asked to
sign off a story whose downstream break the tool flags and refuses to describe.

**5.6 Security conditions — APPROVE all three blocking conditions; one spec line; two explicit refusals.**
*Job:* decide (sign-off). All verified in `Benzene.Mesh.Dispatch` source.
- **Audit trail — APPROVE.** Zero logging in the package, confirmed across all seven files. A real
  `payment:capture` for `amount: 99999` leaves no record outside the browser. Log service, topic,
  caller identity as the edge presents it, and outcome, at the handler.
- **Data-egress framing — APPROVE as documentation.** `HttpMeshServiceDispatcher.DispatchAsync`
  returns the target's body verbatim and headers pass through unmodified; the console renders it. So
  dispatch is a read primitive as much as a write one, from inside the perimeter, at the service's own
  privilege. This goes in the package `CLAUDE.md` and the deployment posture, plainly. **Refused:
  response-body redaction** — mesh cannot know what to redact, and pretending it can is §5.3's trap.
- **Environment identification — APPROVE, and this is the one spec change this round buys.**
  `placement` is on the wire and typed in the UI (`generated.ts:179`) and rendered nowhere; the fleet
  fixture carries `placement.environment` while `mesh.md` §2 documents only `placement.cloud` and
  `placement.region`. Dev and prod meshes being pixel-identical is a safety property, not cosmetics.
  **Approved spec change:** document `placement.environment` as OPTIONAL alongside `cloud`/`region`,
  under the same rule as `region` — *emitted only when the platform documents a way to know it; a port
  MUST NOT guess.* One line in `mesh.md` §2. It pays rent immediately, the field already flows, and
  spec + conformance fixtures + reference implementation move together as always.
- **REFUSED: a separate path/method for dispatch** so read-only access can be enforced by proxy, WAF or
  IAM route policy. It contradicts Cloud Service Profile **R4** — one wire-envelope endpoint is the
  surface generic tooling uses to reach any service without knowing its transport. Splitting it to
  make a WAF rule expressible would trade a spec invariant for a control the gate already provides.
- **DEFERRED: server-side topic validation** against the target's declared contract. Real (the
  dropdowns are client-side, so the reachable set is the service's whole routing table), but it would
  give `Benzene.Mesh.Dispatch` a dependency on the aggregator's catalog — a dependency-discipline cost
  for a risk the gate plus the non-production default already bound. Documented as a known limit in
  the package doc rather than silently omitted.

**5.7 The flow dead-end (Theme 6) — SPLIT: approve the hand-off, reject the viewer.** Per §4.6. The
removal of the fake drill-down affordances is R1 work (it is an outcome-0 violation, not a feature
request); the correlation-id copy and the configured deep-link out are R4.

**5.8 Recently-shipped defects — these are DEFECTS, not refinement items,** with two exceptions.
Straight fixes, ranked by harm: (1) `ComposePage` resolving the target from a topic's **producers**
when dispatch invokes a **consumer's** handler — verified against `selectProducerServicesForTopic`;
composing from `payment:capture` silently sends to `orders-api`, and *"a tester following the obvious
path tests the wrong service and never knows"*; (2) `routing.ts:92` using `history.replaceState`
exclusively, so Back ejects to `about:blank` — *"at 3am back is muscle memory"*; (3) the response panel
discarding `x-correlation-id`, the one field that makes a send traceable; (4) `toHash` returning
`#fleet` for a partially-filled console while the console's own copy promises the selection is in the
URL; (5) the mobile issue headline rendering one character per line — the product's single most
valuable element, on the device a PagerDuty link is opened on. **The two exceptions that are product
positions, not bugs:** the runbook copy (§3.2 — withdrawn) and bad routes silently rendering the fleet
page while leaving the bogus URL in the bar (§1.2 — a wrong page presented as a right one).

**5.9 The demo fixture is a product surface — APPROVE as a first-class deliverable.**
Two of the round's four headline findings were fixture artifacts (§0.1, §0.2), and both hid a
*differentiator*: the version-skew view no competitor can compute, and the ownership field that is the
#1 ask. An evaluator's estate is the fixture. Rule: **the default `contracts/artifacts/*` fixture must
exercise every honesty channel and every differentiator** — versions and a real skew, ownership and
contacts, `missingFeeds` on a service, a `degraded` descriptor, a topic genuinely uncovered by the
usage feed, and schema constraints. Cheap; directly on the industry-bar criterion.

### §6 — Sequenced backlog

Each item: the question, for whom, the data. **Gated** marks items waiting on data that does not exist
yet.

**R1 — "the absence release." One theme: no surface presents absence as evidence.** Nothing else ships
until this does.

1. **Usage coverage three-state + Value-page evidence correction.** *"Can I defend retiring
   `order:legacy-export`?"* — delivery owner, PO, architect. Data: today's rows for the UI half;
   §5.1's coverage declaration for the artifact half. **Ranked #1 in the product.**
2. **Render `missingFeeds` / `degraded` on every surface that makes a claim** — green service cards,
   service pages, fleet tiles. *"Is this healthy, or unwatched?"* — platform engineer, production
   support. Data: on the wire today.
3. **Snapshot age, prominent and loud past a threshold**, and the two clocks reconciled (declared
   `generatedAtUtc` vs live `generatedAt`). *"How old is what I'm looking at?"* — everyone. Data: today.
4. **The window control governs the numbers, or says it can't** (`countsWindowed: false`). *"What
   period is this?"* — delivery owner, production support. Data: today.
5. **Empty states that distinguish "no feed" from "no coupling"** — the topology panel, *"Declares no
   outbound calls"* (which `mesh.md` §2's `degraded: ["outbound-registry"]` already distinguishes and
   the UI does not), and the removal of advertised non-affordances (`#trace/<id>`, "7 events", bad
   routes rendering fleet). *"Is this estate decoupled, or unobserved?"* — architect, platform engineer.
6. **Scope-of-claim statements** — *"no consumers **declared in this estate**"*. Delivery owner:
   *"I'd rather be told the limit than infer it."* Data: none needed.
7. **Defect batch §5.8**, ComposePage inversion first.

**R2 — "make the contract legible."**

8. **Schema constraints rendered** (§5.4), and the response contract shown or its absence stated.
   *"What negative tests do I write / why is my message rejected?"* — QA, developer. Data: today.
9. **Mismatch substance from the aggregator** (§5.5). *"What exactly differs between these two
   consumers?"* — QA, developer, architect. Data: aggregator-derived, no spec change.
10. **Search over descriptions and field names.** *"Does a notification capability already exist?"* —
    BA. Today `email` returns "no match" while `customerEmail` is a field on two topics, and she
    nearly wrote a false negative — *"exactly the answer that gets a duplicate built."* Data: today.
11. **Version reconciliation** — a blank declared version defers to the live plane's version with
    provenance, never to `—` (§0.2). Data: today.
12. **Fixture uplift** (§5.9).

**R3 — "who, and where."**

13. **Registry contact block + discovery-provider population** (§5.2). *"Who do I tell / who do I
    wake?"* — all four jobs. Data: mesh registry, no spec change.
14. **Environment badge**, plus the approved one-line `placement.environment` spec addition (§5.6).
    *"Am I looking at production?"* — security, platform engineer.
15. **Dispatch audit trail + egress posture documented** (§5.6). *"Can I evidence this control?"* —
    security.

**R4 — "meaning, and the hand-off."**

16. **Topic and field descriptions rendered.** *"What does this system do, in English?"* — BA,
    delivery owner. **Gated** on the ports deriving `description` into the Contract Document's schema
    objects (legal today, populated by nobody). Data requirement filed on the .NET port; no spec
    change (§5.3 reasoning applies — schema-level `description` is already legal).
17. **Correlation-id copy + optional configured deep link out** to the trace backend (§4.6). *"Show me
    one real example of this failure"* — developer, production support, QA.
18. **Vocabulary pass + glossary affordance** — *plane*, *reserved*, *raw (benzene-message)*,
    *exemplar traces*, and one word for collector/aggregator/usage feed (§1.3).

**Deliberately deferred** *(each with its reason, so deferral is a decision)*

- **Usage history / trends over time** (outcome 4's second half): gated on a store mesh does not have,
  gated behind R1's coverage work, and one step from boundary §4.1. Not before both.
- **A topic-level `description` field in `contract-document.md`**: the schema's own `description` is
  usually the same sentence; a second place to say it is how specs bloat. Revisit only if R4.16 lands
  and the schema-level answer demonstrably isn't enough.
- **Server-side dispatch topic validation** (§5.6): dependency discipline.
- **Node-link graph scaling beyond small estates** (§3.4): the table and the queue scale; the canvas
  does not. Indefinite.
- **Threaded replies / resolution states on annotations**: still parked — but note outcome 6's
  promotion (§2), which raises the priority of everything else in that surface, starting with making
  the write path available in more deployments than the one the round saw (read-only).

**Rejected** *(with the reason, so it is not re-asked)*

- **Field-level data classification as a normative spec field** (§5.3), and inferred classification.
- **Ownership / rota in the ServiceDescriptor** (§5.2) — federate, don't own.
- **A trace or log viewer in mesh** (§4.6).
- **Any chart with a threshold; any alerting or paging** (§4.1, §4.2).
- **A separate HTTP path/method for dispatch** (§5.6) — contradicts R4.
- **Assertions, test collections or CI integration in the Test Console** (§1.4, §4.3).
- **Mesh as an authorization authority** (§4.4).
- **Response-body redaction in dispatch** (§5.6).

### §7 — Status honesty

- **Shipped and verified:** the issue inbox; the topic catalog; `MeshTopicVersionCompatibility` and the
  `VersionCompatibility` view; `MeshTopicChange` drift substance; annotations read+write; the dispatch
  gate's controls; the 60-second artifact refresh.
- **Shipped but not exercised by the round's estate:** version skew, ownership, `missingFeeds` beyond
  the topic strip, drift substance — all present in code, all invisible in `contracts/artifacts/` (§5.9).
- **Shipped but unverified against a real backend:** the Tempo adapter's metric and label names remain
  **documented convention, never checked against a live Tempo instance** — unchanged by this round and
  restated here because it keeps needing restating; the composite AWS/Azure usage-window behaviour is
  API-shape-correct only.
- **Not built:** usage coverage declaration; mismatch detail; the registry contact block; the
  environment badge; the dispatch audit trail; topic descriptions; the trace hand-off.

Cross-reference: the data-layer half of items 1, 9, 13 and 17 belongs in
`work/service-mesh-roadmap-1.0.md`; the one approved spec change (§5.6, `placement.environment`) is a
`docs/specification/mesh.md` §2 edit and moves with its conformance fixtures and the Go reference
implementation, per the standing rule.

---

## 2026-08-16 (later) — PRODUCT REFINEMENT 2, and the removal of discussion/annotations

Input: `work/mesh-feedback-round2-2026-08-16.md` (all eight personas rerun against a **composed rich
estate** — `owningTeam` on every service, the `payment:capture` / `shipping:book` v1/v2 split with
`versionCompatibility`), read against `work/mesh-feedback-round-2026-08-16.md` and its correction
block. This block **re-ranks the R1 backlog above** and records a maintainer decision to remove
discussion/annotations. It does not rewrite the R1 block; read that as history and this as what
moved.

Everything asserted here about current behaviour was checked in source at `benzene-ui` `3a61f05`,
`benzene-dotnet`, `benzene-typescript` and `docs/specification/**` before it entered a ranking.
Round 2's file:line citations were confirmed individually; where I found the finding to be *stronger*
than the pack stated, §A0 says so.

### §A0 — Verification of round 2's cited findings (all confirmed; three are worse than reported)

| Round-2 claim | Verdict |
|---|---|
| `TopologyGraph.tsx:71` — `const failing = e.errorRate != null && e.errorRate > errorThreshold;` | **Confirmed verbatim.** And the pack is right that the same file already has a third rendering: line 77 computes `unobserved` from `lastObservedAt` and draws the edge dashed, citing `mesh.md` §4.2 in its own comment. The vocabulary exists; the error encoding is still a two-arm ternary, so a null error rate paints `bz-edge-ok`. |
| `TopicCatalog.tsx:100` — a null status prints `ok` | **Confirmed.** `{!r.status && !r.schemaMismatch && <span className="bz-cat-none">ok</span>}` — the file is `src/components/containers/TopicCatalog.tsx`, not `sections/`. |
| `selectors.ts:921` — `errors: statsAbsent ? 0 : …` | **Confirmed, and worse than reported.** It sits in the *same object literal* as line 920, `observed: rows.length === 0 \|\| statsAbsent ? null : …`. One selector returns the correct honest null for one field and a manufactured zero for the next. A reviewer read past that; a type would not have. This single pair is the argument for §A3. |
| `selectors.ts:242` — per-version traffic fabricated | **Confirmed, and it violates a written contract rule.** `selectTrafficForTopic` filters `e.topic === topic` with no version predicate. `MeshUsageEntry`'s own remarks in `Benzene.Mesh.Contracts` state: *"A `null` dimension means the source's backend genuinely doesn't have it … not 'all' - consumers should surface the gap, not guess."* The UI guesses "all". This is not a data-layer gap; the data layer already wrote down the rule the UI breaks. |
| `#topic/<id>` carries no version; v2 unreachable | **Confirmed.** `routing.ts:19` is `topic: '#topic/'`; `selectTopic` (`selectors.ts:296-299`) is `topics.find(t => t.topic === topic)`; `TopicPage.tsx:30` uses it. `selectTopicEntries` (line 307) already returns *every* version and is used only by `selectHttpMappingsForTopic`. The fix is a route parameter and a selector swap — the data is already in the store. |
| Declared and observed producers merged into one unlabelled figure | **Confirmed, and worse than reported.** `TopicPage.tsx:84-86` renders `entry.producers.length === 0 ? 'none'` — declared only. The observed side is on the wire and *typed*: `FleetViewTopicsItem.providers` at `contracts/generated.ts:192`. It is referenced by exactly one file in the repo — `src/test/fleetView.ts:26`, a test fixture. **No production selector or component reads it.** The architect's line, "the UI has both halves and never joins them," is literally true at the type level. |
| Service-level `missingFeeds` parsed and dropped | **Confirmed.** The only `.tsx` references are `TopicLiveStrip.tsx:98,101`. |
| Capabilities duck-typed | **Confirmed.** `capabilitiesSlice.ts:32-34`, `typeof api.getFleet === 'function'` / `postAnnotation` / `sendMessage`. |
| `ServiceAbout` version row silently absent | **Confirmed.** `ServiceAbout.tsx:30`, `{about.version && <ValueRow …>}`. |
| `MessageComposer.tsx:53` renders `vv1` | **Confirmed.** `` {v.version ? `v${v.version}` : 'default'} `` over values already carrying the `v`. |

One correction to the pack's framing, not its facts: **finding 5 (two views of one contract
disagreeing) is not only fixture-origin.** Even with a correctly derived artifact, the product renders
`topics.json` and `services/<name>.json` side by side and never states which is authoritative. That
is a §1.2 provenance defect in its own right and it is cheap: label the source, once.

### §A1 — The R1 backlog, re-ranked (deviations flagged)

**Nothing in the R1 backlog is void.** This is worth stating plainly, because it is the opposite of
what the framing of the task anticipated. The R1 §0 correction block had *already* re-scoped the two
fixture artifacts (ownership, version skew) before they reached the backlog — ownership survived as
R3.13 "contact block, because one free-text string is too thin", version survived as R2.11
"reconciliation". Round 2 confirms both of those re-scopes were right. **The item that round 2
retires is a claim in the round-1 evidence pack, not a line in this backlog.** R1 §0's method —
verify before ranking — is what prevented the artifact from becoming work, and that is now
twice-validated practice, not caution.

What *does* move:

**Promoted into R1 (the absence release):**

- **NEW R1.0 — a Sources / wiring panel.** *"Is the collector unwired, or wired and broken?"* —
  platform engineer, production support, security. Today those two states are pixel-identical because
  capability detection is `typeof api.getFleet === 'function'` (`capabilitiesSlice.ts:32`), and there
  is **no wiring or diagnostics view anywhere**. `capabilitiesSlice` already computes what an operator
  needs. Outcome 0 ("know what this view cannot see") has been a principle with no home for two
  rounds; this gives it a page. Ranked first among the new work because it is the only item that makes
  the provenance guarantee *verifiable by a reader* rather than an internal discipline — the §1.5
  requirement ("the product must evidence its own controls") applied to data instead of security.
- **R2.11 → R1: version reconciliation, rescoped and split.** R1 wrote this as "a blank declared
  version defers to the live plane with provenance." Round 2 shows the real defect is larger and
  sits in the absence class: **per-version traffic is fabricated** (§A0), so a v2 shipped last night
  reads as 10.7k when the live plane says `invocations: 0`. Platform engineer: *"worse than showing
  nothing"* (architect). It violates a rule `MeshUsageEntry` already documents. **Deviation from R1
  sequencing, flagged:** this moves from R2 to R1 because it manufactures a measurement, which is the
  one thing R1 exists to stop.
- **NEW R1 — a topic version must be reachable.** `#topic/<id>` + first-match `selectTopic` means the
  page *raises a version-compatibility alarm about v2 and then refuses to show v2's payload*, though
  `topics.json` carries v2's `messageSchema`. An advertised non-affordance, which R1 §2 already ruled
  is an outcome-0 violation rather than a feature request. Joins R1.5.
- **R2.12 → R1: fixture uplift.** Round 2 *is* the controlled experiment for R1 §5.9. Same code, richer
  estate: the developer moved MAYBE→YES, production support moved from "second tab" to *"unreservedly,
  for the first sixty seconds"*, and round 1's single worst complaint became **the most-praised feature
  of either round**. A product surface that can flip two verdicts by changing a fixture is not a test
  input. **Deviation flagged:** R2 → R1.
- **NEW R1 (cheap) — label the ownership chip as ownership.** Round 2 retired "ownership is absent" but
  exposed something smaller and real: with `owningTeam` served, production support picked a rota from
  it and the delivery owner said *"the coordination list fell out of the tool rather than out of three
  Slack threads"* — while the BA looked at the same chip and concluded *"there is no owner, team,
  squad, or contact anywhere on any service page."* **Rendering is not communicating.** Two of three
  business-side readers got it; one didn't see it as ownership at all. This does not change R3.13's
  priority — the four-field contact block is still R3 — but the label is a one-line R1 fix.
- **NEW R1 (cheap) — say which of two contract views is authoritative** (§A0, finding 5).

**Confirmed and unchanged in position:**

- **R1.1 usage coverage + Value-page evidence** stays **#1 in the product**. Now hit by three personas
  across two rounds. The delivery owner's round-2 line is the sharpest evidence either round produced:
  *"the single decision I most wanted to take away from this tool is the one it talks itself out of."*
- **R1.2 render `missingFeeds` / `degraded` everywhere** — unchanged in rank, **transformed in
  quality**: round 2 converted a theme into a patch list of six named source lines (§A0). It is no
  longer a principle to apply, it is a set of edits to make.
- **R1.3 snapshot age / two clocks** — confirmed by 5 personas again, and **widened**: round 2's
  three-way health disagreement (manifest `unhealthy`, live plane `degraded`, page renders `Heartbeat
  healthy`, estate tile shows `0 DEGRADED` from the manifest alone) is the two-clocks defect wearing a
  health costume. Folded in rather than filed separately.
- **R1.4 window control**, **R1.5 empty states / advertised non-affordances** (now also carrying the
  flow dead end, `#flow/<id>` → `#fleet`, and `Producers: none`), **R1.6 scope-of-claim** — unchanged.
- **R2.8 schema constraints**, **R2.10 search over descriptions and fields** — unchanged.
- **R3.13 contact block**, **R3.14 environment badge**, **R3.15 dispatch audit**, **R4.16 descriptions**
  (still gated), **R4.17 correlation hand-off**, **R4.18 vocabulary** — unchanged. R4.17's *cheap* half
  (stop advertising drill-downs that don't exist; expose `x-correlation-id`) is already inside R1.5 and
  R1.7 and does not wait for R4.

**Raised within its release:**

- **R2.9 mismatch substance** rises to the top of R2. Round 2 supplied the reason: a human note filled
  the vacuum the tool left and filled it **wrongly** — it asserts *"the schema mismatch on
  `shipping:book` is the real issue to chase"* while `schemaMismatch` is `false` on every topic and
  that topic reports *"No schema published."* When the product names a problem and refuses to describe
  it, a human will describe it, and there is no mechanism to check them. That is a direct cost of the
  refusal, not a nice-to-have.
- **R1.7 defect batch** grows from 7 to ~12 and gains one item that is not a defect but a
  **credibility failure**: the Test Console's **transport selector is decorative** — `sendComposed`
  sends `{service, topic, headers, body}` and never transmits `transport`, so `http` and `raw` produce
  byte-identical dispatches. This product's usage promise is *"how often topics are exercised **and
  over which transports**."* A control that pretends to select a transport, in the product whose
  differentiator is transport-awareness, is worse than the other eight console bugs combined. It is
  fixed or it is removed from the screen; there is no third option. Also joining R1.7: the response
  panel rendering only `result.body` and hiding response headers (QA caught the stub by sniffing
  `x-correlation-id` on the wire — *"it manufactures false passes and conceals the evidence that
  they're false"*), and the `vv1` version label.

**Security conditions — deliberately not re-ranked.** Round 2 records *"YES with conditions —
conditions changed"* without itemising the new conditions. I will not re-rank R3.15 on a summary. The
three R1 conditions (audit trail, egress framing, environment identification) stand as approved;
the changed set is an open request back to the round's author.

### §A2 — RULING: declared-vs-observed divergence becomes a first-class product surface

**Decision: first-class. And it costs the Cloud Service spec nothing, because the spec already carries
it — the product ships one half and drops the mirror.**

The evidence for making it first-class arrived from two directions at once, which is the strongest
signal either round produced. The developer reached it from below: *"For a blast-radius tool,
'Producers: none' on a topic that two services are actively producing is the most dangerous single
string on the screen."* The architect reached it from above, on the undeclared `payments-api →
shipping-api` edge at 6.2/min that no contract explains: *"That is the single most interesting fact in
this estate and the UI has both halves and never joins them."* Same seam, opposite approaches, and
both named it as the thing mesh should own.

**The spec-tautness call, made explicitly and in mesh's favour:**

- `docs/specification/mesh.md` **§4.2 "Declared vs. observed — liveness and drift"** already defines
  both directions normatively: **Unobserved** (a declared edge with no trace parentage — *"a
  decommission candidate, not a fact"*, with a MUST that collectors report *last observed at* per edge
  rather than a boolean) and **Undeclared** (trace parentage on a topic absent from the caller's
  `produces` or the handler's `topics`). It closes: *"A view MAY render 'declared, unobserved' and
  'observed, undeclared' states distinctly from a confirmed … edge."*
- `mesh.md` **§4.1** reserves the `contract-drift` classification for *exactly* this case — *"the
  undeclared-edge case §4.2 defines … filed in this same shape by a collector **or reader**."* The
  spec has already granted the aggregator/UI permission to derive it.
- **The product ships the first half and not the second.** `TopologyGraph.tsx:77` computes `unobserved`
  and draws it dashed with a `mesh.md §4.2` citation in the comment. Nothing anywhere computes
  undeclared: `selectEdges` is `topology.json` only, and the live plane's observed producers
  (`FleetViewTopicsItem.providers`, `generated.ts:192`) are read by one test fixture and no
  production code.

So this is **zero spec change, zero conformance change, zero new obligation on any service, and no new
signal from anywhere.** It is a join over two artifacts already in the store. Measured against the
tautness bar, it is the single best-value item in either round: maximum insight from signal already
being emitted. **APPROVE.**

**Where it lives — settled by design history, not re-litigated.** The 2026-07-25 drains-up ruling
(top of this doc) already decided this: *divergence's home is the inbox; primary surfaces show one
best-available number with provenance one affordance deep, and the reconciliation classes stay.* That
gives the shape without a new argument:

1. **The inbox** gains the undeclared edge as a `contract-drift` row — *"`payments-api` produces
   `shipping:book` in traffic but does not declare it"* — with both sides of the evidence named. This
   is the surface the architect said is the only one that survives forty services (*"because it's a
   queue, not a canvas"*), so it is where the estate's most interesting fact belongs.
2. **In place, one affordance deep**: the topic page's Producers row stops being a bare declared list.
   Three states, never merged and never one unlabelled figure — *declared and observed*, *declared,
   not observed*, *observed, not declared*. `Producers: none` may only be rendered when the observed
   side has been checked and is also empty; otherwise it reads *"none declared — 2 observed"*.
3. **`CONSUMES` provenance.** Round 2's finding 4 — the mesh asserts `payments-api CONSUMES
   payment:capture v1` while that service's own `specJson` declares only `payments:get` and
   `payments:get-refunds` — is the same defect from the consumer side: an **inferred** relationship
   presented as a **declared** one. Developer: *"I'd have believed the wrong thing."* Same three-state
   labelling, same rule.

**One thing must be checked before building, and it changes the cost:** this doc's own 2026-07-25
block records the four reconciliation classes — *silent-but-declared*, *observed-but-undeclared*,
*unhealthy*, *stale* — as **SHIPPED** in `collectLiveIssues()` on the then-canonical
`mesh-ui.html`. **None of those five names appears anywhere in `benzene-ui` today** (grep-verified
across `src/`). `work/mesh-ui-react-assessment.md` §9 already records that the React port needed a
parity sweep because *"eight of the original's `render*`/`build*` functions had no counterpart at the
point the port looked finished … roughly 85% parity looks like 100% from the outside."* This is
consistent with a ninth. **Action: a parity audit of the React port against the pre-rewrite
capability list, before this is scoped as new work.** If it is a regression, the honest status is
*shipped, then lost in a rewrite, and reported by users as missing* — which is a worse defect class
than "not built", and one nobody has checked for elsewhere in the product.

**Placement:** R1 for the labelling half (three-state Producers, `CONSUMES` provenance — these are
absence-honesty and belong in the absence release); R2 for the derived undeclared-edge inbox class.

### §A3 — RULING: the provenance guarantee keeps its priority and changes its shape

The R1 standing rule — *no surface may present an absence as a measurement, and no claim may be
rendered at a wider scope than its evidence* — now has six source-cited violations.

**Priority: unchanged, and deliberately so.** It was already ranked above all four jobs and above every
outcome; there is nothing to promote it past. Restating that it matters would be the least useful
thing this refinement could do.

**Shape: changed, and this is the actual finding.** Six violations in one release is not a discipline
problem, and the proof is that the *correct* behaviour sits in the same files: `EdgeList` renders the
identical edge as `errors unknown`; `TopicLiveStrip.tsx:98` says *"not supplied by this plane"*;
`selectors.ts:920` returns a correct `null` for `observed` — on the line **directly above** the
manufactured `errors: 0`. The team knows how to do this and does it about half the time. The platform
engineer diagnosed it exactly: *"The product is honest in the places a human wrote a sentence and
dishonest in the places the code took a default, and I can't tell from the outside which kind of
screen I'm looking at."*

**A rule that must be remembered at every render site is not a guarantee. It becomes a mechanism, in
four parts:**

1. **A typed absence at the store boundary.** Any count derived from a feed is `number | null` in the
   selector's return type, never `number`, and a null is rendered by one shared component that owns
   what "unknown" looks like. This converts six independent judgement calls into one decision made
   once. Highest-leverage single change in either round, and it kills `selectors.ts:921` by
   construction.
2. **No two-arm encoding over a nullable.** `failing ? err : ok` (`TopologyGraph.tsx:71`) is correct
   that null isn't failing and wrong that null is therefore fine. Any good/bad visual encoding gets an
   unknown arm before it ships — the vocabulary already exists in the same file (`unobserved` →
   dashed). This is a shape a lint rule or a review checklist can catch; "be honest" is not.
3. **A surface that states what this view cannot see** — R1.0's Sources/wiring panel (§A1). The
   guarantee has been unverifiable from outside the product for two rounds. A reader who cannot check
   it has to take it on trust, which is precisely what the security reviewer said no reviewer does.
4. **A test, not a reviewer.** A store-level assertion that no selector returns a non-null count for a
   dimension its own `missingFeeds` declares absent. Six violations survived code review; they will not
   survive an assertion.

**And a lifecycle field must not wear a health word.** `TopicCatalog.tsx:100` printing `ok` for a null
status on a topic carrying 310 `service-unavailable` is a distinct sub-case worth naming: absence is
being rendered not as zero but as *reassurance*. Blank, or the word "none", never "ok".

**One more thing the round handed us, and it should be adopted deliberately: the standard already
exists inside the product.** The VERSION COMPATIBILITY panel's own caveat — *"upcasters aren't visible
to the mesh"* — was singled out by **four personas** as the honesty standard the rest of the product
should meet. That sentence is the reference implementation of this guarantee. Every surface that makes
a claim gets one sentence of the same kind, written by a human, in the same voice.

### §A4 — What VERSION COMPATIBILITY tells us about where to invest

It became the most-praised feature of either round the moment the fixture carried versions:
production support put it in a 3am escalation unprompted; the developer called it *"the killer feature
and it's already there"*; the architect and platform engineer both named it the reason they'd return.
Four conclusions, ranked.

**1. The differentiator is reconciliation, not display — invest in the class, not the panel.** Every
product in the comparison set (Datadog service maps, Backstage catalogs, Grafana, AsyncAPI Studio) can
show a service, a topic, a schema, a graph. **None can tell you the producers are on v2 while the
consumers are on v1**, because none of them knows what a topic version is. That is the whole "why
Benzene", and version compatibility is one *instance* of a class: *declared vs. declared*. §A2's
undeclared edge is the second instance — *declared vs. observed*. They are the same product idea and
they should be invested in as one. This retires any ambiguity about where the next increment goes.

**2. Insight-per-byte-of-spec is the metric, and this is the exemplar.** Version compatibility costs
the Cloud Service spec **nothing**: versions are already in the contract document, and the aggregator
derives the reconciliation centrally. That is the tautness thesis proven in the field — a small,
disciplined emission surface, a lot of insight, derived once. Every future proposal is now measured
against it: **does it derive, like version compatibility, or does it demand, like
ownership-in-the-descriptor (rejected, §5.2)?** Proposals of the first kind get a fast yes.

**3. Shipped-and-invisible is worth less than not-built, because it also costs credibility.** Both
rounds' headline complaints were about capabilities that ship. Round 2 is the controlled experiment
(§A1) — and note what the fixture was hiding: not polish, but the *two differentiators*. The default
`contracts/artifacts/*` estate is the demo of the product's reason to exist, not a test input.
Promoted to R1.

**4. Honesty is a feature users name out loud.** Four personas praised the caveat, not the number. That
is direct evidence that §A3 is not a hygiene tax paid against feature velocity — it is what made them
trust the figure beside it. Shipping more surfaces at the cost of provenance trades the asset for the
inventory.

**Concretely, the investment:** finish the version dimension end to end before adding any estate
surface. The most-praised feature in the product currently has **three holes** — v2 is unreachable
(§A0), per-version traffic is fabricated (§A0), and two of three services show no version at all
(`ServiceAbout.tsx:30`). A most-praised feature with three holes is the cheapest available win in the
entire backlog. Then build the second instance of the class (§A2). The 2026-07-25 STOP list — *no new
estate surfaces until the front door and issue detail exist* — still holds.

---

## §B — Removing discussion / annotations

**Maintainer decision, 2026-08-16: discussion/annotations is removed.** *"I don't think it can compete
with Teams and Slack."* That decision is not re-opened here. What follows is the product ruling on the
job it was doing, and the sequenced plan.

I record my own agreement only because it changes what the replacement must look like: the maintainer's
reason (it cannot compete) and round 2's evidence (it became **confident misinformation**) converge on
the same removal from opposite premises, and the second reason is the one that rules out ever hosting
the text somewhere cheaper.

### §B1 — RULING: the decision-record job survives; the hosted text does not

**What was praised in both rounds was never conversation.** It was a **durable, dated, attributed
decision attached to the artefact** — finance confirming a retirement; a drift classified as expected
and tied to `PAY-118`. R1 §2 promoted outcome 6 on exactly that evidence, and the promotion was for the
*record*, not the *thread*. The removal inventory reached the same conclusion independently: *"Nobody
asked for chat; they asked for provenance."*

**What round 2 showed is the price of hosting the text.** The same note the architect praised in round 1
asserts *"the schema mismatch on `shipping:book` is the real issue to chase"* — while `schemaMismatch`
is `false` on every topic and that topic reports *"No schema published."* It was listed among three
self-contradictions that *"would cost me the room."* A free-text human note drifted away from the
system it annotates and became confident misinformation **with no mechanism to detect it**.

That is decisive, and it is §1.2 wearing a human costume: the product presented an unverifiable
assertion at the same visual weight as a derived one. Mesh's entire pitch is *"documentation rots, the
running system doesn't lie."* Hosting prose inside the product re-imports the rot it sells against.

**Ruling: the decision-record job survives, in a cheaper form. Mesh points at provenance instead of
hosting it.**

- **Shape:** an optional, per-entity list of `{ label, url, dateUtc }` — a link to the Slack/Teams
  thread, the Jira ticket, the ADR, or the PR that already holds the decision. Mesh renders label, date
  and a link out, and **asserts nothing about the content.**
- **Why this is the right trade, not a consolation prize:** a link cannot drift into misinformation
  because it makes no claim — a stale link is visibly a pointer elsewhere, whereas stale prose reads as
  fact. It costs zero write path, zero auth, zero identity, zero moderation, and it keeps the static
  floor intact (a field in an artifact, not an endpoint). And it puts the decision where the
  organisation already keeps decisions, which turns the maintainer's reason for removal into the design
  of the replacement rather than a gap left behind it.
- **Where the data comes from — and the tautness call:** the mesh **registry**, operator-supplied,
  already outside the Cloud Service spec. This is the fifth field of the contact block **already
  approved in R1 §5.2** (team, contact URI, repo URL, runbook URL, + decision URL). **No Cloud Service
  spec change, no conformance change, no new obligation on any port.** It is therefore *not a new
  backlog item*: it rides **R3.13** as a one-field extension.
- **Not built now, and not a gate.** The removal does not wait for it.

**Explicitly rejected as replacements**, so they are not re-proposed: a lighter comment box (same
drift, less function); read-only threads kept as an archive (the round-2 failure was a *read-only*
thread — read-only is exactly the mode that produced the misinformation); and importing Slack/Teams
content into mesh (that is hosting the text again, with an integration attached).

### §B2 — What mesh must keep so the Value page does not lose its evidence trail

**The honest answer: the Value page's evidence trail was never the annotations.** R1 §0.5 established
that its evidence string is manufactured from an absent row. The thread was the only thing on that page
a human had checked, but it sat *beside* the evidence, not inside it. Removing it does not remove
evidence — it removes the appearance that the evidence had been reviewed.

So what must be kept is not a feature. It is **one sequencing obligation and two demo obligations**:

1. **R1.1 (usage coverage three-state + Value-page evidence correction) must land in, or before, the
   release that removes annotations.** This is the only genuine broken-intermediate-state risk in the
   whole removal, and it is a product risk, not a code risk. Today the demo's retirement arc is
   *deprecation badge + zero observed usage + a thread in which two named people agree to delete*. Strip
   the thread and what remains is a manufactured zero with **no human check at all** — a Value page that
   is *more* confidently wrong than the one round 1 called the product's most dangerous surface. Gate on
   this.
2. **Keep the four-tier ranking and its per-row evidence strings** (RAG per F2), corrected to §3.1's
   three coverage states. That is the evidence trail, it is derived, and it cannot rot.
3. **Re-stage the `order:legacy-export` arc in the fixture** (R1 fixture uplift, §A1): a topic genuinely
   *covered* by the usage feed with a measured zero, tiered red, next to one *not covered*, tiered amber.
   That is a strictly better demo than the thread was, because it demonstrates the differentiator instead
   of demonstrating a comment box.

### §B3 — Verified: it is NOT spec-pinned

I verified the inventory's decisive claim myself, because it materially changes the cost:

- `docs/specification/**` contains **no annotation contract**. The only `annotation` hits — `mesh.md:181`,
  `core-concepts.md:173` — are `[Message("topic")]` **attribute/annotation sugar**, a different sense.
- **No conformance fixture mentions it.** `grep -l` for `annotation` across `docs/specification/conformance/*.json`
  returns nothing.
- The spec's **reserved topic set** is `benzene:mesh`, `benzene:mesh:register`, `benzene:mesh:heartbeat`,
  `benzene:mesh:traces`, `benzene:mesh:issues`, `benzene:mesh:query:*`. **`benzene:mesh:annotations:add`
  is not among them** — it is an aggregator-host-local topic in the same family as
  `benzene:mesh:aggregate` and `benzene:mesh:report`, and `mesh.md:272` sets the precedent that
  host-local topics in that namespace are deliberately outside the contract.

**Confirmed: no spec change, no conformance-fixture change, no cross-language contract negotiation, and
no conforming service can break, because none was ever required to implement it.** Removal is per-repo
and can proceed at different times without drifting the spec.

**But the inventory understated two things, and both change the ordering and the cost:**

**(a) There is a fifth surface — this repo.** `docs/guides/mesh-ui.md` is the **language-neutral Mesh UI
guide**, and its own line 23 calls it *"the contract that keeps every one of those copies rendering a
consistent product."* It carries the `annotations.json` artifact row (line 75), §3.8 *"Annotations (read
on the static floor)"* (lines 160-162), the backend-gated write toggle (line 189) and two further
mentions (126, 158). `mesh-ui/README.md` documents `data-annotations-url` / `?annotations=`. And
`mesh-ui/mesh-ui.html` is the **canonical vendored build** that every port and the website demo
re-vendors from (guide §5). Leaving the guide describing a section the UI no longer has is precisely the
drift that guide exists to prevent — and it is what would let a future Go or Python port implement it.

**(b) `@benzene/ui` is a published component library and annotations are in its public API.**
`src/index.ts` re-exports `./components`, which exports **`Thread`** and **`Composer`**
(`components/index.ts:26-27`); `./store` exports `annotationsSlice`; and `scripts/verify-package.mjs:58`
**asserts the published store carries an `annotations` slice**, with lines 61-64 asserting
`capabilities.annotate === false`. This is a **breaking change to `@benzene/ui` 0.1.0** — a version bump
and a CHANGELOG entry, not a silent deletion. The inventory's "19 files" is 25 tracked files once the two
build scripts are counted.

**One directional constraint governs the whole plan:** the vendoring chain is one-way —
`benzene-ui` (source) → `Benzene/mesh-ui/mesh-ui.html` (canonical vendored copy) → `benzene-dotnet`,
`benzene-typescript`, `website/demos/mesh/`. Verified: `benzene-dotnet/src/Benzene.Mesh.Ui/mesh-ui.html`
is **byte-identical** to `benzene-ui/build/mesh-ui.html` (md5 `89fdbb58f33609b2b7dd820baad6230c`). The UI
must go first and the vendored copies must be refreshed in the same wave, or every port ships a page
that fetches an artifact its own aggregator no longer publishes.

### §B4 — The sequenced removal plan

**Step 0 — announce before removing. This is an obligation, not an optional courtesy.**
A deployment using this has its data in exactly one place: `annotations.json` in its
`IMeshArtifactStore`. That is **the one artifact in the product that cannot be regenerated from the
fleet** — the .NET publisher parks a corrupt log to a timestamped sibling rather than discarding it,
precisely for that reason. So:

- Deprecation notice one release ahead of the code removal, in `docs/guides/mesh-ui.md`,
  `mesh-ui/README.md`, `deploy/Mesh/README.md`, `examples/AwsMesh/README.md`, and the
  `Benzene.Mesh.Aggregator` / `Benzene.Mesh.Contracts` `CLAUDE.md` in both ports.
- **State plainly: export `annotations.json` before upgrading.** Mesh will stop publishing it and stop
  serving it. **No migration is provided and none is possible** — a paragraph of prose does not convert
  into a URL, and pretending otherwise would be a lossy import dressed as a migration.
- Name the replacement position in the same notice (§B1), so it reads as a decision rather than an
  amputation.
- **Do not delete anyone's `annotations.json` on upgrade.** The publisher stops writing; the file stays
  where it is. Silently deleting the only non-regenerable artifact would be the worst available removal
  behaviour.

**Step 1 — `benzene-ui`. One atomic change; nothing downstream starts until it merges.** Any subset
leaves the build red or the store half-wired, so these move together:

- `store/slices/annotationsSlice.ts`; the reducer in `store/store.ts:6,20`; the exports at
  `store/index.ts:11,13`.
- `selectThread` / `selectCanPost` / `selectCanAnnotate` in `store/selectors.ts` **and** their call
  sites (`TopicPage.tsx:3`, `ServicePage.tsx`) **in the same commit** — a dangling selector import is a
  build break.
- `components/sections/Thread.tsx`, `Composer.tsx`, both `.stories.tsx`, and the barrel exports at
  `components/index.ts:26-27`. Check `components/architecture.test.ts` for a barrel assertion.
- `data/meshApi.ts:158-159` (`getAnnotations`, currently **not** feature-gated — it fires a boot fetch
  that 404s on every static deployment without the artifact) and `163-168`
  (`postAnnotation` / `annotationsEndpoint`); the two optional members on `MeshApi`
  (`store/slices/estateSlice.ts:94-99`).
- `capabilitiesSlice.ts:33` (`annotate`) **together with** `scripts/verify-package.mjs:58,61-64` — that
  script asserts both the slice's presence and `annotate === false`, so leaving it fails package
  verification on a correct build.
- `App.tsx:5,46` (`loadAnnotations` on boot).
- `contracts/artifacts/annotations.json` **and** `scripts/generate-contracts.mjs:156` in one commit
  (the artifact→type map), then regenerate `src/contracts/generated.ts`. Split them and codegen fails.
- Tests/fixtures: `pages.test.tsx`, `catalog.test.ts`, `meshApi.test.ts`, `test/fakeMeshApi.ts`.
- **Version bump `@benzene/ui` 0.1.0 → 0.2.0 + CHANGELOG**, naming `Thread`, `Composer`, the
  `annotations` slice, the three selectors and the two `MeshApi` members as removed public API.
- **Do not grep-and-delete on `/annotation/i`.** Unrelated senses exist across the family — X-Ray
  annotations, Joi/Yup/Zod validation annotations in `benzene-typescript`, `Benzene.DataAnnotations` in
  .NET. Scope by file list, never by regex.

*Side benefit worth recording:* this removes the store's **only read-write data** (the slice's own doc
comment says so), which simplifies the security posture, the auth story and the static-hosting floor in
one move. Dispatch becomes the sole write path.

**Step 2 — re-vendor the build, same wave as step 1. This is the step that prevents the broken
intermediate.** Rebuild `benzene-ui` → refresh `Benzene/mesh-ui/mesh-ui.html` → refresh
`website/demos/mesh/`. Until this lands, every downstream copy still requests a dead artifact.

**Step 3 — the docs contract, in this repo, in the same PR as step 2.**

- `docs/guides/mesh-ui.md`: drop the artifact row (75), §3.8 (160-162), the write-toggle sentence (189)
  and the mentions at 126 and 158. **Add one line recording the position** — *decisions live in the
  organisation's own tools; mesh links to them, it does not host them* — so a future port does not
  re-add it.
- `mesh-ui/README.md`: remove the `data-annotations-url` / `?annotations=` option row and the artifact
  bullet.
- **No `docs/specification/**` change and no conformance change** (§B3). State this in the PR body so a
  reviewer does not go looking.

**Step 4 — `benzene-dotnet`. After steps 1-3; parallel with step 5. One PR, because the handler, the
topic constant and the DI registration reference each other.**

- `Benzene.Mesh.Aggregator`: `MeshAnnotationPublisher.cs`, `MeshAnnotationsMessageHandler.cs`,
  `MeshAggregatorTopics.AnnotationsAdd`, and the registration at `Extensions.cs:48-51`.
- `Benzene.Mesh.Contracts`: `MeshAnnotation.cs`, `MeshAnnotationLog.cs`, `MeshAnnotationRequest.cs`,
  `MeshAnnotationThread.cs`. Four types leave a package whose standing rule is *stay dependency-light*,
  and nothing else references them.
- `Benzene.Mesh.Artifacts/MeshArtifactMiddleware.cs`: drop `"annotations.json"` from the served
  allow-list (~line 114) and the doc comment (line 11). **Order note: do this after step 2 ships.** While
  old UI copies are in the wild, serving the file is harmless and 404ing it is a console error on
  someone's dashboard.
- `test/Benzene.Mesh.Test/MeshAnnotationsTest.cs` deleted in the same PR (10 tests; the suite moves
  ~211 → ~201). `test/Benzene.Mesh.Test` remains the reference suite — the count drops, the bar does not.
- Re-vendor `src/Benzene.Mesh.Ui/mesh-ui.html` from `Benzene/mesh-ui/`.
- Docs: `deploy/Mesh/README.md`, `examples/AwsMesh/README.md`, `docs/mesh-ui.md`, and the
  `Benzene.Mesh.Aggregator` / `Benzene.Mesh.Contracts` / `Benzene.Mesh.Ui` `CLAUDE.md` files —
  **including retiring the "P6 SHIPPED — discussion" narrative in `Benzene.Mesh.Ui/CLAUDE.md`**, so the
  package doc stops describing a section that no longer renders.

**Step 5 — `benzene-typescript`. Mirror of step 4; may run in parallel.** The same four
`Benzene.Mesh.Contracts` types, the same two `Benzene.Mesh.Aggregator` files, `Extensions.ts` and both
`index.ts` barrels, `dist/` regenerated, `src/Benzene.Mesh.Ui/mesh-ui.html` re-vendored. Same caution
about unrelated `annotation` senses (`Benzene.Joi`, `Benzene.Yup`, `Benzene.Zod`,
`Benzene.Aws.Lambda.XRay`, `Benzene.Mesh.Fleet.*`).

**Step 6 — Go and Python: nothing to do.** Verified zero mesh-annotation code in either; their only
`annotation` hits are Go validation tags and Python type hints. One line in the announcement: **two of
four ports never implemented it, and nobody filed for it** — the clearest available evidence that the
job was never the conversation.

**What must NOT be removed along the way:**

- The `IMeshArtifactStore` corrupt-artifact parking behaviour — a general durability property, not
  annotation-specific.
- The **feature-detection / degradation-ladder pattern** itself. `?annotations=` was one instance; the
  same pattern gates the fleet plane and dispatch and is load-bearing for the static floor.
- Any deployment's existing `annotations.json`.

**Risks, ranked:**

1. **Value-page regression** if the removal lands before R1.1 — the demo keeps a manufactured zero and
   loses its only human check. **Gate on R1.1** (§B2).
2. **Broken intermediate** if steps 1 and 2 separate — downstream pages fetch a dead artifact. Treat 1+2
   as one wave.
3. **Console errors on live dashboards** if step 4's allow-list change precedes step 2. Order 4 after 2.
4. **Silent public-API break** if `@benzene/ui` ships as a patch. Bump to 0.2.0 with a CHANGELOG entry.
5. **Collateral damage** from a regex sweep across four repos. Removal is by explicit file list.

### §B5 — Status honesty, updated

- **Shipped and verified:** VERSION COMPATIBILITY (now the most-praised surface in the product, once
  the estate carried versions); the issue inbox; `MeshTopicChange` drift substance; the dispatch gate's
  controls; edge liveness (`unobserved` → dashed, `mesh.md` §4.2).
- **Shipped but wrong:** per-version traffic (`selectors.ts:242` joins on topic only, against
  `MeshUsageEntry`'s own documented rule); `errors` manufactured as 0 when the stats feed is absent
  (`selectors.ts:921`); `ok` printed for a null lifecycle status (`TopicCatalog.tsx:100`); the Test
  Console's transport selector, which transmits no transport at all.
- **On the wire, typed, and read by nothing:** `FleetViewTopicsItem.providers` (`generated.ts:192`) —
  the observed half of §A2, referenced only by a test fixture.
- **Recorded as shipped, absent today, unaudited:** the four live reconciliation classes
  (`collectLiveIssues`, 2026-07-25 block). Parity audit owed (§A2).
- **Shipped but unverified against a real backend:** the Tempo adapter's metric and label names remain
  **documented convention, never checked against a live Tempo instance** — restated again because it
  keeps needing restating; the composite AWS/Azure usage-window behaviour is API-shape-correct only.
- **Being removed:** discussion/annotations (§B). Not spec-pinned (§B3, verified).
- **Not built:** the Sources/wiring panel; usage coverage declaration; mismatch detail; the registry
  contact block (now +1 field, §B1); the environment badge; the dispatch audit trail; topic
  descriptions; the trace hand-off.

Cross-reference: the data-layer halves — the usage coverage declaration, mismatch detail, the
undeclared-edge derivation (§A2) and the contact block's decision URL (§B1) — belong in
`work/service-mesh-roadmap-1.0.md`. **No Cloud Service spec change is required by anything in this
block**; §A2 is served entirely by `mesh.md` §4.1/§4.2 as they already stand, and §B removes a surface
the spec never carried. The one approved spec change remains R1 §5.6's `placement.environment`.

---

## 2026-08-16 (later still) — PRODUCT REFINEMENT 3: breaking changes and contract drift

Input: `work/mesh-feedback-round3-2026-08-16.md` (all eight personas, one question — *can you tell what
changed, and whether it breaks you?* — over a purpose-built drift estate), plus two direct maintainer
observations from a live AWS deployment of the .NET mesh example: that drift is flagged, unclickable
and unexplained, and that the service page "could do with some boxes."

This block **promotes** the roadmap's mid-term *Phase 4 field-level compatibility* item (`:686`,
`:836`) into the next shipping wave, and **corrects the evidence pack on two load-bearing points**
(§C1.3, §C2.2). It does not rewrite the R1 or R2 backlogs; both stand, and §C7 says where this wave
sits relative to them.

Everything asserted about current behaviour was checked in source before it entered a ranking —
`benzene-dotnet` (`Benzene.Schema.OpenApi/Compatibility`, `Benzene.Mesh.Aggregator`,
`Benzene.Mesh.Contracts`, every `.csproj` in the aggregator's closure), `benzene-ui` `3a61f05`,
`benzene-typescript`, `benzene-go`, `benzene-python`, and `docs/specification/mesh.md`. Where I found
the pack wrong, §C1 and §C2 say so; the pack's own five discarded harness artifacts stay discarded and
none of them appears in the backlog.

### §C1 — The reframe: detection ships; what is missing is a wire, a screen, and a *pair*

**The maintainer asked for a capability that already exists.** `Benzene.Schema.OpenApi/Compatibility`
is a complete, direction-aware, field-level engine: `SchemaChange` carries `Kind`, `Direction`,
`Topic`, `Path` (`order:create.request.customerId`), `Description` and `Compatibility`;
`SchemaCompatibilityRules.DefaultFor` encodes the producer/consumer asymmetry;
`SchemaCompatibilityReport` rolls up to `Overall` / `HasBreakingChanges`. **No `Benzene.Mesh.*`
project references it** — verified again this round. The product's answer has existed as a
fully-formed C# type and has never been connected to a screen.

Worth recording because it validates the engine's design: the maintainer's own stated intuition —
*"if a consumer consumes a subset, that's likely to not be a breaking change; whereas if they're
trying to consume something that no longer exists, that is the sign of a breaking change"* — **is
`SchemaCompatibilityRules.DefaultFor` verbatim.** `PropertyRemoved` is `Breaking` on Response/Event
(the consumer may read the removed field) and `Warning` on Request (the service ignores a field the
client still sends). The rule table already thinks the way the maintainer thinks. That is not a
coincidence to gloss over — it is the reason this is a *wiring* job and not a research job.

**So the scoping consequence, stated plainly: this is not a detection feature, it is a
presentation-and-plumbing feature, and it must be scoped and estimated as one.** Roughly one week of
aggregator work and two of UI, not a quarter. Any plan that reads as "build breaking-change analysis"
is mis-specified. Three sub-rulings follow.

**§C1.1 — What the mesh computes today is a different diff, and it is structurally incapable of
answering the question.** `MeshAggregator.DiffTopicEntry` (`MeshAggregator.cs:567`) keys on
`(Topic, Version)` and compares canonicalised **strings**. Two consequences, both confirmed:

- it compares *v2 today against v2 yesterday*, never *v2 against v1* — so the user's actual question
  ("does the new version break my consumers?") has no code path at all; and
- `"Payload schema changed (request)"` is not a terse summary of a known delta, it is the complete
  extent of what a string comparison can know.

**§C1.2 — The engine's entry points do not fit, and the gap is small and specific.**
`SchemaCompatibilityComparer.Compare` takes two `EventServiceDocument`s and indexes requests by
`RequestKey` = `$"{Topic}@{Version}"` (`SchemaCompatibilityComparer.cs:205`). Pointed at one document
across two versions it would emit `TopicAdded`/`TopicRemoved` per version, not property diffs. The
recursive worker that actually does the job — `CompareSchemas`, lines 106-177 — is `private`, operates
on `OpenApiSchema`, and needs nothing but `Type`, `Format`, `Properties`, `Required`, `Items`. The
mesh holds `System.Text.Json.Nodes.JsonObject` with `$ref`s **already inlined** (`MeshTopicEntry`'s
own remarks), so it does not even need the `Resolve` step. **A narrower, public, schema-pair entry
point is required, and it is a small additive change.**

**§C1.3 — CORRECTION to the pack: the aggregator does *not* already carry comparable dependency
weight, and the proposed resolution needs amending.** The pack ruled *"compute in
`Benzene.Mesh.Aggregator`, emit a thin serialisable result into `Mesh.Contracts`"* on the premise
that the aggregator "already carries a comparable dependency weight." Verified against every
`.csproj` in its closure — `Benzene.Mesh.Contracts`, `Benzene.Abstractions.MessageHandlers`,
`Benzene.Results`, `Benzene.Core.MessageHandlers`, `Benzene.Http`, and everything they reference:
**`Benzene.Mesh.Aggregator` has zero third-party `PackageReference`s, transitively.**
`Benzene.Schema.OpenApi` would add five, including `Swashbuckle.AspNetCore.SwaggerGen` — an ASP.NET
Core Swagger generator — to a component that runs in Lambda and Functions hosts.

**Ruling: the second half of the pack's resolution is APPROVED, the first half is AMENDED.**

- **APPROVED, unchanged:** the computation lives aggregator-side, and `Mesh.Contracts` receives a
  **thin serialisable mirror** — five scalar strings per change (`kind`, `direction`, `path`,
  `description`, `compatibility`) with no reference to the engine. `Mesh.Contracts` keeps its single
  `Benzene.HealthChecks.Core` reference. This is right and is not re-opened.
- **AMENDED:** `Benzene.Mesh.Aggregator` **must not reference `Benzene.Schema.OpenApi`.** Two shapes
  are viable; the choice is an implementation call, but the invariant is not:
  1. *(preferred)* extract the taxonomy and the rules — `SchemaChangeKind`, `SchemaDirection`,
     `ChangeCompatibility`, `SchemaCompatibilityRules`, `SchemaChange`, `SchemaCompatibilityReport`
     — plus a `JsonObject`-level walker into a new **dependency-free `Benzene.Schema.Compatibility`**;
     `Benzene.Schema.OpenApi/Compatibility` keeps its entire public API and becomes an adapter over
     it. The aggregator references only the new package.
  2. *(fallback)* extract the taxonomy and rules only; the aggregator carries its own ~120-line
     `JsonObject` walker.
- **The invariant, which is the part that matters: one rules table, one taxonomy, one verdict.** Two
  walkers are tolerable; two rule tables are not, because a verdict that differs between the CI gate
  and the mesh screen destroys both. If shape 2 is chosen, a test must run both walkers over the same
  schema pair and assert identical change sets.
- **This also buys portability, which shape 1 gets for free.** A verdict that lives inside an
  OpenAPI-and-Swashbuckle .NET package cannot be mirrored by the TypeScript aggregator, which also
  builds `topics.json`. A JSON-Schema-level walker can.

### §C2 — Temporal drift vs cross-version compatibility: there are *three* diffs, not two, and the middle one is far cheaper than the pack thought

**§C2.1 — Cross-version compatibility (v2 against v1, inside one snapshot). SHIP FIRST, ALONE.**
Both versions' schemas are already in `topics.json`, in the same document, in the same run
(`MeshTopicEntry.RequestSchema` / `ResponseSchema` / `MessageSchema`, `$ref`s inlined). No history, no
storage decision, no wire-shape change beyond the result field. **This is the half the users want:**
not one of the eight personas asked how a topic differed from yesterday; every one asked whether the
new version breaks the old consumers.

*Which of the maintainer's words this answers:* **"whether or not it's a breaking change or not"** —
completely, and this is the only diff with a consumer on the other side of it.

**§C2.2 — CORRECTION to the pack: field-level *temporal* drift on topics costs almost nothing
either.** The pack ruled temporal drift expensive because `MeshServiceSnapshot` carries `SpecJson`,
`SpecHash` and `PreviousSpecHash` and **no `PreviousSpecJson`** (confirmed, line by line). That is
true — **and it is about the wrong artifact.** `ApplyCatalogDiffAsync` (`MeshAggregator.cs:~520`)
already does `await _store.TryReadAsync("topics.json")` and deserialises the **previous catalog**,
so `previous.RequestSchema` is a live `JsonObject` **on the exact line that currently performs
string equality**. Both sides of a field-level temporal diff are in memory, today, at the point of
the comparison. Zero wire change, zero storage change, zero new dependency beyond §C1.3's — the same
engine call, on a different pair.

*Which of the maintainer's words this answers:* **"when there is drift, it doesn't tell you what the
drift is"** — at the level a reader acts on, which is a topic and a field.

And it yields a class this product should own outright and nothing else can compute: **a payload
schema that changed *under the same version number*.** No version bump, no compatibility panel, no
consumer warned. That is the most dangerous change shape in the estate and it falls out of §C2.2 for
free. It gets its own class, ranked above `Breaking`.

**§C2.3 — Service-level spec drift substance (`PreviousSpecJson`). REJECT for this wave, with a
revisit trigger.** This is the expensive one the pack correctly priced: a `Mesh.Contracts` wire
change, a retention decision, roughly a doubling of snapshot artifact size, and a mirror obligation.
It is also the one with the **worst insight-per-byte** in the set, because once §C2.2 exists the
service-level `DRIFT` badge is re-derivable as a *rollup of its own topics' field-level changes*, and
the residual — a spec that changed with no payload schema change — is honestly describable in one
sentence without carrying a second copy of every spec (§C4.4). Measured against §A4's standing bar —
*does it derive, or does it demand?* — §C2.1 and §C2.2 derive; this demands.
**Revisit trigger, written down so this is a decision and not a refusal:** if adopters report the
residual case (spec moved, no topic schema moved) is frequent *and* material, revisit — and even then
prefer storing a canonical *topic-projection* of the previous spec over the whole document.

**Sequencing, then: C2.1 → C2.2 → (not C2.3).** C2.1 and C2.2 share the engine, the result type and
most of the UI; C2.1 ships alone because it is the question that was asked, and C2.2 follows in the
next aggregator PR. **The rollup in §C2.3 is what finally joins the two halves the pack found were
never joined** — service-level `contractDrift` and topic-level `changes`, different code paths,
different pages, different vocabularies, counts that disagree (**1** vs **4**), no route between
them. After C2.2 they are one derivation and cannot disagree.

### §C3 — The severity question: mesh ships a classification, and a verdict that is always attributed

The engine gives one scalar. The pack's *"Where personas disagree"* section shows six roles with six
non-nesting definitions of "breaking", and — decisively — **the two changes the engine ranks lowest
are the two that most alarmed the BA and the security reviewer**: `PropertyRemoved` on a request is
`Warning` (`address.line2` — *"parcels to blocks of flats with no flat number"*), and a rename is
mechanically `PropertyRemoved` + `PropertyAdded`, each individually unremarkable, while every control
keyed on the old field name silently stops firing.

**Ruling: both, in a fixed order of prominence, and the order is the ruling.**

1. **The classification is primary.** `Kind` + `Direction` + `Path` — *which named field, which
   direction, added/removed/renamed/retyped*. This is the one layer all eight personas agreed on,
   because each role derives its own consequence from it, and it is the layer that does not have to
   be right about anyone's definition of "breaking". It already exists on `SchemaChange` in exactly
   this shape.
2. **The verdict is secondary and always attributed.** Never a bare `Breaking`; always *"Breaking, by
   Benzene's default rules"*, with the rule for that kind+direction available one affordance deep.
   This is not decoration: `SchemaCompatibilityRules` is explicitly user-configurable and ships a
   `Strict()` alternative, so **the verdict is a function of a rule table, not a fact about the
   world.** Saying so converts an argument into a setting.
3. **The estate rollup is a count by class, never a single tick.** `3 breaking · 1 warning · 2
   compatible · 4 not compared`. A green estate-level all-clear is forbidden (§C5).

**What it must never claim** — and these go in the product copy, not just this document:

- Never **"safe"**, **"no breaking changes"**, or **"compatible"** unqualified. The verdict's scope is
  *structural, schema-only, within this estate*.
- Never that a change **will** break a named consumer. Mesh knows who is on which version
  (`MeshTopicVersionCompatibility`) and what changed; it does **not** know which fields a consumer
  reads. Joining those into "this breaks `orders-api`" is a claim it has not earned. **REJECTED:
  per-consumer impact prediction.**
- Never contradict the four things it structurally cannot see, which get one human sentence on every
  compatibility surface, in the §A3 voice the personas already praised:
  > *"This compares published payload schemas only. It cannot see upcasters, what a field means, or
  > consumers outside this estate — a change marked compatible can still break something."*
- **`TypeChanged` stops the walk.** `SchemaCompatibilityComparer.cs:119` returns early on a type
  change (*"fundamentally different types — no point diffing their members"*), so a type change on an
  object **hides every change beneath it**. The UI must say so at that node — *"the type changed
  here, so fields beneath it were not compared"* — or the count is a floor presented as a total.

**Rename gets a labelled hypothesis, not a Kind.** The engine has no rename concept and should not
grow one — inferring intent from a coincidence is exactly the §5.3 trap. But a `PropertyRemoved` and a
`PropertyAdded` at the same parent path with identical type and format is a cheap, honest pairing:
render them together, badge it **"possible rename"**, keep both underlying changes visible, and do
not let it alter the verdict. UI-side, zero wire cost.

### §C4 — The UI design

The chain every persona found broken: **estate says drift → which service → which topic → which
version → which field → breaking or not.** Each hop below is named with what it shows, what is
clickable, and where the click goes.

**§C4.0 — The change ledger: a new *view*, no new data object.** The architect asked for a
severity-ranked estate ledger. **Ruling: a new route and a new page, backed entirely by a selector
over `topics[].compatibility`.** No new artifact, no new store slice, no new aggregator output beyond
the field §C1.3 already approves. It is a route rather than a filter on `TopicCatalog` for two
reasons: the catalogue is keyed on `(topic, version)` while a change is about a *pair* of versions,
and the catalogue is already seven columns wide. It follows the IA the architect already praised —
*"a queue, not a canvas"* — and reuses the existing "see all N →" section-head pattern
(`FleetPage.tsx:82`, `#issue/all`), so it costs the navigation model nothing new.

**§C4.1 — Estate page (`#fleet`). Two changes.**

- **The `Contract drift` tile is re-based and made navigable.** Today `summary.drift`
  (`selectors.ts:62-67`) counts **services** whose spec hash moved; the topic-level `changes` count is
  a different number on a different page — which is why the personas saw **1** and **4**. One
  definition, one number: the tile becomes **`CONTRACT CHANGES`**, valued as the count of `(topic,
  version)` entries carrying at least one change, `rag: red` when any is breaking, `amber` otherwise.
  After §C2.3's rollup the service badge is derived from the same set and cannot disagree.
- **`EstateStat` gains `onClick?: () => void`.** This is a change to a primitive shared by all five
  tiles, and the pack is right that it needs care — but the BA clicking a dead `DRIFT` badge four
  times establishes that the *reverse* defect is worse. Rule: a tile with `onClick` renders as
  `<button class="bz-stat">` with real hover/focus/cursor affordances; a tile without one stays a
  `<div>` and gains nothing. No tile becomes falsely clickable. Destination: `#changes`.
- **A `Contract changes` preview section**, between `Needs attention` and `Services`, showing the top
  five changes ranked (unversioned-change → breaking → warning), each row deep-linking to its topic
  at its version, with `see all N →` to `#changes`.

**§C4.2 — NEW: the Changes page (`#changes`).** *"What moved in this estate, and does any of it break
someone?"* — architect, delivery owner, QA, release morning.

- Head: **Contract changes.** Lede: *"What changed in the estate's payload contracts, and whether it
  breaks a consumer."* Provenance line directly beneath, always: *"Comparing each topic's newest
  published version against its previous one, in the catalogue published at `<generatedAtUtc>`."*
- Filters: **class** (unversioned change / breaking / warning / compatible / not compared), **side**
  (request / response / message), **service**, and free text matching **field paths** — which is also
  the cheapest partial answer to R2.10 (`email` currently returns nothing while `customerEmail` is a
  field on two topics). Default: unversioned + breaking + warning.
- Rows, grouped by topic then version pair, one row per `SchemaChange`:

  `[BREAKING]  orders:create   v1 → v2   request   customerId   Property 'customerId' was removed`

  Field path in monospace with the topic prefix stripped. The class badge carries a glyph, never
  colour alone. The topic name links to `#topic/orders:create@v2`.
- **Three distinct empty states, and they are the point (§C5):** nothing changed / not computed /
  filtered to nothing. They are never the same string.
- The **"Since the previous snapshot"** toggle appears **only when §C2.2 has shipped**. Until then
  there is no toggle — an advertised non-affordance is an outcome-0 violation (R1 §2).

**§C4.3 — Topic page: the version dimension, and the centrepiece.**

- **Route.** `#topic/<topic>` (newest version) and `#topic/<topic>@<version>` (specific). Parse on the
  last `@`. Precedent exists: `#test/<service>/<topic>` already parses a compound key
  (`routing.ts:31-48`), so this is not a new routing concept.
- **`selectTopic` stops returning the first match.** `topics.find(t => t.topic === topic)`
  (`selectors.ts:296`) returns the *lowest* version, because the aggregator orders `ThenBy(Version)`.
  The security reviewer's finding is the decisive one: a DPIA driven off `#topic/shipping:book`
  records that the flow carries `address.line2`, untrue at v2 — *"a data map that is confidently
  wrong is more dangerous than no map, because it gets signed."* Default to the **newest** version;
  render a **version switcher** in the page head; every catalogue row links to its own version.
  `selectTopicEntries` (`selectors.ts:307`) already returns every version and is currently used by
  one selector.
- **NEW section `Changed from v1`, placed directly above `Payload`** — above traffic, because it is
  the deciding content on this page.
  - Header line: `2 breaking · 1 warning · 1 compatible` as glyph-bearing chips, plus a `compare
    with ▾` selector when three or more versions exist.
  - The change list, one row per `SchemaChange`, same shape as §C4.2.
  - **And the highest-value render in this whole design: the `Payload` schema tree is annotated in
    place.** `SchemaTree` gains per-node markers keyed on `SchemaChange.Path` — **removed** (rendered
    from the baseline, struck through), **added**, **now required**, **type changed (was
    `integer`)**, **not compared below here**. This is the direct answer to the maintainer's *"it's
    difficult to envisage where the drift is and exactly what the drift is"*: the drift is shown
    **on the contract itself**, at the field, not in a list beside it. Everything else in this design
    routes a reader to this view.
- **The existing `Changes` section (`TopicPage.tsx:144-149`) is kept and relabelled `Since the
  previous snapshot`.** It is currently dead code for every changed topic — v1 carries `changes: []`
  and v2 has no reachable page — and it becomes live the moment the route lands. Relabelling is
  mandatory: a reader must never confuse *"changed against v1"* with *"changed since yesterday"*.
- **`VersionCompatibility` stays exactly where it is and keeps its caveat.** It was the most-praised
  surface of round 2 and it answers a different question well (*is anyone still on the old
  version?*). It gains only the §C5 third arm.

**§C4.4 — Service page: the drift line becomes a finding instead of a checksum.**

- The head `drift` badge becomes clickable → `#changes` filtered to this service.
- `ServiceAbout`'s drift row (`ServiceAbout.tsx:36-43`) stops leading with the hash pair. Copy:
  `Contract drift — 3 changes across 2 topics, 1 breaking · view changes`. When the spec moved but no
  payload schema did: *"The published spec changed, but no payload schema changed."* The two hashes
  survive as detail text one affordance deep — they are a fine audit token and a category error as a
  finding. The delivery owner's line stands: *"not a number I would defend, a number I would be
  laughed at for showing."*

**§C4.5 — Value page: get the best string in the product out of a tooltip.** `RetirementRow.tsx:37`
renders change descriptions as `<Chip title={change.description}>` — hover-only, unscreenshottable,
unlinkable, invisible to a keyboard user and to a projector, on the one page six personas
independently named the best thing in the product. The chips become visible text with a class badge,
linking to the topic at its version. Half a day.

**§C4.6 — What I am NOT building.**

- **No side-by-side raw JSON diff.** Two JSON blobs is the hash pair one level up: it re-delegates
  the cognitive load the maintainer explicitly asked the tool to carry.
- **No git-style patch view**, no unified diff, no line numbers. The unit is a *field*, not a line.
- **No per-consumer impact prediction** (§C3).
- **No breaking-change gate, alert, or release block.** The engine already ships
  `SchemaCompatibility.EnsureBackwardCompatible` for CI — that is the right home, in the service's own
  test suite. Mesh reporting what is, and CI enforcing what must be, are different products; merging
  them takes mesh across boundary §4.1 by a new door.
- **No rename as an engine `SchemaChangeKind`** (§C3).
- **No `PreviousSpecJson`** (§C2.3).
- **No new backend, no new endpoint, no external request.** Verified against the constraint: every
  screen above is a render over `topics.json` plus one new hash route. **The self-contained /
  no-CDN / no-build / statically-hostable floor is untouched by this entire wave** — which is worth
  stating because it is the first wave in three rounds where that was in no doubt.

### §C5 — The honesty rules, and the third state

Round 3's closing finding is that this product's first obligation here is **never to state a verdict
it did not earn**. The live instance is `MeshTopicVersionCompatibility.IsCompatible =>
ProducedNotConsumed.Length == 0` — verified — which returns `true` for a topic with **no in-estate
producer**, because an empty set has nothing left over. That is the shape of every HTTP-fronted
topic, and in the round-3 estate it fired on `orders:create` (a renamed required field plus a new
required field) and `orders:get-all` (a deleted response field): **the two most dangerous changes got
the all-clear.** The boolean is not wrong — it is *vacuously true*, and the UI renders vacuous truth
as reassurance.

**The third state is named `not compared`.** It is never `ok`, never a green tick, never blank, and —
this is the structural part — **it is a value on the wire, not an absence.**
`MeshTopicCompatibility.Overall` is one of `compatible` / `warning` / `breaking` / `not-compared`,
with a `notComparedReason`. That is §A3's "typed absence at the store boundary" applied to a verdict:
one decision made once, rather than a judgement call at every render site.

Copy, per cause — each one a human sentence, in the voice four personas already singled out:

| Cause | Copy |
|---|---|
| Only one version published | *"Only one version of this topic is published, so there is nothing to compare."* |
| A side's schema is absent on one version | *"No request schema is published at v1, so the request side was not compared."* |
| `TypeChanged` stopped the walk | *"The type changed here, so fields beneath it were not compared."* |
| Aggregator did not publish comparisons | *"This estate's aggregator did not publish contract comparisons, so no verdict is available."* — **never** "no changes" |
| No in-estate producer (the `isCompatible` fix) | *"No service in this estate declares producing this topic, so there is nothing to reconcile. Its producers may be outside the estate — a website, an app, or a partner."* |

**The `isCompatible` fix is UI-side and costs nothing.** `VersionCompatibility.tsx` renders the third
arm whenever `producedVersions.length === 0`, instead of *"Every version produced in the fleet has a
matching consumer."* No wire change: the boolean is correctly named for what it computes, and the
defect is the sentence wrapped around it. `MeshTopicVersionCompatibility`'s doc comment gains one line
naming the vacuous case, in the same wave, so the next reader of the type is not misled either.
The file's own comment (`VersionCompatibility.tsx:27-28`) already states the principle — *"painting
'compatible' over a check nobody ran would be worse than silence"* — and guards only the **absent**
entry. This extends the existing guard by one condition; it does not introduce a new idea.

**And the estate tile obeys the same rule.** If `compatibility` is absent from the artifact, the tile
shows `—` and reads `CONTRACT CHANGES · not computed`. A `0` there would be the R1 §0.5 defect
("absence rendered as good news") landing on the exact question the maintainer asked about.

### §C6 — The service-page grouping: in scope, and here is the grouping

**Ruling: IN SCOPE for this wave, bounded to a regrouping — not a redesign.** It would be defensible
to file it separately, and I am not doing so, because all three costs the pack locates land **on this
round's question**: the contract material is split by ~450px of liveness telemetry; `Contract drift`
renders in the same typographic weight as `Snapshot taken` directly above it (*"the one section that
decides a release blocker should not be indistinguishable from a timestamp"*); and six peer headings
made the BA read `shipping:book v2` as a *call* rather than something the service **produces** — *"a
meaningfully different statement."* A reader who cannot find the contract on the service page cannot
enter the chain in §C4 at all. Verified: `ServicePage.tsx` renders **eight sibling `<section>`s** with
bare `<h3>`s and no wrapper; only Issues and Discussion land in visible cards, which several personas
read as *"the only real content."*

Note the contrast three personas drew unprompted: **the estate page already groups into cards and is
genuinely scannable** — *"the estate page reads like a product, the service page reads like a data
dump."* This is not a missing design language. It is one page that never got it.

**The grouping — five cards, in this order:**

1. **Contract** — description, service version, contract-change summary, `Consumes`, `Produces`.
2. **Calls** — `Outbound`, `Inbound`. Deliberately its own card rather than merged with Contract:
   the BA's error was reading a *produced topic* as a *call*, which merging would entrench.
3. **State** — status, health checks, live heartbeat, feed health, **and `Snapshot taken`, which moves
   here from About** — it is a liveness fact, not a contract fact, and its adjacency to `Contract
   drift` is what made the drift line read as a timestamp.
4. **Traffic** — usage, flows.
5. **Issues** — unchanged; already a card.

**Two supporting rules.** A `Card` primitive (`<section class="bz-card">` + `h3` title + optional
actions slot), reusing the existing surface/border/radius/shadow token set that `.bz-stat` and the
service cards already use — the design language exists, it is only unapplied. And the heading
hierarchy: **card title = `h3`, subsections inside a card = `h4`.** Once the cards are visually
bounded, six peers become two groups of two, which is exactly the maintainer's *"things that go
together naturally to the eye appear to go together."* Within **Contract**, the change line gets its
own emphasis treatment with a class badge — it is the line that decides a release.

**Sequencing note:** §B removes the Discussion section from this page. Do the grouping **after or
within** the §B removal wave, or the card work is redone. Eight sections become seven.

### §C7 — Ranked backlog

Sizing is rough and assumes the §C1 reframe (plumbing, not research). Repo tags: **[ui]**
`benzene-ui`; **[agg]** `benzene-dotnet` aggregator/schema packages; **[wire]** `Mesh.Contracts`,
therefore mirrored by the TypeScript port; **[spec]** `docs/specification/**` — **there are none**
(§C8).

**Wave C1 — "say what changed, and whether it breaks." The shipping unit.**

| # | Item | Repo | Size |
|---|---|---|---|
| C1.1 | **Narrow schema-pair entry point + taxonomy extracted to a dependency-free home** (§C1.2, §C1.3) | [agg] | 2–3 d |
| C1.2 | **Cross-version compatibility computed in `MeshAggregator`** — newest version against its predecessor, per topic (§C2.1) | [agg] | 2 d |
| C1.3 | **`MeshTopicCompatibility` + `MeshSchemaChange` result types**, loose-string convention per `MeshTopicChangeKind`, `Overall` including `not-compared` (§C5) | [wire] | 0.5 d + 0.5 d TS mirror |
| C1.4 | **Fixture uplift** — the demo estate carries versions, a real skew and every verdict class. **Hard prerequisite:** `benzene-ui` generates its types from `contracts/artifacts/*` (`generate-contracts.mjs:148-163`), so the UI type does not exist until the fixture does. Continues R1 §5.9 / §A1 | [ui] | 1 d |
| C1.5 | **Versioned topic route + newest-by-default + version switcher** (§C4.3). Already promoted to R1 by §A1; delivers standalone value the day it lands | [ui] | 1 d |
| C1.6 | **Third state, everywhere** — `not compared` copy, the `isCompatible` vacuous-truth arm, the scope sentence, the `TypeChanged` stop marker (§C5). **Gate: nothing else in C1 ships without this** | [ui] | 1 d |
| C1.7 | **`Changed from v1` section + annotated `SchemaTree`** (§C4.3). **The centrepiece** | [ui] | 2–3 d |
| C1.8 | **`#changes` ledger + navigable estate tile + estate preview section** (§C4.1, §C4.2) | [ui] | 3 d |
| C1.9 | **Service-page grouping into five cards** (§C6) — after/with §B | [ui] | 1.5 d |
| C1.10 | **`RetirementRow` chips out of the tooltip** (§C4.5) | [ui] | 0.5 d |

**Wave C2 — "what changed since last run."**

| # | Item | Repo | Size |
|---|---|---|---|
| C2.1 | **`DiffTopicEntry` calls the engine instead of string equality** (§C2.2) — field-level temporal drift, plus the **changed-without-a-version-bump** class ranked above `breaking` | [agg] | 1.5 d |
| C2.2 | **`MeshTopicChange` gains optional `direction` / `path` / `compatibility`** — additive, older readers still render `description` | [wire] | 0.5 d + TS mirror |
| C2.3 | **`Since the previous snapshot` section + ledger mode toggle** (§C4.3) | [ui] | 1 d |
| C2.4 | **Service `contractDrift` re-derived as a rollup of its topics' changes**; hash demoted to detail (§C2.3, §C4.4). This is the item that makes **1** and **4** the same number | [agg] | 1 d |
| C2.5 | **Possible-rename pairing**, labelled as a hypothesis, verdict unchanged (§C3) | [ui] | 0.5 d |

**Deferred, with reasons**

- **`PreviousSpecJson` / service-spec field-level temporal drift** (§C2.3) — worst insight-per-byte in
  the set; superseded by C2.4's rollup for the case that matters. Revisit trigger recorded.
- **Go and Python parity — nothing to do, and this is verified, not assumed.** Neither port builds a
  topic catalogue: `benzene-go` has `mesh` + `meshd` (a collector with `FleetView` / `TopicSummary`
  read models, no `topics.json`), and `benzene-python`'s `benzene-mesh` is descriptor/collector-side
  only. The mirror obligation is **.NET and TypeScript, two ports, not four.**

**Rejected, so it is not re-asked**

- Per-consumer impact prediction (§C3) · rename as an engine `SchemaChangeKind` (§C3) · a
  breaking-change gate/alert/release-block in mesh (§C4.6, boundary §4.1) · side-by-side raw JSON or
  git-style diff (§C4.6) · `Benzene.Mesh.Aggregator` referencing `Benzene.Schema.OpenApi` (§C1.3) ·
  any compatibility field on the ServiceDescriptor or in the Cloud Service Profile (§C8).

### §C8 — Spec impact: none, and here is why that is a finding rather than a relief

**No `docs/specification/**` change. No conformance-fixture change. No new obligation on any profiled
service. No new signal from anywhere.** Grounds, checked rather than assumed:

- `mesh.md` **§9** states outright that the aggregator's `manifest.json` / `services/*.json` artifacts
  and the Mesh UI are *"collector-side idioms this contract deliberately does not constrain."*
  `topics.json` is an aggregator artifact; its shape is not spec-pinned. Same ground R1 §5.1 stood on
  for the usage-coverage declaration.
- Every input already exists: the per-version payload schemas are in `MeshTopicEntry` with `$ref`s
  inlined, and the previous catalogue is already read back by `ApplyCatalogDiffAsync`.
- The engine, the taxonomy and the rule table already ship in `Benzene.Schema.OpenApi`.

Measured against §A4's standing bar — *insight-per-byte-of-spec* — **this is the best item either
this round or the two before it produced.** Version compatibility was the exemplar: maximum insight,
zero spec cost. Field-level compatibility is the same trade at a larger payoff, and it lands on the
question every one of eight personas asked and none could answer. The roadmap's Phase 4 note was
right twice (`:686`, `:836`); round 3 supplies the evidence to promote it, and this block does.

### §C9 — Status honesty, updated

- **Shipped and verified:** the compatibility engine itself (`Benzene.Schema.OpenApi/Compatibility` —
  nine types, direction-aware rules, field-level paths, a CI gate); `VERSION COMPATIBILITY` as a
  *topology* reconciliation; `MeshTopicChange` run-over-run detection at string granularity.
- **Shipped and never connected:** the engine is referenced by **no `Benzene.Mesh.*` project**. This
  is a worse status than "not built" (§A4.3) — it is a shipped differentiator with no screen.
- **Shipped but vacuously true:** `MeshTopicVersionCompatibility.IsCompatible` on a topic with no
  in-estate producer (§C5). Fires on exactly the HTTP-fronted topics whose callers are outside the
  collector's vision.
- **Shipped but dead code:** `TopicPage.tsx:144-149`'s `Changes` section — real, visible, correct, and
  unreachable for every changed topic because `selectTopic` returns v1 (§C4.3).
- **Shipped but incapable:** `DiffTopicEntry`'s canonicalised string equality — `"Payload schema
  changed (request)"` is not an abbreviation, it is the ceiling.
- **Shipped but unverified against a real backend:** the Tempo adapter's metric and label names remain
  **documented convention, never checked against a live Tempo instance.** Restated for the third
  refinement running, because it keeps needing restating.
- **Not built:** everything in §C7.

Cross-reference: the data-layer halves — C1.1/C1.2/C1.3 and C2.1/C2.2/C2.4 — belong in
`work/service-mesh-roadmap-1.0.md` (benzene-dotnet), against its **Phase 4 field-level
compatibility** item, which this block promotes out of "mid term" and into the next wave. The UI
halves sit against R1/R2 in this document: **C1.5 and C1.6 are R1 items** (a reachable version and an
unearned verdict are both absence-honesty), **C1.7 and C1.8 are the substance R2.9 was always
reaching for**, and §C6's grouping is new. **The R1 STOP list still holds** — none of this is a new
estate surface; `#changes` is a ranked view over data the estate already publishes.
