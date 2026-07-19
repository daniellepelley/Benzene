# Benzene

Benzene is a hexagonal framework designed for services running in serverless environments, containers, or on physical servers. It supports multiple cloud providers and provides a unified programming model for message-based architectures.

### Main Themes

- **General**
  - [Getting Started](getting-started.md) — build and run your first Benzene service in 5 minutes
  - [Project Templates](getting-started-templates.md) — `dotnet new` starter projects for every host, consumable from Visual Studio and Rider
  - [Migration Guide (Alpha → 1.0)](migration-alpha-to-1.0)
  - [Unified Hosting Model](hosting.md)
  - [Capability Matrix](capability-matrix.md) — what Benzene does, deliberately doesn't (and why), and how to fill the gap
  - [Message Handlers](message-handlers.md)
  - [Message Results](message-result.md)
  - [Middleware](middleware.md)
  - [Common Middleware](common-middleware.md)
  - [Correlation Ids](correlation-ids.md)
  - [Testing Benzene](testing-benzene.md)
  - [Payload Testing](payload-testing.md) — construct demo payloads and send them into a running service by topic
  - [Health Checks](health-checks.md)
  - [Kubernetes Health Checks](kubernetes-health-checks.md)
  - [Monitoring & Diagnostics](monitoring.md)
  - [Sampling Strategies](sampling-strategies.md)
  - [Privacy & Data Handling](privacy-and-data-handling.md)

- **Benzene Specification (Draft)** — the language-neutral core Benzene itself is defined by, independent of the .NET implementation, so a future port to another language is a translation of a design rather than a rewrite
  - [Overview & How the Spec Is Organized](specification/README.md) — the two conformance levels (Core vs. the Cloud Service Profile) and how the documents below relate
  - [Design Principles](specification/design-principles.md) — "opinionated but optional": the adoption ladder, and the rule that every steer must be replaceable
  - [Core Concepts](specification/core-concepts.md) — the abstract model: pipeline, context, message handler, topic, result, lifecycle
  - [Wire Contracts](specification/wire-contracts.md) — the message envelope, header conventions, the status vocabulary, and its per-protocol (HTTP/gRPC) mappings
  - [Transport Bindings](specification/transport-bindings.md) — what a transport adapter must satisfy, with every existing binding as a worked example
  - [Mesh Contracts](specification/mesh.md) — service self-description, trace events, and collector topics for fleet-wide visibility
  - [Cloud Service Profile](specification/cloud-service-profile.md) — the named conformance target that guarantees mesh, Spec UI, and fleet tooling work on a service with no per-service negotiation
  - [Payload Schema Versioning](specification/versioning.md) — draft proposal for versioning a topic's request/response shape independently of handler versioning
  - [Porting Guide](specification/porting-guide.md) — concept-vs-idiom mapping and suggested order for implementing Benzene in another language
  - [Conformance Fixtures](specification/conformance/README.md) — the language-neutral test fixtures every implementation runs to prove conformance

- **Service Mesh**
  - [Mesh UI](mesh-ui.md) — the two dashboards `Benzene.Mesh.Ui` ships: the Mesh Explorer (a published-artifact catalog viewer, primarily static-hosted) and the Fleet view (a live dashboard polling a running `Benzene.Mesh.Collector`)

- **Cloud Providers**
  - **AWS**
    - [AWS Lambda Setup](getting-started-aws.md)
    - [AWS IAM Permissions Reference](aws-iam-permissions.md)
  - **Azure**
    - [Azure Functions Setup](azure-functions.md) — HTTP plus every non-HTTP trigger (Event Hubs, Kafka, Service Bus, Cosmos DB Change Feed, Queue/Blob Storage, Event Grid, Timer)
    - [Self-hosted Azure workers](getting-started-worker.md#part-b-built-in-workers-kafka-http-service-bus-event-hub-cosmos-db) — Service Bus, Event Hubs, and Cosmos DB Change Feed consumers without Azure Functions
    - [Managed Identity & RBAC](cookbooks/managed-identity.md) — no connection strings: credential wiring and the roles each integration needs
    - [Service Bus](cookbooks/service-bus-handling.md) / [Event Hubs](cookbooks/event-hub-processing.md) / [Cosmos DB Change Feed](cookbooks/cosmos-change-feed-processing.md) cookbooks
  - **Cloudflare**
    - [Cloudflare Containers Setup](getting-started-cloudflare.md)

- **Messaging**
  - [Getting Started with Kafka](getting-started-kafka.md)
  - [Getting Started with RabbitMQ](getting-started-rabbitmq.md)
  - [Getting Started with gRPC](getting-started-grpc.md)
  - [Getting Started with Worker Services](getting-started-worker.md)

- **Integrations**
  - [ASP.NET Core](asp-net-core.md)
  - **Validation**
    - [Fluent Validation](fluent-validation.md)
    - [Data Annotations](data-annotations.md)

- **Clients & Resilience**
  - [Clients](clients.md)
  - [Caching](caching.md)
  - [Resilience](resilience.md) — retry-with-backoff, plus the full Polly toolkit via `Benzene.Resilience.Polly`
  - [Polly Resilience Pipelines](cookbooks/polly-resilience.md) — circuit breaker, timeout, hedging, fallback

- **Code Generation**
  - [Terraform](terraform.md)
  - [Client SDKs](client-sdks.md)
  - [Spec Endpoint (OpenAPI / AsyncAPI / Benzene format)](spec.md) — a runtime feature of a Benzene service, not to be confused with the [Benzene Specification](specification/README.md) above: this is a `UseSpec` middleware that serves *your* service's own schema
  - [Spec UI](spec-ui.md) — a Swagger-UI-style browser for the spec endpoint above

- **Reference**
  - [Package Reference](reference/packages.md) — every NuGet package and when to install it
  - [Middleware Reference](reference/middleware.md) — every pipeline step and its options
  - [Attributes Reference](reference/attributes.md) — the attributes you apply to handlers
  - [Result & Status Reference](reference/results.md) — result statuses and their HTTP mappings
  - [Configuration Reference](reference/configuration.md) — the StartUp lifecycle and config options

- **Cookbooks**
  - [Cookbook Index](cookbooks/README.md)
  - [Logging to Application Insights](cookbooks/logging-application-insights.md)
  - [Authentication Patterns](cookbooks/auth-patterns.md) — OAuth2 bearer token (JWT) validation, Basic auth, and scope-based authorization for services with no security-terminating gateway in front of them

- **Live Demos**
  - [Mesh UI](../demos/mesh/index.html) — a running dashboard over sample service health, contract drift, and cross-service traffic
  - [Spec UI](../demos/spec/index.html) — browse a sample Benzene message spec, Swagger-UI style
  - Fleet view has no static demo here — it only ever renders what it polls live from a running
    `Benzene.Mesh.Collector`, so there's nothing to show without one. See [Mesh UI](mesh-ui.md#fleet-view)
    for what it looks like, or run [`examples/Mesh`](../examples/Mesh)'s `./run.sh` for the real thing.
