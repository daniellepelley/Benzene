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
- `ServiceBusBatchMessageClient` — `IBenzeneBatchMessageClient` (from `Benzene.Clients`); sends a
  collection via a native `ServiceBusMessageBatch` (`CreateMessageBatchAsync` + `TryAddMessage` until
  full, then one `SendMessagesAsync`; rolls to a new batch when full). Reuses
  `ServiceBusContextConverter<T>` per message (topic/header properties + sender properties). **Failure
  granularity is per-batch, not per-message** — a Service Bus batch send is atomic, so if a
  `SendMessagesAsync` throws, every message in that batch is reported failed (against its request
  index) in the `BatchSendResult`; a single message too large for an empty batch is its own failure
  without aborting the rest. Covered by `test/Benzene.Core.Test/Clients/Azure/BatchMessageClientTest.cs`.

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

## Broker-level sender properties (opt-in) — `ServiceBusSenderProperties`
Beyond forwarding headers as application properties, the send path can map specific headers onto
broker-level `ServiceBusMessage` properties, via an optional `ServiceBusSenderProperties` passed to
`ServiceBusContextConverter<T>` / `.UseServiceBus<T>(..., senderProperties:)`. Each field names the
header key to read (null = unset): `MessageIdHeader` → `MessageId` (enables broker-side duplicate
detection under at-least-once), `SessionIdHeader` → `SessionId` (required to produce to a
session-enabled entity — pairs with the sessions consumer), `ScheduledEnqueueTimeHeader` →
`ScheduledEnqueueTime` (ISO-8601 timestamp), `TimeToLiveHeader` → `TimeToLive` (seconds, ISO-8601
duration like `PT30S`, or a `TimeSpan` string). Additive/opt-in; the header also remains a plain
application property. Covered by `ServiceBusContextConverterTest`.

## No `TokenCredential`/connection-string wrapping — deliberately
Mirrors the ingress `IServiceBusClientFactory` seam: this package takes an already-built
`ServiceBusSender` (obtained from `ServiceBusClient.CreateSender(queueOrTopicName)`), not a
connection string or `TokenCredential`. The caller builds the `ServiceBusClient` however they choose
(connection string, `DefaultAzureCredential` for Managed Identity, the emulator) — Benzene never
wraps that choice (design philosophy principle 1: don't hide the SDK's own capabilities).

## Dependencies
`Azure.Messaging.ServiceBus`; Benzene `Clients`, `Core.Middleware`, `Results`.
