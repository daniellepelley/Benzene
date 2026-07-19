# Benzene Examples Testing Initiative — Plan

## Context

Ahead of 1.0.0, the goal is to reduce the number of bugs that ship — and `examples/` is an
under-tested surface today: 11 of ~15 example folders have **no test project at all**, and the one
CI job that touches `Benzene.Examples.sln` (`examples-build` in `build-benzene.yml`) only compiles
it, never runs a test. `work/1.0.0-release-status.md` already documents examples rotting silently
once (6 broken projects found on a fresh-clone build). This plan closes that gap in two tiers:

1. **In-memory integration tests**, using Benzene's own test-hosting (`Benzene.Testing`'s
   `BenzeneTestHost` + per-transport `.TestHelpers` packages) — no external process, fast, runs on
   every push/PR.
2. **Real-dependency tests**, using LocalStack (AWS) and the existing Azure/Kafka emulators, to
   catch what in-memory testing structurally can't — actual SDK wire behavior, actual queue/topic
   semantics, and (as a spike) an actual deployed Lambda invoked through a real event source
   mapping.

This serves three purposes at once: catches bugs unit tests miss, gives new users working,
CI-proven examples to copy from, and prevents regressions once it's a real build gate — not three
separate efforts.

**Sandbox constraint:** this working environment has the `docker` CLI but no running daemon
(permission-restricted), so anything container-based (LocalStack, docker-compose, the Azure
emulators) can be written and reasoned through here but not locally executed or watched running.
Verification for that tier happens in CI, the same fallback the repo already uses for "no local
.NET SDK guaranteed" (`AGENTS.md`).

## Verified facts this plan relies on

- **`Benzene.Testing`** (`BenzeneTestHost.Create<TStartUp>()` → `.Build<THost>(factory)`) is the
  generic, transport-agnostic in-memory test host; a platform package supplies the `factory`
  (`Benzene.Aws.Lambda.Core.TestHelpers`'s `BuildAwsLambdaHost`, `Benzene.Azure.Function.Core.TestHelpers`'s
  `BuildAzureFunctionApp`, etc.). No external process/network — see `src/Benzene.Testing/CLAUDE.md`.
- 18 `Benzene.*.TestHelpers` companion packages already exist (one per transport, roughly), each
  exposing `MessageBuilder`/`HttpBuilder` extensions for that transport's context type — this is
  the toolkit every new example test project should build on, not something to invent per example.
- **`examples/Aws/Benzene.Examples.Aws.Tests`** is the strongest existing precedent:
  `Integration/InMemoryOrdersTestBase` wraps `AwsLambdaBenzeneTestHost` (from `Benzene.Tools`,
  itself built on `Benzene.Testing`) and drives real Lambda stream events through the handler code
  in-process. New in-memory example test projects should mirror this shape.
- **`examples/Aws/Benzene.Examples.Aws.Tests/Helpers/SqsSetUp.cs` is a dormant, half-built
  LocalStack helper** — hardcodes `http://localhost:4566`, creates a real SQS queue, drains real
  messages via the AWS SDK — but nothing currently calls it. Revive this rather than rebuild it.
- **LocalStack is already used at the library level**, not yet at the examples level:
  `test/Benzene.Aws.Tests/Fixtures/LocalStackFixture.cs` (FluentDocker: `Builder().UseContainer()
  .UseCompose().FromFile(...).ForceBuild().Build().Start()`, then polls
  `http://localhost:4566/_localstack/health`) + `Fixtures/SqsFixture.cs` +
  `Fixtures/Files/sqs-docker-compose.yaml` (`localstack/localstack:3`, `SERVICES=sns,sqs`). This
  exact pattern is what a new examples-level fixture should reuse, not reinvent.
- **No Lambda has ever been deployed into LocalStack anywhere in this repo.** Every existing
  LocalStack usage only exercises SNS/SQS send/receive against the edge; the Lambda handler code
  itself always runs in-process via `BenzeneTestHost`. Deploying a real Lambda into LocalStack is
  new ground for this repo, not an extension of an existing pattern.
- **LocalStack Community edition (free) supports Lambda**, including SQS/SNS/S3/DynamoDB/Kinesis,
  and .NET Lambda runtimes including .NET 10 (added within days of AWS's own release) — confirmed
  via web search, not assumed. A container-image or zip-packaging step will be needed either way,
  since no AWS Lambda example in this repo has a `Dockerfile` today — every one is zip-deploy
  shaped (`dotnet lambda deploy-function` / Terraform, per `deploy-aws-example.yml`).
- **A `.Dev.Test` naming convention already exists** for "needs a running dependency" test tiers
  (`Benzene.Example.Asp.Dev.Test`, `Benzene.Examples.Kafka.Dev.Test` against
  `examples/Kafka/Benzene.Examples.Kafka.Test/docker-compose.yaml`) — per `examples/CLAUDE.md`,
  new real-dependency tiers should follow this naming, not invent a new one.
- **`examples/Azure/Benzene.Example.Azure.Worker/docker-compose.yml`** already spins up a Service
  Bus emulator + Event Hubs emulator + Azurite (checkpoint store) with **zero tests exercising it**
  — same shape of gap as the AWS LocalStack tier, and the library-level `ServiceBusFixture`/
  `EventHubFixture` pattern (`test/Benzene.Integration.Test/Fixtures/`) is the template to reuse.
- **`work/testing-tooling-investigation.md`** is a live, separate spike proposal to migrate the
  FluentDocker/`DockerComposeFixture` pattern to Testcontainers for .NET, explicitly naming
  `SqsFixture`/LocalStack as a migration candidate. This plan deliberately **stays on the current
  FluentDocker convention** (matching `aws-integration-tests`/`azure-integration-tests` in
  `build-benzene.yml` today) rather than pre-empting that decision — if the Testcontainers spike
  lands first, the new examples fixtures migrate alongside the library ones, not separately.
- **1.0 scope** (per `work/1.0-release-plan.md` §1) is AWS + Azure + ASP.NET Core + self-hosted.
  Google/Cloudflare are explicitly flagged experimental/community (Tier 4.4, not yet actioned) —
  this plan follows the same scoping and excludes them from Phase 1.

## Scope: which examples get what

| Example | Today | Phase 1 (in-memory) | Phase 2/3 (real dependency) |
|---|---|---|---|
| `App` (shared domain) | no test project | add — handler/validator unit-level, drives the shared domain directly | n/a (no transport of its own) |
| `Asp` | `Benzene.Example.Asp.Test` + `.Dev.Test` exist | keep | keep, verify CI-wired |
| `Aws` | `Benzene.Examples.Aws.Tests` (in-memory) exists | keep/extend | **Phase 2**: revive `SqsSetUp.cs`, new `Benzene.Examples.Aws.Dev.Test`, LocalStack SQS egress proof, then the Lambda-in-LocalStack spike |
| `AwsMesh` | no test project | add — one in-memory test per service Lambda (Orders/Payments/Shipping/Mesh) | out of scope for now (Terraform-deployed, manual-only workflow already) |
| `Azure` (+ `.Worker`) | no test project | add — Functions host in-memory via `Benzene.Azure.Function.Core.TestHelpers` | **Phase 3**: wire a test project against the existing Service Bus/Event Hub/Azurite compose file |
| `AzureMesh` | no test project | add — one in-memory test per service | out of scope for now (Terraform-deployed, manual-only) |
| `Grpc` | no test project | add — via `Benzene.Grpc.TestHelpers`'s `GrpcTestHost` | n/a |
| `Kafka` | `.Test` + `.Dev.Test` exist | keep | verify CI-wired (confirm it isn't manual-only today) |
| `Mesh` | no test project | add — in-memory test of the 3 demo services + aggregator dogfooding | out of scope (already has `run.sh` + the K8sMesh compose smoke test covers real-infra at a different layer) |
| `OpenTelemetry` | no test project | add — in-memory, assert spans/metrics are emitted (mock exporter) | out of scope (real OTel collector already demoed via `run.sh`, not a regression-prone dependency) |
| `Saga` | no test project | add — in-memory, drive the compensation flow end-to-end | n/a (in-process by design, no external dependency) |
| `CodeGen`, `Google`, `Cloudflare` | mixed | **excluded from Phase 1** per 1.0 scoping (Tier 4.4) | excluded |

## Phases

### Phase 1 — in-memory tier for every in-scope example
For each "add" row above: a new `.Test`/`.Tests` project (naming matches the sibling convention in
that folder — singular `Benzene.Example.*.Test` for Azure/Grpc, plural `Benzene.Examples.*.Tests`
elsewhere, per `examples/CLAUDE.md`'s documented quirk — don't unify it as part of this work).
Minimum bar per project: every ingress handler/topic driven through the test host at least once,
health checks asserted, egress demos asserted by message *shape* (via a fake/mock sender — real
delivery is Phase 2/3's job). Reference `Benzene.Examples.App`/the relevant `.TestHelpers` package,
not hand-rolled request building.

Then: extend CI so these actually **run**, not just compile — either broaden the existing
`examples-build` job in `build-benzene.yml` into `dotnet test`-ing every new project, or add a
sibling job. This is the concrete fix for "run as part of the build."

*Done when:* every in-scope example folder has a test project, all green in CI, and a push/PR that
breaks an example's handler code fails the build the same way breaking `src/` does today.

### Phase 2 — AWS LocalStack tier, spike first
1. Revive `SqsSetUp.cs` + add a `LocalStackFixture` to a new `Benzene.Examples.Aws.Dev.Test`
   project (reusing the library-level fixture pattern verbatim where possible, not a parallel
   implementation). Prove the AWS example's SQS egress demo (`PublishOrderCreatedMessageHandler`)
   lands a real message in a real (LocalStack) queue, asserted by draining it with the AWS SDK.
2. **Spike, standalone, before committing further**: package `examples/Aws/Benzene.Examples.Aws`
   (via `dotnet lambda package`, matching the zip shape `deploy-aws-example.yml` already uses),
   register it in LocalStack (AWS CLI/SDK pointed at the LocalStack endpoint), wire a real SQS
   event source mapping, drop a message on the real queue, and assert the Lambda actually ran (a
   real side effect, not an in-process call). Write up what worked/didn't as a findings note
   appended to this doc — mirroring `work/testing-tooling-investigation.md`'s spike methodology —
   before deciding whether this becomes a permanent tier or stays a documented experiment.
3. New CI job in `build-benzene.yml`, modeled directly on the existing `aws-integration-tests` job
   (FluentDocker-in-`dotnet test`, docker-compose CLI shim already solved there).

*Done when:* step 1 is a permanent, CI-green tier; step 2 has a written verdict either way.

### Phase 3 — extend real-dependency tier to Azure and Kafka
Wire a test project against `examples/Azure/Benzene.Example.Azure.Worker/docker-compose.yml`
(Service Bus + Event Hub emulators + Azurite), mirroring `ServiceBusFixture`/`EventHubFixture` from
`test/Benzene.Integration.Test/Fixtures/`. Confirm `Benzene.Examples.Kafka.Dev.Test` is actually
CI-wired (not manual-only) and fix if not.

*Done when:* Azure has a real-dependency tier with the same shape as AWS's; Kafka's is confirmed
CI-green.

### Phase 4 — cross-cutting concerns coverage matrix
Enumerate what Benzene ships (idempotency, retry/jitter, W3C trace propagation, health checks,
validation, versioning, sagas, egress/ingress symmetry, schema/spec) and map which example's test
suite proves out which end-to-end — not just unit-tested in `src/`'s own tests. Build a matrix
(concern × example), in the spirit of `docs/capability-matrix.md`, and close the gaps it surfaces
by extending Phase 1/3 tests rather than adding new example projects.

*Done when:* the matrix has no unintentional blank cells for 1.0-scoped concerns.

## Open questions

- Does the Lambda-in-LocalStack spike (Phase 2.2) need a container-image Lambda or does a
  zip-packaged, filesystem-mounted "hot reload" Lambda (LocalStack's own faster dev-loop mechanism)
  work well enough for CI use — to be answered by the spike itself, not guessed at here.
- Whether Phase 1's new CI job runs on every push/PR (matching `aws-integration-tests`) or is
  path-filtered to `examples/**`/`src/**` changes only (matching `smoke-mesh-compose.yml`) — leaning
  toward unfiltered, since example breakage is exactly what a `src/` change can silently cause, but
  worth confirming once Phase 1's actual CI runtime cost is known.
- Whether the Testcontainers spike in `work/testing-tooling-investigation.md` lands before Phase 2/3
  — if so, this plan's fixtures should follow suit rather than migrate twice.

## Progress log

### 2026-07-19 — Phase 1 started: Saga, Azure, and an Aws gap-fill; two real bugs found and fixed

**New in-memory test projects:**
- `examples/Saga/Benzene.Example.Saga.Tests` (4 tests) — drives `SignupSaga` directly (no
  host/transport; the example is a plain console app over `Benzene.Saga`). Happy path, final-stage
  failure, and two parallel-first-stage-failure variants, proving rollback correctness when the
  failure happens after the parallel stage has already committed.
- `examples/Azure/Benzene.Example.Azure.Test` (4 tests) — `Benzene.Testing`'s `BenzeneTestHost` +
  `BuildAzureFunctionApp()`, proving the same `CreateOrderMessageHandler`/`GetAllOrderMessageHandler`
  are reachable via both HTTP and Service Bus, plus the egress demo (with `IBenzeneMessageSender`
  replaced by a capturing fake via `WithServices`, so no live Service Bus is needed). QueueStorage
  ingress deliberately not covered yet (envelope-JSON casing needs its own investigation — noted as
  a follow-up, not guessed at).
- `examples/Aws/Benzene.Examples.Aws.Tests/Integration/PublishOrderCreatedTest.cs` (1 new test) —
  the existing 58-test Aws suite had **zero** coverage of the egress demo; added the same
  fake-sender pattern as Azure's.

**Two real, confirmed bugs found and fixed** (not test-harness artifacts — verified against the
real deployment path too, not just the test host):
1. **`examples/Azure/Benzene.Example.Azure/DependenciesBuilder.cs` was missing `.AddBenzene()`
   entirely.** `UsingBenzene(...)`, `AddHttpMessageHandlers()`, and the real Azure Functions
   `HostBuilderExtensions.UseBenzene<TStartUp>()` hosting path none of them call it implicitly —
   every request would have thrown `BenzeneException: Unable to resolve type MessageRouter<...>`
   at runtime. The Aws example's equivalent `DependenciesBuilder` already called `.AddBenzene()`
   correctly, which is how the asymmetry surfaced.
2. **`PublishOrderCreatedMessageHandler` was undiscoverable in *both* the Aws and Azure examples** —
   same bug, same shape, in both. Root cause: `DependenciesBuilder.Register` calls
   `.AddMessageHandlers(typeof(CreateOrderMessage).Assembly)` (the shared App domain assembly only,
   not the host's own assembly where `PublishOrderCreatedMessageHandler` actually lives), and
   `AddMessageHandlers`'s `IMessageHandlersFinder`/`IHttpEndpointFinder` registrations are
   `TryAddSingleton` — locked in by that first, narrower call. The later, broader
   `.UseMessageHandlers(router => ...)` scan inside `Configure()` (which uses
   `AppDomain.CurrentDomain.GetAssemblies()`, and would have found it) never actually takes effect,
   because `TryAddSingleton` silently no-ops once something's already registered. Fixed by passing
   both assemblies to the `ConfigureServices`-time call in both `DependenciesBuilder`s. This means
   the egress demo (`POST /orders/publish-created` / topic `order_publish_demo`) has been
   unreachable via HTTP or its own topic in both examples since it was added — confirmed nothing in
   the pre-existing Aws test suite ever called it.

**One pre-existing, unrelated failure found, not yet fixed:**
`Benzene.Examples.Aws.Tests.Integration.CreateOrderTest.CreateOrder_ApiGateway_BenzeneMessage`
fails on `main` independent of any change in this pass (verified by stashing the
`DependenciesBuilder.cs` fix and re-running) — posts to `admin/benzene-message` and asserts an
order was persisted, but the order collection comes back empty. Not investigated further this pass
(scope creep beyond what was in flight); flagged here as a genuine, separate pre-1.0 bug for a
follow-up pass — Phase 1 CI wiring (once added) will make this kind of thing impossible to miss
going forward, which is exactly the point of this initiative.

**Verified:** `Benzene.Examples.sln` and `Benzene.sln` both build clean; all new/touched test
projects pass except the one pre-existing failure noted above (unchanged, not a regression).

### 2026-07-19 (cont.) — Phase 1 finished: App + OpenTelemetry tests, CI gate wired, pre-existing bug fixed

**More in-memory test projects:**
- `examples/App/Benzene.Examples.App.Test` (19 tests) — the shared domain's `OrderService`
  orchestration (create status-mapping, the three-way update path) against a mocked `IOrderDbClient`,
  plus `OrderMapper` round-trip and `InMemoryOrderDbClient` behaviour. This is the domain every host
  example reuses and none tested directly before.
- `examples/OpenTelemetry/Benzene.Examples.OpenTelemetry.Test` (4 tests) — drives the example's real
  `Program.cs` entry point via `WebApplicationFactory` (pointed at the assembly through a public
  request type, so the example itself is untouched), proving `/api/send` dispatches every handler
  including the deliberately-failing one, and `/api/topics` reflects the registry. The OTLP exporter
  having no collector to reach under test is a non-issue (never fails a request).

**Pre-existing bug fixed (the one flagged above):**
`CreateOrderTest.CreateOrder_ApiGateway_BenzeneMessage` posted to `admin/benzene-message`, but the
example wires the BenzeneMessage-over-HTTP endpoint at its default path
(`BenzeneMessageHttpOptions.DefaultPath` = `/benzene-message`) — the stale `admin/...` path never
matched, so the request fell through to normal HTTP routing, found no matching `[HttpEndpoint]`, and
persisted nothing. Corrected the test's path; the whole 59-test Aws suite is now green.

**CI gate wired (the load-bearing Phase 1 deliverable):**
`build-benzene.yml`'s `examples-build` job gained a "Test in-memory examples" step running the seven
in-memory example test projects (`--no-build`, after the existing solution build). Examples went
from compile-only to a real executing regression gate — a `src/` change that breaks an example's
behaviour now fails the build, which is precisely how both of last pass's real bugs would have been
caught automatically.

**Finding (not fixed — flagged):** `Benzene.Examples.Kafka.Test`, despite its name, connects to a
real Kafka broker in its fixture setup (`KafkaSetUp` → `Broker transport failure` with no broker) —
it's effectively a `.Dev.Test` that was mis-named and left in `Benzene.Examples.sln`. It's therefore
**excluded** from the CI test step (which lists projects explicitly rather than `dotnet test <sln>`
for exactly this reason). Renaming it to `.Dev.Test` and pulling it out of the solution — matching
the Asp/Kafka `.Dev.Test` convention already established — is a clean follow-up (multi-file
`.sln`/reference change, so not bundled here).

**Scope correction:** the plan's original table had **AwsMesh** getting in-memory tests, but AwsMesh
is not in `Benzene.Examples.sln` at all — it's built only by the manual, `workflow_dispatch`-only
Terraform deploy workflow (`deploy-aws-mesh-example.yml`), so a test project there wouldn't run in
the CI gate without adding its four service projects to the solution (a structural change needing
approval per `examples/CLAUDE.md`). Same reasoning applies to AzureMesh. Both are therefore **out of
the in-memory Phase 1 scope**, matching how they were already out of the real-dependency tier scope.
The `examples/Mesh` services (which *are* in the solution) are ASP.NET hosts whose interesting
behaviour is cross-service HTTP tracing/collector-edge derivation — inherently live-integration,
already exercised by `examples/Mesh/run.sh` and the `smoke-mesh-compose.yml` K8sMesh CI job — so
in-memory tests there would only cover trivial hardcoded-list handlers; deferred as low-ROI. **Grpc**
remains a genuine gap but needs a host-build shim (the example has no `BenzeneStartUp` and mixes
native `MapGrpcService` with Benzene routing on the same method) — deferred to a focused follow-up.

**Phase 1 status:** in-memory coverage now exists and runs in CI for App, Asp, Aws, Azure,
OpenTelemetry, Saga, and Google (7 projects, ~103 tests). Remaining Phase 1 gap: Grpc (deferred,
noted above). Phases 2–4 (LocalStack, Azure/Kafka real-dependency tiers, cross-cutting matrix)
unchanged.

**Verified:** `Benzene.sln` + `Benzene.Examples.sln` build clean; all seven CI-gated example test
projects green (103 tests).

### 2026-07-19 (cont.) — Phase 1 complete: Grpc

`examples/Grpc/Benzene.Example.Grpc.Test` (4 tests) closes the last Phase 1 gap. The example has no
`BenzeneStartUp` (top-level `Program.cs`) and mixes a native `MapGrpcService<GreeterService>` with
Benzene `[GrpcMethod]` routing on the same method paths, so the `BuildGrpcHost<TStartUp>` helper
doesn't fit. Instead the test drives the *real* `Program.cs` via `WebApplicationFactory` (entry
point pinned by the public `GreeterService` type) and calls it over a genuine generated
`Greeter.GreeterClient` stub — reused from the client example project, whose generated types live in
a different namespace (`Benzene.Example.Grpc.Client`) so there's no collision with the server's — via
the in-memory HTTP/2 test handler (`server.CreateHandler()`).

All four RPC shapes (unary, server-stream, client-stream, bidi) assert the **Benzene** handler
answered rather than the native `GreeterService`: the interceptor (`BenzeneInterceptor`) routes any
method matching a `[GrpcMethod]` handler through Benzene and only falls back to the native service
otherwise, and the unary handler's distinctive "…this is Benzene" reply is the proof it won. Added
to the CI "Test in-memory examples" step.

**Phase 1 is now complete for every 1.0-scoped, in-solution example:** App, Asp, Aws, Azure, Grpc,
OpenTelemetry, Saga, Google — 8 projects, ~107 tests, all run in CI. The only examples without an
in-memory tier are the deliberately-out-of-scope ones (AwsMesh/AzureMesh — not in the solution,
Terraform-deployed; `examples/Mesh` — inherently live-integration, covered by `run.sh` + the
K8sMesh compose CI job; Kafka's live tier; and Google/Cloudflare/CodeGen per 1.0 scoping).
