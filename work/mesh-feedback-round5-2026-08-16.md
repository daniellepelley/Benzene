# Mesh user-feedback round 5 — 2026-08-16 — deployment coordination

The fifth round, and the second **focused** one. Round 3 asked "can you tell what changed, and
whether it breaks you?"; Wave C1 shipped an answer to that and round 4 confirmed it landed. This
round asks the question that comes immediately after, and that the industry is worst at:

> **What needs to be deployed, what needs to be deployed *together*, and how do I know the contracts
> actually work once it has been?**

Versioning is the textbook answer to a breaking contract change, and it is often not available —
because the versioning decision was not taken in time, because a consumer cut over rather than
dual-running, or because the change is one nobody thought to version. When it is not available, the
fallback is a **coordinated deployment**: several services shipped together, in a particular order,
because one of them will break its counterpart otherwise. Working out which services those are,
which order they go in, and whether the result is actually correct is the part teams get wrong.

Prior rounds: `work/mesh-feedback-round-2026-08-16.md` (read its correction block first),
`work/mesh-feedback-round2-2026-08-16.md`, `work/mesh-feedback-round3-2026-08-16.md`. Round 4's
findings are recorded in §C of `work/mesh-ui-product-vision.md`.

## Round metadata

- **Harness**: frozen `dist` snapshot of `benzene-ui` at `7dc8960`, served on `http://localhost:8920/`
  by `serve-r5.mjs` with a stub collector. Frozen deliberately: in round 4 the working tree was
  rebuilt mid-round and one persona had to re-verify everything they had already written. Every
  persona in this round looked at the same software, so the round's findings attribute to one commit.
- **The estate is an experiment, not a demo.** Five services, six topics at twelve versions, composed
  by `compose-deploy-estate.mjs` to contain five deliberately distinct rollout states — because the
  hypothesis under test is *whether the product can tell them apart*, and four of the five carry the
  identical `▲ breaking` verdict.

  | Scenario | Topic | Shape | Verdict | Ground truth |
  | --- | --- | --- | --- | --- |
  | A | `payment:capture` | producer on v1+v2, consumer on v1 only | breaking | **Live outage** — 412 `no-handler` |
  | B | `inventory:reserve` | producer on v1, consumer on **v2 only** | breaking | **Live outage** — 2,205 `service-unavailable`; the *consumer* moved first |
  | C | `order:placed` | producer on v1+v2, consumer on v1 only | breaking | **Silent** — structurally identical to A, zero telemetry |
  | D | `invoice:raise` | producer on v1, consumer on v1+v2 | compatible | Consumer ready and idle; safe direction |
  | E | `shipping:book` | **all three parties declare both versions** | breaking | **Requires no coordination at all** — the escape hatch, done right |

  Scenarios A and C are the same structure with and without telemetry. E is the control: a brutal
  schema diff (required-field removal) that is completely safe because of an overlap window. If the
  product cannot separate E from A, its `breaking` chip is not load-bearing.
- **Live plane included**, reporting per-`(topic, version)` rows — because "has the thing that was
  supposed to ship actually shipped, and is traffic flowing through it?" is partly an observed
  question, and a static-only harness would make it unanswerable for reasons that are the harness's
  fault rather than the product's.
- **Six personas**, run in parallel, each given the same estate and their own job: architect,
  developer (owner of `billing-api`), production support (paged at 03:12), delivery owner, QA, and
  platform engineer.

## The round-blocking defect

Three personas hit it before they reached their first job, and it framed everything that followed.

**Every service page and every topic page rendered a blank white screen**, with
`TypeError: Cannot read properties of undefined (reading 'filter')` in the console. There was no
error boundary, so once it fired the SPA stayed dead: the back button did not recover it, and every
subsequent hash route rendered nothing until a hard reload.

The cause was in the harness, not the product — `compose-deploy-estate.mjs` wrote `annotations.json`
with the key `entries` where the product reads `annotations`. That is exactly the point. A single
malformed feed, from one of eight artifacts, deleted the entire detail plane of the product. The
platform engineer put it best, and it went straight into the backlog above every design item on this
list:

> *An error boundary, so a single malformed feed degrades to a strip that says which feed is
> unreadable instead of a blank page. Everything else on this list is a product improvement. That one
> is the difference between a tool I use and a tool I closed on day one and never reopened. An
> operator's trust is lost exactly once.*

Fixed in `09564b4` — boundary coercion at the artifact reader, plus an `ErrorBoundary` keyed on route
so a crash costs one page rather than the session.

The consequence for this round is that **every positive finding below was reached from the estate
page, the Changes tab, the Value tab and the issues list alone.** The detail plane — where a release
review would actually live — was unavailable. That the round still produced two SOLVED verdicts on
its central question is, on reflection, the strongest evidence in it.

## The central finding: the badge marks the party that finished, not the party that is late

Reached independently by the architect, the developer, the production-support engineer and the
platform engineer, from four different jobs. It is one defect with four faces.

Contract badges attach to whoever declares the new version. That is whoever has already done the
work. The party that owes the work renders clean.

- `payment:capture v2 ▲ breaking` renders under `orders-api`, who shipped it. `payments-api`, who
  owes the handler, renders `HEALTHY` with no marks.
- `inventory:reserve v2 ▲ breaking` renders under `shipping-api` — which reads as shipping being at
  fault, when shipping is the one that already shipped and `orders-api` is the late party.
- `billing-api` owes **two independent** deploys (consume `order:placed` v2, produce `invoice:raise`
  v2) and is the pivot of the only transitive chain in the estate. Its card reads, in full:
  `CONSUMES order:placed v1 / PRODUCES invoice:raise v1`. No badge, no amber, nothing.

The filter inverts it the same way. Typing `billing-api` into the topics filter returns **"2 of 12"**
— the two v1 rows — because billing's name does not appear in the v2 cells. *The single most natural
query in a release review returns the confident, wrong answer that billing has nothing to do.*

The developer who owns `billing-api` reached it from the other end:

> *The natural first click for a service owner actively tells him he has nothing to do, in a release
> he is the critical path of.*

And the production-support engineer, at 03:12, nearly acted on it:

> *I had to infer deploy order from error `first seen` and the drift badge pointed at the wrong
> service. Cost me ~2 minutes and, worse, nearly cost me a wrong rollback.*

The architect's summary of the fix is the shortest statement of it:

> *Put the direction on the badge. Mark the service that is late, not the service that finished. That
> single inversion fixes the billing-api blind spot, fixes the shipping-api-looks-guilty problem,
> makes the filter return the right rows, and turns the topics table from evidence I interpret into
> an answer I can point at.*

## The second finding: `breaking` and `requires coordination` are not the same predicate

Scenario E is the control, and the product renders it as the worst case in the estate.

`shipping:book` v2 removes a required field — as brutal a diff as the taxonomy has — and needs no
coordination whatsoever, because all three participating services declare both versions. It carries
the same `▲ breaking` chip as `inventory:reserve`, which is at a 100% error rate. Worse, because it
has three participants, it renders that chip **three times** (under `orders-api` PRODUCES, under
`payments-api` PRODUCES, under `shipping-api` CONSUMES) where `payment:capture v2` renders it once,
and the Changes tab chips it with all three service names, which reads as maximum blast radius.

> *A room scanning for red concludes `shipping:book` is the most dangerous topic in the estate. It is
> the only one that has been done correctly. The best-engineered topic in the estate is the reddest
> thing on screen.* — architect

The platform engineer costed the same defect operationally:

> *I reconcile the producers/consumers columns by hand for every topic, every release. Do that under
> time pressure and you either miss a real one or you learn to ignore the red chips — and once I'm
> ignoring them the tool has negative value.*

The distinction is not "breaking vs not". It is **"breaking with an overlap window" vs "breaking with
a cliff edge"**, and it is a two-set comparison over version declarations the catalogue already
carries. `MeshVersionCompatibility` computes exactly this and is one page-hop from where everyone was
looking.

## The third finding: the ordering constraint is derivable and is never stated

Nobody got the deploy order from the product. Two personas derived it themselves, from
event-vs-request semantics that mesh already records on every change; one reasoned it backwards.

> *Getting this backwards causes the outage rather than fixing it — and it already did, on
> `inventory:reserve`. This is not "tell me my deployment plan"; it's "tell me the constraint you can
> already derive: X still consumes v1, so v1 cannot stop being produced yet."* — platform engineer

The QA engineer found the product actively giving the wrong advice on the consumer-ahead case
(scenario D/B shape): the version-compatibility banner's guidance assumes the consumer is behind,
which is false whenever a consumer has cut over first. That is not a missing feature, it is an
incorrect statement, and it is the one class of defect this product cannot afford.

Groundwork on why the rule is derivable, and on the two places a naive derivation breaks, is in
`deployment-groundwork.md` §2 and §7 (reproduced into the PO's design block).

## The fourth finding: mesh's words are usable and its numbers are not

Unanimous across all six, and the single most repeated sentence of the round.

- **`100.0% failed` on every topic whose status vocabulary the product does not recognise**, printed
  directly beside that same topic's own breakdown reading `success 9,840`. Two numbers under one
  `TRAFFIC` heading, disagreeing. Only the topic that happened to use the literal `ok` read 0%.
- **`observed handlers: shipping-api` printed directly beneath `observed 0`**, with a tooltip reading
  *"Seen handling this topic — observed, not declared"*. The field is the live plane's own **declared**
  consumer list; the plane carries no per-handler invocation count, so there is no observation
  available to report there at all. Both the label and the tooltip asserted the opposite of the truth.
- **Per-version traffic on the Value page** rendered without the caveat that `usage.json` carries no
  version attribution — my own incomplete fix from round 4.
- **A green `HEALTHY` badge on the service causing both outages.**

The delivery owner's verdict is the commercial version of it:

> *As it stands I can use mesh's words and none of its numbers, and a delivery owner whose numbers
> all need checking against another tool will eventually just start in the other tool.*

And the QA engineer's is the one that names the mechanism:

> *Each of these individually is a wording or a cross-check; together they are the specific mechanism
> by which a QA signs off something that was never exercised. I need "declared", "registered", and
> "seen carrying traffic" to be three visibly different statements, everywhere they appear.*

Two of these are fixed as of `7dc8960` and `b7f5b3e`: the unrecognised-status bucket is now disclosed
rather than counted as failure, and `TopicLive.services` is renamed `registeredHandlers` with the
label and tooltip rewritten to state what registration does and does not imply.

## What the product got right

Worth recording precisely, because it is what the design round has to protect.

- **The version-compatibility block did the central job.** The QA engineer and the delivery owner both
  named it unprompted as the best thing in the product for their role. It found all four rollout gaps
  including the latent one.
- **The two issue one-liners are the best writing in the product.** *"shipping-api handles
  inventory:reserve at v2 only; callers are still sending v1"* and *"No service in the estate declares
  handling payment:capture at v2"*. Three personas quoted them verbatim; the support engineer went
  cold-to-diagnosis in **45 seconds** on them against a 25–40 minute baseline.
- **The estate topics table with per-version producers and consumers** is, in the architect's words,
  *"the first thing any tool has ever shown me that lets me reconstruct deployment coupling at all."*
- **The scope caveats are landing.** The Value page's own footnote admitting traffic is not
  version-attributed was cited approvingly by the architect as *"exactly the right kind of admission"*
  — the honesty work from rounds 3 and 4 is being read and is buying trust rather than spending it.

On "would you open this again next week?" the architect, the delivery owner and the QA engineer said
an unqualified **YES**; the platform engineer said *"YES — conditionally"*; the developer said
*"MAYBE — leaning YES once the detail pages render"*. The support engineer, asked the runbook version
of the question, said **YES for the estate landing page as an explicit first step before Splunk, NO
for anything past it** until the detail pages stop white-screening.

One regression to record against round 4: the architect's *"would you put this on screen in an
architecture review?"* went `SCREENSHOT ONLY → LIVE` in round 4 and is back to **SCREENSHOT ONLY**
here — *"and today, barely that, because clicking a service name gives the room a blank white page."*
That is entirely the crash, and it is the clearest single measure of what an unbounded render error
costs.

## Verdict table

| Persona | Central question | Round 5 |
| --- | --- | --- |
| Architect | Which services must ship together, and is this coupling accidental? | PARTIAL (synthesis is in my head) / BLOCKED on the escape hatch |
| Developer (`billing-api`) | Am I in this release, and can I ship alone? | SOLVED **from the fleet table**, by hand; my own service page says I have nothing to do |
| Production support | What is broken, how bad, who do I wake? | SOLVED in 45s / PARTIAL on severity (no traffic-by-version) |
| Delivery owner | Write the release plan; go/no-go per batch | PARTIAL — coupling map excellent, batching and sequencing mine, no defensible figure |
| QA | Did the right things get deployed, and can I prove the contract works? | SOLVED on reading / PARTIAL on proving (cannot send at a version) |
| Platform engineer | Is the mesh telling the truth; can I run a release from it? | PARTIAL / BLOCKED — the crash, and unattributed data rendered as per-version |

## Findings discarded after verification

The verify-before-ranking discipline is now five rounds old and has caught a false finding in every
one. This round it caught one of mine.

- **"`structural — no traffic observed` on every call edge, including one carrying 10.3k messages"**
  (delivery owner). Real observation, **not a product defect**. `EdgeList.tsx:30` reads
  `e.requestsPerMinute != null`; the committed artifact carries
  `{client, server, source, requestsPerMinute, errorRate, p50LatencyMs, p95LatencyMs, p99LatencyMs}`;
  my `compose-deploy-estate.mjs` wrote `reqPerMin` and `p95Ms`. Every edge therefore read as
  unmeasured and the product labelled them correctly. **Fixture defect, mine.** Fixed by pinning the
  composer to the real artifact shape, and by leaving exactly one edge genuinely unmeasured so a
  future round can tell a real unmeasured edge from a broken feed. A harness that misreports the
  product is worse than no harness.
- **The white-screen crash itself** is half in this category: the *trigger* was my fixture's wrong
  key, and it does not enter the backlog as "fix the annotations reader". What entered the backlog is
  the product defect it exposed — that there was no boundary, and that one bad artifact could delete
  the whole detail plane.

## Where this round goes next

To the mesh product owner as a design brief, with `deployment-groundwork.md` attached — which
establishes, source-verified, that **both halves of the answer already ship and are computed two lines
apart in `MeshAggregator.BuildTopicCatalog` without ever meeting**: `BuildVersionCompatibility` knows
who is on which version, `ApplyCrossVersionCompatibility` knows whether the gap matters, and the
deployment question is exactly their join. That is the same shape as the Wave C1 finding — the parts
ship, the join doesn't — which makes it a pattern rather than an incident, and worth saying to the PO
in those terms.
