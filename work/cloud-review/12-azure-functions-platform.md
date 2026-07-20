## Azure Functions platform

Scope: `Benzene.Azure.Function.Core`, `.AspNet`, `.Timer`, `.BlobStorage`, `.CosmosDb`, `.EventGrid`, `.QueueStorage`, `.Kafka` (ServiceBus/EventHub excluded — reviewed separately). Verified against the isolated-worker guide and each trigger's Learn reference, plus the restored extension assemblies on disk.

---

### Core / host

**[WRONG-APPROACH] Trigger dispatch keyed solely on CLR request type collides for multiple functions of the same trigger** (Severity: High)
- **Benzene today:** `AzureFunctionApp.HandleAsync<TRequest>` / `<TRequest,TResponse>` scan `_apps` and return the **first** entry point whose generic request type matches. Every Queue Storage function dispatches `QueueStorageMessage[]`, every Blob function `BlobTriggerEvent`, every Timer `TimerTriggerInfo`, every Cosmos function of a given `TDocument` the same `IReadOnlyList<TDocument>`.
- **Azure intent:** A Function App very commonly hosts several functions of the same trigger kind — multiple `[QueueTrigger]`s on different queues, multiple `[BlobTrigger]`s, multiple `[TimerTrigger]`s — each with its own logic.
- **Impact:** If two `UseQueueStorage(...)` (or two `UseBlobStorage`, two `UseTimerTrigger`, two same-`TDocument` Cosmos) pipelines are registered, only the first is reachable; the second is dead. All queue functions must share one pipeline and one `UsePresetTopic`, so per-queue topic routing across two queues is impossible. CLAUDE.md honestly states "two trigger types coexist as long as their request types differ" — a known but materially limiting constraint for real multi-function apps.
- **Recommendation:** Allow a discriminator (a function-name/key from the trigger method into `HandleAsync`) so multiple same-typed entry points can coexist, or document prominently that same-trigger multiplicity requires envelope/preset routing within one shared pipeline.

**[Note] Every entry point is reconstructed on every invocation** (Severity: Low) — the ctor builds all trigger apps per scoped resolve. Correctness fine; minor per-invocation allocation.

Isolated-worker model used correctly throughout (only `Microsoft.Azure.Functions.Worker.*`); `IBenzeneInvocation` populated from `FunctionContext.InvocationId`. Verdict: correct foundation; the type-only dispatch is the one real architectural constraint.

---

### AspNet (HTTP)

**[DIVERGENCE] `SetContentType` omits the lazy-response guard the other setters have — latent NullReferenceException** (Severity: Medium)
- `SetStatusCode`/`SetBody`/`GetBody`/`SetResponseHeader` all call `context.EnsureResponseExists()` first, but `SetContentType` does `context.ContentResult.ContentType = contentType;` with no guard. `ContentResult` is null until first `EnsureResponseExists()`. A response path that sets content type before status/body NREs → 500 with no body. Add `context.EnsureResponseExists();` as the first line of `SetContentType`.

**[DIVERGENCE] Response headers and body/status written through two different channels** (Severity: Low/Medium)
- `SetResponseHeader` writes to `HttpContext.Response.Headers`, while status/content-type/body are set on the returned `ContentResult`. Works today under the ASP.NET Core integration (returning an `IActionResult` executes against the same `HttpContext.Response`), but mixing channels is fragile. Carry headers on the same result object, or document the coupling.

**[DIVERGENCE] Message-headers getter lower-cases header values** (Severity: Low)
- `AspNetMessageHeadersGetter.GetHeaders` ends with `ToDictionary(x => x.Item1.ToLowerInvariant(), x => x.Item2.ToLowerInvariant())` — lower-cases the **value**, not just the key. Case-sensitive header values (bearer tokens, correlation IDs, base64) are corrupted. Preserve values. (Same class of bug as the self-host server's Critical finding — see HTTP review.)

Positives: correct isolated-worker ASP.NET Core integration mode, async buffered body read, request-abort token seeding, documented `/api` route-prefix caveat, 200 default. Verdict: fix the `SetContentType` guard and value lower-casing.

---

### Timer

**[MISSING/DIVERGENCE] Binding the trigger parameter directly as Benzene's `TimerTriggerInfo` may not bind** (Severity: Low)
- The isolated worker binds `[TimerTrigger]` to the extension's `TimerInfo` (dedicated input converter), not arbitrary POCOs. A consumer following "bind as `TimerTriggerInfo`" could fail; the safe path is bind `TimerInfo`, then map. CLAUDE hedges ("or map the two"). Lead with the map-from-`TimerInfo` pattern.

`IsPastDue`/`ScheduleStatus` modelled correctly; singleton/past-due host-managed; a failed tick is not retried (schedule-based). Honest and correct.

---

### BlobStorage

**[MISSING] No streaming / `Stream` binding — full blob content always materialized as `byte[]`** (Severity: Medium)
- `BlobTriggerEvent.Content` is `byte[]`; `HandleBlob(name, byte[])`/`(name, string)` are the only shapes. The blob trigger can bind to `Stream` for large blobs to avoid loading the whole object. Large blobs force a full in-memory buffer per invocation — a real memory/scale risk. Offer a `Stream`-based overload, or document the byte[]-only constraint and size ceiling.

**[MISSING] Event Grid-based blob trigger / polling-scalability caveat disclosed but no first-class story** (Severity: Low, known gap)
- CLAUDE.md correctly notes the classic polling trigger lags on large containers and that MS recommends the Event Grid-based source (`BlobTriggerSource.EventGrid`), plus host-managed poison handling (5 retries → `webjobs-blobtrigger-poison`). The attribute lives in the consumer project, so it's transparent — but no guidance/example wires the Event Grid source.

Verdict: correct and honest about host behavior; the byte[]-only content model is the one substantive gap.

---

### CosmosDb (change feed)

**[MISSING] No poison-batch / dead-letter escape — one un-processable document blocks the lease indefinitely** (Severity: Medium)
- Fan-in: the whole batch is one ordered `StreamContext<TDocument>`, `NullStreamCheckpointer`, and any pipeline exception propagates so "the lease stays put and the runtime redelivers the whole batch." The CosmosDBTrigger checkpoints its lease on successful return and has **no** built-in poison/dead-letter. At-least-once + per-partition-key-range ordering correctly preserved. But a single always-throwing document causes infinite redelivery of the whole batch — the lease never advances, blocking the container's change feed. Benzene provides no in-pipeline catch/skip/dead-letter (unlike Kafka's `CatchExceptions`) and the CLAUDE doesn't flag the infinite-retry risk. Document the poison-batch risk and offer an opt-in per-item try/continue + dead-letter hook.

**[Note] Batch may span multiple partition-key ranges; ordering guarantee is per-range only** (Severity: Low) — the flattened single `IAsyncEnumerable` conflates ranges; a handler assuming whole-batch order would be wrong. Doc-correct.

**[MISSING] Lease/checkpoint controls & change-feed start-from** (Severity: Low, known gap) — `StartFromBeginning`/`StartFromTime`/`LeaseContainerName`/`MaxItemsPerInvocation`/`FeedPollDelay` live on the consumer's `[CosmosDBTrigger]`; transparent to Benzene, and the CLAUDE points at the self-hosted worker for manual checkpoint control. Correctly scoped.

Verdict: checkpointing/ordering correct for the trigger; the missing poison-batch escape is the notable operational gap.

---

### EventGrid

**[DIVERGENCE] Handler failure result silently swallowed; no opt-in to escalate (retry/dead-letter never fires)** (Severity: Medium)
- CLAUDE.md flags "Unsafe by default, and there is no opt-out" — a non-exception `IBenzeneResult` failure reports success, so Event Grid marks the event delivered and never retries. Only a thrown exception drives Event Grid's retry (backoff, up to 24h) + dead-letter. No `Options` type. Notably inconsistent with Kafka/ServiceBus, which offer `RaiseOnFailureStatus`. Add the same escalation.

**[Low] CloudEvents-schema parsing may only ever run on manually-dispatched JSON** — `EventGridTriggerEvent.Parse` detects CloudEvents via `specversion`, else Event Grid schema. Solid parsing, but the native `EventGridTrigger` predominantly delivers Event Grid schema (CloudEvents typically goes to an HTTP webhook). Defensive, harmless.

**[Note] Subscription-validation handshake not handled — correct for the native trigger, undocumented for webhook mode** (Severity: Low) — for the native trigger with the `azurefunction` endpoint, the host performs the `SubscriptionValidationEvent` handshake automatically (Benzene correctly does nothing). A consumer fronting Event Grid with an HTTP-trigger webhook must answer validation themselves; this package offers no help. One-line CLAUDE note.

Verdict: routing-by-event-type and dual-schema parsing are right; the silently-dropped failure result (no escalation) is the real gap.

---

### QueueStorage

**[DIVERGENCE] Handler failure result silently deletes the message — no poison/`maxDequeueCount` protection, no opt-in to escalate** (Severity: Medium)
- CLAUDE.md flags this — a non-exception failure is treated like success, so the host deletes the message; only a thrown exception feeds `maxDequeueCount` → `<queue>-poison`. No options type. "Graceful" failures bypass all poison protection = silent loss; no `RaiseOnFailureStatus` equivalent. Add the escalation option for parity.

Positives: correct that Queue Storage messages carry no properties (topic getter returns null; routing via `UseBenzeneMessage` envelope or `UsePresetTopic`), `DequeueCount`/`MessageId`/`InsertedOn` optionally surfaced, batch/visibility/poison delegated to `host.json`. Verdict: model/routing correct; same silent-failure gap as Event Grid.

---

### Kafka

Verified: `Microsoft.Azure.Functions.Worker.KafkaRecord` (with `Topic`/`Partition`/`Offset`/`Value` byte[]/`Headers`/`Key`) is a real type in `Extensions.Kafka` 4.3.0 — binding and `KafkaMessageHeadersGetter` are accurate.

**[Note] `RaiseOnFailureStatus` + `CatchExceptions` together cancel each other out** (Severity: Low)
- In `KafkaBatchApplication.HandleAsync`, `RaiseOnFailureStatus` throws `KafkaMessageProcessingException`, but the surrounding `catch (Exception ex) when (_options.CatchExceptions)` swallows it when `CatchExceptions` is also true — so the escalated failure is logged, the offset commits, nothing retried. A consumer enabling `RaiseOnFailureStatus` for retries won't get them if `CatchExceptions` is also on. Document the mutual defeat, or exclude the raised exception from the catch.

Positives: commit/offset semantics correct (extension auto-commits after success; default `CatchExceptions=false` lets an exception cascade for host retry — whole-batch redelivery with a documented idempotency caveat); `RaiseOnFailureStatus` gives the escalation the other Azure triggers lack; real-topic routing, W3C trace-context via real headers, per-record scope, bounded fan-out all sound. Verdict: strongest of the triggers.

---

### Cross-cutting observation
Failure-result handling is **inconsistent across the messaging triggers**: Kafka (and ServiceBus) expose `RaiseOnFailureStatus` to turn a non-exception failure into a retry, but **Event Grid and Queue Storage do not**, and both silently succeed on a graceful failure — defeating Event Grid retry/dead-letter and Queue Storage poison protection respectively. The single highest-value consistency fix across the platform. All packages are honest in CLAUDE.md (lowers severity, doesn't remove the runtime risk).
