## RabbitMQ

Scope: every `.cs` + CLAUDE.md under `src/Benzene.RabbitMq` (consumer worker, outbound publish client, getters, DI) plus the shared `BoundedConcurrentDispatcher<T>`. A notably careful integration — several classic RabbitMQ footguns are already handled correctly. Worst-first.

---

### [MISSING] No persistent delivery mode (delivery-mode 2) on publish (Severity: High)
- `RabbitMqClientMiddleware.HandleAsync` builds `new BasicProperties { Headers = context.Headers }` and never sets `Persistent`/`DeliveryMode` (`:34-39`). Every published message is **transient**. No config knob anywhere in the publish path.
- RabbitMQ: messages are written to disk only if delivery mode is `persistent` (2) **and** they land on a durable queue. A transient message on a durable/quorum queue is dropped on broker restart.
- Impact: the consumer side is "safe by default" (Explicit ack), but the producer silently undermines it — a message to a durable queue is lost if the broker restarts before consumption, with no way to prevent it short of custom middleware. Not called out in CLAUDE.md.
- Recommendation: Add a `persistent`/delivery-mode option (default it to persistent, or document the transient default loudly).

### [MISSING] No publisher confirms — publish reports `Accepted` without broker acknowledgement (Severity: Medium)
- Channel created with no `CreateChannelOptions` enabling publisher confirmations; the publish client maps a returned `BasicPublishAsync` straight to `Accepted`. Without confirms, a completed publish means only "handed to the socket," not "broker accepted/enqueued." `BenzeneResult.Accepted` overstates the guarantee. Honestly documented as a "future opt-in" (Medium not High). Offer an opt-in confirm mode (await the confirm before mapping to `Accepted`).

### [WRONG-APPROACH] Single `IChannel` shared across concurrent ack/nack lanes and concurrent publishes (Severity: Medium)
- The worker opens one `_channel` and dispatches deliveries across up to `ConcurrentRequests` lanes, each calling `BasicAckAsync`/`BasicNackAsync` from its own task; the outbound client shares one `IChannel` across concurrent `SendMessageAsync` → `BasicPublishAsync`.
- RabbitMQ: the long-standing rule is a channel must not be shared by publishing threads, and multi-threaded consumers must serialize acks over a shared channel. v7 added internal command serialization implying `IChannel` is now thread-safe — but the maintainers' tracking issue (#1722) says this is "not yet verified," and a maintainer states sharing a channel for multi-frame ops like `basic.publish` "is not supported." Officially a gray area; the publish path (multi-frame) is riskier.
- Impact: in practice v7's serialization likely prevents frame interleaving (Medium not High), but Benzene relies on unverified/officially-unsupported behavior.
- Recommendation: either document the reliance on v7's serialization + pin a minimum `RabbitMQ.Client` version, or use a dedicated publish channel per publisher and/or serialize ack/nack behind a `SemaphoreSlim`. At minimum flag in CLAUDE.md.

### [MISSING] `mandatory` publish flag is wired but broker returns are never observed (Severity: Low-Medium)
- `RabbitMqClientMiddleware` forwards a `mandatory` flag into `BasicPublishAsync`, but nothing subscribes to the channel's `BasicReturnAsync`. An unroutable mandatory message is returned asynchronously, unhandled, and the publish still sets `Published = true` → `Accepted`. Setting `mandatory: true` gives false safety. Wire a `BasicReturnAsync` handler that flips the result to unroutable, or document that `mandatory` currently only affects broker behavior.

### [MISSING] DLX, message/queue TTL, priority, quorum-vs-classic delegated to broker topology (Severity: Low — documented)
- CLAUDE.md states topology management (exchanges/queues/bindings, DLX, quorum delivery-count) is out of scope; the worker assumes the queue and any DLX exist. Legitimately broker-side and consistent with other transports. Keep the "Deliberate boundaries" section; consider a cookbook showing the DLX + quorum-queue delivery-limit policy that pairs with `RequeueOnFailure = false`.

---

### What is correct (verified, no action)
- **Ack timing — correct.** Under `Explicit` (default) the delivery is `BasicAck`ed only *after* the handler succeeds; failure/throw → `BasicNack`. No ack-before-process. `AutoAck` is opt-in, documented as at-most-once.
- **Prefetch/QoS — set and bounded.** `BasicQosAsync(0, PrefetchCount, false)` default 5. Real backpressure, reinforced by capacity-1 dispatcher lanes.
- **Poison-message loop — prevented.** Requeue bounded to one retry via the `Redelivered` flag: first failure requeues, an already-redelivered failure nacks *without* requeue. CLAUDE.md notes `Redelivered` is boolean not a count, deferring precise limits to DLX policy.
- **Body copy before hand-off — correct.** `Body.ToArray()` before enqueueing to another thread, avoiding the rented-buffer reuse bug.
- **Connection recovery** on by the SDK default (documented; delivery tags don't survive a recovery — a RabbitMQ property, not a Benzene defect).

### Verdict
Consumer side is solid — correct post-processing ack, bounded prefetch, and a genuine anti-poison-loop guard put it ahead of most hand-rolled workers; the real weaknesses are on the **producer/reliability** side (transient-by-default publish with no persistence knob, no publisher confirms, unobserved mandatory returns) plus a **shared-channel concurrency** model leaning on v7's still-unverified thread-safety.
