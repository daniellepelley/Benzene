# Benzene Streaming Plan

## Context

Benzene's core is not message handlers — it's a **middleware pipeline over a context**
(`IMiddlewarePipelineBuilder<TContext>` → `IMiddleware<TContext>` → `IMiddlewarePipeline<TContext>`),
plus a library of composable **solutions to common cloud-runtime problems** (correlation, health
checks, spec, metrics, retry, CORS, and — as *one* solution among these — message-handler routing
via `UseMessageHandlers()`). Streaming should be added the same way the other solutions were: as a
pipeline-level capability, **without changing the `IMessageHandler` contract**.

Today the streaming-capable transports (Event Hub, Kafka, SQS, S3) all **fan out**: the transport's
Application maps one runtime event (a batch) to *N* contexts and runs the pipeline once per item,
concurrently. That behaviour lives entirely at the **Application** layer, not in the pipeline core:

- `EventHubApplication` uses `MiddlewareMultiApplication<EventData[], EventHubContext>`
  (`src/Benzene.Azure.Function.EventHub/Function/EventHubApplication.cs`), whose mapper is
  `@event.Select(EventHubContext.CreateInstance)` — one context per event.
- `MiddlewareMultiApplication.HandleAsync` runs those contexts under `Task.WhenAll`, each in its own
  DI scope (`src/Benzene.Core.Middleware/MiddlewareMultiApplication.cs`).

Fan-out is a *choice*, and it has costs the framework can't currently address from inside the
pipeline (documented honestly in `docs/cookbooks/event-hub-processing.md`):

- **No intra-batch ordering** — even though Event Hubs/Kafka guarantee per-partition order on the wire.
- **No batch-level aggregation** — each item is isolated in its own scope; you can't "write 500 rows in one round-trip" inside a handler.
- **No checkpoint control** — the handler can't say "checkpoint after this window."
- **No backpressure / windowing** — there is no stream to apply them to; the batch is already materialised.

**The streaming solution is to add a fan-*in* Application plus stream middleware:** present the whole
batch/stream to the pipeline as a **single** `StreamContext<TItem>` and let stream-aware middleware
consume it. Message handlers stay exactly as they are — you either don't use them in a streaming
pipeline, or you invoke them from inside a stream step.

## Verified facts this plan relies on

- The pipeline is generic over any context; `UseMessageHandlers()` is just one `.Use(...)` step that
  adds `MessageRouter<TContext>` (`src/Benzene.Core.MessageHandlers/Extensions.cs`). Nothing about
  the pipeline mandates one-message-per-item.
- Single-context Applications already exist and run the pipeline **once** over one context:
  `MiddlewareApplication<TEvent, TContext>` and `MiddlewareApplication<TEvent, TContext, TResult>`
  (`src/Benzene.Core.Middleware/MiddlewareApplication.cs`), used by API Gateway and the custom
  authorizer. The streaming Application is this shape, not the multi/fan-out shape.
- `IMiddleware<in TContext>.HandleAsync(TContext context, Func<Task> next)` carries **no
  `CancellationToken`** (`src/Benzene.Abstractions.Middleware/IMiddleware.cs`). A long-lived stream
  needs cancellation, so it must ride on the **context**, not the pipeline signature. (Same
  conclusion the gRPC plan reached.)
- `MessageRouter<TContext>` resolves the request lazily via
  `IRequestMapper<TContext>.GetBody<TRequest>() where TRequest : class`. **`IAsyncEnumerable<T>`
  satisfies `class`**, so a handler `IMessageHandler<IAsyncEnumerable<TItem>, TResponse>` routes
  through the existing router **without touching core abstractions** (verified in
  `docs/plans/grpc-enhancement-plan.md`). This is the basis for the optional handler bridge (Phase 3).
- `IContextConverter<TIn, TOut>` (`src/Benzene.Abstractions.Middleware/IContextConverter.cs`) is the
  established way to bridge one context type to another inside a pipeline — the mechanism the handler
  bridge uses to go from `StreamContext<TItem>` to the router's context.
- Transports opt a pipeline in via a `Use…` extension that builds a sub-pipeline and registers an
  entry-point Application (`UseEventHub`, `UseSqs`, `UseApiGateway` all follow this shape).

## Design

### 1. `StreamContext<TItem>` — the stream as one context (fan-in)

```csharp
public class StreamContext<TItem> : IHasMessageResult   // reuse the existing result-carrying marker
{
    public IAsyncEnumerable<TItem> Items { get; }        // lazily pulled from the wire/batch
    public CancellationToken CancellationToken { get; }  // cancellation rides here (pipeline has none)
    public IStreamCheckpointer Checkpointer { get; }      // transport-supplied ack/checkpoint hook
    public IDictionary<string, object> Metadata { get; }  // partition id, consumer group, etc.
    public IMessageResult MessageResult { get; set; }     // outcome, for enrichment/metrics/health
}
```

- `Items` is an `IAsyncEnumerable<TItem>` so the pipeline can iterate lazily and apply backpressure —
  the batch is **not** pre-materialised the way `MiddlewareMultiApplication` does it.
- `Checkpointer` is a small transport-supplied callback (`Task CheckpointAsync(TItem lastProcessed)`)
  so a stream step can checkpoint after a window/aggregate succeeds. No-op on transports that
  checkpoint themselves.

### 2. Fan-in Application (the pivotal, Application-layer change)

A `StreamMiddlewareApplication<TEvent, TItem>` implementing `IMiddlewareApplication<TEvent>` that
maps the raw event to **one** `StreamContext<TItem>` (wrapping the batch as an `IAsyncEnumerable`)
and runs the pipeline **once**. This is the single-context `MiddlewareApplication` shape — no
`Task.WhenAll` fan-out. Ordering is preserved because iteration order is the pipeline's to control.

### 3. Stream middleware — the actual "solutions"

New `IMiddleware<StreamContext<TItem>>` steps, composed on the same builder as every other solution:

| Step | Solves |
|---|---|
| `UseStream(Func<IAsyncEnumerable<TItem>, …, Task>)` | Terminal: hand the async stream to your code. |
| `UsePartitionedBy(item => key)` | Per-key **ordered** sub-streams (fixes the ordering gap). |
| `UseWindow(size)` / `UseWindow(TimeSpan)` | Count/time windows for batch aggregation. |
| `UseCheckpointAfterEach()` / `UseCheckpointPerWindow()` | Explicit checkpoint control. |

These are ordinary middleware — they wrap `next()` and transform/observe `context.Items` — so they
compose with the existing solutions (`UseCorrelationId`, `UseBenzeneMetrics`, `UseExceptionHandler`)
with no special-casing.

### 4. Optional: bridge to message handlers (ergonomics, not a requirement)

For teams who still want the handler programming model over a stream, a
`UseStreamHandlers()` step uses an `IContextConverter<StreamContext<TItem>, …>` to route the whole
`IAsyncEnumerable<TItem>` to a handler declared as
`IMessageHandler<IAsyncEnumerable<TItem>, TResponse>` — legal today because of the `: class`
constraint on `MessageRouter`. The handler contract is **unchanged**; a streaming handler is just one
whose request type happens to be an async-enumerable. This is strictly opt-in and layered on top.

### 5. Transports — opt-in, alongside the existing behaviour

New extensions that build a stream pipeline instead of fanning out, leaving today's per-item
behaviour exactly as-is:

- `UseEventHubStream<TItem>(...)` next to `UseEventHub(...)`
- `UseKafkaStream<TItem>(...)`, `UseSqsStream<TItem>(...)`

Each constructs the `StreamMiddlewareApplication` and supplies the `IStreamCheckpointer` appropriate
to that runtime (Event Hubs checkpoint, SQS delete-on-success mapping to batch-item failures, etc.).

## What this fixes vs. the current fan-out

- **Ordering**: single context + caller-controlled iteration (and `UsePartitionedBy`) preserves
  per-partition order.
- **Aggregation**: `UseWindow` gives real batch-level processing (one DB round-trip per window).
- **Checkpointing**: `context.Checkpointer` lets a stream step checkpoint on its own terms.
- **Backpressure**: `IAsyncEnumerable` iteration means the pipeline pulls at its own rate.
- **Cancellation**: `context.CancellationToken` gives long streams a shutdown path the pipeline
  otherwise lacks.

None of this touches `IMessageHandler`, `MessageRouter`, or any existing transport.

## ⚠️ Decisions to confirm before implementation

1. **Where the stream abstractions live.** Recommended: `StreamContext<TItem>`, the middleware, and
   `StreamMiddlewareApplication` go in **`Benzene.Core.Middleware`** (no new package, no `.sln`
   change); per-transport `Use…Stream` extensions go in each existing transport package. Alternative:
   a dedicated `Benzene.Streaming` project — cleaner separation but adds a project to `Benzene.sln`
   (CLAUDE.md requires approval for solution-structure changes).
2. **Cancellation on the context** (not the pipeline signature) — consistent with the gRPC plan;
   confirm this is the accepted approach rather than widening `IMiddleware.HandleAsync`.
3. **Opt-in, not a replacement** — streaming transports ship as *new* `Use…Stream` extensions; the
   existing fan-out `UseEventHub`/`UseSqs` stay unchanged. Confirm we're adding, not migrating.
4. **Checkpointer scope** — how far to go in Phase 1 (a no-op default vs. real Event Hubs/SQS
   checkpoint wiring).

## Phased implementation

- **Phase 1 — core stream abstractions + one transport.** `StreamContext<TItem>`,
  `IStreamCheckpointer` (with a no-op default), `StreamMiddlewareApplication`, `UseStream(...)`, and
  `UseEventHubStream<TItem>(...)`. Unit tests: a batch flows through as one ordered stream; a stream
  step sees all items in order in one scope.
- **Phase 2 — stream solutions.** `UseWindow`, `UsePartitionedBy`, checkpoint steps, and real
  checkpoint wiring for Event Hubs + SQS (SQS mapping windowed failures back to `SQSBatchResponse`).
  Tests per operator.
- **Phase 3 — handler bridge (optional).** `UseStreamHandlers()` + the context converter enabling
  `IMessageHandler<IAsyncEnumerable<TItem>, TResponse>`. Tests: a streaming handler receives the full
  async sequence via the existing router.
- **Phase 4 — remaining transports + docs.** `UseKafkaStream`, and turn the honest limitations
  section of `docs/cookbooks/event-hub-processing.md` into a "streaming vs fan-out: pick one" guide.

## Notes

- This is a **design**; it has not been compiled or tested — no .NET SDK is available in the
  authoring environment (egress blocks the SDK/NuGet), so each phase must be built and CI-verified.
- The design deliberately mirrors how gRPC streaming is approached in
  `docs/plans/grpc-enhancement-plan.md` (streaming via `IAsyncEnumerable` request/response types), so
  the two can share the handler-bridge mechanism rather than inventing two streaming models.
