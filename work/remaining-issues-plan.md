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

### P0.1 — TypeScript: the port is not installable at all — **done, 2026-10-15**

**`0.1.0-beta.2` of all 130 packages is on npm.**
- The Release workflow succeeded on run 38694481287, from benzene-typescript `13f30ee`, which
  includes spec 1.2.
- It went through once `NPM_TOKEN` was replaced with a granular token that bypasses two-factor.
- Checked against the registry: every `@benzenejs/*` package has `beta` = `0.1.0-beta.2`.
- A clean `npm install @benzenejs/core` resolves `0.1.0-beta.2`, with its dependencies at
  `0.1.0-beta.2`.
- The release-state note is archived (benzene-typescript `5b33167`).

**Open: maintainer actions.**
- **Point `latest` at `0.1.0-beta.2` on the 25 packages published in August** (`abstractions*`,
  `ajv`, `auth-*`, `avro`, `aws-lambda*`, `aws-sqs*`, `azure-cosmos-db`). Without a tag, those still
  install the August `0.1.0-beta.1`, whose Lambda bindings can lose a returned failure. Run
  `npm dist-tag add <pkg>@0.1.0-beta.2 latest` for each, logged in with two-factor.
- **Move publishing to trusted publishing before January 2027,** when npm plans to stop bypass-2FA
  tokens publishing directly. Detail: benzene-typescript `work/remaining-items.md`.

### P0.2 — Python: the published release ships the Core conformance defect

**Status (2026-10-13): ready to publish `0.1.0b2`** (benzene-python `551f39a`, CI green). The
inter-package floors are `>=0.1.0b2`, so no install can mix in the defective `0.1.0b1`; all 38
artifacts pass `twine check`. **Maintainer actions:** register `benzene-codegen-client`'s pending
trusted publisher on PyPI (new since b1, or the whole upload fails), then push tag `v0.1.0b2`.

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

**Done (2026-10-12).** `0.1.0-alpha.1` is published, a pre-release on the `0.1.0` base
(benzene-dotnet `e7af01d`, `main` green at `785e4ea`).
- **The Count** runs on it (the-count `8e244ac`): no code change, every test green, deployed, and
  answering through the connector.
- **benzene-patterns** pins it (`7c98e86`). All 25 `GetConfiguration` overrides were deleted, not
  20: the five in real-time-risk had the same default body. Two API changes reached the code:
  `Benzene.Abstractions.Pipelines` replaced the misspelt id, and `FailedShards` now carries
  `FailedShard<T>`. The bump also exposed four patterns still sending to `/benzene-message`;
  they now send to `/benzene/invoke` (the spec-0.2.0 path). All eight smoke workflows, which
  include the five end-to-end verifications, pass on that commit.

### P0.4 — Go: the module has never been tagged

**Status (2026-10-13): decided — `v0.1.0-beta.1`, ready to tag** (benzene-go `cbaf92e`, CI
green). Every nested module now requires `v0.1.0-beta.1` (they named an untagged `v0.1.0`).
**Maintainer action:** `scripts/tag-release.sh v0.1.0-beta.1` from a clean `main`, then the one
`git push` it prints (19 tags).

`git tag` returns nothing. A consumer can only pin a commit SHA. That may well be deliberate
pre-1.0, and it is why last night's `Result.Errors` type change was safe to make — but it is worth
being a decision rather than an omission, especially now that the other three ports have published
artifacts.

**Question for you:** tag `v0.1.0-beta.1` to match the others, or stay untagged until the API
settles?

---

## P1 — the three cross-port design questions: decided 2026-10-11

All three were settled by `archive/spec-simplification-plan.md` (D25, D26) and are in the spec: a client that
receives a problem document MUST rebuild `errors` with `field` and `code` intact, and MAY attach the
received document (core-concepts §12); `mesh-service-version-cases.json` is the Service Versions
module's fixture (Provisional: .NET), which the other ports skip with a written reason. **Port work
still open:** .NET's `HttpStatusCode.Convert` and TypeScript's `HttpContextConverter` read nothing from
a problem body yet — the core-concepts §12 MUST (closed in Phase 5, before 1.0).

**The 1.x items became spec 1.1 SHOULDs** (gRPC `code` in `FieldViolation.reason`; a client takes
the status from a received problem document's `benzeneStatus`; a send that fails in transport is a
`timeout`/`service-unavailable` result, one-way clients included) and all four ports implement
them (released as `spec-1.1.0`). **Port follow-ups the 1.1 work found:**
- ~~Python's marshaler could not write `UUID`, `date` or `Decimal`; `Literal` derived to `{}`~~ —
  fixed (benzene-python `356d447`, `8a0bedc`): standard JSON forms and matching §4.2 schemas.
  ~~`benzene-openapi`'s 3.1 generator ignored pydantic models~~ — fixed (benzene-python `e74a6ac`):
  `json_schema` reads a model as the marshaler writes it.
- ~~TypeScript's batch clients threw when batch creation failed~~ — fixed (benzene-typescript
  `157ac27`): reported per entry.
- TypeScript's mesh trace forwarding is opt-in per outbound route (`useMeshTraceContext`); the
  cloud service builds no routes to attach it to, and a raw `IBenzeneMessageClient` call forwards
  nothing — the same documented limit .NET's per-route wiring has.
- .NET's AwsMesh example forwards only the OpenTelemetry span, so it reports M2 missing:
  forwarding the mesh span there would split its X-Ray traces at the first hop.
- ~~Go's one-way clients never returned `timeout`: every transport failure was
  `service-unavailable`~~ — fixed (benzene-go `9a13703`): they classify through `client.CallFailure`.
- ~~Python wrote a naive `datetime` with no offset, though its schema says `format: date-time`~~ —
  fixed (benzene-python `78de3f6`): a naive `datetime` is taken as UTC and written `+00:00`.

---

## P2 — housekeeping I can do without a decision

### P2.1 — Python's type checking stops at the package boundary — **done, 2026-08-20**

CI ran mypy over 129 files: the nineteen packages and two deploy roots, and nothing else. Test code
was unchecked, which is backwards — it is where the awkward shapes live, and the only code that
exercises the packages from outside, the way a user does.

Pointing the checker at `tests/` and `examples/` found **58 errors**. Most were ordinary (an Optional
the test knows is present, a missing annotation), but three earned the exercise on their own:

- **Service overrides could not be written as a one-liner.** `Container`'s `add_*` methods are
  fluent, so `lambda c: c.add_instance(Greeter, fake)` — the form the module's *own docstrings* use —
  returns a `Container`. Typed `Callable[[Container], None]` it was rejected, and a user running mypy
  had to expand it to a four-line named function or reach for `# type: ignore`. Now a
  `ServiceOverride` alias returning `object`.
- **You could not `asyncio.run` your own handler.** `Handler` was
  `Callable[[Any], Awaitable[Result]]`, but `asyncio.run` requires a `Coroutine`, so the most natural
  way to unit test a handler did not type-check. `handler.py`'s own first line says a handler *is*
  `async def`, which produces exactly a Coroutine, so the narrower type states the documented
  contract rather than a superset of it.
- **A variable bound to two types in one function** in the problem-details conformance runner — a
  wire envelope dict and an `HttpResponse` — sloppiness written hours earlier that reading would not
  have caught.

The injection seams (`HttpTransport`, `HttpGet`) were deliberately *not* narrowed the same way:
an injection point wants the most permissive thing it can accept, so an `async def`, a partial
returning a Future and a mock returning a completed Future all satisfy it. `tests/_async.py` records
that reasoning and holds the wrapper on the test side instead.

`files` now covers **292 source files**, up from 129. Verified the guard bites: reintroducing one of
the fixed errors turns the CI-scope run red.

### P2.2 — Go's example and template coverage — **assessed, 2026-08-20; one real gap, now closed**

With all 29 modules reporting, the picture is 56 library packages (min 75%) and 44
examples/templates (min 0%). Examples are not held to the repo's 100%-for-libraries rule, so the
question was only whether any *library* package hid something. Nine sat under 90%. They fall into
three groups, and only one was a defect:

- **A real gap, fixed.** `wire` at 81.5%, with `ErrorPayload.Problems` (added the night before) and
  `ProblemHTTPStatus` both at **0.0%**. Both were exercised from elsewhere — the conformance runner
  reads the registry, `httpclient`'s round-trip reaches `Problems` — so a green suite and an untested
  function looked identical from inside the package. A dependency-free package's own tests should
  stand on their own. Now **98.8%**, and chasing the last branches turned up a genuinely reachable
  untested path: the legacy-`status` tolerance drops one member and re-parses, so a peer mistyping a
  *second* member must surface the error rather than return an empty payload. What remains is a
  `json.Marshal` on RawMessages that were just unmarshalled — it cannot fail, and is now commented as
  such, per the rule that permits a gap only when the gap is explained.
- **Cloud-SDK paths.** `gcppubsubclient`, `azurecosmos`, `azureeventhub`, `awssqs`,
  `azurequeuestorage`, `azureservicebus`: the uncovered functions are `Publish`, `ReadNext`,
  `NewChangeFeedReader` and friends — the real-SDK paths that need an actual cloud. They are covered
  end-to-end by the ten deploy workflows, all currently green. No action.
- **A coverage-attribution artifact.** `benzenetest` at 82.5%, with `NewSQSEvent`/`NewSNSEvent` at
  0.0%. They are *not* untested: `examples/aws-lambda-mesh` and `awssqs` feed their output into the
  real handlers and assert the result. Those are the only modules that can import both the helper and
  the binding without creating the dependency cycle the repo deliberately avoids, so the calls are
  attributed to a different module's run. Writing tests here would have duplicated real ones against
  a number I had misread. **No action** — recorded so the next reader does not chase it either.

### P2.3 — Re-run the persona suite against the changed error surface — **done, 2026-10-14**

The mesh personas were last exercised before structured errors landed everywhere. A failure now
carries `field` and `code` end to end, which changes what several of those personas can see.

**Round run with the two personas the error surface serves:** `mesh-qa`, signing off a field-level
validation story, and `mesh-developer`, chasing failing `orders:create` calls after a deploy.

**Verdict: the error surface was not reaching the reader.** The Test Console printed the problem
document as raw JSON. QA could not sign off without reading it, so the answer was "NO". Fixed and
vendored into every repo:
- benzene-ui `d4f771a`, Test Console:
  - It lays out a failed result's `detail` and one row per error: field, code, message, and what the
    sent body held at that pointer ("not in the body" for a missing member). The raw JSON stays below.
  - A route-mapped HTTP failure with `benzeneStatus` is now read as the handler's result, using that
    status (wire-contracts §4.1). It is no longer reported as a carrier failure.
  - The `Failed` story's non-§1.3 `{"errors":["..."]}` is replaced with spec-shaped stories.
- benzene-ui `bb5feb2`:
  - **Bug:** arriving at the console topic-first ("test this topic") showed "null has no composable
    topic" above the working composer. The topic list read the absent route parameter, not the
    resolved service.
  - Issue headlines now name their Benzene status beside the exception type.
  - Samples and fixtures that used `internal-server-error` (not a status) now use `unexpected-error`.
- Re-vendored: Benzene `ad6f7efc`, benzene-dotnet `086590ec`, benzene-typescript `1a4c74a`,
  benzene-python `5ab3de6`.

**Decided and shipped as spec 1.2.0 (2026-10-14), both proposed by the developer persona:**
- **Issue error detail.** An issue may carry `errors`: the (field, code) pairs behind the failures
  it counts, never the message, as delta counts merged by identity and bounded to ten. An empty
  member is an absent one, and the detail is not part of the fingerprint (mesh.md §4.1).
  - Emitters: .NET (`UseMeshIssues`), Go (`IssueMiddleware`) and Python (`IssueAggregator`, fed by
    hand). TypeScript has no issue emitter.
  - Collectors: all four ports.
  - UI: the issue page lists them.
- **Flow status.** A fleet trace summary carries `status`: its earliest failing event's, else its
  first event's (mesh.md §4). Every collector sets it. Jaeger and the push stores set it too; X-Ray
  only where it fetched the spans and they agree; Tempo never. The UI names it on each flow row.
  Python's collector gained fleet trace summaries in the process.
- Commits:
  - Benzene `3bfe201f` (tag `spec-1.2.0`, cut by the release workflow).
  - Implementations: .NET `ffe5c471`, Go `b57033b`, TypeScript `26eda19`, Python `3ac0d7c`,
    benzene-ui `b5692e7`.
  - All re-pinned to spec-1.2.0.
  - Side fix: TypeScript's ring grouped events in slot order once it wrapped.

**Open: UI-only follow-ups, not yet done**
- Link an issue's exemplar trace ids to their flow. This needs trace selection in the view state and
  routing, which doesn't exist yet.
- The validation issue explanation says "usually the caller". It should not, when `firstSeen`
  follows the service's own registration change.
- An evidence copy ("copy request + response") for QA sign-off.
- A topic page whose catalogue entry is unversioned, while the live plane carries only versioned rows
  for it, reports "never seen traffic". Seen only with the persona's own mock data; real catalogue and
  fleet samples agree on the version form. Say "this version: none, other versions: N" if it is seen
  for real.
- `npm run generate:contracts` no longer reproduces the checked-in `generated.ts`: it would make
  `ManifestServicesItem.owningTeam` required and restamp the spec version. A sample needs adding
  (an `owningTeam`-less manifest service) before the next regenerate.

---

## Suggested order

1. **Say yes or no to the three publishes** (P0.1 npm, P0.2 PyPI, P0.3 NuGet). These are the only
   items with an outward, irreversible effect, and they are what turn last night's work into
   something a user gets. They are independent of each other and can go in any order.
2. **P0.3 step 2–3** (patterns cleanup) immediately follows the NuGet publish — already scoped, no
   decisions left.
3. ~~**P2.1, P2.2 and P2.3**~~ — done (P2.3 2026-10-14; its two spec proposals shipped as spec 1.2.0).
4. ~~**P1.1–P1.3**~~ — decided 2026-10-11 (above); the .NET/TypeScript client port work remains.

---

## Extracted remainders (2026-08-20 archive sweep)

Live items carried out of plans archived to `work/archive/` on 2026-08-20, so those plans could
retire without losing their remainders. Each notes its source.

- ~~Terraform for the dispatch throttle/env is **unvalidated**~~ — **validated 2026-10-14**:
  terraform 1.9.8 `init -backend=false` + `validate` passes on all 18 Terraform roots across the
  repos. `fmt` drift fixed in
  benzene-dotnet `examples/AwsMesh/deploy` (12b96eb7) and benzene-python `deploy/mesh/terraform`
  (3f70aa7). One warning remains: benzene-python `examples/azure_functions_mesh` uses the
  `static_website` block on `azurerm_storage_account`, which is deprecated in favour of
  `azurerm_storage_account_static_website` and removed in AzureRM v5. It was left alone because the
  migration recreates the setting on a live deployment; do it when the provider moves to v5 (from
  `work/archive/mesh-mismatch-and-dispatch-plan.md`).
- **Part C: persistent changes ledger** — an accumulating change-history artifact beside the
  run-over-run diff; parked product decision (from
  `work/archive/mesh-mismatch-and-dispatch-plan.md`).
- PO decision 1: rename target for Value — "Retire" (shipped in Wave 1) or "Retirement candidates"?
  (from `work/archive/mesh-ui-improvement-plan.md`)
- PO decision 2: delete the Estate's Recent-flows band — deleted in Wave 1 as recommended, but not
  covered by a prior ruling; confirm (from `work/archive/mesh-ui-improvement-plan.md`).
- PO decision 3: schemaMismatch full fix scope — in a wave, or interim copy only? (from
  `work/archive/mesh-ui-improvement-plan.md`)
- PO decision 4: ship the environment chip before `placement.environment` lands in the spec —
  Wave 3 shipped the seam with the honest "not published" default; confirm (from
  `work/archive/mesh-ui-improvement-plan.md`).
- schemaMismatch **full fix**: the aggregator publishes the differing paths, feeding the existing
  `SchemaTree` annotations machinery (from `work/archive/mesh-ui-improvement-plan.md` §2.6).
- ~~Repo split **Phase 5**: wire real Go/TypeScript doc sources into the website build~~ — done.
  All four languages build into benzene.app (each port notifies the site when its docs change).
  2026-10-13 (Benzene `68f21ab`): every docs page has an "Edit this page on GitHub" link into its
  own repo, the header's GitHub link follows the page's repo, and the ports' repo-file links no
  longer drop the branch (live benzene.app linked `.../benzene-go/blob/ROADMAP.md`). Live after the
  next dev → live promote.
- **CancellationToken on message handlers** — now a per-port API question (from
  `work/archive/spec-review-2026-07-25.md`, item 1).
