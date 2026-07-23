# Deployment-descriptor spike

A throwaway proof for the [deployment-descriptor design note](../../deployment-descriptor-design.md).
It demonstrates the load-bearing claim: **a Benzene service's infra descriptor can be extracted at
build time from a non-running, non-deployed service** — by constructing its pipeline in-process (no
socket, no AWS, no deploy) and reading the `spec`/`mesh` descriptors it already computes.

## What it does

Against the real `examples/AwsMesh/Payments` service, `Program.cs`:

1. Constructs it in-process via `BenzeneTestHost.Create<Startup>().BuildAwsLambdaHost()` — the same
   `ConfigureServices` + `Configure` a Lambda cold start runs, minus the run/listen step.
2. Sends an in-memory `spec` message → the derived Cloud-Service spec (`output/spec.json`).
3. Builds the `mesh` ServiceDescriptor straight from the handler types (`output/mesh.json`).
4. Distils a neutral `output/service.json` — the proposed descriptor shape.

Nothing contacts the network.

## Run it

```bash
dotnet run --project work/spikes/deployment-descriptor
```

Outputs land in `output/` (checked in so the result is visible without a .NET SDK).

## Caveats

- Not part of `Benzene.sln` / `Benzene.Examples.sln` — it references an example project purely to run
  against a real service.
- The `transportKind`/`destinationRef` fields under `produces` are `TODO`s: those come from the
  outbound-routing read-model the design note proposes (the one net-new extraction capability). The
  topic + payload schema of each produced event are real.
- Schema `$ref`s are resolved one level into the spec's `components.schemas`; a nested `$ref` (e.g. an
  array's item type) may remain a `$ref`.
