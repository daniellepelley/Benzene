# Google Cloud Mesh example

A Benzene service mesh on **Google Cloud Functions (Gen2)**, wired with **Pub/Sub** — the Google Cloud
counterpart of [`examples/AwsMesh`](../AwsMesh) and [`examples/AzureFunctionsMesh`](../AzureFunctionsMesh).
Four domain services publish events to each other over Pub/Sub, each exposes the Cloud Service Profile
over HTTP, and a mesh aggregator polls them and renders the mesh UI.

> **Status: authored, not yet verified against live GCP.** The whole solution **builds**
> (`dotnet build Benzene.Examples.GoogleCloudMesh.sln`), and it's built on real, unit-tested Benzene
> Google Cloud packages — but the Terraform and the deploy workflow have not been run end-to-end on a
> real project. Treat the first deploy as an iteration exercise, like the other cloud mesh examples did.

## What it exercises (and the features it drove out)

Google Cloud is the least-developed cloud in Benzene, so this example required building two new
framework packages, both now on `main`:

- **`Benzene.Clients.GoogleCloud.PubSub`** — the outbound Pub/Sub publish client (there was none). This
  is what lets services publish events to each other. Mirrors `Benzene.Clients.Aws.Sqs`.
- **`Benzene.Mesh.GoogleCloud.Storage`** — a GCS-backed `IMeshArtifactStore` so the mesh catalog
  survives function cold-starts. Mirrors `Benzene.Mesh.Azure.Blob`.

It uses the pre-existing inbound adapters (`Benzene.GoogleCloud.Functions.Http` for the profile,
`Benzene.GoogleCloud.Functions.PubSub` for consuming) unchanged.

## Topology

A Gen2 Cloud Function has exactly **one trigger** (unlike a Lambda / Azure Function that multiplexes
event sources). So each service is deployed as **two functions sharing one `Startup`**:

- an **HTTP function** (`HttpFunction : GoogleCloudFunctionHost<Startup>`) — serves `/benzene/spec`,
  `/health`, `/invoke`; this is what the mesh polls.
- a **Pub/Sub function** (`PubSubFunction : GooglePubSubFunctionHost<Startup>`) — consumes events.

`MeshServiceWiring.Configure` calls **both** `UseHttp(...)` and `UsePubSub(...)`; each is a no-op on the
host it doesn't apply to, so the same object runs unchanged on both.

Inter-service messaging uses one **Pub/Sub "inbox" topic per consumer** (the Cloud-Functions-friendly
analogue of AwsMesh's one-SQS-queue-per-service): a producer publishes to the *target's* inbox with the
Benzene topic in the `"topic"` message attribute (Benzene routes by attribute, not by Pub/Sub topic).

```
orders ──payment:take──▶ payments-inbox ──▶ payments
orders ──order:placed──▶ notifications-inbox ──▶ notifications
payments ──shipment:book──▶ shipping-inbox ──▶ shipping
payments ──payment:captured──▶ notifications-inbox
shipping ──shipment:dispatched──▶ notifications-inbox
```

| Service | Consumes | Publishes |
|---|---|---|
| **orders** | `order:create` (HTTP) | `payment:take`, `order:placed` |
| **payments** | `payment:take` (Pub/Sub, HTTP) | `shipment:book`, `payment:captured` |
| **shipping** | `shipment:book` (Pub/Sub, HTTP) | `shipment:dispatched` |
| **notifications** | `order:placed`, `payment:captured`, `shipment:dispatched` (Pub/Sub) | — |

Handlers are transport-agnostic (`IBenzeneMessageSender`); an outbound route maps a Benzene topic to a
Pub/Sub topic via `.Route(topic, p => p.UsePubSub(topicPath))`.

## The mesh

`Mesh/` is a Gen2 HTTP function. It polls each service's HTTP Cloud Service Profile from a **static
registry** (`MeshRegistry.FromEnvironment` reads per-service `MESH_*_URL` vars — Google Cloud has no
mesh discovery provider yet; a `Benzene.Mesh.Discovery.Google` is a clean follow-on), writes the catalog
to **GCS**, and serves the mesh UI at `/mesh-ui` + the artifacts. Aggregation runs on `POST /mesh/refresh`,
which **Cloud Scheduler** hits every couple of minutes (Cloud Functions has no timer trigger).

## Deploy

`deploy/` (Terraform) provisions the Pub/Sub inbox topics, the mesh GCS bucket, a runtime service
account + IAM, and (second pass) the Cloud Scheduler job. The functions themselves are deployed by
[`.github/workflows/deploy-google-cloud-mesh-example.yml`](../../.github/workflows/deploy-google-cloud-mesh-example.yml)
with `gcloud functions deploy` (Gen2, buildpack source deploy from the repo root so sibling `src/`
project references resolve), which also creates each Pub/Sub function's trigger subscription. Run it
manually with a project id and a Terraform-state bucket.

## Prerequisites: bootstrap the API-enablement APIs (one-time)

The workflow enables every API it needs — but it can't enable the two APIs that *enablement itself*
depends on (you can't turn on an API through an API that's off). On a fresh project, enable these two
once, manually (console links appear in the `SERVICE_DISABLED` error, or run this as a project owner —
**not** the deploy SA):

```bash
gcloud services enable serviceusage.googleapis.com cloudresourcemanager.googleapis.com \
  --project smart-theory-88114
```

After that the workflow's "Enable required Google Cloud APIs" step turns on the rest.

## Prerequisites: deploy service-account roles (one-time)

The workflow authenticates as the `GCP_SA_KEY` service account. That SA provisions and deploys
everything, so — as a **project owner** — grant it the roles below once (the workflow can't grant its
own IAM). Without them the run fails with a `403` (the first symptom is usually `storage.objects.list`
denied on the Terraform state bucket):

```bash
PROJECT=smart-theory-88114
SA=benzene@smart-theory-88114.iam.gserviceaccount.com   # your GCP_SA_KEY account
for role in \
  roles/serviceusage.serviceUsageAdmin \ # enable the required APIs (the workflow does this)
  roles/storage.admin \            # Terraform state bucket + the mesh artifact bucket
  roles/pubsub.admin \             # inbox topics + subscriptions
  roles/cloudfunctions.admin \     # deploy the functions
  roles/run.admin \                # Gen2 functions run on Cloud Run
  roles/cloudbuild.builds.editor \ # Gen2 source builds
  roles/artifactregistry.admin \   # build artifacts
  roles/cloudscheduler.admin \     # the mesh refresh job
  roles/iam.serviceAccountAdmin \  # create the runtime service account
  roles/iam.serviceAccountUser ; do # deploy functions AS the runtime SA
  gcloud projects add-iam-policy-binding "$PROJECT" \
    --member="serviceAccount:$SA" --role="$role" >/dev/null
done
```

(Scope these down for a real deployment; this is the broad set that lets one SA run the whole demo.)

## Known first-deploy iteration points

- **Runtime `dotnet10`** — the workflow assumes a GCF `dotnet10` runtime; adjust if the managed runtime
  lags (the existing `examples/Google` uses the same).
- **Inbox vs. fan-out** — this uses inbox topics (point-to-point + fan-in). True fan-out of one event to
  multiple consumers (as AwsMesh does for `order:placed`) would use one topic per event type with a push
  subscription per consumer; the outbound client supports both — only the topic wiring changes.
- **Auth** — service HTTP functions are `--allow-unauthenticated` for the demo so the mesh can poll them;
  lock down with IAM invoker bindings + the mesh SA in production.
- **Not run end-to-end** — as above; the Terraform/gcloud commands are authored to the documented shapes
  but unproven against a live project.

## Scope

Four services (orders → payments → shipping command chain + notifications fan-in) — the full capability
set (HTTP profile, outbound Pub/Sub, inbound Pub/Sub, fan-in, mesh HTTP aggregation, GCS persistence).
The two pure-consumer domains AwsMesh adds (inventory, analytics) follow the notifications pattern
verbatim.
