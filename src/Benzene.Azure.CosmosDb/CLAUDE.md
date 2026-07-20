# Benzene.Azure.CosmosDb

## What this package does
Standalone (non-Azure-Functions) Cosmos DB Change Feed consumer for Benzene: a self-hosted worker
(`BenzeneCosmosChangeFeedWorker<TDocument>`) that consumes a container's change feed directly via
the SDK's Change Feed Processor (`Microsoft.Azure.Cosmos`) and runs each delivered batch through a
Benzene **streaming** pipeline. One of the "self-hosted worker" startup modes documented in
`docs/hosting.md`, alongside `BenzeneServiceBusWorker`/`BenzeneEventHubWorker`. For batches
delivered by an Azure Functions `CosmosDBTrigger`, use `Benzene.Azure.Function.CosmosDb` instead —
the two share the same fan-in `StreamContext<TDocument>` pipeline shape, so handlers port between
them unchanged.

## What this adds over the Functions trigger
**Manual checkpoint control.** The trigger checkpoints on successful return and exposes no
checkpoint API; here each batch's `StreamContext<TDocument>` carries a **real** checkpointer
(`CosmosChangeFeedStreamCheckpointer`, internal) wrapping the SDK's batch-level manual checkpoint
hook (`GetChangeFeedProcessorBuilderWithManualCheckpoint`). Still batch-level — the change feed
has no per-document resume token, so `CheckpointAsync(item)` ignores the item and acknowledges the
whole delivered batch (the coarse granularity flagged in `work/azure-roadmap-1.0.md`'s 2026-07-17
evaluation; a handler wanting finer-grained safety does its own within-batch bookkeeping).

## Key types
- `BenzeneCosmosChangeFeedWorker<TDocument> : IBenzeneWorker` — creates the processor from
  `ICosmosChangeFeedProcessorFactory<TDocument>` (passing its change/error delegates — the Cosmos
  builder requires the handler at build time, unlike `EventProcessorClient`'s attach-after events)
  and starts it. No hand-rolled poll loop: the processor owns lease ownership (one lease per
  partition key range, stored in a lease container in Cosmos itself), instance load balancing, and
  in-order batch delivery per lease (leases run concurrently). `StartAsync` starts and returns;
  `StopAsync` waits for in-flight batches. The SDK's start/stop take no cancellation token, so the
  host's tokens are unobserved.
- **Checkpoint/failure semantics** (`BenzeneCosmosChangeFeedConfig`):
  - `AutoCheckpointOnSuccess` (default `true`) — checkpoint after a successful pipeline run in
    which the handler didn't checkpoint itself (no double-checkpoint), matching the Functions
    trigger's behavior. `false` = fully manual; an uncheckpointed batch is redelivered after
    restart/rebalance (the processor still moves forward in-memory within current ownership).
  - `CatchHandlerExceptions` (default `false`) — default lets a pipeline exception reach the
    processor: lease not advanced, same batch redelivered (at-least-once; a reliably-failing batch
    retries forever). `true` = log, **checkpoint anyway**, continue — permanently skips the poison
    batch. **Deliberately the opposite default to `BenzeneEventHubConfig.CatchHandlerExceptions`**:
    Event Hubs has no per-batch redelivery so skipping is its only way to keep going; the change
    feed retries natively.
- `CosmosChangeFeedApplication<TDocument> : StreamMiddlewareApplication<CosmosChangeFeedBatch<TDocument>, TDocument, bool>`
  — maps a batch to one `StreamContext<TDocument>` (checkpointer, cancellation token, lease token
  in `Metadata` under `LeaseTokenMetadataKey` = `"cosmosDb.leaseToken"`), wraps the pipeline in
  `TransportMiddlewarePipeline("cosmos-db")`, and returns whether the handler checkpointed — the
  worker reads that to decide auto-checkpoint. Does **not** catch pipeline exceptions (unlike
  `KinesisStreamApplication`) — the worker owns the skip-vs-retry decision.
- `CosmosChangeFeedBatch<TDocument>` — the raw event: documents, the SDK's batch checkpoint hook,
  lease token, cancellation token.
- `ICosmosChangeFeedProcessorFactory<TDocument>` / `CosmosChangeFeedProcessorFactory<TDocument>` —
  the caller decides monitored container, lease container (must already exist — the processor does
  not create it), processor name (shared across cooperating instances; a different name gets the
  full feed independently), instance name, and authentication (connection string vs Managed
  Identity via the `CosmosClient` they build); an optional `Action<ChangeFeedProcessorBuilder>`
  covers `WithPollInterval`/`WithMaxItems`/`WithStartTime`.
- `Extensions.UseCosmosDbChangeFeed<TDocument>(IBenzeneWorkerStartup, config, factory, action)` —
  the worker wiring, mirroring `UseServiceBus`/`UseEventHub`. **No `AddBenzeneMessage()`/consumer
  registrations and no `UseMessageHandlers()` routing** — changed documents carry no message
  envelope, so the pipeline is a streaming pipeline over the document type (`UseStream(...)`),
  exactly like the trigger adapter.

## All-versions-and-deletes mode (2026-07-20, #30.6)
A parallel path so **deletes and intermediate versions** surface, not just the latest version:
- `BenzeneCosmosAllVersionsChangeFeedWorker<TDocument>` + `UseCosmosDbAllVersionsChangeFeed<TDocument>`
  + `BenzeneCosmosAllVersionsChangeFeedConfig` + `CosmosAllVersionsChangeFeedApplication<TDocument>`
  + `CosmosAllVersionsChangeFeedBatch<TDocument>`. The pipeline streams
  `StreamContext<CosmosChangeFeedItem<TDocument>>` instead of `StreamContext<TDocument>`, where
  `CosmosChangeFeedItem<TDocument>` carries `Current`, `Previous` (when retention captured it), and a
  Benzene-owned `CosmosChangeType` (Create/Replace/Delete) projected from the SDK's
  `ChangeFeedOperationType`. Built via `ICosmosChangeFeedProcessorFactory.CreateAllVersionsAndDeletes`
  (a **default-interface method** that throws `NotSupportedException` — the built-in
  `CosmosChangeFeedProcessorFactory` implements it via
  `GetChangeFeedProcessorBuilderWithAllVersionsAndDeletes`; pre-existing custom factories keep
  compiling but must implement it to use this mode).
- **Why a dedicated worker, not a `CosmosChangeFeedMode` enum on the existing worker** (a deliberate
  deviation from the design doc's §30.6 sketch): the Cosmos SDK 3.62 has **no manual-checkpoint
  all-versions builder** — `GetChangeFeedProcessorBuilderWithAllVersionsAndDeletes` is
  **automatic-checkpoint only** (it checkpoints after the handler returns successfully). That's a
  fundamentally different checkpoint model from the manual-checkpoint `BenzeneCosmosChangeFeedWorker`
  (whose whole reason to exist over the Functions trigger is the per-batch checkpointer). Forcing
  both models through one worker would mean a `StreamContext` whose checkpointer is a no-op in one
  mode and load-bearing in the other, and a different streamed item type per mode — so the honest
  shape is two workers. The all-versions config therefore has **no `AutoCheckpointOnSuccess`** and the
  context uses `NullStreamCheckpointer`; the only knob is `CatchHandlerExceptions` (default `false` =
  rethrow, no checkpoint, batch redelivered/at-least-once; `true` = swallow, checkpoint advances,
  poison batch skipped). Requires the caller to configure container/account retention (otherwise
  deletes/intermediate versions don't surface). Tests:
  `test/Benzene.Core.Test/Azure/CosmosDbWorker/BenzeneCosmosAllVersionsChangeFeedWorkerTest.cs`
  (all-versions processor created + started; change items mapped incl. delete + previous state;
  skip-vs-retry on failure), no live Cosmos (same rationale as the latest-version worker).

## Dependencies
- **Microsoft.Azure.Cosmos** (3.62.0) + **Newtonsoft.Json** (13.0.3, required explicitly by the
  Cosmos SDK's build check; same pin as `Benzene.NewtonsoftJson`) — the only Benzene package
  referencing the Cosmos SDK; `Benzene.Azure.Function.CosmosDb` is deliberately SDK-free.
- **Benzene.Core.MessageHandlers** / **Benzene.Core** — `TransportMiddlewarePipeline`, streaming
  engine (via `Benzene.Core.Middleware`).
- **Benzene.SelfHost** — `IBenzeneWorkerStartup`.

## Tests
- `test/Benzene.Core.Test/Azure/CosmosDbWorker/CosmosChangeFeedApplicationTest.cs` — fan-in
  ordering/single-run, checkpointer wiring + `HasCheckpointed` reporting, lease-token metadata +
  cancellation flow, exception propagation (real pipeline, mocked `ISetCurrentTransport`).
- `test/Benzene.Core.Test/Azure/CosmosDbWorker/BenzeneCosmosChangeFeedWorkerTest.cs` — config
  defaults, start/stop lifecycle, and every auto-checkpoint/skip/retry combination, driven by
  capturing the delegates the worker hands the (mocked abstract) processor factory. The document
  type parameter is a public nested class because Moq must proxy
  `ILogger<BenzeneCosmosChangeFeedWorker<TDocument>>`.
- No live/emulator test: the Cosmos emulator is heavyweight and this sandbox has no Docker; the
  worker's SDK-facing seam is the factory interface, covered by delegate capture above. If a live
  test is added later, follow `Benzene*WorkerLiveTest.cs` in `test/Benzene.Integration.Test/`.

## No egress package — deliberately (release plan §5.2)
There is no `Benzene.Clients.Azure.CosmosDb`. Cosmos DB is a **database, not a transport** — see
`Benzene.Azure.Function.CosmosDb`'s `CLAUDE.md` for the full rationale (same as the Function
trigger: the change feed is a read-side stream, writing is ordinary database access, bring your
own `CosmosClient`).
