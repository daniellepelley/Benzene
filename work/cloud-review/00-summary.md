# Benzene cloud/transport review — cross-cutting synthesis

Comparison of every Benzene transport/third-party integration against the provider's own
documentation and design intent, looking for **divergences** (Benzene does something contrary to how
the provider intends the service to be used), **missing core functionality**, and **wrong-approach**
choices. One focused reviewer per transport cluster; per-transport detail in the sibling files
`01`–`12`.

## Method & caveats
- 13 transport clusters reviewed: AWS (SQS, SNS, Lambda event sources, API Gateway, egress clients),
  Azure (Service Bus, Event Hub, Functions platform, egress clients + Cosmos), Kafka, RabbitMQ, gRPC,
  HTTP (self-host / client / GCF).
- Every finding is grounded in the actual Benzene source (file-cited). Provider behavior was verified
  against official docs where reachable. **Caveat:** the agent proxy allowlist blocked direct fetches
  of `learn.microsoft.com` and `docs.aws.amazon.com` for some reviewers (403); those fell back to web
  search + canonical SDK knowledge. The *code* findings are solid regardless; a couple of the
  provider-behavior citations (esp. Azure Service Bus) are from established SDK semantics rather than a
  fresh doc fetch and are worth a second glance before acting.
- Overall posture: **the consumers are generally well-engineered and honest** — most limitations are
  disclosed in each package's CLAUDE.md. The recurring problems cluster on the **producer/egress**
  side and in **settlement semantics**, and several are *internal-consistency* bugs where Benzene's own
  producer and consumer halves don't interoperate.

---

## The seven cross-cutting themes

### 1. "Silent success on a non-throwing failure" — the dominant correctness theme
Across almost every messaging transport, a handler that *returns* a failure `IBenzeneResult` (rather
than **throwing**) has its message acked / completed / checkpointed / deleted — i.e. silently lost —
because only a thrown exception is treated as "don't settle." Appears in: **SQS** (WholeBatch default +
null-result path), **Service Bus** (AutoComplete default — Critical), **S3**, **EventBridge**, **Event
Hub**, **Kafka** worker (at-most-once default), **Azure Event Grid** trigger, **Azure Queue Storage**
trigger, **Cosmos** change feed.
- Benzene's answer is **inconsistent**: some packages expose an opt-in escalation (`RaiseOnFailureStatus`,
  `AckMode.Explicit`, `CommitOnlyOnSuccess`), others (Event Grid trigger, Queue Storage trigger, S3,
  EventBridge) have none. The inconsistency is itself a defect — the same handler shape has different
  data-safety depending on transport.
- **Recommendation:** define one settlement contract for the whole library — a returned failure result
  should map to not-settled by default (or, at minimum, every transport should expose the *same*
  opt-in with the *same* name), and the getting-started path should make the safe mode the default.

### 2. Producers drop the routing / partition / ordering key the consumers rely on
Benzene's consumers carefully preserve ordering and routing, but the **producers don't emit the key
that makes it work end-to-end**:
- **SNS** publisher never writes the `topic` attribute its own Lambda consumer routes on → a
  Benzene→Benzene SNS round-trip routes to a **null topic** (internal-consistency bug, High).
- **Event Hub** producer sets no partition key → events scatter across partitions; the per-partition
  ordering the consumer advertises is **unreachable end-to-end** (High).
- **Kafka** producer never sets `Message.Key` → same story, no partition affinity/ordering (High).
- **Service Bus** sender sets no `SessionId` → cannot even produce to a session-enabled entity.
- **SQS** does send the topic correctly, but forwards every header as a message attribute with no guard
  against the **10-attribute cap** (trace headers + topic + status easily exceed it).

### 3. Two internal-consistency bugs — Benzene's own halves don't interoperate
Worth calling out on their own because they break the "publish with Benzene, consume with Benzene" story
out of the box, silently and config-dependently:
- **SNS**: producer omits the `topic` attribute the consumer requires (theme 2 above).
- **Azure Queue Storage**: the egress client defaults to **plain-text** message encoding while
  Benzene's own Functions Queue trigger defaults to **Base64** — opposite defaults, so the JSON
  envelope fails to decode and garbles/dead-letters. Undocumented.

### 4. Header/value corruption via over-eager lowercasing
The same root bug in two places, both corrupting case-sensitive values:
- **Self-host HTTP** (`Benzene.SelfHost.Http`) lowercases header **values *and* query-string values**
  (`InternalExtensions.ToDictionary` calls `.ToLowerInvariant()` on the value) — corrupts
  `Authorization` tokens, `Cookie`, `ETag`, `traceparent`, signed URLs, and throws on case-only query
  key collisions. **Critical** for that transport.
- **Azure Function AspNet** message-headers getter lowercases header values likewise (Low–Medium there).
- **Recommendation:** lowercase the *key* only, everywhere; preserve values verbatim.

### 5. Binary / payload-format fidelity is text-only
- **API Gateway**: HTTP API **payload format v2 unsupported** (a default-config HTTP API fails hard with
  a misleading "event not recognized"); binary request bodies UTF-8-corrupted; binary responses
  impossible (`IsBase64Encoded` never set). All High.
- **Self-host HTTP**: bodies are `string` end-to-end (binary corrupted), request fully buffered with
  **no size limit** (memory-exhaustion DoS).
- **Blob trigger**: `byte[]`-only, no `Stream` binding → large blobs fully buffered.
- **Queue Storage**: Base64 mismatch (theme 3).

### 6. Batch / throughput primitives missing on nearly every producer
Almost every egress client sends one message per call, forgoing the provider's batch primitive: **SNS**
`PublishBatch` (10), **SQS** `SendMessageBatch` (10), **EventBridge** `PutEvents` (10), **Event Hub**
`EventDataBatch`, **Event Grid** `SendEventsAsync`, **Service Bus** `ServiceBusMessageBatch`. Individually
low-severity, collectively a real cost/throughput ceiling at scale.

### 7. Provider-native reliability features unmapped
Per transport, first-class features with no Benzene story: **FIFO/dedup** (SQS, SNS, Service Bus),
**sessions** (Service Bus — *Critical*, can't consume session entities at all), **explicit dead-letter
verbs** (Service Bus abandon-loops; Kafka/RabbitMQ have no DLQ/retry-topic), **publisher confirms +
persistent delivery** (RabbitMQ — transient by default, loss on broker restart), **visibility/lock
heartbeat for long handlers** (SQS, Service Bus), **Lambda `FunctionError`** (never inspected — a failed
invoke reads as opaque garbage), **StepFunctions idempotency token** (duplicate executions on retry),
**gRPC** deadline/cancellation propagation + streaming client + rich errors, **tumbling windows**
(Kinesis/DynamoDB).

---

## Headline findings by severity

### Critical
| Transport | Finding |
|---|---|
| Self-host HTTP | Header **and query** *values* are lowercased → auth tokens / cookies / traceparent corrupted; case-only query-key collision throws |
| Azure Service Bus | `AutoComplete` **default** completes a non-throwing failure result → silent message loss out of the box |
| Azure Service Bus | **No session support** — a session-enabled queue/subscription cannot be consumed at all |

### High (correctness / data-loss / hard-fail)
| Transport | Finding |
|---|---|
| AWS SQS | WholeBatch default deletes non-throwing failures; unrouted/`null`-result messages acked instead of dead-lettered |
| AWS SQS | FIFO ordering broken on consume (concurrent fan-out + per-message settlement) |
| AWS SNS | Publisher drops the `topic` routing key → Benzene→Benzene routes to null topic |
| AWS Lambda (Kinesis/DynamoDB) | Swallowed exception → silent data loss if `ReportBatchItemFailures` isn't set on the trigger (Benzene neither sets nor checks it) |
| AWS Lambda (Kafka) | Concurrent fan-out breaks per-partition ordering; no partial-batch response despite AWS supporting it |
| AWS API Gateway | HTTP API v2 payload unsupported (hard fail); binary request/response bodies corrupted/impossible |
| AWS Lambda client | `InvokeResponse.FunctionError` never inspected → a failed function reads as opaque error |
| Azure Event Hub | Producer sets no partition key → advertised per-partition ordering unreachable end-to-end |
| Azure Service Bus | No explicit dead-letter → poison messages abandon-loop until max-delivery-count |
| Azure Queue Storage | Egress plain-text vs Benzene's own Base64 Functions trigger → decode failure |
| Kafka | Producer sets no message key (no ordering/affinity); no dead-letter/retry topic; at-most-once default |
| gRPC | Client discards caller deadline + cancellation token (no propagation downstream) |
| HTTP client | No cancellation propagation; error-body always deserialized as `TResponse` → throws, masking status |
| Self-host HTTP | Binary bodies corrupted; request fully buffered with no size limit |
| RabbitMQ | Publish is transient by default (no persistence knob) → loss on broker restart |
| Azure Functions | Trigger dispatch keyed only on CLR type → two functions of the same trigger kind collide (second is dead) |

(Medium/Low findings — batch APIs, TTL/scheduling, tumbling windows, header multiplicity, rich gRPC
errors, streaming clients, doc nits — are itemized per transport in files `01`–`12`.)

---

## Suggested priority order
1. **Fix the silent-data-loss defaults & internal-consistency bugs first** (Critical/High, cheap-ish,
   high blast radius): Service Bus AutoComplete default; SQS WholeBatch/null-result settlement;
   self-host + Azure-AspNet value-lowercasing; SNS routing-key; Queue Storage encoding. These are bugs,
   not features — they make correct-looking code lose data or fail to interoperate.
2. **Unify the settlement contract** (theme 1) across all transports so "return a failure result" is
   safe and consistent, with one opt-in name.
3. **Give producers the ordering/partition key** (theme 2): Event Hub partition key, Kafka message key,
   Service Bus SessionId, SQS attribute-cap guard.
4. **Close the hard-fail platform gaps**: API Gateway v2 + binary; Lambda Kafka ordering + partial-batch;
   Lambda `FunctionError`; gRPC/HTTP-client cancellation; the Lambda-stream `ReportBatchItemFailures`
   enforcement.
5. **Feature breadth** (batch APIs, FIFO/sessions, dead-letter verbs, RabbitMQ confirms/persistence,
   tumbling windows, streaming gRPC client) as roadmap items — several are already honestly documented
   as known gaps.
