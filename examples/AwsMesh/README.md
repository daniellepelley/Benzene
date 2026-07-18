# AWS Mesh Self-Discovery — end-to-end example

A deployable AWS example that proves the Benzene mesh **self-discovery** story end to end: three
Benzene Cloud Services running as Lambdas, plus a **mesh service** (a fourth Lambda) that discovers
them by tag, interrogates each, and serves the Mesh UI — all fronted by API Gateway so you can open
the UIs in a browser.

See `work/mesh-self-discovery-design.md` for the design this example exercises.

## Architecture

```
                    API Gateway (HTTP API, public)
   ┌──────────────┬──────────────┬───────────────┬──────────────┐
   │ /orders/*    │ /payments/*  │ /shipping/*   │ /mesh/*       │
   ▼              ▼              ▼               ▼
 orders-api    payments-api   shipping-api     mesh            ← 4 Lambdas
 (Cloud Svc)   (Cloud Svc)    (Cloud Svc)      (discovery +
   │              │              │              aggregator + UI)
   │ tag: benzene │ tag: benzene │ tag: benzene    │
   └──────────────┴───────┬──────┴─────────────────┘
                          │  1. ListFunctions + ListTags  (discover benzene-tagged Lambdas)
                          │  2. Invoke each ({topic:'spec'|'healthcheck'})  (interrogate)
                          ▼
                    ┌───────────┐
                    │    S3     │   registry.json  (discovered config)
                    │  bucket   │   manifest.json / services/*.json / topology.json  (catalog)
                    └───────────┘
                          ▲
                          │  Mesh UI reads the catalog artifacts
```

- The three **service Lambdas** are full Cloud Service Profile (R1–R8) services: `/benzene/invoke`,
  `/benzene/spec`, `/benzene/health`, `/benzene/spec-ui`, plus their domain routes — and they answer
  the mesh's **direct Lambda-Invoke** interrogation (`spec`/`healthcheck` topics) with no HTTP surface
  required. They carry a `benzene` resource tag so discovery finds them.
- The **mesh Lambda** runs, on an EventBridge schedule: discovery (`AwsLambdaDiscoveryProvider` →
  `ListFunctions`+`ListTags`, filtered by the `benzene` tag) → writes `registry.json` to S3 → the
  aggregator interrogates each discovered Lambda by Invoke → writes the catalog artifacts to S3. Its
  HTTP surface serves the **Mesh UI** (reading those artifacts).

## Projects

| Project | What it is | Status |
|---|---|---|
| `Orders/` (`Benzene.Examples.AwsMesh.Orders`) | orders-api Cloud Service Lambda | ✅ built |
| `Payments/` (`Benzene.Examples.AwsMesh.Payments`) | payments-api Cloud Service Lambda | ✅ built |
| `Shipping/` (`Benzene.Examples.AwsMesh.Shipping`) | shipping-api Cloud Service Lambda | ✅ built |
| `Mesh/` (`Benzene.Examples.AwsMesh.Mesh`) | the discovery + aggregator + UI Lambda (uses `Benzene.Mesh.Aws.S3`) | ✅ built |
| `deploy/` | Terraform: 4 Lambdas, IAM, S3, one HTTP API per Lambda, EventBridge schedule | ✅ built |
| `.github/workflows/deploy-aws-mesh-example.yml` | GitHub Actions: build + `terraform apply` | ✅ built |

Each service Lambda is a **self-contained executable** hosting the Benzene pipeline via an
`Amazon.Lambda.RuntimeSupport` bootstrap — because .NET 10 has no managed Lambda runtime, they deploy
on the **`provided.al2023`** custom runtime (self-contained publish).

## Interconnectivity (later)

The three domains (order → payment → shipment) are set up to call each other later (e.g. an order
handler invoking payments then shipping), so the mesh's topology view shows real cross-service edges.
Not wired yet — the first milestone is discovery + catalog.

## Deploy it (via GitHub Actions — no local tooling)

Tooling: **Terraform, run by GitHub Actions**, fronted by **API Gateway HTTP APIs** (one per Lambda,
so each service's Spec UI and the Mesh UI serve their relative assets from their own API root).

1. **Add two repo secrets** (Settings → Secrets and variables → Actions):
   - `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` — an IAM principal allowed to manage Lambda, IAM,
     S3, API Gateway, and EventBridge.
2. **Run the workflow**: Actions → **Deploy AWS Mesh Example** → *Run workflow* (pick a region).
   It builds all four Lambdas (self-contained `provided.al2023`), then `terraform apply`s the stack.
3. **Grab the URLs** from the workflow's final `terraform output` step:
   - `mesh_ui_url` — the Mesh UI.
   - `service_spec_ui_urls` — each service's Spec UI.
   - `mesh_refresh_url` — POST to force a discovery+aggregation pass now.

### Deploy locally instead (if you do have Terraform)

```bash
# Build + zip each Lambda (self-contained, provided.al2023) into examples/AwsMesh/artifacts/, then:
cd examples/AwsMesh/deploy
terraform init
terraform apply -var region=eu-west-1
```

## See it working (both ends)

1. **Open a service Spec UI** (`service_spec_ui_urls.orders`, etc.) — proof the three services are up
   and Cloud Service Profile-conformant, each with its own domain contract and health.
2. **Trigger a mesh pass**: `curl -XPOST "$mesh_refresh_url"` (or wait for the 5-minute schedule). It
   returns `{ "discovered": 3 }` once it has found the three `benzene`-tagged Lambdas.
3. **Open the Mesh UI** (`mesh_ui_url`) — the catalog of the three services the mesh **discovered by
   itself** (no `mesh.json`), each interrogated by direct Lambda-Invoke, with health and dependencies.

That's the end-to-end test: services on one end, the self-discovering mesh on the other.

## How discovery is scoped

- The three **service** Lambdas carry a `benzene` **resource tag**; the **mesh** Lambda does not — so
  the mesh discovers the services but never itself. Change the tag key via the `discovery_tag_key`
  Terraform variable (and the mesh's filter) to match your own tagging.
- The mesh's IAM role gets exactly `lambda:ListFunctions` + `lambda:ListTags` (discover),
  `lambda:InvokeFunction` scoped to the three services (interrogate), and `s3:*Object`/`ListBucket`
  on the artifact bucket. Read + describe-invoke only.

## Teardown

```bash
cd examples/AwsMesh/deploy && terraform destroy
```

## Known first-deploy iteration points

I can build and compile all of this, but the live AWS behaviour is only verifiable on a real deploy.
The most likely things to tweak on the first run (all localized):
- **Custom-runtime packaging** — the `bootstrap` wrapper + self-contained publish RID/arch
  (`lambda_architecture` must match the CI `RID`). Note the `provided.al2023` runtime ships **no
  libicu**, so all four Lambda projects publish with `<InvariantGlobalization>true</InvariantGlobalization>`
  — without it the apphost aborts at init with "Couldn't find a valid ICU package installed".
- **API Gateway payload format** — pinned to `1.0` to match `Benzene.Aws.Lambda.ApiGateway`; if a UI
  route 500s, this is the first thing to check.
- **EventBridge → `mesh:aggregate` routing** — the scheduled target sends a constant
  `{"detail-type":"mesh:aggregate",...}` payload; if the schedule doesn't trigger a pass, verify the
  Benzene EventBridge adapter reads `detail-type` as the topic (POST `mesh_refresh_url` works
  independently of this).

## Build locally

```bash
for p in Orders Payments Shipping Mesh; do
  dotnet build "examples/AwsMesh/$p/Benzene.Examples.AwsMesh.$p.csproj"
done
```
