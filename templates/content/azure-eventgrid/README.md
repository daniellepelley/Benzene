# Benzene Azure Functions (Event Grid trigger) starter

A Benzene service on Azure Functions (isolated worker) triggered by Azure Event Grid events. One
trigger hands each event to Benzene, which routes it to a handler **by the event's type** (unlike the
queue transports, which route by a `topic` header). Handles both Event Grid schema and CloudEvents 1.0.

## Run it

```bash
func start
```

Requires [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local).
Event Grid pushes events to the function's endpoint, so there's no connection string. To try it locally,
POST an event to the running function, e.g.:

```bash
curl -X POST http://localhost:7071/runtime/webhooks/EventGrid?functionName=event-grid \
  -H "aeg-event-type: Notification" -H "Content-Type: application/json" \
  -d '[{"id":"1","eventType":"hello.world","subject":"","dataVersion":"1.0","eventTime":"2020-01-01T00:00:00Z","data":{"name":"world"}}]'
# → the handler prints "Hello world!"
```

The handler matches the event's `eventType` (`hello.world`) via `[Message("hello.world")]` — swap it for
a real event type (e.g. `Microsoft.Storage.BlobCreated`) to react to Azure resource events.

## What's here

- `HelloWorldMessageHandler.cs` — your business logic: a handler mapped to an **event type**
  (`[Message("hello.world")]`), receiving the event's `data` payload.
- `StartUp.cs` — the transport wiring (`app.UseEventGrid(...)`), reusable across hosts.
- `EventGridFunction.cs` — the one catch-all `[EventGridTrigger]` that forwards to Benzene.
- `Program.cs` / `host.json` / `local.settings.json` — the isolated-worker Functions host.

See `docs/azure-functions.md`.
