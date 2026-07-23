# Benzene Mesh UI — Product Vision & Roadmap

> Living doc owned by `mesh-product-owner`. Convention: append dated update
> blocks at the top (oldest→newest) that flag deviations rather than rewriting
> history. Cross-reference `work/service-mesh-roadmap-1.0.md` (same owner)
> by section number when a UI need depends on the data layer.

---

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
