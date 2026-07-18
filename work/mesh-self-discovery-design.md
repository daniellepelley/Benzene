# Mesh Self-Discovery — Design Proposal (2026-07-18)

**Status:** APPROVED (2026-07-18) — decisions recorded in §0.1; implementation starts with the AWS
offering. §3.1 is superseded by the §0.1 "discovery creates config, aggregator consumes it" seam.

## 0.1 Decisions (2026-07-18)

1. **AWS enumeration:** `AWSSDK.Lambda` `ListFunctions` + per-function tags only (no
   `AWSSDK.ResourceGroupsTaggingAPI` — **zero new AWS dependency**). May add the tagging-API option
   later as an alternative.
2. **Real adapter packages** (`Benzene.Mesh.Discovery.*`), not copy-paste — as recommended.
3. **Union** discovered services with static config (static still pins un-discoverable services).
4. **Discovery creates config; the aggregator consumes it at runtime — a hard seam.** Discovery is a
   *separate phase* that writes a `mesh.json`-shaped registry document (to a file / S3 / blob); the
   existing aggregator reads that document at runtime exactly as it reads a hand-written `mesh.json`.
   This is cleaner than the §3.1 in-process union: discovery and runtime monitoring are decoupled,
   independently hostable, and the generated document is inspectable. The union (decision 3) is
   between the hand-written static config and the discovery-generated document at aggregator load.
5. **Plug-and-play hosting:** the *same* aggregator + UI components run in every host; only the
   discovery provider and the config/artifact store backend swap per platform.
6. **Mesh runtime is .NET-only for now.** The *services* it discovers are multi-language (the Go and
   TypeScript ports), but re-implementing the mesh runtime per language would be duplication — out of
   scope. The mesh is configured entirely via **JSON config**.
7. **Build order:** implement AWS end-to-end first, learn from it, then copy the shape for Azure and
   Kubernetes. The final deliverable is an **end-to-end AWS test**: deploy test Benzene Lambdas + a
   mesh service (itself an AWS Lambda) and watch discovery → interrogation → catalog work; ideally
   with .NET, TypeScript, and Go Lambdas all integrated with one mesh, subject to port maturity.
   **This E2E test is delivered last.**

## 0. The ask (restated from the brief)

Make the Mesh a stronger, more "fully featured" offering without becoming so opinionated it can't be
widely used, by giving it **self-discovery**: a mesh service that runs *inside* a cloud account,
introspects what's running, and interrogates each service for what it supports — instead of being
hand-fed a service list.

Concretely:
1. A mesh service hostable in AWS (e.g. a Lambda) with permission to introspect the account —
   **enumerate the Lambdas that exist**, then **ping each** to see whether it speaks the Cloud
   Service spec and which parts. Self-discovering; nothing coded up front.
2. Prefer **Lambda-to-Lambda** interrogation (a small JSON message in, a JSON manifest out) over HTTP
   where possible.
3. The **same mechanism in Azure** — enumerate Function Apps / App Services and interrogate them.
4. **Tag-based filtering** on both clouds — a `benzene` tag as the default (user-overridable) filter,
   stamped by Terraform/deploy templates by default.
5. The **same for Kubernetes** — discover services by label and interrogate them.
6. Once a service is found, **pull its specification** (over HTTP or the native transport), so we know
   its schemas, transports, health, and what it's connected to.
7. Net result: host a mesh on each platform with a **per-cloud adapter** that self-discovers running
   services and interrogates them for schemas, transports, health, and connectivity.

## 1. Headline: most of this already exists — the gap is discovery

The good news, verified against `main`, is that the hard parts are already built. This is a
**well-bounded addition on top of proven seams**, not a rebuild.

| Capability the ask needs | Status today | Where |
|---|---|---|
| A "describe yourself" protocol (small JSON in → manifest out) | **Exists** | Reserved `mesh` topic → `MeshServiceDescriptor` |
| That describe being **transport-native** (not HTTP-only) | **Exists by design** | Routed on transport-neutral `BenzeneMessageContext` by topic id; same `{topic,headers,body}` envelope flows over direct Lambda Invoke |
| Interrogating a **known** service by Lambda-Invoke | **Exists** | `Benzene.Mesh.Aws.Lambda`'s `LambdaMeshServiceSource` + `Benzene.Clients.Aws`'s Lambda-Invoke client |
| Interrogating a **known** service over HTTP | **Exists** | `HttpMeshServiceSource` |
| A pluggable "how to interrogate" seam | **Exists** | `IMeshServiceSource` (keyed `http` / `aws-lambda`, `IEnumerable<>` DI) |
| Aggregating descriptors → catalog/topology/health artifacts + UI | **Exists** | `MeshAggregator` → `IMeshArtifactStore` → `Benzene.Mesh.Ui` |
| A black-box conformance probe ("what parts of the spec?") | **Exists** | `CloudServiceProbe` (R1–R8 over HTTP) |
| **Enumerating what services exist in a cloud** | **MISSING** | The service list is a static `mesh.json` → `MeshServiceRegistry` singleton |

The mesh roadmap already anticipated this exact gap and **deferred it**: `work/service-mesh-roadmap-1.0.md`
§4.4 ("cloud-API discovery… add real complexity… recommend deferring past v1") and §6 open question 2.
This design is that deferred piece.

### 1.1 The two mesh subsystems (important context)

There are two parallel "mesh" pipelines; a discovery adapter targets the **pull** side:

- **Pull / aggregator** (`Benzene.Mesh.Aggregator` + `.Contracts` + `.Reporting` + `.Aws.Lambda` +
  `deploy/Mesh`): an aggregator polls a registry of services, interrogates each via an
  `IMeshServiceSource`, and writes `manifest.json` / `services/*.json` / `topology.json` to a blob
  store the UI reads. **This is where self-discovery plugs in.**
- **Push / wire+collector** (`Benzene.Mesh.Wire` + `Benzene.Mesh.Collector`): services *self-register*
  and heartbeat (`mesh:register`/`mesh:heartbeat`/`mesh:traces`) to a collector that derives topology
  edges from trace parentage. This is already a form of self-discovery (by push); the intended bridge
  between the two is an open extension point.

The ask is **active pull discovery** ("look at all the Lambdas, ping them"), so the pull side is the
target. §9 covers how the richer wire descriptor and the collector's observed edges feed in.

## 2. The describe protocol, formalized

The reserved **`mesh`** topic (`MeshTopics.Descriptor = "mesh"`) returns a `MeshServiceDescriptor`
(`Benzene.Mesh.Wire`). Send the wire envelope, get the manifest:

```
→  { "topic": "mesh", "headers": {}, "body": "{}" }
←  { "statusCode": "OK", "headers": {}, "body": "<MeshServiceDescriptor JSON>" }
```

`MeshServiceDescriptor` fields (camelCase, nulls omitted):
`service`, `serviceVersion`, `instanceId`, `runtime` (default `"dotnet"`), `binding`,
`placement { cloud, region? }`, `topics[] { id, version?, requestSchema?, responseSchema? }`,
`descriptorHash` (`sha256:…` over canonical JSON), `degraded[]`, `profile { name, missing[]? }`
(the conformance self-assessment — the "what parts of the spec do I support" answer, built from
`CloudServiceProfileReport`).

Two facts make the "Lambda-to-Lambda" ask nearly free:
- The descriptor interception (`UseMeshDescriptor<TContext>`) is **generic and transport-neutral** —
  it routes purely by topic id on `BenzeneMessageContext`.
- The `{topic,headers,body}` envelope is the documented transport-neutral format (`wire-contracts.md`),
  and a Benzene Lambda already deserializes exactly this shape from a **direct Invoke** payload
  (`BenzeneMessageLambdaHandler`, matches any payload with a `topic`) and runs it through the same
  pipeline as HTTP.

### 2.1 The one small gap in the describe path

`Benzene.CloudService.UseBenzeneCloudService` currently mounts the `mesh` descriptor **only over
HTTP** (it's `IHttpContext`-constrained, at `/benzene/invoke`). So a Lambda that's SQS/queue-triggered
(no HTTP surface) answers `spec`/`healthcheck` on a direct Invoke (those are registered topic
handlers) but does **not** answer `mesh` unless `UseMeshDescriptor` is also composed onto its
BenzeneMessage/Lambda pipeline.

**Proposed P0 fix (small):** make the CloudService wiring register the `mesh` descriptor on the
transport-neutral BenzeneMessage pipeline (not just the HTTP one), so **every** Benzene service — HTTP
or invoke-only — answers `{topic:'mesh'}` over its native transport. This is a wiring change in
`Benzene.CloudService`, reusing the already-generic `UseMeshDescriptor<TContext>`; no new mechanism.
(Note: `LambdaMeshServiceSource` today asks for `spec`+`healthcheck`, not `mesh`; §6 covers aligning
interrogation on the richer `mesh` descriptor.)

## 3. Core design: the discovery seam

Introduce one new port, mirroring the proven `IMeshServiceSource` adapter pattern:

```csharp
namespace Benzene.Mesh.Contracts;

/// Enumerates the services that currently exist in some environment (a cloud account, a cluster),
/// producing the registry entries the aggregator polls — instead of a hand-written mesh.json.
public interface IMeshDiscoveryProvider
{
    string Key { get; }   // "aws-lambda", "azure-appservice", "kubernetes" — mirrors IMeshServiceSource.Key
    Task<IReadOnlyList<MeshServiceRegistryEntry>> DiscoverAsync(
        MeshDiscoveryFilter filter, CancellationToken cancellationToken = default);
}

public class MeshDiscoveryFilter        // the tag/label filter (see §5)
{
    public IReadOnlyDictionary<string, string?> Tags { get; init; }  // default: { ["benzene"] = null }  (present, any value)
    public IReadOnlyList<string>? Regions { get; init; }
    public string? Namespace { get; init; }   // k8s
}
```

Each `DiscoverAsync` result is an ordinary `MeshServiceRegistryEntry` (`Name`, `Source`,
`SourceOptions`, optional `SpecUrl`/`HealthUrl`) — i.e. a discovered Lambda comes back bound to the
existing `aws-lambda` source with `SourceOptions["functionName"]` set, a discovered Function App comes
back bound to the `http` source with its URL, a discovered k8s Service comes back bound to `http` with
its in-cluster DNS. **Interrogation is entirely unchanged** — discovery only decides *which* services
exist; the existing `IMeshServiceSource` decides *how to reach* each one.

### 3.1 Where it plugs in (composition, not replacement)

Today `MeshServiceRegistry` is built eagerly in `Startup` and injected as a singleton, then passed to
`MeshAggregator.RunOnceAsync(registry)` on each poll. The change:

- Replace the static singleton with an `IMeshServiceRegistrySource` resolved **per poll**, whose
  default implementation returns the static `mesh.json` entries (unchanged behavior when no discovery
  provider is registered).
- When discovery providers are registered, the registry source **unions** the static entries with
  `provider.DiscoverAsync(filter)` across all providers, de-duplicated by `Name`. Static config stays
  useful for pinning a service discovery can't see (a different account, an on-prem box).
- `MeshPollBackgroundService` / `MeshAggregateMessageHandler` call the registry source each pass, so
  discovery re-runs every poll — new services appear and retired ones drop off automatically. That
  *is* the "self-discovery, no coding beforehand" behavior.

This keeps `MeshAggregator` itself unchanged (it still takes a resolved registry), and keeps the
"discovery is optional; static config still works" property.

## 4. Per-cloud discovery adapters

Each adapter is its own thin package (the established `Benzene.Mesh.*` adapter-package pattern), so a
deployment pulls in only the cloud SDK it actually uses. Interrogation reuses existing sources.

### 4.1 AWS — `Benzene.Mesh.Discovery.Aws`
- **Enumerate:** list Lambda functions in the account/region, filtered by tag. Two options (a real
  decision — see §11): (a) **zero new dependency** — `AWSSDK.Lambda` (already approved) `ListFunctions`
  + `ListTags`/`GetFunction` per function; simple, but N+1 tag calls; or (b) **efficient** —
  `AWSSDK.ResourceGroupsTaggingAPI` `GetResources(TagFilters, ResourceType=lambda:function)` returns
  only tagged ARNs in one paged call (new NuGet).
- **Bind:** emit `aws-lambda` source entries with `SourceOptions["functionName"]` = the discovered
  name → interrogated by the existing `LambdaMeshServiceSource` (direct Invoke, no HTTP, no public URL
  needed). **This is the Lambda-to-Lambda path the brief wants, and it already works for a named
  function.**
- **Where the mesh runs:** a scheduled Lambda (EventBridge rule) or Fargate task; reuses the aggregator
  + artifact store (S3) + UI.
- **IAM (least privilege):** `tag:GetResources` (option b) or `lambda:ListFunctions`+`lambda:ListTags`
  (option a); `lambda:InvokeFunction` (scoped to the `benzene`-tagged resource group) for
  interrogation; `s3:PutObject` for the artifact store. STS (`AWSSDK.SecurityToken`, already present)
  if resolving account id or assuming cross-account roles.
- **Scale/scope:** paginate `ListFunctions`/`GetResources`; make region(s) and (optionally) a list of
  assume-role ARNs configurable for multi-region / multi-account estates.

### 4.2 Azure — `Benzene.Mesh.Discovery.Azure`
- **Enumerate:** Function Apps and App Services carrying the tag, via Azure Resource Manager. Efficient
  path is **Azure Resource Graph** (`Azure.ResourceManager.ResourceGraph`): one KQL query
  `resources | where type in~ ('microsoft.web/sites') and tags['benzene'] != ''` returns every tagged
  app across subscriptions. Alternative: `Azure.ResourceManager.AppService` enumerate + client-side tag
  filter. Auth via `Azure.Identity`'s `DefaultAzureCredential` — **already a repo dependency** — so a
  managed identity on the mesh's host needs no secrets.
- **Bind:** Azure has no cheap "invoke function by ARM" equivalent to Lambda Invoke, so interrogate over
  **HTTP** — emit `http` source entries pointing at `https://{app}.azurewebsites.net/benzene/invoke`
  (POST the `{topic:'mesh'}` envelope) and `/benzene/health`. For function-key-protected apps, the
  adapter resolves the host key via ARM (`ListHostKeys`) and passes it in `SourceOptions` as a header
  the `http` source attaches (a small extension to `HttpMeshServiceSource` to honor per-entry headers).
- **Where the mesh runs:** a timer-triggered Azure Function or container app; artifact store = Azure
  Blob.
- **Permissions:** a **Reader** role on the target scope for enumeration; `listHostKeys` action if
  reading function keys; Blob write on the artifact container. Managed identity, no secrets.

### 4.3 Kubernetes — `Benzene.Mesh.Discovery.Kubernetes`
- **Enumerate:** list `Service`s (and/or `Deployment`s/`Pod`s) matching a label selector
  (`benzene=true` by default) via the K8s API, using in-cluster config (the mounted service-account
  token) — `KubernetesClient` NuGet (new).
- **Bind:** emit `http` source entries at the cluster-internal DNS
  `http://{service}.{namespace}.svc.cluster.local:{port}/benzene/invoke` (+ `/benzene/health`).
- **Where the mesh runs:** an in-cluster `Deployment` (continuous) or `CronJob` (periodic); artifact
  store = a `PersistentVolume`, S3, or Azure Blob.
- **RBAC (least privilege):** a `Role`/`ClusterRole` granting `list`/`watch` on `services` (and
  `deployments`/`pods` if used), bound to the mesh's service account. Nothing that can mutate.

### 4.4 Common shape
All three: `AddMesh{Aws,Azure,Kubernetes}Discovery(filter)` DI extension registers the provider as an
additional `IMeshDiscoveryProvider` (multi-provider `IEnumerable<>`, exactly like `IMeshServiceSource`).
A single mesh host can register several (e.g. AWS + K8s) and get one unified catalog.

## 5. Tagging / labelling convention

- **Default filter:** presence of a **`benzene`** tag (AWS/Azure resource tag) or **`benzene`** label
  (k8s). Value ignored by default (`{ "benzene": null }` = "tag present, any value"); a specific value
  is a user override (`{ "benzene": "prod" }`), as is any additional tag in `MeshDiscoveryFilter.Tags`.
- **Optional per-service hint tags** (read into `SourceOptions`, all optional — discovery works with
  just `benzene`):
  - `benzene:transport` = `lambda` | `http` — override the interrogation source for that resource.
  - `benzene:mesh-path` = a non-default invoke path.
  - `benzene:team` / `benzene:system` — ownership/grouping surfaced in the UI.
- **Terraform / deploy defaults:** ship the tag by default in the repo's IaC/templates so a service is
  discoverable the moment it's deployed. Candidates to update: `Benzene.Templates` starter projects,
  any `deploy/` IaC, and the CodeGen Terraform emitters (`Benzene.CodeGen.*` already emit Terraform —
  the EventBridge-rule emitter is referenced in history). This is the "self-discovery, no coding
  beforehand" promise made real: deploy with the tag → it shows up.
- **Keep it un-opinionated:** the tag *key* and filter are fully configurable; `benzene` is only the
  default. A team already using their own tag taxonomy points the filter at it.

## 6. Interrogation depth — from "it exists" to "here's what it supports"

Once discovery yields an entry, the aggregator interrogates it. Proposed ladder (each step optional,
richer):
1. **Descriptor** (`mesh` topic) — service identity, topics + request/response schemas, placement,
   conformance `profile`, contract hash. This is the primary manifest and answers "what parts of the
   spec does it support?" directly. Requires the P0 fix (§2.1) + aligning `LambdaMeshServiceSource`
   to ask for `mesh` (it currently asks `spec`/`healthcheck`).
2. **Spec** (`/benzene/spec` or `{topic:'spec'}`) — the fuller OpenAPI/AsyncAPI document, when the full
   schema catalog is wanted.
3. **Health** (`{topic:'healthcheck'}`) — healthy/unhealthy + (per roadmap §4.1, an additive
   enhancement) structured `Dependencies` (`{Kind, Name, Criticality}`) that yield "what is this
   connected to" **structural** edges — currently `TopologyEdgeSource.structural` is defined but
   produced by nothing.
4. **Conformance** (optional) — run `CloudServiceProbe` for a full R1–R8 assessment of a service the
   descriptor says should conform.

"What else it's connected to and whether it's healthy" is thus answered by (3) structural edges +
(where a tracing backend exists) the collector's trace-derived edges — the two-subsystem bridge noted
in §1.1.

## 7. Where the mesh service runs (per platform)

The aggregator, artifact store, and UI are unchanged across all of these; only the discovery provider
and artifact-store backend differ:

| Platform | Mesh host | Discovery | Interrogation | Artifact store |
|---|---|---|---|---|
| AWS | Scheduled Lambda / Fargate | `aws-lambda` (tags) | Lambda Invoke | S3 |
| Azure | Timer Function / Container App | `azure-appservice` (ARM/RG) | HTTP | Azure Blob |
| Kubernetes | Deployment / CronJob | `kubernetes` (labels) | in-cluster HTTP | PV / S3 / Blob |
| Local/dev | `deploy/Mesh` Docker host | static `mesh.json` (today) or any provider | HTTP / Lambda | filesystem |

## 8. Security (must-address, not optional)

Active discovery + invoke sharpens an existing gap: **`/benzene/invoke` and the `mesh` topic are
unauthenticated today** (flagged in `work/auth-middleware-design.md` §6). A self-discovering mesh that
invokes every service in an account makes the describe/invoke path a more attractive target and gives
the mesh broad reach.

- **Inbound (services):** recommend the mesh describe path be authenticatable. On AWS, direct
  `lambda:InvokeFunction` is already IAM-gated — the mesh's role is the authorization boundary, which
  is clean. On HTTP transports, services can require auth on `/benzene/invoke` (the shipped
  `Benzene.Auth.*` — e.g. an internal service-to-service scope/role, or an API key), and the mesh's
  `http` source attaches the credential from config/secret store (`Benzene.Configuration.Core`).
- **The mesh's own credentials:** least-privilege per §4 (IAM role / managed identity / k8s SA), all
  read-and-invoke only, no mutation.
- **Blast radius:** discovery is read-only; the only "write" is invoking the describe topic, which is
  side-effect-free by contract. Keep it that way — the mesh must only ever send `mesh`/`spec`/
  `healthcheck`, never arbitrary topics.

## 9. How this strengthens the offering (and stays un-opinionated)

- **Zero-config catalog:** deploy a tagged service → it appears in the mesh, with its schemas,
  transports, health, conformance, and (via traces) its edges. No `mesh.json` upkeep.
- **Cross-platform:** one mesh concept, per-cloud adapters at the edge — the same dependency discipline
  as the rest of Benzene (thin adapters, transport-agnostic core).
- **Opt-in and overridable:** discovery is additive to static config; the tag key/filter is
  configurable; a team can use HTTP-only, Lambda-only, or mix. Nothing forces a topology on the user.
- **Reuses everything:** the descriptor, envelope, sources, aggregator, artifact store, UI, and probe
  are all already built and tested.

## 10. Phasing (proposed build order)

- **P0 — Transport-native describe (small).** Wire the `mesh` descriptor onto the transport-neutral
  BenzeneMessage pipeline in `Benzene.CloudService` so invoke-only services answer `{topic:'mesh'}`;
  align `LambdaMeshServiceSource` to fetch `mesh`. Unblocks Lambda-to-Lambda describe end-to-end.
- **P1 — Discovery seam.** `IMeshDiscoveryProvider` + `MeshDiscoveryFilter` in `Benzene.Mesh.Contracts`;
  the per-poll registry source that unions static + discovered; wire it into the host + aggregate
  handler. No cloud SDK yet — unit-test with a fake provider.
- **P2 — AWS adapter** (`Benzene.Mesh.Discovery.Aws`) — the highest-value first cloud, since the invoke
  interrogation already exists. Pick the enumeration option (§11.1).
  **Status: P1+P2 discovery engine landed (2026-07-18).** `IMeshDiscoveryProvider` + `MeshDiscoveryFilter`
  + `MeshDiscoveryRunner` (union, seed-wins) + `MeshRegistryJson` (the discovery→config seam) in
  `Benzene.Mesh.Contracts`; `AwsLambdaDiscoveryProvider` (`ListFunctions`+`ListTags`, tag-filtered) +
  `AddMeshAwsLambdaDiscovery` in `Benzene.Mesh.Discovery.Aws`; 9 tests. **Still open for AWS:** the
  runnable mesh-host wiring (discovery Lambda → write registry JSON to S3 → aggregator reads it) and
  the E2E test (delivered last).
- **P3 — Tagging convention + IaC defaults** — document the convention; stamp `benzene` in
  `Benzene.Templates` and the Terraform emitters.
- **P4 — Azure adapter** (`Benzene.Mesh.Discovery.Azure`) — ARM/Resource Graph enumeration; per-entry
  header support on `HttpMeshServiceSource` for function keys.
- **P5 — Kubernetes adapter** (`Benzene.Mesh.Discovery.Kubernetes`).
- **P6 — Enrichment** — structured health `Dependencies` → structural edges (roadmap §4.1); optional
  conformance-probe integration; optional collector/wire bridge for trace edges.

Each phase is independently shippable and testable; P0–P2 deliver the headline AWS self-discovery.

## 11. NuGet dependency decisions (need sign-off — AGENTS.md gates new deps)

1. **AWS enumeration** — choose one:
   - **(a) No new dependency:** `AWSSDK.Lambda` (already approved) `ListFunctions` + per-function tag
     reads. Simplest; more API calls on large accounts.
   - **(b) `AWSSDK.ResourceGroupsTaggingAPI`** (new) — one paged, tag-filtered call for all resources;
     scales better and generalizes beyond Lambda. **Recommended** if accounts are large.
2. **Azure enumeration** — `Azure.ResourceManager.ResourceGraph` (recommended, efficient KQL) and/or
   `Azure.ResourceManager.AppService` (new). `Azure.Identity` is **already present** (no new auth dep).
3. **Kubernetes** — `KubernetesClient` (new).

All three cloud adapters are also, like the A.5/A.6 cloud pieces, candidates for the "**ship the seam +
document the adapter as copy-paste**" treatment if you'd rather not take the SDK dependencies into the
repo — the `IMeshDiscoveryProvider` interface is tiny. My recommendation differs here: discovery
adapters have real, non-trivial pagination/auth logic worth shipping and testing as packages (unlike a
3-line secret-store adapter), so I'd lean to real packages — but it's your call, and it's the same
trade-off you decided for A.5/A.6.

## 12. Open questions / decisions

1. **Enumeration deps** — §11.1/11.2/11.3 choices.
2. **Packages vs documented adapters** — real `Benzene.Mesh.Discovery.*` packages (recommended) vs
   copy-paste adapters over the shipped seam (the A.5/A.6 precedent).
3. **Discovery = union or replace?** Recommend **union** (static config still pins un-discoverable
   services). Confirm.
4. **Pull-only, or also bridge the wire/collector?** Recommend pull-first (matches the ask); treat the
   collector bridge as later enrichment (P6). Confirm.
5. **Descriptor over invoke — P0 scope.** OK to change `Benzene.CloudService` wiring so invoke-only
   services answer `mesh` natively (a small, additive change to a shipped package)?
6. **Securing the describe path** — is per-service auth on `/benzene/invoke` in scope now, or is the
   IAM/managed-identity/RBAC boundary on the mesh's own credentials sufficient for v1? (Recommend: rely
   on the cloud IAM boundary for v1; document optional per-service auth.)
7. **Multi-account / multi-subscription / multi-cluster** — v1 single-scope with configurable
   region(s)/namespace, or design cross-account assume-role in from the start? (Recommend: single scope
   in v1, leave a `Regions`/assume-role hook in the filter.)

## 13. Explicitly out of scope

- Becoming a tracing backend (integrate, don't replace — unchanged from the mesh roadmap §9).
- Mutating discovered resources (discovery is strictly read + describe-invoke).
- A new UI (reuse `Benzene.Mesh.Ui`).
- Auto-remediation / alerting.
