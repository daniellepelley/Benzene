# Benzene RabbitMQ Integration Plan

## Context

Benzene has broad broker coverage — every cloud vendor's queue/topic service plus Kafka and gRPC —
but **no vendor-neutral, self-hosted message broker**. RabbitMQ is the most widely deployed
open-source broker (an order of magnitude more job-market demand than NATS), and it's the obvious
first choice to fill that gap: teams running on-prem, in Kubernetes, or across clouds who don't want
to couple to a vendor's queue.

The fit is clean. Benzene already ships two self-hosted consumer-worker blueprints
(`Benzene.Kafka.Core`'s `BenzeneKafkaWorker` and `Benzene.Aws.Sqs`'s `Consumer/`) and a
transport-neutral outbound seam (`IBenzeneMessageSender` / `OutboundRoutingBuilder` in
`Benzene.Clients`). RabbitMQ slots into both. `RabbitMQ.Client` v7 is async-first and
Apache-2.0/MPL-2.0 licensed — no licensing trap.

## Verified facts this plan relies on

- **Inbound blueprint = `Benzene.Kafka.Core`.** `BenzeneKafkaWorker<TKey,TValue> : IBenzeneWorker`
  runs a consume loop on a background task, dispatches each message through
  `Benzene.SelfHost.BoundedConcurrentDispatcher<T>` into a `KafkaApplication` (a
  `MiddlewareApplication<TEvent,TContext>` that creates one DI scope per message), and drains/closes
  on `StopAsync`. `Extensions.UseKafka(...)` on `IBenzeneWorkerStartup` registers
  `.AddBenzeneMessage().AddKafka<...>()`, builds the pipeline, and adds the worker
  (`src/Benzene.Kafka.Core/Extensions.cs`). RabbitMQ's consumer worker mirrors this exactly.
- **`IBenzeneWorker` is tiny** — `StartAsync`/`StopAsync` (`Benzene.Abstractions.Hosting`). The
  drain/close/QoS discipline is the worker's own responsibility (as in `BenzeneKafkaWorker`).
- **Outbound blueprint = `IBenzeneMessageSender` + a transport client.** Business logic depends only
  on `IBenzeneMessageSender.SendAsync<TReq,TResp>(topic, request, headers)`; `OutboundRoutingBuilder`
  builds one outbound pipeline per topic with a transport middleware at the bottom. Kafka's
  `KafkaBenzeneMessageClient` is the reference for a produce-side transport client
  (`src/Benzene.Kafka.Core`); RabbitMQ's is a publish-to-exchange sibling.
- **Topic resolution follows the established queue convention.** SQS/SNS/Service Bus/PubSub all read
  the topic from a message attribute/property named `topic` (with `.UsePresetTopic(...)` as the
  fallback when the producer isn't a Benzene client). RabbitMQ carries this as a message **header**
  (or, optionally, the AMQP routing key) — same `IMessageTopicGetter<RabbitMqContext>` shape as
  every other transport.
- **RabbitMQ has first-class per-message ack** — `BasicAck` / `BasicNack`(requeue) / dead-letter
  exchange. This is the Service Bus `AckMode.Explicit` model, not Kafka's offset watermark: the
  worker acks on handler success and nacks (to requeue or DLX) on failure. That is the natural
  default and a real advantage over the Kafka/SQS batch-ack dance.

## Design

### Packages

- **`Benzene.RabbitMq`** — inbound consumer worker + outbound publish client + the shared
  `RabbitMqContext` and getters. (Split into `.Core` / client packages only if the surface grows;
  start as one package, matching `Benzene.Kafka.Core`'s single-package shape.)

### Inbound: `RabbitMqWorker : IBenzeneWorker`

- Opens an `IConnection`/`IChannel` (RabbitMQ.Client v7 async API), sets QoS prefetch, and consumes
  via `AsyncEventingBasicConsumer` / `BasicConsumeAsync`.
- Wraps each delivery in a **`RabbitMqContext`** (the `BasicDeliverEventArgs`: body, `BasicProperties`
  headers, routing key, delivery tag) and dispatches through `BoundedConcurrentDispatcher<T>` into a
  `RabbitMqApplication : MiddlewareApplication<...>` — one DI scope per message, exactly like
  `KafkaApplication`.
- Getters: `RabbitMqMessageTopicGetter` (topic from the `topic` header, falling back to routing key
  / preset topic), `RabbitMqMessageBodyGetter` (UTF-8 body), `RabbitMqMessageHeadersGetter`
  (`BasicProperties.Headers`), `RabbitMqMessageHandlerResultSetter` (records the outcome).
- **Ack policy** (`RabbitMqOptions.AckMode`, default explicit): ack on handler success; on
  failure, nack with requeue **or** route to a dead-letter exchange (configurable), respecting a
  max-redelivery bound. Mirrors `ServiceBusOptions`' explicit ack semantics.
- Config (`RabbitMqOptions`): queue name, prefetch count, concurrency, ack mode, requeue-vs-DLX,
  connection recovery. `Extensions.UseRabbitMq(this IBenzeneWorkerStartup, options, action)` wires
  it, mirroring `UseKafka`.

### Outbound: `RabbitMqBenzeneMessageClient`

- Publishes to an exchange with a routing key, forwarding the Benzene header dictionary onto
  `BasicProperties.Headers` (so correlation/trace/version headers reach the wire — matching Kafka's
  header forwarding), and maps the publish outcome back to a Benzene status.
- Reliability: opt-in **publisher confirms** for at-least-once publish. Integrated via
  `OutboundRoutingBuilder` as the transport middleware for a topic, so call sites stay on
  `IBenzeneMessageSender`.

## Scope

**In:** inbound consumer worker (per-message ack), outbound publish client (via
`OutboundRoutingBuilder`), `RabbitMqContext` + getters, options/config, unit tests, a live
integration test, `docs/getting-started-rabbitmq.md`, an example, and a package `CLAUDE.md`.

**Out (for now):** RPC-over-RabbitMQ (`reply-to`/direct-reply request/response); RabbitMQ **Streams**
(a distinct super-stream/offset model — closer to Kafka); topology *management* tooling (declaring
exchanges/queues/bindings, and generating them from `[Message]` topics à la
`Benzene.CodeGen.Terraform`) — the worker assumes the queue exists, as Kafka assumes topics do;
quorum-queue-specific features. **NATS.Net** is the natural next self-hosted broker after this and is
explicitly deferred.

## Phases

1. **Inbound consumer worker** — `RabbitMqWorker`/`RabbitMqApplication`/`RabbitMqContext`/getters +
   `UseRabbitMq`, with explicit per-message ack/nack/DLX. Unit tests over the getters and the
   ack-policy branches (success → ack; failure result → nack/DLX; exception → nack/DLX), dispatched
   against the application directly (no live broker), mirroring `ServiceBusFailureHandlingTest`.
2. **Outbound publish client** — `RabbitMqBenzeneMessageClient` + `OutboundRoutingBuilder` wiring +
   publisher-confirm option. Unit tests against a mocked channel (status mapping, header
   forwarding), mirroring `KafkaBenzeneMessageClientTest`.
3. **Live integration test** — round-trip a real message through a real broker, mirroring
   `BenzeneKafkaWorkerLiveTest`. Broker via Testcontainers' RabbitMQ module if that investigation
   (see `work/testing-tooling-investigation.md`) has landed, else the existing `DockerComposeFixture`
   pattern.
4. **Docs & example** — getting-started guide, a self-hosted worker example, and a fan-in/fan-out
   cookbook.

## Open questions

- **Topic source default** — header `topic` (portable, matches other transports) vs AMQP routing
  key (idiomatic RabbitMQ). Leaning: header by default, routing key as a configurable
  `IMessageTopicGetter` swap, `.UsePresetTopic` as the non-Benzene-producer fallback.
- **Requeue vs dead-letter on failure** — immediate requeue risks a poison-message hot loop; a DLX
  is safer but needs topology. Default to requeue-with-bound, document DLX as the production setting.
- **Connection/channel lifetime & recovery** — one shared connection, channel-per-consumer;
  automatic recovery on by default. Confirm the concurrency model against
  `BoundedConcurrentDispatcher`'s lanes (RabbitMQ prefetch + per-lane ack ordering).
- **Publisher confirms default** — off (fire-and-forget, lowest latency) vs on (at-least-once).
  Leaning off by default, opt-in, matching how the other outbound clients start.
