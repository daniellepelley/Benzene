# Mesh Enterprise Readiness — research

**Date:** 2026-08-10
**Status:** research complete; no implementation started. Product positions in §4–§6 are the
mesh-product-owner's, taken against the actual code; the architecture audit in §2 was verified
against `/workspace/benzene-dotnet` at the commit current on this date.

## 1. The question

In an enterprise setting the mesh must be easy to power off many different data sources
(CloudWatch today; an OTel store or Elasticsearch tomorrow). Auto-discovery of services may be
wanted — but an enterprise customer may *not* want it, because software that can enumerate a cloud
account is a security hole in a review; they may want to supply an explicit list instead. Today,
customizing the mesh means building a mesh server in code and changing the components that make it
up — a complicated task without internal Benzene knowledge. Wanted: two first-class paths —

- **(a) custom via code**, heavily customized; and
- **(b) vanilla-but-custom purely through configuration** — configuring the data sources, no C#.

And almost certainly required: **login** protecting the data, working with GitHub/Google (possibly
Facebook) — and with the customer's own SSO.

## 2. What exists today — the audit

The one-sentence finding: **the flexibility already exists in code; none of it is reachable from
configuration, and there is no door on the building.** The product task is not "build flexibility"
— it is *promote existing flexibility from code to configuration, and add auth*.

### 2.1 The ports

The mesh read path is built on eight interfaces. Six are genuinely pluggable today (interface +
multiple real implementations + DI seam):

| Port | Implementations | Verdict |
|---|---|---|
| `IMeshTraceSource` | X-Ray, Tempo, Jaeger | Best seam in the mesh; clean options-per-adapter |
| `IMeshUsageSource` | CloudWatch, Application Insights, in-memory collector | Multi-registered, merged, fetch-isolated |
| `IMeshArtifactStore` | Filesystem, S3, Azure Blob, GCS | Trivially swappable |
| `IMeshServiceSource` | HTTP, AWS Lambda Invoke | Already selected *by name* per registry entry — the pattern to generalize |
| `IMeshDiscoveryProvider` | AWS Lambda, Azure App Service, Kubernetes | Composes via `IEnumerable`, has a runner + filter |
| `IMeshFleetReadModel` | In-memory collector store, composite (trace + usage) | Handlers depend only on the interface |

### 2.2 Welded in practice

- **The composite fleet read model takes exactly one trace source** — X-Ray and Tempo cannot
  coexist. Usage sources got the `IEnumerable` treatment; trace sources did not.
- **Adapter extensions `AddSingleton` raw cloud clients unconditionally** (not `TryAdd`), so
  composing two adapters in one container is last-registration-wins.
- **No `IMeshIssueSource` port at all** — only the in-memory store can serve issues; the composite
  plane marks them permanently missing.
- **No topology port** — `TempoServiceGraphTopologyBuilder` is the only producer, concretely wired.
- **`ServiceAsync`/`TopicAsync` hardcoded to `null`** on the composite plane — two of the five
  query topics are dead on every non-push deployment.
- **Several options classes cannot be config-bound**: ctor-required args
  (`TempoTraceSourceOptions(string)`), and `MeshSelfReportOptions` takes delegates — structurally
  unbindable.
- **Artifact-serving middleware is copy-pasted five times** across the cloud examples (five
  distinct files), not packaged.
- **Zero `IConfiguration` usage inside any `src/Benzene.Mesh.*` package.** All configuration lives
  in hand-written host code.

### 2.3 The two assembly paths today

**Path (a) exists and works** — `examples/AwsMesh/Mesh/Startup.cs` is ~200 lines of expert wiring
(discovery + Lambda interrogation + S3 artifacts + CloudWatch usage + X-Ray fleet + UI + dispatch),
and there are four such hand-built mesh servers (AWS, Azure, Azure Functions, K8s). Each proves the
flexibility; none is reachable without internal Benzene knowledge.

**Path (b) exists as a thin slice** — `deploy/Mesh/Benzene.Mesh.Host` already binds `mesh.json` →
`MeshHostConfig`, already selects interrogation sources **by name** (`source: "Http" |
"AwsLambdaInvoke"` + `sourceOptions` dictionary), has a poll loop, and models the two-stage opt-in
for a dangerous capability (`EnableDispatch` + `DispatchAllowInProduction`). But it references only
aggregator + Lambda + UI + dispatch: **no usage source, no fleet/trace read model, no artifact
store beyond disk, no discovery, no collector, no auth is reachable from config.** Everything that
makes AwsMesh impressive is code-only.

### 2.4 Discovery today

Real cloud enumeration exists (AWS `ListFunctions`+`ListTags`; Azure ARM; K8s API), behind a
uniform provider port with a union-with-static-seed runner. Three notes that matter for the
enterprise posture:

- The vanilla `Benzene.Mesh.Host` has **no discovery reference at all** — currently an accident of
  incompleteness, and worth converting into a stated invariant (§4.2).
- The discovery filter is never configurable — every example calls `new MeshDiscoveryFilter()`.
- Discovery writes `registry.json`, but **no shipped code path reads it back**: the config route
  and the discovery route do not compose today.

### 2.5 Auth today

**None, anywhere in the mesh — as an explicit design position, not an oversight.** The docs and the
spec both say identity belongs to "the gateway in front" (`docs/mesh-ui.md`,
`cloud-service-profile.md` §4). The UI middleware serves the page unconditionally; the envelope
endpoint answering `mesh:query:*` is open in every example. The only guard rails are environment
gates on dispatch, not identity. `Benzene.Auth.Basic/Core/OAuth2` exist as pipeline packages
(Basic + JWT bearer validation), but no mesh package references them, and there is no interactive
login (OIDC authorization-code flow) anywhere.

"The gateway in front" is a defensible answer for a library. It is not a sufficient answer for a
product a customer is expected to deploy.

### 2.6 A spec fact that shapes everything

The five query topics (`benzene:mesh:query:fleet|service|topic|trace|correlation`) are
**deliberately not in the spec** — `mesh.md` §4 pins only the ingest topics and says the query
read models "join the spec if a second collector or third-party view needs them pinned." A shipped
configurable mesh host is exactly that second consumer. Freedom to reshape them ends when slice 1
ships; anything we want to change about the query contracts should be changed **before** then.

## 3. The industry bar

The two products enterprises will compare the mesh against solved these exact problems, the same way:

**Grafana** provisions data sources from YAML files — each source selected by `type` name with a
`jsonData` options block, secrets split into `secureJsonData`, env-var interpolation, and `prune`
semantics for removal ([provisioning docs](https://grafana.com/docs/grafana/latest/administration/provisioning/),
[data sources](https://grafana.com/docs/grafana/latest/datasources/)). Its auth is tiered:
built-in [generic OAuth/OIDC](https://grafana.com/docs/grafana/latest/setup-grafana/configure-access/configure-authentication/generic-oauth/)
covers Google/Okta/Entra/Keycloak with one config section;
[SAML is enterprise-tier only](https://grafana.com/docs/grafana/latest/setup-grafana/configure-access/configure-authentication/),
and the OSS answer to SAML is an **auth proxy** (oauth2-proxy, Authelia) trusted via a forwarded
header. That tiering is a market-tested answer to exactly our §5 question.

**Backstage** draws the discovery line where the owner drew it: the catalog is fed either by
**static locations declared in `app-config.yaml`** or by **opt-in discovery processors/entity
providers** per integration — and offers a `readonly` mode that disables dynamic registration
entirely, so config is the only source of truth
([catalog configuration](https://backstage.io/docs/features/software-catalog/configuration/),
[life of an entity](https://backstage.io/docs/features/software-catalog/life-of-an-entity/)).
Statically-declared locations cannot be removed through the API — only by editing config. That is
the "discovery proposes, config disposes" posture, productized.

Neither product loads arbitrary plugin code from configuration paths. Both keep "add a new source
*type*" as a code/package concern and "use a shipped source" as a config concern. That is the line
§4.3 adopts.

## 4. Positions (mesh-product-owner)

### 4.1 Ranking — what blocks adoption hardest

1. **Auth — an absolute veto.** The dashboard is a map of the entire estate (every topic, schema,
   health status — and with dispatch, a button that invokes real handlers). No security review
   passes an unauthenticated deployment. Without auth the product cannot leave localhost.
2. **Config-driven assembly — a skill veto.** "Write a Startup.cs referencing eight Benzene
   packages" filters the audience to approximately the framework's own authors.
3. **Source breadth — an estate-fit veto, but softer**: CloudWatch + Tempo + X-Ray + Jaeger +
   App Insights already cover many real estates.
4. **Discovery — blocks nothing.** The enterprise default is an explicit list anyway.

By *value* the order inverts at the top: config-driven assembly first (it multiplies every existing
and future source and converts the mesh from framework feature into product), then auth (gating but
undifferentiated), then sources, then discovery.

### 4.2 Discovery: explicit list by default; discovery physically separate

The default is an explicit, hand-written service list, and **the vanilla host must be physically
incapable of enumerating a cloud account — not "off by default"; absent.** In a security review,
"the flag is off" invites "who can turn it on?"; "the image contains no code path that calls
`ListFunctions` and its role needs no list permissions" ends the conversation.

Opt-in discovery becomes a **separate deployable** (`benzene-mesh-discovery`, a scheduled job/second
image) under its own least-privilege role, emitting an inspectable registry document the vanilla
host unions with static config (`registryDocuments`). The discovered list can be reviewed, diffed,
even gated through a PR before the mesh consumes it: *discovery proposes, config disposes*. The
seam already exists (`mesh-self-discovery-design.md`: discovery creates config; the aggregator
consumes it) — this packages it. The existing dispatch posture (off by default, second explicit
opt-in for production) is the house style for dangerous capabilities and the precedent to cite.

### 4.3 The configuration story

"Vanilla but custom through configuration" means: **one published container image (later a
`dotnet tool`), driven by one `mesh.json`, where every prebuilt component is selectable and
parameterizable by name** — the `MeshServiceSource` known-names pattern extended to the other
component axes. Target shape (names illustrative):

```jsonc
{
  "pollIntervalSeconds": 60,
  "artifactStore": { "type": "s3", "options": { "bucket": "…", "prefix": "…" } },  // file | s3 | azureBlob | gcs
  "services": [
    { "name": "orders",   "specUrl": "…", "healthUrl": "…" },
    { "name": "payments", "source": "AwsLambdaInvoke", "sourceOptions": { "functionName": "payments-fn" } }
  ],
  "registryDocuments": [ "s3://mesh/discovered.json" ],  // the discovery seam
  "usage":    [ { "source": "cloudwatch", "options": { "windowHours": 24 } } ],     // later: prometheus | elasticsearch
  "fleet":    { "source": "xray" },                       // xray | tempo | jaeger | collector | none
  "topology": { "source": "tempo", "options": { "prometheusUrl": "…" } },
  "dispatch": { "enabled": false },
  "auth":     { "mode": "none" }                          // none | proxy | basic | oidc
}
```

Operational qualities are part of the product: unknown names **fail fast at startup listing the
known values**; a `--validate-config` mode; effective-config printing. Time-to-understanding is the
UI's quality bar; time-to-first-successful-boot is the host's.

**Deliberately code-only, stated up front:**

- Implementing a new component *type* (`IMeshTraceSource`, `IMeshUsageSource`, …). Config selects
  from the shipped catalog; it never defines behavior.
- **No assembly-loading plugins from config paths.** A `"plugin": "/mnt/custom.dll"` mechanism is a
  security hole in exactly the product being hardened, and a support tar pit. The extensibility
  story for proprietary backends is path (a) — repositioned as first-class: the host is ~200 lines,
  so "copy `deploy/Mesh/Benzene.Mesh.Host`, add your `AddXxx()` call" is documented as *the* way to
  go custom. Config catalog and code path share the same extension methods, so a custom host stays
  small.
- Composition/judgment logic (snapshot building, drift rules) and anything spec-pinned.
- **Credentials never live in `mesh.json`** — config names endpoints and options; secrets come from
  the environment/secret stores (the host's existing AWS-credential-chain stance, generalized;
  Grafana's `jsonData` / `secureJsonData` split is the same lesson).

### 4.4 Auth: four tiers, two table stakes, two declined

| Tier | What | Position |
|---|---|---|
| 0 | None | Stays the default for localhost/dev; the five-minute `docker run` demo must not break. Log a visible note when bound to non-loopback. |
| 1 | **Reverse-proxy-delegated** (`auth: { mode: "proxy" }`) | **Table stakes; ship first.** Many enterprises will *insist* their oauth2-proxy / ALB+Cognito / Azure App Proxy front door does login regardless. Nearly free: a documented pattern + trusting a forwarded-identity header from configured proxies so the UI can show who is signed in. Highest credibility per unit effort — and the correct answer to SAML. |
| 2 | Shared secret / Basic | Cheap (`Benzene.Auth.Basic` exists); legitimate for staging. Never marketed as the enterprise answer. |
| 3 | **OIDC login** (authorization-code + cookie session, ASP.NET Core's built-in handler) | **Table stakes for the standalone sell.** One config-driven implementation (authority, clientId, secret ref, allowed domains/groups) covers Google, Okta, Entra ID, Auth0, Keycloak — **social login and customer SSO are the same feature** when the IdP is configurable. GitHub needs a small OAuth2-not-quite-OIDC accommodation; worth it for the developer audience. |
| — | Facebook | **Declined.** Not an enterprise credential; no audience job behind it; pure review surface. |
| — | SAML | **Declined (buy-not-build).** Every IdP that matters bridges SAML→OIDC, and Tier 1 covers the rest. Grafana's OSS/enterprise split validates this exact line. |

OIDC is **host-side plumbing, not new `Benzene.Auth.*` packages** — `Benzene.Auth.OAuth2` validates
bearer JWTs on the message pipeline; interactive browser login is a different mechanism and belongs
in the deployable.

**Authorization scope for v1:** authenticated → full read access, plus allowed-domains/groups. No
per-service RBAC (scope creep). The one early read/write distinction worth having: when auth is on
and dispatch is enabled, **gate `mesh:dispatch` on a configured role/group claim** — "who may fire
the button" is the first question a reviewer asks.

**What login means for the single-HTML UI: almost nothing, by design.** Auth sits in the host
pipeline *before* `UseMeshUi`/artifact/envelope middleware; the browser logs in before the page
loads, and every fetch is already same-origin (the static floor's no-external-requests rule pays
off — cookies just flow). UI changes are progressive enhancement only: a 401 on a background fetch
→ full-page reload; optionally an identity/logout chip when a `whoami` endpoint is present.
`Benzene.Mesh.Ui` stays auth-free and statically hostable; the static floor is untouched.

## 5. Spec guardianship — essentially none of this enters the spec

`mesh.md` defines what services and collectors say **on the wire**. How a mesh *server* is
assembled, configured, secured, or discovered is a host/deployment concern. Explicitly not
entering the spec:

- **The `mesh.json` vocabulary and source names** — a .NET-host idiom still in flux. Revisit only
  if a second language port ships a mesh host and cross-port config portability becomes a
  demonstrated user job.
- **Any normative auth requirement** — untestable by conformance fixtures, and it would constrain
  legitimate deployments (an air-gapped internal mesh runs open). Fails the tautness test.
- **Discovery mechanics** — adapter territory by definition.

Two narrow items to pursue:

- A short **informative** "Security considerations" paragraph in `mesh.md`: descriptors and
  collector feeds reveal estate structure and contracts; deployments should restrict access to mesh
  endpoints and read models. No conformance impact.
- A real gap this research surfaced: the **usage counter convention**. The CloudWatch source reads
  `benzene.messages.processed` back from the metrics store, but that name appears nowhere in the
  spec — it is a .NET OTel convention. If other ports name their counter differently, every
  metrics-store usage source fractures per language. Not mesh-spec material (the spec-native usage
  signal is TraceEvent counting), but the cross-port metric name needs a documented home → route to
  observability-product-owner as a data requirement.

## 6. Roadmap — five independently shippable slices

> **Build instructions exist.** Each slice below has a self-contained implementation brief in the
> .NET repo at
> [`benzene-dotnet/work/enterprise/`](https://github.com/daniellepelley/benzene-dotnet/tree/main/work/enterprise),
> written to be picked up cold — exact file paths, current code quoted verbatim, a verification
> command per task, and a do-not list. A **slice 0** was added there, extracting the engineering
> pre-work below into a standalone first pickup. Slice 4 is marked design-first and deliberately not
> buildable from its brief alone.

1. **Host source catalog + config schema v1** *(first slice; pure promotion of existing code to
   config, no new packages).* Extend `MeshHostConfig`/`mesh.json` with `artifactStore`, `usage`,
   `fleet`, `topology`; fail-fast unknown names; `--validate-config`; publish the container image;
   README with a per-source least-privilege IAM matrix. **Acceptance test: reproduce
   `examples/AwsMesh` capability-for-capability from config alone.** This slice alone delivers
   path (b) for everything Benzene can already do.
2. **Auth in the host**: `auth.mode` = `proxy` and `oidc` (+ `basic`); allowed-domains/groups;
   dispatch gated on role; UI 401-reload + identity chip. Also formally document path (a) as "copy
   the Host" with a worked custom-source example.
3. **Discovery as a separate deployable**: `benzene-mesh-discovery` job/image writing the registry
   document; `registryDocuments` union in the vanilla host; record "the vanilla host contains no
   discovery code" as a stated product invariant.
4. **New sources**: Prometheus/OTel-store usage and Elasticsearch, designed with
   observability-product-owner — including settling the cross-port usage-metric convention and
   finally verifying the Tempo metric/label names against a real backend (standing caveat: never
   verified live).
5. **Packaging polish**: `dotnet tool`, Helm chart, effective-config printing, config reference.

### Engineering pre-work slice 1 depends on (from the audit)

(a) config-bindable mirrors of adapter options POCOs (several are ctor-immutable);
(b) `TryAddSingleton` for the raw cloud clients so adapters compose;
(c) `CompositeMeshFleetReadModel` takes `IEnumerable<IMeshTraceSource>`;
(d) an `IMeshIssueSource` port (issues are welded to the in-memory store);
(e) promote the five copy-pasted artifact middlewares into a package;
(f) resolve the assembly-scoped handler-discovery collision (`benzene:mesh:aggregate`) properly;
(g) revisit `ServiceAsync`/`TopicAsync` returning hardcoded `null` on the composite plane — and do
any reshaping of the `mesh:query:*` contracts **before** slice 1 makes them de-facto pinned (§2.6).

### Cross-PO decisions needed

- **aws-product-owner**: discovery-job deployment shape; IAM matrix; ECS/App Runner discovery ever?
- **observability-product-owner**: slice-4 sources; the usage-metric name convention.
- **performance-champion**: re-affirm per-source fetch isolation as sources multiply under config —
  a misconfigured Elasticsearch endpoint must never stall the catalog.
- **dx-champion**: config-validation UX and first-boot experience.

## 7. Sources

- Audit: `/workspace/benzene-dotnet` — `src/Benzene.Mesh.*` (20 projects),
  `deploy/Mesh/Benzene.Mesh.Host`, `examples/{AwsMesh,K8sMesh,AzureMesh,AzureFunctionsMesh,GoogleCloudMesh,Mesh}`,
  `work/{mesh-self-discovery-design,auth-middleware-design,service-mesh-roadmap-1.0}.md`.
- Spec: `docs/specification/mesh.md`, `docs/specification/cloud-service-profile.md`.
- Industry: [Grafana provisioning](https://grafana.com/docs/grafana/latest/administration/provisioning/) ·
  [Grafana data sources](https://grafana.com/docs/grafana/latest/datasources/) ·
  [Grafana authentication](https://grafana.com/docs/grafana/latest/setup-grafana/configure-access/configure-authentication/) ·
  [Grafana generic OAuth](https://grafana.com/docs/grafana/latest/setup-grafana/configure-access/configure-authentication/generic-oauth/) ·
  [Backstage catalog configuration](https://backstage.io/docs/features/software-catalog/configuration/) ·
  [Backstage life of an entity](https://backstage.io/docs/features/software-catalog/life-of-an-entity/)
