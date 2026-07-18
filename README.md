# Benzene

[![Build Status](https://github.com/daniellepelley/Benzene/actions/workflows/build-benzene.yml/badge.svg)](https://github.com/daniellepelley/Benzene/actions)
[![codecov](https://codecov.io/gh/daniellepelley/Benzene/graph/badge.svg)](https://codecov.io/gh/daniellepelley/Benzene)
[![NuGet](https://img.shields.io/nuget/v/Benzene.AspNet.Core.svg)](https://www.nuget.org/packages/Benzene.AspNet.Core/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Benzene is a hexagonal (ports-and-adapters) framework for C# built
around a shared middleware pipeline — write your message handlers and
cross-cutting concerns (logging, correlation IDs, validation, health
checks) once, then run the same service behind AWS Lambda (API
Gateway, SQS, SNS, Kafka, EventBridge), Azure Functions (HTTP, Event
Hubs, Service Bus, Cosmos DB Change Feed, Queue/Blob Storage, Event
Grid, Timer), or ASP.NET Core without rewriting any of it.**

## Why Benzene?

- Write a message handler once, against a topic — swap the transport
  (HTTP, Lambda, SQS, SNS, Kafka...) without touching handler code
- Cross-cutting concerns (correlation IDs, logging, validation,
  health checks) live in composable middleware, not scattered across
  handlers
- New message handlers are discovered automatically by reflection —
  no manual routing tables to maintain
- Multi-cloud by design: the same handlers run on AWS, Azure, or a
  plain ASP.NET Core host
- Honest by design: Benzene abstracts your business logic, never the
  transport or the database — so you keep every cloud-native feature of
  the tools you chose (see the [Capability Matrix](docs/capability-matrix.md))

## Quickstart

Benzene's packages are published as prerelease (`-alpha`) versions until 1.0, so
`--prerelease` is required:

```bash
dotnet add package Benzene.AspNet.Core --prerelease
```

A message handler, mapped to a topic:

```csharp
[Message("hello:world")]
public class HelloWorldMessageHandler : IMessageHandler<HelloWorldMessage, HelloWorldResponse>
{
    public Task<IBenzeneResult<HelloWorldResponse>> HandleAsync(HelloWorldMessage message)
    {
        return Task.FromResult(BenzeneResult.Ok(new HelloWorldResponse { Message = $"Hello {message.Name}" }));
    }
}
```

Wired into an ASP.NET Core host (in `Program.cs`):

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.UsingBenzene(x => x
    .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly));

var app = builder.Build();

app.UseBenzene(benzene => benzene
    .UseHttp(http => http
        .UseMessageHandlers()));

app.Run();
```

See [Getting Started](docs/getting-started.md) for the full five-minute walkthrough (including
the request/response message types omitted above for brevity), and [`examples/`](./examples) for
complete, runnable sample projects covering AWS Lambda, Azure Functions, ASP.NET Core, Kafka, and
more.

## How it fits into hexagonal architecture

Your message handlers are the application core — they depend only on
`Benzene.Abstractions` types (`IMessageHandler<TRequest, TResponse>`,
`IBenzeneResult<T>`) and whatever port interfaces you define for your
own dependencies (databases, other services). Each transport
(`Benzene.AspNet.Core`, `Benzene.Aws.Lambda.*`, `Benzene.Azure.*`) is
an adapter that converts its native request into a message and routes
it to the matching handler via `UseMessageHandlers()`, then converts
the `IBenzeneResult` back into a transport-native response. Middleware
in `Benzene.Core.Middleware` provides the pipeline that every adapter
shares, so correlation IDs, logging, validation, and health checks are
written once and apply everywhere.

## Documentation

Full documentation is available in [`docs/`](./docs), including:

- [Migration Guide (Alpha → 1.0)](docs/migration-alpha-to-1.0.md)
- [Message Handlers](docs/message-handlers.md) and [Handler Results](docs/message-result.md)
- [Middleware](docs/middleware.md) and [Common Middleware](docs/common-middleware.md)
- [Testing Benzene](docs/testing-benzene.md)
- [Health Checks](docs/health-checks.md) and [Monitoring & Diagnostics](docs/monitoring.md)
- [AWS Lambda Setup](docs/getting-started-aws.md)
- [Azure Functions Setup](docs/azure-functions.md)
- [ASP.NET Core Integration](docs/asp-net-core.md)
- [Fluent Validation](docs/fluent-validation.md) and [Data Annotations](docs/data-annotations.md)
- [Terraform Code Generation](docs/terraform.md) and [OpenAPI/AsyncAPI Spec](docs/spec.md)
- [Correlation IDs](docs/correlation-ids.md)

## Installation

```bash
dotnet add package Benzene.Core --prerelease
```

Plus whichever transport and integration packages your service needs
(`Benzene.AspNet.Core`, `Benzene.Aws.Lambda.ApiGateway`,
`Benzene.Azure.Function.Core`, `Benzene.FluentValidation`, etc.) — see
[`docs/`](./docs) for the relevant package per adapter.

Requires .NET 10.

## Contributing

Contributions are welcome — see [CONTRIBUTING.md](./CONTRIBUTING.md) for setup, conventions, and
what to read before sending a change.

## License

MIT — see [LICENSE](./LICENSE) for details.
