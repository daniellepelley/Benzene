# Benzene.SelfHost

## What this package does
Provides self-hosted application infrastructure for Benzene. Enables running Benzene applications as standalone console apps or Windows services without external web servers. Foundation for testing and lightweight deployments. This is the shared foundation of the "self-hosted worker" startup mode (see `docs/hosting.md`) - `Benzene.Kafka.Core` and `Benzene.RabbitMq` both depend on it (transitively, via `Benzene.HostedService`) for `IBenzeneWorker` and the bounded-concurrency dispatch primitive below.

## Key types/interfaces

### Self-Hosting Infrastructure
- `IBenzeneWorkerStartup` / `WorkerApplicationBuilder` / `CompositeBenzeneWorker` - self-host application builders
- `IBenzeneWorkerBuilder` / `InlineSelfHostedStartUp` - the inline (no dedicated `BenzeneStartUp`
  class) way to build an `IBenzeneWorker`: `.ConfigureServices(...)` + `.Configure(...)` then
  `.Build()`. Used by the worker live integration tests and consumable via
  `Benzene.HostedService`'s `BuildHostedService()`. **`Build()` runs `ConfigureServices` before
  `Configure`**, matching every other host — so a caller's `ConfigureServices` registration wins the
  TryAdd race over anything the `Configure`/`UseMessageHandlers` path registers (it previously ran
  them in the reverse order).
- Standalone application runners
- Console application helpers

### `BoundedConcurrentDispatcher<T>` (`BoundedConcurrentDispatcher.cs`)
Shared, reusable, **unit-tested** primitive both `Benzene.Kafka.Core.BenzeneKafkaWorker<TKey,TValue>`
and `Benzene.RabbitMq.RabbitMqWorker` use to bound how many message/request handlers run at
once, replacing a raw `SemaphoreSlim` + fire-and-forget `.ContinueWith(...)` pattern both used to
duplicate independently. Built on `System.Threading.Channels` (BCL, no new NuGet dependency) - not
`System.Threading.Tasks.Dataflow`/`ActionBlock<T>`, which would have required one.
- Runs `laneCount` independent lanes, each a single-consumer `Channel<T>` (bounded capacity 1, so
  `EnqueueAsync` gives the caller real backpressure) with one dedicated consumer `Task`.
- Optional `keySelector`: items sharing a key always route to the same lane, so that lane's
  strictly-FIFO consumer preserves order for that key (e.g. a Kafka partition) while different keys
  still run concurrently. With no `keySelector`, items round-robin across lanes with no ordering
  promise.
- A fault thrown by the handler is always logged per item (via the injected `ILogger`). By default
  (`catchExceptions: true`, the default) it's then swallowed - it never stops that lane or goes
  unobserved, unlike the pattern it replaces. With `catchExceptions: false`, the fault is instead
  rethrown after logging (and after invoking the optional `onFault` callback) - this ends that
  lane's consume loop. `Benzene.Kafka.Core.BenzeneKafkaConfig.CatchHandlerExceptions` is the first
  caller-facing toggle for this (default `true`, preserving prior behavior exactly); it wires
  `onFault` to stop the whole worker, since a dead lane's channel otherwise silently deadlocks
  `EnqueueAsync` for that key once it fills.
- `DrainAsync(timeout)` completes every lane's writer and awaits all consumer tasks up to the
  timeout - the mechanism that makes `StopAsync` on both workers actually graceful now, instead of
  abandoning in-flight work.
- `DrainLanesAsync(laneKeys, timeout)` (2026-07-20) quiesces only the lanes the given keys route to
  (via `LaneForKey`, the same `key % laneCount` mapping `EnqueueAsync` uses), waiting until they have
  no item queued or in flight - **without** completing them, so those lanes keep consuming afterward.
  Unlike `DrainAsync` (a terminal, all-lane shutdown drain), this is a partial, reusable quiesce used
  by `BenzeneKafkaWorker`'s consumer-group rebalance handler to settle a revoked partition's lane
  before its offset is committed. Backed by a per-lane outstanding-item counter incremented in
  `EnqueueAsync` and decremented in each lane's consume loop `finally` (so it un-counts on success,
  swallowed fault, and rethrow alike). `LaneCount`/`LaneForKey` are exposed for callers that map
  domain keys (Kafka partitions) to lanes. Covered by the `DrainLanesAsync_*` tests in
  `BoundedConcurrentDispatcherTest`.
- Fully unit-tested in isolation with fake items/handlers (bounded concurrency, per-key ordering,
  round-robin, exception isolation, drain-with-timeout) -
  `test/Benzene.Core.Test/Hosting/BoundedConcurrentDispatcherTest.cs`. `BenzeneKafkaWorker` has no
  direct unit test of its own (it needs a live broker), so this is where the actual concurrency
  correctness is verified.

## When to use this package
- When running Benzene apps as console applications
- When building Windows services with Benzene
- For integration testing without external dependencies
- For lightweight microservices that don't need full web server
- `BoundedConcurrentDispatcher<T>` specifically: any self-hosted worker that polls for work and
  needs to bound how many items it processes concurrently

## Dependencies on other Benzene packages
- **Benzene.Abstractions.Pipelines** - pipeline/hosting abstractions (`IBenzeneWorker`, `IRegisterDependency`)
- **Benzene.Core** / **Benzene.Core.Middleware** - middleware pipeline implementation
- **Benzene.Microsoft.Dependencies** - the MEL DI adapter used by `WorkerApplicationBuilder`
- **Benzene.HealthChecks** / **Benzene.Http** - health-check + HTTP abstractions used by workers

## Important conventions
- Self-hosted apps use Benzene's DI container directly
- Suitable for message-based workloads (queues, events)
- Typically used for testing or lightweight deployments
