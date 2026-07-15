# Getting Started

This guide takes you from an empty folder to a running Benzene service in about five
minutes — no cloud account required. You'll build a small HTTP service locally with ASP.NET
Core, then, if you want, take the exact same message handler to AWS Lambda or Azure Functions
without changing a line of it.

If you already know you're deploying to a specific platform, you can jump straight to
[AWS Lambda Setup](getting-started-aws) or [Azure Functions Setup](azure-functions) — but
starting here first is the quickest way to see how Benzene fits together.

## What you'll build

A single endpoint, `GET /hello/{name}`, that returns a JSON greeting. It's deliberately tiny
so the focus stays on the moving parts you'll reuse in every Benzene service: a **message
handler**, a **topic**, and the **middleware pipeline** that connects a transport to your
handler.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Any editor (Visual Studio, Rider, or VS Code)

That's it — the local walkthrough needs nothing else installed.

## The core idea in 60 seconds

Benzene separates *what your service does* from *how it's invoked*:

- A **message handler** contains your logic. It receives a typed request and returns a typed
  response wrapped in a [result](message-result). It knows nothing about HTTP, Lambda, or
  queues.
- Each handler is mapped to a **topic** — a stable string like `hello:world` that identifies
  the operation. Handlers are discovered automatically by reflection, so there's no routing
  table to maintain.
- A **transport** (ASP.NET Core here; AWS Lambda or Azure Functions elsewhere) is wired up in
  a **middleware pipeline** that turns an incoming request into a message, routes it to the
  matching handler by topic, and turns the result back into a transport-native response.

Because only the transport pipeline changes between hosts, the handler you write below runs
unchanged on every platform Benzene supports. See [Message Handlers](message-handlers) and
[Middleware](middleware) for the full picture.

## 1. Create the project

```bash
mkdir HelloBenzene && cd HelloBenzene
dotnet new web -f net10.0
```

`dotnet new web` gives you a minimal ASP.NET Core app — a `Program.cs` and a `.csproj`, with
no controllers or extra scaffolding to clear away.

## 2. Install the Benzene package

Benzene's packages are published as prerelease (`-alpha`) versions until 1.0, so
`--prerelease` is required:

```bash
dotnet add package Benzene.AspNet.Core --prerelease
```

`Benzene.AspNet.Core` brings in the middleware pipeline, the message handler infrastructure,
and the ASP.NET Core HTTP integration transitively — it's the only package you need for this
guide.

## 3. Write a message handler

Create `HelloWorldMessageHandler.cs`. This is where your logic lives — and the only file
you'd carry over verbatim if you later moved to Lambda or Azure Functions:

```csharp
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Results;

namespace HelloBenzene;

[Message("hello:world")]
[HttpEndpoint("GET", "/hello/{name}")]
public class HelloWorldMessageHandler : IMessageHandler<HelloWorldRequest, HelloWorldResponse>
{
    public Task<IBenzeneResult<HelloWorldResponse>> HandleAsync(HelloWorldRequest message)
    {
        var response = new HelloWorldResponse { Message = $"Hello {message.Name}!" };
        return Task.FromResult(BenzeneResult.Ok(response));
    }
}

public class HelloWorldRequest
{
    public string Name { get; set; }
}

public class HelloWorldResponse
{
    public string Message { get; set; }
}
```

Two attributes do the wiring, and both are found by reflection:

- `[Message("hello:world")]` maps the handler to its topic. Every Benzene transport routes by
  topic, so this is the identifier that stays constant across HTTP, Lambda, SQS, and the rest.
- `[HttpEndpoint("GET", "/hello/{name}")]` maps an HTTP method and path onto that same topic.
  The `{name}` segment is bound onto the request's `Name` property.

The return type — `Task<IBenzeneResult<HelloWorldResponse>>` — is the response wrapped in a
[result](message-result), which carries success/failure status alongside the payload.
`BenzeneResult.Ok(...)` is the success case.

## 4. Register and wire up Benzene

Replace the generated `Program.cs` with this:

```csharp
using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.Microsoft.Dependencies;
using HelloBenzene;

var builder = WebApplication.CreateBuilder(args);

// Register Benzene and discover message handlers in this assembly.
builder.Services.UsingBenzene(x => x
    .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly));

var app = builder.Build();

// Add the Benzene HTTP pipeline: turn each request into a message and route it to a handler.
app.UseBenzene(benzene => benzene
    .UseHttp(http => http
        .UseMessageHandlers()));

app.Run();
```

There are only two Benzene calls:

- `UsingBenzene(x => x.AddMessageHandlers(...))` registers Benzene's services and scans the
  given assembly for handlers. Pass any type from the assembly you want scanned —
  `typeof(HelloWorldMessageHandler).Assembly` here.
- `app.UseBenzene(benzene => benzene.UseHttp(http => http.UseMessageHandlers()))` inserts
  Benzene into the ASP.NET Core request pipeline. `UseHttp` registers the HTTP-specific
  services for you; `UseMessageHandlers()` is the step that routes a matched request to its
  handler.

The Benzene middleware only responds to requests that match one of your `[HttpEndpoint]`
routes — anything it doesn't recognise falls through to the rest of the ASP.NET Core pipeline,
so it coexists cleanly with controllers, static files, or health-check endpoints if you add
them later.

## 5. Run it

```bash
dotnet run
```

The console prints the local URL (typically `http://localhost:5000`). In another terminal:

```bash
curl http://localhost:5000/hello/world
```

```json
{"message":"Hello world!"}
```

That's a complete Benzene service. The request arrived over HTTP, Benzene mapped
`GET /hello/{name}` to the `hello:world` topic, bound `world` onto `HelloWorldRequest.Name`,
invoked your handler, and serialised the result back as JSON.

## What just happened

```
GET /hello/world
      │
      ▼
[HttpEndpoint] route match  ──►  topic "hello:world"
      │
      ▼
HelloWorldMessageHandler.HandleAsync(request)
      │
      ▼
BenzeneResult.Ok(response)  ──►  200  {"message":"Hello world!"}
```

The handler in the middle never touched `HttpContext`. Swap the transport pipeline in
`Program.cs` for an AWS Lambda or Azure Functions one and the same handler runs there — that's
the portability Benzene's hexagonal design buys you.

## Next steps

Now that you have a service running, layer on the cross-cutting concerns and platforms you
need — each is a small, self-contained addition:

- **Add validation** — reject bad requests before they reach your handler with
  [FluentValidation](fluent-validation) or [Data Annotations](data-annotations).
- **Add correlation & logging** — trace requests end-to-end with
  [Correlation IDs](correlation-ids) and [Monitoring & Diagnostics](monitoring).
- **Understand the pipeline** — see what else you can compose in with
  [Middleware](middleware) and [Common Middleware](common-middleware).
- **Test your handlers** — [Testing Benzene](testing-benzene) shows how to test handlers in
  isolation and pipelines end-to-end.
- **Deploy to the cloud** — take the same handler to
  [AWS Lambda](getting-started-aws) (API Gateway, SQS, SNS, Kafka, S3) or
  [Azure Functions](azure-functions).
- **Go deeper with recipes** — the [Cookbooks](cookbooks/README) cover real-world scenarios
  like retries, fan-out, caching, and distributed tracing.

For complete, runnable projects covering every transport, see the
[`examples/`](../examples) folder in the repository.
