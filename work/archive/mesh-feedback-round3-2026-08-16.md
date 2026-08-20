> ARCHIVED 2026-08-20: actioned — absorbed into `work/mesh-ui-product-vision.md`'s dated blocks and distilled by `work/mesh-ui-aims.md`.

# Mesh user-feedback round 3 — 2026-08-16 — breaking changes and contract drift

A **focused** round. Rounds 1 and 2 asked every persona to use the whole product; this one asks all
eight a single question, prompted by three observations from a live AWS deployment of the .NET mesh
example:

1. the service page is a flat list and wants grouping;
2. drift is flagged on the estate page, isn't clickable, and never says what drifted;
3. breaking-vs-safe is mechanically derivable from the schemas, and the tool should carry that
   cognitive load rather than the reader.

The persona brief was therefore narrowed to: **"can you tell what changed, and whether it breaks
you?"** Round 1 is `work/mesh-feedback-round-2026-08-16.md` (read its correction block first);
round 2 is `work/mesh-feedback-round2-2026-08-16.md`.

## Round metadata

- **Harness**: `benzene-ui` built bundle + stub collector on `http://localhost:8903/`, over a
  purpose-built **drift estate** (`compose-drift-estate.mjs`).
- **The estate is designed as an experiment, not a demo.** Six schema deltas, each mapped to a real
  `SchemaChangeKind` from the shipped `Benzene.Schema.OpenApi/Compatibility` engine, spanning every
  verdict class:

  | # | Topic | Delta | Kind | Engine verdict |
  | --- | --- | --- | --- | --- |
  | 1 | `payment:capture` | `amount` integer → number | `TypeChanged` | **Breaking** |
  | 1b | `payment:capture` | `currency` added, optional | `PropertyAdded` | Compatible |
  | 1c | `payment:capture` | `required` `[]` → `["orderId"]` | `PropertyBecameRequired` | **Breaking** (request) |
  | 2 | `orders:create` | `channel` added **and required** | `RequiredPropertyAdded` | **Breaking** (request) |
  | 3 | `orders:create` | `customerId` → `customerRef` | Removed + Added | the hard case |
  | 4 | `shipping:book` | `address.line2` removed | `PropertyRemoved` | Warning (request) |
  | 5 | `orders:get-all` | `total` removed from response | `PropertyRemoved` | **Breaking** (response) |

- **Deliberately withheld: any verdict.** The fixture carries the per-version schemas and the prose
  the product emits today, and nothing else. Whether to compute and show a verdict is precisely the
  decision under test, so the cognitive-load hypothesis had to be *measured* from the user side
  rather than asserted. Case 1's prose is the product's real wording — *"amount widened from integer
  to number"* — which reads as safe while the engine classifies `TypeChanged` as Breaking. That
  disagreement between a human's sentence and the engine's verdict is itself under test.

## The structural finding — verified in source before any persona was read

This is the frame for everything below, and it reverses the shape of the request. The user asked for
breaking-change *detection*. Detection already ships. What is missing is a **wire and a screen**.

### 1. The engine exists, is complete, and is not referenced by the mesh

`Benzene.Schema.OpenApi/Compatibility` ships nine types, including a full direction-aware taxonomy
(`SchemaChangeKind` × `SchemaDirection` → `ChangeCompatibility`, via
`SchemaCompatibilityRules.DefaultFor`). Its output type is, almost exactly, the thing every persona
in this round asked for:

```csharp
public class SchemaChange {
    SchemaChangeKind    Kind;           // PropertyRemoved, TypeChanged, RequiredPropertyAdded, …
    SchemaDirection     Direction;      // Request | Response | Event
    string              Topic;
    string              Path;           // "order:create.request.customerId"  ← field level
    string              Description;    // human-readable
    ChangeCompatibility Compatibility;  // Compatible | Warning | Breaking   ← the verdict
}
```

`SchemaCompatibilityReport` rolls those up to `Overall`, `HasBreakingChanges`, `BreakingChanges`.

**No `Benzene.Mesh.*` project references it.** The only occurrences of the string `Schema.OpenApi`
under `src/Benzene.Mesh.*` are in `CLAUDE.md` files and in a comment in
`LambdaMeshServiceSource.cs:28` explaining that a topic name is *deliberately hardcoded rather than*
referencing it. The product's answer already exists as a fully-formed C# type and has never been
connected to a screen.

### 2. What the mesh computes instead is a different diff, and it is a string comparison

`MeshAggregator.DiffTopicEntry` (`MeshAggregator.cs:567`) is the whole of the mesh's change
detection:

```csharp
if (!previousByKey.TryGetValue((entry.Topic, entry.Version), out var previous)) …
if (Canonical(entry.RequestSchema)  != Canonical(previous.RequestSchema))  changedSides.Add("request");
…
changes.Add(new MeshTopicChange(MeshTopicChangeKind.SchemaChanged,
    "Payload schema changed (" + string.Join(", ", changedSides) + ")"));
```

Two facts follow, and together they explain every persona finding in this round:

- **It keys on `(Topic, Version)`.** So it compares *v2 today against v2 yesterday*. It never
  compares *v2 against v1*. The user's question — "does the new version break my consumers?" — is
  not a temporal diff at all; it is a **cross-version diff within a single snapshot**, and the
  aggregator has no code path for it.
- **The comparison is string equality on a canonicalised schema.** It can only ever know *that*
  something moved, never *what*. `"Payload schema changed (request)"` is not a terse description of
  a known delta; it is the complete extent of what this code is capable of knowing. The same is true
  one level up, where drift surfaces as `spec hash changed: 5feaedb4… → b9b30797…` — a checksum
  shown to a human, which is a category error.

**The sequencing consequence is the important one, and it is the PO's main lever.** The two diffs
have wildly different costs, and the cheap one is the valuable one.

*Temporal drift* is expensive. `MeshServiceSnapshot` (`Benzene.Mesh.Contracts`) carries `SpecJson`,
`SpecHash` and `PreviousSpecHash` — and **no `PreviousSpecJson`**. The before-state does not exist
anywhere in the artifact, by contract design. So "what drifted since yesterday" is not a frontend
ticket and not even an aggregator ticket: it needs a **wire-contract change in `Mesh.Contracts`**,
which every language port must mirror, plus a storage decision about retaining prior spec bodies.
The platform engineer reached the same floor from the operator side: *"the sole piece of drift
evidence is an opaque token I cannot recompute"* — they tried sha256/sha1/sha512/md5 against the
`specJson` they were given and matched none.

*Cross-version compatibility* is nearly free. Both versions' schemas are already sitting in
`topics.json`, in the same document, in the same run. Comparing `payment:capture` v1's
`messageSchema` against v2's requires **no history, no new storage, and no wire-contract change** —
only that someone call the engine that already ships.

And cross-version is the question all eight personas actually asked. Not one asked "how did this
topic differ from yesterday"; every one asked "does the new version break the old consumers".
**The half with no storage cost is the half the users want, and it can ship first, alone.**

### 3. The compatibility panel that looks like it answers this reasons only over topology

`selectors.ts:213` passes `versionCompatibility` through from the aggregator untouched; the UI
derives nothing from schemas. The aggregator computes it from *who sits on which version number*. It
never opens a payload. Consequence, confirmed by the architect against this estate:

- `orders:create` — a renamed required field plus a new required field — is `isCompatible: true`.
- `orders:get-all` — a deleted response field — is `isCompatible: true`.

The two most dangerous changes in the estate are the two the compatibility signal declares fine,
because version topology happens to line up. The architect's framing: *"A compatibility signal that
goes green on a required-field rename is worse than no signal, because it launders the risk."*

This panel was the **most praised feature of round 2**. It is genuinely good at the question it
answers (*is anyone still on the old version?*) and silently wrong at the adjacent question readers
will assume it answers (*is the new version safe?*). Both rounds' evidence has to be held together:
keep it, and make the boundary explicit.

### 4. The Changes section already exists in the UI, and version routing makes it dead code

This corrects the architect's report, which stated the change description exists *only* as a `title`
tooltip. It is a tooltip on the Value page (`RetirementRow.tsx:37` — `<Chip title={change.description}>`),
but `TopicPage.tsx:144-149` renders a proper visible section:

```tsx
{entry.changes && entry.changes.length > 0 && (
  <section><h3>Changes</h3>
    <ul>{entry.changes.map((c, i) => <li key={i}><Chip>{c.kind}</Chip> {c.description}</li>)}</ul>
  </section>
)}
```

It never renders for a changed topic. `selectTopic` (`selectors.ts:296`) is
`topics.find(t => t.topic === topic)` — first match, i.e. **v1** — and v1 entries carry
`changes: []`. Every change in this estate is attached to a v2 entry, and v2 has no reachable page.
So the product's one honest, visible change surface is unreachable by construction.

This reframes the round-2 finding "a topic version is unreachable" from a navigation nuisance into
**the single defect that hides the change signal**. It is also the cheapest fix in this document.

### 5. The drift tile cannot be made clickable without a component API change

`EstateStats.tsx` renders `<div class="bz-stat"><span class="bz-stat-n">…` with no `onClick` prop
anywhere in `EstateStatProps`. The architect checked the computed style and found `cursor: auto` —
correctly concluding it is not a link and does not pretend to be. Making drift navigable is a props
change to a shared primitive used by all five tiles, not a wiring fix in `FleetPage`.

### 6. `isCompatible` is computed from an empty evidence set — and the intent to prevent that is already written down

The platform engineer's headline finding. **Checked against .NET source first, because it looked like it
might be my fixture's fault. It is not.**

`MeshTopicVersionCompatibility.IsCompatible` (`Benzene.Mesh.Contracts`) is:

```csharp
public bool IsCompatible => ProducedNotConsumed.Length == 0;
```

`BuildVersionCompatibility` (`MeshAggregator.cs:481`) emits an entry whenever
`produced ∪ consumed` spans more than one version — so a topic consumed at v1 and v2 with **no
in-estate producer** gets an entry, `produced = []`, therefore `producedNotConsumed = []`, therefore
**`isCompatible: true`**. The UI then prints *"Every version produced in the fleet has a matching
consumer"* directly above the word **none**.

That is the exact shape of every HTTP-fronted topic — where the callers are a website, an app or a
partner, i.e. outside the collector's vision. In this estate it fires on `orders:create`, the topic
carrying a renamed required field, and on `orders:get-all`. The two most dangerous changes get the
all-clear.

The bitter part, and the reason this is a design bug rather than an oversight:
`VersionCompatibility.tsx:27-28` already states the principle —

> *"Renders nothing when the aggregator emitted no entry — … painting 'compatible' over a check
> nobody ran would be worse than silence."*

The guard covers an **absent** entry. It does not cover an **evidence-free** one. The intent is
documented and the implementation misses by one condition, in a different repo from where the
comment lives.

### 7. The dependency tension resolves cleanly — checked, not assumed

The vision doc's roadmap already anticipated this round twice, filed as **Phase 4 field-level
compatibility**, both times with the instruction *"check `Benzene.Schema.OpenApi/Compatibility`
first"* (`work/mesh-ui-product-vision.md:686`, `:836`) — and already ruled that it is
**aggregator-derived, no Cloud Service spec widening needed**. Round 3 supplies the evidence to
promote it; it does not discover it.

The open question was where the computation lives, given `Mesh.Contracts`' standing *stay
dependency-light* rule. Project references settle it:

| Package | References |
| --- | --- |
| `Benzene.Mesh.Contracts` | `Benzene.HealthChecks.Core` — **one**, deliberately |
| `Benzene.Mesh.Aggregator` | Contracts, Abstractions.MessageHandlers, Results, Core.MessageHandlers, Http |
| `Benzene.Schema.OpenApi` | Swashbuckle, Microsoft.OpenApi(+Readers), Newtonsoft.Json, ByteBard.AsyncAPI.NET, +4 Benzene |

`Mesh.Contracts` is the wire-type package every language port mirrors; pointing it at
`Schema.OpenApi` would drag four third-party libraries into it and oblige Go, TypeScript and Python
to mirror an OpenAPI toolchain. Not viable. The aggregator already carries a comparable dependency
weight and already owns `DiffTopicEntry`.

**Resolution: compute in `Benzene.Mesh.Aggregator`, emit a thin serialisable result into
`Mesh.Contracts`** — a plain mirror of `SchemaChange` (`kind`, `direction`, `path`, `description`,
`compatibility`) with no reference to the engine. Contracts keeps its one dependency; the ports
mirror five scalar fields, not a toolchain. No spec widening, no conformance-fixture change beyond
the new result shape.

## Persona verdicts

| Persona | Can you tell what changed? | Is it breaking? | Trust verdict |
| --- | --- | --- | --- |
| Production support | No — drift is on the wrong service | n/a | *"Following this UI literally at 03:12, I roll back payments-api"* |
| Developer | **BLOCKED** on all four topics | Would have shipped the breaking `integer → number` believing it safe | — |
| Architect | **BLOCKED** — left the product for `topics.json` | **BLOCKED** — the ranked estate answer does not exist | **Screenshot only**, one panel |
| QA | **PARTIAL** — 2 of 4, ~25 min across 8 screens | **Would have signed off the breaking one** | **NO** — two blockers |
| Business analyst | **PARTIAL** — 3 of 4, by hand | Misled on the one that matters | Would not show a stakeholder |
| Delivery owner | 3 of 4, via the Test Console | **CONDITIONAL NO-GO** | One screen, heavily caveated |
| Platform engineer | No — v2 is unreachable | Four wrong conclusions, all confident | **NO** — would not trust on release morning |
| Security reviewer | No, on any page a reviewer lands on | Two of four are security-relevant; both invisible | **YES with conditions** |

Not one of the eight could answer the round's single question from the UI. Every persona who got an
answer got it by leaving the product — `topics.json` directly (architect, platform engineer,
security), or by abusing the Test Console's version dropdown as a diff viewer (BA, delivery owner,
developer).

### The finding of the round — the product's verdict was wrong, and a human comment was the only control

QA came to sign off *"when a payment is captured, the shipment is booked automatically"* —
`payment:capture` in, `shipping:book` out. `shipping:book` v2 deletes `address.line2`, on the output
topic of the story under test. What the product told them:

- version-compatibility panel: **"Every version produced in the fleet has a matching consumer."** Green.
- topic catalogue STATUS column: **`ok`**.
- topic page PAYLOAD section: **still displays `line2`** (it renders v1).
- Value page: a green dot under **"NO RETIREMENT SIGNAL"**.

Four surfaces, four all-clears, on a change that deletes an address line. QA's own words:

> *"On this product's own verdicts I would have raised one blocker, not two. … Without that comment,
> a real address field would have gone to production deleted, signed off by me."*

The comment was Priya's free-text note on a *different* service's page: *"The schema mismatch on
`shipping:book` is the real issue to chase."* Three personas independently identified that note as
the strongest contract signal in the estate — and round 2 established that the same note is
**demonstrably wrong on its own terms** (`schemaMismatch` is `false` on every topic). So the only
control that worked is a human sentence that the system cannot verify, that contradicts the system,
and that the annotations-removal plan (task #18) proposes to delete.

That is not an argument to keep chat. It is the sharpest possible argument for computing the verdict:
**the human note is doing the tool's job badly, and it is still beating the tool.**

### The unanimous findings

**1. v2 is unreachable, so the change signal is structurally hidden.** All eight. `selectTopic`
returns the first match — v1 — and the route carries no version. Both the v1 and v2 rows in the
estate table link to the same URL. The topic page therefore renders the **pre-release** contract
with a `v1` chip and no indication a v2 exists with a different shape. Security reviewer, sharpest:

> *"A DPIA driven off `#topic/shipping:book` records that the flow carries `address.line2`; off
> `#topic/orders:get-all`, that it returns `total`. Both untrue at v2. A data map that is confidently
> wrong is more dangerous than no map, because it gets signed."*

**2. The best answer in the product is in a `title` attribute.** Six personas, independently, named
the same string: `amount widened from integer to number`. It is hover-only
(`RetirementRow.tsx:37`), on the **Value** page — a *retirement* screen — under the heading **"NO
RETIREMENT SIGNAL"**, with a **green** glyph. Delivery owner: *"That one string is the best thing in
the product for my job today and it is the least reachable."* It cannot be screenshotted, linked,
pasted into a ticket, read by a keyboard user, or seen on a projector.

**3. Drift dead-ends in a hash, in three places.** All eight confirmed the user's observation, and
two extended it. The estate tile is a non-interactive `<span>` (`cursor: auto`). The `DRIFT` badge on
the service card *does* change the cursor — the BA clicked it four times assuming they had missed
something, which is worse than an obviously dead label. Both terminate at
`spec hash changed: 5feaedb410bf… → b9b30797f974…`. Delivery owner: *"not a number I would defend, a
number I would be laughed at for showing."*

And the two halves are never joined: `payments-api` carries `contractDrift: true`, `payment:capture`
v2 carries a `changes` entry, they live on different pages in different vocabularies (*drift* vs
*schema-changed*), the counts disagree (**1** vs **4**), and neither links to the other.

**4. The prose is wrong or radically incomplete on three of four topics.** Verified against the
schemas:
- `shipping:book` v2 is described as a **request** change. That topic has **no `requestSchema`** —
  only a `messageSchema`. The description names a schema the topic does not have.
- `payment:capture` v2's *"amount widened from integer to number"* is true and is the **least**
  consequential of its three changes; the `required` tightening `[] → ["orderId"]` and the new
  enum-constrained `currency` go unmentioned. Architect: *"a description that names one of three
  changes and picks the reassuring one is not an incomplete description; it is a misleading one."*
- Two topics say only *"Payload schema changed (request/response)"* — which, per §2, is the complete
  extent of what a canonicalised-string comparison can know.

**5. Severity is never computed, so every change looks identical.** The chip reads `schema-changed`
whether a required field was renamed or an optional one was dropped. Six personas asked for the
same thing in their own vocabulary: the architect wants a severity-ranked estate ledger, the
delivery owner wants "a consistent severity read across all four", the BA wants "changed and
harmless vs changed and something downstream now receives less than it used to", the security
reviewer wants changes touching identifiers, addresses and money separated from the rest.

### Where personas disagree — and it is instructive

**"Breaking" is four different predicates, and they do not nest.**

| Persona | Definition | Worst change in this estate, by that definition |
| --- | --- | --- |
| Developer | my deserialiser throws | `orders:create` — rename + required add |
| Platform engineer | this deploy pages someone | `orders:create`, and `payment:capture` v2 into a v1-only consumer |
| Architect | forces coordinated deployment across a team boundary | `orders:create` |
| Delivery owner | I owe someone a conversation I didn't have | `orders:create` — external callers, invisible |
| BA | a business process silently produces the wrong outcome | **`shipping:book`** — parcels to flats with no flat number |
| Security reviewer | a control keyed on a field name stops firing | **`orders:create`** (the *rename*, not the required add) and **`payment:capture`** (amount units) |

The two most interesting are the ones the engine would rank **lowest**:

- `shipping:book` — `PropertyRemoved` on a request is **Warning**, and the BA calls it the one that
  frightens them: *"Shipping will accept v2 quite happily — no error, no red anywhere — and we will
  send parcels to blocks of flats with no flat number on them."* Nothing errors. The engine is right
  and insufficient.
- The `customerId` → `customerRef` rename — the security reviewer's point is that a schema-diff
  review waves it through as cosmetic (same type, same format, same semantics), while *"every
  control keyed on the name `customerId` — log scrubbers, DLP field rules, a DSAR extraction map —
  silently stops matching. Nothing fails; the control just stops firing."*

**Consequence for the PO: a single severity scalar will not serve all six roles.** The engine's
verdict is necessary and is not sufficient. What every persona agreed on is the layer *below* the
verdict — *which named field, which direction, added/removed/renamed/retyped* — because each role
derives its own consequence from that. `SchemaChange.Path` + `Kind` + `Direction` is exactly that
layer, and it already exists.

### Test Console — the version dimension does not reach the wire

QA tried to run the one test this release turns on — *send v2 to a consumer that declares v1* — and
could not. They diffed the dispatch envelopes for a v1-selected and a v2-selected send:

```
{"service":"payments-api","topic":"payment:capture","headers":{},
 "body":"{\"orderId\":\"3fa8…\",\"amount\":42.5,\"currency\":\"GBP\"}"}
```

**Byte-identical. No version field, no version header.** The `v1`/`v2` dropdown reseeds the textarea
and nothing else. QA: *"Flagging a risk I then can't exercise is half a tool."*

This is the same defect class as round 2's finding that the **transport** selector is decorative
(`sendComposed` sends `{service, topic, headers, body}`). Two selectors on one form, both purely
cosmetic. And the compose-skeleton route — the only diff method three personas found — **hides the
headline change**, because both versions stub `"amount": 0`, so `integer → number` is invisible
exactly where users went looking for it.

New Test Console defects this round, both cheap and both cost a persona real time:

- **Editing the Body silently un-ticks the confirmation checkbox** and re-disables Send with no
  message. QA burned three attempts believing their payload was being rejected. A handed-over test
  case therefore fails for the next person with no error.
- **Malformed JSON silently greys out Send** with no message. QA: *"'the tool blocked me' and 'the
  system rejected me' are opposite findings"* — and the console cannot distinguish them.
- `#compose/orders:create` offers a Service dropdown containing **only "Choose a service…"** — no
  options at all. That is round 2's confirmed producer/consumer inversion biting again: the page
  resolves targets from `selectProducerServicesForTopic`, and this topic has no in-estate producer.
  A third confirmation of a bug already logged twice.

## Harness artifacts — DISCARD

Held to the round-2 discipline: my fixture's faults do not enter the backlog.

1. **The `shipping:book` producer contradiction** (reported by four personas as the product
   contradicting itself) is **my composition error**. `compose-drift-estate.mjs:124` gives both v1
   and v2 topic-level producers `[orders-api, payments-api]`, while line 157 hand-writes
   `producedVersions: ['v2']`. The real aggregator derives both from one pass and could not disagree
   with itself this way. **Discard.** The round-2 finding underneath — that the product renders two
   differently-derived views of one contract adjacently and never marks which is authoritative —
   already stands on its own evidence.
2. **`payment:capture` v2's required set differing between `topics.json` and
   `services/orders-api.json`** — I authored new topic schemas and carried the service snapshots over
   unchanged. **Discard**; same round-2 finding.
3. **The `spec` link showing `0 TOPICS / 0 SCHEMAS`** — my server maps the spec page without its
   artifacts. **Discard**, as in round 2. Noting it recurred: it cost the BA ten minutes of trust in
   the whole site, so if the real deployment can ever serve that page unfed, it is worth a check.
4. **"Drift is on the wrong service"** (production support: *"Following this UI literally at 03:12,
   I roll back payments-api"*) is **half mine**. `contractDrift: true` sits on `payments-api` in the
   base service snapshots, which I carried over unchanged while authoring the topic changes onto
   topics produced by `orders-api`. That *specific* misattribution is my composition. **Discard the
   instance.**
   **Keep the mechanism**, which is real and was reached independently by the platform engineer:
   service-level `contractDrift` (a spec-hash comparison on one service) and topic-level `changes`
   (a schema comparison on one topic-version) are computed by different code paths, rendered on
   different pages, named differently (*drift* vs *schema-changed*), counted separately (**1** vs
   **4**), and **never joined**. A correct deployment can therefore still badge one service while
   the change that matters is on another's topic, and the product offers no route between them. The
   fixture made this vivid rather than inventing it.
5. **"The system accepted every invalid payload"** — QA ran eight negative cases (`{}`, missing
   required field, wrong type, bad uuid, negative amount) and got byte-identical `accepted: true`
   from all of them. **That is my stub.** As in round 2, QA caught it themselves, via
   `x-correlation-id: cid-15-stub` in devtools. **Discard the acceptances.**
   **Keep the product finding**, which is stronger stated this round: the console's own copy
   promises *"the same routing, validation, and handler a real transport would use … it is not a dry
   run"*, and nothing on screen contradicts that when it is untrue. QA: *"I nearly filed a P1 against
   the wrong team … it manufactures false defects."* A tester must be able to tell a real handler
   from a stub **without opening devtools**.
6. **`orders:create` having zero producers is NOT an artifact.** It is the case under test — an
   HTTP-fronted topic whose callers are outside the estate — and it is what exposed §6. Keep.

## The service-page grouping observation — confirmed by all eight, with a specific cause

The user's first observation ("a bit of a list… things that go together naturally to the eye appear
to go together") is confirmed unanimously, and the personas located *why* it costs them on this
question specifically.

`ServicePage.tsx` renders **eight sibling `<section>` elements** — About, Health, Usage, Topics,
Calls, Flows, Issues, Discussion — each with a bare `<h3>`, no wrapper, no cards, no rules. Only
Issues and Discussion land in visible cards, which several personas read as "the only real content".

Three concrete costs, all on this round's question:

1. **The contract material is split by 450px of liveness telemetry.** `Contract drift` is the third
   row of ABOUT; the `TOPICS / CONSUMES / PRODUCES` list carrying the version numbers is four
   sections below, with HEALTH and USAGE between them. Architect: *"Those two blocks are the same
   fact — this service's contract surface and the fact that it moved — and the layout puts 450
   vertical pixels of liveness telemetry between them."*
2. **The deciding line is styled as a timestamp.** Both production support and QA noted that
   `Contract drift` renders in the same typographic weight as `Snapshot taken` directly above it.
   QA: *"The one section that decides a release blocker should not be indistinguishable from a
   timestamp."* QA scrolled past TOPICS on all three service pages.
3. **The `h3`/`h4` hierarchy doesn't read.** `Topics` contains `Consumes`/`Produces`; `Calls`
   contains `Outbound`/`Inbound`. The BA read `shipping:book v2` as belonging to CALLS rather than
   to what the service **produces** — *"a meaningfully different statement"* — because six headings
   render as six peers rather than two groups of two.

Note the contrast three personas drew unprompted: **the estate page already groups into cards and is
genuinely scannable.** Delivery owner: *"the estate page reads like a product, the service page reads
like a data dump."* So this is not a missing design language — it is one page that didn't get it.

## What every persona said mesh should own, and not

Unanimous, and unchanged from rounds 1–2 except that this round sharpened it to a single sentence.
Platform engineer:

> **"Mesh is a contract register, not a monitor. Every '0' it prints from a numeric feed is a
> liability. Every version-reconciliation statement it prints is the reason I'd deploy it."**

Every persona volunteered, unprompted, that they keep Grafana/Splunk/CloudWatch, Jira/Confluence,
C4/ADRs, SAST/DAST/CSPM. Nobody asked mesh to replace any of them. What all eight said nothing else
they own can answer: **what the running estate's contracts are, how they changed, and who that
breaks.**

## The single most dangerous string in the product, this round

Two personas arrived at the same sentence independently, from opposite ends of the org. Delivery
owner, closing:

> *"A tool that says 'I don't know' is safe. A tool that says 'none' when it means 'none that I can
> see' is not."*

Platform engineer:

> *"Once I catch the tool saying 'fine' where it meant 'unknown', I re-verify everything it says
> forever, which makes it worthless."*

That is the same finding as round 2's *"absence rendered as good news"*, now hit on the exact
question the user asked about. It is the frame for the PO's assessment: **the breaking-change UI's
first obligation is not to compute a verdict, it is to never state one it did not earn.**

