# Wave E — delivered

**Round 7 → product owner (§E of `mesh-ui-product-vision.md`) → implementation.** Six commits in
`benzene-ui`, zero `[agg]`, zero `[wire]`, zero `[spec]`, zero `[coll]` — as the product owner
predicted when they wrote *"a product whose defect is rendering a discriminating fact
undiscriminatingly has, by definition, the data."* Every input was already published; in seven cases
it was already parsed into the store and thrown away.

The Cloud Service Profile is untouched. No new field, no new obligation on any conforming service,
in any language, in any port.

## The governing ruling

> **THE THIRD STATE IS NOT OPTIONAL AT ANY GRAIN.** Every figure resolves to exactly one of:
> *measured, with its window stated*; *measured as zero*; or *not measured*. A surface may never
> render the third as either of the first two.

The rule already applied to an unreadable **artifact**. Round 7 found it was not applied to an absent
**field** or a declared-missing **feed** — the same rule at two finer grains. Six persona reports
turned out to be one defect, which the product owner stated as: *the product repeatedly fetches a
discriminating fact and renders an undiscriminating one.*

## What shipped

| # | Commit | What |
| --- | --- | --- |
| E1, E2, E4, E6 | `4800f8c` | The gate. Service-level `missingFeeds` honoured on every panel; the live plane's own `health` read, with the two-plane precedence rule; never-heartbeated and undeclared services named; Value tiering split on `succeeded`/`failed`/`unrecognised`; the usage window read for the first time; feed health names what answered instead of diagnosing "unreachable" |
| E3 | `b40be3a` | `— NOT COMPUTED` on the three health tiles when the plane is wired and silent. `Services` stays a real number — the manifest is a fine source for how many services exist, which is a different question from whether they are up |
| E5 | `1cd5644` | The date/age rule: `formatStamp` + the `Stamp` primitive, and an `architecture.test.ts` gate that fails the build if any component interpolates a timestamp into the DOM outside it |
| E8, E9 | `779427f` | `instances` read for the first time: the polled-instance caveat is quantified on a multi-instance service and **withdrawn** on a single-instance one. Observed-but-undeclared services reach the page with their diagnosis |
| E10 | `ed8c0b8` | Four small verified truths: the `Calls` error rate gains its noun and its source; the usage feed's empty state says *handled by*; the service Traffic card reads both planes; the issue detail page states its headline once |
| E7 | `2343983` | The live window leaves the chrome for the two surfaces it governs |

500 tests passing, up from 470 at the start of the wave.

## The three rules that are now executable

Each of these was previously a fix at a render site, and each was reintroduced by the next render
site. That pattern is what these replace.

1. **`src/store/thirdState.test.tsx`** — no positive assertion survives an absent field or a
   declared-missing feed. The wave's gate; nothing else in E shipped without it.
2. **`src/components/architecture.test.ts`** — a timestamp reaches the DOM only through `Stamp`,
   which cannot render a date without its age. Verified against a deliberate violation before the
   commit landed.
3. **`src/components/sections/copyHonesty.test.ts`** — now audits **one entry per branch** rather
   than per function. Auditing whichever arm happened to be listed would have left the arm that stops
   hedging unchecked, which is the one most able to over-claim.

A fourth rule was fixed in the test harness itself: Testing Library's auto-cleanup could not see a
global `afterEach` under this config, so every rendered tree stayed in `document.body` for the rest
of its file and `screen` queries searched the union of every test before them. One new assertion
matched a tile belonging to the previous test's store. That is the same class of problem the harness
gate exists to prevent, one level down — evidence that looks exactly like evidence and is not.

## Two judgements worth recording

**A caveat added for honesty had become the thing blocking action.** `POLLED_INSTANCE_CAVEAT` is
true, and it turned every OWES/MOVED verdict on the product's best surface into a maybe. Withdrawing
it where the plane says it does not apply is the same third-state discipline as everything else in
the wave, applied to a hedge instead of a figure — and the withdrawal is only sound where the count
is genuinely known, which is why an absent or zero count keeps the unqualified sentence.

**Fewer controls, more stated windows.** The live-window picker's failures were failures of
*placement*: a global control over a per-surface fact. The ruling — a window control lives on the
surface whose data it governs, or it does not exist — removed a control rather than fixing one, and
that is the right direction for this product.

## What is deliberately not done

Wave F (E11–E15) — the console evidence block, the console truth pass, the print stylesheet, the
pre-send schema check, and configuration disclosure. All `[ui]`, all separable.

E16 and E17 are `[agg]`/`[wire]` and belong to the aggregator. **E16 carries an open risk the product
owner flagged and nobody has answered:** whether the aggregator's store supports safe
read-modify-write under a concurrent or multi-aggregator deployment. The obligation first-seen ledger
is a rewritten-per-run sidecar, so two aggregators racing would silently lose stamps — which would
make an obligation look younger than it is, on the one surface built to show that it is old. That is
a two-line question to put to the aggregator owners **before** two days of work, not after.
