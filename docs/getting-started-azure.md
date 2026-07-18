# Getting Started: Benzene on Azure

The Azure Functions getting-started guide lives at **[Azure Functions Setup](azure-functions.md)** —
it starts from an empty folder and ends with a deployed isolated-worker Function App handling HTTP
plus every non-HTTP trigger (Event Hubs, Kafka, Service Bus, Cosmos DB Change Feed, Queue/Blob
Storage, Event Grid, Timer).

For consuming Azure messaging in a long-running process **without** Azure Functions (console app,
container, AKS), see the self-hosted workers in
[Worker Service Setup, Part B](getting-started-worker.md#part-b-built-in-workers-kafka-http-service-bus-event-hub-cosmos-db)
(Service Bus, Event Hubs, and Cosmos DB Change Feed).

Related Azure cookbooks:

- [Managed Identity & RBAC](cookbooks/managed-identity.md) — run every Azure integration with no
  connection strings
- [Service Bus Message Handling](cookbooks/service-bus-handling.md)
- [Event Hub Stream Processing](cookbooks/event-hub-processing.md)
- [Cosmos DB Change Feed Processing](cookbooks/cosmos-change-feed-processing.md)

> This page exists so the family-consistent `getting-started-azure` URL resolves; the canonical
> guide is [`azure-functions`](azure-functions.md).
