# Benzene.Clients.Aws.Sns

## What this package does
Outbound SNS client for a Benzene app: publish messages to an SNS topic. Pins **only**
`AWSSDK.SimpleNotificationService`.

## Key types
- `SnsBenzeneMessageClient` — `IBenzeneMessageClient`; publishes to a topic ARN.
- `SnsClientMiddleware` / `SnsSendMessageContext` — terminal publish middleware and its context.
- `SnsContextConverter<T>` — `IBenzeneClientContext<T, Void>` → publish context.
- `OutboundSnsContextConverter` — the `Benzene.Clients.OutboundContext` counterpart, used by the
  `OutboundContext` overloads of `.UseSns(topicArn, …)` for `AddOutboundRouting(...).Route(topic, …)`.
- `Extensions` — `UseSnsClient`, `UseSns<T>` (the `IBenzeneClientContext<T,Void>` overloads) and
  `UseSns` (the `OutboundContext` overloads).

## Conventions
- `SnsContextConverter`/`OutboundSnsContextConverter` forward `IBenzeneClientRequest.Headers` onto
  SNS `MessageAttributes` (so correlation/trace decorators reach the wire) **and** set a `topic`
  message attribute — the same as SQS. The SNS *topic ARN* is the fan-out destination; the Benzene
  *topic* (which handler runs) is a separate routing key, and `Benzene.Aws.Lambda.Sns`'s
  `SnsMessageTopicGetter` reads it from this `topic` attribute. Omitting it (as this package used to)
  made a Benzene→Benzene SNS round-trip resolve to a null topic and fail to route. The attribute key
  is a configurable default (`topicAttributeKey` on the converters and `.UseSns(..., topicAttributeKey:)`),
  `SnsContextConverter<T>.DefaultTopicAttribute` = `"topic"` — keep it in sync with the consumer's key.
- Both outbound response mappers hardcode `IBenzeneResult<Void>` — SNS has only a publish
  acknowledgement, so a topic routed through SNS must be sent via `SendAsync<TRequest, Void>`; any
  other `TResponse` compiles but throws `Benzene.Clients.OutboundResponseTypeMismatchException` at
  runtime, naming the topic, the actual (`Void`) and requested response types (release plan Tier
  2.4 — this used to be a bare `InvalidCastException`; fixed in `DefaultBenzeneMessageSender`).
- **`SnsHealthCheck`** — verifies topic reachability with a read-only `GetTopicAttributes` call (the
  SNS analogue of `SqsHealthCheck`, but non-side-effecting: it does not publish). `Type => "Sns"`,
  dependency `("Topic", topicArn)`. Register via `AddSnsHealthCheck(topicArn)`; the consumer registers
  `IAmazonSimpleNotificationService` in DI (Benzene does not).

## Dependencies
`AWSSDK.SimpleNotificationService`; Benzene `Clients`, `Core.Middleware`, `Results`, `HealthChecks.Core`.
