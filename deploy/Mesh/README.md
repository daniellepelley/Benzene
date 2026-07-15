# Benzene.Mesh.Host

A config-driven, Docker/Compose-deployable Benzene Mesh Aggregator + UI - for running the mesh
dashboard against your *own real services* during local development, the same way you'd spin up a
local Postgres or Redis alongside your app. This is a genuinely different tier from
[`examples/Mesh/`](../../examples/Mesh): that folder is a demo with fake/canned data showing off
the feature; this is a real tool you point at real services.

See [`work/service-mesh-roadmap-1.0.md`](../../work/service-mesh-roadmap-1.0.md) for the full
multi-transport data collection design (Phases A-D) this host is the last phase of.

## What it does

- Polls every configured service on a timer (`pollIntervalSeconds`) via
  `Benzene.Mesh.Aggregator.MeshAggregator` - no external scheduler needed, unlike a hosted
  deployment where `mesh:aggregate` is typically triggered by a scheduled Lambda/Function
  invocation instead. This background poll loop (`MeshPollBackgroundService`) is new capability
  local to this Host app only - `MeshAggregateMessageHandler` itself stays invocation-triggered-only.
- Supports both HTTP-polled services (`Benzene.Mesh.Aggregator`'s `HttpMeshServiceSource`) and
  AWS-Lambda-Invoke-polled services (`Benzene.Mesh.Aws.Lambda`'s `LambdaMeshServiceSource`) via
  each service's `source`/`sourceOptions` in config - see below. Services with no synchronous entry
  point at all need to self-report instead (`Benzene.Mesh.Reporting`, linked into that service, not
  this host) and POST to this host's `/mesh/report` ingestion endpoint.
- Serves the Mesh UI dashboard at `/mesh-ui`, self-served from the same host
  (`Benzene.Mesh.Ui.UseMeshUi()`) - the generated `manifest.json`/`services/*.json`/`topology.json`
  are also served directly at `/artifacts/*`.

## Configuration

The primary path is a bind-mounted `mesh.json` (env var `MESH_CONFIG_PATH` points at it) - this
repo's first use of `IConfiguration.Get<T>()` binding a list of objects, flagged in the roadmap doc
as genuinely new territory, not an established Benzene convention being reused.

```jsonc
{
  "artifactRootDirectory": "/data/mesh-artifacts",
  "pollIntervalSeconds": 60,
  "services": [
    {
      "name": "orders-api",
      "specUrl": "http://orders-api:8080/spec?type=benzene",
      "healthUrl": "http://orders-api:8080/healthcheck"
    },
    {
      "name": "payments-fn",
      "source": "AwsLambdaInvoke",
      "sourceOptions": { "functionName": "payments-fn", "region": "us-east-1" }
    }
  ]
}
```

- `source` defaults to `"Http"` (so it can be omitted, as in the `orders-api` entry above) - see
  `Benzene.Mesh.Contracts.MeshServiceSource` for known values.
- `specUrl`/`healthUrl` are optional for non-`"Http"` sources - the fetch itself doesn't use them,
  but they're worth setting anyway purely so the Mesh UI's "spec"/"health" links have somewhere to
  point.
- Individual top-level scalars (`ArtifactRootDirectory`, `PollIntervalSeconds`) can also be
  overridden via plain environment variables (`Host.CreateDefaultBuilder` already wires those up)
  for single-service smoke-testing without a mounted file - the `services` list itself is
  impractical to express that way, so `mesh.json` is the path for anything beyond a service or two.

AWS credentials for `AwsLambdaInvoke`-sourced services come from the container's normal AWS
credential chain (environment variables, an IAM role, a mounted credentials file) - not from
`mesh.json` itself.

## Running it via Docker Compose

Not something this repo runs itself (there are no real services here to compose against beyond the
demo in `examples/Mesh/`) - this is what a *consuming* solution's own `docker-compose.yml` would add
alongside its own services:

```yaml
services:
  mesh:
    image: ghcr.io/<org>/benzene-mesh:latest
    ports:
      - "8090:8080"
    environment:
      - MESH_CONFIG_PATH=/config/mesh.json
    volumes:
      - ./mesh.json:/config/mesh.json:ro
      - mesh-artifacts:/data/mesh-artifacts
    depends_on: [orders-api, payments-api]
volumes:
  mesh-artifacts:
```

Then browse `http://localhost:8090/mesh-ui`.

## Local development (without Docker)

```bash
cd deploy/Mesh
dotnet build Benzene.Mesh.Host.sln
MESH_CONFIG_PATH=./mesh.json dotnet run --project Benzene.Mesh.Host
```

## Building the image locally

```bash
# from the repo root - the Dockerfile needs the whole repo as build context (sibling src/ ProjectReferences)
docker build -f deploy/Mesh/Benzene.Mesh.Host/Dockerfile -t benzene-mesh:local .
docker run -p 8090:8080 -e MESH_CONFIG_PATH=/config/mesh.json -v "$(pwd)/mesh.json:/config/mesh.json:ro" benzene-mesh:local
```

## Publishing

`.github/workflows/deploy-mesh-host.yml` (manual `workflow_dispatch`, same trigger pattern as every
other Benzene deploy workflow) builds and pushes this image to GHCR. `.github/workflows/build-mesh-host.yml`
runs on every push/PR touching this folder, compiling `Benzene.Mesh.Host.sln` - this deployable gets
real CI coverage, unlike `examples/`, since it's a production-ready primitive, not a demo.

## Why this isn't in `Benzene.sln`/`Benzene.Examples.sln`

Same reasoning as `templates/Benzene.Templates.sln`: an independently-versioned, independently-built
artifact (here, Docker-packaged instead of NuGet-packaged) with its own release lifecycle, not
compiled/tested as part of the main library's build.
