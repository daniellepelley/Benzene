# Benzene.Descriptor

A `dotnet` tool (`benzene-descriptor`) that emits a service's **deployment descriptor** (`service.json`)
at **build time** from a **built but non-running, non-deployed** Benzene service — by constructing it
in-process and reading the `spec` it already computes. No deploy, no socket, no AWS. See
[`work/deployment-descriptor-design.md`](../../work/deployment-descriptor-design.md) for the design and
rationale.

> **Status:** working spike-grade tool. Currently supports the **AWS Lambda** host
> (`AwsLambdaHost<StartUp>`). Not yet in `Benzene.sln` or `deploy-benzene.yml` — wiring it into the
> solution + publish pipeline is a follow-up that needs approval (the repo forbids changing `.sln`
> structure without it). In-repo it uses `ProjectReference`s so it builds/tests against local source;
> the shipped package would use `PackageReference`s pinned to the consuming service's Benzene version.

## What it produces

Against the real `examples/AwsMesh/Payments` service (a compiled `.dll`, never deployed):

```jsonc
{
  "descriptorVersion": "0.1",
  "service": "payments",
  "serviceVersion": "1.0.0",
  "placement": { "cloud": "aws", "region": "eu-west-1" },
  "transports": [ "api-gateway", "benzene", "sqs", "sns", "eventbridge" ],
  "consumes": [
    { "topic": "payments:capture", "http": [ { "method": "POST", "path": "/payments" } ],
      "requestSchema": { "required": ["Currency","OrderId"], ... }, "responseSchema": { ... } },
    { "topic": "payments:get-all", "http": [ { "method": "GET", "path": "/payments" } ], ... }
  ],
  "produces": [
    { "topic": "shipping:book",     "messageSchema": { ... } },
    { "topic": "payment:captured",  "messageSchema": { ... } }
  ],
  "descriptorHash": "sha256:4906226bb54a53eb6352cb0189ead3d13c547d848dabeb9f288dffc3d76fd70b"
}
```

Note `produces[]` carries **topic + payload schema only**. The per-egress **transport kind** and
**destination env-var** are deliberately omitted — that is paused pending the outbound-routing design
(see the design note). Everything else is the service's real, code-derived surface.

## How it works

1. Loads the built service assembly in a plugin `AssemblyLoadContext` (`ServiceLoadContext`) that
   defers Benzene/Microsoft/System contract assemblies to the tool's own copies (keeping type identity)
   and loads the service's unique transports/deps from its output folder.
2. Finds the `BenzeneStartUp`, and replicates `AwsLambdaHost<StartUp>`'s constructor — `ConfigureServices`
   + `Configure` + pipeline build — **without** the run/listen step. This is network-free.
3. Sends an in-memory `spec` message through the built pipeline (`AwsLambdaBenzeneTestHost`) and derives
   the mesh `descriptorHash` from the handler types.
4. Distils the neutral `service.json`.

## Run it directly

```bash
dotnet run --project tools/Benzene.Descriptor -- \
  --assembly examples/AwsMesh/Payments/bin/Debug/net10.0/Benzene.Examples.AwsMesh.Payments.dll \
  --service payments --service-version 1.0.0 --output service.json
```

Options: `--assembly <dll>` (required), `--output <path>` (stdout if omitted), `--service <name>`
(defaults to the assembly name), `--service-version <v>`, `--cloud <aws>`, `--region <r>`.

## As a build step

Install the tool (once published), then either call it from a CI step, or import the example
`build/Benzene.Descriptor.targets` and opt in:

```xml
<PropertyGroup>
  <BenzeneEmitDescriptor>true</BenzeneEmitDescriptor>
</PropertyGroup>
```

That runs `benzene-descriptor` after `Build`, writing `<AssemblyName>.service.json` next to the output.
The `.targets` is shipped here as an **example** (not packed into the tool package — a `dotnet` tool is
installed and run, not `<PackageReference>`d); a service copies/imports it or wires its own CI step.

## Caveats

- **AWS Lambda host only** for now; self-host worker and ASP.NET hosts would each need their
  equivalent construction path.
- The plugin ALC assumes the tool and the service resolve the shared `Benzene.*` assemblies to the
  **same version** — pin the tool to the service's Benzene version.
- A `StartUp` that does real I/O in `ConfigureServices`/`Configure` (reads a secret, pings a DB) would
  have build-time side effects. Benzene's convention is registration-only.
