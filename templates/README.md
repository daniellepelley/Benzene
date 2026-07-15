# Benzene.Templates

`dotnet new` starter-project templates for Benzene, packaged as a single NuGet template pack. See
[`docs/getting-started-templates.md`](../docs/getting-started-templates.md) for the end-user guide
(installing, generating a project, consuming from Visual Studio/Rider).

## Layout

```
Benzene.Templates.csproj   # the template pack project (PackageType=Template)
content/
  asp/                      # benzene.asp            - ASP.NET Core
  aws-apigateway/           # benzene.aws.apigateway  - AWS Lambda + API Gateway
  aws-sqs/                  # benzene.aws.sqs         - AWS Lambda + SQS
  aws-sns/                  # benzene.aws.sns         - AWS Lambda + SNS
  azure-http/               # benzene.azure.http      - Azure Functions (isolated worker, HTTP trigger)
  kafka-worker/             # benzene.kafka.worker    - Self-hosted Kafka consumer
```

Each `content/<name>/` folder is a complete, standalone project: a `.template.config/template.json`
manifest plus the files that get copied (and renamed, via the `sourceName` "BenzeneStarter") into the
user's output directory. **These files are never built or referenced as part of this repo's own
`Benzene.sln`/`Benzene.Examples.sln`** — they're inert content until `dotnet new` copies them
somewhere else, and they only ever reference Benzene via `PackageReference` (never
`ProjectReference` back into this repo), since a generated project has no access to this repo's
source tree.

`asp`/`aws-apigateway`/`aws-sqs`/`aws-sns`/`azure-http` all ship a byte-identical
`HelloWorldMessageHandler.cs` — the same handler running unchanged behind five different transports
is the actual point of the exercise. `content/asp/HelloWorldMessageHandler.cs` is the canonical
copy; **keep the other four identical to it** (`kafka-worker`'s is intentionally different — Kafka's
fire-and-forget, literal-topic-name routing is a genuinely different shape, not a drift). CI
(`.github/workflows/build-templates.yml`) enforces this with a diff check — see below.

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

Run this before committing a change to any of the five identical `HelloWorldMessageHandler.cs`
copies (CI runs the same check):

```bash
canonical="templates/content/asp/HelloWorldMessageHandler.cs"
for f in templates/content/aws-apigateway templates/content/aws-sqs templates/content/aws-sns templates/content/azure-http; do
  diff "$canonical" "$f/HelloWorldMessageHandler.cs" || { echo "DRIFT: $f"; exit 1; }
done
```

## Why this isn't in `Benzene.sln`

A template pack's build verb is `dotnet pack` + generate-and-build the *output*, not `dotnet
build`/`dotnet test` against this repo's own source — it doesn't fit either existing solution's CI
gate. `Benzene.Templates.sln` here is for local dev convenience only.
