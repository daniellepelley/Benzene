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
