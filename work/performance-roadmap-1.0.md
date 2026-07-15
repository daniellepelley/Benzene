# Benzene Performance & Reliability — Roadmap

**Document Version:** 1.0
**Last Updated:** 2026-07-15
**Owner:** Performance Champion
**Status:** First entry — startup-mode documentation + self-hosted worker concurrency rewrite

This is the first entry in the performance/reliability workstream — no prior `work/performance-*`
document existed. It follows the same convention as the other `work/*-roadmap-1.0.md` documents:
verified current state, a phased plan, explicitly flagged open questions, and an honest scope
boundary.

## 1. Purpose

Benzene supports several fundamentally different ways a `BenzeneStartUp` gets run, but the
conceptual distinction between them had never been named on its own terms, and the one mode where
Benzene owns its own concurrency (the self-hosted worker) had real, unaddressed reliability gaps in
its dispatch mechanism. This document records both: the startup-mode documentation, and the
concurrency rewrite it motivated.

## 2. Current state (verified against actual code before this pass)

Benzene's hosts fall into three execution models, now named explicitly in `docs/hosting.md`'s new
"Three ways Benzene starts" section:

1. **Triggered (serverless)** — AWS Lambda, Azure Functions. No Benzene-owned process; the platform
   invokes per event.
2. **Embedded in an existing host** — ASP.NET Core (and gRPC-on-ASP.NET-Core). Kestrel owns the
   process and its own concurrency; Benzene is just middleware.
3. **Self-hosted worker** — `Benzene.HostedService` + `Benzene.SelfHost.Http`/`Benzene.Kafka.Core`.
   Benzene owns a long-running poll loop and is responsible for keeping the process alive. This is
   the only mode where Benzene's own concurrency choices matter.

Before this pass, mode 3's two built-in workers (`BenzeneKafkaWorker<TKey,TValue>`,
`BenzeneHttpWorker`) both used an identical, independently-duplicated pattern: a raw
`SemaphoreSlim(ConcurrentRequests)` gating the poll loop, then fire-and-forget dispatch via
`HandleAsync(...).ContinueWith(_ => semaphore.Release())`. Verified, concrete problems with that
pattern:

- **Faults were silently swallowed.** The `.ContinueWith(...)` continuation never inspected
  `.Exception`; a faulting handler simply vanished with no log line. The bare `catch (Exception ex)`
  around the poll/dispatch call in both workers didn't log anything either — not even the
  `Console.WriteLine` the Kafka-specific `ConsumeException` branch had.
- **`StopAsync` was not graceful.** Both workers' `StopAsync` just closed the consumer/listener
  immediately — in-flight handler calls were abandoned mid-flight, not drained.
- **`StartAsync` never returned until cancellation**, which is incorrect `IHostedService` semantics
  (`StartAsync` should do just enough to kick off background work and return promptly) and would
  have prevented a *second* `IHostedService` registered after this one from ever starting, in any
  application registering more than one.
- **Neither worker has any direct test coverage.** `IConsumer<TKey,TValue>` and `HttpListener` both
  need a live broker/port to exercise for real — confirmed no `test/Benzene.Kafka.Core.Test` project
  exists at all, and `Benzene.SelfHost.Http`'s own (pre-existing) `CLAUDE.md` already documented the
  same gap for `HttpListenerContext`.
- **Kafka ordering was never guaranteed.** Two messages from the same partition could complete out
  of order under the old bounded-parallel dispatch, even though per-partition order is what Kafka
  consumers conventionally guarantee (partitions are Kafka's own unit of parallelism; order is only
  ever promised within one).

Research (via the `performance-champion` agent, reading actual source rather than assuming)
confirmed `System.Threading.Channels` is already part of the BCL for the `net10.0` TFN every
relevant project targets — no new NuGet dependency. `System.Threading.Tasks.Dataflow`
(`ActionBlock<T>`) would have required a **new** package for no material benefit here, which is why
it wasn't chosen.

Separately (verified, but **explicitly out of scope for this pass**): the *serverless* batch
adapters already run **unbounded** `Task.WhenAll` parallelism over an entire batch, with no cap at
all —

- `Benzene.Aws.Lambda.Sqs.SqsApplication.HandleAsync` (`src/Benzene.Aws.Lambda.Sqs/SqsApplication.cs:40-74`)
- `Benzene.Azure.Function.EventHub/Kafka/ServiceBus`'s shared `MiddlewareMultiApplication`
  (`src/Benzene.Core.Middleware/MiddlewareMultiApplication.cs:30-41,66-76`)

This is a real, separate reliability gap (a large batch could spike concurrency well past what a
downstream dependency can handle), but it's architecturally a different problem shape — a
short-lived, already-finite batch that must fully resolve before the invocation returns, versus an
indefinite polling stream. Flagged below as a named follow-up, not bundled into this pass.

## 3. What this pass built

- **`Benzene.SelfHost.BoundedConcurrentDispatcher<T>`** (`src/Benzene.SelfHost/BoundedConcurrentDispatcher.cs`) —
  a shared, reusable, unit-tested primitive on `System.Threading.Channels`. `laneCount` independent
  lanes, each a single-consumer `Channel<T>` (bounded capacity 1, so `EnqueueAsync` gives the poll
  loop real backpressure) with one dedicated consumer task. An optional `keySelector` routes
  same-key items to the same lane, preserving per-key order (used for Kafka partitions); with no
  key selector, items round-robin across lanes (used for HTTP requests, which have no natural
  ordering key). A fault in the handler is caught and logged per item, never stopping the lane or
  going unobserved. `DrainAsync(timeout)` completes every lane and awaits all consumer tasks up to
  the timeout.
- Fully unit-tested in isolation, with fake items/handlers, no live broker/listener needed —
  `test/Benzene.Core.Test/Hosting/BoundedConcurrentDispatcherTest.cs`: bounded concurrency never
  exceeds `laneCount`; same-key items complete in enqueue order despite deliberately-adverse handler
  delays; different keys run concurrently; an exception in one item is logged and doesn't stop the
  lane; `DrainAsync` waits for in-flight work; `DrainAsync` still returns once its timeout elapses
  rather than waiting forever.
- **`BenzeneKafkaWorker<TKey,TValue>`** rewired onto the dispatcher. `BenzeneKafkaConfig` gains
  `PreserveOrderPerPartition` (default `true` — per the product decision below) and `DrainTimeout`
  (default 30s). `StartAsync` now returns immediately (spawns the loop, doesn't await it) and
  `StopAsync` signals a dedicated `CancellationTokenSource`, then awaits the loop's own drain +
  consumer close — this is what actually fixes the "`StartAsync` never returns" gap, as a direct,
  necessary consequence of making `StopAsync` genuinely await a real drain (not a separate,
  independent fix bolted on afterward).
- **`BenzeneHttpWorker`** rewired the same way (no ordering key — round-robin), with the same
  `StartAsync`/`StopAsync` shape. `BenzeneHttpConfig` gains `DrainTimeout`.
- `docs/hosting.md` — new "Three ways Benzene starts" section, plus a "Worker concurrency"
  subsection under the worker host documenting `ConcurrentRequests`/`PreserveOrderPerPartition`/
  `DrainTimeout`.
- `src/Benzene.Kafka.Core/CLAUDE.md`, `src/Benzene.SelfHost.Http/CLAUDE.md`,
  `src/Benzene.SelfHost/CLAUDE.md` updated to name the real types and behavior (these were flagged
  as generic boilerplate by the recent DX audit's Finding #6 — fixed here as a direct side effect,
  not a separate sweep of the other 60+ package `CLAUDE.md` files, which remains that audit's own
  open follow-up).

## 4. Product decision made this pass

**Should Kafka processing preserve per-partition order by default?** Yes — confirmed this is
standard, expected Kafka consumer behavior (order is only ever promised within a partition, and
most real-world Kafka consumer patterns process a given partition sequentially while parallelizing
across partitions), so `PreserveOrderPerPartition` defaults to `true` for least-surprise, but is
configurable to `false` for throughput-first use cases that don't need ordering.

## 5. Phased plan

**Phase 1 (this pass, complete):** Startup-mode documentation; `BoundedConcurrentDispatcher<T>` +
its test suite; `BenzeneKafkaWorker`/`BenzeneHttpWorker` rewired onto it; `CLAUDE.md` updates.

**Phase 2 (recommended next):** Cap the serverless batch adapters' unbounded `Task.WhenAll`
(SQS in `Benzene.Aws.Lambda.Sqs`; Event Hubs/Kafka/Service Bus in the `Benzene.Azure.Function.*`
packages) — likely reusing `BoundedConcurrentDispatcher<T>` (or a simpler bounded-`Task.WhenAll`
helper, since a finite batch has no drain/shutdown lifecycle to manage) with a new configurable cap.
Route to whoever owns those adapters (`aws-product-owner`/`azure-product-owner`) — this pass
deliberately didn't touch them, since a short-lived finite batch and an indefinite polling stream
are different enough problem shapes to warrant an independent design pass.

**Phase 3 (recommended, smaller):** Now that `Benzene.Benchmarks` exists
(`benchmarks/Benzene.Benchmarks`, added independently on `main` alongside this work), add a
benchmark for `BoundedConcurrentDispatcher<T>` itself — throughput/latency under varying
`laneCount`/handler-cost combinations — so future changes to it have a measured baseline instead of
reasoning from code inspection alone.

## 6. Open questions

1. **Should the serverless batch adapters share `BoundedConcurrentDispatcher<T>`, or get their own,
   simpler bounded-parallel helper?** A finite batch has no drain/shutdown lifecycle to manage
   (the invocation itself is the lifetime boundary), so reusing the full dispatcher might be more
   machinery than needed — a decision for whoever picks up Phase 2.
2. **Is a benchmark for the dispatcher (Phase 3) worth prioritizing before Phase 2**, or should the
   batch-adapter reliability gap take priority since it's a live, unbounded-concurrency risk in
   production traffic today? Not decided here — a resourcing call, not an engineering one.

## 7. What this document does not cover

- The serverless batch adapters' unbounded `Task.WhenAll` (Finding in §2, tracked as Phase 2, not
  fixed in this pass).
- `IHostedService`-level composition beyond the two workers touched here (e.g. whether
  `CompositeBenzeneWorker`/`BenzeneHostedServiceAdapter` need further changes now that
  `BenzeneKafkaWorker`/`BenzeneHttpWorker`'s `StartAsync` return promptly) — read and confirmed
  compatible (`CompositeBenzeneWorker.StartAsync` already just fans out via `Task.WhenAll`, which
  now also returns promptly as a consequence — a behavior change worth being aware of, not a
  correctness problem), but not independently audited further.
- A full sweep of every other package's `CLAUDE.md` for the same generic-boilerplate pattern —
  that's `work/dx-roadmap-1.0.md`'s Finding #6, not this document's job.
- Benchmark numbers for the new dispatcher (Phase 3, not yet built) — no throughput/latency claim in
  this document should be read as measured; it's reasoned from the design and the unit tests' pass/
  fail behavior, not a load test.

## 8. 2026-07-15 follow-up: rest-of-package audit of `Benzene.Kafka.Core`

A second pass over the rest of `src/Benzene.Kafka.Core/` (producer/client side, consumer-side
getters, DI wiring) and a fresh, skeptical re-review of `BenzeneKafkaWorker`/
`BoundedConcurrentDispatcher<T>` from §3, done by the performance-champion agent, not re-litigating
§2-§7 above. All findings verified by reading the actual code (and, where noted, confirmed by
building/testing), not assumed.

### Highest-priority finding - NOT fixed here, needs `core-product-owner`

**`Benzene.Core.Middleware.MiddlewareApplication<TEvent,TContext>`/`<TEvent,TContext,TResult>`
(`src/Benzene.Core.Middleware/MiddlewareApplication.cs:28-33,56-60`) create a DI scope per
event/message via `serviceResolverFactory.CreateScope()` and never dispose it.** `IServiceResolver`
is `IDisposable` (`src/Benzene.Abstractions/DI/IServiceResolver.cs`), and `MiddlewarePipeline<TContext>.HandleAsync`
(`src/Benzene.Core.Middleware/MiddlewarePipeline.cs:33`) takes the resolver as a parameter and never
disposes it either - by design, every other call site of `CreateScope()` in the codebase (`SqsApplication`,
`MiddlewareMultiApplication`, `DynamoDbApplication`, `Benzene.Grpc`'s `GrpcMethodHandler`, both
`Kafka.Core`'s and `SelfHost.Http`'s own `Extensions.cs`, `AwsLambdaEntryPoint`) wraps it in
`using`. `MiddlewareApplication<TEvent,TContext>` is the one exception, and `KafkaApplication<TKey,TValue>`
(`src/Benzene.Kafka.Core/KafkaMessage/KafkaApplication.cs`) inherits from exactly that overload - so
every single Kafka message processed by `BenzeneKafkaWorker` leaks a DI scope (and any scoped
`IDisposable` resolved inside it - DbContext, per-scope HttpClient handler, etc.) forever, in a
process that by definition runs indefinitely (the self-hosted worker mode this whole document is
about). This predates this session's dispatcher rework - it is not a regression introduced by it -
but it's a live, standing leak today, and the impact is directly proportional to message throughput.
Blast radius is much wider than Kafka: every AWS Lambda handler, Azure Function adapter, ASP.NET
Core's `AspNetApplication`, `BenzeneMessageApplication`, and `Benzene.SelfHost.Http`'s
`HttpListenerApplication` all go through the same two `MiddlewareApplication<...>` overloads.
Given the blast radius and that the fix lives in `Benzene.Core.Middleware` (core-product-owner's
file), this is flagged, not patched, here - recommend an urgent, carefully-tested fix (wrap in
`using`/`try`-`finally`, verified across a representative sample of the adapters listed above, not
just Kafka) rather than a unilateral one-line change from this review.

### Fixed directly in this pass (rebuilt `Benzene.Kafka.Core.csproj`, 0 warnings from this package,
### all affected tests passing)

- **`BenzeneKafkaWorker<TKey,TValue>`'s `ConsumeException` retry loop had no backoff** (flagged as a
  known caveat in §3, not fixed there) - confirmed real: a persistently failing broker/connection
  (not just one bad message) would spin `Consume`/catch/retry as fast as it can fail, burning CPU
  and spamming the log. Fixed with a new `BenzeneKafkaConfig.ConsumeExceptionRetryDelay` (default
  1s), awaited with the loop's own cancellation token so shutdown stays responsive during the delay.
- **The consumer was `Close()`d but never `Dispose()`d** - `IConsumer<TKey,TValue>` is `IDisposable`;
  `Close()` alone unsubscribes/commits offsets but doesn't release the underlying native librdkafka
  handle. Fixed: `_consumer.Dispose()` now runs alongside `Close()`.
- **Only `ConsumeException`/`OperationCanceledException` were caught** - any other exception (a
  fatal, non-`ConsumeException` `KafkaException`, or a `ConsumerBuilder`/`Subscribe`/dispatcher
  construction failure, none of which were inside the original `try`) would fault `_runTask` with no
  logging at all and skip drain/close entirely - worse than the old bare `catch (Exception)` this
  session's rework replaced. Fixed: the whole loop body is now wrapped in `try`/`catch(Exception)`
  (logs at `Critical`)/`finally` (always drains + closes + disposes the consumer, guarded against a
  `null` consumer/dispatcher if setup itself failed).
- **`KafkaMessageContextConverter<TContext>.CreateRequestAsync`'s 2 pre-existing nullable warnings**
  (`Kafka/KafkaMessageContextConverter.cs:32,35`) - `IMessageTopicGetter<TContext>.GetTopic` can
  legitimately return `null`; that was being dereferenced unchecked, so a topic-less context would
  NRE with no useful message. Fixed with an explicit `InvalidOperationException` instead. The other
  warning (assigning `IMessageBodyGetter.GetBody`'s nullable `string?` into `Message<string,string>.Value`)
  is suppressed with `!`, not worked around, since a `null` value is legitimate Kafka semantics
  (a tombstone record on a compacted topic) - this is a real value, not a bug.
- **`BenzeneKafkaConfig.ConsumerConfig`/`Topics`' 2 pre-existing non-nullable-without-default
  warnings** - both are always set at construction in every real usage (`docs/getting-started-kafka.md`,
  `examples/Kafka/Benzene.Examples.Kakfa`); marked `required` instead of leaving them silently
  nullable-unsafe.
- **`KafkaBenzeneMessageClient.SendMessageAsync` allocated `new KafkaContextConverter<TRequest>(new JsonSerializer())`
  on every single send** - `JsonSerializer()` constructs a fresh `JsonSerializerOptions` per call,
  which defeats System.Text.Json's per-`JsonSerializerOptions` converter/metadata cache on every
  outbound message, not just a small allocation. Fixed: a single shared `static readonly ISerializer`
  is reused across calls (System.Text.Json's serializer is documented thread-safe once its options
  aren't being mutated). The `KafkaContextConverter<TRequest>` wrapper object itself is still
  allocated per call (it's generic over the per-call `TRequest`, so it can't be cached the same way)
  - a smaller, lower-priority follow-up, not fixed here.
- **Test coverage gap**: none of `Benzene.Kafka.Core`'s pure getters/converters/middleware
  (`KafkaMessageBodyGetter`, `KafkaMessageHeadersGetter`, `KafkaMessageTopicGetter`,
  `KafkaMessageHandlerResultSetter`, `KafkaSendMessageBodyGetter`/`HeadersGetter`/`TopicGetter`,
  `KafkaMessageContextConverter`, `KafkaClientMiddleware`) had any unit test, despite none of them
  needing a live broker (unlike `BenzeneKafkaWorker` itself, or the existing
  `test/Benzene.Integration.Test/Kafka/KafkaConsumerPipelineTest.cs`, which does). Added
  `test/Benzene.Core.Test/Kafka/KafkaCoreMappersTest.cs` (12 tests, all passing) covering all of the
  above, including the new topic-getter-returns-null guard above.

### Checked, confirmed not a bug

- `KafkaClientMiddleware`/`KafkaBenzeneMessageClient` don't dispose the `IProducer<string,string>`
  they're given - correct, since it's caller-provided/injected (`docs/clients.md:315`,
  `docs/getting-started-kafka.md:191`), not owned or constructed by this package; ownership and
  disposal are the registering application's responsibility, same as any other injected dependency.
- `IMiddleware<TContext>.HandleAsync` (and so `KafkaClientMiddleware.HandleAsync`) has no
  `CancellationToken` parameter to thread through to `producer.ProduceAsync(...)` - confirmed this is
  the shared middleware interface's shape (`Benzene.Abstractions.Middleware`), not something
  `Benzene.Kafka.Core` can unilaterally add; `ProduceAsync`'s effective timeout today is entirely
  governed by the caller's `ProducerConfig.MessageTimeoutMs` (Confluent.Kafka default 300000ms/5min).
  Worth a documentation note (an explicit `MessageTimeoutMs` recommendation in
  `docs/getting-started-kafka.md`) rather than a code change here.
- DI registrations in `Kafka/DependencyInjectionExtensions.cs`/`DependencyInjectionExtensions.cs` are
  all `Scoped`, matching every other transport's convention - no unnecessary singleton/transient
  churn found.

### Filed, not fixed (follow-ups)

1. ~~**`MiddlewareApplication<TEvent,TContext>` DI scope leak** (above) - route to `core-product-owner`.~~
   **Fixed - see §9 below.**
2. **`KafkaContextConverter<TRequest>` per-send allocation** - minor, not fixed (see above).
3. **`docs/getting-started-kafka.md` `MessageTimeoutMs` guidance** - minor documentation gap, not a
   code change.

## 9. 2026-07-15 follow-up: fixed the `MiddlewareApplication<TEvent,TContext>` DI scope leak

The highest-priority finding from §8 - `Benzene.Core.Middleware.MiddlewareApplication<TEvent,TContext>`/
`<TEvent,TContext,TResult>` (`src/Benzene.Core.Middleware/MiddlewareApplication.cs`) creating a DI
scope per event via `serviceResolverFactory.CreateScope()` and never disposing it - is now fixed:
both `HandleAsync` overloads wrap the created scope in `using`, so it's disposed once the pipeline
(and, for the `TResult` overload, `resultMapper`) finishes.

**Blast radius, confirmed by reading every concrete usage** (not assumed): `MiddlewareApplication<...>`
is the base class for `Benzene.AspNet.Core.AspNetApplication` (mainstream ASP.NET Core HTTP -
likely the single highest-traffic path in the framework), `Benzene.Azure.Function.AspNet.AspNetApplication`
(Azure Functions isolated-worker HTTP trigger), `Benzene.Kafka.Core.KafkaMessage.KafkaApplication<TKey,TValue>`
(the Kafka worker rewired in §3), `Benzene.SelfHost.Http.HttpListenerApplication`, and
`Benzene.Core.MessageHandlers.BenzeneMessage.BenzeneMessageApplication` (used by AWS Lambda's
`DirectMessageLambdaHandler` and Azure Event Hub's `BenzeneMessageEventHubHandler`).
`MiddlewareMultiApplication<...>` (the batch-oriented sibling used by SQS/Event Hub/Kafka/Service
Bus triggers) already disposed its per-record scopes correctly via `using` and was not affected.

**Safety of disposing immediately verified per call site**, not assumed: `Benzene.AspNet.Core.AspNetApplication`
and `Benzene.SelfHost.Http.HttpListenerApplication` both write directly into a live, externally-owned
response object (`HttpContext.Response`, `HttpListenerContext.Response`) *during* `pipeline.HandleAsync`,
so nothing needs the scope once it returns. `Benzene.Azure.Function.AspNet.AspNetApplication`'s
`resultMapper` (`context => context.ContentResult`) and `BenzeneMessageApplication`'s
(`context => context.BenzeneMessageResponse`) both extract a plain, already-fully-populated data DTO
(`Microsoft.AspNetCore.Mvc.ContentResult`, `IBenzeneMessageResponse` - just `StatusCode`/`Headers`/`Body`
strings) that holds no live reference to a Benzene-scoped service, so disposing the scope right as
`HandleAsync` returns can't invalidate anything the caller still needs.

This pairs with an earlier fix (already on `main` before this entry - see
`test/Benzene.Core.Test/Core/Core/DI/ServiceResolverScopeDisposalTest.cs`) to
`MicrosoftServiceResolverAdapter`/`AutofacServiceResolverAdapter`, whose own `Dispose()` used to be a
no-op: that fix ensured disposing a scope actually works; this one ensures it actually gets called.
Together they close the leak end to end.

**Verification**: new regression tests in
`test/Benzene.Core.Test/Core/Middleware/MiddlewareApplicationScopeDisposalTest.cs` (both `HandleAsync`
overloads, using a fake pipeline resolving a tracked `IDisposable` and asserting it's disposed after
`HandleAsync` returns) - confirmed these fail against the pre-fix code by temporarily reverting the
fix and re-running them, not just written and assumed correct. Full `Benzene.sln` test suite (925/928
in `Benzene.Test.dll`, plus Conformance/Mesh/Grpc all green) and `Benzene.Examples.sln` both pass with
no regressions; `examples/Google`'s 11 end-to-end tests (which dispatch real HTTP requests through
`AspNetApplication` via `GoogleCloudFunctionApplicationBuilder`) also pass, giving genuine
end-to-end confidence beyond the unit-level regression test alone.
