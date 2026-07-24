# Message Results

Every [message handler](message-handlers.md) returns its outcome wrapped in an `IBenzeneResult<T>` (or
`IBenzeneResult` for handlers with no payload) instead of throwing for expected failure cases. The
result carries a status, a success flag, the payload (on success), and error messages (on failure).
Build one with the static `BenzeneResult` factory (`Benzene.Results`) — you should not need to
implement `IBenzeneResult<T>` yourself.

## `IBenzeneResult` / `IBenzeneResult<T>`

Defined in `Benzene.Abstractions.Results`:

```csharp
public interface IBenzeneResult
{
    string Status { get; }
    bool IsSuccessful { get; }
    object PayloadAsObject { get; }
    string[] Errors { get; }
}

public interface IBenzeneResult<T> : IBenzeneResult
{
    T Payload { get; }
}
```

`Status` is a plain string (see [`BenzeneResultStatus`](#benzeneresultstatus) below) — not a .NET
`enum` — which is what lets transport-specific status mappers (HTTP status codes, SQS
acknowledgement, ...) key off it without a hard dependency on `Benzene.Results` itself.

## `BenzeneResult` factory

Static factory methods on `Benzene.Results.BenzeneResult`, each with a generic `<T>` overload (for
handlers with a payload) and a non-generic overload (for `IMessageHandler<TRequest>`/`Void`
payloads):

```csharp
BenzeneResult.Ok(new OrderDto());          // BenzeneResult.Ok<T>()  also available (default payload)
BenzeneResult.Created(new OrderDto());     // BenzeneResult.Created<T>()
BenzeneResult.Accepted(new OrderDto());    // BenzeneResult.Accepted<T>() / BenzeneResult.Accepted()
BenzeneResult.Updated(new OrderDto());     // BenzeneResult.Updated<T>()
BenzeneResult.Deleted(new OrderDto());     // BenzeneResult.Deleted<T>()
BenzeneResult.Ignored<OrderDto>();         // BenzeneResult.Ignored()

BenzeneResult.NotFound<OrderDto>("Order 123 not found");
BenzeneResult.BadRequest<OrderDto>("Invalid request");
BenzeneResult.ValidationError<OrderDto>("Name is required");
BenzeneResult.Forbidden<OrderDto>();
BenzeneResult.Unauthorized<OrderDto>();
BenzeneResult.Conflict<OrderDto>();
BenzeneResult.ServiceUnavailable<OrderDto>();
BenzeneResult.NotImplemented<OrderDto>();
BenzeneResult.UnexpectedError<OrderDto>("Something went wrong");
```

All the error-style factories (`NotFound`, `BadRequest`, `ValidationError`, `Forbidden`,
`Unauthorized`, `Conflict`, `ServiceUnavailable`, `NotImplemented`, `UnexpectedError`) accept
`params string[] errors` and produce `IsSuccessful == false`. There's also a lower-level escape
hatch, `BenzeneResult.Set(status, ...)`, for a custom status string that isn't one of the built-ins
— used internally (e.g. `MessageRouter<TContext>` sets `validation-error`/`not-found` results this way
when a topic is missing or unmatched).

### `BenzeneResultExtensions`

`Benzene.Results.BenzeneResultExtensions` adds `Is*()` checks (`IsOk`, `IsCreated`, `IsNotFound`,
`IsValidationError`, etc.) mirroring every status, plus `.As<TOutput>(...)` helpers for remapping a
result's payload type while preserving its status/success/errors — handy when adapting one
handler's result to another shape. It also has `HttpStatusCode.Convert()` / `Convert<T>()`
extensions that go the other direction: turning a raw `HttpStatusCode` (e.g. from an outbound HTTP
call inside a handler) into an `IBenzeneResult`/`IBenzeneResult<T>`.

## `BenzeneResultStatus`

A static class of string constants (`Benzene.Results`), **not** a .NET `enum`:

```csharp
public static class BenzeneResultStatus
{
    public const string Accepted = "accepted";
    public const string Ok = "ok";
    public const string Created = "created";
    public const string Updated = "updated";
    public const string Deleted = "deleted";
    public const string Ignored = "ignored";
    public const string NotFound = "not-found";
    public const string BadRequest = "bad-request";
    public const string ValidationError = "validation-error";
    public const string ServiceUnavailable = "service-unavailable";
    public const string NotImplemented = "not-implemented";
    public const string UnexpectedError = "unexpected-error";
    public const string Conflict = "conflict";
    public const string Forbidden = "forbidden";
    public const string Unauthorized = "unauthorized";
}
```

## Transport mapping

### HTTP

`Benzene.Http`'s `DefaultHttpStatusCodeMapper` (`IHttpStatusCodeMapper`) maps every
`BenzeneResultStatus` value onto an HTTP status code; unrecognized/null statuses default to `500`:

| Status | HTTP code |
|---|---|
| `ok`, `ignored` | 200 |
| `created` | 201 |
| `accepted` | 202 |
| `updated`, `deleted` | 204 |
| `bad-request` | 400 |
| `unauthorized` | 401 |
| `forbidden` | 403 |
| `not-found` | 404 |
| `conflict` | 409 |
| `validation-error` | 422 |
| `unexpected-error` (or anything unmapped) | 500 |
| `not-implemented` | 501 |
| `service-unavailable` | 503 |

`HttpStatusCodeResponseHandler<TContext>` applies this mapping to the HTTP response via
`IBenzeneResponseAdapter<TContext>`. On success, `SerializerResponseRenderer<TContext>` (see
[Message Handlers](message-handlers.md#response-handling)) serializes `Payload`; on failure, it
serializes an `ErrorPayload` (`{ Status, Detail }`, where `Detail` is `Errors` joined with `", "`) —
so a `BenzeneResult.NotFound<OrderDto>("Order 123 not found")` becomes an HTTP `404` with a JSON
body describing the error, not the (empty) `OrderDto` payload.

### Async/event transports — settlement (ack/nack/checkpoint)

For queues, streams, and event triggers there is no synchronous HTTP status to return to a caller;
instead the result's `IsSuccessful` flag decides whether the message is **settled** (acked/completed/
checkpointed) or **redelivered** (nacked/abandoned/left for retry). Each transport's result-setter
records the outcome on the context's `IHasMessageResult.MessageResult`, and the transport's
application/worker reads that back to settle the message.

**As of the 1.0 settlement contract, every queue-shaped transport is safe by default**: a returned
failure result (`IsSuccessful == false`, e.g. `validation-error`/`not-found`/`service-unavailable`) —
**or** an unset/null result, e.g. an unroutable message no handler matched — is redelivered
(at-least-once), exactly like an unhandled exception, rather than being silently settled. The two
self-hosted **stream** workers (`Benzene.Kafka.Core`, `Benzene.Azure.EventHub`) are the deliberate
exception and default to at-most-once. The full per-transport table — the default on a returned
failure result and the exact opt-in/opt-out knob for each (`SqsOptions.BatchFailureMode`,
`SnsOptions.RaiseOnFailureStatus`, `ServiceBusOptions.AckMode`, `CommitOnlyOnSuccess`, …) — is the
single source of truth in the **[Capability Matrix](capability-matrix.md#retry-on-handler-failure-result--the-per-transport-breakdown)**.
Because a redelivered message re-runs the handler, **any handler on an at-least-once transport must be
idempotent** — see [Idempotency](cookbooks/idempotency.md).

Two representative examples:

- **AWS SQS** (`Benzene.Aws.Lambda.Sqs`) — batch-based: `SqsApplication` reports every record whose
  `MessageResult` is unsuccessful **or unset**, or that threw, back to Lambda as an
  `SQSBatchResponse.BatchItemFailure`, so SQS retries (or dead-letters, per your redrive policy) only
  those records — successfully-handled records in the same batch are not reprocessed. Configurable via
  `SqsOptions.BatchFailureMode` (default `PartialBatchFailure`; `FailWholeBatch` retries the whole
  batch on any failure) — see [Handling SQS Message Failures](cookbooks/handling-sqs-failures.md).
- **AWS SNS** (`Benzene.Aws.Lambda.Sns`) — one notification per invocation, no per-record ack API, so
  settlement rides on whether the invocation throws. `SnsMessageHandlerResultSetter` records the
  result; `SnsOptions.RaiseOnFailureStatus` (**default `true`**) escalates a non-exception failure
  result into a thrown `SnsMessageProcessingException` so SNS's subscription retry/redrive applies —
  the same at-least-once treatment a thrown exception already gets. Set `RaiseOnFailureStatus = false`
  for at-most-once (a failure result is accepted, no retry). `CatchExceptions` (default `false`)
  conversely controls whether a thrown exception is caught/logged instead of cascading — see
  [SNS Fan-Out Pattern](cookbooks/sns-fan-out.md#configuring-exception-and-retry-behavior-with-snsoptions).

## See also

- [Message Handlers](message-handlers.md) — how handlers produce `IBenzeneResult<T>` and how the
  router/response-handling pipeline consumes it.
- [Middleware](middleware.md) — the pipeline mechanism handlers run inside.
