## AWS SNS

Reviewed `src/Benzene.Aws.Lambda.Sns` (ingress) and `src/Benzene.Clients.Aws.Sns` (publisher) against the SNS Developer Guide, the Lambda SNS event-source docs, filter-policy docs, and FIFO-topic docs.

---

**[DIVERGENCE] Benzene SNS publisher drops the routing key — a Benzene→Benzene SNS round-trip routes to a null topic** (Severity: High)
- **Benzene today:** `SnsContextConverter<T>.CreateRequestAsync` and `OutboundSnsContextConverter.CreateRequestAsync` forward `IBenzeneClientRequest.Headers` onto SNS `MessageAttributes` but **never write the topic** (`request.Topic`) anywhere (`src/Benzene.Clients.Aws.Sns/SnsContextConverter.cs:46-60`, `OutboundSnsContextConverter.cs:54-68`). Yet the ingress side routes on exactly that: `SnsMessageTopicGetter.GetTopic` reads the topic from the `"topic"` message attribute (`src/Benzene.Aws.Lambda.Sns/SnsMessageTopicGetter.cs:39-42`), returning a null-id topic when absent. The SQS sibling *does* set it — `SqsContextConverter.cs:67` writes `messageAttributes[_topicAttributeKey] = request.Topic`. SNS is the odd one out.
- **AWS intent:** SNS message attributes travel intact to a Lambda subscription inside the JSON envelope (`SnsRecord.Sns.MessageAttributes`), so an attribute-carried routing key is a valid, idiomatic pattern (it is the same mechanism filter policies use). The gap is not SNS's model; it is that the producer half omits the attribute the consumer half requires.
- **Impact:** Any message published by Benzene's own SNS client and consumed by Benzene's SNS Lambda handler arrives with a **null Benzene topic**, so message-handler routing fails to match — unless the caller manually stuffs a `topic` header. The two halves of the same library don't interoperate out of the box. The CLAUDE.md rationalizes this ("SNS routing is the topic ARN itself, so they forward headers only") but that conflates the SNS **topic ARN** (fan-out destination) with Benzene's **routing key** (which handler runs).
- **Recommendation:** Either set the topic attribute on publish for symmetry with SQS (add a `topicAttributeKey` parameter to both SNS converters mirroring `SqsContextConverter`), or, if the deliberate choice is that SNS routing is ARN-based, make the ingress default reflect that and stop documenting a `topic` attribute the library never emits. Today's split is an internal contract mismatch, not a documented design.

**[MISSING] No FIFO topic support (MessageGroupId / MessageDeduplicationId)** (Severity: Medium)
- **Benzene today:** Neither converter sets `PublishRequest.MessageGroupId` or `MessageDeduplicationId`, and there is no configuration surface for them.
- **AWS intent:** Publishing to a FIFO topic **requires** `MessageGroupId`, and requires `MessageDeduplicationId` unless content-based deduplication is enabled. A `PublishAsync` to a `.fifo` topic without a group id is rejected by SNS.
- **Impact:** Benzene's SNS publisher cannot target FIFO topics at all — a whole SNS delivery mode is unreachable. This also undercuts the idempotency guidance in the ingress CLAUDE.md.
- **Recommendation:** Expose optional `MessageGroupId`/`MessageDeduplicationId` on the publish path, and document the FIFO story. At minimum, note the limitation explicitly.

**[MISSING] No batch publish (PublishBatch)** (Severity: Low)
- **Benzene today:** `SnsClientMiddleware.HandleAsync` calls `_amazonSns.PublishAsync(context.Request)` one message at a time (`SnsClientMiddleware.cs:36`).
- **AWS intent:** SNS offers `PublishBatch` (up to 10 messages per request) to cut request count and cost.
- **Impact:** High-throughput publishers pay per-message API overhead. (SQS has the same gap, so this is consistent.)
- **Recommendation:** Optional batch-publish overload; low priority.

**[MISSING] Filter-policy interop is undermined and undocumented; all attributes sent as `String`** (Severity: Low)
- **Benzene today:** Headers are forwarded as message attributes with `DataType = "String"` for every value. No docs/story for subscription filter policies, and the natural routing attribute (`topic`) isn't emitted.
- **AWS intent:** Filter policies match on message attributes; numeric matching requires `DataType=Number`.
- **Impact:** Numeric filter policies won't match Benzene-published attributes (always stringified), and the most obvious filter key (topic) is absent.
- **Recommendation:** Document how filter policies interact with forwarded headers; consider honoring a Number data type where the value is numeric.

**[MISSING] No large-payload (SNS Extended Client / S3 offload) or multi-protocol message-structure story** (Severity: Low)
- **Benzene today:** The body is `_serializer.Serialize(request.Message)` placed directly in `PublishRequest.Message`; `MessageStructure` is never set.
- **AWS intent:** SNS caps a message at 256 KB; the Extended Client Library offloads larger payloads to S3, and `MessageStructure="json"` lets one publish carry different payloads per protocol.
- **Impact:** Payloads >256 KB fail the publish; multi-protocol fan-out with per-protocol bodies isn't expressible.
- **Recommendation:** Note the 256 KB ceiling; treat extended-client support as future work.

**[Observation — correct for SNS, not a defect] Per-record fan-out and retry-by-throw match the SNS→Lambda contract** (informational)
- SNS→Lambda delivers **one record per invocation** and always as a **JSON envelope** (raw message delivery is not supported for the Lambda protocol). Benzene reads the body verbatim from `SnsRecord.Sns.Message` and attributes from `SnsRecord.Sns.MessageAttributes` — both correct. Because there is exactly one record, escalating a failure to a thrown `SnsMessageProcessingException` cleanly hands the whole invocation back to SNS's subscription-level retry/redrive — the right lever for SNS.
- The `SnsOptions.MaxDegreeOfParallelism` / `BoundedFanOut` "batch fan-out" machinery is effectively inert for SNS (record list is always length 1). Harmless, but the docs slightly oversell a batch dimension SNS→Lambda doesn't have.
- The unsafe-by-default silent-drop of non-exception failure results is a genuine at-least-once gap, but it is **honestly and thoroughly documented** with an opt-in fix.

---

**Verdict:** Ingress is faithful to the SNS→Lambda contract, but the publisher has a real internal-consistency bug (drops the routing key that its own consumer needs), and SNS's FIFO/batch/filter-policy/large-payload capabilities have no Benzene story — the first is High and should be fixed or explicitly reconciled; the rest are documented-gap-worthy.
