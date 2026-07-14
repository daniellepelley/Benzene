# Benzene

Benzene is a hexagonal framework designed for services running in serverless environments, containers, or on physical servers. It supports multiple cloud providers and provides a unified programming model for message-based architectures.

### Main Themes

- **General**
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
  - [Monitoring & Diagnostics](monitoring)

- **Cloud Providers**
  - **AWS**
    - [AWS Lambda Setup](getting-started-aws)
    - [AWS IAM Permissions Reference](aws-iam-permissions)
  - **Azure**
    - [Azure Functions Setup](azure-functions)

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
  - [Client SDKs](client-sdks) (Coming Soon)
  - [OpenAPI Specification](spec)

- **Cookbooks**
  - [Cookbook Index](cookbooks/README)
  - [Logging to Application Insights](cookbooks/logging-application-insights)
