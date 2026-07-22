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
  `IBenzeneClientContext<T,Void>` and `OutboundContext` overloads), `AddEventHubMessageClient`, and
  **`AddEventHubHealthCheck`**.
- `EventHubHealthCheck` — verifies a hub with a read-only `GetEventHubProperties` call (`Type =
  "EventHub"`, dependency `("EventHub", producerClient.EventHubName)`; non-destructive — no send).
  Failures go through `HealthCheckError.Classify` (§3.9). Event Hubs is **AMQP, not HTTP**, so there is
  no status code: the SDK's `EventHubsException.FailureReason` is surfaced as `ErrorCode`, and an
  `UnauthorizedAccessException` (a bad credential/claim) is mapped to `403` so it degrades to a
  **Warning** like the HTTP-based checks. The message is never included.
  - **Auto-wired (Phase 4, default-on).** The two `producerClient`-instance `UseEventHub`/`UseEventHub<T>`
    overloads take `bool healthCheck = true`: unless opted out they register the check on the
    **dependency category** (`AddDependencyHealthCheck`, dedup `"EventHub:{name}"`), **capturing the
    passed `EventHubProducerClient` directly** (Event Hubs clients are passed, not DI-resolved). Deep
    `healthcheck` layer only — never a probe (shared-fate; see `IDependencyHealthCheck`). The
    `action`-based overloads don't auto-wire.
- `EventHubBatchMessageClient` — `IBenzeneBatchMessageClient` (from `Benzene.Clients`); sends a
  collection via a native `EventDataBatch` (`CreateBatchAsync` + `TryAdd` until full, then one
  `SendAsync`; rolls to a new batch when full). A batch's partition key is fixed at creation, so
  requests are first **grouped by their resolved partition key** (`partitionKeyHeader`) — events
  sharing a key stay co-located and in order, exactly as the single-send path guarantees. **Failure
  granularity is per-batch** (atomic send): a throwing `SendAsync` reports every event in that batch
  as failed against its request index in the `BatchSendResult`. Covered by
  `test/Benzene.Core.Test/Clients/Azure/BatchMessageClientTest.cs`.

## Routing — matches the ingress exactly
Sets a `"topic"` property on `EventData.Properties` (the AMQP application-properties bag) — the same
property `EventHubConsumerMessageTopicGetter` reads on the self-hosted worker ingress side. The
property key is a configurable default, not hard-coded (`topicPropertyKey` on the converters,
`.UseEventHub(..., topicPropertyKey: "x")`, and `AddEventHubMessageClient(..., topicPropertyKey)`) —
keep it in sync with the consumer's `BenzeneEventHubConfig.TopicPropertyKey`. Headers
(correlation id, W3C `traceparent`) are forwarded onto the same `Properties` bag as string values.
Note: the Azure Functions Event Hub *trigger* package routes via a Benzene-envelope body instead
(`UseBenzeneMessage`), not a property — if publishing to a trigger-based consumer using that path,
serialize the envelope into the event body yourself rather than relying on this package's
property-based converter.

## Partition key — per-key ordering
By default an event is sent with **no partition key**, so Event Hubs round-robins it across
partitions (no ordering guarantee). To co-locate related events on one partition (the only way to get
ordered delivery), set `partitionKeyHeader` on `.UseEventHub(..., partitionKeyHeader: "x")` (or the
converter ctor): the named request header's value becomes the batch's `CreateBatchOptions.PartitionKey`.
Without this the per-partition ordering the consumer side advertises is unreachable end-to-end. The
key is carried through `EventHubSendMessageContext.PartitionKey` and applied in
`EventHubClientMiddleware` at `CreateBatchAsync` time.

## No `TokenCredential`/connection-string wrapping — deliberately
Mirrors the ingress `IEventProcessorClientFactory` seam: this package takes an already-built
`EventHubProducerClient`, not a connection string or `TokenCredential`. The caller builds the
producer client however they choose (connection string, `DefaultAzureCredential`, the emulator).

## Dependencies
`Azure.Messaging.EventHubs`; Benzene `Clients`, `Core.Middleware`, `Results`.
