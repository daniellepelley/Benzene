# Benzene.Aws.Lambda.EventBridge

## What this package does
Inbound Amazon EventBridge adapter: routes EventBridge events delivered to a Lambda target into
Benzene message handlers. The event's `detail-type` is the message topic — EventBridge's native
routing key, so no `topic` attribute needs bolting on the way SQS/SNS require — and `detail` is
the message body. See `docs/plans/eventbridge-plan.md` for the design decisions (E1–E7).

## Failure handling: a returned failure result is not retried by default (opt in via `EventBridgeOptions`)
By default, if your handler returns a non-exception failure result (e.g.
`BenzeneResult.ServiceUnavailable(...)`), the Lambda invocation still reports success, so EventBridge
considers the event delivered and never retries it — only an unhandled exception cascades to fail the
invocation and lets the rule target's Lambda destination/DLQ (`MaximumRetryAttempts`, `OnFailure`
destination) take over. To retry on failure *results* too, set
`EventBridgeOptions.RaiseOnFailureStatus = true` (via `UseEventBridge(action, configure)`), which
escalates a failure result into a thrown `EventBridgeMessageProcessingException` so the target's
retry applies the same way it would for an exception — mirroring `Benzene.Aws.Lambda.Sns`.
`EventBridgeOptions.CatchExceptions` (default `false`) conversely swallows/logs handler exceptions
instead of cascading them. Both default off (purely additive). If you enable `RaiseOnFailureStatus`,
the handler must be idempotent.

## Key types/interfaces
- `EventBridgeEvent` — Benzene's own model of the EventBridge envelope (`detail-type`, `source`,
  `id`, `account`, `region`, `time`, `resources`, raw-`JsonElement` `detail`). Deliberately not a
  dependency on `Amazon.Lambda.CloudWatchEvents`: the envelope is stable, and this keeps the
  package dependency-free beyond `Benzene.Aws.Lambda.Core`.
- `EventBridgeLambdaHandler : AwsLambdaMiddlewareRouter<EventBridgeEvent>` — claims payloads with
  both `detail-type` and `source` present; everything else falls through to the next event source
  adapter. Fire-and-forget (EventBridge targets are invoked asynchronously; no response written).
- `EventBridgeApplication : MiddlewareApplication<EventBridgeEvent, EventBridgeContext>` —
  single-context: EventBridge delivers ONE event per invocation (no `Records` batch), so this is
  one pipeline invocation + one DI scope per event, not a `MiddlewareMultiApplication` fan-out.
- `EventBridgeContext : IHasMessageResult` — carries the event and the handler result.
- Getters: topic = `detail-type`; body = `detail` raw JSON; headers = `eventbridge-`-prefixed
  envelope metadata plus Benzene wire headers lifted from the reserved `_benzeneHeaders` object
  inside `detail` (EventBridge has no native per-message attributes — the outbound client in
  `Benzene.Clients.Aws/EventBridge/` embeds them there).
- `UseEventBridge(action)` / `AddEventBridge()` / `EventBridgeRegistrations` — standard adapter
  wiring, mirrors `Benzene.Aws.Lambda.Sns`.

## When to use this package
- Handling EventBridge bus events (domain events, scheduled rules, AWS service events) with
  `[Message("<detail-type>")]` handlers, alongside the other `UseXxx` event sources in one Lambda.

## Dependencies on other Benzene packages
- **Benzene.Aws.Lambda.Core** — `AwsLambdaMiddlewareRouter`, `AwsEventStreamContext`

## Important conventions
- Topic matching is against `detail-type` verbatim; `source` is metadata
  (`eventbridge-source` header), not part of the topic.
- Embedded `_benzeneHeaders` win over prefixed envelope keys on collision; string values only.
- Transport name: `"eventbridge"`.
- The `aws_cloudwatch_event_rule`/target/permission Terraform for a consuming Lambda can be
  generated from its `[Message]` topics — see `Benzene.CodeGen.Terraform`
  (`TerraformEventBridgeRuleBuilder`).

## Tests
- `test/Benzene.Core.Test/Aws/EventBridge/EventBridgeGettersTest.cs` — topic/body/headers getters,
  including edge cases: undefined `detail` (`Body` returns null), a non-object `_benzeneHeaders`
  value (ignored), and a non-string value inside `_benzeneHeaders` (skipped, only string-valued
  entries are lifted).
- `test/Benzene.Core.Test/Aws/EventBridge/EventBridgeLambdaHandlerTest.cs` — `CanHandle` routing:
  both `detail-type`/`source` present (handled), neither present (falls through), and the two
  partial-match branches (`detail-type` without `source`, and vice versa — both also fall through).
- `test/Benzene.Core.Test/Aws/EventBridge/EventBridgeMessagePipelineTest.cs` — full pipeline
  round-trip (`EventBridgeApplication` + `AddEventBridge()`), including an unknown `detail-type`
  producing a not-found result.
