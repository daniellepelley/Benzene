# Attributes Reference

Benzene uses attributes to declare the contract of a message handler — its topic and the
transports that route to it — so they're discovered by reflection with no manual registration.
This page lists every attribute you apply when authoring handlers.

## `[Message]`

**Package:** `Benzene.Core.MessageHandlers` · **Namespace:** `Benzene.Core.MessageHandlers` ·
**Target:** class (not inherited)

Maps a message handler to its **topic** — the stable identifier every transport routes by. Every
handler needs exactly one.

```csharp
public MessageAttribute(string topic, string version = "")
```

| Property | Purpose |
|---|---|
| `Topic` | The topic the handler responds to (e.g. `"order:create"`). |
| `Version` | Optional contract version, for running versioned message contracts side by side. Defaults to `""`. |

```csharp
[Message("order:create")]
public class CreateOrderHandler : IMessageHandler<CreateOrderRequest, OrderDto> { /* … */ }

[Message("order:create", "v2")]
public class CreateOrderHandlerV2 : IMessageHandler<CreateOrderRequestV2, OrderDto> { /* … */ }
```

See [Message Handlers](../message-handlers).

## `[HttpEndpoint]`

**Package:** `Benzene.Http` · **Namespace:** `Benzene.Http` · **Target:** class (repeatable)

Maps an HTTP method and URL pattern onto the handler's topic, so HTTP transports (ASP.NET Core,
API Gateway, Azure Functions HTTP) can route to it. Apply it **multiple times** to expose one
handler at several routes.

```csharp
public HttpEndpointAttribute(string method, string url)
```

| Property | Purpose |
|---|---|
| `Method` | The HTTP method — `GET`, `POST`, `PUT`, `DELETE`, `PATCH`, etc. |
| `Url` | The URL pattern; may include route parameters like `{id}`, bound onto the request. |

```csharp
[Message("user:get")]
[HttpEndpoint("GET", "/users/{id}")]
[HttpEndpoint("GET", "/api/users/{id}")]
public class GetUserHandler : IMessageHandler<GetUserRequest, GetUserResponse> { /* … */ }
```

Route parameters (`{id}`) are bound onto the matching property of the request type. See
[ASP.NET Core](../asp-net-core) and [Getting Started](../getting-started).

## `[GrpcMethod]`

**Package:** `Benzene.Grpc` · **Namespace:** `Benzene.Grpc` · **Target:** class (repeatable)

Maps a gRPC method onto the handler's topic so the [gRPC transport](packages#other-hosts) can
route calls to it. Repeatable, to serve several methods from one handler.

```csharp
public GrpcMethodAttribute(string method)
```

| Property | Purpose |
|---|---|
| `Method` | The gRPC method name to route to this handler. |

```csharp
[Message("greeter:hello")]
[GrpcMethod("Greeter/SayHello")]
public class SayHelloHandler : IMessageHandler<HelloRequest, HelloResponse> { /* … */ }
```

---

> **Tooling note:** `[Arg]` (`Benzene.CodeGen.Cli.Core`) configures arguments for the
> code-generation CLI itself and is not part of the handler-authoring surface — you won't apply
> it to your own message handlers.

## See also

- [Message Handlers](../message-handlers) — where these attributes are applied.
- [Middleware Reference](middleware) — the pipeline steps that act on routed messages.
- [Package Reference](packages) — which package each attribute ships in.
