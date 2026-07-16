# Benzene

Benzene is a hexagonal framework designed for services running in serverless environments, containers, or on physical servers. It supports multiple cloud providers and provides a unified programming model for message-based architectures.

### Main Themes

- **General**
  - [Getting Started](getting-started) — build and run your first Benzene service in 5 minutes
  - [Project Templates](getting-started-templates) — `dotnet new` starter projects for every host, consumable from Visual Studio and Rider
  - [Migration Guide (Alpha → 1.0)](migration-alpha-to-1.0)
  - [Benzene Specification (Draft)](specification/README) — the language-neutral portable core: concepts, wire contracts, transport bindings, porting guide
  - [Unified Hosting Model](hosting)
  - [Message Handlers](message-handlers)
  - [Message Results](message-result)
  - [Middleware](middleware)
  - [Common Middleware](common-middleware)
  - [Correlation Ids](correlation-ids)
  - [Testing Benzene](testing-benzene)
  - [Health Checks](health-checks)
  - [Kubernetes Health Checks](kubernetes-health-checks)
  - [Monitoring & Diagnostics](monitoring)
  - [Sampling Strategies](sampling-strategies)
  - [Privacy & Data Handling](privacy-and-data-handling)

- **Cloud Providers**
  - **AWS**
    - [AWS Lambda Setup](getting-started-aws)
    - [AWS IAM Permissions Reference](aws-iam-permissions)
  - **Azure**
    - [Azure Functions Setup](azure-functions)
  - **Cloudflare**
    - [Cloudflare Containers Setup](getting-started-cloudflare)

- **Messaging**
  - [Getting Started with Kafka](getting-started-kafka)
  - [Getting Started with gRPC](getting-started-grpc)
  - [Getting Started with Worker Services](getting-started-worker)

- **Integrations**
  - [ASP.NET Core](asp-net-core)
  - **Validation**
    - [Fluent Validation](fluent-validation)
    - [Data Annotations](data-annotations)

- **Clients & Resilience**
  - [Clients](clients)
  - [Caching](caching)
  - [Resilience](resilience)

- **Code Generation**
  - [Terraform](terraform)
  - [Client SDKs](client-sdks)
  - [OpenAPI Specification](spec)

- **Reference**
  - [Package Reference](reference/packages) — every NuGet package and when to install it
  - [Middleware Reference](reference/middleware) — every pipeline step and its options
  - [Attributes Reference](reference/attributes) — the attributes you apply to handlers
  - [Result & Status Reference](reference/results) — result statuses and their HTTP mappings
  - [Configuration Reference](reference/configuration) — the StartUp lifecycle and config options

- **Cookbooks**
  - [Cookbook Index](cookbooks/README)
  - [Logging to Application Insights](cookbooks/logging-application-insights)

- **Live Demos**
  - [Mesh UI](../demos/mesh/index.html) — a running dashboard over sample service health, contract drift, and cross-service traffic
  - [Spec UI](../demos/spec/index.html) — browse a sample Benzene message spec, Swagger-UI style
