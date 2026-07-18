# Client SDK Generation

Because every Benzene handler declares its contract — a topic and typed request/response — Benzene
can generate a strongly-typed C# **client SDK** for a service. Callers get a typed client instead
of hand-assembling messages, and the client stays in sync with the service's handlers.

The generator lives in the `Benzene.CodeGen.Client` package and produces a
`{Service}ServiceClient` class (implementing an `I{Service}ServiceClient` interface) with one
`…Async` method per handler.

## How it works

Generation runs off a service description called an `EventServiceDocument` — the same model behind
the [OpenAPI/AsyncAPI spec](spec.md). You can build that document two ways:

- **From the handler assembly directly** (reflection), at build time.
- **From a running service's [`spec` endpoint](spec.md)**, so you can generate a client from a
  deployed service without referencing its code.

## Generating from a handler assembly

Given handlers in an assembly:

```csharp
[Message("hello:world")]
public class HelloWorldMessageHandler : IMessageHandler<HelloWorldMessage, HelloWorldResponse>
{
    public Task<IBenzeneResult<HelloWorldResponse>> HandleAsync(HelloWorldMessage message)
        => BenzeneResult.Ok(new HelloWorldResponse { Message = $"Hello {message.Name}" }).AsTask();
}
```

Build the service document and run the SDK builder:

```csharp
using Benzene.CodeGen.Client;
using Benzene.Core.MessageHandlers;
using Benzene.Schema.OpenApi.EventService;

// 1. Discover the handlers and turn them into a service document
var definitions = new ReflectionMessageHandlersFinder(typeof(HelloWorldMessageHandler).Assembly)
    .FindDefinitions();
var document = definitions.ToEventServiceDocument();

// 2. Generate the client SDK
var sdkBuilder = new MessageClientSdkBuilder(
    serviceName: "HelloWorld",
    baseNamespace: "Benzene.Examples.Clients");

var codeFiles = sdkBuilder.BuildCodeFiles(document);

// 3. Write the generated files out
foreach (var file in codeFiles)   // each ICodeFile has a Name and Lines
{
    File.WriteAllLines(file.Name, file.Lines);
}
```

This produces `HelloWorldServiceClient.cs` containing a `HelloWorldServiceClient` with a
`HelloWorldAsync(HelloWorldMessage message)` method (plus a `HealthCheckAsync()` and header-aware
overloads).

## Using the generated client

The generated client takes an `IBenzeneMessageSender` (from `Benzene.Clients`) in its constructor
and returns results as `IBenzeneResult<T>` — the same result model your handlers use:

```csharp
public class HelloWorldServiceClient : IHelloWorldServiceClient
{
    public HelloWorldServiceClient(IBenzeneMessageSender sender) { /* generated */ }

    public Task<IBenzeneResult<HelloWorldResponse>> HelloWorldAsync(HelloWorldMessage message) { /* generated */ }
    public Task<IBenzeneResult<HelloWorldResponse>> HelloWorldAsync(HelloWorldMessage message, IDictionary<string, string> headers) { /* generated */ }
}
```

The generator also emits a sibling `HelloWorldServiceClientRouting.RequiredTopics` array, for
`ValidateOutboundRouting()`'s startup check — see [Validating routes at
startup](clients.md#validating-routes-at-startup-validateoutboundrouting).

Configure the underlying transport by routing each of the client's topics via
`AddOutboundRouting(...)` — for example `.UseSqs(...)`/`.UseSns(...)` from `Benzene.Clients.Aws` to
call the service via AWS, or an HTTP transport for calling it over HTTP. The generated client is
transport-agnostic; the outbound route registered for each topic decides how the message is
actually sent.

```csharp
var client = new HelloWorldServiceClient(sender);
var result = await client.HelloWorldAsync(new HelloWorldMessage { Name = "World" });
if (BenzeneResult.IsSuccess(result))
{
    Console.WriteLine(result.Payload.Message);
}
```

## Generating message handler stubs

The same package includes `MessageHandlerBuilder`, which generates handler *stubs* from a service
document — useful for scaffolding a new service from an existing contract:

```csharp
var handlerFiles = new MessageHandlerBuilder("MyService.Handlers").BuildCodeFiles(document);
```

## Generating from a deployed service

To generate a client from a service you don't have the source for, fetch its `EventServiceDocument`
from the running service's [`spec` endpoint](spec.md) (the service must have
[`UseSpec()`](reference/middleware.md#usespecstring-topic--spec) in its pipeline), then feed that
document into `MessageClientSdkBuilder` exactly as above. The
[`Benzene.CodeGen.Cli`](reference/packages.md#code-generation--tooling) tool wraps this flow for
command-line use.

## Further Reading

- [OpenAPI Specification](spec.md) - the `spec` endpoint the document comes from
- [Package Reference](reference/packages.md#code-generation--tooling) - the code-generation packages
- [Package Reference: outbound clients](reference/packages.md#outbound-messaging-clients) - transports the client sends over
- [Message Handlers](message-handlers.md) - the contracts the SDK is generated from
