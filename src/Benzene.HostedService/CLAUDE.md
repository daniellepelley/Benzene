# Benzene.HostedService

## What this package does
Bridges a Benzene self-hosted worker (`IBenzeneWorker`, from `Benzene.SelfHost`) onto the .NET generic
host's `IHostedService`, so a Benzene worker starts/stops with the host. This is the glue between
`Benzene.SelfHost`'s worker model and `Microsoft.Extensions.Hosting`.

## Key types/interfaces
- `BenzeneHostedServiceAdapter : IHostedService` - wraps an `IBenzeneWorker`; `StartAsync`/`StopAsync`
  delegate straight to the worker's `StartAsync`/`StopAsync` (graceful shutdown is the worker's own
  drain logic — see `Benzene.SelfHost`).
- `HostBuilderExtensions.UseBenzene<TStartUp>(this IHostBuilder)` - runs a platform-neutral
  `BenzeneStartUp` as a hosted worker: builds a `WorkerApplicationBuilder`, runs `ConfigureServices`/
  `Configure`, and registers the resulting worker as a singleton `IHostedService`.
- `BenzeneWorkerExtensions.BuildHostedService(this IBenzeneWorkerBuilder)` - wraps a built worker in a
  `BenzeneHostedServiceAdapter` directly.

## When to use this package
- When processing background messages in ASP.NET Core
- For queue consumers as hosted services
- For scheduled tasks
- For long-running background operations

## Dependencies on other Benzene packages
- **Benzene.SelfHost** - `IBenzeneWorker`, `WorkerApplicationBuilder`, `IBenzeneWorkerBuilder`
- **Benzene.Microsoft.Dependencies** - `MicrosoftBenzeneServiceContainer`/`MicrosoftServiceResolverFactory`, `BenzeneStartUp`
- **Benzene.Core** / **Benzene.Core.Middleware**
- **Microsoft.Extensions.Hosting.Abstractions** - `IHostedService`, `IHostBuilder`

## Important conventions
- Registered as a singleton `IHostedService`, so it starts/stops with the generic host.
- Graceful shutdown is delegated to the wrapped `IBenzeneWorker.StopAsync` (bounded drain).
- Suitable for queue/stream consumers (e.g. `Benzene.Kafka.Core`) and self-hosted HTTP workers.
