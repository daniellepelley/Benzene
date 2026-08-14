# Cloudflare Transport Bindings — research

**Date:** 2026-08-13
**Status:** research only; no implementation started. Answers "what could Benzene support from
Cloudflare beyond HTTP, and does Cloudflare have its own SQS/SNS/EventBridge/Kafka/Event Hub/
Service Bus?"

## 1. Where Benzene stands with Cloudflare today

[`docs/getting-started-cloudflare.md`](https://github.com/daniellepelley/benzene-dotnet/blob/main/docs/getting-started-cloudflare.md)
(benzene-dotnet, explicitly marked experimental/out-of-1.0-scope) covers exactly one path:
**HTTP only**, via [Cloudflare Containers](https://developers.cloudflare.com/containers/) — a
thin Worker (Cloudflare's V8-isolate compute) proxies HTTP requests into a normal Docker
container running Kestrel/`Benzene.AspNet.Core`, unchanged from any other container host. No
Cloudflare-specific Benzene package exists. That's the whole surface today: no queue, no event,
no stream binding.

## 2. The architectural fact that shapes everything

Cloudflare Workers has no .NET runtime, so a Benzene service can only run **inside a container**,
never inside a Worker. That matters because on Cloudflare, **bindings** (the mechanism that
connects a Worker to Queues, R2, D1, KV, Durable Objects, etc.) are a Workers-runtime concept — a
container cannot declare a binding in `wrangler.toml` and have it materialize inside the
container process the way an AWS Lambda gets `AWS_LAMBDA_*` env vars or an Azure Function gets a
trigger payload.

Cloudflare Containers bridges this with an **outbound handler**: the container makes a plain HTTP
request to a virtual hostname (e.g. `http://my.kv/some-key`), and a Worker-side handler
intercepts it and resolves the binding on the container's behalf. This works for KV, R2, D1, and
Durable Objects. It is a real path, but it means every such call round-trips through a Worker.

**The one load-bearing exception is Cloudflare Queues**, and it's the reason a real
transport binding is actually feasible without a Worker in the message path at all:

- Push-based consumption (a Worker's exported `queue()` handler) is Worker-only, as expected.
- But Queues also exposes **pull consumers** — a plain account-scoped REST API reachable from
  any HTTP client, anywhere: `POST /accounts/{id}/queues/{queue_id}/messages` (push, single or
  `/messages/batch`), `POST /messages/pull` (pull a batch, lease-based), `POST /messages/ack`
  (ack + retry by `lease_id`). This is architecturally the same shape as SQS long-polling, and
  it needs nothing but an HTTP client and an API token — no Worker relay, no outbound-handler
  proxy.

That single fact means a Benzene container can be a **first-class Queues producer and consumer**
using the exact same "self-hosted poller" shape Benzene already has for
`Benzene.Aws.Sqs`/`SqsConsumer` (long-poll loop → dispatch through the pipeline → ack/delete on
success, leave unacked for redelivery on failure) and the Go port's `awssqs.Consumer`. This is by
a wide margin the strongest, most spec-compatible transport-binding candidate Cloudflare offers.

## 3. Cloudflare's product lineup, mapped against the AWS/Azure vocabulary the question asked about

| Cloudflare product | Closest AWS/Azure analog | Verdict |
|---|---|---|
| **Queues** | **SQS** (point-to-point, at-least-once, batches, retries/delays, DLQ) | **Real transport-binding candidate** — pull-consumer HTTP API works from outside Workers. Closest and strongest match. |
| — (none) | **SNS** (fan-out: one publish, many independent subscriber queues) | **No first-class equivalent.** A Queue has one configured consumer path; fan-out to N independent consumer groups isn't a native primitive the way SNS→multiple SQS is. A producer wanting fan-out publishes to multiple queues itself. Worth an explicit "not verified beyond docs" flag before this is stated as a hard product limitation in any spec-facing doc. |
| **R2 Event Notifications** | **S3 Event Notifications** / **EventBridge** (narrowly) | **Real, narrow candidate.** Object-storage change events route to exactly one Queue, consumable the same pull-consumer way. Direct shape-match for `Benzene.Aws.Lambda.S3`'s `{bucket}:{eventType}` topic convention. |
| — (none) | **EventBridge** (schema registry, rule-based routing across many arbitrary sources/targets) | **No equivalent.** Nothing manages routing rules across heterogeneous event sources the way EventBridge does; "custom event routing" on Cloudflare is DIY (a Worker or container publishing to whichever Queue it chooses). |
| — (none, but see Pipelines) | **Kafka / MSK / Event Hubs** (durable ordered log, consumer-group replay) | **No managed equivalent exists today.** Cloudflare doesn't operate a Kafka-compatible broker. |
| **Pipelines** (GA-ish, built on the acquired Arroyo stream engine) | Loosely, a **Kinesis Firehose / Kafka Connect sink**, not Kafka itself | **Not a transport-binding candidate.** It's one-way: ingest events (HTTP or Worker binding) → SQL transform → land in R2 as Iceberg/Parquet. A "Kafka client source" is on Cloudflare's own roadmap but **not shipped** — even once it lands, Pipelines is a data-lake ingestion sink other apps write to, not a broker other Benzene services consume live *from*. Relevant to `Benzene.Mesh.*` analytics/usage-feed work far more than to the transport-bindings catalog. |
| **Durable Objects** | Nothing exact; closest is a stateful actor / **Service Bus session** or a **WebSocket gateway** | **Different shape entirely**, not a queue/topic binding. Single-instance stateful coordination + WebSocket Hibernation API. If Benzene ever wanted this, it would be a new *streaming-shaped* binding pattern (like the spec's Cosmos DB Change Feed binding — fanned-in, not topic-routed), not a drop-in queue adapter. Speculative; not scoped here. |
| **Workflows** (GA, built on Workers) | **Step Functions** / **Durable Functions** | **Outbound-client candidate, not an inbound transport.** Defining/running a Workflow is Worker-only, but it's reachable over its own REST API — the natural shape is a `Benzene.Clients.Cloudflare.Workflows` outbound client (`Send` starts a run), mirroring `Benzene.Clients.Aws.StepFunctions`'s existing pattern exactly. Not a binding a Benzene service is *invoked by*. |
| **Email Workers** | Nothing analogous in the existing catalog | Niche. Inbound email → Worker trigger. Only relevant if someone specifically wants "route inbound email to a Benzene topic"; not investigated further. |
| — (none) | **RabbitMQ / AMQP**, **Azure Service Bus** | **No managed equivalent.** Nothing broker-shaped with topics/subscriptions/sessions beyond what Queues already offers as a plain point-to-point queue. |

## 4. What a `Benzene.Cloudflare.Queues` binding would actually look like

Sketched against [`transport-bindings.md`](../docs/specification/transport-bindings.md) §1's
seven-point contract, and against the existing `Benzene.Aws.Sqs`/`awssqs.Consumer` self-hosted
poller as the closest sibling:

- **Context type**: same shape as the SQS/RabbitMQ self-hosted consumer — a per-message
  invocation context, one pipeline invocation and one DI scope per pulled message (§1.6).
- **Topic resolution — the one real design decision.** Cloudflare Queue messages have **no
  general per-message header/attribute channel** (confirmed: a message is `body` + `id` +
  `timestamp_ms` + `attempts` + a `metadata` object that holds Cloudflare-internal fields like
  `CF-sourceMessageSource`, not arbitrary user key/values) — the same situation Benzene already
  solved for **EventBridge** (embed `_benzeneHeaders` inside the JSON body) and **Azure Queue
  Storage** (publish the whole `wire.Request` envelope as the message body verbatim). A Queues
  binding would reuse the Azure Queue Storage convention directly: the outbound client publishes
  the full Benzene wire envelope (topic, headers, body) as the message's JSON `body`; the
  consumer deserializes that envelope back out. No new wire convention needed — this is a
  straight application of an existing pattern, not new design.
- **Body mapping**: the envelope's own body, per the existing envelope rules (wire-contracts §1).
- **Result mapping**: no response channel (fire-and-forget, like SQS/RabbitMQ) — success calls
  `POST /messages/ack`; failure leaves the message unacked for Cloudflare's own retry/DLQ
  handling (Queues natively supports `max_retries` + a configured dead-letter queue, so this
  maps cleanly).
- **Scope/failure rules**: identical to every existing queue-shaped binding — per-message scope,
  a batch fetch that dispatches N invocations, never crash the host on one bad message.
- **Outbound client**: `sendMessage` → `POST /messages` (or `/messages/batch` for a fan-out
  publish loop), same interface every other outbound client already implements
  (transport-bindings.md §"Outbound clients").

This is a **self-hosted worker/consumer binding**, not a platform-hosted one (no Lambda-style
runtime event to key off) — it belongs alongside `Benzene.Aws.Sqs`'s `Consumer` and
`Benzene.RabbitMq`'s `Consumer` in the catalog's shape, running inside the same container that
already hosts the HTTP binding. That also means it composes for free with the existing
Cloudflare Containers HTTP story: one container, `UseHttp(...)` for the Worker-proxied HTTP
surface and `UseCloudflareQueues(...)` for a background poll loop, same as a service today can
run both `UseHttp` and `UseAwsSqs`'s self-hosted consumer side by side.

An `R2EventNotifications` binding would be the same code path, one layer up: it's just a Queue
whose messages happen to originate from R2, so it needs no separate consumer implementation —
only documentation of the topic convention (`{bucket}:{eventType}`, matching
`Benzene.Aws.Lambda.S3`) and how to point a Queue's consumer config at the bucket's event
notification rule.

## 5. Bottom line

- **Yes, there is a real, spec-compatible transport Benzene could add for Cloudflare**: Queues,
  via its pull-consumer HTTP API, needing no Worker in the message-delivery path at all — a
  genuine gap in today's HTTP-only Cloudflare story.
- Cloudflare has **no SNS, no EventBridge, and no managed Kafka**. Its one general-purpose
  message-transport primitive is Queues (SQS-shaped), plus R2 Event Notifications as a narrow
  S3-Events-shaped derivative of it. Everything else in the AWS/Azure list the question named has
  no Cloudflare product to bind to.
- The topic-resolution design is not a new problem: it's a direct reapplication of the
  envelope-in-body convention Benzene already uses for EventBridge and Azure Queue Storage, both
  of which share Queues' lack of a native header channel.
- This was not scoped or estimated as a work package (the user asked for research, not a build) —
  if it's wanted, the natural next step is a WP-shaped brief mirroring
  [`third-party-tool-integrations-plan.md`](third-party-tool-integrations-plan.md)'s format, with
  benzene-dotnet as the home (new `Benzene.Cloudflare.Queues` package, self-hosted-module shape)
  and the `examples/Cloudflare` project as the place to prove it end-to-end.

## 6. Sources

- This repo/benzene-dotnet: `docs/specification/transport-bindings.md`,
  `docs/getting-started-cloudflare.md`, `examples/Cloudflare/`, the SQS/RabbitMQ binding
  descriptions in `transport-bindings.md` §2.
- Cloudflare: [Queues docs](https://developers.cloudflare.com/queues/) ·
  [Queues pull consumers](https://developers.cloudflare.com/queues/configuration/pull-consumers/) ·
  [Queues JavaScript APIs](https://developers.cloudflare.com/queues/configuration/javascript-apis/) ·
  [Queues REST API](https://developers.cloudflare.com/api/resources/queues/) ·
  [Containers: connect to Workers and bindings](https://developers.cloudflare.com/containers/platform-details/workers-connections/) ·
  [R2 event notifications](https://developers.cloudflare.com/r2/buckets/event-notifications/) ·
  [Pipelines](https://developers.cloudflare.com/pipelines/) ·
  [Cloudflare acquires Arroyo / Pipelines streaming ingestion](https://blog.cloudflare.com/cloudflare-acquires-arroyo-pipelines-streaming-ingestion-beta/) ·
  [Workflows GA](https://blog.cloudflare.com/workflows-ga-production-ready-durable-execution/)
- Note: direct fetches to `developers.cloudflare.com` were blocked by this session's network
  egress policy; findings above are synthesized from search-result snippets of those pages
  rather than the full primary source. Worth a direct-fetch confirmation pass (especially the
  Queues message-format/metadata fields and the "no SNS-style fan-out" claim) before this
  research becomes the basis of a spec-facing document.
