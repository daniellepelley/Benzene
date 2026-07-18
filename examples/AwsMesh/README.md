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
| `Mesh/` | the discovery + aggregator + UI Lambda (+ an S3 artifact store) | ⏳ next |
| `deploy/` | Terraform: 4 Lambdas, IAM, S3, API Gateway HTTP API, EventBridge schedule | ⏳ next |
| `.github/workflows/deploy-aws-mesh-example.yml` | GitHub Actions: build + `terraform apply` | ⏳ next |

Each service Lambda is a **self-contained executable** hosting the Benzene pipeline via an
`Amazon.Lambda.RuntimeSupport` bootstrap — because .NET 10 has no managed Lambda runtime, they deploy
on the **`provided.al2023`** custom runtime (self-contained publish).

## Interconnectivity (later)

The three domains (order → payment → shipment) are set up to call each other later (e.g. an order
handler invoking payments then shipping), so the mesh's topology view shows real cross-service edges.
Not wired yet — the first milestone is discovery + catalog.

## Deploying

Chosen tooling: **Terraform, run by GitHub Actions** (so you don't need Terraform installed locally —
CI runs `terraform apply` with AWS credentials from GitHub secrets), fronted by an **API Gateway HTTP
API**. The `deploy/` Terraform and the workflow land in the next step; this README will then carry the
full deploy walkthrough (secrets to set, how to open the Mesh UI + each Spec UI, how to trigger a
refresh, and teardown).

## Build locally

```bash
dotnet build examples/AwsMesh/Orders/Benzene.Examples.AwsMesh.Orders.csproj
dotnet build examples/AwsMesh/Payments/Benzene.Examples.AwsMesh.Payments.csproj
dotnet build examples/AwsMesh/Shipping/Benzene.Examples.AwsMesh.Shipping.csproj
```
