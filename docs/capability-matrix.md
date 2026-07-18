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
| **Resilience** | `UseRetry` — retry with exponential backoff | Circuit breaker, timeout, bulkhead — these are best served by a mature library (Polly), which Benzene will not hide behind its own abstraction | Drop your own Polly `ResiliencePipeline` into a middleware step. Benzene gives Polly a clean place to plug in; it doesn't wrap it. See [Resilience](resilience.md). |
| **Sagas / workflows** | In-process, compensation-based saga with LIFO rollback (`Benzene.Saga`) | Durable crash-resume — the saga is in-memory `Func` closures that can't be re-hydrated after a process dies; the state store records progress for *observability*, not recovery | For crash-durable, long-running or human-in-the-loop workflows, use a real orchestrator (AWS Step Functions, or an Azure durable workflow). See [Sagas](cookbooks/sagas.md). |
| **Schema evolution** | The Confluent wire-format codec (solid, tested) + an `ISchemaRegistryClient` seam | Structural Avro/JSON backward-compatibility checking in-box — the shipped `TextualSchemaCompatibilityChecker` only accepts byte-identical schemas | Point at a real schema-registry server, or supply your own `ISchemaCompatibilityChecker`. See [Schema Registry](cookbooks/schema-registry.md). |
| **Database / state access** | *(nothing — by design)* | Any database or state-store abstraction — a database is not a transport, so wrapping one would hide its capabilities (the core anti-pattern) | Your handler uses its own SDK/ORM directly (`CosmosClient`, EF Core, Dapper, `DynamoDBContext`, …). Benzene delivers the event in; persistence is yours |
| **Configuration & secrets** | Provider-agnostic `ISecretStore` (env vars, mounted files, composed, cached) + fail-fast startup validation | Ship maintained cloud secret-store adapters (Key Vault / Secrets Manager / SSM) as packages | Copy the ~30-line adapter from [Secrets & Configuration](cookbooks/secrets-configuration.md) and reference the cloud SDK yourself (a shipped adapter is a candidate for post-1.0) |
| **Outbound HTTP** | `Benzene.Client.Http` middleware over an `HttpClient` you supply | Manage `HttpClient` lifetime for you (no built-in `IHttpClientFactory` wiring on the low-level path) | Register a typed/`IHttpClientFactory` client and pass it in; correlation/trace propagation is applied on the Benzene-message outbound path |
| **Distributed tracing** | W3C trace context on HTTP transports; OpenTelemetry, exporter-agnostic | (Today) inbound trace-context extraction on async transports (SQS/SNS/Kafka/Event Hub) — a known gap being worked | Until it lands, propagate correlation via message headers. See [Monitoring & Diagnostics](monitoring.md). |
| **Multi-tenancy** | *(nothing as a framework feature today)* | — (a candidate for post-1.0) | Roll a tenant-resolver middleware + a scoped tenant holder (the documented [Multi-Tenancy](cookbooks/multi-tenancy.md) pattern) |
| **AuthN / AuthZ** | OAuth2 bearer (JWT) validation with a strict algorithm allowlist + scope-based authorization (`Benzene.Auth.OAuth2`) | Ship a policy-engine (OPA/Cedar) adapter, or rate-limiting middleware | Add an authorization middleware calling your policy engine; the `Benzene.Auth.Core` seams are there to build on |

## Why "we don't do that" is a feature

Every deliberate "no" above buys you something: you keep the full power of the tool you chose,
you're never blocked by a leaky abstraction, and the surface you *do* depend on stays small and
stable. When you need something Benzene doesn't ship, the extension model (custom middleware,
custom pipes, the getter/setter mapper pattern) is a supported, documented path — see
[Middleware](middleware.md) and [Common Middleware](common-middleware.md).

If you find yourself wishing Benzene abstracted a transport or a database away, that's usually the
signal to reach for the SDK directly inside your handler — which is exactly where Benzene's design
wants that code to live.
