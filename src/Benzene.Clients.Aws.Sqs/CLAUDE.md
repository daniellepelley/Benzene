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
- `SqsHealthCheck` — verifies the queue; reports `HealthCheckDependency` (`Kind = "Queue"`, `Name` = URL).
  Default `HealthCheckMode.Reachability` is a **non-destructive** read-only `GetQueueAttributes` call
  (`Type = "Sqs"`); `HealthCheckMode.Active` sends a real `ping` message (`Type = "Sqs.Active"`,
  side-effecting — the consumer must recognise and drop it). See `HealthCheckMode` in
  `Benzene.HealthChecks.Core`. Failures are classified via `HealthCheckError.Classify` (§3.9, reversed):
  an authorization/permission failure (403) is a **persistent `Failed`**, surfacing as unhealthy even for
  the auto-wired dependency check (a deterministic misconfiguration that won't self-heal; opt out with
  `healthCheck: false` where the read permission is legitimately absent); the SDK `ErrorCode`/`StatusCode`
  are surfaced in `Data`, never the exception message.
- `SqsBatchMessageClient` — `IBenzeneBatchMessageClient` (from `Benzene.Clients`); sends a collection
  via `SendMessageBatch` (≤10/call). Reuses `SqsContextConverter<T>` per entry, chunks with
  `BatchSend.Chunk`, and maps `response.Failed` back to caller indices in a `BatchSendResult`. The
  entry `Id` carries the caller's zero-based request index. Covered by
  `test/Benzene.Core.Test/Clients/Aws/BatchMessageClientTest.cs`.
- `LocalSqsClientFactory` (in `LocalAwsLambdaClientFactory.cs` — historically misnamed file) —
  builds an `IAmazonSQS` from a local AWS profile for dev/test.
- `Extensions` — `UseSqsClient`, `UseSqs<T>`/`UseSqs` (both the `IBenzeneClientContext<T,Void>` and
  `OutboundContext` overloads), `AddSqsMessageClient`, and **`AddSqsHealthCheck`**.
  - **Auto-wired health check (Phase 1, default-on).** The two default (DI-handle) `UseSqs`/`UseSqs<T>`
    overloads take `bool healthCheck = true`: unless opted out, they auto-register a non-destructive
    `SqsHealthCheck` for the queue on the **dependency category** (`AddDependencyHealthCheck`, deduped by
    `"Sqs:{queueUrl}"`), reusing the `IAmazonSQS` from DI. It surfaces on the deep `healthcheck` layer
    only — **never** a liveness/readiness probe (a queue check is shared-fate; see
    `IDependencyHealthCheck`). The `action`-based overloads (where you hand-wire the client, possibly
    with a non-DI handle) do **not** auto-wire — add `AddSqsHealthCheck` yourself there.

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
- **10-attribute cap guard.** SQS caps a message at 10 message attributes (the routing topic
  attribute counts toward it). Both converters fail fast with a clear `InvalidOperationException`
  naming the count if more are set, instead of letting the SDK throw an opaque error the send path
  would swallow into a generic `ServiceUnavailable`. `SqsContextConverter<T>.GuardAttributeLimit`.
- Both outbound response mappers hardcode `IBenzeneResult<Void>` — SQS has only a send
  acknowledgement, so a topic routed through SQS must be sent via `SendAsync<TRequest, Void>`; any
  other `TResponse` compiles but throws `Benzene.Clients.OutboundResponseTypeMismatchException` at
  runtime, naming the topic, the actual (`Void`) and requested response types (release plan Tier
  2.4 — this used to be a bare `InvalidCastException`; fixed in `DefaultBenzeneMessageSender`).

## Dependencies
`AWSSDK.SQS`; Benzene `Clients`, `Core.Middleware`, `HealthChecks.Core`, `Results`.
