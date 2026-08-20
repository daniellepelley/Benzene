# Cross-repo outstanding work — verified audit, 2026-08-20

**Status:** ACTION PLAN — every item below was verified against source, not taken from a doc's own
claim. Five parallel audits (one per language port + this repo) harvested every outstanding-work
assertion in the documentation, then checked each one against the code and classified it
DONE / PARTIAL / OUTSTANDING / OBSOLETE with file:line evidence.

Baselines audited: spec `c307cae` · dotnet `2621759` · go `b44d53c` · typescript `816f4c0` ·
python `6b9f6c6`.

## Why this document exists

The audit's headline result is not any single defect. It is that **roughly 75 documents across the
five repos assert that work is pending which has already shipped**. benzene-dotnet's `work/` tree
alone has six design documents still headed "no code accompanies this document" for six subsystems
that all ship. That is the exact failure mode `work/README.md` was written to prevent, and it makes
every other plan in the tree untrustworthy — which is why this audit had to verify rather than read.

So the plan has two halves of comparable size: **fix what is genuinely broken**, and **make the
documentation stop lying**. The second half is mechanical but not optional; without it the next
audit costs the same as this one.

## Retired: the conformance-fixture drift alarm

Three of the five audits reported that the ports' vendored fixtures had drifted from canonical, that
two fixture files existed in the ports but not in the spec repo, and that the pinned `SPEC_VERSION`
commit was unresolvable. **All of that was an artefact of a stale spec-repo checkout** and is false.

Verified against canonical `c307cae`: all four ports' 14 fixtures are **byte-identical** to canonical
and to each other; `84bf13a` is a real commit, 13 behind canonical; and none of those 13 commits
touches `docs/specification/conformance/`. Every port has a bidirectional drift-check workflow.

Recorded because the wrong version of this finding is more expensive than no finding: it would have
sent four agents to "re-vendor" files that are already correct.

---

## P0 — broken now

### P0.1 The website build fails its broken-link self-check (OWNER: typescript)
`benzene.app`'s dev deploy is red. The generator reports 7 broken internal links, **all** from
benzene-typescript's docs; dropping `--source typescript` makes the same build succeed with 177
pages. Two root causes, both stale anchors after renames:
- `docs/hosting.md:278/343` were renamed to `### Self-hosted worker — BenzeneHost` and
  `### Self-hosted worker, inline — InlineSelfHostedStartUp`; four inbound links still target
  `#self-hosted-worker--inlineselfhostedstartup` (`hosting.md:74`, `azure-functions.md:474`,
  `cookbooks/service-bus-handling.md:39`, `cookbooks/event-hub-processing.md:39`).
- `docs/health-checks.md:296,314,334` headings say `(@benzenejs/health-checks-…)`; three links in
  `kubernetes-health-checks.md:36,38` and `cookbooks/typeorm-integration.md:208` still spell the
  pre-rename `benzenehealth-checks-…`.
Effort: small. This is an `AGENTS.md` invariant ("do not break the broken-link self-check").

### P0.2 Kafka's self-hosted worker commits a failed message — silent data loss (OWNER: dotnet)
`BenzeneKafkaWorker.cs:206-208` (and `:226`, `:239`): under `CommitOnlyOnSuccess=true` it awaits the
handler and calls `StoreOffset` **without inspecting `IsSuccessful`**. A thrown exception is
protected; a *returned* failure result is committed and lost. `BenzeneKafkaConfig` has no
`RaiseOnFailureStatus` at all, while all ten other transports now default it to `true`.
Effort: small–medium. This is the last silent-loss default in the repo.

### P0.3 Python receivers ignore the envelope's `isSuccessful` (OWNER: python)
Wire-contracts §1.2 makes `isSuccessful` authoritative — a receiver MUST prefer it over anything
derived from `statusCode` text. Python writes it (`core/envelope.py:157`) and reads it in exactly one
place (`http/app.py:293`). Six receivers classify from status text instead:
`core/envelope.py:188` (`decode_response`, used by Lambda-to-Lambda and in-process),
`grpc/server.py:73`, `aws/app.py:158,205,219,245`, `gcp/functions.py:92`, `azure/app.py:170`.
Consequence: a result with an application-defined status and `successful=True` round-trips as a
**failure** — SQS nacks it, Pub/Sub redelivers forever, gRPC returns `Internal`.
Effort: small — one `successful_from(envelope)` helper threaded through six call sites.

### P0.4 A null/unrouted message is silently acked on eight transports (OWNER: dotnet)
The escalation guard is `MessageResult?.IsSuccessful == false`, so a **null** result reads as success
and the message is acked: SNS `SnsApplication.cs:66`, S3 `:63`, EventBridge `:59`, Queue Storage
`:77`, Event Grid `:75`, Event Hub `:96`, Azure Kafka `:79`, Pub/Sub `:71`. SQS, DynamoDB and Service
Bus already use the safe `!= true`. An **unrouted** message — no handler matched the topic — is the
common case, and it vanishes.
Effort: small per adapter. Behaviour change: needs maintainer sign-off before it ships.

### P0.5 The published Python package ships a Core conformance defect (OWNER: python)
PyPI `benzene-core` has one release, `0.1.0b1`, and it contains **zero** occurrences of
`isSuccessful` — a required envelope member. Fixed on `main`, unreleased. Every `pip install` today
gets a non-conformant Core.
Effort: small (cut the release). Needs credentials.

### P0.6 `benzene-codegen-client` is missing from the PyPI trusted-publisher list (OWNER: python)
`release.yml:57-62` builds all 19 packages and uploads them in one `pypi-publish` call, but the
one-time-setup list in `release.yml:8-12` and `docs/publishing.md:56-59` names only 18, omitting
`benzene-codegen-client`. PyPI verifies OIDC per project, so the first tagged release fails.
Effort: small. Latent until the next release — which P0.5 is about to trigger.

---

## P1 — cross-language conformance gaps

### P1.1 `mesh-service-version-cases.json`: vendored by all four ports, run by none
The strongest finding in the audit, reached independently in four languages. Every collector keys its
catalog by service **name** only — `MeshCollectorStore.cs:23`, `meshd/store.go:36`,
`mesh/collector.py:120`, and TypeScript has no `MeshServiceVersion` type at all — so two releases of
one service overwrite each other in the mesh, in every language.
.NET makes the incoherence sharpest: it implements and tests version **ordering**
(`MeshVersionOrderConformanceTest.cs`) while lacking the version **identity** that ordering sorts.
The fixture is collector-shaped `steps`, structurally identical to `mesh-collector-cases.json`, so
each port's existing collector runner can consume it nearly unchanged once the store is re-keyed.
This claim is *conditional* under the fixture table — a collector may decline it. But all four
vendoring it reads as intent while none runs it, which is the worst of both. **Decide once,
centrally: claim it in all four, or stop vendoring it in all four.**
Effort: medium per port.

### P1.2 `mesh-version-order-cases.json`: run only by .NET
Go, TypeScript and Python vendor it and never load it, and none has a version comparator
(`grep semver|compare_version|VersionScheme` → zero in all three). Same claim-or-drop decision.
Effort: medium per port (needs the comparator, not just a runner).

### P1.3 A vendored fixture that no runner opens is invisible to every drift-check
*(Canonical rule now written into `docs/specification/conformance/README.md`: vendoring a fixture is
a claim that you run it. Note the drift-checks diff `*.json` only, so that README's own re-vendor to
the four ports will not be enforced by CI — a second instance of the same blind spot, worth closing
when the port snapshots next move.)*
Every port's drift-check guards the fixture **bytes**; none guards that a runner **opens** the file.
That is exactly how P1.1 and P1.2 stayed invisible. TypeScript's `mesh-issue-cases.json` is the third
instance — the collector implements the feed, only the runner is missing.
Fix in all four: assert that every vendored fixture is claimed by a runner or listed in an explicit,
reviewed opt-out. Effort: small per port, and it is the check that stops this recurring.

### P1.4 RFC 9457 `status` is missing on HTTP bindings (OWNER: go, typescript)
§1.3 requires the integer `status` member on HTTP bindings, equal to the code actually sent.
- **Go**: filled in by `httpbinding` only. `awslambda/http.go:133` and
  `azurefunctions/azurefunctions.go:107` build their own responses and omit it — while still sending
  `content-type: application/problem+json`. The conformance runner drives `httpRules` through
  `httpbinding` alone, so it cannot see the gap. Fix: export `problemWithHTTPStatus` and call it from
  both; then drive `httpRules` through every shipped HTTP binding.
- **TypeScript**: absent entirely — no `HttpProblemDetailsResponsePayloadMapper`, and
  `ProblemTypes.ts:118` explicitly defers to an HTTP-aware mapper that does not exist. The
  `httpRules` fixture group is vendored and deliberately not run.
Effort: small each.

### P1.5 Failure responses do not carry `application/problem+json` (OWNER: typescript)
`SerializerResponseRenderer.ts:45` sets the negotiated content type unconditionally, with no failure
branch. .NET has `ResolveContentType`/`ProblemContentType`. Transport-neutral, so independent of
P1.4 and should land with it. Effort: small.

### P1.6 gRPC destroys structured errors (OWNER: go, typescript, python)
§4.2 maps `errors` onto `google.rpc.BadRequest` in the `grpc-status-details-bin` trailer, one
`FieldViolation` per error. Only .NET does it (`GrpcMethodHandler.cs:145`). Go collapses to a joined
string (`grpcbinding/server.go:31`), Python reads only `detail` (`grpc/server.py:79`), TypeScript
documents it as deferred. The `field`/`code` that every validation adapter now produces survive an
HTTP hop and die on a gRPC one. Effort: medium each.

### P1.7 Python's `/benzene/spec` is not a Contract Document (OWNER: python)
Profile R5 requires the `contract-document.md` shape; Python serves a port-native
`{service, topics}` (`core/spec.py:141`) with no `?type=` switch. Consequences are concrete: the
port's own Cloud Service probe hard-codes the native shape (`mesh/probe.py:153,194`), so pointed at a
.NET service it reports R2 unsatisfied and R5 malformed — and the README's claim that it "grades any
port's service the same way" is false today. The ingredients all exist; the projection does not.
Effort: medium, plus small to make the probe accept both shapes.

### P1.8 TypeScript never adopted the `benzene:` reserved-topic prefix (OWNER: typescript)
TS uses bare ids (`mesh`, `mesh:register`, `healthcheck`, `spec`) where the Cloud Service Profile is
normative on `benzene:`-prefixed ones (R3, R6). A TS service cannot register with, heartbeat to, or
be health-probed by a collector in any other language. **The conformance runner masks it**:
`MeshCollectorConformanceTest.test.ts:73` strips the prefix before dispatch, so the suite is green
while interop is broken. Deleting that strip is part of the fix, not a follow-up.
Effort: medium — needs a `BenzeneTopic` constants module plus every hardcoded id, test and doc.

### P1.9 Python's OpenAPI generator advertises the withdrawn error body (OWNER: python)
`openapi/generator.py:50-57` still emits `{status: string, detail: string}` as `required`, and serves
it as `application/json`. §1.3 withdrew that shape. The RFC 9457 commit (`a3c1256`) touched core,
grpc-status, http and results — and missed this package. Every generated document lies about the
failure body. Effort: small.

### P1.10 Mesh UI vendored copies have drifted (OWNER: typescript, python)
Canonical `benzene-ui` build is md5 `d4ae9584`; benzene-dotnet matches. `benzene-typescript`
`src/Benzene.Mesh.Ui/mesh-ui.html` and both Python copies are `f606dd34`, last re-vendored at
`dac95a5` on 2026-08-19 while .NET took Wave 3 on 2026-08-20. Both repos' `mesh-ui-drift-check`
should be red on their next scheduled run. Effort: small (`cp` + commit).

---

## P2 — make the documentation true

Verified-DONE work whose documents still claim it is pending. Mechanical, individually trivial,
collectively the reason this audit was expensive.

- **dotnet (~24 docs).** Six design docs headed "no code accompanies this document" for shipped
  subsystems: `outbox-plan.md`, `claim-check-plan.md`, `saga-design.md`, `cancellation-design.md`,
  `kinesis-batch-failure-handling-design.md`, `azure-functions-trigger-codegen-design.md`.
  `1.0-release-plan.md` unticked boxes for the error-payload model and the `benzene:` prefix (both
  shipped), plus stale post-1.0 entries for rate-limiting, ALB/v2 adapters and Service Bus sessions
  (all ship). Four resolved `[DECISION]` entries in `outstanding-bugs.md`. Five mesh enterprise
  slices shipped with every acceptance checkbox blank. `otel-fleet-adapter-scope.md` "not yet built"
  (built). One in-code doc bug that matters more than the rest:
  `EventHubContext.cs:37-38` describes settlement behaviour that its own package contradicts.
- **go (18 docs).** Highest priority is user-facing: `docs/getting-started-aws.md:402-407` tells
  readers `awss3`, `awskinesis` and Kafka-on-Lambda **do not exist**; all three ship. `PARITY.md`'s
  tables contradict `PARITY.md`'s own headline on RabbitMQ and all four Azure outbound clients.
  `docs/design/mesh.md:22-27`'s "Superseded (2026-08)" note **states the role inversion backwards**
  — worse than no note. `go-idioms-review.md`'s status column is blank for all 12 rows, 9 of which
  are done.
- **typescript (9 docs).** Three Azure `Extensions.ts` files call batch clients "deferred" while the
  same package's `index.ts` says they ship. `AzureFunctionStartUpRunner.ts:22` says start-up checks
  are unported, 47 lines above the call that runs them. `docs/health-checks.md:709` claims per-check
  `timeout`/`isNonCritical` don't exist; both are on the interface and honoured.
  `BenzeneResult.problem(...)` ships and appears in no doc at all.
- **python (12 docs).** `README.md:255` says the port lacks circuit breaker, bulkhead, auth and
  caching — contradicted by items 23/24/25 eight lines below. `packages.md:16` "(transport pending)"
  for a gRPC transport that ships. Package count is wrong in five places (says 18 or 10; there are
  19) — and that is not cosmetic, it is the same list P0.6 is missing a name from.
  `docs/reference/results.md:23` documents a `Result` signature three parameters out of date.
- **spec (12 docs).** `error-payload-proposal.md` is the worst: it reads as an open proposal
  recommending option C and recording full RFC 9457 as **rejected** — and RFC 9457 is what shipped.
  `specification/README.md:3` still calls the spec a draft with ".NET the single normative
  reference", contradicting `AGENTS.md`. `porting-guide.md:54-57` defines conformance as requiring an
  interop harness that does not exist, so by its own definition **no port is currently "Benzene"**.
  `repo-split/STATUS.md` Phase 5 PENDING (done). `mesh-ui-react-assessment.md` "no code written"
  (`benzene-ui` exists and is canonical). `benzene-headers-plan.md` deferred "until after the repo
  split" — which completed months ago.

---

## P3 — real features, not dispatched in this wave

Held back deliberately, with the reason:

- **`_benzeneHeaders` → `benzene-headers` (spec + all four ports).** A breaking wire change, free
  only until the 1.0 tag. Strictly ordered spec → ports → re-vendor, so it must not be parallelised.
  Its stated blocker (the repo split) cleared months ago. Needs a maintainer go/no-go.
  **Two corrections to this entry's first draft, both found while executing it.** The blast radius is
  five repos, not three — `_benzeneHeaders` is in all four ports, not just dotnet and go. And the
  claim that the drift-check would go red in between is **false**: the key is pinned by no fixture at
  all (conformance/README.md says so explicitly, precisely *because* the rename was scheduled), and
  the ports' drift-checks diff `*.json` only. The check would stay green throughout and prove
  nothing. The reason not to pin the key expires the moment it is renamed, so a fixture pinning
  `benzene-headers` should land as part of the rename rather than after it.
- **Publishing actions** (npm 25-of-129, PyPI release, NuGet lag, Go tagging). Need credentials and
  are irreversible.
- **Design-gated**: mesh enterprise slice 4, Cloudflare Queues (WP-CF0 is a hard verification gate —
  every API claim in that plan is search-sourced, not primary-verified), `MeshSchemaGenerator`
  `oneOf` (would shift every `descriptorHash`), Go hedging (needs a `Middleware`/`next` contract
  change), version-casting conformance fixtures.
- **Go's missing transport identity.** `transport` is permanently `<missing>` in every Go metric and
  span, making "over which transports" structurally unanswerable for Go services. Cross-cutting
  across every `Use<Transport>` constructor; wants a design pass first.
- **CloudEvents.** Go ships a full CloudEvents binding that no spec section pins and no fixture
  covers, and whose concrete choices contradict `work/cloudevents-design.md`. This is the situation
  `AGENTS.md` names explicitly: an observable contract shaped by one implementation. Spec first.

## Merge-time obligation created by this branch

This branch edits `docs/specification/conformance/README.md` (the phantom-interop-gate fix). That
file is **vendored by every port**, so merging to main without re-vendoring leaves four stale copies.

Only one port will actually tell you: benzene-python extended its `conformance-drift-check` to diff
the README as well as the JSON, on the reasoning that the README "is not decoration: it is what says
which fixtures are conditional". So on merge, **benzene-python's drift-check goes red** and the other
three drift silently. Verified: Python's copy matches canonical `main` today and differs from this
branch, so the check is green until the moment this lands.

Do the re-vendor as part of the merge, in all four ports, and adopt Python's README-diffing check in
the other three — a canonical file that no drift-check covers is the same blind spot as a fixture no
runner opens, one level up.

## Sequencing

P0.1 and P1.10 are minutes of work and currently red — do them first. P0.2/P0.4 need a maintainer
decision before merge because they change settlement behaviour. P1.1/P1.2 need one central
claim-or-drop ruling before four agents implement four different shapes. Everything in P2 is
independent and parallel-safe.
