# Cosmos DB Change Feed Processing

Consume a Cosmos DB container's change feed with Benzene as an ordered stream of documents, and
understand exactly where Benzene's responsibility ends and the Azure Functions runtime's begins.

## Problem Statement

You have a Cosmos DB container and want to react to document changes — build materialized views,
invalidate caches, project events into another store — without hand-rolling change feed plumbing.
Doing this well means understanding a few things up front:

- Why the Cosmos DB adapter looks different from every other Benzene Azure trigger (no
  `UseMessageHandlers()`, generic over your document type).
- How Benzene processes a triggered batch internally (one pipeline run over an ordered stream,
  not per-document fan-out).
- What Benzene controls about checkpointing and retries — and what is purely the Azure Functions
  Cosmos DB extension's job (leases, polling, batch size), with zero Benzene involvement.
- What redelivery looks like when your handler fails, and why idempotency is non-negotiable.

This cookbook works through a realistic projection handler and answers each of those honestly,
citing the actual source in `src/Benzene.Azure.Function.CosmosDb/`.

## Prerequisites

- An Azure Functions isolated-worker project already wired up per
  [Azure Functions Setup](../azure-functions.md), steps 1–5 (project, `BenzeneStartUp`,
  `Program.cs`).
- A Cosmos DB account with the container you want to observe, and a connection string or managed
  identity configured for the Function App.

## Installation

```bash
dotnet add package Benzene.Azure.Function.CosmosDb --prerelease
dotnet add package Microsoft.Azure.Functions.Worker.Extensions.CosmosDB
```

The Microsoft extension package must be referenced directly by your function app project — it
supplies the `[CosmosDBTrigger]` binding. `Benzene.Azure.Function.CosmosDb` itself deliberately
carries **no** Azure SDK dependency: the trigger hands Benzene already-deserialized documents, so
no Cosmos types appear anywhere in the adapter.

## Why this adapter is generic over your document type

Every other Benzene Azure trigger (Event Hubs, Service Bus, Kafka) receives an opaque transport
payload — raw bytes plus headers — that Benzene deserializes and routes to a message handler by
topic. The Cosmos DB trigger is fundamentally different: it delivers **documents of a concrete
type you choose**, already deserialized by the Functions runtime. A changed document has no
envelope, no topic, no headers — it's just your data, again. So there is nothing for
`UseMessageHandlers()` to route on, and the pipeline is instead generic over the document type:
`UseCosmosDbChangeFeed<TDocument>(...)` builds a pipeline of `StreamContext<TDocument>`.

## Why fan-in (a stream), not fan-out (per-document dispatch)

The change feed delivers changes **in order within each partition key range**, and the trigger's
lease checkpoints a whole batch at a time — there is no per-document resume token (unlike a
Kinesis sequence number). Fanning the batch out into isolated concurrent per-document contexts
would throw that ordering away and create false failure isolation (one failed document can't be
retried alone anyway — the whole batch redelivers). So Benzene presents the batch intact: one
`StreamContext<TDocument>`, one pipeline run, one DI scope, documents pulled lazily in feed order.
This is the same streaming engine as `UseEventHubStream` and AWS's `UseKinesisStream` — the
stream operators (`PartitionBy`, `Window` — see the Kinesis section of
[Getting Started with AWS](../getting-started-aws.md)) compose with it.

## The recipe

### 1. Define the document type

The type the trigger deserializes changes into — typically a projection of the container's
documents with the properties you care about:

```csharp
public class OrderDocument
{
    public string id { get; set; }          // Cosmos documents use lowercase "id"
    public string CustomerId { get; set; }
    public string Status { get; set; }
    public decimal Total { get; set; }
}
```

### 2. Configure the pipeline

In your `BenzeneStartUp.Configure` (or inline startup), on the same platform-neutral `app` as
every other trigger:

```csharp
public override void Configure(IBenzeneApplicationBuilder app)
{
    app.UseCosmosDbChangeFeed<OrderDocument>(feed => feed
        .UseStream<OrderDocument>(async (documents, cancellationToken) =>
        {
            await foreach (var order in documents)
            {
                // in change feed order for the partition key range
            }
        }));
}
```

`UseStream` is the terminal step; because these are ordinary Benzene middleware pipelines, you can
put correlation, metrics, or exception-handling middleware in front of it on the same builder.

### 3. Add the trigger function

```csharp
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.CosmosDb;
using Microsoft.Azure.Functions.Worker;

public class OrdersChangeFeedFunction
{
    private readonly IAzureFunctionApp _app;

    public OrdersChangeFeedFunction(IAzureFunctionApp app)
    {
        _app = app;
    }

    [Function("orders-change-feed")]
    public Task Run([CosmosDBTrigger(
        databaseName: "shop",
        containerName: "orders",
        Connection = "CosmosDbConnection",
        LeaseContainerName = "leases",
        CreateLeaseContainerIfNotExists = true)] IReadOnlyList<OrderDocument> documents)
    {
        return _app.HandleCosmosDbChanges(documents);
    }
}
```

Bind the parameter as `IReadOnlyList<OrderDocument>` — that is the type
`HandleCosmosDbChanges<TDocument>` dispatches on. Several document types (from different
containers) can be registered side by side; each `UseCosmosDbChangeFeed<TDocument>` call is its
own entry point.

### 4. Windowing and aggregation

Because the batch arrives as one stream, handlers can aggregate across it instead of processing
document-by-document — for example, collapsing multiple updates to the same order into one
downstream write:

```csharp
app.UseCosmosDbChangeFeed<OrderDocument>(feed => feed
    .UseStream<OrderDocument>(async (documents, ct) =>
    {
        var latestByOrder = new Dictionary<string, OrderDocument>();
        await foreach (var order in documents)
        {
            latestByOrder[order.id] = order;   // later changes overwrite earlier ones
        }

        foreach (var order in latestByOrder.Values)
        {
            await projection.UpsertAsync(order, ct);
        }
    }));
```

This works *because* the feed guarantees you see changes to a given document in order (within its
partition key range) — the last one wins.

## Where Benzene's responsibility ends

Be clear-eyed about the split:

| Concern | Owned by |
|---|---|
| Batch size, polling interval, lease container, start position | The Functions Cosmos DB extension (`host.json` / trigger attribute) — zero Benzene involvement |
| Lease checkpointing | The trigger: it advances the lease automatically when the invocation returns successfully |
| Ordering within the batch | Cosmos (per partition key range), preserved by Benzene's stream |
| What happens on handler failure | Benzene lets the exception propagate; the trigger does **not** advance the lease and redelivers the whole batch |
| Deletes | Not delivered at all in the standard (latest-version) change feed mode — only creates and updates. Model deletes as soft-delete flags if you need to observe them |

There is no dead-letter concept and no per-document retry: a document that reliably throws will
poison its whole batch into endless redelivery. Catch and route irrecoverable documents yourself
(e.g. write them to a quarantine container) rather than letting the exception escape — the same
honest advice as the [Event Hubs cookbook](event-hub-processing.md), because the constraint is the
platform's, not Benzene's.

The `StreamContext<TDocument>.Checkpointer` is the no-op default (`NullStreamCheckpointer`) on
this transport — calling it is harmless but does nothing, because the Functions trigger exposes no
manual checkpoint API. Manual per-batch checkpoint control is the domain of the planned
self-hosted change feed worker (`Microsoft.Azure.Cosmos` Change Feed Processor), which doesn't
exist yet — see `work/azure-roadmap-1.0.md`.

## Idempotency is non-negotiable

Two platform behaviors make redelivery and duplication a matter of *when*, not *if*:

- A failed invocation redelivers the **whole batch**, including documents you'd already processed.
- The change feed is itself at-least-once per lease ownership change.

Design every downstream write as an upsert keyed on the document's identity (plus `_ts` or an
ETag if you need to reject stale replays). If your processing has side effects that can't be made
naturally idempotent, put [Benzene.Idempotency](idempotency.md) or an equivalent de-duplication
check in front of them.

## Testing

The pipeline is testable without any Cosmos emulator — build the app inline and hand it a list,
exactly as `test/Benzene.Core.Test/Azure/CosmosDbChangeFeedPipelineTest.cs` does:

```csharp
var app = new InlineAzureFunctionStartUp()
    .ConfigureServices(services => services.AddSingleton(projection))
    .Configure(app => app
        .UseCosmosDbChangeFeed<OrderDocument>(feed => feed
            .UseStream<OrderDocument>(HandleOrders)))
    .Build();

await app.HandleCosmosDbChanges<OrderDocument>(new[]
{
    new OrderDocument { id = "order-1", Status = "paid" }
});
```

## See also

- [Azure Functions Setup](../azure-functions.md) — the getting-started guide, including the
  Cosmos DB trigger subsection
- [Event Hub Stream Processing](event-hub-processing.md) — the other Azure fan-in stream
- [Idempotency](idempotency.md) — de-duplication middleware for non-idempotent side effects
