## Azure Service Bus

> Note: this reviewer could not reach learn.microsoft.com (proxy allowlist 403); findings are grounded in the Benzene code plus canonical, stable Service Bus SDK semantics rather than a fresh doc fetch.

Four packages: self-hosted `ServiceBusProcessor` worker (`Benzene.Azure.ServiceBus`), Functions trigger (`Benzene.Azure.Function.ServiceBus`), sender (`Benzene.Clients.Azure.ServiceBus`), peek health check (`Benzene.HealthChecks.Azure.ServiceBus`). Ingress/egress plumbing is clean; the gaps are almost entirely Service Bus *messaging semantics*.

---

### [DIVERGENCE] AutoComplete default completes a handler failure result (only a thrown exception is retried) — Critical
- **Benzene today:** Both consumers default `AckMode = AutoComplete`. In the worker (`BenzeneServiceBusWorker.OnProcessMessageAsync`, ~108–116) messages dispatch with `AutoCompleteMessages = true`, so a handler that *returns* `BenzeneResult.ServiceUnavailable(...)` (no throw) is **completed** — permanently removed. Only a thrown exception abandons. Functions adapter same (`ServiceBusOptions.AckMode`/`RaiseOnFailureStatus` default off). Both CLAUDE.md admit this under "⚠️ Unsafe by default".
- **Azure intent:** Complete = "processed successfully, delete it." Reporting failure but completing silently drops the message, defeating at-least-once. A transient `ServiceUnavailable` is exactly what redelivery + max-delivery-count + DLQ exist to handle.
- **Impact:** Silent message loss on the *default* setting; a failing handler that never throws looks healthy to the broker (no DLQ entry, no redelivery). High blast radius (out-of-the-box).
- **Recommendation:** Flip default to `Explicit`, or make an unsuccessful `IMessageResult` abandon by default.

### [MISSING] No session support (ordered FIFO / SessionId / session state) — Critical
- **Benzene today:** The worker unconditionally calls `_client.CreateProcessor(...)` — never `CreateSessionProcessor(...)`. No `SessionId` exposure, no session-state, no per-session ordering. Functions CLAUDE.md explicitly: session handling "not implemented."
- **Azure intent:** Sessions are the *only* way to get ordered (FIFO) processing + per-consumer affinity. They require `ServiceBusSessionProcessor` — a plain `ServiceBusProcessor` cannot receive from a session-enabled entity at all (it fails).
- **Impact:** A session-enabled queue/subscription simply cannot be consumed by the Benzene worker — outright incompatibility. Any ordering workload (per-aggregate streams, sagas, per-customer FIFO) has no path.
- **Recommendation:** Add a session mode building a `ServiceBusSessionProcessor`; surface `SessionId`/session state (as transport-shape); mirror in Functions via `ServiceBusSessionMessageActions`. Single biggest gap.

### [DIVERGENCE] No explicit dead-lettering; permanent failures abandon-loop until max delivery count — High
- **Benzene today:** In `Explicit` mode the only verbs are complete and abandon. A handler can never call `DeadLetterMessageAsync`. A poison failure is abandoned → immediately available again, incrementing delivery count each attempt until `MaxDeliveryCount` auto-dead-letters it.
- **Azure intent:** Abandon is for *transient* failures. For known-bad messages, `DeadLetterMessageAsync(reason, description)` moves to DLQ immediately with a diagnosable reason. Abandon has no backoff → tight redelivery loop.
- **Impact:** Poison messages burn delivery attempts in a hot loop and land in the DLQ with a generic `MaxDeliveryCountExceeded` reason instead of the handler's actual diagnosis.
- **Recommendation:** Give the handler outcome a way to request dead-letter (a distinct result/marker → `DeadLetterMessageAsync`).

### [MISSING] No message deferral — Medium
- No `DeferMessageAsync` / `ReceiveDeferredMessageAsync`. Deferral parks a message (keyed by sequence number) for later explicit retrieval — the standard "arrived out of order / dependency not ready" pattern. Workflows needing it have no primitive.

### [MISSING] No auto lock-renewal control for long handlers — Medium
- **Benzene today:** The worker never sets `MaxAutoLockRenewalDuration`. The processor auto-renews only until the SDK default (5 min); a handler running longer loses the lock, the message is redelivered mid-processing, and the settle throws `MessageLockLost`.
- **Recommendation:** Surface `MaxAutoLockRenewalDuration` (and lock-duration guidance) on `BenzeneServiceBusConfig`.

### [MISSING] Sender has no scheduled/delayed send, TTL, MessageId (dedup), SessionId, or batch send — Medium
- **Benzene today:** Converters build a `ServiceBusMessage` with body + topic property + headers only; middleware sends a single message. Nothing sets `ScheduledEnqueueTime`, `TimeToLive`, `MessageId`, `SessionId`, `CorrelationId`, `Subject`; no `ServiceBusMessageBatch`.
- **Azure intent:** Scheduled enqueue (delayed delivery); per-message TTL; `MessageId` to drive broker-side **duplicate detection** (valuable given Benzene's at-least-once); `SessionId` (mandatory to send *to* a session-enabled entity — so Benzene currently cannot correctly produce to the very entities the consumer gap also can't read); batch for throughput.
- **Recommendation:** Let callers set standard `ServiceBusMessage` properties (at least `MessageId`, `SessionId`, `ScheduledEnqueueTime`, `TimeToLive`); add a batch-send path.

### [MISSING] No transactions, no topic/subscription rules & filters, no prefetch/partitioning guidance — Low
- No `CreateTransaction`/`TransactionScope` (atomic settle-and-send / outbox-lite); no management-plane story for subscription SQL/correlation filters (Benzene routes in-process on a `"topic"` app property). Acceptable to defer to SDK/ARM, but there's no documented "bring your own" seam. Document as explicit non-goals with a "use the SDK directly" pointer.

### [WRONG-APPROACH] Routing via a `"topic"` application property instead of native subscription filters — Low/Medium
- Handler routing keys off a configurable `"topic"` application property, set by the sender. Service Bus's native mechanism for "different message kinds → different subscribers" is topics + subscriptions with SQL/correlation filters evaluated broker-side. Benzene's app-property routing is orthogonal and the naming collides conceptually with a Service Bus "topic" entity. Mostly a conceptual-overloading/interop concern, not a correctness bug — consistent across transports and configurable. Keep the convention but document its relationship to native filters; align the missing-topic fallback (`Constants.Missing` worker vs null-topic Functions).

### Note (not a finding): Health check
`Benzene.HealthChecks.Azure.ServiceBus` is sound — read-only `PeekMessageAsync`, non-side-effecting, `Listen`-claim only, reports exception *type* not message, no secret leakage.

---

**Verdict:** Ingress/egress plumbing is clean, but Benzene treats Service Bus like a generic queue — headline gaps are the **silent-completion-on-failure AutoComplete default (Critical, documented but still the default)** and **complete absence of session support (Critical — session-enabled entities are unusable)**; explicit dead-lettering, deferral, lock-renewal tuning, and producer-side scheduling/dedup/SessionId round out the missing surface.
