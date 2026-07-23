# Design note: a build-time deployment descriptor for Benzene services

**Status:** investigation / proposal — not committed to the roadmap. Captures the conclusion of the
"descriptor-first infra generation" investigation and is backed by a runnable spike
(`work/spikes/deployment-descriptor/`) that produced the real output quoted below against the
`examples/AwsMesh/Payments` service.

---

## Problem

Standing up a Benzene service on a cloud today means maintaining **two** descriptions of the same
system:

1. **The Benzene service** — handlers (`[Message]`), HTTP routes (`[HttpEndpoint]`), the transports it
   consumes over, and the topics it publishes.
2. **The infrastructure** — the Terraform/CDK/CloudFormation that provisions the Lambdas, queues,
   topics, buses, and API Gateway routes those handlers need.

The two drift, and keeping them in sync is manual. Benzene's existing answer, the
`Benzene.CodeGen.Terraform` generator, tries to close the gap by **emitting `.tf` directly** — but it
bakes in one shop's opinions (zip-only packaging, a fixed tag taxonomy, an IAM role with no policy,
references to ambient `local.tracing_config`/`data.terraform_remote_state.sns` it doesn't define) and
is Terraform-only. Everyone's IaC is different, so a generator that owns the *rendering* can't fit
everyone. It's flagged `IsPackable=false`, "at a fork in the road."

### The chicken-and-egg

The Mesh already extracts a service's logical contract as JSON — but it does so by **polling the
running service's `spec`/`health` endpoints**. That's useless for *provisioning*: to poll a service it
must already be deployed, which means the infrastructure already exists. You can't use it to *create*
the infrastructure.

## The two insights that make this tractable

**1. Logical vs. physical.** Every Benzene transport routes on a **logical topic**, never a physical
resource identity. Inbound queue/topic/bus names don't exist in the service at all (the AWS
event-source mapping binds them externally); outbound destinations exist as `UseSqs(url)` arguments but
are conventionally env-var-sourced. So a descriptor can authoritatively state a service's **logical
needs + env-var contracts**, and the operator maps those to physical names. That's the correct
boundary, and it's what keeps the model robust across different IaC styles.

**2. "Running" ≠ "deployed".** The Mesh needs a *deployed* service only because it polls over the
network. But the descriptor's *content* is derived entirely from **startup registration**, and
registration is **network-free**. Constructing a Benzene app — running `ConfigureServices` +
`Configure` — opens no socket, connects to nothing, starts no hosted service; all I/O lives in a
separate `StartAsync`/run step. So a **build step can construct the service in-process and ask it for
the descriptor it already computes**, with no deployment. The chicken exists at build time; it just
isn't listening on a port. (Verified for Lambda, self-host worker, and ASP.NET hosts — the Lambda host
even builds its whole pipeline in its constructor.)

## What's extractable, and how

| Descriptor field | Pure static (Roslyn) | Compile-then-reflect | In-process construction |
|---|---|---|---|
| Consumed topics (`[Message]`) | ✅ | ✅ | ✅ |
| HTTP routes (`[HttpEndpoint]`) | ✅ | ✅ | ✅ |
| Payload JSON schemas | ⚠️ needs symbol-based rewrite of `MeshSchemaGenerator` | ✅ (existing `Derive(Type)`) | ✅ |
| **Produced/egress topics** | ❌ imperative | ❌ imperative | ✅ |
| **Transport kinds wired** | ❌ imperative | ❌ imperative | ✅ |
| Physical queue/topic/bus names | ❌ (not in the service) | ❌ | ❌ (operator input) |
| Outbound transport + env-var per topic | ❌ | ❌ | ⚠️ needs a small new read-model (see below) |

The decisive rows are **produced events** and **transport kinds**: in Benzene today these are
*imperative* (`AddResponseEventDeclarations(...)`, `.UseSqs(...)` inside `Configure`), not attributes,
so no static analysis reaches them. Only **executing the registration** materialises them — which,
per insight #2, a build step can do without deploying.

**Recommended mechanism: in-process construction.** Highest fidelity, and it reuses the entire
existing chain (`BenzeneTestHost` → `SpecBuilder`/`SpecMessageHandler` for the spec, `MeshDescriptorFactory`
for the mesh descriptor). A pure Roslyn source generator (the dormant `Benzene.CodeGen.SourceGenerators`
already extracts `[Message]` topics) is a useful *partial* complement — fast, no execution — for the
consumer contract and for an estate-wide static scan, but it structurally cannot see what a service
produces or which transports it uses.

## The spike — real output from a non-running service

`work/spikes/deployment-descriptor/` constructs the **real `examples/AwsMesh/Payments` service**
in-process (its actual `Startup.ConfigureServices` + `Configure`, exactly as `AwsLambdaHost<Startup>`
runs on cold start), never deploying and never opening a socket, then reads the `spec` and `mesh`
descriptors it already serves and distils a neutral `service.json`. The core is ~15 lines:

```csharp
// Construct the service as the Lambda host does — but never call the run/listen step.
var entryPoint = BenzeneTestHost.Create<Startup>().BuildAwsLambdaHost();
using var host = new AwsLambdaBenzeneTestHost(entryPoint);

// Ask the constructed pipeline for the spec it already computes (no network).
var spec = await host.SendBenzeneMessageAsync(
    MessageBuilder.Create("spec", new SpecRequest("benzene", "json")));

// The mesh ServiceDescriptor builds straight from handler types — no host at all.
var descriptor = MeshDescriptorFactory.Create(lookUp, new MeshServiceInfo("payments", "1.0.0", ...));
```

The payments service consumes two domain topics, publishes two events over two different transports,
and is wired over five inbound transports — and the distilled `service.json` captured **all of it**
(full file in `work/spikes/deployment-descriptor/output/service.json`):

```jsonc
{
  "service": "payments",
  "serviceVersion": "1.0.0",
  "placement": { "cloud": "aws", "region": "eu-west-1" },
  "transports": [ "api-gateway", "benzene", "sqs", "sns", "eventbridge" ],
  "consumes": [
    {
      "topic": "payments:capture",
      "http": [ { "method": "POST", "path": "/payments" } ],
      "requestSchema": {
        "required": [ "Currency", "OrderId" ],
        "type": "object",
        "properties": {
          "OrderId":  { "type": "string", "description": "Not Empty" },
          "Amount":   { "type": "number", "description": "Greater Than 0", "format": "double" },
          "Currency": { "type": "string", "description": "Not Empty" }
        }
      },
      "responseSchema": { "type": "object", "properties": { "Id": {...}, "Amount": {...}, ... } }
    },
    { "topic": "payments:get-all", "http": [ { "method": "GET", "path": "/payments" } ], ... }
  ],
  "produces": [
    {
      "topic": "shipping:book",
      "messageSchema": { "type": "object", "properties": { "OrderId": {...}, "Carrier": {...} } },
      "transportKind": "TODO: needs outbound-routing accessor",
      "destinationRef": "TODO: env-var name, needs outbound-routing accessor"
    },
    { "topic": "payment:captured", "messageSchema": {...}, ... }
  ],
  "descriptorHash": "sha256:4906226bb54a53eb6352cb0189ead3d13c547d848dabeb9f288dffc3d76fd70b"
}
```

Points worth noting from the **real** output:

- **Produced events and transports came through** (`shipping:book`, `payment:captured`; five
  transports) — the imperative facts static analysis can't see, captured because the pipeline was
  constructed.
- **Schemas are the genuine article** — the `required` set and the `"Not Empty"` / `"Greater Than 0"`
  descriptions are lifted from the service's FluentValidation rules, not re-derived.
- **The one honest gap** is the `TODO`s under `produces`: the topic and payload of each egress are
  known, but *which transport carries it* and *which env-var names the destination* are not surfaced
  by the spec today. That's the single new capability this needs (next section).
- The `descriptorHash` is stable and content-addressed — a natural drift signal for CI.

> **Update (tool spike):** `tools/Benzene.Descriptor` now recovers the outbound **`transportKind`**
> (`sqs`/`sns`/`eventbridge`/…) cloud-agnostically, by reading the route's converter context-type name
> via best-effort reflection — so the `TODO`s below are partly addressed for the transport half. The
> **destination** (env-var binding) is still deliberately deferred to the outbound redesign, and the
> reflection approach is spike-grade: the clean fix remains the read-model described here. The tool is
> also now structured as a cloud-agnostic core + host adapters (only the inbound transport list needs a
> host adapter), matching the "logical is cloud-neutral, transports are just names" principle.

## The one new capability required

Everything above except the egress transport/env-var reuses code that already ships. To fill the two
`TODO`s, add a small **read-model over outbound routing**: `OutboundRoutingBuilder` already holds the
registered routes keyed by topic; the transport kind and the destination config-key
(`SHIPPING_QUEUE_URL`, `EVENT_BUS_NAME`) are known at registration time but currently buried in private
fields on the built pipeline. Surfacing them as `{ topic, transportKind, destinationRef }` is the only
genuinely net-new code on the extraction side. (It does **not** resolve the ARN — `destinationRef` is
the env-var name, by design; the operator supplies the value.)

## Proposed `service.json` schema (v0.1)

A projection of the existing `MeshServiceDescriptor`, not a new parallel schema — reuse `Benzene.Mesh.Wire`
types:

```
service, serviceVersion, placement{cloud,region}
transports[]                      # logical kinds this service receives over
consumes[]  { topic, http[]{method,path}, requestSchema, responseSchema }
produces[]  { topic, messageSchema, transportKind, destinationRef }   # destinationRef = env-var name
descriptorHash
```

## Packaging

A `Benzene.Descriptor` NuGet package with an MSBuild target: referenced by a service project, on
post-build it constructs the app in-memory (the `BenzeneTestHost` seam), resolves the descriptors, and
writes `service.json` next to the build output. Running it **inside the service's own build context**
is the clean choice — all transport packages and payload types are already loaded, avoiding
assembly-load-context pain. (A standalone `dotnet tool` is the alternative if a build dependency is
unwanted.)

**Then**, separately: a **reference generator** (an *example*, not a package) that reads `service.json`
and renders Terraform/CDK in the operator's own house style — "teach the pattern, don't own the
policy," the same posture as the Serverless Framework cookbook. This is what replaces the opinionated
code-gen without deprecating it.

## Estate view (the mono-repo / one-big-solution case)

Run the per-project emit across every service project in the solution, then feed the per-service
`service.json` files into the **existing `MeshAggregator` reconciliation** (producer/consumer matching,
topology, version compatibility). That yields the whole-estate graph **at build time**, no deployment —
the same aggregation the Mesh does over polled specs, fed by static descriptors instead. Harder
(load/construct each project) but the aggregation logic already exists.

## Boundaries and risks

- **Physical names stay an operator input.** The descriptor emits logical needs + env-var contracts;
  it never invents a queue name or ARN. This is a feature, not a gap — it's what makes it fit every
  shop's IaC.
- **A `StartUp` that does real work in `ConfigureServices`/`Configure`** (reads a secret, pings a DB)
  would have build-time side effects. Benzene's convention is registration-only; document/enforce it,
  or offer a build-mode guard.
- **Pure-static completeness** (produced events + transports without construction) would require a new
  **declarative surface** — e.g. `[ProducesEvent("topic", typeof(Payload))]` and a transport-declaration
  attribute. A real lever if the Roslyn path ever needs to be complete on its own, but in-process
  construction makes it optional.

## Relationship to what exists

- **Reuses:** `Benzene.Mesh.Wire` (`MeshServiceDescriptor`, `MeshDescriptorFactory`, `MeshJson`),
  `Benzene.Schema.OpenApi` (`SpecBuilder`, `MeshSchemaGenerator`), `Benzene.Testing`
  (`BenzeneTestHost`), the Cloud Service Profile (`docs/specification/cloud-service-profile.md`).
- **Complements:** the dormant `Benzene.CodeGen.SourceGenerators` (the pure-static partial path).
- **Repositions, doesn't deprecate:** `Benzene.CodeGen.Terraform` becomes one opinionated reference
  renderer among several, not the strategic path.

## Recommendation

1. Add the **outbound-routing read-model** (the one new extraction capability).
2. Ship **`Benzene.Descriptor`** (MSBuild target) emitting `service.json` per service at build.
3. Provide a **reference `service.json` → Terraform** example generator (not a package).
4. Wire the per-service emit into **`MeshAggregator`** for the estate view.
5. Keep `Benzene.CodeGen.Terraform`; fix `docs/terraform.md`'s overclaims and re-position it.
