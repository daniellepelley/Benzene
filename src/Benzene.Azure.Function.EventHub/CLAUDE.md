# Benzene.Azure.Function.EventHub

## What this package does
The **Event Hubs trigger** adapter for Benzene's Azure Functions isolated-worker host. It runs a
triggered batch of `EventData` through the middleware pipeline. This is *consumption* only — it is
the Functions-trigger counterpart of the self-hosted `Benzene.Azure.EventHub` worker (which owns
its own `EventProcessorClient`); there is no producer here. For consuming Event Hubs in a
long-running process instead of an Azure Function, use `Benzene.Azure.EventHub`.

## Failure handling: safe-by-default failure escalation, opt-in per-event isolation (`EventHubOptions`)
By default the fan-out is all-or-nothing: every event in the triggered batch runs concurrently, but
if any handler **throws**, the exception cascades, fails the whole Functions invocation, and the
Event Hubs trigger re-delivers the **entire** batch — so every already-succeeded sibling re-runs. A
handler returning a non-exception **failure result** (e.g. `BenzeneResult.ServiceUnavailable(...)`)
is now **escalated** the same way by default (`RaiseOnFailureStatus` defaults to `true`, flipped
2026-07-21 — see `work/settlement-contract-1.0.md`, and the caveat below about the envelope path).
`CatchExceptions` (per-event isolation) stays opt-in (mirroring `Benzene.Azure.Function.EventGrid` /
`Benzene.Azure.Function.QueueStorage` / `Benzene.Azure.Function.Kafka`):

- `EventHubOptions.CatchExceptions = true` (via `UseEventHub(action, configure)`) catches/logs a
  handler exception per event so its siblings still complete and the batch checkpoints — trading the
  all-or-nothing re-delivery for **sibling isolation**. **Ordering tradeoff:** Event Hub records
  within a partition are ordered; catch-and-continue trades that ordering (and the poison event's
  re-delivery) for isolation, the same tradeoff `S3`/`EventGrid` already accept. The poison event is
  **not** retried once caught. Default `false` preserves today's behavior exactly.
- `EventHubOptions.RaiseOnFailureStatus` (default `true`) escalates a non-exception failure result
  into a thrown `EventHubMessageProcessingException`, so the Event Hubs trigger re-delivers the batch
  the same way it would for an exception (at-least-once; the handler must then be idempotent). Set
  `false` for at-most-once. **Caveat:** this reads `EventHubContext.MessageResult` (`EventHubContext : IHasMessageResult`),
  but this package's default routing path (`UseBenzeneMessage`) runs handlers on the inner
  `BenzeneMessageContext` with its response **suppressed** (`SuppressResponse()`), so nothing
  populates `EventHubContext.MessageResult` in that path today — the flag is wired structurally
  (and unit-tested) but only bites when a middleware/result-setter records a result directly on the
  `EventHubContext`. Fully surfacing the inner envelope handler's failure result to the outer context
  would need a new propagation mechanism (the response is suppressed) — deferred, flagged for a
  maintainer decision. This is the same envelope-path limitation `QueueStorage`'s
  `RaiseOnFailureStatus` has; `QueueStorage`/`EventGrid` only work fully because they *also* run
  handlers directly on their own context (preset-topic / by-event-type routing), which this package
  does not expose.

Both flags default off (purely additive, non-breaking). The existing `maxDegreeOfParallelism` knob
is folded into `EventHubOptions.MaxDegreeOfParallelism`; the original `UseEventHub(action,
maxDegreeOfParallelism)` / `EventHubApplication(pipeline, srf, int?)` signatures are unchanged (the
options form is an **additional** overload). The batch fan-out lives in `EventHubBatchApplication`
(which `EventHubApplication` delegates to, like `EventGridApplication` → `EventGridBatchApplication`).
Covered by `test/Benzene.Core.Test/Azure/EventHubFailureHandlingTest.cs`. The fan-in
`UseEventHubStream(...)` path is batch-level and out of scope.

## Two dispatch shapes (both under `Benzene.Azure.Function.EventHub.Function`)
1. **Fan-out (default), `UseEventHub(...)`** — `EventHubApplication` delegates to
   `EventHubBatchApplication`, which maps the batch to one `EventHubContext` per event and runs each
   (in its own DI scope, via `Benzene.Core.Middleware`'s `BoundedFanOut`) with a per-event
   try/catch + failure-escalation governed by `EventHubOptions` (see "Failure handling" above),
   transport-tagged `"event-hub"`. Inside it, `UseBenzeneMessage(...)` routes an event whose body deserializes into a
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
- `EventHubApplication : EntryPointMiddlewareApplication<EventData[]>` — the fan-out entry point;
  delegates the batch fan-out to `EventHubBatchApplication : IMiddlewareApplication<EventData[]>`
  (the per-record try/catch + `RaiseOnFailureStatus` escalation live there), mirroring
  `EventGridApplication` → `EventGridBatchApplication`.
- `EventHubOptions` (`CatchExceptions`, `RaiseOnFailureStatus`, `MaxDegreeOfParallelism`) +
  `EventHubMessageProcessingException` — the failure-handling knobs and the escalation exception (see
  "Failure handling" above). `EventHubContext` implements `IHasMessageResult` to carry the result
  `RaiseOnFailureStatus` reads.
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
  own DI scope via `EventHubBatchApplication`'s per-event `CreateScope()`, disconnected from
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
  concurrently; `null` (the default) leaves the fan-out unbounded - the original behavior. Folded
  into `EventHubOptions.MaxDegreeOfParallelism` and routed through `Benzene.Core.Middleware`'s
  `BoundedFanOut` inside `EventHubBatchApplication`. The fan-in `UseEventHubStream(...)` path
  processes the batch as one unit, so it has nothing to bound.
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
