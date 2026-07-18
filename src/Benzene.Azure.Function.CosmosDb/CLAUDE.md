# Benzene.Azure.Function.CosmosDb

## What this package does
Inbound Azure Cosmos DB Change Feed adapter for the Azure Functions `CosmosDBTrigger` binding
(isolated worker): delivers a triggered batch of changed documents to a Benzene **streaming**
pipeline. The whole batch is exposed as one ordered `IAsyncEnumerable<TDocument>` (fan-in) — the
Cosmos counterpart to `Benzene.Azure.Function.EventHub`'s `UseEventHubStream` and AWS's
`Benzene.Aws.Lambda.Kinesis`, built on the streaming engine in `Benzene.Core.Middleware/Streaming`.

## Fan-in (streaming), not fan-out — and generic over the document type
Two deliberate design choices, both flagged in `work/azure-roadmap-1.0.md`'s 2026-07-17 Cosmos DB
Change Feed evaluation:

1. **Fan-in.** Change feed batches are ordered per partition key range and checkpointed (via the
   trigger's lease) as a whole batch — there is no per-document resume token (unlike Kinesis
   sequence numbers). So the batch is handed to the handler intact as one
   `StreamContext<TDocument>`, one pipeline run, one DI scope, rather than fanned out into isolated
   per-document contexts.
2. **Generic over `TDocument`.** Every other Azure adapter binds an opaque transport payload
   (`EventData`, `ServiceBusReceivedMessage`, Kafka key/value) and routes on a message envelope.
   The Cosmos DB trigger has no raw-bytes shape — it delivers already-deserialized POCOs — so the
   entry point, context, and pipeline are all generic over the consumer's document type.

## Zero dependencies — deliberately
This package references only `Benzene.Azure.Function.Core` and `Benzene.Core.Middleware` — **no
Cosmos SDK, no Functions extension package**. The documents arrive as plain POCOs, so no Azure
types are needed here (same dependency-free approach as `Benzene.Aws.Lambda.Kinesis`'s hand-rolled
event model). The *consumer's* Function App project supplies the trigger attribute by referencing
`Microsoft.Azure.Functions.Worker.Extensions.CosmosDB` itself. Do not add either SDK package here
without asking first (repo NuGet policy).

## Key types
- `StreamingExtensions.UseCosmosDbChangeFeed<TDocument>(action)` — registers the entry point:
  builds a `StreamContext<TDocument>` pipeline and wires an
  `EntryPointMiddlewareApplication<IReadOnlyList<TDocument>>` wrapping a
  `StreamMiddlewareApplication<IReadOnlyList<TDocument>, TDocument>`. Overloads exist on both
  `IAzureFunctionAppBuilder` and the platform-neutral `IBenzeneApplicationBuilder` (no-op on
  non-Azure platforms, mirroring `UseEventHubStream`).
- `Extensions.HandleCosmosDbChanges<TDocument>(documents)` — dispatch helper the function method
  calls with the `IReadOnlyList<TDocument>` its `[CosmosDBTrigger]` parameter received.
- No checkpointer type: the trigger checkpoints its lease automatically on successful return, so
  the context carries the default `NullStreamCheckpointer<TDocument>`. An exception from the
  pipeline **propagates** (no catch-and-continue like Kinesis's `ReportBatchItemFailures` shape) —
  the lease stays put and the runtime redelivers the whole batch.

```csharp
app.UseCosmosDbChangeFeed<OrderDocument>(feed => feed
    .UseStream<OrderDocument>(async (documents, ct) =>
    {
        await foreach (var document in documents)
        {
            // documents arrive in change feed order for their partition key range
        }
    }));
```

## When to use this package
- Consuming a Cosmos DB container's change feed in Azure Functions: CDC fan-in, materialized
  views, cache invalidation, event sourcing projections.
- For manual per-batch checkpoint control or non-Functions hosting (AKS/Container Apps), use the
  self-hosted `Benzene.Azure.CosmosDb` worker (`BenzeneCosmosChangeFeedWorker<TDocument>` on
  `Microsoft.Azure.Cosmos`'s Change Feed Processor) — same `StreamContext<TDocument>` pipeline
  shape, real batch-level checkpointer.

## Dependencies on other Benzene packages
- **Benzene.Azure.Function.Core** — `IAzureFunctionAppBuilder`/`IAzureFunctionApp` entry-point
  plumbing.
- **Benzene.Core.Middleware** — `StreamContext<TItem>`, `StreamMiddlewareApplication`,
  `UseStream(...)` and the stream operators.

## Important conventions
- The dispatch match is on `IReadOnlyList<TDocument>` — bind the trigger parameter as
  `IReadOnlyList<TDocument>` (the isolated-worker default for this trigger) and pass it straight
  to `HandleCosmosDbChanges`. Multiple document types can be registered side by side; each is a
  distinct entry point.
- A null batch is treated as empty (pipeline still runs once, sees no items).

## Tests
- `test/Benzene.Core.Test/Azure/CosmosDbChangeFeedPipelineTest.cs` — fan-in single-run/ordering,
  empty and null batches, two document types routing independently, exception propagation, the
  platform-neutral no-op overload, and unregistered-type dispatch failure.

## No egress package — deliberately (release plan §5.2)
There is no `Benzene.Clients.Azure.CosmosDb`. Cosmos DB is a **database**, not a transport — the
change feed is a read-side event stream (hence the ingress above), but writing to Cosmos is
ordinary database access, the same category as any other persistence call. Benzene doesn't get
involved in database access (design philosophy principle 2 — see the
[Capability Matrix](../../docs/capability-matrix.md)); use `CosmosClient`/`Container` directly in
your own handler code.
