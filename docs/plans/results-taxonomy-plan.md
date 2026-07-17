# Results Taxonomy Plan

## Context

`Benzene.Results` deliberately keeps the status vocabulary small, closed, and
transport-translatable — that philosophy is right and this plan keeps it. A review of the
package and every consumer of the taxonomy found two kinds of gap:

1. **Two missing statuses that matter for retry semantics.** Throttling (Lambda concurrency,
   DynamoDB throughput, HTTP 429) and downstream timeouts are everyday *transient* outcomes for
   the services Benzene targets, and today both collapse into `UnexpectedError`: not retried by
   `RetryBenzeneMessageClient` (which retries only `ServiceUnavailable`), reported over HTTP as
   a 500 (a server bug, when the truth is "back off" / "deadline exceeded"), and unrepresentable
   in the gRPC mapping even though gRPC has dedicated codes for both (`ResourceExhausted`,
   `DeadlineExceeded`).
2. **The taxonomy's meaning lives outside the package, four times over.** "Which statuses are
   successes?" is hardcoded in `Benzene.Clients.Common.BenzeneResultHttpMapper`
   (`SuccessStatuses`/`FailureStatuses`), in the conformance `StatusConformanceHandler`, and
   implicitly in each transport mapper; "which are retryable?" exists only as
   `RetryBenzeneMessageClient`'s single `IsServiceUnavailable()` check. Adding any status means
   touching all of them, and a missed one silently degrades to 500/`Internal`/`UnexpectedError`.

The review also surfaced concrete defects fixed here because they sit on the same lines:
the `BenzeneResult.Set` success trap, two missing-`$` interpolation bugs, and reverse-mapping
holes in the HTTP mappers.

## Verified facts this plan relies on

- **The status vocabulary is spec, not just code.** `docs/specification/wire-contracts.md` §3
  defines the closed set (15 statuses) and §4.1/§4.2 the HTTP/gRPC mappings;
  `docs/specification/conformance/{status-vocabulary,http-status-mapping,grpc-status-mapping}.json`
  pin them, and `test/Benzene.Conformance.Test/StatusMappingConformanceTest.cs` is
  fixture-driven (`MemberData` over the JSON), so fixture rows added here are tested
  automatically. The spec's extension rule — "Applications MAY use additional status strings;
  every mapping table routes unknown statuses to its generic-error row" — is what makes adding
  statuses wire-safe: old services/clients degrade an unknown status to a generic failure.
- **The spec already says `isSuccessful` is "Derived from status class"**
  (`core-concepts.md`), but the implementation disagrees: `BenzeneResult.Set(status)` and
  `Set(status, payload)` hardcode `IsSuccessful = true`, so
  `BenzeneResult.Set(BenzeneResultStatus.NotFound)` yields a *successful* NotFound. The
  errors-array overloads hardcode `false`; only `Set<T>(status, bool)` is explicit.
- **`Set` callers audited** (matters for the inference change):
  - `BenzeneResultExtensions.As(...)` pass-throughs use the payload overloads only on
    `IsSuccessful` results, and the errors overload otherwise — unaffected for known statuses.
  - `ClientResultExtensions.AsBenzeneResult` gates the payload overload on
    `BenzeneResultHttpMapper.IsSuccessStatus` — unaffected.
  - `GrpcBenzeneMessageClient` uses the payload overload when gRPC says `OK` — but a
    `benzene-status` trailer can carry a failure status alongside gRPC `OK`; today that
    produces a *successful* failure-status result. Inference fixes this case.
  - `KafkaMessageContextConverter` (`Set("Ok")`), `HealthCheckProcessor` (errors overload),
    `MessageRouter`/`MessageHandler`/`JsonSchemaMiddleware` (errors/bool overloads) — unaffected.
  - Tests passing *numeric* strings (`SnsMessagePipelineTest`: `Set("200")`) and custom
    statuses rely on the historical `true` default for unknown statuses.
- **`RetryBenzeneMessageClient`** retries only `result.IsServiceUnavailable()`, up to N times,
  and after exhaustion discards the last result and fabricates a fresh
  `BenzeneResult.ServiceUnavailable<T>()`.
- **`BenzeneResultHttpMapper` (Benzene.Clients)**: `Map<T>` has no cases for `429`, `408`,
  `500`, `502`, or `504` (all fall to `default` → `UnexpectedError`), and both it and
  `ClientResultExtensions.AsBenzeneResult` build the unmapped-status error with
  `"Status code {statusCode} not mapped"` — a missing `$`, so the literal placeholder text is
  emitted with the real code as a second error string.
- **Two reverse HTTP mappers disagree**: `BenzeneResultHttpMapper.MapBenzeneResultStatus`
  (string codes, Benzene.Clients) handles `422/501/503`;
  `BenzeneResultExtensions.Convert(HttpStatusCode)` (Benzene.Results) does not — 422/501/503
  currently come back as `UnexpectedError` through `Convert`. The conformance reverse fixture
  only pins the rows both agree on.
- **gRPC reverse mapping** (`DefaultGrpcStatusReverseMapper`, trailer wins verbatim):
  `DeadlineExceeded → ServiceUnavailable` and no `ResourceExhausted` row (falls to default).
- `Benzene.Results` targets `net10.0` and is referenced by `Benzene.Clients`, `Benzene.Http`,
  `Benzene.Grpc`, and the conformance test project — every rewire below is dependency-legal.
- All existing per-status members (constants, factories, `Is*()` extensions) follow a strict
  pattern; new statuses must add all three shapes.

## Goals / non-goals

**Goals**

1. Add `TooManyRequests` and `Timeout` to the vocabulary, end to end: constants, factories,
   `Is*()` extensions, HTTP + gRPC mappings (both directions), spec text, conformance fixtures.
2. Make `Benzene.Results` the single owner of status classification: `IsSuccess`, `IsFailure`,
   `IsKnown`, `IsTransient` on `BenzeneResultStatus`, with the duplicated lists rewired onto it.
3. Fix the `Set` success trap so `IsSuccessful` is derived from status class, as the spec says.
4. Make `RetryBenzeneMessageClient` retry the retry-safe transient statuses and stop discarding
   the final result.
5. Fix the interpolation bugs and reverse-mapping holes found during the review.

**Non-goals**

- **No `Cancelled` status.** Cooperative-cancellation reporting is a worker-lifecycle concern
  with its own design questions (redelivery semantics per queue transport); the gRPC
  `Cancelled → ServiceUnavailable` reverse row keeps working. Revisit if a concrete need appears.
- No `PreconditionFailed` (`Conflict` covers optimistic concurrency), `Gone` (`NotFound`
  covers it), or redirect/`NotModified` statuses (HTTP-cache mechanics, not message semantics).
- No structured error codes on `Errors` / `ProblemDetails` rework — worth doing someday,
  independent of the vocabulary.
- No consolidation of the transport mappers into one table — they are per-protocol by design;
  this plan only aligns their contents and pins them with fixtures.

## Design decisions (final)

- **D1 — Two new statuses.**
  | Status | Success? | Transient? | HTTP | gRPC | Meaning |
  |---|---|---|---|---|---|
  | `TooManyRequests` | no | yes | 429 | `ResourceExhausted` | Throttled / rate limited; back off and retry |
  | `Timeout` | no | yes | 504 | `DeadlineExceeded` | A downstream deadline elapsed; the operation may or may not have been applied |
  Reverse mappings: HTTP `429 → TooManyRequests`, `408/504 → Timeout`, plus explicit
  `500 → UnexpectedError` and `502 → ServiceUnavailable`; gRPC `ResourceExhausted →
  TooManyRequests`, `DeadlineExceeded → Timeout` (changed from `ServiceUnavailable` — the whole
  point is to stop conflating them; the `benzene-status` trailer still wins verbatim).
- **D2 — Classification lives on `BenzeneResultStatus`.** New static methods, `null`-safe:
  `IsSuccess` (true only for the six success statuses), `IsFailure` (true only for the known
  failure statuses), `IsKnown` (either), `IsTransient` (`ServiceUnavailable`,
  `TooManyRequests`, `Timeout` — "a later retry may succeed"). Rewired consumers:
  `BenzeneResultHttpMapper.IsSuccessStatus`/`NormalizeStatus` (its two lists are deleted),
  `StatusConformanceHandler`, `RetryBenzeneMessageClient`, and `BenzeneResult.Set` (D3).
  A new fixture-driven conformance test asserts `IsSuccess` agrees with
  `status-vocabulary.json` row by row.
- **D3 — `Set` derives success from status class, with unknown-status back-compat.** The
  payload/void overloads change from hardcoded `true` to `!BenzeneResultStatus.IsFailure(status)`:
  known failure statuses now produce `IsSuccessful == false`; success statuses stay `true`;
  **unknown/application-defined statuses stay `true`** (the historical default — the spec
  leaves extension statuses' success class to the application, and repo tests pass numeric
  strings through `Set` relying on it). `Set<T>(status, bool)` remains the explicit escape
  hatch, joined by a new `Set<T>(status, payload, isSuccessful)` overload: implementation
  surfaced one legitimate "failure status + payload, rendered as payload" pattern — the health
  check flow returns `ServiceUnavailable` (so HTTP probes see a 503) with the health report as
  the body, which requires the result to stay successful for the body renderer to serialize the
  payload; `HealthCheckProcessor` now states that explicitly. Behavioral change, CHANGELOG'd:
  `Set("NotFound")` and the gRPC failure-trailer-with-OK edge now correctly report failure.
- **D4 — Retry policy: transient minus `Timeout`, pluggable.** `RetryBenzeneMessageClient`
  retries `ServiceUnavailable` and `TooManyRequests` by default. `Timeout` is deliberately
  **not** retried by default: a timed-out operation may have been applied, so blind retry is
  only safe for idempotent calls — callers opt in via a new optional
  `Func<IBenzeneResult, bool> shouldRetry` constructor parameter (e.g.
  `r => BenzeneResultStatus.IsTransient(r.Status)`). After exhausting retries the client now
  returns the **last inner result** instead of fabricating a fresh
  `ServiceUnavailable` — behavioral change, CHANGELOG'd (callers keep a failure result; they
  now also keep its errors and true status).
- **D5 — Bug fixes riding along:** the two missing-`$` interpolations; `Convert(HttpStatusCode)`
  gains the rows it was missing (`422 → ValidationError`, `501 → NotImplemented`,
  `503 → ServiceUnavailable`) so the two reverse HTTP mappers agree, and the reverse fixture
  is extended to pin all shared rows.
- **D6 — Spec and docs move in lockstep:** `wire-contracts.md` §3/§4.1/§4.2 tables, the three
  conformance fixtures, `docs/reference/results.md` (new rows + a classification section), and
  CHANGELOG entries.

## ⚠️ FLAGS — approved by approving this plan

- **Behavioral changes (no signature breaks):**
  1. `BenzeneResult.Set(status)` / `Set(status, payload)` report `IsSuccessful == false` for
     known failure statuses (previously always `true`).
  2. `RetryBenzeneMessageClient` also retries `TooManyRequests`, and returns the last result
     after exhaustion instead of a synthesized `ServiceUnavailable`.
  3. gRPC reverse mapping `DeadlineExceeded` now yields `Timeout` (was `ServiceUnavailable`);
     HTTP 429/408/504 reverse-map to the new statuses (were `UnexpectedError`).
  4. Unknown statuses on the wire still degrade to generic errors everywhere — additive and
     wire-safe per the spec's extension rule.
- **No new NuGet dependencies. No new projects.** Pre-1.0 posture, CHANGELOG entries required.

## Work items

1. `Benzene.Results`: constants, classifier methods, `TooManyRequests`/`Timeout` factories
   (`Void` + `<T>`, `params string[] errors`), `IsTooManyRequests`/`IsTimeout`/`IsTransient`
   extensions, `Set` inference, `Convert(HttpStatusCode)` rows (429/408/504/422/501/503/502).
2. `Benzene.Http`: `DefaultHttpStatusCodeMapper` + XML docs (429, 504).
3. `Benzene.Grpc` / `Benzene.Grpc.Client`: forward + reverse gRPC rows.
4. `Benzene.Clients`: `BenzeneResultHttpMapper` (new cases, classifier rewire, `$` fix),
   `ClientResultExtensions` (`$` fix), `RetryBenzeneMessageClient` (policy + last-result).
5. Conformance: fixture rows in all three JSON files; `StatusConformanceHandler` rewired to
   the classifier; new `StatusVocabularyConformanceTest` asserting `IsSuccess` matches the
   fixture.
6. Docs: `wire-contracts.md`, `docs/reference/results.md`, CHANGELOG.
7. Tests (`Benzene.Core.Test`): classifier truth table, `Set` inference (failure/success/
   custom/numeric statuses), new factories and `Is*` extensions, `BenzeneResultHttpMapper`
   429/408/504/500/502 + fixed error message, retry client (retries `TooManyRequests`, does
   not retry `Timeout` by default, honours `shouldRetry`, returns last result). Full suite +
   conformance suite green before commit.

## Open questions (defaults chosen)

1. HTTP code for `Timeout`: 504 (chosen — "a downstream deadline", matching the gRPC
   `DeadlineExceeded` semantics) vs 408 (client-request timeout; accepted on reverse only).
2. Should `IsTransient` include `Timeout` even though the retry client skips it by default?
   Yes (chosen): `IsTransient` describes the status, the retry default encodes idempotency
   caution; the two concepts are documented separately.
