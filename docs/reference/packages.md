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
| `Benzene.Abstractions.Validation` | Validation contracts — `IValidationSchemaBuilder`, `IValidationStatusMapper`. |

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
| `Benzene.Aws.Lambda.DynamoDb` | Handle DynamoDB Streams change-data-capture records — `UseDynamoDb(...)`. |
| `Benzene.Aws.Lambda.EventBridge` | Handle EventBridge events — `UseEventBridge(...)`. |
| `Benzene.Aws.Lambda.Kinesis` | Handle Kinesis Data Streams batches as a **streaming** pipeline — `UseKinesisStream(...)`. |

See [AWS Lambda Setup](../getting-started-aws) for a full walkthrough and
[AWS IAM Permissions](../aws-iam-permissions) for the per-source policies.

### Azure Functions

| Package | What it gives you |
|---|---|
| `Benzene.Azure.Function.Core` | The Azure Functions (isolated worker) host and app builder. |
| `Benzene.Azure.Function.AspNet` | Handle HTTP-triggered functions via the ASP.NET Core integration — `UseHttp(...)`. |
| `Benzene.Azure.Function.EventHub` | Handle Event Hub stream events. |
| `Benzene.Azure.Function.Kafka` | Handle Kafka-triggered functions. |
| `Benzene.Azure.Function.ServiceBus` | Handle Service Bus-triggered functions (queue or topic/subscription, single or batched) — `UseServiceBus(...)` — distinct from `Benzene.Azure.ServiceBus` below, which consumes Service Bus in a self-hosted worker instead of as a Functions trigger. |

See [Azure Functions Setup](../azure-functions).

### Azure (self-hosted / worker)

Consume Azure messaging in a long-running process you own (console app, container, AKS, App
Service WebJob) via `Benzene.HostedService`/`Benzene.SelfHost` — no Azure Functions trigger. These
are the Azure counterparts of `Benzene.Kafka.Core`'s and `Benzene.Aws.Sqs`'s standalone consumers.

| Package | What it gives you |
|---|---|
| `Benzene.Azure.ServiceBus` | A self-hosted Service Bus consumer (`BenzeneServiceBusWorker`, `worker.UseServiceBus(...)`) that runs a `ServiceBusProcessor` over a queue or topic/subscription and dispatches each message through the middleware pipeline — distinct from `Benzene.Azure.Function.ServiceBus`, which handles Service Bus *as a Functions trigger*. |
| `Benzene.Azure.EventHub` | A self-hosted Event Hubs consumer (`BenzeneEventHubWorker`, `worker.UseEventHub(...)`) that runs an `EventProcessorClient` (consumer groups, partition load balancing, blob checkpointing) and dispatches each event through the middleware pipeline — distinct from `Benzene.Azure.Function.EventHub`, which handles Event Hubs *as a Functions trigger*. |

See [Worker Service Setup](../getting-started-worker#part-b-built-in-workers-kafka-http-service-bus-event-hub).

### Other hosts

| Package | What it gives you |
|---|---|
| `Benzene.SelfHost` | Run Benzene as a standalone worker pipeline with no external transport — useful for background/worker services. |
| `Benzene.SelfHost.Http` | A self-hosted HTTP server (built on `HttpListener`) for running a Benzene HTTP service without ASP.NET Core. |
| `Benzene.HostedService` | Run Benzene inside a .NET Generic Host as an `IHostedService` / background worker. |
| `Benzene.Grpc` | Expose message handlers over gRPC — includes the `[GrpcMethod]` attribute and method-handler factory. Reference `Benzene.Grpc.AspNet` (below) to actually host it; you rarely reference this directly. |
| `Benzene.Grpc.AspNet` | Host `Benzene.Grpc` on ASP.NET Core — `AddBenzeneGrpc()` / `UseGrpc(...)`, plus opt-in `grpc.health.v1`/`grpc.reflection.v1alpha` support. The package a gRPC server application actually references. See [Getting Started: gRPC](../getting-started-grpc). |

### Google Cloud Functions

| Package | What it gives you |
|---|---|
| `Benzene.GoogleCloud.Functions.Core` | Shared bootstrap for every Google Cloud Functions trigger-type package — not referenced directly. |
| `Benzene.GoogleCloud.Functions.Http` | Host an HTTP-triggered function on Cloud Functions Gen2 (`GoogleCloudFunctionHost<TStartUp>`). If you're targeting Cloud Run instead, you don't need this — `Benzene.AspNet.Core` alone is enough. |
| `Benzene.GoogleCloud.Functions.PubSub` | Handle a Pub/Sub **push** subscription delivered to a Cloud Functions Gen2 CloudEvent trigger — `UsePubSub(...)`. |

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
| `Benzene.Grpc.Client` | An outbound gRPC client (`GrpcBenzeneMessageClient`) that sends unary calls through a Benzene middleware pipeline over a `Grpc.Net.Client.GrpcChannel`. |

## Validation

Add request validation to the message-handler pipeline. See [Fluent Validation](../fluent-validation)
and [Data Annotations](../data-annotations).

| Package | What it gives you |
|---|---|
| `Benzene.FluentValidation` | Validate requests with FluentValidation via `UseFluentValidation()`; also feeds validation rules into schema generation. |
| `Benzene.DataAnnotations` | Validate requests using `System.ComponentModel.DataAnnotations` attributes. |
| `Benzene.JsonSchema` | JSON Schema validation middleware for incoming messages. |

## Authentication

Opt-in authentication middleware for services with no security-terminating gateway in front of
them. See the [Authentication Patterns cookbook](../cookbooks/auth-patterns) and
[Common Middleware](../common-middleware#useoauth2bearer).

| Package | What it gives you |
|---|---|
| `Benzene.Auth.Core` | The shared contracts every concrete auth package builds on — the scoped `AuthenticationHolder` a validated caller identity is handed through on, plus the `Unauthorized`/`Forbidden` short-circuit helper. Reference directly only when writing your own authentication middleware. |
| `Benzene.Auth.Basic` | RFC 7617 HTTP Basic authentication — `UseBasicAuth(...)`. The simplest option when you just need a username/password gate. |
| `Benzene.Auth.OAuth2` | OAuth2 bearer token (JWT) validation against a JWKS endpoint — `UseOAuth2Bearer(...)` — plus scope-based authorization, `RequireScope(...)`. |

## Serialization

Benzene serializes with `System.Text.Json` by default (in the core packages). Add these only
to override that.

| Package | What it gives you |
|---|---|
| `Benzene.NewtonsoftJson` | Use Newtonsoft.Json (`Json.NET`) as the serializer instead of `System.Text.Json`. |
| `Benzene.Xml` | XML serialization support — an `XmlSerializer` and serializer option for XML request/response bodies. |
| `Benzene.Avro` | Apache Avro binary serialization — an `IMediaFormat` (`application/avro`) for compact, schema-evolving payloads, popular in finance/data streaming (Kafka). |
| `Benzene.MessagePack` | MessagePack binary serialization — an `IMediaFormat` backed by MessagePack-CSharp, for compact encoding in high-throughput domains where JSON's text overhead matters. |

## Payload versioning

Run multiple schema versions of the same message side by side, casting an older/newer payload to
the version your handler understands. See [Versioning](../specification/versioning).

| Package | What it gives you |
|---|---|
| `Benzene.Core.Versioning` | Casts a payload between schema versions (upcasting older payloads forward, downcasting newer ones back), composing multi-step chains automatically — registered in `ConfigureServices` via `services.UsingBenzene(x => x.UsePayloadVersionCasting<TContext>())`. |

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
| `Benzene.Spec.Ui` | Serve a self-contained, Swagger-UI-style web viewer for the spec `Benzene.Schema.OpenApi` generates — `UseSpecUi()`. See [Spec UI](../spec-ui). |
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

## Cloud Service Profile

Batteries-included conformance with the [Cloud Service Profile specification](../specification/cloud-service-profile)
— the standard operational surface (invoke envelope, spec, health, mesh descriptor, trace feed,
collector registration) a Benzene HTTP service can self-certify against.

| Package | What it gives you |
|---|---|
| `Benzene.CloudService` | Wires every profile-required operational surface at the default standard paths in one call — `UseBenzeneCloudService("name", ...)`. Syntactic sugar over the underlying `Use*` calls; never required for full manual control. |
| `Benzene.CloudService.Probe` | An external live-probe checker: hits a running service over real HTTP from outside and independently verifies profile conformance, the way an operator or CI job would. |

## Service mesh

Cross-service fleet visibility — a catalog of every service's spec, health, and contract drift,
plus a live trace feed. See [Service Mesh](../specification/mesh) for the full wire contract and
`deploy/Mesh/README.md` for running the aggregator/UI via `docker-compose`.

| Package | What it gives you |
|---|---|
| `Benzene.Mesh.Contracts` | Shared data shapes for the mesh feature — the service registry config and the aggregator's generated snapshot/manifest artifacts. Pure data types, no HTTP or file I/O. |
| `Benzene.Mesh.Wire` | The spec-conformant wire layer that makes a .NET service a citizen of a cross-language mesh fleet — the derived service descriptor (`UseMeshDescriptor(...)`) and the trace-emitting middleware (`UseMeshTrace(...)`). |
| `Benzene.Mesh.Aggregator` | Polls every service in a `MeshServiceRegistry` for its spec and health, computes contract drift, and publishes a catalog — the pull-based collector. |
| `Benzene.Mesh.Collector` | An ordinary Benzene service that ingests the mesh wire topics and answers live `mesh:query:*` read models over an in-memory store — the push-based counterpart to the aggregator. |
| `Benzene.Mesh.Reporting` | Lets a service with no synchronous entry point an aggregator could poll (e.g. a Lambda triggered only by SQS/SNS) self-report its own spec/health as a side effect of real traffic. |
| `Benzene.Mesh.Tracing.Tempo` | Queries Grafana Tempo's service-graph metrics and publishes the result as mesh topology — live trace integration for the aggregator's catalog. |
| `Benzene.Mesh.Aws.Lambda` | Fetches a service's spec/health via a synchronous AWS Lambda `Invoke` instead of HTTP, for services with no public HTTP surface at all. |
| `Benzene.Mesh.Ui` | Serves a self-contained, catalog-style web viewer for the aggregator's manifest/service artifacts — `UseMeshUi()` — plus a live Fleet view over a collector's query topic, `UseMeshFleetUi()`. |

## Testing support

See [Testing Benzene](../testing-benzene).

| Package | What it gives you |
|---|---|
| `Benzene.Testing` | The in-memory test host (`BenzeneTestHost`) and message/HTTP builders for driving handlers and pipelines in unit/integration tests. |
| `*.TestHelpers` | Per-transport test helpers (message builders and test-host extensions) that make it easy to feed transport-shaped events into the test host. Every host/transport package above has a matching `*.TestHelpers` package, named identically with the suffix added (e.g. `Benzene.Aws.Lambda.Sqs` → `Benzene.Aws.Lambda.Sqs.TestHelpers`); a few foundational packages (`Benzene.Core.MessageHandlers`, `Benzene.Core.Messages`) have one too. |

## See also

- [Getting Started](../getting-started) — install your first package and run a service.
- [Middleware](../middleware) and [Common Middleware](../common-middleware) — what you compose into the pipeline.
- [Message Handlers](../message-handlers) — the code the packages exist to run.
