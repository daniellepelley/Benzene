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
| AWS SNS (`Benzene.Aws.Lambda.Sns`) | **Unsafe** — silently accepted, no retry | `SnsOptions.RaiseOnFailureStatus = true` — see [SNS Fan-Out Pattern](cookbooks/sns-fan-out.md) |
| AWS EventBridge (`Benzene.Aws.Lambda.EventBridge`) | **Unsafe, no opt-out** — silently accepted, no retry, no options class at all | None today — handler must throw instead |
| AWS Kafka/MSK (`Benzene.Aws.Lambda.Kafka`) | **Unsafe, no opt-out** — silently accepted, no per-record reporting despite AWS supporting it | None today — handler must throw instead |
| AWS S3 (`Benzene.Aws.Lambda.S3`) | **Unsafe, no opt-out** — silently accepted, no retry | None today — handler must throw instead |
| AWS SQS self-hosted worker (`Benzene.Aws.Sqs`'s `SqsConsumer`) | **Unsafe by default** (`SqsConsumerAckMode.WholeBatch`) — a failure result is deleted along with the rest of the batch | `SqsConsumerAckMode.PerMessage` |
| RabbitMQ self-hosted worker (`Benzene.RabbitMq`) | **Safe by default** (`RabbitMqAckMode.Explicit`) — a failure result (or exception) nacks the message for requeue/dead-letter, not ack | N/A (default is already safe); `RequeueOnFailure`/`AckMode` tune *how* it's redelivered, or `AckMode = AutoAck` to opt into at-most-once |
| Azure Service Bus Function trigger (`Benzene.Azure.Function.ServiceBus`) | **Unsafe** — auto-completed by default | `ServiceBusOptions.AckMode = Explicit` and/or `RaiseOnFailureStatus = true` |
| Azure Service Bus self-hosted worker (`Benzene.Azure.ServiceBus`) | **Unsafe** — auto-completed by default | `BenzeneServiceBusConfig.AckMode = ServiceBusConsumerAckMode.Explicit` |
| Azure Kafka/Event Hubs Function trigger (`Benzene.Azure.Function.Kafka`) | **Unsafe** — silently accepted by default | `KafkaOptions.RaiseOnFailureStatus = true` |
| Azure Event Grid (`Benzene.Azure.Function.EventGrid`) | **Unsafe, no opt-out** — silently accepted, no options class at all | None today — handler must throw instead |
| Azure Queue Storage (`Benzene.Azure.Function.QueueStorage`) | **Unsafe, no opt-out** — deleted like a success; the storage poison-queue never sees it | None today — handler must throw instead |
| Azure Event Hubs self-hosted worker (`Benzene.Azure.EventHub`) | **Unsafe, no opt-out** — checkpointed like a success | None today — handler must throw instead |
| Azure Cosmos DB Change Feed (`Benzene.Azure.Function.CosmosDb`) | N/A as a "failure result" concern — like Kinesis, this is a fan-in `StreamContext<TDocument>` with no `IBenzeneResult`/message-handler routing; the trigger's lease checkpoints the whole batch on any non-throwing return, and unlike Kinesis there's no per-document checkpoint API to opt out of that with (see the package's own `CLAUDE.md`) | Have the handler throw for anything that must redeliver the whole batch |
| HTTP / API Gateway | N/A — a failure result maps straight to an HTTP status code the caller sees synchronously; there's no async "retry" concept to opt into | N/A |

If a row says "no opt-out," that's a real gap, not a documented trade-off — the fix, until a
future release adds the missing knob, is to have the handler `throw` for any failure you need
retried instead of returning an `IBenzeneResult` failure.

## Why "we don't do that" is a feature

Every deliberate "no" above buys you something: you keep the full power of the tool you chose,
you're never blocked by a leaky abstraction, and the surface you *do* depend on stays small and
stable. When you need something Benzene doesn't ship, the extension model (custom middleware,
custom pipes, the getter/setter mapper pattern) is a supported, documented path — see
[Middleware](middleware.md) and [Common Middleware](common-middleware.md).

If you find yourself wishing Benzene abstracted a transport or a database away, that's usually the
signal to reach for the SDK directly inside your handler — which is exactly where Benzene's design
wants that code to live.
