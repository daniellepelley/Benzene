# Remaining issues, and the plan to close them

Written 2026-08-20, after the overnight quality sweep. Every claim below was checked against the
thing itself — the live registries, the published artifacts, the newest run of every workflow — not
inferred from the repo state.

## Where things stand right now

All seven repos are on `main`, clean, and in sync with origin. Every workflow's newest run is green:
benzene-dotnet (7 workflows), benzene-go (CI + drift + 10 cloud deploys), benzene-typescript (4),
benzene-python (3), benzene-patterns (all 8 pattern smokes). **There is nothing red and nothing
unpushed.**

The remaining issues are therefore not broken code. They are, in order of how much they cost a user:
what has been *released*, three cross-port design questions, and some housekeeping.

---

## P0 — the published artifacts lag `main`, and two of them are broken

This is the headline. Every port's source is in good shape; what people can actually `install` is
not. Tonight's fixes reach nobody until a release happens.

### P0.1 — TypeScript: the port is not installable at all

**Verified live against the registry, this morning:**

```
@benzenejs/abstractions   -> 200
@benzenejs/core           -> 404
@benzenejs/results        -> 404
@benzenejs/http           -> 404
@benzenejs/testing        -> 404
@benzenejs/mesh           -> 404
```

`npm install @benzenejs/core` fails today. The `0.1.0-beta.1` publish on 14 August died partway
through the alphabetical workspace order at `@benzenejs/azure-cosmos-db`; 25 of 129 packages are on
the registry and 104 are not. The re-run's `404 ... PUT` error reads like a broken trusted-publisher
setup, which is where the investigation went, so the real story — the first package is already
out — was never reached. Full detail in `benzene-typescript/work/npm-release-state.md`.

**Plan.** Do *not* publish the missing 104 at `0.1.0-beta.1`: the `@benzenejs/abstractions` on the
registry is the 14 August build, and abstractions has changed since (`BenzeneError` moved into it,
`IBenzeneResult.errors` widened), so consumers would get old abstractions with new dependents.
Instead:

1. Bump all 129 workspaces to `0.1.0-beta.2`.
2. Publish the whole set in one run. `scripts/check-not-already-published.mjs` now runs in the
   release workflow's build-and-verify job and will name any collision before anything is pushed.
3. Verify `npm install @benzenejs/core` from a clean directory afterwards, not just that the
   workflow exited 0.

**Blocked on:** your go-ahead. npm versions cannot be reused, so this is irreversible.

### P0.2 — Python: the published release ships the Core conformance defect

`benzene-core 0.1.0b1` (uploaded 14 August) is what `pip install` gives you today. I downloaded the
wheel and checked: it contains **zero** occurrences of `isSuccessful`. So the published package is
the one that omits a required, authoritative envelope member (§1.2), and the one whose unhealthy
health check throws away its own per-check report. Both were fixed on `main` last night.

**Plan.** Release `0.1.0b2` from current `main`. No version-collision risk here — the fix is a
straight forward bump. **Blocked on:** your go-ahead (PyPI versions are also permanent).

### P0.3 — .NET: alpha.3 does not contain the ergonomics it is assumed to

NuGet's latest is `0.0.3-alpha.3`, which looks like it should have unblocked the pattern-example
cleanup. It does not. I resolved the published package and reflected over it:

```
Benzene.Microsoft.Dependencies 0.0.3-alpha.3
  GetConfiguration: found=True abstract=True virtual=True
```

Still **abstract**. `b3d3b5f` ("Give BenzeneStartUp.GetConfiguration a default") landed *after*
`eca6227`, the commit the Deploy Benzene workflow published from. Four commits are on `main` and not
in any release, including that one and `aecd10a` (the `MeshAggregationPass` single-writer gate).

**Plan.**

1. Publish `0.0.3-alpha.4` from current `main`.
2. Then, in benzene-patterns, do the cleanup already scoped in
   `benzene-patterns/work/waiting-on-the-next-alpha.md`: bump the pin, delete the
   `GetConfiguration` override from the 20 of 25 StartUps whose body is exactly the new default,
   leave the 5 that build a richer configuration alone.
3. Re-run the five end-to-end verifications that doc lists (two-tier's four saga outcomes,
   modular-monolith's in-process/HTTP equivalence, choreography's one-emit-three-reactions,
   cqrs' read-model join, streaming's resume-from-failure) rather than assuming the bump is inert.

**Blocked on:** your go-ahead for the publish. Steps 2–3 need no further decisions.

### P0.4 — Go: the module has never been tagged

`git tag` returns nothing. A consumer can only pin a commit SHA. That may well be deliberate
pre-1.0, and it is why last night's `Result.Errors` type change was safe to make — but it is worth
being a decision rather than an omission, especially now that the other three ports have published
artifacts.

**Question for you:** tag `v0.1.0-beta.1` to match the others, or stay untagged until the API
settles?

---

## P1 — three cross-port design questions (recorded in `work/ceremony-parity-audit.md`)

These are the ones I deliberately did not act on overnight. Each needs your call, not more code.

### P1.1 — Should an HTTP client carry a peer's `errors`?

Go's `httpclient`/`awslambdaclient` now rebuild a failed `Result` from the peer's problem document
with `field` and `code` intact. .NET's `HttpStatusCode.Convert` and TypeScript's
`HttpContextConverter` map the status code and read nothing from the body, so a failure through them
has an empty `errors`.

They are not the same shape, which is why this is a question: Go's clients speak the wire
**envelope**, so the body always *is* a problem document; .NET's and TypeScript's HTTP client calls
an arbitrary verb+path endpoint whose body is whatever that endpoint returns. Reading `errors` there
means guessing the peer speaks RFC 9457. No fixture requires it and the two ports agree with each
other today.

**The question:** should a failure from a *Benzene* peer over the HTTP client carry that peer's
`errors` — and if so, how does the client know the peer is one? (A negotiated content type of
`application/problem+json` is the obvious candidate; it is already emitted.)

### P1.2 — Should Go and Python attach the *received* problem document?

.NET's envelope client attaches the document it received, so `result.GetProblem()` returns exactly
what the peer sent rather than one re-derived from the status. That is what preserves an
application-owned `type` URI across a hop **inbound** — the mirror of what `ProblemResult` /
`Result.problem` does outbound. Go and Python now carry the peer's `errors` but re-derive the rest,
so an app-authored `type` still dies at the first hop inbound.

Small and well-shaped, but it is new public API on two ports for something nobody has asked for.

### P1.3 — `mesh-service-version-cases.json`: claim it or stop vendoring it

All four ports vendor this fixture. **None runs it.** It is legitimately conditional — required only
of a collector claiming service-version identity — so not running it is conformant. But vendoring it
in four places reads like intent. Either claim service-version identity and wire it up, or drop the
file from the snapshots so the coverage matrix stops looking like an oversight.

(The sibling `mesh-version-order-cases.json` is a cleaner case: only .NET implements §2.5 ordering at
all, so the other three correctly skip it. Worth confirming that is intended and not just unbuilt.)

---

## P2 — housekeeping I can do without a decision

### P2.1 — Python's type checking stops at the package boundary

CI runs bare `mypy`, which reads `files=` from `pyproject.toml`: 129 source files, clean. Run
`mypy .` and there are **43 errors**, 39 of them in `tests/`. Test code is where the awkward
type-level mistakes hide, and one of those 43 was the signal that led to the health-check bug last
night (`with_services` wanting a `Callable[[Container], None]` while the natural thing to write is a
fluent lambda). Proposal: extend the mypy scope to `tests/` and `examples/`, fix the 43, and let CI
hold the line.

### P2.2 — Go's example and template coverage is now measurable for the first time

Until last night CI tested 20 of 29 modules, so nine had never had their coverage reported. Now they
do, and it ranges from 30% (`kafka-helloworld`) to 100%. Examples are not held to the repo's
100%-for-library-packages rule, so this is not a defect — but the numbers are visible now and worth
one deliberate look to decide which, if any, deserve attention.

### P2.3 — Re-run the persona suite against the changed error surface

The mesh personas were last exercised before structured errors landed everywhere. A failure now
carries `field` and `code` end to end, which changes what several of those personas can see. Worth a
round to confirm the UI actually surfaces the new information rather than continuing to render
prose.

---

## Suggested order

1. **Say yes or no to the three publishes** (P0.1 npm, P0.2 PyPI, P0.3 NuGet). These are the only
   items with an outward, irreversible effect, and they are what turn last night's work into
   something a user gets. They are independent of each other and can go in any order.
2. **P0.3 step 2–3** (patterns cleanup) immediately follows the NuGet publish — already scoped, no
   decisions left.
3. **P2.1 and P2.2** can proceed in parallel with any of the above; neither needs input.
4. **P1.1–P1.3** whenever you want to settle them. Nothing is blocked on them; they are the
   difference between "these ports agree by accident" and "these ports agree on purpose".
