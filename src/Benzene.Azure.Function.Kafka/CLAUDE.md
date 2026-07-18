# Benzene.Azure.Function.Kafka

## What this package does
The **Kafka trigger** adapter for Benzene's Azure Functions isolated-worker host, targeting Event
Hubs' Kafka-compatible endpoint (the `Microsoft.Azure.Functions.Worker.Extensions.Kafka`
extension). It runs a triggered batch of `KafkaRecord`s through the middleware pipeline, routing
each by its native Kafka **topic** — so `[Message("topic")]` handlers and `.UseMessageHandlers()`
work as with every other transport. This is Kafka-over-Event-Hubs specifically; for MSK / native
AWS Kafka use `Benzene.Aws.Lambda.Kafka`, and for a self-hosted (non-Functions) Kafka consumer use
`Benzene.Kafka.Core`.

## ⚠️ Unsafe by default: a handler failure result is silently accepted, not retried
`KafkaOptions.RaiseOnFailureStatus` defaults to `false`. A handler that returns a failure result
(e.g. `BenzeneResult.ServiceUnavailable(...)`) without throwing does not fail the invocation — the
Functions host's retry notices nothing, and there is no partial-batch-failure mechanism for this
trigger to fall back on either (see "Key types" below). Only a thrown exception is retried by
default. Set `RaiseOnFailureStatus = true` to escalate a failure result into a thrown
`KafkaMessageProcessingException` if you want it retried too — this means a redelivered record can
be reprocessed, so the handler needs to be idempotent (see
[Capability Matrix](../../docs/capability-matrix.md) /
[Idempotency](../../docs/cookbooks/idempotency.md)).

## Key types
- `KafkaContext : IHasMessageResult` — wraps a single `KafkaRecord`; records the handler outcome on
  `MessageResult`.
- Mappers: `KafkaMessageTopicGetter` (topic from `record.Topic` — a real Kafka topic, unlike Queue
  Storage which has none), `KafkaMessageBodyGetter`, `KafkaMessageHeadersGetter`,
  `KafkaMessageHandlerResultSetter`.
- `KafkaApplication : EntryPointMiddlewareApplication<KafkaRecord[]>` / `KafkaBatchApplication` —
  the entry point and its per-record batch loop, transport-tagged `"kafka"`.
- `KafkaOptions` — `CatchExceptions` (default `false`: a handler exception cascades to fail the
  whole trigger invocation, so the Functions host's retry notices) and `RaiseOnFailureStatus`
  (escalate a non-exception failure result the same way). Mirrors `ServiceBusOptions`' first two
  flags; Kafka has no per-message ack shape, so there's no `AckMode` here.
- `Extensions.HandleKafkaEvents(this IAzureFunctionApp, params KafkaRecord[])` — the dispatch helper
  the `[KafkaTrigger]` function calls. `DependencyInjectionExtensions.UseKafka(...)` is the wiring
  (both builder types), with an optional `Action<KafkaOptions>`.
- **invocationId (release plan Tier 3.5).** `UseKafka(...)` now auto-wires
  `BenzeneInvocationExtensions.cs`'s `UseBenzeneInvocation()` as the first middleware, so
  `IBenzeneInvocation` resolves inside each record's dispatch (`InvocationId` =
  `"{topic}-{partition}-{offset}"` - Kafka has no single message-id field) - each record in the
  trigger's batch is dispatched through its own DI scope via `MiddlewareMultiApplication`'s
  per-record `CreateScope()`, disconnected from whatever `IBenzeneInvocation` the outer Azure
  Functions invocation populated. No application code changes needed.

## Headers and W3C trace context
`KafkaMessageHeadersGetter` reads `KafkaRecord.Headers` (`Microsoft.Azure.Functions.Worker.KafkaHeader[]`
— confirmed present on `Microsoft.Azure.Functions.Worker.Extensions.Kafka` 4.3.0's `KafkaRecord` type,
each entry a `Key`/`Value` (`byte[]`) pair), UTF-8 decoding each value — same convention as
`Benzene.Kafka.Core`'s and `Benzene.Aws.Lambda.Kafka`'s header getters. This means `.UseW3CTraceContext
<KafkaContext>()` works on this transport: a `traceparent`/`tracestate` header set by an upstream
producer round-trips through the trigger and becomes the pipeline's root `Activity`'s parent, same as
every other transport whose `IMessageHeadersGetter<TContext>` reads real headers. Covered by
`KafkaGettersTest.cs` (decoding) and `KafkaW3CTraceContextTest.cs` (end-to-end trace continuation).

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
  `KafkaBatchAndNoOpTest.cs`, `KafkaW3CTraceContextTest.cs`.
