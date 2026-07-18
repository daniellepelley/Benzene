# Getting Started: Benzene on Cloudflare Containers

Cloudflare Workers has no native .NET runtime, so a Benzene app can't run *inside* a Worker the
way it runs in AWS Lambda or Azure Functions. [Cloudflare Containers](https://developers.cloudflare.com/containers/)
is the supported path instead: a thin Worker proxies HTTP traffic to a full Docker container
running any language runtime, including .NET. Because a container just runs a normal
Kestrel/ASP.NET Core process, this needs **no Cloudflare-specific Benzene package** — the same
`Benzene.AspNet.Core` integration used for IIS, AKS, or Elastic Beanstalk works unchanged. This
guide starts from an empty folder and ends with a containerized Benzene app fronted by a
Cloudflare Worker.

See [`examples/Cloudflare`](../examples/Cloudflare) for the complete runnable version of
everything below.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) to build the container image
- A Cloudflare account with [Wrangler](https://developers.cloudflare.com/workers/wrangler/) and
  Containers enabled, if you want to deploy

## 1. Create the project

```bash
dotnet new web -f net10.0 -o MyApi
cd MyApi
dotnet add package Benzene.AspNet.Core --prerelease
```

## 2. Define a message handler

Business logic lives in message handlers, not in a controller action — this keeps it testable
and portable across hosts. See [Message Handlers](message-handlers.md) for the full picture; the
minimal shape is:

```csharp
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Results;
using Benzene.Core.MessageHandlers;
using Benzene.Http;
using Benzene.Results;

[Message("hello:world")]
[HttpEndpoint("GET", "/hello/{name}")]
public class HelloWorldMessageHandler : IMessageHandler<HelloWorldRequest, HelloWorldResponse>
{
    public Task<IBenzeneResult<HelloWorldResponse>> HandleAsync(HelloWorldRequest message)
    {
        return Task.FromResult(BenzeneResult.Ok(new HelloWorldResponse { Message = $"Hello {message.Name}!" }));
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

`[Message]` maps the handler to a topic; `[HttpEndpoint]` maps an HTTP method and path to that
same topic. Both attributes are discovered by reflection, so there is nothing further to
register per-handler.

## 3. Define your StartUp

`BenzeneStartUp` (from `Benzene.Microsoft.Dependencies`, referenced transitively) is the
platform-neutral application definition shared by every Benzene host. Configure the HTTP
pipeline via `UseHttp`, and register a liveness check — Cloudflare Containers health-checks the
container over HTTP, so this is load bearing, not decorative (see
[Kubernetes Health Checks](kubernetes-health-checks.md) for the full liveness/readiness pattern
this reuses):

```csharp
using Benzene.Abstractions.Hosting;
using Benzene.AspNet.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandlers.DI;
using Benzene.HealthChecks;
using Benzene.Http;
using Benzene.Http.Routing;
using Benzene.Microsoft.Dependencies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class StartUp : BenzeneStartUp
{
    public override IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .Build();
    }

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.UsingBenzene(x => x
            .AddMessageHandlers(typeof(HelloWorldMessageHandler).Assembly)
            .AddSingleton<IHttpEndpointDefinition>(_ =>
                new HttpEndpointDefinition("GET", "/livez", Constants.DefaultLivenessTopic)));
    }

    public override void Configure(IBenzeneApplicationBuilder app, IConfiguration configuration)
    {
        app.UseHttp(http => http
            .UseLivenessCheck(x => x.AddHealthCheck<SimpleHealthCheck>())
            .UseMessageHandlers());
    }
}
```

## 4. Wire it into Program.cs

```csharp
using Benzene.AspNet.Core;

var builder = WebApplication.CreateBuilder(args);
builder.UseBenzene<StartUp>();

var app = builder.Build();
app.UseBenzene();

app.Run();
```

See [ASP.NET Core Integration](asp-net-core.md) for the full picture of this wiring.

## 5. Containerize it

Cloudflare Containers builds and runs a normal Docker image. Kestrel needs to listen on
`0.0.0.0:8080` — the conventional Cloudflare Containers listen port:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish MyApi.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s CMD curl -f http://localhost:8080/livez || exit 1
ENTRYPOINT ["dotnet", "MyApi.dll"]
```

```bash
docker build -t my-api .
docker run -p 8080:8080 my-api
```

`GET` `http://localhost:8080/hello/world` to confirm the handler responds, and
`http://localhost:8080/livez` to confirm the liveness check does.

## 6. Deploy to Cloudflare

Add a `wrangler.toml` that points a Worker at the image, backed by a Durable Object (Cloudflare
Containers' current binding model):

```toml
name = "my-api"
main = "src/index.ts"
compatibility_date = "2026-04-01"

[[containers]]
class_name = "MyApiContainer"
image = "../Dockerfile"
max_instances = 5

[[durable_objects.bindings]]
name = "MY_API_CONTAINER"
class_name = "MyApiContainer"

[[migrations]]
tag = "v1"
new_sqlite_classes = ["MyApiContainer"]
```

And a minimal Worker that proxies every request into the container:

```typescript
import { Container, getContainer } from "@cloudflare/containers";

export class MyApiContainer extends Container {
  defaultPort = 8080;
}

interface Env {
  MY_API_CONTAINER: DurableObjectNamespace<MyApiContainer>;
}

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    return getContainer(env.MY_API_CONTAINER).fetch(request);
  },
};
```

```bash
npm install @cloudflare/containers wrangler
wrangler deploy
```

## Health Checks

`/livez` above is a liveness check only — it confirms the process itself is responsive, not
that any external dependency is reachable, per Kubernetes' (and Cloudflare Containers') own
guidance. If your app has a real external dependency (a database, a downstream API), add a
`/readyz` readiness check the same way, with `UseReadinessCheck` in place of `UseLivenessCheck`.
See [Kubernetes Health Checks](kubernetes-health-checks.md) for the full liveness/readiness model
and how to write a check against a real dependency.

## Troubleshooting

**Container never becomes healthy / Cloudflare keeps restarting it** — confirm
`ASPNETCORE_URLS` is set to `http://0.0.0.0:8080` (not `localhost`, which won't accept
connections from outside the container) and that `/livez` returns `200`, not `503` — a
`UseLivenessCheck` failure reports `503 Service Unavailable`, which reads as unhealthy to any
HTTP-based health check, Cloudflare's included.

**`docker build` fails to find project files** — if your Benzene project references other
projects via relative `ProjectReference` paths (e.g. a shared domain project, as in
[`examples/Cloudflare`](../examples/Cloudflare)), the build context must include all of them —
build from the repository root (`docker build -f path/to/Dockerfile .`), not the project's own
folder.

**Worker deploys but requests never reach the container** — double check the Durable Object
`class_name` in `wrangler.toml`'s `[[containers]]` and `[[durable_objects.bindings]]` sections
match the exported class name in `src/index.ts` exactly, and that a `[[migrations]]` entry with
`new_sqlite_classes` (not the older `new_classes`) lists that same class name.

## See Also

- [ASP.NET Core Integration](asp-net-core.md) — the full `Benzene.AspNet.Core` wiring this builds on
- [Kubernetes Health Checks](kubernetes-health-checks.md) — the liveness/readiness pattern used above
- [Message Handlers](message-handlers.md)
- [`examples/Cloudflare`](../examples/Cloudflare) — the complete runnable project, Dockerfile, and
  Worker config this guide is drawn from (hand-checked against current Cloudflare Containers docs,
  but not independently deployed or verified against a live account — review before production use)
