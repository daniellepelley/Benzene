# Benzene.Clients.Azure.EventHub

## What this package does
Outbound Azure Event Hubs client for a Benzene app: send an event to an Event Hub, so a Benzene
service can publish with the same routing property its own ingress consumes. The egress counterpart
of `Benzene.Azure.Function.EventHub` / `Benzene.Azure.EventHub` (release plan Tier 2.2, §5.2). Pins
only `Azure.Messaging.EventHubs` (5.11.5, matching the ingress packages' `.Processor` pin).

## Key types
- `EventHubBenzeneMessageClient` — `IBenzeneMessageClient`; sends via a caller-supplied `EventHubProducerClient`.
- `EventHubClientMiddleware` / `EventHubSendMessageContext` — terminal send middleware and its context;
  the middleware sends the event as a single-event batch (`CreateBatchAsync` + `TryAdd` + `SendAsync`).
- `EventHubContextConverter<T>` — `IBenzeneClientContext<T, Void>` → send context.
- `OutboundEventHubContextConverter` — the `Benzene.Clients.OutboundContext` counterpart, used by
  the `OutboundContext` overloads of `.UseEventHub(...)` for `AddOutboundRouting(...).Route(topic, …)`.
- `Extensions` — `UseEventHubClient`, `UseEventHub<T>`/`UseEventHub` (both the
  `IBenzeneClientContext<T,Void>` and `OutboundContext` overloads), `AddEventHubMessageClient`.

## Routing — matches the ingress exactly
Sets a `"topic"` property on `EventData.Properties` (the AMQP application-properties bag) — the same
property `EventHubConsumerMessageTopicGetter` reads on the self-hosted worker ingress side. Headers
(correlation id, W3C `traceparent`) are forwarded onto the same `Properties` bag as string values.
Note: the Azure Functions Event Hub *trigger* package routes via a Benzene-envelope body instead
(`UseBenzeneMessage`), not a property — if publishing to a trigger-based consumer using that path,
serialize the envelope into the event body yourself rather than relying on this package's
property-based converter.

## No `TokenCredential`/connection-string wrapping — deliberately
Mirrors the ingress `IEventProcessorClientFactory` seam: this package takes an already-built
`EventHubProducerClient`, not a connection string or `TokenCredential`. The caller builds the
producer client however they choose (connection string, `DefaultAzureCredential`, the emulator).

## Dependencies
`Azure.Messaging.EventHubs`; Benzene `Clients`, `Core.Middleware`, `Results`.
