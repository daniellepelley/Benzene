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
**Focus**: AWS Lambda, SQS, SNS, EventBridge, X-Ray integrations
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
- **EventBridge**: Improve EventBridge integration
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
| Benzene.Datadog, Zipkin, XRay | Observability |
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

---

**Last Updated**: 2026-07-10
**Version**: 1.0
