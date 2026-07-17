# Benzene.Azure.Function.ServiceBus

## What this package does
Azure Service Bus integration for Benzene's Azure Functions isolated-worker host. Wraps a triggered
Service Bus message (or batch, if the trigger is configured with `IsBatched = true`) in a
`ServiceBusContext` and runs it through the standard message-handler middleware pipeline, so
`[Message("topic")]`-attributed handlers and `.UseMessageHandlers()` topic-based routing work exactly
as they do for HTTP, Event Hubs, and Kafka.

## Key types/interfaces
- `ServiceBusContext` - wraps a single `Azure.Messaging.ServiceBus.ServiceBusReceivedMessage`; a
  plain description of the message only - preset-topic override (see "Important conventions"
  below) is scoped DI state, not a context capability
- `ServiceBusMessageTopicGetter` - reads the topic from the message's `"topic"` application property
- `ServiceBusMessageBodyGetter` - reads the message body as a string (`message.Body.ToString()`)
- `ServiceBusMessageHeadersGetter` - exposes the message's string-typed application properties as headers
- `ServiceBusMessageMessageHandlerResultSetter` - records the outcome onto `MessageResult` (see "Important conventions" below)
- `ServiceBusApplication` / `ServiceBusBatchApplication` - the entry point application invoked by the
  Azure Functions trigger method, and the per-message-loop application it wraps.
  `ServiceBusBatchApplication` implements both `IMiddlewareApplication<ServiceBusReceivedMessage[]>`
  and `IMiddlewareApplication<ServiceBusTriggerBatch>` - see "True per-message ack" below.
- `ServiceBusTriggerBatch` - carries a batch's messages together with the
  `Microsoft.Azure.Functions.Worker.ServiceBusMessageActions` needed to complete/abandon them -
  a distinct request type from `ServiceBusReceivedMessage[]` so `ServiceBusAckMode.Explicit` can be
  dispatched to specifically. Named `...TriggerBatch`, not `...MessageBatch`, to avoid colliding
  with the real `Azure.Messaging.ServiceBus.ServiceBusMessageBatch` SDK type (an outbound-sending
  concept, unrelated to this).
- `ServiceBusOptions` / `ServiceBusMessageProcessingException` - configurable exception/failure-status
  handling (see "Important conventions" below)
- `ServiceBusAckMode` - `AutoComplete` (default, unchanged behavior) vs `Explicit` (true per-message
  complete/abandon control - see "True per-message ack" below)
- `DependencyInjectionExtensions.AddAzureServiceBus()` / `UseServiceBus(...)` - registration and pipeline wiring

## When to use this package
- When consuming messages from an Azure Service Bus queue or topic/subscription via an Azure Functions
  isolated-worker `[ServiceBusTrigger]` method
- When you want the same `[Message("topic")]` handler-routing model already used for HTTP/Event Hubs/Kafka

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Core.MessageHandlers** - Message handler infrastructure, `MessageMessageHandlerResultSetterBase`
- **Benzene.Azure.Function.Core** - Azure Functions isolated-worker host integration
- **Azure.Messaging.ServiceBus** - Service Bus SDK (for `ServiceBusReceivedMessage`)
- **Microsoft.Azure.Functions.Worker.Extensions.ServiceBus** - isolated-worker Service Bus trigger binding

## Important conventions
- **Topic routing**: since Service Bus has no native per-message "topic" field in the Benzene sense (a
  Service Bus topic/subscription is a routing destination configured on the trigger itself, not a
  per-message property), the topic used for handler routing comes from a custom `"topic"` application
  property on the message - set this when sending the message. This mirrors the exact convention used by
  `Benzene.Aws.Sqs`/`Benzene.Aws.Lambda.Sqs`/`Benzene.Aws.Lambda.Sns`.
- **Preset topic override**: if a subscription's producer isn't a Benzene client and never sets a
  `"topic"` application property at all, call `.UsePresetTopic("some-topic")`
  (`Benzene.Core.MessageHandlers`) before `.UseMessageHandlers()` in that subscription's pipeline to
  route every message on it to a fixed topic instead of relying on the property. Carried via scoped
  DI state (`PresetTopicHolder`), not a property on `ServiceBusContext`.
- **True per-message ack** (`ServiceBusOptions.AckMode`): defaults to `ServiceBusAckMode.AutoComplete`
  - the Azure Functions Service Bus trigger auto-completes the message on its own default settings
  when the trigger function returns without throwing, exactly as before this option existed. Set
  `AckMode = ServiceBusAckMode.Explicit` for real per-message `CompleteMessageAsync`/
  `AbandonMessageAsync` control based on the handler's outcome - this requires **two** things
  together: (1) the trigger's `[ServiceBusTrigger]` attribute must set `AutoCompleteMessages = false`
  (a Functions-runtime-level setting Benzene can't set for you), and (2) the trigger function must
  call the `HandleServiceBusMessages(IAzureFunctionApp, ServiceBusMessageActions, params
  ServiceBusReceivedMessage[])` overload - bind `ServiceBusMessageActions` as a trigger function
  parameter and pass it through. The plain `HandleServiceBusMessages(IAzureFunctionApp, params
  ServiceBusReceivedMessage[])` overload has no `ServiceBusMessageActions` to act on, so `AckMode`
  has no effect through it even if set to `Explicit` - see `ServiceBusBatchApplication`'s own doc
  comments. On success, the message is completed; on a non-exception failure result or an unhandled
  exception, it's abandoned (returned to the queue, respecting the queue's own max-delivery-count
  before auto-dead-lettering) - abandon happens exactly once per message regardless of
  `CatchExceptions`/`RaiseOnFailureStatus`, since those two options only decide whether the *whole
  invocation* cascades, not whether *this message* gets acted on. Session handling
  (`ServiceBusSessionMessageActions`, ordered per-session processing) is still **not implemented**.
  `ServiceBusMessageMessageHandlerResultSetter` DOES record the outcome onto
  `ServiceBusContext.MessageResult` (it's not a no-op) - that's what both `RaiseOnFailureStatus` and
  `AckMode = Explicit` read to decide a message's outcome.
- **Exception/failure-status handling is configurable via `ServiceBusOptions`**
  (`UseServiceBus(..., configure)`), defaulting to today's original behavior: a handler exception
  cascades and fails the whole trigger invocation, and a non-exception failure result is silently
  accepted. Set `ServiceBusOptions.CatchExceptions = true` to catch and log an exception instead of
  cascading it (that message's failure doesn't affect the rest of the batch or fail the invocation);
  set `ServiceBusOptions.RaiseOnFailureStatus = true` to escalate a non-exception failure result into
  a thrown `ServiceBusMessageProcessingException` too. Both default to `false`
  (purely additive/opt-in). On their own (with `AckMode` left at the default `AutoComplete`), these
  only control whether the *whole invocation* is reported as failed to the Functions host - true
  per-message completion needs `AckMode = ServiceBusAckMode.Explicit` too, see "True per-message
  ack" below.
- Supports both single-message triggers (the common case) and batched triggers (`IsBatched = true`) via
  the same `params ServiceBusReceivedMessage[]` dispatch signature.

## Tests
- `test/Benzene.Core.Test/Azure/ServiceBusPipelineTest.cs` - full pipeline happy path.
- `test/Benzene.Core.Test/Azure/ServiceBus/` - `ServiceBusMessageTopicGetter`/`ServiceBusMessageHeadersGetter`.
- `test/Benzene.Core.Test/Azure/ServiceBusFailureHandlingTest.cs` - `ServiceBusOptions`'
  `CatchExceptions`/`RaiseOnFailureStatus` combinations against `ServiceBusBatchApplication` directly,
  plus `AckMode = Explicit` complete/abandon behavior (success completes, failure result abandons, an
  unhandled exception abandons then cascades or is swallowed per `CatchExceptions`, and the plain
  `ServiceBusReceivedMessage[]` overload never touches `ServiceBusMessageActions` even when `AckMode`
  is `Explicit`) - dispatches through `IMiddlewareApplication<ServiceBusTriggerBatch>` directly with
  a mocked `Microsoft.Azure.Functions.Worker.ServiceBusMessageActions` (mockable: non-sealed, virtual
  methods, protected constructor Moq's proxy can call).
