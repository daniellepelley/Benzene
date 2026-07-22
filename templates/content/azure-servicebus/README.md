# Benzene Azure Functions (Service Bus trigger) starter

A Benzene service on Azure Functions (isolated worker) triggered by Azure Service Bus messages. One
trigger hands every message to Benzene, which routes it to a handler by the message's `topic`
application property — so you add handlers, not Functions.

> This is the **Azure Functions** trigger. For a plain long-running consumer that owns its own process
> (no Functions runtime), use the `benzene.servicebus.worker` template instead.

## Run it

```bash
# set ServiceBusConnection in local.settings.json (or use the Service Bus emulator), then:
func start
```

Requires [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local).
Send a message to the `hello_world` queue with a `topic` application property of `hello:world` and body
`{"name":"world"}` — the handler prints `Hello world!`.

The queue name and connection setting are on the `[ServiceBusTrigger]` in `ServiceBusFunction.cs`
(`local.settings.json` → `ServiceBusConnection`).

## What's here

- `HelloWorldMessageHandler.cs` — your business logic: a handler mapped to a topic
  (`[Message("hello:world")]`). It knows nothing about Service Bus — the same handler shape runs behind
  Kafka, RabbitMQ, SQS, or HTTP.
- `StartUp.cs` — the transport wiring (`app.UseServiceBus(...)`), reusable across hosts.
- `ServiceBusFunction.cs` — the one catch-all `[ServiceBusTrigger]` that forwards to Benzene.
- `Program.cs` / `host.json` / `local.settings.json` — the isolated-worker Functions host.

See `docs/azure-functions.md`.
