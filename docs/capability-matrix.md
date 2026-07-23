# Capability Matrix — what Benzene does, deliberately doesn't, and how to fill the gap

Benzene is honest about its boundaries. This page is the single place that states, for each
common production concern, **what Benzene provides**, **what it deliberately does not do (and
why)**, and **how to solve the rest outside Benzene**. Nothing here is a hidden gap — these are
design decisions that follow directly from Benzene's philosophy.

## The one idea behind every row

Benzene abstracts at the **business-logic boundary** — you write a message handler once and host
it anywhere — and **never at the transport or storage boundary**. This is the opposite of
frameworks like Dapr that hide the third party behind a generic interface (a "queue", a "state
store") and, in doing so, hide that third party's best features. If you wrap SQS behind a generic
queue interface, you lose the SQS-specific capabilities that were the reason to choose SQS.

So Benzene's answer to "does it abstract X?" is usually a deliberate **no** — you keep full,
direct access to the underlying SDK and all its cloud-native capabilities, and Benzene stays out
of the way. When Benzene doesn't ship an adapter for something, **rolling your own is a
first-class, supported path**: a small piece of middleware or a custom pipe into the pipeline, not
an escape hatch.

Two corollaries you'll see below:

- **A database is not a transport.** Benzene delivers events *in* (an ingress adapter), but it
  does not do database access. Persisting state is your handler's own code (`CosmosClient`,
  EF Core, Dapper, the AWS SDK, …).
- **Some problems can't be solved at runtime inside Benzene at all.** Benzene instances are
  independent processes (separate Lambda invocations, separate containers) that don't know about
  each other. Anything requiring cross-instance coordination (distributed idempotency,
  exactly-once) needs external shared state with atomic semantics — and even then, races are
  inherent. Benzene won't pretend otherwise.

## The matrix

| Capability | What Benzene provides | What it deliberately does NOT do (and why) | How to solve the rest |
|---|---|---|---|
| **Transport features** | Full, direct access to the underlying SDK message/context on every adapter | Hide transport-specific capabilities behind a generic interface (the anti-pattern above) | Use the raw SDK feature directly — the context exposes the native message/event |
| **Message routing** | Topic-based dispatch to `[Message]` handlers; the same handler runs behind every transport | Impose a canonical message envelope on transports that already carry routing (Kafka topic, Service Bus properties) | For envelope-less transports (Queue Storage, Event Hubs bodies) use the Benzene envelope or a preset topic; otherwise route on the native key |
| **Idempotency** | `IIdempotencyStore` seam + `InMemoryIdempotencyStore` (single-process) + atomic-claim middleware | Cross-instance de-duplication — independent processes can't coordinate at runtime without external state, and adding shared state can just relocate the race, not remove it | An external store with an **atomic conditional write** (DynamoDB conditional put, Redis `SET NX`) keyed on message identity, **plus** handlers designed to be naturally idempotent. See [Idempotency](cookbooks/idempotency.md). |
| **Resilience** | `UseRetry` — retry with exponential backoff, an optional max-delay cap, and pluggable jitter (`RetryMiddleware.FullJitter()`); or the full Polly toolkit (circuit breaker, timeout, hedging, fallback, rate limiting) via `Benzene.Resilience.Polly`'s `.UseResiliencePipeline(...)` | Benzene does not re-implement or hide Polly behind its own abstraction — the Polly package runs *your* `ResiliencePipeline`, exposing its full configuration surface | For retry-only with zero extra dependency use `UseRetry`; for anything more add `Benzene.Resilience.Polly` and drop a Polly `ResiliencePipeline` into `.UseResiliencePipeline(...)` — it even bridges a returned failure result to Polly's outcome model. See [Polly Resilience Pipelines](cookbooks/polly-resilience.md) and [Resilience](resilience.md). |
| **Sagas / workflows** | In-process, compensation-based saga with LIFO rollback (`Benzene.Saga`) | Durable crash-resume — the saga is in-memory `Func` closures that can't be re-hydrated after a process dies; the state store records progress for *observability*, not recovery | For crash-durable, long-running or human-in-the-loop workflows, use a real durable orchestrator: AWS Step Functions, Azure Durable Functions, Temporal, or similar. Benzene's `Benzene.Clients.Aws.StepFunctions` package can *start* a Step Functions execution from a handler, but running/resuming the workflow itself is the orchestrator's job, not Benzene's. See [Sagas](cookbooks/sagas.md). |
| **Schema evolution** | The Confluent wire-format codec (solid, tested) + an `ISchemaRegistryClient` seam | Structural Avro/JSON backward-compatibility checking in-box — the shipped `TextualSchemaCompatibilityChecker` only accepts byte-identical schemas | Point at a real schema-registry server, or supply your own `ISchemaCompatibilityChecker`. See [Schema Registry](cookbooks/schema-registry.md). |
| **Database / state access** | *(nothing — by design)* | Any database or state-store abstraction — a database is not a transport, so wrapping one would hide its capabilities (the core anti-pattern) | Your handler uses its own SDK/ORM directly (`CosmosClient`, EF Core, Dapper, `DynamoDBContext`, …). Benzene delivers the event in; persistence is yours |
| **Configuration & secrets** | Provider-agnostic `ISecretStore` (env vars, mounted files, composed, cached) + fail-fast startup validation | Ship maintained cloud secret-store adapters (Key Vault / Secrets Manager / SSM) as packages | Copy the ~30-line adapter from [Secrets & Configuration](cookbooks/secrets-configuration.md) and reference the cloud SDK yourself (a shipped adapter is a candidate for post-1.0) |
| **Outbound HTTP** | `Benzene.Client.Http` middleware over an `HttpClient` you supply | Manage `HttpClient` lifetime for you (no built-in `IHttpClientFactory` wiring on the low-level path) | Register a typed/`IHttpClientFactory` client and pass it in; correlation/trace propagation is applied on the Benzene-message outbound path |
| **Distributed tracing** | W3C `traceparent`/`tracestate` inbound extraction on HTTP **and** async transports (SQS, SNS, Kafka — AWS Lambda, Azure Functions, and the self-hosted worker — and Event Hub, Azure Functions and the self-hosted worker); OpenTelemetry, exporter-agnostic | Sampling/backend-specific configuration (Jaeger, Application Insights, etc.) is yours to wire — Benzene exports via the standard OTel API, it doesn't ship a backend | `.UseW3CTraceContext()` as the first middleware on any pipeline continues a caller's trace instead of starting a new one per hop. See [Monitoring & Diagnostics](monitoring.md) and the [distributed tracing cookbook](cookbooks/distributed-tracing-opentelemetry.md). |
| **Retry-on-handler-failure-result** | Per-transport: some retry a returned `IBenzeneResult` failure (`IsSuccessful == false`) the same as an exception by default; some only retry it if you opt in; some can't retry it at all today. See the breakdown below — **the default is not uniform across transports, and on several it's silently unsafe.** | A single cross-transport reliability abstraction — retry semantics are inherently transport-native (Lambda batch-item-failure vs. Service Bus abandon vs. EventBridge destinations), so Benzene surfaces each transport's own mechanism rather than inventing one | Know your transport's default (below); opt in where an `Options`/`AckMode` knob exists; if none exists, have the handler throw instead of returning a failure result for anything that must be retried. **Any handler that can be retried needs to be idempotent** — see the Idempotency row above. |
| **Multi-tenancy** | *(nothing as a framework feature today)* | — (a candidate for post-1.0) | Roll a tenant-resolver middleware + a scoped tenant holder (the documented [Multi-Tenancy](cookbooks/multi-tenancy.md) pattern) |
| **AuthN / AuthZ** | OAuth2 bearer (JWT) validation with a strict algorithm allowlist + scope-based authorization (`Benzene.Auth.OAuth2`) | Ship a policy-engine (OPA/Cedar) adapter, or rate-limiting middleware | Add an authorization middleware calling your policy engine; the `Benzene.Auth.Core` seams are there to build on |

### Retry-on-handler-failure-result — the per-transport breakdown

"Failure result" below means your handler returned `IsSuccessful == false` (e.g.
`BenzeneResult.ServiceUnavailable(...)`) — **not** a thrown exception. Every transport in Benzene
retries an unhandled *exception* by default (that's what makes it propagate to the host's own
retry machinery); what varies is what happens to a *returned* failure result, which is the
surprising case because nothing crashed.

| Transport | Default on a returned failure result | Opt-in fix |
|---|---|---|
| AWS Lambda SQS (`Benzene.Aws.Lambda.Sqs`) | **Safe** — retried per-message via `ReportBatchItemFailures`, same as an exception | N/A (default is already safe); `SqsOptions.BatchFailureMode = FailWholeBatch` to change the *shape* of the retry, not to enable it |
| AWS DynamoDB Streams (`Benzene.Aws.Lambda.DynamoDb`) | **Safe** — sequential processing stops at the first failed record and reports it for redelivery | N/A |
| AWS Kinesis (`Benzene.Aws.Lambda.Kinesis`) | **Safe by design** — the stream checkpoints only where your handler explicitly checkpoints; an unfailed-but-uncheckpointed record is redelivered | N/A — see [Benzene.Aws.Lambda.Kinesis's CLAUDE.md](../src/Benzene.Aws.Lambda.Kinesis/CLAUDE.md) |
| AWS SNS (`Benzene.Aws.Lambda.Sns`) | **Safe by default** — `SnsOptions.RaiseOnFailureStatus` defaults `true`, escalating a failure result into a throw so SNS's subscription retry/redrive applies | `SnsOptions.RaiseOnFailureStatus = false` for at-most-once — see [SNS Fan-Out Pattern](cookbooks/sns-fan-out.md) |
| AWS EventBridge (`Benzene.Aws.Lambda.EventBridge`) | **Safe by default** — `EventBridgeOptions.RaiseOnFailureStatus` defaults `true`, escalating a failure result into a throw so EventBridge's retry/DLQ applies | `EventBridgeOptions.RaiseOnFailureStatus = false` for at-most-once |
| AWS Kafka/MSK (`Benzene.Aws.Lambda.Kafka`) | **Safe by default** — `KafkaOptions.BatchFailureMode` defaults to `PartialBatchFailure`, reporting each failed partition's resume offset via the batch response (needs `ReportBatchItemFailures` on the event source mapping, as with SQS) so records are redriven from the failure, honouring per-partition ordering | `KafkaOptions.BatchFailureMode = FailWholeBatch` (throws) when the event source mapping isn't configured for `ReportBatchItemFailures` |
| AWS S3 (`Benzene.Aws.Lambda.S3`) | **Safe by default** — `S3Options.RaiseOnFailureStatus` defaults `true`, escalating a failure result into a throw so the Lambda invocation fails and the event's async retry/DLQ applies | `S3Options.RaiseOnFailureStatus = false` for at-most-once |
| AWS SQS self-hosted worker (`Benzene.Aws.Sqs`'s `SqsConsumer`) | **Safe by default** — `SqsConsumerOptions.AckMode` defaults to `SqsConsumerAckMode.PerMessage`, so only successfully-handled messages are deleted and a failure result is left for redelivery | `SqsConsumerAckMode.WholeBatch` for at-most-once (delete the whole batch regardless) |
| RabbitMQ self-hosted worker (`Benzene.RabbitMq`) | **Safe by default** (`RabbitMqAckMode.Explicit`) — a failure result (or exception) nacks the message for requeue/dead-letter, not ack | N/A (default is already safe); `RequeueOnFailure`/`AckMode` tune *how* it's redelivered, or `AckMode = AutoAck` to opt into at-most-once |
| Azure Service Bus Function trigger (`Benzene.Azure.Function.ServiceBus`) | **Safe by default** — although `AckMode` defaults to `AutoComplete`, `ServiceBusOptions.RaiseOnFailureStatus` defaults `true`, so a failure result throws (→ the message is abandoned, not completed) | `RaiseOnFailureStatus = false` for at-most-once; `AckMode = Explicit` for true per-message complete/abandon control |
| Azure Service Bus self-hosted worker (`Benzene.Azure.ServiceBus`) | **Safe by default** — `BenzeneServiceBusConfig.AckMode` defaults to `ServiceBusConsumerAckMode.Explicit`, so a failure result (or throw) abandons the message for redelivery | `AckMode = ServiceBusConsumerAckMode.AutoComplete` for at-most-once |
| Azure Kafka/Event Hubs Function trigger (`Benzene.Azure.Function.Kafka`) | **Safe by default** — `KafkaOptions.RaiseOnFailureStatus` defaults `true`, escalating a failure result into a throw so the trigger re-delivers | `KafkaOptions.RaiseOnFailureStatus = false` for at-most-once |
| Azure Event Grid (`Benzene.Azure.Function.EventGrid`) | **Safe by default** — `EventGridOptions.RaiseOnFailureStatus` defaults `true`, escalating a failure result into a throw so Event Grid's retry/dead-letter (backoff up to 24h) applies | `EventGridOptions.RaiseOnFailureStatus = false` for at-most-once |
| Azure Event Hubs Function trigger (`Benzene.Azure.Function.EventHub`) | **Safe by default** — `EventHubOptions.RaiseOnFailureStatus` defaults `true`, escalating a failure result (on both the property-based and envelope routing paths) into a throw so the trigger re-delivers the batch | `EventHubOptions.RaiseOnFailureStatus = false` for at-most-once |
| Azure Queue Storage (`Benzene.Azure.Function.QueueStorage`) | **Safe by default** — `QueueStorageOptions.RaiseOnFailureStatus` defaults `true`, escalating a failure result (on both the preset-topic and envelope routing paths) into a throw so the host's `maxDequeueCount` retry/poison handling applies | `QueueStorageOptions.RaiseOnFailureStatus = false` for at-most-once |
| Azure Event Hubs self-hosted worker (`Benzene.Azure.EventHub`) | **Safe by default** — `BenzeneEventHubConfig.RaiseOnFailureStatus` defaults `true`, so a failure result is not checkpointed and the partition is re-delivered | `BenzeneEventHubConfig.RaiseOnFailureStatus = false` for at-most-once |
| Google Cloud Pub/Sub (`Benzene.GoogleCloud.Functions.PubSub`) | **Safe by default** — `PubSubOptions.RaiseOnFailureStatus` defaults `true`, escalating a failure result into a throw so Pub/Sub redelivers per the subscription's ack-deadline/retry policy | `PubSubOptions.RaiseOnFailureStatus = false` for at-most-once |
| Azure Cosmos DB Change Feed (`Benzene.Azure.Function.CosmosDb`) | N/A as a "failure result" concern — like Kinesis, this is a fan-in `StreamContext<TDocument>` with no `IBenzeneResult`/message-handler routing; the trigger's lease checkpoints the whole batch on any non-throwing return, and unlike Kinesis there's no per-document checkpoint API to opt out of that with (see the package's own `CLAUDE.md`) | Have the handler throw for anything that must redeliver the whole batch |
| HTTP / API Gateway | N/A — a failure result maps straight to an HTTP status code the caller sees synchronously; there's no async "retry" concept to opt into | N/A |

As of the 1.0 settlement contract (see `work/settlement-contract-1.0.md`), every async/event
transport is **safe by default**: a returned failure result is redelivered (at-least-once), not
silently settled, and each transport exposes an explicit opt-out (the "Opt-out" column) for the
at-most-once cases where you deliberately want a returned failure accepted. The two rows marked
N/A above (Cosmos DB Change Feed, HTTP) have no per-item async-retry knob to opt into — there, a
handler that `throw`s is the escape hatch for anything that must redeliver.

## Why "we don't do that" is a feature

Every deliberate "no" above buys you something: you keep the full power of the tool you chose,
you're never blocked by a leaky abstraction, and the surface you *do* depend on stays small and
stable. When you need something Benzene doesn't ship, the extension model (custom middleware,
custom pipes, the getter/setter mapper pattern) is a supported, documented path — see
[Middleware](middleware.md) and [Common Middleware](common-middleware.md).

If you find yourself wishing Benzene abstracted a transport or a database away, that's usually the
signal to reach for the SDK directly inside your handler — which is exactly where Benzene's design
wants that code to live.
