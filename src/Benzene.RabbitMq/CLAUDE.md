# Benzene.RabbitMq

## What this package does
RabbitMQ transport for Benzene: a self-hosted consumer worker (`RabbitMqWorker`) with per-message
ack/nack, plus an outbound publish client (`RabbitMqBenzeneMessageClient`). Built on the
`RabbitMQ.Client` v7 async API (Apache-2.0/MPL-2.0 - no licensing trap). This is the first
vendor-neutral, self-hosted broker in Benzene (every other broker is a cloud vendor's, plus Kafka);
it fills the on-prem/Kubernetes/multi-cloud gap. One of the "self-hosted worker" startup modes in
`docs/hosting.md` - Benzene owns the process, like `Benzene.Kafka.Core`/`Benzene.Azure.ServiceBus`,
unlike the Lambda/Functions triggers.

## ⚠️ Ack policy: safe by default, unlike the Kafka/ServiceBus triggers
`RabbitMqConfig.AckMode` defaults to `RabbitMqAckMode.Explicit`: a delivery is `BasicAck`ed on
handler success and `BasicNack`ed on a failure `IMessageResult` **or** a thrown exception. RabbitMQ's
first-class per-message ack (the Service Bus `Explicit` model, not Kafka's offset watermark) makes
this the natural default and a real advantage - a failed message is redelivered or dead-lettered
rather than silently lost. `RabbitMqAckMode.AutoAck` (broker acks on dispatch, before the handler
runs) is available for at-most-once, loss-tolerant workloads. Because redelivery can reprocess a
message, handlers must be idempotent - see [Idempotency](../../docs/cookbooks/idempotency.md).

## Key types/interfaces

### Inbound (`RabbitMqMessage/`)
- `RabbitMqWorker : IBenzeneWorker` - opens an `IConnection`/`IChannel` (v7 async API) via
  `IRabbitMqConnectionFactory`, sets prefetch QoS, and consumes with an `AsyncEventingBasicConsumer`.
  Deliveries are **pushed** (not hand-polled like Kafka) and fanned out through
  `Benzene.SelfHost.BoundedConcurrentDispatcher<T>` so up to `ConcurrentRequests` handlers run at
  once; prefetch bounds unacked deliveries. `StartAsync` connects + starts consuming and returns;
  `StopAsync` cancels the consumer (`BasicCancelAsync`), drains in-flight handlers (up to
  `DrainTimeout`), then closes the channel and connection. The delivery body is a rented buffer only
  valid for the consumer callback, so the worker **copies it** (`Body.ToArray()`) before handing off
  to a dispatcher lane - do not remove that copy.
- `RabbitMqApplication : MiddlewareApplication<BasicDeliverEventArgs, RabbitMqContext, IMessageResult?>` -
  maps each delivery to a `RabbitMqContext`, runs it through the pipeline (tagged transport
  `"rabbitmq"`) in one DI scope per message, and returns the recorded `IMessageResult` the worker
  reads for ack. Mirrors `ServiceBusConsumerApplication`.
- `RabbitMqContext : IHasMessageResult` - wraps `BasicDeliverEventArgs` (context purity: transport
  shape only), carries `MessageResult`.
- Getters: `RabbitMqMessageTopicGetter` (topic from the topic header, **falling back to the AMQP
  routing key**; wrapped in `PresetTopicMessageTopicGetter` so `.UsePresetTopic(...)` works),
  `RabbitMqMessageBodyGetter` (UTF-8 body), `RabbitMqMessageHeadersGetter` (decodes the
  `byte[]`-valued `BasicProperties.Headers`, so W3C-trace/correlation/version header decorators
  work), `RabbitMqMessageHandlerResultSetter` (`MessageHandlerResultSetterBase`).
- **Ack/nack policy** (`RabbitMqWorker`, `Explicit` mode): success → `BasicAck`; failure result or
  exception → `BasicNack`. Requeue is governed by `RequeueOnFailure` and **bounded to one retry**: a
  first-attempt failure requeues, an already-`Redelivered` failure is nacked without requeue (to the
  DLX / dropped) so a poison message can't hot-loop. `RequeueOnFailure = false` always nacks without
  requeue (straight to DLX). RabbitMQ's `Redelivered` is a boolean, not a count - for a higher,
  precise redelivery limit, use a dead-letter exchange + queue policy on the broker (quorum-queue
  delivery-count features are out of scope).
- **`AddRabbitMq` must register everything `.UseMessageHandlers()` resolves** per `RabbitMqContext`:
  the four getters, `IMessageVersionGetter` (`HeaderMessageVersionGetter`),
  `AddMediaFormatNegotiation`, and `IRequestMapper` (`MultiSerializerOptionsRequestMapper`). A gap
  wouldn't throw visibly - the worker nacks handler faults - so it surfaces only as messages never
  handled; `RabbitMqRealPipelineTest` drives the real DI + routing (no broker) to catch it,
  mirroring `ServiceBusConsumerRealPipelineTest`.
- `IRabbitMqConnectionFactory` / `RabbitMqConnectionFactory` - the connection seam (mirrors
  `IKafkaConsumerFactory`/`IServiceBusClientFactory`): the caller builds the `ConnectionFactory`
  (host, credentials, vhost, TLS, automatic recovery - on by the SDK's default), the worker owns the
  channel and disposes both on stop.
- `Extensions.UseRabbitMq(IBenzeneWorkerStartup, config, connectionFactory, action)` - the worker
  wiring, mirroring `UseKafka`/`UseServiceBus`; registers `AddBenzeneMessage().AddRabbitMq()`.

### Outbound (`RabbitMqSendMessage/`)
- `RabbitMqBenzeneMessageClient : IBenzeneMessageClient` - publishes so business logic depends only
  on `IBenzeneMessageSender`/`IBenzeneMessageClient`. Mirrors `KafkaBenzeneMessageClient`, including
  the shared static `ISerializer` (a fresh `JsonSerializer` per send would defeat System.Text.Json's
  per-options converter cache) and the second (prebuilt-pipeline) constructor for testing.
- `RabbitMqContextConverter<T>` - the request `Topic` becomes the AMQP **routing key** and is also
  carried as a `"topic"` **header**, so a Benzene consumer routes by header (portable, matching every
  other transport) with the routing key as the idiomatic fallback. Forwards the Benzene header
  dictionary onto `BasicProperties.Headers` (UTF-8 encoded).
- `RabbitMqClientMiddleware` / `.UseRabbitMqClient(channel)` - the publish middleware
  (`BasicPublishAsync`); `.UseRabbitMq<T>(exchange, ...)` is the `OutboundRoutingBuilder` conversion
  entry point, mirroring Kafka's `.UseKafka<T>(...)`.
- Publish is **persistent by default** (delivery mode 2) so a message on a durable queue survives a
  broker restart; pass `.UseRabbitMqClient(channel, persistent: false)` for transient delivery. This
  is a behavioral change from earlier versions, which always published transient.
- Publish is fire-and-forget by default (maps a completed publish to `Accepted`, a thrown publish to
  `ServiceUnavailable`). Publisher confirms (at-least-once) are opt-in at the **channel** level: build
  the channel with `CreateChannelOptions { PublisherConfirmationsEnabled = true }` and RabbitMQ.Client
  v7's `BasicPublishAsync` awaits the broker confirm before completing, so a broker-side rejection
  surfaces as a thrown publish → `ServiceUnavailable`. The middleware itself needs no change for this.

## Configurable topic header key
The topic header key defaults to `RabbitMqConstants.DefaultTopicHeader` (`"topic"`) but is **not
hard-coded** - override it on each side to interoperate with a non-Benzene producer/consumer that
carries the topic on a different header, without writing a custom `IMessageTopicGetter`/converter:
- **Consumer**: `RabbitMqConfig.TopicHeaderKey` (threaded by `UseRabbitMq` into
  `AddRabbitMq(topicHeaderKey)`, which constructs `new RabbitMqMessageTopicGetter(topicHeaderKey)`).
  The bare `AddRabbitMq()` / `new RabbitMqMessageTopicGetter()` keep the default.
- **Producer**: the `topicHeaderKey` argument on the outbound `.UseRabbitMq<T>(...)` extensions,
  `RabbitMqBenzeneMessageClient`, and `RabbitMqContextConverter<T>` - all default to the same
  constant.
Keep the producer's and consumer's keys in sync. The routing-key fallback is unaffected: a message
lacking the configured header still routes by its AMQP routing key.

## When to use this package
- Consuming/producing RabbitMQ from a long-running process (console, container, Kubernetes) via
  `Benzene.HostedService` / `Benzene.SelfHost`.

## Deliberate boundaries (NOT shipped)
- RPC-over-RabbitMQ (`reply-to`/direct-reply request/response); RabbitMQ **Streams** (a distinct
  offset model, closer to Kafka); topology *management* (declaring exchanges/queues/bindings, or
  generating them from `[Message]` topics) - the worker assumes the queue and any DLX exist, as the
  Kafka worker assumes topics do; quorum-queue-specific features. **NATS.Net** is the next
  self-hosted broker candidate and is deferred. See `docs/plans/rabbitmq-plan.md`.

## Dependencies on other Benzene packages
- **Benzene.Clients** - `IBenzeneMessageClient`, outbound seam.
- **Benzene.Core.MessageHandlers** - routing, mappers, `TransportMiddlewarePipeline`,
  `MessageHandlerResultSetterBase`, `PresetTopicMessageTopicGetter`.
- **Benzene.SelfHost** - `IBenzeneWorkerStartup`, `BoundedConcurrentDispatcher<T>` (and
  `IBenzeneWorker` via `Benzene.Abstractions.Pipelines`).
- **RabbitMQ.Client** (v7) - the broker client.

## Test coverage
- Unit (`test/Benzene.Core.Test/RabbitMq/`, no broker): `RabbitMqGettersTest` (topic header vs
  routing-key fallback, body, header decoding), `RabbitMqApplicationTest` (delivery→context→result),
  `RabbitMqWorkerTest` (drives real deliveries through the `AsyncEventingBasicConsumer` against a
  mocked `IChannel`: success→ack, failure→nack-requeue, redelivered-failure→nack-no-requeue,
  requeue-disabled, exception→nack, no-result→ack, AutoAck mode, config defaults),
  `RabbitMqBenzeneMessageClientTest` (status mapping + topic-as-routing-key + header forwarding),
  `RabbitMqRealPipelineTest` (real DI registration completeness).
- Live (`test/Benzene.Integration.Test/RabbitMq/`, CI-only, needs Docker): `RabbitMqWorkerLiveTest`
  round-trips a real message through a real broker, mirroring `BenzeneKafkaWorkerLiveTest`.
