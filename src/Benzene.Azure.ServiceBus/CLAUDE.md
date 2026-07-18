# Benzene.Azure.ServiceBus

## What this package does
Standalone (non-Azure-Functions) Azure Service Bus consumer for Benzene: a self-hosted worker
(`BenzeneServiceBusWorker`) that consumes a queue or topic subscription directly via the SDK's
`ServiceBusProcessor` and dispatches each message through a Benzene middleware pipeline. This is
the Service Bus counterpart of `Benzene.Aws.Sqs`'s `Consumer/` (SQS polled outside Lambda) and
`Benzene.Kafka.Core`'s `BenzeneKafkaWorker` (Kafka consumed outside any trigger) - one of the
"self-hosted worker" startup modes documented in `docs/hosting.md`, where Benzene owns the
process. For Service Bus messages delivered by an Azure Functions trigger, use
`Benzene.Azure.Function.ServiceBus` instead.

## ⚠️ Unsafe by default: a handler failure result is silently completed, not retried
`BenzeneServiceBusConfig.AckMode` defaults to `ServiceBusConsumerAckMode.AutoComplete` — a handler
that returns a failure result (e.g. `BenzeneResult.ServiceUnavailable(...)`) without throwing still
gets its message **completed** (removed from the queue/subscription); only a thrown exception is
abandoned for redelivery. Set `AckMode = ServiceBusConsumerAckMode.Explicit` to have a failed
`IMessageResult` abandon the message too (redelivery subject to the entity's own lock
duration/max-delivery-count/dead-letter settings) — see `ServiceBusConsumerAckMode`'s doc comments
below. This means Service Bus may redeliver the same message, so the handler needs to be
idempotent — see [Capability Matrix](../../docs/capability-matrix.md) /
[Idempotency](../../docs/cookbooks/idempotency.md).

## Key types/interfaces
- `BenzeneServiceBusWorker : IBenzeneWorker` - creates a `ServiceBusProcessor` from
  `IServiceBusClientFactory` + `BenzeneServiceBusConfig` and starts it. Unlike the SQS/Kafka
  workers there is no hand-rolled poll loop or `BoundedConcurrentDispatcher`: the processor itself
  owns receiving, message-lock renewal, and bounded concurrency (`MaxConcurrentCalls`), and pushes
  messages to the worker's handler. `StartAsync` starts the processor and returns (correct
  `IHostedService` semantics, like `BenzeneKafkaWorker`); `StopAsync` calls
  `StopProcessingAsync` (which waits for in-flight handlers), then disposes the processor and
  client. Receive-side errors surface via the processor's error handler and are logged (scope per
  error, like `SqsConsumer`) without ending the worker.
- `BenzeneServiceBusConfig` - `QueueName` XOR (`TopicName` + `SubscriptionName`), validated at
  `StartAsync` (throws `InvalidOperationException` otherwise - unit-tested without a live bus in
  `test/Benzene.Core.Test/Azure/ServiceBusWorker/BenzeneServiceBusWorkerTest.cs`);
  `MaxConcurrentCalls` (default 5, matching `BenzeneKafkaConfig.ConcurrentRequests`);
  `PrefetchCount` (default 0, the SDK default); `AckMode`.
- `ServiceBusConsumerAckMode` - `AutoComplete` (default): the processor completes a message when
  the handler returns and abandons it when the handler throws; a non-exception failure result
  still completes. `Explicit`: the worker turns the processor's auto-complete off and settles each
  message itself from the handler's outcome - abandoned on a thrown exception **or** an
  unsuccessful `IMessageResult`, completed otherwise. Mirrors
  `Benzene.Azure.Function.ServiceBus.ServiceBusAckMode`, minus the trigger configuration that
  package needs (here the worker owns the processor).
- `ServiceBusConsumerContext` - pipeline context wrapping one `ServiceBusReceivedMessage`
  (context purity: transport shape only), plus `MessageResult` recorded by
  `ServiceBusConsumerMessageHandlerResultSetter` and read by the worker for `Explicit` ack.
- `ServiceBusConsumerApplication` - `MiddlewareApplication<ServiceBusReceivedMessage,
  ServiceBusConsumerContext, IMessageResult?>` wrapping the pipeline in
  `TransportMiddlewarePipeline("service-bus")`; one DI scope per message via the base class.
- Mappers (`ServiceBusConsumerMessage{TopicGetter,HeadersGetter,BodyGetter}`) - same conventions
  as `Benzene.Azure.Function.ServiceBus`: topic from the `"topic"` application property (wrapped
  in `PresetTopicMessageTopicGetter`/`PresetTopicHolder` for per-pipeline presets), headers from
  string-typed application properties, body as string.
- **`AddServiceBusConsumer` must register, per context type, everything `.UseMessageHandlers()`
  resolves** - besides the four getters above, that means `IMessageVersionGetter<ServiceBusConsumerContext>`
  (`HeaderMessageVersionGetter`), `AddMediaFormatNegotiation<ServiceBusConsumerContext>()`, and
  `IRequestMapper<ServiceBusConsumerContext>` (`MultiSerializerOptionsRequestMapper`). None of
  these has an open-generic default (only `BenzeneMessageContext` is pre-registered by
  `AddBenzeneMessage`), so omitting them makes the message-handler router throw at resolve time -
  which, since the worker swallows handler faults (AutoComplete logs via the processor's error
  handler), surfaces only as messages never being handled. This mirrors `AddSqsConsumer` exactly.
  Because the mapper-level unit tests mock `IMiddlewarePipeline`, the registration completeness is
  covered separately by `ServiceBusConsumerRealPipelineTest` (real DI + real `.UseMessageHandlers()`
  routing, no emulator).
- `IServiceBusClientFactory` / `ServiceBusClientFactory` - like `ISqsClientFactory`: the caller
  builds the `ServiceBusClient` (connection string, Managed Identity, emulator...), the worker
  disposes it on stop.
- `Extensions.UseServiceBus(IBenzeneWorkerStartup, config, clientFactory, action)` - the
  `IBenzeneWorkerStartup` wiring, mirroring `UseSqs`/`UseKafka`; registers
  `AddBenzeneMessage().AddServiceBusConsumer()` and adds the worker.

## When to use this package
- Consuming Service Bus from a long-running process (console app, container, AKS, App Service
  WebJob-style host) instead of an Azure Function
- Via `Benzene.HostedService`'s generic-host integration or `Benzene.SelfHost`'s
  `InlineSelfHostedStartUp`

## Dependencies on other Benzene packages
- **Benzene.Core** / **Benzene.Core.MessageHandlers** - pipeline + message handler infrastructure
- **Benzene.SelfHost** - `IBenzeneWorkerStartup` (and `IBenzeneWorker` via `Benzene.Abstractions`)
- **Azure.Messaging.ServiceBus** - Service Bus SDK (same pinned version as
  `Benzene.Azure.Function.ServiceBus`)

## Important conventions
- No Azure Functions dependency - deliberately does not reference
  `Microsoft.Azure.Functions.Worker.*`
- Settlement semantics follow the entity's own lock duration / max delivery count / dead-letter
  configuration; abandoning just releases the message for redelivery
- Test coverage: mappers, application, ack-mode settlement decisions, and config validation are
  unit-tested in `test/Benzene.Core.Test/Azure/ServiceBusWorker/` (using
  `ServiceBusModelFactory`-built messages, no live bus); `ServiceBusConsumerRealPipelineTest` there
  additionally drives the real DI + `.UseMessageHandlers()` routing (no emulator) so a missing
  registration can't slip past the mocked-pipeline tests; the worker's real
  processor-consume-dispatch path is covered end to end against the Service Bus emulator in
  `test/Benzene.Integration.Test/ServiceBus/BenzeneServiceBusWorkerLiveTest.cs` (own queue,
  `benzene-worker-queue`, so it doesn't compete with the trigger-pipeline test's queue)
