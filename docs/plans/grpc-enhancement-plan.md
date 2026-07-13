# Benzene.Grpc Enhancement Plan

## Context

`src/Benzene.Grpc` is a prototype-grade adapter: it routes only **unary** gRPC calls into the Benzene pipeline via a server interceptor, bridges protobuf↔POCO with a lossy `System.Text.Json` round-trip, ignores gRPC metadata entirely (`GrpcMessageHeadersGetter` returns an empty dict), discards `IBenzeneResult` status (no `RpcException`/StatusCode mapping), never propagates deadlines/cancellation, is hardcoded to a single `ServiceDescriptor` (one gRPC service max), has a bug in `GrpcContext<TReq,TResp>.ResponseAsObject` (missing `return` — always JSON round-trips), uses obsolete `IgnoreNullValues`, carries dead code (`BenzeneInterceptor2`, `GrpcMethodHandler2`, commented `GrpcMethodHandlerFactory2`, unused `GrpcMethodTopicRoute`), registers only half its services in `AddGrpc()` (the example hand-assembles the rest in `Program.cs`), and has **zero tests**.

Goal: bring the package up to the conventions of the mature adapters (`Benzene.AspNet.Core` for inbound, `Benzene.Kafka.Core/Kafka` for outbound) while exploiting the full gRPC feature set: all four streaming shapes, metadata↔headers both directions, status-code mapping, deadlines/cancellation, health checks, server reflection, and an outbound `IBenzeneMessageClient`.

This plan is written for mechanical execution. Design decisions are final — do not re-open them. Phases must run in order; each compiles and tests independently.

## Verified facts the plan relies on

- `MessageRouter<TContext>` (`src/Benzene.Core.MessageHandlers/MessageRouter.cs`) is transport-agnostic and unary-shaped; it resolves requests lazily via `IRequestMapper<TContext>.GetBody<TRequest>(context) where TRequest : class`. `IAsyncEnumerable<T>` satisfies `class`, which enables streaming handlers **without touching core abstractions**.
- The Benzene pipeline carries **no CancellationToken** (`IMiddleware<TContext>.HandleAsync(TContext, Func<Task>)`), so cancellation must ride on the gRPC context.
- Status-mapping precedent: `IHttpStatusCodeMapper`/`DefaultHttpStatusCodeMapper` in `src/Benzene.Http/`; statuses are `BenzeneResultStatus` constants in `src/Benzene.Results/BenzeneResultStatus.cs`.
- Inbound gold standard: `src/Benzene.AspNet.Core/{AspNetContext, AspNetApplication, BenzeneExtensions (UseHttp), AspApplicationBuilder, DependencyInjectionExtensions, AspNetResponseAdapter}.cs`. `UseHttp(this IBenzeneApplicationBuilder ...)` pattern-matches `IAspApplicationBuilder` and no-ops otherwise (`docs/hosting.md`).
- Outbound blueprint: `src/Benzene.Kafka.Core/Kafka/{KafkaSendMessageContext, KafkaContextConverter, KafkaClientMiddleware, KafkaBenzeneMessageClient, Extensions, DependencyInjectionExtensions}.cs`.
- Every generated protobuf message type exposes static `Descriptor` and `Parser` properties — this removes any need for a hardcoded `ServiceDescriptor`.
- Tests: xUnit + Moq; pipeline-test model is `test/Benzene.Core.Test/Aws/Sqs/SqsMessagePipelineTest.cs`; test host infra in `src/Benzene.Testing/BenzeneTestHost.cs`.

## ⚠️ FLAGS — approved by approving this plan

**Solution structure** (CLAUDE.md requires approval): 4 new projects added to `Benzene.sln`:
`src/Benzene.Grpc.AspNet`, `src/Benzene.Grpc.Client`, `src/Benzene.Grpc.TestHelpers`, `test/Benzene.Grpc.Test`. Rationale: dependency hygiene matching Http/Kafka/AWS — `Benzene.Grpc` stays lean (Grpc.Core.Api only); hosting glue needs Grpc.AspNetCore; client needs Grpc.Net.Client.

**NuGet dependencies** (CLAUDE.md requires approval):

| Project | Change |
|---|---|
| src/Benzene.Grpc | Upgrade Google.Protobuf 3.18.0 → 3.26.1 |
| src/Benzene.Grpc.AspNet (new) | Grpc.AspNetCore 2.62.0, Grpc.AspNetCore.HealthChecks 2.62.0, Grpc.AspNetCore.Server.Reflection 2.62.0 |
| src/Benzene.Grpc.Client (new) | Grpc.Net.Client 2.62.0, Google.Protobuf 3.26.1 |
| src/Benzene.Grpc.TestHelpers (new) | Grpc.Net.Client 2.62.0, Microsoft.AspNetCore.Mvc.Testing (repo's ASP.NET version) |
| test/Benzene.Grpc.Test (new) | xunit, Moq, Microsoft.NET.Test.Sdk (copy versions from test/Benzene.Core.Test), Grpc.AspNetCore, Grpc.Net.Client, Grpc.Tools |
| examples/Grpc/Benzene.Example.Grpc | Grpc.AspNetCore 2.40.0 → 2.62.0; replace raw-DLL `<Reference>` to Benzene.Core.Messages with a `<ProjectReference>` |

**Breaking changes — clean break, no `[Obsolete]` shims** (the package is an unusable prototype; its only consumer is the broken example, which this plan fixes):
1. `GrpcContext` ctor gains `ServerCallContext`.
2. `GrpcMethodHandlerFactory` ctor loses the `ServiceDescriptor` parameter.
3. Failed `IBenzeneResult` now throws `RpcException` with a mapped StatusCode instead of returning an empty message (behavioral break — this is the headline fix).
4. Dead public types deleted: `BenzeneInterceptor2`, `GrpcMethodHandler2`, `GrpcMethodTopicRoute`.
5. `AddGrpc(this IBenzeneServiceContainer)` renamed `AddGrpcMessageHandlers` (matches `AddAspNetMessageHandlers`, avoids colliding with Grpc.AspNetCore's `IServiceCollection.AddGrpc`).

## Design decisions (final)

- **D1 Streaming:** one pipeline invocation per RPC (like one per HTTP request); streams are `IAsyncEnumerable<T>` values flowing through `MessageRouter` untouched. Server-streaming handler: `IMessageHandler<TRequest, IAsyncEnumerable<TItem>>`; client-streaming: `IMessageHandler<IAsyncEnumerable<TItem>, TResponse>`; bidi: both. Per-item middleware is out of scope (document: per-item concerns belong in the handler).
- **D2 Serialization:** new `IGrpcMessageAdapter`. Rule 1: if the handler's type IS the protobuf type, pass through untouched (zero-copy, code-first). Rule 2: otherwise bridge via protobuf's own JSON (`Google.Protobuf.JsonFormatter`/`JsonParser`), obtaining `MessageDescriptor` from the generated type's static `Descriptor` property, cached in `ConcurrentDictionary<Type, MessageDescriptor>`. Plus a `GrpcRequestMapper : IRequestMapper<GrpcContext>` doing pass-through/convert/stream-wrap.
- **D3 Multi-service:** falls out of D2 (no descriptor needed). `GrpcRouteFinder` becomes a `Dictionary<string, IGrpcMethodDefinition>(StringComparer.OrdinalIgnoreCase)` built once, singleton.
- **D4 Status mapping:** `IGrpcStatusCodeMapper` mirroring `DefaultHttpStatusCodeMapper`. Table: Ok/Ignored/Created/Accepted/Updated/Deleted→OK; BadRequest/ValidationError→InvalidArgument; Unauthorized→Unauthenticated; Forbidden→PermissionDenied; NotFound→NotFound; Conflict→AlreadyExists; NotImplemented→Unimplemented; ServiceUnavailable→Unavailable; UnexpectedError/unknown→Internal. Non-OK → `throw RpcException(new Status(mapped, message))`; all responses get a `benzene-status: <raw>` trailer.
- **D5 Metadata:** inbound `RequestHeaders` (skip `.IsBinary`, duplicates: last wins) → Benzene headers, so correlation-id/traceparent middleware work unchanged. Outbound: buffered `GrpcContext.ResponseHeaders` (written via `WriteResponseHeadersAsync` before the first response message) + pass-through `ResponseTrailers`.
- **D6 Deadlines/cancellation:** `GrpcContext` carries `ServerCallContext` (hence token + deadline). New scoped `IGrpcServerCallAccessor` (analogous to `IHttpContextAccessor`) populated per call so handlers can observe the token. Catch `OperationCanceledException` → `RpcException(DeadlineExceeded)` if past deadline else `RpcException(Cancelled)`.
- **D7 Client:** Kafka blueprint in `Benzene.Grpc.Client`; topic→`Method<TReq,TResp>` registry; `CallInvoker`-based middleware; `GrpcBenzeneMessageClient : IBenzeneMessageClient`.
- **D8 Health/reflection:** opt-in via `BenzeneGrpcOptions` in `Benzene.Grpc.AspNet`, using standard Grpc.AspNetCore packages, plus a bridge exposing Benzene health checks (`src/Benzene.HealthChecks.Core`) to grpc.health.v1.
- **D9 Hosting:** `AddBenzeneGrpc(this IServiceCollection, ...)` (registers Grpc.AspNetCore + `BenzeneInterceptor`) and `UseGrpc(this IBenzeneApplicationBuilder, Action<IMiddlewarePipelineBuilder<GrpcContext>>)` pattern-matching `IAspApplicationBuilder`, exactly like `UseHttp`.

---

## Phase 1 — Cleanup and bug fixes

Modify (all under `src/Benzene.Grpc/`):
- `GrpcContext.cs` — fix setter: `if (value is TResponse typed) { Response = typed; return; }` before the JSON fallback.
- `GrpcMethodHandler.cs` — delete `GrpcMethodHandler2`; replace `IgnoreNullValues = true` with `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`.
- `BenzeneInterceptor.cs` — delete `BenzeneInterceptor2`.
- `GrpcMethodHandlerFactory.cs` — delete the commented `GrpcMethodHandlerFactory2` block.
- `GrpcRouteFinder.cs` — case-insensitive dictionary built in ctor; `Find` = `TryGetValue` (D3).
- `Benzene.Grpc.csproj` — Google.Protobuf → 3.26.1.

Delete: `src/Benzene.Grpc/GrpcMethodTopicRoute.cs` (unused). Defer example-file deletions to Phase 3 so the examples solution keeps building.

Create `test/Benzene.Grpc.Test/` (net10.0; xunit/Moq/Test.Sdk versions copied from `test/Benzene.Core.Test/Benzene.Test.csproj`; ProjectReferences: Benzene.Grpc, Benzene.Core.MessageHandlers, Benzene.Microsoft.Dependencies, Benzene.Testing) with `Protos/test.proto` (package `benzene.test`, service `TestService`: unary `Echo`, server-streaming `Subscribe`, client-streaming `Upload`, bidi `Chat`), compiled `GrpcServices="Both"`.

Tests: `GrpcContextTest` (`ResponseAsObject_WhenValueIsTResponse_AssignsSameInstance` asserting `ReferenceEquals`; POCO conversion path), `GrpcRouteFinderTest` (exact match, case-insensitive, miss→null, duplicate `[GrpcMethod]` → `BenzeneException` via `ReflectionGrpcMethodFinder` with mocked handler finder).

Acceptance: `dotnet build Benzene.sln` + `dotnet test test/Benzene.Grpc.Test` green; `grep -rn "IgnoreNullValues\|BenzeneInterceptor2\|GrpcMethodHandler2\|GrpcMethodTopicRoute" src/` empty.

## Phase 2 — Serialization overhaul + multi-service (D2, D3)

Create in `src/Benzene.Grpc/`:
- `Serialization/IGrpcMessageAdapter.cs`:
  ```csharp
  public interface IGrpcMessageAdapter
  {
      TRequest? ConvertRequest<TRequest>(object protobufMessage) where TRequest : class;
      TResponse ConvertResponse<TResponse>(object? payload) where TResponse : class;
  }
  ```
- `Serialization/ProtobufJsonGrpcMessageAdapter.cs` — pass-through when types already match; POCO→protobuf via System.Text.Json serialize (camelCase, WhenWritingNull) then `JsonParser.Default.Parse(json, GetDescriptor(typeof(TResponse)))`; protobuf→POCO via `JsonFormatter.Default.Format((IMessage)msg)` then `JsonSerializer.Deserialize<TRequest>(json, PropertyNameCaseInsensitive = true)`; `GetDescriptor` reflects the static `Descriptor` property, cached; throws `BenzeneException` if the target isn't a protobuf message.
- `GrpcRequestMapper.cs : IRequestMapper<GrpcContext>` — `TRequest direct => direct`, else adapter convert. (Phase 5 extends for streams.)

Modify:
- `GrpcMethodHandler.cs` — remove `ServiceDescriptor` field/param and `Parse` method; inject `IGrpcMessageAdapter`; `HandleAsync` = run pipeline, `return _adapter.ConvertResponse<TResponse>(payload)`.
- `GrpcMethodHandlerFactory.cs` — drop the `ServiceDescriptor` ctor param (clean break).
- `GrpcMessageBodyGetter.cs` — `IMessage m => JsonFormatter.Default.Format(m)`, else System.Text.Json.
- `GrpcContext.cs` — setter becomes typed pass-through; add `object? ResponsePayload` on the base for untyped payloads; add `IMessageHandlerResult? MessageHandlerResult { get; set; }` (needed by Phase 4).
- `GrpcMessageMessageHandlerResultSetter.cs` — set both `ResponseAsObject` and `MessageHandlerResult`.
- `DependencyInjectionExtensions.cs` — register `IGrpcMessageAdapter` (TryAdd) + `GrpcRequestMapper`.

Tests: `ProtobufJsonGrpcMessageAdapterTest` (pass-through both ways, POCO→`EchoReply` incl. camelCase/null handling, protobuf→POCO, non-protobuf target throws); `GrpcRequestMapperTest`; `GrpcMethodPipelineTest` modeled on `SqsMessagePipelineTest.cs` (build `MiddlewarePipelineBuilder<GrpcContext>` over `MicrosoftBenzeneServiceContainer`, `.UseMessageHandlers(...)`, invoke `GrpcMethodHandler.HandleAsync<EchoRequest, EchoReply>`) with handlers from **two different services** to prove multi-service works.

Acceptance: no `Google.Protobuf.Reflection.ServiceDescriptor` reference remains in Benzene.Grpc; handlers may declare protobuf types directly; tests green.

## Phase 3 — DI parity, hosting, example fix (D9)

Create `src/Benzene.Grpc.AspNet/` (net10.0, `<FrameworkReference Include="Microsoft.AspNetCore.App"/>`, Grpc.AspNetCore; ProjectReferences: Benzene.Grpc, Benzene.AspNet.Core, Benzene.Core.MessageHandlers, Benzene.Microsoft.Dependencies):
- `ServiceCollectionExtensions.cs`: `AddBenzeneGrpc(this IServiceCollection, Action<GrpcServiceOptions>? configure = null)` → `services.AddGrpc(o => { o.Interceptors.Add(typeof(BenzeneInterceptor)); configure?.Invoke(o); })`.
- `BenzeneExtensions.cs` mirroring `src/Benzene.AspNet.Core/BenzeneExtensions.cs`:
  ```csharp
  public static IAspApplicationBuilder UseGrpc(this IAspApplicationBuilder app, Action<IMiddlewarePipelineBuilder<GrpcContext>> action)
  {
      var pipeline = app.Create<GrpcContext>();
      app.Register(x => x.AddGrpcMessageHandlers());
      action(pipeline);
      var built = pipeline.Build();
      app.Register(x => x.AddSingleton<IGrpcMethodHandlerFactory>(_ => new GrpcMethodHandlerFactory(x, built)));
      return app;
  }
  public static IBenzeneApplicationBuilder UseGrpc(this IBenzeneApplicationBuilder app, Action<IMiddlewarePipelineBuilder<GrpcContext>> action)
  { if (app is IAspApplicationBuilder aspApp) aspApp.UseGrpc(action); return app; }
  ```
  (Post-build registration works because `AspApplicationBuilder` reopens the Microsoft container and `BenzeneInterceptor` resolves its dependencies from DI per call — same trick `UseHttp` relies on.)

Modify `src/Benzene.Grpc/DependencyInjectionExtensions.cs` — rename to `AddGrpcMessageHandlers` with full parity to `AddAspNetMessageHandlers` (`src/Benzene.AspNet.Core/DependencyInjectionExtensions.cs`): finders (singleton — reflection once), the four context-keyed mappers (scoped), `GrpcRequestMapper`, `IGrpcMessageAdapter` (TryAdd), `MessageRouter<GrpcContext>`, `ITransportInfo("grpc")`, `AddContextItems()`.

Create `src/Benzene.Grpc/Registrations/GrpcRegistrations.cs : RegistrationsBase` mirroring `src/Benzene.Http/Registrations/HttpRegistrations.cs`.

Fix `examples/Grpc/Benzene.Example.Grpc/`: `Program.cs` uses `AddBenzeneGrpc()` + `AddGrpcMessageHandlers` + `app.UseBenzene(x => x.UseGrpc(g => g.UseMessageHandlers()))` + `MapGrpcService<GreeterService>()`; csproj swaps the raw DLL `<Reference>` for ProjectReferences (Benzene.Core.Messages, Benzene.Grpc.AspNet), Grpc.AspNetCore → 2.62.0; delete `Services/Extensions.cs` and `Services/ErrorHandlerInterceptor.cs`.

Tests: `GrpcHostingTest` — in-process TestServer host, `GrpcChannel` over the TestServer handler, generated `TestService.TestServiceClient.EchoAsync` returns the handler payload; non-Benzene routes still fall through to the generated service base; `UseGrpc` on a non-AspNet builder no-ops (mirror `test/Benzene.Core.Test/Hosting/UnifiedStartUpTest.cs`).

Acceptance: fresh app needs only `AddBenzeneGrpc` + `AddGrpcMessageHandlers` + `UseGrpc`; example builds without the raw DLL reference; `dotnet build Benzene.Examples.sln` green.

## Phase 4 — Metadata, status codes, deadlines/cancellation (D4, D5, D6)

Modify `src/Benzene.Grpc/GrpcContext.cs` (**breaking**): ctor gains `ServerCallContext`; add `ServerCallContext CallContext`, `CancellationToken CancellationToken => CallContext.CancellationToken`, buffered `Metadata ResponseHeaders`, `Metadata ResponseTrailers => CallContext.ResponseTrailers`.

Modify `GrpcMessageHeadersGetter.cs`: map `CallContext.RequestHeaders` (skip `IsBinary`, group by key, last wins) into the dictionary.

Create `IGrpcStatusCodeMapper.cs` + `DefaultGrpcStatusCodeMapper.cs` (shape of `src/Benzene.Http/IHttpStatusCodeMapper.cs`, table per D4; default `Internal`); register TryAddSingleton.

Create `IGrpcServerCallAccessor.cs` + `GrpcServerCallAccessor.cs` (scoped; register concrete + interface to the same instance).

Rewrite `GrpcMethodHandler.HandleAsync`:
1. Create scope; set `GrpcServerCallAccessor.CallContext` (tolerate unregistered).
2. Build `GrpcContext<TRequest,TResponse>(topic, callContext, request)`; run pipeline in try/catch(`OperationCanceledException`) → `RpcException(DeadlineExceeded | Cancelled)` per D6.
3. Map `MessageHandlerResult.BenzeneResult.Status` via mapper; add `benzene-status` trailer.
4. Non-OK → `throw RpcException(new Status(code, result.Message ?? status))`.
5. If buffered `ResponseHeaders` non-empty → `WriteResponseHeadersAsync`.
6. Return `_adapter.ConvertResponse<TResponse>(...)`.

Tests: `[Theory]` covering every `BenzeneResultStatus` constant in the mapper; `GrpcMethodHandlerTest` using a hand-rolled `TestCallContext : ServerCallContext` in `test/Benzene.Grpc.Test/Helpers/` (do NOT add the Grpc.Core.Testing package): headers reach `IMessageHeadersGetter`; NotFound result → `RpcException(NotFound)` + trailer; cancelled token → `RpcException(Cancelled)`; handler observes token via `IGrpcServerCallAccessor`. Integration: `UseCorrelationId` picks up `x-correlation-id` sent as metadata.

## Phase 5 — Streaming (D1)

Create `src/Benzene.Grpc/Streaming/GrpcStreamAdapter.cs` (internal static): `ReadAll<T>(IAsyncStreamReader<T>, CancellationToken)` → `IAsyncEnumerable<T>`; `Convert<TIn,TOut>(IAsyncEnumerable<TIn>, IGrpcMessageAdapter, CancellationToken)` lazily-converting; `WriteAll<T>(IAsyncEnumerable<T>, IServerStreamWriter<T>, CancellationToken)`.

Extend `GrpcRequestMapper`: when `TRequest` is `IAsyncEnumerable<TItem>` and `RequestAsObject` is `IAsyncEnumerable<TProto>` — pass through if assignable, else wrap with `Convert` (cached `MakeGenericMethod`).

Extend `IGrpcMethodHandler` + `GrpcMethodHandler` with `ServerStreamingAsync<TRequest,TResponse>(TRequest, IServerStreamWriter<TResponse>, ServerCallContext)`, `ClientStreamingAsync<TRequest,TResponse>(IAsyncStreamReader<TRequest>, ServerCallContext)`, `DuplexStreamingAsync<...>` — all reusing a private `RunPipelineAsync(GrpcContext, ServerCallContext)` extracted from the Phase 4 skeleton (scope/accessor/status/metadata/cancellation). Server-streaming: handler payload is `IAsyncEnumerable<TResponse>` (pass-through) or `IAsyncEnumerable<TPoco>` (wrap), then `WriteAll` under the call token; error status throws before writing anything. Client-streaming: `RequestAsObject = ReadAll(requestStream, ct)`. Duplex: both.

Extend `BenzeneInterceptor` with `ServerStreamingServerHandler`, `ClientStreamingServerHandler`, `DuplexStreamingServerHandler` — same route-find/substitute/fall-through shape as the unary override.

Tests: in-process host + generated client — server-streaming yields 3 items, client-streaming sums an upload, bidi echo, POCO-item variants (exercise the converting wrapper), client cancels mid-stream → handler token fires + `StatusCode.Cancelled`, error status in a streaming handler → `RpcException`. Unit tests for `GrpcStreamAdapter` with hand-rolled reader/writer fakes.

Acceptance: all four RPC shapes route through the same `IMiddlewarePipeline<GrpcContext>`; middleware observes exactly one invocation per call regardless of stream length.

## Phase 6 — Outbound client (D7)

Create `src/Benzene.Grpc.Client/` (deps per flag table; ProjectReferences: Benzene.Grpc, Benzene.Clients, Benzene.Abstractions.Messages, Benzene.Core.Middleware, Benzene.Core.MessageHandlers). Each file mirrors its Kafka counterpart in `src/Benzene.Kafka.Core/Kafka/`:

1. `GrpcSendMessageContext.cs` — `{ string Topic; object Message; Metadata Headers; DateTime? Deadline; CancellationToken CancellationToken; object? Response; Status Status; Metadata? ResponseTrailers; }`.
2. `IGrpcClientRouteRegistry.cs` + `GrpcClientRouteRegistry.cs` — `Add<TRequest,TResponse>(string topic, string fullMethodName)` builds `Method<TReq,TResp>` from `MethodType.Unary`, service/method names, and `Marshallers.Create(m => m.ToByteArray(), TReq.Parser.ParseFrom)` (static `Parser` via cached reflection, same trick as Phase 2 descriptors); `IGrpcClientRoute? Find(string topic)`; the closed-generic route converts POCO→`TRequest` via the adapter and calls `invoker.AsyncUnaryCall(method, null, new CallOptions(headers, deadline, ct), request)`.
3. `GrpcClientMiddleware.cs : IMiddleware<GrpcSendMessageContext>` — ctor `(CallInvoker, IGrpcClientRouteRegistry, IGrpcMessageAdapter)`; no route → `Status(Unimplemented)`; catch `RpcException` into `context.Status`/`ResponseTrailers`; do NOT swallow other exceptions.
4. `GrpcContextConverter<T>.cs : IContextConverter<IBenzeneClientContext<T, Void>, GrpcSendMessageContext>` — headers dict→`Metadata`; `MapResponseAsync` maps StatusCode back via new `IGrpcStatusReverseMapper`/`DefaultGrpcStatusReverseMapper` (OK→Ok, InvalidArgument→BadRequest, Unauthenticated→Unauthorized, PermissionDenied→Forbidden, NotFound→NotFound, AlreadyExists→Conflict, Unimplemented→NotImplemented, Unavailable/DeadlineExceeded/Cancelled→ServiceUnavailable, else UnexpectedError; a `benzene-status` trailer wins verbatim when present).
5. `GrpcBenzeneMessageClient.cs : IBenzeneMessageClient` — mirror `KafkaBenzeneMessageClient`: ctor `(GrpcChannel, IGrpcClientRouteRegistry, ...)` builds the send pipeline; `SendMessageAsync<TRequest,TResponse>` converts, runs pipeline, converts protobuf response → `TResponse` via the adapter, wraps in mapped `IBenzeneResult<TResponse>`; catch → `ServiceUnavailable`.
6. `Extensions.cs` — `UseGrpcClient(...)` + `UseGrpc<T>(this IMiddlewarePipelineBuilder<IBenzeneClientContext<T, Void>>, ...)` using the same `Convert(...)` shape as Kafka's `UseKafka<T>`.
7. `DependencyInjectionExtensions.cs` — `AddGrpcClient(this IBenzeneServiceContainer, Action<IGrpcClientRouteRegistry>)`.

Tests: `TestCallInvoker : CallInvoker` capturing calls and returning canned `AsyncUnaryCall` — route found/not found, headers→`CallOptions`, `RpcException(NotFound)` → Benzene NotFound, trailer wins over reverse map. Integration: `GrpcBenzeneMessageClient` over the Phase 3 TestServer channel round-trips a POCO request/response.

## Phase 7 — Health checks + reflection (D8)

In `src/Benzene.Grpc.AspNet/`:
- Extend `AddBenzeneGrpc` with `BenzeneGrpcOptions { bool EnableHealthChecks; bool EnableReflection; Action<GrpcServiceOptions>? ConfigureGrpc; }` — flags call `services.AddGrpcHealthChecks()` / `services.AddGrpcReflection()`.
- `GrpcEndpointExtensions.cs`: `MapBenzeneGrpcHealthService` / `MapBenzeneGrpcReflectionService` wrapping the standard `MapGrpcHealthChecksService()` / `MapGrpcReflectionService()`.
- `BenzeneHealthCheckBridge.cs`: ASP.NET `IHealthCheck` aggregating Benzene health checks so grpc.health.v1 `Check`/`Watch` reflect Benzene health; registered via `.AddCheck<BenzeneHealthCheckBridge>("benzene")` when enabled. **Before coding, read `src/Benzene.HealthChecks.Core` to confirm the Benzene health-check interface members** (only item deliberately left to verify at implementation time).

Tests: `HealthClient.Check` returns SERVING; fake unhealthy Benzene check → NOT_SERVING; `ServerReflectionClient` lists `benzene.test.TestService` when enabled. Both features off by default.

## Phase 8 — TestHelpers, docs, examples polish

Create `src/Benzene.Grpc.TestHelpers/` (pattern: `src/Benzene.Aws.Sqs.TestHelpers/`): `GrpcTestHost` building an in-memory TestServer from a `BenzeneStartUp` (reuse `BenzeneTestHostBuilder` from `src/Benzene.Testing/BenzeneTestHost.cs`) with `CreateChannel()` + `BuildGrpcHost<TStartUp>` extension; promote `TestServerCallContext` (the hand-rolled `ServerCallContext` subclass) from the test project into this package; retarget the test project's helpers onto it.

Docs: rewrite `docs/getting-started-grpc.md` (new wiring, protobuf-direct + POCO handler styles, all three streaming recipes, metadata/correlation, D4 status table, deadlines via `IGrpcServerCallAccessor`, health/reflection opt-in, client, TestHelpers); update `docs/hosting.md` (UseGrpc), `docs/clients.md` (GrpcBenzeneMessageClient), `docs/health-checks.md` (bridge); rewrite `src/Benzene.Grpc/CLAUDE.md` to match reality and add CLAUDE.md to the three new src projects.

Examples: add one handler + client call per streaming shape to `examples/Grpc`; client sends `x-correlation-id` metadata and prints the `benzene-status` trailer.

---

## Verification

After each phase: `dotnet build /home/user/Benzene/Benzene.sln` and `dotnet test /home/user/Benzene/test/Benzene.Grpc.Test`; from Phase 3 onward also `dotnet build /home/user/Benzene/Benzene.Examples.sln`.

End-to-end (Phase 8): run `examples/Grpc/Benzene.Example.Grpc` and drive it with `examples/Grpc/Benzene.Example.Grpc.Client` — verify unary + all streaming shapes, a failing handler surfacing the mapped gRPC status code and `benzene-status` trailer, correlation-id metadata round-trip, and (with flags enabled) grpc.health.v1 SERVING + reflection listing the service. Full suite: `dotnet test` at repo root must stay green (no existing tests skipped or disabled).

Regression guard: keep one test asserting the old POCO-with-matching-property-names handler style still round-trips (now via protobuf JSON instead of System.Text.Json).

## Execution notes for the implementing agent

- Follow existing file style: file-scoped namespaces, one type per file (except tiny interface+impl pairs already co-located).
- Never edit files outside the paths listed.
- Design decisions D1–D9 are final; the flagged NuGet/solution/breaking changes were approved with this plan — do not re-ask.
- Work on branch `claude/benzene-grpc-design-plan-1z2hb8`; commit per phase with a descriptive message; push with `git push -u origin claude/benzene-grpc-design-plan-1z2hb8`.
