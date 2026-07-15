# Benzene.SelfHost

## What this package does
Provides self-hosted application infrastructure for Benzene. Enables running Benzene applications as standalone console apps or Windows services without external web servers. Foundation for testing and lightweight deployments. This is the shared foundation of the "self-hosted worker" startup mode (see `docs/hosting.md`) - `Benzene.Kafka.Core` and `Benzene.SelfHost.Http` both depend on it (transitively, via `Benzene.HostedService`) for `IBenzeneWorker` and the bounded-concurrency dispatch primitive below.

## Key types/interfaces

### Self-Hosting Infrastructure
- `IBenzeneWorkerStartup` / `WorkerApplicationBuilder` / `CompositeBenzeneWorker` - self-host application builders
- Standalone application runners
- Console application helpers

### `BoundedConcurrentDispatcher<T>` (`BoundedConcurrentDispatcher.cs`)
Shared, reusable, **unit-tested** primitive both `Benzene.Kafka.Core.BenzeneKafkaWorker<TKey,TValue>`
and `Benzene.SelfHost.Http.BenzeneHttpWorker` use to bound how many message/request handlers run at
once, replacing a raw `SemaphoreSlim` + fire-and-forget `.ContinueWith(...)` pattern both used to
duplicate independently. Built on `System.Threading.Channels` (BCL, no new NuGet dependency) - not
`System.Threading.Tasks.Dataflow`/`ActionBlock<T>`, which would have required one.
- Runs `laneCount` independent lanes, each a single-consumer `Channel<T>` (bounded capacity 1, so
  `EnqueueAsync` gives the caller real backpressure) with one dedicated consumer `Task`.
- Optional `keySelector`: items sharing a key always route to the same lane, so that lane's
  strictly-FIFO consumer preserves order for that key (e.g. a Kafka partition) while different keys
  still run concurrently. With no `keySelector`, items round-robin across lanes with no ordering
  promise.
- A fault thrown by the handler is caught and logged per item (via the injected `ILogger`) - it
  never stops that lane or goes unobserved, unlike the pattern it replaces.
- `DrainAsync(timeout)` completes every lane's writer and awaits all consumer tasks up to the
  timeout - the mechanism that makes `StopAsync` on both workers actually graceful now, instead of
  abandoning in-flight work.
- Fully unit-tested in isolation with fake items/handlers (bounded concurrency, per-key ordering,
  round-robin, exception isolation, drain-with-timeout) -
  `test/Benzene.Core.Test/Hosting/BoundedConcurrentDispatcherTest.cs`. Neither
  `BenzeneKafkaWorker` nor `BenzeneHttpWorker` has direct test coverage of its own (both need a
  live broker/listener), so this is where the actual concurrency correctness is verified.

## When to use this package
- When running Benzene apps as console applications
- When building Windows services with Benzene
- For integration testing without external dependencies
- For lightweight microservices that don't need full web server
- `BoundedConcurrentDispatcher<T>` specifically: any self-hosted worker that polls for work and
  needs to bound how many items it processes concurrently

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.Middleware** - Middleware implementations

## Important conventions
- Self-hosted apps use Benzene's DI container directly
- Suitable for message-based workloads (queues, events)
- Can be combined with SelfHost.Http for HTTP endpoints
- Typically used for testing or lightweight deployments
