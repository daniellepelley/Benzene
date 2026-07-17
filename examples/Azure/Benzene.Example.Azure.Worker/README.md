# Benzene Azure self-hosted worker example

Consume **Azure Service Bus** and **Azure Event Hubs** from a plain long-running .NET Worker
Service — no Azure Functions runtime. Benzene owns the process and hosts both consumers as
background `IHostedService`s, dispatching every message and event through the same
message-handler pipeline (the shared `Benzene.Examples.App` order handlers, routed by the
message's `"topic"`).

This is the self-hosted counterpart of the Azure Functions example next door
(`../Benzene.Example.Azure`): same handlers, but here you own the host instead of a Functions
trigger. It uses:

- [`Benzene.Azure.ServiceBus`](../../../src/Benzene.Azure.ServiceBus) — `worker.UseServiceBus(...)`,
  a `ServiceBusProcessor` over a queue.
- [`Benzene.Azure.EventHub`](../../../src/Benzene.Azure.EventHub) — `worker.UseEventHub(...)`, an
  `EventProcessorClient` over an event hub with blob-checkpointed offsets.

See [Worker Service Setup, Part B](../../../docs/getting-started-worker.md#part-b-built-in-workers-kafka-http-service-bus-event-hub)
for the guide this example follows.

## What it does

`StartUp.Configure` registers both consumers inside a single `UseWorker(...)`:

```csharp
app.UseWorker(worker => worker
    .UseServiceBus(serviceBusConfig, new ServiceBusClientFactory(serviceBusClient),
        serviceBus => serviceBus.UseMessageHandlers())
    .UseEventHub(eventHubConfig, new EventProcessorClientFactory(processorClient),
        eventHub => eventHub.UseMessageHandlers()));
```

A message whose `"topic"` is `order_create` (and whose body is a JSON `CreateOrderMessage`) routes
to `CreateOrderMessageHandler` in `Benzene.Examples.App`, regardless of whether it arrived over
Service Bus or Event Hubs. The `LoggingProcessTimer` (wired in `DependenciesBuilder`) logs each
handled message, so you can see the pipeline fire.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker, to run the local Azure emulators (Service Bus emulator + its SQL Server backend, Event
  Hubs emulator + Azurite for the blob checkpoint store)

## Run it

```bash
# from this directory
docker compose up -d      # start the Service Bus + Event Hubs emulators
dotnet run                # start the worker; leave it running
```

The emulator connection strings and entity names are already set in `config.json` (a Service Bus
queue named `orders` and an event hub named `orders`, both created by the emulator config files in
this folder). `EventProcessorClient` needs its blob checkpoint container to exist; the example
creates it on startup, so no manual step is required.

## Send a test message

With the worker running, send a message to either transport from a scratch console app (or
LINQPad). Set the `topic` property to `order_create` so it routes to the order handler:

**Service Bus**

```csharp
using Azure.Messaging.ServiceBus;

const string connectionString =
    "Endpoint=sb://localhost:5673;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

await using var client = new ServiceBusClient(connectionString);
var sender = client.CreateSender("orders");

var message = new ServiceBusMessage("""{"status":"new","name":"widget"}""");
message.ApplicationProperties["topic"] = "order_create";
await sender.SendMessageAsync(message);
```

**Event Hubs**

```csharp
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

const string connectionString =
    "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

await using var producer = new EventHubProducerClient(connectionString, "orders");

var eventData = new EventData("""{"status":"new","name":"widget"}""");
eventData.Properties["topic"] = "order_create";
await producer.SendAsync(new[] { eventData });
```

Either way, the worker logs the handled `order_create` message.

## Switching to real Azure

Point `config.json` (or environment variables — `ServiceBus__ConnectionString`,
`EventHub__ConnectionString`, `Storage__ConnectionString`, ...) at a real Service Bus namespace,
Event Hubs namespace, and Storage account. Nothing in `StartUp`/`DependenciesBuilder` changes —
only configuration. For production you'd typically build the `ServiceBusClient`/`EventProcessorClient`
with a `TokenCredential` (managed identity) instead of a connection string; the client factories
in this example are the seam where you'd do that.
