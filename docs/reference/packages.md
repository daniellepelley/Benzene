# Package Reference

Benzene ships as a set of small, focused NuGet packages. You compose the ones you need rather
than taking a single monolithic dependency: a core, one host/transport, and whatever
cross-cutting packages (validation, observability, health checks, …) your service uses.

This page is the map of every published package — what it gives you and when to install it.

> **All packages are prerelease (`-alpha`) until 1.0**, so every `dotnet add package` command
> needs `--prerelease`:
>
> ```bash
> dotnet add package Benzene.AspNet.Core --prerelease
> ```

## How the packages fit together

You rarely install the low-level packages directly. In practice you pick:

1. **One host/transport package** — `Benzene.AspNet.Core`, `Benzene.Aws.Lambda.ApiGateway`,
   `Benzene.Azure.Function.Core`, etc. This transitively brings in the core pipeline, message
   handler infrastructure, and abstractions.
2. **A DI package** — `Benzene.Microsoft.Dependencies` (default) or `Benzene.Autofac`. Host
   packages already depend on the Microsoft one, so you usually get this for free.
3. **Cross-cutting packages as needed** — validation, serialization, observability, health
   checks, caching, resilience.

The **Abstractions** and **Core** packages listed first are the foundation everything builds
on; you normally get them transitively and only reference them directly when writing your own
middleware, adapters, or shared contract libraries.

## Foundation — Abstractions

Contract-only packages (interfaces and attributes, minimal implementation). Reference these
directly when you're building shared libraries or your own Benzene extensions and want to
depend on the abstractions without pulling in an implementation.

| Package | What it gives you |
|---|---|
| `Benzene.Abstractions` | Root abstractions — `ISerializer`, service-container and service-resolver interfaces, log-context and message builders shared across the framework. |
| `Benzene.Abstractions.MessageHandlers` | Interfaces for message handlers, routing, and the handler pipeline (`IMessageRouterBuilder`, `IHandlerPipelineBuilder`, `IMessageHandlerFactory`). |
| `Benzene.Abstractions.Messages` | Client/message-sending contracts (`IBenzeneClientContext`, `IBenzeneClientRequest`, `IMessageSenderBuilder`) for outbound messaging. |
| `Benzene.Abstractions.Middleware` | The middleware pipeline contracts — `IMiddleware`, `IMiddlewarePipeline`, `IMiddlewarePipelineBuilder`, `IMiddlewareApplication`. |
| `Benzene.Abstractions.Pipelines` | Application/host contracts — `IBenzeneApplicationBuilder`, `IStartUp`, `IClientHeaders`. |
| `Benzene.Abstractions.Validation` | Validation contracts — `IValidationSchemaBuilder`, `ValidationStatusAttribute`. |

## Foundation — Core

The default implementations of the abstractions. Brought in transitively by every host
package; reference directly only when hand-building a pipeline.

| Package | What it gives you |
|---|---|
| `Benzene.Core` | Foundational internals — registration checking, context dictionaries, log-context building. |
| `Benzene.Core.MessageHandlers` | Message handler pipeline, routing, and the `[Message]` topic attribute — the heart of dispatch. |
| `Benzene.Core.Messages` | Outbound message-sender pipeline and context-predicate building. |
| `Benzene.Core.Middleware` | The concrete middleware pipeline: `BenzeneApplicationBuilder`, exception-handler and context-converter middleware, entry-point application. |
| `Benzene.Results` | `IBenzeneResult<T>` result helpers and extensions (`BenzeneResult.Ok(...)`, status mapping). See [Message Results](../message-result). |
| `Benzene.Http` | Shared HTTP building blocks used by every HTTP transport — the `[HttpEndpoint]` routing attribute and CORS middleware. |

## Dependency injection

Pick one. Host packages depend on the Microsoft container by default.

| Package | What it gives you |
|---|---|
| `Benzene.Microsoft.Dependencies` | Integration with `Microsoft.Extensions.DependencyInjection` and the `UsingBenzene(...)` registration entry point. The default DI backend. |
| `Benzene.Autofac` | Integration with Autofac as the DI container instead of the Microsoft one. |

## Hosts & transports

Each package adapts one runtime/event source into the Benzene pipeline. Install the one(s)
matching where your service runs; your message handlers stay identical across all of them.

### ASP.NET Core

| Package | What it gives you |
|---|---|
| `Benzene.AspNet.Core` | Host Benzene inside an ASP.NET Core app — `UseBenzene(...)` / `UseHttp(...)`. The simplest local-first host. See [ASP.NET Core](../asp-net-core) and [Getting Started](../getting-started). |

### AWS Lambda

| Package | What it gives you |
|---|---|
| `Benzene.Aws.Lambda.Core` | The Lambda host (`AwsLambdaHost<TStartUp>`) and event-stream pipeline shared by all AWS event sources. |
| `Benzene.Aws.Lambda.ApiGateway` | Handle API Gateway (HTTP) events — `UseApiGateway(...)`. |
| `Benzene.Aws.Lambda.Sqs` | Handle SQS queue events — `UseSqs(...)`. |
| `Benzene.Aws.Lambda.Sns` | Handle SNS notification events — `UseSns(...)`. |
| `Benzene.Aws.Lambda.S3` | Handle S3 bucket notification events — `UseS3(...)`. |
| `Benzene.Aws.Lambda.Kafka` | Handle MSK / self-managed Kafka events — `UseKafka(...)`. |

See [AWS Lambda Setup](../getting-started-aws) for a full walkthrough and
[AWS IAM Permissions](../aws-iam-permissions) for the per-source policies.

### Azure Functions

| Package | What it gives you |
|---|---|
| `Benzene.Azure.Function.Core` | The Azure Functions (isolated worker) host and app builder. |
| `Benzene.Azure.Function.AspNet` | Handle HTTP-triggered functions via the ASP.NET Core integration — `UseHttp(...)`. |
| `Benzene.Azure.Function.EventHub` | Handle Event Hub stream events. |
| `Benzene.Azure.Function.Kafka` | Handle Kafka-triggered functions. |
| `Benzene.Azure.Function.CosmosDb` | Handle Cosmos DB Change Feed triggers as a fan-in document stream — `UseCosmosDbChangeFeed<TDocument>(...)`. |
| `Benzene.Azure.Function.QueueStorage` | Handle Queue Storage triggers — `UseQueueStorage(...)`, routing via a Benzene message envelope (`UseBenzeneMessage`) or a fixed per-queue topic (`UsePresetTopic`). |
| `Benzene.Azure.Function.BlobStorage` | Handle Blob Storage triggers — `UseBlobStorage(...)` + `UseBlob(...)`, delivering the blob's name and content to a non-routed pipeline. |
| `Benzene.Azure.Function.EventGrid` | Handle Event Grid triggers — `UseEventGrid(...)`, routing events to message handlers by event type (both Event Grid and CloudEvents schemas). |
| `Benzene.Azure.Function.Timer` | Handle Timer triggers — `UseTimerTrigger(...)`, consuming ticks directly (`UseTick`) or dispatching them to a message handler via `UsePresetTopic`. |

See [Azure Functions Setup](../azure-functions).

### Azure (self-hosted / worker)

Consume Azure messaging in a long-running process you own (console app, container, AKS, App
Service WebJob) via `Benzene.HostedService`/`Benzene.SelfHost` — no Azure Functions trigger. These
are the Azure counterparts of `Benzene.Kafka.Core`'s and `Benzene.Aws.Sqs`'s standalone consumers.

| Package | What it gives you |
|---|---|
| `Benzene.Azure.ServiceBus` | A self-hosted Service Bus consumer (`BenzeneServiceBusWorker`, `worker.UseServiceBus(...)`) that runs a `ServiceBusProcessor` over a queue or topic/subscription and dispatches each message through the middleware pipeline — distinct from `Benzene.Azure.Function.ServiceBus`, which handles Service Bus *as a Functions trigger*. |
| `Benzene.Azure.EventHub` | A self-hosted Event Hubs consumer (`BenzeneEventHubWorker`, `worker.UseEventHub(...)`) that runs an `EventProcessorClient` (consumer groups, partition load balancing, blob checkpointing) and dispatches each event through the middleware pipeline — distinct from `Benzene.Azure.Function.EventHub`, which handles Event Hubs *as a Functions trigger*. |
| `Benzene.Azure.CosmosDb` | A self-hosted Cosmos DB Change Feed consumer (`BenzeneCosmosChangeFeedWorker<TDocument>`, `worker.UseCosmosDbChangeFeed<TDocument>(...)`) that runs the SDK's Change Feed Processor (lease-container ownership, instance load balancing) and runs each batch through a fan-in streaming pipeline with manual batch-level checkpoint control — distinct from `Benzene.Azure.Function.CosmosDb`, which handles the change feed *as a Functions trigger*. |

See [Worker Service Setup](../getting-started-worker#part-b-built-in-workers-kafka-http-service-bus-event-hub).

### Other hosts

| Package | What it gives you |
|---|---|
| `Benzene.SelfHost` | Run Benzene as a standalone worker pipeline with no external transport — useful for background/worker services. |
| `Benzene.SelfHost.Http` | A self-hosted HTTP server (built on `HttpListener`) for running a Benzene HTTP service without ASP.NET Core. |
| `Benzene.HostedService` | Run Benzene inside a .NET Generic Host as an `IHostedService` / background worker. |
| `Benzene.Grpc` | Expose message handlers over gRPC — includes the `[GrpcMethod]` attribute and method-handler factory. |

## Outbound messaging clients

For calling *other* services (or other Benzene handlers) from inside a handler. The transports
above are inbound; these are outbound.

| Package | What it gives you |
|---|---|
| `Benzene.Clients` | The Benzene message-client abstraction and builder for sending typed messages to other Benzene services. |
| `Benzene.Clients.Aws` | Send messages by invoking another AWS Lambda (`AwsLambdaBenzeneMessageClient`), plus a Lambda health check. |
| `Benzene.Clients.HealthChecks` | Health checks that verify downstream Benzene clients are reachable and contract-compatible. |
| `Benzene.Client.Http` | HTTP client middleware for sending outbound HTTP requests through the Benzene client pipeline. |
| `Benzene.Aws.Sqs` | An SQS client for sending to / consuming from queues directly (`ISqsClient`, `SqsMessageClient`, `SqsConsumerConfig`) — distinct from `Benzene.Aws.Lambda.Sqs`, which handles SQS *as a Lambda trigger*. |
| `Benzene.Kafka.Core` | A Kafka client for producing Benzene messages to topics (`KafkaBenzeneMessageClient`), plus Kafka config. |

## Validation

Add request validation to the message-handler pipeline. See [Fluent Validation](../fluent-validation)
and [Data Annotations](../data-annotations).

| Package | What it gives you |
|---|---|
| `Benzene.FluentValidation` | Validate requests with FluentValidation via `UseFluentValidation()`; also feeds validation rules into schema generation. |
| `Benzene.DataAnnotations` | Validate requests using `System.ComponentModel.DataAnnotations` attributes. |
| `Benzene.JsonSchema` | JSON Schema validation middleware for incoming messages. |

## Serialization

Benzene serializes with `System.Text.Json` by default (in the core packages). Add these only
to override that.

| Package | What it gives you |
|---|---|
| `Benzene.NewtonsoftJson` | Use Newtonsoft.Json (`Json.NET`) as the serializer instead of `System.Text.Json`. |
| `Benzene.Xml` | XML serialization support — an `XmlSerializer` and serializer option for XML request/response bodies. |

## Observability & resilience

See [Monitoring & Diagnostics](../monitoring) and [Correlation IDs](../correlation-ids).

| Package | What it gives you |
|---|---|
| `Benzene.Diagnostics` | The observability toolkit — W3C trace context (`UseW3CTraceContext()`), log enrichment (`UseBenzeneEnrichment()`), metrics, `Activity`-based tracing decorators, and debug timing. |
| `Benzene.OpenTelemetry` | Wire Benzene's diagnostics into OpenTelemetry for traces/metrics export. |
| `Benzene.Resilience` | Retry middleware (`RetryMiddleware`) for wrapping handler/pipeline calls with retry policies. |

## Health checks

See [Health Checks](../health-checks).

| Package | What it gives you |
|---|---|
| `Benzene.HealthChecks` | The health-check message handler, builder, and processor that expose health status through the Benzene pipeline. |
| `Benzene.HealthChecks.Core` | Core health-check contracts and results (`IHealthCheck`, `HealthCheckStatus`, `HealthCheckResponse`) — depend on this to write a custom check. |
| `Benzene.HealthChecks.EntityFramework` | Ready-made database/EF Core connectivity health checks. |
| `Benzene.HealthChecks.Http` | An HTTP ping health check for verifying downstream HTTP dependencies. |

## Caching

| Package | What it gives you |
|---|---|
| `Benzene.Cache.Core` | Caching abstractions and a cache health check. |
| `Benzene.Cache.Redis` | A Redis-backed cache implementation (`RedisConnectionFactory`). |

## Code generation & tooling

Benzene can generate SDKs, infrastructure, and API specs from your message handlers and their
topics. See [Terraform](../terraform) and [OpenAPI Specification](../spec).

| Package | What it gives you |
|---|---|
| `Benzene.Schema.OpenApi` | Generate OpenAPI (HTTP) and AsyncAPI (event) documents from your handlers — `UseSpec()`. |
| `Benzene.CodeGen.Core` | The code-generation engine (code/file/example builders) the other CodeGen packages build on. |
| `Benzene.CodeGen.Client` | Generate strongly-typed C# client SDKs from a service's message contract. |
| `Benzene.CodeGen.ApiGateway` | Generate AWS API Gateway definitions from HTTP endpoints. |
| `Benzene.CodeGen.Terraform` | Generate Terraform for Lambda functions and event-bus permissions. |
| `Benzene.CodeGen.Markdown` | Generate Markdown documentation for a service and its messages. |
| `Benzene.CodeGen.LambdaTestTool` | Generate per-topic test payload files (BenzeneMessage envelope, SNS, SQS, API Gateway) for the AWS Lambda Test Tool — see [Payload Testing](../payload-testing). |
| `Benzene.CodeGen.SourceGenerators` | Roslyn source generator that discovers message handlers at compile time (an alternative to runtime reflection). |
| `Benzene.CodeGen.Cli` / `Benzene.CodeGen.Cli.Core` | The `benzene` code-generation command-line tool and its core. |
| `Benzene.Extras` | Additional/optional middleware — event broadcasting and JSON-patch (`PATCH`) support. |
| `Benzene.Tools` | Shared tooling helpers, including inline start-ups and builders used by the test host and CLI. |

## Testing support

See [Testing Benzene](../testing-benzene).

| Package | What it gives you |
|---|---|
| `Benzene.Testing` | The in-memory test host (`BenzeneTestHost`) and message/HTTP builders for driving handlers and pipelines in unit/integration tests. |
| `*.TestHelpers` | Per-transport test helpers (message builders and test-host extensions) that make it easy to feed transport-shaped events into the test host. One exists per transport: `Benzene.Aws.Lambda.ApiGateway.TestHelpers`, `Benzene.Aws.Lambda.Sqs.TestHelpers`, `Benzene.Aws.Lambda.Sns.TestHelpers`, `Benzene.Aws.Lambda.Kafka.TestHelpers`, `Benzene.Aws.Sqs.TestHelpers`, `Benzene.Azure.Function.AspNet.TestHelpers`, `Benzene.Azure.Function.EventHub.TestHelpers`, `Benzene.Azure.Function.Kafka.TestHelpers`, `Benzene.Core.MessageHandlers.TestHelpers`, `Benzene.Core.Messages.TestHelpers`. |

## See also

- [Getting Started](../getting-started) — install your first package and run a service.
- [Middleware](../middleware) and [Common Middleware](../common-middleware) — what you compose into the pipeline.
- [Message Handlers](../message-handlers) — the code the packages exist to run.
