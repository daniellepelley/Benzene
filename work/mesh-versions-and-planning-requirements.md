# Service versions, compositions, and the planning plane — rough requirements

**Status: draft requirements for discussion.** Not a plan. The plan comes after these are argued over
and cut down. Written 2026-08-16, after Wave E and alongside `mesh-ui-design-simplicity.md`, which it
depends on.

---

## 1. The shift, in one paragraph

Mesh today is **present tense and single-estate**: it polls what is running, right now, in one place,
and reports on it. Every question it answers is implicitly a question about *whatever instance
answered the last poll*.

The proposal adds two axes:

- **Time.** A service does not *have* a contract; it **accumulates service versions**, each an
  immutable, dated snapshot of its contract at a build. History becomes real instead of depth-one.
- **Tense.** The same analysis runs over what *is* deployed, what the pipeline *says* is deployed, and
  what *would be* deployed under a plan.

And the idea that unifies them, which is the whole architectural bet:

> **Every question mesh answers is a function of a set of service versions.**
>
> Today that set is implicit and unnameable. Make it explicit, addressable and datable, and one
> engine answers *"is production coherent?"*, *"would this release be coherent?"*, and *"if I change
> this contract, who has to change and who has to deploy?"* — because they are the same question
> asked of three different sets.

That is what "the same base model underneath" has to mean concretely, and it is the requirement
everything else serves.

---

## 2. What already exists — do not rebuild any of this

Substantially more of this is already designed and built than the framing suggests. Getting this
inventory right changes the size of the work by a lot.

| Piece | Where | State |
| --- | --- | --- |
| **Service version as an identity layer** | `docs/specification/mesh.md` §2.4 | **Specified and normative.** Catalog key is `(service, serviceVersion)`. Conformance fixture exists: `conformance/mesh-service-version-cases.json` |
| **Side-by-side versions as a deployment mechanism** | `docs/specification/versioning.md` §5 (Mechanism C) | Specified, status *proposed* |
| **Build-time contract extraction** | `benzene-descriptor` (`Benzene.Descriptor`) | **Shipped.** Emits `{name}.service.json` from a *built but non-running, non-deployed* assembly, byte-identical to what `benzene:mesh:register` sends. No deploy, no socket, no cloud |
| **Pairwise contract compatibility taxonomy** | `Benzene.Schema.Compatibility` | Shipped, dependency-free |
| **Obligation derivation from a change's direction** | `benzene-ui/src/store/rollouts.ts` | Shipped — `Rollout`/`Obligation`, owner-vs-adapter from `SchemaChange.Direction` |
| **Pluggable artifact stores** | `Benzene.Mesh.Artifacts` + S3/Blob/GCS ports | Shipped |

Two facts from that table deserve to be stated on their own, because they carry the design:

**The snapshot mechanism already exists and already works.** `benzene-descriptor` does exactly what
was described — takes a built service, produces its contract at that point in time, without deploying
or running it. What it does not do is *keep* the result anywhere, or stamp it with a version and a
date. That is a smaller job than building an extractor.

**The identity layer is fully specified and completely invisible.** `mesh.md` §2.4 is careful and
good — *"a service version is an entity, not a shape"*, identity is extrinsic, deriving it from
contract content collapses the case it exists for. The collector keys on it. The UI has **zero**
references to `serviceVersion`. The concept is built and unused.

### 2.1 What is genuinely missing

1. **Persistence over time.** The collector keys by `(service, serviceVersion)` but only for what is
   *currently registered*; the aggregator publishes a snapshot of *now* and overwrites it next run.
   Nothing accumulates. There is no catalogue.
2. **Environments and deployment records.** There is **no environment concept in the spec at all**.
   `placement.cloud` is infrastructure, not a named environment, and `mesh.md` §2 explicitly notes the
   descriptor does not change between environments — only the wiring does. Nothing anywhere records
   *what version went where, when*.
3. **Compatibility over a set.** Today's compatibility is pairwise per topic. Nothing takes a *set* of
   service versions and reports whether that set is coherent.
4. **Tense.** No way to express a hypothesis.

---

## 3. The domain model

Rough shapes, deliberately minimal. Names are proposals; §7 opens the naming question.

**Service** — `serviceId`. Permanent, no contract of its own.

**ServiceVersion** — the new centre of gravity. Immutable, append-only, never rewritten.

| Field | Notes |
| --- | --- |
| `serviceId` | which service |
| `version` | the build identity. Comes from the pipeline. **Never derived from contract content** — `mesh.md` §2.4 forbids it, because two builds may declare identical contracts and still differ |
| `createdAtUtc` | when this build happened. The field that makes history and age possible |
| `spec` | the §2 `ServiceDescriptor` verbatim — topics consumed and produced, payload schemas, versions |
| `descriptorHash` | fingerprint of the contract, for drift detection. Not an identity |
| provenance | commit sha, pipeline run, artifact digest. Optional but cheap and it is what makes a finding actionable |

**Environment** — `environmentId`, a display name, and a **rank** (dev < test < prod). The rank is not
decoration: it is what lets the tool say *"production is four versions behind staging"* without being
told which is ahead.

**Deployment** — `(environmentId, serviceId, version, deployedAtUtc, actor)`. Append-only. The current
state of an environment is a **fold** over these, never a mutated row.

**Composition** — a set of `ServiceVersion` references considered together, plus a **tense**:

| Tense | Source | The question it answers |
| --- | --- | --- |
| **observed** | live plane registrations — today's mesh | what is *actually* running |
| **recorded** | deployment records | what the pipeline *believes* is deployed |
| **proposed** | authored by a reader | what *would* happen |

Three tenses, one type. That is the whole trick, and it is why this extends rather than forks the
product.

### 3.1 Version order — DECIDED: versions are sortable

**Maintainer decision, 2026-08-16: `version` is sortable.** Recorded here rather than left in §7,
because a great deal rests on it and because it is the kind of requirement that is cheap to impose now
and impossible to retrofit.

**What it buys.** Four things, and the third is the one that matters most:

1. A service's history is a **timeline** — newest first — without leaning on the build timestamp, which
   is the wrong field for the job and is wrong in practice (rebuilt artifacts, clock skew, pipelines
   finishing out of order).
2. *"Production is four versions behind staging"* becomes answerable, which is most of what an
   environment rank is for.
3. **A composition diff can state a direction.** Pinning a service to another version is either an
   **upgrade** or a **rollback**, and *difference alone cannot tell you which*. Without order the
   planning view can only say "these differ" — which is a diff, not a plan. This is the requirement
   that makes §4's question 3 answerable at all.
4. **A "tip" composition is definable** — the newest build of every service. That is the natural
   default starting point for a proposed composition, and it has no meaning without an order.

**What it costs, and must be nailed down.** "Sortable" is not a specification; a comparator is.
`"10"` versus `"9"` sorts one way as integers and the opposite way as strings, and two language ports
that guess differently will disagree about which version is newer. So:

- **The comparison rule is declared, from a small closed set** — `integer`, `semver`, `lexicographic` —
  carried on the record, not inferred from the value. Three comparators, pinned by conformance
  fixtures, identical in every port. Inferring the scheme from the string is the version of this
  feature that fails silently.
- **Order is only ever needed *within* one `serviceId`**, never across services. That is what makes a
  per-service declared scheme completely safe, and it should be stated as a constraint so nobody
  builds a global comparison that has no meaning.
- **Order is not lineage.** It tells you which version is *later*, never which *contains* the other. A
  hotfix `1.2.4` cut from a release branch while main is on `1.3.0` is correctly ordered and is not an
  ancestor of anything. The product must not imply otherwise — "newer" is a fact, "supersedes" is not.
- **`createdAtUtc` stays required.** It is a different fact, it is what the date/age rule renders, and
  sortability does not replace it. Where the two **disagree** — version 5 built before version 4 —
  that is a **finding**, not something to reconcile silently: it means an out-of-order pipeline, a
  rebuilt artifact or a backdated tag, and any of those is worth telling somebody about.
- **Mixed schemes within one service degrade honestly.** If a service switched from build numbers to
  semver, versions either side of the switch **cannot be ordered against each other**, and the product
  says exactly that rather than picking. This is the third state applied to an ordering.

**One consequence for the spec.** `mesh.md` §2.4 currently treats the identity as an opaque non-empty
string, and its case-2 fallback reads a substrate-assigned identifier — a published Lambda version
(integer-like, orderable) or a Kubernetes ReplicaSet name (a hash, **not** orderable). So requiring
sortability is a *tightening* of §2.4 that its own fallback cannot always satisfy. Two consequences
follow: the amendment must be written as an additional property of a *declared* version rather than of
all versions, and §2.4's *"operators SHOULD declare one per release"* gets materially stronger — an
undeclared version now costs you the timeline, the direction and the tip, not just a nice label.

### 3.2 The fourth thing, which falls out for free

**observed vs recorded** is a diff between two compositions, so the engine already computes it. And it
is the honest answer to a question the product currently cannot ask: *is the estate what we think it
is?* A version running that nobody recorded deploying, or a recorded version that nothing is running,
are both findings that no amount of polling can produce today.

---

## 4. What the tool must answer

Ranked by how often somebody will actually ask.

1. **Is this set coherent?** Given a composition: for every topic and payload version, who produces it
   and who handles it, and where are the gaps. This is today's estate view, re-expressed.
2. **What is the difference between these two sets?** prod vs staging, prod vs a plan, prod now vs prod
   last month. **This is the workhorse** — most real questions are a diff, and answering (2) well
   answers most of (1) and (3) as a side effect.
3. **If I make this contract change, who must change and who must deploy?** From a proposed spec delta:
   the affected services, split into *must change code* and *must merely redeploy*, plus the order.
4. **In what order — and is one release enough?** The highest-value output and the one most likely to
   be fudged. Some changes have **no valid single-release ordering** and require expand-then-contract
   across two. Saying so is more valuable than producing a sequence that cannot work.
5. **Is production what we think it is?** observed vs recorded.

---

## 5. Requirements

### R1 — Capture, at build time

- **R1.1** A build emits a `ServiceVersion` record: `serviceId`, `version`, `createdAtUtc`, `spec`,
  `descriptorHash`, provenance. Extend `benzene-descriptor`; do not write a second extractor.
- **R1.2** `version` is supplied by the pipeline (build number, tag, run id). The tool never invents it
  and never derives it from the contract.
- **R1.2a** The record carries its **ordering scheme** — one of `integer`, `semver`, `lexicographic` —
  declared, never inferred from the value. See §3.1: inferring it is the silent-failure version of this
  feature.
- **R1.2b** A declared `version` that does not parse under its declared scheme fails the build that
  emitted it. This is the one place the error is cheap, and every later surface depends on it.
- **R1.3** Emitting must continue to require no deploy, no network and no cloud. This already holds and
  is the property that makes the whole thing cheap to adopt.
- **R1.4** Publishing a record for an existing `(serviceId, version)` with a *different*
  `descriptorHash` is **drift** — two builds disagreeing about one declared version — and is reported,
  never silently overwritten. `mesh.md` §2.4 already rules on this.
- **R1.5** A service that declares no version still works: `mesh.md` §2.4 case 3 gives it exactly one
  service version, and that must not be reported as an error.

### R2 — Storage: the service version catalogue

- **R2.1** **Append-only, immutable keys.** No read-modify-write anywhere. This makes it safe under
  concurrent and multi-aggregator writes by construction, which is the open risk that was blocking
  E16 — resolved by a better model rather than by locking.
- **R2.2** Bounded by *builds × services*, not by estate size. This is the **first thing in mesh that
  grows without limit**, so it is also the first that needs a stated retention policy. That is a real
  new cost and should be argued, not assumed.
- **R2.3** Reuse the existing pluggable artifact stores (`Benzene.Mesh.Artifacts`: S3, Blob, GCS,
  filesystem). No new storage abstraction.
- **R2.4** **Readable with no estate running.** A planning session must work against the catalogue
  alone, offline. This is precisely what makes "future" possible, and it is a hard requirement rather
  than a nice-to-have.

### R3 — Deployment records

- **R3.1** A deploy emits `(environmentId, serviceId, version, deployedAtUtc, actor)`.
- **R3.2** Append-only. Current state is a fold. Retirement is an explicit record, not a deletion.
- **R3.3** Environments are declared, ranked, and carry no contract of their own.
- **R3.4** **Recording is optional, and its absence degrades to exactly today's behaviour.** Without
  deploy records mesh is observed-only, as now. This cannot become a hard dependency or every existing
  deployment breaks on upgrade — non-negotiable, and the same degradation discipline `mesh.md` §6
  already applies everywhere else.

### R4 — The analysis engine

- **R4.1** **One pure function**: `analyse(composition) → report`. No clock, no I/O, no environment
  awareness. Testable from fixtures, portable to every language port.
- **R4.2** **The existing estate views become a projection of it.** If today's mesh keeps a second
  implementation, the two will disagree, and "two numbers on two screens that cannot be reconciled" is
  a defect class this product has already been bitten by four times.
- **R4.3** Reuse `Benzene.Schema.Compatibility`. Do not fork the taxonomy; a second opinion about what
  "breaking" means is worse than none.
- **R4.4** Ordering output must include the outcome **"no valid single-release ordering exists"**, with
  the expand/contract split that would work. A sequence that cannot be executed is worse than an
  admission.
- **R4.5** **The third state applies unchanged.** A service in the composition with no `ServiceVersion`
  record is **unknown**, never *compatible*. Every honesty rule from Waves A–E holds on the new plane:
  no claim of safety, dates with ages, planes never merged.
- **R4.6** **A diff states its direction per service** — upgrade, rollback, unchanged, or
  **not orderable** — using the declared scheme (§3.1). `not orderable` is a real outcome, not an
  error: mixed schemes and undeclared versions both land there, and the product says so rather than
  guessing which way a change is going.
- **R4.7** Where version order and `createdAtUtc` order **disagree**, the report says so. It means an
  out-of-order pipeline, a rebuilt artifact or a backdated tag, and it is a finding in its own right.

### R5 — The planning surface

- **R5.1** A proposed composition is authored by **overriding an existing one** — start from prod, pin
  service X to version N — never assembled from nothing. Diff-from-reality is both the common case and
  the safe default.
- **R5.2** A plan is shareable by URL and holds no server state. Mesh is a static bundle with no
  backend and that must not change for this.
- **R5.3** **A plan is never mistakable for reality.** Tense must be unmistakable at a glance, at every
  depth, in a screenshot. This is a visual-system requirement and belongs to `mesh-visual-designer`;
  getting it wrong is the one failure mode here that could cause an outage.
- **R5.4** *(Scope fork — see §7.4.)* Expressing a contract change for which **no build yet exists**.
  Everything above compares real, built versions. Answering *"what if I added a required field?"*
  before anyone has built it needs either a schema editor in the tool or an import of a proposed spec.
  This is the boundary between "compare what exists" and "design what doesn't", and it is the single
  biggest scope decision in this document.

### R6 — Specification

- **R6.1** **The Cloud Service Profile gains nothing.** A running service already declares everything
  needed; the catalogue and deployment records are *pipeline* artifacts. Keeping the profile taut is a
  standing priority and this proposal, correctly scoped, does not touch it.
- **R6.2** New spec sections for the catalogue record and the deployment record — the **wire shapes**,
  with conformance fixtures. Storage layout stays an implementation choice.
- **R6.3** Any new wire topic follows the naming principle: Benzene's namespace, so `benzene:mesh:…`.

### R7 — UI

Listed last deliberately, and gated.

- **R7.1** A `ServiceVersion` becomes addressable and visible. The UI's current count of
  `serviceVersion` references is zero.
- **R7.2** Environment becomes a first-class selector, and every surface states which environment and
  which tense it is showing.
- **R7.3** The composition **diff** is the central new view, not a new band on an existing page.
- **R7.4** **Hard dependency: the simplicity work lands first.** The estate page carries eleven bands
  today. Adding a planning plane before that is fixed doubles a problem that is already the top
  complaint. See `mesh-ui-design-simplicity.md`.

---

## 6. What this subsumes

Worth being explicit, because the proposal **retires** backlog rather than only adding to it.

| Existing item | Effect |
| --- | --- |
| **E16 — obligation first-seen ledger** | **Superseded, withdraw it.** A dated catalogue gives obligation age directly and correctly. The sidecar was a workaround for having no history, and its open concurrency risk vanishes under R2.1 |
| **`POLLED_INSTANCE_CAVEAT`** | Demoted to a fallback. A recorded deployment set replaces "whatever instance answered the poll". The caveat stays for estates with no deploy records — which is the honest place for it |
| **D10 — distinct descriptor-hash rollup** | Mostly answered. *"Do the four instances agree?"* becomes recorded-vs-observed, with no new collector rollup |
| **`previousSpecHash` depth-one history** | Replaced by real history |

---

## 7. Open questions — these need a maintainer decision before a plan

1. ~~**Is `version` sortable?**~~ **DECIDED 2026-08-16: yes.** See §3.1 for what it buys, what it
   obliges (a declared comparator from a closed set, not an inferred one), and the one place it
   tightens `mesh.md` §2.4 beyond what that section's own fallback can always deliver. The remaining
   sub-decision, if you want to settle it now: whether the default scheme for a service that declares
   none is `integer` (matches "roughly a build number") or whether declaring the scheme is mandatory.
   My recommendation is **mandatory** — a default here is a guess wearing a specification's clothes.
2. **Who emits deployment records?** A `benzene-deploy` CLI, a pipeline task per platform, or does the
   spec define only the wire shape and leave emission to the operator? This decides how much of the
   work is Benzene's.
3. **Retention.** Versions per service, forever or pruned? First unbounded growth in the product.
4. **Does R5.4 belong at all?** Compare-real-builds is a much smaller, safer product than
   design-a-change-that-does-not-exist. My recommendation: **ship compare-real-builds first and
   completely**, because it is answerable entirely from published artifacts and it already covers
   questions 1, 2, 4 and 5 in §4. Treat hypothetical-contract editing as a separate later product
   decision, not a phase of this one.
5. **Naming.** Is `Composition` the right word for "a set of service versions considered together"?
   Candidates: composition, release set, lineup, bill of materials. The naming principle governs wire
   names but not domain vocabulary, so this is a taste call that wants making once, early.

---

## 8. Indicative shape of the work

Subordinate to the requirements above; included only to show the phases are separable and each is
independently useful.

| Phase | Content | Independently useful? |
| --- | --- | --- |
| **0** | Simplicity work on the current UI (R7.4 gate) | yes — it is the top complaint today |
| **1** | `benzene-descriptor` stamps and publishes `ServiceVersion` records; catalogue store; the three comparators with conformance fixtures; spec section | yes — a contract history with dates **and an order**, and drift detection between builds |
| **2** | `analyse(composition)`, pure, fixture-driven; today's estate views re-expressed as `analyse(observed)` (R4.2) | yes — removes a duplicate implementation and fixes obligation age |
| **3** | Environments and deployment records; recorded-vs-observed drift | yes — answers "is prod what we think it is" |
| **4** | Composition diff view; proposed compositions by override; ordering with the no-single-release outcome | yes — this is the planning product |
| **5** | *(Only if §7.4 is answered yes)* hypothetical contract changes with no build behind them | — |

Phases 1–3 are worth shipping even if 4 never happens, which is the test I would want any staged plan
to pass.
