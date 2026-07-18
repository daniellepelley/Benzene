# Benzene

Benzene is a hexagonal framework designed for services running in serverless environments, containers, or on physical servers. It supports multiple cloud providers and provides a unified programming model for message-based architectures.

### Main Themes

- **General**
  - [Getting Started](getting-started.md) — build and run your first Benzene service in 5 minutes
  - [Project Templates](getting-started-templates.md) — `dotnet new` starter projects for every host, consumable from Visual Studio and Rider
  - [Migration Guide (Alpha → 1.0)](migration-alpha-to-1.0)
  - [Benzene Specifications (Draft)](specification/README.md) — two levels: the Core Specification (the language-neutral portable core: concepts, wire contracts, transport bindings, porting guide) and the [Cloud Service Profile](specification/cloud-service-profile.md) (the conformance target that guarantees mesh, Spec UI, and fleet tooling work on a service)
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
  - [Resilience](resilience.md)

- **Code Generation**
  - [Terraform](terraform.md)
  - [Client SDKs](client-sdks.md)
  - [OpenAPI Specification](spec.md)

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
