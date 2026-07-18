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
  other `TResponse` compiles but throws `InvalidCastException` at runtime (release plan Tier 2.4).
- **No health check** — SNS has no lightweight liveness probe analogous to SQS `GetQueueAttributes`.

## Dependencies
`AWSSDK.SimpleNotificationService`; Benzene `Clients`, `Core.Middleware`, `Results`.
