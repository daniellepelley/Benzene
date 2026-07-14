# Message Results

Every [message handler](message-handlers) returns its outcome wrapped in an `IBenzeneResult<T>` (or
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
— used internally (e.g. `MessageRouter<TContext>` sets `ValidationError`/`NotFound` results this way
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
    public const string Accepted = "Accepted";
    public const string Ok = "Ok";
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Deleted = "Deleted";
    public const string Ignored = "Ignored";
    public const string NotFound = "NotFound";
    public const string BadRequest = "BadRequest";
    public const string ValidationError = "ValidationError";
    public const string ServiceUnavailable = "ServiceUnavailable";
    public const string NotImplemented = "NotImplemented";
    public const string UnexpectedError = "UnexpectedError";
    public const string Conflict = "Conflict";
    public const string Forbidden = "Forbidden";
    public const string Unauthorized = "Unauthorized";
}
```

## Transport mapping

### HTTP

`Benzene.Http`'s `DefaultHttpStatusCodeMapper` (`IHttpStatusCodeMapper`) maps every
`BenzeneResultStatus` value onto an HTTP status code; unrecognized/null statuses default to `500`:

| Status | HTTP code |
|---|---|
| `Ok`, `Ignored` | 200 |
| `Created` | 201 |
| `Accepted` | 202 |
| `Updated`, `Deleted` | 204 |
| `BadRequest` | 400 |
| `Unauthorized` | 401 |
| `Forbidden` | 403 |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `ValidationError` | 422 |
| `UnexpectedError` (or anything unmapped) | 500 |
| `NotImplemented` | 501 |
| `ServiceUnavailable` | 503 |

`HttpStatusCodeResponseHandler<TContext>` applies this mapping to the HTTP response via
`IBenzeneResponseAdapter<TContext>`. On success, `SerializerResponseRenderer<TContext>` (see
[Message Handlers](message-handlers#response-handling)) serializes `Payload`; on failure, it
serializes an `ErrorPayload` (`{ Status, Detail }`, where `Detail` is `Errors` joined with `", "`) —
so a `BenzeneResult.NotFound<OrderDto>("Order 123 not found")` becomes an HTTP `404` with a JSON
body describing the error, not the (empty) `OrderDto` payload.

### AWS SQS

SQS Lambda processing is batch-based, so instead of a single status code, each record's
`IsSuccessful` flag decides whether that individual message is retried. `SqsMessageMessageHandlerResultSetter`
copies `IBenzeneResult.IsSuccessful` onto the per-record `SqsMessageContext.IsSuccessful`; after the
whole batch is processed, `SqsApplication` reports every record where `IsSuccessful == false` (or
where an unhandled exception occurred) back to Lambda as an `SQSBatchResponse.BatchItemFailure`,
which tells SQS to retry (or dead-letter, per your queue's redrive policy) only those records —
successfully-handled records in the same batch are not reprocessed. This means any non-`Ok`/`Created`/
etc. result (anything with `IsSuccessful == false`, e.g. `ValidationError`, `NotFound`,
`ServiceUnavailable`) causes that message to be retried by SQS, exactly like an unhandled exception
would.

### AWS SNS

SNS delivers one notification per Lambda invocation rather than a batch, so there's no per-record
acknowledgement API to report back to — `SnsMessageMessageHandlerResultSetter` records the result
onto the context's `MessageResult` for logging/diagnostics purposes, but retry behavior for SNS is
governed by whether the Lambda invocation itself throws, not by the `IBenzeneResult.Status`.

## See also

- [Message Handlers](message-handlers) — how handlers produce `IBenzeneResult<T>` and how the
  router/response-handling pipeline consumes it.
- [Middleware](middleware) — the pipeline mechanism handlers run inside.
