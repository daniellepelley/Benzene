> ARCHIVED 2026-08-20: actioned — absorbed into `work/mesh-ui-product-vision.md`'s dated blocks and distilled by `work/mesh-ui-aims.md`.

# Mesh user-feedback round 2 — 2026-08-16

A rerun of all eight personas after round 1's headline findings turned out to be fixture artifacts.
Round 1 is `work/mesh-feedback-round-2026-08-16.md`; read its correction block first. This file
records **what changed**, not everything — a finding repeated verbatim from round 1 is noted as
confirmed rather than re-argued.

## Round metadata

- **Harness**: `benzene-ui` at `3a61f05`, served with a stub collector, as before — **but over a
  composed rich estate** rather than the base fixtures. `contracts/artifacts/` carries variants that
  exist to widen generated types (`topics.versioned.json` has 4 topics, `manifest.minimal.json` has
  1 service), so serving one raw would swap one distortion for another. The composition took the
  base estate's breadth and added the dimensions the variants prove the product supports:
  `owningTeam` on all three services, and the `payment:capture` / `shipping:book` v1/v2 split with
  `versionCompatibility`. Everything served is derived from a committed fixture; nothing invented.
- **Deliberately preserved** so confirmed defects would still reproduce: `order:legacy-export` with
  zero rows in `usage.json`, and the old manifest clock against a live plane.

## Verdicts, round 1 → round 2

| Persona | R1 | R2 | Movement |
| --- | --- | --- | --- |
| Production support | YES (2nd tab) | **YES** — "unreservedly, for the first sixty seconds" | ↑ |
| Architect | MAYBE | MAYBE | — (same ask: history) |
| Business analyst | YES | YES | — |
| Delivery owner | YES | **YES** | ↑ confidence |
| QA | Read yes / test no | **Read yes / test no** | — (sharper) |
| Developer | MAYBE | **YES** | ↑ |
| Platform engineer | YES, but no release-morning trust | **YES, still no release-morning trust** | — |
| Security reviewer | YES w/ conditions | **YES w/ conditions** | conditions changed |

## Findings RETIRED — round 1 was wrong, the fixtures were the cause

1. **"Ownership is absent"** — was the #1 ask for three personas. With `owningTeam` served, production
   support used it to pick a rota, and the delivery owner said *"the coordination list fell out of
   the tool rather than out of three Slack threads."* The remaining ask is far smaller: resolve the
   team label to something pageable/@-mentionable.
   **But note a real presentation finding underneath**: the BA saw the same chip and still concluded
   *"there is no owner, team, squad, or contact anywhere on any service page."* Rendering is not
   communicating. Two of three business-side personas read it; one didn't see it as ownership at all.
2. **"Version renders as an em-dash"** — was the developer's "single worst thing I found". With
   `versionCompatibility` served, the VERSION COMPATIBILITY panel became the **most praised feature
   in either round**. Production support put it in a 3am escalation unprompted; the developer called
   it "the killer feature and it's already there"; the architect and platform engineer both named it
   the reason they'd return. The panel's own caveat — *"upcasters aren't visible to the mesh"* — was
   singled out by four personas as the honesty standard the rest of the product should meet.

## Findings CONFIRMED — survived the fixture fix, therefore real

- **The `order:legacy-export` contradiction.** The topic page says *"No usage source is wired, so
  traffic for this topic is unknown"*; the Value page says *"no traffic observed while the usage feed
  is wired."* Now hit by **three personas across two rounds**. The delivery owner: *"the single
  decision I most wanted to take away from this tool is the one it talks itself out of."* Still the
  #1 product defect.
- **Absence rendered as good news**, now with source lines from the platform engineer:
  - topology feed missing → *"No producer/consumer edges are declared yet — no registered service
    consumes another's topic."* A false claim about the reader's architecture. Worst finding, both rounds.
  - `TopologyGraph.tsx:71` — `const failing = e.errorRate != null && e.errorRate > errorThreshold;`
    Correct that null isn't *failing*; wrong that null is therefore *fine*. There is no third
    rendering — **though the same file already has one** (`unobserved` → dashed, per `mesh.md` §4.2).
    `EdgeList` renders the identical edge honestly as `errors unknown`.
  - service-level `missingFeeds` is parsed and dropped (used only in `TopicLiveStrip.tsx`), so a
    service whose trace feed is declared missing reads as *"No flows observed"*.
  - `TopicCatalog.tsx:100` — `null` topic status prints the word **`ok`**, on a topic carrying 310
    `service-unavailable`. A lifecycle field wearing a health word.
  - `selectors.ts:921` — `errors: statsAbsent ? 0 : …` manufactures a zero.
- **The inert Live window** — 4 personas again. "Poisons everything above it."
- **The two clocks** — a 31-day-old manifest beside live flows, unflagged. 5 personas.
- **Flows are a dead end** — exemplar trace ids are plain text, no flow row is clickable anywhere,
  `#flow/<id>` silently redirects to `#fleet`. Every persona that hit a failure left for
  Splunk/CloudWatch at this exact point.
- **Numbers disagree across screens** — 486/412, 9.1k/10.7k, `observed 0` above `5,207 calls`.
- **Drift is a hash pair.** *"Two hashes is not a diff."*
- **Search matches names only** — `email` returns nothing while `customerEmail` is a field on two topics.
- **No descriptions** — one of three services has a purpose sentence; no topic has one.

## NEW — only visible once the estate carried versions and ownership

1. **A topic version is unreachable.** `#topic/<id>` carries no version and `selectTopic` takes the
   first match, so `payment:capture` v2 and `shipping:book` v2 cannot be opened — clicking their row
   lands on v1. Found independently by the developer and the delivery owner. The developer's framing
   is the sharp one: *the page raises a version-compatibility alarm about v2 and then refuses to show
   v2's payload*, even though `topics.json` carries the v2 `messageSchema`. **A UI gap, not a
   collector gap.**
2. **Per-version traffic is fabricated.** `selectTrafficForTopic` (`selectors.ts:242`) joins on topic
   name only; `usage.json` rows carry `version: null`. So v1 and v2 both display the topic total.
   Platform engineer: *"If I'd shipped v2 last night and come here to check the cutover, mesh would
   tell me v2 is doing 10.7k. It is doing zero"* — the live plane says `invocations: 0` for v2.
   Architect: *"worse than showing nothing."* This is the absence class again, wearing a version column.
3. **Declared and observed producers are silently merged into one label.** `shipping:book` shows
   `Producers: none` while the live plane lists `providers: ["orders-api","payments-api"]` and
   `payments-api`'s own page shows the outbound call. Developer: *"For a blast-radius tool,
   'Producers: none' on a topic that two services are actively producing is the most dangerous single
   string on the screen."* The architect hit the same seam from above: an **undeclared edge**
   (`payments-api → shipping-api`, 6.2/min) that no contract explains and nothing labels as a
   divergence. *"That is the single most interesting fact in this estate and the UI has both halves
   and never joins them."*
4. **A `CONSUMES` claim is inferred but presented as declared.** The mesh asserts `payments-api
   CONSUMES payment:capture v1`, while `payments-api`'s own `specJson` declares only `payments:get`
   and `payments:get-refunds`. Developer: *"I'd have believed the wrong thing."*
5. **Two views of the same contract disagree.** `topics.json` says `orders:create` requires
   `customerEmail` (email); `services/orders-api.json` says it requires `customerId` (uuid). BA:
   *"The whole pitch here is 'documentation rots, the running system doesn't lie'. The first time I
   cross-checked two screens, they disagreed."* **Fixture-origin** — in a real deployment the
   aggregator derives one from the other — but the product finding is real: it renders two sources
   of one contract and never reconciles them or marks which is authoritative.
6. **Three-way health disagreement on one page.** Manifest says `unhealthy`, live plane says
   `degraded`, service page renders `Heartbeat healthy`. And the estate counter shows `0 DEGRADED`
   because it reads the manifest only.
7. **`payments-api` and `shipping-api` show no version at all** — `ServiceAbout` renders the row
   conditionally, and their specs carry no `info.version`, so the row silently isn't there.
8. **Capabilities are duck-typed client-side** (`typeof api.getFleet === 'function'`), so "no
   collector wired" and "collector wired but broken" are indistinguishable — and there is **no
   wiring/diagnostics view** anywhere, though `capabilitiesSlice` already computes exactly what an
   operator needs.

## The annotations evidence FLIPPED

Round 1's strongest defence of discussion threads does not survive round 2.

- **R1, architect**: Priya's note *"carries more contract-health signal than every automated
  indicator on the page combined."*
- **R2, architect**: that same note asserts *"the schema mismatch on `shipping:book` is the real issue
  to chase"* — and `schemaMismatch` is `false` on every topic, while `shipping:book` reports *"No
  schema published."* Listed among three self-contradictions that *"would cost me the room."*

So a free-text human note drifted away from the system it annotates and became confident
misinformation, with **no mechanism to detect it**. The BA still valued threads highly but noted they
*"exist by luck: someone happened to have commented"* — and could not add one, since every page reads
*"This mesh is read-only — no annotation endpoint is configured."*

What personas actually praised, in both rounds, was never conversation: it was a **durable, dated
decision attached to the artefact** (finance confirming a retirement; a drift classified as expected
and tied to `PAY-118`). That job is real. Chat is not what delivered it.

## Harness artifacts — DISCARD

1. **"The system rejected nothing."** QA sent ten invalid payloads (`{}`, `[1,2,3]`, string amounts,
   negatives); all returned `ACCEPTED`. That is the stub. **But QA caught it themselves** by sniffing
   `x-correlation-id: cid-15-stub` on the wire — and the product finding underneath is severe and
   real: **the UI renders only `result.body` and hides the response headers**, so a tester cannot
   distinguish a real acceptance from a stub. QA: *"it manufactures false passes and conceals the
   evidence that they're false."*
2. **`Producers: none` on `shipping:book` v1** is partly composition (the versioned fixture declares
   no v1 producer). The *product* finding — declared-only producers, observed ones dropped, neither
   labelled — stands independently and was reached from two directions.

## Bugs in the Test Console (shipped 2026-08-15/16), now twice-confirmed

1. **`ComposePage` dispatches to a topic's producer, not its consumer.** Dispatch invokes the
   *target's handler*, so the target must be a consumer. From the `payment:capture` page — which
   states *"Consumers: payments-api"* — compose sent to `orders-api`. QA, round 2: *"Had I used the
   obvious button on the obvious page, I would have 'tested' my story against the wrong service and
   signed it off."* Confirmed by QA in both rounds and by the developer.
2. **The Test Console permits targeting a non-consumer with no warning** — `orders-api` +
   `payment:capture` returns `ACCEPTED` though that service consumes neither.
3. **The transport selector is decorative.** `sendComposed` sends `{service, topic, headers, body}` —
   `transport` is never transmitted. `http` and `raw` produce byte-identical dispatches.
4. **Switching payload version or transport silently destroys a typed body.** Intentional in the
   reducer (a new version means a new schema) but delivered with no warning and no undo. QA lost a
   hand-written boundary case.
5. **`toHash` returns `#fleet` for a partially-filled console**, so the copy promising *"service and
   topic are both in the URL"* is wrong as written until both are chosen.
6. **Version renders as `vv1` / `vv2`** — `MessageComposer.tsx:53` does `` `v${v.version}` `` where
   version is already `"v1"`.
7. **Send is disabled with no explanation** — the confirmation checkbox is a good gate presented
   invisibly. Two personas read the DOM to find out why.
8. **The response panel discards `x-correlation-id`** — see harness note 1. The single field that
   would make a send traceable.
9. **Skeletons use type names as values** (`{"orderId": "string", "amount": 0}`) while a realistic
   example (`3fa85f64-…`, `42.5`, `"GBP"`) exists on the spec page and isn't reused.

## The boundary, restated by the personas

Unchanged from round 1 and stated more sharply. Platform engineer:

> "Mesh should never be the thing that tells me something is broken. It should be the thing that
> tells me what breaks if I change this."

And the best single summary of both rounds, same source:

> "The product is honest in the places a human wrote a sentence and dishonest in the places the code
> took a default, and I can't tell from the outside which kind of screen I'm looking at."
