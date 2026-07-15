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
  close.
- `BenzeneKafkaConfig` - `ConsumerConfig`/`Topics` (Confluent.Kafka passthrough),
  `ConcurrentRequests` (max concurrent message handlers, default 5),
  `PreserveOrderPerPartition` (default `true` - routes same-partition messages to the same
  dispatcher lane so they're handled in order, since Kafka only ever promises order within a
  partition; set `false` for unordered round-robin dispatch when throughput matters more than
  order), `DrainTimeout` (default 30s - how long `StopAsync` waits for in-flight messages before
  abandoning them).
- `KafkaApplication<TKey,TValue>` - wraps the built middleware pipeline; `HandleAsync` is what the
  dispatcher calls per message.
- `KafkaContextConverter` forwards `IBenzeneClientRequest.Headers` onto the outbound
  `Message.Headers` (UTF-8 encoded, matching Confluent.Kafka's `byte[]`-valued headers) so
  header-based decorators (correlation ID, W3C trace context) reach the wire.

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
  own test suite (`test/Benzene.Core.Test/Hosting/BoundedConcurrentDispatcherTest.cs`).
