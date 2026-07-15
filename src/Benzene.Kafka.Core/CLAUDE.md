# Benzene.Kafka.Core

## What this package does
Core Kafka integration for Benzene: a self-hosted worker (`BenzeneKafkaWorker<TKey,TValue>`) that
runs its own consume loop and dispatches messages into a Benzene middleware pipeline, plus outbound
producer support. This is one of the "self-hosted worker" startup modes documented in
`docs/hosting.md` — Benzene itself owns the process here, unlike AWS Lambda/Azure Functions
(triggered by infrastructure) or ASP.NET Core (embedded in an existing listener).

## Key types/interfaces
- `BenzeneKafkaWorker<TKey,TValue> : IBenzeneWorker` - runs `IConsumer<TKey,TValue>.Consume(...)`
  in a loop on a background task and dispatches each `ConsumeResult` through
  `Benzene.SelfHost.BoundedConcurrentDispatcher<T>` (see that package's `CLAUDE.md`) instead of a
  raw semaphore. `StartAsync` kicks off the loop and returns immediately (correct `IHostedService`
  semantics - it does not block until cancellation, which the old implementation did); `StopAsync`
  signals its own `CancellationTokenSource`, then awaits the loop's graceful drain and consumer
  close. The whole loop body (consumer/dispatcher construction included) is wrapped in a
  `try`/`finally` so the consumer is always `Close()`d **and** `Dispose()`d - not just closed - on
  every exit path, including a setup failure or an unexpected (non-`ConsumeException`) error, which
  is also now logged at `Critical` rather than silently killing the loop. A `ConsumeException`
  (e.g. broker unreachable) is retried after `ConsumeExceptionRetryDelay` rather than immediately,
  so a persistently failing broker can't spin the loop as a tight, log-spamming busy-loop.
- `BenzeneKafkaConfig` - `ConsumerConfig`/`Topics` (Confluent.Kafka passthrough, both `required`),
  `ConcurrentRequests` (max concurrent message handlers, default 5),
  `PreserveOrderPerPartition` (default `true` - routes same-partition messages to the same
  dispatcher lane so they're handled in order, since Kafka only ever promises order within a
  partition; set `false` for unordered round-robin dispatch when throughput matters more than
  order), `DrainTimeout` (default 30s - how long `StopAsync` waits for in-flight messages before
  abandoning them), `ConsumeExceptionRetryDelay` (default 1s - backoff between retries after a
  `ConsumeException`).
- `KafkaApplication<TKey,TValue>` - wraps the built middleware pipeline; `HandleAsync` is what the
  dispatcher calls per message, via its base `MiddlewareApplication<TEvent,TContext>` (`Benzene.Core.Middleware`),
  which creates a new DI scope per message and disposes it once the pipeline finishes - previously a
  real per-message resource leak (not specific to Kafka; every `MiddlewareApplication<...>`-based
  adapter had the same gap), fixed in `Benzene.Core.Middleware` itself - see that package's
  `CLAUDE.md` and `work/performance-roadmap-1.0.md`'s 2026-07-15 follow-up entry.
- `KafkaContextConverter` forwards `IBenzeneClientRequest.Headers` onto the outbound
  `Message.Headers` (UTF-8 encoded, matching Confluent.Kafka's `byte[]`-valued headers) so
  header-based decorators (correlation ID, W3C trace context) reach the wire.
- `KafkaBenzeneMessageClient.SendMessageAsync` reuses a single shared `ISerializer` instance across
  every call (rather than a fresh `JsonSerializer()`/`JsonSerializerOptions` per send) so
  System.Text.Json's per-`JsonSerializerOptions` converter/metadata cache isn't defeated on every
  outbound message.
- Getters/converters (`KafkaMessage/*Getter*`, `Kafka/*Getter*`, `KafkaMessageContextConverter`,
  `KafkaClientMiddleware`) operate on already-constructed Confluent.Kafka objects and need no live
  broker - now unit-tested directly in `test/Benzene.Core.Test/Kafka/KafkaCoreMappersTest.cs`
  (previously untested, unlike `KafkaContextConverter`'s header-forwarding, which was already
  covered by `test/Benzene.Core.Test/Clients/OutboundHeaderForwardingTest.cs`).
- `KafkaBenzeneMessageClient` - like `SqsBenzeneMessageClient`/`SnsBenzeneMessageClient`, its second
  constructor accepts an already-built `IMiddlewarePipeline<KafkaSendMessageContext>` directly, so
  `SendMessageAsync`'s status mapping (`Persisted` → `Accepted`, anything else → `UnexpectedError`,
  a thrown exception → `ServiceUnavailable`) is unit-testable against a mocked
  `IProducer<string,string>` with no live broker - see
  `test/Benzene.Core.Test/Kafka/KafkaBenzeneMessageClientTest.cs`.

## When to use this package
- When building Kafka-based applications
- For Kafka consumer/producer implementations
- As foundation for cloud Kafka services
- Used by Aws.Kafka and Azure.Kafka

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.MessageHandlers** - Message handler infrastructure
- **Benzene.HostedService** / **Benzene.SelfHost** - `IBenzeneWorker`, `BoundedConcurrentDispatcher<T>`
- **Confluent.Kafka** - Kafka client library

## Important conventions
- Consumer groups for scaling
- Partition assignment
- Offset management
- Message key for partitioning
- Headers for metadata
- Commit strategies configurable
- **No test coverage exists for `BenzeneKafkaWorker` itself** - `IConsumer<TKey,TValue>` needs a
  live broker to exercise for real, so this has never been unit tested directly (same situation as
  `Benzene.SelfHost.Http`'s `BenzeneHttpWorker`). The concurrency/ordering/drain behavior it relies
  on is unit-tested in isolation instead, via `Benzene.SelfHost.BoundedConcurrentDispatcher<T>`'s
  own test suite (`test/Benzene.Core.Test/Hosting/BoundedConcurrentDispatcherTest.cs`). Everything
  else in this package that doesn't need a broker (getters, converters, `KafkaClientMiddleware`) is
  unit-tested in `test/Benzene.Core.Test/Kafka/KafkaCoreMappersTest.cs` plus
  `test/Benzene.Core.Test/Clients/OutboundHeaderForwardingTest.cs`.
