# Benzene.Azure.Function.Kafka

## What this package does
The **Kafka trigger** adapter for Benzene's Azure Functions isolated-worker host, targeting Event
Hubs' Kafka-compatible endpoint (the `Microsoft.Azure.Functions.Worker.Extensions.Kafka`
extension). It runs a triggered batch of `KafkaRecord`s through the middleware pipeline, routing
each by its native Kafka **topic** — so `[Message("topic")]` handlers and `.UseMessageHandlers()`
work as with every other transport. This is Kafka-over-Event-Hubs specifically; for MSK / native
AWS Kafka use `Benzene.Aws.Lambda.Kafka`, and for a self-hosted (non-Functions) Kafka consumer use
`Benzene.Kafka.Core`.

## Key types
- `KafkaContext : IHasMessageResult` — wraps a single `KafkaRecord`; records the handler outcome on
  `MessageResult`.
- Mappers: `KafkaMessageTopicGetter` (topic from `record.Topic` — a real Kafka topic, unlike Queue
  Storage which has none), `KafkaMessageBodyGetter`, `KafkaMessageHeadersGetter`,
  `KafkaMessageMessageHandlerResultSetter`.
- `KafkaApplication : EntryPointMiddlewareApplication<KafkaRecord[]>` / `KafkaBatchApplication` —
  the entry point and its per-record batch loop, transport-tagged `"kafka"`.
- `KafkaOptions` — `CatchExceptions` (default `false`: a handler exception cascades to fail the
  whole trigger invocation, so the Functions host's retry notices) and `RaiseOnFailureStatus`
  (escalate a non-exception failure result the same way). Mirrors `ServiceBusOptions`' first two
  flags; Kafka has no per-message ack shape, so there's no `AckMode` here.
- `Extensions.HandleKafkaEvents(this IAzureFunctionApp, params KafkaRecord[])` — the dispatch helper
  the `[KafkaTrigger]` function calls. `DependencyInjectionExtensions.UseKafka(...)` is the wiring
  (both builder types), with an optional `Action<KafkaOptions>`.

## Known limitation
`KafkaMessageHeadersGetter` **always returns an empty dictionary** — the Functions Kafka trigger's
record headers aren't surfaced through this path yet. Body-based routing and payloads are
unaffected; only transport headers are missing. Documented in `docs/getting-started-kafka.md`'s
Azure Functions section.

## When to use this package
- Consuming Event Hubs over its Kafka protocol via an Azure Functions Kafka trigger. Add
  `Microsoft.Azure.Functions.Worker.Extensions.Kafka` (4.3.0) directly to your function app so the
  trigger registers (see `docs/azure-functions.md` → Kafka).

## Dependencies on other Benzene packages
- **Benzene.Azure.Function.Core** — the host/app builder.
- **Benzene.Core.MessageHandlers** — routing and mapper infrastructure.

## Important conventions
- The Kafka record value is `byte[]`, decoded as UTF-8 JSON like every other transport; there is no
  `UseBenzeneMessage` envelope bridge (that's for Event Hubs and Queue Storage, whose messages
  carry no routable topic of their own — a Kafka record does).
- Coverage: `KafkaPipelineTest.cs`, `KafkaGettersTest.cs`, `KafkaFailureHandlingTest.cs`,
  `KafkaBatchAndNoOpTest.cs`.
