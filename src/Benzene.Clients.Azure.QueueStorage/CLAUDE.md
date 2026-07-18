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
  `IBenzeneClientContext<T,Void>` and `OutboundContext` overloads), `AddQueueStorageMessageClient`.

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

## No `TokenCredential`/connection-string wrapping — deliberately
This package takes an already-built `QueueClient`, not a connection string or `TokenCredential` — the
caller chooses how to construct it (connection string, `DefaultAzureCredential`, the emulator).

## Dependencies
`Azure.Storage.Queues`; Benzene `Clients`, `Core.Messages`, `Core.Middleware`, `Results`.
