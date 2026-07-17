# Benzene.Azure.EventHub

## What this package does
Standalone (non-Azure-Functions) Azure Event Hubs consumer for Benzene: a self-hosted worker
(`BenzeneEventHubWorker`) that consumes a hub directly via the SDK's `EventProcessorClient`
(consumer groups, partition load balancing, blob-checkpointed offsets) and dispatches each event
through a Benzene middleware pipeline. This is the Event Hubs counterpart of
`Benzene.Kafka.Core`'s `BenzeneKafkaWorker` - one of the "self-hosted worker" startup modes
documented in `docs/hosting.md`, where Benzene owns the process. For events delivered by an Azure
Functions trigger, use `Benzene.Azure.Function.EventHub` instead.

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
  IMessageResult?>` wrapping the pipeline in `TransportMiddlewarePipeline("event-hub")`; one DI
  scope per event via the base class.
- Mappers (`EventHubConsumerMessage{TopicGetter,HeadersGetter,BodyGetter}`) - topic from the
  event's `"topic"` property (wrapped in `PresetTopicMessageTopicGetter`/`PresetTopicHolder`),
  headers from string-typed properties, body as string. Note this differs from
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
  `test/Benzene.Core.Test/Azure/EventHubWorker/` (hand-built `EventData`, no live hub); the
  worker's real processor-consume-checkpoint path is covered end to end against the Event Hubs
  emulator + azurite checkpoint store in
  `test/Benzene.Integration.Test/EventHub/BenzeneEventHubWorkerLiveTest.cs` (own entity, `eh2`,
  so it doesn't cross-read the trigger-pipeline test's events on `eh1`)
