# Health Checks — pre-1.0 review & work plan

Deep review of the Benzene health-check libraries (`Benzene.HealthChecks.Core`, `Benzene.HealthChecks`,
`.EntityFramework`, `.Http`, `.Schema`, `Benzene.Clients.HealthChecks`) plus the integration seams
(HTTP/gRPC status mapping, CloudService profile + probe, mesh reporting, CLI). Findings are grouped
by how expensive they are to change after the 1.0 API freeze.

**Verdict:** the core design is sound and honestly documented. 503 genuinely reaches the HTTP status
code, Warning/Failed aggregation is correct, liveness/readiness guidance matches Kubernetes, and the
`HealthCheckNamer` collision bug is already fixed with a regression test. The risk is a few one-way-door
API decisions to make before the freeze, two verified integration bugs, and provider polish + gaps.

> **Status:** Tier 0 DONE (injectable `IHealthCheckProcessor`, configurable timeout, per-check
> duration, `HealthCheckNamer` internal, unused `topic` dropped; cancellation via the scoped
> `ICancellationTokenAccessor` any component can resolve — `HttpPingHealthCheck` consumes it, OCE
> reported distinctly. Remaining framework-wide step: seed the token from each transport). Tier 1
> DONE (probe R3, drift loop, TimeOutHealthCheck, HttpPing dispose).
> Tier 2 IN PROGRESS: DONE — T2.1 (OCE), T2.2 (shared-scope concurrency documented — no child-scope
> API to isolate), T2.3 (EF Add* extensions + migration-error detail), T2.4 (AWS side-effect
> warnings), T2.5 (gRPC doc fixed — bridge doesn't split liveness/readiness; the code split is
> deferred), T2.6 (faulted-`.Result` fixed in Sqs/StepFunctions/Lambda; Http URL userinfo stripped;
> `ContinueWith` fixed). REMAINING — T2.7 (caching/throttling), and the gRPC per-service-name code
> split. Below kept as the record.

## Tier 0 — decide before the API freeze (one-way doors)
- **T0.1 `CancellationToken` in `IHealthCheck.ExecuteAsync()`** (`IHealthCheck.cs:18`). Biggest one-way
  door. Without it the timeout cannot cancel the inner work (it runs to completion in the background),
  nothing is cooperatively abortable. Add `CancellationToken token = default` now (source-compatible for
  most implementers); breaking to add later.
- **T0.2 Per-check duration/latency** on `IHealthCheckResult`. Most-requested health-report field; trivial
  now (Stopwatch in the processor), painful to retrofit onto the result contract later.
- **T0.3 Make `HealthCheckProcessor` injectable** (`IHealthCheckProcessor`) instead of `static` with
  `new`-inside — otherwise timeout/tags/subset are all future breaking changes. Drop the unused `topic`
  parameter; make `HealthCheckNamer` `internal`.
- **T0.4 Configurable timeout** (currently hardcoded 10s in `TimeOutHealthCheck.cs:36`). Global + per-check
  override; flows from the builder. Tie to T0.1/T0.3.

## Tier 1 — bugs to fix before release (verified)
- **T1.1 Probe R3 false-negative.** `CloudServiceProbe.cs:82-85` fails R3 conformance on any non-200, so a
  conformant-but-currently-unhealthy service (503) is reported non-conformant — contradicting the profile's
  "runtime degradation is not a conformance failure" rule and the mesh aggregator's deliberate opposite
  handling (`HttpMeshServiceSource.FetchHealthAsync` reads the body on 503). Fix: accept 200-or-503, judge
  R3 on the presence of the `isHealthy` field. Cascades to R7.
- **T1.2 Client drift loop masks two failure modes.** Drift is annotation-only (never flips Warning/Failed)
  AND the generated `HealthCheckAsync()` (`MessageClientSdkBuilder.cs:134-139`) skips the drift check when
  the provider is non-Ok. So an unhealthy+drifted service surfaces neither clearly. Fix: run the drift
  annotation regardless of overall health; surface drift as `Warning`.
- **T1.3 `TimeOutHealthCheck` fragility** (`TimeOutHealthCheck.cs:38-40`): returns `task.Result` (rethrows
  `AggregateException` if the inner faults; safe only by decorator order) and the `Task.Delay(10000)` timer
  is never cancelled (a live 10s timer per fast check). Fix: `return await task`, cancel the delay via a
  CTS, own defensive result.
- **T1.4 `HttpPingHealthCheck`**: response never disposed (`:39`); no `HttpClient` is registered by the
  package, so `AddHttpPing` without a separate registration throws at first probe (silent adopter trap).
  Dispose the response; register/document the client; prefer `IHttpClientFactory`.

## Tier 2 — improvements
- **T2.1** `ExceptionHandlingHealthCheck` catches everything incl. `OperationCanceledException` — a trap that
  becomes a bug once cancellation lands (T0.1): a cooperative cancel would report a failed check named
  `"OperationCanceledException"`. Rethrow OCE when the token is cancelled.
- **T2.2** Concurrency/shared-scope hazard: checks run in parallel over one `IServiceResolver` scope; a
  scoped non-thread-safe EF `DbContext` shared by two checks can race. Child scope per check, or document.
- **T2.3** EF: a swallowed migration exception is indistinguishable from "migration behind"
  (`DatabaseHealthCheck.cs:74-84`) — capture the exception type into `Data`. EF is the only provider with no
  `Add*` registration extension (asymmetry) — add `AddDatabaseHealthCheck`/`AddDatabaseConnectionHealthCheck`.
- **T2.4** AWS checks are side-effecting: `StepFunctionsHealthCheck` starts a real execution every probe,
  SQS sends a real message, Lambda really invokes — cost/noise at 10s cadence. Document prominently; consider
  read-only variants (`GetTopicAttributes`, `DescribeStateMachine`).
- **T2.5** gRPC has no liveness/readiness split (one aggregate `"benzene"` service), yet
  `kubernetes-health-checks.md` recommends gRPC probes — which runs readiness (external-dep) checks as a
  liveness probe, the restart-storm anti-pattern the same doc warns against. Fix the doc and/or add a split.
- **T2.6** Faulted-task `.Result` in SQS/StepFunctions loses dependency metadata;
  `HealthCheckResult.CreateInstance(Task<bool>)` uses `ContinueWith` with the `TaskScheduler.Current` footgun
  (use async/await); URL userinfo can leak into Http `Data`/`Dependency`.
- **T2.7** No caching/throttling — every probe runs every check fresh against the real dependency.

## Tier 3 — extensions / missing providers
- **T3.1** Provider gaps: AWS SNS & DynamoDB (parity with SQS/Lambda/StepFunctions); all Azure transports
  (ServiceBus/EventHub/QueueStorage/EventGrid) have zero health checks; generic TCP connect; host disk/memory;
  Kafka.
- **T3.2** Graceful-shutdown/drain: readiness flips failing on SIGTERM — `SelfHost.Http` already has a drain
  lifecycle (`DrainTimeout`), a natural unconnected wiring point.
- **T3.3** Startup/warmup probe; tags/grouping + subset execution; CLI `benzene healthcheck` is AWS-Lambda-only.

## Test gaps
- Warning-doesn't-flip-`IsHealthy` (core guarantee, unguarded); the 10s timeout path (needs configurable
  timeout + `InternalsVisibleTo`); ASP.NET Core 503-reaches-the-wire (only body asserted today);
  mixed-status aggregation; concurrency.

## Doc fixes
- `docs/health-checks.md:437` says an empty Type is named `"HealthCheck"`; it's actually `"HealthCheck-1"`.

## Implementation order
1. Contained verified fixes (no public-signature change): **T1.3, T1.4, T2.1(partial), T2.6(ContinueWith)**.
2. Integration bugs: **T1.1, T1.2**.
3. Tier 0 API-seam pass (deliberate pre-1.0 breaking change): **T0.1 + T0.3 + T0.4 + T0.2** together, then
   fix `ExceptionHandlingHealthCheck` OCE (T2.1) and all providers/decorators for the new signature.
4. Provider polish + missing `Add*` extensions (T2.3); missing providers (T3.1) as follow-ups.
5. Fill test gaps alongside each change.
