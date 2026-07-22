# Benzene Azure Functions (Queue Storage trigger) starter

A Benzene service on Azure Functions (isolated worker) triggered by Azure Storage Queue messages. One
trigger hands each message to Benzene, which routes it to a handler.

## Run it

```bash
# StorageConnection defaults to the local Azurite emulator (UseDevelopmentStorage=true):
azurite &            # or the Azure Storage emulator of your choice
func start
```

Requires [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local).
A Storage Queue message carries no properties, so the topic comes from a Benzene message envelope in the
body. Enqueue `{"topic":"hello:world","headers":{},"body":"{\"name\":\"world\"}"}` on the `hello-world`
queue and the handler prints `Hello world!`. (For a queue that always maps to one topic, use
`.UsePresetTopic("hello:world").UseMessageHandlers()` with a raw body — see `StartUp.cs`.)

The queue name and connection setting are on the `[QueueTrigger]` in `QueueFunction.cs`
(`local.settings.json` → `StorageConnection`).

## What's here

- `HelloWorldMessageHandler.cs` — your business logic: a handler mapped to a topic
  (`[Message("hello:world")]`). The same handler shape runs behind Kafka, Service Bus, SQS, or HTTP.
- `StartUp.cs` — the transport wiring (`app.UseQueueStorage(...)`), reusable across hosts.
- `QueueFunction.cs` — the one catch-all `[QueueTrigger]` that forwards to Benzene.
- `Program.cs` / `host.json` / `local.settings.json` — the isolated-worker Functions host.

See `docs/azure-functions.md`.
