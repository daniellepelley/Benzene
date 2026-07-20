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
  SNS `MessageAttributes` (so correlation/trace decorators reach the wire) but — unlike SQS — do
  **not** set a `topic` attribute: SNS routing is the topic ARN itself, so they forward headers only.
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
