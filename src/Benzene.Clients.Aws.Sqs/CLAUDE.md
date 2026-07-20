# Benzene.Clients.Aws.Sqs

## What this package does
Outbound SQS client for a Benzene app: send messages to an SQS queue (to reach a Benzene SQS
consumer, or any SQS target), plus an SQS health check. Pins **only** `AWSSDK.SQS`.

## Key types
- `SqsBenzeneMessageClient` — `IBenzeneMessageClient`; sends to a queue URL.
- `SqsClientMiddleware` / `SqsSendMessageContext` — terminal send middleware and its context.
- `SqsContextConverter<T>` — `IBenzeneClientContext<T, Void>` → send context.
- `OutboundSqsContextConverter` — the `Benzene.Clients.OutboundContext` counterpart, used by the
  `OutboundContext` overloads of `.UseSqs(queueUrl, …)` for `AddOutboundRouting(...).Route(topic, …)`.
- `SqsHealthCheck` — pings the queue; reports `HealthCheckDependency` (`Kind = "Queue"`, `Name` = URL).
- `LocalSqsClientFactory` (in `LocalAwsLambdaClientFactory.cs` — historically misnamed file) —
  builds an `IAmazonSQS` from a local AWS profile for dev/test.
- `Extensions` — `UseSqsClient`, `UseSqs<T>`/`UseSqs` (both the `IBenzeneClientContext<T,Void>` and
  `OutboundContext` overloads), `AddSqsMessageClient`, and **`AddSqsHealthCheck`**.

## Conventions
- `SqsContextConverter`/`OutboundSqsContextConverter` forward `IBenzeneClientRequest.Headers` onto
  real SQS `MessageAttributes` so header decorators (correlation ID, W3C trace context) reach the
  wire, **and** set a `topic` message attribute (the SQS consumer routes on it). The topic attribute
  key is a configurable default, not hard-coded (`topicAttributeKey` on the converters,
  `.UseSqs(queueUrl, topicAttributeKey: "x")`, `AddSqsMessageClient(..., topicAttributeKey)`, and
  `AddSqsHealthCheck(queueUrl, topicAttributeKey)`) — keep it in sync with the consumer's key.
- **Empty attribute values are skipped.** SQS rejects a message attribute whose value is empty, so
  both converters omit the `topic` attribute when `Topic` is null/empty and skip any header whose
  value is empty. LocalStack tolerates empty values (so the live LocalStack tests passed either way),
  but real SQS does not — this mirrors the SNS converters' guard. Covered by `SqsContextConverterTest`.
- Both outbound response mappers hardcode `IBenzeneResult<Void>` — SQS has only a send
  acknowledgement, so a topic routed through SQS must be sent via `SendAsync<TRequest, Void>`; any
  other `TResponse` compiles but throws `Benzene.Clients.OutboundResponseTypeMismatchException` at
  runtime, naming the topic, the actual (`Void`) and requested response types (release plan Tier
  2.4 — this used to be a bare `InvalidCastException`; fixed in `DefaultBenzeneMessageSender`).

## Dependencies
`AWSSDK.SQS`; Benzene `Clients`, `Core.Middleware`, `HealthChecks.Core`, `Results`.
