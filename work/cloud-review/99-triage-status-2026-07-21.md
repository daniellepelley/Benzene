# Cloud-review triage & action status — 2026-07-21

This file records the outcome of working through the cloud/transport review (`00-summary.md` and the
per-transport files `01`–`12`) against the **current** source. Many findings had already been closed
by intervening bug-fixing rounds; each was re-verified against the code before deciding what, if
anything, remained to do.

Legend: **FIXED** = already resolved in current source · **ACTIONED** = fixed in this pass ·
**BY DESIGN** = current behaviour is deliberate and tested · **FLAG** = real, but a
feature/design/API change that needs a decision before implementing.

---

## Actioned in this pass (safe, self-contained correctness fixes, each with a test)

| # | Transport | Finding | Commit |
|---|---|---|---|
| 1 | AWS API Gateway | Response header dictionaries were case-sensitive while the framework writes `content-type` lowercase, so a handler setting `Content-Type` produced a duplicate header. Both v1 and v2 now build the dictionary `OrdinalIgnoreCase`. | `Make API Gateway response headers case-insensitive` |
| 2 | AWS SQS (consumer) | `DeleteMessageBatchAsync`'s response was discarded; a partial delete failure (entries in `Failed`) silently caused redelivery. Now logged with the undeleted message ids. | `Log SQS messages left undeleted by a partial batch-delete failure` |
| 3 | AWS SQS (consumer) | `SqsConsumerApplication`'s XML docs still described `WholeBatch` as the default `AckMode` after the default flipped to `PerMessage`. Corrected. | `Correct SqsConsumerApplication docs…` |

> Note: two other items I triaged as open — the AWS Lambda client `typeof(TResponse).Name == "Void"`
> fire-and-forget bug, and the `SqsConsumer` (worker) `WholeBatch`-default doc drift — turned out to
> have been fixed **independently on `main`** (commit `67a2631`, "Gate outbound-routing scan…") while
> this pass was in progress, with the same `typeof(TResponse) == typeof(...Void)` fix and an
> equivalent test. No duplicate change was pushed for those; the residual `SqsConsumerApplication`
> doc drift that commit didn't touch is item 3 above.

---

## Verified already FIXED by earlier bug-fixing rounds (no action needed)

- **Self-host HTTP header/query *value* lowercasing (Critical)** — `InternalExtensions.ToDictionary`
  now lower-cases the *key* only and preserves values verbatim (RFC 9110), with the case-only
  query-key collision handled via the indexer.
- **SNS publisher drops the `topic` routing key (High, internal-consistency)** —
  `SnsContextConverter` writes the routing topic as a message attribute and even guards the SNS
  10-attribute cap.
- **AWS SQS WholeBatch default deletes non-throwing failures (High)** — default `AckMode` is now
  `PerMessage`; unrouted/`null`-result messages are reported failed and left on the queue.
- **API Gateway HTTP API v2 + binary bodies (High)** — v2 payload format is supported
  (`UseApiGatewayV2`), and binary request/response bodies work (`IsBase64Encoded`, byte getters).
- **AWS Lambda client `FunctionError` never inspected (High)** — `AwsLambdaClient` throws
  `AwsLambdaFunctionErrorException` when `FunctionError` is set rather than mis-deserializing.
- **Kafka re-produce path dropped headers (High, trace loss)** — `KafkaMessageContextConverter`
  forwards headers onto `Message.Headers` (UTF-8), matching the client converter.
- **Self-host HTTP binary bodies / unbounded buffering (High)** — binary responses supported;
  `MaxRequestBodyBytes` closes the unbounded-buffering DoS.

---

## By design — deliberate and locked in by a test (not a bug)

- **Azure Kafka / Service Bus / Event Grid: `RaiseOnFailureStatus` escalation is swallowed when
  `CatchExceptions = true`.** The escalated `*MessageProcessingException` is deliberately caught by
  the `CatchExceptions` filter — `KafkaFailureHandlingTest.HandleAsync_RaiseOnFailureStatusAnd
  CatchExceptionsBothTrue_FailureResultIsEscalatedThenSwallowed` asserts exactly this ("escalated then
  swallowed"). `CatchExceptions` is the outermost, contain-everything decision. Left unchanged.

---

## FLAG — real findings that need a decision (feature / design / public-API changes)

These were **not** implemented unilaterally because each is more than a self-contained bug fix.

1. **Unify the settlement contract (theme 1).** SQS flipped to a safe `PerMessage` default; **Azure
   Service Bus still defaults to `AutoComplete`** (a non-throwing failure result is completed → silent
   loss out of the box). Flipping it to a safe default is a **breaking change that also requires the
   user to set `AutoCompleteMessages = false` on their `[ServiceBusTrigger]`** (Benzene can't set that
   for them), which is why it's currently opt-in (`AckMode.Explicit`/`RaiseOnFailureStatus`). Decision
   needed: keep opt-in, or flip the default and document the trigger-attribute requirement. Event Grid
   and Queue Storage triggers have *no* escalation opt-in at all — same decision applies.
2. **Producer ordering/partition keys (theme 2).** Event Hub producer sets no partition key; Kafka
   **re-produce path** (`KafkaMessageContextConverter`) sets no `Message.Key` (the *client* path
   already supports `keyHeader`); Service Bus sender sets no `SessionId`. Each needs a new public
   option/parameter + wiring — API design decision.
3. **AWS Lambda stream triggers (Kinesis/DynamoDB) `ReportBatchItemFailures`.** Benzene neither sets
   nor checks it, so a swallowed handler failure is silent data loss unless the trigger is configured
   for partial-batch responses. Enforcement is a behavioural/feature change.
4. **SQS FIFO consumption ordering.** Concurrent fan-out + per-message settlement breaks FIFO order;
   supporting FIFO consume is a feature.
5. **EventBridge non-object `Detail`.** Headers are only embedded when the payload serializes to a JSON
   object; a top-level array/scalar silently drops headers. Carrying them would need an envelope
   wrapper understood by the inbound binding — a coordinated wire-format change.
6. **Azure Queue Storage 64 KB size guard.** A pre-flight size check would be nice for DX, but the real
   limit is encoding-dependent (Base64 vs None) and this layer takes a caller-built `QueueClient`, so a
   guard here risks false positives. Needs a design decision on where/how to enforce.
7. **Other roadmap breadth** (already honestly documented as gaps): batch producer APIs
   (`PublishBatch`/`SendMessageBatch`/`PutEvents`/`EventDataBatch`), FIFO/dedup, Service Bus sessions
   & explicit dead-letter verbs, RabbitMQ publisher confirms/persistence, gRPC deadline/cancellation
   propagation + streaming client, HTTP-client cancellation, tumbling windows.

---

## Net

The consumers were already well-engineered and the earlier bug-fixing rounds had closed essentially
all of the Critical/High **code** findings. What remained were (a) a handful of small, safe
correctness/observability/doc fixes — done here — and (b) genuine feature/design/API decisions, which
are flagged above rather than actioned unilaterally.
