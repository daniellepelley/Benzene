# Client health checks — designs for the remaining transports

Companion to `work/client-health-checks-design.md`. That plan is **implemented** for every transport
whose reachability maps cleanly onto the "send-client names its resource at config time" template:
**SQS, SNS, EventBridge** (AWS) and **Queue Storage, Event Hub** (Azure) all auto-wire a non-destructive
reachability check onto the **dependency category** (deep `healthcheck` layer, never a probe — §3.2).

This document designs the transports that do **not** fit that template, grounded in what each underlying
library actually exposes. Each entry states: what "reachable" means, the concrete library call, the
config-time hook to auto-wire from (or why it must stay explicit), placement, and the open questions.

---

## 0. Cross-cutting decisions (decide once, apply to all below)

### 0.1 Consumers verify the *inbound* source — same discipline, still the deep layer
SQS/SNS/… are outbound **send** clients; the check verifies "can I reach the thing I publish to". Kafka
and RabbitMQ are inbound **consumer** workers; the check verifies "can I reach the broker I consume from".
The placement rule is unchanged: a broker being unreachable is **shared-fate** (every replica consumes
from the same broker), so it belongs on the deep `healthcheck` layer, never a liveness/readiness probe —
and for a pure worker there is no inbound Service to gate on anyway. A worker that can't reach its broker
simply has nothing to poll; restarting or de-routing it does not help.

### 0.2 The worker-startup seam already supports auto-wiring
`IBenzeneWorkerStartup : IRegisterDependency`, and `UseKafka`/`UseRabbitMq` already call
`app.Register(x => …)`. So the **exact same** `app.Register(x => x.AddDependencyHealthCheck(factory,
dedupKey))` hook used by the AWS/Azure send clients works on the worker seam — no new plumbing. This is
the key finding: Kafka and RabbitMQ **are** auto-wireable, not explicit-only.

### 0.3 Reuse the long-lived connection; do not open one per probe
The send-client checks are cheap because a describe/get-properties call reuses the SDK's pooled HTTP
connection. Broker checks must be equally careful: **do not open a fresh TCP/AMQP connection per probe.**
- RabbitMQ: reuse the worker's existing `IConnection` (open a lightweight channel for a passive declare,
  or just read `IConnection.IsOpen`), rather than `CreateConnectionAsync` per probe.
- Kafka: a fresh `AdminClient` per probe establishes broker connections each time — build it **once**
  (singleton) and reuse, or read metadata off the existing producer/consumer handle if one is exposed.
This makes §3.6 caching more relevant here than for the HTTP describe calls — see 0.4.

### 0.4 These are the checks that most want the §3.6 caching layer
A metadata/passive-declare round-trip at monitoring-scrape cadence, times every replica, is more load
than an HTTP describe. When the deep-layer caching seam lands (§3.6, currently blocked on per-topic
processor selection), the broker checks are its first customers. Until then, 0.3's connection reuse is
the mitigation.

### 0.5 `HealthCheckError` classification needs a per-library extractor
Each library signals permission/auth differently (there is no HTTP status):
- Kafka: `ErrorCode.TopicAuthorizationFailed` / `GroupAuthorizationFailed` / `ClusterAuthorizationFailed`
  → map to 403 → Warning. Other `KafkaException` → Failed with the `ErrorCode` name.
- RabbitMQ: `OperationInterruptedException` with reply code `403 (access-refused)` → Warning; `404
  (not-found)` for a passive declare on a missing queue → Failed. AMQP reply codes ARE 3-digit and align
  with the §3.9 policy, so they can be passed straight through as the status.
- gRPC: `StatusCode.PermissionDenied`/`Unauthenticated` → Warning; `Unavailable` → Failed.
Follow the Event Hub precedent (`work/client-health-checks-design.md` §4, Phase 4): the check does the
per-library extraction and calls the shared `HealthCheckError.Classify`.

---

## 1. Kafka (`Benzene.Kafka.Core`, Confluent.Kafka) — ✅ **IMPLEMENTED**

Shipped as designed: `KafkaHealthCheck` (cluster `GetMetadata` + subscribed-topic existence, `Type =
"Kafka"`, deps `("Broker", bootstrap)` + `("Topic", t)`), `IKafkaAdminClientFactory`/
`KafkaAdminClientFactory` (one admin client, lazily built + reused — captured at registration, not
per-probe), §3.9 auth→Warning classification, and auto-wiring on `UseKafka(..., healthCheck: true)` via
`AddKafkaDependencyHealthCheck` (dependency category, dedup `"Kafka:{bootstrap}"`). One refinement vs
the design below: **cluster-level** `GetMetadata` (not per-topic) — a per-topic metadata request can
trigger broker-side auto-topic-creation, so it would not be non-destructive; the cluster read + local
topic verification is safe. Original design notes retained below for context.



**What "reachable" means:** the consumer can reach the cluster's bootstrap brokers and its subscribed
topics exist. This is exactly what a Kafka consumer needs to make progress.

**Library call:** `IAdminClient.GetMetadata(TimeSpan)` (whole-cluster) or `GetMetadata(topic, TimeSpan)`
(per-topic — also proves the topic exists). Build the admin client from
`BenzeneKafkaConfig.ConsumerConfig.BootstrapServers` (Confluent config already carries the broker list and
any SASL/SSL credentials, so the check authenticates exactly as the consumer does). Read-only, no consume,
no commit.

**Auto-wire hook:** `UseKafka<TKey,TValue>(app, config, …)` already calls `app.Register(x => …)`. Add:
```csharp
app.Register(x => x.AddDependencyHealthCheck(
    _ => new KafkaHealthCheck(config.ConsumerConfig, config.Topics, adminClientFactory),
    $"Kafka:{config.ConsumerConfig.BootstrapServers}"));
```
with a `bool healthCheck = true` opt-out parameter, plus an explicit `AddKafkaHealthCheck(consumerConfig,
topics)` helper.

**Design specifics:**
- `Type = "Kafka"`; dependency `("Broker", bootstrapServers)` and one `("Topic", t)` per subscribed topic
  (so the mesh inventory shows the topic wiring). Consider a per-topic `GetMetadata(topic)` so a
  missing/unauthorized topic is caught, not just cluster reachability.
- **Build the `AdminClient` once (singleton), not per probe** (0.3). An `IKafkaAdminClientFactory` seam
  (mirroring `IKafkaConsumerFactory`) keeps it testable and lets the check reuse one admin client.
- Classify per 0.5.

**Open question:** cluster-only vs per-topic metadata. Per-topic is more useful (catches topic/ACL
problems) but N metadata calls; cluster-level is one call. Recommend per-topic, capped, with the topic
list from `config.Topics`.

---

## 2. RabbitMQ (`Benzene.RabbitMq`, RabbitMQ.Client v7 async) — ✅ **IMPLEMENTED**

Shipped: `RabbitMqHealthCheck` (passive `QueueDeclarePassiveAsync`, `Type = "RabbitMq"`, dep
`("Queue", config.QueueName)`), §3.9 via AMQP reply codes (403 → Warning, 404 → Failed), auto-wired on
`UseRabbitMq(..., healthCheck: true)`. The connection-reuse design work resolved as
`IRabbitMqConnectionProvider`/`RabbitMqConnectionProvider`: **one dedicated connection reused across
probes** (a cheap channel per probe), rather than sharing the worker's private connection — the same
dedicated-reused-handle trade-off as the Kafka admin client, chosen over a more invasive worker change.
The provider retries a failed connect (not a memoised `Lazy<Task>`) and re-opens a dropped connection.
Original design notes retained below.



**What "reachable" means:** the broker connection is up and the consumed queue exists.

**Library call (two tiers):**
- Cheapest: read `IConnection.IsOpen` on the worker's existing connection (RabbitMQ.Client auto-recovers,
  so this reflects live connectivity). Zero round-trip.
- Stronger: open a short-lived channel on the existing connection and `QueueDeclarePassiveAsync(queueName)`
  — passive declare neither creates nor mutates; it returns message/consumer counts and throws `404` if
  the queue is gone. This also proves the queue (from `RabbitMqConfig.QueueName`) still exists.

**Auto-wire hook:** `UseRabbitMq(app, config, connectionFactory, …)` calls `app.Register(x => …)`. Add the
same `AddDependencyHealthCheck` registration + `healthCheck` opt-out + explicit `AddRabbitMqHealthCheck`.

**Design specifics:**
- `Type = "RabbitMq"`; dependency `("Queue", config.QueueName)`.
- **Reuse the worker's `IConnection`** (0.3) — do NOT `CreateConnectionAsync` per probe. This needs the
  worker to expose its connection (or a shared connection holder) to the check via DI. If the current
  worker owns the connection privately, the design work is a small **shared connection holder** the worker
  and the check both resolve — analogous to the `PresetTopicHolder` scoped-holder pattern in
  `Benzene.Core.MessageHandlers`.
- Classify AMQP reply codes per 0.5 (403 → Warning, 404 → Failed).

**Open question:** passive-declare (stronger, needs a channel) vs `IsOpen` (free, connection-only). Recommend
passive-declare as the default (it verifies the actual queue), `IsOpen` as a documented lighter fallback.

---

## 3. gRPC (`Benzene.Grpc.Client`, Grpc.Net.Client) — ✅ **IMPLEMENTED (transport tier)**

Shipped: `GrpcHealthCheck` — the **transport-reachability** tier (a) — `GrpcChannel.ConnectAsync` against
the caller's DI-registered channel, `Type = "Grpc"`, dependency `("Grpc", channel.Target)`, auto-wired on
`AddGrpcClient(configureRoutes, healthCheck: true)`. An unreachable target times out → Failed; an
`RpcException` classifies by status (PermissionDenied/Unauthenticated → Warning). The `grpc.health.v1`
tier (b) is intentionally **not** shipped: it is transitive (belongs on `contracts`, not a probe/default)
and needs the `Grpc.HealthCheck` package (a new dependency). Original two-tier design retained below.



gRPC is the one where "health check" is genuinely ambiguous, and the choice interacts with §3.2 / the
`contracts` topic (§7 of the main plan).

- **(a) Transport reachability — the dependency-layer check.** `GrpcChannel.ConnectAsync(CancellationToken)`
  establishes the HTTP/2 connection to the downstream without issuing an app RPC. This is a true
  reachability signal ("can I open a channel to this address") and belongs on the deep `healthcheck` layer
  like the others. Dependency `("Grpc", channel.Target)`.
- **(b) Downstream `grpc.health.v1` Check — a contract/transitive check, NOT the dependency layer.**
  `new Health.HealthClient(channel).CheckAsync(new HealthCheckRequest{ Service = "" })` asks the downstream
  whether *it* is serving. That is **transitive** — the downstream aggregates its own dependencies — exactly
  the hazard §3.2/§7 flag for the CodeGen client contract check. So (b) belongs on the **`contracts`**
  diagnostic topic (monitoring/mesh), never a probe and not the auto-wired dependency default. It also
  requires the downstream to implement `grpc.health.v1` (needs the `Grpc.HealthCheck` package).

**Recommendation:** auto-wire **(a)** `ConnectAsync` on the dependency category from wherever the
`GrpcChannel` is registered (`AddGrpcClient` + the DI-registered channel). Offer **(b)** as an explicit
`AddGrpcContractCheck(...)` on the `contracts` topic (reusing the `Benzene.Clients.HealthChecks`
contract-check machinery), clearly documented as transitive. Do not make (b) a default.

**Open question:** `ConnectAsync` on a channel configured with a lazy/absent connection may block up to the
deadline; ensure the check passes the scoped cancellation token and a short per-check `Timeout` (the §3.5
DIM — already shipped).

---

## 4. Azure Event Grid (`Benzene.Clients.Azure.EventGrid`) — ✅ **RESOLVED: no check (documented)**

Decision shipped: **no** health check (documented in the package CLAUDE.md). `EventGridPublisherClient`
is publish-only — no data-plane read exists — and every alternative (management-plane `GetTopic`,
synthetic publish, TCP/DNS) is either the wrong dependency, side-effecting, or meaningless. An opt-in
`Active` synthetic-publish check remains a future option if demand appears. Rationale below.



`EventGridPublisherClient` is **publish-only** — there is no data-plane read (no describe/get-properties on
the topic endpoint). So there is no non-destructive reachability check analogous to the others:
- A **management-plane** check (`ARM`/`Azure.ResourceManager.EventGrid` `GetTopic`) would verify the topic
  exists, but pulls in a whole new SDK + management credentials the publisher doesn't have — wrong
  dependency for a data-plane client.
- A **synthetic publish** (send a throwaway event) is **side-effecting** — it fans out to subscribers — so
  it could only ever be an opt-in `Active`-mode check, never the non-destructive default.
- A bare **TCP/DNS reach** of the endpoint host proves almost nothing (fronted by shared Azure infra).

**Recommendation:** ship **no auto-wired check** for Event Grid. Document the limitation in its CLAUDE.md
(mirroring EventBridge's "reachability-only, no Active mode" note but inverted: "no cheap reachability
read exists"). If demand appears, offer an explicit opt-in `Active` synthetic-publish check with the
`⚠️ side-effecting` treatment, pointed at a dedicated no-subscriber probe topic.

---

## 5. AWS Lambda (`Benzene.Clients.Aws.Lambda`) — ✅ **RESOLVED: explicit-only (documented)**

Decision shipped: the Lambda client is a dynamic-target invoker (no fixed function at config time), so
auto-wiring doesn't apply — documented in the package CLAUDE.md. The explicit non-destructive
`AddLambdaHealthCheck(name)` (already §3.9-classified) is the path; a fixed-target client, if ever
added, would auto-wire there. Rationale below.



`.UseAwsLambda<T>()` carries **no function name** — the target function is supplied per-invocation
(`AwsLambdaClient.SendMessageAsync(request, lambdaName, …)`), i.e. the client is a *dynamic-target* invoker,
not a fixed "always call function X" client. So there is no single dependency to health-check at config
time, and auto-wiring would have to guess a target.

**Recommendation:** keep the existing explicit `AddLambdaHealthCheck(name)` (already non-destructive
`GetFunctionConfiguration` + §3.9 classified). **If** a fixed-target Lambda client is later introduced
(a `.UseAwsLambda<T>(functionName)` overload / `AddLambdaClient(functionName)` registration), auto-wire it
there with the standard pattern — that overload is the actual design work, and it should be driven by a
real use case, not added just to hang a check on.

---

## 6. AWS Step Functions (`Benzene.Clients.Aws.StepFunctions`) — ✅ **IMPLEMENTED**

Shipped both steps: (1) added the missing `AddStepFunctionsClient(arn)` DI-registration seam
(`IStepFunctionsClientFactory`/`IStepFunctionsClient`, resolving `IAmazonStepFunctions` from DI), and
(2) it auto-wires the existing non-destructive `DescribeStateMachine` check on the dependency category
(dedup `"StepFunctions:{arn}"`, `healthCheck: false` opt-out). The explicit `AddStepFunctionHealthCheck`
remains. Original design retained below.



Unlike the others there is **no `.Use*` pipeline extension** at all — the client is
`StepFunctionsClientFactory(stateMachineArn, …)`, constructed directly, and there is no
`AddStepFunctionsClient(arn)` DI-registration extension to hook. The state machine ARN *is* fixed per
client (unlike Lambda), so a check is meaningful; there is just nowhere to auto-wire it.

**Recommendation:** two steps, in order:
1. Add the missing registration extension `AddStepFunctionsClient(arn)` (mirrors `AddSqsMessageClient`) —
   worthwhile on its own for DI ergonomics.
2. Have it auto-wire `AddStepFunctionHealthCheck(arn)` (already non-destructive `DescribeStateMachine` +
   §3.9) on the dependency category, dedup `"StepFunctions:{arn}"`, `healthCheck` opt-out.
Until (1) exists, the explicit `AddStepFunctionHealthCheck(arn)` remains the path.

---

## 7. Azure Service Bus — ✅ **IMPLEMENTED (consumer-side auto-wire)**

Shipped: `Benzene.Azure.ServiceBus`'s `UseServiceBus(..., healthCheck: true)` auto-registers the existing
peek-based `ServiceBusHealthCheck` for the consumed entity on the dependency category (dedup
`"ServiceBus:{queue}"` / `"ServiceBus:{topic}/{subscription}"`), reusing one `ServiceBusClient` from the
factory. As designed, it wires on the **consumer** (Listen claim + known entity), not the sender. The
consumer package references `Benzene.HealthChecks.Azure.ServiceBus` directly (no breaking namespace move
needed). Also upgraded `ServiceBusHealthCheck` to §3.9: an `UnauthorizedAccessException` (no Listen) is
now a Warning, closing the last §3.9 gap. Original design retained below.



The **sender** (`.UseServiceBus`) must not auto-wire: it holds only the *Send* claim, but
`ServiceBusHealthCheck` peeks (needs *Listen*), and a topic sender has no subscription to peek
(producer/consumer mismatch — documented in the main plan §4).

The **consumer** side is the right home: the Service Bus worker/trigger
(`Benzene.Azure.ServiceBus` / `Benzene.Azure.Function.ServiceBus`) holds the Listen claim and knows its
queue or topic+subscription — exactly what `ServiceBusHealthCheck` (already shipped in
`Benzene.HealthChecks.Azure.ServiceBus`, peek-based, non-destructive, needs only Listen) verifies.

**Recommendation:**
1. Confirm the consumer's config-time seam exposes `.Register` + the entity identity (queue name, or
   topic+subscription) — the worker path likely does (worker-startup is `IRegisterDependency`); the
   Functions-trigger path may not have a config hook and would stay explicit.
2. Where the seam exists, auto-wire `ServiceBusHealthCheck` on the dependency category, dedup
   `"ServiceBus:{entity}"`, reusing the consumer's `ServiceBusClient`. This is the §3.7 co-location payoff —
   but note the check does **not** need to move packages for the consumer to reference it; the consumer can
   reference `Benzene.HealthChecks.Azure.ServiceBus` directly (lighter than a breaking namespace move).

---

## Status: all designed transports resolved ✅

Every transport in this document is now either **implemented** (Kafka, RabbitMQ, gRPC transport tier,
Step Functions, Service Bus consumer) or **resolved as intentionally check-less** (Event Grid — no
data-plane read; Lambda — dynamic target). The only deliberately-deferred item is gRPC's `grpc.health.v1`
tier (b), which needs the `Grpc.HealthCheck` dependency and belongs on the `contracts` topic, not an
auto-wired default. The original recommended sequencing is kept below for history.

## Recommended sequencing

1. **Kafka** — highest value (self-hosted/consumer interest), cleanly auto-wireable via the worker seam,
   `AdminClient.GetMetadata`. Ship the `IKafkaAdminClientFactory` singleton seam alongside.
2. **RabbitMQ** — same worker seam; the design work is the **shared connection holder** so the check reuses
   the worker's `IConnection` (0.3).
3. **gRPC (a) ConnectAsync** — dependency-layer reachability; keep (b) grpc.health.v1 as an explicit
   `contracts` check.
4. **Step Functions** — small: add `AddStepFunctionsClient(arn)`, then auto-wire.
5. **Service Bus consumer** — auto-wire on the worker seam if it exposes the entity + `.Register`.
6. **Event Grid, Lambda** — leave explicit; document the limitation. Revisit only on a concrete use case.

Cross-cutting prerequisite for 1–2 to be truly "low-cost": the **§3.6 deep-layer caching seam**
(per-topic processor selection). Worth doing before/alongside the broker checks.
