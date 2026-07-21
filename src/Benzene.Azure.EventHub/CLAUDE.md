# Benzene.Azure.EventHub

## What this package does
Standalone (non-Azure-Functions) Azure Event Hubs consumer for Benzene: a self-hosted worker
(`BenzeneEventHubWorker`) that consumes a hub directly via the SDK's `EventProcessorClient`
(consumer groups, partition load balancing, blob-checkpointed offsets) and dispatches each event
through a Benzene middleware pipeline. This is the Event Hubs counterpart of
`Benzene.Kafka.Core`'s `BenzeneKafkaWorker` - one of the "self-hosted worker" startup modes
documented in `docs/hosting.md`, where Benzene owns the process. For events delivered by an Azure
Functions trigger, use `Benzene.Azure.Function.EventHub` instead.

## A handler failure result stops the partition checkpointing past it by default (`RaiseOnFailureStatus`)
**`BenzeneEventHubConfig.RaiseOnFailureStatus` defaults to `true`** (flipped 2026-07-21 — see
`work/settlement-contract-1.0.md`). A handler that returns a failure result (e.g.
`BenzeneResult.ServiceUnavailable(...)`) without throwing is escalated into a thrown
`EventHubMessageProcessingException`, which takes the same not-checkpointed path as an unhandled
exception (see `CatchHandlerExceptions` below): the failed event isn't checkpointed, so the partition
doesn't advance past it and a restart redelivers it. Since Event Hubs is checkpoint-based with no
per-event abandon, the semantics are "don't checkpoint, reprocess from here" — the handler must be
idempotent. Set `RaiseOnFailureStatus = false` for at-most-once (a failure result is checkpointed like
a success once `CheckpointInterval` is reached; `EventHubConsumerContext.MessageResult` is then only
recorded for diagnostics/middleware). Mirrors the escalation on the Function triggers
(`KafkaOptions.RaiseOnFailureStatus` etc.).

## Key types/interfaces
- `BenzeneEventHubWorker : IBenzeneWorker` - wires `ProcessEventAsync`/`ProcessErrorAsync` on an
  `EventProcessorClient` from `IEventProcessorClientFactory` and starts it. No hand-rolled poll
  loop or `BoundedConcurrentDispatcher`: the processor owns partition ownership, load balancing
  across worker instances (via its blob checkpoint store), and per-partition sequential dispatch
  (partitions run concurrently; one event at a time within a partition - the same ordering promise
  as `BenzeneKafkaConfig.PreserveOrderPerPartition = true`). `StartAsync` starts the processor and
  returns (correct `IHostedService` semantics); `StopAsync` calls `StopProcessingAsync`, which
  waits for in-flight handlers. Receive-side errors surface via `ProcessErrorAsync` and are logged
  (scope per error) without ending the worker.
- **Checkpointing** - per partition, every `BenzeneEventHubConfig.CheckpointInterval` (default 1)
  successfully handled events, via `args.UpdateCheckpointAsync()`. A failed event is never itself
  checkpointed. The per-partition counter is race-free because the processor invokes the handler
  one event at a time per partition (`ConcurrentDictionary` only for cross-partition access).
- `BenzeneEventHubConfig.DefaultStartingPosition` (`EventPosition?`, default `null`) - where a
  partition with **no stored checkpoint** starts reading. `null` leaves the SDK default
  (`EventPosition.Latest` - only events enqueued after the processor claims the partition);
  set `EventPosition.Earliest` to process the retained backlog on first run. A checkpointed
  partition always resumes from its checkpoint regardless. Wired via the processor's
  `PartitionInitializingAsync` event only when set. Kafka analog: `ConsumerConfig.AutoOffsetReset`.
  (This is why a send-before-start event is invisible by default - the live test sets `Earliest`,
  matching the Kafka worker test's `AutoOffsetReset.Earliest`.)
- `BenzeneEventHubConfig.CatchHandlerExceptions` - default `true`: a handler exception is logged
  and the partition keeps going; since Event Hubs is a stream with no per-event retry/dead-letter,
  the failed event is effectively skipped once a later event checkpoints past it (the same
  trade-off as the Functions trigger's checkpoint-advances-regardless behavior, and Kafka's
  default). Set `false` for at-least-once: the worker stops on the first unhandled handler
  exception *without* checkpointing it (initiated via a background `Task.Run` -
  `StopProcessingAsync` must not be called from inside the handler, it would deadlock; guarded by
  an `Interlocked` flag so a concurrent host `StopAsync` is safe), so a restart resumes from the
  last checkpoint and redelivers the failed event. There is no equivalent of Kafka's
  `CommitOnlyOnSuccess` startup-validation matrix here because per-partition dispatch is always
  sequential in `EventProcessorClient` - the failure mode that validation guards against
  (a later event checkpointing past an earlier in-flight one) can't happen.
- `EventHubConsumerContext` - pipeline context wrapping one `EventData` (context purity: transport
  shape only) plus `MessageResult`. Event Hubs has no per-event settlement, so an unsuccessful
  result is recorded for middleware/diagnostics only - it doesn't affect checkpointing.
- `EventHubConsumerApplication` - `MiddlewareApplication<EventData, EventHubConsumerContext,
  IBenzeneResult?>` wrapping the pipeline in `TransportMiddlewarePipeline("event-hub")`; one DI
  scope per event via the base class.
- Mappers (`EventHubConsumerMessage{TopicGetter,HeadersGetter,BodyGetter}`) - topic from the
  event's `"topic"` property (wrapped in `PresetTopicMessageTopicGetter`/`PresetTopicHolder`),
  headers from string-typed properties, body as string. The topic property key is a configurable
  default, not hard-coded: `BenzeneEventHubConfig.TopicPropertyKey` (threaded into
  `AddEventHubConsumer(topicPropertyKey)`) — keep it in sync with the producer's key.
- **`AddEventHubConsumer` must register, per context type, everything `.UseMessageHandlers()`
  resolves** - besides the four getters above, that means `IMessageVersionGetter<EventHubConsumerContext>`
  (`HeaderMessageVersionGetter`), `AddMediaFormatNegotiation<EventHubConsumerContext>()`, and
  `IRequestMapper<EventHubConsumerContext>` (`MultiSerializerOptionsRequestMapper`). None has an
  open-generic default, so omitting them makes the router throw at resolve time - and because the
  worker catches handler faults (`CatchHandlerExceptions`, default `true`) that surfaces only as
  events never being handled. Mirrors `AddSqsConsumer`. Covered by `EventHubConsumerRealPipelineTest`
  (real DI + `.UseMessageHandlers()`, no emulator) since the mapper unit tests mock the pipeline. Note this differs from
  `Benzene.Azure.Function.EventHub`, which has no mappers of its own and instead routes
  BenzeneMessage-envelope bodies via `UseBenzeneMessage` - this package follows the worker-mode
  convention (`Benzene.Kafka.Core`, `Benzene.Aws.Sqs`) of full first-class mappers, so
  `UseMessageHandlers()` works directly.
- `IEventProcessorClientFactory` / `EventProcessorClientFactory` - the caller builds the
  `EventProcessorClient` (hub, consumer group, blob checkpoint container, connection string vs
  Managed Identity), the worker only runs it. The blob container must already exist -
  `EventProcessorClient` does not create it.
- `Extensions.UseEventHub(IBenzeneWorkerStartup, config, clientFactory, action)` - the
  `IBenzeneWorkerStartup` wiring, mirroring `UseKafka`/`UseSqs`/`UseServiceBus`; registers
  `AddBenzeneMessage().AddEventHubConsumer()` and adds the worker.
- **W3C trace context and invocationId (release plan Tier 3.5).** `.UseW3CTraceContext<EventHubConsumerContext>()`
  works: `EventHubConsumerMessageHeadersGetter` already read real string-typed `EventData.Properties`.
  Separately, `UseEventHub(...)` now auto-wires `UseBenzeneInvocation()`
  (`BenzeneInvocationExtensions.cs`) as the first middleware, so `IBenzeneInvocation` resolves
  inside each event's dispatch (`InvocationId` = the event's service-assigned `SequenceNumber`,
  `Platform` = `"Worker"`) - a long-running worker has no outer invocation boundary at all, so this
  is the only invocation identity available here. No application code changes needed for either fix.

## When to use this package
- Consuming Event Hubs from a long-running process (console app, container, AKS) instead of an
  Azure Function
- Via `Benzene.HostedService`'s generic-host integration or `Benzene.SelfHost`'s
  `InlineSelfHostedStartUp`
- If the hub is being consumed over the Kafka protocol instead, `Benzene.Kafka.Core`'s worker
  already covers that - this package is for the native AMQP/Event Hubs SDK path

## Dependencies on other Benzene packages
- **Benzene.Core** / **Benzene.Core.MessageHandlers** - pipeline + message handler infrastructure
- **Benzene.SelfHost** - `IBenzeneWorkerStartup` (and `IBenzeneWorker` via `Benzene.Abstractions`)
- **Azure.Messaging.EventHubs.Processor** - `EventProcessorClient` (same pinned version as
  `Benzene.Azure.Function.EventHub`)

## Important conventions
- No Azure Functions dependency - deliberately does not reference
  `Microsoft.Azure.Functions.Worker.*`
- Test coverage: mappers and the application are unit-tested in
  `test/Benzene.Core.Test/Azure/EventHubWorker/` (hand-built `EventData`, no live hub);
  `EventHubConsumerRealPipelineTest` there additionally drives the real DI +
  `.UseMessageHandlers()` routing (no emulator) so a missing registration can't slip past the
  mocked-pipeline tests; the worker's real processor-consume-checkpoint path is covered end to end
  against the Event Hubs emulator + azurite checkpoint store in
  `test/Benzene.Integration.Test/EventHub/BenzeneEventHubWorkerLiveTest.cs` (own entity, `eh2`,
  so it doesn't cross-read the trigger-pipeline test's events on `eh1`)
