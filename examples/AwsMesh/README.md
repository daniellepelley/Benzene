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

## OpenTelemetry (traces + metrics)

Every Lambda (the three services and the mesh) wires **full OpenTelemetry**: Benzene's instrumentation
(`AddBenzeneInstrumentation`) for traces and metrics, exported over **OTLP**, plus the pipeline
middleware `UseW3CTraceContext` → `UseBenzeneEnrichment` → `UseBenzeneMetrics` on every transport. The
W3C trace-context propagation is what stitches the **order → payment → shipment** spans (across the SQS
hops) into a single distributed trace — feed it to Grafana Tempo and the mesh's Topology can show
*observed* edges on top of the structural ones.

Two things are different from a typical Generic-Host app, because a bare AWS Lambda host has no `IHost`
(see `Shared/LambdaTelemetry.cs`):

- **The providers are built eagerly.** `services.AddOpenTelemetry()` only *constructs* the
  `TracerProvider`/`MeterProvider` from a hosted service that never runs under a Lambda host — so the
  `"Benzene"` `ActivitySource` would get no listener and **no middleware spans would ever be recorded**.
  `LambdaTelemetry.Configure` builds them with `Sdk.Create*ProviderBuilder().Build()` at startup instead,
  which attaches the listener immediately.
- **Spans are force-flushed per invocation.** `TracingLambdaHost` (the `AwsLambdaHost` subclass every
  `Function` uses) overrides `OnInvocationCompleteAsync` to `ForceFlush` the batched exporters before the
  execution environment freezes, so the current invocation's spans aren't delayed to the next invocation
  or dropped on scale-in.

**X-Ray active tracing** (`tracing_config { mode = "Active" }`) is turned on for every function
automatically — but note it only captures the **AWS-level** segments (the `AWS::Lambda::Function`
segments and their `Overhead` subsegments). Benzene's **per-middleware** spans are OpenTelemetry spans
that leave the process over **OTLP**, a separate pipe that needs a collector to reach X-Ray. To bridge
them, set **`var.adot_collector_layer_arn`** to the ADOT collector Lambda layer for your region: the
Terraform attaches it to every function and points `OTEL_EXPORTER_OTLP_ENDPOINT` at the layer's
in-process collector (`http://localhost:4317`), whose default config forwards OTLP traces to X-Ray —
so the middleware spans show up as subsegments in the same trace as the AWS-level segments. (No
`AWS_LAMBDA_EXEC_WRAPPER` is set: these custom-runtime functions already emit their own spans, so only
the collector half of the layer is used.) `var.otlp_endpoint` is an escape hatch for pointing at an
out-of-process collector instead. With neither set, spans are recorded but exported nowhere.

## What each service shows off

Every service is wired through the shared `Shared/MeshServiceWiring` helper, which "goes to town" on
Benzene's features so the example dogfoods them on a real deploy:

- **One set of handlers, five transports.** Each domain handler is reachable over **API Gateway**
  (HTTP), **direct Lambda invoke** (BenzeneMessage), **SQS**, **SNS**, and **EventBridge** — the same
  handler, no per-transport code. Fire any of them from the **Lambda test tool**: each service ships
  saved requests under `.lambda-test-tool/SavedRequests/` (e.g. `orders-create-sqs.json`,
  `orders-create-eventbridge.json`, `orders-create-direct.json`, `orders-create-apigateway.json`).
- **Tracing/logging across every pipeline.** Every transport pipeline is wrapped with
  `UseLogResult` + a **correlation id**, emitting a structured JSON log line per invocation (request,
  response, `processTime`) to stdout → **CloudWatch**.
- **Validation everywhere.** Each domain request has a **FluentValidation** validator applied via
  `router.UseFluentValidation()`, so an invalid payload is rejected identically no matter which
  transport it arrived on.

## Interconnectivity → topology (a real chain)

The services form a live **order → payment → shipment** chain:

- `orders-api`, on `orders:create`, sends **`payments:capture`** to the payments SQS queue.
- `payments-api`, on `payments:capture`, sends **`shipping:book`** to the shipping SQS queue.
- `shipping-api` books the shipment (terminal).

Each hop uses a Benzene `IBenzeneMessageSender` (`AddOutboundRouting` → `UseSqs`) targeting the next
service's SQS **ingress** — the same queue the shared wiring already consumes. Terraform provisions
the two queues, the SQS→Lambda event-source mappings, and the send/receive IAM. Sends are best-effort,
so a downstream hiccup never fails the upstream call (and locally, with no queue, it just logs).

Because each service also **declares** what it sends (in its spec's `events`), the mesh aggregator
derives a **structural topology** — an edge from each sender to each handler — and publishes
`topology.json`. After a refresh the Mesh UI's **Topology** table shows `orders → payments` and
`payments → shipping` (source `structural`), no tracing backend required. Layer on
`Benzene.Mesh.Tracing.Tempo` to add *observed* edges (real req-rate / error / latency) on top.

**See the chain fire:** invoke `orders-api` (any transport — `orders-create-sqs.json`, the API, …),
then watch CloudWatch: `orders` logs "sent payments:capture", `payments` logs "sent shipping:book",
`shipping` logs the booking — all tied together by the propagated correlation id.

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

State is kept in a per-account S3 bucket (`benzene-mesh-tfstate-<account-id>`) so repeated runs are
incremental rather than colliding — configured at `init` time, so nothing account-specific is
committed. Create the bucket once, then `init` against it:

```bash
# Build + zip each Lambda (self-contained, provided.al2023) into examples/AwsMesh/artifacts/, then:
cd examples/AwsMesh/deploy
ACCOUNT=$(aws sts get-caller-identity --query Account --output text)
aws s3api create-bucket --bucket "benzene-mesh-tfstate-$ACCOUNT" --region eu-west-1 \
  --create-bucket-configuration LocationConstraint=eu-west-1   # omit --create-bucket-configuration in us-east-1
terraform init \
  -backend-config="bucket=benzene-mesh-tfstate-$ACCOUNT" \
  -backend-config="key=aws-mesh/terraform.tfstate" \
  -backend-config="region=eu-west-1"
# If resources already exist from an earlier run without persisted state, adopt them first:
REGION=eu-west-1 PROJECT=benzene-mesh ./adopt-existing.sh
terraform apply -var region=eu-west-1
```

If an earlier run failed *midway*, the account can end up with duplicate/partial resources (API
Gateway allows duplicate names), which makes adoption ambiguous. Recover with a one-time clean slate —
delete every app resource (never the state bucket), then recreate:

```bash
REGION=eu-west-1 PROJECT=benzene-mesh ./cleanup-all.sh
terraform apply -var region=eu-west-1
```

In the GitHub Actions workflow this is the **`recreate`** checkbox on *Run workflow* — tick it once to
recover, then leave it off for normal incremental deploys.

## See it working (both ends)

1. **Open a service Spec UI** (`service_spec_ui_urls.orders`, etc.) — proof the three services are up
   and Cloud Service Profile-conformant, each with its own domain contract and health.
2. **Trigger a mesh pass**: `curl -XPOST "$mesh_refresh_url"` (or wait for the 5-minute schedule). It
   returns `{ "discovered": 3 }` once it has found the three `benzene`-tagged Lambdas.
3. **Open the Mesh UI** (`mesh_ui_url`) — the catalog of the three services the mesh **discovered by
   itself** (no `mesh.json`), each interrogated by direct Lambda-Invoke, with health and dependencies.
   Below the service list, the **Topics** table is the cross-service catalog (every topic across the
   platform → which service owns it, its HTTP mapping, domain vs utility), with a **show utilities**
   toggle that hides the reserved Benzene endpoints by default.
4. **Open a service's Spec UI** and note the **Benzene utilities** panel — the reserved
   `spec`/`health`/`mesh` endpoints are collapsed out of the service's domain topics.

That's the end-to-end test: services on one end, the self-discovering mesh on the other.

The `POST $mesh_refresh_url` endpoint returns **201 Created** (a pass creates/refreshes the catalog
artifacts) with `{ "discovered": N }`.

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
