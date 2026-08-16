# Mesh user-feedback round 6 — 2026-08-16 — deployment coordination, re-tested

The re-test half of round 5. Same six personas, same estate, same question — **what needs to be
deployed, what needs to go out together, and are the contracts working?** — against Wave D, which
shipped between the two rounds.

Round 5 is `work/mesh-feedback-round5-2026-08-16.md`. The design block it fed is §D of
`work/mesh-ui-product-vision.md`; what Wave D actually shipped is recorded at the end of that file.

## Round metadata

- **Harness**: `benzene-ui` built bundle on `http://localhost:8930/`, over the same five-service,
  six-topic, twelve-version mid-deployment estate as round 5, with the same live plane.
- **Two corrections to the round-5 harness**, both mine, both recorded in round 5's discard list:
  the topology edges now carry the artifact's real key names, and the usage rows use a status from
  Benzene's actual vocabulary rather than an invented one.
- **Method failure to record: the dist was not frozen.** Round 5 froze a snapshot precisely so every
  persona saw one commit. This round I rebuilt and redeployed the bundle **while two personas were
  still running**, to land fixes the first four had found. The architect noticed and flagged it
  unprompted — *"the working tree was dirty … so what I saw is not pinnable to a commit"* — which is
  the correct call. The findings below are all reproducible against `2afea8d`, but the round cannot
  claim single-commit attribution the way round 5 could. Freeze it next time, and batch the fixes
  until every persona has reported.

## The headline

Round 5's central finding was that **the badge marked the party that had finished, not the party
that was late** — reached independently by four personas from four different jobs. That is fixed, and
every persona confirmed it independently this round.

The movement on the standing verdicts:

| Persona | Round 5 | Round 6 |
| --- | --- | --- |
| Architect | PARTIAL / BLOCKED on the escape hatch · **SCREENSHOT ONLY** | Synthesis **SOLVED** · escape hatch PARTIAL · **LIVE**, with one page they would not open |
| Developer (`billing-api`) | "the natural first click tells him he has nothing to do" · **MAYBE** | First click leads with `OUTSTANDING · 2 contract moves` · **YES** |
| Production support | 45s to diagnosis, then ~2 min recovering from a badge pointing the wrong way | 65s to the same diagnosis **with the direction already correct** · runbook **YES**, upgraded from landing-page-only |
| Delivery owner | PARTIAL — "the batching and sequencing are mine" | **PARTIAL, strongly improved** — ~70% of the batching is the product's · **YES, unreservedly** |
| QA | SOLVED on reading / PARTIAL on proving | **YES for reading, NO for proving** — would not sign off; see the harness adjudication below |
| Platform engineer | **NO** on release morning | **MAYBE**, and "YES, I'd open it again" — blocked on one defect, since fixed |

The delivery owner's summary is the one worth keeping: *"for the first time the numbers reconcile
against each other, which means I can quote one."* Round 5's unanimous complaint was that mesh's
words were usable and its numbers were not.

## What round 6 found that round 5 could not

Every one of these is a defect in code or copy that Wave D itself introduced. That is the point of
re-testing: the round-5 findings were about absence, and these are about the specific ways a new
claim can be wrong.

### 1. The constraint was in the wrong tense for a constraint already broken

*"A must move before B stops"* is right only while B has not stopped. On `inventory:reserve` the
handler had dropped v1 already and the call was failing 100%, and the sentence described a deadline
that had passed as though there were time.

> *"The sentence describes a deadline that has already passed as if I have time, and at 3am, 'before
> X does Y' reads as 'not yet urgent'."* — production support

Detectable exactly — no service declares the baseline on the owning side — so the sentence now goes
present-tense and the state chip reads `gap live now` rather than `move outstanding`. Fixed in
`d1f9866`.

### 2. The product absolved the one service that was on fire

The worst finding of the round, and structurally the round-5 defect wearing new clothes.
`shipping-api` owes nothing — precisely because it moved first and moved correctly — so the
Outstanding block's empty state fired and said *"every version shipping-api declares is covered on
both sides of every topic it touches"*. Directly beneath it: `CONSUMES inventory:reserve v2`, an
issue card at 2,205 errors, an `UNHEALTHY` badge, and the estate's own table showing that version has
no producer at all.

> *"The product has taken the round-5 error — reward attaches to whoever finished — and promoted it
> from a badge to an **absolution sentence**. That is worse, because a badge is a hint and a sentence
> is a claim. … One sentence, whole product."* — architect

The sentence was generated from *"does this service owe a move?"* and worded as *"is this service's
contract surface healthy?"*. Fixed in `e445b61`: the empty state now claims only what it checked, and
the page gained the other half of the question — **what is owed to this service**, with the
counterpart named and the breach marked live where it is live. That second half also answers the
developer's *"who is blocked on me"*, which had been reachable only from a different screen.

### 3. `handle v2` reads as `swap to v2`

On a topic whose producer still emits v1, a version-only deploy kills the live path. The catalogue
knows which case this is; the row left the reader to infer it from two version lists.

> *"nothing anywhere says 'keep v1 up until orders-api retires it.' I inferred it from the
> Produced/Consumed lists."* — developer

Fixed in `d1f9866`, and suppressed where there is no live baseline traffic to strand.

### 4. Two roles independently asked for the same missing view

The delivery owner and the architect arrived at it from opposite directions — *"how many teams do I
book and which can start today"* and *"who is the bottleneck"* — and both assembled it by hand from
one service page at a time.

> *"The single most useful artefact I made this morning is one the product doesn't render:
> outstanding moves grouped by the service that owes them. … Assembling it took me twenty minutes."*
> — delivery owner

It is **not** the transitive coordination set, which stays refused. The architect made the argument
for the refusal better than the design block did, and then made the argument for this instead:

> *"it is O(services), it never explodes, it doesn't assert coupling that doesn't exist … A hub
> doesn't break it, because a hub that has already moved simply has a count of zero."*

Shipped in `2afea8d`, with the dependency it cannot see stated on the same block: every obligation is
startable by construction, and whether one of a service's *own* moves gates another is inside that
service and invisible to mesh — which is exactly the case the developer hit.

### 5. The refusal itself was silent

> *"Nowhere on screen does the product say 'we deliberately do not group services into a release
> train'. … A user who doesn't find it assumes the feature is missing and asks for it — which is
> precisely what I did last round."* — delivery owner

An unstated refusal reads as a missing feature and gets re-asked every round. It is now on the screen
where someone goes looking for it. Same for the rollout state names, which readers were
reverse-engineering from which verdicts happened to carry them.

### 6. The old surfaces still marked the mover

Wave D fixed attribution on the new surfaces and left the inherited ones alone, so one page held both
semantics four inches apart. A service's own topic list only ever contains versions that service
**declares** — so a verdict badge there always marks work it has already done. It now renders as a
neutral chip that keeps the fact and loses the alarm.

### 7. A version heading sat above another version's numbers

The most damaging defect of the round, found by the platform engineer, and the one an operator would
have acted on. The topic page defaults to the newest version when none is pinned, but the traffic
selectors keyed off the version in the URL — which on that same arrival is null — so they merged
every version and printed the old one's numbers under the new one's heading.

> *"`order:placed` **v2 has carried zero messages**. The live plane says `invocations: 0`. The heading
> says v2, the schema panel says v2, the compatibility panel says v2 — and the traffic strip merges
> v1+v2 and prints v1's clean 9.8k under it. … 'v2 is deployed and flowing cleanly with zero errors'
> is what that page says, and it is false."*

Every in-product link pins the version, so it was reachable only by bookmark, typed URL, pasted link
or runbook step — *"which is how I reach things"*. Two tests had encoded the merge as correct
behaviour and were rewritten: summing across services is right, summing across versions is not.
Fixed in `a75e310`, along with three in the same family — a latency figure printed for a version that
carried nothing, the breach check running on only one of the two arms (so the mirror-image outage
kept the calmest label in the vocabulary), and a failed fetch rendering as a statement about the
estate.

That last one is worth stating on its own. `catalogSlice` collapsed a failed read into `null` with a
`.catch(() => null)`, so a 404 on `topics.json` was indistinguishable from an empty catalogue and the
page said *"the aggregator has run but no service declared one"*:

> *"I go looking for a registration problem in five services instead of a 403 on one URL … This is
> one distinction — fetch failed vs fetch succeeded and was empty — thrown away in one `.catch`, and
> it costs the product its credibility on the exact axis it markets itself on."*

The live plane had said *"unreachable — no successful poll yet; retrying"* since round 2. The static
half now meets the same standard, and names the artifact.

### 8. The estate tile mixed two denominators

`9 / CONTRACT CHANGES / 4 awaiting a move` parses as four of the nine. It is not — 9 counts field
changes, 4 counts topics — and clicking the 9 lands on a page headed `6 rollouts`. The first number
on the estate page did not survive its own click-through. The note now carries its own denominator;
the tile's value is untouched.

## What the personas would still not do

- **Trend.** Fourth round asking, unmoved, and now the single largest gap. *"I am bringing a
  photograph to a meeting about a trajectory, and eventually someone will ask me to bring the
  trajectory instead and I'll go build it in a spreadsheet — and once it's in the spreadsheet, that's
  where I'll start."* (delivery owner). The architect: *"Blocks any argument for or against investing
  in contract discipline, because I can't show it working or failing."* This needs retained history,
  which mesh does not have and no wave has scoped.
- **Traffic split by version.** Third round asking. It is the number that decides Sev-1 from Sev-2,
  and the product now says explicitly that it cannot attribute it — which the support engineer rated
  as *"80% of the value"* of having it, because they know not to quote it. The answer itself is still
  missing.
- **Joining the rollout state to what is failing right now.** The architect and the delivery owner
  both hit this: three outstanding moves render identically on `#changes` while one is bleeding and
  one is silent. The product holds both facts on the same page and does not join them. This one
  crosses a line the design block drew deliberately — the rollout surface is a review surface, not an
  incident surface — so it is a question for the PO rather than a defect.
- **Retirement intent.** *"the entire model hinges on a date only the other team knows, which is
  nowhere in the product"* (developer). Mesh has no future tense by design; this is the cost of that
  ruling, stated plainly for the first time.

## The harness manufactured a finding again, and it was the round's headline

The QA engineer's verdict was **"yes for reading, no for proving"**, and they would not sign the
release off:

> *"I have nine screenshots that all say ACCEPTED, one of which I obtained by sending to a service
> that does not exist … the product currently makes it easier to produce convincing false evidence
> than true evidence."*

**Substantially my harness, not the product.** Verified in source before adjudicating:
`MeshDispatchMessageHandler` returns `not-found` for an unregistered service and otherwise serialises
the target's own `MeshDispatchResult`, and `MessageComposer` already renders that status and colours
the badge red when it is not ok. My stub collector returned a hardcoded `accepted` for every dispatch
and echoed the request back as the response body — so the persona was never able to see the correct
behaviour, and reasoned impeccably from what they were shown.

Same for the `spec` link 404 they have now reported in **three consecutive rounds**: the product links
to `mesh-spec-ui.html`, which `MeshSpecUiMiddleware` serves in a real .NET deployment and my harness
did not. I should have adjudicated that the first time it was raised.

**That is three harness-authored findings across rounds 5 and 6** — the `annotations.json` key that
white-screened the product, the topology field names that made every edge read unmeasured, and now
the dispatch stub. The pattern is that the harness is written to be *good enough to click through*
rather than to be faithful, and every time it diverges the round pays for it in a persona's headline.

The stub now mirrors the real handler: unknown service 404s, and a topic no service declares at the
requested version returns `no-handler` rather than green. Verified end to end before re-running. A
focused QA re-run against the corrected harness is in flight; its result is appended below when it
lands, because a verdict formed against a lying harness cannot be left standing as the round's
conclusion either way.

### The re-run, and a fourth process failure

The corrected stub discriminates exactly as the real handler does — verified end to end before the
re-run: an unregistered service returns `not-found`, a version nothing declares returns `no-handler`,
a handled version returns `ok`. QA re-tested and **five of their nine identical passes now
discriminate**:

> *"The last round's headline finding — 'I sent to a service that does not exist and got ACCEPTED' —
> is fixed and verified on screen. … The v1/v2 split is the single most valuable thing here: the
> estate's issue card says nobody handles v2, and the console now proves it at runtime. That is a
> claim I could not previously verify at all."*

Their verdict moved from an effective no to **"YES, I'd open this again"**, while still declining to
sign off — now for narrower and better reasons (they cannot demonstrate the acceptance criterion,
because nothing correlates a send with its downstream effect, and payload-level validation does not
discriminate).

**But they reported three of my claimed fixes as not landed, and they were right that what they
tested was broken — because I served them a stale bundle.** The rebuild in the harness-fix step was
killed partway and I copied the previous `dist` over it, so the deep-link seeding, the version
carried through "compose a message", and the discarded stale response were all absent from what they
drove. Re-verified against the correct build in a browser: all three work. That is the fourth
process failure of this block, after the unfrozen dist and the two fixture defects, and it is the
same root cause every time — **I verified the code and not the artifact the persona was actually
given.**

One finding from the re-run was real, and is the sharpest of the round because of its direction:

> *"It dispatched to **orders-api** — the producer — not `payments-api`, the registered consumer that
> the same topic page lists under 'Consumers'. A tester following the obvious in-product path gets a
> red failure for a working v1 path and would raise a bug that doesn't exist. Last round the product
> manufactured false passes; on this path it now manufactures **false failures**. Same disease,
> opposite sign."*

`ComposePage` resolved its dispatch target from the topic's producers. A dispatch invokes a topic ON
a service, so the target has to be the one with the handler. Fixed in `437c860`.

What was **genuinely** the product in QA's report, and is fixed in `98cbd1d`: a Test Console deep
link — the URL the page's own header invites you to bookmark for a runbook — loaded with no version
and no payload and would happily send an empty unversioned message; "compose a message" from a v1
page opened at v2; the response panel survived edits to the request, so a screenshot showed a green
v2 result beside a v1 request; and `#test/does-not-exist/<topic>` rendered a working, sendable
console, the one place a bad URL produced fake evidence instead of an honest empty state.

## Findings recorded but not acted on

- **The developer's field-flow ask** — that `taxJurisdiction` entering on `order:placed` and leaving
  on `invoice:raise` is the same field, and that one of their obligations therefore gates the other.
  The design block rejected this inference explicitly: field names matching is a coincidence, and
  inferring the causal chain from it is the rename trap. The ruling stands; what changed is that the
  product now *admits* the limit on the by-service block rather than leaving the reader to discover
  it. Worth putting back to the PO with this evidence, because it is the second round the same
  inference has been asked for by the person it would serve.
- **Identical change text, opposite verdicts.** `Property added (required)` scores `breaking` on a
  request and `compatible` on an event. The logic is right and the `REQUEST`/`EVENT` tag is on
  screen; nothing connects the tag to the verdict, so the verdicts read as arbitrary. Flagged by both
  the architect and the delivery owner.
- **Scale.** *"Six cards is a page; sixty is a scroll, and every card is equally sized and equally
  weighted regardless of whether it's a live incident or a compatible add-a-field."* No sort by blast
  radius, traffic, or currently-failing.
