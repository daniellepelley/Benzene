# Clients

Benzene clients let one Benzene service call another — over AWS Lambda, SQS, SNS, Kafka, or HTTP — through a single `IBenzeneMessageClient` interface, decorated with cross-cutting behavior like correlation IDs, W3C trace propagation, and retries.

## Overview

A Benzene service is a set of message handlers reachable by topic (see [Message Handlers](message-handlers)). When one service needs to call another, it does so through an `IBenzeneMessageClient` — a small, transport-agnostic abstraction:

```csharp
public interface IBenzeneMessageClient : IDisposable
{
    Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request);
}
```

The same interface is implemented for every supported transport (`AwsLambdaBenzeneMessageClient`, `SqsBenzeneMessageClient`, `SnsBenzeneMessageClient`, `KafkaBenzeneMessageClient`), so calling code never needs to know or care which transport sits underneath — you send a request to a topic and get back an `IBenzeneResult<TResponse>`, the same [message result](message-result) type your own handlers return.

Cross-cutting behavior — correlation IDs, distributed trace propagation, fixed headers, retries — is added by wrapping one `IBenzeneMessageClient` around another (the decorator pattern), composed with a small builder (`ClientBuilder`) rather than baked into each transport implementation.

## Installation

Add the core client abstractions, plus whichever transport package(s) you need:

| Package | What it adds |
|---|---|
| `Benzene.Clients` | `IBenzeneMessageClient`, `IBenzeneClientRequest<T>`, `ClientBuilder`, the built-in decorators (correlation ID, W3C trace context, headers, retry), and client registration/routing (`AddBenzeneMessageClients`, `IBenzeneMessageClientFactory`). Pulled in transitively by every transport package below. |
| `Benzene.Clients.Aws` | `AwsLambdaBenzeneMessageClient`, `SqsBenzeneMessageClient`, `SnsBenzeneMessageClient` (AWS Lambda/SQS/SNS transports), plus health checks for them. |
| `Benzene.Kafka.Core` | `KafkaBenzeneMessageClient` (Kafka transport). |
| `Benzene.Client.Http` | `HttpContextConverter`/`HttpClientMiddleware` — the lower-level pipeline building blocks for sending over HTTP (see [HTTP](#http) below). |

## Basic Usage

Resolve an `IBenzeneMessageClient` (directly, or via `IBenzeneMessageClientFactory` — see [Registering and routing clients](#registering-and-routing-clients)) and call `SendMessageAsync`:

```csharp
public class OrderClient
{
    private readonly IBenzeneMessageClient _client;

    public OrderClient(IBenzeneMessageClient client)
    {
        _client = client;
    }

    public Task<IBenzeneResult<OrderCreatedResponse>> CreateOrderAsync(CreateOrderRequest request)
    {
        return _client.SendMessageAsync<CreateOrderRequest, OrderCreatedResponse>("order:create", request);
    }
}
```

`ClientExtensions` provides the `string topic, TMessage message` overloads used above; they build an `IBenzeneClientRequest<TMessage>` (a `BenzeneClientRequest<TMessage>`) for you:

```csharp
// Request/response, no extra headers
Task<IBenzeneResult<TResponse>> SendMessageAsync<TMessage, TResponse>(string topic, TMessage message);

// Request/response, with headers
Task<IBenzeneResult<TResponse>> SendMessageAsync<TMessage, TResponse>(string topic, TMessage message, IDictionary<string, string> headers);

// Fire-and-forget (TResponse is Void)
Task<IBenzeneResult> SendMessageAsync<TRequest>(string topic, TRequest request);
```

Passing headers explicitly:

```csharp
await _client.SendMessageAsync<CreateOrderRequest, OrderCreatedResponse>(
    "order:create",
    request,
    new Dictionary<string, string> { ["x-tenant-id"] = tenantId });
```

Or construct the request yourself and call the interface method directly — useful when you already have an `IBenzeneClientRequest<T>` from elsewhere in the pipeline:

```csharp
var clientRequest = new BenzeneClientRequest<CreateOrderRequest>("order:create", request, headers);
var result = await _client.SendMessageAsync<CreateOrderRequest, OrderCreatedResponse>(clientRequest);
```

## The decorator pattern: `ClientBuilder`

Cross-cutting behavior is added by wrapping one `IBenzeneMessageClient` in another. Each decorator implements the same interface, holds an `_inner` client, and does its work before (or after, for retry) delegating to it — for example `HeaderBenzeneMessageClient` stamps a fixed header onto every request:

```csharp
public class HeaderBenzeneMessageClient : IBenzeneMessageClient
{
    private readonly IBenzeneMessageClient _inner;
    // ...
    public Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
    {
        var matchingHeaders = PopulateHeaders(request.Headers);
        return _inner.SendMessageAsync<TRequest, TResponse>(new BenzeneClientRequest<TRequest>(request.Topic, request.Message, matchingHeaders));
    }
    // ...
}
```

`ClientBuilder` composes a chain of these decorators around a base (transport) client:

```csharp
public class ClientBuilder
{
    public ClientBuilder(Func<IServiceResolver, IBenzeneMessageClient> builder);
    public ClientBuilder WithDependencyWrapper(IDependencyWrapper<IBenzeneMessageClient> dependencyWrapper);
    public IBenzeneMessageClient Build(IServiceResolver serviceResolver);
}
```

`IDependencyWrapper<T>` is the interface each decorator's wrapper implements:

```csharp
public interface IDependencyWrapper<T>
{
    T Wrap(IServiceResolver serviceResolver, T source);
}
```

`Build(serviceResolver)` applies every registered wrapper, in the order they were added, on top of the base client (`DependencyWrapperFactory<T>.Create` does this via `Aggregate`). That means **the first `.With…()` call wraps the base transport client directly, and each later call wraps around the previous result** — the last one you chain ends up as the outermost layer of the returned client. In practice, put `.WithRetry(n)` last so a failed attempt retries the whole pipeline beneath it (header stamping included), matching how the existing tests chain them: `.WithCorrelationId().WithRetry(2)`.

### Built-in decorators

| Extension | Wraps with | Behavior |
|---|---|---|
| `.WithCorrelationId()` | `CorrelationIdBenzeneMessageClient` | Stamps the current `ICorrelationId.Get()` value onto a header (default key `correlationId`). See [Correlation IDs](correlation-ids) — this style is now considered legacy in favor of W3C trace context below. |
| `.WithW3CTraceContext()` | `TraceContextBenzeneMessageClient` | Stamps `Activity.Current`'s W3C `traceparent`/`tracestate` onto outgoing headers, so the receiving service can continue the same distributed trace. See [Monitoring & Diagnostics — W3C Trace Context](monitoring#w3c-trace-context). No-ops (leaves headers unchanged) when there's no ambient `Activity`. |
| `.WithRetry(n)` | `RetryBenzeneMessageClient` | Retries up to `n` times while the inner client's result `IsServiceUnavailable()`; returns `BenzeneResult.ServiceUnavailable<TResponse>()` if every attempt fails. |

Two more decorators exist without a `ClientBuilder` extension shortcut — construct them directly (they're most commonly used from `Extensions.AddLambdaClients`, see [AWS Lambda](#aws-lambda)):

- `HeaderBenzeneMessageClient(inner, key, value)` — stamps one fixed key/value header (e.g. a `sender` name) onto every request.
- `HeadersBenzeneMessageClient(inner, IClientHeaders)` — stamps every header currently held by a scoped `IClientHeaders` (`ClientHeaders`, a simple `Set`/`Get` dictionary you can populate per-request from DI) onto every request.

### Worked example

Chaining correlation ID, W3C trace context, and retry when registering a named AWS Lambda client:

```csharp
services.UsingBenzene(x => x.AddBenzeneMessageClients(clients => clients
    .CreateAwsLambdaBenzeneMessageClient("orders-service",
        map => map.ForService("orders"),
        client => client
            .WithCorrelationId()
            .WithW3CTraceContext()
            .WithRetry(3))));
```

The resulting client, from outermost to innermost, is:

```
RetryBenzeneMessageClient(3)
  -> TraceContextBenzeneMessageClient
    -> CorrelationIdBenzeneMessageClient
      -> AwsLambdaBenzeneMessageClient("orders-service")
```

Each retry re-runs the trace-context and correlation-ID stamping (both idempotent) before re-invoking the Lambda function.

### Writing a custom decorator

Follow the same three-piece shape as `CorrelationIdBenzeneMessageClient`/`CorrelationIdBenzeneMessageClientWrapper`/`Extensions.WithCorrelationId()`:

```csharp
public class TenantHeaderBenzeneMessageClient : IBenzeneMessageClient
{
    private readonly IBenzeneMessageClient _inner;
    private readonly ITenantContext _tenantContext;

    public TenantHeaderBenzeneMessageClient(IBenzeneMessageClient inner, ITenantContext tenantContext)
    {
        _inner = inner;
        _tenantContext = tenantContext;
    }

    public void Dispose() => _inner.Dispose();

    public Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
    {
        request.Headers["x-tenant-id"] = _tenantContext.TenantId;
        return _inner.SendMessageAsync<TRequest, TResponse>(request);
    }
}

public class TenantHeaderBenzeneMessageClientWrapper : IDependencyWrapper<IBenzeneMessageClient>
{
    public IBenzeneMessageClient Wrap(IServiceResolver serviceResolver, IBenzeneMessageClient benzeneMessageClient)
    {
        return new TenantHeaderBenzeneMessageClient(benzeneMessageClient, serviceResolver.Resolve<ITenantContext>());
    }
}

public static class TenantHeaderExtensions
{
    public static ClientBuilder WithTenantHeader(this ClientBuilder source)
    {
        return source.WithDependencyWrapper(new TenantHeaderBenzeneMessageClientWrapper());
    }
}
```

Now `.WithTenantHeader()` chains alongside the built-in decorators.

## Registering and routing clients

> **A newer, topic-keyed mechanism is being staged in as a replacement** —
> `IBenzeneMessageSender`/`AddOutboundRouting(...)` (`Benzene.Clients`). Everything below still
> works (nothing here is deprecated-and-broken), but new code, and `Benzene.CodeGen.Client`'s
> generated clients, should prefer the new mechanism — see `src/Benzene.Clients/CLAUDE.md` and
> `work/benzene-clients-redesign-plan.md`. `Benzene.Clients.Aws`'s transport wiring (`.UseSqs(...)`
> etc. below) hasn't migrated onto it yet.

`AddBenzeneMessageClients(Action<ClientsBuilder>)` (from `Benzene.Clients`, extended per-transport by `Benzene.Clients.Aws`/`Benzene.Kafka.Core`) registers an `IBenzeneMessageClientFactory` — resolvable via DI — built from one or more `ClientMapping`s:

```csharp
public interface IBenzeneMessageClientFactory
{
    IBenzeneMessageClient Create();               // first registered client
    IBenzeneMessageClient Create(string service, string topic); // routed lookup
}
```

Each registered client is associated with one or more `(service, topic)` keys via a `ClientMappingBuilder`, exposed through the per-transport `Create*BenzeneMessageClient` extensions as a `map` callback:

```csharp
services.UsingBenzene(x => x.AddBenzeneMessageClients(clients => clients
    .CreateAwsLambdaBenzeneMessageClient("lambda2", map => map.ForService("lambda1"))
    .CreateAwsLambdaBenzeneMessageClient("sns", map => map.ForTopic("topic1"))
    .CreateAwsLambdaBenzeneMessageClient("lambdai", map => map.ForServiceAndTopic("lambda3", "topic2"))));
```

- `.ForService(params string[] services)` — matches any topic for these service name(s).
- `.ForTopic(params string[] topics)` — matches this topic for any (or unspecified) service.
- `.ForServiceAndTopic(string service, string topic)` — matches only this exact pair.
- `.ForTopicAndService(string topic, params string[] services)` — the same pair, expressed topic-first for multiple services.

`factory.Create(service, topic)` looks for the most specific match first (exact service+topic), then falls back to a service-only or topic-only mapping; it throws `InvalidOperationException` if nothing matches. Registering the same `(service, topic)` pair against two different clients throws `ArgumentException` immediately, at startup registration time.

For a single client with no routing (`AddBenzeneMessageClient(Action<SingleClientsBuilder>)`), or to wire a transport with no dedicated `Create*BenzeneMessageClient` sugar (SNS and Kafka — see below), register directly with `ClientsBuilder.WithMessageClient(service, Func<IServiceResolver, IBenzeneMessageClient> builder)`.

`IClientMessageRouter`/`ClientMessageSender<TRequest, TResponse>` build on top of this to give a typed `IMessageSender<TRequest, TResponse>` per request type — `ClientMessageSender` looks up the topic via `IGetTopic` and the client via `IClientMessageRouter.GetClient<TRequest>()`, so generated/typed request senders don't need to know about topics or routing at all.

## Per-transport specifics

### AWS Lambda

Package: `Benzene.Clients.Aws`.

```csharp
services.UsingBenzene(x => x.AddBenzeneMessageClients(clients => clients
    .CreateAwsLambdaBenzeneMessageClient("orders-service",
        map => map.ForService("orders"),
        client => client.WithCorrelationId().WithW3CTraceContext().WithRetry(3))));
```

`AwsLambdaBenzeneMessageClient` invokes the named Lambda function via `IAmazonLambda`, choosing the invocation type based on `TResponse`:

- `TResponse` is `Void` → fire-and-forget (`InvocationType.Event`).
- Otherwise → request/response (`InvocationType.RequestResponse`), awaiting and mapping the function's response.

It wraps the topic, headers, and serialized message body into its own envelope, `BenzeneMessageClientRequest`, and invokes with that as the payload:

```csharp
public class BenzeneMessageClientRequest
{
    public string Topic { get; }
    public IDictionary<string, string> Headers { get; }
    public string Message { get; }
}
```

An exception during invocation is caught and returned as `BenzeneResult.ServiceUnavailable<TResponse>()`.

For a single target Lambda function with a fixed `sender` header and retry, `AddLambdaClients(sender)` is a one-call convenience registration (adds `IClientHeaders`, retry, the `sender` header, and an `IBenzeneMessageClientFactory` that always targets that function regardless of service/topic):

```csharp
services.UsingBenzene(x => x.AddLambdaClients(sender: "orders-service"));
```

### SQS

Package: `Benzene.Clients.Aws`.

```csharp
services.UsingBenzene(x => x.AddBenzeneMessageClients(clients => clients
    .CreateSqsBenzeneMessageClient("orders-queue", queueUrl, new NullServiceResolver(),
        client => client.WithCorrelationId().WithW3CTraceContext().WithRetry(3))));
```

`CreateSqsBenzeneMessageClient(name, queueUrl, serviceResolver, action)` builds a `SqsBenzeneMessageClient` around a small internal middleware pipeline (`UseSqsClient(...)`). The `serviceResolver` parameter is used only to run that internal pipeline (built against a `NullBenzeneServiceContainer`), not to resolve your app's own DI services — passing `new NullServiceResolver()` (from `Benzene.Core.Middleware`), as the existing tests do, is normal unless you've supplied a custom pipeline that needs DI-resolved middleware. An unnamed/default-client overload (`CreateSqsBenzeneMessageClient(queueUrl, serviceResolver, action)`, no `name`) is also available.

Internally, `SqsContextConverter<T>` puts every `IBenzeneClientRequest.Headers` entry onto the outgoing `SendMessageRequest.MessageAttributes` (alongside a `topic` attribute), and the response is mapped from the SQS call's HTTP status code.

### SNS

Package: `Benzene.Clients.Aws`.

There's no `CreateSnsBenzeneMessageClient` sugar extension yet — wire `SnsBenzeneMessageClient` with the same `ClientBuilder`/`ClientsBuilder.WithMessageClient` building blocks the Lambda/SQS extensions use internally:

```csharp
var clientBuilder = new ClientBuilder(resolver =>
    new SnsBenzeneMessageClient(topicArn,
        resolver.GetService<IAmazonSimpleNotificationService>(),
        resolver.GetService<ILogger<SnsBenzeneMessageClient>>(),
        new NullServiceResolver()));

clientBuilder.WithCorrelationId().WithW3CTraceContext();

services.UsingBenzene(x => x.AddBenzeneMessageClients(clients => clients
    .WithMessageClient("notifications", clientBuilder.Build)));
```

Like SQS, `SnsBenzeneMessageClient` builds an internal middleware pipeline (`UseSnsClient(...)`), and `SnsContextConverter<T>` forwards `IBenzeneClientRequest.Headers` onto the `PublishRequest.MessageAttributes`. The response is mapped from the publish call's HTTP status code.

### Kafka

Package: `Benzene.Kafka.Core`.

Like SNS, there's no `ClientsBuilder` sugar extension for Kafka yet — wire `KafkaBenzeneMessageClient` the same way:

```csharp
var clientBuilder = new ClientBuilder(resolver =>
    new KafkaBenzeneMessageClient(resolver.GetService<IProducer<string, string>>(),
        resolver.GetService<ILogger<KafkaBenzeneMessageClient>>(),
        new NullServiceResolver()));

clientBuilder.WithCorrelationId().WithW3CTraceContext();

services.UsingBenzene(x => x.AddBenzeneMessageClients(clients => clients
    .WithMessageClient("orders-events", clientBuilder.Build)));
```

`KafkaContextConverter<T>` forwards `IBenzeneClientRequest.Headers` onto the outbound `Message.Headers` (UTF-8 encoded, matching Confluent.Kafka's `byte[]`-valued headers). A send is treated as accepted when the resulting `PersistenceStatus` is `Persisted`; anything else maps to `BenzeneResult.UnexpectedError<TResponse>()`.

### EventBridge

Package: `Benzene.Clients.Aws` (`Benzene.Clients.Aws.EventBridge`).

`EventBridgeBenzeneMessageClient` publishes messages as EventBridge events via `PutEvents`: the request's topic becomes the event's `detail-type` (EventBridge's native routing key — this is what a receiving `Benzene.Aws.Lambda.EventBridge` service routes on), the serialized message becomes `detail`, and the client is configured with a fixed `source` and optional event bus name (default bus when omitted). Like Kafka and SNS, there's no `ClientsBuilder` sugar extension yet — wire it with `ClientBuilder`:

```csharp
var clientBuilder = new ClientBuilder(resolver =>
    new EventBridgeBenzeneMessageClient("com.mycompany.orders",
        resolver.GetService<IAmazonEventBridge>(),
        resolver.GetService<ILogger<EventBridgeBenzeneMessageClient>>(),
        new NullServiceResolver(),
        eventBusName: "my-bus"));

clientBuilder.WithCorrelationId().WithW3CTraceContext();

services.UsingBenzene(x => x.AddBenzeneMessageClients(clients => clients
    .WithMessageClient("orders-events", clientBuilder.Build)));
```

EventBridge has no native per-message attributes, so headers are embedded into `detail` under the reserved `_benzeneHeaders` key (only when there are headers to send and the payload is a JSON object); the inbound EventBridge binding lifts them back out, so correlation/trace decorators propagate end to end. Publishing is fire-and-forget: success maps to `Accepted`. `PutEvents` can succeed at the HTTP level while individual entries fail, so the mapper also checks `FailedEntryCount` — a failed entry maps to `ServiceUnavailable` carrying the entry's error code and message.

### gRPC

Package: `Benzene.Grpc.Client`. Like Kafka and SNS, there's no `ClientsBuilder` sugar extension yet — wire `GrpcBenzeneMessageClient` with `ClientBuilder` directly, over a `GrpcChannel` the application owns:

```csharp
var routes = new GrpcClientRouteRegistry()
    .Add<HelloRequest, HelloReply>("greet", "/greet.Greeter/SayHello");

var clientBuilder = new ClientBuilder(resolver =>
    new GrpcBenzeneMessageClient(GrpcChannel.ForAddress("https://greeter.internal"), routes,
        resolver.GetService<IGrpcMessageAdapter>(),
        resolver.GetService<IGrpcStatusReverseMapper>(),
        resolver.GetService<ILogger<GrpcBenzeneMessageClient>>(),
        new NullServiceResolver()));

clientBuilder.WithCorrelationId().WithW3CTraceContext();

services.UsingBenzene(x => x.AddBenzeneMessageClients(clients => clients
    .WithMessageClient("greet", clientBuilder.Build)));
```

`IGrpcClientRouteRegistry.Add<TRequest,TResponse>(topic, fullMethodName)` registers the RPC's
*protobuf wire types* (not necessarily what you pass to `SendMessageAsync<TRequest,TResponse>` — a
POCO caller type is bridged onto the wire type by `IGrpcMessageAdapter`, same JSON-bridging rule as
the server side) against its full gRPC method path. `AddGrpcClient(routes => routes.Add<...>(...))`
is the DI-registration shorthand for `IGrpcMessageAdapter`/`IGrpcStatusReverseMapper`/the route
registry itself, if you'd rather resolve those from the container than construct them by hand as
above — it still expects a `GrpcChannel` to already be registered separately, the same way the
Kafka client above expects an `IProducer<string,string>`.

Unlike the other transports, a non-OK gRPC status doesn't collapse to a single generic failure
status: `IGrpcStatusReverseMapper` maps the `StatusCode` back to a `BenzeneResultStatus` (e.g.
`NotFound` → `NotFound`, `PermissionDenied` → `Forbidden`), preferring a `benzene-status` trailer
verbatim when the far side is itself a Benzene.Grpc service — several distinct Benzene statuses
(`Created`, `Accepted`, `Updated`, ...) collapse to the same `StatusCode.OK` on the wire, and the
trailer is the only way to recover which one it actually was. An `RpcException` is caught inside
`GrpcClientMiddleware` and mapped the same way, rather than propagating out of `SendMessageAsync`.

### HTTP

Package: `Benzene.Client.Http`.

HTTP is the odd one out: there is no `HttpBenzeneMessageClient : IBenzeneMessageClient` shipped today, so the `ClientBuilder` decorator chain (`WithCorrelationId()`/`WithW3CTraceContext()`/`WithRetry()`) doesn't attach to it directly. Instead, `Benzene.Client.Http` gives you the lower-level pipeline building blocks to compose an outbound HTTP call yourself:

```csharp
var pipeline = new MiddlewarePipelineBuilder<IBenzeneClientContext<CreateOrderRequest, OrderCreatedResponse>>(services)
    .UseHttp<CreateOrderRequest, OrderCreatedResponse>("POST", "https://orders.internal/api/orders")
    .Build();

var context = new BenzeneClientContext<CreateOrderRequest, OrderCreatedResponse>(
    new BenzeneClientRequest<CreateOrderRequest>("order:create", request, headers));

await pipeline.HandleAsync(context, serviceResolver);
var result = context.Response;
```

`UseHttp<TRequest, TResponse>(verb, path)` converts the pipeline's context via `HttpContextConverter<TRequest, TResponse>`, which serializes `contextIn.Request.Message` as the JSON body and — importantly — copies every entry in `contextIn.Request.Headers` onto the real `HttpRequestMessage.Headers` before `HttpClientMiddleware` sends it with the injected `HttpClient`. So if you populate `headers` yourself (e.g. from `ICorrelationId`/`Activity.Current` before constructing the request, mirroring what `WithCorrelationId()`/`WithW3CTraceContext()` do for the other transports), they do reach the wire — see [Header forwarding](#header-forwarding) below.

If you want the same `ClientBuilder` decorator experience for HTTP as for the other transports, wrap this pipeline in your own small `IBenzeneMessageClient`, the same shape `SqsBenzeneMessageClient`/`SnsBenzeneMessageClient`/`KafkaBenzeneMessageClient` use internally (build the pipeline once, then translate `IBenzeneClientRequest`/`IBenzeneResult` to and from `IBenzeneClientContext` inside `SendMessageAsync`).

## Header forwarding

Every built-in decorator (`WithCorrelationId()`, `WithW3CTraceContext()`, `HeaderBenzeneMessageClient`, `HeadersBenzeneMessageClient`) works purely at the `IBenzeneClientRequest<T>.Headers` level — it mutates that dictionary and passes the request down the chain. What matters is whether the transport at the bottom of the chain actually puts those headers on the real outgoing request. As of this release, it does for every transport that ships a full `IBenzeneMessageClient`:

- **HTTP** — `HttpContextConverter` copies `Headers` onto `HttpRequestMessage.Headers`.
- **SQS** — `SqsContextConverter` copies `Headers` onto `SendMessageRequest.MessageAttributes` (alongside `topic`).
- **SNS** — `SnsContextConverter` copies `Headers` onto `PublishRequest.MessageAttributes`.
- **Kafka** — `KafkaContextConverter` copies `Headers` onto `Message.Headers` (UTF-8 encoded).
- **EventBridge** — `EventBridgeContextConverter` embeds `Headers` into the event's `detail` under the reserved `_benzeneHeaders` key (EventBridge has no native per-message attributes); the inbound binding lifts them back out.
- **gRPC** — `GrpcClientRoute` copies `Headers` onto the outbound `CallOptions.Headers` (a `Metadata`).
- **AWS Lambda** (`AwsLambdaBenzeneMessageClient`) — embeds `Headers` directly into its own `BenzeneMessageClientRequest` envelope, which is what actually gets invoked as the payload.

The one exception is the lower-level `UseAwsLambda()`/`LambdaContextConverter` pipeline style (see [The context-converter pipeline](#the-context-converter-pipeline) below): a raw `InvokeRequest` has no header-like concept comparable to HTTP/SQS/SNS/Kafka, so `LambdaContextConverter.CreateRequestAsync` does not forward `Headers` — a decorator like `WithW3CTraceContext()` has no effect on a client pipeline built with `UseAwsLambda()` specifically. This doesn't affect `AwsLambdaBenzeneMessageClient`/`CreateAwsLambdaBenzeneMessageClient()`, which is unrelated and already forwards headers as described above.

See [`OutboundHeaderForwardingTest`](../test/Benzene.Core.Test/Clients/OutboundHeaderForwardingTest.cs) for the tests that pin this behavior down per transport, and [Monitoring & Diagnostics — W3C Trace Context](monitoring#w3c-trace-context) for the same note in the context of trace propagation specifically.

## The context-converter pipeline

Everything above — `SqsBenzeneMessageClient`, `SnsBenzeneMessageClient`, `KafkaBenzeneMessageClient`, and the HTTP example above — is itself built on a more general, lower-level primitive: `IContextConverter<TContextIn, TContextOut>`, which translates between a generic `IBenzeneClientContext<TRequest, TResponse>` and a transport-specific send context (`SqsSendMessageContext`, `SnsSendMessageContext`, `KafkaSendMessageContext`, `HttpSendMessageContext`, `LambdaSendMessageContext`):

```csharp
public interface IContextConverter<TContextIn, TContextOut>
{
    Task<TContextOut> CreateRequestAsync(TContextIn contextIn);
    Task MapResponseAsync(TContextIn contextIn, TContextOut contextOut);
}
```

`.Convert(converter, action)` (or the transport-specific shorthand — `UseSqs<T>(queueUrl)`, `UseSns<T>(topicArn)`, `UseKafka<T>()`, `UseGrpc<T>()`, `UseHttp<TRequest,TResponse>(verb, path)`, `UseAwsLambda<T>()`) plugs a converter into an `IMiddlewarePipelineBuilder`, letting you build a custom send pipeline directly out of transport-specific middleware (`UseSqsClient()`, `UseSnsClient()`, `UseKafkaClient()`, `UseGrpcClient()`, `UseHttpClient()`, `UseAwsLambdaClient()`) rather than going through a named, decorator-wrapped `IBenzeneMessageClient`. Reach for this when you need pipeline-level control (e.g. inserting custom middleware between the conversion and the transport call) that the `ClientBuilder` decorator chain doesn't expose — the named-client pattern documented above is the better default for most services. Note `UseGrpc<T>()`'s converter always maps the response to `Void` (matching Kafka's fire-and-forget shape); `GrpcBenzeneMessageClient` above bypasses it precisely to return the real typed response instead.

## See Also

- [Correlation IDs](correlation-ids)
- [Monitoring & Diagnostics — W3C Trace Context](monitoring#w3c-trace-context)
- [Message Handlers](message-handlers)
- [gRPC Setup](getting-started-grpc)
