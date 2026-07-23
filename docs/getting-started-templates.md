# Getting Started: Project Templates

`Benzene.Templates` is a `dotnet new` template pack with starter projects for every host Benzene
supports out of the box: ASP.NET Core, self-hosted HTTP, AWS Lambda (API Gateway, SQS, SNS), Azure
Functions (HTTP, Service Bus, Event Hub, Event Grid, Queue Storage), and self-hosted workers (Kafka,
RabbitMQ, Azure Service Bus). Each one generates a complete, buildable project with a single demo
handler (`HelloWorldMessageHandler`) wired end to end — the fastest way to get a real Benzene
service running before you write a line of your own code.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## 1. Install the template pack

```bash
dotnet new install Benzene.Templates
```

One-time, per machine. Confirm it installed correctly:

```bash
dotnet new list --author Benzene
```

## 2. Generate a project

```bash
dotnet new benzene.asp -n MyOrderService -o ./MyOrderService
cd MyOrderService
dotnet build
```

Swap `benzene.asp` for any of the short names below. `-n` sets the project/namespace name (every
occurrence of the placeholder gets renamed); `-o` sets the output directory.

| Short name | What it generates |
|---|---|
| `benzene.asp` | ASP.NET Core service — no cloud account needed, the quickest way to try Benzene locally |
| `benzene.aws.apigateway` | AWS Lambda triggered by API Gateway, with a SAM `template.yaml` |
| `benzene.aws.sqs` | AWS Lambda triggered by an SQS queue |
| `benzene.aws.sns` | AWS Lambda triggered by an SNS topic |
| `benzene.azure.http` | Azure Functions (isolated worker), HTTP trigger |
| `benzene.azure.servicebus` | Azure Functions (isolated worker), Service Bus trigger |
| `benzene.azure.eventhub` | Azure Functions (isolated worker), Event Hub trigger |
| `benzene.azure.eventgrid` | Azure Functions (isolated worker), Event Grid trigger — routes by event **type** |
| `benzene.azure.queuestorage` | Azure Functions (isolated worker), Queue Storage trigger |
| `benzene.kafka.worker` | Self-hosted Kafka consumer (`Confluent.Kafka`), for a long-running worker/container |
| `benzene.rabbitmq.worker` | Self-hosted RabbitMQ consumer, for a long-running worker/container |
| `benzene.servicebus.worker` | Self-hosted Azure Service Bus consumer (Benzene owns the process), for a long-running worker/container |

Every generated project has its own `README.md` with run/deploy instructions and links back to the
matching [getting-started guide](getting-started.md) for that host — the template gets you to a
buildable starting point; the guide covers everything beyond it (validation, other event sources,
testing, observability).

> **Newer transports, publish cadence:** `benzene.rabbitmq.worker`, `benzene.servicebus.worker`,
> `benzene.azure.eventgrid`, and `benzene.azure.queuestorage` reference Benzene packages
> (`Benzene.RabbitMq`, `Benzene.Azure.ServiceBus`, `Benzene.Azure.Function.EventGrid`,
> `Benzene.Azure.Function.QueueStorage`) that are complete in the repo but publish to nuget.org on
> the **next** release. Until then those four generate fine but won't `restore`/`build` (you'll see
> an `NU1101` for the missing package). The other nine work today.

## What you get

The HTTP-shaped and Lambda templates (`benzene.asp`, `benzene.aws.apigateway`, `benzene.aws.sqs`,
`benzene.aws.sns`, `benzene.azure.http`) generate the identical request/response starter handler:

```csharp
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
```

That's deliberate — it's the same handler shape that runs unchanged behind every transport Benzene
supports (see [Getting Started](getting-started.md)'s "core idea in 60 seconds"), so trying a second
template after your first one feels familiar rather than like starting over.

The fire-and-forget worker templates (`benzene.rabbitmq.worker`, `benzene.servicebus.worker`,
`benzene.azure.servicebus`, `benzene.azure.eventhub`, `benzene.azure.queuestorage`) generate the
same handler minus the `[HttpEndpoint]` — a queue/topic message has no HTTP route, so it routes on
`[Message("hello:world")]` alone. `benzene.kafka.worker` (literal Kafka topic-name routing) and
`benzene.azure.eventgrid` (routes by event **type**, `[Message("hello.world")]`) are each a
deliberately different shape — see their generated READMEs.

## Visual Studio

> **Not yet independently verified against a real Visual Studio 2026 install from this repo —
> confirm the exact click-path before treating this as final.**

Templates installed via `dotnet new install` are expected to surface automatically in
File → New → Project, without a separate Visual Studio extension:

1. `dotnet new install Benzene.Templates` from any terminal (once).
2. Visual Studio → **File → New → Project** → type "Benzene" in the search box.
3. Pick a template (e.g. "Benzene AWS Lambda (API Gateway)"), name the project, choose a location,
   **Create**.

## Rider

> **Not yet independently verified against a real Rider install from this repo — confirm the exact
> click-path before treating this as final.**

JetBrains Rider is expected to discover `dotnet new`-installed templates directly, without a
plugin:

1. `dotnet new install Benzene.Templates` from a terminal (once) — Rider's own terminal panel works
   too.
2. Rider → **New Solution** → search "Benzene" in the template list.
3. Pick a template, configure the name/location, **Create**.
   If Rider was already open when you ran `dotnet new install`, reopen the New Solution dialog (or
   restart Rider) to pick up the newly-installed template.

## Troubleshooting

- **`dotnet new list` doesn't show any Benzene templates** — confirm `dotnet new install
  Benzene.Templates` reported success and that you're on a recent .NET SDK (`dotnet --version`).
- **Generated project fails to restore** — Benzene packages are prerelease-only until 1.0; the
  templates already reference them with a floating `Version="*-*"` (equivalent to `dotnet add
  package ... --prerelease`), so this usually means a transient NuGet outage or no network access,
  not a template bug.
- **AWS/Azure templates reference package versions that seem out of date** — the Microsoft Azure
  Functions SDK packages in the `benzene.azure.*` templates (host SDK plus the per-trigger extension)
  are intentionally pinned (not floating) to the versions Benzene itself builds and tests against;
  see each template's generated `README.md`.
- **One of the four newer transports fails to restore with `NU1101`** — `benzene.rabbitmq.worker`,
  `benzene.servicebus.worker`, `benzene.azure.eventgrid`, and `benzene.azure.queuestorage` depend on
  Benzene packages that publish on the next release (see the note under the short-name table). This
  isn't a template bug; those four build once their packages reach nuget.org.

## See Also

- [Getting Started](getting-started.md) — the hand-written walkthrough each template automates
- [AWS Lambda Setup](getting-started-aws.md) / [Azure Functions Setup](azure-functions.md) /
  [Kafka Setup](getting-started-kafka.md) — the full picture beyond what a starter template covers
- [`templates/`](../templates) in the repository — the template pack's source, and how to build/pack/test it locally
