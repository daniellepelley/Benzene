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

## Settlement default: a handler failure result abandons for redelivery (safe by default)
`BenzeneServiceBusConfig.AckMode` defaults to `ServiceBusConsumerAckMode.Explicit` — a handler that
returns a failure result (e.g. `BenzeneResult.ServiceUnavailable(...)`) **or** throws abandons the
message for redelivery (subject to the entity's own lock duration/max-delivery-count/dead-letter
settings), rather than silently completing it. This is a behavioral change from earlier versions,
which defaulted to `AutoComplete` (a returned failure was silently completed — only a throw
abandoned). Set `AckMode = ServiceBusConsumerAckMode.AutoComplete` to hand settlement back to the
processor if you specifically want a non-exception failure result to complete the message. Because a
failure now redelivers, the handler needs to be idempotent — see
[Capability Matrix](../../docs/capability-matrix.md) /
[Idempotency](../../docs/cookbooks/idempotency.md).

**Explicit settlement override (`ServiceBusSettlementHolder`).** Beyond the outcome-based default, a
handler can request a specific settlement by resolving the scoped `ServiceBusSettlementHolder` (a
"scoped DI state, not context" holder like `PresetTopicHolder`, registered by `AddServiceBusConsumer`)
and setting `Override` to `ServiceBusSettlement.{Complete,Abandon,DeadLetter,Defer}` (plus
`DeadLetterReason`/`DeadLetterDescription`). The worker applies it in `Explicit` mode: `DeadLetter` →
`DeadLetterMessageAsync(reason, description)` (quarantines a poison message instead of abandon-looping
to max-delivery-count), `Defer` → `DeferMessageAsync` (receiving deferred messages back by sequence
number is the caller's own advanced path), else Complete/Abandon. The application surfaces the holder
to the worker via `ServiceBusSettlementDecision` (it owns its DI scope so it can read the holder the
handler mutated). Only honored in `Explicit` mode — in `AutoComplete` the processor settles itself.
Additive/opt-in; a handler that never sets the holder keeps the outcome-based default.

## Session support (FIFO per session) — `BenzeneServiceBusConfig.SessionsEnabled`
Set `SessionsEnabled = true` to consume a **session-enabled** entity via a
`ServiceBusSessionProcessor` instead of a `ServiceBusProcessor`: each session is locked to one
handler and its messages are delivered in strict FIFO order; different sessions run concurrently
(`MaxConcurrentSessions`, default 8), one message at a time within a session
(`MaxConcurrentCallsPerSession`, default 1 — the ordering-preserving setting). The entity must be
created session-enabled, and producers must set a `SessionId` (see the client's
`ServiceBusSenderProperties.SessionIdHeader`). Settlement (including the explicit-override path below)
and `AckMode` behave identically to the non-session path — the worker settles both processor kinds
through the shared internal `IServiceBusMessageSettler` adapter over `ProcessMessageEventArgs` /
`ProcessSessionMessageEventArgs`. Default off (purely additive).

## Key types/interfaces
- `BenzeneServiceBusWorker : IBenzeneWorker` - creates a `ServiceBusProcessor` (or, when
  `SessionsEnabled`, a `ServiceBusSessionProcessor`) from
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
  `PrefetchCount` (default 0, the SDK default); `AckMode`; `MaxAutoLockRenewalDuration` (`TimeSpan?`,
  default `null` = SDK default of 5 min) — plumbed straight to
  `ServiceBusProcessorOptions.MaxAutoLockRenewalDuration` so a long-running handler's message lock is
  renewed past the entity's lock duration instead of being redelivered mid-processing.
- `ServiceBusConsumerAckMode` - `Explicit` (**default**, see the "Settlement default" section above):
  the worker turns the processor's auto-complete off and settles each message itself from the
  handler's outcome - abandoned on a thrown exception **or** a non-exception failure result.
  `AutoComplete` (opt-in): the processor completes a message when the handler returns and abandons it
  when the handler throws; a non-exception failure result still completes. Mirrors
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
  string-typed application properties, body as string. The topic property key is a configurable
  default, not hard-coded: `BenzeneServiceBusConfig.TopicPropertyKey` (threaded into
  `AddServiceBusConsumer(topicPropertyKey)`) — keep it in sync with the producer's key.
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
