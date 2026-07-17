# Azure Functions Setup

Benzene runs on the Azure Functions **isolated worker** model, using the same platform-neutral
`BenzeneStartUp` base class as every other Benzene host (AWS Lambda, ASP.NET Core). This guide
starts from an empty folder and ends with a deployed Function App handling HTTP requests, plus
optional Event Hub, Kafka, Service Bus, and Cosmos DB Change Feed triggers.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- An Azure subscription, with the [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
  configured, if you want to deploy

## 1. Create the project

```bash
mkdir MyFunction && cd MyFunction
dotnet new classlib -f net10.0
```

Add the Azure Functions isolated-worker properties to the `.csproj` (`OutputType` must be
`Exe` — a Function App is a runnable worker process, not a plain library):

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <AzureFunctionsVersion>v4</AzureFunctionsVersion>
  <OutputType>Exe</OutputType>
</PropertyGroup>
```

## 2. Install the NuGet packages

First, the standard Microsoft packages every isolated-worker Function App needs — these must be
referenced directly in your function app project (not just transitively) so the Functions SDK's
build step can discover them and generate the worker's extension manifest. The versions below are
the ones Benzene itself builds and tests against (see `examples/Azure/Benzene.Example.Azure/Benzene.Example.Azure.csproj`):

```bash
dotnet add package Microsoft.Azure.Functions.Worker --version 2.2.0
dotnet add package Microsoft.Azure.Functions.Worker.Sdk --version 2.0.7
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Http --version 3.3.0
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore --version 2.1.0
```

Then Benzene's packages, published as prerelease (`-alpha`) versions, so `--prerelease` is
required until 1.0:

```bash
dotnet add package Benzene.Azure.Function.Core --prerelease
dotnet add package Benzene.Azure.Function.AspNet --prerelease
```

`Benzene.Azure.Function.Core` brings in the middleware pipeline, message handler infrastructure,
`BenzeneStartUp` base class, and the isolated-worker hosting glue (`IHostBuilder.UseBenzene<TStartUp>()`),
transitively. `Benzene.Azure.Function.AspNet` adds the `UseHttp` middleware for handling HTTP
requests as ASP.NET Core `HttpRequest`/`IActionResult`. Add `Benzene.Azure.Function.EventHub` or
`Benzene.Azure.Function.Kafka` the same way if your function also needs to handle those event
sources (see [Event Hub, Kafka, Service Bus, and Cosmos DB triggers](#event-hub-kafka-service-bus-and-cosmos-db-triggers) below) — each has a
corresponding direct Microsoft package too (`Microsoft.Azure.Functions.Worker.Extensions.EventHubs`
version `6.5.0`, or `Microsoft.Azure.Functions.Worker.Extensions.Kafka` version `4.3.0`).

## 3. Define a message handler

Business logic lives in message handlers, not in the trigger function — this keeps it testable
and portable across hosts. See [Message Handlers](message-handlers) for the full picture; the
minimal shape is:

```csharp
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Results;

[Message("hello:world")]
[HttpEndpoint("GET", "/hello/{name}")]
public class HelloWorldMessageHandler : IMessageHandler<HelloWorldRequest, HelloWorldResponse>
{
    public Task<IBenzeneResult<HelloWorldResponse>> HandleAsync(HelloWorldRequest message)
    {
        return Task.FromResult(BenzeneResult.Ok(new HelloWorldResponse { Message = $"Hello {message.Name}!" }));
    }
}

public class HelloWorldRequest
{
    public string Name { get; set; }
}

public class HelloWorldResponse
{
    public string Message { get; set; }
}
```

`[Message]` (from `Benzene.Core.MessageHandlers`) maps the handler to a topic; `[HttpEndpoint]`
(from `Benzene.Http`) maps an HTTP method and path to that same topic. Both attributes are
discovered by reflection, so there is nothing further to register per-handler.

## 4. Define your StartUp

`BenzeneStartUp` (from `Benzene.Microsoft.Dependencies`) is the platform-neutral application
definition shared by every Benzene host — the same class shape you'd write for AWS Lambda or
ASP.NET Core. It has three members to implement:

```csharp
public abstract class BenzeneStartUp
{
    public abstract IConfiguration GetConfiguration();
    public abstract void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    public abstract void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration);
}
```

Configure the HTTP pipeline via `UseHttp`:

```csharp
using Benzene.Abstractions.Hosting;
using Benzene.Azure.Function.AspNet;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Http;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .Build();
    }

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.UsingBenzene(x => x
            .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly)
            .AddHttpMessageHandlers());
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseHttp(http => http.UseMessageHandlers());
    }
}
```

`UseHttp` on `IBenzeneApplicationBuilder` is a no-op on any host other than Azure Functions (it
only does something when `app` is actually an `IAzureFunctionAppBuilder`), which is what lets the
same `StartUp` shape be reused across platforms — see [ASP.NET Core Integration](asp-net-core) for
the same `UseHttp` method used in a plain ASP.NET Core app.

## 5. Wire up the isolated worker host

`Program.cs` registers `StartUp` with the isolated worker's `IHostBuilder`:

```csharp
using Benzene.Azure.Function.Core;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .UseBenzene<StartUp>()
    .Build();

host.Run();
```

`ConfigureFunctionsWebApplication()` turns on the ASP.NET Core integration (advanced HTTP
features, `HttpRequest`/`IActionResult` trigger bindings). `UseBenzene<StartUp>()` (from
`Benzene.Azure.Function.Core`) instantiates your `StartUp`, runs `GetConfiguration()` once, then
runs `ConfigureServices`/`Configure` inside the host's `ConfigureServices` callback and registers
the built `IAzureFunctionApp` as a scoped service so trigger functions can inject it.

Then add a single catch-all HTTP trigger function that delegates to Benzene's own routing — one
Azure Function handles every route your message handlers define:

```csharp
using Benzene.Azure.Function.AspNet;
using Benzene.Azure.Function.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

public class HttpFunction
{
    private readonly IAzureFunctionApp _app;

    public HttpFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("http")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", "options", Route = "{*restOfPath}")] HttpRequest req)
    {
        return await _app.HandleHttpRequest(req);
    }
}
```

`HandleHttpRequest` is an extension method from `Benzene.Azure.Function.AspNet` — under the hood
it calls the generic `IAzureFunctionApp.HandleAsync<HttpRequest, IActionResult>(req)`, which
dispatches to whichever entry point application your `Configure` method registered for that
request/response type pairing (here, the one `UseHttp` added).

## 6. Configuration

`GetConfiguration()` runs once on cold start, before any services are registered, and its result
is passed into both `ConfigureServices` and `Configure`. Anything built on top of
`Microsoft.Extensions.Configuration` works here — the example above reads environment variables
(which map to Application Settings once deployed), but `AddJsonFile(...)`, Azure App Configuration,
or Azure Key Vault configuration providers all work the same way.

For local development, add a `local.settings.json` (not checked into source control — it holds
secrets and machine-specific values; Benzene's own example project `.gitignore`s it too):

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

## 7. Run it locally

```bash
func start
```

`GET` `http://localhost:7071/api/hello/world` to confirm the handler above responds (the `api`
prefix is the default Azure Functions route prefix — clear it via `"routePrefix": ""` in
`host.json`'s `extensions.http` section if you'd rather not have it, as Benzene's own example
project does).

## 8. Deploy

Create the Function App resource (a Consumption-plan example; adjust SKU/plan for your needs):

```bash
az group create --name my-function-rg --location eastus
az storage account create --name mystorageacct --location eastus --resource-group my-function-rg --sku Standard_LRS
az functionapp create --resource-group my-function-rg --consumption-plan-location eastus \
  --runtime dotnet-isolated --functions-version 4 --name my-function-app --storage-account mystorageacct
```

Then publish:

```bash
func azure functionapp publish my-function-app
```

Once deployed, `GET` the printed URL at `/api/hello/world` (or just `/hello/world`, depending on
your `routePrefix` setting) to confirm the handler responds.

### Deploying with Bicep

For a repeatable, declarative deployment instead of the `az` commands above, see
[`examples/Azure/Benzene.Example.Azure/main.bicep`](../examples/Azure/Benzene.Example.Azure/main.bicep) -
it provisions the Storage Account, workspace-based Application Insights resource, Consumption
hosting plan, and Function App an HTTP-triggered example like this one needs:

```bash
az group create --name my-function-rg --location eastus
az deployment group create --resource-group my-function-rg \
  --template-file examples/Azure/Benzene.Example.Azure/main.bicep \
  --parameters functionAppName=my-benzene-function
```

The template only covers the HTTP trigger path - add your own resources for Event Hub, Kafka, or
Service Bus namespaces if you wire up those triggers too (see the next section).

## Event Hub, Kafka, Service Bus, and Cosmos DB triggers

Benzene provides specialized middleware for other Azure Functions trigger types, each
configured inside the same `Configure` method, on the same platform-neutral `app` shown in step 4
— a single `BenzeneStartUp` can wire up several trigger types at once, each with its own
sub-pipeline, exactly as with any other Benzene host:

- **HTTP**: `app.UseHttp(...)`, in `Benzene.Azure.Function.AspNet`
- **Event Hubs**: `app.UseEventHub(...)`, in `Benzene.Azure.Function.EventHub`
- **Kafka** (Event Hubs' Kafka-compatible endpoint): `app.UseKafka(...)`, in `Benzene.Azure.Function.Kafka`
- **Service Bus**: `app.UseServiceBus(...)`, in `Benzene.Azure.Function.ServiceBus`
- **Cosmos DB Change Feed**: `app.UseCosmosDbChangeFeed<TDocument>(...)`, in `Benzene.Azure.Function.CosmosDb`

### Event Hubs

Install the package and add the pipeline in `Configure`:

```bash
dotnet add package Benzene.Azure.Function.EventHub --prerelease
```

```csharp
app.UseEventHub(eventHub => eventHub
    .UseBenzeneMessage(direct => direct
        .UseMessageHandlers()));
```

`UseBenzeneMessage` routes an Event Hub event whose body deserializes into a Benzene message
envelope (topic + payload) to the direct-message pipeline — this is the same envelope shape
`MessageBuilder` produces for AWS SQS/SNS. Add a trigger function that injects `IAzureFunctionApp`
and calls `HandleEventHub(...)`:

```csharp
using Azure.Messaging.EventHubs;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.EventHub.Function;
using Microsoft.Azure.Functions.Worker;

public class EventHubFunction
{
    private readonly IAzureFunctionApp _app;

    public EventHubFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("event-hub")]
    public Task Run([EventHubTrigger("my-event-hub", Connection = "EventHubConnection")] EventData[] events)
    {
        return _app.HandleEventHub(events);
    }
}
```

### Kafka

Install the package and add the pipeline in `Configure`:

```bash
dotnet add package Benzene.Azure.Function.Kafka --prerelease
```

```csharp
app.UseKafka(kafka => kafka.UseMessageHandlers());
```

Works against Event Hubs' Kafka-compatible endpoint. The Kafka record's value is `byte[]`; Benzene
decodes it as UTF-8 JSON the same way as every other transport, and dispatches by topic via
`[Message]`/message handler registration — there is no `UseBenzeneMessage` bridge for Kafka today
(that only exists for Event Hubs). Add a trigger function that injects `IAzureFunctionApp` and
calls `HandleKafkaEvents(...)`:

```csharp
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.Kafka;
using Microsoft.Azure.Functions.Worker;

public class KafkaFunction
{
    private readonly IAzureFunctionApp _app;

    public KafkaFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("kafka")]
    public Task Run([KafkaTrigger("BrokerList", "my-topic", ConsumerGroup = "my-consumer-group")] KafkaRecord[] events)
    {
        return _app.HandleKafkaEvents(events);
    }
}
```

(Adjust the `[KafkaTrigger]` binding attribute's parameters and connection string setting names to
match your Event Hubs Kafka endpoint configuration — this follows the same shape as any
`Microsoft.Azure.Functions.Worker.Extensions.Kafka` trigger.)

### Service Bus

Install the package and add the pipeline in `Configure`:

```bash
dotnet add package Benzene.Azure.Function.ServiceBus --prerelease
```

```csharp
app.UseServiceBus(serviceBus => serviceBus.UseMessageHandlers());
```

Service Bus messages carry real key/value application properties, so — unlike Event Hubs — Benzene
dispatches by topic via `[Message]`/message handler registration directly, the same way it does for
Kafka. Since a Service Bus queue or topic/subscription isn't itself a per-message "topic" in
Benzene's sense, set a `"topic"` application property on each message you send; that's what
`ServiceBusMessageTopicGetter` reads to route to the matching handler. If a subscription's producer
isn't a Benzene client and never sets that property at all, call `.UsePresetTopic("orders.created")`
before `.UseMessageHandlers()` in that subscription's pipeline to route every message on it to a
fixed topic instead — see [Common Middleware: UsePresetTopic](common-middleware#usepresettopic).
Add a trigger function that
injects `IAzureFunctionApp` and calls `HandleServiceBusMessages(...)`:

```csharp
using Azure.Messaging.ServiceBus;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.ServiceBus;
using Microsoft.Azure.Functions.Worker;

public class ServiceBusFunction
{
    private readonly IAzureFunctionApp _app;

    public ServiceBusFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("service-bus")]
    public Task Run([ServiceBusTrigger("my-queue", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message)
    {
        return _app.HandleServiceBusMessages(message);
    }
}
```

(Bind a `ServiceBusReceivedMessage[]` instead of a single `ServiceBusReceivedMessage` if your
trigger is configured with `IsBatched = true`; `HandleServiceBusMessages` accepts both via a
`params` array. Note that message completion is a no-op in this package today — the trigger
completes the message per its own default settings regardless of the handler's outcome; explicit
complete/abandon/dead-letter control isn't implemented yet.)

### Cosmos DB Change Feed

Install the package and add the pipeline in `Configure`:

```bash
dotnet add package Benzene.Azure.Function.CosmosDb --prerelease
```

```csharp
app.UseCosmosDbChangeFeed<OrderDocument>(feed => feed
    .UseStream<OrderDocument>(async (documents, cancellationToken) =>
    {
        await foreach (var document in documents)
        {
            // documents arrive in change feed order for their partition key range
        }
    }));
```

Cosmos DB is different from the other triggers in two ways. First, the trigger delivers
**already-deserialized documents** of a concrete type you choose, not opaque payloads — so the
pipeline is generic over your document type, and there is no topic-based `UseMessageHandlers()`
dispatch (a changed document has no message envelope to route on). Second, changes arrive as an
**ordered batch per partition key range**, so Benzene presents the whole batch to one pipeline run
as a single `StreamContext<TDocument>` (fan-in) — the same streaming shape as `UseEventHubStream`
— rather than fanning it out into isolated per-document contexts. See
[Cosmos DB Change Feed Processing](cookbooks/cosmos-change-feed-processing) for the full
walkthrough.

`Benzene.Azure.Function.CosmosDb` deliberately has no Azure SDK dependency; your function app
project supplies the trigger binding by referencing
`Microsoft.Azure.Functions.Worker.Extensions.CosmosDB` directly. Add a trigger function that
injects `IAzureFunctionApp` and calls `HandleCosmosDbChanges(...)`:

```csharp
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.CosmosDb;
using Microsoft.Azure.Functions.Worker;

public class CosmosDbFunction
{
    private readonly IAzureFunctionApp _app;

    public CosmosDbFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("orders-change-feed")]
    public Task Run([CosmosDBTrigger(
        databaseName: "my-database",
        containerName: "orders",
        Connection = "CosmosDbConnection",
        LeaseContainerName = "leases",
        CreateLeaseContainerIfNotExists = true)] IReadOnlyList<OrderDocument> documents)
    {
        return _app.HandleCosmosDbChanges(documents);
    }
}
```

(The trigger checkpoints its lease automatically when the invocation returns successfully. If the
pipeline throws, the exception propagates, the lease is not advanced, and the runtime redelivers
the whole batch — there is no per-document resume point in the change feed, so design handlers to
be idempotent across batch redelivery.)

## Correlation and tracing

Every middleware in every pipeline — HTTP, Event Hub, Kafka — is automatically wrapped in a
`System.Diagnostics.Activity` span once you call `AddDiagnostics()` in `ConfigureServices`:

```csharp
services.UsingBenzene(x => x.AddDiagnostics());
```

This is the same tracing system used across every Benzene host, not something Azure-specific. See
[Monitoring & Diagnostics](monitoring) for the full picture, and [Correlation Ids](correlation-ids)
for the header-based legacy alternative to W3C trace context propagation.

### `IBenzeneInvocation`

Unlike AWS Lambda or ASP.NET Core, the isolated worker dispatches each trigger type (HTTP, Event
Hub, Kafka) through its own separate pipeline, so there's no single request-flowing middleware
that can populate `IBenzeneInvocation` (the `FunctionContext.InvocationId`-backed accessor used for
enrichment) automatically. To opt in:

1. Call `app.UseBenzeneInvocation()` in `Configure`.
2. Register the worker middleware in `Program.cs`, using `ConfigureFunctionsWebApplication`'s
   overload that configures the worker pipeline:

```csharp
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(worker => worker.UseBenzene())
    .UseBenzene<StartUp>()
    .Build();

host.Run();
```

With both in place, `IBenzeneInvocation.InvocationId` resolves to the isolated worker's
`FunctionContext.InvocationId` for the duration of each invocation, and `GetFeature<FunctionContext>()`
returns the native `FunctionContext`.

### Application Insights

`Microsoft.ApplicationInsights.WorkerService` plus `Microsoft.Azure.Functions.Worker.ApplicationInsights`
(both referenced by `examples/Azure/Benzene.Example.Azure`) is the standard way to get Functions-host
telemetry — cold starts, invocation duration, dependency calls — into Application Insights, wired in
`Program.cs`:

```csharp
.ConfigureServices(services =>
{
    services.AddApplicationInsightsTelemetryWorkerService();
    services.ConfigureFunctionsApplicationInsights();
})
```

That alone doesn't correlate Benzene's own topic/handler/invocationId onto the logs and traces it
collects — for that, add `AddDiagnostics()` in `ConfigureServices` and `UseBenzeneEnrichment()` in
`Configure` (both shown wired up in the example project). Since Application Insights' `ILogger`
provider reads `BeginScope` values into `customDimensions` automatically, no Application-Insights-specific
code is required beyond those two calls. See
[Logging to Application Insights](cookbooks/logging-application-insights) for the full recipe
(including KQL queries against `customDimensions`) and
[Distributed Tracing with OpenTelemetry](cookbooks/distributed-tracing-opentelemetry) if you'd rather
export Benzene's `Activity` spans to Application Insights via OTLP instead of (or alongside) the
classic SDK shown here.

## Testing

Benzene ships a unified test host (`Benzene.Testing`) that builds an in-memory app straight from
your real `StartUp` — no need to run `func start` or hit the network. See
[Testing Benzene](testing-benzene) for the full picture; for Azure Functions specifically:

```csharp
var app = BenzeneTestHost.Create<StartUp>()
    .WithServices(services => services.AddScoped(_ => mockHelloWorldService.Object))
    .BuildAzureFunctionApp();

var request = HttpBuilder.Create("GET", "/hello/world").AsAspNetCoreHttpRequest();
var response = await app.HandleHttpRequest(request) as ContentResult;
```

`BuildAzureFunctionApp()` (from `Benzene.Azure.Function.Core`) performs the same construction
`IHostBuilder.UseBenzene<TStartUp>()` does for a real deployment, and returns an `IAzureFunctionApp`
you can dispatch into directly. `AsAspNetCoreHttpRequest()` (from
`Benzene.Azure.Function.AspNet.TestHelpers`) turns an `HttpBuilder` into a real `HttpRequest`.
`HandleEventHub(...)`, `HandleKafkaEvents(...)`, and `HandleServiceBusMessages(...)` work the same
way for those transports —
`Benzene.Azure.Function.EventHub.TestHelpers`/`Benzene.Azure.Function.Kafka.TestHelpers`/`Benzene.Azure.Function.ServiceBus.TestHelpers`
add `AsEventHubBenzeneMessage()`/`AsAzureKafkaEvent()`/`AsAzureServiceBusMessage()` extensions on
`MessageBuilder` to build the matching event payloads.

For quick, StartUp-free pipeline tests (useful when testing a single trigger type in isolation
rather than a whole app), `InlineAzureFunctionStartUp` (from `Benzene.Azure.Function.Core`) is a
fluent alternative:

```csharp
var app = new InlineAzureFunctionStartUp()
    .ConfigureServices(services => services
        .UsingBenzene(x => x.AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly))
        .AddSingleton(mockHelloWorldService.Object))
    .Configure(app => app
        .UseEventHub(eventHub => eventHub
            .UseBenzeneMessage(direct => direct
                .UseMessageHandlers())))
    .Build();

var request = MessageBuilder.Create("hello:world", new HelloWorldMessage { Name = "World" })
    .AsEventHubBenzeneMessage();

await app.HandleEventHub(request);
```

### Real broker/emulator integration tests

The tests above dispatch a hand-built event/message directly into the pipeline — fast, but they
don't exercise the real Azure SDK client, wire protocol, or (for Event Hubs/Kafka/Service Bus)
partitioning and delivery semantics. `test/Benzene.Integration.Test` covers that gap with a real
produce/consume round trip against the actual Docker-emulator images for each transport, driving
Benzene's real production pipeline on the receiving end (not a hand-built event):

- **Event Hubs** (`EventHub/EventHubConsumerPipelineTest.cs`) and **Kafka**
  (`Kafka/KafkaConsumerPipelineTest.cs`) both run against the same
  `mcr.microsoft.com/azure-messaging/eventhubs-emulator` container — it exposes both the native
  AMQP endpoint and a Kafka-compatible endpoint (port 9092) on the same instance. The two tests
  share that one container via `EventHubEmulatorCollection` (an xunit collection fixture) and use
  separate entities (`eh1` vs `kafka1`) so their events don't cross-contaminate.
- **Service Bus** (`ServiceBus/ServiceBusConsumerPipelineTest.cs`) runs against
  `mcr.microsoft.com/azure-messaging/servicebus-emulator`, which requires a SQL Server backend
  container — its ports are remapped (`5673`/`5301` instead of the emulator's defaults `5672`/`5300`)
  so it can run alongside the Event Hubs emulator without a host port conflict; the Service Bus
  SDK's emulator connection string supports specifying that non-default port explicitly.

These require a working Docker daemon and aren't run as part of the main `Benzene.Core.Test` suite
— see the `azure-integration-tests` job in `.github/workflows/build-benzene.yml`.

## Troubleshooting

- **`func start` can't find the function app / "No job functions found"**: confirm `OutputType`
  is `Exe` in the `.csproj` and that `Microsoft.Azure.Functions.Worker.Sdk` is referenced directly
  (not just transitively) — the Functions SDK's build step needs it to generate `functions.metadata`
  and the worker extension manifest.
- **404 on every route locally, but the function runs**: check `host.json`'s
  `extensions.http.routePrefix` — Azure Functions defaults to prefixing every HTTP route with
  `/api`, so `/hello/world` needs to be requested as `/api/hello/world` unless you've cleared the
  prefix.
- **Handler never gets called**: confirm `[Message]`/`[HttpEndpoint]` are both present and that
  the handler's assembly is passed to `AddMessageHandlers(typeof(SomeHandlerInThatAssembly).Assembly)`
  — handlers are discovered by reflection over that assembly, not auto-registered globally.
- **`IBenzeneInvocation was requested before ... UseBenzeneInvocation() populated it`**: you called
  `app.UseBenzeneInvocation()` in `Configure` but didn't also wire
  `ConfigureFunctionsWebApplication(worker => worker.UseBenzene())` in `Program.cs` (or vice
  versa) — both halves are required together; see [`IBenzeneInvocation`](#ibenzeneinvocation) above.
- **NuGet can't find the Benzene packages**: they're prerelease-only until 1.0, so
  `dotnet add package` needs `--prerelease` (or pin an explicit `-alpha` version).

## See Also

- [Message Handlers](message-handlers) — the full picture on `[Message]`, `[HttpEndpoint]`, and handler discovery
- [ASP.NET Core Integration](asp-net-core) — the same `UseHttp` pipeline, hosted outside Azure Functions
- [Testing Benzene](testing-benzene) — `BenzeneTestHost`, including AWS Lambda and ASP.NET Core patterns
- [Monitoring & Diagnostics](monitoring) — tracing, metrics, and W3C trace context propagation
- [Correlation Ids](correlation-ids) — the legacy header-based correlation ID middleware
- [`examples/Azure`](../examples/Azure) — a complete, runnable project covering HTTP routing, validation, and OpenAPI spec generation
