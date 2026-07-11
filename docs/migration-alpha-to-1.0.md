# Migration Guide: Alpha → 1.0

Benzene's alpha releases (`0.x.x-alpha`) did not follow strict semver, and several
renames happened without a corresponding migration note. This guide collects the
API changes you're most likely to hit upgrading an alpha-era service to 1.0.

If you find something not covered here, please open an issue — this list was
compiled from what surfaced while auditing docs and examples for 1.0, not from a
complete diff of every alpha release.

## Naming changes

| Alpha | 1.0 |
|---|---|
| `UseDirectMessage(...)` | `UseBenzeneMessage(...)` |
| `DirectMessageRequest` / `DirectMessageResponse` | `BenzeneMessageRequest` / `BenzeneMessageResponse` |
| `DirectMessageContext` | `BenzeneMessageContext` |
| `UseProcessDirectMessageResponse()` | Removed — response mapping now happens automatically inside `UseBenzeneMessage(...)` |
| `UseMessageRouter(...)` | `UseMessageHandlers(...)` |
| `IServiceResult<T>` / `HandlerResult` | `IBenzeneResult<T>` / `BenzeneResult` (static factory: `BenzeneResult.Ok(...)`, `.NotFound<T>()`, etc.) |
| `UseElementsLogContext()` | `UseLogResult(x => x.WithCorrelationId())` or `UseLogContext(...)` |
| `TestAwsLambdaStartUp<TStartUp>` | `AwsLambdaBenzeneTestStartUp<TStartUp>` |
| `testHost.SendEventAsync<T>(...)` | `testHost.SendBenzeneMessageAsync(...)` (built via `MessageBuilder.Create(topic, message)`) |
| `Benzene.Core.MiddlewareBuilder` (namespace) | `Benzene.Core.Middleware` |
| `AwsEventStreamPipelineBuilder` | `MiddlewarePipelineBuilder<AwsEventStreamContext>` |

## Message results

The old `IMessageResult` carried `Topic`, `Status`, `Errors`, `Payload`, and
`MessageHandlerDefinition` directly. It's been replaced by `IMessageHandlerResult`
(and `IMessageHandlerResult<TResponse>`), which wraps an `IBenzeneResult` instead
of exposing those fields directly:

```csharp
// Alpha
public string Map(TContext context, ISerializer serializer)
{
    var messageResult = context.MessageResult;
    return messageResult.IsSuccessful
        ? serializer.Serialize(messageResult.Payload)
        : serializer.Serialize(new ErrorPayload(messageResult.Status, messageResult.Errors));
}

// 1.0
public string Map(TContext context, IMessageHandlerResult messageHandlerResult, ISerializer serializer)
{
    return messageHandlerResult.BenzeneResult.IsSuccessful
        ? serializer.Serialize(messageHandlerResult.MessageHandlerDefinition.ResponseType, messageHandlerResult.BenzeneResult.PayloadAsObject)
        : serializer.Serialize(new ErrorPayload(messageHandlerResult.BenzeneResult.Status, messageHandlerResult.BenzeneResult.Errors));
}
```

If you had a custom `IResponsePayloadMapper<TContext>`, see
`Benzene.Core.MessageHandlers.Response.DefaultResponsePayloadMapper` for the
current reference implementation.

## Bug fixes that change behavior

Two bugs fixed during 1.0 prep change runtime behavior if you were relying on
the old (incorrect) behavior:

- **`TryAddSingleton(Type)`** previously called `AddScoped` internally, so
  "singleton" registrations were silently scoped instead. It now genuinely
  registers a singleton. If something in your app depended on getting a new
  instance per scope from a `TryAddSingleton` call, it will now get one shared
  instance.
- **`Extensions.Split()`** passed the wrong variable to the branch pipeline
  builder. Split pipelines now branch correctly; if your split branch appeared
  to silently no-op before, it should now actually execute.

## Removed: `AddScoped<T>(T instance)` extension

`BenzeneServiceContainerExtensions.AddScoped<T>(T instance)` — the overload
with "don't register if already registered" semantics — has been removed. It
was unreachable dead code: `IBenzeneServiceContainer` declares its own
`AddScoped<T>(T instance)` member with the identical signature, so normal call
syntax always resolved to that (unconditional) method instead. If you want
Try-semantics for an existing instance, check `IsTypeRegistered<T>()` yourself
before calling `AddScoped`.

## Target framework

Benzene 1.0 targets **.NET 10**. See [VERSIONING.md](../VERSIONING.md) for the
ongoing target framework support policy.
