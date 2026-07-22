# Benzene.Grpc.Client

## What this package does
Outbound gRPC client: `GrpcBenzeneMessageClient : IBenzeneMessageClient`, sending unary calls through
a Benzene middleware pipeline over a `Grpc.Net.Client.GrpcChannel`. Mirrors the shape of
`Benzene.Kafka.Core`'s and `Benzene.Client.Http`'s outbound clients.

## Key types/interfaces

### The client
- `GrpcBenzeneMessageClient` - two constructors: one takes a `GrpcChannel` and builds its own send
  pipeline (the normal DI path); the other takes a pre-built `IMiddlewarePipeline<GrpcSendMessageContext>`
  directly (used by tests). `SendMessageAsync<TRequest,TResponse>` converts the outbound payload,
  runs the pipeline, converts the protobuf response back to `TResponse` via `IGrpcMessageAdapter`,
  and wraps it in an `IBenzeneResult<TResponse>` via the reverse status mapper.
  **Deadline propagation**: when the send happens inside an inbound gRPC call, it resolves
  `IGrpcServerCallAccessor` and forwards that call's absolute `ServerCallContext.Deadline` onto the
  downstream `CallOptions.Deadline`, so the downstream call must finish by the same wall-clock time
  (an inbound `DateTime.MaxValue`, i.e. no deadline, forwards none). This complements the
  already-propagated ambient cancellation token.
- `GrpcSendMessageContext` - the send-pipeline context: topic, boxed message, headers, deadline,
  cancellation in; raw response object, `Status`, response trailers out

### Routing
- `IGrpcClientRouteRegistry`/`GrpcClientRouteRegistry` - `Add<TRequest,TResponse>(topic, fullMethodName)`
  registers a topic against a protobuf-typed `Method<TRequest,TResponse>`, built from each message
  type's static `Parser` (reflected once per type and cached, same pattern as
  `Benzene.Grpc`'s `Descriptor` cache)
- `IGrpcClientRoute`/`GrpcClientRoute<TRequest,TResponse>` - the closed-generic call site; converts
  the outbound payload via `IGrpcMessageAdapter.ConvertResponse` (produce-a-protobuf-from-a-payload -
  the same direction the server uses for its own responses) and invokes `CallInvoker.AsyncUnaryCall`
- `GrpcClientMiddleware` - looks up the route, maps a missing route to `StatusCode.Unimplemented`,
  and captures a thrown `RpcException`'s status/trailers onto the context instead of rethrowing

### Status mapping
- `IGrpcStatusReverseMapper`/`DefaultGrpcStatusReverseMapper` - the reverse of
  `Benzene.Grpc`'s `IGrpcStatusCodeMapper`: maps a `StatusCode` back to a `BenzeneResultStatus`,
  preferring a `benzene-status` trailer verbatim when present (several distinct Benzene statuses
  collapse to the same `StatusCode.OK` on the wire, so the trailer is the only way to recover which
  one it actually was)

### Health check
- `GrpcHealthCheck` - verifies **transport reachability** to the channel's target with
  `GrpcChannel.ConnectAsync` (opens the HTTP/2 connection, no application RPC — non-destructive). `Type =
  "Grpc"`, dependency `("Grpc", channel.Target)`. An unreachable target surfaces as a `ConnectAsync`
  timeout → Failed; an `RpcException` is classified by its gRPC status (`PermissionDenied`/
  `Unauthenticated` → Warning) via `HealthCheckError.Classify`.
  - **Auto-wired (Phase 4, default-on):** `AddGrpcClient(configureRoutes, healthCheck: true)` registers
    it on the **dependency** category (deep `healthcheck` layer only — never a probe; see
    `IDependencyHealthCheck`), resolving the caller's registered `GrpcChannel`. `healthCheck: false` opts
    out; explicit `AddGrpcHealthCheck()` on an `IHealthCheckBuilder` is the manual path.
  - **Deliberately NOT `grpc.health.v1`.** That downstream `Check` asks whether the *downstream itself*
    is serving — **transitive** (it aggregates its own deps), the same hazard as the CodeGen
    contract-drift check — so it belongs on the diagnostic `contracts` topic, not this auto-wired
    default, and it needs the `Grpc.HealthCheck` package (a new dependency, out of scope here).

### Pipeline-embedding building block
- `GrpcContextConverter<T>`/`Extensions.UseGrpc<T>`/`UseGrpcClient` - the reusable
  `Convert(...)`-based building block for embedding a gRPC send as one step of a broader
  `IBenzeneClientContext<T, Void>` pipeline (mirrors Kafka's `UseKafka<T>`). Not used by
  `GrpcBenzeneMessageClient` itself, which talks to `GrpcSendMessageContext` directly so it can
  return the real typed response instead of collapsing to `Void`.

## When to use this package
- Calling another Benzene.Grpc (or any gRPC) service from application code via `IBenzeneMessageClient`

## Dependencies on other Benzene packages
- **Benzene.Abstractions.Messages** - `IBenzeneClientRequest`/`IBenzeneClientContext`/`BenzeneClientContext`
- **Benzene.Clients** - `IBenzeneMessageClient`
- **Benzene.Core.Middleware** - `ContextConverterMiddleware`, `MiddlewarePipelineBuilder`
- **Benzene.Grpc** - `IGrpcMessageAdapter`/`ProtobufJsonGrpcMessageAdapter`

## Important conventions
- `Add<TRequest,TResponse>`'s type parameters are the RPC's protobuf wire types, not necessarily what
  the caller passes to `SendMessageAsync<TRequest,TResponse>` - the adapter bridges POCO callers onto
  those wire types on both the request and response side
- The app owns and registers its own `GrpcChannel` (`AddGrpcClient` does not create one), matching how
  callers own their own `IProducer`/`HttpClient` for the other outbound clients
