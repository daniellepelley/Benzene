# Benzene.Kafka.Core

## What this package does
Core Kafka integration for Benzene: a self-hosted worker (`BenzeneKafkaWorker<TKey,TValue>`) that
runs its own consume loop and dispatches messages into a Benzene middleware pipeline, plus outbound
producer support. This is one of the "self-hosted worker" startup modes documented in
`docs/hosting.md` — Benzene itself owns the process here, unlike AWS Lambda/Azure Functions
(triggered by infrastructure) or ASP.NET Core (embedded in an existing listener).

## Key types/interfaces
- `KafkaHealthCheck` — verifies the consumer can reach its cluster and that its subscribed topics
  exist, via a read-only **cluster** `AdminClient.GetMetadata(timeout)` (cluster-level, not per-topic,
  so it never triggers broker-side auto-topic-creation) + a local check that each `Topics` entry is
  present. `Type = "Kafka"`, dependencies `("Broker", bootstrapServers)` + one `("Topic", t)` each.
  Non-destructive (no consume/commit/produce). A Kafka authorization failure is classified as a
  **Warning** via `HealthCheckError.Classify` (§3.9); the error code is surfaced, never the message.
  `IKafkaAdminClientFactory` / `KafkaAdminClientFactory` supply the admin client — **one is built and
  reused** across probes (the factory is captured at registration; its `Lazy<IAdminClient>` builds on
  first probe), never per probe. It copies only the connection/auth settings off the `ConsumerConfig`
  (bootstrap + common SASL/SSL); a `configure` callback / custom factory handles OAUTHBEARER etc.
  **Auto-wired (Phase 4, default-on):** `UseKafka(..., healthCheck: true)` calls
  `AddKafkaDependencyHealthCheck(config)` — registers on the **dependency** category (deep `healthcheck`
  layer only, never a probe — a broker blip is shared-fate; see `IDependencyHealthCheck`), dedup
  `"Kafka:{bootstrapServers}"`. `healthCheck: false` opts out; explicit `AddKafkaHealthCheck(config)` on
  an `IHealthCheckBuilder` is the manual path. The worker-startup seam supports this because
  `IBenzeneWorkerStartup : IRegisterDependency`.
- `IKafkaConsumerFactory<TKey,TValue>` / `KafkaConsumerFactory<TKey,TValue>` (2026-07-17,
  additive public API) - the seam through which the worker creates its `IConsumer`, mirroring the
  Azure workers' client-factory seams (`IEventProcessorClientFactory` etc.). `Create(ConsumerConfig)`
  receives the worker's own `ConsumerConfig` *after* worker adjustments (`CommitOnlyOnSuccess`'s
  `EnableAutoOffsetStore = false`) - build from the passed config, not a captured copy. The
  default factory takes an optional `Action<ConsumerBuilder<TKey,TValue>>` for builder
  configuration plain `ConsumerConfig` can't express - deserializers, handlers, and notably
  `SetOAuthBearerTokenRefreshHandler` for secretless Entra ID managed identity against Event Hubs'
  Kafka endpoint (see `docs/cookbooks/managed-identity.md`'s Kafka section). Passed as an optional
  last parameter on `UseKafka<TKey,TValue>(...)` and the worker ctor; omitted = the original
  build-straight-from-config behavior, unchanged. Tests:
  `test/Benzene.Core.Test/Kafka/KafkaConsumerFactoryTest.cs` (factory receives the adjusted
  config instance; worker subscribes/closes/disposes the created consumer; configure action runs).
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
  `ConsumeException`), `CatchHandlerExceptions` (default `true` - a message handler's unhandled
  exception is logged and that lane keeps consuming; set `false` to instead stop the whole worker
  on the first unhandled handler exception, wired via `BoundedConcurrentDispatcher`'s `onFault`
  callback calling this worker's own `StopAsync`-equivalent cancellation), `CommitOnlyOnSuccess`
  (default `false` - Confluent.Kafka's own default of auto-storing an offset as soon as `Consume`
  returns it, before it's actually been handled; set `true` for at-least-once processing, so a
  message whose handler fails - or whose worker crashes mid-handling - is redelivered instead of
  silently skipped). `CommitOnlyOnSuccess` sets `ConsumerConfig.EnableAutoOffsetStore = false` at
  worker startup and instead calls `IConsumer.StoreOffset` itself, only after
  `KafkaApplication.HandleAsync` returns successfully - `BenzeneKafkaWorker.StartAsync` throws
  `InvalidOperationException` at startup if `CommitOnlyOnSuccess = true` is combined with either
  `CatchHandlerExceptions = true` or `PreserveOrderPerPartition = false`. Both are load-bearing:
  `StoreOffset` is a last-write-wins watermark with **no gap tracking** (confirmed against
  librdkafka's C source - storing offset N+5 after skipping N+2..N+4 silently commits past the
  skipped messages), so a caught-and-swallowed handler exception would let a later, successful
  message on the same partition advance the watermark past the failed one before its offset was
  ever stored, and out-of-order handling (`PreserveOrderPerPartition = false`) has the same failure
  mode even without any exception involved.
  - **`DrainOnRevoke`** (`bool?`, 2026-07-20) - on a consumer-group rebalance, drains in-flight
    handlers for the *revoked* partitions and commits their stored offsets before releasing them, so
    no record is committed as done while still in flight and none is needlessly reprocessed by the
    partition's next owner. `null` (default) resolves (`ShouldDrainOnRevoke`) to `CommitOnlyOnSuccess`
    - draining is strictly safer under at-least-once and pointless under auto-store. Wired via the
    consumer's `SetPartitionsRevokedHandler`, which quiesces the revoked partitions' dispatcher lanes
    with `BoundedConcurrentDispatcher.DrainLanesAsync` (bounded by `DrainTimeout`) then `Commit()`s.
    Only honored when the consumer is built through the default `KafkaConsumerFactory` or a custom
    factory that applies the worker's builder-configuration callback (the new
    `IKafkaConsumerFactory.Create(config, configureBuilder)` overload - a default interface method, so
    pre-existing custom factories keep compiling but must implement it to get the drain handler). The
    worker still calls the original single-arg `Create` when draining is off, so nothing changes on
    that path. Prefer `PartitionAssignmentStrategy.CooperativeSticky` (a `ConsumerConfig` passthrough)
    to reduce stop-the-world rebalances. Tests: `KafkaWorkerDeadLetterAndDrainTest` (config
    resolution + the consumer is built with/without a rebalance handler per the flag); the lane-drain
    mechanics themselves in `BoundedConcurrentDispatcherTest.DrainLanesAsync_*`.
- **`KafkaDeadLetterOptions<TKey,TValue>`** (2026-07-20) - opt-in retry-then-dead-letter, passed to
  `UseKafka(..., deadLetterOptions:)` / the worker ctor. When a record's handler keeps failing, the
  worker retries it up to `MaxAttempts` (default 1, no retry), then re-produces the **original**
  record (key, value, headers) to `DeadLetterTopic` via the caller-built `Producer` - Benzene never
  wraps the producer's auth, matching the consumer-factory seam - with added `x-dlt-reason`
  (exception **type name** only, never the message text), `x-dlt-original-topic`,
  `x-dlt-original-partition`, `x-dlt-original-offset` headers, then advances past it (`StoreOffset`
  under `CommitOnlyOnSuccess`). Off unless both `DeadLetterTopic` and `Producer` are set
  (`IsEnabled`); the worker ctor throws if a topic is set without a producer. This keeps a poison
  record from either wedging the partition (the `CatchHandlerExceptions=false` failure mode) or being
  silently skipped (`CatchHandlerExceptions=true`): the dead-letter handle catches the handler fault
  itself, so only a failure to *produce* the dead-letter propagates. **Dead-lettering manages offsets
  the same way `CommitOnlyOnSuccess` does** (2026-07-20 hardening): it forces
  `EnableAutoOffsetStore=false`, requires `PreserveOrderPerPartition=true` (enforced at startup), and
  stores the offset only after a record is handled or successfully dead-lettered. If the dead-letter
  *produce* itself fails, the offset is **not** stored and the worker stops (`_stoppingCts.Cancel()`),
  so the record is redelivered on restart rather than silently lost when the next record on the
  partition would advance the watermark past it — trading availability for no-loss (a persistently
  unreachable DLT wedges the worker loudly instead of dropping poison records). This closes a
  silent-loss gap under the default `CommitOnlyOnSuccess=false` config. Tests:
  `KafkaWorkerDeadLetterAndDrainTest` (`DeadLetter_RetriesThenProducesOriginalRecordWithDiagnosticHeaders`,
  `DeadLetter_WhenProduceFails_StopsWorkerWithoutStoringTheOffset`,
  `DeadLetter_WithoutPreserveOrderPerPartition_ThrowsAtStartAsync`).
- `KafkaApplication<TKey,TValue>` - wraps the built middleware pipeline; `HandleAsync` is what the
  dispatcher calls per message, via its base `MiddlewareApplication<TEvent,TContext>` (`Benzene.Core.Middleware`),
  which creates a new DI scope per message and disposes it once the pipeline finishes - previously a
  real per-message resource leak (not specific to Kafka; every `MiddlewareApplication<...>`-based
  adapter had the same gap), fixed in `Benzene.Core.Middleware` itself - see that package's
  `CLAUDE.md` and `work/performance-roadmap-1.0.md`'s 2026-07-15 follow-up entry.
- `KafkaContextConverter` forwards `IBenzeneClientRequest.Headers` onto the outbound
  `Message.Headers` (UTF-8 encoded, matching Confluent.Kafka's `byte[]`-valued headers) so
  header-based decorators (correlation ID, W3C trace context) reach the wire. It also supports an
  optional **message key**: pass `.UseKafka<T>(keyHeader: "x")` (or the converter's `keyHeader` ctor
  arg) and the named header's value becomes `Message.Key` (hash(key) → partition, giving per-key
  ordering/affinity); `null` (the default) sends a keyless message (round-robin, no ordering). The
  context-based `KafkaMessageContextConverter` (the re-produce path) now also forwards headers onto
  `Message.Headers` — previously dropped, silently losing trace context on that path — **except the
  routing-topic key** (`"topic"`), which it deliberately excludes: the outbound produce topic is set
  explicitly (from the topic getter), so an inbound `topic` header (a Benzene SQS/SNS source surfaces
  its routing topic as a header) must not ride onto the produced message and re-route it to the topic
  being consumed. An outbound topic is always explicit, never inherited from the handled message's headers.
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
- **This package IS the Azure "Kafka over Event Hubs" egress** (release plan Tier 2.2/§5.2's Kafka
  decision): Event Hubs exposes a Kafka-protocol endpoint, so `KafkaBenzeneMessageClient`/
  `.UseKafka(...)` work unchanged against it - just point `ProducerConfig.BootstrapServers` at the
  namespace's Kafka endpoint (`<namespace>.servicebus.windows.net:9093`) and configure SASL/OAuth
  (`SetOAuthBearerTokenRefreshHandler` for Managed Identity, matching the consumer-side note above,
  or `SaslMechanism.Plain` with a connection-string-derived token for the non-MI path). No dedicated
  `Benzene.Clients.Azure.Kafka` package was built - it would be a thin, duplicate wrapper around
  exactly this producer. Document the Event Hubs Kafka endpoint setup alongside
  `docs/cookbooks/managed-identity.md`'s existing consumer-side Kafka/Event Hubs guidance, not as new
  code.
- **W3C trace context and invocationId (release plan Tier 3.5).** `.UseW3CTraceContext<KafkaRecordContext<TKey,TValue>>()`
  works: `KafkaMessageHeadersGetter` already read real Confluent.Kafka `Message.Headers`.
  Separately, `UseKafka(...)` now auto-wires `UseBenzeneInvocation<TKey,TValue>()`
  (`KafkaMessage/BenzeneInvocationExtensions.cs`) as the first middleware, so `IBenzeneInvocation`
  resolves inside each record's dispatch (`InvocationId` = `"{topic}-{partition}-{offset}"`,
  `Platform` = `"Worker"`) - a long-running worker has no outer invocation boundary at all, so this
  is the only invocation identity available here. No application code changes needed for either fix.

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
- `IConsumer<TKey,TValue>` needs a live broker to exercise for real, so the consume loop's own
  synchronous startup validation is unit-tested broker-independently (see below), and its live
  behavior is now covered end to end against a real broker (see "Live-broker test coverage"
  below). The concurrency/ordering/drain behavior it relies on is unit-tested in isolation too, via
  `Benzene.SelfHost.BoundedConcurrentDispatcher<T>`'s own test suite
  (`test/Benzene.Core.Test/Hosting/BoundedConcurrentDispatcherTest.cs`).
  `test/Benzene.Core.Test/Kafka/BenzeneKafkaWorkerTest.cs` covers the one part of
  `BenzeneKafkaWorker.StartAsync` that *is* synchronous and broker-independent: the
  `CommitOnlyOnSuccess` startup validation (throws for the two disallowed combinations) and the
  `ConsumerConfig.EnableAutoOffsetStore` wiring, using a `ConsumerConfig` with an unreachable
  `BootstrapServers` - `Build()`/`Subscribe()` don't require a live connection, and the loop's
  `Consume` call honors cancellation without one, so `StartAsync`/`StopAsync` round-trip cleanly in
  a unit test despite spinning up a real `IConsumer`. Everything else in this package that doesn't
  need a broker (getters, converters, `KafkaClientMiddleware`) is unit-tested in
  `test/Benzene.Core.Test/Kafka/KafkaCoreMappersTest.cs` plus
  `test/Benzene.Core.Test/Clients/OutboundHeaderForwardingTest.cs`.
- **Live-broker test coverage** (`test/Benzene.Integration.Test/Kafka/BenzeneKafkaWorkerLiveTest.cs`):
  `BenzeneKafkaWorkerLiveTest.StartAsync_ConsumesRealKafkaMessage_DispatchesThroughPipeline` runs a
  real `BenzeneKafkaWorker<Ignore,string>` (via `InlineSelfHostedStartUp`/`IBenzeneWorkerStartup.UseKafka`,
  hosted through `Benzene.HostedService.BuildHostedService()`) against the same Event Hubs emulator's
  Kafka-compatible endpoint (`localhost:9092`, SASL PLAIN) that
  `Benzene.Azure.Function.Kafka`'s `KafkaConsumerPipelineTest` already proved works in CI - see
  `DockerEmulatorCollection`. It produces a real message with Confluent.Kafka's own `IProducer`,
  waits (via a `TaskCompletionSource` set from a Moq callback, not a fixed delay) for the worker's
  own consume loop to dispatch it through `KafkaApplication`/`.UseMessageHandlers()` to a real
  `IExampleService` mock, then stops the host and asserts the handler ran. This is the first test in
  the repo that exercises `BenzeneKafkaWorker.StartAsync`'s actual `Consume` loop against a live
  broker, closing the gap called out above for everything except the loop's own polling/dispatch
  wiring (still exercised only indirectly through `BoundedConcurrentDispatcher<T>`'s isolated unit
  tests, since asserting ordering/concurrency against a real, timing-sensitive broker would be
  flaky).
