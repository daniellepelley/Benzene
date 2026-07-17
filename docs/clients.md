# Clients

Benzene clients let one Benzene service call another — over SQS, SNS, AWS Lambda, Kafka, EventBridge, gRPC, or HTTP — through a single topic-keyed outbound routing table.

## Overview

A Benzene service is a set of message handlers reachable by topic (see [Message Handlers](message-handlers)). When one service needs to call another, it sends through `IBenzeneMessageSender` — the one interface business logic depends on:

```csharp
public interface IBenzeneMessageSender
{
    Task<IBenzeneResult<TResponse>> SendAsync<TRequest, TResponse>(
        string topic, TRequest request, IDictionary<string, string>? headers = null);
}
```

No service name, no client type, no factory resolution at the call site — just a topic and a request. `AddOutboundRouting(...)` builds one outbound pipeline per topic ahead of time (at startup), and `SendAsync` routes to the right one. Cross-cutting behavior (retry, correlation IDs, W3C trace propagation) is added as ordinary `IMiddleware<OutboundContext>` on that pipeline, the same middleware model used everywhere else in Benzene — there's no separate decorator mechanism to learn.

## Installation

Add the core client abstractions, plus whichever transport package(s) you need:

| Package | What it adds |
|---|---|
| `Benzene.Clients` | `IBenzeneMessageSender`, `OutboundContext`, `OutboundRoutingBuilder`/`AddOutboundRouting(...)`, `ValidateOutboundRouting()`, and the cross-cutting middleware (correlation ID, W3C trace context). Pulled in transitively by every transport package below. |
| `Benzene.Clients.Aws` | `.UseSqs(...)`/`.UseSns(...)` outbound route extensions, plus the standalone `AwsLambdaBenzeneMessageClient`/`SqsBenzeneMessageClient`/`SnsBenzeneMessageClient`/`EventBridgeBenzeneMessageClient` transport clients and health checks for them. |
| `Benzene.Kafka.Core` | `KafkaBenzeneMessageClient` (Kafka transport). |
| `Benzene.Grpc.Client` | `GrpcBenzeneMessageClient` (gRPC transport). |
| `Benzene.Client.Http` | `HttpContextConverter`/`HttpClientMiddleware` — the lower-level pipeline building blocks for sending over HTTP (see [HTTP](#http) below). |
| `Benzene.Resilience` | `RetryMiddleware<TContext>`/`.UseRetry(...)` — works on `OutboundContext` unmodified. |

## Basic usage

Register your routes once at startup, then resolve `IBenzeneMessageSender` and call `SendAsync`:

```csharp
services.UsingBenzene(x => x.AddOutboundRouting(routing => routing
    .Route("order:create", pipeline => pipeline.UseSqs(queueUrl))
    .Route("audit:log", pipeline => pipeline.UseSns(topicArn))));
```

```csharp
public class OrderClient
{
    private readonly IBenzeneMessageSender _sender;

    public OrderClient(IBenzeneMessageSender sender)
    {
        _sender = sender;
    }

    public Task<IBenzeneResult<Void>> CreateOrderAsync(CreateOrderRequest request)
    {
        return _sender.SendAsync<CreateOrderRequest, Void>("order:create", request);
    }
}
```

Passing per-call headers (e.g. a caller-supplied tenant value, distinct from anything a route's own middleware adds statically):

```csharp
await _sender.SendAsync<CreateOrderRequest, Void>(
    "order:create",
    request,
    new Dictionary<string, string> { ["x-tenant-id"] = tenantId });
```

Sending to a topic with no registered route throws `UnroutedTopicException`.

## Wiring routes: `OutboundRoutingBuilder`

`AddOutboundRouting(Action<OutboundRoutingBuilder>)` (`Benzene.Clients`) registers one `IMiddlewarePipeline<OutboundContext>` per topic and the `IBenzeneMessageSender` that routes to them:

```csharp
public class OutboundRoutingBuilder
{
    public OutboundRoutingBuilder Route(string topic, Action<IMiddlewarePipelineBuilder<OutboundContext>> configure);
    public IReadOnlyDictionary<string, IMiddlewarePipeline<OutboundContext>> Build();
}
```

`.Route(topic, configure)` builds an ordinary middleware pipeline over `OutboundContext` — the outbound mirror of every inbound transport context in Benzene:

```csharp
public class OutboundContext
{
    public string Topic { get; }                          // the topic being sent on
    public object Request { get; }                         // the request payload
    public IDictionary<string, string> Headers { get; }     // per-call headers, never null
    public object? Response { get; set; }                   // set by transport middleware once the send completes
}
```

Registering the same topic twice throws `DuplicateOutboundRouteException` — each topic gets exactly one route.

## Outbound middleware

Cross-cutting concerns are ordinary `IMiddleware<OutboundContext>`, added to a route the same way you'd add middleware to any other Benzene pipeline:

| Extension | Middleware | Behavior |
|---|---|---|
| `.UseRetry(n)` (`Benzene.Resilience`) | `RetryMiddleware<OutboundContext>` | Retries the whole pipeline beneath it up to `n` times. Pass `shouldRetryContext: ctx => ((IBenzeneResult)ctx.Response).IsServiceUnavailable()` (or similar) to retry on a specific result status, and/or `shouldRetry` to retry specific exceptions. Fully generic — this is the same `RetryMiddleware<TContext>` used elsewhere in Benzene, not something built specifically for outbound clients. |
| `.UseCorrelationId(correlationKey = "correlationId")` (`Benzene.Clients.CorrelationId`) | `CorrelationIdMiddleware` | Stamps the current `ICorrelationId.Get()` value onto `OutboundContext.Headers`. See [Correlation IDs](correlation-ids). |
| `.UseW3CTraceContext()` (`Benzene.Clients.TraceContext`) | `W3CTraceContextMiddleware` | Stamps `Activity.Current`'s W3C `traceparent`/`tracestate` onto `OutboundContext.Headers`, so the receiving service can continue the same distributed trace. See [Monitoring & Diagnostics — W3C Trace Context](monitoring#w3c-trace-context). No-ops (leaves headers unchanged) when there's no ambient `Activity`. Same method name as `Benzene.Diagnostics.UseW3CTraceContext<TContext>()` (the *inbound* trace-context-extraction middleware) — they run in opposite directions, don't confuse the two. |

There's no dedicated headers middleware — `IBenzeneMessageSender.SendAsync`'s per-call `headers` parameter already covers ambient/per-request header state without a decorator.

Put retry outermost so a failed attempt retries the whole pipeline beneath it, including header stamping:

```csharp
services.UsingBenzene(x => x.AddOutboundRouting(routing => routing
    .Route("order:create", pipeline => pipeline
        .UseCorrelationId()
        .UseW3CTraceContext()
        .UseSqs(queueUrl)
        .UseRetry(3))));
```

### Writing a custom middleware

Any `IMiddleware<OutboundContext>` works — no special interface beyond the one every other Benzene middleware implements:

```csharp
public class TenantHeaderMiddleware : IMiddleware<OutboundContext>
{
    private readonly ITenantContext _tenantContext;

    public TenantHeaderMiddleware(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public string Name => nameof(TenantHeaderMiddleware);

    public Task HandleAsync(OutboundContext context, Func<Task> next)
    {
        context.Headers["x-tenant-id"] = _tenantContext.TenantId;
        return next();
    }
}
```

```csharp
public static class TenantHeaderExtensions
{
    public static IMiddlewarePipelineBuilder<OutboundContext> UseTenantHeader(
        this IMiddlewarePipelineBuilder<OutboundContext> app)
    {
        return app.Use(resolver => new TenantHeaderMiddleware(resolver.GetService<ITenantContext>()));
    }
}
```

Now `.UseTenantHeader()` chains alongside the built-in middleware, in whatever order you add them.

## Validating routes at startup: `ValidateOutboundRouting()`

`Benzene.CodeGen.Client`'s generated clients (see below) each emit a sibling `{Service}ServiceClientRouting` static class with a `RequiredTopics` array. Call `ValidateOutboundRouting()` on `IServiceResolver` — typically right after resolving `IBenzeneMessageSender` — to catch a missing route at startup instead of the first time a rarely-hit code path executes it:

```csharp
var resolver = ...; // your app's IServiceResolver
resolver.ValidateOutboundRouting(); // throws MissingOutboundRoutesException if anything's missing
```

It reflects over every loaded assembly for any type with a public static `string[] RequiredTopics` field (not just generated clients — you can add your own `*Routing` class with the same shape), and throws `MissingOutboundRoutesException` listing every required topic with no registered route. Entirely opt-in: an app with no generated clients has nothing to validate, so calling it is optional.

## Per-transport specifics

### SQS

Package: `Benzene.Clients.Aws`.

```csharp
services.UsingBenzene(x => x.AddOutboundRouting(routing => routing
    .Route("order:create", pipeline => pipeline.UseSqs(queueUrl))));
```

`.UseSqs(queueUrl)` converts the route via `OutboundSqsContextConverter`, which serializes `OutboundContext.Request` as the message body and puts every `Headers` entry — plus a `topic` attribute — onto the outgoing `SendMessageRequest.MessageAttributes`. An overload, `.UseSqs(queueUrl, configure)`, lets you customize the inner SQS send pipeline (e.g. resolving `IAmazonSQS` a different way) instead of the default `UseSqsClient()`.

**SQS has no request/response semantics beyond a send acknowledgement**, so a topic routed through `.UseSqs(...)` must be sent via `SendAsync<TRequest, Void>` — any other `TResponse` compiles but throws `InvalidCastException` at runtime, since `OutboundContext.Response` is always boxed as `IBenzeneResult<Void>` for this transport.

### SNS

Package: `Benzene.Clients.Aws`.

```csharp
services.UsingBenzene(x => x.AddOutboundRouting(routing => routing
    .Route("audit:log", pipeline => pipeline.UseSns(topicArn))));
```

Same shape as SQS: `.UseSns(topicArn)` (and the `.UseSns(topicArn, configure)` overload) via `OutboundSnsContextConverter`, forwarding `Headers` onto `PublishRequest.MessageAttributes`. Same `Void`-only constraint as SQS above.

### AWS Lambda, Kafka, EventBridge, gRPC

These transports don't have an `OutboundContext` route extension yet — `.UseAwsLambda(...)` and equivalents for Kafka/EventBridge/gRPC are not yet implemented on the outbound routing pipeline. Until they land, use the transport's `IBenzeneMessageClient` implementation directly (see [Using a transport client directly](#using-a-transport-client-directly) below) rather than through `AddOutboundRouting(...)`.

### HTTP

Package: `Benzene.Client.Http`. HTTP has never had an `IBenzeneMessageClient` implementation or an outbound-routing route extension — see [Using a transport client directly — HTTP](#http-1) below for the lower-level pipeline you compose yourself.

## Generated clients (`Benzene.CodeGen.Client`)

Generated service clients target `IBenzeneMessageSender` directly — no factory resolution per call:

```csharp
public class UserServiceClient : IUserServiceClient
{
    private readonly IBenzeneMessageSender _sender;

    public UserServiceClient(IBenzeneMessageSender sender)
    {
        _sender = sender;
    }

    public Task<IBenzeneResult<Guid?>> CreateUserAsync(CreateUserMessage message, IDictionary<string, string>? headers = null)
    {
        return _sender.SendAsync<CreateUserMessage, Guid?>("user:create", message, headers);
    }

    // ...HealthCheckAsync() the same way, against the "healthcheck" topic
}

public static class UserServiceClientRouting
{
    public static readonly string[] RequiredTopics = { "user:create", "healthcheck" };
}
```

The generated *interface* (`IUserServiceClient`) is unchanged from what you're used to — only the implementation's internals changed. Register `UserServiceClient` for DI as you already do, wire its topics via `AddOutboundRouting(...)`, and optionally call `ValidateOutboundRouting()` (above) using the generated `UserServiceClientRouting.RequiredTopics`.

## Using a transport client directly

Outside outbound routing, every transport still has a standalone `IBenzeneMessageClient` implementation you can resolve and call directly — useful for AWS Lambda/Kafka/EventBridge/gRPC (no route extension yet), or any one-off case where you don't want a topic-keyed route table:

```csharp
public interface IBenzeneMessageClient : IDisposable
{
    Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request);
}
```

`ClientExtensions` provides `string topic, TMessage message` overloads that build the `IBenzeneClientRequest<TMessage>` for you:

```csharp
Task<IBenzeneResult<TResponse>> SendMessageAsync<TMessage, TResponse>(string topic, TMessage message);
Task<IBenzeneResult<TResponse>> SendMessageAsync<TMessage, TResponse>(string topic, TMessage message, IDictionary<string, string> headers);
Task<IBenzeneResult> SendMessageAsync<TRequest>(string topic, TRequest request); // fire-and-forget (TResponse is Void)
```

There's no built-in decorator chain anymore for a directly-resolved `IBenzeneMessageClient` — if you need retry/correlation/trace-context on one of these, either write a small wrapper implementing `IBenzeneMessageClient` yourself (mutate `request.Headers`/retry before delegating to `_inner`, the same shape the deleted decorators used), or prefer routing the call through `AddOutboundRouting(...)` instead, where that behavior is ordinary middleware (above).

### AWS Lambda

Package: `Benzene.Clients.Aws`.

```csharp
services.AddScoped<IBenzeneMessageClient>(x =>
    new AwsLambdaBenzeneMessageClient("orders-service", x.GetService<IAmazonLambda>(), x.GetService<ILogger<AwsLambdaBenzeneMessageClient>>()));
```

`AwsLambdaBenzeneMessageClient` invokes the named Lambda function via `IAmazonLambda`, choosing the invocation type based on `TResponse`:

- `TResponse` is `Void` → fire-and-forget (`InvocationType.Event`).
- Otherwise → request/response (`InvocationType.RequestResponse`), awaiting and mapping the function's response.

It wraps the topic, headers, and serialized message body into its own envelope, `BenzeneMessageClientRequest`, and invokes with that as the payload. An exception during invocation is caught and returned as `BenzeneResult.ServiceUnavailable<TResponse>()`.

### SQS (as a standalone client)

Package: `Benzene.Clients.Aws`. Prefer `.UseSqs(queueUrl)` on an outbound route (above) for new code — this is the lower-level path, useful when you want an `IBenzeneMessageClient` handle rather than routing through `IBenzeneMessageSender`:

```csharp
services.AddSqsMessageClient(queueUrl, pipeline => pipeline.UseSqsClient());
```

`AddSqsMessageClient(queueUrl, action)` builds a `SqsBenzeneMessageClient` around a small internal middleware pipeline. `SqsContextConverter<T>` puts every `IBenzeneClientRequest.Headers` entry onto the outgoing `SendMessageRequest.MessageAttributes` (alongside a `topic` attribute), and the response is mapped from the SQS call's HTTP status code.

### SNS (as a standalone client)

Package: `Benzene.Clients.Aws`. Prefer `.UseSns(topicArn)` on an outbound route (above) for new code.

```csharp
var client = new SnsBenzeneMessageClient(topicArn,
    amazonSimpleNotificationService,
    logger,
    serviceResolver);
```

Like SQS, `SnsBenzeneMessageClient` builds an internal middleware pipeline (`UseSnsClient(...)`), and `SnsContextConverter<T>` forwards `IBenzeneClientRequest.Headers` onto the `PublishRequest.MessageAttributes`. The response is mapped from the publish call's HTTP status code.

### Kafka

Package: `Benzene.Kafka.Core`.

```csharp
var client = new KafkaBenzeneMessageClient(producer, logger, serviceResolver);
```

`KafkaContextConverter<T>` forwards `IBenzeneClientRequest.Headers` onto the outbound `Message.Headers` (UTF-8 encoded, matching Confluent.Kafka's `byte[]`-valued headers). A send is treated as accepted when the resulting `PersistenceStatus` is `Persisted`; anything else maps to `BenzeneResult.UnexpectedError<TResponse>()`.

### EventBridge

Package: `Benzene.Clients.Aws` (`Benzene.Clients.Aws.EventBridge`).

```csharp
var client = new EventBridgeBenzeneMessageClient("com.mycompany.orders",
    amazonEventBridge, logger, serviceResolver, eventBusName: "my-bus");
```

`EventBridgeBenzeneMessageClient` publishes messages as EventBridge events via `PutEvents`: the request's topic becomes the event's `detail-type` (EventBridge's native routing key — this is what a receiving `Benzene.Aws.Lambda.EventBridge` service routes on), the serialized message becomes `detail`, and the client is configured with a fixed `source` and optional event bus name (default bus when omitted).

EventBridge has no native per-message attributes, so headers are embedded into `detail` under the reserved `_benzeneHeaders` key (only when there are headers to send and the payload is a JSON object); the inbound EventBridge binding lifts them back out, so correlation/trace-context propagate end to end when set. Publishing is fire-and-forget: success maps to `Accepted`. `PutEvents` can succeed at the HTTP level while individual entries fail, so the mapper also checks `FailedEntryCount` — a failed entry maps to `ServiceUnavailable` carrying the entry's error code and message.

### gRPC

Package: `Benzene.Grpc.Client`.

```csharp
var routes = new GrpcClientRouteRegistry()
    .Add<HelloRequest, HelloReply>("greet", "/greet.Greeter/SayHello");

var client = new GrpcBenzeneMessageClient(GrpcChannel.ForAddress("https://greeter.internal"), routes,
    grpcMessageAdapter, grpcStatusReverseMapper, logger, serviceResolver);
```

`IGrpcClientRouteRegistry.Add<TRequest,TResponse>(topic, fullMethodName)` registers the RPC's *protobuf wire types* (not necessarily what you pass to `SendMessageAsync<TRequest,TResponse>` — a POCO caller type is bridged onto the wire type by `IGrpcMessageAdapter`, same JSON-bridging rule as the server side) against its full gRPC method path. `AddGrpcClient(routes => routes.Add<...>(...))` is the DI-registration shorthand for `IGrpcMessageAdapter`/`IGrpcStatusReverseMapper`/the route registry itself, if you'd rather resolve those from the container than construct them by hand as above — it still expects a `GrpcChannel` to already be registered separately, the same way the Kafka client above expects an `IProducer<string,string>`.

Unlike the other transports, a non-OK gRPC status doesn't collapse to a single generic failure status: `IGrpcStatusReverseMapper` maps the `StatusCode` back to a `BenzeneResultStatus` (e.g. `NotFound` → `NotFound`, `PermissionDenied` → `Forbidden`), preferring a `benzene-status` trailer verbatim when the far side is itself a Benzene.Grpc service — several distinct Benzene statuses (`Created`, `Accepted`, `Updated`, ...) collapse to the same `StatusCode.OK` on the wire, and the trailer is the only way to recover which one it actually was. An `RpcException` is caught inside `GrpcClientMiddleware` and mapped the same way, rather than propagating out of `SendMessageAsync`.

### HTTP

Package: `Benzene.Client.Http`.

HTTP is the odd one out: there is no `HttpBenzeneMessageClient : IBenzeneMessageClient` shipped today. Instead, `Benzene.Client.Http` gives you the lower-level pipeline building blocks to compose an outbound HTTP call yourself:

```csharp
var pipeline = new MiddlewarePipelineBuilder<IBenzeneClientContext<CreateOrderRequest, OrderCreatedResponse>>(services)
    .UseHttp<CreateOrderRequest, OrderCreatedResponse>("POST", "https://orders.internal/api/orders")
    .Build();

var context = new BenzeneClientContext<CreateOrderRequest, OrderCreatedResponse>(
    new BenzeneClientRequest<CreateOrderRequest>("order:create", request, headers));

await pipeline.HandleAsync(context, serviceResolver);
var result = context.Response;
```

`UseHttp<TRequest, TResponse>(verb, path)` converts the pipeline's context via `HttpContextConverter<TRequest, TResponse>`, which serializes `contextIn.Request.Message` as the JSON body and copies every entry in `contextIn.Request.Headers` onto the real `HttpRequestMessage.Headers` before `HttpClientMiddleware` sends it with the injected `HttpClient`. If you want an `IBenzeneMessageClient` handle for this pipeline, wrap it yourself the same way `SqsBenzeneMessageClient`/`SnsBenzeneMessageClient`/`KafkaBenzeneMessageClient` do internally (build the pipeline once, then translate `IBenzeneClientRequest`/`IBenzeneResult` to and from `IBenzeneClientContext` inside `SendMessageAsync`).

## Header forwarding

Every transport puts `Headers` (from either `OutboundContext.Headers` on the outbound-routing path, or `IBenzeneClientRequest<T>.Headers` on the standalone-client path) onto the real outgoing request:

- **SQS** — copies `Headers` onto `SendMessageRequest.MessageAttributes` (alongside `topic`).
- **SNS** — copies `Headers` onto `PublishRequest.MessageAttributes`.
- **Kafka** — copies `Headers` onto `Message.Headers` (UTF-8 encoded).
- **EventBridge** — embeds `Headers` into the event's `detail` under the reserved `_benzeneHeaders` key (EventBridge has no native per-message attributes); the inbound binding lifts them back out.
- **gRPC** — copies `Headers` onto the outbound `CallOptions.Headers` (a `Metadata`).
- **AWS Lambda** (`AwsLambdaBenzeneMessageClient`) — embeds `Headers` directly into its own `BenzeneMessageClientRequest` envelope, which is what actually gets invoked as the payload.
- **HTTP** — `HttpContextConverter` copies `Headers` onto `HttpRequestMessage.Headers`.

The one exception is the lower-level `UseAwsLambda()`/`LambdaContextConverter` pipeline style (see [The context-converter pipeline](#the-context-converter-pipeline) below): a raw `InvokeRequest` has no header-like concept comparable to HTTP/SQS/SNS/Kafka, so `LambdaContextConverter.CreateRequestAsync` does not forward headers — a middleware like `.UseW3CTraceContext()` would have no effect on a client pipeline built with `UseAwsLambda()` specifically. This doesn't affect `AwsLambdaBenzeneMessageClient`, which is unrelated and already forwards headers as described above.

See [`OutboundHeaderForwardingTest`](../test/Benzene.Core.Test/Clients/OutboundHeaderForwardingTest.cs) for the tests that pin this behavior down per transport (standalone-client path), and `test/Benzene.Core.Test/Clients/Aws/Sqs/OutboundSqsContextConverterTest.cs`/`Aws/Sns/OutboundSnsContextConverterTest.cs` for the outbound-routing path, and [Monitoring & Diagnostics — W3C Trace Context](monitoring#w3c-trace-context) for the same note in the context of trace propagation specifically.

## The context-converter pipeline

Both the outbound-routing path and the standalone-client path above are built on the same lower-level primitive: `IContextConverter<TContextIn, TContextOut>`, which translates between a generic context type and a transport-specific send context (`SqsSendMessageContext`, `SnsSendMessageContext`, `KafkaSendMessageContext`, `HttpSendMessageContext`, `LambdaSendMessageContext`):

```csharp
public interface IContextConverter<TContextIn, TContextOut>
{
    Task<TContextOut> CreateRequestAsync(TContextIn contextIn);
    Task MapResponseAsync(TContextIn contextIn, TContextOut contextOut);
}
```

`.Convert(converter, action)` plugs a converter into an `IMiddlewarePipelineBuilder`, letting you build a custom send pipeline directly out of transport-specific middleware (`UseSqsClient()`, `UseSnsClient()`, `UseKafkaClient()`, `UseGrpcClient()`, `UseHttpClient()`, `UseAwsLambdaClient()`) rather than going through the named-route (`OutboundContext`) or named-client (`IBenzeneMessageClient`) shorthand. Reach for this when you need pipeline-level control (e.g. inserting custom middleware between the conversion and the transport call) that neither of those exposes — the transport-specific shorthand documented above (`.UseSqs(...)`, `.UseSns(...)`, etc.) is the better default for most services. Note `UseGrpc<T>()`'s converter always maps the response to `Void` (matching Kafka's fire-and-forget shape); `GrpcBenzeneMessageClient` above bypasses it precisely to return the real typed response instead.

## See Also

- [Correlation IDs](correlation-ids)
- [Monitoring & Diagnostics — W3C Trace Context](monitoring#w3c-trace-context)
- [Message Handlers](message-handlers)
- [gRPC Setup](getting-started-grpc)
- [Migration Guide: Alpha → 1.0](migration-alpha-to-1.0) — the old `ClientBuilder`-based mechanism this page used to describe, and the full old→new mapping
