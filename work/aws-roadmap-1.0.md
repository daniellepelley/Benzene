# Benzene AWS Packages - Roadmap to 1.0.0 and Beyond

**Document Version:** 1.4
**Last Updated:** 2026-07-12
**Owner:** AWS Product Team
**Status:** DRAFT for Review

> **2026-07-12 changelog** — P0 items resolved today, in order. Package/percentage
> details and discovered bugs are recorded inline at each item's section below rather
> than repeated here:
> 1. **XML Documentation** (P0 #1) — 100% coverage across all 9 AWS packages, 0 CS1591
>    warnings.
> 2. **EventBridge/S3 rename** (P0 #2) — `Benzene.Aws.Lambda.EventBridge` renamed to
>    `Benzene.Aws.Lambda.S3` (pure rename; the code was always S3 event handling, never
>    EventBridge). Real EventBridge/CloudWatch Events support remains unbuilt, tracked
>    separately (see package section 5).
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
>
> Remaining P0 work: Dependency Cleanup, Code Quality Fixes, Migration Guide (31-42h,
> see Prioritized Feature List below). Full narrative detail for each completed item
> remains in that item's own section further down this document.

---

## Executive Summary

This roadmap outlines the path to 1.0.0 for Benzene's AWS integration packages and defines the strategic direction for AWS-specific features over the next 12+ months. The AWS ecosystem within Benzene currently consists of **8 production packages** and **5 TestHelper packages** supporting Lambda, SQS, SNS, EventBridge, Kafka, and X-Ray.

### Current State
- **Package Count:** 9 AWS production packages, 5 TestHelpers
- **Version:** All at 0.0.1 (pre-release)
- **Target Framework:** .NET 10
- **Source Files:** ~179 AWS-related source files
- **Test Coverage:** ✅ 90%+ unit test coverage across all 9 packages, plus LocalStack integration tests passing in CI (both completed 2026-07-12)
- **Documentation:** ✅ 100% XML documentation (completed 2026-07-12), basic CLAUDE.md files exist
- **Maturity:** Functional but not production-ready for 1.0

### Key Findings
✅ **Strengths:**
- Clean, consistent architecture across all Lambda adapters
- Good separation of concerns (each event source = separate package)
- TestHelpers properly extracted to dedicated packages
- Working examples demonstrate real-world usage
- No TODO/FIXME/HACK comments found in codebase
- ✅ 100% XML documentation coverage across all 9 packages, zero CS1591 warnings (completed 2026-07-12)

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
| **Benzene.Aws.XRay** | 0.0.1 | AWS X-Ray distributed tracing | Low | ❌ Not ready |
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

**Issues:**
1. ⚠️ **Inconsistent AWSSDK.SQS versions** (3.7.100.74 vs 3.7.2.63)
2. ⚠️ **EventBridge package references S3Events** instead of CloudWatchEvents
3. ⚠️ Old `System.Text.Encodings.Web` version (6.0.0) - should align with .NET 10

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
- [ ] Add comprehensive XML documentation
- [ ] Improve error messages with actionable guidance
- [ ] Add startup time logging
- [ ] Document cold-start best practices
- [ ] Add Lambda runtime initialization hooks
- [ ] Create migration guide from bare-metal to StartUp pattern
- [ ] Add examples of custom IAwsEntryPointBuilder implementations

**Estimated Effort:** 15-20 hours

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
- **CORS:** `ApiGatewayContextCorsMiddleware` + extensions
- **Custom Authorizer:** Full custom authorizer support
- Various registration and extension classes

**Strengths:**
- Most feature-complete AWS adapter
- CORS support built-in
- Custom authorizer implementation
- Clean HTTP abstraction mapping
- Supports both REST API and HTTP API formats

**Issues:**
1. ❌ No XML documentation
2. ❌ ApiGatewayContext (line 6) is too simple - missing request/response properties
3. ⚠️ No guidance on binary content handling (base64)
4. ⚠️ No multi-value header/query string examples
5. ⚠️ CORS configuration not documented
6. ⚠️ Custom authorizer IAM policy generation not documented
7. ⚠️ No OpenAPI/Swagger integration examples

**1.0 Requirements:**
- [ ] Add comprehensive XML documentation
- [ ] Expand ApiGatewayContext with convenience properties
- [ ] Document CORS setup with examples
- [ ] Document custom authorizer patterns
- [ ] Add binary content handling guide
- [ ] Add OpenAPI integration example
- [ ] Document API Gateway request/response limits
- [ ] Add IAM policy examples for authorizers
- [ ] Performance testing for cold starts
- [ ] Document differences between REST API v1 and HTTP API v2

**Estimated Effort:** 20-25 hours

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
- [ ] Add comprehensive XML documentation
- [ ] Improve exception logging in batch processing
- [ ] Document DLQ configuration patterns
- [ ] Document FIFO queue usage
- [ ] Add retry and backoff strategies
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
- [ ] Add comprehensive XML documentation
- [ ] Document subscription confirmation flow
- [ ] Add message filtering policy examples
- [ ] Document raw vs. wrapped message delivery
- [ ] Add SNS FIFO topic support documentation
- [ ] Document message attributes vs. headers mapping
- [ ] Add fan-out architecture examples
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
> Real EventBridge/CloudWatch Events support **does not exist** anywhere in Benzene.
> Building it is now tracked as a distinct, unstarted future package (working name
> `Benzene.Aws.Lambda.EventBridge`, to be created from scratch with
> `Amazon.Lambda.CloudWatchEvents`) — see the "Post-1.0 Features" / medium-term roadmap
> sections for the EventBridge-specific line items originally listed here, which now
> describe that future work rather than this package.

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
- [ ] Add comprehensive XML documentation
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
- [ ] Add comprehensive XML documentation
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

### 8. Benzene.Aws.XRay 📊 Distributed Tracing

**Location:** `src/Benzene.Aws.XRay/`
**Current State:** Low maturity, minimal implementation

**Public API Surface:**
- `Extensions.UseXRayTracing<TContext>()` (C:\Users\pelled\source\libs\Benzene\src\Benzene.Aws.XRay\Extensions.cs)
- `XRayProcessTimerFactory` - Timer factory
- `XRayProcessProcessTimer` - Process timer

**Strengths:**
- Simple integration point
- Wraps AWS X-Ray SDK

**Issues:**
1. ❌ No XML documentation
2. ⚠️ Too simplistic - only registers SDK handler (line 12 of Extensions.cs)
3. ⚠️ No segment/subsegment management
4. ⚠️ No annotation/metadata capture
5. ⚠️ No custom tracing middleware
6. ⚠️ Timer implementation not reviewed
7. ⚠️ No guidance on X-Ray sampling rules
8. ⚠️ No integration with Benzene's diagnostics
9. ⚠️ References AWSSDK.SQS which seems unnecessary

**1.0 Requirements:**
- [ ] Add comprehensive XML documentation
- [ ] Add segment/subsegment middleware
- [ ] Add annotation and metadata helpers
- [ ] Document X-Ray sampling configuration
- [ ] Add tracing best practices guide
- [ ] Integrate with Benzene.Diagnostics
- [ ] Add custom segment naming strategies
- [ ] Remove unnecessary AWSSDK.SQS dependency
- [ ] Add error and exception tracking
- [ ] Document cost optimization (sampling)
- [ ] Add service map visualization examples

**Estimated Effort:** 15-20 hours

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
- [ ] Add comprehensive XML documentation
- [ ] Clarify purpose vs. AWS SDK
- [ ] Add client usage examples
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
   - ~~Document every public type, method, property~~ ✅ Done for all 9 packages, 0 CS1591 warnings
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

4. **Dependency Cleanup** (8-12 hours)
   - Standardize AWSSDK versions
   - Remove unnecessary dependencies
   - Update System.Text.Encodings.Web to align with .NET 10

5. **Documentation** (30-40 hours)
   - Getting started guide for each adapter
   - IAM permissions documentation
   - CloudFormation/SAM/CDK templates
   - Architecture decision records
   - Migration guides

6. **Code Quality Fixes** (15-20 hours)
   - Improve error messages
   - Add missing error handling
   - Fix hard-coded values
   - Add configuration options
   - Remove constructor virtual calls

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

1. **AWS Lambda - DynamoDB Streams** (6-8 weeks)
   - Event source adapter
   - Change data capture patterns
   - DynamoDB Streams-specific middleware
   - Example: Event sourcing with DynamoDB
   - **Effort:** 30-40 hours

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
1. ⚠️ EventBridge package naming/dependency confusion
2. ⚠️ Inconsistent AWSSDK versions
3. ⚠️ Exception swallowing in SqsApplication
4. ⚠️ Virtual member calls in constructors
5. ⚠️ Hard-coded configuration values (e.g., WaitTimeSeconds = 1)
6. ⚠️ Unnecessary dependencies (XRay → AWSSDK.SQS)

**Medium Priority:**
1. ApiGatewayContext too simple - needs convenience properties
2. Error messages not actionable
3. No batch operations in SqsMessageClient
4. Consumer infinite loop without safeguards
5. No retry policies on client operations

**Low Priority:**
1. Code duplication across message getters/setters
2. Missing async suffix on some async methods
3. No nullable reference type annotations consistently
4. Test helper naming could be more consistent

### Code Quality Improvements

**Standardization:**
- [ ] Consistent error handling patterns
- [ ] Standardized logging approach
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
- ✅ Unit test coverage complete (2026-07-12): all 9 packages 90%+ (Core 93.2%,
  ApiGateway 91.5%, Sqs 94.8%, Sns 98.5%, S3 100%, Kafka 96.7%, Aws.Sqs 100%, XRay
  92.7%, Clients.Aws 90.6%), in `test/Benzene.Core.Test/Aws/`
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
- [ ] Troubleshooting guide (common errors)
- [ ] FAQ for each adapter

**Developer Documentation:**
- [ ] Architecture decision records (ADRs)
- [ ] Contributing guide for AWS packages
- [ ] Adding new event source guide
- [ ] Testing guide (LocalStack, mocking)
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
- [ ] CORS configuration security
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

**1. EventBridge Package Restructure** (CRITICAL)
- Rename S3* classes to EventBridge* OR
- Create separate Benzene.Aws.Lambda.S3 package
- Fix dependency (S3Events → CloudWatchEvents)
- **Impact:** High - anyone using EventBridge package
- **Migration:** Automatic rename if classes renamed

**2. Standardize AWS SDK Versions**
- Update all AWSSDK.* packages to latest compatible versions
- **Impact:** Low - internal dependency change
- **Migration:** None required

**3. Remove AWSSDK.SQS from Benzene.Aws.XRay**
- Remove unnecessary dependency
- **Impact:** Low - unlikely anyone depends on this transitive dependency
- **Migration:** None required

**4. Rename SqsMessageClient.PublishAsync Parameters**
- Change `status` parameter to `messageAttributes` (Dictionary)
- More flexible message attribute support
- **Impact:** Medium - anyone using SqsMessageClient
- **Migration:** Simple parameter name change

**5. Make SqsConsumer Configuration More Flexible**
- Move hard-coded values to SqsConsumerConfig
- Add cancellation token support
- **Impact:** Low - likely few users of SqsConsumer
- **Migration:** Configuration object changes

**6. Improve ApiGatewayContext**
- Add convenience properties (Headers, QueryString, etc.)
- **Impact:** Low - additive change
- **Migration:** None required

**7. Exception Handling in SqsApplication**
- Log exceptions instead of silently catching
- Add configurable exception handling strategy
- **Impact:** Medium - error handling behavior change
- **Migration:** May expose errors previously hidden

### Document in Migration Guide

**Breaking Behavioral Changes:**
1. SqsApplication now logs exceptions (previously silent)
2. EventBridge package renamed (if applicable)
3. Some hard-coded values now configurable

**New Required Dependencies:**
- Ensure AWSSDK.* packages are latest compatible versions

**Deprecated (Remove in 2.0):**
- TBD - no deprecations yet, clean slate for 1.0

---

## Dependencies & Compatibility

### AWS SDK Version Strategy

**Current Issues:**
- Inconsistent AWSSDK.SQS versions (3.7.100.74 vs 3.7.2.63)
- Old System.Text.Encodings.Web (6.0.0)

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
- Microsoft.Extensions.Configuration.Abstractions: 5.0.0
- Microsoft.Extensions.DependencyInjection: (from Core packages)
- System.Text.Encodings.Web: 6.0.0

**Action Items:**
- [ ] Update to Microsoft.Extensions.* 8.0+ (align with .NET 10)
- [ ] Update System.Text.Encodings.Web to 8.0+
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
8. **Dependency Cleanup** - Standardize versions (8-12h)
9. **Code Quality Fixes** - Error handling, config (15-20h)
10. **Migration Guide** - 0.x to 1.0 (8-10h)

**Total P0 Effort:** 31-42 hours remaining (Dependency Cleanup, Code Quality Fixes,
Migration Guide only — everything else on this list is now complete)

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
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Aws.XRay\Extensions.cs`

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
1. ✅ 100% XML documentation (completed 2026-07-12)
2. ✅ Test helpers properly separated
3. ✅ EventBridge/S3 naming confusion resolved (renamed 2026-07-12); 3 further bugs
   found and fixed during the test-coverage pass (X-Ray timer crash, SNS client
   resolver bug, Lambda health check status bug — see 2026-07-12 update above); 1 more
   found but not fixed (`AddLambdaClients` DI registration gap — needs a design
   decision)
4. ✅ Versioning policy applies to all packages
5. ✅ Unit test coverage complete, 90%+ across all 9 packages, plus LocalStack
   integration tests wired into CI and passing (both completed 2026-07-12)
6. ✅ IAM permissions doc, getting-started guide expansion, and a SAM template all
   complete (completed 2026-07-12); CDK example remains unbuilt, tracked as future work
7. ⚠️ Examples exist but a CDK template is not yet built (SAM is)

**Gap Analysis:**
AWS packages are ~85% toward 1.0 readiness using core criteria (up from ~75%).
Primary remaining gaps: Dependency Cleanup, Code Quality Fixes, Migration Guide, the
`AddLambdaClients` DI gap, and (lower priority) a CDK example alongside the SAM one

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
