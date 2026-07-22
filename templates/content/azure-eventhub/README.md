# Benzene Azure Functions (Event Hub trigger) starter

A Benzene service on Azure Functions (isolated worker) triggered by Azure Event Hubs events. One
trigger hands each batch of events to Benzene, which routes every event to a handler.

## Run it

```bash
# set EventHubConnection in local.settings.json (or use the Event Hubs emulator), then:
func start
```

Requires [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local).
Send an event to the `hello_world` hub whose body is a Benzene message envelope —
`{"topic":"hello:world","headers":{},"body":"{\"name\":\"world\"}"}` — and the handler prints
`Hello world!`. (To route by an event property instead of an envelope, swap the `Configure` wiring to
`eventHub.UseMessageHandlers()` — see `StartUp.cs`.)

The hub name and connection setting are on the `[EventHubTrigger]` in `EventHubFunction.cs`
(`local.settings.json` → `EventHubConnection`).

## What's here

- `HelloWorldMessageHandler.cs` — your business logic: a handler mapped to a topic
  (`[Message("hello:world")]`). The same handler shape runs behind Kafka, Service Bus, SQS, or HTTP.
- `StartUp.cs` — the transport wiring (`app.UseEventHub(...)`), reusable across hosts.
- `EventHubFunction.cs` — the one catch-all `[EventHubTrigger]` that forwards to Benzene.
- `Program.cs` / `host.json` / `local.settings.json` — the isolated-worker Functions host.

See `docs/azure-functions.md`.
