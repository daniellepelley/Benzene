# Outstanding bugs requiring action — consolidated triage (2026-07-21)

> **Action status update (2026-07-21, later pass).** Tier 1 has been worked through and pushed to
> `main`. Actioned with tests: #1 (`Utils.GetTypes` keeps loadable types, 3 copies), #2
> (`OutboundSnsContextConverter` FIFO/numeric parity), #3 (`ActivityProcessTimer`/`UseTimer` marks a
> failed span), #8 (X-Ray segment try/finally in the Lambda test host), #9-part (`SqsConsumerMessageTopicGetter`
> null-`MessageAttributes` guard), #10 (`BenzeneHttpWorker` catch-all + finally), #6-part (gRPC
> `[EnumeratorCancellation]` on `ReadAll`/`Convert`). Already fixed in current source: #5
> (`HttpRequest` properties now have initializers). **Deferred with rationale (not release-blocking as
> mechanical fixes):** #4 `MiddlewareRouter` needs a `where TRequest : class` **public-API constraint** —
> a source-breaking change to hold for the 1.0 API-freeze decision, not applied unilaterally; #6-second-clause
> ("OK unary response with no payload → Unknown") not reproduced in current source (`status` comes from
> `BenzeneResult.Status`, which is `"Ok"` for an OK result — only a *null* `MessageHandlerResult` yields
> `"Unknown"`); #7 Cosmos `MapChangeType` unknown-op→`Replace` is latent and safe against the shipped SDK
> (a "fix" means either a risky throw in the change-feed hot path or a new `CosmosChangeType.Unknown`
> enum value — an API/behaviour decision); #9a `SnsMessageBodyGetter` returns a non-null `string`, so a
> `?.` would weaken its contract and isn't production-reachable; #11 the `Debug.WriteLine` leftovers are
> `[Conditional("DEBUG")]` and compiled out of the Release NuGet (cosmetic; dead-reflection-code removal
> is cleanup, not a bug); #12 `AsRawHttpRequest` CR/LF is on dead code with no caller. Tier 2/Tier 3
> remain maintainer decisions / perf-hygiene as noted below.


Synthesised from every bug-tracking doc under `work/` (`bughunt/findings.md`, `arch-review/findings.md`,
`cloud-review/*.md`, `overnight-bug-hunt-findings.md`, `audit-remaining-suggestions.md`,
`debuggability-assessment.md`, `health-checks-1.0-review.md`, `designs/*.md`, and this session's
`overnight-fixes-log.md`), each candidate **re-verified against current source** and cross-referenced with
git history. The large majority of documented findings are already **FIXED** (by the #29/#30 review series,
the overnight-fixes series, and Cycles 1–31) and are excluded here. Pure **missing-feature/roadmap** items
(SQS-FIFO-consume, gRPC streaming, Service Bus transactions, Kafka EOS, tumbling windows, blob Stream
binding, etc.) are also excluded — this list is **bugs**, not capabilities.

Legend: **[T1]** clear correctness/robustness bug with an obvious fix → just fix it. **[T2]** real issue but
the fix is a behavioural/API/policy decision → needs a maintainer's call first. **[T3]** low/cosmetic/deferred
or perf-hygiene. Each item cites the file and the source doc(s).

---

## Tier 1 — Confirmed correctness/robustness bugs (clear fix, action = fix)

| # | Sev | Bug | Location | Source |
|---|-----|-----|----------|--------|
| 1 | LOW-MED | **`Utils.GetTypes` swallows `ReflectionTypeLoadException`** — bare `catch { return Type.EmptyTypes; }` drops **every** type in an assembly if one fails to load, so all handlers in a partially-loadable assembly become undiscoverable (silent 404s). The sibling `ValidateOutboundRoutingExtensions` already uses the correct `ex.Types.Where(t => t != null)` pattern. **3 copies.** | `Benzene.Core/Helper/Utils.cs:48`, `Benzene.Core.MessageHandlers/Utils.cs:53`, `Benzene.Core.MessageHandlers/Helper/Utils.cs:53` | overnight-log, arch-review |
| 2 | LOW-MED | **OutboundSnsContextConverter drift** — hardcodes `DataType = "String"` for every attribute and has none of the FIFO group/dedup or numeric-type logic (or the 10-attribute cap) that `SnsContextConverter` got. So the `OutboundContext` (`AddOutboundRouting`) SNS path silently can't do FIFO or numeric filter policies. | `Benzene.Clients.Aws.Sns/OutboundSnsContextConverter.cs:77,86` | cloud-review, arch-review(T2) |
| 3 | LOW | **`ActivityProcessTimer` never marks a span failed on throw** — a span opened by `UseTimer("name")` whose work throws shows as *successful* in Jaeger/Tempo (the sibling `ActivityMiddlewareDecorator` was fixed to `SetStatus(Error)`; the timer wasn't, and its `Dispose()` can't observe the exception — needs a try/catch in the `UseTimer` wrapper). | `Benzene.Diagnostics/Timers/ActivityProcessTimer.cs:24` | debuggability, audit |
| 4 | LOW | **`MiddlewareRouter`/`MiddlewareRouter` value-type request null-check is always false** — `request == null` on an unconstrained `TRequest` means a value-type request can never route. Latent (no in-repo value-type router); a clean fix needs a `where TRequest : class` public-API constraint. | `Benzene.Core.Middleware/MiddlewareRouter.cs:34` | arch-review, bughunt |
| 5 | LOW | **`HttpRequest` public API violates its non-null contract** — `Method`/`Path`/`Headers` are non-nullable `get;set;` with no initializer, so a default-constructed instance holds nulls; a hand-built `HttpRequest` (tests/adapters) NREs downstream. | `Benzene.Http/HttpRequest.cs:16,21,26` | arch-review |
| 6 | LOW | **gRPC streaming helpers missing `[EnumeratorCancellation]`**; an OK unary response with no payload maps to `Unknown`. | `Benzene.Grpc` (`ReadAll`/`Convert`) | bughunt |
| 7 | LOW | **Cosmos `MapChangeType` defaults an unknown op to `Replace`** — a future SDK change-feed op type is silently mislabelled (latent; safe against today's SDK). | `Benzene.Azure.Function.CosmosDb` / cosmos worker | bughunt |
| 8 | LOW | **`AwsLambdaBenzeneTestHost.SendEventAsync` leaks an X-Ray segment on the exception path** — `BeginSegment`/`EndSegment` with no `try/finally`, so a throwing handler leaves the process-global recorder segment open → later `.BuildHost()` tests stack on it (order-dependent, can throw under `ContextMissingStrategy.RUNTIME_ERROR`). | `Benzene.Tools/Aws/AwsLambdaBenzeneTestHost.cs:44` | overnight-log |
| 9 | LOW | **Un-hardened null-guard stragglers** — `SnsMessageBodyGetter` dereferences `SnsRecord.Sns` and `SqsMessageTopicMapper` dereferences `MessageAttributes` without the `?.` the sibling getters got. Real SDK paths always populate these, so not production-reachable; consistency hardening. | `Benzene.Aws.Lambda.Sns/SnsMessageBodyGetter.cs:17`, `Benzene.Aws.Sqs/Consumer/SqsMessageTopicMapper.cs:49` | overnight-log |
| 10 | LOW | **`BenzeneHttpWorker` accept loop lacks the catch-all + `finally`** the Kafka worker has — a non-`HttpListenerException`/`OperationCanceledException` escaping the loop faults `_runTask` silently and leaks the listener (no confirmed trigger under normal operation). | `Benzene.SelfHost.Http/BenzeneHttpWorker.cs:56` | overnight-log |
| 11 | LOW | **Dead reflection-era code + stray `Debug.WriteLine` leftovers** — `MessageClientSdkBuilder` `_propertyTypeMapping`/`GetTypeName(Type,..)` have no external caller; `Debug.WriteLine` diagnostics remain on handler exception paths (`MessageHandler.cs:64,81,87`, `HandlerPipelineBuilder.cs:52`, `ReflectionMessageHandlersFinder.cs:69`). Cleanup. | see cells | arch-review |
| 12 | LOW | **`AsRawHttpRequest` emits LF not CRLF** (RFC 7230) — **dead code** (no caller in src/test/examples), noted for completeness. | `Benzene.Testing/MessageBuilderExtensions.cs` | overnight-log |

---

## Tier 2 — Design decisions (real issue; need a maintainer's call, then fix)

### Data-loss / at-least-once semantics (highest impact)
1. **[MED] Kinesis up-to checkpointing is unsafe under out-of-order `PartitionBy`** — checkpointing partition A's last record claims "safe up to here" while an earlier-in-batch record of partition B is unprocessed → that B record is skipped on failure. The monotonic fix (`21f7333`) does not close it — needs a per-record/set-based checkpoint model or a documented "checkpoint in batch order only" constraint. `KinesisStreamCheckpointer`. *(bughunt #1)*
2. **[MED] Kinesis: a successful batch that never checkpoints reprocesses forever** — the `UseStream((records,ct)=>…)` overload exposes no checkpointer, so `BuildResponse` reports record 0 even on success → AWS redelivers the whole batch. Cosmos solved the analogue with `AutoCheckpointOnSuccess=true`; Kinesis wants the same default (or a `UseCheckpointAfterEach()`). `KinesisStreamApplication`. *(bughunt #2, cloud-review)*
3. **[MED] Kinesis & DynamoDB Streams always return a batch response and swallow the pipeline exception** — correctness depends on the ESM having `ReportBatchItemFailures` configured, which Benzene can't see; without it, a failure is silent loss. Consider a thrown-exception fallback or a startup warning. `KinesisStreamApplication.cs:77`, `DynamoDbApplication.cs`. *(cloud-review)*
4. **[MED] Split-brain `RaiseOnFailureStatus` defaults across transports** — SQS/Kafka/ServiceBus-worker/RabbitMQ retain a failure *result*; S3/EventBridge/QueueStorage/EventGrid/EventHub-trigger discard it by default. Inconsistent at-least-once behaviour; align or document loudly. Also: **S3 & EventBridge Lambda sources have no `RaiseOnFailureStatus` opt-in at all**. *(bughunt #3, arch-review C7, cloud-review)*
5. **[MED] Self-hosted SQS consumer `WholeBatch` (the default `AckMode`) deletes a non-throwing failure result** — only a *thrown* exception skips the batch delete; a handler returning a failure result under the default is silently deleted. `PerMessage` mode settles correctly. Parallel to the Lambda `SqsApplication` fix (`6ef8387`) that was applied — decide whether `PerMessage` should be the default. `Benzene.Aws.Sqs/Consumer/SqsConsumer.cs:80`. *(cloud-review; verified — documented-but-footgun)*
6. **[MED, verify] Service Bus worker `AutoComplete` default completes a non-throwing failure result** — `ccead61` made a *null* result abandon, but a returned failure under the default `AckMode` may still complete. Verify against `BenzeneServiceBusWorker.cs:108` + config default. *(cloud-review — needs confirm)*
7. **[LOW] RabbitMQ null-result → ack** — left as-is (explicitly documented/tested as deliberate), unlike ServiceBus/DynamoDB which were fixed. Flagged for cross-transport consistency only. *(bughunt #4)*

### Egress / batch
8. **[MED] AWS batch clients let a whole-request transport exception escape** (`SqsBatchMessageClient`/`SnsBatchMessageClient`/`EventBridgeBatchMessageClient`) instead of converting it to per-entry `BatchSendResult` failures like the Azure batch clients — on a throttle/network throw mid-chunk, earlier successes + recorded failures are lost and the caller resends everything (duplicate deliveries). "Throw vs always-return-a-result" contract call. *(overnight-log, bughunt, arch-review)*
9. **[LOW-MED] Batch clients: a per-entry *conversion* failure mid-batch aborts the whole `SendBatchAsync`** after a partial send, instead of recording it as that entry's failure. *(bughunt)*

### DI / lifetime / mesh
10. **[MED] `MicrosoftServiceResolverFactory.Dispose()` is a no-op → the built provider is never disposed on the Lambda / self-host-from-`IServiceCollection` paths**, so async-only-disposable singletons (`MeshAnnouncer`, `HttpMeshTraceExporter`) leak their announce loop / drop their tail trace batch until process exit. The ASP.NET/generic-host path was fixed; this is the remaining piece and needs a core-DI ownership decision. `Benzene.Microsoft.Dependencies/MicrosoftServiceResolverFactory.cs:16,21`. *(audit #1, arch-review)*
11. **[MED] `AddMessageHandlers` finder lock-in** — both overloads register `IMessageHandlersFinder` via `TryAddSingleton`, so a no-arg-then-typed call sequence silently drops the typed (reflection) finder → 404s. Naive aggregation was reverted (duplicate-topic dedup); a real dedup-semantics decision. `Benzene.Core.MessageHandlers/DI/Extensions.cs:152,189`. *(arch-review)*
12. **[LOW-MED] `MeshSelfReportMiddleware` fire-and-forget publish is unreliable on Lambda** — `_ = PublishBestEffortAsync()` after `await next()`; the Lambda runtime freezes after return, so the self-report often never completes on exactly the on-demand host it targets. Latency-vs-telemetry tradeoff. `Benzene.Mesh.Reporting/MeshSelfReportMiddleware.cs:47`. *(audit #4, arch-review)*

### Contracts / validation / serialization
13. **[MED] `SchemaCompatibilityComparer` gaps** — enum-value removal, nullable flips, and facet tightening pass the backward-compat gate; a policy call on what counts as breaking per direction. *(arch-review)*
14. **[MED] Avro `Dictionary`/map round-trip unsupported**, and **Avro deserialize does an unbounded length-prefix allocation** (untrusted `application/avro` → OOM). Map support needs a bidirectional schema change; the OOM needs a size knob. (Note: the `ulong > long.MaxValue` overflow was fixed in Cycle 21.) *(arch-review)*
15. **[MED] Overlapping result abstractions** — `IBenzeneResult<T>` / `IMessageHandlerResult<T>` / legacy `IMessageResult` coexist and the ack path runs on the legacy type; consolidation/`[Obsolete]` is an API call. *(arch-review C3)*
16. **[MED] Cache null-payload negative-caching** and **versioning unknown-version passthrough** are per-policy behaviours worth an explicit decision (cache one confirmed intact by design at `CacheEntry.cs:60`). *(arch-review)*
17. **[MED, fragility] `ValidateOutboundRouting` matches any `public static string[] RequiredTopics` field across all loaded assemblies** (no marker interface/attribute) → documented test-pollution caveat. `Benzene.Clients/ValidateOutboundRoutingExtensions.cs:56`. *(arch-review)*

### Health / convergence / lower-impact
18. **[LOW-MED] `DynamoDbHealthCheck` verdict is driven only by the DescribeTable HTTP 200**, ignoring `TableStatus` → reports healthy for a `DELETING`/`INACCESSIBLE_ENCRYPTION_CREDENTIALS` table. Which statuses should fail is a policy call. *(overnight-log, health-review)*
19. **[LOW] `CachingHealthCheckProcessor` cache key is just the sorted check-`Type` set** → two probes (liveness vs readiness) sharing the singleton with the same type-set but different instances can serve each other's cached result for up to the TTL. *(overnight-log, health-review)*
20. **[MED] SQS/DynamoDb are the two-generation adapter holdouts + magic-string transport tags** (`SetTransport("sqs"/"dynamodb")` literals, bare `bool? IsSuccessful` instead of `IHasMessageResult`). Convergence debt. *(arch-review T1)*
21. **[LOW] `MeshAggregator.BuildTopicEntry` SchemaMismatch folds Response into the compare string** without the request-side "no schema ⇒ no signal" guard (false-positive mismatch); **structural-edge dedup key** `$"{client} {server}"` collides if a service name contains a space. *(overnight-log)*
22. **[LOW] `CloudServiceDescriptorSource._descriptor` non-volatile double-checked locking** — benign on x86/x64, a real hazard on ARM64/Graviton (canonical one-word `volatile` fix; can't be failing-test-first verified). *(overnight-log)*
23. **[LOW] `BenzeneResultExtensions.IsSuccess()` returns true only for `Ok`**, disagreeing with `IBenzeneResult.IsSuccessful` (true for all six success statuses) — likely an intentional narrow alias; decide the intended semantics. *(overnight-log)*
24. **[LOW] `mesh-ui.html` sets `specUrl`/`healthUrl` anchor `href` from the self-reported manifest without a scheme allow-list** (`javascript:` URI; browser-mitigated by `target="_blank"`). *(overnight-log)*
25. **[LOW] CR/LF response-header injection (defence-in-depth)** — API-Gateway/self-host response adapters don't strip CR/LF from header values (not a confirmed live vector today). *(arch-review)*
26. **[LOW] Outbound Kafka converter `GetBytes(header.Value)` can throw on a null header value** (inbound getters were hardened; outbound is caller-controlled). *(bughunt)*
27. **[LOW] `Benzene.Kafka.Core` body getters `.ToString()` a non-string `TValue`** (e.g. `byte[]` → `"System.Byte[]"`) — not reachable via the shipped/tested `<Ignore,string>` path; latent generic sharp edge. *(overnight-log)*

---

## Tier 3 — Performance / hygiene (not correctness bugs; listed for completeness)

- `ActivityMiddlewareDecorator` re-resolves the handler (`FindHandler`) per middleware when an OTel listener is attached. *(arch-review)*
- Per-send serializer+converter allocation in the 4 Azure + 3 AWS single-message egress clients (batch siblings already cache). *(arch-review T3)*
- Azure workers resolve the logger via a per-error DI scope. *(arch-review C2)*
- Enrichment dictionary churn per HTTP request. *(audit #3)*
- No `ConfigureAwait(false)` in core; `ValueTask`/`Task` mix — relies on host being SynchronizationContext-free. *(arch-review C8)*

---

## Notes on excluded items
- **Already FIXED (verified in source):** the vast majority of every doc — e.g. SNS topic-key/FIFO/numeric/attr-cap, Event Hub partition-key/batch, Kafka rebalance-drain/dead-letter/duplicate-header/per-partition-ordering/all-headers-read, API Gateway v2 + binary req/resp, Service Bus sessions/dead-letter/lock-renewal/null-settlement, StepFunctions idempotency, Lambda-client FunctionError + invoke-type-by-identity, value-type cache miss-as-hit, both DI adapters, `CompositeBenzeneWorker` shutdown drain, and all Cycle 1–31 fixes.
- **Not bugs (missing features / roadmap) — excluded:** SQS-FIFO-consume + visibility heartbeat, gRPC streaming, Service Bus transactions/deferral/filters, Kafka EOS/schema-registry/seek, Kinesis tumbling windows, BlobStorage `Stream` binding, Queue-Storage size guard, SNS Extended-Client (>256 KB), ALB/HTTP-API-simple-authorizer/response-streaming, worker-base/handler-convergence refactors (T4). These live in the roadmap docs, not here.
- **Verified FALSE (do not action):** "gRPC client discards caller deadline/cancellation" (`GrpcBenzeneMessageClient` propagates both from the inbound `ServerCallContext`); "outbound SQS/SNS return `Ok` instead of `Accepted`" (both return `Accepted`).
- Several `cloud-review/*` items are marked "(unverified)" by the extracting agent and predate the fix series; treat that doc's list as candidates, not confirmed defects — the confirmed residue is folded in above.
