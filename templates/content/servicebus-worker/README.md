# Benzene Azure Service Bus Worker starter

A self-hosted Benzene worker that consumes an Azure Service Bus queue and dispatches each message to a
handler. A plain console app — Benzene owns the process (it uses the SDK's `ServiceBusProcessor`).

> This is the **self-hosted** consumer. If you want an **Azure Functions** Service Bus trigger instead,
> use the `benzene.azure.servicebus` template.

## Run it

```bash
export ServiceBus__ConnectionString="<your Service Bus connection string>"
dotnet run
```

The worker consumes the `hello_world` queue. Send a message with a `topic` application property of
`hello:world` and a JSON body `{"name":"world"}`, and the console prints `Hello world!`. For a local
loop, use the [Service Bus emulator](https://learn.microsoft.com/azure/service-bus-messaging/overview-emulator).

Queue name, concurrency, and ack mode are in `StartUp.cs` (`BenzeneServiceBusConfig`); the connection
string reads the `ServiceBus__ConnectionString` environment variable.

## What's here

- `HelloWorldMessageHandler.cs` — your business logic: a fire-and-forget handler mapped to a topic
  (`[Message("hello:world")]`), routed by the message's `topic` application property. The same handler
  shape runs behind Kafka, RabbitMQ, SQS, or HTTP.
- `StartUp.cs` — the transport wiring (`app.UseWorker(worker => worker.UseServiceBus(...))`).
- `Program.cs` — the generic host that runs the consumer as a background service.

See [docs/getting-started.md](https://github.com/daniellepelley/benzene) and `docs/azure-functions.md`.
