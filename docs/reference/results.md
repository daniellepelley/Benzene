# Result & Status Reference

Every message handler returns its response wrapped in `IBenzeneResult<T>` (or `IBenzeneResult`
for handlers with no payload). The result carries a **status** — success, not found, validation
error, and so on — alongside the payload or error. Transports map that status onto their native
response (HTTP status codes, etc.), so your handler expresses intent once and it's translated
everywhere.

You build results with the static `BenzeneResult` factory (in `Benzene.Results`). For the
conceptual introduction, see [Message Results](../message-result).

```csharp
public Task<IBenzeneResult<OrderDto>> HandleAsync(GetOrderRequest request)
{
    var order = _repository.Find(request.Id);
    return order is null
        ? Task.FromResult(BenzeneResult.NotFound<OrderDto>())
        : Task.FromResult(BenzeneResult.Ok(order));
}
```

## Statuses

The status strings are defined as constants on `BenzeneResultStatus` (`Benzene.Results`). This
table lists every status, the factory method that produces it, whether it counts as success, and
the HTTP status code HTTP transports map it to (via `DefaultHttpStatusCodeMapper`).

### Success statuses

| Factory | Status | HTTP | Notes |
|---|---|---|---|
| `BenzeneResult.Ok(payload)` | `Ok` | `200` | Standard success with a payload. |
| `BenzeneResult.Created(payload)` | `Created` | `201` | Resource created. |
| `BenzeneResult.Accepted()` | `Accepted` | `202` | Acknowledged for async processing. The **default** result for fire-and-forget event handlers (those with no response). |
| `BenzeneResult.Updated(payload)` | `Updated` | `204` | Resource updated; no content returned. |
| `BenzeneResult.Deleted<T>()` | `Deleted` | `204` | Resource deleted; no content returned. |
| `BenzeneResult.Ignored<T>()` | `Ignored` | `200` | Message deliberately not acted upon but acknowledged as handled. |

### Failure statuses

| Factory | Status | HTTP | Notes |
|---|---|---|---|
| `BenzeneResult.BadRequest(message)` | `BadRequest` | `400` | Malformed or invalid request. |
| `BenzeneResult.Unauthorized()` | `Unauthorized` | `401` | Authentication required or failed. |
| `BenzeneResult.Forbidden()` | `Forbidden` | `403` | Authenticated but not permitted. |
| `BenzeneResult.NotFound<T>()` | `NotFound` | `404` | Resource does not exist. |
| `BenzeneResult.Conflict()` | `Conflict` | `409` | Conflicts with current state. |
| `BenzeneResult.ValidationError(message)` | `ValidationError` | `422` | Request failed validation. Returned automatically by [validation middleware](middleware#message-router-middleware). |
| `BenzeneResult.UnexpectedError(message)` | `UnexpectedError` | `500` | Unhandled/unexpected failure. |
| `BenzeneResult.NotImplemented()` | `NotImplemented` | `501` | Operation not implemented. |
| `BenzeneResult.ServiceUnavailable()` | `ServiceUnavailable` | `503` | A dependency is unavailable; safe to retry. |

> Any status not in the map — or a `null` status — falls back to **HTTP 500**.

## Payload vs. no-payload overloads

Each factory has two forms:

- `IBenzeneResult<T>` — carries a typed payload. On success you pass the value
  (`BenzeneResult.Ok(order)`); on failure you specify the type parameter
  (`BenzeneResult.NotFound<OrderDto>()`) since there's no payload.
- `IBenzeneResult` — no payload, for handlers declared as `IMessageHandler<TMessage>`.

```csharp
BenzeneResult.Ok(new OrderDto { /* … */ });   // IBenzeneResult<OrderDto>
BenzeneResult.NotFound<OrderDto>();            // IBenzeneResult<OrderDto>, no payload
BenzeneResult.Accepted();                      // IBenzeneResult, no payload
```

Failure factories that describe an error (`BadRequest`, `ValidationError`, `UnexpectedError`, …)
accept an optional message:

```csharp
BenzeneResult.ValidationError<OrderDto>("Name is required");
BenzeneResult.BadRequest("Invalid request");
```

## Helpers and lower-level building

| Member | Purpose |
|---|---|
| `BenzeneResult.Set(status, isSuccess)` / `Set<T>(...)` | Build a result with an explicit status string and success flag — the escape hatch for custom statuses. |
| `BenzeneResult.IsSuccess(result)` | True when the result's status is a success status. |
| `BenzeneResult.IsAccepted(result)` | True when the result is `Accepted`. |
| `*Internal` factories (`OkInternal`, `NotFoundInternal`, …) | Variants used for internal/inter-service results — e.g. results returned across a Benzene [message client](packages#outbound-messaging-clients) rather than mapped straight to an HTTP response. |

## Mapping in both directions

- **Outbound (handler → transport):** `DefaultHttpStatusCodeMapper` (`Benzene.Http`) converts a
  result status to an HTTP status code using the table above. Non-HTTP transports apply their
  own conventions.
- **Inbound (transport → result):** when a Benzene client calls another service over HTTP,
  `BenzeneResultHttpMapper` (`Benzene.Clients`) converts the received HTTP status code back into
  a `BenzeneResult` status, so calling code sees the same result model regardless of transport.

## See also

- [Message Results](../message-result) — the conceptual introduction.
- [Message Handlers](../message-handlers) — where results are returned.
- [Fluent Validation](../fluent-validation) / [Data Annotations](../data-annotations) — produce `ValidationError` results automatically.
