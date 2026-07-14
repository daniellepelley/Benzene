# Benzene.Grpc

## What this package does
Core gRPC/Benzene bridge: routes gRPC calls of all four RPC shapes (unary, server-streaming,
client-streaming, bidirectional) into Benzene message handlers, and bridges protobuf
messages/metadata/status codes to Benzene's transport-agnostic abstractions. Deliberately
transport-host-agnostic - depends only on `Grpc.Core.Api` and `Google.Protobuf`, not
`Grpc.AspNetCore`. Hosting glue lives in `Benzene.Grpc.AspNet`; the outbound client in
`Benzene.Grpc.Client`; test infrastructure in `Benzene.Grpc.TestHelpers`.

## Key types/interfaces

### Routing and dispatch
- `BenzeneInterceptor` - a `Grpc.Core.Interceptors.Interceptor` with one override per RPC shape
  (`UnaryServerHandler`, `ClientStreamingServerHandler`, `ServerStreamingServerHandler`,
  `DuplexStreamingServerHandler`); falls through to the native `ServiceBase` implementation for any
  method not matched to a Benzene handler
- `GrpcMethodAttribute` - decorates a message handler class with the gRPC method path it serves
  (e.g. `/package.Service/Method`); combined with `[Message("topic")]`
- `IGrpcMethodFinder`/`ReflectionGrpcMethodFinder` - discovers `[GrpcMethod]`-decorated handlers via
  `IMessageHandlersFinder`; throws if a method is claimed by more than one handler
- `IGrpcRouteFinder`/`GrpcRouteFinder` - case-insensitive method-path -> topic lookup, built once
- `IGrpcMethodHandler`/`GrpcMethodHandler` - runs the shared `IMiddlewarePipeline<GrpcContext>` for
  a call (`HandleAsync`, `ServerStreamingAsync`, `ClientStreamingAsync`, `DuplexStreamingAsync`),
  translating cancellation and non-OK `IBenzeneResult` statuses into `RpcException`
- `IGrpcMethodHandlerFactory`/`GrpcMethodHandlerFactory`, `IGrpcMethodHandlerFactoryAccessor` - the
  accessor is a DI *instance* registered during `ConfigureServices` and populated once the pipeline
  is built in `Configure`/`UseGrpc`, so it resolves to the same object from both ASP.NET Core's own
  per-request DI (which activates `BenzeneInterceptor`) and Benzene's pipeline-building container

### Context
- `GrpcContext`/`GrpcContext<TRequest,TResponse>` - carries the `ServerCallContext`, request/response
  payloads, buffered response headers, and (via the base type's `MessageHandlerResult`) the handler's
  result. For streaming shapes, `TRequest`/`TResponse` are instantiated as `IAsyncEnumerable<TItem>`.

### Serialization (D2)
- `IGrpcMessageAdapter`/`ProtobufJsonGrpcMessageAdapter` - `ConvertRequest<T>` converts an incoming
  protobuf message to an arbitrary target type (protobuf's own JSON round-trip when not a direct
  pass-through); `ConvertResponse<T>` converts an arbitrary payload to an outgoing protobuf message.
  A handler that declares the protobuf type directly gets zero-copy pass-through either way.
- `GrpcRequestMapper : IRequestMapper<GrpcContext>` - pass-through, adapter-convert, or (for
  `IAsyncEnumerable<T>` handler types) lazily wrap a stream via `GrpcStreamAdapter`
- `Streaming/GrpcStreamAdapter` (internal) - bridges `IAsyncStreamReader<T>`/`IServerStreamWriter<T>`
  and `IAsyncEnumerable<T>`; per-item conversion is direction-aware: `ConvertRequest` for inbound
  request-stream items, `ConvertResponse` for outbound response-stream items - these are genuinely
  different directions and using the wrong one silently produces the wrong JSON bridging path

### Status, metadata, cancellation (D4-D6)
- `IGrpcStatusCodeMapper`/`DefaultGrpcStatusCodeMapper` - maps `BenzeneResultStatus` to
  `Grpc.Core.StatusCode`; every response also gets a `benzene-status` trailer carrying the raw status
- `IGrpcServerCallAccessor`/`GrpcServerCallAccessor` - scoped accessor exposing the call's
  `ServerCallContext`/`CancellationToken` to handler code (mirrors `IHttpContextAccessor`)
- `GrpcMessageHeadersGetter` - maps inbound `RequestHeaders` (skipping binary entries) to Benzene
  headers, so header-driven middleware (correlation IDs, etc.) work unchanged over gRPC

## When to use this package
- Building gRPC services with Benzene's message handler pipeline (see `Benzene.Grpc.AspNet` to
  actually host one)
- Calling gRPC services from a Benzene client pipeline (see `Benzene.Grpc.Client`)

## Dependencies on other Benzene packages
- **Benzene.Core** - `BenzeneException`
- **Benzene.Core.MessageHandlers** - message handler/request-mapper infrastructure

## Important conventions
- One middleware pipeline invocation per RPC, regardless of shape or stream length; per-item
  middleware is out of scope by design - per-item concerns belong in the handler
- A handler may declare the protobuf type directly (zero-copy) or a POCO (JSON-bridged) for either
  side, independently, including per stream item
- Multiple gRPC services can share one pipeline; nothing here depends on a fixed `ServiceDescriptor`
- Never add `Grpc.Core.Testing` as a dependency - hand-roll test doubles instead (see
  `Benzene.Grpc.TestHelpers`)
