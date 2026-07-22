# Azure Functions Setup

Benzene runs on the Azure Functions **isolated worker** model, using the same platform-neutral
`BenzeneStartUp` base class as every other Benzene host (AWS Lambda, ASP.NET Core). This guide
starts from an empty folder and ends with a deployed Function App handling HTTP requests, plus
optional non-HTTP triggers — Event Hubs, Kafka, Service Bus, Cosmos DB Change Feed, Queue
Storage, Blob Storage, Event Grid, and Timer.

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
sources (see [Non-HTTP triggers](#non-http-triggers) below) — each has a
corresponding direct Microsoft package too (`Microsoft.Azure.Functions.Worker.Extensions.EventHubs`
version `6.5.0`, or `Microsoft.Azure.Functions.Worker.Extensions.Kafka` version `4.3.0`).

## 3. Define a message handler

Business logic lives in message handlers, not in the trigger function — this keeps it testable
and portable across hosts. See [Message Handlers](message-handlers.md) for the full picture; the
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
same `StartUp` shape be reused across platforms — see [ASP.NET Core Integration](asp-net-core.md) for
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

Then **declare** a single catch-all HTTP trigger. Benzene's source generator writes the
`[Function]`/`[HttpTrigger]` class for you — the part that has to be exactly right for the Functions
host to register and dispatch the trigger — so one Azure Function handles every route your message
handlers define, and you write none of the ceremony. Add the declaration at assembly scope (anywhere
in the project, e.g. a `Triggers.cs`):

```csharp
using Benzene.Azure.Function.AspNet;

[assembly: BenzeneHttpTrigger(Name = "orders", Route = "{*restOfPath}")]
```

That's the whole trigger. The generator emits a catch-all HTTP function that forwards into your built
`IAzureFunctionApp`, which routes the request to the right message handler. You own the `Name` and
`Route` (and `AuthorizationLevel`/`Methods` if you want them) — nothing is hard-coded; a bare
`[assembly: BenzeneHttpTrigger]` also works (a `benzene`-named anonymous catch-all). Every transport
has an equivalent attribute — see [Non-HTTP triggers](#non-http-triggers).

**One required setting.** The generator relies on `FunctionsEnableWorkerIndexing=false` (the Functions
SDK's own worker-indexing source generators can't see another generator's output). Benzene's NuGet
packages set this for you via `buildTransitive`; nothing to do. (If you consume Benzene via
`ProjectReference` — e.g. inside this repo — set `<FunctionsEnableWorkerIndexing>false</FunctionsEnableWorkerIndexing>`
in your csproj, since `buildTransitive` props don't flow across project references.)

<details>
<summary><b>Prefer to write the trigger by hand?</b> (the escape hatch)</summary>

The generator only *adds* a path — a hand-written `[Function]` still works, and both can coexist.
Reach for this when you need a binding shape the attribute doesn't expose:

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

`HandleHttpRequest` is an extension method from `Benzene.Azure.Function.AspNet` — under the hood it
calls the generic `IAzureFunctionApp.HandleAsync<HttpRequest, IActionResult>(req)`, which dispatches
to whichever entry point application your `Configure` method registered for that request/response type
pairing (here, the one `UseHttp` added). The declaration above generates exactly this class.
</details>

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

The template only covers the HTTP trigger path - add your own resources for Event Hub, Service
Bus, Cosmos DB, Storage, or Event Grid if you wire up those triggers too (see the next section).

### Deploying with Terraform

The same infrastructure is also available as Terraform, in
[`examples/Azure/Benzene.Example.Azure/main.tf`](../examples/Azure/Benzene.Example.Azure/main.tf) -
resource-for-resource equivalent to the Bicep template (Storage Account, workspace-based
Application Insights, Consumption plan, Linux isolated-worker Function App with a
system-assigned managed identity), plus a sketched `azurerm_role_assignment` for when you add
identity-based trigger connections:

```bash
cd examples/Azure/Benzene.Example.Azure
terraform init
terraform apply -var function_app_name=my-benzene-function
```

One deliberate difference: Terraform creates and manages the resource group itself (Terraform
convention), where the Bicep flow deploys into a group you create with `az group create`. Both
templates carry the same hand-checked-not-deployed disclaimer - review before production use.

## Non-HTTP triggers

Benzene provides specialized middleware for other Azure Functions trigger types, each
configured inside the same `Configure` method, on the same platform-neutral `app` shown in step 4
— a single `BenzeneStartUp` can wire up several trigger types at once, each with its own
sub-pipeline, exactly as with any other Benzene host:

- **HTTP**: `app.UseHttp(...)`, in `Benzene.Azure.Function.AspNet`
- **Event Hubs**: `app.UseEventHub(...)`, in `Benzene.Azure.Function.EventHub`
- **Kafka** (Event Hubs' Kafka-compatible endpoint): `app.UseKafka(...)`, in `Benzene.Azure.Function.Kafka`
- **Service Bus**: `app.UseServiceBus(...)`, in `Benzene.Azure.Function.ServiceBus`
- **Cosmos DB Change Feed**: `app.UseCosmosDbChangeFeed<TDocument>(...)`, in `Benzene.Azure.Function.CosmosDb`
- **Queue Storage**: `app.UseQueueStorage(...)`, in `Benzene.Azure.Function.QueueStorage`
- **Blob Storage**: `app.UseBlobStorage(...)`, in `Benzene.Azure.Function.BlobStorage`
- **Event Grid**: `app.UseEventGrid(...)`, in `Benzene.Azure.Function.EventGrid`
- **Timer**: `app.UseTimerTrigger(...)`, in `Benzene.Azure.Function.Timer`

Each trigger has two parts: the **pipeline** you wire in `Configure` (`app.UseEventHub(...)`,
`app.UseServiceBus(...)`, …, covered per-transport below) and the **trigger function** that feeds it.
Just like HTTP, you **declare** the trigger function and Benzene's source generator writes it — one
assembly attribute per trigger, you own every binding value:

```csharp
[assembly: BenzeneServiceBusTrigger(Name = "orders", QueueName = "orders", Connection = "ServiceBusConnection")]
// or a topic:  TopicName = "audit", SubscriptionName = "svc"
[assembly: BenzeneEventHubTrigger(Name = "telemetry", EventHubName = "telemetry", Connection = "EventHubConnection")]
[assembly: BenzeneKafkaTrigger(Name = "orders", BrokerList = "BrokerList", Topic = "orders", ConsumerGroup = "svc")]
[assembly: BenzeneQueueTrigger(Name = "orders", QueueName = "orders")]                       // Connection defaults to AzureWebJobsStorage
[assembly: BenzeneBlobTrigger(Name = "ingest", Path = "incoming/{name}")]                    // {name} binds the blob name
[assembly: BenzeneEventGridTrigger(Name = "events")]
[assembly: BenzeneCosmosDbTrigger(Name = "orders", DatabaseName = "shop", ContainerName = "orders", DocumentType = typeof(OrderDocument))]
[assembly: BenzeneTimerTrigger(Name = "nightly", Schedule = "0 0 2 * * *")]
```

Each attribute lives in that transport's `Benzene.Azure.Function.*` package. You still reference the
transport's `Microsoft.Azure.Functions.Worker.Extensions.*` package directly (a Functions tooling
requirement — the trigger isn't registered from a transitive reference), and the
`FunctionsEnableWorkerIndexing=false` note from the HTTP section applies to all of them.

The per-transport sections below cover each pipeline's wiring and options, and show the **hand-written**
trigger function as the escape hatch — the equivalent of the declaration above, for when you need a
binding shape the attribute doesn't expose.

### Event Hubs

Install the package and add the pipeline in `Configure`:

```bash
dotnet add package Benzene.Azure.Function.EventHub --prerelease
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.EventHubs --version 6.5.0
```

The Microsoft extension package must be referenced **directly** by your function app project.
`Benzene.Azure.Function.EventHub` already references it, so the `[EventHubTrigger]` attribute
compiles either way — but the Functions SDK's build step only discovers extensions your project
references directly, and without the direct reference the trigger is never registered (the
failure shows up only at `func start` as "No job functions found").

```csharp
app.UseEventHub(eventHub => eventHub
    .UseBenzeneMessage(direct => direct
        .UseMessageHandlers()));
```

`UseBenzeneMessage` routes an Event Hub event whose body deserializes into a **Benzene message
envelope** to the direct-message pipeline. The envelope is a small JSON wrapper any producer — a
Benzene client, or anything else — can send; on the wire it looks like this (note `body` is the
serialized payload *as a string*, and `topic` is what routes to the matching `[Message]` handler):

```json
{
  "topic": "orders.created",
  "headers": { "correlation-id": "abc-123" },
  "body": "{\"name\":\"some-name\"}"
}
```

This is the same envelope shape `MessageBuilder` produces for AWS SQS/SNS (and the same one the
Queue Storage trigger's `UseBenzeneMessage` reads — see that section below). An event whose body
*isn't* an envelope is simply deferred to the next middleware in the pipeline, not failed. Add a
trigger function that injects `IAzureFunctionApp` and calls `HandleEventHub(...)`:

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
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Kafka --version 4.3.0
```

(As with every trigger, reference the Microsoft extension package directly — the
`[KafkaTrigger]` attribute compiles transitively via the Benzene package, but the Functions
SDK's build step only registers extensions your project references directly.)

```csharp
app.UseKafka(kafka => kafka.UseMessageHandlers());
```

Works against Event Hubs' Kafka-compatible endpoint. The Kafka record's value is `byte[]`; Benzene
decodes it as UTF-8 JSON the same way as every other transport, and dispatches by topic via
`[Message]`/message handler registration — there is no `UseBenzeneMessage` bridge for Kafka today
(that exists for Event Hubs and Queue Storage, whose messages carry no routable topic of their
own). Add a trigger function that injects `IAzureFunctionApp` and calls `HandleKafkaEvents(...)`:

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
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.ServiceBus --version 5.22.0
```

(As with every trigger, reference the Microsoft extension package directly — the
`[ServiceBusTrigger]` attribute compiles transitively via the Benzene package, but the Functions
SDK's build step only registers extensions your project references directly.)

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
fixed topic instead — see [Common Middleware: UsePresetTopic](common-middleware.md#usepresettopic).
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
`params` array.)

By default the trigger completes each message automatically when the invocation returns —
whatever your handler returned. For **real per-message complete/abandon control tied to the
handler's outcome**, set `ServiceBusOptions.AckMode = ServiceBusAckMode.Explicit`, add
`AutoCompleteMessages = false` to the trigger attribute, bind `ServiceBusMessageActions`, and pass
it to `HandleServiceBusMessages(messageActions, message)` — a successful result completes the
message, a failure result or exception abandons it (respecting the queue's max delivery count
before Service Bus's native dead-lettering kicks in). The full walkthrough, including how
`CatchExceptions`/`RaiseOnFailureStatus` interact with it, is in
[Service Bus Message Handling, step 5](cookbooks/service-bus-handling.md#5-message-completion-the-default-and-real-per-message-control).

### Cosmos DB Change Feed

Install the package and add the pipeline in `Configure`:

```bash
dotnet add package Benzene.Azure.Function.CosmosDb --prerelease
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.CosmosDB
```

(As with every trigger: the Microsoft extension package must be referenced directly — it
supplies the `[CosmosDBTrigger]` attribute and the trigger registration. Benzene doesn't pin its
version because `Benzene.Azure.Function.CosmosDb` deliberately has no dependency on it.)

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
[Cosmos DB Change Feed Processing](cookbooks/cosmos-change-feed-processing.md) for the full
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

### Queue Storage

Install the package and add the pipeline in `Configure`:

```bash
dotnet add package Benzene.Azure.Function.QueueStorage --prerelease
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues
```

A Queue Storage message has no properties or attributes — the body is the entire message — so
there are two routing modes. If the producer sends the Benzene message envelope (the
`{"topic": ..., "headers": ..., "body": ...}` JSON shown in the Event Hubs section above), use
the `UseBenzeneMessage` bridge; if the queue carries raw payloads from a non-Benzene producer,
give the queue a fixed topic (a queue usually carries one message type anyway):

```csharp
// Envelope-routed (Benzene producer):
app.UseQueueStorage(queue => queue
    .UseBenzeneMessage(direct => direct.UseMessageHandlers()));

// Or fixed-topic (raw payloads):
app.UseQueueStorage(queue => queue
    .UsePresetTopic("orders.created")
    .UseMessageHandlers());
```

`Benzene.Azure.Function.QueueStorage` has no SDK dependency; your project supplies the trigger
attribute. Add a trigger function that injects `IAzureFunctionApp` and calls
`HandleQueueMessage(...)`:

```csharp
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.QueueStorage;
using Microsoft.Azure.Functions.Worker;

public class QueueFunction
{
    private readonly IAzureFunctionApp _app;

    public QueueFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("orders-queue")]
    public Task Run([QueueTrigger("orders", Connection = "StorageConnection")] string messageText)
    {
        return _app.HandleQueueMessage(messageText);
    }
}
```

(Bind the SDK's `QueueMessage` instead of `string` if you want the message id and dequeue count
available on the context — construct a `QueueStorageMessage` with those properties and pass it to
`HandleQueueMessages(...)`. On failure, the exception propagates and the host's own retry and
`<queue>-poison` machinery takes over — configure `maxDequeueCount`/`visibilityTimeout` in
`host.json`.)

### Blob Storage

Install the package and add the pipeline in `Configure`:

```bash
dotnet add package Benzene.Azure.Function.BlobStorage --prerelease
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs
```

A blob is a file, not a message envelope, and one blob-trigger function watches one container
path — so unlike the queue/Service Bus triggers there is no message-handler routing; the pipeline
consumes the blob directly via `UseBlob(...)` (composing with correlation/metrics/exception
middleware as usual):

```csharp
app.UseBlobStorage(blob => blob
    .UseBlob(async delivered =>
    {
        // delivered.Name, delivered.Content (byte[]), delivered.GetContentAsString()
    }));
```

Add a trigger function that binds the content and the `{name}` expression, and calls
`HandleBlob(...)`:

```csharp
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.BlobStorage;
using Microsoft.Azure.Functions.Worker;

public class BlobFunction
{
    private readonly IAzureFunctionApp _app;

    public BlobFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("invoice-uploaded")]
    public Task Run(
        [BlobTrigger("invoices/{name}", Connection = "StorageConnection")] byte[] content,
        string name)
    {
        return _app.HandleBlob(name, content);
    }
}
```

(On failure the host retries up to 5 times, then records the blob in its
`webjobs-blobtrigger-poison` queue. The classic blob trigger polls via blob receipts, so delivery
can lag on large containers — consider the Event Grid-based blob trigger source for
latency-sensitive work; the Benzene pipeline side is unchanged either way.)

### Event Grid

Install the package and add the pipeline in `Configure`:

```bash
dotnet add package Benzene.Azure.Function.EventGrid --prerelease
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.EventGrid
```

Event Grid events route **by their event type** — `Microsoft.Storage.BlobCreated`, or your own
custom types — exactly the way the AWS S3 adapter routes on the S3 event name. Declare message
handlers for those topics and the event's `data` payload arrives as the handler's request:

```csharp
app.UseEventGrid(eventGrid => eventGrid.UseMessageHandlers());
```

```csharp
[Message("Microsoft.Storage.BlobCreated")]
public class BlobCreatedHandler : IMessageHandler<BlobCreatedData>
{
    public Task HandleAsync(BlobCreatedData request) { /* ... */ }
}
```

`Benzene.Azure.Function.EventGrid` has no SDK dependency — bind the trigger as `string` and
Benzene parses it, handling **both** delivery schemas (the Event Grid schema and CloudEvents 1.0,
detected by `specversion`). The envelope's `id`, `subject`, and `source` surface as message
headers:

```csharp
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.EventGrid;
using Microsoft.Azure.Functions.Worker;

public class EventGridFunction
{
    private readonly IAzureFunctionApp _app;

    public EventGridFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("event-grid")]
    public Task Run([EventGridTrigger] string eventJson)
    {
        return _app.HandleEventGridEvent(eventJson);
    }
}
```

(On failure the exception propagates and Event Grid's own delivery retry — with backoff, up to 24
hours — and optional dead-letter destination take over, configured on the event subscription.)

### Timer

Install the package and add the pipeline in `Configure`:

```bash
dotnet add package Benzene.Azure.Function.Timer --prerelease
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.Timer
```

Two consumption modes. For scheduled work that doesn't need routing, `UseTick(...)`:

```csharp
app.UseTimerTrigger(timer => timer
    .UseTick(async info =>
    {
        // info.IsPastDue, info.ScheduleStatus?.Next
    }));
```

Or — the more interesting mode — give the pipeline a preset topic and the tick invokes the
message handler declaring it, making a scheduled job just another (testable, portable) message
handler:

```csharp
app.UseTimerTrigger(timer => timer
    .UsePresetTopic("nightly-cleanup")
    .UseMessageHandlers());
```

(The extension is named `UseTimerTrigger` because `UseTimer` is already `Benzene.Diagnostics`' timing
middleware.) Add a trigger function — bind the timer parameter directly as Benzene's
`TimerTriggerInfo`, whose property names match the worker's `TimerInfo` JSON:

```csharp
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.Timer;
using Microsoft.Azure.Functions.Worker;

public class NightlyCleanupFunction
{
    private readonly IAzureFunctionApp _app;

    public NightlyCleanupFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("nightly-cleanup")]
    public Task Run([TimerTrigger("0 0 2 * * *")] TimerTriggerInfo timer)
    {
        return _app.HandleTimer(timer);
    }
}
```

(Platform reality worth knowing: a failed tick is **not** retried — the next occurrence just runs
on schedule. A job needing at-least-once semantics should enqueue its work rather than doing it
inline.)

### Managed identity instead of connection strings

Every trigger above can authenticate as the Function App's managed identity with **zero code
changes** — the `Connection` name in the trigger attribute stays the same, and only the app
settings change (`ServiceBusConnection` → `ServiceBusConnection__fullyQualifiedNamespace`, etc.),
plus RBAC role assignments to the app's identity. See
[Managed Identity & RBAC for Azure Resources](cookbooks/managed-identity.md) for the per-trigger
settings, the roles each trigger needs (including the Functions host's own storage roles), and
the Consumption-plan caveat. The example's `main.bicep` enables a system-assigned identity and
outputs its `principalId` ready for role assignments.

## Correlation and tracing

Every middleware in every pipeline — HTTP, Event Hub, Kafka — is automatically wrapped in a
`System.Diagnostics.Activity` span once you call `AddDiagnostics()` in `ConfigureServices`:

```csharp
services.UsingBenzene(x => x.AddDiagnostics());
```

This is the same tracing system used across every Benzene host, not something Azure-specific. See
[Monitoring & Diagnostics](monitoring.md) for the full picture, and [Correlation Ids](correlation-ids.md)
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
[Logging to Application Insights](cookbooks/logging-application-insights.md) for the full recipe
(including KQL queries against `customDimensions`) and
[Distributed Tracing with OpenTelemetry](cookbooks/distributed-tracing-opentelemetry.md) if you'd rather
export Benzene's `Activity` spans to Application Insights via OTLP instead of (or alongside) the
classic SDK shown here.

## Testing

Benzene ships a unified test host (`Benzene.Testing`) that builds an in-memory app straight from
your real `StartUp` — no need to run `func start` or hit the network. See
[Testing Benzene](testing-benzene.md) for the full picture; for Azure Functions specifically:

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
  and the worker extension manifest. The same applies per trigger: each
  `Microsoft.Azure.Functions.Worker.Extensions.*` package must be a direct reference (see the
  install block in each trigger's section above).
- **A non-HTTP trigger never fires locally**: every trigger except HTTP needs
  `AzureWebJobsStorage` to be a real connection — the empty string in step 6's
  `local.settings.json` is enough for HTTP only. Run [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite)
  and set `"AzureWebJobsStorage": "UseDevelopmentStorage=true"` (a Queue Storage trigger then also
  reads its queue from Azurite), plus the trigger's own connection setting
  (`ServiceBusConnection`, `EventHubConnection`, ...) pointing at a real namespace or emulator.
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

- [Message Handlers](message-handlers.md) — the full picture on `[Message]`, `[HttpEndpoint]`, and handler discovery
- [ASP.NET Core Integration](asp-net-core.md) — the same `UseHttp` pipeline, hosted outside Azure Functions
- [Testing Benzene](testing-benzene.md) — `BenzeneTestHost`, including AWS Lambda and ASP.NET Core patterns
- [Monitoring & Diagnostics](monitoring.md) — tracing, metrics, and W3C trace context propagation
- [Correlation Ids](correlation-ids.md) — the legacy header-based correlation ID middleware
- [`examples/Azure`](../examples/Azure) — a complete, runnable project covering HTTP routing, validation, OpenAPI spec generation, and Service Bus + Queue Storage triggers dispatching into the same handlers
