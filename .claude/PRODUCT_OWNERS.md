# Benzene Product Owners

This document describes the product ownership structure for the Benzene library. Each product owner is a Claude agent responsible for the strategic direction, feature prioritization, and quality standards of their domain.

## Product Owner Structure

### Core Product Owner
**Agent**: `core-product-owner`
**Focus**: Foundational abstractions, middleware pipeline, message handling
**Packages**: Benzene.Abstractions.*, Benzene.Core.*, Benzene.Http, Benzene.Results, Benzene.Testing

**Key Responsibilities:**
- Protect architectural integrity (hexagonal/ports-and-adapters)
- Manage API surface and backward compatibility
- Ensure middleware pipeline remains flexible and composable
- Maintain highest quality standards for foundational packages

**Contact for:**
- Changes to core abstractions or interfaces
- Middleware pipeline enhancements
- Breaking changes to public APIs
- Core architectural decisions

---

### AWS Product Owner
**Agent**: `aws-product-owner`
**Focus**: AWS Lambda, SQS, SNS, S3, X-Ray integrations
**Packages**: Benzene.Aws.*, Benzene.Clients.Aws, AWS TestHelpers

**Key Responsibilities:**
- AWS service integration roadmap
- Lambda performance optimization (cold starts, memory)
- AWS security and IAM best practices
- CloudWatch and X-Ray observability

**Contact for:**
- New AWS service integrations
- AWS Lambda adapter improvements
- AWS-specific performance optimizations
- IAM and security guidance

---

### Azure Product Owner
**Agent**: `azure-product-owner`
**Focus**: Azure Functions, Event Hubs, Service Bus, ASP.NET Core
**Packages**: Benzene.Azure.*, Benzene.AspNet.Core, Azure TestHelpers

**Key Responsibilities:**
- Azure service integration roadmap
- Azure Functions scaling and performance
- Managed Identity and security
- Application Insights integration

**Contact for:**
- New Azure service integrations
- Azure Functions adapter improvements
- Azure-specific configuration
- Application Insights and diagnostics

---

### Observability Product Owner
**Agent**: `observability-product-owner`
**Focus**: Logging, tracing, metrics, health checks, diagnostics
**Packages**: Benzene.Diagnostics, Benzene.OpenTelemetry, Benzene.*.Logging, Benzene.HealthChecks.*

**Key Responsibilities:**
- OpenTelemetry standards compliance
- Distributed tracing strategy
- Structured logging patterns
- Health check implementations

**Contact for:**
- New observability platform integrations
- Logging and tracing enhancements
- Performance overhead concerns
- Correlation and context propagation

---

### Validation & Tooling Product Owner
**Agent**: `validation-product-owner`
**Focus**: Validation frameworks, schema generation, code generation
**Packages**: Benzene.FluentValidation, Benzene.DataAnnotations, Benzene.Schema.*, Benzene.CodeGen.*

**Key Responsibilities:**
- Validation framework integrations
- OpenAPI and JSON Schema generation
- Source generators and code generation
- Developer productivity tools

**Contact for:**
- Validation framework support
- Schema generation improvements
- Code generation templates
- Developer tooling enhancements

---

### Infrastructure Product Owner
**Agent**: `infrastructure-product-owner`
**Focus**: DI containers, caching, resilience, serialization, clients
**Packages**: Benzene.Microsoft.Dependencies, Benzene.Autofac, Benzene.Cache.*, Benzene.Resilience, etc.

**Key Responsibilities:**
- DI container integrations
- Caching and resilience patterns
- Serialization support
- Client library patterns

**Contact for:**
- DI container support
- Caching strategies
- Resilience patterns (retry, circuit breaker)
- Serialization and client libraries

---

### Mesh Product Owner
**Agent**: `mesh-product-owner` *(merged 2026-07: absorbs the former `mesh-ui-product-owner` — one owner for the whole mesh product, data packages through UI)*
**Focus**: The product that sits on top of Cloud-Service-spec Benzene services and lets a user, business person, business analyst, or product owner **review the estate**: what each service does (topics consumed/produced, payloads, versions — the most vital part), how often topics are exercised and over which transports (via an OpenTelemetry/collector metrics feed), current health (present, but not the centerpiece), the evolution of the data, and the viability of the platform as it stands — so they can make decisions about the platform's evolution.
**Packages**: the `Benzene.Mesh.*` family (Contracts, Aggregator, Collector, Reporting, Wire, Tracing.Tempo, Discovery.*, Azure.Blob, Ui) + `deploy/Mesh/Benzene.Mesh.Host`

**Key Responsibilities:**
- Own both living docs: `work/service-mesh-roadmap-1.0.md` (data/packages) and `work/mesh-ui-product-vision.md` (product vision/UI)
- **Guardian of the Cloud Service spec**: it must cover everything the product needs while staying **taut and small** — a lot of insight from a relatively small surface area of data out of each service; every spec addition pays rent
- Deliver the product outcomes: see what services do, see data evolution + platform viability, see usage per topic/transport, check current state, understand the domain and flows, judge value vs. deprecation
- Drive the usage/metrics feed (OTel → collector path) with `observability-product-owner`; drive toward an industry-leading product (benchmark vs. Datadog service maps, Grafana/Kibana, Moesif, AsyncAPI Studio, Backstage)
- Protect the load-bearing constraints: static/no-CDN Mesh UI floor, thin Contracts, aggregator fetch isolation, spec-pinned wire shapes
- Keep the `examples/Mesh` demo and each package's `CLAUDE.md` honest about what's real vs. mocked/unverified

**Contact for:**
- New service-mesh visibility features (catalog, topology, usage analytics, contract testing)
- Mesh UI product direction, new views/workflows, collaboration features, and "how do we surface X so a user can act on it?"
- Cloud Service spec surface questions (coverage vs. tautness)
- Changes to `Benzene.Mesh.*` package boundaries or dependencies; roadmap prioritization

---

### Performance & Reliability Champion
**Agent**: `performance-champion`
**Focus**: Cross-cutting — not a package owner. Hot-path latency/allocations in the
middleware pipeline, serialization, and handler dispatch; benchmarking discipline;
and load-bearing reliability (timeouts, failure isolation, resource cleanup,
backpressure/batch-failure correctness) across every package.

**Key Responsibilities:**
- Review changes for per-request/per-message cost, not just correctness
- Push for measured benchmarks over "should be faster" reasoning
  (`benchmarks/Benzene.Benchmarks` covers the middleware pipeline and request
  mapping; expanding coverage to other hot paths is a standing priority)
- Ensure anything that calls out to something slow/unreliable has an explicit
  timeout and failure isolation (the `TimeOutHealthCheck`/
  `ExceptionHandlingHealthCheck` pattern is the reference)
- Flag cascading-failure and resource-leak risks other reviewers may miss
  because they're scoped to one package

**Contact for:**
- Any change to the middleware pipeline, request mapping, or response
  rendering hot path
- New serializer/client packages, to check for avoidable allocation or
  round-tripping (e.g. string round-trips a byte-native format didn't need)
- Reliability review before a release: timeouts, cleanup, degradation behavior
- Benchmarking a specific path, or extending `benchmarks/Benzene.Benchmarks`

**Note:** This role reviews and advises across every product owner's domain —
it does not override a PO's design call. A performance win that conflicts with
a PO's abstraction needs that PO's sign-off (see Escalation, below).

---

## How to Work with Product Owners

### For Feature Requests

1. **Identify the domain**: Determine which product owner owns the relevant packages
2. **Invoke the agent**: Use `/task` with the appropriate product owner agent
3. **Provide context**:
   - What problem are you solving?
   - What packages are affected?
   - What's the proposed solution?
4. **Get feedback**: The product owner will assess business value, technical approach, and risks

**Example:**
```
/task @aws-product-owner "Evaluate adding support for AWS Lambda SnapStart in Benzene.Aws.Lambda.Core. Users report cold start times of 2-3 seconds, SnapStart could reduce this to <500ms."
```

### For Architectural Decisions

1. **Multi-domain decisions**: Involve all relevant product owners
2. **Core changes**: Always consult `core-product-owner` for abstraction changes
3. **Cross-cutting concerns**: Involve `infrastructure-product-owner` for DI, caching, etc.

**Example:**
```
/task @core-product-owner "Review proposed change to IMiddleware<TContext> interface to add cancellation token support. This affects all middleware implementations."
```

### For Code Reviews

Product owners can review PRs and changes for:
- Alignment with product roadmap
- Consistency with package patterns
- Impact on users and backward compatibility
- Quality and testing standards

**Example:**
```
/task @observability-product-owner "Review the new Grafana integration in Benzene.OpenTelemetry. Does it follow our observability patterns?"
```

### For Release Planning

Before releasing packages, consult relevant product owners for:
- Versioning decisions (major/minor/patch)
- Breaking change assessment
- Migration guide requirements
- Documentation completeness

**Example:**
```
/task @azure-product-owner "We're planning to release Benzene.Azure.Functions 1.0. Review readiness: API stability, documentation, examples, breaking changes."
```

## Product Owner Coordination

### Cross-Domain Features

Some features span multiple domains. Coordinate with multiple product owners:

**Example: Adding retry middleware for AWS Lambda**
- `core-product-owner`: Review middleware abstraction
- `aws-product-owner`: Validate AWS-specific retry patterns
- `infrastructure-product-owner`: Ensure resilience patterns are consistent
- `performance-champion`: Confirm retry/backoff can't cascade into a request
  storm and that the added middleware's per-invocation cost is measured, not
  assumed

### Escalation

If product owners disagree or can't reach consensus:
1. Document the trade-offs and perspectives
2. Escalate to maintainer for final decision
3. Update product owner guidelines based on decision

## Product Owner Evolution

Product owners are living documents that evolve with the product:

- **Update regularly**: As packages mature, update priorities and focus areas
- **Add new owners**: As Benzene expands (e.g., GCP, messaging platforms)
- **Refine boundaries**: Adjust ownership as package relationships evolve
- **Learn from decisions**: Document patterns and anti-patterns

## Current Priorities (2026)

### Core PO
- **1.0 Release**: API stability, XML documentation, final breaking changes
- **Testing**: Ensure Benzene.Testing provides excellent DX
- **Performance**: Middleware pipeline benchmarks

### AWS PO
- **SnapStart**: Investigate Lambda SnapStart support
- **EventBridge**: No real EventBridge/CloudWatch Events support exists yet (the
  former `Benzene.Aws.Lambda.EventBridge` package was renamed to
  `Benzene.Aws.Lambda.S3` — it only ever implemented S3 event notifications). Building
  genuine EventBridge support is unstarted future work.
- **Observability**: Better X-Ray integration

### Azure PO
- **Durable Functions**: Investigate Durable Functions support
- **Managed Identity**: Improve Managed Identity patterns
- **App Configuration**: Azure App Configuration integration

### Observability PO
- **OpenTelemetry**: Full OTel compliance for traces, metrics, logs
- **Sampling**: Smart sampling strategies for high-throughput
- **Dashboards**: Reference dashboard templates

### Validation PO
- **Source Generators**: Expand source generator capabilities
- **OpenAPI 3.1**: Full OpenAPI 3.1 support
- **Client SDK**: Improve generated client quality

### Infrastructure PO
- **Resilience**: Polly v8 integration
- **Redis**: RedisJSON and Redis Streams support
- **gRPC**: Improve gRPC client/server patterns

### Mesh PO
- **Multi-transport data collection**: ✅ Complete — all four phases landed. Phase A
  (`IMeshServiceSource` port), Phase B (`Benzene.Mesh.Aws.Lambda`), Phase C
  (`Benzene.Mesh.Reporting` push path — opportunistic self-report, two swappable
  `IMeshReportPublisher`s), Phase D (`deploy/Mesh/Benzene.Mesh.Host`, a config-driven
  Docker/Compose-deployable Mesh Aggregator+UI, published to GHCR) — see
  `work/service-mesh-roadmap-1.0.md`'s 2026-07-15 updates for the full history
- **Staleness representation**: flagged by Phase C — an opportunistically self-reporting service's
  entry has no way to signal "this is old data," since `MeshServiceStatus` has no `Stale` value —
  still open
- **Live Tempo verification**: Phase 3's PromQL/metric-name assumptions (`traces_service_graph_request_total`/`..._failed_total`/`..._request_server_seconds_bucket`, `client`/`server` labels) are documented convention, not confirmed against a real Tempo + Prometheus instance (blocked by this dev environment's network egress policy so far) — still open
- **Structural edge derivation**: Phase 1's `TopologyEdgeSource.Structural` gap — deriving "designed to call" edges (e.g. from generated `CodeGen.Client`s, or matching `HealthCheckDependency` entries against other registered services) is a real, still-open design question, not started
- **Topology graph visualization**: Mesh UI now renders `topology.json` as a sortable table (v1) — a full interactive node-link graph is a natural next step, not yet built
- **Phase 4 — field-level contract compatibility**: structural per-field schema diffing between a caller's expectation and a callee's current contract; worth checking whether `Benzene.Schema.OpenApi/Compatibility` already covers this before building from scratch
- **Phase 5 — polish**: mesh-level health rollup, historical trend storage, alerting — unstarted

- **Product vision (absorbed from the former Mesh UI PO)**: `work/mesh-ui-product-vision.md` — the user outcomes (understand the domain, see message flows, spot issues, see usage, judge value vs. deprecation, discuss/decide) and a sequenced roadmap
  - **Near term (mostly static, low data risk)**: interactive topology graph (replaces the v1 table), end-to-end flow view over the AsyncAPI 3.0 operations+reply model, and an "issue inbox" promoting failing/drifting/stale services into a triage list — the staleness signal is still OPEN in the mesh data layer
  - **Mid term (needs data layer)**: usage analytics over time — how often each topic is exercised and **over which transports** (OTel/collector feed) — and a value-vs-deprecation view (Tempo metric-name convention remains unverified against a real backend)
  - **Long term (crosses the static constraint)**: discussion/annotations — backend-backed; pick the vessel explicitly, keep the static explorer working without it
- **Cloud Service spec tautness**: standing review — the spec covers what the product needs with the smallest surface that delivers the insight

### Performance & Reliability Champion
- **Benchmark infrastructure**: ✅ `benchmarks/Benzene.Benchmarks` covers the
  middleware pipeline and request mapping (no recorded baseline numbers yet —
  see its README). Serialization packages and other hot paths are still
  unbenchmarked — expanding coverage there is the next step
- **Middleware pipeline audit**: ✅ done — found and fixed a resource leak in
  `MicrosoftServiceResolverAdapter`/`AutofacServiceResolverAdapter` (DI scopes
  were never disposed) and a dead cache field in `MiddlewarePipeline`
- **Reliability sweep**: partially done — `Benzene.Mesh.Aggregator` fixed
  (concurrent polling + explicit timeout, matching the
  `TimeOutHealthCheck`/`ExceptionHandlingHealthCheck` pattern). Confirming
  every other call to something that can be slow or down has the same
  explicit timeout/isolation is still open
- **Serialization cost**: still open — audit new/existing serializer packages
  for avoidable round-trips (e.g. a byte-native format forced through a
  string) against the Phase 4 byte-oriented path (`IPayloadSerializer`)

---

## Quick Reference: Which PO for Which Package?

| Package Pattern | Product Owner |
|----------------|---------------|
| Benzene.Abstractions.* | Core |
| Benzene.Core.* | Core |
| Benzene.Http | Core |
| Benzene.Results | Core |
| Benzene.Testing | Core |
| Benzene.Aws.* | AWS |
| Benzene.Clients.Aws | AWS |
| Benzene.Azure.* | Azure |
| Benzene.AspNet.* | Azure |
| Benzene.*Logging | Observability |
| Benzene.Diagnostics | Observability |
| Benzene.OpenTelemetry | Observability |
| Benzene.HealthChecks.* | Observability |
| Benzene.FluentValidation | Validation |
| Benzene.DataAnnotations | Validation |
| Benzene.Schema.* | Validation |
| Benzene.CodeGen.* | Validation |
| Benzene.*.Dependencies | Infrastructure |
| Benzene.Cache.* | Infrastructure |
| Benzene.Resilience | Infrastructure |
| Benzene.Client.* | Infrastructure |
| Benzene.Grpc | Infrastructure |
| Benzene.Kafka.* | Infrastructure |
| Benzene.SelfHost.* | Infrastructure |
| Benzene.Mesh.* | Mesh |

`performance-champion` has no row here by design — it's cross-cutting, not
package-scoped. Loop it in on any hot-path or reliability-sensitive change
regardless of which package it lands in.

*(The former `mesh-ui-product-owner` was merged into `mesh-product-owner` in
2026-07 — one owner now covers the mesh data packages and the product
experience, including UI product direction, usage/value/deprecation insight,
and collaboration features; see `work/mesh-ui-product-vision.md`.)*

---

**Last Updated**: 2026-07-20
**Version**: 1.3
