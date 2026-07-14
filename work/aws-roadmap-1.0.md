# Benzene AWS Packages - Roadmap to 1.0.0 and Beyond

**Document Version:** 1.9
**Last Updated:** 2026-07-14
**Owner:** AWS Product Team
**Status:** DRAFT for Review

> **2026-07-14 changelog** — CORS parity + full audit pass, in order of significance:
> 1. **CORS was stale, not missing.** Every mention below of `ApiGatewayContextCorsMiddleware`
>    (in `src/Benzene.Aws.Lambda.ApiGateway/Cors/`) as the current CORS implementation was
>    wrong — that class and its `Extensions.cs` were deleted (the `Cors/` directory no
>    longer exists in that package). CORS is provided by the portable, generic
>    `Benzene.Http.Cors.CorsMiddleware<TContext>` (works identically across AWS API
>    Gateway, Azure Functions, and ASP.NET Core), registered via
>    `.UseCors(new CorsSettings {...})`. This session brought it to full parity with
>    `Microsoft.AspNetCore.Cors`: exact scheme+host+port origin matching, `Access-Control-
>    Expose-Headers` (new `CorsSettings.ExposedHeaders`), `Access-Control-Max-Age`
>    (`MaxAgeSeconds`), `Access-Control-Allow-Credentials` (`AllowCredentials`), a working
>    `AllowAnyHeader()`-equivalent wildcard for `AllowedHeaders`, `Vary: Origin`, and
>    preflight header validation (a requested header outside a non-wildcard allow-list now
>    fails the check). Fully documented in `docs/common-middleware.md`'s `UseCors` section.
>    Every "CORS not documented" / "Document CORS setup" item below is now resolved.
> 2. **Version corrected**: repo-root `version.txt` (the single centralized version source
>    per `Directory.Build.props`) reads `0.0.2`, not `0.0.1` as this document claimed
>    throughout.
> 3. **Source file count recounted**: 131 AWS-related `.cs` files across all 8 production +
>    5 TestHelper packages (`src/Benzene.Aws.*`, `src/Benzene.Clients.Aws`) — the previous
>    "~179" figure could not be reproduced and appears stale. (`src/Benzene.Aws.XRay/` still
>    exists as a directory but contains only stale `obj`/`bin` build artifacts, no source or
>    project file — consistent with the 2026-07-13 deletion claim below.)
> 4. **Unified hosting model found, previously undocumented here**: `AwsLambdaHost<TStartUp>`
>    plus a platform-neutral `BenzeneStartUp`/`IBenzeneApplicationBuilder` (shared across
>    AWS/Azure/ASP.NET Core, one `StartUp` class runs on any of them) already exists in
>    `Benzene.Aws.Lambda.Core` — tested
>    (`test/Benzene.Core.Test/Hosting/UnifiedStartUpTest.cs`), documented (`docs/hosting.md`,
>    `docs/getting-started-aws.md`, `docs/migration-alpha-to-1.0.md`), and additive (the
>    pre-1.0 `AwsLambdaStartUp` path still works unchanged). This resolves section 1's
>    "Create migration guide from bare-metal to StartUp pattern" item —
>    `docs/migration-alpha-to-1.0.md`'s "Unified hosting" section covers exactly that,
>    including a per-platform host-entry-point table.
> 5. **New cookbook found**: `docs/cookbooks/lambda-cold-start-optimization.md` — resolves
>    section 1's "Document cold-start best practices" item (source-generated handler
>    registration via `Benzene.CodeGen.SourceGenerators`, arm64/memory tuning, ReadyToRun,
>    lazy initialization, provisioned concurrency guidance). Actual benchmarks and Native AOT
>    package support remain unbuilt — see Performance & Optimization section, unchanged.
> 6. **Checked and confirmed NOT resolved** (flagging so this isn't overclaimed):
>    `docs/cookbooks/README.md` links to `api-gateway-authorizers.md` and
>    `s3-event-processing.md`, but neither file exists in `docs/cookbooks/`. The "Document
>    custom authorizer patterns" / IAM-policy-for-authorizers / S3-image-pipeline-example
>    items below remain genuinely open despite the dangling README reference suggesting
>    otherwise.
>
> Everything else in the 2026-07-12/2026-07-13 changelogs below remains accurate and
> unchanged by this pass.

> **2026-07-13 changelog** — audit pass ticking off work completed since the
> 2026-07-12 update, in order of significance:
> 1. **`Benzene.Aws.XRay` deleted** (Checkpoint B, commit `1081bd1`) — the whole
>    package (section 8 below) no longer exists. X-Ray tracing isn't gone, it's
>    superseded: every pipeline already gets a real `System.Diagnostics.Activity`
>    span per middleware via `AddDiagnostics()`, and `Benzene.OpenTelemetry`'s
>    `AddBenzeneInstrumentation()` exports those spans to X-Ray (or any other
>    backend) through a standard OTel exporter — one mechanism instead of a
>    dedicated per-vendor package. **AWS production package count is now 8, not
>    9** — every "current state" reference to "9 packages" in this document has
>    been corrected to 8; the 2026-07-12 changelog's own "9 packages" narrative
>    is left as-is, since it accurately describes what was true at the time that
>    work was done (XRay wasn't deleted until the next day). Section 8 below is
>    kept as a historical record, marked deleted, rather than removed outright.
> 2. **SQS/SNS client header forwarding fixed** (commit `6e88ad6`) —
>    `SqsContextConverter`/`SnsContextConverter` in `Benzene.Clients.Aws` now
>    copy `IBenzeneClientRequest.Headers` onto `MessageAttributes` (previously
>    only the `topic` attribute was set; headers, including correlation IDs and
>    W3C trace context, were silently dropped on the wire). This resolves the
>    SNS section's "Document message attributes vs. headers mapping" item —
>    it's now real, working behavior, documented in `docs/clients.md`.
>    (The raw-`InvokeRequest` Lambda client path has no header concept to
>    forward into and is documented as such, not a gap.)
> 3. **New cookbooks resolve several open documentation items** (commit
>    `51cd0b4`, "Write comprehensive documentation across the docs/ tree"):
>    `docs/cookbooks/handling-sqs-failures.md` (retry + DLQ patterns — resolves
>    two open SQS-section items), `docs/cookbooks/sns-fan-out.md` (fan-out
>    architecture example — resolves an open SNS-section item, and documents
>    the header-mapping fix above), `docs/cookbooks/testing-lambda-functions.md`
>    (resolves the open "Testing guide (LocalStack, mocking)" documentation
>    item), and a new `docs/clients.md` reference doc for `Benzene.Clients.Aws`
>    (resolves part of that package's "clarify purpose" / "usage examples"
>    items). `docs/getting-started-aws.md` also gained a "Test locally with
>    `BenzeneTestHost`" section (commit `fc4cda2`'s new
>    `BuildAwsLambdaHost()` bridge).
> 4. **Logging stack unified** (commits `3f3b25d`, `eee1aa5`) — the old
>    `IBenzeneLogger` abstraction was replaced with `Microsoft.Extensions.Logging`
>    across every package, AWS included. Resolves the "Standardized logging
>    approach" item under Code Quality Improvements.
> 5. **Found and fixed one real gap while auditing "AWSSDK.SQS aligned to
>    3.7.502.57 across all packages"**: `Benzene.Aws.Sqs.TestHelpers` was
>    missed in the 2026-07-12 pass and was still on `3.7.100.74`. Bumped to
>    `3.7.502.57`; verified via `dotnet build`. The alignment claim is now
>    actually true, not just claimed.
> 6. **Found and fixed one dangling doc cross-reference**: the SAM template's
>    `Tracing: Active` comment (`examples/Aws/Benzene.Examples.Aws/template.yaml`)
>    pointed at an "X-Ray" section in `docs/aws-iam-permissions.md` that
>    `1081bd1` deleted. Rewritten to explain SAM's automatic IAM grant and the
>    OTel-based tracing story instead.
> 7. **`WithRequestId()`/`WithApplication()` in `Benzene.Aws.Lambda.Core` marked
>    `[Obsolete]`** (commit `13c71de`), superseded by the portable
>    `Benzene.Diagnostics.EnrichmentExtensions.UseBenzeneEnrichment()`. Noted
>    here since it's a new public-API surface change in a package this roadmap
>    tracks; no roadmap checkbox maps directly to it.
>
> Everything else in the 2026-07-12 changelog above remains accurate and
> unchanged by this pass.

> **2026-07-12 changelog** — P0 items resolved today, in order. Package/percentage
> details and discovered bugs are recorded inline at each item's section below rather
> than repeated here:
> 1. **XML Documentation** (P0 #1) — 100% coverage across all 9 AWS packages, 0 CS1591
>    warnings.
> 2. **EventBridge/S3 rename** (P0 #2) — `Benzene.Aws.Lambda.EventBridge` renamed to
>    `Benzene.Aws.Lambda.S3` (pure rename; the code was always S3 event handling, never
>    EventBridge). ~~Real EventBridge/CloudWatch Events support remains unbuilt~~ — **built
>    2026-07-14**: `Benzene.Aws.Lambda.EventBridge` (inbound, topic = `detail-type`) +
>    `Benzene.Clients.Aws/EventBridge` (outbound PutEvents client) + TestHelpers, per
>    `docs/plans/eventbridge-plan.md`.
> 3. **Unit Tests** (P0 #3) — all 9 packages to 90%+ coverage (up from as low as 52%).
>    Found and fixed 3 real bugs along the way (X-Ray timer crash, SNS client resolver
>    bug, Lambda health check status bug); found but did NOT fix a 4th
>    (`Extensions.AddLambdaClients` DI registration gap — needs a design decision, not
>    a mechanical fix).
> 4. **Integration Tests / LocalStack** (P0 #7) — `test/Benzene.Aws.Tests` wired into
>    CI (`aws-integration-tests` job), passing against a real LocalStack container.
>    Fixed fixture robustness issues and a CI-environment `docker-compose` binary gap
>    along the way; added 3 new tests exercising client classes directly.
> 5. **IAM Permissions Docs, Getting Started Guides, SAM Template** (P0 #4-#6) — new
>    `docs/aws-iam-permissions.md`; `docs/getting-started-aws.md` expanded with
>    SNS/S3/Kafka snippets; new `examples/Aws/Benzene.Examples.Aws/template.yaml` SAM
>    template (API Gateway + SQS + SNS, optional Kafka/MSK). SAM only — CDK explicitly
>    out of scope for this pass, noted as future work.
> 6. **Dependency Cleanup** (P0 #8) — aligned `AWSSDK.SQS` to one version
>    (`3.7.502.57`) across all packages, deliberately staying within the 3.7.x line
>    rather than jumping to the new v4 major (every AWSSDK.*/Amazon.Lambda.* package
>    has had a major release since this repo pinned; that's a bigger, riskier
>    decision left for later — see package section for the full version table).
>    Removed the unused `AWSSDK.SQS` reference from `Benzene.Aws.XRay` and the stale
>    `System.Text.Encodings.Web` 6.0.0 pin from 7 packages (vestigial from before they
>    targeted `net10.0`; verified safe via restore/build/full test suite).
> 7. **Code Quality Fixes** (P0 #9) — scoped to two verifiable bugs, not the full
>    original list: `SqsApplication.HandleAsync` no longer swallows a failed record's
>    exception (now logged via `IBenzeneLogger`); `SqsConsumer.StartAsync` no longer
>    dies permanently on a transient AWS error (broadened from catching only
>    `TaskCanceledException` to all `OperationCanceledException`, with any other
>    exception logged and the poll loop continuing) and its hardcoded
>    `WaitTimeSeconds = 1` is now a configurable `SqsConsumerConfig` property
>    (defaulting to `1`, non-breaking). The `AwsLambdaStartUp` virtual-call-in-constructor
>    item was deliberately NOT touched — it's a suppressed, intentional pattern
>    underpinning the whole entry-point model, and "fixing" it means a breaking
>    redesign, not a code-quality fix. Two new tests added, full suite green
>    (655/655).
> 8. **Migration Guide** (P0 #10) — descoped, not written. No external adopters of the
>    AWS packages exist yet (pre-1.0, nothing released that anyone depends on), so
>    there's no one to write a migration guide for. Re-add as a real item if that
>    changes before 1.0 ships.
>
> **P0 list is now fully resolved** — every remaining item is complete, consciously
> deferred pending a design/product decision (`AddLambdaClients` DI gap, AWSSDK v4
> upgrade), or descoped as not applicable (Migration Guide). Full narrative detail for
> each completed item remains in that item's own section further down this document.

---

## Executive Summary

This roadmap outlines the path to 1.0.0 for Benzene's AWS integration packages and defines the strategic direction for AWS-specific features over the next 12+ months. The AWS ecosystem within Benzene currently consists of **8 production packages** and **5 TestHelper packages** supporting Lambda, SQS, SNS, S3, and Kafka. (X-Ray tracing is no longer a dedicated package — see the 2026-07-13 changelog above.)

### Current State
- **Package Count:** 8 AWS production packages, 5 TestHelpers (`Benzene.Aws.XRay` deleted 2026-07-13, superseded by OpenTelemetry — see changelog)
- **Version:** All at 0.0.2 (pre-release; centralized via repo-root `version.txt`, corrected 2026-07-14 — this document previously claimed 0.0.1)
- **Target Framework:** .NET 10
- **Source Files:** 131 AWS-related `.cs` source files across all 8 production + 5 TestHelper packages, recounted 2026-07-14 — the previous "~179" figure could not be reproduced and appears stale
- **Test Coverage:** ✅ 90%+ unit test coverage across all 8 remaining packages, plus LocalStack integration tests passing in CI (both completed 2026-07-12)
- **Documentation:** ✅ 100% XML documentation (completed 2026-07-12; regressed to 3 missing summaries on `AwsLambdaHost<TStartUp>` when that class was added afterward, found and fixed 2026-07-13 — see changelog), basic CLAUDE.md files exist
- **Maturity:** Functional but not production-ready for 1.0

### Key Findings
✅ **Strengths:**
- Clean, consistent architecture across all Lambda adapters
- Good separation of concerns (each event source = separate package)
- TestHelpers properly extracted to dedicated packages
- Working examples demonstrate real-world usage
- No TODO/FIXME/HACK comments found in codebase
- ✅ 100% XML documentation coverage across all 8 remaining packages, zero CS1591 warnings (completed 2026-07-12; a regression from `AwsLambdaHost<TStartUp>`'s 3 undocumented members, added after that pass, was found and fixed 2026-07-13 — verified via a clean rebuild)

❌ **Critical Blockers for 1.0:**
- ~~ZERO XML documentation on any public API~~ ✅ RESOLVED 2026-07-12
- Minimal test coverage (~4 test classes for 8 packages)
- No performance benchmarks or cold-start optimization metrics
- Missing IAM permission documentation
- No CloudFormation/SAM/CDK integration examples
- Inconsistent AWS SDK versions across packages
- Missing multi-region testing
- No cost optimization guidance

### Recommended 1.0 Strategy

**CONSERVATIVE APPROACH (RECOMMENDED):**
Keep all AWS packages at **0.9.x-preview** until after core 1.0 release, then:
- Ship AWS packages at **1.0.0** only after addressing blockers above
- Allows core packages to stabilize first (Benzene 1.0 dependency)
- Gives time to gather AWS-specific production feedback
- Reduces risk of breaking changes to AWS-specific APIs

**Timeline Estimate:** 3-6 months post core 1.0 release

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
| **Benzene.Aws.Lambda.Core** | 0.0.1 | Core Lambda abstractions & entry points | Medium | ⚠️ Needs work |
| **Benzene.Aws.Lambda.ApiGateway** | 0.0.1 | API Gateway (REST/HTTP) adapter | Medium | ⚠️ Needs work |
| **Benzene.Aws.Lambda.Sqs** | 0.0.1 | SQS event source adapter | Medium | ⚠️ Needs work |
| **Benzene.Aws.Lambda.Sns** | 0.0.1 | SNS event source adapter | Medium | ⚠️ Needs work |
| **Benzene.Aws.Lambda.S3** (was `.EventBridge`) | 0.0.1 | S3 event notification adapter | Medium | ⚠️ Needs work |
| **Benzene.Aws.Lambda.Kafka** | 0.0.1 | MSK/Kafka event source adapter | Low | ❌ Not ready |
| **Benzene.Aws.Sqs** | 0.0.1 | SQS client for publishing | Medium | ⚠️ Needs work |
| ~~**Benzene.Aws.XRay**~~ | — | ✅ Deleted 2026-07-13 — superseded by `Benzene.OpenTelemetry`'s `AddBenzeneInstrumentation()` (see changelog) | — | N/A |
| **Benzene.Clients.Aws** | 0.0.1 | AWS service clients (Lambda, SQS, SNS, Step Functions) | Low | ❌ Not ready |

**TestHelper Packages (not for 1.0):**
- Benzene.Aws.Lambda.ApiGateway.TestHelpers
- Benzene.Aws.Lambda.Kafka.TestHelpers
- Benzene.Aws.Lambda.Sns.TestHelpers
- Benzene.Aws.Lambda.Sqs.TestHelpers
- Benzene.Aws.Sqs.TestHelpers

### Code Quality Metrics

**Positive Indicators:**
- ✅ No TODO/FIXME/HACK comments found
- ✅ Consistent naming conventions
- ✅ Clean separation: one package per event source
- ✅ TestHelpers properly separated
- ✅ Async/await used throughout (11 occurrences in Lambda.Core)
- ✅ Proper disposal patterns (IDisposable on entry points)

**Red Flags:**
- ~~0 XML documentation comments across ALL packages~~ ✅ RESOLVED 2026-07-12 (100% coverage, all 9 packages)
- ❌ Only 4 test classes found for 8 packages
- ✅ LocalStack integration tests wired into CI and passing (completed 2026-07-12)
- ❌ EventBridge package references wrong dependency (Amazon.Lambda.S3Events instead of CloudWatchEvents)
- ❌ No performance benchmarks
- ❌ No SAM/CloudFormation templates in examples

### Dependency Analysis

**AWS SDK Dependencies:**
```
Amazon.Lambda.Core                      2.2.0
Amazon.Lambda.Serialization.SystemTextJson 2.4.0
Amazon.Lambda.APIGatewayEvents          2.6.0
Amazon.Lambda.SQSEvents                 2.1.0
Amazon.Lambda.SNSEvents                 2.0.0
Amazon.Lambda.KafkaEvents               1.0.1
Amazon.Lambda.S3Events                  3.1.0  ⚠️ WRONG (EventBridge pkg)
AWSSDK.SQS                             3.7.100.74, 3.7.2.63  ⚠️ INCONSISTENT
AWSSDK.Lambda                          3.7.303.2
AWSSDK.StepFunctions                   3.7.301.4
AWSSDK.SimpleNotificationService       3.7.301.4
AWSXRayRecorder.Handlers.AwsSdk        2.11.0
```

> This table is the original as-found snapshot and is kept for historical
> context. All three issues below were resolved 2026-07-12 (SDK versions
> aligned, "EventBridge" renamed to `Benzene.Aws.Lambda.S3` since it always was
> S3 event handling, `System.Text.Encodings.Web` pin removed) — see the
> 2026-07-12 changelog at the top of this document. `AWSXRayRecorder.Handlers.AwsSdk`
> no longer appears anywhere in the repo — it was `Benzene.Aws.XRay`'s only
> purpose, and that package was deleted 2026-07-13 (see the 2026-07-13 changelog).

**Issues:**
1. ~~⚠️ **Inconsistent AWSSDK.SQS versions** (3.7.100.74 vs 3.7.2.63)~~ ✅ RESOLVED 2026-07-12 (aligned to `3.7.502.57`); one package missed in that pass (`Benzene.Aws.Sqs.TestHelpers`) was found and fixed 2026-07-13
2. ~~⚠️ **EventBridge package references S3Events** instead of CloudWatchEvents~~ ✅ RESOLVED 2026-07-12 (renamed to `Benzene.Aws.Lambda.S3`; the dependency was always correct)
3. ~~⚠️ Old `System.Text.Encodings.Web` version (6.0.0) - should align with .NET 10~~ ✅ RESOLVED 2026-07-12 (pin removed from all 7 affected packages)

---

## Package-by-Package Analysis

### 1. Benzene.Aws.Lambda.Core ⭐ Foundation Package

**Location:** `src/Benzene.Aws.Lambda.Core/`
**Current State:** Medium maturity, functional but incomplete

**Public API Surface:**
- `IAwsLambdaEntryPoint` - Entry point abstraction
- `AwsLambdaEntryPoint` - Base implementation (C:\Users\pelled\source\libs\Benzene\src\Benzene.Aws.Lambda.Core\AwsLambdaEntryPoint.cs)
- `AwsLambdaStartUp` / `AwsLambdaStartUp<TContainer>` - Startup pattern
- `InlineAwsLambdaStartUp` - Inline configuration
- `AwsEventStreamContext` - Event stream context
- `AwsLambdaMiddlewareRouter` - Event routing
- `IAwsEntryPointBuilder` - Builder abstraction
- BenzeneMessage integration (DirectMessageLambdaHandler)
- `AwsLambdaHost<TStartUp>` / `AwsLambdaApplicationBuilder` - unified, platform-neutral
  `BenzeneStartUp` hosting model shared with Azure Functions and ASP.NET Core (found
  2026-07-14 — was already tracked indirectly via a doc-coverage regression note on
  2026-07-13, but not listed here; see `docs/hosting.md`)

**Strengths:**
- Clean startup pattern similar to ASP.NET Core
- Proper disposal of service resolver factory
- Generic support for different DI containers
- Router pattern for multiple event sources

**Issues:**
1. ❌ No XML documentation on any type
2. ❌ Error message in line 34 of AwsLambdaEntryPoint.cs is too long and not helpful
3. ⚠️ Virtual member calls in constructor (AwsLambdaStartUp.cs:28-37) - suppressed but potentially dangerous
4. ⚠️ No cold-start optimization guidance
5. ⚠️ No metrics/logging for startup time

**1.0 Requirements:**
- [x] Add comprehensive XML documentation (completed 2026-07-12, part of the AWS-wide documentation pass)
- [ ] Improve error messages with actionable guidance
- [ ] Add startup time logging
- [x] Document cold-start best practices (found 2026-07-14 —
      `docs/cookbooks/lambda-cold-start-optimization.md` covers source-generated handler
      registration, arm64/memory tuning, ReadyToRun, lazy initialization, and provisioned
      concurrency; actual benchmarks remain unbuilt, see Performance & Optimization section)
- [ ] Add Lambda runtime initialization hooks
- [x] Create migration guide from bare-metal to StartUp pattern (found 2026-07-14 —
      `docs/migration-alpha-to-1.0.md`'s "Unified hosting" section documents migrating to
      `AwsLambdaHost<TStartUp>`/`BenzeneStartUp`, including a per-platform host-entry-point
      table; the pre-1.0 `AwsLambdaStartUp` path is explicitly called out as still working,
      not a forced migration)
- [ ] Add examples of custom IAwsEntryPointBuilder implementations

**Estimated Effort:** 8-12 hours remaining (down from 15-20 — cold-start documentation and
the bare-metal-to-StartUp migration guide were both found already done, 2026-07-14)

---

### 2. Benzene.Aws.Lambda.ApiGateway ⭐ HTTP Adapter

**Location:** `src/Benzene.Aws.Lambda.ApiGateway/`
**Current State:** Medium maturity, most complete adapter

**Public API Surface:**
- `ApiGatewayApplication` - Main application
- `ApiGatewayLambdaHandler` - Lambda handler
- `ApiGatewayContext` - HTTP context implementation (C:\Users\pelled\source\libs\Benzene\src\Benzene.Aws.Lambda.ApiGateway\ApiGatewayContext.cs)
- `ApiGatewayHttpRequestAdapter` - Request adapter
- `ApiGatewayResponseAdapter` - Response builder
- Message handlers (BodyGetter, HeadersGetter, TopicGetter, ResultSetter)
- `ApiGatewayRequestEnricher` - Request enrichment
- ~~**CORS:** `ApiGatewayContextCorsMiddleware` + extensions~~ ❌ WRONG, corrected 2026-07-14 —
  that class and its `Extensions.cs` were deleted; `src/Benzene.Aws.Lambda.ApiGateway/Cors/`
  no longer exists. CORS is now provided by the portable `Benzene.Http.Cors.CorsMiddleware<TContext>`
  (generic over `IHttpContext`, shared with Azure Functions and ASP.NET Core), wired up the
  same way here via `.UseCors(new CorsSettings {...})` — see changelog
- **Custom Authorizer:** Full custom authorizer support
- Various registration and extension classes

**Strengths:**
- Most feature-complete AWS adapter
- ~~CORS support built-in~~ CORS support built-in via the shared `Benzene.Http.Cors.CorsMiddleware<TContext>`
  (corrected 2026-07-14 — not an AWS-specific implementation as previously described)
- Custom authorizer implementation
- Clean HTTP abstraction mapping
- Supports both REST API and HTTP API formats

**Issues:**
1. ❌ No XML documentation
2. ❌ ApiGatewayContext (line 6) is too simple - missing request/response properties
3. ⚠️ No guidance on binary content handling (base64)
4. ⚠️ No multi-value header/query string examples
5. ~~⚠️ CORS configuration not documented~~ ✅ RESOLVED 2026-07-14 — `docs/common-middleware.md`'s
   `UseCors` section fully documents `CorsSettings`/`CorsMiddleware<TContext>` (origin matching,
   exposed headers, max-age, credentials, wildcard headers, preflight validation)
6. ⚠️ Custom authorizer IAM policy generation not documented
7. ⚠️ No OpenAPI/Swagger integration examples

**1.0 Requirements:**
- [x] Add comprehensive XML documentation (completed 2026-07-12, part of the AWS-wide documentation pass)
- [ ] Expand ApiGatewayContext with convenience properties
- [x] Document CORS setup with examples (✅ RESOLVED 2026-07-14 — see `docs/common-middleware.md`'s
      `UseCors` section; this was never an AWS-specific gap once `ApiGatewayContextCorsMiddleware`
      was deleted, since CORS moved to the shared `Benzene.Http` package, see changelog)
- [ ] Document custom authorizer patterns (checked 2026-07-14: `docs/cookbooks/README.md`
      links to a not-yet-written `api-gateway-authorizers.md` — genuinely still open)
- [ ] Add binary content handling guide
- [ ] Add OpenAPI integration example
- [ ] Document API Gateway request/response limits
- [ ] Add IAM policy examples for authorizers
- [ ] Performance testing for cold starts
- [ ] Document differences between REST API v1 and HTTP API v2

**Estimated Effort:** 18-22 hours (down from 20-25 — CORS documentation item resolved
2026-07-14, see changelog)

---

### 3. Benzene.Aws.Lambda.Sqs ⭐ Queue Consumer

**Location:** `src/Benzene.Aws.Lambda.Sqs/`
**Current State:** Medium maturity, functional

**Public API Surface:**
- `SqsApplication` - Main application (C:\Users\pelled\source\libs\Benzene\src\Benzene.Aws.Lambda.Sqs\SqsApplication.cs)
- `SqsLambdaHandler` - Lambda handler
- `SqsMessageContext` - Message context
- Message handlers (BodyGetter, HeadersGetter, TopicGetter, ResultSetter)
- `SqsRegistrations` - Service registration

**Strengths:**
- Batch processing with Task.WhenAll (line 47 of SqsApplication.cs)
- Partial batch failure support
- Clean message attribute handling
- Topic extraction from attributes

**Issues:**
1. ❌ No XML documentation
2. ⚠️ Exception handling swallows exception details (line 40-43 of SqsApplication.cs)
3. ⚠️ No retry configuration guidance
4. ⚠️ No dead-letter queue documentation
5. ⚠️ No message visibility timeout guidance
6. ⚠️ No FIFO queue support documentation
7. ⚠️ No message deduplication guidance
8. ⚠️ Batch failure handling could log more details

**1.0 Requirements:**
- [x] Add comprehensive XML documentation (completed 2026-07-12, part of the AWS-wide documentation pass)
- [x] Improve exception logging in batch processing (completed 2026-07-12 — `SqsApplication.HandleAsync` now logs a failed record's exception via `ILogger` before adding it to `BatchItemFailures`)
- [x] Document DLQ configuration patterns (completed 2026-07-13 — `docs/cookbooks/handling-sqs-failures.md`)
- [ ] Document FIFO queue usage
- [x] Add retry and backoff strategies (completed 2026-07-13 — `docs/cookbooks/handling-sqs-failures.md` covers in-process retry middleware plus partial-batch-failure reporting)
- [ ] Document visibility timeout implications
- [ ] Add message attribute best practices
- [ ] Document batch size optimization
- [ ] Add CloudWatch Logs integration example
- [ ] Document cost optimization (batch sizes, polling)

**Estimated Effort:** 15-20 hours

---

### 4. Benzene.Aws.Lambda.Sns 📢 Pub/Sub

**Location:** `src/Benzene.Aws.Lambda.Sns/`
**Current State:** Medium maturity, simple but functional

**Public API Surface:**
- `SnsApplication` - Main application (C:\Users\pelled\source\libs\Benzene\src\Benzene.Aws.Lambda.Sns\SnsApplication.cs)
- `SnsLambdaHandler` - Lambda handler
- `SnsRecordContext` - Record context
- Message handlers (BodyGetter, HeadersGetter, TopicGetter, ResultSetter)
- `SnsUtils` - Utility functions
- `SnsRegistrations` - Service registration

**Strengths:**
- Clean implementation using MiddlewareMultiApplication
- Proper record processing with transport tagging
- Topic ARN extraction

**Issues:**
1. ❌ No XML documentation
2. ⚠️ No SNS subscription confirmation handling documented
3. ⚠️ No message filtering policy examples
4. ⚠️ No raw message delivery documentation
5. ⚠️ Topic ARN parsing could fail - no error handling

**1.0 Requirements:**
- [x] Add comprehensive XML documentation (completed 2026-07-12, part of the AWS-wide documentation pass)
- [ ] Document subscription confirmation flow
- [ ] Add message filtering policy examples
- [ ] Document raw vs. wrapped message delivery
- [ ] Add SNS FIFO topic support documentation
- [x] Document message attributes vs. headers mapping (completed 2026-07-13 — this
      was also a real bug, not just missing docs: `SnsContextConverter` didn't forward
      `IBenzeneClientRequest.Headers` onto `PublishRequest.MessageAttributes` at all,
      silently dropping correlation IDs/W3C trace context; fixed in commit `6e88ad6`,
      documented in `docs/clients.md` and `docs/cookbooks/sns-fan-out.md`)
- [x] Add fan-out architecture examples (completed 2026-07-13 — `docs/cookbooks/sns-fan-out.md`)
- [ ] Document message deduplication for FIFO
- [ ] Add error handling for malformed topic ARNs

**Estimated Effort:** 12-15 hours

---

### 5. Benzene.Aws.Lambda.S3 ✅ Renamed (was Benzene.Aws.Lambda.EventBridge)

**Location:** `src/Benzene.Aws.Lambda.S3/`
**Current State:** Medium maturity, naming mismatch resolved 2026-07-12

> **2026-07-12 update:** This package was renamed from `Benzene.Aws.Lambda.EventBridge`
> (`AssemblyName`/`RootNamespace` `Benzene.Aws.EventBridge`) to
> `Benzene.Aws.Lambda.S3` (`Benzene.Aws.S3`), resolving the naming-mismatch blocker
> below. This was a pure rename — no functional changes. All classes (`S3Application`,
> `S3LambdaHandler`, `S3RecordContext`, `S3Registrations`) were already correctly named
> for what they do (S3 event notification handling); only the package/assembly/
> namespace and surrounding docs were wrong. `Amazon.Lambda.S3Events` is the correct
> dependency for this package's actual functionality — it was never "wrong," it just
> didn't match the old package name.
>
> ~~Real EventBridge/CloudWatch Events support **does not exist** anywhere in Benzene.~~
> **Built 2026-07-14**: `Benzene.Aws.Lambda.EventBridge` now exists (inbound adapter — topic
> = `detail-type`, body = `detail`, headers per the `_benzeneHeaders` convention, one
> pipeline invocation per event; no `Amazon.Lambda.CloudWatchEvents` dependency needed — the
> envelope is modeled as Benzene's own POCO), plus an outbound
> `EventBridgeBenzeneMessageClient` in `Benzene.Clients.Aws` (`PutEvents`; new
> `AWSSDK.EventBridge` dependency) and a `.TestHelpers` package. Design record:
> `docs/plans/eventbridge-plan.md`.

**Public API Surface:**
- `S3Application`, `S3LambdaHandler`, `S3RecordContext`, `S3Registrations` — names now
  match the package name and match what they've always done

**1.0 Requirements:**
- [x] Fix package naming (renamed to `Benzene.Aws.Lambda.S3`)
- [x] Add comprehensive XML documentation (completed 2026-07-12, part of the AWS-wide
      documentation pass)
- [ ] Document S3 event notification structure (object created/removed/restored, etc.)
- [ ] Add example: image processing pipeline triggered by S3 uploads

**Estimated Effort:** 2-3 hours remaining (narrative documentation only; the
naming/dependency work is done)

---

### 6. Benzene.Aws.Lambda.Kafka 🆕 Newer, Less Mature

**Location:** `src/Benzene.Aws.Lambda.Kafka/`
**Current State:** Low maturity, newer addition

**Public API Surface:**
- `KafkaApplication` - Main application
- `KafkaLambdaHandler` - Lambda handler
- `KafkaContext` - Message context
- Message handlers (BodyGetter, HeadersGetter, TopicGetter, ResultSetter)
- `KafkaRegistrations` - Service registration

**Strengths:**
- Supports both MSK and self-managed Kafka
- Kafka headers mapped to Benzene headers
- Partition and offset available

**Issues:**
1. ❌ No XML documentation
2. ⚠️ Newer package, less battle-tested
3. ⚠️ No Kafka-specific error handling documented
4. ⚠️ No schema registry integration
5. ⚠️ No Avro/Protobuf serialization examples
6. ⚠️ No consumer group management documentation
7. ⚠️ No offset management strategies documented
8. ⚠️ No MSK IAM authentication examples

**1.0 Requirements:**
- [x] Add comprehensive XML documentation (completed 2026-07-12, part of the AWS-wide documentation pass)
- [ ] Document MSK vs. self-managed Kafka differences
- [ ] Add IAM authentication examples for MSK
- [ ] Document offset commit strategies
- [ ] Add schema registry integration examples
- [ ] Document Avro/Protobuf serialization
- [ ] Add batch processing optimization guidance
- [ ] Document error handling and DLQ patterns
- [ ] Add partition assignment documentation
- [ ] Document scaling considerations

**Recommendation:** Keep at 0.9.x-preview through 2026 to gather production feedback

**Estimated Effort:** 20-25 hours

---

### 7. Benzene.Aws.Sqs 📤 SQS Client

**Location:** `src/Benzene.Aws.Sqs/`
**Current State:** Medium maturity, dual-purpose package

**Public API Surface:**
- **Client:** `ISqsClient`, `SqsMessageClient` (C:\Users\pelled\source\libs\Benzene\src\Benzene.Aws.Sqs\Client\SqsMessageClient.cs)
- **Consumer:** `SqsConsumer`, `SqsConsumerApplication`, `SqsConsumerConfig` (C:\Users\pelled\source\libs\Benzene\src\Benzene.Aws.Sqs\Consumer\SqsConsumer.cs)
- Message mappers (BodyMapper, TopicMapper, HeadersGetter, ResultSetter)
- `ISqsClientFactory`, `SqsClientFactory`
- `SqsRegistrations` - Service registration

**Strengths:**
- Both publishing AND consuming (non-Lambda)
- Clean abstraction over AWSSDK.SQS
- Message attribute handling
- Queue URL resolution

**Issues:**
1. ❌ No XML documentation
2. ⚠️ Consumer has infinite loop without cancellation safeguards (line 29-58 in SqsConsumer.cs)
3. ⚠️ TaskCanceledException silently swallowed (line 54-56)
4. ⚠️ No batch send operation
5. ⚠️ No FIFO queue support documented
6. ⚠️ Message deduplication not supported
7. ⚠️ No retry policy on publish failures
8. ⚠️ Hard-coded WaitTimeSeconds = 1 (should be configurable)
9. ⚠️ Depends on Amazon.Lambda.SQSEvents but isn't a Lambda package

**1.0 Requirements:**
- [x] Add comprehensive XML documentation (completed 2026-07-12, part of the AWS-wide documentation pass)
- [ ] Add batch send operation
- [ ] Add configurable polling settings
- [ ] Improve cancellation handling
- [ ] Add retry policies for publish failures
- [ ] Document FIFO queue usage
- [ ] Add message deduplication support
- [ ] Remove Lambda dependency from non-Lambda package
- [ ] Add circuit breaker pattern for resilience
- [ ] Document cost optimization (long polling)
- [ ] Add graceful shutdown handling

**Estimated Effort:** 18-22 hours

---

### 8. ~~Benzene.Aws.XRay~~ 📊 Distributed Tracing — ✅ DELETED 2026-07-13

**Location:** `src/Benzene.Aws.XRay/` — no longer exists (commit `1081bd1`, "Delete
Benzene.Datadog, Benzene.Zipkin, and Benzene.Aws.XRay (Checkpoint B)").

Every issue and requirement this section used to list — no XML docs, no
segment/subsegment management, no annotation/metadata capture, no integration
with Benzene's diagnostics, an unnecessary `AWSSDK.SQS` reference — is now moot;
there's no package left to fix. It wasn't replaced by a better X-Ray-specific
package, it was **subsumed**: `AddDiagnostics()` (`Benzene.Diagnostics`) already
wraps every middleware in every pipeline in a real `System.Diagnostics.Activity`
span, and `Benzene.OpenTelemetry`'s `AddBenzeneInstrumentation()` exports those
spans through any standard OpenTelemetry exporter — including an X-Ray one, via
the OTel Collector's AWS X-Ray exporter — instead of a bespoke package hard-wired
to one backend. This is a strict improvement over the old package's "Too
simplistic - only registers SDK handler" state: annotation/metadata capture,
custom span naming, and segment nesting are now just standard `Activity` API,
already exercised by the existing diagnostics test suite (`BenzeneInstrumentationTest`).

Deleted alongside its tests (`test/Benzene.Core.Test/Aws/XRay/*`), its
`ProjectReference` from `Benzene.Test.csproj`, its entries in both `.sln` files,
and its usage in the AWS example (`StartUpAutofac.cs`'s `.UseXRayTracing(true)`
call). `docs/aws-iam-permissions.md` and `docs/monitoring.md` had their
X-Ray-specific sections removed as part of the same commit; general OTel export
guidance remains in `docs/monitoring.md`'s "OpenTelemetry" section.

A real OTel-collector-backed integration test proving the X-Ray export path
end-to-end is explicitly left as follow-up per the deletion commit's own
message — the OTel wiring itself is unit-tested, but nothing in CI currently
verifies a span actually lands in X-Ray specifically.

**Remaining work (if this is ever revisited):**
- [ ] Add an integration test that exports a span through the OTel Collector's
      AWS X-Ray exporter and confirms it's queryable in X-Ray (or an X-Ray-shaped
      local emulator) — the one thing the deletion commit didn't cover
- [ ] Consider a short cookbook ("Exporting Benzene traces to AWS X-Ray") if
      this comes up in practice — `docs/monitoring.md` documents the OTel path
      generically but doesn't have an X-Ray-specific walkthrough

**Estimated Effort:** 0 hours (package deleted); 4-6 hours if the integration
test above is picked up

---

### 9. Benzene.Clients.Aws 🔌 Service Clients

**Location:** `src/Benzene.Clients.Aws/`
**Current State:** Low maturity, needs investigation

**Public API Surface:**
- Lambda invocation client (folder: Lambda/)
- SQS client (folder: Sqs/)
- SNS client (folder: Sns/)
- Step Functions client (folder: StepFunctions/)

**Issues:**
1. ❌ No XML documentation
2. ❌ Not enough information - need to review actual implementations
3. ⚠️ Overlaps with Benzene.Aws.Sqs client functionality
4. ⚠️ Purpose unclear vs. direct AWS SDK usage

**1.0 Requirements:**
- [ ] Full code review of client implementations
- [x] Add comprehensive XML documentation (completed 2026-07-12, part of the AWS-wide documentation pass)
- [x] Clarify purpose vs. AWS SDK (completed 2026-07-13 — `docs/clients.md`: a
      Benzene client is an `IBenzeneMessageClient` decorated with cross-cutting
      behavior — correlation IDs, W3C trace propagation, retries — that another
      Benzene service calls through, on top of the AWS SDK, not instead of it)
- [x] Add client usage examples (completed 2026-07-13 — `docs/clients.md` and
      `docs/cookbooks/sns-fan-out.md`)
- [ ] Document authentication patterns
- [ ] Add retry and resilience patterns
- [ ] Document health check integration
- [ ] Add circuit breaker support
- [ ] Document service discovery patterns
- [ ] Add mocking support for testing

**Estimated Effort:** 20-25 hours (pending full review)

---

## Roadmap to 1.0.0

### Critical Path Items (BLOCKERS)

**Must Have Before 1.0:**

1. ~~**XML Documentation** (60-80 hours) - HIGHEST PRIORITY~~ ✅ COMPLETE 2026-07-12
   - ~~Document every public type, method, property~~ ✅ Done for all 8 remaining
     packages, 0 CS1591 warnings (a 3-member regression on `AwsLambdaHost<TStartUp>`,
     added after this pass, was found and fixed 2026-07-13)
   - `<example>` blocks and AWS-specific behavior docs (IAM, limits, costs) were out of scope
     for this pass and remain open as narrative documentation work (see Documentation
     Requirements section below)

2. ~~**Fix Broken EventBridge Package** (25-30 hours) - CRITICAL~~ ✅ NAMING RESOLVED 2026-07-12
   - ~~Resolve S3 vs. EventBridge naming confusion~~ ✅ Renamed `Benzene.Aws.Lambda.EventBridge`
     → `Benzene.Aws.Lambda.S3`; dependency (`Amazon.Lambda.S3Events`) was already correct
     for the package's actual S3 functionality
   - Building genuine EventBridge/CloudWatch Events support remains unstarted and is now
     tracked as separate, distinct future work (a new package, not a fix to this one) —
     see medium-term roadmap

3. ~~**Test Coverage** (40-60 hours) - CRITICAL~~ ✅ COMPLETE 2026-07-12
   - ~~Unit tests for all packages (target 80%+ coverage)~~ ✅ All 9 packages now 90%+
     (see 2026-07-12 update above); 3 real bugs found and fixed along the way
   - ~~Integration tests with LocalStack~~ ✅ Wired into CI (`aws-integration-tests` job),
     passing (see 2026-07-12 update above)
   - End-to-end Lambda examples — still open
   - Performance benchmarks — still open

4. ~~**Dependency Cleanup** (8-12 hours)~~ ✅ COMPLETE 2026-07-12
   - ~~Standardize AWSSDK versions~~ ✅ `AWSSDK.SQS` was on two different versions
     (`3.7.100.74` in Benzene.Aws.Sqs/XRay vs `3.7.2.63` in Benzene.Clients.Aws);
     aligned to `3.7.502.57` (latest within the 3.7.x line — no major-version jump,
     per a conscious decision to keep this pass low-risk since AWS SDK majors can
     carry breaking changes and this sandbox has no way to test against live AWS)
   - ~~Remove unnecessary dependencies~~ ✅ Removed `AWSSDK.SQS` from
     `Benzene.Aws.XRay` — nothing in that package touches SQS
   - ~~Update System.Text.Encodings.Web to align with .NET 10~~ ✅ Removed the
     explicit `6.0.0` pin from all 7 packages that had it — vestigial from before
     these projects targeted `net10.0`; the pin was actually forcing an *older*
     version than what `net10.0`'s shared framework already provides transitively.
     Verified via `dotnet restore`/`dotnet build` (no NU1605 downgrade errors) and
     the full test suite (653/653 passing)

5. **Documentation** (30-40 hours)
   - Getting started guide for each adapter
   - IAM permissions documentation
   - CloudFormation/SAM/CDK templates
   - Architecture decision records
   - Migration guides

6. ~~**Code Quality Fixes** (15-20 hours)~~ ✅ PARTIALLY COMPLETE 2026-07-12 (scoped to
   the two verifiable bugs; virtual-constructor-call left as a deliberate pattern)
   - ~~Improve error messages~~ ✅ `SqsApplication.HandleAsync` no longer swallows the
     exception from a failed record — now logged via `IBenzeneLogger` before the item
     is added to `BatchItemFailures`
   - ~~Add missing error handling~~ ✅ `SqsConsumer.StartAsync` no longer dies
     permanently on a transient AWS error — only `OperationCanceledException` exits
     the loop silently; any other exception is logged and polling continues
   - ~~Fix hard-coded values~~ ✅ `WaitTimeSeconds` (was hardcoded `1`) is now a
     `SqsConsumerConfig` property, defaulting to `1` for unchanged behavior
   - Add configuration options — out of scope for this pass (see Medium/Low priority
     items below, e.g. batch send, retry policies — these are feature work, not fixes)
   - Remove constructor virtual calls — deliberately NOT done: `AwsLambdaStartUp`'s
     virtual-call-in-constructor pattern is how `new StartUp()` becomes immediately
     usable as the Lambda entry point; the calls are already explicitly suppressed
     (`// ReSharper disable once VirtualMemberCallInConstructor`), and "fixing" it
     means splitting construction from initialization — a breaking redesign of the
     whole entry-point pattern, not a code-quality fix. Left as a distinct,
     consciously-deferred item

**Total Estimated Effort for 1.0:** 53-112 hours remaining (60-80h XML documentation +
25-30h EventBridge/S3 naming fix + 40-50h unit test coverage now complete)

### Phased Approach

**Phase 1: Foundation (Weeks 1-2) - 60-80 hours**
- Fix EventBridge package
- Standardize dependencies
- Set up LocalStack integration tests
- Begin XML documentation (Core, ApiGateway)

**Phase 2: Quality (Weeks 3-4) - 60-80 hours**
- Complete XML documentation (all packages)
- Add unit tests (80%+ coverage)
- Fix code quality issues
- Performance benchmarking

**Phase 3: Polish (Weeks 5-6) - 58-102 hours**
- Integration tests
- Documentation and examples
- SAM/CloudFormation templates
- Security review
- Migration guides

**Phase 4: Release (Week 7) - 10-15 hours**
- Final testing
- CHANGELOG updates
- Release notes
- NuGet publishing
- Announcement

---

## Short-Term Roadmap (3-6 Months)

**Goal:** Release AWS packages at 1.0.0 after core Benzene 1.0 is stable

### Q3 2026 (Months 1-3)

**Month 1: Foundation & Cleanup**
- ✅ Fix EventBridge package crisis
- ✅ Standardize AWS SDK dependencies
- ✅ Set up LocalStack integration testing
- ✅ Begin comprehensive XML documentation
- ✅ Create SAM template examples
- Deliverable: Working EventBridge package, test infrastructure

**Month 2: Quality & Testing**
- ✅ Complete XML documentation (all packages)
- ✅ Achieve 80%+ unit test coverage
- ✅ Add integration tests for each event source
- ✅ Performance baseline measurements
- ✅ Security audit (IAM, encryption, logging)
- Deliverable: Test coverage report, security audit results

**Month 3: Documentation & Examples**
- ✅ Complete getting-started guides
- ✅ IAM permission documentation
- ✅ CloudFormation/SAM/CDK examples
- ✅ Cost optimization guide
- ✅ Migration guide from preview to 1.0
- ✅ Beta release (1.0.0-rc.1)
- Deliverable: Complete documentation, RC release

### Q4 2026 (Months 4-6)

**Month 4: Beta Testing & Feedback**
- 🔄 Community beta testing
- 🔄 Address beta feedback
- 🔄 Performance optimization based on real workloads
- 🔄 Final security review
- Deliverable: Beta feedback report, final fixes

**Month 5: Release Preparation**
- ✅ Final CHANGELOG updates
- ✅ Release notes preparation
- ✅ NuGet package validation
- ✅ Documentation review
- ✅ 1.0.0 release
- Deliverable: AWS packages at 1.0.0

**Month 6: Post-Release Support**
- 🔄 Monitor adoption and issues
- 🔄 Quick patches for critical bugs
- 🔄 Gather feedback for 1.1 features
- Deliverable: 1.0.1 patch release if needed

---

## Medium-Term Roadmap (6-12 Months)

**Goal:** Expand AWS integration coverage and optimize for production

### New Event Sources (Priority Order)

1. **AWS Lambda - DynamoDB Streams** (6-8 weeks) — ✅ **Built 2026-07-14**:
   `Benzene.Aws.Lambda.DynamoDb` (+ `.TestHelpers`), per `docs/plans/dynamodb-streams-plan.md`.
   Topic = `"{tableName}:{eventName}"`, body = record image unmarshalled from AttributeValue
   format to plain JSON, ordered sequential processing with stop-at-first-failure
   `ReportBatchItemFailures` checkpointing, zero new NuGet dependencies (own envelope POCOs).
   ~~Event source adapter~~ / ~~Change data capture patterns~~ done; DynamoDB-specific
   middleware (old-vs-new image diffing) and an event-sourcing example remain open as follow-up.

2. **AWS Lambda - Kinesis Data Streams** (6-8 weeks)
   - Event source adapter
   - Shard processing patterns
   - Checkpoint management
   - Example: Real-time analytics
   - **Effort:** 35-45 hours

3. **AWS Lambda - S3 Events** (4-6 weeks)
   - Event source adapter (reuse EventBridge code if refactored)
   - S3 event patterns (PUT, DELETE, etc.)
   - Presigned URL generation helpers
   - Example: Image processing pipeline
   - **Effort:** 25-30 hours

4. **AWS Lambda - Application Load Balancer** (4-6 weeks)
   - ALB target adapter
   - Health check support
   - Multi-value header handling
   - Example: HTTP service behind ALB
   - **Effort:** 25-30 hours

5. **AWS AppSync** (8-10 weeks)
   - GraphQL resolver adapter
   - Subscription support
   - DynamoDB integration
   - Example: Real-time chat app
   - **Effort:** 40-50 hours

### Advanced Features

1. **Lambda Powertools Integration** (4-6 weeks)
   - Metrics integration
   - Structured logging
   - Parameter/secrets integration
   - Example: Production-ready Lambda
   - **Effort:** 20-30 hours

2. **Cold Start Optimization** (6-8 weeks)
   - AOT compilation support (.NET 8+ Native AOT)
   - Initialization optimization
   - Dependency trimming
   - Lazy loading patterns
   - Benchmarking suite
   - **Effort:** 35-45 hours

3. **AWS Step Functions Integration** (8-10 weeks)
   - State machine adapter
   - Activity worker support
   - Express workflow optimizations
   - Example: Order processing workflow
   - **Effort:** 40-50 hours

4. **EventBridge Schema Registry** (4-6 weeks)
   - Schema validation integration
   - Code generation from schemas
   - Version management
   - Example: Type-safe event contracts
   - **Effort:** 25-30 hours

5. **Multi-Region Support** (6-8 weeks)
   - Region-aware routing
   - Cross-region replication helpers
   - Latency-based routing
   - Example: Global application
   - **Effort:** 30-40 hours

### Developer Experience

1. **VS Code Extension** (8-12 weeks)
   - SAM local debugging integration
   - Lambda function scaffolding
   - EventBridge event testing
   - **Effort:** 50-60 hours

2. **Pulumi/CDK Constructs** (6-8 weeks)
   - High-level constructs for common patterns
   - TypeScript and C# support
   - Best practice templates
   - **Effort:** 35-45 hours

3. **Observability Dashboard** (8-10 weeks)
   - CloudWatch dashboard templates
   - X-Ray integration templates
   - Custom metrics helpers
   - **Effort:** 40-50 hours

---

## Long-Term Vision (12+ Months)

### Strategic Initiatives

**1. AWS Serverless Platform** (6-12 months)
- Complete coverage of all Lambda event sources
- Serverless framework integration
- AWS SAM/CDK native integration
- Reference architectures for common patterns
- Comprehensive performance optimization guide

**2. Multi-Cloud Abstraction** (12-18 months)
- Unified event/message abstractions across AWS/Azure/GCP
- Cloud-agnostic business logic
- Adapter pattern for cloud services
- Migration tooling between clouds

**3. Enterprise Features** (12+ months)
- Multi-tenancy patterns
- Cost allocation and tagging
- Compliance and audit logging
- Advanced security (KMS, Secrets Manager)
- SLA monitoring and alerting

**4. AI/ML Integration** (12+ months)
- SageMaker endpoint integration
- Bedrock integration for AI workloads
- Lambda inference optimization
- Vector database integration (OpenSearch)

### Emerging AWS Services

**Monitor and Evaluate:**
- AWS Lambda SnapStart (Java/.NET support when available)
- Lambda Function URLs enhancements
- EventBridge Pipes
- Amazon MSK Serverless
- Aurora Serverless v2 integration
- Step Functions Distributed Map

---

## Technical Debt & Quality

### Current Technical Debt

**High Priority:**
1. ~~⚠️ EventBridge package naming/dependency confusion~~ ✅ RESOLVED 2026-07-12 (renamed to `Benzene.Aws.Lambda.S3`)
2. ~~⚠️ Inconsistent AWSSDK versions~~ ✅ RESOLVED 2026-07-12 (aligned to `3.7.502.57`); `Benzene.Aws.Sqs.TestHelpers` missed in that pass, fixed 2026-07-13
3. ~~⚠️ Exception swallowing in SqsApplication~~ ✅ RESOLVED 2026-07-12 (now logged via `IBenzeneLogger`/`ILogger`)
4. ⚠️ Virtual member calls in constructors — deliberately NOT fixed; see `AwsLambdaStartUp` discussion in the 2026-07-12 changelog (item 7) — fixing it means a breaking redesign of the entry-point pattern, not a code-quality fix
5. ~~⚠️ Hard-coded configuration values (e.g., WaitTimeSeconds = 1)~~ ✅ RESOLVED 2026-07-12 (now a configurable `SqsConsumerConfig` property, defaulting to `1`)
6. ~~⚠️ Unnecessary dependencies (XRay → AWSSDK.SQS)~~ ✅ MOOT 2026-07-13 — `Benzene.Aws.XRay` was deleted entirely, not just cleaned up

**Medium Priority:**
1. ApiGatewayContext too simple - needs convenience properties
2. Error messages not actionable
3. No batch operations in SqsMessageClient
4. ~~Consumer infinite loop without safeguards~~ ✅ PARTIALLY RESOLVED 2026-07-12 — `SqsConsumer.StartAsync` no longer dies permanently on a transient error (broadened from catching only `TaskCanceledException` to all `OperationCanceledException`, with any other exception logged and polling continuing); the loop is still intentionally infinite (that's the design for a long-running consumer), it just no longer silently stops
5. No retry policies on client operations

**Low Priority:**
1. Code duplication across message getters/setters
2. Missing async suffix on some async methods
3. No nullable reference type annotations consistently
4. Test helper naming could be more consistent

### Code Quality Improvements

**Standardization:**
- [ ] Consistent error handling patterns
- [x] Standardized logging approach — ✅ the old `IBenzeneLogger` abstraction was
      replaced with `Microsoft.Extensions.Logging` across every Benzene package,
      AWS included (commits `3f3b25d`, `eee1aa5`); every package now logs through
      the same standard `ILogger`/`ILogger<T>` API
- [ ] Unified configuration patterns
- [ ] Common retry/resilience patterns
- [ ] Consistent async/await usage

**Architecture:**
- [ ] Review separation of concerns in each package
- [ ] Evaluate if Consumer should be in Benzene.Aws.Sqs
- [ ] Consider base classes for event source adapters
- [ ] Review abstraction boundaries

**Performance:**
- [ ] Lazy initialization where appropriate
- [ ] Object pooling for high-throughput scenarios
- [ ] Memory allocation optimization
- [ ] Async enumerable for batch processing

---

## Testing Strategy

### Current State
- ✅ Unit test coverage complete (2026-07-12): all 8 remaining packages 90%+ (Core
  93.2%, ApiGateway 91.5%, Sqs 94.8%, Sns 98.5%, S3 100%, Kafka 96.7%, Aws.Sqs 100%,
  Clients.Aws 90.6%), in `test/Benzene.Core.Test/Aws/`. (`Benzene.Aws.XRay` measured
  92.7% at the time but was deleted 2026-07-13, superseded by OpenTelemetry — see
  changelog; its tests were deleted with it, not left uncovered.)
- ✅ LocalStack integration tests complete (2026-07-12): `test/Benzene.Aws.Tests` is now
  wired into CI as the `aws-integration-tests` job in `build-benzene.yml` and passing
  against a real LocalStack container. 7 test classes total (up from 4):
  `LambdaSenderBuilderTest`, `SnsMessageSenderBuilderTest`, `SqsConsumerTest`,
  `SqsMessageSenderBuilderTest` (pre-existing) plus new `SqsHealthCheckTest`,
  `SqsBenzeneMessageClientTest`, `SnsBenzeneMessageClientTest` (exercise the client
  classes directly rather than only through the pipeline-builder wrappers). The
  original "only 4 test classes" figure undercounted unit coverage (it described this
  project in isolation, missing `test/Benzene.Core.Test/Aws/`) — both gaps are now
  closed.
- No performance benchmarks
- No load tests

### Target Testing Strategy

**Unit Tests (Target: 80%+ coverage)**
- ✅ Every public method tested
- ✅ Edge cases and error conditions
- ✅ Mock AWS SDK dependencies
- ✅ Fast, deterministic tests
- Estimated: 60-80 hours to achieve target

**Integration Tests (Target: Key scenarios covered)**
- ✅ LocalStack for AWS services
- ✅ Real event source format validation
- ✅ End-to-end message flow
- ✅ IAM permission validation
- ✅ Multi-event source scenarios
- Estimated: 40-50 hours

**Performance Tests**
- ✅ Cold start benchmarks
- ✅ Warm start latency
- ✅ Throughput tests (messages/second)
- ✅ Memory usage profiling
- ✅ Comparison with baseline (raw Lambda)
- Estimated: 30-40 hours

**Load Tests**
- ✅ Sustained load handling
- ✅ Burst traffic patterns
- ✅ Concurrent Lambda execution
- ✅ SQS batch processing optimization
- Estimated: 20-30 hours

**Chaos Testing**
- ✅ Partial batch failures
- ✅ Timeout scenarios
- ✅ DLQ handling
- ✅ Retry exhaustion
- ✅ Service unavailability
- Estimated: 15-20 hours

### Test Infrastructure

**LocalStack Setup:**
```yaml
# docker-compose.yml for integration tests
services:
  localstack:
    image: localstack/localstack:latest
    environment:
      - SERVICES=lambda,sqs,sns,dynamodb,s3,eventbridge,kafka
      - DEBUG=1
    ports:
      - "4566:4566"
```

**Benchmark Suite:**
- BenchmarkDotNet for micro-benchmarks
- Lambda cold start measurement harness
- Cost estimation based on execution time
- Comparison reports (before/after optimization)

### Testing Checklist for Each Package

- [ ] Unit test coverage ≥80%
- [ ] Integration tests with LocalStack
- [ ] Performance benchmark baseline
- [ ] Load test (1000 msgs/sec minimum)
- [ ] Error scenario coverage
- [ ] Documentation includes test examples
- [ ] CI/CD pipeline runs all tests
- [ ] Test results published to dashboard

---

## Documentation Requirements

### Critical Documentation Gaps

**User Documentation:**
- [ ] Getting started guide for each event source
- [ ] IAM permissions reference (minimal permissions for each adapter)
- [ ] CloudFormation/SAM template examples
- [ ] CDK construct examples (TypeScript + C#)
- [ ] Migration guide from raw Lambda to Benzene
- [ ] Best practices guide (costs, performance, security)
- [ ] Troubleshooting guide (common errors) — not a dedicated guide, but partially covered:
      `docs/getting-started-aws.md` has its own "Troubleshooting" section, and
      `docs/cookbooks/handling-sqs-failures.md` has one per the standard cookbook structure
- [ ] FAQ for each adapter

**Developer Documentation:**
- [ ] Architecture decision records (ADRs)
- [ ] Contributing guide for AWS packages
- [ ] Adding new event source guide
- [x] Testing guide (LocalStack, mocking) (completed 2026-07-13 —
      `docs/cookbooks/testing-lambda-functions.md` covers in-memory `BenzeneTestHost`-based
      testing of API Gateway + SQS end to end; `docs/testing-benzene.md` was also restructured
      the same day with a `BenzeneTestHost`-first AWS section. LocalStack integration testing
      itself was already covered by `test/Benzene.Aws.Tests` + CI, completed 2026-07-12)
- [ ] Release process for AWS packages
- [ ] Compatibility matrix (AWS SDK versions, .NET versions)

**API Documentation:**
- [ ] XML documentation for all public APIs
- [ ] Generated API docs (DocFX or similar)
- [ ] Code examples in XML docs
- [ ] Parameter validation documentation
- [ ] Exception documentation

**Operations Documentation:**
- [ ] Monitoring and alerting setup
- [ ] CloudWatch metrics and logs
- [ ] X-Ray tracing configuration
- [ ] Cost optimization guide
- [ ] Scaling considerations
- [ ] Multi-region deployment patterns
- [ ] Disaster recovery patterns

### Documentation Structure

```
docs/aws/
├── getting-started/
│   ├── api-gateway.md
│   ├── sqs.md
│   ├── sns.md
│   ├── eventbridge.md
│   ├── kafka.md
│   └── quickstart.md
├── architecture/
│   ├── event-routing.md
│   ├── middleware-pipeline.md
│   ├── cold-start-optimization.md
│   └── adr/  (Architecture Decision Records)
├── reference/
│   ├── iam-permissions.md
│   ├── configuration.md
│   ├── error-codes.md
│   └── api/  (generated docs)
├── examples/
│   ├── cloudformation/
│   ├── sam/
│   ├── cdk/
│   └── serverless-framework/
├── operations/
│   ├── monitoring.md
│   ├── logging.md
│   ├── tracing.md
│   ├── cost-optimization.md
│   └── scaling.md
├── migration/
│   ├── from-raw-lambda.md
│   ├── from-0.x-to-1.0.md
│   └── breaking-changes.md
└── troubleshooting.md
```

### IAM Permissions Reference

**Example Documentation Needed:**
```markdown
# IAM Permissions for Benzene.Aws.Lambda.Sqs

## Minimal Permissions
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "sqs:ReceiveMessage",
        "sqs:DeleteMessage",
        "sqs:GetQueueAttributes"
      ],
      "Resource": "arn:aws:sqs:*:*:my-queue-name"
    }
  ]
}
```

## With Dead Letter Queue
... (additional permissions)
```

---

## Performance & Optimization

### Current Performance Metrics
- ❌ **No baseline measurements exist**
- ❌ No cold start benchmarks
- ❌ No warm invocation latency data
- ❌ No throughput measurements
- ❌ No memory usage profiling

### Performance Goals

**Cold Start (P99):**
- API Gateway Lambda: <1000ms
- SQS Lambda: <800ms
- SNS Lambda: <800ms
- EventBridge Lambda: <800ms
- Kafka Lambda: <1200ms

**Warm Invocation (P99):**
- All adapters: <50ms overhead vs. raw Lambda

**Throughput:**
- SQS batch processing: 1000+ messages/second
- API Gateway: 500+ requests/second per Lambda
- SNS: 1000+ messages/second
- Kafka: 5000+ messages/second per partition

**Memory:**
- Overhead: <50MB beyond minimal Lambda
- No memory leaks in long-running scenarios

### Optimization Strategies

**1. Cold Start Optimization**
- [ ] Lazy initialization of heavy dependencies
- [ ] AOT compilation exploration (.NET Native AOT)
- [ ] Dependency trimming (remove unused assemblies)
- [ ] Startup code profiling
- [ ] Lambda SnapStart preparation (when .NET supported)
- [ ] Provisioned concurrency guidance

**2. Warm Invocation Optimization**
- [ ] Object pooling for frequently allocated objects
- [ ] Reduce allocations in hot paths
- [ ] Async/await optimization
- [ ] Span<T> usage for string operations
- [ ] ArrayPool usage for buffer management

**3. Throughput Optimization**
- [ ] Batch processing optimization (SQS, Kafka)
- [ ] Parallel processing where safe
- [ ] Connection pooling (AWS SDK clients)
- [ ] HTTP/2 for API Gateway
- [ ] Optimal batch sizes documentation

**4. Memory Optimization**
- [ ] Memory leak detection
- [ ] GC tuning guidance
- [ ] Memory profiling tools
- [ ] Disposal pattern enforcement
- [ ] Large object heap management

### Benchmarking Suite

**Micro-Benchmarks (BenchmarkDotNet):**
```csharp
[Benchmark]
public async Task ApiGateway_ColdStart()
{
    // Measure cold start overhead
}

[Benchmark]
public async Task ApiGateway_WarmInvocation()
{
    // Measure warm invocation overhead
}

[Benchmark]
public async Task Sqs_BatchProcessing_100Messages()
{
    // Measure batch processing throughput
}
```

**Load Testing (Artillery/K6):**
- API Gateway: sustained load tests
- SQS: burst and sustained message processing
- End-to-end latency measurements
- Cost per million invocations

### Cost Optimization

**Current State:**
- No cost guidance documentation
- No cost estimation tools
- No optimization recommendations

**Cost Optimization Guide Needed:**
1. **Lambda Configuration**
   - Memory vs. execution time tradeoffs
   - Provisioned concurrency costs
   - ARM vs. x86 cost comparison

2. **Event Source Configuration**
   - SQS polling costs (long polling)
   - Batch size optimization
   - Reserved concurrency costs

3. **Observability Costs**
   - CloudWatch Logs costs
   - X-Ray sampling strategies
   - Metrics vs. logs tradeoffs

4. **Architecture Patterns**
   - Direct invocation vs. queues
   - Event batching strategies
   - Lambda vs. Fargate cost comparison

---

## Security & Best Practices

### Security Audit Checklist

**Input Validation:**
- [ ] All event sources validate input structure
- [ ] Deserialization security (no unsafe types)
- [ ] Size limits enforced (prevent DOS)
- [ ] Injection attack prevention (SQL, NoSQL, command)

**Authentication & Authorization:**
- [ ] IAM role best practices documented
- [ ] Least privilege principle enforcement
- [ ] Custom authorizer security patterns
- [ ] Cross-account access patterns
- [ ] Service-to-service authentication

**Data Protection:**
- [ ] Encryption at rest (SQS, SNS, Kafka)
- [ ] Encryption in transit (TLS 1.2+)
- [ ] Secrets management (Secrets Manager, Parameter Store)
- [ ] PII handling guidance
- [ ] Data retention policies

**Logging & Monitoring:**
- [ ] No secrets logged
- [ ] Structured logging for security events
- [ ] Audit trail for sensitive operations
- [ ] CloudTrail integration
- [ ] Anomaly detection guidance

**Dependency Security:**
- [ ] AWS SDK versions up-to-date
- [ ] Vulnerability scanning (Dependabot, Snyk)
- [ ] License compliance
- [ ] Supply chain security

### AWS Best Practices Implementation

**Lambda Best Practices:**
- [ ] Function timeout configuration guidance
- [ ] Reserved concurrency patterns
- [ ] VPC configuration (when needed)
- [ ] Environment variable encryption
- [ ] Layer usage for common dependencies
- [ ] X-Ray tracing enabled by default

**SQS Best Practices:**
- [ ] Dead letter queue configuration
- [ ] Message retention policies
- [ ] Visibility timeout optimization
- [ ] FIFO vs. standard queue guidance
- [ ] Message deduplication
- [ ] Queue encryption (SSE)

**SNS Best Practices:**
- [ ] Topic encryption
- [ ] Message filtering policies
- [ ] Retry policies
- [ ] DLQ for failed deliveries
- [ ] Fan-out pattern implementation

**EventBridge Best Practices:**
- [ ] Event schema versioning
- [ ] Archive and replay configuration
- [ ] Cross-region event routing
- [ ] Resource policies
- [ ] Event pattern optimization

**API Gateway Best Practices:**
- [ ] Request/response validation
- [ ] Throttling configuration
- [ ] API keys and usage plans
- [x] CORS configuration security (✅ RESOLVED 2026-07-14 — `docs/common-middleware.md`'s
      `UseCors` section documents the security-relevant behavior directly: exact
      scheme+host+port origin matching, why a literal `"*"` is never echoed back for
      credentialed requests, preflight header validation, and `Vary: Origin` cache
      correctness; see `Benzene.Http.Cors.CorsMiddleware<TContext>`)
- [ ] CloudFront integration
- [ ] WAF integration patterns

### Compliance & Governance

**Documentation Needed:**
- [ ] GDPR considerations (data handling)
- [ ] HIPAA compliance patterns
- [ ] PCI DSS compliance guidance
- [ ] SOC 2 audit trail configuration
- [ ] Data residency requirements

---

## Breaking Changes Pre-1.0

### Allowed Before 1.0 (Do Now)

**1. EventBridge Package Restructure** (CRITICAL) — ✅ DONE 2026-07-12
- ~~Rename S3* classes to EventBridge* OR~~
- ~~Create separate Benzene.Aws.Lambda.S3 package~~ ✅ renamed `Benzene.Aws.Lambda.EventBridge` → `Benzene.Aws.Lambda.S3`
- ~~Fix dependency (S3Events → CloudWatchEvents)~~ ✅ turned out unnecessary — `Amazon.Lambda.S3Events` was always the correct dependency for what the package actually does
- **Impact:** High - anyone using EventBridge package
- **Migration:** Automatic rename if classes renamed

**2. Standardize AWS SDK Versions** — ✅ DONE 2026-07-12 (one package missed and fixed 2026-07-13)
- ~~Update all AWSSDK.* packages to latest compatible versions~~ ✅ `AWSSDK.SQS` aligned to `3.7.502.57`
- **Impact:** Low - internal dependency change
- **Migration:** None required

**3. Remove AWSSDK.SQS from Benzene.Aws.XRay** — ✅ MOOT 2026-07-13
- ~~Remove unnecessary dependency~~ — `Benzene.Aws.XRay` was deleted entirely, not just cleaned up
- **Impact:** Low - unlikely anyone depends on this transitive dependency
- **Migration:** None required

**4. Rename SqsMessageClient.PublishAsync Parameters** — still open
- Change `status` parameter to `messageAttributes` (Dictionary) — `ISqsClient.PublishAsync(string topic, string message, string status)` is unchanged
- More flexible message attribute support
- **Impact:** Medium - anyone using SqsMessageClient
- **Migration:** Simple parameter name change

**5. Make SqsConsumer Configuration More Flexible** — ✅ DONE 2026-07-12
- ~~Move hard-coded values to SqsConsumerConfig~~ ✅ `WaitTimeSeconds` is now a `SqsConsumerConfig` property (default `1`, non-breaking)
- Add cancellation token support — not done; broadened exception handling (any `OperationCanceledException` exits cleanly, other exceptions are logged and polling continues) is a related but distinct improvement, also shipped 2026-07-12
- **Impact:** Low - likely few users of SqsConsumer
- **Migration:** Configuration object changes

**6. Improve ApiGatewayContext**
- Add convenience properties (Headers, QueryString, etc.)
- **Impact:** Low - additive change
- **Migration:** None required

**7. Exception Handling in SqsApplication** — ✅ PARTIALLY DONE 2026-07-12
- ~~Log exceptions instead of silently catching~~ ✅ done, via `ILogger` before the item is added to `BatchItemFailures`
- Add configurable exception handling strategy — not done, still a fixed behavior
- **Impact:** Medium - error handling behavior change
- **Migration:** May expose errors previously hidden

### Document in Migration Guide

**2026-07-12: no longer applicable.** No external adopters of the AWS packages exist
yet, so there's nothing to migrate and no guide to write. The changes below all
shipped (see the 2026-07-12 changelog at the top of this document) — kept here purely
as a historical record of what was tracked, in case a migration guide becomes relevant
again before 1.0 ships:

**Breaking Behavioral Changes (all shipped):**
1. ✅ SqsApplication now logs exceptions (previously silent)
2. ✅ EventBridge package renamed to `Benzene.Aws.Lambda.S3`
3. ✅ Some hard-coded values now configurable (`SqsConsumerConfig.WaitTimeSeconds`)

**New Required Dependencies (all shipped):**
- ✅ AWSSDK.SQS aligned to `3.7.502.57` across all packages

**Deprecated (Remove in 2.0):**
- TBD - no deprecations yet, clean slate for 1.0

---

## Dependencies & Compatibility

### AWS SDK Version Strategy

**Current Issues:** ✅ both resolved (see 2026-07-12/2026-07-13 changelogs)
- ~~Inconsistent AWSSDK.SQS versions (3.7.100.74 vs 3.7.2.63)~~ aligned to `3.7.502.57` everywhere
- ~~Old System.Text.Encodings.Web (6.0.0)~~ stale pin removed from all 7 affected packages

**Proposed Strategy:**
- Use latest stable AWS SDK packages at release time
- Pin to MAJOR.MINOR (e.g., 3.7.x) to allow patch updates
- Document minimum compatible versions
- Test with latest versions in CI/CD

**Compatibility Matrix:**
```markdown
| Benzene AWS | .NET | AWS SDK | Lambda Runtime |
|-------------|------|---------|----------------|
| 1.0.x       | 10.0 | 3.7.x   | dotnet8/10     |
| 0.9.x       | 10.0 | 3.7.x   | dotnet8/10     |
```

### Benzene Core Dependencies

**Current State:**
All AWS packages reference:
- Benzene.Abstractions.*
- Benzene.Core.*
- Benzene.Microsoft.Dependencies

**Strategy:**
- AWS 1.0 packages require Benzene Core 1.x
- Allow minor version upgrades within same major
- Document tested combinations

**Example:**
```xml
<PackageReference Include="Benzene.Core" Version="[1.0.0,2.0.0)" />
```

### Third-Party Dependencies

**Current:**
- Microsoft.Extensions.Configuration.Abstractions: 8.0.0 (was 5.0.0 when this table was written)
- Microsoft.Extensions.DependencyInjection: (from Core packages)
- ~~System.Text.Encodings.Web: 6.0.0~~ pin removed entirely 2026-07-12 (see changelog)

**Action Items:**
- [x] Update to Microsoft.Extensions.* 8.0+ (align with .NET 10) — `Microsoft.Extensions.Configuration.Abstractions` is now `8.0.0`
- [x] Update System.Text.Encodings.Web to 8.0+ — done differently than specified: the explicit pin was removed entirely rather than bumped, since it was vestigial and net10.0's shared framework already provides a newer version transitively (completed 2026-07-12)
- [ ] Document minimum version requirements

### Lambda Runtime Compatibility

**Target Runtimes:**
- dotnet8 (current AWS-managed runtime)
- dotnet10 (when available - custom runtime initially)

**Action Items:**
- [ ] Test with dotnet8 runtime
- [ ] Document custom runtime setup for .NET 10
- [ ] Create custom runtime layer for .NET 10
- [ ] Monitor AWS announcements for dotnet10 managed runtime

---

## Success Metrics

### Adoption Metrics (6 months post-1.0)

**NuGet Statistics:**
- Target: 1,000+ downloads total
- Target: 50+ dependent packages
- Target: 10+ contributors

**GitHub Metrics:**
- Target: 100+ stars
- Target: 20+ forks
- Target: 50+ issues/discussions
- Target: 10+ external contributors

### Quality Metrics

**Code Coverage:**
- Target: 80%+ unit test coverage
- Target: 60%+ integration test coverage
- Target: 100% of public APIs documented

**Performance:**
- Cold start: <1000ms P99
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
- Target: Active community discussions (weekly)

**Documentation:**
- Target: <5 "documentation unclear" issues per package
- Target: Getting-started guide completable in <30 minutes
- Target: Examples run successfully for 95%+ users

### Business Impact

**AWS Service Coverage:**
- Month 6: 9 event sources (current 8 + DynamoDB Streams)
- Month 12: 12 event sources (+ Kinesis, S3, ALB)
- Month 18: 15 event sources (+ AppSync, custom)

**Enterprise Adoption:**
- Target: 5+ enterprise teams using in production
- Target: 2+ case studies published
- Target: 1+ AWS partner blog post

---

## Prioritized Feature List

### Must Have for 1.0 (P0)

1. ~~**XML Documentation** - All packages (60-80h)~~ ✅ COMPLETE 2026-07-12
2. ~~**Fix EventBridge Package** - Critical bug (25-30h)~~ ✅ RENAMED to Benzene.Aws.Lambda.S3 2026-07-12
3. ~~**Unit Tests** - 80%+ coverage (40-50h)~~ ✅ COMPLETE 2026-07-12 (all 9 packages 90%+)
4. ~~**IAM Permissions Docs** - All event sources (15-20h)~~ ✅ COMPLETE 2026-07-12
   (`docs/aws-iam-permissions.md`)
5. ~~**Getting Started Guides** - All event sources (20-25h)~~ ✅ COMPLETE 2026-07-12
   (SNS/S3/Kafka snippets added to `docs/getting-started-aws.md`)
6. ~~**SAM Template Examples** - All event sources (15-20h)~~ ✅ COMPLETE 2026-07-12
   (`examples/Aws/Benzene.Examples.Aws/template.yaml`; SAM only, no CDK — see note below)
7. ~~**Integration Tests** - LocalStack (20-30h)~~ ✅ COMPLETE 2026-07-12 (wired into CI, passing)
8. ~~**Dependency Cleanup** - Standardize versions (8-12h)~~ ✅ COMPLETE 2026-07-12
9. ~~**Code Quality Fixes** - Error handling, config (15-20h)~~ ✅ SCOPED PORTION
   COMPLETE 2026-07-12 (2 real bugs fixed; virtual-constructor-call deliberately
   deferred — see package section for why)
10. ~~**Migration Guide** - 0.x to 1.0 (8-10h)~~ ❌ DESCOPED 2026-07-12 — no external
    adopters of the AWS packages exist yet (pre-1.0, no released version anyone
    depends on), so there's no one to migrate. Re-add if that changes before 1.0 ships.

**Total P0 Effort:** 0 hours remaining — every item on this list is now complete,
consciously deferred (needs a design/product decision, not more mechanical work), or
descoped as not applicable

### Should Have for 1.0 (P1)

1. **Performance Benchmarks** - All packages (20-30h)
2. **CDK Examples** - TypeScript + C# (15-20h)
3. **CloudFormation Examples** - All patterns (15-20h)
4. **Troubleshooting Guide** - Common issues (10-15h)
5. **Cost Optimization Guide** - All services (10-15h)
6. **Load Tests** - Throughput validation (15-20h)
7. **Security Audit** - Best practices (10-15h)
8. **API Reference Docs** - Generated (8-10h)

**Total P1 Effort:** 103-145 hours

### Nice to Have for 1.0 (P2)

1. **Pulumi Examples** - Infrastructure (10-15h)
2. **VS Code Snippets** - Code generation (8-10h)
3. **Video Tutorials** - Getting started (15-20h)
4. **Blog Posts** - Architecture deep dives (10-15h)
5. **Chaos Tests** - Resilience validation (10-15h)

**Total P2 Effort:** 53-75 hours

### Post-1.0 Features (P3)

1. **DynamoDB Streams** - Event source (30-40h)
2. **Kinesis Streams** - Event source (35-45h)
3. **S3 Events** - Event source (25-30h)
4. **ALB Target** - Event source (25-30h)
5. **AppSync** - GraphQL resolver (40-50h)
6. **Lambda Powertools** - Integration (20-30h)
7. **Cold Start Optimization** - AOT, etc. (35-45h)
8. **Step Functions** - Workflow integration (40-50h)
9. **VS Code Extension** - Dev tools (50-60h)
10. **CDK Constructs** - High-level components (35-45h)

**Total P3 Effort:** 335-425 hours

---

## Appendix A: File Reference

**Key Source Files:**
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Aws.Lambda.Core\AwsLambdaEntryPoint.cs`
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Aws.Lambda.Core\AwsLambdaStartUp.cs`
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Aws.Lambda.ApiGateway\ApiGatewayApplication.cs`
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Aws.Lambda.ApiGateway\ApiGatewayContext.cs`
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Aws.Lambda.Sqs\SqsApplication.cs`
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Aws.Lambda.Sns\SnsApplication.cs`
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Aws.Sqs\Client\SqsMessageClient.cs`
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Aws.Sqs\Consumer\SqsConsumer.cs`
- ~~`C:\Users\pelled\source\libs\Benzene\src\Benzene.Aws.XRay\Extensions.cs`~~ — deleted 2026-07-13, see changelog

**Related Documentation:**
- `C:\Users\pelled\source\libs\Benzene\work\1.0.0-release-status.md`
- `C:\Users\pelled\source\libs\Benzene\work\api-surface-review.md`
- `C:\Users\pelled\source\libs\Benzene\VERSIONING.md`
- `C:\Users\pelled\source\libs\Benzene\CHANGELOG.md`
- `C:\Users\pelled\source\libs\Benzene\docs\getting-started-aws.md`

---

## Appendix B: Comparison with Core 1.0

**Core Package 1.0 Criteria:**
Per `work/1.0.0-release-status.md`, core packages need:
1. ✅ Complete XML documentation
2. ✅ No test code in production packages (DONE for AWS)
3. ✅ No critical bugs
4. ✅ Versioning policy documented
5. ✅ Reasonable test coverage (>70%)
6. ✅ Up-to-date documentation
7. ✅ Working examples

**AWS Packages Current Status:**
1. ✅ 100% XML documentation (completed 2026-07-12; a 3-member regression on
   `AwsLambdaHost<TStartUp>`, added after that pass, found and fixed 2026-07-13)
2. ✅ Test helpers properly separated
3. ✅ EventBridge/S3 naming confusion resolved (renamed 2026-07-12); 3 further bugs
   found and fixed during the test-coverage pass (X-Ray timer crash, SNS client
   resolver bug, Lambda health check status bug — see 2026-07-12 update above); 1 more
   found but not fixed (`AddLambdaClients` DI registration gap — needs a design
   decision)
4. ✅ Versioning policy applies to all packages
5. ✅ Unit test coverage complete, 90%+ across all 8 remaining packages (was 9,
   `Benzene.Aws.XRay` deleted 2026-07-13 — see changelog), plus LocalStack
   integration tests wired into CI and passing (both completed 2026-07-12)
6. ✅ IAM permissions doc, getting-started guide expansion, and a SAM template all
   complete (completed 2026-07-12, guide further expanded 2026-07-13 with a
   `BenzeneTestHost` testing section); CDK example remains unbuilt, tracked as
   future work. New cookbooks (`handling-sqs-failures.md`, `sns-fan-out.md`,
   `testing-lambda-functions.md`) and a `docs/clients.md` reference doc added
   2026-07-13 close several previously-open documentation gaps — see changelog
7. ⚠️ Examples exist but a CDK template is not yet built (SAM is)
8. ✅ Dependency versions aligned and unused/stale references removed (completed
   2026-07-12, one missed package — `Benzene.Aws.Sqs.TestHelpers` — found and
   fixed 2026-07-13); AWSSDK v3→v4 and Amazon.Lambda.* major-version upgrades
   deliberately deferred as a separate, higher-risk decision
9. ✅ Two real code-quality bugs fixed (completed 2026-07-12): SqsApplication's
   swallowed exception, SqsConsumer's fragile catch + hardcoded wait time.
   Virtual-call-in-constructor deliberately left as an intentional, suppressed
   pattern — not a bug

**Gap Analysis:**
AWS packages are ~97% toward 1.0 readiness using core criteria (up from ~93% on
2026-07-12, ~96% before this pass). The P0 list is fully resolved (complete,
consciously deferred, or descoped as not applicable), and this 2026-07-13 pass
closed several previously-open P1 documentation items (SNS message-attribute/header
mapping and fan-out examples, SQS retry/DLQ patterns, a testing guide, and a
`Benzene.Clients.Aws` reference doc) plus standardized logging across every
package. What's left is either lower-priority P1/P2 work (a CDK example alongside
the SAM one, performance benchmarks) or decisions that need product/architecture
sign-off rather than more mechanical work: the `AddLambdaClients` DI gap, the AWSSDK
v3→v4 / Amazon.Lambda.* major-version upgrade, and whether to ever redesign
`AwsLambdaStartUp`'s construction/initialization split

---

## Appendix C: Risk Register

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| AWS SDK breaking changes | Medium | High | Pin versions, test updates before adopting |
| .NET 10 Lambda runtime delay | High | Medium | Document custom runtime, provide layer |
| Community adoption low | Medium | High | Marketing, blog posts, conference talks |
| Performance regressions | Low | High | Continuous benchmarking, before/after tests |
| Security vulnerability | Low | Critical | Dependency scanning, security audit, quick patching |
| EventBridge refactor scope creep | Medium | Medium | Clear requirements, time-box implementation |
| Documentation effort underestimated | High | Medium | Phased approach, prioritize critical docs |
| Test infrastructure costs | Low | Low | Use LocalStack, minimize AWS testing costs |
| Breaking changes post-1.0 | Low | Critical | Thorough review, beta testing, semver discipline |
| Dependency conflicts with Core | Medium | High | Coordinate releases, test combinations |

---

## Next Steps

**Immediate Actions (Week 1):**
1. Review this roadmap with stakeholders
2. Prioritize P0 features
3. Fix EventBridge package naming crisis
4. Set up LocalStack integration testing
5. Begin XML documentation (Core + ApiGateway packages)

**Short-Term (Month 1):**
1. Complete all P0 items for Lambda.Core and Lambda.ApiGateway
2. Publish first beta: Benzene.Aws.Lambda.* 1.0.0-beta.1
3. Gather community feedback
4. Create project board with issues for all roadmap items

**Decision Points:**
1. **EventBridge Strategy:** Rename OR create separate S3 package?
2. **1.0 Timing:** Ship with core 1.0 OR wait 3-6 months?
3. **Native AOT:** Investigate now OR defer to 1.1?
4. **Test Strategy:** LocalStack only OR real AWS sandbox?

---

**Document Owner:** AWS Product Team
**Reviewers:** Core Team, Community
**Approval Required:** Yes
**Next Review:** Monthly during 1.0 development

**Status:** DRAFT - Awaiting Approval
