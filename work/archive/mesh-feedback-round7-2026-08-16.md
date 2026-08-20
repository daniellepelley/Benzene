> ARCHIVED 2026-08-20: actioned — absorbed into `work/mesh-ui-product-vision.md`'s dated blocks and distilled by `work/mesh-ui-aims.md`.

# Mesh user-feedback round 7 — 2026-08-16 — open round

The seventh round, and the first **open** one since round 2. Rounds 5 and 6 drilled hard on a single
seam (deployment coordination) and moved it a long way; this round asks each role to use the whole
product for their actual job and answer one question: **what is now the weakest thing about it?**

Each persona was also asked two things the previous rounds did not ask:

- **Name what you would PROTECT**, as precisely as what you would fix. Six waves have shipped and I
  need to know which parts are load-bearing before the next one moves anything.
- **Sharpen your standing ask** into the smallest version that would change a decision, rather than
  restating it. Three of them had been asking for the same thing for four rounds.

Round 5 is `work/mesh-feedback-round5-2026-08-16.md`, round 6 is `…round6…`, and the method — plus
the four ways I broke it — is `work/mesh-persona-round-method.md`.

## Round metadata

- **Harness**: frozen `dist` of `benzene-ui` at `22d87ad`, served on `http://localhost:8940/` over the
  five-service mid-deployment estate. Every persona confirmed the commit was unchanged at the start
  and end of their run.
- **The harness was gated this time.** `verify-harness.mjs` asserts artifact shapes against the
  committed fixture, statuses against Benzene's real vocabulary, each service's spec against what the
  catalogue says it handles, the dispatch stub against `MeshDispatchMessageHandler`'s contract, and
  marker strings against the **served bundle**. It exits non-zero and gates the round. After four
  harness-authored findings across rounds 5 and 6, this round produced **none**.
- It failed on its first run, and the assertion was wrong rather than the fixture — `orders-api` is
  this estate's pure event source and genuinely handles nothing, so an empty inbound contract is
  faithful for it. Recorded because a check is only worth having if it is held to the product's
  standard.
- Two harness defects were fixed before the round on the strength of round-6 findings: the service
  snapshots carried a stub `specJson` (which is why the spec page read "0 topics" for three rounds)
  and the wrong `health` shape. Behind the second was a real product defect, fixed separately: an
  unreadable health payload rendered as "this service published no health checks".

## The headline: the product hedges its contract claims and does not hedge its numbers

Reached independently, in different words, by the production-support engineer and the architect.

> *"Mesh has become genuinely disciplined about qualifying its **contract** claims, and has not
> applied any of that discipline to its **numbers**."* — production support

The contract surfaces are full of earned hedging, and it is working: *"Each service's versions are
what the instance that answered the last poll declared"*, *"a change marked compatible can still
break something"*, *"† the whole topic's traffic — the usage feed does not break it down by
version"*, *"structural — no traffic observed"*, *"not compared"*. Multiple personas cited those as
the reason they trust the rest, and the support engineer said two of them stopped him over-claiming
in an incident channel.

The numeric surfaces do the opposite. Verified in source:

### 1. The Live window control changes nothing, beside a label that contradicts it

`selectRangeMs` is consumed in exactly **one** place — the topic strip's range label. The range *is*
sent to the collector, and the plane declares whether its counts honour it via
`window.countsWindowed`; that declaration is read in exactly **one** place too. Meanwhile `FleetPage`
and `IssuePage` hardcode **"last 24 hours"**, next to a control reading "15 minutes".

> *"At 3am I flip that to 15 minutes for exactly one reason: is this happening right now, or did it
> happen at teatime? I get an unchanged number and a header that now says 15 minutes next to it. That
> is worse than not having the control."* — production support

### 2. Provenance is shown when a value is absent and hidden when it is present

The `Calls` block renders `100.0%` as a bare chip with a generic tooltip. `topology.json` carries
`source: "tempo"` vs `source: "declared"`, and the product **does** surface that when the value is
missing — `structural — no traffic observed`. So the one case it discloses is the harmless one.

> *"I nearly posted '100% of orders→shipping is down' into a Sev1 justification."* The real edge is
> ~30%: the chip is per-edge from the trace source, the traffic panel three inches below is from the
> usage feed, and they disagree by 3×. Resolving it required fetching raw JSON — *"a real user cannot
> do this, and that I had to is the finding."*

The topic page gets this right (`LIVE observed 0` above `2,205 calls over the usage feed's own
window`). The discipline exists in the codebase and has not reached the service page.

### 3. A dagger with no footnote, on the page where retirement is decided

`RetirementRow` prints `†` and `ValuePage` contains no footnote and no tooltip — while `TopicCatalog`
prints the same marker **and** its explanation. So the Value page reads `payment:capture v2 · 10.3k
msgs observed†` for a version carrying none, and files it under *"NO RETIREMENT SIGNAL — actively
used"*.

## The second finding: an obligation cannot tell a dormant declaration from a live outage

The developer's headline, and structurally the same defect as the one that started Wave D — both
halves computed, shown on adjacent screens, never joined.

`order:placed v2` and `payment:capture v2` render **identically**: `MOVE OUTSTANDING`, `▲ breaking`,
the same sentence shape. What the product also knows:

| | `order:placed` v2 | `payment:capture` v2 |
| --- | --- | --- |
| observed | 0 | 412 |
| errors | 0 | 412, `no-handler` |
| issue | none | `NoHandlerRegisteredException` ×412 |

> *"payments-api is losing 412 messages an hour right now. I am losing zero. … That changes my
> sprint. If order:placed v2 were live and failing, I'd cut something to ship it this week."*

They only discovered their own version was dark by opening **someone else's topic page and comparing
numbers**. Note the product already proves it can make this join — the ledger stamps `GAP LIVE NOW` —
but that badge is structural, so the one topic in the estate with 412 counted, fingerprinted failures
does not get it.

## The third finding: the product reasons about edges and refuses to reason about the graph

The architect's weakest-thing, and the one that is a capability gap rather than a defect.

Every single-topic, single-service, single-pair question is answered better here than in any tool
they own. **No** estate-level structural question is answered at all: no fan-out, no hub-ness, no
depth, no ownership concentration, no team roll-up. They derived "four of six rollouts touch
`orders-api`" and "`orders-api` produces five of six topics" **by counting on screen**.

> *"It has diagnosed my estate in a disclaimer and then declined to measure it"* — referring to the
> Rollouts page's own sentence, *"on an estate where one service touches most topics, that combination
> degenerates to the whole estate"*.

The topology graph is where shape could live and it is collapsed by default, positioned last,
unlabelled, unclickable, and discards the `source`, `errorRate` and percentile fields its own artifact
carries.

## Verified defects

Each checked against source or the DOM before entering this list.

| # | Defect | Found by |
| --- | --- | --- |
| 1 | Live window control changes nothing; `countsWindowed` read once; two pages hardcode "last 24 hours" | support, architect |
| 2 | `Calls` percentages carry no provenance and disagree 3× with the traffic panel | support |
| 3 | `†` on `#value` has no footnote and no tooltip | architect (and platform engineer, r6) |
| 4 | Filtering the topic catalogue by a service **hides that service's outstanding rows** — the round-5 defect surviving in a second filter | architect, developer |
| 5 | An obligation renders identically whether it is dormant or costing 412 messages | developer |
| 6 | `ServiceDrift` says "7 changes across 4 topics"; its own "view changes" button lands on "8 changes across 5". The doc comment claims the copy discloses this; **the copy never shipped** | developer |
| 7 | Test Console offers a service topics it *produces*, then returns `no-handler`. Compose was fixed for this in `437c860`; the console was not | developer |
| 8 | Compose's confirm says "sends a real message to **the service's** real handler" without naming which service — the target is known before the click and printed only in the response | developer |
| 9 | Team is displayed five times and matches zero filters | architect |
| 10 | Estate topic search cannot reach payload schemas — `taxJurisdiction` returns "no topic or service matches" | developer |
| 11 | Load-bearing meaning lives in `title` attributes: 5 of 8 titled elements on `#changes` are the state vocabulary definitions | architect |
| 12 | `412 ↗` drops the version from the URL and is a visual no-op; `↗` reads as a trend arrow | developer |
| 13 | Flow rows advertise "8 events" the collector structurally cannot supply; the exemplar trace is named as plain text beside a live flow row with the same id; `transport` is dropped entirely from the issue page | developer |

## The standing asks, sharpened

All three personas who had been asking for the same thing for four rounds were told to define the
smallest version. They did, and the answers are much cheaper than the asks.

**Trend** (architect, delivery owner, four rounds): **two numbers, weekly, ninety days** — the count
of topic-versions in an outstanding state (already on the tile) and the **age of the oldest
outstanding move**. Not a history browser, not a timeline UI.

> *"Count flat, age rising → the estate is calcifying. I freeze new breaking versions until the
> backlog drains, and that is now a defensible position in the room rather than a hunch."*

And the raw material is already there: the `DRIFT` badge means *the published spec changed since the
last snapshot*, the service page prints `previousSpecHash`, and the topic page has a **since the
previous snapshot** section. *"This is not a cold start. It is a depth-of-one history you're already
computing and then discarding."*

**Freshness vs traffic-by-version** (production support, forced to choose one): they kept
**freshness**, and the reasoning is worth keeping. The version fraction is the more mesh-shaped ask
and it is unobtainable anywhere else — but *"the two failures are not symmetric. A wrong severity is
embarrassing and the incident commander corrects it in five minutes. A conclusion drawn from a
three-hour-old contract snapshot means I wake the wrong team for something that's already rolled
back."* Also: *"the stale part is the valuable part"* — the errors are live, the contract catalogue is
a snapshot, and mesh's whole pitch is the contract picture.

**Linked obligations** (developer, ruled against in round 6): **accepted without reservation** — *"if
mesh had told me my invoice:raise move is gated on my order:placed move and been wrong once, I'd stop
trusting the obligations block, which is the best thing on the page."* But: *"the ruling closed the
wrong door. I don't need mesh to infer. I need it to let me look"* — hence defect 10, and a request to
group field changes by field name so a coincidence is visible without being asserted.

## What to protect

Asked explicitly, and the answers overlap heavily. In rough order of how often they were named:

- **The service page's `OUTSTANDING` block**, with its two sentences — *"orders-api has already moved,
  and cannot retire v1 until this ships"* and *"keep v1 live — orders-api still uses it"*. The
  developer: *"That is my sprint. It took me zero clicks and I believed it."*
- **The `WAITING ON` block** — the reciprocal. Both ends of the conversation see the same fact from
  their own side.
- **`Changes` → Rollouts, including the refusal paragraph.** The architect would read it aloud in a
  review: *"It is the best writing in the product and it is the reason I trust the rest of it."*
- **The three distinct states** — `GAP LIVE NOW` / `MOVE OUTSTANDING` / `COMPLETION OUTSTANDING`.
- **The plain-English cause line on each issue row**, pasteable verbatim into Slack.
- **The victim/culprit inversion.** `shipping-api` is UNHEALTHY and owes nothing; `orders-api` is
  HEALTHY and owes the move. *"My dashboards would have sent me to the wrong team."*
- **The topic PAYLOAD panel** with `ADDED, REQUIRED` and enum values inline — *"the difference between
  me writing the handler and me opening orders-api in the IDE"*.
- **The Test Console URL** as a runbook step, and **Compose's schema-seeded body**.
- **Every disclaimer.** Named individually by three personas. *"Every one of those bought trust; do
  not trade any of them for a cleaner screen."*

## What merely exists

Named by more than one persona as looked-at-and-moved-on: the **Value** tab (twelve rows, all "no
retirement signal", plus the broken footnote), the **Topology** graph (the service page's `Calls`
block is strictly better and has numbers), the **Live window** selector, **Recent flows** (rows are
not clickable), and **Discussion** (read-only and empty on every page).

## The fourth finding: three signals saying "I don't know", all rendering green

The platform engineer's, and it is the same defect as the Value page one level up — which is why the
two should be read as one thing. They put each of these on the wire through a proxy and watched it
render.

1. **Service-level `missingFeeds` is ignored entirely.** A service whose collector says
   `missingFeeds: ["health","usage"]` renders *"Heartbeat healthy"*, *"9.8k messages observed"* with a
   full breakdown, and *"● No issues observed for this service"* with a green dot. Three positive
   assertions built on feeds the collector said it does not have. The mechanism **exists** one level
   down: topic-level `missingFeeds` renders *"not supplied by this plane: usage"*.
2. **The live plane's `health` field is not read at all.** `health: "unreachable"` renders `HEALTHY`.
   The estate counters are fed only from `manifest.json` — 2h40m stale here — while the fresh feed
   contradicting it is dropped.
3. **The estate page carries no liveness.** Two never-heartbeated services render identically to one
   that heartbeated 30 seconds ago, and the divergence banner fires only on `stale`, never on
   `silent` — so with two silent services on screen it named the stale one and called it "silent".

> *"The one failure mode I most need to catch on a rollout — I deployed the service, I forgot to wire
> the mesh middleware — is the one you can only find by opening five service pages one at a time."*

Their summary is the round's, and it generalises the Value-page finding exactly:

> *"Stop letting the live plane's own admissions of ignorance render as green."*

They also give the boundary argument for what to do about the KPI strip and the Live window:

> *"The moment mesh renders a number that a monitoring tool renders better, it inherits the monitoring
> tool's burden of proof — and right now it fails that burden. … My advice is not 'make them better'.
> It is: either feed them properly from the live plane, or delete them. A dash is better than a wrong
> zero, and no widget is better than a dash."*

And they **withdrew a claim of their own from round 6** — that `lastSeen` was read by nothing. It
drives the whole liveness model and works. Worth recording: the verify-before-ranking discipline is
now being applied by the personas to their own prior reports.

## Verdicts

| Persona | Round 6 | Round 7 |
| --- | --- | --- |
| Architect | LIVE, with one page they would not open | **LIVE for `#changes` and the `GAP LIVE NOW` card; SCREENSHOT ONLY for the rest** — because the meaning is in tooltips and *"I cannot hover in a room"* |
| Developer | YES | **YES, Monday morning** — and would come back mid-week if an obligation said whether it was bleeding |
| Production support | runbook YES | **YES, with two DO-NOTs in the runbook** — do not read the Calls percentages, do not use the Live window |
| Delivery owner | YES unreservedly | **YES for Rollouts; would not open the Value page again** until it stops calling a 100%-failing topic "actively used" |
| QA | would not sign off | **Would not sign off — but for four evidenced reasons rather than a feeling**, which they called a real product win. YES for the contract surfaces, before writing a single test case |
| Platform engineer | MAYBE | **Split, explicitly: YES for "what contract state is this estate in and who owes what"; NO for "is everything up"** — adoption MAYBE, one change from YES |
