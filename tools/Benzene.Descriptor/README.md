# Benzene.Descriptor

A `dotnet` tool (`benzene-descriptor`) that emits a service's **deployment descriptor** (`service.json`)
at **build time** from a **built but non-running, non-deployed** Benzene service — by constructing it
in-process and reading the `spec` it already computes. No deploy, no socket, no AWS. See
[`work/deployment-descriptor-design.md`](../../work/deployment-descriptor-design.md) for the design and
rationale.

> **Status:** working spike-grade tool. The introspection is **cloud-agnostic** (see below); an
> **AWS Lambda** host adapter additionally supplies the inbound transport-name list. Not yet in
> `Benzene.sln` or `deploy-benzene.yml` — wiring it into the solution + publish pipeline is a follow-up
> that needs approval (the repo forbids changing `.sln` structure without it). In-repo it uses
> `ProjectReference`s so it builds/tests against local source; the shipped package would use
> `PackageReference`s pinned to the consuming service's Benzene version.

## Cloud-agnostic by design

The descriptor content is cloud-neutral, and the tool is built that way. Everything except the inbound
transport-name list comes from host-neutral `ConfigureServices` — so the same service (which in Benzene
can target multiple clouds from one codebase) yields the same logical descriptor regardless of host:

- **Cloud-agnostic core** (`NeutralHostAdapter`, works for any host): service identity, `consumes`
  (topics + HTTP routes + base schemas), `produces` (topics + payload schemas + **outbound
  `transportKind`**). No cloud coupling.
- **Host adapter** (AWS Lambda today): runs the host-specific `Configure` so the **inbound
  transport-name list** and validation-enriched schemas are populated. A new cloud is just a new
  adapter of the same shape — nothing else changes.

`transportsResolved` in the output says whether a host adapter ran (inbound transports present) or the
neutral core was used. Force one with `--host neutral` / `--host aws-lambda`.

Outbound `transportKind` (`sqs`/`sns`/`eventbridge`/`servicebus`/…) is recovered cloud-agnostically
from the route's converter type name — no hard-coded per-cloud list. The outbound **destination** is
deliberately *not* emitted: that value is resolved (its env-var name is lost) and is the crux of the
paused outbound-routing redesign.

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
  "host": "aws-lambda",
  "transportsResolved": true,
  "produces": [
    { "topic": "shipping:book",     "transportKind": "sqs",         "messageSchema": { ... } },
    { "topic": "payment:captured",  "transportKind": "eventbridge", "messageSchema": { ... } }
  ],
  "descriptorHash": "sha256:4906226bb54a53eb6352cb0189ead3d13c547d848dabeb9f288dffc3d76fd70b"
}
```

`produces[]` carries topic + payload schema + the outbound **transportKind**. The per-egress
**destination env-var** is deliberately omitted — that is paused pending the outbound-routing design
(see the design note). Everything else is the service's real, code-derived surface.

## How it works

1. Loads the built service assembly in a plugin `AssemblyLoadContext` (`ServiceLoadContext`) that
   defers Benzene/Microsoft/System contract assemblies to the tool's own copies (keeping type identity)
   and loads the service's unique transports/deps from its output folder.
2. Selects a host adapter (`HostAdapters`): the AWS Lambda adapter if the service references
   `Benzene.Aws.Lambda.Core`, else the cloud-agnostic `NeutralHostAdapter`. The adapter runs the
   service's registration (`ConfigureServices`, plus host-specific `Configure` for AWS) **without** the
   run/listen step. Network-free.
3. Runs `SpecBuilder` directly against the built container for `consumes`/`produces`/schemas/transports,
   derives the mesh `descriptorHash` from the handler types, and recovers each outbound topic's
   `transportKind` via `OutboundRouteInspector`.
4. Distils the neutral `service.json`.

## Run it directly

```bash
dotnet run --project tools/Benzene.Descriptor -- \
  --assembly examples/AwsMesh/Payments/bin/Debug/net10.0/Benzene.Examples.AwsMesh.Payments.dll \
  --service payments --service-version 1.0.0 --output service.json
```

Options: `--assembly <dll>` (required), `--output <path>` (stdout if omitted), `--service <name>`
(defaults to the assembly name), `--service-version <v>`, `--cloud <aws>`, `--region <r>`,
`--host <neutral|aws-lambda>` (force an adapter; auto-selected otherwise).

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

- **Inbound transports need a host adapter** — AWS Lambda is implemented; other hosts (self-host
  worker, ASP.NET, Azure Functions) fall back to the neutral core (full logical descriptor, but
  `transports: []` and `transportsResolved: false`) until their adapter is added.
- **Outbound `transportKind` uses best-effort reflection** into the built routing table (today's
  outbound model exposes no read-model). It degrades to `"unknown"` on any failure, and is meant to be
  replaced when the outbound-routing redesign lands. Destination binding is intentionally not surfaced.
- The plugin ALC assumes the tool and the service resolve the shared `Benzene.*` assemblies to the
  **same version** — pin the tool to the service's Benzene version.
- A `StartUp` that does real I/O in `ConfigureServices`/`Configure` (reads a secret, pings a DB) would
  have build-time side effects. Benzene's convention is registration-only.
