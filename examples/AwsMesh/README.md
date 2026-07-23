# AWS Mesh Self-Discovery — end-to-end example

A deployable AWS example that proves the Benzene mesh **self-discovery** story end to end: **six**
Benzene Cloud Services running as Lambdas that call each other over **SQS, SNS and EventBridge**, plus
a **mesh service** (a seventh Lambda) that discovers them by tag, interrogates each, and serves the
Mesh UI — all fronted by API Gateway so you can open the UIs in a browser.

The six services dogfood Benzene's transports by using **each one for what it's actually good at**
(commands vs fan-out events vs routed integration events), so the Mesh UI's topology renders a real,
non-trivial graph. See `work/aws-mesh-multi-transport-plan.md` for the plan and
`work/mesh-self-discovery-design.md` for the discovery design this example exercises.

## Architecture

```
                          API Gateway (one HTTP API per Lambda, public)  +  direct Lambda-invoke (mesh interrogation)

  ┌─────────┐  SQS  payments:capture  ┌──────────┐  SQS  shipping:book  ┌──────────┐
  │ orders  │ ───────────────────────►│ payments │ ───────────────────► │ shipping │
  └────┬────┘                         └────┬─────┘                      └────┬─────┘
       │ SNS  order:placed (fan-out)       │ EventBridge  payment:captured   │ EventBridge  shipping:dispatched
       ▼               ▼                   ▼               ▼                  ▼          ▼          ▼
 ┌───────────┐  ┌───────────────┐   ┌───────────────┐ ┌───────────┐   inventory  notifications  analytics
 │ inventory │  │ notifications │   │ notifications │ │ analytics │
 └───────────┘  └───────────────┘   └───────────────┘ └───────────┘
   reserve         notify              notify            metrics
                                                                          ← 6 Cloud Service Lambdas (tag: benzene)
                          │  mesh (7th Lambda, untagged):
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

- The six **service Lambdas** are full Cloud Service Profile (R1–R8) services: `/benzene/invoke`,
  `/benzene/spec`, `/benzene/health`, `/benzene/spec-ui`, plus any domain routes — and they answer
  the mesh's **direct Lambda-Invoke** interrogation (`spec`/`healthcheck` topics) with no HTTP surface
  required. They carry a `benzene` resource tag so discovery finds them.
- The **mesh Lambda** runs, on an EventBridge schedule: discovery (`AwsLambdaDiscoveryProvider` →
  `ListFunctions`+`ListTags`, filtered by the `benzene` tag) → writes `registry.json` to S3 → the
  aggregator interrogates each discovered Lambda by Invoke → writes the catalog artifacts to S3. Its
  HTTP surface serves the **Mesh UI** (reading those artifacts).

## Projects

| Project | What it is | Sends | Consumes |
|---|---|---|---|
| `Orders/` (`…AwsMesh.Orders`) | orders-api Cloud Service Lambda | `payments:capture` (SQS), `order:placed` (SNS) | — |
| `Payments/` (`…AwsMesh.Payments`) | payments-api Cloud Service Lambda | `shipping:book` (SQS), `payment:captured` (EventBridge) | `payments:capture` |
| `Shipping/` (`…AwsMesh.Shipping`) | shipping-api Cloud Service Lambda | `shipping:dispatched` (EventBridge) | `shipping:book` |
| `Inventory/` (`…AwsMesh.Inventory`) | inventory-api Cloud Service Lambda | — | `order:placed` (SNS), `shipping:dispatched` (EventBridge) |
| `Notifications/` (`…AwsMesh.Notifications`) | notifications-api Cloud Service Lambda | — | `order:placed` (SNS), `payment:captured` + `shipping:dispatched` (EventBridge) |
| `Analytics/` (`…AwsMesh.Analytics`) | analytics-api Cloud Service Lambda | — | `payment:captured` + `shipping:dispatched` (EventBridge) |
| `Mesh/` (`…AwsMesh.Mesh`) | the discovery + aggregator + UI Lambda (uses `Benzene.Mesh.Aws.S3`) | — | — |
| `deploy/` | Terraform: 7 Lambdas, IAM, S3, one HTTP API per Lambda, SQS queues, an SNS topic, a custom EventBridge bus + rules, and the aggregation schedule | | |
| `.github/workflows/deploy-aws-mesh-example.yml` | GitHub Actions: build all 7 Lambdas + `terraform apply` | | |

Each service Lambda is a **self-contained executable** hosting the Benzene pipeline via an
`Amazon.Lambda.RuntimeSupport` bootstrap — because .NET 10 has no managed Lambda runtime, they deploy
on the **`provided.al2023`** custom runtime (self-contained publish).

## OpenTelemetry (traces + metrics)

Every Lambda (the six services and the mesh) wires **full OpenTelemetry**: Benzene's instrumentation
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
that leave the process over **OTLP**, a separate pipe that needs a collector to reach X-Ray.

To bridge them, **`var.adot_collector_layer_arn`** (defaulted to the eu-west-1 amd64 ADOT collector
layer) attaches the collector to every function and points `OTEL_EXPORTER_OTLP_ENDPOINT` at its
in-process receiver (`http://localhost:4317`). The layer's *default* config is **metrics-only** (it
drops traces), so the Terraform also sets `OPENTELEMETRY_COLLECTOR_CONFIG_URI=/var/task/collector.yaml`
to select the [`collector.yaml`](collector.yaml) shipped in each Lambda zip, which adds the
`traces → awsxray` pipeline. (No `AWS_LAMBDA_EXEC_WRAPPER` is set: these custom-runtime functions
already emit their own spans, so only the collector half of the layer is used.) `var.otlp_endpoint` is
an escape hatch for pointing at an out-of-process collector instead. With neither set, spans are
recorded but exported nowhere.

Because Benzene builds its **own** W3C trace across the whole mesh run (its `traceparent` propagation),
these OTLP spans arrive in X-Ray as their **own** traces — grouped by OTel service name (`orders-api`,
`payments-api`, `benzene-mesh`) — rather than nested inside the per-invocation `AWS::Lambda` segments,
which carry X-Ray's own (different) trace ids.

### Middleware spans nested *inside* the X-Ray segments — `AddXRayTracing()`
If what you want is the middleware breakdown **nested under the `AWS::Lambda::Function` segment** (the
classic X-Ray view), that's what **`Benzene.Aws.Lambda.XRay`** is for. Every service and the mesh wire
`AddXRayTracing()` alongside `AddDiagnostics()`: it wraps each middleware in an AWS **X-Ray subsegment**
via the X-Ray SDK, which attaches to the Lambda's own segment (`_X_AMZN_TRACE_ID`) — so the stages nest
directly under it, in the same trace as the AWS-level segments, **with no OTLP collector at all**. It
needs only X-Ray active tracing (on) + the X-Ray write IAM (granted); the ADOT collector / `collector.yaml`
path above is then just the *alternative* for shipping the same pipeline's OTel spans to a non-X-Ray
backend (Tempo/Jaeger/Honeycomb). Wire one or both — they coexist.

### Topic usage → the Mesh UI (metrics, not just traces)
The same `UseBenzeneMetrics()` on every pipeline emits the `benzene.messages.processed` counter tagged
`topic`/`transport`/`result`. The collector's `metrics` pipeline exports it to **CloudWatch** via the
`awsemf` (Embedded Metric Format) exporter (`collector.yaml`) into the `Benzene/Mesh` namespace, dimensions
`[topic, transport, result]`, in the `/benzene/mesh/usage` log group. The counter is exported with **delta**
temporality (`LambdaTelemetry`) so a CloudWatch `Sum` over a window equals the request count.

The mesh Lambda then reads it back: `AddCloudWatchUsage(...)` registers
`Benzene.Mesh.Usage.CloudWatch`'s `IMeshUsageSource`, which the aggregator pulls each run to write
`usage.json` — per-topic request counts over `var.usage_window_hours` (default 24h). The **Mesh UI** renders
those as a Usage column on the estate topic table plus per-topic by-transport / by-status breakdowns and the
window. IAM: the service role gains CloudWatch Logs perms on the usage group (`service_emf`), and the mesh
role gains `cloudwatch:GetMetricData`/`ListMetrics` (in its policy). This is deliberately coarse — request
counts over a window; fine-grained analysis stays in CloudWatch/Grafana. (Per-service attribution and
duration are documented follow-ups — the counter isn't tagged by service, so `usage.json` reports that
dimension as absent, which the UI surfaces honestly rather than guessing.)

#### Generate traffic with the Lambda test tool
There's nothing to show until services actually handle messages. The quickest way to create some is the
**Test** tab on a service Lambda in the console (or `aws lambda invoke`). On a direct invoke the services
accept the **Benzene message envelope** `{ "topic", "headers", "body" }` — note `body` is a *string* holding
the message JSON (escaped quotes), and it flows through the same metered pipeline. Which Lambda handles
which topic: `orders:*` → orders, `payments:*` → payments, `shipping:*` → shipping; the events `order:placed`
→ inventory/notifications, `payment:captured` → notifications/analytics, `shipping:dispatched` →
inventory/notifications/analytics.

**Best starting point — `orders:create` on the `orders` Lambda.** Because the queues/topics/bus are wired,
the handler's downstream sends fire for real, so one invoke fans out `payments:capture` (SQS) →
`shipping:book` (SQS) → `shipping:dispatched` (EventBridge) → the consumers — giving traffic across many
topics and the **sqs/sns/eventbridge** transports, not just the invoke path. Fire it a dozen times:

```json
{ "topic": "orders:create", "headers": {}, "body": "{\"item\":\"Espresso Machine\",\"quantity\":2}" }
```

Per-topic payloads to hit any service directly (these count under the *invoke* transport):

```json
{ "topic": "payments:capture",    "headers": {}, "body": "{\"orderId\":\"ord-1\",\"amount\":20,\"currency\":\"GBP\"}" }
{ "topic": "shipping:book",       "headers": {}, "body": "{\"orderId\":\"ord-1\",\"carrier\":\"DPD\"}" }
{ "topic": "order:placed",        "headers": {}, "body": "{\"orderId\":\"ord-1\",\"item\":\"Espresso Machine\",\"quantity\":2,\"amount\":20,\"currency\":\"GBP\"}" }
{ "topic": "payment:captured",    "headers": {}, "body": "{\"orderId\":\"ord-1\",\"amount\":20,\"currency\":\"GBP\"}" }
{ "topic": "shipping:dispatched", "headers": {}, "body": "{\"orderId\":\"ord-1\",\"shipmentId\":\"shp-1\",\"carrier\":\"DPD\",\"trackingNumber\":\"DPD-123\"}" }
{ "topic": "orders:get-all",      "headers": {}, "body": "" }
```

Or from the CLI (`--cli-binary-format raw-in-base64-out` lets AWS CLI v2 take a raw JSON payload):

```bash
aws lambda invoke --function-name <orders-fn-name> --cli-binary-format raw-in-base64-out \
  --payload '{"topic":"orders:create","headers":{},"body":"{\"item\":\"Espresso Machine\",\"quantity\":2}"}' /dev/stdout
```

Notes: the counter is recorded around the whole pipeline, so **every** invoke produces a datapoint — a
payload that fails validation just lands as `result=failure`. Validation to respect: `orders:create` needs a
non-empty item and quantity 1–1000; `payments:capture` a 3-char currency and amount > 0; `shipping:book` a
carrier in {DPD, RoyalMail, UPS, FedEx}. After firing a batch, give the metric ~1–2 min to reach CloudWatch,
`POST /mesh/refresh` to aggregate now (instead of waiting for the schedule), then check `usage.json` / the
Mesh UI Usage column.

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

## Interconnectivity → topology — one transport per job

The six services form a live fulfilment flow, and **each transport is used for what it's good at** —
that's the whole point of the dogfood, and what makes the Mesh UI topology worth looking at:

| Transport | Idiomatic for | In this example |
|---|---|---|
| **SQS** | a point-to-point **command** — one consumer, must arrive, retry/DLQ | `orders → payments` (`payments:capture`), `payments → shipping` (`shipping:book`) |
| **SNS** | a **fan-out event** — one publisher, many subscribers | `orders` publishes `order:placed` → **inventory _and_ notifications** |
| **EventBridge** | **routed integration events** — content rules, one event → many targets | `payments` publishes `payment:captured`, `shipping` publishes `shipping:dispatched` → routed to **notifications / inventory / analytics** |

The flow:

- `orders-api`, on `orders:create`: **sends** `payments:capture` (SQS command) **and** publishes
  `order:placed` (SNS event).
- `payments-api`, on `payments:capture`: **sends** `shipping:book` (SQS command) **and** publishes
  `payment:captured` (EventBridge event).
- `shipping-api`, on `shipping:book`: books the shipment and publishes `shipping:dispatched`
  (EventBridge event).
- `inventory-api` reserves stock on `order:placed` (SNS) and decrements it on `shipping:dispatched`
  (EventBridge) — one service consuming from **two** event transports.
- `notifications-api` notifies the customer on `order:placed` (SNS) + `payment:captured` +
  `shipping:dispatched` (EventBridge).
- `analytics-api` records metrics on `payment:captured` + `shipping:dispatched` (EventBridge).

Every hop goes through the same Benzene `IBenzeneMessageSender` (`AddOutboundRouting` → `UseSqs` /
`UseSns` / `UseEventBridge`), and the receiving side is just the matching Benzene ingress the shared
wiring already registers (`aws.UseSqs` / `aws.UseSns` / `aws.UseEventBridge`) — the **same handler**, no
per-transport code. The choice of transport per send lives entirely in each service's `Startup`
(`OutboundSend.Sqs/Sns/EventBridge(...)`). Terraform provisions the two SQS queues + event-source
mappings, the SNS topic + Lambda subscriptions, and a **custom EventBridge bus** + rules + targets, plus
the send/publish IAM. Sends are best-effort, so a downstream hiccup never fails the upstream call (and
locally, with no target wired, they just log).

Because each service also **declares** what it sends (in its spec's `events`), the mesh aggregator
derives a **structural topology** — an edge from each sender to each consuming handler — and publishes
`topology.json`, **transport-agnostically** (an SQS command, an SNS event and an EventBridge event all
surface as edges the same way). After a refresh the Mesh UI's **Topology** table shows the whole graph
(`orders → payments`, `orders → inventory`, `orders → notifications`, `payments → shipping`, `payments →
notifications`, `payments → analytics`, `shipping → inventory/notifications/analytics`), source
`structural`, no tracing backend required. Layer on `Benzene.Mesh.Tracing.Tempo` to add *observed* edges
(real req-rate / error / latency) on top.

**See the flow fire:** invoke `orders-api` (any transport — `orders-create-sqs.json`, the API, …), then
watch CloudWatch: `orders` logs "sent payments:capture" + "published order:placed"; `inventory` and
`notifications` log the `order:placed` fan-out; `payments` logs "sent shipping:book" + "published
payment:captured"; `shipping` logs the booking + "published shipping:dispatched"; `analytics` logs its
metrics — all tied together by the propagated correlation id.

## Deploy it (via GitHub Actions — no local tooling)

Tooling: **Terraform, run by GitHub Actions**, fronted by **API Gateway HTTP APIs** (one per Lambda,
so each service's Spec UI and the Mesh UI serve their relative assets from their own API root).

1. **Add two repo secrets** (Settings → Secrets and variables → Actions):
   - `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` — an IAM principal allowed to manage Lambda, IAM,
     S3, API Gateway, and EventBridge.
2. **Run the workflow**: Actions → **Deploy AWS Mesh Example** → *Run workflow* (pick a region).
   It builds all seven Lambdas (self-contained `provided.al2023`), then `terraform apply`s the stack.
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

1. **Open a service Spec UI** (`service_spec_ui_urls.orders`, etc.) — proof the services are up
   and Cloud Service Profile-conformant, each with its own domain contract and health.
2. **Trigger a mesh pass**: `curl -XPOST "$mesh_refresh_url"` (or wait for the schedule — every minute
   by default, `var.aggregate_schedule`). It returns `{ "discovered": 6 }` once it has found the six
   `benzene`-tagged Lambdas. The schedule keeps the catalog + usage feed fresh on its own; the Mesh UI
   explorer loads artifacts once per page load, so reload the page to pick up a newer pass.
3. **Open the Mesh UI** (`mesh_ui_url`) — the catalog of the six services the mesh **discovered by
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

- The six **service** Lambdas carry a `benzene` **resource tag**; the **mesh** Lambda does not — so
  the mesh discovers the services but never itself. Change the tag key via the `discovery_tag_key`
  Terraform variable (and the mesh's filter) to match your own tagging.
- The mesh's IAM role gets exactly `lambda:ListFunctions` + `lambda:ListTags` (discover),
  `lambda:InvokeFunction` scoped to the six services (interrogate), and `s3:*Object`/`ListBucket`
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
  libicu**, so all seven Lambda projects publish with `<InvariantGlobalization>true</InvariantGlobalization>`
  — without it the apphost aborts at init with "Couldn't find a valid ICU package installed".
- **API Gateway payload format** — pinned to `1.0` to match `Benzene.Aws.Lambda.ApiGateway`; if a UI
  route 500s, this is the first thing to check.
- **EventBridge → topic routing** — both the `mesh:aggregate` schedule and the inter-service
  integration events (`payment:captured`, `shipping:dispatched`) rely on the Benzene EventBridge adapter
  reading `detail-type` as the topic. The custom-bus rules match on that same `detail-type`; if a
  consumer never fires, confirm the publisher's `DetailType` and the rule's `event_pattern` agree (POST
  `mesh_refresh_url` triggers a pass independently of any of this).
- **SNS fan-out routing** — `order:placed` carries the Benzene topic in the `topic` **message
  attribute**; the SNS→Lambda subscription delivers it to inventory-api and notifications-api, whose
  `aws.UseSns` ingress routes on that attribute. If a subscriber doesn't route, check the attribute is
  present on the published message.

## Build locally

```bash
for p in Orders Payments Shipping Inventory Notifications Analytics Mesh; do
  dotnet build "examples/AwsMesh/$p/Benzene.Examples.AwsMesh.$p.csproj"
done
```
