# Running a mesh persona round — the method, and the four ways I broke it

Six rounds of persona feedback have driven the mesh UI from "screenshot only" to "I'd run a release
from it". The method works. What repeatedly did not work was the harness the method runs on, so this
file records both — the loop, and the specific failures that cost four persona headlines.

## The loop

1. **Compose an estate that is an experiment, not a demo.** Every round's fixture is designed around
   one hypothesis. Round 3 varied the *verdict* (one topic per compatibility outcome); rounds 5–6
   varied the *rollout state*, because four of those five scenarios carry the identical `breaking`
   verdict and need four different answers. A fixture that only varies one axis cannot test the other.
2. **Freeze the build.** Snapshot `dist`, serve the snapshot, and do not rebuild until every persona
   has reported.
3. **Verify the harness before spawning anyone** — `verify-harness.mjs`, below.
4. **Run every persona in parallel against the same commit**, each with their own job, and with the
   standing instruction to verify claims on screen before ranking them.
5. **Adjudicate every finding against source before it enters a backlog.** This has caught false
   findings in every round, including two of the product owner's and several of mine.
6. **Put the evidence to the product owner as a design brief**, not as a task list.
7. **Implement, then re-test with the same personas.** The re-test is where the *new* claims get
   examined; the first round only ever finds absence.

## The four harness failures, and what they cost

| # | Failure | What a persona reported |
| --- | --- | --- |
| 1 | `annotations.json` written with key `entries` | Every service and topic page white-screened. Three personas hit it; the round's detail plane was unavailable. |
| 2 | `topology.json` written with `reqPerMin` / `p95Ms` | *"'structural — no traffic observed' on every call edge … I stopped trusting it."* |
| 3 | Dispatch stub returning hardcoded `accepted`, echoing the request back | *"The product makes it easier to produce convincing false evidence than true evidence."* The real handler returns the target's own status and 404s an unknown service; the UI already renders that correctly. |
| 4 | A stale `dist` served after a killed build | Three genuinely-fixed defects reported as not fixed. The persona was right about what they saw. |

A fifth, smaller one: the harness never served `mesh-spec-ui.html`, which the real .NET middleware
serves, so a dead `spec` link was reported in three consecutive rounds before I adjudicated it.

**The common root cause is that the harness was checked by clicking through it rather than by
asserting it.** Every one of these produced output that looked exactly like evidence. A harness that
misreports the product is worse than no harness.

## The gate

`verify-harness.mjs` asserts, before a round starts:

- every artifact's **shape** against the committed fixture the product's own types are generated from
  — shape, not content, because the estate is meant to differ and the schema is not;
- that `usage` statuses are in Benzene's actual vocabulary, since an invented one is counted as a
  failure (correctly, and with disclosure) and makes a healthy estate read as 100% failed;
- that each service's spec describes **exactly** the topics the catalogue says it handles — a spec and
  a catalogue that disagree is the drift this exists to catch;
- that the dispatch stub behaves like `MeshDispatchMessageHandler`: unknown service → `not-found`, a
  version nothing declares → `no-handler`, a handled version → `ok`, and that the status actually
  **discriminates**;
- that the **served bundle** contains marker strings from the work under test. This is the check that
  would have caught failure 4, and the reason it exists is that verifying the code is not the same as
  verifying the artifact the persona was handed.

It exits non-zero, so it can gate the round. On its first run it failed — and the assertion was wrong
rather than the fixture, which is itself the point: the check is only worth having if it is held to
the same standard as the product.

## What the personas are for, and what they are not

They are a way to find the questions a role actually asks and the order they ask them in, and to
catch the product asserting something it has not earned. They are not a substitute for the product
owner: the PO overruled findings in rounds 5 and 6 that were real observations pointing at the wrong
fix, and refused two features that personas asked for twice. A persona reports what they experienced;
whether that becomes a change is a product decision.
