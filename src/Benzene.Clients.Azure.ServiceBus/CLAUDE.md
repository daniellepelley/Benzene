# Benzene.Clients.Azure.ServiceBus

## What this package does
Outbound Azure Service Bus client for a Benzene app: send a message to a queue or topic, so a
Benzene service can publish to Service Bus with the same envelope shape its own ingress consumes.
The egress counterpart of `Benzene.Azure.Function.ServiceBus` / `Benzene.Azure.ServiceBus` (release
plan Tier 2.2, §5.2). Pins only `Azure.Messaging.ServiceBus` (7.18.2, matching the ingress packages).

## Key types
- `ServiceBusBenzeneMessageClient` — `IBenzeneMessageClient`; sends via a caller-supplied `ServiceBusSender`.
- `ServiceBusClientMiddleware` / `ServiceBusSendMessageContext` — terminal send middleware and its context.
- `ServiceBusContextConverter<T>` — `IBenzeneClientContext<T, Void>` → send context.
- `OutboundServiceBusContextConverter` — the `Benzene.Clients.OutboundContext` counterpart, used by
  the `OutboundContext` overloads of `.UseServiceBus(...)` for `AddOutboundRouting(...).Route(topic, …)`.
- `Extensions` — `UseServiceBusClient`, `UseServiceBus<T>`/`UseServiceBus` (both the
  `IBenzeneClientContext<T,Void>` and `OutboundContext` overloads), `AddServiceBusMessageClient`.

## Routing — matches the ingress exactly
Sets the `"topic"` application property on `ServiceBusMessage.ApplicationProperties` — the same
property `ServiceBusMessageTopicGetter`/`ServiceBusConsumerMessageTopicGetter` read on the ingress
side (both the Function trigger and the self-hosted worker). Headers (correlation id, W3C
`traceparent`) are forwarded onto the same `ApplicationProperties` bag as string values, matching
`ServiceBusMessageHeadersGetter`'s "every string-typed application property is a header" convention.
The topic property key is a configurable default, not hard-coded (`topicPropertyKey` on the
converters, `.UseServiceBus(..., topicPropertyKey: "x")`, and
`AddServiceBusMessageClient(..., topicPropertyKey)`) — keep it in sync with the consumer's key
(`BenzeneServiceBusConfig.TopicPropertyKey` / `.AddAzureServiceBus(topicPropertyKey)`).

## No `TokenCredential`/connection-string wrapping — deliberately
Mirrors the ingress `IServiceBusClientFactory` seam: this package takes an already-built
`ServiceBusSender` (obtained from `ServiceBusClient.CreateSender(queueOrTopicName)`), not a
connection string or `TokenCredential`. The caller builds the `ServiceBusClient` however they choose
(connection string, `DefaultAzureCredential` for Managed Identity, the emulator) — Benzene never
wraps that choice (design philosophy principle 1: don't hide the SDK's own capabilities).

## Dependencies
`Azure.Messaging.ServiceBus`; Benzene `Clients`, `Core.Middleware`, `Results`.
