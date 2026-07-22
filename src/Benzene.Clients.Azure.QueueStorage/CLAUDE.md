# Benzene.Clients.Azure.QueueStorage

## What this package does
Outbound Azure Queue Storage client for a Benzene app: send a message to a storage queue, enveloped
so it can be routed by `BenzeneMessageQueueStorageHandler` on the ingress side. The egress counterpart
of `Benzene.Azure.Function.QueueStorage` (release plan Tier 2.2, §5.2). This package introduces
`Azure.Storage.Queues` (12.27.1) fresh — the ingress package is deliberately SDK-free.

## Key types
- `QueueStorageBenzeneMessageClient` — `IBenzeneMessageClient`; sends via a caller-supplied `QueueClient`.
- `QueueStorageClientMiddleware` / `QueueStorageSendMessageContext` — terminal send middleware and its
  context (a plain string body — this transport has no properties/attributes bag).
- `QueueStorageContextConverter<T>` — `IBenzeneClientContext<T, Void>` → send context.
- `OutboundQueueStorageContextConverter` — the `Benzene.Clients.OutboundContext` counterpart, used by
  the `OutboundContext` overloads of `.UseQueueStorage(...)` for `AddOutboundRouting(...).Route(topic, …)`.
- `Extensions` — `UseQueueStorageClient`, `UseQueueStorage<T>`/`UseQueueStorage` (both the
  `IBenzeneClientContext<T,Void>` and `OutboundContext` overloads), `AddQueueStorageMessageClient`, and
  **`AddQueueStorageHealthCheck`**.
- `QueueStorageHealthCheck` — verifies a queue with a read-only `GetProperties` call (`Type =
  "QueueStorage"`, dependency `("Queue", queueClient.Name)`; non-destructive — no send/receive/peek).
  Failures are classified via `HealthCheckError.Classify` (§3.9): a permission error (403) is a
  **Warning**; the SDK `ErrorCode`/`StatusCode` are surfaced in `Data`, never the exception message
  (which can carry a SAS token). Azure discriminators come off `RequestFailedException`.
  - **Auto-wired (Phase 4, default-on).** The two `queueClient`-instance `UseQueueStorage`/`UseQueueStorage<T>`
    overloads take `bool healthCheck = true`: unless opted out they auto-register the check on the
    **dependency category** (`AddDependencyHealthCheck`, dedup `"QueueStorage:{name}"`), **capturing the
    passed `QueueClient` directly** (no DI round-trip — Queue Storage clients are passed, not resolved).
    Deep `healthcheck` layer only — never a probe (shared-fate; see `IDependencyHealthCheck`). The
    `action`-based overloads don't auto-wire.

## Routing — there is no property to set; the envelope IS the routing
A Queue Storage message has no properties/attributes at all, so unlike Service Bus/Event Hubs there
is nothing to set alongside the body. Both converters serialize a
`Benzene.Core.Messages.BenzeneMessage.BenzeneMessageRequest` envelope (`Topic`, `Headers`, `Body`) as
the queue message text — the exact shape `BenzeneMessageQueueStorageHandler`
(`queue.UseBenzeneMessage(...)`) deserializes on the ingress side.

## If the destination queue uses `UsePresetTopic(...)` instead — do NOT use this package's converters
A queue wired with `UsePresetTopic("orders.created").UseMessageHandlers()` expects the message body
to **be** the request payload directly, not wrapped in a `BenzeneMessageRequest` envelope — sending
the envelope to a preset-topic queue would deliver the wrong shape. For that case, serialize the
payload yourself and call `QueueClient.SendMessageAsync(serializedPayload)` directly; no Benzene
wrapper is needed for a one-line SDK call (design philosophy principle 3: rolling your own is easy
and first-class where Benzene doesn't need to get involved).

## ⚠️ Message encoding must match the consumer — set `MessageEncoding = Base64` for a Functions trigger
This package sends via a caller-supplied `QueueClient`, and it does **not** set
`QueueClientOptions.MessageEncoding`. `Azure.Storage.Queues` v12 therefore defaults to
`QueueMessageEncoding.None` — the envelope text goes onto the queue as **plain UTF-8**. But the
matching ingress, `Benzene.Azure.Function.QueueStorage` (the Functions `[QueueTrigger] string`
binding), defaults to **Base64** (`Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues` /
`host.json` `extensions.queues.messageEncoding`). Left unconfigured, Benzene's own two halves default
to **opposite encodings**, so the consumer tries to Base64-decode plain text and garbles or
dead-letters the message — a silent, config-dependent failure.

**When the consumer is a Benzene Functions Queue trigger, build the `QueueClient` with Base64
encoding** so the two agree:

```csharp
var queueClient = new QueueClient(connectionString, queueName,
    new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
```

(Or, conversely, set `host.json` `extensions.queues.messageEncoding` to `none` on the consumer to
match a plain-text producer.) If both sides are your own non-Functions code, either encoding works as
long as they match.

## No `TokenCredential`/connection-string wrapping — deliberately
This package takes an already-built `QueueClient`, not a connection string or `TokenCredential` — the
caller chooses how to construct it (connection string, `DefaultAzureCredential`, the emulator).

## Dependencies
`Azure.Storage.Queues`; Benzene `Clients`, `Core.Messages`, `Core.Middleware`, `Results`.
