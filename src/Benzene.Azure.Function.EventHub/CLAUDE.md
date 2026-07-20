# Benzene.Azure.Function.EventHub

## What this package does
The **Event Hubs trigger** adapter for Benzene's Azure Functions isolated-worker host. It runs a
triggered batch of `EventData` through the middleware pipeline. This is *consumption* only — it is
the Functions-trigger counterpart of the self-hosted `Benzene.Azure.EventHub` worker (which owns
its own `EventProcessorClient`); there is no producer here. For consuming Event Hubs in a
long-running process instead of an Azure Function, use `Benzene.Azure.EventHub`.

## Two dispatch shapes (both under `Benzene.Azure.Function.EventHub.Function`)
1. **Fan-out (default), `UseEventHub(...)`** — `EventHubApplication` maps the batch to one
   `EventHubContext` per event via `MiddlewareMultiApplication` and processes them, transport-tagged
   `"event-hub"`. Inside it, `UseBenzeneMessage(...)` routes an event whose body deserializes into a
   **Benzene message envelope** (`{"topic","headers","body"}`) to the direct-message pipeline via
   `BenzeneMessageEventHubHandler` (a `MiddlewareRouter` that defers a non-envelope body to the next
   middleware rather than failing). This is the routing path the getting-started guide shows.
2. **Fan-in (streaming), `UseEventHubStream(...)`** — `StreamingExtensions` presents the whole
   batch as one `StreamContext<EventData>` (from `Benzene.Core.Middleware`'s streaming engine), for
   windowing/aggregation over the batch. Batch-level only: the Functions Event Hub trigger
   checkpoints the whole batch on successful return, so there's no per-event checkpoint control
   here (the self-hosted `Benzene.Azure.EventHub` worker is where `CheckpointInterval` lives). This
   is the Azure sibling of AWS's `Benzene.Aws.Lambda.Kinesis`.

## Key types
- `EventHubContext` — wraps a single `Azure.Messaging.EventHubs.EventData` (created via
  `CreateInstance`); transport shape only.
- `EventHubApplication : EntryPointMiddlewareApplication<EventData[]>` — the fan-out entry point.
- `BenzeneMessageEventHubHandler` — the envelope router used by `UseBenzeneMessage`.
- `EventHubMessageHeadersGetter : IMessageHeadersGetter<EventHubContext>` — reads string-typed
  `EventData.Properties` back as headers (same shape `EventHubContextConverter`/
  `OutboundEventHubContextConverter` in `Benzene.Clients.Azure.EventHub` write: `eventData
  .Properties[header.Key] = header.Value`, plus a `"topic"` property this getter doesn't filter
  out - mirrors `Benzene.Azure.EventHub`'s `EventHubConsumerMessageHeadersGetter`). Registered by
  `AddAzureEventHub()` (called automatically by `UseEventHub(...)`). This is the only first-class
  mapper this package registers for `EventHubContext` itself (see "Important conventions" below for
  why there's no topic/body getter here) - it exists so `.UseW3CTraceContext<EventHubContext>()`
  works: a `traceparent`/`tracestate` property set by an upstream producer round-trips through the
  trigger and becomes the pipeline's root `Activity`'s parent. Covered by `EventHubGettersTest.cs`
  (string-typed filtering, `traceparent` round-trip) and `EventHubW3CTraceContextTest.cs`
  (end-to-end trace continuation, in `test/Benzene.Core.Test/Diagnostics/`). Before this getter
  existed, `.UseW3CTraceContext<EventHubContext>()` compiled but silently never found any headers -
  `IMessageHeadersGetter<EventHubContext>` had zero DI registrations, so `traceparent` extraction was
  always a no-op.
- `Extensions.HandleEventHub(this IAzureFunctionApp, params EventData[])` — the dispatch helper the
  `[EventHubTrigger]` function calls. `DependencyInjectionExtensions.UseEventHub(...)` and
  `StreamingExtensions.UseEventHubStream(...)` are the two wiring extensions (each on both
  `IAzureFunctionAppBuilder` and the platform-neutral `IBenzeneApplicationBuilder`).
- **invocationId (release plan Tier 3.5).** `UseEventHub(...)` (the fan-out path) now auto-wires
  `Function/BenzeneInvocationExtensions.cs`'s `UseBenzeneInvocation()` as the first middleware, so
  `IBenzeneInvocation` resolves inside each event's dispatch (`InvocationId` = the event's
  service-assigned `SequenceNumber`) - each event in the trigger's batch is dispatched through its
  own DI scope via `MiddlewareMultiApplication`'s per-event `CreateScope()`, disconnected from
  whatever `IBenzeneInvocation` the outer Azure Functions invocation populated. No application code
  changes needed.

## When to use this package
- Consuming an Event Hub via an Azure Functions Event Hub trigger. Add
  `Microsoft.Azure.Functions.Worker.Extensions.EventHubs` (6.5.0) directly to your function app so
  the trigger registers (see `docs/azure-functions.md` → Event Hubs, and the partitioning/
  checkpointing cookbook `docs/cookbooks/event-hub-processing.md`).

## Dependencies on other Benzene packages
- **Benzene.Azure.Function.Core** — the host/app builder.
- **Benzene.Core.MessageHandlers** — routing, `BenzeneMessageApplication`, and (transitively via
  `Benzene.Core.Middleware`) the `StreamContext`/streaming engine used by `UseEventHubStream`.
- **Azure.Messaging.EventHubs.Processor** — the `EventData` type.

## Important conventions
- Consumption-side only; no producer. Envelope routing (`UseBenzeneMessage`) mirrors the Queue
  Storage adapter; the Kafka adapter has no envelope bridge (its records route by native topic).
- **Bounded batch fan-out**: `UseEventHub(action, maxDegreeOfParallelism)` (both builder overloads,
  and the `EventHubApplication` constructor) optionally caps how many events from a batch run
  concurrently; `null` (the default) leaves the fan-out unbounded - the original behavior. Threaded
  into the `MiddlewareMultiApplication`, which routes it through `Benzene.Core.Middleware`'s
  `BoundedFanOut`. The fan-in `UseEventHubStream(...)` path processes the batch as one unit, so it
  has nothing to bound.
- No first-class topic/body mappers on `EventHubContext` itself (unlike `Benzene.Azure.EventHub`'s
  worker-mode `EventHubConsumerContext`, which has a full mapper set so `.UseMessageHandlers()`
  works directly) - this package routes by deserializing the event body into a Benzene message
  envelope via `UseBenzeneMessage` instead. `EventHubMessageHeadersGetter` is the one exception:
  headers are useful independent of routing (W3C trace context, correlation), so it's registered on
  its own by `AddAzureEventHub()`.
- Coverage: `EventHubPipelineTest.cs` (fan-out + envelope routing), `EventHubGettersTest.cs`
  (`EventHubMessageHeadersGetter`), `EventHubW3CTraceContextTest.cs` (in
  `test/Benzene.Core.Test/Diagnostics/` - `.UseW3CTraceContext<EventHubContext>()` end to end).
  Streaming shares the engine tests in `Core/Middleware/Streaming/`.
