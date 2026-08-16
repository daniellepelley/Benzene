# Mesh user-feedback round — 2026-08-16

The first round using the eight user personas in `.claude/agents/mesh-*.md` (see
`work/mesh-user-personas.md`). Each ran its own jobs against a live mesh UI and reported
independently. None saw another's report. This file is the raw evidence pack; the product
refinement it fed is in `work/mesh-ui-product-vision.md`.

## Round metadata

- **Harness**: `benzene-ui` built at **`3a61f05`**, served over the vendored `contracts/artifacts/`
  fixtures with a **stub collector** at `POST /benzene/invoke` answering `benzene:mesh:query:fleet`
  and `benzene:mesh:dispatch`, so the live plane and Test Console were switched on rather than
  dormant. Estate: `orders-api`, `payments-api`, `shipping-api`; topics `orders:create`,
  `orders:get-all`, `payment:capture`, `shipping:book`, `order:legacy-export`.
- **Overtaken during the round**: `benzene-ui` moved to `d4f440b` while the personas were running —
  adding sign-out/refresh controls and, relevantly, replacing a raw `404 Not Found for
  manifest.json` with a real "No catalog yet" empty state. Findings below are against `3a61f05`.

> **⚠ CORRECTED 2026-08-16 by the product refinement — read `work/mesh-ui-product-vision.md` §0
> before quoting anything below.** The PO checked four headline findings against source and found
> them mis-scoped. Two were caused by **this harness serving the base fixtures**, and they are the
> round's two biggest claims:
>
> - **"Ownership is absent"** (5 personas, the #1 ask for three) — `MeshManifestEntry.OwningTeam`
>   exists, the aggregator populates it, and `ServiceCard.tsx:67` / `ServicePage.tsx:70` already
>   render it. It is absent from `contracts/artifacts/manifest.json`, the fixture served here.
> - **"Version renders as an em-dash"** (the developer's "single worst thing I found") — version
>   skew is **shipped**. `contracts/artifacts/topics.versioned.json` encodes exactly the case
>   reported: `payment:capture` `producedVersions [v1,v2]` / `consumedVersions [v1]` /
>   `producedNotConsumed [v2]` / `isCompatible: false`. The harness served the unversioned
>   `topics.json`.
> - **`missingFeeds` "rendered nowhere"** — it is rendered on one surface of five, as *"not supplied
>   by this plane"*, which is why a DOM search for "missing" found nothing. The finding is
>   *inconsistency*, not absence.
> - **The inert window control** — a control-honesty defect, not a data gap: the wire already
>   self-describes `countsWindowed: false` and the store already reads it.
>
> **Harness lesson**: `contracts/artifacts/` carries richer variants (`topics.versioned.json`,
> `topics.liveness.json`, `topology.structural.json`, `fleet.windowed.json`, `manifest.minimal.json`)
> that exist precisely to exercise these dimensions. Serving the base files understates the product
> and sends personas hunting for capabilities that ship. Future rounds must state which fixture
> variant each artifact was served from, and should run the richest ones by default.
>
> The rest of the pack stands. The #1 finding (the Value page manufacturing deletion evidence from an
> absent row) was **confirmed exactly as reported** and ranked first in the product.

## Findings to DISCARD — harness artifacts, not product defects

Recorded so they cannot leak into the backlog:

1. **The `spec` link 404s** (reported by architect, BA, delivery owner, platform engineer,
   production support). The UI links correctly to `mesh-spec-ui.html`; the harness had not mapped
   that filename. Fixed mid-round. A real deployment serves it via `UseMeshSpecUi` or by copying the
   page next to the artifacts. **However** the developer's related finding — that the spec page is
   reachable only via a small grey link on the fleet list, is a visually separate application, and
   *contradicts the mesh's own topic page* — is real and kept.
2. **"The system rejected nothing"** (QA, developer). The stub accepted every payload including
   `{}`. That specific result is the harness. **The product findings underneath it survive and are
   kept**: a tester cannot distinguish a real acceptance from a no-op; the response panel discards
   `x-correlation-id`; there is no response contract to assert against.

## Verdicts

| Persona | Would return? | Adoption test | For what |
| --- | --- | --- | --- |
| Production support | **YES** (second tab, not first) | Runbook? **MAYBE** | "Is it us or them", and who else is on this topic |
| Architect | **MAYBE**, leaning yes | Live in review? **SCREENSHOT ONLY** | The Value page; pre-review structural check |
| Business analyst | **YES** | Stakeholder over shoulder? **MAYBE** | Impact analysis — found `PAY-118` unprompted |
| Delivery owner | **YES** | Steering pack? **WITH CAVEATS** | Delivery risk + the coordination list |
| QA | **YES for reading, NO for testing** | Repeatable test case? **NO** | Test-case design and regression surface |
| Developer | **MAYBE** | — | The producer/consumer map, once per change |
| Platform engineer | **YES** | Trust on release morning? **NO** | Topic topology, drift, Value tab |
| Security reviewer | **YES** | Sign off? **YES WITH CONDITIONS** | The contract-derived data-flow map |

Nobody said NO. Everybody said the estate/contract map is real value nothing else in their toolbox
provides. The failures are concentrated and repeat across roles.

## Theme 1 — Absence renders as good news (5 personas, SEVERE)

The most dangerous class of defect found, and the platform engineer's headline:

- **Topology feed missing → the estate asserts it has no coupling.** With `topology.json` absent the
  fleet panel states *"No producer/consumer edges are declared yet — no registered service consumes
  another's topic."* The real estate has three edges, one at 86.4 req/min with 18% errors. Every
  service page simultaneously reads *"Declares no outbound calls."* — byte-identical to the string
  legitimately shown for a service that genuinely declares none. This is the exact state of a mesh
  on day one of rollout, and it will tell architects the services are decoupled. *"That reads as
  good news. It is the single worst thing I found."*
- **A zero row-count renders as a measured zero.** `order:legacy-export` has **zero rows** in
  `usage.json` (verified independently). The fleet table prints a hard `0`; the Value page lists it
  under RETIREMENT CANDIDATES with evidence *"no traffic observed **while the usage feed is
  wired**"*; its own topic page says *"No usage source is wired, so traffic for this topic is
  unknown."* Three surfaces, three incompatible claims. The discussion thread shows two named people
  already agreeing to delete on that basis. **The product manufactured the evidence for a production
  deletion out of an absence.** The delivery owner reached the same conclusion from the business
  side: *"this zero is a placeholder, not a measurement, and it's rendered identically to real
  zeros."*
- **`missingFeeds` is on the wire and rendered nowhere.** The live plane returns it per service and
  per topic. The string "missing" does not appear in the DOM. The mesh knows its own blind spots and
  does not say. This single omission is upstream of most of this theme.
- **Green "No issues observed"** for `shipping-api`, whose live record is `health: unknown,
  missingFeeds: [health, traces]`. No trace feed means no issue can exist; it renders identically to
  healthy `orders-api`.
- **`0 DEGRADED`** tile from the month-old manifest, contradicted by the live plane reporting
  `payments-api: degraded` on the same screen.

**Counter-evidence worth protecting**: the Value page *does* downgrade its own strapline to
*"Structural evidence only: no usage feed is wired, so 'unused' cannot be proven here"* when the feed
dies, and a null `errorRate` renders as **`errors unknown`**, not `0.0%`. The team demonstrably knows
how to do this. It is applied in two places and missing everywhere else.

## Theme 2 — Ownership is absent (5 personas, and it is the #1 ask for three)

No team, owner, squad, contact, rota, repo or channel anywhere. Confirmed absent from the
`manifest.json` model, not merely unrendered. The only humans in the product are whoever left a
comment.

- Production support: *"the single gap between mesh being a nice tool and mesh being* the *tool."*
  They wrote a genuinely excellent incident message and could not address it.
- BA: *"I know precisely what my change hits and I'm guessing who to talk to."*
- Developer: *"I can see exactly who I'll break and have no idea who to tell."*
- Architect: *"a risk with no owner is a risk nobody actions."* For governance, *"not a gap, a hole
  in the floor."*

Each persona independently reached the same shape: mesh builds a precise, evidenced case and then
cannot say who to hand it to.

## Theme 3 — Flags that name a problem and refuse to describe it (5 personas)

- **`contract drift` = a hash pair.** `spec hash changed: 5feaedb410bf… → b9b30797f974…`. Architect:
  *"a change-detection primitive presented as a finding."* Developer: *"A hash is not information"* —
  went to his own git diff. BA and delivery owner both read it as noise occupying the most prominent
  row on the page.
- **`SCHEMA MISMATCH` = a boolean.** Confirmed in the model: `schemaMismatch` is a bare boolean and
  `changes[]` is `{kind, description}` with description as free prose, so the product *cannot* say
  what differs even in principle. Architect: *"The badge is a rumour with a border-radius."* QA is
  being asked to sign off a story whose downstream contract the tool itself flags as broken, and it
  won't show the break. The developer produces that topic and cannot tell whether his messages are
  among the 87 validation errors on it.
- The one thing that *did* classify drift was **a human comment**: Priya's note distinguishing
  expected drift (`PAY-118`) from the real issue. Architect: *"that one comment carries more
  contract-health signal than every automated indicator on the page combined"* — and it is the
  clearest statement of what the tooling is missing. **And discussion is read-only in this
  deployment.**

## Theme 4 — Version is the whole question, and it renders as an em-dash (developer, SEVERE)

The sharpest single finding of the round, because the data is already in flight:

- Fleet table: `VERSION —`
- The producer's own spec (`mesh-spec-ui`): `payment:capture` **v2**, `currency` **not required**
- The mesh topic page: version `—`, `currency` **required** — *contradicting the producer's spec on
  requiredness, in the same product*
- The live plane the UI is already fetching: `"version": "1"` for the topic and for the issue

**The consumer is running v1 while the producer declares v2.** That is the entire go/no-go on a
payload change, it is in the response the UI already parses, and it is rendered as a dash. The
architect independently flagged the same dimension from above: four of five topics show `—`, meaning
*no live topic in this estate is versioned* — a five-alarm architectural fact rendered as punctuation.

## Theme 5 — Payload constraints are silently dropped (developer, QA)

The topic page renders name / type / format / required and discards everything else:
`maxLength`, `pattern`, `minimum`/`maximum`. `orders:create` hides `pattern: ^[A-Z]{3}-[0-9]{4}$`
and `quantity min 1 / max 99` **while carrying 94 `validation-error`s in the usage feed**.

> "The UI is hiding the cause of the failures it's counting." — developer

QA's negative test cases are consequently invented rather than derived: `currency` has no enum, so
"is `gbp` valid, is `""` valid" are four guesses; `amount` has no minimum, so *"is a negative amount
rejected"* — on a payment capture topic — is a case they made up. There is also **no response
contract at all** (the block is labelled REQUEST, there is no RESPONSE), so QA has no expected result
to assert against for any case.

## Theme 6 — The dead end at the flow (5 personas)

Every persona that hit a failure wanted one real example and could not get one. Flow rows are
`<code>`, not links. Exemplar trace ids on the issue page are unclickable text. There is no trace
detail view anywhere (`#trace/<id>` silently bounces to fleet). "7 events" is advertised as a
drill-down and is not one.

Production support, developer and QA all left for Splunk/CloudWatch at exactly this point — *"which
is exactly the workflow the product says it's replacing."* Note the honest constraint: the live
plane's `traces[]` carries only summary fields, so this is a data-plane gap the UI is advertising as
a feature.

## Theme 7 — Numbers that disagree with each other (4 personas)

On one service page: USAGE `412 failed`, ISSUES `×486`. On the topic page: `observed 9.1k` /
`errors 486` and, two lines below, `10,702 calls · 412 failed (3.8%)`. `shipping:book` shows
`observed 0` directly above `5,207 calls`. Two vocabularies (`exception` vs `service-unavailable`).
Nothing says which is authoritative or that they are different planes over different windows.

- Delivery owner: *"a dashboard I have to caveat is a dashboard I don't open."* Would not screenshot
  the topic page at all.
- QA: *"I will not paste a figure into a ticket I'd have to defend in a stand-up."*

## Theme 8 — The Live window control is inert (4 personas)

Selecting 15m / 1h / 6h / 24h changes no figure on any page, while the caption reads *"counts cover
from …"* ~26 hours back under all four. Delivery owner: *"This poisons everything above it… it
silently invites me to misdescribe every figure in the product."* Production support lost ~90 seconds
re-checking, and worse, *"it made me doubt every other number on the page."* Their single requested
fix: make it govern the numbers, or take it off the screen.

## Theme 9 — Two clocks, unreconciled (5 personas)

The header reads `generated 2026-07-16T09:15:00Z` beside flows timestamped `2026-08-16` — a
**31-day-old snapshot** with no age, no warning, no styling. Production support nearly closed the tab
in the first 20 seconds. Architect will not drive it live in a review because of it. Platform
engineer: *"I make release-morning judgements about health, drift and topic status from a month-old
picture and never know I did."*

## Theme 10 — No business language (BA, delivery owner)

One of three services has a plain-English sentence. **No topic has a description of any kind.** The
BA's capability inventory was *"my own inference dressed up as fact"*, built by reading field names.

**Search matches names only** — not descriptions, not payload fields. Typing `email` returns *"No
topic or service matches"* while `customerEmail` is a field on two topics. She nearly wrote
"confirmed, no notification capability exists" on a false negative — *"exactly the answer that gets a
duplicate built."*

Jargon logged as unexplained: *plane*, *reserved*, *raw (benzene-message)*, *exemplar traces*,
*producer*/*consumer* (her mental model broke on `producers: none`), and *collector* / *aggregator* /
*usage feed* — *"three words for what I think is one thing. If they're different things, I've
misunderstood something structural."* The landing page's most prominent text is
`System.TimeoutException`.

## Theme 11 — Scope of claim is unstated (delivery owner, SHARP)

*"No consumers"* means *no consumers declared in this estate*. But "someone might still need it"
almost always means someone **outside** it — a warehouse pull, a partner feed, a finance batch.
Nothing states the boundary, so the deprecation argument ends in an opinion fight anyway.

> "I'd rather be told the limit than infer it."

## Theme 12 — Security: dispatch is a bigger capability than it looks (security reviewer)

Verified against `Benzene.Mesh.Dispatch` source. **Sign-off: YES WITH CONDITIONS.**

Controls that are genuinely good and should be protected: default-deny in Production, **unset
environment treated as Production**, three independent opt-ins (`UseMeshDispatch()`, a separate
`dispatchUrl`, the gate), registry-bounded targeting (not an SSRF primitive), and a confirmation
checkbox that **resets after every send**.

Blocking conditions for any deployment with dispatch on a production registry:

1. **Dispatch is a data-egress primitive, not just a write primitive.** `HttpMeshServiceDispatcher`
   returns the target's response body verbatim and the console renders it — so `orders:get-all`
   pulls customer emails into the browser via a server-to-server call from inside the perimeter,
   bypassing the normal front door's auth. Caller-supplied headers pass through **unmodified**
   (verified: `authorization: Bearer forged` and `x-tenant: other-tenant` were carried). *"Access to
   the mesh page is functionally access to every registered service's data at that service's own
   privilege level."*
2. **No audit trail.** Zero logging in `Benzene.Mesh.Dispatch`. A real `payment:capture` for
   `amount: 99999` left no record outside the browser.
3. **No environment identification anywhere** — dev and prod meshes are pixel-identical, while
   `placement.region`/`placement.account` are already in the payload and typed in the UI, unrendered.

Advisory: topic is **not validated server-side** against the target's declared contract (dropdowns
are client-side only, so the real reachable set is every handler the service routes); read and
dispatch share one path/method/content-type so read-only access cannot be enforced by proxy, WAF or
IAM route policy; cloud account IDs are returned to the browser and never used.

**The meta-finding**: every load-bearing control is invisible from the product. *"A control I cannot
verify from the product is a control the next reviewer will not credit."*

## Bugs in recently-shipped code (found by personas, verified in source)

Owned honestly — these are from the Test Console work of 2026-08-15/16:

1. **`ComposePage` dispatches to the wrong service.** It resolves the target from a topic's
   **producers** (`selectProducerServicesForTopic`), but dispatch *invokes the target's handler* —
   so the target must be a **consumer**. Composing from `payment:capture` (producer `orders-api`,
   consumer `payments-api`) silently sent to `orders-api`. QA found it only because the response
   echoes the service back after the send: *"A tester following the obvious path tests the wrong
   service and never knows."*
2. **`toHash` returns `#fleet` for a partially-filled Test Console**, so the page misreports its own
   URL and the selection cannot be bookmarked — while the console's own copy promises *"service and
   topic are both in the URL."* Found independently by architect and developer.
3. **The Test Console's runbook copy contradicts the product's own posture.** It says *"Bookmark or
   link a filled-in console … as a step in a production runbook"* while `MeshDispatchGate` refuses to
   run in Production by default. Security reviewer: those two positions cannot both be right.
4. **The response panel discards `x-correlation-id`.** It is in the HTTP response and in the
   `ComposeResult` type; `MessageComposer` renders only `result.body`. QA: this is the one field that
   would have made a send traceable.
5. **Browser Back ejects you from the app** (→ `about:blank`). `routing.ts` uses
   `history.replaceState`, which never pushes a history entry, so no navigation is recoverable.
   Production support: *"At 3am back is muscle memory."*
6. **Mobile: the issue headline renders one character per line** at phone width — the single most
   valuable element in the product, on the device a PagerDuty link is actually opened on.
7. **Bad routes silently render the fleet page** while leaving the bogus URL in the bar
   (`#trace/<id>`, `#flow/<id>`, `#test`).

## What every persona said mesh should NOT become

Unprompted and near-unanimous — worth recording because it bounds the backlog:

- **Not a monitoring system.** Platform engineer: *"the moment there's a chart with a threshold on
  it, mesh is a worse Grafana and I'll stop trusting both."* Production support keeps alerting,
  paging, log search and time series.
- **Not an incident tool.** Lifecycle, comms and the rota stay in PagerDuty.
- **Not a test runner.** QA keeps collections, assertions, CI, and test-case management.
- **Not the authority on who may call what.** Security: *"Mesh should tell me a route exists; it
  should not become the authority on who may use it."*
- **Not intent or target state.** Architect keeps ADRs, the why, and the 18-month picture.

The seam every persona independently drew: **mesh answers "what does this estate declare, what is
actually running, and do they match" — because it is the only thing that knows what a topic, a
payload and a version are.** Everything else is somebody else's tool.

## Scale warning (architect)

The sample estate is three services. At forty: the service list becomes an unsorted scroll with no
grouping dimension (there is no owner/domain field to group on), the topics table breaks (no
pagination, no faceting — *"show me every topic with more than one producer"* is not expressible),
and the topology *"dies at roughly node six"* — it already occludes an edge behind a node at three.
The one pattern that scales is the issue inbox, *"because it's a queue, not a canvas."*
