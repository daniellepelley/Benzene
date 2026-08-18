# The aims of the Mesh UI

**2026-08-18.** A decision instrument, not a vision. Its test: hand this to a designer with a
screenshot of any mesh screen, and they can say *"this element serves aim N"* or *"this element
serves no aim — delete it"* without asking me.

It **distils** rulings already made in `work/mesh-ui-product-vision.md` (§1, §2, §4, §5, §C5, §D8,
§D9, §E8), `work/mesh-ui-design-simplicity.md` and the seven persona rounds. Where those disagree,
this file rules and this file wins. Nothing here is new policy except **R10**, which is flagged.

---

## 1. What the mesh UI is for

> **It shows what an estate of Benzene services declares, what is actually running, and where the
> two disagree — so a reader can decide what to change, without reading source.**

Everything below is that sentence made checkable.

---

## 2. The aims

Six. **Aim 0 gates the other five**: no aim ships on a surface where aim 0 does not hold (§2,
outcome 0). Each is a question a named person arrives with.

### Aim 0 — "Can I trust what this screen just told me?"

- **Who asks:** everyone. It is the only aim all eight personas raised unprompted.
- **Data:** `manifest.generatedAtUtc`, `services/<n>.json:fetchedAtUtc`, `usage.windowStartUtc/EndUtc`,
  `fleet.window.countsWindowed`/`countsSince`, descriptor `degraded[]`, fleet `missingFeeds[]`,
  `compatibility.notComparedReason`/`truncatedPaths`/`notComparedSides`, and the UI's own feed-read
  errors. All published today.
- **Answered when:** every number on screen can be traced, without leaving the screen, to which feed
  produced it, over what window, and how old it is — and every empty thing says *which* kind of empty
  it is.

### Aim 1 — "What does this estate actually do?"

- **Who asks:** business analyst before writing a requirement; a developer joining the team; an
  architect assessing coherence.
- **Data:** `manifest.services[]`; `topics.json` — topic id, version, `producers[]`, `consumers[]`,
  `requestSchema`/`responseSchema`/`messageSchema`, `consumers[].httpMappings[]`;
  `services/<n>.json:specJson`.
- **Answered when:** a reader who cannot read C# can name, for any service, the topics it consumes
  and produces and the shape of each payload; and for any topic, who is on both ends.
- **Known data gap:** nothing on the wire carries what a topic *means*. `mesh.md` §2.1's schema
  vocabulary has no `description`/`title`, and `TopicsTopicsItem` has no description field. See §7,
  Aim 1.

### Aim 2 — "What changed, and does it break anyone?"

- **Who asks:** a developer about to ship a payload change; QA signing a story off; an architect
  reviewing a release.
- **Data:** `topics[].changes[].schemaChanges[]` (`kind`, `direction`, `path`, `description`,
  `compatibility`); `topics[].compatibility` (`overall` ∈ `compatible`/`warning`/`breaking`/
  `not-compared`, `baselineVersion`, `changes[]`); `versionCompatibility[]`;
  `services/<n>.json:specHash`/`previousSpecHash`.
- **Answered when:** every drift or change signal anywhere in the product leads, **in one click**, to
  the named fields that moved, the verdict per field, and the named services on the other side.
- This aim exists because of the live-deployment complaint of 2026-08-17: *"it tells me there is
  drift, but there's nowhere I can see where that drift is or what it might affect or whether it's a
  breaking change."* A badge whose body is a pair of truncated hashes fails this aim by definition.

### Aim 3 — "Who has to do something, and who is that?"

- **Who asks:** delivery owner planning a release; platform engineer on a rollout; production support
  deciding who to wake.
- **Data:** rollouts derived from `versionCompatibility[]` + declared consumers; the issue feed
  (`fingerprint`, `classification`, `service`, `topic`, `count`, `firstSeen`, `lastSeen`);
  `manifest.services[].owningTeam`.
- **Answered when:** the outstanding work is a list of *(named service, named topic, one sentence of
  what it owes, how long it has owed it)* — and the reader can reach a contact for that service.
- Mesh states the **constraint** between two named ends. It never states a plan, a sequence or a date
  (§D5, §D9).

### Aim 4 — "Is this earning its keep?"

- **Who asks:** delivery owner or PO defending a retirement; architect pruning an estate.
- **Data:** `usage.entries[]` (`topic`, `service`, `transport`, `status`, `count`, `avgDurationMs`,
  `source`) with `windowStartUtc`/`windowEndUtc`; live-plane `invocations`/`errors`;
  `topics[].consumers[]`/`producers[]`; `removedTopics[]`.
- **Answered when:** for any topic the reader gets either *a count, with its window, its transports
  and its age*, or *a named reason no count exists* — and the retirement tiers are built only from
  the first. Structural evidence (nobody declares a consumer) and observed evidence (measured, and it
  was zero) are separate rows, never one score.
- **Known data gap:** `usage.json` carries no per-topic coverage statement, so *"the feed did not
  cover this topic"* is still underivable (§5.1). Aggregator-side fix, not a spec change.

### Aim 5 — "What is wrong right now, and is it mine?"

- **Who asks:** production support at 3am; platform engineer after a deploy; a developer whose message
  is not arriving.
- **Data:** `manifest.services[].status`, `contractDrift`; heartbeat `lastSeen`; `services/<n>.json:health`;
  the issue feed; the four declared-vs-observed divergence classes (`mesh.md` §4.2).
- **Answered when:** the reader sees a worst-first queue where each row names the service, the topic,
  the classification, the count, the age, and has exactly one next click. Health is **a queue, not a
  canvas** — this is the strongest surface in the product and it stays a queue (§3.4).
- Deliberately **not the centrepiece.** This is an estate-comprehension product first.

---

## 3. One screen, one question

Every screen owns exactly one question. A screen that cannot be given one is named for deletion or
merge.

| Screen | Route | The one question it owns | Aims |
|---|---|---|---|
| **Estate** | `#fleet` | *What state is the estate in, and what should I look at first?* | 0, 5 → entry to all |
| **Service** | `#service/<n>` | *What does this service do, and what does it owe?* | 1, 3 |
| **Topic** | `#topic/<id>` | *What is this message, who is on both ends, and what changed?* | 1, 2 |
| **Changes** | `#changes` | *What moved in the contracts, and who has to move because of it?* | 2, 3 |
| **Issues** | `#issue/<fp>` | *What is failing, how much, and since when?* | 5 |
| **Value** | `#value` | *What could we retire, and what is the evidence?* | 4 |
| **Test Console** | `#test/<svc>/<topic>` | *Does this handler accept this payload?* | 2 |
| **Spec viewer** | separate app | *What is one service's full contract, in detail?* | 1 |

### Rulings on screens that fail the test

- **Compose (`#compose/<topic>`) is MERGED into the Test Console.** It has no question of its own —
  it is the Test Console entered from a topic instead of from a service. Two routes, two composers,
  one job. The topic entry point survives as a deep link *into* `#test`.
- **Discussion (a card on Service, a section on Topic) is DELETED.** Ruled removed on 2026-08-16
  (§B1, `mesh-ui-product-vision.md:1680`); still shipping. It serves no aim above. The decision-record
  job it was carrying is served by aim 3's obligation list, which is derived and cannot go stale.
- **Topics and Topology leave the Estate page.** Neither answers *"what should I look at first?"*;
  both answer aim 1. **Topics becomes a destination in its own right** — it is the estate's functional
  map and today it has no route at all. **Topology stays a small-estate affordance on that
  destination**, deliberately under-invested in (§3.4): it is not the surface that scales.
- **The Estate page's four exception banners become one block** — *declared and observed disagree* —
  with N rows. Four diagnoses rendered as four near-identical paragraphs is one fact with four
  costumes (`mesh-ui-design-simplicity.md`, move 2).
- **The navigation must reach every destination.** Four nav entries for eight screens is why readers
  report they cannot get back. Issues is reachable only through a *see all*; Topics is unreachable.
- **The Value page is renamed.** *Value* is the product's internal word, not the reader's. The
  question is *"what could we retire?"*

---

## 4. What the mesh UI will not do

Stated as flatly as the aims. Each is already ruled (§4, §D9, §E8); this is the single list. If a
proposed element serves one of these, it is out — no further discussion.

| # | Not this | It belongs to |
|---|---|---|
| 1 | Monitoring. **No chart with a threshold on it, ever.** No alerting, no paging. | Grafana, Datadog |
| 2 | Incident management — lifecycle, comms, rota. | PagerDuty |
| 3 | A test runner — no assertions, collections, CI integration, test-case management. | the CI suite, Postman |
| 4 | The authority on who may call what — no policy, allow-lists, entitlement views. | IAM, the gateway |
| 5 | Intent or target state — no ADRs, no roadmap, no *why*. Mesh reports what **is**. | the architect, Confluence |
| 6 | A trace or log store. Mesh hands off a correlation id and an optional configured deep link. | Splunk, CloudWatch, Tempo |
| 7 | Customer, order or revenue impact. Mesh counts messages; it has no entity model. | the analytics product |
| 8 | Broker state — DLQ, queue depth, retries, in-flight. A stale copy of it is the most dangerous number we could print. | the broker console |
| 9 | Time series, history browsing, charts. **Ages, not series** (§E6). | Grafana |
| 10 | A value score, index, or any single number that hides which fact moved. | — |
| 11 | Effort, size, cost or story-point estimates on an obligation. | Jira |
| 12 | Hosted discussion, comments or annotations. | the ticket, the channel |
| 13 | A future tense of any kind — no schedule, no plan, no "will land". | the pipeline |

Two of these must be **visible on screen**, not just recorded here: an unstated refusal reads as a
missing feature and gets re-asked. That is a demonstrated fact — twice (§E8).

---

## 5. The rules every surface obeys

Non-negotiable. A reviewer cites the number; the author fixes it or the element goes.

- **R1 — Absent is never zero.** A thing not measured renders as *not measured*, with the reason.
  A `0` where nothing looked is the product inventing good news.
- **R2 — A detection is not a finding.** Any badge, count or status must lead in **one click** to the
  named evidence underneath it. A hash pair is a change-detection primitive, not a finding; a bare
  boolean is a rumour with a border-radius (§5.5).
- **R3 — Two planes that disagree are both shown.** Declared, registered and observed are three
  facts from three sources. Never summed, never averaged, never one printed as another (§D8.8).
- **R4 — Never a verdict the product has not earned.** `not-compared` is a value with a reason, not
  a blank and never a tick (§C5). No "safe", "ready", "clear to deploy". Never green at estate level.
- **R5 — Every number states its window and its age.** A date without its age is not a date. A count
  without its window cannot be quoted.
- **R6 — Nothing load-bearing lives in a hover.** This product's evidence travels by screenshot into
  an incident channel and a steering pack; a screenshot has no hover, and neither does a screen
  reader or a phone.
- **R7 — No view requires reading C#, or Benzene's internal vocabulary.** *Plane*, *reserved*,
  *raw (benzene-message)*, *collector/aggregator/usage feed* on a reader-facing surface is the same
  defect as a stack trace on a landing page.
- **R8 — Say the verdict at full volume, the qualifier at half, the derivation on demand.** The
  honesty rules constrain *what* must be said, never *how loudly*. Three volumes, assigned
  deliberately, on every surface (`mesh-ui-design-simplicity.md`).
- **R9 — Status colour is never decoration.** Red, amber and the unknown tone mean one thing each,
  everywhere.
- **R10 — Every link leads to a page that agrees the thing exists.** *(New, 2026-08-18.)* The product
  must not contradict itself one click apart. Where a page knows only half of a subject, it renders
  the half it knows and names the other half as **unknown**, never as empty — an empty Consumes list
  is a claim about the service, not about the catalogue.
- **R11 — The static floor holds.** `Benzene.Mesh.Ui` is self-contained: no CDN, no build step, no
  external request, statically hostable. Anything needing a backend is progressive enhancement that
  degrades to a named absence.

---

## 6. How to tell an aim is met

Checkable. Each is a test a reviewer can run against a screenshot or a build.

| Aim | The test |
|---|---|
| **0** | Pick any number on any screen. Without leaving that screen, name its feed, its window and its age. Then find every empty region and name which of the five absences it is (empty / unwired / not-measured / not-yet / could-not-read). Any failure fails aim 0, and therefore fails whatever else that screen was doing. |
| **1** | Give a reader who cannot read C# a service name. In under two minutes they list what it consumes, what it produces, and one payload's required fields — from the UI alone. Then give them a topic id and they name both ends. |
| **2** | From **every** place the product says something changed — the estate tile, the service `drift` badge, the topic badge, the catalogue row — reach the named fields, their paths, and a per-field verdict in **one click**. Zero dead ends. Any signal that bottoms out in a hash, a boolean or an empty page is a failure. |
| **3** | Every outstanding item names a service, a topic, what is owed, and how long it has been owed. Nothing on the surface implies a date, an order or an estimate. A reader can get from the item to a contact. |
| **4** | For every topic in the catalogue, the Value surface shows either a count with window + transports, or a named reason there is no count. No topic sits in a retirement tier on the strength of an absence. |
| **5** | The queue is ordered worst-first, every row has exactly one next click, and a row that has gone quiet is distinguishable from a row that was never wired — by its words, not by inference. |

---

## 7. What is not met today

Read from `/workspace/benzene-ui/src` at commit `0f01052`, 2026-08-18. Honest, per aim.

**Aim 0 — partially met, and the largest source of the "messy" complaint.**
- Four separate exception banners stack on the landing page (`FleetPage.tsx`), each a one-line
  coloured paragraph a reader must tell apart by reading to the end of the sentence.
- **59** hover-only `title` attributes remain in shipped components. Each is a decision — promote or
  delete — and every one of them violates R6.
- The Estate page still opens with a five-tile KPI strip and **no sentence**: there is nowhere the
  product says what state the estate is in.
- `usage.json` has no per-source coverage statement (`Usage` in `contracts/generated.ts` confirms),
  so aim 4's "the feed did not cover this topic" cannot yet be said. Approved in §5.1, not built.

**Aim 1 — structurally served, semantically empty. The weakest aim in the product.**
- **No topic anywhere carries a description.** Not in the UI, not in `topics.json`, not on the wire.
  This is the one aim with a genuine coverage question against the Cloud Service spec, and it is
  **not yet decided**: §5.3 established that OpenAPI-shaped schemas can already carry `description`,
  but `mesh.md` §2.1's derivation vocabulary drops it. Decide before designing: either the derivation
  carries doc-comment descriptions through (spec + fixtures + reference move together) or aim 1's
  "what does this mean" half is permanently the reader's own inference from field names. I lean to
  carrying it through — it is the highest insight-per-byte addition available and it is derived, not
  hand-maintained — but that is a spec change and it gets written up properly, not assumed here.
- The **Topics catalogue has no route.** The estate's functional map is a collapsible section on the
  landing page.
- Ownership renders one string (`owningTeam`). The contact block approved in §5.2 — team, contact
  URI, repo, runbook, harvested from the discovery providers — has not landed, so aim 3's "reach a
  contact" test currently fails for that reason too.

**Aim 2 — much improved this week, and still has two holes.**
- Field-level drift and the *Since the last run* section shipped 2026-08-17 (`0f01052`). **Verified
  against fixtures and one live AWS estate only.** Say so.
- `schemaMismatch` is still a bare boolean rendered as a red badge with a tooltip and **no body**
  (`TopicCatalog.tsx`, `TopicList.tsx`, `TopicPage.tsx`). The aggregator holds both consumers'
  schemas when it sets that flag and does not emit the differing paths (§5.5, approved, not built).
  This is the *exact* shape of the complaint aim 2 exists to make impossible, still shipping.
- `SchemaTree.tsx` renders `format` and `enum` and silently drops `pattern`, `minimum`, `maximum`,
  `minLength`, `maxLength` — while the fixtures carry them and the usage feed counts validation
  errors on the topics that have them (§5.4, approved, not built). Pure render fix.

**Aim 3 — met at the obligation, unmet at the person.** The outstanding-work model is good and
honest. The "who is that" half is one team string with no contact.

**Aim 4 — met structurally, unmet on measurement.**
- The transport dimension exists only on the **service** usage panel (`ServiceUsage.tsx`); the topic
  grain has no per-transport breakdown, though `usage.entries[].transport` carries it. "Over which
  transports" is an explicit product promise and it is half-delivered.
- Without per-topic coverage (above), the structural and observed tiers cannot be kept fully apart.

**Aim 5 — met. Protect it.** The issue queue is the strongest surface in the product. Its only
defect is reachability: no nav destination.

**Across all aims — two shipped things that contradict rulings already made.**
- **Discussion is still in the product**, on `ServicePage.tsx` and `TopicPage.tsx`, four months after
  §B1 removed it.
- **The Test Console still tells readers to bookmark it "as a step in a production runbook"**
  (`TestConsolePage.tsx`), copy that §3.2 **withdrew** because it contradicts `MeshDispatchGate`'s own
  production default in the same product.
- **The approved `placement.environment` spec line (§5.6) has not landed in `docs/specification/mesh.md`.**
  Dev and prod meshes remain pixel-identical, which was accepted as a safety defect, not cosmetics.

---

## 8. What happens next

1. **Design against this file, not against the vision doc.** The vision doc keeps its history; this
   file is what a screen is checked against.
2. **The deletions first** — Discussion, the Compose route, the runbook copy, the four banners. They
   need no design debate and they remove reading from the two most-visited screens.
3. **Then the R2 sweep**: every badge in the product, walked, until each one reaches its evidence in
   one click. `schemaMismatch` and the dropped payload constraints are the two known failures.
4. **Then routing**: Topics gets a destination, Issues gets a destination, the nav reaches all of it.
5. **One open spec question, written up separately**: does the descriptor's schema derivation carry
   descriptions through? Aim 1 depends on the answer and nothing else in this file does.
