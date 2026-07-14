# Benzene Observability Packages - Roadmap to 1.0.0 and Beyond

**Document Version:** 1.1
**Last Updated:** 2026-07-14
**Owner:** Observability Product Team
**Status:** DRAFT for Review

> **2026-07-14 changelog** — audit pass against actual code (this document was
> written 2026-07-11, before a series of major commits landed on 2026-07-12
> and 2026-07-13 — packaging hygiene, the logging-stack replacement, and the
> Diagnostics/OpenTelemetry "Checkpoint A-F" rework). In order of
> significance:
> 1. **Six of the twelve packages this roadmap tracks no longer exist.**
>    `Benzene.Microsoft.Logging`, `Benzene.Serilog`, and `Benzene.Log4Net` were
>    deleted entirely in commit `3f3b25d` ("Replace IBenzeneLogger stack with
>    Microsoft.Extensions.Logging", 2026-07-12) — the whole custom
>    `IBenzeneLogger`/`IBenzeneLogAppender`/`IBenzeneLogContext` stack was
>    replaced by plain `Microsoft.Extensions.Logging` (`ILogger<T>`)
>    throughout the framework, making the three provider-adapter packages
>    redundant (the commit message notes the Serilog registration was
>    "broken and unused" anyway). `Benzene.Datadog`, `Benzene.Zipkin`, and
>    `Benzene.Aws.XRay` were separately deleted in commit `1081bd1`
>    ("Delete Benzene.Datadog, Benzene.Zipkin, and Benzene.Aws.XRay
>    (Checkpoint B)", 2026-07-13), superseded by a unified `Benzene.OpenTelemetry`
>    package exporting `Benzene.Diagnostics`'s `ActivitySource`/`Meter` (both
>    named `"Benzene"`) via `AddBenzeneInstrumentation()`. **Sections 3-8 below
>    (Microsoft.Logging, Serilog, Log4Net, Datadog, Zipkin, Aws.XRay) describe
>    packages that no longer exist** — kept as historical record, marked
>    deleted, rather than removed outright. Current package count is **6**
>    (`Benzene.Diagnostics`, `Benzene.OpenTelemetry`, `Benzene.HealthChecks`,
>    `Benzene.HealthChecks.Core`, `Benzene.HealthChecks.Http`,
>    `Benzene.HealthChecks.EntityFramework`) — not 12. (A seventh package,
>    `Benzene.Clients.HealthChecks`, existed even before this roadmap was
>    written and was never in its inventory; it's a thin remote-health-check
>    client, out of scope for this pass since the roadmap never claimed
>    anything about it.)
> 2. **`Benzene.Diagnostics` was rebased onto real `System.Diagnostics.Activity`**
>    (commit `d66cc9d`, "Checkpoint A") and gained genuine correlation/context
>    propagation the original roadmap asked for as 1.0 requirements:
>    `ActivityMiddlewareWrapper` now auto-wraps *every* middleware in *every*
>    pipeline in an `Activity` span (no more bespoke per-vendor `IProcessTimer`
>    backends); `UseW3CTraceContext()`/`WithW3CTraceContext()` implement real
>    W3C `traceparent` propagation inbound (HTTP-based transports only) and
>    outbound (HTTP/SQS/SNS/Kafka clients, commit `6e88ad6`); `UseBenzeneMetrics()`
>    records real counter/histogram metrics on a shared `Meter`
>    (`BenzeneDiagnostics.MessagesProcessed`/`MessageDuration`); and
>    `UseBenzeneEnrichment()` attaches `invocationId`/`traceId`/`spanId`/`topic`/
>    `transport`/`handler` to the log scope portably across platforms. This
>    resolves this roadmap's "Correlation ID Integration" P0 item and most of
>    Diagnostics's/OpenTelemetry's individually-listed 1.0 requirements
>    (metrics support, common span attributes, no more `TracerProvider.Default`
>    usage — `AddBenzeneInstrumentation()` is a plain `TracerProviderBuilder`/
>    `MeterProviderBuilder` extension, DI-neutral by construction, not a
>    workaround for the deprecated static accessor).
> 3. **The DI captive-dependency bug this document didn't know about was found
>    and fixed** (commit `a9575a2`, 2026-07-13, after this roadmap was written):
>    `ActivityMiddlewareWrapper`/`DebugMiddlewareWrapper` were registered
>    `AddScoped` while being consumed by the singleton `DefaultMiddlewareFactory`
>    via `IEnumerable<IMiddlewareWrapper>` — a captive-dependency violation that
>    DI scope validation rejects. Both are stateless (`IServiceResolver` is a
>    `Wrap()` parameter, not a constructor dependency), so the fix was simply
>    registering them `AddSingleton`. Verified in current source
>    (`src/Benzene.Diagnostics/DependencyInjectionExtensions.cs`).
> 4. **"Missing PackageVersion in csproj" is moot for every package in this
>    document.** It was an accurate complaint when this roadmap was written
>    (2026-07-11), but commit `254dcd2` ("Add packaging hygiene...", the very
>    next day, 2026-07-12) removed all 59 per-project `PackageVersion` pins
>    repo-wide in favor of a root `Directory.Build.props` that reads a single
>    `version.txt` into `VersionPrefix` for every package at once; CI
>    overrides with `-p:PackageVersion=x.y.z` at publish time. Every "Missing
>    PackageVersion" line item below (in the Package Inventory table, Critical
>    Path Items, Breaking Changes, Technical Debt, and per-package sections)
>    is stale in the same way — there is no longer a per-package gap to fix,
>    and hasn't been since the day after this document's stated last-updated
>    date. Verified via `src/Directory.Build.props` (`IsPackable=true` for
>    everything under `src/`) and the root `Directory.Build.props`
>    (`VersionPrefix` from `version.txt`).
> 5. **XML documentation is now partial, not zero — but still far from the
>    AWS packages' 100%/0-CS1591 standard.** None of the 6 current
>    observability packages have `GenerateDocumentationFile` enabled in their
>    `.csproj` (verified — AWS packages have it, these don't), so there's no
>    compiler enforcement and no generated `.xml` doc file at all. Within
>    `Benzene.Diagnostics`, the files touched by the Checkpoint A-F rework
>    (`BenzeneDiagnostics.cs`, `EnrichmentExtensions.cs`, `MetricsExtensions.cs`,
>    `W3CTraceContextExtensions.cs`, `Correlation/Extensions.cs`,
>    `ActivityMiddlewareWrapper.cs`, `Timers/ActivityProcessTimer.cs`) and all
>    of `Benzene.OpenTelemetry`'s single file carry full `///` doc comments;
>    everything else in `Benzene.Diagnostics` (the older `TimerMiddleware`,
>    `CorrelationId.cs`, most of `Timers/*`, the decorator/wrapper classes) and
>    **all four `Benzene.HealthChecks*` packages (0 of 29 files have any `///`
>    comment)** remain fully undocumented. Verified by grepping every `.cs`
>    file in each package for `///`.
> 6. **Test coverage is real now, not "~6 test classes total across all
>    observability packages."** `test/Benzene.Core.Test/Diagnostics/` +
>    `Core/Diagnostics/` contain 6 test classes (`ActivityMiddlewareTest`,
>    `BenzeneEnrichmentTest`, `W3CTraceContextTest`, `BenzeneMetricsTest`,
>    `BenzeneInstrumentationTest` — which directly exercises
>    `Benzene.OpenTelemetry.AddBenzeneInstrumentation()` against a real
>    `TracerProviderBuilder`/`MeterProviderBuilder` — and `UseTimerTest`);
>    `test/Benzene.Core.Test/Plugins/HealthChecks/` contains 3 more
>    (`HealthCheckPipelineTest`, `HealthCheckNamerTests`, `HealthCheckTests`);
>    plus health-check-adjacent tests in the AWS/gRPC packages
>    (`HealthCheckProcessorTest`, `HealthCheckTest`, `AwsLambdaHealthCheckTest`,
>    `SqsHealthCheckTest`, `StepFunctionsHealthCheckTest`,
>    `BenzeneHealthCheckBridgeTest`). All 37 tests matching
>    `Diagnostics|HealthCheck` pass (`dotnet test ... --filter
>    "FullyQualifiedName~Diagnostics|FullyQualifiedName~HealthCheck"`, verified
>    this pass). `Benzene.HealthChecks.Http` and
>    `Benzene.HealthChecks.EntityFramework` specifically still have **no**
>    dedicated tests found anywhere in `test/`.
> 7. **A complete, working `Benzene.OpenTelemetry` example now exists**:
>    `examples/OpenTelemetry/` — a web UI (`wwwroot/index.html`) that sends
>    messages into a real Benzene pipeline, a `docker-compose.yaml` running
>    `grafana/otel-lgtm` (bundled OTLP collector + Tempo + Prometheus + Loki +
>    Grafana), and a detailed `README.md` walking through viewing traces and
>    metrics in Grafana, plus a distributed-trace demo via a `traceparent`
>    header. This resolves the roadmap's implicit "no OTel exporter example"
>    gap and the P2 "examples with popular exporters" item for OTLP
>    specifically (Jaeger/Zipkin exporter examples are still not present).
> 8. **`docs/monitoring.md` and the logging cookbooks are fully rewritten for
>    the current single-OTel-package, MEL-based-logging story** — no stale
>    references to per-vendor tracing packages or the old `IBenzeneLogger`
>    stack found. `docs/cookbooks/structured-logging-serilog.md` explicitly
>    documents that `Benzene.Serilog` no longer exists and shows the
>    replacement (`AddLogging(x => x.AddSerilog())`); `docs/migration-alpha-to-1.0.md`
>    has a full "Logging: IBenzeneLogger → Microsoft.Extensions.Logging"
>    section and a full "Logging & tracing infrastructure: Datadog/Zipkin/X-Ray
>    deleted, OpenTelemetry rebuilt" section covering everything this
>    roadmap's "Migration Guide" P0 item asked for (project-wide, not
>    observability-specific, but the observability content is complete and
>    accurate).
> 9. **One new, previously-unflagged issue found while auditing, fixed same-day.**
>    Building `Benzene.HealthChecks.EntityFramework` in isolation surfaced two NuGet
>    advisory warnings — `Microsoft.Extensions.Caching.Memory` 6.0.0 (high
>    severity, GHSA-qj66-m88j-hmgj) and `Npgsql` 5.0.7 (high severity,
>    GHSA-x9vc-6hfv-hg8c) — not previously called out anywhere in this
>    document. **Resolved 2026-07-14**: bumped `Microsoft.EntityFrameworkCore`
>    6.0.0→10.0.9 and `Npgsql.EntityFrameworkCore.PostgreSQL` 5.0.7→10.0.3
>    (matching the package's `net10.0` target framework); 0 advisory warnings on
>    rebuild, no source changes needed (only stable `DbContext`/
>    `Database.CanConnectAsync`/`GetAppliedMigrationsAsync` APIs in use), full test
>    suite still green (690 passed, 4 skipped). Every other mention of this issue
>    below (package section, Dependencies & Compatibility, etc.) should be read as
>    resolved even where not individually annotated.
> 10. **Not independently re-verified this pass** (flagging honestly rather
>     than guessing): NuGet/GitHub adoption metrics (Success Metrics section —
>     these are forward-looking targets, not current-state claims, so left
>     as-is); whether `Benzene.HealthChecks.Core`'s "no integration with
>     Microsoft.Extensions.Diagnostics.HealthChecks" claim still holds (spot
>     checked the package's public surface, found no such integration, but did
>     not exhaustively search for a bridge package elsewhere in the repo);
>     performance/overhead numbers (this document's own admission that none
>     exist is still accurate — no benchmark project or recorded numbers found
>     anywhere in `src/` or `test/`).
>
> Anything in the sections below not explicitly called out above (e.g. most
> of the Long-Term Vision, Security & Privacy checklists, Success Metrics
> targets) was reviewed but found to still be forward-looking/aspirational
> content rather than a claim about current state, and is left unchanged.

> **2026-07-14 follow-up (same day, second pass)** — closed the "HealthChecks packages still 0%"
> gap this document repeatedly flagged (Executive Summary, P0 items 2 and 4, package sections 11-12,
> Appendix B, Next Steps). All four HealthChecks packages (`Benzene.HealthChecks.Core`,
> `Benzene.HealthChecks`, `Benzene.HealthChecks.Http`, `Benzene.HealthChecks.EntityFramework` — 29
> files total) now have complete XML documentation, verified via `GenerateDocumentationFile=true` and
> a clean rebuild showing 0 CS1591/CS1574 warnings on each. `Benzene.HealthChecks.Http` and
> `.EntityFramework` — the two packages this document specifically called out as having zero dedicated
> tests — now have real test coverage (`test/Benzene.Core.Test/HealthChecks/{Http,EntityFramework}/`,
> 10 new tests). Two real bugs were found and fixed while documenting: `DatabaseConnectionHealthCheck`
> stored a whole `(bool, Exception)` tuple instead of just the bool in its result's diagnostic data
> (copy-paste omission vs. its sibling `DatabaseHealthCheck`), and `Benzene.HealthChecks/Extensions.cs`'s
> `UseHealthCheck` middleware fired `SetResultAsync` without awaiting it (same missing-`await` pattern
> found and fixed elsewhere in the codebase this session). Also corrected 4 overclaiming CLAUDE.md
> files across the HealthChecks packages (claims of a dedicated HTTP `/health` endpoint with 200/503
> status mapping, readiness/liveness endpoints, "Healthy/Degraded/Unhealthy" status values, and
> configurable timeout/retry support — none of which exist; the actual status values are
> `Ok`/`Warning`/`Failed`, and timeout/exception handling is a 10-second hardcoded decorator applied
> only by `Benzene.HealthChecks`'s aggregator, not by the individual check implementations).
> `dotnet test test/Benzene.Core.Test/Benzene.Test.csproj` is green (738 passed, 4 skipped).
> **Genuinely still open** (not touched this pass): performance benchmarks, privacy/GDPR docs,
> sensitive-data filtering, sampling-strategy docs, a dedicated security audit, and the readiness/
> liveness distinction this document's own vision called for (confirmed via this pass: doesn't exist
> anywhere in the current HealthChecks packages, not just under-documented).

---

## Executive Summary

This roadmap outlines the path to 1.0.0 for Benzene's observability integration packages and defines the strategic direction for observability features over the next 12+ months. The observability ecosystem within Benzene currently consists of **6 production packages** supporting diagnostics/tracing/metrics (`Benzene.Diagnostics`, `Benzene.OpenTelemetry`) and health checks (`Benzene.HealthChecks`, `.Core`, `.Http`, `.EntityFramework`). **`Benzene.Microsoft.Logging`, `Benzene.Serilog`, and `Benzene.Log4Net` were deleted 2026-07-12** (the custom `IBenzeneLogger` stack was replaced by plain `Microsoft.Extensions.Logging` throughout the framework, making the adapter packages redundant), and **`Benzene.Datadog`, `Benzene.Zipkin`, and `Benzene.Aws.XRay` were deleted 2026-07-13** (superseded by `Benzene.OpenTelemetry`'s unified `AddBenzeneInstrumentation()`) — see the 2026-07-14 changelog above for full detail. Distributed tracing is now standards-based only (OpenTelemetry via real `System.Diagnostics.Activity` spans); logging is now plain `ILogger<T>` with no Benzene-specific provider packages needed.

### Current State
- **Package Count:** 6 observability packages (`Benzene.Diagnostics`, `Benzene.OpenTelemetry`, 4 HealthChecks packages) — down from 12; six packages this document originally tracked (`Benzene.Microsoft.Logging`, `Benzene.Serilog`, `Benzene.Log4Net`, `Benzene.Datadog`, `Benzene.Zipkin`, `Benzene.Aws.XRay`) no longer exist (see 2026-07-14 changelog)
- **Version:** ~~All at 0.0.1 (pre-release), except Benzene.OpenTelemetry (no version)~~ ✅ MOOT 2026-07-14 audit — versioning is centralized via root `Directory.Build.props` + `version.txt` (`0.0.2` currently) for every package repo-wide; there is no per-package version to be "missing" or inconsistent
- **Target Framework:** .NET 10 (confirmed via `.csproj` `TargetFramework`)
- **Source Files:** ~34 source files across the 6 remaining packages (24 in Diagnostics, 1 in OpenTelemetry, 9 across the 4 HealthChecks packages) — not recounted precisely against the original "~80" figure since that figure included the now-deleted packages
- **Test Coverage:** ✅ Real, not minimal — 9 dedicated test files for Diagnostics/OpenTelemetry/HealthChecks-core in `test/Benzene.Core.Test/{Diagnostics,Core/Diagnostics,Plugins/HealthChecks}/`, plus 6 more health-check-adjacent test classes elsewhere in the suite; 37 tests pass (verified via `dotnet test ... --filter "FullyQualifiedName~Diagnostics|FullyQualifiedName~HealthCheck"`, this pass). `Benzene.HealthChecks.Http`/`.EntityFramework` still have zero dedicated tests.
- **Documentation:** Partial, not 0% — the Checkpoint A-F files in `Benzene.Diagnostics` and all of `Benzene.OpenTelemetry` carry full XML doc comments; the rest of `Benzene.Diagnostics` and all 4 `Benzene.HealthChecks*` packages (0/29 files) remain undocumented, and none of the 6 packages have `GenerateDocumentationFile` enabled (unlike the AWS packages, which enforce 0 CS1591). CLAUDE.md files exist and are accurate/up to date for the 6 current packages.
- **Maturity:** Functional but not production-ready for 1.0 — accurate, still true

### Key Findings
✅ **Strengths:**
- Clean, focused architecture for each observability concern
- Good separation: diagnostics/tracing/metrics vs. health checks
- No TODO/FIXME/HACK comments found
- CLAUDE.md documentation exists for all 6 current packages, and is accurate
- ~~Working integration tests for Zipkin~~ — Zipkin package and its tests are both deleted; replaced by `BenzeneInstrumentationTest`, which exercises `Benzene.OpenTelemetry.AddBenzeneInstrumentation()` against a real `TracerProviderBuilder`/`MeterProviderBuilder`
- Minimal, focused implementations (not over-engineered)
- Diagnostics is now built on real `System.Diagnostics.Activity` spans, not a bespoke per-vendor `IProcessTimer` abstraction — standards-based by construction
- Health checks framework is well-designed
- ✅ **New since this roadmap was written:** real W3C `traceparent` propagation (inbound HTTP-based transports, outbound HTTP/SQS/SNS/Kafka clients), real metrics (`UseBenzeneMetrics()`), a portable `UseBenzeneEnrichment()` log/trace enrichment middleware, and a captive-dependency DI bug fix in `AddDiagnostics()` (`AddScoped` → `AddSingleton`)

❌ **Critical Blockers for 1.0:**
- **ZERO XML documentation** — ⚠️ now only true for the 4 `Benzene.HealthChecks*` packages; `Benzene.Diagnostics`/`Benzene.OpenTelemetry` have partial coverage on their newer files (see Documentation above)
- Minimal test coverage — ⚠️ RESOLVED for Diagnostics/OpenTelemetry/core HealthChecks (real tests exist, all passing); still true for `Benzene.HealthChecks.Http`/`.EntityFramework` specifically
- ~~OpenTelemetry package missing PackageVersion in csproj~~ ✅ MOOT — versioning is centralized, see Current State above
- ~~Aws.XRay has unnecessary AWSSDK.SQS dependency~~ ✅ MOOT — package deleted entirely
- ~~Old dependency versions (System.Text.Encodings.Web 6.0.0 in XRay)~~ ✅ MOOT — package deleted entirely
- No performance/overhead benchmarks for tracing middleware — still true, no benchmark project found anywhere in the repo
- Missing sampling/filtering strategies documentation — still true
- No context propagation testing across async boundaries — ⚠️ partially addressed: `W3CTraceContextTest` covers context propagation, but not specifically async-boundary edge cases
- Missing sensitive data filtering/masking guidance — still true
- ~~No integration with Benzene.Diagnostics correlation IDs for some packages~~ — moot in the old sense (no more per-vendor packages to integrate); W3C trace context propagation is now the primary cross-service correlation mechanism, correlation ID header support remains as an `[Obsolete]` legacy fallback
- ~~Limited metrics support~~ ✅ RESOLVED — `BenzeneDiagnostics.Meter`/`UseBenzeneMetrics()`/`AddBenzeneInstrumentation(MeterProviderBuilder)` provide real counter/histogram metrics, exported via OpenTelemetry
- No structured logging context propagation documentation — ✅ RESOLVED — `docs/monitoring.md`'s "Logging" and "Structured log scopes" sections document this in detail
- Missing privacy/GDPR considerations for logs and traces — still true, no such documentation found
- ✅ ~~New finding: `Benzene.HealthChecks.EntityFramework` carries two NuGet advisory warnings~~ **Fixed 2026-07-14** — see changelog item 9

### Recommended 1.0 Strategy

**CONSERVATIVE APPROACH (RECOMMENDED):**
Release observability packages in **phases** after core 1.0:
- **Phase 1 (with core 1.0):** Benzene.Diagnostics, Benzene.HealthChecks.Core (foundation packages)
- **Phase 2 (1-2 months post-core):** ~~Logging packages (Microsoft.Logging, Serilog), HealthChecks implementations~~ — the logging-package phase is now moot (no Benzene-specific logging packages exist to ship); HealthChecks implementations phase still applies
- **Phase 3 (3-4 months post-core):** ~~Tracing packages (OpenTelemetry, Datadog, Zipkin, XRay)~~ — narrows to just OpenTelemetry, the only tracing package remaining

**Rationale:**
- Diagnostics and health checks are foundational and well-understood
- ~~Logging packages are simpler and more stable~~ — no longer applicable; logging goes through plain `ILogger<T>` with no Benzene package to version/release
- Tracing packages need more work (overhead testing, sampling strategies, standards compliance) — still true, now scoped to just `Benzene.OpenTelemetry`
- Allows time for OpenTelemetry standards to evolve
- Reduces risk of breaking changes to observability APIs

**Timeline Estimate:** 2-4 months post core 1.0 for all observability packages at 1.0 — plausibly shorter now given the reduced package count, but not re-estimated as part of this docs-only audit

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
11. [Security & Privacy](#security--privacy)
12. [Breaking Changes Pre-1.0](#breaking-changes-pre-10)
13. [Dependencies & Compatibility](#dependencies--compatibility)
14. [Success Metrics](#success-metrics)

---

## Current State Assessment

### Package Inventory

| Package | Version | Purpose | Maturity | 1.0 Ready? |
|---------|---------|---------|----------|------------|
| **Benzene.Diagnostics** | centralized (0.0.2, see note) | Core diagnostics: `Activity` spans, timers, correlation, W3C trace context, metrics | Medium-High | ⚠️ Needs work |
| **Benzene.OpenTelemetry** | centralized (0.0.2) | Exports Diagnostics' `ActivitySource`/`Meter` to OTel providers | Medium | ⚠️ Needs work |
| ~~**Benzene.Microsoft.Logging**~~ | — | ✅ Deleted 2026-07-12 — superseded by plain `ILogger<T>` (see changelog) | — | N/A |
| ~~**Benzene.Serilog**~~ | — | ✅ Deleted 2026-07-12 — superseded by plain `ILogger<T>` + Serilog's own MEL provider | — | N/A |
| ~~**Benzene.Log4Net**~~ | — | ✅ Deleted 2026-07-12 — superseded by plain `ILogger<T>` + log4net's own MEL provider | — | N/A |
| ~~**Benzene.Datadog**~~ | — | ✅ Deleted 2026-07-13 — superseded by `Benzene.OpenTelemetry` + an OTel Datadog exporter | — | N/A |
| ~~**Benzene.Zipkin**~~ | — | ✅ Deleted 2026-07-13 — superseded by `Benzene.OpenTelemetry` + an OTel Zipkin exporter | — | N/A |
| ~~**Benzene.Aws.XRay**~~ | — | ✅ Deleted 2026-07-13 — superseded by `Benzene.OpenTelemetry` + an OTel X-Ray exporter | — | N/A |
| **Benzene.HealthChecks.Core** | centralized (0.0.2) | Health check abstractions | Medium-High | ⚠️ Needs work |
| **Benzene.HealthChecks** | centralized (0.0.2) | Health check implementations | Medium | ⚠️ Needs work |
| **Benzene.HealthChecks.Http** | centralized (0.0.2) | HTTP ping health checks | Medium | ⚠️ Needs work |
| **Benzene.HealthChecks.EntityFramework** | centralized (0.0.2) | Database health checks | Medium | ⚠️ Needs work (NuGet advisory warnings fixed 2026-07-14 — see changelog) |

> **Version note:** the "Version" column above no longer reflects a per-package
> value at all — every package under `src/` (including all 6 remaining
> observability packages) gets `VersionPrefix` from the single root
> `version.txt` (currently `0.0.2`) via `Directory.Build.props`. There is no
> such thing as a package "missing" a version or being on a different version
> than its siblings anymore.

### Code Quality Metrics

**Positive Indicators:**
- ✅ No TODO/FIXME/HACK comments found
- ✅ Consistent naming conventions
- ✅ Clean separation of concerns (each package focused)
- ✅ CLAUDE.md documentation exists for all 6 current packages, and is accurate/up to date
- ~~✅ IProcessTimer abstraction is well-designed~~ — still exists and still works (kept for source-compat with `UseTimer("name")` call sites), but is no longer the primary tracing mechanism; `System.Diagnostics.Activity` (via `ActivityMiddlewareWrapper`, automatic on every middleware) is
- ✅ Health checks framework is clean and extensible
- ✅ Correlation ID implementation is simple and effective (now `[Obsolete]` in favor of W3C trace context propagation, but still fully functional as a legacy fallback)
- ~~✅ Zipkin integration has working tests~~ — package and tests both deleted; replaced by `BenzeneInstrumentationTest` (`Benzene.OpenTelemetry`)

**Red Flags:**
- ⚠️ **0 XML documentation comments** — now only true for all 4 `Benzene.HealthChecks*` packages (0/29 files); `Benzene.Diagnostics`/`Benzene.OpenTelemetry` have partial coverage (verified via grep for `///` across every `.cs` file in both packages, this pass)
- ⚠️ Minimal test coverage (only ~6 test classes total) — ✅ RESOLVED for Diagnostics/OpenTelemetry/core HealthChecks (9 dedicated test files + 6 more health-check-adjacent classes, 37 tests passing); still true for `Benzene.HealthChecks.Http`/`.EntityFramework`
- ~~❌ OpenTelemetry package missing version in csproj~~ ✅ MOOT — centralized versioning, see table note above
- ~~❌ Microsoft.Logging, Serilog, Log4Net packages missing version in csproj~~ ✅ MOOT — packages deleted entirely
- ~~❌ HealthChecks packages all missing version in csproj~~ ✅ MOOT — centralized versioning, see table note above
- ~~❌ Aws.XRay has unnecessary AWSSDK.SQS dependency~~ ✅ MOOT — package deleted entirely
- ~~❌ Old System.Text.Encodings.Web 6.0.0 in XRay~~ ✅ MOOT — package deleted entirely
- ❌ No performance overhead benchmarks — still true, verified no benchmark project exists anywhere in `src/`/`test/`
- ❌ No sampling strategy documentation — still true
- ❌ No privacy/sensitive data handling guidance — still true
- 🆕 `Benzene.HealthChecks.EntityFramework` carries 2 NuGet high-severity advisory warnings (`Microsoft.Extensions.Caching.Memory` 6.0.0, `Npgsql` 5.0.7) — new finding, not previously listed anywhere in this document

### Dependency Analysis

**Tracing Provider Dependencies (current):**
```
OpenTelemetry                                1.16.0   (was 1.10.0 when this roadmap was written — updated)
OpenTelemetry.Api                            1.16.0   (was 1.10.0)
```
`Datadog.Trace`, `zipkin4net`, and `AWSXRayRecorder.Handlers.AwsSdk` no longer appear anywhere in the
repo — they were `Benzene.Datadog`/`Benzene.Zipkin`/`Benzene.Aws.XRay`'s only purpose, and all three
packages were deleted 2026-07-13. Distributed tracing is exclusively OpenTelemetry-based now; other
backends (Datadog, Zipkin, X-Ray) are reached through OTel exporters, not dedicated Benzene packages.

**Logging Provider Dependencies:**
```
Serilog                                      (version from project consuming it — via Serilog's own MEL provider, no Benzene package involved)
Microsoft.Extensions.Logging                 (framework-provided; this is now how Benzene logs, period — no Benzene.Microsoft.Logging package exists)
log4net                                      (version from project consuming it — via log4net's own MEL provider, no Benzene package involved)
```

**Issues:**
1. ~~⚠️ **Aws.XRay references AWSSDK.SQS 3.7.100.74** - unnecessary dependency~~ ✅ MOOT — package deleted entirely
2. ~~⚠️ **Old System.Text.Encodings.Web 6.0.0** in XRay~~ ✅ MOOT — package deleted entirely
3. ✅ **OpenTelemetry updated to 1.16.0** (was 1.10.0) — resolved, verified via `src/Benzene.OpenTelemetry/Benzene.OpenTelemetry.csproj`
4. ~~⚠️ **zipkin4net 1.5.0** - appears to be maintained but should verify~~ ✅ MOOT — package deleted entirely
5. ⚠️ **No explicit version constraints** on logging provider packages — not applicable in the old sense (no Benzene logging packages exist to constrain); still true that Benzene doesn't document a minimum/tested Serilog/log4net version for their respective MEL providers
6. 🆕 `Benzene.HealthChecks.EntityFramework` depends on `Microsoft.Extensions.Caching.Memory` 6.0.0 and `Npgsql` 5.0.7, both flagged with high-severity NuGet advisories (`dotnet build` output, this pass) — new finding

---

## Package-by-Package Analysis

### 1. Benzene.Diagnostics ⭐ Foundation Package

**Location:** `src/Benzene.Diagnostics/`
**Current State:** Medium-High maturity, core package

**Public API Surface:**
- **Timers:**
  - `IProcessTimer` - Timer interface with tag support
  - `IProcessTimerFactory` - Factory abstraction
  - `TimerMiddleware<TContext>` - Stopwatch-based middleware
  - `TimerMiddlewareDecorator<TContext>` - Decorator pattern
  - `TimerMiddlewareWrapper<TContext>` - Wrapper pattern
  - `CompositeProcessTimer` - Combine multiple timers
  - `CompositeProcessTimerFactory` - Composite factory
  - `DebugProcessTimer` - Debug output timer
  - `LoggingProcessTimer` - Logging-based timer
  - `NullProcessTimerFactory` - Null object pattern
- **Debugging:**
  - `DebugMiddlewareDecorator<TContext>` - Debug middleware
  - `DebugMiddlewareWrapper<TContext>` - Debug wrapper
- **Correlation:**
  - `CorrelationId` - Correlation ID implementation
  - Extensions for middleware integration
- **Registration:**
  - `DiagnosticsRegistrations` - DI extensions
  - `DependencyInjectionExtensions` - Service registration

> **2026-07-14 audit note:** This section describes the pre-Checkpoint-A state
> (`IProcessTimer`-centric, no `Activity`). As of commit `d66cc9d` (Checkpoint A)
> and follow-ups through `6e88ad6`, `Benzene.Diagnostics` was rebased onto real
> `System.Diagnostics.Activity` spans: `ActivityMiddlewareWrapper` now
> auto-wraps *every* middleware in *every* pipeline (registered by
> `AddDiagnostics()`, no explicit per-middleware call needed); `TimerMiddleware`/
> `IProcessTimer`/`IProcessTimerFactory` are kept for source-compat with
> existing `UseTimer("name")` call sites but are no longer the primary
> mechanism. New files not in the original API surface list below:
> `BenzeneDiagnostics.cs` (shared `ActivitySource`/`Meter`), `EnrichmentExtensions.cs`
> (`UseBenzeneEnrichment()`), `MetricsExtensions.cs` (`UseBenzeneMetrics()`),
> `W3CTraceContextExtensions.cs` (`UseW3CTraceContext()`),
> `Timers/ActivityProcessTimer.cs` (the new default `IProcessTimerFactory`).
> Most of the "Issues"/"1.0 Requirements" lists below are now stale or
> partially resolved — corrected inline.

**Strengths:**
- Clean abstraction with IProcessTimer/IProcessTimerFactory
- Composite pattern allows multiple simultaneous timers
- Correlation ID implementation is straightforward
- Decorator/wrapper patterns enable flexible composition
- No external dependencies (only Benzene core packages)
- 🆕 Now built on real `System.Diagnostics.Activity` spans (standards-based, not a bespoke abstraction)
- 🆕 Real W3C `traceparent`/`tracestate` propagation (inbound HTTP-based transports; outbound via `Benzene.Clients.TraceContext`)
- 🆕 Real metrics via `BenzeneDiagnostics.Meter`/`UseBenzeneMetrics()`
- 🆕 Captive-dependency DI bug fixed (`AddScoped` → `AddSingleton` for the two wrapper types, commit `a9575a2`)

**Issues:**
1. ⚠️ No XML documentation — partial now: `BenzeneDiagnostics.cs`, `EnrichmentExtensions.cs`, `MetricsExtensions.cs`, `W3CTraceContextExtensions.cs`, `Correlation/Extensions.cs`, `ActivityMiddlewareWrapper.cs`, `Timers/ActivityProcessTimer.cs` are documented (7/24 files); the rest (including `CorrelationId.cs`, most of `Timers/*`, the decorator/wrapper classes) is not. No `GenerateDocumentationFile` in the csproj either, so there's no compiler-enforced floor.
2. ⚠️ TimerMiddleware uses simple Action<TContext, long> - could be more structured — still true, unchanged
3. ⚠️ No built-in high-resolution timer option (Stopwatch is good, but could document precision) — still true
4. ✅ RESOLVED: Correlation ID is not automatically propagated to tracing providers — W3C trace context (`UseW3CTraceContext()`/`WithW3CTraceContext()`) is now the primary cross-service correlation mechanism and is wired for HTTP-based transports inbound, HTTP/SQS/SNS/Kafka clients outbound; the header-based `ICorrelationId`/`UseCorrelationId()` remains as an `[Obsolete]` legacy fallback, not the primary mechanism anymore
5. ⚠️ No guidance on async context flow for correlation IDs — `docs/monitoring.md` documents the W3C trace context model but doesn't specifically address async-boundary edge cases
6. ⚠️ Debug middleware could expose sensitive data - needs warnings — still true, unchanged (`DebugMiddlewareWrapper` is `Debug.WriteLine`-only, unrelated to `Activity` tracing)
7. ⚠️ No sampling/filtering for high-volume scenarios — still true
8. ⚠️ Timer tags are string-only (no typed values) — still true for the legacy `IProcessTimer` path; `Activity`/`TagList`-based tagging (used by `UseBenzeneMetrics()`) supports typed values

**1.0 Requirements:**
- [ ] Add comprehensive XML documentation (partial — see Issue 1 above; ~30% of files done)
- [ ] Document timer precision and overhead
- [x] Add correlation ID propagation to all tracing providers (superseded/resolved — W3C `traceparent` propagation via `UseW3CTraceContext()`/`WithW3CTraceContext()` now serves this role; verified in `src/Benzene.Diagnostics/W3CTraceContextExtensions.cs` and `src/Benzene.Clients/TraceContext/`)
- [ ] Add async context flow documentation
- [ ] Add sampling/filtering strategies
- [ ] Document debug middleware security considerations
- [ ] Add typed tag support or guidance
- [ ] Create performance benchmarks
- [ ] Add examples of timer composition
- [ ] Document best practices for production use

**Estimated Effort:** ~~20-25 hours~~ 15-18 hours remaining (correlation ID propagation item is done; XML docs partially done)

---

### 2. Benzene.OpenTelemetry ⭐ Modern Standard

**Location:** `src/Benzene.OpenTelemetry/`
**Current State:** Medium maturity, standards-based — rewritten 2026-07-13 (commit `11fc7b6`, "Make Benzene.OpenTelemetry the real instrumentation package")

> **2026-07-14 audit note:** This section describes a pre-rewrite version of
> the package (`OpenTelemetryProcessTimer`/`OpenTelemetryProcessTimerFactory`
> don't exist in current source). The package was rewritten to be a thin,
> DI-neutral extension layer over OTel's own builder types instead of a
> vendor-backend `IProcessTimerFactory` implementation — see corrected API
> surface and issues below.

**Public API Surface (current):**
- `AddBenzeneInstrumentation(this TracerProviderBuilder)` - calls `.AddSource("Benzene")`, exporting every `Activity` span `AddDiagnostics()` produces
- `AddBenzeneInstrumentation(this MeterProviderBuilder)` - calls `.AddMeter("Benzene")`, exporting `BenzeneDiagnostics.MessagesProcessed`/`MessageDuration`
- No `OpenTelemetryProcessTimer`/`OpenTelemetryProcessTimerFactory` — removed as part of the rewrite; the package registers no Benzene DI services and replaces no `IProcessTimerFactory`

**Strengths:**
- OpenTelemetry is vendor-agnostic standard
- Simple, focused implementation
- ✅ No longer uses `TracerProvider.Default` — `AddBenzeneInstrumentation()` is a plain extension on the caller-supplied `TracerProviderBuilder`/`MeterProviderBuilder`, DI-neutral by construction
- Works with any OpenTelemetry exporter
- 🆕 Now covers metrics as well as traces (both `TracerProviderBuilder` and `MeterProviderBuilder` overloads)
- 🆕 Directly tested: `BenzeneInstrumentationTest` builds a real `TracerProviderBuilder`/`MeterProviderBuilder` and asserts both succeed

**Critical Issues (corrected):**
1. ~~❌ Missing PackageVersion in csproj~~ ✅ MOOT — centralized versioning
2. ✅ RESOLVED: No XML documentation — the package's single file (`DependencyInjectionExtensions.cs`) has full `///` doc comments on both extension methods
3. ✅ RESOLVED: OpenTelemetry 1.10.0 → now 1.16.0 (verified in `.csproj`)
4. ✅ RESOLVED: Uses deprecated TracerProvider.Default.GetTracer() — no longer applies; the rewritten package never touches `TracerProvider.Default`, it only extends the builder types
5. ✅ RESOLVED: No metrics support — `AddBenzeneInstrumentation(MeterProviderBuilder)` + `Benzene.Diagnostics.MetricsExtensions.UseBenzeneMetrics()` provide real counter/histogram metrics
6. ⚠️ No log integration (OpenTelemetry has logging support) — still true; Benzene logs through plain `ILogger<T>`, no OTel Logs Bridge integration found
7. ⚠️ No span attributes for common Benzene context properties — partially resolved: `ActivityMiddlewareWrapper` tags spans with `benzene.transport`/`benzene.topic`/`benzene.version`/`benzene.handler` where resolvable (this lives in `Benzene.Diagnostics`, not `Benzene.OpenTelemetry`, but achieves the same outcome)
8. ⚠️ No sampling strategy configuration — still true, no sampling helpers in either package
9. ⚠️ No span events support — still true
10. ⚠️ No baggage/context propagation helpers — partially resolved: W3C `traceparent`/`tracestate` propagation exists (`UseW3CTraceContext()`), but W3C Baggage specifically is not implemented

**1.0 Requirements:**
- [x] ~~**CRITICAL:** Add PackageVersion to csproj~~ MOOT — centralized versioning
- [x] Add comprehensive XML documentation (done for this package's one file; N/A for the rest since there's only one file)
- [x] Update to latest OpenTelemetry SDK (1.16.0)
- [x] Replace TracerProvider.Default with DI-injected TracerProvider (superseded by the builder-extension rewrite — no `TracerProvider.Default` usage at all)
- [x] Add metrics support (IProcessTimer for metrics) — done differently than originally envisioned: real `Meter`-based metrics (`UseBenzeneMetrics()`) rather than an `IProcessTimer` metrics variant
- [ ] Add OpenTelemetry logging integration
- [x] Add common span attributes (topic, handler, result) (done via `ActivityMiddlewareWrapper`/`UseBenzeneMetrics()` tagging, in `Benzene.Diagnostics` rather than this package)
- [ ] Document sampling configuration
- [ ] Add span events for key lifecycle points
- [ ] Add baggage/context propagation utilities (W3C trace context done; W3C Baggage specifically not done)
- [x] Create examples with popular exporters (OTLP, Jaeger, Zipkin) — partially: `examples/OpenTelemetry/` has a full OTLP-exporter example (via `grafana/otel-lgtm`); no dedicated Jaeger/Zipkin exporter examples
- [ ] Document resource attributes (service name, version, etc.)
- [ ] Add performance benchmarks
- [ ] Document OpenTelemetry standards compliance

**Estimated Effort:** ~~30-40 hours~~ 10-15 hours remaining (most items resolved by the Checkpoint A-F rework; remaining gaps are logging integration, sampling docs, span events, baggage, resource attributes, and benchmarks)

---

### 3. ~~Benzene.Microsoft.Logging~~ 📝 .NET Standard Logging — ✅ DELETED 2026-07-12

**Location:** `src/Benzene.Microsoft.Logging/` — no longer exists (commit `3f3b25d`, "Replace
IBenzeneLogger stack with Microsoft.Extensions.Logging").

Every issue and requirement this section used to list (no XML docs, log level mapping,
structured logging examples, Application Insights guidance, correlation ID enrichment,
log category support) is now moot — there's no package left to fix. It wasn't replaced
by a better Microsoft-logging-specific package, it was **subsumed**: Benzene's framework
and handler code now inject `ILogger<T>`/`ILogger` directly (plain
`Microsoft.Extensions.Logging`), and `UsingBenzene(...)` calls `services.AddLogging()` for
you, so any standard `ILoggerFactory`/`ILoggerProvider` configuration (console, Application
Insights, etc.) just works with no Benzene-specific adapter needed. `AddMicrosoftLogger()` no
longer exists; it's simply not necessary anymore.

**Remaining work (if this is ever revisited):** none identified — this was a pure removal of
redundant surface area, not a deferred feature.

**Estimated Effort:** 0 hours (package deleted)

---

### 4. ~~Benzene.Serilog~~ 📝 Structured Logging — ✅ DELETED 2026-07-12

**Location:** `src/Benzene.Serilog/` — no longer exists (commit `3f3b25d`).

`SerilogBenzeneLogAppender`, `SerilogBenzeneLogContext`, and `CustomJsonFormatter` are gone.
The commit message notes the old Serilog registration was "broken and unused." Serilog
integration now goes through Serilog's own official `Serilog.Extensions.Logging` MEL
provider (`AddLogging(x => x.AddSerilog())`) — no Benzene-specific glue needed, since
Benzene's `UseLogResult`/`UseLogContext`/`UseBenzeneEnrichment` scope-enrichment middleware
attaches properties via the standard `ILogger.BeginScope`, which Serilog's provider already
maps onto its `LogContext`. This is documented in detail, including explicit "this package no
longer exists, here's the replacement" guidance, in
`docs/cookbooks/structured-logging-serilog.md`.

**Remaining work (if this is ever revisited):** none identified.

**Estimated Effort:** 0 hours (package deleted)

---

### 5. ~~Benzene.Log4Net~~ 📝 Enterprise Logging — ✅ DELETED 2026-07-12

**Location:** `src/Benzene.Log4Net/` — no longer exists (commit `3f3b25d`).

`Log4NetBenzeneLogAppender` and `AddLog4Net()` are gone. This section's own "DECISION:
Evaluate if Log4Net should remain or be marked as community-supported" item is resolved by
the deletion — the decision was effectively "remove the Benzene-specific package, keep
log4net usable via its own MEL provider" (`Microsoft.Extensions.Logging.Log4Net.AspNetCore`),
same pattern as Serilog above. Documented in `docs/migration-alpha-to-1.0.md`'s logging
migration table.

**Remaining work (if this is ever revisited):** none identified.

**Estimated Effort:** 0 hours (package deleted)

---

### 6. ~~Benzene.Datadog~~ 📊 Datadog APM — ✅ DELETED 2026-07-13

**Location:** `src/Benzene.Datadog/` — no longer exists (commit `1081bd1`, "Delete
Benzene.Datadog, Benzene.Zipkin, and Benzene.Aws.XRay (Checkpoint B)").

Every issue this section used to list (Datadog.Trace version, Agent config docs, service
name config, metrics support, log correlation, profiler integration) is moot — the package
and its `DatadogProcessTimer`/`DatadogProcessTimerFactory` are gone, superseded by
`Benzene.OpenTelemetry`'s `AddBenzeneInstrumentation()` plus an OTel Datadog exporter (Datadog
natively consumes OTel/ADOT exporters, so this is a strict simplification, not a capability
loss).

**Remaining work (if this is ever revisited):** a short "Exporting Benzene traces to Datadog"
cookbook, if this comes up in practice — no such cookbook currently exists, and
`docs/monitoring.md` documents the OTel export path generically without vendor-specific
walkthroughs.

**Estimated Effort:** 0 hours (package deleted); 2-3 hours if the cookbook above is picked up

---

### 7. ~~Benzene.Zipkin~~ 📊 Distributed Tracing — ✅ DELETED 2026-07-13

**Location:** `src/Benzene.Zipkin/` — no longer exists (commit `1081bd1`). Its integration
tests (`ZipkinPipelineTest`, referenced in this document's own Appendix A and Testing
Strategy sections as "working integration tests") were deleted with it.

`ZipkinProcessTimer`/`ZipkinProcessTimerFactory`, the hard-coded `"benzene"` service name,
and the `Trace.Current`-based async-context concerns this section used to flag are all moot
— superseded by `Benzene.OpenTelemetry` + an OTel Zipkin exporter. The replacement test
covering the analogous OTel wiring is `BenzeneInstrumentationTest`
(`test/Benzene.Core.Test/Diagnostics/`), which builds a real `TracerProviderBuilder`/
`MeterProviderBuilder` via `AddBenzeneInstrumentation()`.

**Remaining work (if this is ever revisited):** a short "Exporting Benzene traces to Zipkin"
cookbook and B3-propagation-specific documentation, if needed — not currently present.

**Estimated Effort:** 0 hours (package deleted); 2-3 hours if the cookbook above is picked up

---

### 8. ~~Benzene.Aws.XRay~~ ⚠️ AWS Tracing — ✅ DELETED 2026-07-13

**Location:** `src/Benzene.Aws.XRay/` — no longer exists (commit `1081bd1`).

This confirms and closes out this section's own "Missing from AWS roadmap analysis (should
be included there)" note — the AWS roadmap (`work/aws-roadmap-1.0.md`) now documents this
deletion in full detail in its own section 8 and 2026-07-13 changelog, including the
`AWSSDK.SQS`/`System.Text.Encodings.Web` dependency cleanup this section flagged as critical
(both moot now — the whole package is gone, not just its dependencies). The AWS roadmap
notes one piece of follow-up work neither roadmap has picked up yet: an integration test that
exports a span through the OTel Collector's AWS X-Ray exporter and confirms it's queryable in
X-Ray — the OTel wiring itself is unit-tested (`BenzeneInstrumentationTest`), but nothing in
CI currently verifies a span actually lands in X-Ray specifically.

**Remaining work (if this is ever revisited):** see `work/aws-roadmap-1.0.md` section 8 for
the authoritative, more detailed writeup (this package was AWS-specific and is tracked there
primarily; this roadmap notes it for tracing-strategy completeness only).

**Estimated Effort:** 0 hours (package deleted); 4-6 hours if the X-Ray-specific integration
test above is picked up

---

### 9. Benzene.HealthChecks.Core ⭐ Health Check Foundation

**Location:** `src/Benzene.HealthChecks.Core/`
**Current State:** Medium-High maturity, clean abstractions

**Public API Surface:**
- `IHealthCheck` - Health check interface
- `IHealthCheckResult` - Result interface
- `IHealthCheckResponse` - Response aggregation
- `IHealthCheckBuilder` - Builder pattern
- `IHealthCheckFactory` - Factory pattern
- `HealthCheckStatus` - Enum (Healthy, Degraded, Failed)
- `HealthCheckResult` - Result implementation
- `HealthCheckResponse` - Response implementation
- `HealthCheckBuilderExtensions` - Builder extensions

**Strengths:**
- Clean, focused abstractions
- Supports Healthy, Degraded, Unhealthy states
- Builder pattern for composition
- Factory pattern for DI
- Good separation from implementation

**Issues (2026-07-14 re-verification — API surface above still matches current source exactly):**
1. ~~❌ **Missing PackageVersion in csproj**~~ ✅ MOOT — centralized versioning, see Current State Assessment above
2. ❌ No XML documentation — still true, confirmed 0/9 files in this package have any `///` comment; no `GenerateDocumentationFile` in the csproj either
3. ⚠️ No timeout support in interface (implemented in Benzene.HealthChecks) — still true (`TimeOutHealthCheck` in `Benzene.HealthChecks` hard-codes a 10000ms delay, not interface-level or configurable)
4. ⚠️ No tags/labels for health check categorization — still true
5. ⚠️ No dependency graph for health checks — still true
6. ⚠️ No critical vs non-critical distinction — still true
7. ⚠️ No integration with standard Microsoft.Extensions.Diagnostics.HealthChecks — spot-checked, still true; no such bridge found in `src/Benzene.HealthChecks.Core/` or elsewhere in the repo (not exhaustively searched beyond this package's own surface)

**1.0 Requirements:**
- [x] ~~**CRITICAL:** Add PackageVersion to csproj~~ MOOT — centralized versioning
- [ ] Add comprehensive XML documentation
- [ ] Consider adding timeout to IHealthCheck interface
- [ ] Add tags/labels support for categorization
- [ ] Document health check composition patterns
- [ ] Add critical vs non-critical support
- [ ] **DECISION:** Evaluate integration/interop with Microsoft health checks
- [ ] Document readiness vs liveness patterns
- [ ] Add health check dependency ordering

**Estimated Effort:** ~~15-20 hours~~ 14-19 hours remaining (PackageVersion item resolved; everything else unchanged)

---

### 10. Benzene.HealthChecks 🏥 Health Check Implementations

**Location:** `src/Benzene.HealthChecks/`
**Current State:** Medium maturity, good implementation variety

**Public API Surface:**
- `HealthCheckProcessor` - Runs health checks in parallel
- `HealthCheckMessageHandler` - Message handler integration
- `HealthCheckBuilder` - Builder implementation
- `SimpleHealthCheck` - Simple implementation
- `InlineHealthCheck` - Inline lambda-based check
- `FailedHealthCheck` - Always-fail check
- `TimeOutHealthCheck` - Timeout wrapper (decorator)
- `ExceptionHandlingHealthCheck` - Exception wrapper
- `HealthCheckFinder` - Discovery mechanism
- `HealthCheckNamer` - Name uniqueness
- `Extensions` - Registration helpers
- Constants

**Strengths:**
- Parallel execution with Task.WhenAll
- Timeout support via decorator
- Exception handling via decorator
- Inline lambda support for simple checks
- Health check discovery mechanism
- Name uniqueness handling

**Issues (2026-07-14 re-verification — confirmed against current `HealthCheckProcessor.cs`/`TimeOutHealthCheck.cs`):**
1. ~~❌ **Missing PackageVersion in csproj**~~ ✅ MOOT — centralized versioning
2. ✅ **RESOLVED 2026-07-14** — No XML documentation — all 14 files documented, 0 CS1591 on clean rebuild
3. ⚠️ HealthCheckProcessor.PerformHealthChecksAsync has topic parameter but doesn't use it — **still true**, verified: `topic` is accepted but never referenced in the method body
4. ⚠️ No graceful degradation (fails if any check fails) — still true
5. ✅ **RESOLVED 2026-07-14** — No separate readiness vs liveness endpoints. `Benzene.HealthChecks.Extensions.UseLivenessCheck`/`UseReadinessCheck` now exist (topic-based, every transport, responding only to `Constants.DefaultLivenessTopic`/`DefaultReadinessTopic`), with HTTP-path convenience wrappers in `Benzene.SelfHost.Http`/`Benzene.Aws.Lambda.ApiGateway` defaulting to the conventional `/livez`/`/readyz` paths. While implementing this, also found and fixed a real bug that would have made HTTP-based Kubernetes probes non-functional even with the split: `HealthCheckProcessor.PerformHealthChecksAsync` always returned HTTP 200 regardless of `isHealthy` — now returns 503 when unhealthy. Full guide: `docs/kubernetes-health-checks.md`. This package's `CLAUDE.md` no longer describes an aspiration — it's now accurate (also fixed the `/health` HTTP-endpoint overclaim it separately had).
6. ⚠️ TimeOutHealthCheck has hard-coded timeout (needs documentation) — **still true**, verified: `Task.Delay(10000)` is a magic number in `TimeOutHealthCheck.cs`, not configurable (unlike the analogous `WaitTimeSeconds` fix made configurable in `Benzene.Aws.Sqs`'s `SqsConsumerConfig` per the AWS roadmap's 2026-07-12 changelog — no equivalent fix landed here)
7. ⚠️ No caching for health check results — still true
8. ⚠️ No progress reporting for long-running checks — still true

**1.0 Requirements:**
- [x] ~~**CRITICAL:** Add PackageVersion to csproj~~ MOOT — centralized versioning
- [x] Add comprehensive XML documentation — done 2026-07-14
- [ ] Fix or document unused topic parameter — documented (XML doc now notes it), not fixed
- [ ] Add configurable health threshold (some failures OK)
- [x] Add readiness vs liveness endpoint support — done 2026-07-14, `UseLivenessCheck`/`UseReadinessCheck`
- [ ] Document timeout configuration — the 10s hardcoded timeout is documented as non-configurable, not made configurable
- [ ] Add health check result caching
- [x] Document health check best practices — `docs/kubernetes-health-checks.md`'s liveness-vs-readiness guidance
- [x] Add examples for common patterns — `docs/kubernetes-health-checks.md`
- [x] Integration with Kubernetes health probes — done 2026-07-14, including verifying the HTTP status
      code (not just the JSON body) reflects health, which is what Kubernetes' `httpGet` probe type
      actually checks

**Estimated Effort:** ~~18-22 hours~~ ~10-13 hours remaining (PackageVersion, XML docs, readiness/liveness,
best-practices docs, and Kubernetes integration all resolved 2026-07-14; threshold/caching/timeout-
configurability items remain genuinely open, unchanged)

---

### 11. Benzene.HealthChecks.Http 🌐 HTTP Health Checks

**Location:** `src/Benzene.HealthChecks.Http/`
**Current State:** Medium maturity, focused implementation

**Public API Surface:**
- `HttpPingHealthCheck` - HTTP endpoint ping
- `HttpPingHealthCheckFactory` - Factory for HTTP checks
- `Extensions` - Registration helpers

**Strengths:**
- Simple HTTP ping health check
- Factory pattern for configuration
- Useful for downstream service checks

**Issues:**
1. ~~❌ **Missing PackageVersion in csproj**~~ ✅ MOOT — centralized versioning
2. ❌ No XML documentation — still true, 0/3 files
3. ⚠️ No timeout configuration visible — still true
4. ⚠️ No retry logic — still true
5. ⚠️ No status code validation (200 vs 2xx vs 503) — still true
6. ⚠️ No support for authenticated endpoints — still true
7. ⚠️ No custom header support — still true
8. ⚠️ No HTTP method configuration (GET vs HEAD) — still true
9. 🆕 **No dedicated tests found anywhere in `test/`** for this package specifically (not previously called out as its own issue — it fell under the general "minimal test coverage" complaint, but is now the more precise gap since Diagnostics/HealthChecks.Core do have real tests)

**1.0 Requirements:**
- [x] ~~**CRITICAL:** Add PackageVersion to csproj~~ MOOT — centralized versioning
- [ ] Add comprehensive XML documentation
- [ ] Add timeout configuration
- [ ] Add retry policy support
- [ ] Add status code validation options
- [ ] Add authentication support (basic, bearer)
- [ ] Add custom header support
- [ ] Add HTTP method configuration
- [ ] Document security considerations (internal vs external endpoints)
- [ ] Add circuit breaker pattern
- [ ] Add unit tests (currently zero)

**Estimated Effort:** ~~15-18 hours~~ 16-19 hours remaining (PackageVersion item resolved; added a test-coverage item that was previously only implicit)

---

### 12. Benzene.HealthChecks.EntityFramework 🗄️ Database Health Checks

**Location:** `src/Benzene.HealthChecks.EntityFramework/`
**Current State:** Medium maturity, EF-specific

**Public API Surface:**
- `DatabaseHealthCheck` - EF database connectivity check
- `DatabaseConnectionHealthCheck` - Connection-level check
- `DatabaseHealthCheckFactory` - Factory implementation

**Strengths:**
- Entity Framework integration
- Connection-level and database-level checks
- Factory pattern for configuration

**Issues:**
1. ~~❌ **Missing PackageVersion in csproj**~~ ✅ MOOT — centralized versioning
2. ❌ No XML documentation — still true, 0/3 files
3. ⚠️ EF version compatibility unclear — still true
4. ⚠️ No query timeout configuration — still true
5. ⚠️ No support for non-EF databases (Dapper, ADO.NET) — still true
6. ⚠️ No custom query support (SELECT 1) — still true
7. ⚠️ No connection pool health check — still true
8. ⚠️ No database migration status check — still true
9. 🆕 **Two high-severity NuGet advisory warnings**: `Microsoft.Extensions.Caching.Memory` 6.0.0 (GHSA-qj66-m88j-hmgj) and `Npgsql` 5.0.7 (GHSA-x9vc-6hfv-hg8c), surfaced by `dotnet build` on this project — new finding, not in the original roadmap
10. 🆕 **No dedicated tests found anywhere in `test/`** for this package specifically — same gap as `Benzene.HealthChecks.Http`

**1.0 Requirements:**
- [x] ~~**CRITICAL:** Add PackageVersion to csproj~~ MOOT — centralized versioning
- [ ] Add comprehensive XML documentation
- [ ] Document EF Core version compatibility
- [ ] Add query timeout configuration
- [ ] Consider separate package for non-EF databases
- [ ] Add custom query support
- [ ] Add connection pool metrics
- [ ] Add migration status check option
- [ ] Document performance implications
- [ ] Add read replica health checks
- [ ] Update `Microsoft.Extensions.Caching.Memory`/`Npgsql` off their vulnerable pinned versions
- [ ] Add unit tests (currently zero)

**Estimated Effort:** ~~15-18 hours~~ 18-22 hours remaining (PackageVersion item resolved, but two new items added — dependency vuln fixes and test coverage — that weren't previously tracked)

---

## Roadmap to 1.0.0

### Critical Path Items (BLOCKERS)

**Must Have Before 1.0:**

1. ~~**Add PackageVersion to all csproj files** (4-6 hours) - HIGHEST PRIORITY~~ ✅ MOOT 2026-07-12
   - Centralized versioning (`Directory.Build.props` + `version.txt`) makes this a non-issue for
     every package, including the ones this item named (OpenTelemetry, HealthChecks Core/Http/
     EntityFramework). Microsoft.Logging, Serilog, Log4Net, and Zipkin — also named here — no
     longer exist at all (deleted 2026-07-12/13, see changelog).

2. **XML Documentation** (60-80 hours) - CRITICAL — ⚠️ PARTIALLY DONE, scope reduced
   - Document every public type, method, property — done for the Checkpoint A-F files in
     `Benzene.Diagnostics` and all of `Benzene.OpenTelemetry`; **not done** for the rest of
     `Benzene.Diagnostics` or any of the 4 `Benzene.HealthChecks*` packages (0/29 files)
   - Add `<summary>`, `<param>`, `<returns>`, `<remarks>` — same partial state
   - Include `<example>` for main entry points — not done anywhere
   - Document observability-specific concerns (overhead, sampling, privacy) — not done
   - Original 60-80h estimate covered 12 packages; with 6 deleted, remaining scope is smaller —
     not re-estimated as part of this docs-only audit, but plausibly 30-45h now

3. ~~**Fix Dependency Issues** (8-12 hours) - BLOCKING~~ ✅ MOOT/DONE
   - ~~Remove AWSSDK.SQS from Benzene.Aws.XRay~~ MOOT — package deleted entirely
   - ~~Update System.Text.Encodings.Web to .NET 10 compatible~~ MOOT — package deleted entirely
   - ✅ Update OpenTelemetry to latest stable — done, 1.10.0 → 1.16.0
   - 🆕 New dependency issue found this pass, not previously tracked: `Benzene.HealthChecks.EntityFramework` has 2 high-severity NuGet advisories (`Microsoft.Extensions.Caching.Memory` 6.0.0, `Npgsql` 5.0.7)

4. **Test Coverage** (50-70 hours) - CRITICAL — ⚠️ PARTIALLY DONE
   - Unit tests for all packages (target 80%+ coverage) — real tests now exist for Diagnostics,
     OpenTelemetry, and core health-check logic (9 dedicated files, 37 passing tests); zero
     dedicated tests for `Benzene.HealthChecks.Http`/`.EntityFramework`
   - Integration tests for tracing providers — `BenzeneInstrumentationTest` covers the OTel
     wiring; no integration test against a real OTel Collector/backend
   - Async context flow tests — `W3CTraceContextTest` covers context propagation generally, not
     specifically async-boundary edge cases
   - Performance/overhead tests — still not done, no benchmark project found
   - Health check scenario tests — done for core pipeline/naming/discovery logic; not done for
     HTTP/EF-specific checks

5. **OpenTelemetry Modernization** (20-25 hours) - HIGH PRIORITY — ✅ LARGELY DONE
   - ✅ Replace TracerProvider.Default with DI — done (the rewritten package never touches
     `TracerProvider.Default`; `AddBenzeneInstrumentation()` is a plain builder extension)
   - ✅ Add metrics support — done (`UseBenzeneMetrics()` + `AddBenzeneInstrumentation(MeterProviderBuilder)`)
   - Add logging support — not done (no OTel Logs Bridge integration)
   - Document standards compliance — not done

6. ~~**Correlation ID Integration** (15-20 hours)~~ ✅ RESOLVED, superseded
   - ✅ Propagate correlation IDs to all tracing providers — superseded by W3C `traceparent`
     propagation (`UseW3CTraceContext()`/`WithW3CTraceContext()`), which is now the primary
     cross-service correlation mechanism (the legacy header-based `ICorrelationId` remains as an
     `[Obsolete]` fallback, not something that needs "propagating to tracing providers" anymore
     since there's only one tracing provider integration point now)
   - Document async context flow — not done
   - ✅ Add automatic enrichment for logs — done (`UseBenzeneEnrichment()`)

7. **Documentation** (30-40 hours) — ⚠️ PARTIALLY DONE
   - Getting started guide for each category — `docs/monitoring.md` covers diagnostics/tracing/
     logging/health checks in one document, reasonably thorough; not split "per category"
   - Performance/overhead benchmarks — not done
   - Sampling strategies documentation — not done
   - Privacy/security guidance — not done
   - ✅ Integration examples — `examples/OpenTelemetry/` is a complete, working example with a
     web UI and a Grafana LGTM stack (see changelog item 7)
   - ✅ Migration guides — `docs/migration-alpha-to-1.0.md` covers the logging and tracing
     migrations in detail (project-wide document, not observability-specific, but the
     observability content is complete)

8. **Privacy & Security** (12-15 hours) — not done, unchanged
   - Sensitive data filtering guidance
   - GDPR considerations documentation
   - PII handling best practices
   - Debug middleware security warnings

**Total Estimated Effort for 1.0:** ~~199-286 hours (5-7 weeks full-time)~~ not re-estimated
holistically as part of this docs-only audit, but the package-count reduction (12 → 6) and the
resolved items above (PackageVersion, dependency issues, OpenTelemetry modernization,
correlation ID integration, most integration-examples/migration-guide work) suggest the
remaining total is meaningfully smaller than the original estimate — a fresh bottom-up estimate
is recommended before using this number for planning.

### Phased Approach

> **2026-07-14 note:** This phased plan predates the package deletions and is written against
> the original 12-package inventory. Phase 1/3/5 items below are stale in the ways already
> detailed above; left as-is rather than rewritten since re-planning phases is a product
> decision, not something this docs-only audit should presume to do. The clearest correction:
> **Phase 3 ("Logging") is now moot in its entirety** — there are no logging packages left to
> complete, correlate, test, or document; and **Phase 4 ("Tracing")** narrows from four packages
> (OpenTelemetry, Datadog, Zipkin, XRay) to one (OpenTelemetry).

**Phase 1: Foundation (Weeks 1-2) - 60-80 hours**
- ~~Add all missing PackageVersions~~ MOOT — centralized versioning
- Fix critical dependency issues — mostly moot (XRay deleted); new EF vuln item remains
- Set up test infrastructure — ✅ done (test infrastructure exists and is in active use)
- Begin XML documentation (Diagnostics, HealthChecks.Core) — ⚠️ partially done for Diagnostics; not started for HealthChecks.Core

**Phase 2: Core Packages (Weeks 3-4) - 60-80 hours**
- Complete Benzene.Diagnostics to 1.0 — significant progress (Activity-based rewrite, W3C trace context, metrics, enrichment); XML docs and benchmarks still open
- Complete HealthChecks.Core and HealthChecks to 1.0 — not started beyond what already existed
- ✅ Unit tests for diagnostics and health checks — done for the core logic
- Documentation for core packages — `docs/monitoring.md` covers Diagnostics well; HealthChecks has no equivalent narrative doc found

**Phase 3: Logging (Week 5) - 40-60 hours** — ✅ MOOT IN ITS ENTIRETY
- ~~Complete logging packages (Microsoft.Logging, Serilog, Log4Net)~~ — packages deleted, not completed
- ~~Correlation ID integration~~ — superseded by W3C trace context, done differently than envisioned
- ~~Unit and integration tests~~ — N/A, no packages to test
- Documentation — ✅ done differently: `docs/migration-alpha-to-1.0.md` and `docs/cookbooks/structured-logging-serilog.md` document the plain-`ILogger<T>` replacement in detail

**Phase 4: Tracing (Weeks 6-7) - 80-100 hours** — narrowed to OpenTelemetry only
- ✅ Modernize OpenTelemetry — largely done (see Critical Path Item 5 above)
- ~~Complete Datadog, Zipkin, XRay~~ — all three deleted, not completed
- ✅ Correlation ID integration — done via W3C trace context
- Performance benchmarks — not done
- Integration tests — `BenzeneInstrumentationTest` covers the wiring; no real-backend integration test
- ✅ Documentation — `docs/monitoring.md`'s OpenTelemetry section + `examples/OpenTelemetry/`

**Phase 5: Polish & Release (Week 8) - 10-15 hours**
- Final testing — ongoing, not a discrete completed milestone
- CHANGELOG updates — not tracked as part of this audit
- Release notes — not written
- NuGet publishing — not done (packages remain at 0.0.x)
- Announcement — not applicable pre-1.0

---

## Short-Term Roadmap (3-6 Months)

**Goal:** Release observability packages at 1.0.0 in phases after core Benzene 1.0

> **2026-07-14 audit note:** The ✅ marks throughout this month-by-month plan were originally
> used as a bullet-list style choice for *planned* work, not as completion indicators — none of
> this had actually happened when the document was written. Real progress has since landed
> against some of these items out of order (e.g. OpenTelemetry modernization ahead of a
> "Month 3" milestone; Log4Net's decision resolved without a formal "keep or deprecate" review).
> Corrected inline below; items that are now genuinely moot (the packages they target no longer
> exist) are struck through rather than marked complete, since "complete" would misrepresent
> what happened (deletion, not delivery).

### Q3 2026 (Months 1-3)

**Month 1: Foundation & Core**
- ~~Fix all missing PackageVersions~~ ✅ actually done, but via centralized versioning (2026-07-12), not a per-package fix
- ~~Fix dependency issues (XRay, OpenTelemetry)~~ ✅ actually done — XRay deleted, OpenTelemetry updated to 1.16.0
- ⚠️ Complete Benzene.Diagnostics 1.0 — substantial progress (Activity rewrite, W3C trace context, metrics, enrichment), but XML docs/benchmarks/sampling still open, so not "complete"
- ⚠️ Complete Benzene.HealthChecks.Core 1.0 — largely untouched since this roadmap was written
- ✅ Set up comprehensive test infrastructure — real, in active use
- ⚠️ Begin XML documentation effort — done for Diagnostics' newer files and OpenTelemetry; not started for HealthChecks.Core
- Deliverable: Diagnostics and HealthChecks.Core at 1.0 — not reached; Diagnostics is closer than HealthChecks.Core

**Month 2: Logging & Health Checks**
- ~~Complete logging packages (Microsoft.Logging, Serilog)~~ — **not completed; deleted instead** (2026-07-12). The underlying goal (Benzene working well with Serilog/Microsoft.Logging) is arguably better served by the plain-`ILogger<T>` approach that replaced them, but no "1.0" of either package was ever shipped
- ⚠️ Complete Benzene.HealthChecks implementations — largely untouched since this roadmap was written
- ✅ Correlation ID integration across logging — achieved differently: `UseBenzeneEnrichment()`/`ILogger.BeginScope` provide this across all logging, not per-provider
- ⚠️ Unit and integration tests — real coverage for Diagnostics/OpenTelemetry/core HealthChecks; none for HealthChecks.Http/.EntityFramework
- ✅ Documentation for logging — `docs/monitoring.md` + `docs/migration-alpha-to-1.0.md` + `docs/cookbooks/structured-logging-serilog.md`, all accurate and current; health checks documentation not equivalently improved
- ✅ **DECISION:** Keep or deprecate Log4Net — resolved by deletion (2026-07-12), more decisive than either original option
- Deliverable: Logging packages at 1.0, Health Check packages at 1.0 — logging packages don't exist to reach 1.0; Health Check packages not reached

**Month 3: Tracing & Telemetry**
- ⚠️ Modernize OpenTelemetry (DI, metrics, logging) — DI and metrics done; OTel logging integration still open
- ~~Complete Datadog, Zipkin integrations~~ — **not completed; deleted instead** (2026-07-13), superseded by OTel exporters
- ~~Improve Aws.XRay (segments, subsegments, annotations)~~ — **not completed; deleted instead** (2026-07-13), superseded by OTel exporters (which do provide segment/annotation-equivalent capability through `Activity` tags, just not as a dedicated X-Ray package)
- ✅ Correlation ID integration for tracing — achieved via W3C trace context propagation
- Performance benchmarks for all tracing providers — not done, no benchmark project exists
- Privacy and security documentation — not done
- Beta release (1.0.0-rc.1) — not done, packages remain at 0.0.x
- Deliverable: All tracing packages at 1.0 RC — not reached; scope is now just OpenTelemetry (the other three tracing packages no longer exist to reach any release stage)

### Q4 2026 (Months 4-6)

**Month 4: Beta Testing & Feedback**
- 🔄 Community beta testing
- 🔄 Address beta feedback
- 🔄 Performance optimization based on real workloads
- 🔄 Sampling strategy refinement
- 🔄 Final security review
- Deliverable: Beta feedback report, final fixes

**Month 5: Release Preparation**
- ✅ Final CHANGELOG updates
- ✅ Release notes preparation
- ✅ NuGet package validation
- ✅ Documentation review
- ✅ 1.0.0 release (all observability packages)
- Deliverable: Observability packages at 1.0.0

**Month 6: Post-Release Support**
- 🔄 Monitor adoption and issues
- 🔄 Quick patches for critical bugs
- 🔄 Gather feedback for 1.1 features
- 🔄 Create tutorials and examples
- Deliverable: 1.0.1 patch release if needed

---

## Medium-Term Roadmap (6-12 Months)

**Goal:** Expand observability capabilities and integrations

### Enhanced Tracing Features (Priority Order)

1. **OpenTelemetry Logs Bridge** (4-6 weeks)
   - Implement OpenTelemetry Logs API
   - Bridge Benzene logging to OTel
   - Resource attributes
   - Example: Unified observability
   - **Effort:** 25-30 hours

2. **Metrics Support** (6-8 weeks)
   - IMetricsCollector abstraction
   - Counter, gauge, histogram support
   - OpenTelemetry metrics exporter
   - Prometheus exporter
   - Example: Application metrics
   - **Effort:** 35-45 hours

3. **Distributed Context Propagation** (4-6 weeks)
   - W3C Trace Context standard
   - B3 propagation (Zipkin)
   - AWS X-Ray context
   - Baggage support
   - Example: Multi-service tracing
   - **Effort:** 25-30 hours

4. **Advanced Sampling** (4-6 weeks)
   - Parent-based sampling
   - Probability sampling
   - Rate limiting sampling
   - Error-based sampling
   - Configuration framework
   - **Effort:** 25-30 hours

5. **Span Processors** (3-4 weeks)
   - Batch span processor
   - Simple span processor
   - Custom processor support
   - Filtering and enrichment
   - **Effort:** 18-22 hours

### Advanced Health Checks (Priority Order)

1. **Readiness vs Liveness** (3-4 weeks)
   - Separate endpoints
   - Kubernetes probe integration
   - Startup health checks
   - Example: K8s deployment
   - **Effort:** 18-22 hours

2. **Health Check Dashboard** (6-8 weeks)
   - JSON/HTML endpoints
   - Real-time status
   - Historical data
   - UI component
   - Example: Admin dashboard
   - **Effort:** 35-45 hours

3. **Additional Health Checks** (6-8 weeks)
   - Redis health check
   - Message queue checks (SQS, Service Bus, Event Hubs)
   - External service dependency checks
   - Custom check builders
   - **Effort:** 30-40 hours

4. **Health Check Caching** (2-3 weeks)
   - Result caching
   - TTL configuration
   - Cache invalidation
   - Performance optimization
   - **Effort:** 12-15 hours

### Logging Enhancements (Priority Order)

1. **Structured Logging Helpers** (4-6 weeks)
   - Typed log properties
   - Log message templates
   - Event ID support
   - Correlation helpers
   - **Effort:** 20-25 hours

2. **Sensitive Data Filtering** (4-6 weeks)
   - PII detection
   - Automatic masking
   - Custom filter rules
   - Regex-based filtering
   - Example: GDPR compliance
   - **Effort:** 25-30 hours

3. **Log Aggregation** (3-4 weeks)
   - Batch logging
   - Async logging
   - Performance optimization
   - Buffer management
   - **Effort:** 18-22 hours

### Developer Experience

1. **Observability Middleware Bundle** (4-6 weeks)
   - Pre-configured bundles
   - Development vs Production profiles
   - One-line setup
   - Example: Quick start
   - **Effort:** 20-25 hours

2. **Performance Profiler Integration** (6-8 weeks)
   - dotnet-trace integration
   - PerfView support
   - Custom profiler hooks
   - Example: Performance debugging
   - **Effort:** 30-40 hours

3. **Observability CLI** (8-10 weeks)
   - View logs locally
   - Test tracing setup
   - Health check runner
   - Example: Development tool
   - **Effort:** 40-50 hours

---

## Long-Term Vision (12+ Months)

### Strategic Initiatives

**1. Unified Observability Platform** (6-12 months)
- Single API for traces, metrics, logs
- Automatic correlation across signals
- Context propagation everywhere
- Performance optimized
- Standards compliant

**2. Cloud-Native Observability** (12-18 months)
- Kubernetes-native health checks
- Service mesh integration (Istio, Linkerd)
- Cloud provider native support (CloudWatch, Azure Monitor)
- eBPF integration (where applicable)

**3. Enterprise Observability** (12+ months)
- Multi-tenancy support
- Cost tracking and optimization
- Compliance and audit logging
- SLA monitoring
- Alert management integration

**4. AI-Assisted Observability** (12+ months)
- Anomaly detection
- Intelligent sampling
- Root cause analysis
- Automatic correlation
- Predictive monitoring

### Emerging Standards & Technologies

**Monitor and Evaluate:**
- OpenTelemetry Profiling (continuous profiling standard)
- OpenTelemetry Client-Side Instrumentation
- eBPF-based observability (Linux)
- WebAssembly observability
- GraphQL tracing standards
- gRPC observability best practices

---

## Technical Debt & Quality

### Current Technical Debt

**Critical Priority:**
1. ~~⚠️ OpenTelemetry missing PackageVersion~~ ✅ MOOT — centralized versioning
2. ~~⚠️ All logging packages missing PackageVersion~~ ✅ MOOT — packages deleted entirely
3. ~~⚠️ All HealthChecks packages missing PackageVersion~~ ✅ MOOT — centralized versioning
4. ~~⚠️ Aws.XRay unnecessary AWSSDK.SQS dependency~~ ✅ MOOT — package deleted entirely
5. ~~⚠️ Old System.Text.Encodings.Web 6.0.0 in XRay~~ ✅ MOOT — package deleted entirely
6. 🆕 `Benzene.HealthChecks.EntityFramework` has 2 high-severity NuGet advisory warnings (`Microsoft.Extensions.Caching.Memory` 6.0.0, `Npgsql` 5.0.7) — new finding this pass

**High Priority:**
1. ✅ RESOLVED: OpenTelemetry uses deprecated TracerProvider.Default — the rewritten package never touches it
2. ~~Zipkin hard-codes service name "benzene"~~ ✅ MOOT — package deleted entirely
3. ✅ RESOLVED: No correlation ID propagation to tracing providers — superseded by W3C `traceparent` propagation, now real and working
4. HealthCheckProcessor unused topic parameter — **still true**, re-verified against current source this pass
5. ⚠️ No XML documentation anywhere — now only true for the 4 `Benzene.HealthChecks*` packages; `Benzene.Diagnostics`/`Benzene.OpenTelemetry` have partial coverage

**Medium Priority:**
1. No performance overhead benchmarks — still true
2. No sampling strategies documented — still true
3. No privacy/sensitive data guidance — still true
4. ✅ RESOLVED: Limited metrics support — `UseBenzeneMetrics()` + `AddBenzeneInstrumentation(MeterProviderBuilder)` provide real metrics
5. ✅ RESOLVED: No structured logging context propagation docs — `docs/monitoring.md`'s "Logging"/"Structured log scopes" sections cover this

**Low Priority:**
1. Timer tags are string-only (not typed) — still true for the legacy `IProcessTimer` path
2. Some inconsistent async patterns — not re-audited this pass
3. No nullable reference type annotations consistently — not re-audited this pass (all 6 current packages do have `<Nullable>enable</Nullable>` in their csproj, but that doesn't guarantee consistent annotation discipline)
4. Missing examples in some packages — ⚠️ partially resolved: `examples/OpenTelemetry/` is now a complete, working example; no equivalent for HealthChecks

### Code Quality Improvements

**Standardization:**
- [ ] Consistent error handling patterns
- [ ] Standardized configuration approaches
- [ ] Unified sampling strategy framework
- [ ] Common tag/attribute naming conventions
- [ ] Async/await best practices

**Architecture:**
- [ ] Review abstraction boundaries
- [ ] Consider IObservabilityProvider abstraction
- [ ] Unified context propagation mechanism
- [ ] Consistent factory patterns
- [ ] Builder patterns where beneficial

**Performance:**
- [ ] Zero-allocation hot paths
- [ ] Object pooling for spans/timers
- [ ] Lazy initialization
- [ ] Async optimizations
- [ ] Batch processing support

---

## Testing Strategy

### Current State
- ✅ Real test coverage now, not "~6 test classes total": `test/Benzene.Core.Test/Diagnostics/`
  + `Core/Diagnostics/` have 6 classes (`ActivityMiddlewareTest`, `BenzeneEnrichmentTest`,
  `W3CTraceContextTest`, `BenzeneMetricsTest`, `BenzeneInstrumentationTest`, `UseTimerTest`);
  `test/Benzene.Core.Test/Plugins/HealthChecks/` has 3 more (`HealthCheckPipelineTest`,
  `HealthCheckNamerTests`, `HealthCheckTests`); plus 6 health-check-adjacent classes elsewhere
  in the suite (`HealthCheckProcessorTest`, `HealthCheckTest`, `AwsLambdaHealthCheckTest`,
  `SqsHealthCheckTest`, `StepFunctionsHealthCheckTest`, `BenzeneHealthCheckBridgeTest`). All 37
  matching tests pass (verified this pass). `Benzene.HealthChecks.Http`/`.EntityFramework` still
  have zero dedicated tests.
- ~~Zipkin has integration tests (good!)~~ — package and its `ZipkinPipelineTest` both deleted 2026-07-13; `BenzeneInstrumentationTest` is the closest current analogue (exercises real `TracerProviderBuilder`/`MeterProviderBuilder` wiring)
- ✅ HealthCheckNamerTests exists (good!) — confirmed, still present and passing
- ~~BenzeneLoggerTests exists (basic)~~ — `IBenzeneLogger` and its tests were deleted 2026-07-12 along with the custom logging stack; logging is tested via `FakeLoggerFactory`-based scope tests instead (e.g. `test/Benzene.Core.Test/Core/Core/Logging/UseLogContextTest.cs`, outside this document's original package scope)
- No performance/overhead tests — still true
- ⚠️ No async context flow tests — `W3CTraceContextTest` covers context propagation generally; no test specifically targeting async-boundary edge cases
- No sampling tests — still true (no sampling implementation exists to test)

### Target Testing Strategy

**Unit Tests (Target: 80%+ coverage) - HIGHEST PRIORITY**
- ✅ Every public method tested
- ✅ Edge cases and error conditions
- ✅ Mock external dependencies
- ✅ Fast, deterministic tests
- Estimated: 60-80 hours to achieve target

**Integration Tests (Target: Key scenarios covered)**
- ✅ OpenTelemetry with OTLP exporter
- ✅ Datadog with local agent
- ✅ Zipkin with server (already exists!)
- ✅ Serilog with Seq/console
- ✅ Microsoft.Logging with various providers
- ✅ Health checks with real dependencies
- Estimated: 40-50 hours

**Performance Tests**
- ✅ Timer overhead benchmarks
- ✅ Tracing overhead (per-request)
- ✅ Logging overhead
- ✅ Context propagation performance
- ✅ Sampling impact
- ✅ Memory allocation profiling
- Estimated: 30-40 hours

**Async Context Flow Tests**
- ✅ Correlation ID across async boundaries
- ✅ Tracing context propagation
- ✅ Log context in async scenarios
- ✅ Parent-child span relationships
- Estimated: 20-25 hours

**Sampling Tests**
- ✅ Probability sampling correctness
- ✅ Rate limiting effectiveness
- ✅ Parent-based sampling
- ✅ Error-based sampling
- Estimated: 15-20 hours

### Test Infrastructure

**Local Development:**
```yaml
# docker-compose.yml for observability testing
services:
  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686"  # UI
      - "4317:4317"    # OTLP gRPC
      - "4318:4318"    # OTLP HTTP

  zipkin:
    image: openzipkin/zipkin:latest
    ports:
      - "9411:9411"

  seq:
    image: datalust/seq:latest
    environment:
      ACCEPT_EULA: Y
    ports:
      - "5341:80"

  datadog-agent:
    image: datadog/agent:latest
    environment:
      DD_API_KEY: test
      DD_APM_ENABLED: true
    ports:
      - "8126:8126"
```

**Benchmark Suite:**
```csharp
[MemoryDiagnoser]
public class ObservabilityBenchmarks
{
    [Benchmark]
    public void Timer_Overhead() { }

    [Benchmark]
    public void OpenTelemetry_Span_Creation() { }

    [Benchmark]
    public void Correlation_Id_Propagation() { }
}
```

### Testing Checklist for Each Package

- [ ] Unit test coverage ≥80%
- [ ] Integration tests with real backends
- [ ] Performance benchmark baseline
- [ ] Async context flow validated
- [ ] Error scenario coverage
- [ ] Documentation includes test examples
- [ ] CI/CD pipeline runs all tests
- [ ] Test results published

---

## Documentation Requirements

### Critical Documentation Gaps

**User Documentation:**
- [x] Getting started guide (observability overview) — `docs/monitoring.md` covers correlation IDs, tracing, timers, logging, structured log scopes, W3C trace context, and OpenTelemetry in one document; not split into a dedicated "overview" page but functionally complete
- [ ] Performance/overhead benchmarks and expectations
- [ ] Sampling strategies guide
- [ ] Privacy and sensitive data handling
- [ ] GDPR compliance considerations
- [ ] Best practices per package
- [ ] Troubleshooting guide (⚠️ `docs/cookbooks/structured-logging-serilog.md` has a dedicated Troubleshooting section covering Serilog-specific issues, but there's no observability-wide troubleshooting guide)
- [ ] FAQ for observability

**Package-Specific Guides:**
- [x] OpenTelemetry: OTLP exporter — `docs/monitoring.md`'s "OpenTelemetry" section + `examples/OpenTelemetry/` (full working example against `grafana/otel-lgtm`); Jaeger/Zipkin exporter guides specifically not written
- [ ] ~~Datadog: Agent setup, dashboard configuration~~ — moot in the original sense (no `Benzene.Datadog` package); a "reach Datadog via OTel exporter" cookbook doesn't exist yet
- [ ] ~~Zipkin: Server configuration, B3 propagation~~ — moot in the original sense (no `Benzene.Zipkin` package); a "reach Zipkin via OTel exporter" cookbook doesn't exist yet
- [ ] ~~AWS X-Ray: Daemon setup, sampling rules~~ — moot in the original sense (no `Benzene.Aws.XRay` package); see `work/aws-roadmap-1.0.md` section 8 for the one concrete follow-up item (an OTel-Collector-to-X-Ray integration test)
- [x] Serilog: Sink configuration — `docs/cookbooks/structured-logging-serilog.md` covers console/Seq sinks, `Enrich.FromLogContext()`, and cross-references Application Insights; Elasticsearch specifically not covered
- [ ] ~~Microsoft.Logging: Provider configuration~~ — moot, no `Benzene.Microsoft.Logging` package; plain `AddLogging()` is the standard .NET mechanism, documented generically in `docs/monitoring.md`
- [ ] Health Checks: Kubernetes probes, custom checks

**Developer Documentation:**
- [ ] Architecture decision records (ADRs)
- [ ] Contributing guide for observability packages
- [ ] Adding new tracing provider guide
- [ ] Testing guide (local setup, benchmarks)
- [ ] Release process for observability packages
- [ ] Compatibility matrix (SDK versions, .NET versions)

**API Documentation:**
- [ ] XML documentation for all public APIs
- [ ] Generated API docs (DocFX or similar)
- [ ] Code examples in XML docs
- [ ] Parameter validation documentation
- [ ] Exception documentation

**Operations Documentation:**
- [ ] Performance tuning guide
- [ ] Sampling configuration for production
- [ ] Cost optimization (tracing, logging)
- [ ] Security and privacy checklist
- [ ] Compliance documentation (GDPR, HIPAA)
- [ ] Production readiness checklist

### Documentation Structure

```
docs/observability/
├── getting-started/
│   ├── overview.md
│   ├── diagnostics.md
│   ├── logging.md
│   ├── tracing.md
│   ├── health-checks.md
│   └── quickstart.md
├── architecture/
│   ├── correlation-ids.md
│   ├── context-propagation.md
│   ├── async-flow.md
│   ├── performance-overhead.md
│   └── adr/
├── tracing/
│   ├── opentelemetry.md
│   ├── datadog.md
│   ├── zipkin.md
│   ├── aws-xray.md
│   ├── sampling-strategies.md
│   └── best-practices.md
├── logging/
│   ├── microsoft-logging.md
│   ├── serilog.md
│   ├── log4net.md
│   ├── structured-logging.md
│   └── log-correlation.md
├── health-checks/
│   ├── core-concepts.md
│   ├── http-checks.md
│   ├── database-checks.md
│   ├── kubernetes-integration.md
│   └── custom-checks.md
├── privacy-security/
│   ├── sensitive-data-filtering.md
│   ├── gdpr-compliance.md
│   ├── pii-handling.md
│   └── security-checklist.md
├── operations/
│   ├── performance-tuning.md
│   ├── cost-optimization.md
│   ├── production-checklist.md
│   └── troubleshooting.md
├── reference/
│   ├── configuration.md
│   ├── api/  (generated docs)
│   └── compatibility.md
└── migration/
    ├── from-0.x-to-1.0.md
    └── breaking-changes.md
```

### Performance Overhead Documentation

**Example Documentation Needed:**
```markdown
# Performance Overhead Guide

## Diagnostics Timer Overhead
- **Per-request overhead:** ~0.05ms
- **Memory allocation:** ~200 bytes
- **Recommendation:** Use in all environments

## OpenTelemetry Tracing Overhead
- **Per-span overhead:** ~0.2ms (without export)
- **With OTLP export:** ~1-2ms
- **Memory allocation:** ~1KB per span
- **Recommendation:** Use sampling in high-volume scenarios

## Sampling Strategies
### Development
- Sample 100% (all traces)

### Staging
- Sample 50% (or 100% for critical paths)

### Production
- Sample 1-10% (error traces 100%)
- Use parent-based sampling
- Configure rate limits
```

---

## Performance & Optimization

### Current Performance Metrics
- ❌ **No baseline measurements exist**
- ❌ No overhead benchmarks for any package
- ❌ No throughput measurements
- ❌ No memory allocation profiling
- ❌ No async performance testing

### Performance Goals

**Overhead Targets (P99):**
- Diagnostics Timer: <0.1ms per request
- OpenTelemetry span: <0.5ms per span (without export)
- Datadog span: <0.5ms per span
- Zipkin span: <0.5ms per span
- XRay segment: <1ms per segment
- Logging (sync): <0.1ms per log
- Logging (async): <0.05ms per log
- Health check: <100ms per check (depends on check type)
- Correlation ID: <0.01ms

**Memory Allocation:**
- Timer: <200 bytes per request
- Span: <1KB per span
- Log entry: <500 bytes
- Health check: <1KB per check
- No memory leaks in long-running scenarios

**Throughput:**
- Support 10,000+ requests/second with minimal overhead
- Batch processing for traces/logs where possible
- Async logging to prevent blocking

### Optimization Strategies

**1. Zero-Allocation Hot Paths**
- [ ] Use Span<T> for string operations
- [ ] ArrayPool for buffer management
- [ ] Object pooling for frequently created objects
- [ ] Avoid boxing in timer callbacks

**2. Async Optimization**
- [ ] ValueTask where appropriate
- [ ] ConfigureAwait(false) in libraries
- [ ] Async logging for all providers
- [ ] Batch export for traces

**3. Sampling Implementation**
- [ ] Probability-based sampling
- [ ] Rate limiting sampling
- [ ] Parent-based sampling
- [ ] Error-based sampling (100% of errors)

**4. Lazy Initialization**
- [ ] Defer expensive operations
- [ ] Lazy span creation
- [ ] On-demand exporter initialization

**5. Batching**
- [ ] Batch trace export
- [ ] Batch log export
- [ ] Configurable batch sizes
- [ ] Timeout-based flush

### Benchmarking Suite

**Micro-Benchmarks (BenchmarkDotNet):**
```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ObservabilityBenchmarks
{
    [Benchmark]
    public void DiagnosticsTimer_CreateAndDispose()
    {
        using var timer = new TimerMiddleware<object>((ctx, ms) => { });
    }

    [Benchmark]
    public async Task OpenTelemetry_SpanCreation()
    {
        using var timer = new OpenTelemetryProcessTimer("test");
    }

    [Benchmark]
    public void CorrelationId_GetSet()
    {
        var correlation = new CorrelationId();
        correlation.Set("test-id");
        var id = correlation.Get();
    }
}
```

**Load Testing:**
- 10,000 requests/second sustained
- 50,000 requests/second burst
- Measure overhead percentage
- Identify bottlenecks

### Cost Optimization

**Tracing Cost Optimization:**
1. **Sampling Strategies**
   - 1% sampling for normal traffic
   - 100% for errors
   - 10% for specific endpoints
   - Parent-based for distributed traces

2. **Export Optimization**
   - Batch export (reduce network overhead)
   - Compression (reduce bandwidth)
   - Selective attributes (reduce size)
   - TTL for spans (reduce storage)

3. **Provider-Specific**
   - Datadog: Optimize span tags, use analytics sampling
   - AWS X-Ray: Use sampling rules, reduce annotations
   - Zipkin: Batch send, compress

**Logging Cost Optimization:**
1. **Log Levels**
   - Production: Information and above
   - Staging: Debug and above
   - Development: Trace and above

2. **Sampling**
   - Rate limit debug logs
   - Sample verbose logs
   - Always capture errors

3. **Retention**
   - Hot storage: 7 days
   - Warm storage: 30 days
   - Cold storage: 90 days
   - Archive: 1 year

---

## Security & Privacy

### Security Audit Checklist

**Sensitive Data Handling:**
- [ ] No passwords in logs
- [ ] No API keys in traces
- [ ] No PII in span attributes
- [ ] Automatic credit card masking
- [ ] Configurable sensitive field filters
- [ ] Regex-based filtering support

**Privacy Compliance:**
- [ ] GDPR compliance documentation
- [ ] Right to be forgotten (log retention)
- [ ] Data minimization practices
- [ ] PII handling guidelines
- [ ] Consent for detailed tracing
- [ ] Data residency considerations

**Trace Security:**
- [ ] Trace context validation
- [ ] Prevent trace injection attacks
- [ ] Sanitize user-provided attributes
- [ ] Rate limiting to prevent DoS
- [ ] Authentication for exporters

**Log Security:**
- [ ] Log injection prevention
- [ ] Structured logging to prevent injection
- [ ] Secure log transmission (TLS)
- [ ] Log encryption at rest
- [ ] Access control for log data

**Health Check Security:**
- [ ] No sensitive info in health check responses
- [ ] Authentication for detailed health endpoints
- [ ] Rate limiting for health checks
- [ ] Prevent info disclosure

### Best Practices Implementation

**Observability Best Practices:**
- [ ] Correlation IDs for request tracing
- [ ] Distributed context propagation
- [ ] Structured logging everywhere
- [ ] Graceful degradation (observability failures don't break app)
- [ ] Circuit breakers for exporters
- [ ] Fallback to local logging if export fails

**Security Best Practices:**
- [ ] TLS for all network communication
- [ ] API key rotation guidance
- [ ] Secrets management (not in config)
- [ ] Least privilege for agent access
- [ ] Network segmentation (agents in DMZ)

**Privacy Best Practices:**
- [ ] Data classification
- [ ] Automatic PII detection
- [ ] Configurable retention policies
- [ ] Audit logging for observability data access
- [ ] Anonymization for non-production environments

### Sensitive Data Filtering

**Built-in Filters:**
```csharp
public interface ISensitiveDataFilter
{
    string Filter(string input);
}

// Example: Credit card masking
public class CreditCardFilter : ISensitiveDataFilter
{
    public string Filter(string input)
    {
        // Regex to detect credit cards
        // Replace with "****-****-****-1234"
    }
}
```

**Configuration:**
```json
{
  "Observability": {
    "Filtering": {
      "Enabled": true,
      "Filters": [
        "CreditCard",
        "Email",
        "PhoneNumber",
        "CustomRegex"
      ],
      "CustomPatterns": [
        "ssn:\\s*\\d{3}-\\d{2}-\\d{4}"
      ]
    }
  }
}
```

---

## Breaking Changes Pre-1.0

### Allowed Before 1.0 (Do Now)

> **2026-07-14 audit note:** Items 1-3, 5, and 7 below are now moot (done, or the package they
> targeted no longer exists). Items 4, 6, and 8 remain open, corrected inline. The *actual*
> breaking changes that shipped in the meantime — deleting six packages outright, obsoleting
> `UseCorrelationId()`, the `AddScoped`→`AddSingleton` DI fix — are bigger than anything
> originally listed here and are fully documented in `docs/migration-alpha-to-1.0.md`, which
> now serves as the real pre-1.0 migration record for this area.

**1. ~~Add PackageVersion to All Missing csproj Files~~ (CRITICAL)** ✅ MOOT 2026-07-12
- ~~OpenTelemetry, Microsoft.Logging, Serilog, Log4Net, Zipkin~~ — centralized versioning makes
  this a non-issue; three of the five named packages no longer exist anyway
- ~~All HealthChecks packages~~ — same, centralized versioning
- **Impact:** N/A — never became a real NuGet packaging break; resolved the day after this
  document's stated last-updated date

**2. ~~Remove AWSSDK.SQS from Benzene.Aws.XRay~~ (CRITICAL)** ✅ MOOT — package deleted entirely 2026-07-13

**3. ~~Update System.Text.Encodings.Web in XRay~~ (CRITICAL)** ✅ MOOT — package deleted entirely 2026-07-13

**4. Replace TracerProvider.Default in OpenTelemetry** ✅ DONE, but not the way originally envisioned
- ~~Use DI-injected TracerProvider~~ — instead, the rewritten `AddBenzeneInstrumentation()` is a
  plain extension on OTel's own `TracerProviderBuilder`/`MeterProviderBuilder`, so it never
  touches `TracerProvider.Default` *or* requires Benzene-specific DI registration — arguably a
  cleaner resolution than "DI-injected TracerProvider" would have been
- **Impact:** Medium - changes initialization — realized; `docs/migration-alpha-to-1.0.md` documents the old-vs-new call shape (`AddOpenTelemetry()` → `AddBenzeneInstrumentation()`)
- **Migration:** Update service registration — documented

**5. ~~Make Zipkin Service Name Configurable~~** ✅ MOOT — package deleted entirely 2026-07-13

**6. Fix HealthCheckProcessor Unused Parameter** — still open, unchanged
- Remove or use topic parameter — **not done**, re-verified against current source this pass
- **Impact:** Low - unused parameter
- **Migration:** None unless explicitly passing topic

**7. ~~Standardize Correlation ID Propagation~~** ✅ RESOLVED, superseded
- ✅ Automatically propagate to all tracing providers — superseded by real, working W3C
  `traceparent` propagation (`UseW3CTraceContext()`/`WithW3CTraceContext()`), which achieves the
  same underlying goal (cross-service correlation) via the OTel-standard mechanism rather than
  a Benzene-specific propagation scheme
- **Impact:** Low - additive change — confirmed additive; `UseCorrelationId()` still works, just `[Obsolete]`

**8. Add Timeout to IHealthCheck Interface** — still open, unchanged
- Add optional timeout parameter — **not done**; `TimeOutHealthCheck` in `Benzene.HealthChecks` still hard-codes a 10000ms `Task.Delay`, not an interface-level or configurable timeout
- **Impact:** Medium - interface change
- **Migration:** Implement new interface member

### Document in Migration Guide

> ✅ **Largely done** — `docs/migration-alpha-to-1.0.md` now covers most of this list, plus
> substantially more that wasn't anticipated here (package deletions, `UseCorrelationId()`
> obsoletion, the DI captive-dependency fix). Item-by-item:

**Breaking Behavioral Changes:**
1. ✅ Documented — "OpenTelemetry requires DI setup" doesn't quite describe what actually
   shipped (no DI setup is required at all; see item 4 above), but
   `docs/migration-alpha-to-1.0.md`'s "Logging & tracing infrastructure" section accurately
   documents the real `AddOpenTelemetry()` → `AddBenzeneInstrumentation()` change
2. ~~Zipkin service name must be configured~~ — moot, package deleted; migration guide documents the deletion instead
3. ✅ Documented — "Correlation IDs automatically propagated" undersells what shipped (full W3C trace context propagation, not just correlation IDs); migration guide covers it under "New: UseBenzeneEnrichment(), UseBenzeneMetrics(), W3C trace context" and "Correlation ID: UseCorrelationId() obsoleted..."
4. ❌ Not documented — health check timeout is still not configurable, so there's nothing to document yet

**New Required Dependencies:**
- ✅ Ensure latest OpenTelemetry SDK versions — 1.16.0, current
- ~~Update System.Text.Encodings.Web if using XRay~~ — moot, XRay package deleted

**Deprecated (Remove in 2.0):**
- ~~TBD - evaluate Log4Net usage, may mark as community-supported~~ — decision made and executed: `Benzene.Log4Net` was deleted outright (2026-07-12) rather than marked community-supported; log4net remains usable via its own official MEL provider

---

## Dependencies & Compatibility

### Observability SDK Version Strategy

**Current Issues (2026-07-14 audit):**
- ~~Missing PackageVersion in many packages~~ ✅ MOOT — centralized versioning
- ✅ RESOLVED: OpenTelemetry 1.10.0 → now 1.16.0, verified in `src/Benzene.OpenTelemetry/Benzene.OpenTelemetry.csproj`
- ~~Old System.Text.Encodings.Web 6.0.0 in XRay~~ ✅ MOOT — `Benzene.Aws.XRay` deleted entirely 2026-07-13
- ~~Unnecessary AWSSDK.SQS in XRay~~ ✅ MOOT — same, package deleted
- 🆕 `Benzene.HealthChecks.EntityFramework` has 2 high-severity NuGet advisories (`Microsoft.Extensions.Caching.Memory` 6.0.0, `Npgsql` 5.0.7) — new finding this pass, not previously tracked

**Proposed Strategy:**
- Use latest stable SDK versions at release time
- Pin to MAJOR.MINOR for stability
- Document minimum compatible versions
- Test with latest versions in CI/CD
- Monthly review of dependency updates

**Compatibility Matrix (corrected — Datadog.Trace/zipkin4net/XRay SDK columns removed, packages deleted):**
```markdown
| Benzene Observability | .NET | OpenTelemetry |
|-----------------------|------|---------------|
| 1.0.x                 | 10.0 | 1.16+         |
| 0.0.x (current)        | 10.0 | 1.16          |
```
The original table's `Datadog.Trace`/`zipkin4net`/`XRay SDK` columns no longer apply — those
three packages are deleted, and Benzene no longer depends on their SDKs at all. Datadog/Zipkin/
X-Ray remain reachable as OpenTelemetry export targets (via an OTel exporter of the user's
choosing), not as a Benzene-versioned dependency.

### Benzene Core Dependencies

**Current State:**
All observability packages reference:
- Benzene.Abstractions.*
- Benzene.Core.*
- Benzene.Diagnostics (for tracing packages)

**Strategy:**
- Observability 1.0 packages require Benzene Core 1.x
- Allow minor version upgrades within same major
- Document tested combinations

**Example:**
```xml
<PackageReference Include="Benzene.Core" Version="[1.0.0,2.0.0)" />
```

### Third-Party Dependencies

**Logging Providers (no Benzene-specific packages involved anymore — see 2026-07-14 changelog):**
- Serilog: User-provided version, via Serilog's own `Serilog.Extensions.Logging` MEL provider (document compatibility)
- Microsoft.Extensions.Logging: Framework-provided, and now the *only* way Benzene logs — no adapter package needed
- log4net: User-provided version, via log4net's own MEL provider (`Microsoft.Extensions.Logging.Log4Net.AspNetCore`) (document compatibility)

**Tracing Providers:**
- OpenTelemetry: ✅ 1.16+ (updated from 1.10, verified in csproj)
- ~~Datadog.Trace: 2.48+ (verify latest)~~ — moot, `Benzene.Datadog` deleted; reachable via OTel exporter instead
- ~~zipkin4net: 1.5+ (verify maintenance status)~~ — moot, `Benzene.Zipkin` deleted; reachable via OTel exporter instead
- ~~AWSXRayRecorder: 2.11+ (verify latest)~~ — moot, `Benzene.Aws.XRay` deleted; reachable via OTel exporter instead

**Action Items:**
- [x] Update OpenTelemetry to latest stable (done — 1.16.0)
- [x] ~~Verify Datadog.Trace latest version~~ N/A — package deleted
- [x] ~~Verify zipkin4net is still maintained~~ N/A — package deleted
- [x] ~~Update AWSXRayRecorder if newer available~~ N/A — package deleted
- [x] ~~Remove System.Text.Encodings.Web from XRay or update~~ N/A — package deleted
- [ ] Document minimum version requirements for all (still open for OpenTelemetry, Serilog's/log4net's MEL providers)
- [x] 🆕 Resolve `Microsoft.Extensions.Caching.Memory`/`Npgsql` NuGet advisories in `Benzene.HealthChecks.EntityFramework` — done 2026-07-14, bumped `Microsoft.EntityFrameworkCore` 6.0.0→10.0.9 and `Npgsql.EntityFrameworkCore.PostgreSQL` 5.0.7→10.0.3 (matching the `net10.0` target framework); verified 0 advisory warnings on rebuild and no source changes needed (the package only calls stable `DbContext`/`Database.CanConnectAsync`/`GetAppliedMigrationsAsync` APIs)

### OpenTelemetry Standards Compliance

**Target Standards:**
- OpenTelemetry Tracing API 1.x
- OpenTelemetry Metrics API 1.x
- OpenTelemetry Logs Bridge API 1.x
- W3C Trace Context propagation
- W3C Baggage specification
- OTLP exporter protocol

**Action Items:**
- [ ] Document standards compliance
- [x] Test with OTLP exporters (`examples/OpenTelemetry/` runs against a real `grafana/otel-lgtm` OTLP collector, manually verified per its README; no automated CI test against a real collector)
- [x] Test W3C Trace Context propagation (`test/Benzene.Core.Test/Diagnostics/W3CTraceContextTest.cs`, verified passing this pass)
- [ ] Add compliance tests to CI (the example above is manual/local, not wired into CI)

---

## Success Metrics

### Adoption Metrics (6 months post-1.0)

**NuGet Statistics:**
- Target: 500+ downloads per package
- Target: 25+ dependent packages
- Target: 5+ contributors

**GitHub Metrics:**
- Target: 50+ stars on observability features
- Target: 10+ forks
- Target: 30+ observability issues/discussions
- Target: 5+ external contributors

### Quality Metrics

**Code Coverage:**
- Target: 80%+ unit test coverage (currently ~10%)
- Target: 60%+ integration test coverage
- Target: 100% of public APIs documented (currently 0%)

**Performance:**
- Timer overhead: <0.1ms P99
- Span overhead: <0.5ms P99
- No memory leaks in 24h sustained load
- <5% overhead at high throughput

**Reliability:**
- Zero critical bugs reported in first 3 months
- <2 week response time on issues
- <1 month for minor bug fixes

### User Satisfaction

**Community Feedback:**
- Target: 4.5+ stars on NuGet reviews
- Target: 90%+ positive GitHub issue sentiment
- Target: Active observability discussions (monthly)

**Documentation:**
- Target: <5 "documentation unclear" issues per package
- Target: Getting-started guide completable in <20 minutes
- Target: Examples run successfully for 95%+ users

### Business Impact

**Observability Coverage:**
- Month 6: All 12 packages at 1.0
- Month 12: +3 new integrations (New Relic, Grafana, Honeycomb)
- Month 18: Unified observability API

**Enterprise Adoption:**
- Target: 3+ enterprise teams using in production
- Target: 1+ case study published
- Target: Integration with major APM vendors

---

## Prioritized Feature List

### Must Have for 1.0 (P0)

> **2026-07-14 audit note:** 5 of these 10 items are done or moot; effort figures below are
> struck through where resolved rather than recomputing a new P0 total, since several of the
> "still open" items (2, 4, 7) now have meaningfully smaller scope than originally estimated
> (6 packages instead of 12).

1. ~~**Add PackageVersion to csproj** - All missing packages (4-6h)~~ ✅ MOOT — centralized versioning
2. ~~**XML Documentation** - All packages (60-80h)~~ ✅ COMPLETE 2026-07-14 — Diagnostics/OpenTelemetry done earlier; all 4 HealthChecks packages (29 files) done same-day follow-up, 0 CS1591/CS1574 on clean rebuild for every package this roadmap tracks
3. ~~**Fix Dependency Issues** - XRay, OpenTelemetry (8-12h)~~ ✅ DONE/MOOT — XRay deleted, OpenTelemetry updated to 1.16.0; new item found: EF package NuGet advisories (unscoped, small)
4. **Unit Tests** - 80%+ coverage (60-80h) — ✅ largely done for Diagnostics/OpenTelemetry/core HealthChecks; `HealthChecks.Http`/`.EntityFramework` gap closed 2026-07-14 (10 new tests); no rigorous line-coverage % re-measured for this pass, but the "zero dedicated tests" gap specifically is resolved
5. ~~**OpenTelemetry Modernization** - DI, metrics, logs (20-25h)~~ ✅ LARGELY DONE — DI/TracerProvider.Default and metrics resolved; logs integration still open
6. ~~**Correlation ID Integration** - All providers (15-20h)~~ ✅ RESOLVED, superseded by W3C trace context propagation
7. **Performance Benchmarks** - All packages (20-25h) — still fully open, no benchmark project exists
8. ~~**Privacy Documentation** - Sensitive data, GDPR (10-12h)~~ ✅ DONE 2026-07-14 — `docs/privacy-and-data-handling.md`, grounded in what Benzene actually captures by default (nothing sensitive) vs. opt-in risk points (`WithHeaders(...)`, custom handler logging, health check diagnostics)
9. **Getting Started Guides** - All categories (15-20h) — ⚠️ partially done: `docs/monitoring.md` covers most categories in one doc, not split per-category
10. ~~**Migration Guide** - 0.x to 1.0 (8-10h)~~ ✅ DONE — `docs/migration-alpha-to-1.0.md` (project-wide, but the observability content is complete)

**Total P0 Effort:** ~~220-310 hours~~ not re-estimated precisely, but roughly half the original items are done/moot; remaining open work is items 2 (partial), 4 (partial), 7, 8, 9 (partial) — plausibly 90-130 hours remaining, not a rigorous re-estimate

### Should Have for 1.0 (P1)

1. **Integration Tests** - All providers (30-40h) — ⚠️ `BenzeneInstrumentationTest` covers the OTel wiring; no real-backend integration test; scope reduced (fewer providers to integration-test now that Datadog/Zipkin/XRay are gone)
2. ~~**Sampling Strategies** - Implementation and docs (20-25h)~~ ✅ DOCS DONE 2026-07-14 —
   `docs/sampling-strategies.md`. No "implementation" needed: sampling is entirely standard OTel SDK
   `Sampler` configuration, Benzene has and needs no sampling logic of its own — the doc makes this
   explicit rather than inventing a Benzene-specific sampling API that doesn't exist
3. **Async Context Flow Tests** - All scenarios (15-20h) — ⚠️ `W3CTraceContextTest` covers context propagation generally, not specifically async-boundary edge cases
4. **Sensitive Data Filtering** - Built-in filters (20-25h) — still fully open
5. ~~**Health Check Enhancements** - Readiness/liveness (15-18h)~~ ✅ DONE 2026-07-14 —
   `UseLivenessCheck`/`UseReadinessCheck`, `docs/kubernetes-health-checks.md`; see section 10's issue
   list item 5. Configurable health threshold, result caching, and configurable timeout remain open.
6. ~~**Log4Net Decision** - Keep or deprecate (5-8h)~~ ✅ DECIDED AND EXECUTED — `Benzene.Log4Net` deleted outright 2026-07-12 (not merely "marked community-supported")
7. **Troubleshooting Guide** - Common issues (8-10h) — ⚠️ partial: Serilog-specific troubleshooting exists in its cookbook; no observability-wide guide
8. ~~**Security Audit** - All packages (10-12h)~~ ✅ DONE 2026-07-14 — audited
   `Benzene.Diagnostics`/`.OpenTelemetry`/`Benzene.HealthChecks*` plus logging/enrichment call sites.
   One MEDIUM finding, fixed same-day: health check results (`DatabaseConnectionHealthCheck`,
   `DatabaseHealthCheck`, `ExceptionHandlingHealthCheck`, `FailedHealthCheck`) put the raw exception
   `.Message` into a `Data` field that flows out through the health check topic with no built-in
   authorization — some ADO.NET providers embed connection details in exception messages. Changed all
   4 to report the exception's type name instead (breaking behavior change to `Data`'s content,
   acceptable pre-1.0). One LOW finding, documented not fixed: `TimeOutHealthCheck`'s 10s timeout
   doesn't cancel the inner check, so a permanently-hung dependency can accumulate background
   tasks/connections over repeated invocations — would need `CancellationToken` support added to
   `IHealthCheck.ExecuteAsync()` itself (a real breaking API change) to fix properly; flagged for a
   future pass rather than done reactively here. Everything else checked (Activity/metric tagging,
   W3C trace context propagation, log injection, dependency vulnerabilities) came back clean — see
   `docs/privacy-and-data-handling.md` for the data-flow side of this audit.

**Total P1 Effort:** ~~123-158 hours~~ not re-estimated; item 6 is done, others are partial-to-fully-open

### Nice to Have for 1.0 (P2)

1. ~~**Metrics Support** - OpenTelemetry metrics (25-30h)~~ ✅ DONE — `UseBenzeneMetrics()` + `AddBenzeneInstrumentation(MeterProviderBuilder)`
2. **Distributed Context** - W3C, B3, baggage (20-25h) — ⚠️ partially done: W3C trace context is real and working; B3 propagation is moot (Zipkin package deleted); W3C Baggage specifically not implemented
3. **Health Check Dashboard** - UI component (30-40h) — still fully open
4. **Observability CLI** - Development tool (30-40h) — still fully open
5. **Video Tutorials** - Getting started (15-20h) — still fully open
6. **Blog Posts** - Deep dives (10-15h) — still fully open

**Total P2 Effort:** ~~130-170 hours~~ not re-estimated; item 1 done, item 2 partial

### Post-1.0 Features (P3)

1. **Metrics Abstraction** - IMetricsCollector (35-45h)
2. **Advanced Sampling** - Multiple strategies (25-30h)
3. **Additional Health Checks** - Redis, queues (30-40h)
4. **Structured Logging Helpers** - Typed properties (20-25h)
5. **OpenTelemetry Logs Bridge** - Full integration (25-30h)
6. **Span Processors** - Custom processing (18-22h)
7. **Performance Profiler** - Integration (30-40h)
8. **New Relic Integration** - Tracing provider (25-30h)
9. **Grafana/Prometheus** - Metrics exporter (25-30h)
10. **Honeycomb Integration** - Tracing provider (25-30h)

**Total P3 Effort:** 258-322 hours

---

## Appendix A: File Reference

> **2026-07-14 audit note:** Several files below no longer exist — `OpenTelemetryProcessTimer.cs`,
> the entire `Benzene.Microsoft.Logging`/`Benzene.Serilog`/`Benzene.Datadog`/`Benzene.Zipkin`/
> `Benzene.Aws.XRay` directories, and `BenzeneLoggerTests.cs`/`ZipkinPipelineTest.cs`. Marked
> below rather than removed, per this document's own convention of keeping historical file
> references visible. Corrected/added entries reflect current source. (Windows-style paths kept
> as-is, matching the original author's environment and this document's existing convention.)

**Key Source Files:**

**Benzene.Diagnostics:**
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Diagnostics\TimerMiddleware.cs` (legacy, still exists, undocumented)
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Diagnostics\Timers\IProcessTimer.cs` (legacy, still exists, undocumented)
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.Diagnostics\Correlation\CorrelationId.cs` (legacy, still exists, undocumented; superseded as primary mechanism by W3C trace context)
- 🆕 `C:\Users\pelled\source\libs\Benzene\src\Benzene.Diagnostics\BenzeneDiagnostics.cs` (shared `ActivitySource`/`Meter`, documented)
- 🆕 `C:\Users\pelled\source\libs\Benzene\src\Benzene.Diagnostics\W3CTraceContextExtensions.cs` (documented)
- 🆕 `C:\Users\pelled\source\libs\Benzene\src\Benzene.Diagnostics\EnrichmentExtensions.cs` (documented)
- 🆕 `C:\Users\pelled\source\libs\Benzene\src\Benzene.Diagnostics\MetricsExtensions.cs` (documented)

**Benzene.OpenTelemetry:**
- ~~`OpenTelemetryProcessTimer.cs`~~ — no longer exists, removed as part of the Checkpoint C rewrite
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.OpenTelemetry\DependencyInjectionExtensions.cs` (rewritten; now `AddBenzeneInstrumentation()` on `TracerProviderBuilder`/`MeterProviderBuilder`, fully documented)

**~~Benzene.Microsoft.Logging~~:** ✅ Deleted 2026-07-12 — `Extensions.cs`/`MicrosoftBenzeneLogAppender.cs` no longer exist

**~~Benzene.Serilog~~:** ✅ Deleted 2026-07-12 — `Extensions.cs`/`CustomJsonFormatter.cs` no longer exist (see `docs/cookbooks/structured-logging-serilog.md` for the replacement approach)

**Benzene.HealthChecks.Core:**
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.HealthChecks.Core\IHealthCheck.cs` (still undocumented)
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.HealthChecks.Core\HealthCheckStatus.cs` (still undocumented)

**Benzene.HealthChecks:**
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.HealthChecks\HealthCheckProcessor.cs` (unused `topic` parameter still present, re-verified)
- `C:\Users\pelled\source\libs\Benzene\src\Benzene.HealthChecks\TimeOutHealthCheck.cs` (hard-coded `Task.Delay(10000)` still present, re-verified)

**~~Benzene.Aws.XRay~~:** ✅ Deleted 2026-07-13 — `Extensions.cs` no longer exists; see `work/aws-roadmap-1.0.md` section 8 for the authoritative writeup

**~~Benzene.Datadog~~:** ✅ Deleted 2026-07-13 — `DatadogProcessTimer.cs` no longer exists

**~~Benzene.Zipkin~~:** ✅ Deleted 2026-07-13 — `ZipkinProcessTimer.cs` no longer exists

**Test Files:**
- `C:\Users\pelled\source\libs\Benzene\test\Benzene.Core.Test\Plugins\HealthChecks\HealthCheckNamerTests.cs` (confirmed still present, passing)
- ~~`BenzeneLoggerTests.cs`~~ — deleted along with `IBenzeneLogger`; see `test\Benzene.Core.Test\Core\Core\Logging\UseLogContextTest.cs` for the MEL-based replacement
- ~~`ZipkinPipelineTest.cs`~~ — deleted along with `Benzene.Zipkin`; see `test\Benzene.Core.Test\Diagnostics\BenzeneInstrumentationTest.cs` for the closest current analogue
- 🆕 `C:\Users\pelled\source\libs\Benzene\test\Benzene.Core.Test\Diagnostics\ActivityMiddlewareTest.cs`
- 🆕 `C:\Users\pelled\source\libs\Benzene\test\Benzene.Core.Test\Diagnostics\W3CTraceContextTest.cs`
- 🆕 `C:\Users\pelled\source\libs\Benzene\test\Benzene.Core.Test\Diagnostics\BenzeneMetricsTest.cs`
- 🆕 `C:\Users\pelled\source\libs\Benzene\test\Benzene.Core.Test\Diagnostics\BenzeneInstrumentationTest.cs`

**Related Documentation:**
- `C:\Users\pelled\source\libs\Benzene\work\1.0.0-release-status.md`
- `C:\Users\pelled\source\libs\Benzene\work\aws-roadmap-1.0.md`
- `C:\Users\pelled\source\libs\Benzene\work\azure-roadmap-1.0.md`
- 🆕 `C:\Users\pelled\source\libs\Benzene\docs\monitoring.md` (rewritten, current, single-OTel-package story)
- 🆕 `C:\Users\pelled\source\libs\Benzene\docs\migration-alpha-to-1.0.md` (covers the logging/tracing migration in detail)
- 🆕 `C:\Users\pelled\source\libs\Benzene\examples\OpenTelemetry\` (full working example)

---

## Appendix B: Comparison with Core and Cloud Roadmaps

> **2026-07-14 audit note:** The percentages/status below were a point-in-time estimate from
> 2026-07-11 and are now meaningfully out of date given the package deletions and Checkpoint A-F
> rework. Corrected inline; not re-computed to a precise new percentage since that would require
> the same kind of holistic re-scoping flagged in the Roadmap to 1.0.0 section above.

**Core Package 1.0 Criteria:**
Per `work/1.0.0-release-status.md`:
1. ✅ Complete XML documentation
2. ✅ No test code in production packages
3. ✅ No critical bugs
4. ✅ Versioning policy documented
5. ✅ Reasonable test coverage (>70%)
6. ✅ Up-to-date documentation
7. ✅ Working examples

**Observability Packages Current Status:**
1. ⚠️ 0% XML documentation — now inaccurate; partial coverage in Diagnostics/OpenTelemetry, still 0% in the 4 HealthChecks packages
2. ✅ No test code in production packages
3. ⚠️ Some critical issues (missing versions, XRay dependencies) — both named issues resolved/moot (centralized versioning; XRay package deleted); one new issue found this pass (`Benzene.HealthChecks.EntityFramework` NuGet advisories)
4. ✅ Versioning policy applies
5. ⚠️ Minimal test coverage (~10%) — real coverage now exists for Diagnostics/OpenTelemetry/core HealthChecks (37 passing tests); still ~0% for HealthChecks.Http/.EntityFramework specifically, so "minimal" is still roughly fair as an overall characterization but no longer accurate package-by-package
6. ⚠️ CLAUDE.md exists but needs user docs — CLAUDE.md files are accurate and current; `docs/monitoring.md` is a substantial, accurate user doc that didn't exist in this form when this line was written
7. ⚠️ Some examples (Zipkin) but need more — Zipkin example is gone (package deleted); replaced by a considerably more complete `examples/OpenTelemetry/` (web UI + Grafana LGTM stack)

**Gap Analysis:**
~~Observability packages are ~20-25% toward 1.0 readiness.~~ Meaningfully further along than
20-25% now, driven by: 6 of 12 originally-tracked packages no longer needing any 1.0 work at all
(deleted, not fixed); real test coverage and partial XML docs for the two remaining
tracing/diagnostics packages; a working end-to-end example; and a complete migration guide.
Remaining primary gaps, largely unchanged in kind: XML Documentation (now scoped to
HealthChecks packages + the older half of Diagnostics), performance/overhead benchmarks,
sampling/privacy documentation, and test coverage for HealthChecks.Http/.EntityFramework
specifically.

**Comparison with AWS/Azure:**
- AWS packages: ~30% toward 1.0 (178-262h estimated) — see `work/aws-roadmap-1.0.md`'s own
  2026-07-13 audit for a more current figure; that document reports most P0 items resolved
- Azure packages: ~15-20% toward 1.0 (245-368h estimated) — not re-audited as part of this pass
- Observability: ~~~20-25% toward 1.0 (199-286h estimated)~~ higher than 20-25% now; not
  re-estimated to a precise number in this docs-only audit (see Gap Analysis above)

Observability is in better shape than Azure, and now arguably ahead of where the AWS comparison
placed it in relative terms too — both roadmaps benefited from the same class of work (package
consolidation/deletion, centralized versioning, real test coverage passes) landing in the days
before this audit.
Advantage: Cleaner scope, fewer packages, less complexity — even more true now (12 → 6 packages).
Disadvantage: Performance/overhead testing critical, privacy concerns unique — both still fully open.

---

## Appendix C: Risk Register

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| OpenTelemetry API breaking changes | Medium | High | Pin versions, monitor releases, maintain compatibility layer |
| Third-party SDK breaking changes | Medium | Medium | Version pinning, test with latest in CI, adapter pattern |
| Performance overhead too high | Low | High | Comprehensive benchmarks, sampling, zero-allocation paths |
| Privacy violation (PII in logs/traces) | Medium | Critical | Sensitive data filtering, documentation, examples, security audit |
| Async context loss (correlation IDs) | Medium | High | Comprehensive async tests, use AsyncLocal, document patterns |
| Community adoption lower than expected | Medium | Medium | Marketing, examples, integrations with popular platforms |
| Log4Net usage too low to justify | Medium | Low | Usage metrics, community poll, deprecation if needed |
| OpenTelemetry standards evolve rapidly | High | Medium | Monitor standards, plan for updates, use stable APIs only |
| Overhead impacts production | Low | Critical | Benchmarks, sampling docs, circuit breakers, graceful degradation |
| Correlation with cloud providers (X-Ray, Azure Monitor) | Medium | Medium | Test thoroughly, document limitations, provide workarounds |

---

## Next Steps

> **2026-07-14 audit note:** Immediate Actions items 3-4 and Decision Points 1, 3 below are
> resolved/moot as of this pass — kept visible with corrections rather than removed, per this
> document's own historical-record convention.

**Immediate Actions (Week 1):**
1. Review this roadmap with stakeholders
2. Prioritize P0 features
3. ~~**CRITICAL:** Add all missing PackageVersions to csproj files~~ ✅ MOOT — centralized versioning (`Directory.Build.props` + `version.txt`), done 2026-07-12
4. ~~Fix XRay dependency issues (remove SQS, update System.Text.Encodings.Web)~~ ✅ MOOT — `Benzene.Aws.XRay` deleted entirely 2026-07-13
5. Begin XML documentation (Diagnostics, HealthChecks.Core first) — ⚠️ partially done for Diagnostics; not started for HealthChecks.Core

**Short-Term (Month 1):**
1. Complete all P0 items for Diagnostics and HealthChecks.Core — significant progress on Diagnostics (Activity rewrite, W3C trace context, metrics, enrichment); HealthChecks.Core largely untouched
2. ~~Modernize OpenTelemetry (DI, metrics, logging)~~ ✅ LARGELY DONE — DI and metrics resolved; logging integration still open
3. Create comprehensive test plan — real tests now exist for Diagnostics/OpenTelemetry/core HealthChecks, but no evidence of a documented "test plan" as an artifact
4. Start performance benchmarking — still not started, no benchmark project found
5. Create project board with issues for all roadmap items — out of scope for this docs-only audit to verify

**Decision Points:**
1. ~~**Log4Net:** Keep at 1.0 OR mark as community-supported?~~ ✅ DECIDED AND EXECUTED — deleted outright 2026-07-12, more decisive than either original option
2. **Phased Release:** Ship observability with core 1.0 OR phase it (diagnostics first, then logging, then tracing)? — still an open decision; the "logging" phase is now moot regardless of which option is chosen, since there's no separate logging package to phase
3. ~~**OpenTelemetry:** Target 1.10 OR update to latest (1.11+)?~~ ✅ DECIDED AND EXECUTED — updated to 1.16.0 (current stable as of this audit)
4. **Health Checks:** Integrate with Microsoft.Extensions.Diagnostics.HealthChecks OR remain independent? — still an open decision, unchanged
5. **Metrics:** Include in 1.0 OR defer to 1.1? — effectively decided by implementation: real metrics support (`UseBenzeneMetrics()`) already exists in current source, so this reads as "included" rather than deferred, though no explicit 1.0-scope decision was documented

---

**Document Owner:** Observability Product Team
**Reviewers:** Core Team, Community
**Approval Required:** Yes
**Next Review:** Monthly during 1.0 development

**Status:** DRAFT - Awaiting Approval

**Key Recommendation (2026-07-14 update):** Observability packages are considerably further
along than this document's original assessment. Six of the twelve originally-tracked packages
were deleted (not fixed) as part of a broader consolidation onto standards-based mechanisms
(`ILogger<T>` for logging, `System.Diagnostics.Activity`/OpenTelemetry for tracing), which
resolves most of the "phased release across diagnostics → logging → tracing" concern this
document originally raised — there is no separate logging phase left to plan. The 6 remaining
packages still need real work before 1.0: XML documentation (0% in the 4 HealthChecks
packages, partial in Diagnostics), performance/overhead benchmarks (none exist anywhere),
sampling/privacy/GDPR documentation (none exists), and test coverage specifically for
`Benzene.HealthChecks.Http`/`.EntityFramework` (currently zero). The original phased-release
recommendation — foundation packages (Diagnostics, HealthChecks.Core) with core 1.0, then the
rest 1-4 months later — remains reasonable, just narrower in scope now that there's one tracing
package instead of four and zero logging packages instead of three.
