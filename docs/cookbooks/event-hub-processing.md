# Event Hub Stream Processing

Handle high-throughput Event Hub streams with Benzene, and understand exactly where Benzene's
responsibility ends and the Azure Functions runtime's begins.

## Problem Statement

You're ingesting a high-volume stream through Azure Event Hubs (telemetry, clickstream, change
events) and want to process it with Benzene's message-handler pipeline instead of hand-rolling
per-event dispatch logic. Doing this well means understanding a few things the
[Azure Functions getting-started guide](../azure-functions.md) doesn't go into:

- How Benzene actually processes a triggered batch of events internally (sequentially? in
  parallel? with what isolation between events?).
- What Benzene controls about batching, checkpointing, and retries — and what is purely the
  Azure Functions Event Hubs extension's job, configured in `host.json`, with zero Benzene
  involvement.
- What happens to a "poison" event that reliably fails, given Event Hubs has no native dead-letter
  queue the way Service Bus does.
- How to reach data on the raw `EventData` (partition key, sequence number, custom `Properties`)
  that never makes it into a Benzene message envelope.

This cookbook works through a realistic handler for a high-throughput scenario and answers each of
those honestly, citing the actual source in `src/Benzene.Azure.Function.EventHub/`.

## Prerequisites

- An Azure Functions isolated-worker project already wired up per
  [Azure Functions Setup](../azure-functions.md), steps 1–5 (project, `BenzeneStartUp`,
  `Program.cs`).
- An Event Hubs namespace and event hub, with a connection string or managed identity configured
  for the Function App.
- Familiarity with the direct-message envelope Benzene uses internally (topic + JSON payload) —
  see [Message Handlers](../message-handlers.md).

## Installation

```bash
dotnet add package Benzene.Azure.Function.EventHub --prerelease
```

This also needs `Microsoft.Azure.Functions.Worker.Extensions.EventHubs` (version `6.5.0` is what
Benzene itself builds against) referenced directly in your function app project, same as the other
Microsoft worker packages — see [Azure Functions Setup, step 2](../azure-functions.md#2-install-the-nuget-packages).

```bash
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.EventHubs --version 6.5.0
```

## Step-by-Step Implementation

### 1. The trigger function and pipeline

This is the same shape as the [Event Hubs section](../azure-functions.md#event-hubs) of the
getting-started guide:

```csharp
using Benzene.Azure.Function.EventHub;

app.UseEventHub(eventHub => eventHub
    .UseBenzeneMessage(direct => direct
        .UseMessageHandlers()));
```

```csharp
using Azure.Messaging.EventHubs;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.EventHub.Function;
using Microsoft.Azure.Functions.Worker;

public class TelemetryEventHubFunction
{
    private readonly IAzureFunctionApp _app;

    public TelemetryEventHubFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("telemetry-event-hub")]
    public Task Run(
        [EventHubTrigger("telemetry-hub", ConsumerGroup = "%TelemetryConsumerGroup%", Connection = "EventHubConnection")]
        EventData[] events)
    {
        return _app.HandleEventHub(events);
    }
}
```

`ConsumerGroup` is read from configuration here (`%TelemetryConsumerGroup%`) rather than hardcoded,
which matters once you have more than one consumer of the same hub — see
[Troubleshooting](#partition-and-consumer-group-misconfiguration) below.

### 2. How Benzene actually processes a batch — read the source, not the assumption

`[EventHubTrigger]` hands you the whole batch as `EventData[]`. It's tempting to assume Benzene
loops over that array and processes events one at a time, in partition order. It doesn't. Reading
`src/Benzene.Azure.Function.EventHub/Function/EventHubApplication.cs`:

```csharp
public class EventHubApplication : EntryPointMiddlewareApplication<EventData[]>
{
    public EventHubApplication(IMiddlewarePipeline<EventHubContext> pipelineBuilder, IServiceResolverFactory serviceResolverFactory)
        : base(new MiddlewareMultiApplication<EventData[], EventHubContext>(
                new TransportMiddlewarePipeline<EventHubContext>("event-hub", pipelineBuilder),
        @event => @event.Select(EventHubContext.CreateInstance).ToArray()),
            serviceResolverFactory)
    { }
}
```

Every `EventData` in the batch is wrapped in its own `EventHubContext`, and the fan-out is done by
`MiddlewareMultiApplication<TEvent, TContext>` (`src/Benzene.Core.Middleware/MiddlewareMultiApplication.cs`):

```csharp
public Task HandleAsync(TEvent @event, IServiceResolverFactory serviceResolverFactory)
{
    var tasks = mapper(@event).Select(async context =>
        {
            using var scope = serviceResolverFactory.CreateScope();
            await pipeline.HandleAsync(context, scope);
        })
        .ToArray();

    return Task.WhenAll(tasks);
}
```

Two consequences follow directly from this, and they matter for a high-throughput handler:

- **Events in a batch run concurrently, each in its own DI scope**, via `Task.WhenAll`. This is
  good for throughput — a batch of hundreds of events isn't processed one at a time — but it means
  **Benzene does not preserve intra-batch ordering**, even though Event Hubs guarantees ordering
  *within a partition* on the wire. If your handler logic depends on processing events from the
  same partition in order (e.g. applying updates to the same aggregate), you cannot rely on
  Benzene's batch processing to preserve that order; you need to either partition your own
  processing by a key extracted from `EventHubContext.EventData` yourself, or reduce
  `maxEventBatchSize` to `1` in `host.json` (at a real cost to throughput — see
  [step 5](#5-batching-and-checkpointing-are-entirely-the-runtimes-job)).
- **A fresh DI scope per event** means scoped services (e.g. a scoped `DbContext`) are never shared
  across two events in the same batch. There is no Benzene concept of "batch-scoped" or
  per-invocation aggregation — if you want to, say, write 500 telemetry readings in one database
  round trip instead of 500 round trips, that has to happen outside Benzene's per-event pipeline
  (e.g. buffering in your own singleton service and flushing on a timer/threshold), not inside a
  message handler.

### 3. A realistic handler

```csharp
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Results;

[Message("telemetry:reading")]
public class TelemetryReadingHandler : IMessageHandler<TelemetryReadingRequest, TelemetryReadingResponse>
{
    private readonly ITelemetryStore _store;

    public TelemetryReadingHandler(ITelemetryStore store)
    {
        _store = store;
    }

    public async Task<IBenzeneResult<TelemetryReadingResponse>> HandleAsync(TelemetryReadingRequest request)
    {
        await _store.RecordAsync(request.DeviceId, request.Value, request.Timestamp);
        return BenzeneResult.Ok(new TelemetryReadingResponse { Accepted = true });
    }
}

public class TelemetryReadingRequest
{
    public string DeviceId { get; set; }
    public double Value { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public class TelemetryReadingResponse
{
    public bool Accepted { get; set; }
}
```

This handler is invoked because `BenzeneMessageEventHubHandler`
(`src/Benzene.Azure.Function.EventHub/Function/BenzeneMessageEventHubHandler.cs`) deserializes each
`EventData.EventBody` into a `BenzeneMessageRequest { Topic, Headers, Body }` envelope and only
handles it if `Topic` is non-null:

```csharp
protected override bool CanHandle(BenzeneMessageRequest request)
{
    return request?.Topic != null;
}

protected override BenzeneMessageRequest TryExtractRequest(EventHubContext context)
{
    try
    {
        return _serializer.Deserialize<BenzeneMessageRequest>(context.EventData.EventBody.ToString());
    }
    catch
    {
        return default;
    }
}
```

That means your Event Hub **producer** needs to publish that envelope shape — `{"topic":
"telemetry:reading", "body": "{...serialized TelemetryReadingRequest...}"}` — not a bare JSON
payload. If you're publishing from a Benzene client (another Benzene service, or a test), the
`MessageBuilder`/`AsEventHubBenzeneMessage()` helpers produce this shape for you (see
[Testing](#testing) below). If your producer is a non-Benzene device or service emitting raw JSON
telemetry with no topic wrapper, `CanHandle` returns `false` for every event, and — because
`TryExtractRequest` swallows deserialization exceptions and `MiddlewareRouter.HandleAsync` just
calls `next()` when a router can't handle a request — **the event is silently dropped with no
error** unless you've registered something else after `UseBenzeneMessage` to catch it. For non-
enveloped producers, write your own `IMiddleware<EventHubContext>` (see the next section) rather
than trying to force raw payloads through `UseBenzeneMessage`.

### 4. Reaching data that never makes it into the envelope

`EventHubContext` only exposes the raw `EventData`:

```csharp
public class EventHubContext
{
    public static EventHubContext CreateInstance(EventData eventData) => new(eventData);
    public EventData EventData { get; }
}
```

Partition key, sequence number, enqueued time, and any custom application properties set by the
producer (`EventData.Properties`) are all on that `EventData` object, but none of them flow into
`BenzeneMessageRequest`/your handler's request type automatically. To use them — for metrics,
filtering, or routing decisions — add your own middleware to the Event Hub pipeline, before
`UseBenzeneMessage`:

```csharp
using Azure.Messaging.EventHubs;
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.EventHub;
using Microsoft.Extensions.Logging;

public class TelemetryEventMetadataMiddleware : IMiddleware<EventHubContext>
{
    private readonly ILogger<TelemetryEventMetadataMiddleware> _logger;

    public TelemetryEventMetadataMiddleware(ILogger<TelemetryEventMetadataMiddleware> logger)
    {
        _logger = logger;
    }

    public string Name => "TelemetryEventMetadata";

    public Task HandleAsync(EventHubContext context, Func<Task> next)
    {
        var eventData = context.EventData;

        // Custom application properties set by the producer (EventData.Properties), not part of
        // the Benzene envelope.
        if (eventData.Properties.TryGetValue("schema-version", out var schemaVersion)
            && !"2".Equals(schemaVersion?.ToString()))
        {
            _logger.LogWarning(
                "Skipping event with unsupported schema-version {SchemaVersion} on partition {PartitionKey}, sequence {SequenceNumber}",
                schemaVersion, eventData.PartitionKey, eventData.SequenceNumber);
            return Task.CompletedTask; // short-circuits: does not call next()
        }

        _logger.LogDebug(
            "Processing event enqueued at {EnqueuedTime} on partition {PartitionKey}, offset {Offset}",
            eventData.EnqueuedTime, eventData.PartitionKey, eventData.Offset);

        return next();
    }
}
```

Register it ahead of `UseBenzeneMessage`:

```csharp
app.UseEventHub(eventHub => eventHub
    .Use(resolver => new TelemetryEventMetadataMiddleware(resolver.GetService<ILogger<TelemetryEventMetadataMiddleware>>()))
    .UseBenzeneMessage(direct => direct
        .UseMessageHandlers()));
```

`EventData.Properties`, `PartitionKey`, `SequenceNumber`, `Offset`, and `EnqueuedTime` are all
standard `Azure.Messaging.EventHubs.EventData` members — nothing Benzene-specific — Benzene simply
hands you the object untouched.

### 5. Batching and checkpointing are entirely the runtime's job

This is worth stating plainly: **Benzene has no API for batch size, prefetch, or checkpointing**.
`EventHubContext` exposes exactly one property (`EventData`); there is no `Checkpoint()` method,
no batch-size configuration, and nothing in `Benzene.Azure.Function.EventHub`'s source references
checkpointing at all. All of that is configured through `host.json`, entirely by the
`Microsoft.Azure.Functions.Worker.Extensions.EventHubs` extension, independent of Benzene:

```json
{
  "version": "2.0",
  "extensions": {
    "eventHubs": {
      "maxEventBatchSize": 100,
      "minEventBatchSize": 25,
      "maxWaitTime": "00:00:05",
      "batchCheckpointFrequency": 5,
      "prefetchCount": 300
    }
  }
}
```

- `maxEventBatchSize` / `minEventBatchSize` / `maxWaitTime` control how many events get bundled
  into a single `EventData[]` your trigger function receives (and therefore how large a batch
  `MiddlewareMultiApplication` fans out per invocation).
- `prefetchCount` controls how many events the underlying client reads ahead of what's been
  dispatched to your function; keep it at least as large as `maxEventBatchSize`, commonly several
  multiples of it, or throughput suffers.
- `batchCheckpointFrequency` controls how often (in terms of processed batches) a checkpoint is
  written to the backing storage account — the default is 1 (checkpoint after every batch); raising
  it trades reduced storage I/O for more events being reprocessed on a restart.

None of the `[EventHubTrigger]` attribute properties on your trigger function change this either —
`ConsumerGroup` and `Connection` are the only two that matter for routing/auth; batch tuning is
`host.json`-only.

### 6. Checkpointing on failure, and why Benzene doesn't help with poison events

This is the part that's easy to get wrong, so it's worth being precise:

- **By default, the Event Hubs extension advances the checkpoint whether or not your function
  threw an exception.** Only if you configure a retry policy (in `host.json`, or via a
  `[FixedDelayRetry]`/`[ExponentialBackoffRetry]` attribute on the trigger function) does the
  runtime hold the checkpoint back until retries are exhausted — and once they *are* exhausted, the
  checkpoint still advances. There is no infinite hold, and Event Hubs has **no dead-letter queue**
  the way Service Bus does — an event the runtime gives up on is just gone from the perspective of
  that checkpoint.
- **Benzene's own message-handler wrapper already catches exceptions your handler throws**, before
  they ever reach the trigger function. `MessageHandler.HandleAsync`
  (`src/Benzene.Core.MessageHandlers/MessageHandler.cs`) wraps every handler invocation:

  ```csharp
  catch(ArgumentException ex)
  {
      return BenzeneResult.Set(_defaultStatuses.ValidationError, ex.Message);
  }
  catch(Exception ex)
  {
      return BenzeneResult.ServiceUnavailable("Message handler threw an exception", ex.Message);
  }
  ```

  This means a "poison" event — one whose payload reliably throws inside your handler's business
  logic — does **not** propagate as an exception out of `HandleEventHub` by default. It comes back
  as a `service-unavailable` (or `validation-error`) result, the trigger function returns normally,
  and the Functions runtime checkpoints the batch as successfully processed. Benzene's result
  status has no effect on the Event Hubs extension's retry/checkpoint behavior — that machinery
  only reacts to the .NET exception actually escaping your trigger function.

If you want poison events to interact with the runtime's retry policy, you have to bridge that gap
yourself — Benzene does not do it for you. One way: inspect the result and rethrow inside your own
trigger function or an Event Hub-level middleware, so the exception actually reaches the runtime:

```csharp
using Benzene.Abstractions.Middleware;
using Benzene.Azure.Function.EventHub;
using Benzene.Core.Messages.BenzeneMessage;
using Benzene.Results;

public class RethrowOnServiceUnavailableMiddleware : IMiddleware<BenzeneMessageContext>
{
    public string Name => "RethrowOnServiceUnavailable";

    public async Task HandleAsync(BenzeneMessageContext context, Func<Task> next)
    {
        await next();

        if (context.BenzeneMessageResponse.StatusCode == BenzeneResultStatus.ServiceUnavailable)
        {
            // Escalate to an actual exception so host.json's retry policy (and, once retries are
            // exhausted, the runtime's failure logging) can act on it.
            throw new InvalidOperationException(
                $"Handler for topic '{context.BenzeneMessageRequest.Topic}' returned ServiceUnavailable");
        }
    }
}
```

Add it inside the direct-message pipeline, before `UseMessageHandlers`:

```csharp
app.UseEventHub(eventHub => eventHub
    .UseBenzeneMessage(direct => direct
        .Use(resolver => new RethrowOnServiceUnavailableMiddleware())
        .UseMessageHandlers()));
```

Even with this in place, remember there's still no DLQ underneath — once retries (if any) are
exhausted, the event is checkpointed past regardless. For real poison-message handling, log the
raw `EventData` (body, partition, offset, sequence number) somewhere durable — a storage table, a
separate "dead" Event Hub, a queue — from within your handler or a middleware, *before* deciding
whether to let the failure surface, so you have something to replay or inspect later. Benzene gives
you the hooks (middleware, `EventHubContext.EventData`) to build that; it doesn't ship it.

## Testing

Use `Benzene.Testing` and the Event Hub test helpers, exactly as the message-handling tests in
`test/Benzene.Core.Test/Azure/EventHubPipelineTest.cs` do:

```csharp
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.EventHub.TestHelpers;
using Benzene.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

var mockTelemetryStore = new Mock<ITelemetryStore>();

var app = BenzeneTestHost.Create<StartUp>()
    .WithServices(services => services.AddScoped(_ => mockTelemetryStore.Object))
    .BuildAzureFunctionApp();

var request = MessageBuilder
    .Create("telemetry:reading", new TelemetryReadingRequest { DeviceId = "sensor-1", Value = 21.5, Timestamp = DateTimeOffset.UtcNow })
    .AsEventHubBenzeneMessage();

await app.HandleEventHub(request);

mockTelemetryStore.Verify(x => x.RecordAsync("sensor-1", 21.5, It.IsAny<DateTimeOffset>()));
```

`HandleEventHub` takes `params EventData[]`, so you can pass a whole batch to exercise the
parallel-fan-out behavior directly:

```csharp
var batch = Enumerable.Range(0, 50)
    .Select(i => MessageBuilder.Create("telemetry:reading", new TelemetryReadingRequest { DeviceId = $"sensor-{i}", Value = i }).AsEventHubBenzeneMessage())
    .ToArray();

await app.HandleEventHub(batch);

mockTelemetryStore.Verify(x => x.RecordAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<DateTimeOffset>()), Times.Exactly(50));
```

For pipeline-only tests without a full `StartUp`, `InlineAzureFunctionStartUp` works the same way
as it does in `azure-functions.md`'s testing section:

```csharp
var app = new InlineAzureFunctionStartUp()
    .ConfigureServices(services => services
        .UsingBenzene(x => x.AddMessageHandlers(typeof(TelemetryReadingHandler).Assembly))
        .AddSingleton(mockTelemetryStore.Object))
    .Configure(app => app
        .UseEventHub(eventHub => eventHub
            .UseBenzeneMessage(direct => direct
                .UseMessageHandlers())))
    .Build();
```

## Troubleshooting

### Partition and consumer-group misconfiguration

- **Events never arrive / function never triggers**: check the `Connection` property on
  `[EventHubTrigger]` — it names an app setting (or `local.settings.json` value) holding the Event
  Hubs connection string, not the connection string itself. A typo here fails silently in local
  development (no error, just nothing happens).
- **The function re-processes old events on every restart, or reads from the wrong offset**: each
  distinct `ConsumerGroup` maintains its own checkpoint/offset state. If you deploy a second
  function against the same hub using the default `$Default` consumer group instead of a dedicated
  one, both functions compete for the same checkpoints and offsets, and you'll see inconsistent
  redelivery. Give every distinct consumer its own consumer group, created ahead of time on the
  Event Hub itself (`az eventhubs eventhub consumer-group create`) — the extension does not create
  consumer groups for you.
- **Only a subset of instances scale out under load / uneven partition distribution**: this is
  entirely a function of your Event Hub's partition count (fixed at creation) and the Functions
  host's own scaling, not something Benzene participates in — Benzene never sees partitions
  directly, only the `EventData[]` batch the trigger hands it.

### Handler never gets invoked, but no error appears

Per [step 3](#3-a-realistic-handler): confirm the producer is actually publishing the
`{topic, body}` envelope `BenzeneMessageEventHubHandler` expects. If `Topic` deserializes to
`null` (or the body isn't valid JSON at all), the event is routed to `next()` in the pipeline and,
if nothing else is registered to handle it, disappears with no exception, no log, no result —
because `CanHandle` returning `false` is a normal "not for me" outcome, not a failure.

### A single bad event seems to have no effect, but data loss still happened

Remember `MessageHandler.HandleAsync` converts your handler's exceptions into a `service-unavailable`
result rather than throwing (see [step 6](#6-checkpointing-on-failure-and-why-benzene-doesnt-help-with-poison-events)).
If you're not logging inside the handler or checking the returned status somewhere, a systematically
failing event type can churn through your pipeline indefinitely, checkpointing past every time,
with nothing visible unless you've wired up logging/diagnostics
(`AddDiagnostics()` — see [Monitoring & Diagnostics](../monitoring.md)) or the rethrow pattern above.

### Ordering bugs that only show up under load

If your handler assumes events for the same key arrive and complete in order, but you're seeing
interleaved writes, revisit [step 2](#2-how-benzene-actually-processes-a-batch--read-the-source-not-the-assumption) —
`MiddlewareMultiApplication` processes every event in a batch concurrently via `Task.WhenAll`.
Partition-level ordering from Event Hubs does not survive into per-event concurrent handler
execution.

## Variations

### Kafka-compatible endpoint

Event Hubs' Kafka-compatible endpoint is handled by a separate package
(`Benzene.Azure.Function.Kafka`, `app.UseKafka(...)`), with its own trigger shape
(`[KafkaTrigger]`/`KafkaRecord[]`). There is no `UseBenzeneMessage` envelope bridge for Kafka today
— dispatch there is by `[Message]`/topic directly on the decoded UTF-8 JSON body. See the
[Kafka section](../azure-functions.md#kafka) of the getting-started guide.

### Correlation across a batch

Each event in a batch gets its own DI scope, so `IBenzeneInvocation` (populated via
`app.UseBenzeneInvocation()` — see [Correlation and tracing](../azure-functions.md#correlation-and-tracing))
resolves independently per event, not once per Function invocation. If you need a single
correlation identifier that spans the whole triggered batch (rather than per-event), you'll need
to generate and thread it through yourself, e.g. via a middleware ahead of the per-event fan-out
that stashes a batch ID somewhere your per-event middleware can pick up.

### Consuming without Azure Functions (self-hosted worker)

This whole cookbook assumes an Azure Functions Event Hub *trigger*, where — as step 5 stresses —
batching and checkpointing are **entirely the runtime's job**. If you want to consume an event hub
from a long-running process you own instead, use `Benzene.Azure.EventHub` (not
`Benzene.Azure.Function.EventHub`). It's a self-hosted
[worker](../getting-started-worker.md#azure-event-hubs-benzeneazureeventhub):
`worker.UseEventHub(config, clientFactory, eh => eh.UseMessageHandlers())` runs the SDK's
`EventProcessorClient` and dispatches each event through the same `[Message]`/topic model (from the
event's `"topic"` property).

The key inversion from the trigger: **here Benzene owns what the runtime owned above.**

- **Checkpointing is Benzene's**, not `host.json`'s — `BenzeneEventHubConfig.CheckpointInterval`
  controls how many successfully handled events a partition accumulates before it checkpoints (via
  `EventProcessorClient`'s blob store). Step 5's "the runtime checkpoints the whole batch regardless
  of exceptions" no longer applies; a failed event is never itself checkpointed.
- **Poison-event behaviour is a config toggle, not a workaround.** `CatchHandlerExceptions` (default
  `true`) skips-and-continues like the runtime does; set it `false` to stop the worker without
  checkpointing the failure, so a restart redelivers it (at-least-once) — the self-hosted answer to
  the honest "Benzene can't help with poison events under the trigger" of step 6.
- **Starting position is yours.** `DefaultStartingPosition` (e.g. `EventPosition.Earliest`) sets
  where a fresh consumer group with no checkpoint begins — the `EventProcessorClient` default is the
  *end* of the partition.

See [Worker Service Setup, Part B](../getting-started-worker.md#part-b-built-in-workers-kafka-http-service-bus-event-hub-cosmos-db)
for the full host wiring.

## Further Reading

- [Azure Functions Setup](../azure-functions.md) — project setup, HTTP routing, and the Event
  Hub/Kafka trigger basics this cookbook builds on
- [Message Handlers](../message-handlers.md) — `[Message]`, handler discovery, and the
  `BenzeneMessage` envelope
- [Monitoring & Diagnostics](../monitoring.md) — `AddDiagnostics()`, tracing every middleware in
  the Event Hub pipeline
- [Correlation Ids](../correlation-ids.md) — the header-based legacy correlation alternative
- [Testing Benzene](../testing-benzene.md) — `BenzeneTestHost`, `InlineAzureFunctionStartUp`
- [Worker Service Setup](../getting-started-worker.md) — consuming Event Hubs in-process, without
  Azure Functions, via `Benzene.Azure.EventHub` (see "Consuming without Azure Functions" above)
- [Microsoft.Azure.Functions.Worker.Extensions.EventHubs host.json reference](https://learn.microsoft.com/azure/azure-functions/functions-bindings-event-hubs) — the runtime-side batching/checkpointing settings this cookbook references
