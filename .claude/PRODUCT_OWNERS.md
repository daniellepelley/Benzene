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

### Performance & Reliability Champion
**Agent**: `performance-champion`
**Focus**: Cross-cutting — not a package owner. Hot-path latency/allocations in the
middleware pipeline, serialization, and handler dispatch; benchmarking discipline;
and load-bearing reliability (timeouts, failure isolation, resource cleanup,
backpressure/batch-failure correctness) across every package.

**Key Responsibilities:**
- Review changes for per-request/per-message cost, not just correctness
- Push for measured benchmarks over "should be faster" reasoning (no
  BenchmarkDotNet project exists yet — establishing one is a standing priority)
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
- Benchmarking a specific path, or standing up benchmark infrastructure

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

### Performance & Reliability Champion
- **Benchmark infrastructure**: No BenchmarkDotNet project exists yet — stand
  one up covering the middleware pipeline, request mapping, and serialization
  hot paths, so future perf claims are measured, not estimated
- **Middleware pipeline audit**: Systematic allocation/latency review of
  `MiddlewarePipeline`/`MiddlewarePipelineBuilder` and handler dispatch,
  building on Phase 1 of `docs/plans/request-response-improvements-plan.md`
- **Reliability sweep**: Confirm every call to something that can be slow or
  down has an explicit timeout and failure isolation, matching the
  `TimeOutHealthCheck`/`ExceptionHandlingHealthCheck` pattern
- **Serialization cost**: Audit new/existing serializer packages for avoidable
  round-trips (e.g. a byte-native format forced through a string) against the
  Phase 4 byte-oriented path (`IPayloadSerializer`)

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

`performance-champion` has no row here by design — it's cross-cutting, not
package-scoped. Loop it in on any hot-path or reliability-sensitive change
regardless of which package it lands in.

---

**Last Updated**: 2026-07-15
**Version**: 1.1
