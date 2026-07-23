# Benzene.Templates

`dotnet new` starter-project templates for Benzene, packaged as a single NuGet template pack. See
[`docs/getting-started-templates.md`](../docs/getting-started-templates.md) for the end-user guide
(installing, generating a project, consuming from Visual Studio/Rider).

## Layout

```
Benzene.Templates.csproj   # the template pack project (PackageType=Template)
content/
  # HTTP-shaped
  asp/                      # benzene.asp                - ASP.NET Core
  selfhost-http/            # benzene.selfhost.http       - Self-hosted HTTP (HttpListener) - DEPRECATED,
                            #                               use benzene.asp (Kestrel); see docs/deprecations.md
  aws-apigateway/           # benzene.aws.apigateway      - AWS Lambda + API Gateway
  azure-http/               # benzene.azure.http          - Azure Functions (isolated worker, HTTP trigger)
  # AWS Lambda event sources
  aws-sqs/                  # benzene.aws.sqs             - AWS Lambda + SQS
  aws-sns/                  # benzene.aws.sns             - AWS Lambda + SNS
  # Self-hosted workers (Benzene owns the process)
  kafka-worker/             # benzene.kafka.worker        - Self-hosted Kafka consumer
  rabbitmq-worker/          # benzene.rabbitmq.worker     - Self-hosted RabbitMQ consumer
  servicebus-worker/        # benzene.servicebus.worker   - Self-hosted Azure Service Bus consumer
  # Azure Functions triggers (isolated worker)
  azure-servicebus/         # benzene.azure.servicebus    - Azure Functions + Service Bus trigger
  azure-eventhub/           # benzene.azure.eventhub      - Azure Functions + Event Hub trigger
  azure-eventgrid/          # benzene.azure.eventgrid     - Azure Functions + Event Grid trigger (routes by event type)
  azure-queuestorage/       # benzene.azure.queuestorage  - Azure Functions + Queue Storage trigger
```

Each `content/<name>/` folder is a complete, standalone project: a `.template.config/template.json`
manifest plus the files that get copied (and renamed, via the `sourceName` "BenzeneStarter") into the
user's output directory. **These files are never built or referenced as part of this repo's own
`Benzene.sln`/`Benzene.Examples.sln`** — they're inert content until `dotnet new` copies them
somewhere else, and they only ever reference Benzene via `PackageReference` (never
`ProjectReference` back into this repo), since a generated project has no access to this repo's
source tree.

## Optional unit-test project (`--IncludeTests`)

Every template also carries a `BenzeneStarter.Tests/` xUnit project, included by **default** and
switchable off with `--IncludeTests false` (a `bool` `parameter` symbol in each `template.json`; a
`sources` `modifier` excludes the folder when it's false). The main project stays flat at the output
root exactly as before — a `<Compile Remove="BenzeneStarter.Tests\**" />` in each main `.csproj` keeps
the test sources out of the main project's own compilation (default globbing would otherwise pull the
subfolder in; the Remove is a harmless no-op when tests aren't generated).

Two test styles are in play — the same setup wraps every transport, so you learn it once:

- **Component test (every message/event/Lambda template)** — boots the SAME app the `StartUp`
  configures for a real deployment: `BenzeneTestHost.Create<StartUp>()` (from `Benzene.Testing`) then a
  transport `Build*` extension — `.BuildAwsLambdaHost()`, `.BuildAzureFunctionApp()`,
  `.BuildRabbitMqWorkerHost()`, `.BuildServiceBusWorkerHost()`, `.BuildKafkaWorkerHost<StartUp, TKey, TValue>()` —
  with `WithServices`/`WithConfiguration` seams to override dependencies and settings, then pushes a
  message through the whole pipeline via the transport's own entry point (`HandleEventHub`,
  `HandleQueueMessages`, `HandleEventGridEvents`, or the worker hosts' `HandleAsync(delivery)`) built
  from a `MessageBuilder` with a matching `As*BenzeneMessage()` builder. The demo handler takes one
  injected service (`IGreeter`) so the test can swap it for a spy and assert the handler actually ran
  with the routed message. The consistent shape everywhere is `Create<StartUp>()` →
  `Build<Transport>Host()` → push a built message. These tests are **bespoke per transport** (each
  transport's host build + entry point differ), so they're excluded from the shared-**test-file** drift
  checks below — but the handler they exercise is still shared (see the handler groups).
- **In-memory test (`asp`, `selfhost-http`)** — transport-agnostic: routes the demo topic through
  Benzene's in-memory `BenzeneMessageApplication` host (no HTTP/Lambda/broker needed). The demo topic
  returns `Ok` and an unknown topic returns `NotFound`. References only `Benzene.Core.MessageHandlers`
  + `Benzene.Microsoft.Dependencies` plus the generated main project. (These two HTTP hosts don't yet
  have a `Build*Host`, so they stay on the in-memory test.)

The `HelloWorldMessageHandler.cs` falls into two shared groups plus two one-offs — the same handler
running unchanged behind many transports is the actual point of the exercise, and CI
(`.github/workflows/build-templates.yml`) enforces the sharing with a diff check (see below):
- **Group A** (request/response, `content/asp/HelloWorldMessageHandler.cs` is canonical): `asp`,
  `selfhost-http`, `aws-apigateway`, `azure-http`. The `[HttpEndpoint]` is harmless on the non-HTTP ones.
- **Group C** (fire-and-forget with an injected `IGreeter` collaborator, `content/aws-sqs/HelloWorldMessageHandler.cs`
  is canonical): `aws-sqs`, `aws-sns`, `azure-eventhub`, `azure-servicebus`, `azure-queuestorage`,
  `rabbitmq-worker`, `servicebus-worker` — every fire-and-forget transport whose demo topic is
  `hello:world`.
- **Standalone** (same collaborator shape, different topic string, so deliberately not a drift):
  `kafka-worker` (literal Kafka topic `hello_world`) and `azure-eventgrid` (routes by event **type**
  `hello.world`).

> **Publish cadence:** every template references its Benzene packages with `Version="*-*"` (latest
> published prerelease). Some packages are in `Benzene.sln` and packable but publish on the **next**
> `deploy-benzene.yml` release, so their generated projects can't `restore`/`build` until then: a few
> transports (`Benzene.RabbitMq`, `Benzene.Azure.ServiceBus`, `Benzene.Azure.Function.EventGrid`,
> `Benzene.Azure.Function.QueueStorage`) plus every test-host package the component tests use
> (`Benzene.Azure.Function.EventGrid.TestHelpers`, `Benzene.Azure.Function.QueueStorage.TestHelpers`,
> `Benzene.RabbitMq.TestHelpers`, `Benzene.Azure.ServiceBus.TestHelpers`, `Benzene.Kafka.Core.TestHelpers`).
> `build-templates.yml` treats that `NU1101` as a warning (not a failure) and self-heals once the
> package is published — but a user who generates one of those templates before the release will hit the
> same restore error.

## Local workflow

```bash
# from the repo root
dotnet pack templates/Benzene.Templates.csproj -c Release -o /tmp/benzene-templates-pack
dotnet new install /tmp/benzene-templates-pack/Benzene.Templates.0.1.0-alpha.nupkg --force

dotnet new list --author Benzene

dotnet new benzene.asp -n MySample -o /tmp/my-sample
cd /tmp/my-sample && dotnet build
```

To uninstall a locally-installed copy before re-testing changes:

```bash
dotnet new uninstall Benzene.Templates
```

### Shared-handler diff check

Run this before committing a change to any of the shared `HelloWorldMessageHandler.cs` copies (CI runs
the same check — group A + group B, see "Layout" above):

```bash
# Group A (request/response, canonical asp)
canonical_a="templates/content/asp/HelloWorldMessageHandler.cs"
for d in aws-apigateway azure-http selfhost-http; do
  diff "$canonical_a" "templates/content/$d/HelloWorldMessageHandler.cs" || { echo "DRIFT (A): $d"; exit 1; }
done
# Group C (fire-and-forget with an injected collaborator, canonical aws-sqs)
canonical_c="templates/content/aws-sqs/HelloWorldMessageHandler.cs"
for d in aws-sns azure-eventhub azure-servicebus azure-queuestorage rabbitmq-worker servicebus-worker; do
  diff "$canonical_c" "templates/content/$d/HelloWorldMessageHandler.cs" || { echo "DRIFT (C): $d"; exit 1; }
done
# kafka-worker (hello_world) and azure-eventgrid (hello.world) carry the same collaborator shape with a
# different topic string, so they're standalone - not diffed against any canonical.
```

### Shared-test diff check

Only templates still on the **in-memory** test keep the identical `.csproj` + shared test `.cs` — now
just the two HTTP-shaped ones (`asp`, `selfhost-http`), which assert `Ok` + body. Every message/event
template has migrated to a bespoke per-transport component test (different TestHelpers package +
`Build*Host` + `As*` builder), so they drop out of these shared-file lists; their handler sharing is
still enforced by the handler diff check above.

```bash
# every in-memory test .csproj is identical (component-test templates are omitted)
canonical_csproj="templates/content/asp/BenzeneStarter.Tests/BenzeneStarter.Tests.csproj"
for d in selfhost-http; do
  diff "$canonical_csproj" "templates/content/$d/BenzeneStarter.Tests/BenzeneStarter.Tests.csproj" || { echo "DRIFT (csproj): $d"; exit 1; }
done
# group A test (request/response): asserts Ok + body
canonical_test_a="templates/content/asp/BenzeneStarter.Tests/HelloWorldMessageHandlerTests.cs"
for d in selfhost-http; do
  diff "$canonical_test_a" "templates/content/$d/BenzeneStarter.Tests/HelloWorldMessageHandlerTests.cs" || { echo "DRIFT test (A): $d"; exit 1; }
done
```

## Publishing

`.github/workflows/deploy-templates.yml` (manual `workflow_dispatch`, same trigger style as the
main library's `deploy-benzene.yml`) packs `Benzene.Templates.csproj` and pushes it to nuget.org.
It's a separate workflow from `deploy-benzene.yml` because this package isn't part of `Benzene.sln`
and versions independently (`VersionPrefix`/`VersionSuffix` above, not `version.txt`) — running it
resolves the next `0.1.0.N-alpha` build number against nuget.org the same way `deploy-benzene.yml`
does for `Benzene.Core`.

## Why this isn't in `Benzene.sln`

A template pack's build verb is `dotnet pack` + generate-and-build the *output*, not `dotnet
build`/`dotnet test` against this repo's own source — it doesn't fit either existing solution's CI
gate. `Benzene.Templates.sln` here is for local dev convenience only.
