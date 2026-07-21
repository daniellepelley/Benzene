# Benzene.Azure.Function.EventGrid

## What this package does
Inbound Azure Event Grid adapter for the Azure Functions `EventGridTrigger` binding (isolated
worker): routes delivered events to message handlers **by event type** — the direct Azure
counterpart of `Benzene.Aws.Lambda.S3`/`EventBridge` routing on the event name. Handles both wire
schemas Event Grid can deliver: the Event Grid schema (`eventType`/`topic`) and CloudEvents 1.0
(`type`/`source`, detected by `specversion`).

## Failure handling: a returned failure result is retried by default (safe-by-default)
`EventGridOptions.RaiseOnFailureStatus` defaults to `true` (flipped 2026-07-21 — see
`work/settlement-contract-1.0.md`): if a handler returns a non-exception failure result (e.g.
`BenzeneResult.ServiceUnavailable(...)`), it is escalated into a thrown
`EventGridMessageProcessingException` so the invocation fails and Event Grid's own delivery retry
(backoff, up to 24h) + optional dead-letter destination take over — the same treatment an unhandled
exception already got. Set `EventGridOptions.RaiseOnFailureStatus = false` (via
`UseEventGrid(action, configure)`) for at-most-once (a failure result reports success, no retry).
`EventGridOptions.CatchExceptions` (default `false`) conversely swallows/logs handler exceptions.
Because a returned failure is now retried by default, the handler must be idempotent.

## Zero dependencies — deliberately
References only `Benzene.Azure.Function.Core` + `Benzene.Core.MessageHandlers` — no
`Azure.Messaging.EventGrid`, no Functions extension package; the event payload rides as a BCL
`JsonElement`. The consumer's Function App project references
`Microsoft.Azure.Functions.Worker.Extensions.EventGrid` itself for the attribute, binds the event
as `string`, and calls `HandleEventGridEvent(json)` — `EventGridTriggerEvent.Parse` does the
schema detection/mapping. Do not add SDK packages here without asking first (repo NuGet policy).

## Routing
- **Topic = the event type** (`Microsoft.Storage.BlobCreated`, or your own custom type) via
  `EventGridMessageTopicGetter`, wrapped in `PresetTopicMessageTopicGetter` so
  `UsePresetTopic(...)` can override per pipeline as everywhere else.
- **Body = the event's `data` payload** as raw JSON (`{}` when absent, so empty request types
  bind), deserialized by the standard request mapper into the handler's request type.
- **Headers = the envelope**: `id`, `subject`, `source` (the Event Grid schema's `topic` /
  CloudEvents' `source` — named `Source` on the model to avoid colliding with Benzene's routing
  notion of topic).

```csharp
app.UseEventGrid(eventGrid => eventGrid.UseMessageHandlers());
// [Message("Microsoft.Storage.BlobCreated")] handlers receive the event's data payload
```

## Key types
- `EventGridTriggerEvent` — Benzene's own dependency-free model (`Id`, `EventType`, `Subject`,
  `Source`, `EventTime`, `DataVersion`, `Data` as `JsonElement?`) + `Parse(string)` covering both
  schemas.
- `EventGridContext : IHasMessageResult` — result is diagnostics-only; a thrown exception is what
  drives Event Grid's own retry/dead-letter machinery.
- `EventGridApplication` — `EntryPointMiddlewareApplication<EventGridTriggerEvent[]>`, fan-out,
  transport tag `"event-grid"`; array shape covers batched ("many"-cardinality) triggers and tests.
- `UseEventGrid(action, maxDegreeOfParallelism = null)` (both builders, no-op off-Azure),
  `AddAzureEventGrid()`, `EventGridRegistrations`, `HandleEventGridEvents(params ...)`,
  `HandleEventGridEvent(string)`. `maxDegreeOfParallelism` optionally bounds fan-out concurrency
  (routed through `Benzene.Core.Middleware`'s `BoundedFanOut`); it only bites on a batched
  ("many"-cardinality) trigger - the default one-event-per-invocation delivery has nothing to bound.

## Failure handling
None in-package: a pipeline exception propagates and Event Grid's delivery retry (with backoff, up
to 24h) and optional dead-letter destination take over — configured on the event subscription, not
in code.

## Tests
- `test/Benzene.Core.Test/Azure/EventGridPipelineTest.cs` — end-to-end routing for both schemas,
  `Parse` field mapping for both schemas, headers surface, empty-data body fallback.
