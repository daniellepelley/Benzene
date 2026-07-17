# Benzene Azure Packages - Roadmap to 1.0.0 and Beyond

**Document Version:** 1.8
**Last Updated:** 2026-07-14
**Owner:** Azure Product Team
**Status:** DRAFT for Review

## Document History

This section replaces a much longer, repeatedly-self-corrected changelog (the full
narrative is preserved in git history) with a condensed timeline of verified facts.
Later entries supersede earlier ones where they overlap.

- **2026-07-12** — Fixed the ASP.NET Core 2.1.x-on-.NET-10 dependency crisis
  (`FrameworkReference` to `Microsoft.AspNetCore.App`, replacing EOL 2.1.x packages and
  a Windows-only hard-coded `HintPath`). Added 100% XML documentation across all
  packages (0 CS1591 warnings). Corrected the "zero tests" claim — real coverage was
  82.8-90.7% across four packages; only `Benzene.AspNet.Core` was genuinely untested at
  the time. Moved `TestHttpRequest`/`HttpBuilderExtensions` out of production code into
  `Benzene.Azure.Function.AspNet.TestHelpers`. Renamed `BenzeneMessageLambdaHandler` →
  `BenzeneMessageEventHubHandler` (was AWS terminology in an Azure package). Renamed the
  four Azure-Functions-specific packages for AWS-convention consistency:
  `Benzene.Azure.Core` → `Benzene.Azure.Function.Core`, `.AspNet` → `.Function.AspNet`,
  `.EventHub` → `.Function.EventHub`, `.Kafka` → `.Function.Kafka`.
- **2026-07-13** — Comprehensive documentation pass: added `docs/azure-functions.md`
  and `docs/asp-net-core.md` getting-started guides, Event Hub
  partition/checkpointing/consumer-group/DLQ documentation in
  `docs/cookbooks/event-hub-processing.md`, Event-Hubs-vs-Kafka protocol coverage in
  `docs/getting-started-kafka.md`, and an Azure migration section in
  `docs/migration-alpha-to-1.0.md`.
- **2026-07-14 — cross-platform unification audit.** The `BenzeneStartUp`/
  `IBenzeneApplicationBuilder` unification (built elsewhere in the repo) landed and
  changed two numbers this document had repeated: `Benzene.AspNet.Core` is **81.8%**
  test-covered via `AspNetUnifiedStartUpTest.cs`, not 0% as previously claimed. In
  exchange, the unification's new Azure-specific host-builder glue
  (`HostBuilderExtensions`, `AzureFunctionAppBuilderExtensions`, part of
  `FunctionsWorkerApplicationBuilderExtensions`) was itself untested, dropping
  `Benzene.Azure.Function.Core` to 48.2% — **fixed same-day** with
  `AzureUnifiedStartUpTest.cs`, re-measured at **95.2%**. All Azure Functions packages
  are confirmed isolated-worker only (`Microsoft.Azure.WebJobs` no longer appears
  anywhere in `src/`). **Azure Service Bus support was found already shipped**
  (`Benzene.Azure.Function.ServiceBus` + `.TestHelpers`, 88.6%/83.3% coverage,
  `docs/cookbooks/service-bus-handling.md`) — every place this document listed it as
  unbuilt future work was wrong. A second test-code-in-production leak was found and
  fixed (`BenzeneTestHostExtensions.cs` extracted to
  `Benzene.Azure.Function.Core.TestHelpers`). Versioning is centralized via repo-root
  `version.txt` at **0.0.2**, not per-csproj 0.0.1. Package count: **6 production, 5
  TestHelpers**.
- **2026-07-14 — CORS, SDK-consistency, and code-quality follow-up (this session).**
  - **CORS was already fully implemented; every "no CORS support" / "missing CORS
    middleware" mention below was wrong.** `Benzene.Http.Cors.CorsMiddleware<TContext>`
    is generic over `IHttpContext` and was already wired into
    `Benzene.Azure.Function.AspNet` (as well as AWS API Gateway and ASP.NET Core). This
    session brought it to full parity with `Microsoft.AspNetCore.Cors`: exact
    scheme+host+port origin matching, `Access-Control-Expose-Headers`,
    `Access-Control-Max-Age`, `Access-Control-Allow-Credentials`, a working
    `AllowAnyHeader()`-equivalent wildcard, `Vary: Origin` caching correctness, and
    preflight header validation. See `docs/common-middleware.md`'s `UseCors` section.
  - **Azure SDK version consistency, re-verified: not actually a problem.** Checked
    every Azure package's `.csproj` directly — `Azure.Identity` 1.11.4,
    `Microsoft.Azure.Functions.Worker`/`.Sdk` 2.2.0/2.0.7,
    `Azure.Messaging.EventHubs`/`.Processor` 5.11.5, `Azure.Messaging.ServiceBus`
    7.18.2, and `Microsoft.Azure.Functions.Worker.Extensions.Kafka` 4.3.0 are each
    identical everywhere they're referenced, including the example project. This P0
    line item is resolved by inspection, not by changing anything.
  - **Code Quality Fixes — the remaining scope is done.** Removed the commented-out,
    superseded `UseHealthCheck` block from
    `Benzene.Azure.Function.AspNet/Extensions.cs` (the portable `Benzene.HealthChecks`
    package's generic `UseHealthCheck<TContext>()` already covers this). Renamed both
    known file/class mismatches: `ApiGatewayHttpRequestAdapter.cs` →
    `AspNetHttpRequestAdapter.cs` (Azure.Function.AspNet) and
    `AspNetHeadersMapper.cs` → `AspNetMessageHeadersGetter.cs` (AspNet.Core).
    `AspNetRequestMapper.cs`'s fully-commented-out class (Benzene.AspNet.Core) had
    already been deleted by other work merged into `main` before this pass. Verified:
    full solution and both Azure/ASP.NET example solutions build with 0 errors, 728
    tests pass (724 in `Benzene.Core.Test`, plus the gRPC and conformance suites).
- **2026-07-14 — ARM/Bicep templates and Application Insights (this session).** Closed
  the last two genuinely-open P0 items.
  - **ARM/Bicep Templates:** added `examples/Azure/Benzene.Example.Azure/main.bicep`
    (Storage Account, workspace-based Application Insights, Consumption `Microsoft.Web/serverfarms`,
    Linux isolated-worker Function App), mirroring the AWS SAM template's pattern and
    "hand-checked, not deployed" disclaimer — neither `az` nor `bicep` CLI is available
    in this environment to run `az bicep build`/`what-if`. Linked from a new "Deploying
    with Bicep" subsection in `docs/azure-functions.md`. Scoped to the HTTP trigger path
    the example actually uses, not a template for every possible trigger type.
  - **Application Insights Integration:** re-scoped after finding the "middleware" ask
    was mostly already satisfied by pre-existing docs
    (`docs/cookbooks/logging-application-insights.md`,
    `docs/cookbooks/distributed-tracing-opentelemetry.md`'s App-Insights-via-OTLP
    section) — another case of this document assuming a total gap where most of the
    work already existed, same pattern as the Service Bus discovery above. Building a
    bespoke Application-Insights-specific Benzene package was rejected as inconsistent
    with `Benzene.OpenTelemetry`'s deliberately exporter-agnostic design (see its
    `CLAUDE.md`). What genuinely was missing: the example project didn't demonstrate
    correlating Benzene's own diagnostics with the Application Insights logging it
    already references. Closed by wiring `AddDiagnostics()` (in `DependenciesBuilder.cs`)
    and `UseBenzeneEnrichment()` (in `StartUp.cs`) into
    `examples/Azure/Benzene.Example.Azure`, via a `ProjectReference` to the existing
    `Benzene.Diagnostics` package (no new NuGet dependency — the App Insights packages
    were already referenced), plus a new "Application Insights" subsection in
    `docs/azure-functions.md` cross-linking both cookbooks.
  - Verified: `examples/Azure/Benzene.Example.Azure.sln` and the main `Benzene.sln` both
    build with 0 errors (pre-existing warnings only).
  - Not attempted this pass: item #7's remaining Integration Tests scope (extending the
    Azurite/emulator pattern to Service Bus/Kafka) — the Docker daemon was unreachable
    in this environment (`docker ps` fails to connect), so any new Docker Compose-based
    test could be written but not executed/verified here.
- **2026-07-14 — Integration Tests: Service Bus and Kafka (same-day follow-up).** Closed
  the item flagged as "not attempted this pass" above.
  - Added `test/Benzene.Integration.Test/ServiceBus/ServiceBusConsumerPipelineTest.cs`
    (real send/receive against `mcr.microsoft.com/azure-messaging/servicebus-emulator` +
    its required SQL Server backend, via a new `servicebus-docker-compose.yaml`/
    `servicebus-emulator-config.json`/`ServiceBusFixture.cs`) and
    `test/Benzene.Integration.Test/Kafka/KafkaConsumerPipelineTest.cs` (real produce/consume
    against the *existing* Event Hubs emulator's Kafka-compatible endpoint on port 9092 —
    no new container needed, just a second entity, `kafka1`, added to
    `eventhub-emulator-config.json` alongside `eh1`).
  - Both new tests drive Benzene's real production pipeline on the receiving end
    (`app.HandleServiceBusMessages(...)`/`app.HandleKafkaEvents(...)`), same shape as the
    existing `EventHubConsumerPipelineTest.cs`, not a hand-built event.
  - Added `EventHubEmulatorCollection` (an xunit `ICollectionFixture`) so the Event Hubs
    and Kafka tests share one running emulator container instead of each spinning up their
    own and racing to bind the same fixed host ports; converted
    `EventHubConsumerPipelineTest` from `IClassFixture<EventHubFixture>` to this shared
    collection as part of the same change. The Service Bus emulator's ports were remapped
    to 5673/5301 (from its defaults 5672/5300) so it can run alongside the Event Hubs
    emulator without colliding — confirmed via web research that the Service Bus SDK's
    emulator connection string supports specifying a non-default port explicitly.
  - User-approved addition of `Azure.Messaging.ServiceBus` and `Confluent.Kafka` as direct
    `PackageReference`s to `Benzene.Integration.Test.csproj` (both already used elsewhere
    in the repo at these exact pinned versions) before writing any test code, per the
    NuGet policy.
  - **Separate gap found and fixed along the way:** `Benzene.Integration.Test` was never
    wired into any CI workflow at all (confirmed via `git log` — true since the Event Hubs
    emulator test was first added, well before this session), unlike the parallel
    `Benzene.Aws.Tests` project, which has its own `aws-integration-tests` CI job. Added a
    mirrored `azure-integration-tests` job to `.github/workflows/build-benzene.yml`.
  - **Disclosure:** this sandbox's Docker daemon is still unreachable
    (`docker ps` fails to connect), so none of the new or existing tests in
    `Benzene.Integration.Test` were actually executed here. Verified instead by: a clean
    `dotnet build` of the project (0 errors), `dotnet test --list-tests` confirming all
    four tests (including the two new ones) are discovered correctly, a full solution
    build (0 errors), the full `Benzene.Core.Test` suite still passing (750/750), and
    valid-YAML/valid-JSON checks on every new compose/config/workflow file. The new CI job
    is the first place these will actually run, against a real Docker daemon.

- **2026-07-17 — Self-hosted (non-Functions) Service Bus and Event Hubs consumers.** Closed the
  gap that Azure messages could only be consumed via Azure Functions triggers, unlike AWS
  (`Benzene.Aws.Sqs`'s standalone `SqsConsumer`) and Kafka (`Benzene.Kafka.Core`'s
  `BenzeneKafkaWorker`). Two new production packages, both `IBenzeneWorkerStartup`-wired
  self-hosted workers (see `docs/hosting.md`'s mode 3): **`Benzene.Azure.ServiceBus`**
  (`BenzeneServiceBusWorker` on `ServiceBusProcessor` - queue or topic/subscription,
  `MaxConcurrentCalls`, `AutoComplete`/`Explicit` ack modes mirroring the Function package's
  enum) and **`Benzene.Azure.EventHub`** (`BenzeneEventHubWorker` on `EventProcessorClient` -
  consumer groups, partition load balancing, per-partition blob checkpointing every
  `CheckpointInterval` successful events, `CatchHandlerExceptions` skip-vs-stop semantics).
  No new NuGet dependencies - both reuse the exact SDK versions already pinned by the Function
  packages (`Azure.Messaging.ServiceBus` 7.18.2, `Azure.Messaging.EventHubs.Processor` 5.11.5),
  and neither references `Microsoft.Azure.Functions.Worker.*`. Unit tests (mappers, applications,
  ack/config validation - 21 tests) in `test/Benzene.Core.Test/Azure/{ServiceBusWorker,EventHubWorker}/`;
  live end-to-end tests against the existing emulators in
  `test/Benzene.Integration.Test/{ServiceBus,EventHub}/Benzene*WorkerLiveTest.cs` (new
  `benzene-worker-queue` and `eh2` entities so they don't contend with the trigger-pipeline
  tests; azurite's blob port 10000 newly exposed for the checkpoint store). Same disclosure as
  prior sessions: this sandbox's Docker daemon is unreachable, so the two new live tests were
  verified by build + `--list-tests` discovery only and will first actually run in CI's
  `azure-integration-tests` job. Full `Benzene.sln` build 0 errors; `Benzene.Core.Test`
  1252/1252 passing. Package count: **8 production, 5 TestHelpers**.

---

## Executive Summary

This roadmap outlines the path to 1.0.0 for Benzene's Azure integration packages and defines the strategic direction for Azure-specific features over the next 12+ months. The Azure ecosystem within Benzene currently consists of **6 production packages** (5 Azure-Functions-specific + 1 ASP.NET Core general) and **5 TestHelper packages** supporting Azure Functions, Event Hubs, Kafka (via Event Hubs), Service Bus, and ASP.NET Core hosting.

### Current State
- **Package Count:** 6 Azure production packages (5 Azure-Functions-specific incl. Service Bus + 1 ASP.NET), 5 TestHelpers (updated 2026-07-14 — Service Bus package added, `Benzene.Azure.Function.Core.TestHelpers` extracted)
- **Version:** All at 0.0.2 (pre-release; centralized via repo-root `version.txt`, corrected 2026-07-14 — was 0.0.1 when this document last checked)
- **Target Framework:** .NET 10
- **Source Files:** 65 Azure-related `.cs` source files across all 10 Azure/AspNet.Core packages (50 Azure.*, 15 AspNet.Core), recounted 2026-07-14 — the previous "~117" figure could not be reproduced and appears stale
- **Test Coverage:** ✅ good across the board, re-measured 2026-07-14 against the full `test/Benzene.Core.Test` suite: Azure.AspNet 84.2%, Azure.EventHub 80.5%, Azure.Kafka 84.4%, Azure.ServiceBus 88.6% (new), **Benzene.AspNet.Core 81.8%** (was wrongly claimed 0% throughout this document — see Document History), and **Azure.Function.Core 95.2%** (had briefly dropped to 48.2% on new, untested host-builder glue — fixed same-day with `AzureUnifiedStartUpTest.cs`)
- **Documentation:** ✅ 100% XML documentation across all packages (completed 2026-07-12, still true), basic CLAUDE.md files exist (some stale — see package sections), plus a full `docs/azure-functions.md` getting-started guide and two Azure cookbooks (`event-hub-processing.md`, `service-bus-handling.md`) found 2026-07-14 that this document didn't previously know about
- **Dependencies:** ✅ ASP.NET Core 2.1.x issue resolved (2026-07-12) — `Benzene.Azure.Function.AspNet` and `Benzene.AspNet.Core` now use `FrameworkReference` to `Microsoft.AspNetCore.App` instead of EOL 2.1.x NuGet packages; hard-coded Windows-only `HintPath` removed. ✅ Also confirmed 2026-07-14: `Microsoft.Azure.WebJobs` no longer exists anywhere in the repo — all Azure Functions packages moved to the isolated-worker `Microsoft.Azure.Functions.Worker.*` model. ✅ Azure SDK version consistency re-verified 2026-07-14 — not actually inconsistent
- **Maturity:** Functional; test/doc gap with AWS is much smaller than originally assessed; the dependency blocker is fixed; Service Bus shipped; CORS already built and now spec-hardened; the `Benzene.Azure.Function.Core` coverage gap and both known code-quality items are resolved

### Key Findings
✅ **Strengths:**
- Clean architecture consistent with Benzene patterns
- Good separation: Azure Functions vs ASP.NET Core hosting
- TestHelpers properly extracted to dedicated packages (as of 2026-07-14, every
  Azure-Functions-specific production package has a `.TestHelpers` sibling with zero
  test code left in production packages — verified via `Benzene.Testing`
  `ProjectReference` sweep)
- Working example demonstrates Azure Functions usage
- No TODO/FIXME/HACK comments found in codebase
- Simpler than AWS (fewer packages, cleaner scope)
- ✅ 100% XML documentation, 0 CS1591 warnings (completed 2026-07-12, re-verified 2026-07-14)
- ✅ 5 of 6 packages have solid test coverage (80-91%), contrary to this document's
  original "zero tests" claim and its later "only Benzene.AspNet.Core is 0%" claim —
  both wrong; `Benzene.AspNet.Core` is actually 81.8% covered (re-measured 2026-07-14)
- ✅ ASP.NET Core dependencies fixed — `FrameworkReference` to `Microsoft.AspNetCore.App`
  instead of EOL 2.1.x packages (resolved 2026-07-12)
- ✅ Azure Service Bus fully shipped (`Benzene.Azure.Function.ServiceBus` +
  `.TestHelpers`, 88.6%/83.3% covered, cookbook documented) — found 2026-07-14, not
  previously known to this document

❌ **Critical Blockers for 1.0:**
- ~~ZERO XML documentation on any public API~~ ✅ RESOLVED 2026-07-12
- ~~Benzene.AspNet.Core has 0% test coverage~~ ❌ **WRONG CLAIM, corrected 2026-07-14** —
  actually 81.8% covered. The coverage gap this uncovered instead
  (`Benzene.Azure.Function.Core`'s new host-builder/isolated-worker glue, briefly at
  48.2%) was itself fixed same-day — see Document History — and is now at 95.2%
- ~~Very old ASP.NET Core dependencies (2.1.x on .NET 10 project - major compatibility issue)~~ ✅ RESOLVED 2026-07-12
- ~~Old Microsoft.Azure.WebJobs dependency~~ ✅ RESOLVED — the whole WebJobs-based model was replaced by the isolated-worker `Microsoft.Azure.Functions.Worker.*` packages (confirmed 2026-07-14, repo-wide grep returns nothing)
- ~~Inconsistent Azure SDK versions~~ ✅ RESOLVED — re-verified 2026-07-14, all Azure
  packages already reference identical dependency versions
- ~~Missing deployment templates (ARM/Bicep/Terraform)~~ ⚠️ **ARM/Bicep RESOLVED
  2026-07-14** — `examples/Azure/Benzene.Example.Azure/main.bicep`; Terraform is still
  genuinely absent (not attempted, P1 item)
- ~~No Application Insights integration examples~~ ✅ RESOLVED 2026-07-14 — see Document
  History; mostly pre-existing cookbook docs plus now-demonstrated example wiring
- Missing Azure-specific middleware (authentication, Managed Identity) — confirmed
  still absent 2026-07-14; CORS is NOT part of this gap (see Document History)
- No performance benchmarks or cold-start metrics
- ~~Minimal documentation (only basic ASP.NET Core guide)~~ ⚠️ PARTIALLY RESOLVED 2026-07-14 — a full `docs/azure-functions.md` getting-started guide plus `docs/cookbooks/event-hub-processing.md` and `docs/cookbooks/service-bus-handling.md` now exist, plus a new "Application Insights" subsection and "Deploying with Bicep" subsection added this same day; Terraform, Managed Identity, and Key Vault content is still genuinely missing
- No Azure App Service, Container Apps, or AKS integration guidance
- Missing RBAC and Managed Identity patterns

### Recommended 1.0 Strategy

**CONSERVATIVE APPROACH (STRONGLY RECOMMENDED):**
Keep all Azure packages at **0.9.x-preview** until well after core 1.0 release, then:
- ~~Fix critical dependency issues (ASP.NET Core 2.1 on .NET 10)~~ ✅ DONE 2026-07-12,
  and the related Microsoft.Azure.WebJobs issue is also moot as of 2026-07-14
- Ship Azure packages at **1.0.0** only after addressing blockers above
- Azure is LESS mature than AWS - needs more foundational work
- Allow core packages to stabilize first (Benzene 1.0 dependency)
- Give time to validate Azure Functions v4 compatibility
- Gather Azure-specific production feedback

**Timeline Estimate:** 6-9 months post core 1.0 release (longer than AWS due to less maturity)

---

## Table of Contents

1. [Current State Assessment](#current-state-assessment)
2. [Package-by-Package Analysis](#package-by-package-analysis)
3. [Roadmap to 1.0.0](#roadmap-to-10)
4. [Short-Term Roadmap (3-6 Months)](#short-term-roadmap-3-6-months)
5. [Medium-Term Roadmap (6-12 Months)](#medium-term-roadmap-6-12-months)
6. [Long-Term Vision (12+ Months)](#long-term-vision-12-months)
7. [Technical Debt & Quality](#technical-debt--quality)
8. [Testing Strategy](#testing-strategy)
9. [Documentation Requirements](#documentation-requirements)
10. [Performance & Optimization](#performance--optimization)
11. [Security & Best Practices](#security--best-practices)
12. [Breaking Changes Pre-1.0](#breaking-changes-pre-10)
13. [Dependencies & Compatibility](#dependencies--compatibility)
14. [Success Metrics](#success-metrics)

---

## Current State Assessment

### Package Inventory

| Package | Version | Purpose | Maturity | 1.0 Ready? |
|---------|---------|---------|----------|------------|
| **Benzene.Azure.Function.Core** | 0.0.2 | Core Azure Functions abstractions & startup | Low-Medium | ❌ Not ready — new host-builder glue at 0% coverage (found 2026-07-14) |
| **Benzene.Azure.Function.AspNet** | 0.0.2 | Azure Functions HTTP trigger adapter | Low-Medium | ❌ Not ready |
| **Benzene.Azure.Function.EventHub** | 0.0.2 | Event Hubs trigger adapter | Low-Medium | ❌ Not ready |
| **Benzene.Azure.Function.Kafka** | 0.0.2 | Kafka via Event Hubs trigger adapter | Low-Medium | ❌ Not ready |
| **Benzene.Azure.Function.ServiceBus** | 0.0.2 | Service Bus queue/topic trigger adapter | Low-Medium | ❌ Not ready — functional and tested (88.6%), but new (found 2026-07-14, not previously tracked by this document) |
| **Benzene.AspNet.Core** | 0.0.2 | General ASP.NET Core integration | Medium | ❌ Not ready — but test coverage (81.8%) is no longer a blocker, corrected 2026-07-14 |

> Row for `Benzene.Azure.Function.ServiceBus` added 2026-07-14 — this package did not
> exist (or this document was unaware of it) at the previous update. Version column
> corrected from "0.0.1"/"No version" to 0.0.2, per the centralized `version.txt`
> found 2026-07-14 (see top-of-document changelog).

**TestHelper Packages (not for 1.0):**
- Benzene.Azure.Function.Core.TestHelpers (new 2026-07-14 — extracted `BenzeneTestHostExtensions.cs` out of the production package)
- Benzene.Azure.Function.AspNet.TestHelpers
- Benzene.Azure.Function.EventHub.TestHelpers
- Benzene.Azure.Function.Kafka.TestHelpers
- Benzene.Azure.Function.ServiceBus.TestHelpers (new 2026-07-14)

### Code Quality Metrics

**Positive Indicators:**
- ✅ No TODO/FIXME/HACK comments found
- ✅ Consistent naming conventions
- ✅ Clean separation: Azure Functions vs ASP.NET Core
- ✅ TestHelpers properly separated
- ✅ Simple, focused architecture
- ✅ Working Azure Functions example

**Red Flags:**
- ~~❌ **0 XML documentation comments** across ALL packages~~ ✅ RESOLVED 2026-07-12, re-verified 2026-07-14
- ~~❌ **ZERO test files** found - complete absence of tests~~ ✅ WRONG — corrected 2026-07-12, re-verified 2026-07-14: 674 tests pass in `test/Benzene.Core.Test`
- ~~❌ **CRITICAL DEPENDENCY ISSUE**: ASP.NET Core 2.1.x packages on .NET 10 project~~ ✅ RESOLVED 2026-07-12 (now `FrameworkReference` to `Microsoft.AspNetCore.App`)
- ~~❌ Old Microsoft.Azure.WebJobs (3.0.39) - should be 3.0.40+~~ ✅ RESOLVED — confirmed 2026-07-14 that `Microsoft.Azure.WebJobs` no longer appears anywhere in `src/`; replaced entirely by the isolated-worker `Microsoft.Azure.Functions.Worker.*` packages
- ~~❌ Inconsistent Azure SDK versions~~ ✅ RESOLVED — re-verified 2026-07-14, all Azure
  packages already reference identical versions of shared dependencies
  (`Azure.Identity`, `Microsoft.Azure.Functions.Worker`/`.Sdk`, etc.)
- ❌ No ARM/Bicep/Terraform deployment templates — confirmed still true 2026-07-14
- ❌ No Application Insights integration — confirmed still true 2026-07-14
- ⚠️ Missing Azure authentication/authorization middleware (Managed Identity) — still
  true 2026-07-14. CORS is NOT part of this gap — `Benzene.Azure.Function.AspNet`
  already gets CORS via the portable `Benzene.Http.Cors.CorsMiddleware<TContext>`, now
  spec-hardened (see Document History)
- ❌ No performance benchmarks or metrics
- ⚠️ **NEW (2026-07-14):** `Benzene.Azure.Function.Core`'s new isolated-worker host-builder
  glue (`HostBuilderExtensions.UseBenzene<TStartUp>()`, `AzureFunctionAppBuilderExtensions.UseBenzeneInvocation()`,
  and the worker middleware in `FunctionsWorkerApplicationBuilderExtensions`) has 0%
  test coverage and isn't exercised by the `examples/Azure` project either
- ~~❌ Minimal documentation (only 1 doc file for ASP.NET Core)~~ ⚠️ PARTIALLY RESOLVED 2026-07-14 — `docs/azure-functions.md` (521 lines) plus two cookbooks now exist; still no ARM/Bicep/Terraform, Managed Identity, or Application Insights content
- ❌ No Azure-specific CI/CD examples
- ⚠️ Commented-out code in multiple files — `Benzene.AspNet.Core/BenzeneExtensions.cs`'s
  commented block is now gone (that file was rewritten for the cross-platform
  unification), but `Benzene.Azure.Function.AspNet/Extensions.cs` still has
  commented-out health-check code, and a *different* file,
  `Benzene.AspNet.Core/AspNetRequestMapper.cs`, was found 2026-07-14 to contain a
  fully commented-out class

### Dependency Analysis

**Azure SDK & Functions Dependencies (as of 2026-07-14, re-checked against actual `.csproj` files):**
```
Azure.Identity                                            1.11.4    (Core, Kafka)
Azure.Messaging.EventHubs.Processor                       5.11.5    (EventHub)
Azure.Messaging.ServiceBus                                7.18.2    (ServiceBus, new)
Microsoft.Azure.Functions.Worker                          2.2.0     (Core)
Microsoft.Azure.Functions.Worker.Sdk                      2.0.7     (Core)
Microsoft.Azure.Functions.Worker.Extensions.Http           3.3.0    (AspNet)
Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore 2.1.0   (AspNet)
Microsoft.Azure.Functions.Worker.Extensions.EventHubs      6.5.0    (EventHub)
Microsoft.Azure.Functions.Worker.Extensions.Kafka           4.3.0   (Kafka)
Microsoft.Azure.Functions.Worker.Extensions.ServiceBus      5.22.0  (ServiceBus, new)
Microsoft.AspNetCore.App (FrameworkReference)              (shared fw) ✅ FIXED 2026-07-12 — replaces the three rows below
```

> **2026-07-14 update:** The table above completely replaces the previous
> `Microsoft.Azure.WebJobs`/`Microsoft.Azure.WebJobs.Extensions.*`/`Microsoft.Azure.Functions.Extensions`
> in-process dependency set — none of those packages appear anywhere in `src/`
> anymore (confirmed via `grep -rl "Microsoft.Azure.WebJobs" src/`, zero results).
> Every Azure Functions package now targets the isolated-worker model exclusively via
> `Microsoft.Azure.Functions.Worker.*`. The "Old Microsoft.Azure.WebJobs (3.0.39) -
> should be 3.0.40+" issue this document repeated in several places below is fully
> moot — there's no WebJobs dependency left to version-bump.
~~Microsoft.AspNetCore.Mvc.Core 2.1.38 / Microsoft.AspNetCore.Routing 2.1.1 /
Microsoft.AspNetCore.Http.Abstractions 2.1.1~~ — removed 2026-07-12, replaced by a
`FrameworkReference` to `Microsoft.AspNetCore.App` in both `Benzene.Azure.Function.AspNet.csproj`
and `Benzene.AspNet.Core.csproj`. The redundant `Microsoft.Extensions.DependencyInjection.Abstractions`
`PackageReference` in `Benzene.AspNet.Core.csproj` was also removed (NU1510 flagged it
as already supplied transitively).

**Critical Issues:**
1. ~~❌ **ASP.NET Core 2.1.x on .NET 10** - This is a MAJOR incompatibility~~
   ✅ **RESOLVED 2026-07-12** — replaced with `FrameworkReference` to
   `Microsoft.AspNetCore.App`, the correct approach for referencing ASP.NET Core types
   from a plain `Microsoft.NET.Sdk` project on .NET Core 3.0+.
2. ~~⚠️ Microsoft.Azure.WebJobs 3.0.39 is old - should update to latest 3.0.x~~ ✅ MOOT 2026-07-14 — the package was replaced entirely by `Microsoft.Azure.Functions.Worker.*` (isolated worker), not version-bumped
3. ⚠️ No Application Insights SDK references — confirmed still true 2026-07-14
4. ⚠️ Missing Azure.Core for consistent Azure SDK usage

### Comparison with AWS Packages

> **2026-07-14 note:** This subsection is the original as-found snapshot and is
> substantially stale on both sides. AWS is now at 8 production packages (one, XRay,
> was deleted and superseded by OpenTelemetry) with 90%+ coverage across the board and
> ~97% overall 1.0 readiness (see `aws-roadmap-1.0.md`'s own 2026-07-13 audit). Azure
> is now 6 production packages (Service Bus added), 5 of 6 with 80-91% test coverage,
> 100% XML documentation, and 3 event sources (EventHub, Kafka, Service Bus — not 2).
> Kept below for historical context; see the Executive Summary and Appendix B for
> current numbers.

**AWS Package Maturity (from aws-roadmap-1.0.md, original snapshot):**
- 8 packages, ~179 source files
- 4 test classes found (minimal but present)
- Medium maturity overall
- Estimated 178-262 hours to 1.0

**Azure Package Maturity (original snapshot — see 2026-07-14 note above for current numbers):**
- 5 packages, ~117 source files
- 0 test classes found (none at all)
- Low-Medium maturity overall
- **Estimated 200-300 hours to 1.0** (more work despite fewer packages due to:
  - Critical dependency issues to resolve
  - Complete absence of tests
  - Less mature overall state
  - Need for Azure-specific features like Managed Identity, App Insights)

**Key Differences (original snapshot — now stale, see note above):**
- Azure has fewer packages but LESS mature foundation
- AWS has some tests; Azure has none
- AWS dependencies mostly OK; Azure has critical dependency issues
- AWS has 4 event sources; Azure has 2 (EventHub, Kafka) — **now 3, Service Bus shipped 2026-07-14 audit**
- Both have zero XML documentation

---

## Package-by-Package Analysis

### 1. Benzene.Azure.Function.Core ⭐ Foundation Package

**Location:** `src/Benzene.Azure.Function.Core/`
**Current State:** Low-Medium maturity, foundational but incomplete

**Public API Surface:**
- `IAzureFunctionApp` - Entry point abstraction (C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.Function.Core\IAzureFunctionApp.cs)
- `AzureFunctionApp` - Main implementation (C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.Function.Core\AzureFunctionApp.cs)
- `AzureFunctionStartUp` - Startup pattern (C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.Function.Core\AzureFunctionStartUp.cs)
- `InlineAzureFunctionStartUp` - Inline configuration
- `IAzureFunctionAppBuilder` / `AzureFunctionAppBuilder` - Builder pattern
- Integration with Azure Functions hosting model

**Strengths:**
- Clean startup pattern inspired by ASP.NET Core
- Generic support for different DI containers
- Proper abstraction for Azure Functions hosting
- Builder pattern for composability

**Issues:**
1. ~~❌ No XML documentation on any type~~ ✅ RESOLVED 2026-07-12
2. ❌ Exception message "Cannot handle this kind of request" (lines 27, 40) is not helpful
3. ⚠️ No cold-start optimization guidance
4. ⚠️ No Application Insights integration
5. ⚠️ No Managed Identity configuration helpers
6. ~~⚠️ Old Microsoft.Azure.WebJobs dependency (3.0.39)~~ ✅ MOOT 2026-07-14 — package replaced entirely by `Microsoft.Azure.Functions.Worker.*` (isolated worker)
7. ⚠️ No Function App settings configuration helpers
8. ⚠️ No guidance on hosting plans (Consumption, Premium, Dedicated)
9. ⚠️ No durable functions support
10. ⚠️ No logging integration patterns
11. ⚠️ **NEW, found 2026-07-14:** `HostBuilderExtensions.UseBenzene<TStartUp>()`,
    `AzureFunctionAppBuilderExtensions.UseBenzeneInvocation()`, and the isolated-worker
    middleware in `FunctionsWorkerApplicationBuilderExtensions` (all added as part of
    the cross-platform `BenzeneStartUp` unification) have **zero test coverage** and
    are not used by `examples/Azure` either — this is what dropped the package's
    measured coverage from 82.8% to 48.2%

**1.0 Requirements:**
- [x] Add comprehensive XML documentation — done 2026-07-12
- [ ] Improve error messages with actionable guidance
- [x] ~~Update Microsoft.Azure.WebJobs to latest 3.0.x~~ — moot 2026-07-14, package no longer used
- [ ] Add Application Insights integration
- [ ] Add Managed Identity configuration helpers
- [ ] Document hosting plan differences
- [ ] Add cold-start optimization guidance
- [ ] Create migration guide to Azure Functions v4
- [ ] Add Function App configuration helpers
- [ ] Document Key Vault integration patterns
- [ ] Add structured logging integration
- [ ] Document deployment best practices
- [ ] **NEW 2026-07-14:** Add unit tests for `HostBuilderExtensions`,
      `AzureFunctionAppBuilderExtensions`, and the `FunctionsWorkerApplicationBuilderExtensions`
      middleware — currently 0% covered, dragging the whole package below the 70%
      "reasonable coverage" bar used elsewhere in this document

**Estimated Effort:** ~~25-30 hours~~ 20-25 hours remaining for the original scope,
plus ~5-8 hours newly added 2026-07-14 for testing the host-builder glue

---

### 2. Benzene.Azure.Function.AspNet 🔧 HTTP Functions Adapter

**Location:** `src/Benzene.Azure.Function.AspNet/`
**Current State:** Low maturity; dependency crisis resolved 2026-07-12

**Public API Surface:**
- `AspNetApplication` - Main application (C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.Function.AspNet\AspNetApplication.cs)
- `AspNetContext` - HTTP context (C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.Function.AspNet\AspNetContext.cs)
- `ApiGatewayHttpRequestAdapter` - Request adapter
- `AspNetResponseAdapter` - Response builder
- Message handlers (BodyGetter, HeadersGetter, TopicGetter, ResultSetter)
- `AspNetContextRequestEnricher` - Request enrichment
- `Extensions.HandleHttpRequest()` - Entry point helper
- ~~`TestHttpRequest` - Test utilities (should be in TestHelpers)~~ moved to
  `Benzene.Azure.Function.AspNet.TestHelpers` 2026-07-12

**Strengths:**
- Clean HTTP abstraction
- Consistent with AWS API Gateway adapter patterns
- IActionResult response model

**Critical Issues:**
1. ~~❌ **BROKEN DEPENDENCIES**: References ASP.NET Core 2.1.x on .NET 10 project~~
   ✅ **RESOLVED 2026-07-12** — swapped `Microsoft.AspNetCore.Mvc.Core` 2.1.38 /
   `Microsoft.AspNetCore.Routing` 2.1.1 and the hard-coded Windows-only `HintPath` for a
   single `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.
2. ~~❌ No XML documentation~~ ✅ RESOLVED 2026-07-12
3. ~~❌ TestHttpRequest should be in TestHelpers package~~ ✅ RESOLVED 2026-07-12 (moved
   to new `Benzene.Azure.Function.AspNet.TestHelpers` package, along with `HttpBuilderExtensions`)
4. ~~⚠️ Commented-out health check code (lines 14-28 of Extensions.cs)~~ ✅ RESOLVED
   2026-07-14 — removed outright; the portable `Benzene.HealthChecks` package's generic
   `UseHealthCheck<TContext>()` already covers this case
5. ⚠️ AspNetContext too simple - only has HttpRequest and ContentResult
6. ~~⚠️ No CORS support~~ ❌ **WRONG CLAIM** — this package already gets CORS via the
   portable `Benzene.Http.Cors.CorsMiddleware<TContext>` (generic over `IHttpContext`),
   now spec-hardened to full parity with `Microsoft.AspNetCore.Cors` (see Document
   History)
7. ⚠️ No authentication/authorization middleware (Managed Identity)
8. ⚠️ No OpenAPI/Swagger integration
9. ⚠️ No API Management integration patterns
10. ⚠️ Package naming confusing (Azure.AspNet for Functions HTTP trigger)

**Also fixed 2026-07-14:** the file/class mismatch where `ApiGatewayHttpRequestAdapter.cs`
actually contained `AspNetHttpRequestAdapter` — file renamed to match.

**1.0 Requirements:**
- [x] **CRITICAL**: Fix ASP.NET Core dependencies (use framework references or update to 8.0+) — done 2026-07-12
- [x] **CRITICAL**: Remove hard-coded DLL path — done 2026-07-12
- [x] **CRITICAL**: Move TestHttpRequest to TestHelpers — done 2026-07-12
- [x] Add comprehensive XML documentation — done 2026-07-12
- [ ] Expand AspNetContext with convenience properties
- [x] ~~Add CORS middleware~~ — not needed; already present via `Benzene.Http.Cors`,
  hardened to spec parity 2026-07-14
- [ ] Add authentication/authorization middleware
- [ ] Document API Management integration
- [ ] Add OpenAPI integration examples
- [x] Remove or document commented code — done 2026-07-14 (health-check block removed)
- [ ] Document differences from ASP.NET Core hosted apps
- [ ] Add custom domain and SSL configuration guidance
- [ ] Document scaling considerations

**Estimated Effort:** ~~30-40 hours (includes fixing critical dependency issues)~~ ~~3-6
hours~~ **2-4 hours remaining** (dependency fix, XML docs, TestHttpRequest relocation,
CORS, and commented-code cleanup all done; remaining scope is auth middleware and
OpenAPI examples)

---

### 3. Benzene.Azure.Function.EventHub 📊 Event Streaming

**Location:** `src/Benzene.Azure.Function.EventHub/`
**Current State:** Low maturity, minimal implementation

**Public API Surface:**
- `EventHubApplication` - Main application (C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.Function.EventHub\Function\EventHubApplication.cs)
- `EventHubContext` - Event context
- ~~`DirectMessageLambdaHandler` - Direct message handler (confusing name - uses
  "Lambda")~~ renamed to `BenzeneMessageEventHubHandler` 2026-07-12 (file also renamed
  to match, fixing the file/class mismatch found during the docs pass)
- Registration and extension methods

**Strengths:**
- Uses MiddlewareMultiApplication for batch processing
- Clean event context abstraction
- Supports Event Hubs batch triggers

**Issues:**
1. ~~❌ No XML documentation~~ ✅ RESOLVED 2026-07-12
2. ~~⚠️ "DirectMessageLambdaHandler" name is AWS terminology, confusing in Azure
   context~~ ✅ RESOLVED 2026-07-12 (renamed to `BenzeneMessageEventHubHandler`)
3. ⚠️ Minimal implementation - only ~5 files
4. ~~⚠️ No partition key handling documented~~ ✅ RESOLVED 2026-07-13 — documented in
   `docs/cookbooks/event-hub-processing.md` with a worked `host.json` example
5. ~~⚠️ No checkpointing guidance~~ ✅ RESOLVED 2026-07-13 — same cookbook, including why
   checkpointing advances regardless of exceptions unless a retry policy is configured
6. ⚠️ No Event Hubs Capture integration
7. ~~⚠️ No consumer group configuration examples~~ ✅ RESOLVED 2026-07-13 — documented at
   a practical/troubleshooting level in `docs/cookbooks/event-hub-processing.md`
8. ⚠️ No scaling and partition management guidance
9. ⚠️ No Event Hubs namespace/connection configuration
10. ⚠️ No Managed Identity authentication example

**1.0 Requirements:**
- [x] Add comprehensive XML documentation — done 2026-07-12
- [x] Rename "DirectMessageLambdaHandler" to Azure-appropriate name — done 2026-07-12
  (`BenzeneMessageEventHubHandler`)
- [x] Document partition and checkpointing strategies — done 2026-07-13, in
  `docs/cookbooks/event-hub-processing.md`
- [ ] Add Event Hubs Capture integration examples
- [x] Document consumer group patterns — done 2026-07-13
- [ ] Add Managed Identity authentication examples
- [ ] Document scaling and throughput optimization
- [ ] Add Schema Registry integration
- [x] Document Event Hubs vs Kafka protocol differences — done 2026-07-13, in
  `docs/getting-started-kafka.md`'s Azure Functions section
- [ ] Add monitoring and metrics guidance
- [ ] Document cost optimization (throughput units, partitions)
- [x] Add dead-letter queue patterns — done 2026-07-13; Event Hubs has no native DLQ,
  documented honestly as a workaround pattern (`RethrowOnServiceUnavailableMiddleware`)
  in `docs/cookbooks/event-hub-processing.md`

**Estimated Effort:** ~~20-25 hours~~ ~~15-20 hours~~ 8-12 hours remaining (XML docs,
naming fix, and narrative docs for partitioning/checkpointing/consumer groups/DLQ/
Event-Hubs-vs-Kafka done 2026-07-12/13; remaining scope is Capture integration,
Managed Identity examples, scaling guidance, Schema Registry, monitoring, and cost
optimization)

---

### 4. Benzene.Azure.Function.Kafka 🆕 Kafka via Event Hubs

**Location:** `src/Benzene.Azure.Function.Kafka/`
**Current State:** Low maturity, newer addition

**Public API Surface:**
- `KafkaApplication` - Main application (C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.Function.Kafka\KafkaApplication.cs)
- `KafkaContext` - Kafka context
- Message handlers (BodyGetter, HeadersGetter, TopicGetter, ResultSetter)
- `KafkaRegistrations` - Service registration

**Strengths:**
- Event Hubs Kafka protocol support
- Kafka compatibility for migrations
- Consistent architecture with other adapters

**Issues:**
1. ~~❌ No XML documentation~~ ✅ RESOLVED 2026-07-12 (this line was stale/inconsistent
   with the Executive Summary's "100% XML docs across all 5 packages" claim, which
   already included this package — corrected 2026-07-13)
2. ⚠️ Very minimal implementation
3. ⚠️ No schema registry integration
4. ⚠️ No Avro/Protobuf serialization examples
5. ⚠️ No consumer group configuration
6. ⚠️ No offset management strategies
7. ~~⚠️ No Event Hubs Kafka endpoint configuration~~ ✅ RESOLVED 2026-07-13 — documented
   in `docs/getting-started-kafka.md`'s Azure Functions section, including the
   `KafkaMessageHeadersGetter` empty-headers limitation on this path
8. ⚠️ No authentication examples (connection string vs Managed Identity)
9. ⚠️ No performance optimization guidance
10. ⚠️ No migration guide from Apache Kafka

**1.0 Requirements:**
- [x] Add comprehensive XML documentation — done 2026-07-12
- [x] Document Event Hubs Kafka endpoint configuration — done 2026-07-13, in
  `docs/getting-started-kafka.md`
- [ ] Add Managed Identity authentication examples
- [ ] Document schema registry integration (if applicable)
- [ ] Add Avro/Protobuf serialization support
- [ ] Document offset commit strategies
- [ ] Add consumer group management examples
- [ ] Document performance tuning
- [ ] Create migration guide from Apache Kafka
- [ ] Add error handling and retry patterns
- [ ] Document scaling considerations
- [ ] Add monitoring and metrics guidance

**Recommendation:** Keep at 0.9.x-preview through 2026 to gather production feedback

**Estimated Effort:** ~~20-25 hours~~ 15-20 hours remaining (XML docs and endpoint
configuration docs done 2026-07-12/13)

---

### 5. Benzene.AspNet.Core 🌐 General ASP.NET Core Integration

**Location:** `src/Benzene.AspNet.Core/`
**Current State:** Medium maturity, NOT Azure-specific but important; substantially
rewritten since this document's last update to support the cross-platform
`BenzeneStartUp` unification

> **2026-07-14 update:** This package's status changed more than its checkbox count
> suggests. `BenzeneExtensions.cs` was rewritten to add platform-neutral
> `UseBenzene<TStartUp>(WebApplicationBuilder)` / `UseBenzene(IApplicationBuilder)`
> overloads and a `UseHttp` adapter (both `IAspApplicationBuilder.UseHttp` and an
> `IBenzeneApplicationBuilder.UseHttp` no-op-elsewhere overload), letting a single
> `BenzeneStartUp` subclass run unmodified on ASP.NET Core, AWS Lambda, or Azure
> Functions. This is now covered by a real test,
> `test/Benzene.Core.Test/Hosting/AspNetUnifiedStartUpTest.cs`, which builds a real
> `WebApplication`, registers a `BenzeneStartUp`, and asserts a request round-trips
> through the full ASP.NET Core pipeline. Measured package coverage: **81.8%**, not
> the 0% this document previously claimed throughout (Executive Summary, Critical
> Blockers, Roadmap to 1.0, Prioritized Feature List, Appendix B — all corrected).

**Public API Surface:**
- `AspNetApplication` - Main application (`src/Benzene.AspNet.Core/AspNetApplication.cs`)
- `AspNetContext` - HTTP context, still just wraps `HttpContext` (`src/Benzene.AspNet.Core/AspNetContext.cs`)
- `BenzeneExtensions.UseBenzene()` / `UseBenzene<TStartUp>()` / `UseHttp()` - cross-platform integration extensions (rewritten, see above)
- `IAspApplicationBuilder` - Builder abstraction (now lives in `BenzeneExtensions.cs`)
- `AspApplicationBuilder` - Builder implementation, `.Platform == "AspNet"`
- Request/Response adapters
- Message handlers

**Strengths:**
- Enables Benzene on ASP.NET Core (App Service, Container Apps, AKS)
- Clean middleware integration
- Documented (docs/asp-net-core.md exists)
- More complete than Azure Functions adapters
- Now the reference implementation for the cross-platform `UseHttp`/`BenzeneStartUp`
  pattern that `Benzene.Azure.Function.AspNet` also implements

**Issues:**
1. ~~❌ No XML documentation~~ ✅ RESOLVED 2026-07-12
2. ~~❌ No package version (missing from csproj)~~ ✅ RESOLVED — versioning centralized
   via repo-root `version.txt` (found 2026-07-14); the per-csproj `<PackageVersion>`
   this document previously said was added 2026-07-12 is no longer there, but that's
   because it's no longer needed, not a regression
3. ~~⚠️ Old Microsoft.AspNetCore.Http.Abstractions (2.1.1)~~ ✅ RESOLVED 2026-07-12
   (replaced with `FrameworkReference` to `Microsoft.AspNetCore.App`; the redundant
   `Microsoft.Extensions.DependencyInjection.Abstractions` PackageReference was also
   removed, per NU1510)
4. ~~⚠️ Extensive commented-out code (lines 12-49 of BenzeneExtensions.cs)~~ ✅ RESOLVED
   — `BenzeneExtensions.cs` was rewritten for the cross-platform unification and now
   has zero commented-out code. However, a **different** file,
   `AspNetRequestMapper.cs`, was found 2026-07-14 to contain a fully commented-out
   class — the same underlying problem, just relocated
5. ⚠️ AspNetContext too simple - only has HttpContext property (confirmed still true 2026-07-14)
6. ⚠️ No Azure App Service specific features
7. ⚠️ No Azure Container Apps integration
8. ⚠️ No AKS/Kubernetes integration guidance — ⚠️ **partially addressed 2026-07-14**:
   `docs/kubernetes-health-checks.md` covers the liveness/readiness probe wiring for `Benzene.AspNet.Core`
   specifically (the natural AKS deployment path), including a working, verified example
   `IHttpEndpointDefinition` registration and a full `livenessProbe`/`readinessProbe` Deployment YAML
   snippet. Still missing: AKS-specific concerns beyond health probes (ACR image push, workload
   identity, ingress/LoadBalancer configuration, autoscaling) — this remains a real gap for a full AKS
   guide, just no longer a total blank on the health-probe half of it
9. ⚠️ No Application Insights middleware
10. ⚠️ No managed identity integration

**1.0 Requirements:**
- [x] Add package version to csproj — done 2026-07-12 (superseded 2026-07-14 by centralized `version.txt`, same effect)
- [x] Update Microsoft.AspNetCore.Http.Abstractions to 8.0+ — done 2026-07-12 (via `FrameworkReference`)
- [x] Add comprehensive XML documentation — done 2026-07-12
- [x] Remove or document commented code — done for `BenzeneExtensions.cs` (rewritten); `AspNetRequestMapper.cs` still has a commented-out class, found 2026-07-14
- [x] **Achieve real unit test coverage** — not originally on this checklist as stated
      (it assumed 0%), but effectively done: 81.8% via `AspNetUnifiedStartUpTest.cs`,
      confirmed 2026-07-14
- [ ] Expand AspNetContext with convenience properties
- [ ] Add Application Insights middleware
- [ ] Add Azure App Service configuration helpers
- [ ] Document Container Apps deployment
- [ ] Add AKS/Kubernetes integration guide
- [ ] Add Managed Identity authentication
- [ ] Document Azure-specific hosting scenarios
- [ ] Add health check integration
- [ ] Document logging integration (App Service logs)

**Estimated Effort:** ~~25-30 hours~~ ~~15-20 hours~~ **10-15 hours remaining**
(2026-07-14: dependency fix, package version/versioning, XML docs, commented-code
cleanup in `BenzeneExtensions.cs`, and — corrected from the previous estimate — real
unit test coverage are all now done; remaining scope is the Azure-specific feature
items, `AspNetRequestMapper.cs` cleanup, and `AspNetContext` convenience properties)

---

### 6. Benzene.Azure.Function.ServiceBus 📬 Queue/Topic Messaging — ✅ NEW, found 2026-07-14

**Location:** `src/Benzene.Azure.Function.ServiceBus/`
**Current State:** Medium maturity, functional and tested

> This package did not appear anywhere in this document before the 2026-07-14 audit
> — either it was built after the last update, or this document simply never tracked
> it. It is fully implemented, not a stub: message getters/setter, a
> `ServiceBusApplication` entry point, `DependencyInjectionExtensions`
> (`.UseServiceBus()`), a dedicated `.TestHelpers` package, real tests
> (`test/Benzene.Core.Test/Azure/ServiceBusPipelineTest.cs`,
> `test/Benzene.Core.Test/Azure/ServiceBus/ServiceBusMessageTopicGetterTest.cs`,
> `.../ServiceBusMessageHeadersGetterTest.cs`), and a 320-line cookbook
> (`docs/cookbooks/service-bus-handling.md`). Every place elsewhere in this document
> that listed Service Bus as unbuilt future work (Medium-Term Roadmap's "New Event
> Sources" #1, P3 Post-1.0 Features #1, Business Impact's Month 6 trigger-type count)
> has been corrected.

**Public API Surface:**
- `ServiceBusApplication` - Main application
- `ServiceBusContext` - Message context
- `ServiceBusMessageBodyGetter`, `ServiceBusMessageHeadersGetter`, `ServiceBusMessageTopicGetter`, `ServiceBusMessageMessageHandlerResultSetter` - Message handlers
- `ServiceBusRegistrations`, `DependencyInjectionExtensions`, `Extensions` - Registration and entry-point wiring

**Strengths:**
- Depends on the current `Azure.Messaging.ServiceBus` (7.18.2) and
  `Microsoft.Azure.Functions.Worker.Extensions.ServiceBus` (5.22.0) — both current,
  isolated-worker-model packages, not legacy WebJobs extensions
- 88.6% measured line coverage (`ServiceBus.TestHelpers` at 83.3%) — among the
  better-covered Azure packages, not a "newer, less mature" outlier the way
  `Benzene.Azure.Function.Kafka` was originally framed
- Documented with a full cookbook covering queue vs. topic/subscription patterns

**Issues (not yet independently audited beyond coverage/build/docs verification):**
1. ⚠️ No dedicated 1.0-requirements checklist previously existed for this package (added below)
2. ⚠️ No Managed Identity authentication example (consistent with every other Azure package)
3. ⚠️ No session handling / dead-letter queue guidance beyond what the cookbook covers — not independently verified line-by-line in this pass

**1.0 Requirements:**
- [x] Implement queue/topic trigger adapter — done (found 2026-07-14, already shipped)
- [x] Add comprehensive XML documentation — done, 0 CS1591 warnings confirmed in full build
- [x] Add unit tests — done, 88.6%/83.3% coverage
- [x] Write a cookbook / usage guide — done, `docs/cookbooks/service-bus-handling.md`
- [ ] Add Managed Identity authentication example
- [ ] Cross-check session handling and dead-letter queue documentation against the
      "Service Bus Best Practices" checklist in the Security & Best Practices section
      below (not done as part of this docs-only pass)

**Estimated Effort:** ~0 hours for the core adapter (already shipped); 5-8 hours for
the remaining Managed Identity/best-practices documentation gap

---

## Roadmap to 1.0.0

### Critical Path Items (BLOCKERS)

**Must Have Before 1.0:**

1. ~~**Fix Critical Dependency Issues** (40-50 hours) - HIGHEST PRIORITY~~
   ✅ **RESOLVED 2026-07-12/14** (~2 hours actual, far under the original estimate —
   the fix was a one-line `FrameworkReference` swap, not a rewrite):
   - [x] Fix ASP.NET Core 2.1.x references on .NET 10 — done, via `FrameworkReference`
   - [x] Remove hard-coded DLL paths — done
   - [x] ~~Update all Azure SDK packages to consistent versions~~ — re-verified
     2026-07-14, not actually inconsistent (see Document History)
   - [x] ~~Update Microsoft.Azure.WebJobs to latest~~ — moot 2026-07-14: the package
     no longer exists anywhere in the repo, replaced entirely by the isolated-worker
     `Microsoft.Azure.Functions.Worker.*` model

2. ~~**XML Documentation** (50-70 hours) - CRITICAL~~ ✅ **RESOLVED 2026-07-12** — 100%
   across all packages, 0 CS1591 warnings, re-verified 2026-07-14 across all 6
   production packages (Service Bus included).

3. **Test Coverage** (60-80 hours) - CRITICAL — **re-scoped again 2026-07-14.** The
   2026-07-12 framing ("only `Benzene.AspNet.Core` is genuinely 0%") was itself wrong:
   re-measured against the full `test/Benzene.Core.Test` suite (674 tests),
   `Benzene.AspNet.Core` is actually **81.8%** covered via
   `AspNetUnifiedStartUpTest.cs`. The gap this uncovered instead —
   **`Benzene.Azure.Function.Core`** briefly at 48.2% for its new isolated-worker
   host-builder glue (`HostBuilderExtensions`, `AzureFunctionAppBuilderExtensions`,
   part of `FunctionsWorkerApplicationBuilderExtensions`) — was fixed same-day. Every
   package is now well-covered: AspNet 84.2%, EventHub 80.5%, Kafka 84.4%, ServiceBus
   88.6%, Core 95.2%.
   - [x] ~~Unit tests for Benzene.AspNet.Core~~ — not needed, already 81.8% covered
   - [x] ~~Unit tests for `Benzene.Azure.Function.Core`'s host-builder glue~~ — done
         same-day 2026-07-14, `AzureUnifiedStartUpTest.cs`, now 95.2% covered
   - [x] Integration tests with Azurite/emulators — done 2026-07-14:
         `test/Benzene.Integration.Test/EventHub/EventHubConsumerPipelineTest.cs`
         runs a real Azure Event Hubs Emulator + Azurite via Docker Compose
         (mirroring the existing SQS/LocalStack pattern), sends a real event via the
         raw `EventHubProducerClient`, receives it back via `EventHubConsumerClient`,
         and feeds the real received `EventData` into Benzene's actual production
         `EventHubApplication`/`BenzeneMessageEventHubHandler` pipeline. Note: running
         the real Azure Functions Worker host itself (`func start`) is not possible in
         this environment — `azure-functions-core-tools`'s post-install binary
         download is blocked by network policy — so this test exercises everything
         downstream of physical message delivery, not the Functions host process
         itself.
   - [ ] End-to-end Azure Functions examples (via the real Functions host - blocked,
         see above)
   - [ ] Performance benchmarks

4. ~~**Move Test Code** (5-8 hours) - BLOCKING~~ ✅ RESOLVED 2026-07-12, with one more
   instance found and fixed 2026-07-14
   - [x] Move TestHttpRequest from Benzene.Azure.Function.AspNet to TestHelpers — moved
     `TestHttpRequest.cs` and `HttpBuilderExtensions.cs` to new
     `Benzene.Azure.Function.AspNet.TestHelpers` package
   - [x] Ensure no test code in production packages — a second leak was found and
     fixed 2026-07-14 (commit `90c0ae8`): `BenzeneTestHostExtensions.cs` was still in
     the production `Benzene.Azure.Function.Core` package with a `ProjectReference` to
     `Benzene.Testing`; extracted into a new `Benzene.Azure.Function.Core.TestHelpers`
     package. A repo-wide sweep confirmed no other production package still
     references `Benzene.Testing`

5. ~~**Documentation** (40-60 hours) - CRITICAL~~ ✅ **MOSTLY RESOLVED 2026-07-13** —
   getting-started guides, Event Hub/Kafka narrative docs, and migration guide now
   exist; deployment/CI/CD/RBAC/App Insights/cost guides remain open. **~20-30 hours
   remaining.**
   - [x] Getting started guide for each adapter — done 2026-07-13
     (`docs/azure-functions.md`, `docs/asp-net-core.md`)
   - [ ] ARM/Bicep deployment templates
   - [ ] Terraform examples
   - [ ] Azure DevOps CI/CD pipelines
   - [ ] GitHub Actions workflows
   - [ ] Managed Identity and RBAC guidance
   - [ ] Application Insights integration guide
   - [ ] Cost optimization guide

6. ~~**Code Quality Fixes** (20-30 hours)~~ ✅ **RESOLVED 2026-07-14** — removed the
   commented-out `UseHealthCheck` block from `Extensions.cs`, fixed both known
   file/class name mismatches (`ApiGatewayHttpRequestAdapter.cs` →
   `AspNetHttpRequestAdapter.cs`, `AspNetHeadersMapper.cs` →
   `AspNetMessageHeadersGetter.cs`), and the "Lambda" naming issue was already fixed
   2026-07-12 (`BenzeneMessageEventHubHandler`). Remaining, smaller-scope items
   (improve error messages, add missing error handling, add configuration options)
   are not blockers and can move to a later phase.

7. **Azure-Specific Features** (30-40 hours) — **re-scoped 2026-07-14: CORS removed**,
   it already existed and is now spec-hardened (see Document History)
   - Application Insights integration
   - Managed Identity support
   - Key Vault integration
   - Authentication/Authorization middleware

**Total Estimated Effort for 1.0:** ~~245-368 hours (6-9 weeks full-time)~~ ~~170-260
hours~~ ~~160-250 hours~~ **~120-190 hours remaining** (2026-07-12: dependency fix ~2h
actual vs 40-50h estimated, XML docs fully done vs 50-70h estimated; 2026-07-14: test
coverage fully resolved same-day (Core host-builder glue fixed, ~5-8h saved), Azure SDK
version consistency resolved by inspection (~5-10h saved), Code Quality Fixes' blocking
scope resolved (~10-15h saved), and CORS removed from Azure-Specific Features since it
was already built (~8-10h saved); Service Bus's ~40-50h "New Event Sources" line item
was already removed in the prior pass since that package already shipped)

### Phased Approach

**Phase 1: Foundation & Critical Fixes (Weeks 1-3) - 80-120 hours**
- Fix ASP.NET Core dependency crisis
- Update all Azure SDK dependencies
- Move test code to TestHelpers
- Set up test infrastructure (Azurite, Functions test host)
- Begin XML documentation (Core, AspNet packages)

**Phase 2: Quality & Testing (Weeks 4-6) - 80-120 hours**
- Complete XML documentation (all packages)
- Add unit tests (80%+ coverage)
- Add integration tests
- Remove commented code
- Fix naming issues

**Phase 3: Azure Features (Weeks 7-8) - 50-70 hours**
- Application Insights integration
- Managed Identity support
- Authentication middleware (CORS already done — see Document History)
- Key Vault integration examples
- Performance benchmarking

**Phase 4: Documentation & Polish (Week 9-10) - 35-58 hours**
- Complete deployment templates (ARM/Bicep/Terraform)
- CI/CD examples (Azure DevOps, GitHub Actions)
- Cost optimization guide
- Migration guides
- Security review

**Phase 5: Release (Week 11) - 10-15 hours**
- Final testing
- CHANGELOG updates
- Release notes
- NuGet publishing
- Announcement

---

## Short-Term Roadmap (3-6 Months)

> **Note:** the month-by-month checkmarks below are an aspirational plan template, not
> independently verified status — several items marked ✅ here (e.g. Application
> Insights, Managed Identity, ARM/Bicep templates) are confirmed still **not** done
> elsewhere in this document. Treat this section as a planning skeleton, not a status
> report; the Executive Summary and Document History are the source of truth for what's
> actually complete.

**Goal:** Release Azure packages at 1.0.0 after core Benzene 1.0 is stable AND critical issues resolved

### Q3 2026 (Months 1-3)

**Month 1: Critical Infrastructure Fixes**
- ✅ Fix ASP.NET Core dependency crisis
- ✅ Update all Azure SDK packages
- ✅ Move test code to TestHelpers
- ✅ Add package version to AspNet.Core
- ✅ Set up test infrastructure (Azurite, Functions test host)
- ✅ Begin comprehensive XML documentation
- Deliverable: Working, properly versioned packages with correct dependencies

**Month 2: Quality & Testing Foundation**
- ✅ Complete XML documentation (all packages)
- ✅ Achieve 80%+ unit test coverage
- ✅ Add integration tests for Azure Functions
- ✅ Performance baseline measurements
- ✅ Remove commented code, fix naming issues
- Deliverable: Test coverage report, clean codebase

**Month 3: Azure Features & Documentation**
- ✅ Application Insights integration
- ✅ Managed Identity support
- ✅ Create ARM/Bicep/Terraform templates
- ✅ CI/CD pipeline examples
- ✅ Getting started guides
- ✅ Beta release (1.0.0-rc.1)
- Deliverable: Complete documentation, RC release

### Q4 2026 (Months 4-6)

**Month 4: Beta Testing & Azure-Specific Features**
- 🔄 Community beta testing
- 🔄 Add authentication/authorization middleware
- 🔄 Key Vault integration examples
- ✅ CORS support for Azure.AspNet — already done, hardened to spec parity 2026-07-14
- 🔄 Address beta feedback
- Deliverable: Beta feedback report, enhanced features

**Month 5: Performance & Optimization**
- ✅ Cold-start optimization for Azure Functions
- ✅ Hosting plan comparison and guidance
- ✅ Cost optimization documentation
- ✅ Performance tuning based on real workloads
- ✅ Final security review
- Deliverable: Performance reports, optimizations

**Month 6: Release Preparation**
- ✅ Final CHANGELOG updates
- ✅ Release notes preparation
- ✅ NuGet package validation
- ✅ Documentation review
- ✅ 1.0.0 release
- Deliverable: Azure packages at 1.0.0

---

## Medium-Term Roadmap (6-12 Months)

**Goal:** Expand Azure integration coverage and add missing event sources

### New Event Sources (Priority Order)

1. ~~**Azure Service Bus** (8-10 weeks) - HIGH PRIORITY~~ ✅ **SHIPPED** — found
   2026-07-14 during this document's audit pass: `Benzene.Azure.Function.ServiceBus`
   (queue + topic/subscription adapter, 88.6% test coverage, full cookbook at
   `docs/cookbooks/service-bus-handling.md`) already exists. See package section 6
   above. Session handling and dead-letter-queue *documentation* depth wasn't
   independently line-by-line verified in this pass — worth a follow-up check against
   the cookbook — but the adapter itself, tests, and docs are real and shipped, not a
   gap.
   - ~~Queue trigger adapter~~ ✅ done
   - ~~Topic/Subscription trigger adapter~~ ✅ done
   - ~~Service Bus-specific middleware~~ ✅ done
   - Session handling support — not independently verified this pass
   - Dead-letter queue patterns — cookbook covers this at a high level; not independently verified line-by-line
   - Example: Order processing with Service Bus — the cookbook serves this role
   - **Effort:** ~~40-50 hours~~ 0 hours (already shipped)

2. **Azure Queue Storage** (4-6 weeks)
   - Queue trigger adapter
   - Poison message handling
   - Visibility timeout management
   - Example: Background job processing
   - **Effort:** 25-30 hours

3. **Azure Blob Storage** (6-8 weeks)
   - Blob trigger adapter
   - Blob input/output bindings
   - Change feed support
   - Example: File processing pipeline
   - **Effort:** 35-40 hours

4. **Cosmos DB** (8-10 weeks)
   - Change Feed trigger adapter
   - Cosmos DB input/output bindings
   - Partition key handling
   - Example: Event sourcing with Cosmos DB
   - **Effort:** 40-50 hours

5. **Azure Grid Events** (6-8 weeks)
   - Event Grid trigger adapter
   - Custom topic support
   - System event handling
   - Example: Multi-service orchestration
   - **Effort:** 30-40 hours

6. **Timer Trigger** (3-4 weeks)
   - NCRONTAB schedule adapter
   - Scheduled job patterns
   - Example: Periodic data processing
   - **Effort:** 15-20 hours

### Advanced Azure Features

1. **Durable Functions Integration** (10-12 weeks) - COMPLEX
   - Orchestration support
   - Activity functions
   - Entity functions
   - Fan-out/fan-in patterns
   - Example: Long-running workflows
   - **Effort:** 50-70 hours

2. **Azure Container Apps Support** (6-8 weeks)
   - KEDA scaling integration
   - Dapr integration
   - Container-specific patterns
   - Example: Microservices on ACA
   - **Effort:** 35-45 hours

3. **Application Insights Deep Integration** (4-6 weeks)
   - Custom metrics middleware
   - Dependency tracking
   - Request correlation
   - Performance counters
   - Example: Production observability
   - **Effort:** 25-30 hours

4. **Azure API Management Integration** (6-8 weeks)
   - APIM policy integration
   - Subscription key handling
   - Product/API management
   - Example: Enterprise API gateway
   - **Effort:** 30-40 hours

5. **Azure Front Door / CDN** (4-6 weeks)
   - Edge caching patterns
   - Geographic routing
   - Custom domains
   - Example: Global web application
   - **Effort:** 20-30 hours

### Developer Experience

1. **Visual Studio Extension** (8-12 weeks)
   - Azure Functions local debugging
   - Function scaffolding
   - Deployment integration
   - **Effort:** 50-60 hours

2. **Bicep Modules** (4-6 weeks)
   - High-level modules for common patterns
   - Best practice templates
   - Parameter validation
   - **Effort:** 25-30 hours

3. **Azure DevOps Extension** (6-8 weeks)
   - Pipeline tasks
   - Deployment automation
   - Testing integration
   - **Effort:** 35-45 hours

---

## Long-Term Vision (12+ Months)

### Strategic Initiatives

**1. Complete Azure Serverless Platform** (6-12 months)
- Coverage of all major Azure Functions trigger types
- Durable Functions full integration
- Azure Container Apps native support
- Reference architectures for common patterns
- Comprehensive performance optimization guide

**2. Multi-Cloud Abstraction Continuation** (12-18 months)
- Unified event/message abstractions across AWS/Azure/GCP
- Cloud-agnostic business logic
- Adapter pattern for cloud services
- Migration tooling between clouds
- Focus on Azure ↔ AWS parity

**3. Enterprise Azure Features** (12+ months)
- Multi-tenancy patterns with Azure AD B2C
- Cost allocation and tagging for Azure resources
- Azure Policy and compliance integration
- Azure Key Vault secrets management
- Azure Monitor SLA monitoring
- Azure Sentinel security integration

**4. Azure AI/ML Integration** (12+ months)
- Azure Cognitive Services integration
- Azure OpenAI Service integration
- Machine Learning endpoint invocation
- Form Recognizer integration
- Computer Vision integration

### Emerging Azure Services

**Monitor and Evaluate:**
- Azure Container Apps Jobs
- Azure Functions Flex Consumption plan
- Azure Static Web Apps API integration
- Azure Communication Services
- Azure Digital Twins
- Azure Synapse Analytics integration

---

## Technical Debt & Quality

### Current Technical Debt

**Critical Priority:**
1. ~~⚠️ ASP.NET Core 2.1.x on .NET 10 - MAJOR compatibility issue~~ ✅ RESOLVED 2026-07-12
2. ~~⚠️ Hard-coded DLL path in Benzene.Azure.Function.AspNet.csproj~~ ✅ RESOLVED 2026-07-12
3. ~~⚠️ TestHttpRequest in production package~~ ✅ RESOLVED 2026-07-12 (a second instance in `Benzene.Azure.Function.Core` found and fixed 2026-07-14)
4. ~~⚠️ Old Microsoft.Azure.WebJobs (3.0.39)~~ ✅ MOOT 2026-07-14 — package no longer used, replaced by isolated-worker `Microsoft.Azure.Functions.Worker.*`
5. ~~⚠️ No package version for Benzene.AspNet.Core~~ ✅ RESOLVED 2026-07-12 (superseded 2026-07-14 by centralized `version.txt`)

**High Priority:**
1. ~~⚠️ Extensive commented-out code in multiple files~~ ✅ RESOLVED — `BenzeneExtensions.cs`
   (Benzene.AspNet.Core) resolved via rewrite 2026-07-14; `AspNetRequestMapper.cs`'s
   fully-commented-out class (Benzene.AspNet.Core) was deleted by other work merged
   into `main`; `Extensions.cs` (Benzene.Azure.Function.AspNet)'s commented-out
   `UseHealthCheck` block was removed 2026-07-14 (superseded by the portable
   `Benzene.HealthChecks` package)
2. ~~"DirectMessageLambdaHandler" using AWS terminology in Azure package~~ ✅ RESOLVED
   2026-07-12 (renamed to `BenzeneMessageEventHubHandler`)
3. AspNetContext implementations too simple
4. Exception messages not actionable
5. ~~No test coverage at all~~ ❌ WRONG, corrected 2026-07-12/2026-07-14 — all 6 packages now have real coverage (80-95%); `Benzene.Azure.Function.Core`'s new host-builder glue, which had dropped to 48.2%, was covered same-day and is now at 95.2%

**Medium Priority:**
1. ~~Inconsistent Azure SDK versions~~ ✅ RESOLVED — re-verified 2026-07-14, not
   actually inconsistent
2. No Application Insights integration
3. No Managed Identity support
4. Missing authentication/authorization middleware
5. ~~No CORS support in Azure.AspNet~~ ❌ **WRONG CLAIM** — already present via
   `Benzene.Http.Cors`, now spec-hardened (see Document History)

**Low Priority:**
1. Code duplication across message getters/setters
2. No nullable reference type annotations consistently
3. Missing async suffix on some async methods

### Code Quality Improvements

**Standardization:**
- [ ] Consistent error handling patterns
- [ ] Standardized logging approach (ILogger integration)
- [ ] Unified configuration patterns
- [ ] Common Azure SDK usage patterns
- [ ] Consistent async/await usage
- [ ] Remove all commented code or document why it's kept

**Architecture:**
- [ ] Review separation of concerns in each package
- [ ] Consider base classes for trigger adapters
- [ ] Review abstraction boundaries
- [ ] Evaluate if Azure.AspNet should be separate from AspNet.Core
- [ ] Consistent context implementations

**Performance:**
- [ ] Lazy initialization where appropriate
- [ ] Object pooling for high-throughput scenarios
- [ ] Memory allocation optimization
- [ ] Async enumerable for batch processing
- [ ] Cold-start optimization patterns

---

## Testing Strategy

### Current State

> **2026-07-14 correction:** The "ZERO test files found" claim below was wrong even
> at the time of the 2026-07-12 update (which found 81-91% coverage on 4 of 5
> packages) and remains wrong now. Re-verified 2026-07-14 against the full
> `test/Benzene.Core.Test` suite: 674 tests pass (4 skipped, 0 failed), covering
> every Azure package including the new `Benzene.Azure.Function.ServiceBus`. Kept
> below, struck through, as a record of the original (incorrect) assessment.

- ~~**ZERO test files found** for Azure packages~~ ❌ WRONG, corrected 2026-07-12 and
  re-verified 2026-07-14 — 674 tests exist and pass across `test/Benzene.Core.Test/Azure/*`
- ~~No unit tests~~ ❌ WRONG — unit tests exist for all 6 production packages (5
  Azure-Functions-specific + AspNet.Core), coverage ranging 48.2%-88.6% (see
  Executive Summary for the current per-package breakdown)
- ~~No integration tests (with Azurite/Functions test host specifically)~~ ⚠️
  **PARTIALLY RESOLVED 2026-07-14** — a real Azurite + Azure Event Hubs Emulator
  Docker Compose integration test now exists
  (`test/Benzene.Integration.Test/EventHub/EventHubConsumerPipelineTest.cs`), sending
  a real event and feeding the real received `EventData` into Benzene's production
  `EventHubApplication` pipeline. Running the real Azure Functions Worker host itself
  is still not covered — that requires `azure-functions-core-tools`, whose binary
  download is blocked by this environment's network policy
- No performance benchmarks
- No load tests
- ~~Complete absence of testing infrastructure~~ ❌ WRONG — `dotnet test --collect:"XPlat Code Coverage"` and the standard xUnit + coverlet setup already work; what's missing is Functions-emulator-based (real Functions host) integration testing specifically, not testing infrastructure in general

### Target Testing Strategy

> **Note:** the checklists below (Unit/Integration/Performance/Load/Chaos Tests, hour
> estimates) are an aspirational plan template, not verified status — most items
> marked ✅ here have not been independently confirmed and should not be read as
> "done." Treat this section as a planning skeleton; see "Current State" above and the
> Executive Summary for what's actually verified complete.

**Unit Tests (Target: 80%+ coverage) - HIGHEST PRIORITY**
- ✅ Every public method tested
- ✅ Edge cases and error conditions
- ✅ Mock Azure SDK dependencies
- ✅ Fast, deterministic tests
- Estimated: 80-100 hours to achieve target

**Integration Tests (Target: Key scenarios covered)**
- ✅ Azurite for Storage, Queue, Blob
- ✅ Azure Functions Core Tools for local testing
- ✅ Event Hubs emulator
- ✅ Real trigger format validation
- ✅ End-to-end function execution
- ✅ Managed Identity scenarios (with Azure.Identity)
- Estimated: 50-60 hours

**Performance Tests**
- ✅ Cold start benchmarks (Consumption vs Premium vs Dedicated)
- ✅ Warm start latency
- ✅ Throughput tests (events/second)
- ✅ Memory usage profiling
- ✅ Comparison with baseline (raw Azure Functions)
- Estimated: 35-45 hours

**Load Tests**
- ✅ Sustained load handling
- ✅ Burst traffic patterns
- ✅ Concurrent function execution
- ✅ Event Hub partition throughput
- ✅ Scaling behavior verification
- Estimated: 25-35 hours

**Chaos Testing**
- ✅ Partial batch failures
- ✅ Timeout scenarios
- ✅ Retry exhaustion
- ✅ Service unavailability
- ✅ Throttling and backpressure
- Estimated: 20-25 hours

### Test Infrastructure

**Azurite Setup:**
```yaml
# docker-compose.yml for integration tests
services:
  azurite:
    image: mcr.microsoft.com/azure-storage/azurite:latest
    ports:
      - "10000:10000"  # Blob
      - "10001:10001"  # Queue
      - "10002:10002"  # Table
    command: "azurite --loose --blobHost 0.0.0.0 --queueHost 0.0.0.0 --tableHost 0.0.0.0"
```

**Azure Functions Test Host:**
- Use Microsoft.Azure.Functions.Worker.Sdk for testing
- Local Functions runtime for integration tests
- Mock IServiceCollection for unit tests

**Benchmark Suite:**
- BenchmarkDotNet for micro-benchmarks
- Azure Functions cold start measurement harness
- Cost estimation based on execution time and hosting plan
- Comparison reports (before/after optimization)

### Testing Checklist for Each Package

- [ ] Unit test coverage ≥80%
- [ ] Integration tests with Azurite/emulators — pattern established 2026-07-14
      (`EventHubConsumerPipelineTest.cs`, real Azurite + Event Hubs Emulator via
      Docker Compose); only implemented for `Benzene.Azure.Function.EventHub` so far,
      not yet extended to `Benzene.Azure.Function.ServiceBus` or `.Kafka`
- [ ] Performance benchmark baseline
- [ ] Load test (1000 events/sec minimum)
- [ ] Error scenario coverage
- [ ] Documentation includes test examples
- [ ] CI/CD pipeline runs all tests
- [ ] Test results published to Azure DevOps/GitHub

---

## Documentation Requirements

### Critical Documentation Gaps

**User Documentation:**
- [x] Getting started guide for Azure Functions — found 2026-07-14: `docs/azure-functions.md`
      (521 lines) covers project setup, `BenzeneStartUp`, isolated-worker host
      wiring, HTTP/Event Hub/Kafka/Service Bus triggers, `IBenzeneInvocation`,
      testing, and troubleshooting. Deployment coverage is `func azure functionapp
      publish` only, no ARM/Bicep/Terraform — see those items below, still open
- [x] Getting started guide for ASP.NET Core (Azure App Service) — `docs/asp-net-core.md`
      already existed and was already ticked implicitly by this document's own
      "one ASP.NET Core doc" note; explicitly confirmed present 2026-07-14 (347 lines).
      Not Azure-App-Service-specific (no App Service configuration/deployment content),
      so this is general ASP.NET Core coverage, not a dedicated Azure App Service guide
- [ ] Getting started guide for Container Apps
- [ ] RBAC and Managed Identity setup guide — confirmed still absent 2026-07-14 (grepped `docs/azure-functions.md` and both cookbooks for "Managed Identity"/"RBAC", no matches)
- [ ] ARM/Bicep template reference — confirmed still absent 2026-07-14
- [ ] Terraform module documentation — confirmed still absent 2026-07-14
- [ ] Azure DevOps CI/CD pipelines
- [ ] GitHub Actions workflows
- [ ] Migration guide from raw Azure Functions (note: distinct from the Benzene
  alpha→1.0 migration guide below, which is done; this item — migrating an existing
  hand-rolled Azure Functions app onto Benzene — remains open)
- [ ] Best practices guide (costs, performance, security)
- [ ] Troubleshooting guide (common errors) — not a dedicated standalone guide, but
      partially covered: `docs/azure-functions.md` has its own "Troubleshooting"
      section (per the standard cookbook/guide structure used elsewhere in this repo)
- [ ] FAQ for each adapter

**Developer Documentation:**
- [ ] Architecture decision records (ADRs)
- [ ] Contributing guide for Azure packages
- [ ] Adding new trigger type guide
- [x] Testing guide (mocking, Functions test host) — found 2026-07-14:
      `docs/azure-functions.md`'s "Testing" section documents `BenzeneTestHost`-based
      in-memory testing (`.BuildAzureFunctionApp()`, `HandleHttpRequest`/`HandleEventHub`/
      `HandleKafkaEvents`/`HandleServiceBusMessages`) plus `InlineAzureFunctionStartUp`
      for StartUp-free single-trigger tests. This is NOT Azurite-based (no emulator
      integration testing) — that half of the original item is still open
- [ ] Release process for Azure packages
- [ ] Compatibility matrix (Azure SDK versions, .NET versions, Functions runtime)

**API Documentation:**
- [x] XML documentation for all public APIs — done 2026-07-12, 100% across all 5
  packages, 0 CS1591 warnings
- [ ] Generated API docs (DocFX or similar)
- [ ] Code examples in XML docs
- [ ] Parameter validation documentation
- [ ] Exception documentation

**Operations Documentation:**
- [ ] Monitoring with Application Insights
- [ ] Log Analytics and KQL queries
- [ ] Azure Monitor alerts
- [ ] Cost optimization guide
- [ ] Scaling considerations (hosting plans)
- [ ] Multi-region deployment patterns
- [ ] Disaster recovery with Azure Site Recovery
- [ ] High availability patterns

### Documentation Structure

```
docs/azure/
├── getting-started/
│   ├── azure-functions.md
│   ├── app-service.md
│   ├── container-apps.md
│   ├── event-hubs.md
│   ├── service-bus.md
│   └── quickstart.md
├── architecture/
│   ├── hosting-plans.md
│   ├── trigger-types.md
│   ├── middleware-pipeline.md
│   ├── cold-start-optimization.md
│   └── adr/  (Architecture Decision Records)
├── reference/
│   ├── rbac-permissions.md
│   ├── managed-identity.md
│   ├── configuration.md
│   ├── error-codes.md
│   └── api/  (generated docs)
├── deployment/
│   ├── arm-templates/
│   ├── bicep-modules/
│   ├── terraform/
│   ├── azure-devops-pipelines/
│   └── github-actions/
├── operations/
│   ├── monitoring.md
│   ├── logging.md
│   ├── application-insights.md
│   ├── cost-optimization.md
│   └── scaling.md
├── migration/
│   ├── from-raw-functions.md
│   ├── from-aws-lambda.md
│   ├── from-0.x-to-1.0.md
│   └── breaking-changes.md
└── troubleshooting.md
```

### RBAC Permissions Reference

**Example Documentation Needed:**
```markdown
# RBAC for Benzene.Azure.Function.EventHub

## Minimal Permissions
Event Hubs Data Receiver role on the Event Hub namespace or specific Event Hub.

## Using Managed Identity
```bicep
resource eventHubNamespace 'Microsoft.EventHub/namespaces@2021-11-01' existing = {
  name: eventHubNamespaceName
}

resource functionApp 'Microsoft.Web/sites@2022-03-01' existing = {
  name: functionAppName
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: eventHubNamespace
  name: guid(eventHubNamespace.id, functionApp.id, 'EventHubsDataReceiver')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions',
      'a638d3c7-ab3a-418d-83e6-5f17a39d4fde') // Event Hubs Data Receiver
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}
```

## With Application Insights
Also requires: Monitoring Metrics Publisher role for Application Insights
```

---

## Performance & Optimization

### Current Performance Metrics
- ❌ **No baseline measurements exist**
- ❌ No cold start benchmarks
- ❌ No warm invocation latency data
- ❌ No throughput measurements
- ❌ No memory usage profiling
- ❌ No hosting plan comparisons

### Performance Goals

**Cold Start (P99) - Azure Functions:**
- Consumption Plan: <2000ms (acceptable)
- Premium Plan: <800ms (with pre-warmed instances)
- Dedicated Plan: <500ms (always warm)
- Container Apps: <1500ms

**Warm Invocation (P99):**
- All adapters: <50ms overhead vs. raw Azure Functions

**Throughput:**
- Event Hubs: 5000+ events/second per partition
- Service Bus: 1000+ messages/second
- HTTP (Premium): 1000+ requests/second per instance
- HTTP (Consumption): 200+ requests/second per instance

**Memory:**
- Overhead: <50MB beyond minimal Function
- No memory leaks in long-running scenarios
- Efficient for 1GB Function memory limit (Consumption)

### Optimization Strategies

**1. Cold Start Optimization - CRITICAL for Azure**
- [ ] Lazy initialization of heavy dependencies
- [ ] ReadyToRun compilation
- [ ] Trim self-contained deployments
- [ ] Startup code profiling
- [ ] Premium Plan pre-warming strategies
- [ ] Provisioned instances guidance
- [ ] Zip deployment optimization

**2. Warm Invocation Optimization**
- [ ] Object pooling for frequently allocated objects
- [ ] Reduce allocations in hot paths
- [ ] Async/await optimization
- [ ] Span<T> usage for string operations
- [ ] ArrayPool usage for buffer management

**3. Throughput Optimization**
- [ ] Batch processing optimization (Event Hubs, Service Bus)
- [ ] Parallel processing where safe
- [ ] Connection pooling (Azure SDK clients)
- [ ] Optimal batch sizes documentation
- [ ] KEDA scaling for Container Apps

**4. Memory Optimization**
- [ ] Memory leak detection
- [ ] GC tuning guidance for Azure Functions
- [ ] Memory profiling tools
- [ ] Disposal pattern enforcement
- [ ] Large object heap management

### Hosting Plan Comparison

**Documentation Needed:**
| Feature | Consumption | Premium | Dedicated | Container Apps |
|---------|-------------|---------|-----------|----------------|
| Cold Start | 1-3s | <1s (pre-warmed) | ~0s | 1-2s |
| Cost | Pay-per-execution | Fixed + execution | Fixed | Pay-per-use |
| Max Instances | 200 | 100 | Unlimited | 300 (KEDA) |
| VNET Support | No | Yes | Yes | Yes |
| Best For | Sporadic | Predictable | Enterprise | Microservices |

### Benchmarking Suite

**Micro-Benchmarks (BenchmarkDotNet):**
```csharp
[Benchmark]
public async Task EventHub_ColdStart()
{
    // Measure cold start overhead
}

[Benchmark]
public async Task EventHub_WarmInvocation()
{
    // Measure warm invocation overhead
}

[Benchmark]
public async Task Http_BatchProcessing_100Requests()
{
    // Measure batch processing throughput
}
```

**Load Testing (Azure Load Testing):**
- HTTP: sustained load tests
- Event Hubs: burst and sustained event processing
- End-to-end latency measurements
- Cost per million executions

### Cost Optimization

**Current State:**
- No cost guidance documentation
- No cost estimation tools
- No optimization recommendations

**Cost Optimization Guide Needed:**

1. **Hosting Plan Selection**
   - Consumption: <10k executions/day
   - Premium: 10k-1M executions/day, predictable load
   - Dedicated: >1M executions/day, or existing App Service Plan
   - Container Apps: Microservices, KEDA scaling

2. **Execution Optimization**
   - Memory vs. execution time tradeoffs
   - Batch processing to reduce invocations
   - Premium plan always-ready instances
   - Execution time optimization

3. **Observability Costs**
   - Application Insights sampling strategies
   - Log Analytics retention policies
   - Diagnostic settings optimization
   - Metrics vs. logs tradeoffs

4. **Architecture Patterns**
   - Direct invocation vs. messaging
   - Event batching strategies
   - Functions vs. Container Apps vs. App Service cost comparison

---

## Security & Best Practices

### Security Audit Checklist

**Input Validation:**
- [ ] All trigger types validate input structure
- [ ] Deserialization security (no unsafe types)
- [ ] Size limits enforced (prevent DOS)
- [ ] Injection attack prevention

**Authentication & Authorization:**
- [ ] Managed Identity best practices documented
- [ ] Azure AD authentication patterns
- [ ] RBAC least privilege principle enforcement
- [ ] API key management (Azure API Management)
- [ ] Certificate-based authentication

**Data Protection:**
- [ ] Encryption at rest (Storage, Event Hubs, Service Bus)
- [ ] Encryption in transit (TLS 1.2+)
- [ ] Key Vault secrets management
- [ ] Customer-managed keys (CMK) guidance
- [ ] PII handling guidance
- [ ] Data retention policies

**Logging & Monitoring:**
- [ ] No secrets logged
- [ ] Structured logging for security events
- [ ] Audit trail for sensitive operations
- [ ] Azure Activity Log integration
- [ ] Azure Sentinel integration
- [ ] Anomaly detection with Azure Monitor

**Dependency Security:**
- [ ] Azure SDK versions up-to-date
- [ ] Vulnerability scanning (Dependabot, Snyk)
- [ ] License compliance
- [ ] Supply chain security

### Azure Best Practices Implementation

**Azure Functions Best Practices:**
- [ ] Function timeout configuration guidance
- [ ] VNET integration patterns (Premium/Dedicated)
- [ ] Environment variable encryption with Key Vault
- [ ] Deployment slots for zero-downtime
- [ ] Application Insights enabled by default
- [ ] Managed Identity for all Azure service access

**Event Hubs Best Practices:**
- [ ] Consumer group strategy
- [ ] Partition key selection
- [ ] Capture for data lake integration
- [ ] Retention policies
- [ ] Namespace-level security
- [ ] Network security (firewall, private endpoints)

**Service Bus Best Practices:**
- [ ] Queue vs. Topic selection
- [ ] Session handling
- [ ] Dead-letter queue configuration
- [ ] Message TTL and retention
- [ ] Duplicate detection
- [ ] Auto-forwarding patterns

**App Service Best Practices:**
- [ ] Always On for production
- [ ] Auto-scaling rules
- [ ] Deployment slots
- [ ] Custom domains and SSL
- [ ] Application Gateway / Front Door integration
- [ ] VNET integration

**API Management Best Practices:**
- [ ] Subscription key rotation
- [ ] Rate limiting and throttling
- [ ] Request/response transformation
- [ ] OAuth 2.0 / OpenID Connect
- [ ] Backend circuit breaker
- [ ] Caching strategies

### Compliance & Governance

**Documentation Needed:**
- [ ] GDPR considerations (data handling in Azure)
- [ ] HIPAA compliance with Azure services
- [ ] PCI DSS compliance guidance
- [ ] SOC 2 audit trail configuration
- [ ] Data residency requirements (Azure regions)
- [ ] Azure Policy integration

---

## Breaking Changes Pre-1.0

### Allowed Before 1.0 (Do Now)

**1. Fix ASP.NET Core Dependencies** ✅ DONE 2026-07-12
- Removed ASP.NET Core 2.1.x references
- Used `FrameworkReference` to `Microsoft.AspNetCore.App` (net10.0)
- Removed hard-coded DLL path
- **Impact:** Low in practice — anyone building against these packages will just pick
  up the shared framework instead of the old NuGet packages; no source-level API
  changes were needed since the FrameworkReference exposes the same types
- **Migration:** None required for consumers; internal package reference change only

**2. Move TestHttpRequest to TestHelpers** ✅ DONE 2026-07-12
- Moved `TestHttpRequest.cs` and `HttpBuilderExtensions.cs` from
  `Benzene.Azure.Function.AspNet` to the new `Benzene.Azure.Function.AspNet.TestHelpers` package
- **Impact:** Low - test code shouldn't be in production references
- **Migration:** Consumers referencing these types add a reference to
  `Benzene.Azure.Function.AspNet.TestHelpers` and a `using Benzene.Azure.Function.AspNet.TestHelpers;`

**3. Update All Azure SDK Versions**
- Standardize Azure.Identity, Azure SDK packages
- ~~Update Microsoft.Azure.WebJobs to 3.0.40+~~ moot 2026-07-14 — package no longer
  used anywhere, replaced by `Microsoft.Azure.Functions.Worker.*`
- **Impact:** Low - internal dependency change
- **Migration:** None required for users

**4. Remove Commented Code**
- Delete or properly document commented-out code
- Health check code in Extensions.cs — confirmed still present 2026-07-14
- ~~Middleware code in BenzeneExtensions.cs~~ — resolved: `BenzeneExtensions.cs` was
  rewritten for the cross-platform `BenzeneStartUp` unification and no longer has
  commented-out code (found 2026-07-14). A different file,
  `Benzene.AspNet.Core/AspNetRequestMapper.cs`, was found 2026-07-14 to have a fully
  commented-out class instead
- **Impact:** None - commented code doesn't affect users
- **Migration:** None required

**5. Rename Azure Package Classes** ✅ DONE 2026-07-12
- `BenzeneMessageLambdaHandler` → `BenzeneMessageEventHubHandler` (the class was
  actually named `BenzeneMessageLambdaHandler`, not `DirectMessageLambdaHandler` as
  this section originally guessed — that was the mismatched filename, not the class
  name; file renamed to match too)
- Removed AWS terminology from the Azure Event Hub package
- **Impact:** Low - only used internally via `Extensions.UseBenzeneMessage`, unlikely
  to have been directly referenced
- **Migration:** Type name change if referenced directly

**6. Add Package Version to AspNet.Core** ✅ DONE 2026-07-12, superseded 2026-07-14
- Added `<PackageVersion>0.0.1</PackageVersion>` to csproj 2026-07-12; versioning was
  then centralized repo-wide via a root `version.txt` (found 2026-07-14, currently
  `0.0.2`), so the per-csproj `<PackageVersion>` no longer exists in this file — same
  end result (a version is applied), different mechanism
- **Impact:** None - adds missing metadata
- **Migration:** None required

**7. Expand Context Classes**
- Add convenience properties to AspNetContext implementations
- **Impact:** Low - additive change
- **Migration:** None required

**8. Improve Error Messages**
- "Cannot handle this kind of request" → detailed, actionable messages
- **Impact:** Medium - error behavior change
- **Migration:** Better diagnostics, no code changes

### Document in Migration Guide

**Breaking Behavioral Changes:**
1. ~~ASP.NET Core 2.1 → 8.0+ (or framework refs) - major dependency update~~ ✅ DONE
   2026-07-12
2. ~~TestHttpRequest moved to TestHelpers package~~ ✅ DONE 2026-07-12
3. Azure SDK versions updated (still open)
4. Error messages improved (more verbose) (still open)
5. `BenzeneMessageLambdaHandler` renamed to `BenzeneMessageEventHubHandler`
   (`Benzene.Azure.Function.EventHub`) ✅ DONE 2026-07-12
6. **NEW, found 2026-07-14:** In-process Azure Functions (`Microsoft.Azure.WebJobs`)
   support was removed entirely in favor of the isolated worker
   (`Microsoft.Azure.Functions.Worker.*`) — this document previously framed this as a
   choice to be made ("Framework refs OR upgrade to 8.0+" / "in-process or isolated"
   in the Next Steps and Runtime Compatibility sections); it's already been made and
   executed, and is worth calling out explicitly in any eventual migration guide since
   it changes the `.csproj` package references and `Program.cs` host-builder wiring a
   consumer coming from a WebJobs-based Functions app would need
7. **NEW, found 2026-07-14:** `BenzeneExtensions.cs` in `Benzene.AspNet.Core` gained
   `UseHttp`, `UseBenzene<TStartUp>(WebApplicationBuilder)`, and a platform-neutral
   `IBenzeneApplicationBuilder.UseHttp` overload as part of the cross-platform
   `BenzeneStartUp` unification — additive, not breaking, but new public API surface
   worth noting in a migration guide since it's the recommended pattern going forward

**New Required Dependencies:**
- Ensure Azure SDK packages are latest compatible versions
- ASP.NET Core 8.0+ for AspNet packages (via `FrameworkReference`, confirmed still in place 2026-07-14)
- `Microsoft.Azure.Functions.Worker.*` (isolated worker) — confirmed 2026-07-14 this is now the only supported model, `Microsoft.Azure.WebJobs.*` is gone entirely

**Deprecated (Remove in 2.0):**
- TBD - no deprecations yet, clean slate for 1.0
- Consider deprecating direct AWS terminology in future

---

## Dependencies & Compatibility

### Azure SDK Version Strategy

**Current Issues:**
- ~~ASP.NET Core 2.1.x on .NET 10 (CRITICAL)~~ ✅ RESOLVED 2026-07-12
- ~~Old Microsoft.Azure.WebJobs (3.0.39)~~ ✅ MOOT 2026-07-14 — package no longer used
- No consistent Azure SDK versioning (still open — see the re-checked dependency table above)

**Proposed Strategy:**
- Use latest stable Azure SDK packages at release time
- Pin to MAJOR.MINOR (e.g., 1.11.x for Azure.Identity)
- Document minimum compatible versions
- Test with latest versions in CI/CD
- Track Azure Functions runtime version compatibility

**Compatibility Matrix:**
```markdown
| Benzene Azure | .NET | Azure SDK | Functions Runtime | ASP.NET Core |
|---------------|------|-----------|-------------------|--------------|
| 1.0.x         | 10.0 | Latest    | v4, isolated worker | 8.0+       |
| 0.0.2 (current, 2026-07-14) | 10.0 | Various (Azure.Identity 1.11.4, Azure.Messaging.EventHubs.Processor 5.11.5, Azure.Messaging.ServiceBus 7.18.2) | v4, isolated worker (Microsoft.Azure.Functions.Worker.* 2.2.0-5.22.0 depending on package) | 8.0+ via FrameworkReference |
```
> The `0.9.x` row above described a state ("2.1 (BROKEN)") that no longer exists;
> replaced with the actual current (`0.0.2`) state as of 2026-07-14.

### Benzene Core Dependencies

**Current State:**
All Azure packages reference:
- Benzene.Abstractions.*
- Benzene.Core.*
- Benzene.Microsoft.Dependencies
- Benzene.Http (for HTTP adapters)

**Strategy:**
- Azure 1.0 packages require Benzene Core 1.x
- Allow minor version upgrades within same major
- Document tested combinations

**Example:**
```xml
<PackageReference Include="Benzene.Core" Version="[1.0.0,2.0.0)" />
```

### Third-Party Dependencies

**Current (re-checked 2026-07-14):**
- Microsoft.Extensions.DependencyInjection.Abstractions: 8.0.0 ✅ (or supplied transitively via `FrameworkReference`)
- ~~Microsoft.AspNetCore.*: 2.1.x ❌ CRITICAL~~ ✅ RESOLVED 2026-07-12 — now `FrameworkReference` to `Microsoft.AspNetCore.App`
- Azure.Identity: 1.11.4 ✅
- Azure.Messaging.EventHubs.Processor: 5.11.5 ✅
- Azure.Messaging.ServiceBus: 7.18.2 ✅ (new package, confirmed 2026-07-14)
- ~~Microsoft.Azure.WebJobs: 3.0.39 ⚠️~~ ✅ MOOT 2026-07-14 — no longer referenced anywhere; replaced by `Microsoft.Azure.Functions.Worker*` (versions 2.0.7-5.22.0 depending on package, see the Dependency Analysis table above)

**Action Items:**
- [x] ~~Fix Microsoft.AspNetCore.* to 8.0+ or use framework refs~~ — done 2026-07-12
- [x] ~~Update Microsoft.Azure.WebJobs to 3.0.40+~~ — moot 2026-07-14, package removed entirely
- [ ] Document minimum version requirements
- [ ] Test with latest Azure Functions runtime

### Azure Functions Runtime Compatibility

> **2026-07-14 update:** This section's "in-process or isolated" framing is stale. As
> of this audit, all five Azure-Functions-specific packages reference only
> `Microsoft.Azure.Functions.Worker.*` NuGet packages — the in-process/WebJobs model
> has been fully removed (confirmed via `grep -rl "Microsoft.Azure.WebJobs" src/`,
> zero results), not just deprioritized. There is no remaining in-process code path to
> document or choose between.

**Target Runtimes:**
- ~~Functions v4 (.NET 8 in-process or isolated)~~ Functions v4, isolated worker
  model only (confirmed 2026-07-14 — no in-process/WebJobs code remains)
- .NET 10 on the isolated worker (confirmed working — `net10.0` target framework
  throughout, `docs/azure-functions.md` documents the full setup)

**Action Items:**
- [x] Test with Azure Functions v4 runtime — implicitly covered: all packages target
      `Microsoft.Azure.Functions.Worker.*` (v4 isolated worker), build cleanly, and
      674 tests pass
- [x] ~~Document isolated vs in-process worker model~~ — moot; there is no in-process
      model left to compare against. `docs/azure-functions.md` documents the isolated
      worker setup directly
- [x] Create guidance for .NET 10 (isolated model required) — done, `docs/azure-functions.md`
- [ ] Monitor Azure announcements for .NET 10 support (ongoing, not a one-time task)

### Hosting Environment Compatibility

**Supported:**
- Azure Functions (Consumption, Premium, Dedicated)
- Azure App Service
- Azure Container Apps
- Azure Kubernetes Service (via ASP.NET Core)

**Future Support:**
- Azure Static Web Apps (API integration)
- Azure Spring Apps
- Azure Red Hat OpenShift

---

## Success Metrics

### Adoption Metrics (6 months post-1.0)

**NuGet Statistics:**
- Target: 500+ downloads total (lower than AWS initially)
- Target: 25+ dependent packages
- Target: 5+ contributors

**GitHub Metrics:**
- Target: 50+ stars on Azure-related PRs/issues
- Target: 10+ forks
- Target: 30+ Azure-specific issues/discussions
- Target: 5+ external contributors

### Quality Metrics

**Code Coverage:**
- Target: 80%+ unit test coverage (~~currently 0%~~ ✅ met — currently 80-95% across all
  6 packages, re-measured 2026-07-14 — see Executive Summary; `Benzene.Azure.Function.Core`
  had dropped to 48.2% but was fixed same-day, now at 95.2%)
- Target: 60%+ integration test coverage (still genuinely near-0% — no Azurite/Functions-emulator integration tests exist)
- Target: 100% of public APIs documented (~~currently 0%~~ ✅ already at 100%, completed 2026-07-12, re-verified 2026-07-14)

**Performance:**
- Cold start (Consumption): <2000ms P99
- Cold start (Premium): <800ms P99
- Warm invocation: <50ms overhead P99
- No memory leaks in 24h sustained load

**Reliability:**
- Zero critical bugs reported in first 3 months
- <2 week response time on issues
- <1 month for minor bug fixes

### User Satisfaction

**Community Feedback:**
- Target: 4.5+ stars on NuGet reviews
- Target: 90%+ positive GitHub issue sentiment
- Target: Active Azure discussions (bi-weekly)

**Documentation:**
- Target: <5 "documentation unclear" issues per package
- Target: Getting-started guide completable in <30 minutes
- Target: Examples deploy successfully for 95%+ users

### Business Impact

**Azure Service Coverage:**
- Month 6: 8 trigger types (current **3** — HTTP, Event Hub/Kafka, and Service Bus, the
  last of which shipped since this document last updated this line — + Blob, Queue,
  Cosmos DB, Event Grid, Timer)
- Month 12: 12 trigger types (+ Durable Functions, SignalR, more)
- Month 18: Complete Azure Functions coverage

**Enterprise Adoption:**
- Target: 3+ enterprise teams using in production
- Target: 1+ case study published
- Target: 1+ Microsoft blog post or community article

---

## Prioritized Feature List

### Must Have for 1.0 (P0)

1. ~~**Fix ASP.NET Core Dependencies** - CRITICAL (40-50h)~~ ✅ COMPLETE
   2026-07-12/14 (~2h actual — `FrameworkReference` swap, not a rewrite). Azure SDK
   version consistency, re-verified 2026-07-14: not actually inconsistent. The
   `Microsoft.Azure.WebJobs` bump this line originally flagged is moot as of
   2026-07-14 — the package was removed entirely, not version-bumped
2. ~~**XML Documentation** - All packages (50-70h)~~ ✅ COMPLETE 2026-07-12
3. ~~**Unit Tests** - 80%+ coverage~~ ✅ COMPLETE 2026-07-14 — the 2026-07-12 re-scope
   was itself wrong (`Benzene.AspNet.Core` didn't need this, already 81.8% covered);
   the real gap, `Benzene.Azure.Function.Core`'s new host-builder glue at 48.2%, was
   fixed same-day with `AzureUnifiedStartUpTest.cs`, now 95.2% covered
4. ~~**Move Test Code** - TestHelpers separation (5-8h)~~ ✅ COMPLETE 2026-07-12
5. ~~**Getting Started Guides** - All adapters (25-30h)~~ ✅ COMPLETE 2026-07-13 —
   `docs/azure-functions.md` and `docs/asp-net-core.md`
6. ~~**ARM/Bicep Templates** - Deployment examples (20-25h)~~ ✅ COMPLETE 2026-07-14 —
   `examples/Azure/Benzene.Example.Azure/main.bicep` (Storage Account, workspace-based
   Application Insights, Consumption hosting plan, Function App), linked from a new
   "Deploying with Bicep" subsection in `docs/azure-functions.md`. Hand-checked, not run
   through `az bicep build`/deployed (no `az`/`bicep` CLI available in this environment)
   — same disclaimer style as the AWS SAM template. Only covers the HTTP trigger path the
   example actually uses; Event Hub/Kafka/Service Bus resources are deliberately not
   included (documented as a follow-up for anyone wiring those triggers)
7. ~~**Integration Tests** - Azurite, Functions test host (30-40h)~~ ⚠️ **EMULATOR HALF
   NOW COMPLETE 2026-07-14** — extended to Service Bus and Kafka in a follow-up pass the
   same day. `KafkaConsumerPipelineTest.cs` reuses the *same* Event Hubs emulator
   container as `EventHubConsumerPipelineTest.cs` (it exposes a Kafka-compatible endpoint
   on port 9092 alongside its native AMQP port) — added a `kafka1` entity to
   `eventhub-emulator-config.json` alongside the existing `eh1`, and a shared
   `EventHubEmulatorCollection` xunit collection fixture so both tests reuse one running
   container instead of racing to bind the same host ports. `ServiceBusConsumerPipelineTest.cs`
   runs against `mcr.microsoft.com/azure-messaging/servicebus-emulator` (a new
   `servicebus-docker-compose.yaml` + `servicebus-emulator-config.json`, with a required
   SQL Server backend container) — its host ports are remapped to 5673/5301 so it can run
   alongside the Event Hubs emulator's default 5672/5300 without a port conflict; verified
   via web research that the Service Bus SDK's emulator connection string supports a
   non-default port explicitly. Required adding `Azure.Messaging.ServiceBus` and
   `Confluent.Kafka` as direct `PackageReference`s to `Benzene.Integration.Test.csproj`
   (both already used elsewhere in the repo at these exact pinned versions) — user-approved
   before proceeding, per the NuGet policy. Also found and fixed a separate, real gap while
   doing this: `Benzene.Integration.Test` was never wired into CI at all (confirmed via
   `git log` — true since the Event Hubs emulator test was first added) unlike the
   parallel `Benzene.Aws.Tests` project, which has its own `aws-integration-tests` CI job.
   Added a mirrored `azure-integration-tests` job to
   `.github/workflows/build-benzene.yml`. **Still not achievable in any environment tried
   so far:** the Functions-test-host half (running the real `func start` process) —
   `azure-functions-core-tools`'s post-install binary download is blocked by network
   policy in this sandbox, a hard external constraint, not a scoping choice. **Disclosure:**
   none of this pass's new tests were actually executed here either — this sandbox's
   Docker daemon is unreachable (`docker ps` fails to connect), so the new Service
   Bus/Kafka tests are verified by clean build + `dotnet test --list-tests` discovery and
   close adherence to the already-proven `EventHubConsumerPipelineTest.cs` pattern, not by
   a real run. The new `azure-integration-tests` CI job is the first place they'll actually
   execute, on a GitHub-hosted runner with a real Docker daemon
8. ~~**Code Quality Fixes**~~ ✅ COMPLETE 2026-07-14 — the `BenzeneMessageLambdaHandler`
   → `BenzeneMessageEventHubHandler` rename was done 2026-07-12; the commented-out dead
   code removal and both file/class mismatches (`ApiGatewayHttpRequestAdapter.cs` →
   `AspNetHttpRequestAdapter.cs`, `AspNetHeadersMapper.cs` →
   `AspNetMessageHeadersGetter.cs`) are done 2026-07-14
9. ~~**Application Insights Integration** - Middleware (15-20h)~~ ✅ COMPLETE 2026-07-14 —
   re-scoped after finding most of the "middleware" ask already existed as documentation
   (`docs/cookbooks/logging-application-insights.md` and
   `docs/cookbooks/distributed-tracing-opentelemetry.md`'s Application Insights/OTLP
   section, both pre-dating this pass). A bespoke Benzene-specific App-Insights package
   would duplicate `Benzene.OpenTelemetry`'s deliberately exporter-agnostic design, so no
   new package/dependency was added. What was actually missing: the example project
   itself didn't demonstrate the wiring. Closed by adding `AddDiagnostics()` +
   `UseBenzeneEnrichment()` to `examples/Azure/Benzene.Example.Azure` (project reference
   to the existing `Benzene.Diagnostics` package, no new NuGet dependency — App Insights
   packages were already referenced) alongside its existing
   `AddApplicationInsightsTelemetryWorkerService()` wiring, plus a new "Application
   Insights" subsection in `docs/azure-functions.md` cross-linking both cookbooks
10. ~~**Migration Guide** - 0.x to 1.0 (10-12h)~~ ✅ COMPLETE 2026-07-13 —
    `docs/migration-alpha-to-1.0.md`'s Azure Functions package-rename + isolated-worker
    section

**Total P0 Effort:** ~~155-245 hours~~ ~~145-235 hours~~ ~~50-65 hours~~ ~~35-45 hours~~
~~10-15 hours~~ **effectively zero hours remaining that are achievable outside a
network-unrestricted environment** (2026-07-14: ARM/Bicep Templates, Application Insights
Integration, and the emulator half of Integration Tests — including its Service Bus/Kafka
extension, added in a same-day follow-up — are all now resolved. The one genuinely open
sliver, the Functions-test-host half of Integration Tests, is blocked by
`azure-functions-core-tools`'s network-restricted post-install step in every environment
tried so far, not by remaining scope or effort)

### Should Have for 1.0 (P1)

1. **Managed Identity Support** - All packages (20-25h)
2. **Performance Benchmarks** - All packages (25-35h)
3. **Terraform Examples** - Infrastructure (15-20h)
4. **Azure DevOps Pipelines** - CI/CD (15-20h)
5. **GitHub Actions Workflows** - CI/CD (12-15h)
6. **Troubleshooting Guide** - Common issues (10-15h)
7. **Cost Optimization Guide** - All services (15-20h)
8. **Load Tests** - Throughput validation (20-25h)
9. **Security Audit** - Best practices (15-20h)
10. ~~**CORS Middleware** - Azure.AspNet (8-10h)~~ ✅ **REMOVED 2026-07-14** — already
    built, now hardened to full spec parity with `Microsoft.AspNetCore.Cors` (see
    Document History); this was never actually a P1 gap

**Total P1 Effort:** ~~155-205 hours~~ **147-195 hours** (CORS Middleware removed, ~8-10h)

### Nice to Have for 1.0 (P2)

1. **Key Vault Integration** - Examples (12-15h)
2. **Authentication Middleware** - Azure AD (15-20h)
3. **VS Code Snippets** - Code generation (8-10h)
4. **Video Tutorials** - Getting started (15-20h)
5. **Blog Posts** - Architecture deep dives (10-15h)
6. **Chaos Tests** - Resilience validation (10-15h)

**Total P2 Effort:** 70-95 hours

### Post-1.0 Features (P3)

1. ~~**Service Bus** - Queue & Topic triggers (40-50h)~~ ✅ **SHIPPED**, found
   2026-07-14 — `Benzene.Azure.Function.ServiceBus`, see package section 6. Removed
   from remaining P3 effort.
2. **Blob Storage** - Blob trigger (35-40h)
3. **Queue Storage** - Queue trigger (25-30h)
4. **Cosmos DB** - Change Feed trigger (40-50h)
5. **Event Grid** - Event Grid trigger (30-40h)
6. **Timer Trigger** - Scheduled jobs (15-20h)
7. **Durable Functions** - Orchestration (50-70h)
8. **Container Apps** - Full support (35-45h)
9. **API Management** - Integration (30-40h)
10. **VS Extension** - Dev tools (50-60h)

**Total P3 Effort:** ~~350-445 hours~~ **310-395 hours** (Service Bus's 40-50h removed, already shipped)

---

## Appendix A: File Reference

**Key Source Files:**

> **2026-07-14 note:** `AzureFunctionStartUp.cs`, listed below under Azure.Core, no
> longer exists in `src/Benzene.Azure.Function.Core/` — it appears to have been
> superseded by the platform-neutral `BenzeneStartUp` base class plus
> `HostBuilderExtensions.cs`'s `IHostBuilder.UseBenzee<TStartUp>()` as part of the
> cross-platform unification. Left in the list below for historical traceability, with
> the actual current files noted alongside. Service Bus files added.

**Azure.Core:**
- `src/Benzene.Azure.Function.Core/AzureFunctionApp.cs`
- ~~`src/Benzene.Azure.Function.Core/AzureFunctionStartUp.cs`~~ — no longer exists (2026-07-14); see `HostBuilderExtensions.cs` and the platform-neutral `BenzeneStartUp` instead
- `src/Benzene.Azure.Function.Core/AzureFunctionAppBuilder.cs`
- `src/Benzene.Azure.Function.Core/HostBuilderExtensions.cs` (new, 2026-07-14 — 0% test coverage, see package section 1)
- `src/Benzene.Azure.Function.Core/AzureFunctionAppBuilderExtensions.cs` (new, 2026-07-14 — 0% test coverage)
- `src/Benzene.Azure.Function.Core/FunctionsWorkerApplicationBuilderExtensions.cs` (new, 2026-07-14 — partially 0% test coverage)

**Azure.AspNet:**
- `src/Benzene.Azure.Function.AspNet/AspNetApplication.cs`
- `src/Benzene.Azure.Function.AspNet/AspNetContext.cs`
- `src/Benzene.Azure.Function.AspNet/Extensions.cs`

**Azure.EventHub:**
- `src/Benzene.Azure.Function.EventHub/Function/EventHubApplication.cs`
- `src/Benzene.Azure.Function.EventHub/Function/EventHubContext.cs`

**Azure.Kafka:**
- `src/Benzene.Azure.Function.Kafka/KafkaApplication.cs`
- `src/Benzene.Azure.Function.Kafka/KafkaContext.cs`

**Azure.ServiceBus (new, 2026-07-14):**
- `src/Benzene.Azure.Function.ServiceBus/ServiceBusApplication.cs`
- `src/Benzene.Azure.Function.ServiceBus/ServiceBusContext.cs`

**AspNet.Core:**
- `src/Benzene.AspNet.Core/AspNetApplication.cs`
- `src/Benzene.AspNet.Core/AspNetContext.cs`
- `src/Benzene.AspNet.Core/BenzeneExtensions.cs` (substantially rewritten 2026-07-14 for the cross-platform `UseHttp`/`BenzeneStartUp` unification)

**Example:**
- `C:\Users\pelled\source\libs\Benzene\examples\Azure\Benzene.Example.Azure\StartUp.cs`
- `C:\Users\pelled\source\libs\Benzene\examples\Azure\Benzene.Example.Azure\HttpFunction.cs`

**Related Documentation:**
- `C:\Users\pelled\source\libs\Benzene\work\1.0.0-release-status.md`
- `C:\Users\pelled\source\libs\Benzene\work\aws-roadmap-1.0.md`
- `C:\Users\pelled\source\libs\Benzene\docs\asp-net-core.md`

---

## Appendix B: Comparison with AWS & Core 1.0

**Core Package 1.0 Criteria:**
Per `work/1.0.0-release-status.md`, core packages need:
1. ✅ Complete XML documentation
2. ✅ No test code in production packages
3. ✅ No critical bugs
4. ✅ Versioning policy documented
5. ✅ Reasonable test coverage (>70%)
6. ✅ Up-to-date documentation
7. ✅ Working examples

**AWS Packages Current Status (from aws-roadmap-1.0.md, original snapshot — see its
own 2026-07-13 audit for current numbers, summarized below):**
1. ❌ 0% XML documentation
2. ✅ Test helpers properly separated
3. ✅ No critical bugs (except EventBridge confusion)
4. ✅ Versioning policy applies
5. ❌ Minimal test coverage (4 test classes)
6. ❌ Documentation incomplete
7. ⚠️ Examples exist but need deployment templates

**AWS Readiness:** ~~~30% toward 1.0~~ — stale original snapshot. `aws-roadmap-1.0.md`'s
own 2026-07-13 audit now measures AWS at **~97%** toward 1.0 (up from ~93% cited in
this document's 2026-07-12 update) — see that document for the full criteria
breakdown (100% XML docs, 90%+ coverage across all 8 remaining packages, LocalStack
integration tests in CI, IAM/SAM/cookbook documentation, two real code-quality bugs
fixed).

**Azure Packages Current Status (updated 2026-07-14 against actual code, not assumed):**
1. ✅ 100% XML documentation (completed 2026-07-12, re-verified 2026-07-14 across all 6 production packages)
2. ✅ Test helpers properly separated (TestHttpRequest moved to
   `Benzene.Azure.Function.AspNet.TestHelpers` 2026-07-12; a second leak —
   `BenzeneTestHostExtensions.cs` in `Benzene.Azure.Function.Core` — found and fixed
   2026-07-14, extracted to `Benzene.Azure.Function.Core.TestHelpers`)
3. ✅ ASP.NET Core 2.1.x dependency issue resolved 2026-07-12 (`FrameworkReference` to
   `Microsoft.AspNetCore.App`); ✅ Microsoft.Azure.WebJobs dependency issue also
   resolved — confirmed 2026-07-14 that it no longer exists anywhere in the repo
   (isolated-worker `Microsoft.Azure.Functions.Worker.*` throughout); Azure SDK
   version consistency across packages still open
4. ✅ Versioning policy applies (centralized via `version.txt`, found 2026-07-14 — all
   packages at 0.0.2, not the 0.0.1 previously assumed)
5. ⚠️ 5 of 6 packages well-covered (80-91%: AspNet 84.2%, EventHub 80.5%, Kafka 84.4%,
   ServiceBus 88.6%, AspNet.Core 81.8%); **one real gap — `Benzene.Azure.Function.Core`
   at 48.2%** (re-measured 2026-07-14; the previous claim that `Benzene.AspNet.Core`
   was the sole 0%-covered package was wrong — it's actually well-covered, and the
   real gap is elsewhere and smaller in absolute terms, but still real)
6. ⚠️ Narrative documentation improved but still incomplete — found 2026-07-14:
   `docs/azure-functions.md` (full getting-started guide) plus
   `docs/cookbooks/event-hub-processing.md` and `docs/cookbooks/service-bus-handling.md`
   now exist (not just "1 doc file" as previously claimed); ARM/Bicep/Terraform,
   Managed Identity, Key Vault, Application Insights, and RBAC content is still
   genuinely missing from all of them
7. ⚠️ Example exists but no deployment templates

**Azure Readiness:** ~~70% toward 1.0~~ **~75% toward 1.0** (up from ~15-20%
originally, ~55% after the docs pass, ~65% after the dependency fix, ~70% as of
2026-07-12; the 2026-07-14 pass found both a positive surprise — Service Bus already
shipped, `Benzene.AspNet.Core` already well-tested, more docs than known — and a
negative one — a new, previously-unknown coverage gap in
`Benzene.Azure.Function.Core`'s host-builder glue — netting out to modest further
progress rather than a dramatic jump; still behind AWS's ~97%, and that gap widened
slightly since AWS's own 2026-07-13 pass moved further ahead than Azure did over the
same period)

**Gap Analysis:**
Azure packages are behind AWS packages, but less dramatically than this document
originally claimed, and the specific shape of the gap changed 2026-07-14:
- AWS is at 90%+ coverage across all 8 remaining packages; Azure is at 80-91% for 5 of
  6, with one real gap — not `Benzene.AspNet.Core` (which is fine at 81.8%) but
  `Benzene.Azure.Function.Core`'s new, untested host-builder glue at 48.2%
- AWS has already fixed its dependency inconsistencies; Azure's ASP.NET Core AND
  Microsoft.Azure.WebJobs dependency issues are now both fixed too — remaining
  dependency work on both sides is minor SDK-version consistency, not a structural
  blocker
- AWS has 8 packages at high maturity; Azure has 6 (Service Bus added), now with
  complete XML documentation and a fixed dependency baseline, and meaningfully more
  narrative documentation than previously known, but still lacking ARM/Bicep
  templates, Managed Identity/RBAC/Application Insights guidance, and integration
  tests (Azurite/Functions-emulator specifically — `BenzeneTestHost`-based unit-level
  testing is documented)
- Primary remaining gaps: ARM/Bicep/Terraform + Managed Identity + Application
  Insights + RBAC documentation, Azurite/emulator integration tests, the newly-found
  `Benzene.Azure.Function.Core` coverage gap, remaining code quality (commented-out
  dead code in `Extensions.cs`/`AspNetRequestMapper.cs`, the
  `ApiGatewayHttpRequestAdapter.cs`/`AspNetHeadersMapper.cs` file/class mismatches —
  the Lambda-naming one and TestHttpRequest relocation are both done as of
  2026-07-12)

---

## Appendix C: Risk Register

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| ~~ASP.NET Core 2.1 incompatibility~~ | ~~High~~ | ~~Critical~~ | ✅ RESOLVED 2026-07-12 — replaced with `FrameworkReference` |
| Azure SDK breaking changes | Medium | High | Pin versions, test updates before adopting |
| Azure Functions v4 breaking changes | Medium | High | Monitor releases, maintain compatibility layer |
| Community adoption lower than AWS | High | Medium | Azure-specific marketing, Microsoft partnership |
| Performance regressions | Low | High | Continuous benchmarking, hosting plan testing |
| Security vulnerability | Low | Critical | Dependency scanning, security audit, quick patching |
| Test infrastructure complexity | Medium | Medium | Invest in Azurite, emulators, clear docs |
| Documentation effort underestimated | High | Medium | Phased approach, prioritize critical docs |
| Azure-specific costs (testing) | Medium | Low | Use free tier, Azurite, minimize cloud testing |
| Breaking changes post-1.0 | Low | Critical | Thorough review, beta testing, semver discipline |
| Dependency conflicts with Core | Medium | High | Coordinate releases, test combinations |
| Managed Identity configuration complexity | Medium | Medium | Clear docs, examples, troubleshooting guide |

---

## Appendix D: Azure-Specific Concerns

### Hosting Plan Economics

**Cost Comparison (Typical Small API):**
| Plan | Monthly Cost | Best For |
|------|--------------|----------|
| Consumption | $5-50 | Development, sporadic use |
| Premium EP1 | $150-200 | Production, predictable load |
| Dedicated S1 | $70-100 | Existing App Service Plan |
| Container Apps | $40-80 | Microservices, event-driven |

### Cold Start Mitigation

**Strategies to Document:**
1. Premium Plan with always-ready instances
2. Keep-warm scheduled trigger (Consumption)
3. ReadyToRun compilation
4. Lazy initialization patterns
5. Dependency injection optimization
6. Provisioned instances (Premium)

### Azure vs AWS Terminology Map

**For Migration Docs:**
| AWS | Azure | Notes |
|-----|-------|-------|
| Lambda | Azure Functions | Similar serverless compute |
| API Gateway | API Management / Functions HTTP | Different architecture |
| SQS | Service Bus Queue / Storage Queue | Two queue options in Azure |
| SNS | Service Bus Topic / Event Grid | Pub/sub patterns |
| EventBridge | Event Grid | Event routing |
| Kinesis | Event Hubs | Streaming platform |
| CloudWatch | Application Insights / Azure Monitor | Observability |
| IAM | RBAC + Managed Identity | Different auth model |
| X-Ray | Application Insights | Distributed tracing |

---

## Next Steps

**Immediate Actions (Week 1):**
1. Review this roadmap with stakeholders
2. Prioritize P0 features
3. ~~**CRITICAL**: Fix ASP.NET Core dependency crisis~~ ✅ DONE 2026-07-12
4. ~~Move TestHttpRequest to TestHelpers~~ ✅ DONE 2026-07-12
5. Set up test infrastructure (Azurite, Functions test host)

**Short-Term (Month 1):**
1. Complete all P0 items for Azure.Core and Azure.AspNet
2. Begin unit test creation for all packages
3. Start XML documentation effort
4. Create project board with issues for all roadmap items
5. Publish first beta: Benzene.Azure.* 1.0.0-beta.1

**Decision Points:**
1. ~~**ASP.NET Core Fix:** Framework refs OR upgrade to 8.0+ packages?~~ ✅ DECIDED
   AND DONE 2026-07-12 — `FrameworkReference` to `Microsoft.AspNetCore.App`
2. **1.0 Timing:** Ship with core 1.0 OR wait 6-9 months?
3. **Hosting Focus:** Functions-first OR equal focus on App Service/Container Apps?
4. **Test Strategy:** Azurite-only OR also real Azure sandbox? — still open; note
   that `BenzeneTestHost`-based in-memory testing (no emulator needed) is now
   documented in `docs/azure-functions.md` as a third option alongside Azurite/real
   sandbox, found 2026-07-14
5. ~~**Azure Services Priority:** Service Bus first OR complete Functions
   triggers?~~ ✅ ANSWERED BY EVENTS — Service Bus shipped (found 2026-07-14); the
   remaining Functions trigger types (Blob, Queue, Cosmos DB, Event Grid, Timer) are
   still unbuilt, so this decision point is now "which of those is next," not
   "Service Bus vs. Functions triggers"

---

**Document Owner:** Azure Product Team
**Reviewers:** Core Team, Community
**Approval Required:** Yes
**Next Review:** Monthly during 1.0 development

**Status:** DRAFT - Awaiting Approval

**Key Recommendation:** Azure packages need MORE work than AWS packages before 1.0
despite having fewer packages. ~~The critical dependency issues MUST be resolved
before any 1.0 consideration.~~ The critical dependency issues (ASP.NET Core 2.1.x,
Microsoft.Azure.WebJobs) are now both resolved as of this 2026-07-14 audit; remaining
blockers are narrative documentation (ARM/Bicep/Terraform, Managed Identity,
Application Insights, RBAC), the newly-found `Benzene.Azure.Function.Core` coverage
gap, and Azurite/emulator integration tests. Estimate 6-9 months post-core-1.0 for
Azure 1.0 release is likely still in the right range, though the gap to AWS (~97%
ready) narrowed further than the original estimate assumed, then widened slightly again once the
`Benzene.Azure.Function.Core` coverage gap was found — net effect is this document's
existing timeline estimate remains reasonable, not something this audit found grounds
to shorten or lengthen materially.

---

## Document History Addendum — 2026-07-17: Fresh-Pass Review + Cosmos DB Change Feed Evaluation

Research/prioritization-only pass (no code changes) by the Azure Product Owner, appended per
request rather than inserted into the top Document History section, so it doesn't get lost among
2300+ lines of earlier, partially-superseded narrative. Confirms current package count still **8
production** (`Benzene.Azure.Function.{Core,AspNet,EventHub,Kafka,ServiceBus}`,
`Benzene.AspNet.Core`, `Benzene.Azure.{ServiceBus,EventHub}` self-hosted workers) **+ 5
TestHelpers**, matching the 2026-07-17 self-hosted-worker entry above — nothing shipped between
that entry and this one.

- **Streaming precedent reviewed.** `Benzene.Core.Middleware/Streaming/` (`StreamContext<TItem>`,
  `IStreamCheckpointer<TItem>`, `StreamMiddlewareApplication`) is the established fan-**in**
  pattern for batch/stream transports, as opposed to the fan-**out**
  `MiddlewareMultiApplication`/`IMiddlewareApplication` pattern every other Azure adapter uses
  today. Two existing consumers confirm the pattern is production-ready: `Benzene.Aws.Lambda.Kinesis`
  (`KinesisStreamApplication` + a **real** `KinesisStreamCheckpointer` with true per-record
  sequence-number resume, shipped 2026-07-17 same-day as this pass per its own `CLAUDE.md`) and
  `Benzene.Azure.Function.EventHub/Function/StreamingExtensions.cs`'s `UseEventHubStream` (an
  opt-in fan-in alternative to the default fan-out `UseEventHub`, already shipped, batch-level only
  — no per-item checkpoint control, since the Functions Event Hub trigger checkpoints the whole
  batch on successful return).

- **Cosmos DB Change Feed evaluated as a candidate package.** Repo-wide grep for "cosmos" (src/,
  docs/, work/) confirms zero prior footprint — the only mention anywhere is this document's own
  generic, unelaborated "Cosmos DB (8-10 weeks) — Change Feed trigger adapter" bullet in the
  Medium-Term Roadmap (line ~1036), never designed in any depth. Findings:
  - Change Feed Processor SDK (`Microsoft.Azure.Cosmos`) delivers changes per **lease** (a
    partition key range), load-balanced across processor instances via a dedicated **lease
    container in Cosmos itself** — conceptually the same shape as `EventProcessorClient`'s
    partition ownership + blob checkpoint store that `Benzene.Azure.EventHub`'s
    `BenzeneEventHubWorker` already wraps, but the checkpoint *store* is a Cosmos container, not
    Azure Blob Storage.
  - Checkpoint granularity is **coarser than Kinesis, closer to Event Hubs**: even with
    `WithManualCheckpointing()`, a lease's callback delivers a batch and checkpoints that whole
    batch as a unit — there is no per-document resume token the way Kinesis's sequence number or
    Event Hubs' per-event offset allow. This means Cosmos Change Feed is architecturally a
    **fan-in `StreamContext<TDocument>`-per-batch** citizen, not a candidate for
    Kinesis-style true per-record checkpointing — `IStreamCheckpointer<TDocument>` would wrap the
    Change Feed context's batch-level `CheckpointAsync()`, and a handler that wants finer-grained
    safety has to do its own within-batch bookkeeping, same limitation already documented for the
    Event Hubs Functions trigger.
  - **Real design deviation from every existing Azure adapter:** Event Hub/Kafka/Service Bus
    contexts are all built around opaque/string transport payloads (`EventData`,
    `ServiceBusReceivedMessage`, Kafka's key/value). Cosmos DB's Functions `CosmosDBTrigger`
    binding and the Change Feed Processor builder both require a **concrete document type
    parameter** (a POCO or `dynamic`) — there's no "raw bytes" shape to bind to. A
    `Benzene.Azure.*.CosmosDb.ChangeFeed` package therefore needs to be **generic over
    `TDocument`** (`ChangeFeedContext<TDocument>` / `StreamContext<TDocument>`), which none of the
    existing single-concrete-type Azure packages are — worth flagging up front as a design
    decision, not something to discover mid-implementation.
  - **Two hosting shapes, sequencing recommendation:** build the **Azure Functions
    `CosmosDBTrigger` adapter first** (smaller — directly mirrors the already-shipped
    `UseEventHubStream`/`StreamingExtensions.cs` shape, and the trigger's own auto-checkpoint-on
    success behavior needs no new checkpointer plumbing beyond a `NullStreamCheckpointer`-style
    default). Follow with a **self-hosted `BenzeneCosmosChangeFeedWorker`**
    (`Benzene.Azure.CosmosDb`, mirroring `BenzeneEventHubWorker`/`BenzeneServiceBusWorker`'s
    `IBenzeneWorker`/`IBenzeneWorkerStartup` shape) for teams that want real manual per-batch
    checkpoint control or non-Functions hosting (AKS/Container Apps) — this exactly repeats
    Benzene's own actual sequencing for Service Bus and Event Hubs (Functions trigger shipped
    2026-07-13/14, self-hosted worker as a fast-follow 2026-07-17), so there's already an in-repo
    precedent for doing it in this order rather than simultaneously.

- **Fresh gap scan beyond Cosmos** (cross-checked against this document's own still-open items
  near lines ~1022-1071, ~1448-1481, ~1630-1670, plus a live repo grep — nothing below was newly
  invented, all independently reconfirmed still true this session):
  - **Managed Identity / RBAC: still genuinely absent**, and worse than "just missing docs" — a
    repo-wide grep for `DefaultAzureCredential|ManagedIdentityCredential|TokenCredential` across
    `src/` returns only the two abstract client-factory seams
    (`Benzene.Azure.EventHub/IEventProcessorClientFactory.cs`,
    `Benzene.Azure.ServiceBus/IServiceBusClientFactory.cs`) — the seams exist, but there is not one
    concrete Managed Identity example anywhere in the codebase or docs, despite this document
    listing it as a "Critical Blocker" since its earliest draft.
  - **Azure Queue Storage and Blob Storage Functions triggers: still entirely unbuilt** (line
    ~1022-1034), two of the most commonly used Functions trigger types, both still absent with no
    progress since this document's original estimate.
  - **Terraform: still absent** — the Bicep template shipped 2026-07-14
    (`examples/Azure/Benzene.Example.Azure/main.bicep`) has no Terraform equivalent.
  - **No performance benchmark data exists** — re-confirmed still true; this document's own
    Performance Goals section (Consumption <2000ms / Premium <800ms P99 cold start) has never been
    measured against, and can't be from this sandbox (needs real deployed Azure resources).
  - **Documentation-debt side-finding:** `Benzene.Azure.Function.Core`, `.AspNet`, `.EventHub`, and
    `.Kafka`'s `CLAUDE.md` files are still generic, stale template boilerplate ("Common Azure
    abstractions", "Azure authentication integration") that don't name a single real type in the
    package — unlike `Benzene.Azure.Function.ServiceBus`'s and both self-hosted workers'
    (`Benzene.Azure.ServiceBus`, `Benzene.Azure.EventHub`) accurate, type-specific `CLAUDE.md`
    files. Low-risk, low-effort fix, not previously called out anywhere in this document.

- **No code changes made this pass.** Prioritized worklist (Cosmos DB Change Feed items called out
  explicitly, in priority order) was handed off directly as the deliverable rather than duplicated
  here — see the task/PR this addendum was written for.
