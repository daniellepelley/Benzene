# Benzene.Azure.Function.ServiceBus

## What this package does
Azure Service Bus integration for Benzene's Azure Functions isolated-worker host. Wraps a triggered
Service Bus message (or batch, if the trigger is configured with `IsBatched = true`) in a
`ServiceBusContext` and runs it through the standard message-handler middleware pipeline, so
`[Message("topic")]`-attributed handlers and `.UseMessageHandlers()` topic-based routing work exactly
as they do for HTTP, Event Hubs, and Kafka.

## Key types/interfaces
- `ServiceBusContext` - wraps a single `Azure.Messaging.ServiceBus.ServiceBusReceivedMessage`; also
  implements `IHasPresetTopic` (`Benzene.Abstractions.MessageHandlers.Mappers`) so a
  subscription can be given a fixed preset topic - see "Important conventions" below
- `ServiceBusMessageTopicGetter` - reads the topic from the message's `"topic"` application property
- `ServiceBusMessageBodyGetter` - reads the message body as a string (`message.Body.ToString()`)
- `ServiceBusMessageHeadersGetter` - exposes the message's string-typed application properties as headers
- `ServiceBusMessageMessageHandlerResultSetter` - no-op (see "Important conventions" below)
- `ServiceBusApplication` - the entry point application invoked by the Azure Functions trigger method
- `DependencyInjectionExtensions.AddAzureServiceBus()` / `UseServiceBus(...)` - registration and pipeline wiring

## When to use this package
- When consuming messages from an Azure Service Bus queue or topic/subscription via an Azure Functions
  isolated-worker `[ServiceBusTrigger]` method
- When you want the same `[Message("topic")]` handler-routing model already used for HTTP/Event Hubs/Kafka

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Core.MessageHandlers** - Message handler infrastructure, `DefaultMessageMessageHandlerResultSetterBase`
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
  route every message on it to a fixed topic instead of relying on the property.
- **Result handling is a no-op**: unlike some other Benzene transports, this package does not complete,
  abandon, or dead-letter the message based on the handler's outcome. The Azure Functions Service Bus
  trigger auto-completes the message on its own default settings when the trigger function returns
  without throwing. Explicit complete/abandon/dead-letter control (via `ServiceBusMessageActions`) and
  session handling are **not implemented** - they're candidates for future work, not present today.
- Supports both single-message triggers (the common case) and batched triggers (`IsBatched = true`) via
  the same `params ServiceBusReceivedMessage[]` dispatch signature.
