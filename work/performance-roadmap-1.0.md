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
