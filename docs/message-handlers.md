# Message Handlers

Message handlers are the components that receive and process a single message. There should be
exactly one message handler per topic your service handles. The topic (and the request/response
types) form the front-facing contract for the service — they're what's used to generate OpenAPI /
AsyncAPI documentation, client code, etc. (see [Spec](spec)).

Handlers support constructor dependency injection, so keep the handler itself thin and push
business logic into an injected service.

## `IMessageHandler<TRequest, TResponse>` / `IMessageHandler<TRequest>`

Defined in `Benzene.Abstractions.MessageHandlers`:

```csharp
public interface IMessageHandler<TRequest, TResponse>
    : IMessageHandlerBase<TRequest, TResponse>
{}

public interface IMessageHandlerBase<TRequest, TResponse>
{
    Task<IBenzeneResult<TResponse>> HandleAsync(TRequest request);
}

public interface IMessageHandler<TRequest>
{
    Task HandleAsync(TRequest request);
}
```

- Use `IMessageHandler<TRequest, TResponse>` for request/response handlers — `HandleAsync` returns
  the response wrapped in an `IBenzeneResult<TResponse>`.
- Use `IMessageHandler<TRequest>` for fire-and-forget handlers with no meaningful response.
  Internally, `MessageHandlerNoResultWrapper<TRequest, TResponse>` wraps it so it still fits the
  request/response shape; the wrapper always returns `BenzeneResult.Accepted<TResponse>()` once
  your handler's `HandleAsync` completes — this is why a no-response handler always reports back an
  "Accepted" result.

See [Message Results](message-result) for everything about `IBenzeneResult<T>` and the available
status factories — this page doesn't repeat that detail.

### Request / response example

```csharp
[HttpEndpoint("POST", "/orders")]
[Message("order:create")]
public class CreateOrderMessageHandler : IMessageHandler<CreateOrderMessage, OrderDto>
{
    private readonly IOrderService _orderService;

    public CreateOrderMessageHandler(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<IBenzeneResult<OrderDto>> HandleAsync(CreateOrderMessage request)
    {
        return await _orderService.SaveAsync(request);
    }
}
```

### Fire-and-forget (no response) example

```csharp
[Message("order:archive")]
public class ArchiveOrderMessageHandler : IMessageHandler<ArchiveOrderMessage>
{
    private readonly IOrderService _orderService;

    public ArchiveOrderMessageHandler(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task HandleAsync(ArchiveOrderMessage request)
    {
        await _orderService.ArchiveAsync(request);
    }
}
```

## `[Message("topic")]`

Defined in `Benzene.Core.MessageHandlers`:

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class MessageAttribute : Attribute
{
    public MessageAttribute(string topic, string version = "");

    public string Version { get; }
    public string Topic { get; }
}
```

Applied once per handler class. `Topic` is the routing key used to look up the handler at request
time (see [Handler discovery](#handler-discovery) below); `Version` is optional and lets multiple
versions of a handler coexist for the same topic — `IVersionSelector` picks which version answers a
given request.

## `[HttpEndpoint("METHOD", "/path")]`

Defined in `Benzene.Http`:

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class HttpEndpointAttribute : Attribute
{
    public HttpEndpointAttribute(string method, string url);

    public string Method { get; }
    public string Url { get; }
}
```

Maps an HTTP method + URL pattern (route parameters like `/orders/{id}` are supported) onto a
message handler, for any HTTP-shaped transport (ASP.NET Core, AWS API Gateway). Unlike `[Message]`,
it can be applied **multiple times** to the same handler class to expose it under more than one
route/method. It's discovered separately from `[Message]` by `IHttpEndpointFinder`
(`ReflectionHttpEndpointFinder`, `CacheHttpEndpointFinder`, `CompositeHttpEndpointFinder`, ... — all
in `Benzene.Http`), which is what HTTP transports use to route an inbound HTTP request to the right
topic before handing off to the same message-handler pipeline every other transport uses.

A handler commonly carries both attributes together, so the same handler answers both an HTTP
route and a direct topic dispatch (e.g. from a queue or another service):

```csharp
[HttpEndpoint("GET", "/orders/{id}")]
[Message("order:get")]
public class GetOrderMessageHandler : IMessageHandler<GetOrderMessage, OrderDto>
{
    // ...
}
```

## Handler discovery (`IMessageHandlersFinder`)

```csharp
public interface IMessageHandlersFinder : IMessageDefinitionFinder<IMessageHandlerDefinition>
{}
```

New handlers are found automatically — you don't register them individually. Discovery is layered:

- **`ReflectionMessageHandlersFinder`** — scans a set of types/assemblies for classes implementing
  `IMessageHandler<TRequest, TResponse>` or `IMessageHandler<TRequest>` that also carry a
  `[Message]` attribute, and builds an `IMessageHandlerDefinition` for each (topic, version, request
  type, response type, handler type). A type without `[Message]` is skipped (logged via
  `Debug.WriteLine`, not an error).
- **`CacheMessageHandlersFinder`** — wraps another finder and caches its results, since reflection
  scanning is done once at startup and re-used for every request afterwards.
- **`DependencyMessageHandlersFinder`** — discovers definitions already registered directly in DI
  (used when you register a handler by hand via `IMessageRouterBuilder.AddMessageHandler<...>`
  instead of relying on assembly scanning).
- **`CompositeMessageHandlersFinder`** — combines multiple finders into one, so reflection-based and
  DI-based discovery can coexist.

`IMessageHandlerDefinitionLookUp` (`MessageHandlerDefinitionLookUp`) is what actually answers "which
handler serves this topic at request time": it merges every registered finder's definitions, groups
by `(topic, version)`, and uses `IVersionSelector` to pick the best-matching version when more than
one exists for the same topic.

`UseMessageHandlers(...)` wires the assemblies/types you pass (or, if you pass none,
`AppDomain.CurrentDomain.GetAssemblies()`) into `AddMessageHandlers(...)`, which registers
`MessageHandlersList`, `DependencyMessageHandlersFinder`, and a `CompositeMessageHandlersFinder`
combining them (plus a `CacheMessageHandlersFinder` wrapping a `ReflectionMessageHandlersFinder`
when you pass explicit types/assemblies) — all as part of one call.

## `.UseMessageHandlers(...)`

Adds the routing middleware to a pipeline (`Benzene.Core.MessageHandlers.MiddlewarePipelineExtensions`):

```csharp
// Scan the current AppDomain's assemblies
app.UseMessageHandlers();

// Scan specific assemblies/types, optionally configuring handler-pipeline middleware
app.UseMessageHandlers(typeof(CreateOrderMessageHandler).Assembly);
app.UseMessageHandlers(router => router.UseFluentValidation());
app.UseMessageHandlers(typeof(CreateOrderMessageHandler).Assembly, router => router.UseFluentValidation());
```

Every overload ultimately registers a **`MessageRouter<TContext>`** as an `IMiddleware<TContext>` in
the pipeline. `MessageRouter<TContext>.HandleAsync`:

1. Extracts the topic via `IMessageGetter<TContext>`. If it's missing, sets a `ValidationError`
   result ("Topic is missing") and returns — no handler lookup happens.
2. Looks up the handler definition for that topic via `IMessageHandlerDefinitionLookUp`. If none is
   found, sets a `NotFound` result and returns.
3. Creates the handler instance via `IMessageHandlerFactory` (resolving it from DI, wrapping it per
   `IMessageHandlerWrapper`, and building its own per-handler middleware pipeline — see
   [Response handling](#response-handling) below).
4. Invokes the handler through an `IRequestMapperThunk<TContext>` (defers request-body mapping until
   the handler actually needs it) and sets the resulting `IMessageHandlerResult` on the context via
   `IMessageHandlerResultSetter<TContext>`.

The overload that accepts `Action<MessageRouterBuilder> router` lets you add middleware that runs
**per handler invocation**, wrapped around the actual call — this is how
[`.UseFluentValidation()`](fluent-validation) plugs in: it registers an `IHandlerMiddlewareBuilder`
that runs before `MessageHandlerMiddleware<TRequest, TResponse>` and short-circuits with a
validation-failure result if the request fails validation, without ever reaching your handler code.

## Request mapping (`IRequestMapper<TContext>`)

```csharp
public interface IRequestMapper<in TContext>
{
    TRequest? GetBody<TRequest>(TContext context) where TRequest : class;
}
```

`RequestMapper<TContext>` (the default implementation, registered via `AddContextItems()` as
`JsonDefaultMultiSerializerOptionsRequestMapper<TContext>`) resolves the request body two ways:

- If the context already implements `IRequestContext<TRequest>` (some contexts carry an
  already-deserialized/typed request), that's returned directly.
- Otherwise it reads the raw body string via `IMessageBodyGetter<TContext>` and deserializes it with
  `ISerializer` — falling back to `Activator.CreateInstance<TRequest>()` (an empty instance) if the
  body is empty, rather than passing `null` to your handler.

`MultiSerializerOptionsRequestMapper<TContext, TDefaultSerializer>` extends this to pick a different
`ISerializer` per request based on `ISerializerOption<TContext>` (e.g. content negotiation between
JSON and [XML](common-middleware) bodies) instead of always using the one default serializer.
`EnrichingRequestMapper<TContext>` layers on `IRequestEnricher<TContext>` to merge extra
context-derived fields into the deserialized request object.

## Response handling

Once your handler returns an `IBenzeneResult<TResponse>`, a chain of `IResponseHandler<TContext>`s
(registered via `AddContextItems()`/`AddBenzeneMessage()`) turns it into whatever the transport
needs:

- **`ResponseBodyHandler<TContext>`** — serializes the payload (success) or an `ErrorPayload`
  (failure) into the response body string, via `IResponsePayloadMapper<TContext>`
  (`DefaultResponsePayloadMapper<TContext>`).
- **`DefaultResponseStatusHandler<TContext>`** / transport-specific status handlers — map the
  `IBenzeneResult.Status` string onto the transport's native status/acknowledgement concept (HTTP
  status code, SQS batch-item-failure, etc. — see [Message Results](message-result#transport-mapping)
  for the full mapping table).

`IMessageHandlerResultSetter<TContext>` is the seam between `MessageRouter<TContext>` and all of
this — it's what actually stores the `IMessageHandlerResult` on the context so the response
handlers (and diagnostics, e.g. `ActivityMiddlewareDecorator`'s `benzene.handler` tag — see
[Middleware](middleware#automatic-activity-wrapping-imiddlewarewrapper)) can read it afterwards.

## See also

- [Message Results](message-result) — `IBenzeneResult<T>`, the `BenzeneResult` factory, result
  statuses, and how they map onto transport-specific responses.
- [Middleware](middleware) — the pipeline mechanism `MessageRouter<TContext>` and per-handler
  middleware (like FluentValidation) are built on.
- [Common Middleware](common-middleware#usemessagehandlers) — `.UseMessageHandlers(...)` and
  `.UseFluentValidation()` as ready-made pipeline middleware.
- [Fluent Validation](fluent-validation) — request validation before a handler is invoked.
- [Spec](spec) — generating OpenAPI/AsyncAPI documentation from `[Message]`/`[HttpEndpoint]`
  metadata.
