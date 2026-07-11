# Handler Result

Message handlers return their response wrapped in an `IBenzeneResult<T>` (or `IBenzeneResult` for
handlers with no payload). The result carries a status such as success, not found, or validation
error, along with the payload or error details. Build one using the static `BenzeneResult` factory.

---
### Ok

```csharp
BenzeneResult.Ok(new DemoResponse());
```

### Not Found

```csharp
BenzeneResult.NotFound<DemoResponse>();
```

### Other common results

```csharp
BenzeneResult.Created(new DemoResponse());
BenzeneResult.Updated(new DemoResponse());
BenzeneResult.Deleted<DemoResponse>();
BenzeneResult.Accepted();
BenzeneResult.ValidationError("Name is required");
BenzeneResult.BadRequest("Invalid request");
BenzeneResult.Forbidden();
BenzeneResult.Unauthorized();
BenzeneResult.Conflict();
BenzeneResult.ServiceUnavailable();
BenzeneResult.NotImplemented();
```
