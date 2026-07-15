# BenzeneStarter

A self-hosted [Benzene](https://github.com/daniellepelley/Benzene) worker that consumes a Kafka
topic directly (via `Confluent.Kafka`), generated from the `benzene.kafka.worker` template.

## Run it locally

Needs a Kafka broker.
[`examples/Kafka/docker-compose.yaml`](https://github.com/daniellepelley/Benzene/blob/main/examples/Kafka/docker-compose.yaml)
in the Benzene repo brings up a single-broker Confluent Kafka cluster on `localhost:9092` (matching
this project's default `BootstrapServers`), plus [Kafdrop](https://github.com/obsidiandynamics/kafdrop)
at `http://localhost:19000` for inspecting topics:

```bash
docker compose -f docker-compose.yaml up -d   # from a copy of examples/Kafka
dotnet run
```

Produce a message onto the `hello_world` topic (any Kafka producer works) and watch it print.

## Where to go next

- **`HelloWorldMessageHandler.cs`** is where your logic goes - replace it, or add more handlers
  alongside it. Remember: `[Message("...")]` must equal the literal Kafka topic name for this
  transport (not a colon-separated topic id like HTTP/SQS/SNS use).
- **`StartUp.cs`** configures the consumer (`BootstrapServers`, `GroupId`, `Topics`,
  `ConcurrentRequests`) - update it to point at your own broker.
- To send messages from another Benzene service instead of a plain producer, see
  `KafkaBenzeneMessageClient` in the full guide.
- Full guide, plus the AWS Lambda (MSK) and Azure Functions Kafka trigger variants:
  [Kafka Setup](https://github.com/daniellepelley/Benzene/blob/main/docs/getting-started-kafka.md)
