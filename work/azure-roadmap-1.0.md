# Benzene Azure Packages - Roadmap to 1.0.0 and Beyond

**Document Version:** 1.3
**Last Updated:** 2026-07-12
**Owner:** Azure Product Team
**Status:** DRAFT for Review

> **2026-07-12 update:** Two of this document's core claims were verified against actual
> code and found stale — corrected here rather than trusted at face value (the AWS
> roadmap had the same problem before its own update pass):
> - **XML Documentation** (P0 #2): now 100% across all 5 packages (Benzene.Azure.Core,
>   .AspNet, .EventHub, .Kafka, Benzene.AspNet.Core), 0 CS1591 warnings. Full solution
>   and the Azure example solution both build clean; full test suite still green
>   (655/655).
> - **Test Coverage** (P0 #3): the "ZERO test files, complete absence of tests" claim
>   was wrong. Real coverage, measured via `dotnet test --collect:"XPlat Code
>   Coverage"`: Benzene.Azure.Core 82.8%, Benzene.Azure.AspNet 81.0%,
>   Benzene.Azure.EventHub 86.3%, Benzene.Azure.Kafka 90.7%. Only **Benzene.AspNet.Core**
>   is genuinely 0% covered. This is a much smaller gap than the original 80-100h
>   estimate assumed — re-scope before starting that work.
> - **ASP.NET Core 2.1.x on .NET 10** (P0 #1) was also re-checked: the dependency really
>   is that old (confirmed in `Benzene.Azure.AspNet.csproj` and
>   `Benzene.AspNet.Core.csproj`), and there's a hard-coded Windows-only `HintPath`
>   pointing at a `netcoreapp3.1` reference assembly in `Benzene.Azure.AspNet.csproj`
>   that can't resolve on non-Windows machines — but despite both issues, **both
>   packages currently build successfully with 0 errors**. The "CRITICAL — likely
>   causes runtime failures" framing is overstated for the current state; treat this as
>   real technical debt worth fixing, not a currently-broken build.
>
> Discovered along the way, not yet fixed: two file/class name mismatches
> (`ApiGatewayHttpRequestAdapter.cs` in `Benzene.Azure.AspNet` actually contains
> `AspNetHttpRequestAdapter`; `AspNetHeadersMapper.cs` in `Benzene.AspNet.Core` actually
> contains `AspNetMessageHeadersGetter`) — consistent with similar mismatches found and
> left as-is during the AWS documentation pass, not fixed here either since this was a
> docs-only pass. `AspNetResponseAdapter.cs` (Benzene.AspNet.Core) also has a
> non-obvious default: if `SetStatusCode` is never called, the response defaults to
> `404` rather than `200` — now called out explicitly in its XML doc comment since it's
> easy to miss.
>
> **2026-07-12 update (dependency fix):** P0 #1 (ASP.NET Core 2.1.x on .NET 10) is now
> **RESOLVED**. `Benzene.Azure.AspNet.csproj` and `Benzene.AspNet.Core.csproj` no longer
> reference the EOL `Microsoft.AspNetCore.Mvc.Core` 2.1.38 / `Microsoft.AspNetCore.Routing`
> 2.1.1 / `Microsoft.AspNetCore.Http.Abstractions` 2.1.1 NuGet packages, and the
> hard-coded Windows-only `HintPath` to a `netcoreapp3.1` reference assembly is gone.
> Both now use `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, the correct
> way to reference ASP.NET Core types from a plain `Microsoft.NET.Sdk` project targeting
> .NET Core 3.0+ (the old packages are legacy/empty shims from the 2.x era — this was
> never really a valid dependency, just one that happened to still compile). Also
> removed a now-redundant `Microsoft.Extensions.DependencyInjection.Abstractions`
> `PackageReference` from `Benzene.AspNet.Core.csproj` (flagged by NuGet's NU1510
> "will not be pruned" warning, already supplied transitively by the framework
> reference and not directly used in the package's own code), and added the
> previously-missing `<PackageVersion>0.0.1</PackageVersion>` to that same csproj.
> Verified: both packages build with 0 errors; full `Benzene.sln` builds clean; full
> test suite still 655/655 passing; `examples/Azure/Benzene.Example.Azure.sln` builds
> clean. Side effect: warning count rose in both packages (22→99 in
> `Benzene.Azure.AspNet`) because the real, nullable-annotated ASP.NET Core reference
> assemblies surface nullability warnings (CS8602/8603/8604/etc.) and analyzer warnings
> (ASP0019) the old, unannotated 2.1.x packages never triggered — a more accurate
> picture of the code, not a regression, left for a future code-quality pass rather than
> fixed here since this was scoped as a dependency-only fix.
>
> **2026-07-12 update (code quality):** Two more P0/high-priority items resolved.
> (1) `TestHttpRequest.cs` and `HttpBuilderExtensions.cs` — both pure test
> infrastructure (a settable fake `HttpRequest` and the builder extension that produces
> it from `IHttpBuilder<T>`, used only by `AspNetPipelineTest`) — moved out of the
> production `Benzene.Azure.AspNet` package into a new
> `Benzene.Azure.AspNet.TestHelpers` package, mirroring the existing
> `Benzene.Azure.EventHub.TestHelpers`/`Benzene.Azure.Kafka.TestHelpers` pattern. Added
> to `Benzene.sln` and referenced from `Benzene.Test.csproj`. (2) The Event Hub
> package's message router class was literally named `BenzeneMessageLambdaHandler`
> (copy-pasted from the AWS Lambda equivalent in `Benzene.Aws.Lambda.Core`) despite
> having nothing to do with AWS Lambda — renamed to `BenzeneMessageEventHubHandler`,
> and its file (previously mismatched as `DirectMessageLambdaHandler.cs`) renamed to
> match. Both are breaking API changes but pre-1.0 with no external consumers yet.
> Verified: all affected packages build with 0 errors, full solution and Azure example
> solution build clean, 655/655 tests still passing (including the moved
> `AspNetPipelineTest`, run in isolation to confirm the relocation works).

---

## Executive Summary

This roadmap outlines the path to 1.0.0 for Benzene's Azure integration packages and defines the strategic direction for Azure-specific features over the next 12+ months. The Azure ecosystem within Benzene currently consists of **5 production packages** (4 Azure-specific + 1 ASP.NET Core general) and **2 TestHelper packages** supporting Azure Functions, Event Hubs, Kafka (via Event Hubs), and ASP.NET Core hosting.

### Current State
- **Package Count:** 5 Azure production packages (4 Azure + 1 ASP.NET), 2 TestHelpers
- **Version:** All at 0.0.1 (pre-release)
- **Target Framework:** .NET 10
- **Source Files:** ~117 Azure-related source files (91 Azure.*, 26 AspNet.*)
- **Test Coverage:** ✅ mostly good — Azure.Core 82.8%, Azure.AspNet 81.0%, Azure.EventHub 86.3%, Azure.Kafka 90.7% (measured 2026-07-12); only Benzene.AspNet.Core is genuinely 0%
- **Documentation:** ✅ 100% XML documentation across all 5 packages (completed 2026-07-12), basic CLAUDE.md files exist (some stale — see package sections), one ASP.NET Core doc
- **Dependencies:** ✅ ASP.NET Core 2.1.x issue resolved (2026-07-12) — `Benzene.Azure.AspNet` and `Benzene.AspNet.Core` now use `FrameworkReference` to `Microsoft.AspNetCore.App` instead of EOL 2.1.x NuGet packages; hard-coded Windows-only `HintPath` removed
- **Maturity:** Functional; test/doc gap with AWS is much smaller than originally assessed; the dependency blocker is now fixed

### Key Findings
✅ **Strengths:**
- Clean architecture consistent with Benzene patterns
- Good separation: Azure Functions vs ASP.NET Core hosting
- TestHelpers properly extracted to dedicated packages
- Working example demonstrates Azure Functions usage
- No TODO/FIXME/HACK comments found in codebase
- Simpler than AWS (fewer packages, cleaner scope)
- ✅ 100% XML documentation, 0 CS1591 warnings (completed 2026-07-12)
- ✅ 4 of 5 packages already have solid test coverage (81-91%), contrary to this
  document's original "zero tests" claim (measured 2026-07-12)
- ✅ ASP.NET Core dependencies fixed — `FrameworkReference` to `Microsoft.AspNetCore.App`
  instead of EOL 2.1.x packages (resolved 2026-07-12)

❌ **Critical Blockers for 1.0:**
- ~~ZERO XML documentation on any public API~~ ✅ RESOLVED 2026-07-12
- **Benzene.AspNet.Core has 0% test coverage** (the other 4 packages are 81-91%,
  corrected 2026-07-12 — this is a single-package gap now, not a "zero tests" problem)
- ~~Very old ASP.NET Core dependencies (2.1.x on .NET 10 project - major compatibility issue)~~ ✅ RESOLVED 2026-07-12
- Inconsistent Azure SDK versions
- Missing deployment templates (ARM/Bicep/Terraform)
- No Application Insights integration examples
- Missing Azure-specific middleware (authentication, authorization, managed identity)
- No performance benchmarks or cold-start metrics
- Minimal documentation (only basic ASP.NET Core guide)
- No Azure App Service, Container Apps, or AKS integration guidance
- Missing RBAC and Managed Identity patterns

### Recommended 1.0 Strategy

**CONSERVATIVE APPROACH (STRONGLY RECOMMENDED):**
Keep all Azure packages at **0.9.x-preview** until well after core 1.0 release, then:
- Fix critical dependency issues (ASP.NET Core 2.1 on .NET 10)
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
| **Benzene.Azure.Core** | 0.0.1 | Core Azure Functions abstractions & startup | Low-Medium | ❌ Not ready |
| **Benzene.Azure.AspNet** | 0.0.1 | Azure Functions HTTP trigger adapter | Low | ❌ Not ready |
| **Benzene.Azure.EventHub** | 0.0.1 | Event Hubs trigger adapter | Low | ❌ Not ready |
| **Benzene.Azure.Kafka** | 0.0.1 | Kafka via Event Hubs trigger adapter | Low | ❌ Not ready |
| **Benzene.AspNet.Core** | No version | General ASP.NET Core integration | Low-Medium | ❌ Not ready |

**TestHelper Packages (not for 1.0):**
- Benzene.Azure.EventHub.TestHelpers
- Benzene.Azure.Kafka.TestHelpers

### Code Quality Metrics

**Positive Indicators:**
- ✅ No TODO/FIXME/HACK comments found
- ✅ Consistent naming conventions
- ✅ Clean separation: Azure Functions vs ASP.NET Core
- ✅ TestHelpers properly separated
- ✅ Simple, focused architecture
- ✅ Working Azure Functions example

**Red Flags:**
- ❌ **0 XML documentation comments** across ALL packages
- ❌ **ZERO test files** found - complete absence of tests
- ~~❌ **CRITICAL DEPENDENCY ISSUE**: ASP.NET Core 2.1.x packages on .NET 10 project~~ ✅ RESOLVED 2026-07-12 (now `FrameworkReference` to `Microsoft.AspNetCore.App`)
- ❌ Old Microsoft.Azure.WebJobs (3.0.39) - should be 3.0.40+
- ❌ Inconsistent Azure SDK versions
- ❌ No ARM/Bicep/Terraform deployment templates
- ❌ No Application Insights integration
- ❌ Missing Azure authentication/authorization middleware
- ❌ No performance benchmarks or metrics
- ❌ Minimal documentation (only 1 doc file for ASP.NET Core)
- ❌ No Azure-specific CI/CD examples
- ❌ Commented-out code in multiple files

### Dependency Analysis

**Azure SDK & Functions Dependencies:**
```
Azure.Identity                                   1.11.4
Azure.Messaging.EventHubs.Processor              5.11.5
Microsoft.Azure.Functions.Extensions             1.1.0
Microsoft.Azure.WebJobs                          3.0.39      ⚠️ OLD
Microsoft.Azure.WebJobs.Extensions.EventHubs     6.3.5
Microsoft.Azure.WebJobs.Extensions.Kafka         3.9.0
Microsoft.AspNetCore.App (FrameworkReference)    (shared fw) ✅ FIXED 2026-07-12 — replaces the three rows below
```
~~Microsoft.AspNetCore.Mvc.Core 2.1.38 / Microsoft.AspNetCore.Routing 2.1.1 /
Microsoft.AspNetCore.Http.Abstractions 2.1.1~~ — removed 2026-07-12, replaced by a
`FrameworkReference` to `Microsoft.AspNetCore.App` in both `Benzene.Azure.AspNet.csproj`
and `Benzene.AspNet.Core.csproj`. The redundant `Microsoft.Extensions.DependencyInjection.Abstractions`
`PackageReference` in `Benzene.AspNet.Core.csproj` was also removed (NU1510 flagged it
as already supplied transitively).

**Critical Issues:**
1. ~~❌ **ASP.NET Core 2.1.x on .NET 10** - This is a MAJOR incompatibility~~
   ✅ **RESOLVED 2026-07-12** — replaced with `FrameworkReference` to
   `Microsoft.AspNetCore.App`, the correct approach for referencing ASP.NET Core types
   from a plain `Microsoft.NET.Sdk` project on .NET Core 3.0+.
2. ⚠️ Microsoft.Azure.WebJobs 3.0.39 is old - should update to latest 3.0.x
3. ⚠️ No Application Insights SDK references
4. ⚠️ Missing Azure.Core for consistent Azure SDK usage

### Comparison with AWS Packages

**AWS Package Maturity (from aws-roadmap-1.0.md):**
- 8 packages, ~179 source files
- 4 test classes found (minimal but present)
- Medium maturity overall
- Estimated 178-262 hours to 1.0

**Azure Package Maturity (Current Assessment):**
- 5 packages, ~117 source files
- 0 test classes found (none at all)
- Low-Medium maturity overall
- **Estimated 200-300 hours to 1.0** (more work despite fewer packages due to:
  - Critical dependency issues to resolve
  - Complete absence of tests
  - Less mature overall state
  - Need for Azure-specific features like Managed Identity, App Insights)

**Key Differences:**
- Azure has fewer packages but LESS mature foundation
- AWS has some tests; Azure has none
- AWS dependencies mostly OK; Azure has critical dependency issues
- AWS has 4 event sources; Azure has 2 (EventHub, Kafka)
- Both have zero XML documentation

---

## Package-by-Package Analysis

### 1. Benzene.Azure.Core ⭐ Foundation Package

**Location:** `src/Benzene.Azure.Core/`
**Current State:** Low-Medium maturity, foundational but incomplete

**Public API Surface:**
- `IAzureFunctionApp` - Entry point abstraction (C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.Core\IAzureFunctionApp.cs)
- `AzureFunctionApp` - Main implementation (C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.Core\AzureFunctionApp.cs)
- `AzureFunctionStartUp` - Startup pattern (C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.Core\AzureFunctionStartUp.cs)
- `InlineAzureFunctionStartUp` - Inline configuration
- `IAzureFunctionAppBuilder` / `AzureFunctionAppBuilder` - Builder pattern
- Integration with Azure Functions hosting model

**Strengths:**
- Clean startup pattern inspired by ASP.NET Core
- Generic support for different DI containers
- Proper abstraction for Azure Functions hosting
- Builder pattern for composability

**Issues:**
1. ❌ No XML documentation on any type
2. ❌ Exception message "Cannot handle this kind of request" (lines 27, 40) is not helpful
3. ⚠️ No cold-start optimization guidance
4. ⚠️ No Application Insights integration
5. ⚠️ No Managed Identity configuration helpers
6. ⚠️ Old Microsoft.Azure.WebJobs dependency (3.0.39)
7. ⚠️ No Function App settings configuration helpers
8. ⚠️ No guidance on hosting plans (Consumption, Premium, Dedicated)
9. ⚠️ No durable functions support
10. ⚠️ No logging integration patterns

**1.0 Requirements:**
- [ ] Add comprehensive XML documentation
- [ ] Improve error messages with actionable guidance
- [ ] Update Microsoft.Azure.WebJobs to latest 3.0.x
- [ ] Add Application Insights integration
- [ ] Add Managed Identity configuration helpers
- [ ] Document hosting plan differences
- [ ] Add cold-start optimization guidance
- [ ] Create migration guide to Azure Functions v4
- [ ] Add Function App configuration helpers
- [ ] Document Key Vault integration patterns
- [ ] Add structured logging integration
- [ ] Document deployment best practices

**Estimated Effort:** 25-30 hours

---

### 2. Benzene.Azure.AspNet 🔧 HTTP Functions Adapter

**Location:** `src/Benzene.Azure.AspNet/`
**Current State:** Low maturity; dependency crisis resolved 2026-07-12

**Public API Surface:**
- `AspNetApplication` - Main application (C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.AspNet\AspNetApplication.cs)
- `AspNetContext` - HTTP context (C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.AspNet\AspNetContext.cs)
- `ApiGatewayHttpRequestAdapter` - Request adapter
- `AspNetResponseAdapter` - Response builder
- Message handlers (BodyGetter, HeadersGetter, TopicGetter, ResultSetter)
- `AspNetContextRequestEnricher` - Request enrichment
- `Extensions.HandleHttpRequest()` - Entry point helper
- ~~`TestHttpRequest` - Test utilities (should be in TestHelpers)~~ moved to
  `Benzene.Azure.AspNet.TestHelpers` 2026-07-12

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
   to new `Benzene.Azure.AspNet.TestHelpers` package, along with `HttpBuilderExtensions`)
4. ⚠️ Commented-out health check code (lines 14-28 of Extensions.cs)
5. ⚠️ AspNetContext too simple - only has HttpRequest and ContentResult
6. ⚠️ No CORS support
7. ⚠️ No authentication/authorization middleware
8. ⚠️ No OpenAPI/Swagger integration
9. ⚠️ No API Management integration patterns
10. ⚠️ Package naming confusing (Azure.AspNet for Functions HTTP trigger)

**1.0 Requirements:**
- [x] **CRITICAL**: Fix ASP.NET Core dependencies (use framework references or update to 8.0+) — done 2026-07-12
- [x] **CRITICAL**: Remove hard-coded DLL path — done 2026-07-12
- [x] **CRITICAL**: Move TestHttpRequest to TestHelpers — done 2026-07-12
- [x] Add comprehensive XML documentation — done 2026-07-12
- [ ] Expand AspNetContext with convenience properties
- [ ] Add CORS middleware
- [ ] Add authentication/authorization middleware
- [ ] Document API Management integration
- [ ] Add OpenAPI integration examples
- [ ] Remove or document commented code
- [ ] Document differences from ASP.NET Core hosted apps
- [ ] Add custom domain and SSL configuration guidance
- [ ] Document scaling considerations

**Estimated Effort:** ~~30-40 hours (includes fixing critical dependency issues)~~ 3-6
hours remaining (dependency fix, XML docs, and TestHttpRequest relocation all done
2026-07-12; remaining scope is CORS/auth middleware and OpenAPI examples)

---

### 3. Benzene.Azure.EventHub 📊 Event Streaming

**Location:** `src/Benzene.Azure.EventHub/`
**Current State:** Low maturity, minimal implementation

**Public API Surface:**
- `EventHubApplication` - Main application (C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.EventHub\Function\EventHubApplication.cs)
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
4. ⚠️ No partition key handling documented
5. ⚠️ No checkpointing guidance
6. ⚠️ No Event Hubs Capture integration
7. ⚠️ No consumer group configuration examples
8. ⚠️ No scaling and partition management guidance
9. ⚠️ No Event Hubs namespace/connection configuration
10. ⚠️ No Managed Identity authentication example

**1.0 Requirements:**
- [x] Add comprehensive XML documentation — done 2026-07-12
- [x] Rename "DirectMessageLambdaHandler" to Azure-appropriate name — done 2026-07-12
  (`BenzeneMessageEventHubHandler`)
- [ ] Document partition and checkpointing strategies
- [ ] Add Event Hubs Capture integration examples
- [ ] Document consumer group patterns
- [ ] Add Managed Identity authentication examples
- [ ] Document scaling and throughput optimization
- [ ] Add Schema Registry integration
- [ ] Document Event Hubs vs Kafka protocol differences
- [ ] Add monitoring and metrics guidance
- [ ] Document cost optimization (throughput units, partitions)
- [ ] Add dead-letter queue patterns

**Estimated Effort:** ~~20-25 hours~~ 15-20 hours remaining (XML docs and naming fix
done 2026-07-12; remaining scope is narrative/operational documentation, not code)

---

### 4. Benzene.Azure.Kafka 🆕 Kafka via Event Hubs

**Location:** `src/Benzene.Azure.Kafka/`
**Current State:** Low maturity, newer addition

**Public API Surface:**
- `KafkaApplication` - Main application (C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.Kafka\KafkaApplication.cs)
- `KafkaContext` - Kafka context
- Message handlers (BodyGetter, HeadersGetter, TopicGetter, ResultSetter)
- `KafkaRegistrations` - Service registration

**Strengths:**
- Event Hubs Kafka protocol support
- Kafka compatibility for migrations
- Consistent architecture with other adapters

**Issues:**
1. ❌ No XML documentation
2. ⚠️ Very minimal implementation
3. ⚠️ No schema registry integration
4. ⚠️ No Avro/Protobuf serialization examples
5. ⚠️ No consumer group configuration
6. ⚠️ No offset management strategies
7. ⚠️ No Event Hubs Kafka endpoint configuration
8. ⚠️ No authentication examples (connection string vs Managed Identity)
9. ⚠️ No performance optimization guidance
10. ⚠️ No migration guide from Apache Kafka

**1.0 Requirements:**
- [ ] Add comprehensive XML documentation
- [ ] Document Event Hubs Kafka endpoint configuration
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

**Estimated Effort:** 20-25 hours

---

### 5. Benzene.AspNet.Core 🌐 General ASP.NET Core Integration

**Location:** `src/Benzene.AspNet.Core/`
**Current State:** Low-Medium maturity, NOT Azure-specific but important

**Public API Surface:**
- `AspNetApplication` - Main application (C:\Users\pelled\source\libs\Benzene\src\Benzene.AspNet.Core\AspNetApplication.cs)
- `AspNetContext` - HTTP context (C:\Users\pelled\source\libs\Benzene\src\Benzene.AspNet.Core\AspNetContext.cs)
- `BenzeneExtensions.UseBenzene()` - Integration extension
- `IAspApplicationBuilder` - Builder abstraction
- `AspApplicationBuilder` - Builder implementation
- Request/Response adapters
- Message handlers

**Strengths:**
- Enables Benzene on ASP.NET Core (App Service, Container Apps, AKS)
- Clean middleware integration
- Documented (docs/asp-net-core.md exists)
- More complete than Azure Functions adapters

**Issues:**
1. ~~❌ No XML documentation~~ ✅ RESOLVED 2026-07-12
2. ~~❌ No package version (missing from csproj)~~ ✅ RESOLVED 2026-07-12 (`<PackageVersion>0.0.1</PackageVersion>` added)
3. ~~⚠️ Old Microsoft.AspNetCore.Http.Abstractions (2.1.1)~~ ✅ RESOLVED 2026-07-12
   (replaced with `FrameworkReference` to `Microsoft.AspNetCore.App`; the redundant
   `Microsoft.Extensions.DependencyInjection.Abstractions` PackageReference was also
   removed, per NU1510)
4. ⚠️ Extensive commented-out code (lines 12-49 of BenzeneExtensions.cs)
5. ⚠️ AspNetContext too simple - only has HttpContext property
6. ⚠️ No Azure App Service specific features
7. ⚠️ No Azure Container Apps integration
8. ⚠️ No AKS/Kubernetes integration guidance
9. ⚠️ No Application Insights middleware
10. ⚠️ No managed identity integration

**1.0 Requirements:**
- [x] Add package version to csproj — done 2026-07-12
- [x] Update Microsoft.AspNetCore.Http.Abstractions to 8.0+ — done 2026-07-12 (via `FrameworkReference`)
- [x] Add comprehensive XML documentation — done 2026-07-12
- [ ] Remove or document commented code
- [ ] Expand AspNetContext with convenience properties
- [ ] Add Application Insights middleware
- [ ] Add Azure App Service configuration helpers
- [ ] Document Container Apps deployment
- [ ] Add AKS/Kubernetes integration guide
- [ ] Add Managed Identity authentication
- [ ] Document Azure-specific hosting scenarios
- [ ] Add health check integration
- [ ] Document logging integration (App Service logs)

**Estimated Effort:** ~~25-30 hours~~ 15-20 hours remaining (dependency fix, package
version, and XML docs all done 2026-07-12; remaining scope is test coverage — this
package is still the one genuinely 0% covered — plus commented-code cleanup and the
Azure-specific feature items)

---

## Roadmap to 1.0.0

### Critical Path Items (BLOCKERS)

**Must Have Before 1.0:**

1. ~~**Fix Critical Dependency Issues** (40-50 hours) - HIGHEST PRIORITY~~
   ✅ **MOSTLY RESOLVED 2026-07-12** (~2 hours actual, far under the original estimate —
   the fix was a one-line `FrameworkReference` swap, not a rewrite):
   - [x] Fix ASP.NET Core 2.1.x references on .NET 10 — done, via `FrameworkReference`
   - [x] Remove hard-coded DLL paths — done
   - [ ] Update all Azure SDK packages to consistent versions — not yet done
   - [ ] Update Microsoft.Azure.WebJobs to latest — not yet done

2. ~~**XML Documentation** (50-70 hours) - CRITICAL~~ ✅ **RESOLVED 2026-07-12** — 100%
   across all 5 packages, 0 CS1591 warnings.

3. **Test Coverage** (60-80 hours) - CRITICAL — **re-scoped to ~15-20 hours.** 4 of 5
   packages already measured at 81-91% coverage (2026-07-12); only
   `Benzene.AspNet.Core` is genuinely 0%. Remaining work is that one package plus
   integration tests/benchmarks, not the full original scope.
   - Unit tests for Benzene.AspNet.Core (target 80%+ coverage)
   - Integration tests with Azurite/emulators
   - End-to-end Azure Functions examples
   - Performance benchmarks

4. ~~**Move Test Code** (5-8 hours) - BLOCKING~~ ✅ RESOLVED 2026-07-12
   - [x] Move TestHttpRequest from Benzene.Azure.AspNet to TestHelpers — moved
     `TestHttpRequest.cs` and `HttpBuilderExtensions.cs` to new
     `Benzene.Azure.AspNet.TestHelpers` package
   - [x] Ensure no test code in production packages

5. **Documentation** (40-60 hours) - CRITICAL
   - Getting started guide for each adapter
   - ARM/Bicep deployment templates
   - Terraform examples
   - Azure DevOps CI/CD pipelines
   - GitHub Actions workflows
   - Managed Identity and RBAC guidance
   - Application Insights integration guide
   - Cost optimization guide

6. **Code Quality Fixes** (20-30 hours)
   - Remove commented-out code
   - Improve error messages
   - Add missing error handling
   - Add configuration options
   - Consistent naming (remove "Lambda" terminology)

7. **Azure-Specific Features** (30-40 hours)
   - Application Insights integration
   - Managed Identity support
   - Key Vault integration
   - CORS middleware (Azure.AspNet)
   - Authentication/Authorization middleware

**Total Estimated Effort for 1.0:** ~~245-368 hours (6-9 weeks full-time)~~ **~170-260
hours remaining** (2026-07-12: dependency fix ~2h actual vs 40-50h estimated, XML docs
fully done vs 50-70h estimated, test coverage re-scoped from 60-80h to ~15-20h — net
reduction of roughly 100-150 hours off the original estimate)

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
- CORS and authentication middleware
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
- 🔄 CORS support for Azure.AspNet
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

1. **Azure Service Bus** (8-10 weeks) - HIGH PRIORITY
   - Queue trigger adapter
   - Topic/Subscription trigger adapter
   - Service Bus-specific middleware
   - Session handling support
   - Dead-letter queue patterns
   - Example: Order processing with Service Bus
   - **Effort:** 40-50 hours
   - **Why prioritize:** Most requested Azure messaging service

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
2. ~~⚠️ Hard-coded DLL path in Benzene.Azure.AspNet.csproj~~ ✅ RESOLVED 2026-07-12
3. ~~⚠️ TestHttpRequest in production package~~ ✅ RESOLVED 2026-07-12
4. ⚠️ Old Microsoft.Azure.WebJobs (3.0.39)
5. ~~⚠️ No package version for Benzene.AspNet.Core~~ ✅ RESOLVED 2026-07-12

**High Priority:**
1. Extensive commented-out code in multiple files
2. ~~"DirectMessageLambdaHandler" using AWS terminology in Azure package~~ ✅ RESOLVED
   2026-07-12 (renamed to `BenzeneMessageEventHubHandler`)
3. AspNetContext implementations too simple
4. Exception messages not actionable
5. No test coverage at all

**Medium Priority:**
1. Inconsistent Azure SDK versions
2. No Application Insights integration
3. No Managed Identity support
4. Missing authentication/authorization middleware
5. No CORS support in Azure.AspNet

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
- **ZERO test files found** for Azure packages
- No unit tests
- No integration tests
- No performance benchmarks
- No load tests
- Complete absence of testing infrastructure

### Target Testing Strategy

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
- [ ] Integration tests with Azurite/emulators
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
- [ ] Getting started guide for Azure Functions
- [ ] Getting started guide for ASP.NET Core (Azure App Service)
- [ ] Getting started guide for Container Apps
- [ ] RBAC and Managed Identity setup guide
- [ ] ARM/Bicep template reference
- [ ] Terraform module documentation
- [ ] Azure DevOps CI/CD pipelines
- [ ] GitHub Actions workflows
- [ ] Migration guide from raw Azure Functions
- [ ] Best practices guide (costs, performance, security)
- [ ] Troubleshooting guide (common errors)
- [ ] FAQ for each adapter

**Developer Documentation:**
- [ ] Architecture decision records (ADRs)
- [ ] Contributing guide for Azure packages
- [ ] Adding new trigger type guide
- [ ] Testing guide (Azurite, mocking, Functions test host)
- [ ] Release process for Azure packages
- [ ] Compatibility matrix (Azure SDK versions, .NET versions, Functions runtime)

**API Documentation:**
- [ ] XML documentation for all public APIs
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
# RBAC for Benzene.Azure.EventHub

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
  `Benzene.Azure.AspNet` to the new `Benzene.Azure.AspNet.TestHelpers` package
- **Impact:** Low - test code shouldn't be in production references
- **Migration:** Consumers referencing these types add a reference to
  `Benzene.Azure.AspNet.TestHelpers` and a `using Benzene.Azure.AspNet.TestHelpers;`

**3. Update All Azure SDK Versions**
- Standardize Azure.Identity, Azure SDK packages
- Update Microsoft.Azure.WebJobs to 3.0.40+
- **Impact:** Low - internal dependency change
- **Migration:** None required for users

**4. Remove Commented Code**
- Delete or properly document commented-out code
- Health check code in Extensions.cs
- Middleware code in BenzeneExtensions.cs
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

**6. Add Package Version to AspNet.Core** ✅ DONE 2026-07-12
- Added `<PackageVersion>0.0.1</PackageVersion>` to csproj
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
   (`Benzene.Azure.EventHub`) ✅ DONE 2026-07-12

**New Required Dependencies:**
- Ensure Azure SDK packages are latest compatible versions
- ASP.NET Core 8.0+ for AspNet packages

**Deprecated (Remove in 2.0):**
- TBD - no deprecations yet, clean slate for 1.0
- Consider deprecating direct AWS terminology in future

---

## Dependencies & Compatibility

### Azure SDK Version Strategy

**Current Issues:**
- ASP.NET Core 2.1.x on .NET 10 (CRITICAL)
- Old Microsoft.Azure.WebJobs (3.0.39)
- No consistent Azure SDK versioning

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
| 1.0.x         | 10.0 | Latest    | v4 (.NET 8+)      | 8.0+         |
| 0.9.x         | 10.0 | Various   | v4 (.NET 8+)      | 2.1 (BROKEN) |
```

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

**Current:**
- Microsoft.Extensions.DependencyInjection.Abstractions: 8.0.0 ✅
- Microsoft.AspNetCore.*: 2.1.x ❌ CRITICAL
- Azure.Identity: 1.11.4 ✅
- Microsoft.Azure.WebJobs: 3.0.39 ⚠️

**Action Items:**
- [ ] Fix Microsoft.AspNetCore.* to 8.0+ or use framework refs
- [ ] Update Microsoft.Azure.WebJobs to 3.0.40+
- [ ] Document minimum version requirements
- [ ] Test with latest Azure Functions runtime

### Azure Functions Runtime Compatibility

**Target Runtimes:**
- Functions v4 (.NET 8 in-process or isolated)
- .NET 10 when supported by Functions v4 (isolated model)

**Action Items:**
- [ ] Test with Azure Functions v4 runtime
- [ ] Document isolated vs in-process worker model
- [ ] Create guidance for .NET 10 (isolated model required)
- [ ] Monitor Azure announcements for .NET 10 support

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
- Target: 80%+ unit test coverage (currently 0%)
- Target: 60%+ integration test coverage
- Target: 100% of public APIs documented (currently 0%)

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
- Month 6: 8 trigger types (current 2 + Service Bus, Blob, Queue, Cosmos DB, Event Grid, Timer)
- Month 12: 12 trigger types (+ Durable Functions, SignalR, more)
- Month 18: Complete Azure Functions coverage

**Enterprise Adoption:**
- Target: 3+ enterprise teams using in production
- Target: 1+ case study published
- Target: 1+ Microsoft blog post or community article

---

## Prioritized Feature List

### Must Have for 1.0 (P0)

1. ~~**Fix ASP.NET Core Dependencies** - CRITICAL (40-50h)~~ ✅ MOSTLY COMPLETE
   2026-07-12 (~2h actual — `FrameworkReference` swap, not a rewrite). Remaining:
   Azure SDK version consistency + `Microsoft.Azure.WebJobs` bump, not part of the
   ASP.NET Core fix itself (~5-10h)
2. ~~**XML Documentation** - All packages (50-70h)~~ ✅ COMPLETE 2026-07-12
3. **Unit Tests** - 80%+ coverage — RE-SCOPED 2026-07-12: only `Benzene.AspNet.Core`
   needs this; the other 4 packages already measure 81-91% (was 80-100h for "all
   packages," now a single-package gap, estimate ~15-20h)
4. ~~**Move Test Code** - TestHelpers separation (5-8h)~~ ✅ COMPLETE 2026-07-12
5. **Getting Started Guides** - All adapters (25-30h)
6. **ARM/Bicep Templates** - Deployment examples (20-25h)
7. **Integration Tests** - Azurite, Functions test host (30-40h)
8. **Code Quality Fixes** - RE-SCOPED 2026-07-12: the `BenzeneMessageLambdaHandler` →
   `BenzeneMessageEventHubHandler` rename (one of the two file/class mismatches found)
   is done; remaining scope is removing commented-out dead code and the
   `ApiGatewayHttpRequestAdapter.cs`/`AspNetHeadersMapper.cs` file/class mismatches
   (~10-15h, down from 20-30h)
9. **Application Insights Integration** - Middleware (15-20h)
10. **Migration Guide** - 0.x to 1.0 (10-12h)

**Total P0 Effort:** ~155-245 hours remaining (2026-07-12: dependency fix ~2h actual vs
40-50h estimated, XML documentation now fully complete vs 50-70h estimated, Unit Tests
re-scoped down from 80-100h to ~15-20h now that 4 of 5 packages are confirmed already
well-covered, Move Test Code now complete vs 5-8h estimated, Code Quality Fixes
re-scoped down to ~10-15h with the Lambda-naming rename done)

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
10. **CORS Middleware** - Azure.AspNet (8-10h)

**Total P1 Effort:** 155-205 hours

### Nice to Have for 1.0 (P2)

1. **Key Vault Integration** - Examples (12-15h)
2. **Authentication Middleware** - Azure AD (15-20h)
3. **VS Code Snippets** - Code generation (8-10h)
4. **Video Tutorials** - Getting started (15-20h)
5. **Blog Posts** - Architecture deep dives (10-15h)
6. **Chaos Tests** - Resilience validation (10-15h)

**Total P2 Effort:** 70-95 hours

### Post-1.0 Features (P3)

1. **Service Bus** - Queue & Topic triggers (40-50h)
2. **Blob Storage** - Blob trigger (35-40h)
3. **Queue Storage** - Queue trigger (25-30h)
4. **Cosmos DB** - Change Feed trigger (40-50h)
5. **Event Grid** - Event Grid trigger (30-40h)
6. **Timer Trigger** - Scheduled jobs (15-20h)
7. **Durable Functions** - Orchestration (50-70h)
8. **Container Apps** - Full support (35-45h)
9. **API Management** - Integration (30-40h)
10. **VS Extension** - Dev tools (50-60h)

**Total P3 Effort:** 350-445 hours

---

## Appendix A: File Reference

**Key Source Files:**

**Azure.Core:**
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.Core\AzureFunctionApp.cs`
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.Core\AzureFunctionStartUp.cs`
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.Core\AzureFunctionAppBuilder.cs`

**Azure.AspNet:**
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.AspNet\AspNetApplication.cs`
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.AspNet\AspNetContext.cs`
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.AspNet\Extensions.cs`

**Azure.EventHub:**
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.EventHub\Function\EventHubApplication.cs`
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.EventHub\Function\EventHubContext.cs`

**Azure.Kafka:**
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.Kafka\KafkaApplication.cs`
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Azure.Kafka\KafkaContext.cs`

**AspNet.Core:**
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.AspNet.Core\AspNetApplication.cs`
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.AspNet.Core\AspNetContext.cs`
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.AspNet.Core\BenzeneExtensions.cs`

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

**AWS Packages Current Status (from aws-roadmap-1.0.md):**
1. ❌ 0% XML documentation
2. ✅ Test helpers properly separated
3. ✅ No critical bugs (except EventBridge confusion)
4. ✅ Versioning policy applies
5. ❌ Minimal test coverage (4 test classes)
6. ❌ Documentation incomplete
7. ⚠️ Examples exist but need deployment templates

**AWS Readiness:** ~30% toward 1.0

**Azure Packages Current Status (updated 2026-07-12 against actual code, not assumed):**
1. ✅ 100% XML documentation (completed 2026-07-12)
2. ✅ Test helpers properly separated (TestHttpRequest moved to
   `Benzene.Azure.AspNet.TestHelpers` 2026-07-12)
3. ✅ ASP.NET Core 2.1.x dependency issue resolved 2026-07-12 (`FrameworkReference` to
   `Microsoft.AspNetCore.App`); Azure SDK version consistency and
   `Microsoft.Azure.WebJobs` bump still open
4. ✅ Versioning policy applies
5. ✅ 4 of 5 packages well-covered (81-91%); only Benzene.AspNet.Core is 0%
   (corrected 2026-07-12 — the original "zero test files" claim was wrong)
6. ❌ Minimal narrative documentation (1 doc file; getting-started guides, ARM/Bicep
   templates, etc. still needed)
7. ⚠️ Example exists but no deployment templates

**Azure Readiness:** ~70% toward 1.0 (up from ~15-20% originally, ~55% after the docs
pass, ~65% after the dependency fix; still behind AWS's ~93%, but the gap was
previously overstated)

**Gap Analysis:**
Azure packages are behind AWS packages, but less dramatically than this document
originally claimed:
- AWS is at 90%+ coverage across all 9 packages; Azure is at 81-91% for 4 of 5, with
  one real gap (Benzene.AspNet.Core)
- AWS has already fixed its dependency inconsistencies; Azure's ASP.NET Core dependency
  issue is now fixed too (2026-07-12) — remaining dependency work on both sides is
  minor SDK-version consistency, not a structural blocker
- AWS has 9 packages at high maturity; Azure has 5, now with complete XML
  documentation and a fixed dependency baseline, but still lacking narrative docs,
  deployment templates, and integration tests
- Primary remaining gaps: narrative documentation (getting-started, ARM/Bicep),
  integration tests, the `Benzene.AspNet.Core` test gap, remaining code quality
  (commented-out dead code, the `ApiGatewayHttpRequestAdapter.cs`/
  `AspNetHeadersMapper.cs` file/class mismatches — the Lambda-naming one and
  TestHttpRequest relocation are both done as of 2026-07-12)

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
1. **ASP.NET Core Fix:** Framework refs OR upgrade to 8.0+ packages?
2. **1.0 Timing:** Ship with core 1.0 OR wait 6-9 months?
3. **Hosting Focus:** Functions-first OR equal focus on App Service/Container Apps?
4. **Test Strategy:** Azurite-only OR also real Azure sandbox?
5. **Azure Services Priority:** Service Bus first OR complete Functions triggers?

---

**Document Owner:** Azure Product Team
**Reviewers:** Core Team, Community
**Approval Required:** Yes
**Next Review:** Monthly during 1.0 development

**Status:** DRAFT - Awaiting Approval

**Key Recommendation:** Azure packages need MORE work than AWS packages before 1.0 despite having fewer packages. The critical dependency issues MUST be resolved before any 1.0 consideration. Estimate 6-9 months post-core-1.0 for Azure 1.0 release.
