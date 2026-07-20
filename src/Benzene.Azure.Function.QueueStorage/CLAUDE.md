# Benzene.Azure.Function.QueueStorage

## What this package does
Inbound Azure Queue Storage adapter for the Azure Functions `QueueTrigger` binding (isolated
worker): delivers a queue message to a Benzene middleware pipeline. The Azure counterpart of
`Benzene.Aws.Lambda.Sqs` in spirit, but structurally closer to `Benzene.Azure.Function.EventHub` —
see "Routing" below for why.

## ⚠️ Unsafe by default, and there is no opt-out: a handler failure result is always silently dropped
There is no `Options` class here (unlike `Benzene.Aws.Lambda.Sqs`, whose `SqsOptions.BatchFailureMode`
defaults to retrying failed records). If a handler returns a non-exception failure result (e.g.
`BenzeneResult.ServiceUnavailable(...)`), nothing in this package inspects it — the message is
deleted like any success. Queue Storage's own poison-queue/`maxDequeueCount` machinery (see
"Failure handling" below) only ever sees an **unhandled exception**, never a returned failure
result — so a handler that "fails gracefully" via `IBenzeneResult` instead of throwing gets none of
that retry/poison-queue protection.

## Zero dependencies — deliberately
References only `Benzene.Azure.Function.Core` + `Benzene.Core.MessageHandlers` — no storage SDK,
no Functions extension package (same approach as `Benzene.Azure.Function.CosmosDb` and
`Benzene.Aws.Lambda.Kinesis`'s hand-rolled event model). The consumer's Function App project
references `Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues` itself for the
`[QueueTrigger]` attribute, then calls `HandleQueueMessage(...)` with what the binding delivered.
Do not add SDK packages here without asking first (repo NuGet policy).

## Routing — the body is the entire message
A Queue Storage message has **no properties/attributes** (unlike Service Bus application
properties or SQS message attributes) — just a body. So `QueueStorageMessageTopicGetter` always
returns null, and routing comes from exactly two places:

1. **A Benzene message envelope in the body** — `queue.UseBenzeneMessage(direct =>
   direct.UseMessageHandlers())`, via `BenzeneMessageQueueStorageHandler` (mirrors
   `BenzeneMessageEventHubHandler`: deserializes the text into a `BenzeneMessageRequest`, defers
   to the next middleware if it isn't one).
2. **A fixed per-queue topic** — `queue.UsePresetTopic("orders.created").UseMessageHandlers()`,
   for queues whose producer isn't a Benzene client (a queue usually carries one message type
   anyway). Works because `AddAzureQueueStorage` wraps the null topic getter in
   `PresetTopicMessageTopicGetter` + registers the full mapper set (empty headers, text body,
   result setter, version getter) — so `.UseMessageHandlers()` resolves cleanly.

## Key types
- `QueueStorageMessage` — Benzene's own dependency-free message model: `MessageText` (the common
  `[QueueTrigger] string` binding) plus optional `MessageId`/`DequeueCount`/`InsertedOn` for
  callers who bind the SDK's `QueueMessage` and want the metadata carried across.
- `QueueStorageContext : IHasMessageResult` — wraps one message; `MessageResult` is
  diagnostics-only (the trigger has no per-message settlement — success deletes the message,
  an exception retries it and eventually the host moves it to `<queue>-poison`).
- `QueueStorageApplication` — `EntryPointMiddlewareApplication<QueueStorageMessage[]>` fanning
  out via `MiddlewareMultiApplication`, transport-tagged `"queue-storage"`. The array event shape
  exists for tests/multi-dispatch; the trigger itself delivers one message per invocation.
- `UseQueueStorage(action, maxDegreeOfParallelism = null)` (both `IAzureFunctionAppBuilder` and
  platform-neutral `IBenzeneApplicationBuilder`, no-op off-Azure), `AddAzureQueueStorage()`,
  `QueueStorageRegistrations`, `HandleQueueMessages(params QueueStorageMessage[])`,
  `HandleQueueMessage(string messageText)`. `maxDegreeOfParallelism` optionally bounds fan-out
  concurrency (routed through `Benzene.Core.Middleware`'s `BoundedFanOut`); it only bites on
  multi-message dispatch - the trigger's default one-message-per-invocation delivery has nothing to
  bound.

## Failure handling
Deliberately none in this package: a pipeline exception propagates to the Functions host, whose
own retry (`maxDequeueCount`, visibility timeout in `host.json`) and poison-queue machinery is the
per-message failure story. There is no options class — unlike Service Bus there's no
settlement/batching dimension to configure.

## No TestHelpers package
Deliberate: the transport message is a plain string, so
`Benzene.Core.Messages.TestHelpers`' `AsBenzeneMessage(serializer)` (serialized to text) is
already the whole helper — a `.TestHelpers` package would be an identity function.

## Tests
- `test/Benzene.Core.Test/Azure/QueueStoragePipelineTest.cs` — envelope routing, preset-topic
  routing of raw payloads (also proves the `AddAzureQueueStorage` registration set is complete
  for `.UseMessageHandlers()`), non-envelope deferral, exception propagation, metadata flow.
