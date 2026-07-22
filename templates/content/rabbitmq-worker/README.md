# Benzene RabbitMQ Worker starter

A self-hosted Benzene worker that consumes a RabbitMQ queue and dispatches each message to a handler.
A plain console app — Benzene owns the process; there's no web framework or serverless runtime.

## Run it

```bash
# a local broker (management UI on http://localhost:15672, guest/guest):
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management

dotnet run
```

The worker consumes the `hello_world` queue (assumed to already exist — declare it in the management
UI or from your producer). Publish a message with a `topic` header of `hello:world` and a JSON body
`{"name":"world"}`, and the console prints `Hello world!`.

Broker URI and queue name are in `StartUp.cs` (`RabbitMqConfig` / `RabbitMqConnectionFactory`); the URI
also reads the `RabbitMq__Uri` environment variable.

## What's here

- `HelloWorldMessageHandler.cs` — your business logic: a fire-and-forget handler mapped to a topic
  (`[Message("hello:world")]`). It knows nothing about RabbitMQ — the same handler shape runs behind
  Kafka, Azure Service Bus, SQS, or HTTP.
- `StartUp.cs` — the transport wiring (`app.UseWorker(worker => worker.UseRabbitMq(...))`).
- `Program.cs` — the generic host that runs the consumer as a background service.

See [docs/getting-started.md](https://github.com/daniellepelley/benzene).
