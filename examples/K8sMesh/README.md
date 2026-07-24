# Kubernetes Mesh Self-Discovery — end-to-end example

The Kubernetes counterpart of `examples/AwsMesh`: three Benzene Cloud Services running as pods, plus a
**mesh service** that discovers them **by label** via the Kubernetes API, interrogates each over plain
in-cluster HTTP, and serves the Mesh UI. The three services also **call each other** — orders → payments
→ shipping — over lightweight Benzene messages on HTTP, so the mesh has real service-to-service traffic
to observe, not just static specs. It runs two ways from the same manifests: credential-free on
a throwaway [`kind`](https://kind.sigs.k8s.io) cluster in CI, or on a real **AWS EKS** cluster with
the Mesh UI on the public internet (see "Deploy to AWS (EKS)" below).

## Architecture

```
        Kubernetes namespace: benzene-mesh
  ┌──────────┐   ┌───────────┐   ┌────────────┐
  │ orders   │──▶│ payments  │──▶│ shipping   │   3 Deployments (one image, MESH_SERVICE selects domain)
  │ Service  │   │ Service   │   │ Service    │   each Service labelled  benzene: "true"
  └────┬─────┘   └─────┬─────┘   └─────┬──────┘   ──▶ POST /benzene-message  (a { topic, headers, body }
       │  ▲            │  ▲            │  ▲             envelope, addressed by in-cluster DNS — the chain)
       │  │  3. each service PUSHES register + heartbeat + traces to the mesh's collector
       │  │     (http://mesh/benzene/invoke) — the live feed
       │   1. list Services (label benzene=true) via the Kubernetes API
       │   2. GET http://<svc>.<ns>.svc.cluster.local/benzene/spec|health  (interrogate — the pull feed)
       ▼
   ┌────────┐   writes manifest.json / services/*.json / topics.json / registry.json
   │  mesh  │   to /artifacts (pod volume) and serves the Mesh UI at /mesh-ui (pulled/declared,
   └────────┘   enriched in-page with the live Fleet plane: pushed/observed) — NodePort 30080
```

## Service-to-service calls — lightweight Benzene messages over HTTP

Beyond discovery, each service **chains to the next** over its neighbour's BenzeneMessage endpoint:

- **Ingress** — every service exposes `POST /benzene-message` (`asp.UseBenzeneMessage(...)` in
  `Service/Startup.cs`). A `{ topic, headers, body }` envelope POSTed there is routed to the service's
  handlers **by the envelope's topic**, exactly as a queue or a Lambda invoke would — one endpoint serves
  every topic, no per-route REST contract. It's the same server endpoint the AWS Lambda invoke path
  exposes, just over HTTP.
- **Egress** — `orders`' `order:create` handler asks `payments` to `payment:take`, and `payments`'
  `payment:take` handler asks `shipping` to `shipment:book`, each via **`HttpBenzeneMessageClient`**
  (`src/Benzene.Client.Http`). The downstream URL is the neighbour's in-cluster DNS name, injected as
  `DOWNSTREAM_MSG_URL` (e.g. `http://payments/benzene-message`); the terminal `shipping` service has none.
  Registration is one line — `x.AddHttpBenzeneMessageClient(downstreamUrl)` — which also auto-wires a
  non-destructive reachability check (a `healthcheck`-topic POST) onto the deep `healthcheck` layer.

Send an order into the front of the chain and watch it propagate (from a `port-forward svc/orders 8081:80`,
or directly against a service ELB on EKS):

```bash
curl -XPOST localhost:8081/orders -H 'content-type: application/json' \
     -d '{"customerId":"cust-1","sku":"espresso","quantity":2}'
# => {"orderId":"order-1","status":"created"}   ... orders logs: payment:take -> Created
#    ... payments logs: shipment:book -> Created

# Or hit any service's envelope endpoint directly, addressing a topic it owns:
curl -XPOST localhost:8081/benzene-message -H 'content-type: application/json' \
     -d '{"topic":"payment:take","headers":{},"body":"{\"orderId\":\"o-9\",\"amount\":30,\"currency\":\"GBP\"}"}'
# => {"statusCode":"Created", ... ,"body":"{\"paymentId\":\"pay-1\",\"status\":\"captured\"}"}
```

Design + trade-offs (why HTTP-envelope over gRPC/TCP for internal calls):
`work/lightweight-non-http-transport-design.md`. The quickest way to see the whole chain **without
Kubernetes** is the `compose/` variant (`DOWNSTREAM_MSG_URL` is pre-wired there too).

### Message versioning over the chain (send v1 → upcast → one v2 handler)

The `payment:take` hop also demonstrates [payload versioning](../../docs/specification/versioning.md):

- `payments` runs a **single v2 handler** (`[Message("payment:take", "2")]`, taking `V2.TakePaymentRequest`,
  which added a `currency` field over V1) and registers a **V1→V2 upcaster** + `UsePayloadVersionCasting`.
- `orders` is pinned to **v1**: it sends `V1.TakePaymentRequest` (no currency) declaring `version: "1"`
  (`SendMessageAsync(..., version: "1")`), which travels in the `benzene-version` envelope header.
- So a v1 payload is **upcast to v2 before the handler runs** — the handler always sees a currency (seeded
  by the upcast), never the v1 shape. `orders` also declares producing `payment:take@1` (spec `events`), so
  the Mesh UI's **Version compatibility** panel shows the producer-v1 / consumer-v2 skew the upcaster bridges.

Run the two-hop chain locally (no cluster) and watch the upcast in the payments log:

```bash
# from examples/K8sMesh/Service, after `dotnet build`
MESH_SERVICE=payments PORT=8091 dotnet bin/Debug/net10.0/Benzene.Examples.K8sMesh.Service.dll &
MESH_SERVICE=orders  PORT=8090 DOWNSTREAM_MSG_URL=http://localhost:8091/benzene-message \
  dotnet bin/Debug/net10.0/Benzene.Examples.K8sMesh.Service.dll &
curl -XPOST localhost:8090/orders -H 'content-type: application/json' \
     -d '{"customerId":"c1","sku":"espresso","quantity":2}'
# payments logs: "payment pay-1 captured in GBP (v2 handler)"  ← currency the v1 payload never carried
```

- Discovery is `Benzene.Mesh.Discovery.Kubernetes` (`KubernetesServiceDiscoveryProvider`): it lists
  Services carrying the `benzene` label and emits **HTTP** registry entries at their in-cluster DNS —
  so the aggregator's existing `HttpMeshServiceSource` interrogates them, no K8s-specific fetch source.
- The mesh's ServiceAccount has RBAC to **list Services** only (`k8s/mesh.yaml`). The mesh's own
  Service is **not** `benzene`-labelled, so it never discovers itself.
- The catalog lives on the mesh pod's own `emptyDir` volume (single writer + reader) — no blob store.
- **The live Fleet plane** (the "Fleet" nav on `/mesh-ui`, plus the live sections on each service/topic
  page) is the mesh's second, complementary lens, merged into the Mesh UI. Where the catalog renders
  what services *declare* (the aggregator's pulled + published artifacts), the Fleet plane renders
  what's *actually running*: the mesh pod also hosts a `Benzene.Mesh.Collector` at
  `/benzene/invoke`, and each Cloud Service reports to it (`WithCollector(...)`, driven by the
  `MESH_COLLECTOR_ENVELOPE_URL` the manifests set) — registrations, health heartbeats, and per-call
  traces. The Fleet plane polls the collector and shows live health, observed consumer edges (who
  actually calls whom, from trace parentage), recent flows, and "missing feed" markers for partial
  data. The single always-on mesh pod is the right home for the collector's in-memory state (one
  process accumulates every service's feed) — which is why this live view fits K8sMesh but not the
  scale-to-zero Azure Functions Consumption mesh. It reduces gracefully: an unreachable collector
  never fails a service, it just leaves that service's live feed empty.

## Projects

| Path | What it is |
|---|---|
| `Service/` | one ASP.NET Core Cloud Service image; `MESH_SERVICE` picks the domain (orders/payments/shipping) |
| `Mesh/` | the discovery + aggregator + UI service (K8s discovery, filesystem store, 30s background pass + `POST /mesh/refresh`); also hosts the live `Benzene.Mesh.Collector` at `/benzene/invoke` that the Mesh UI's Fleet plane polls |
| `k8s/` | manifests: namespace, the 3 Deployments+Services, and the mesh (SA + RBAC + Deployment + NodePort Service), with a kustomize base for target-specific overlays |
| `deploy/` | Terraform for the AWS leg: EKS cluster + node group + the two ECR repositories |
| `deploy/eks/` | kustomize overlay over `k8s/`: ECR images (set by the workflow) + a LoadBalancer mesh Service |
| `.github/workflows/deploy-k8s-mesh-example.yml` | build images → kind → deploy → assert 3 discovered |
| `.github/workflows/deploy-eks-mesh-example.yml` | terraform apply → push images to ECR → deploy → assert 3 discovered → print the public URLs (Mesh UI + services) |

## Run it in CI (no credentials)

**Actions → Deploy K8s Mesh Example → Run workflow.** It builds both images, creates a `kind` cluster,
loads the images, applies the manifests, waits for rollout, then `POST`s `/mesh/refresh` and asserts
`{"discovered":3}` — a real end-to-end proof of the Kubernetes discovery path.

## Run it locally

```bash
kind create cluster --name benzene
docker build -f examples/K8sMesh/Service/Dockerfile -t benzene-k8smesh-service:local .
docker build -f examples/K8sMesh/Mesh/Dockerfile     -t benzene-k8smesh-mesh:local .
kind load docker-image benzene-k8smesh-service:local --name benzene
kind load docker-image benzene-k8smesh-mesh:local     --name benzene
kubectl apply -k examples/K8sMesh/k8s/   # -k: the directory is a kustomize base (deploy/eks overlays it)

kubectl -n benzene-mesh port-forward svc/mesh 8080:80
# then, in another shell:
curl -XPOST localhost:8080/mesh/refresh   # {"discovered":3}
open http://localhost:8080/mesh-ui        # the discovered catalog + Topics table (declared), with the
                                          # live Fleet plane merged in (observed) — services as they
                                          # register, heartbeat, and push traces to the mesh's collector
```

Each service's own Spec UI is reachable the same way (`port-forward svc/orders 8081:80` →
`http://localhost:8081/benzene/spec-ui`).

## Deploy to AWS (EKS)

**Actions → Deploy EKS Mesh Example → Run workflow.** The AWS leg of this example, using the same
credentials setup as `Deploy AWS Mesh Example` (the `test` GitHub Environment's
`AWS_ACCESS_KEY_ID`/`AWS_SECRET_ACCESS_KEY`, which additionally need EKS, EC2, and ECR permissions)
and the same per-account S3 state bucket (key `k8s-mesh/`). The workflow:

1. `terraform apply` on `deploy/` — an EKS cluster (`benzene-k8smesh`) with one small managed node
   group on the account's default VPC, plus two ECR repositories. First-time cluster creation takes
   ~10–15 minutes.
2. builds the two images and pushes them to ECR, tagged with the commit SHA.
3. applies the **unchanged** `k8s/` manifests through the `deploy/eks` kustomize overlay, which swaps
   in the ECR images and turns the mesh's NodePort Service into an internet-facing **LoadBalancer** —
   and does the same for each `benzene`-labelled Service, so orders/payments/shipping are directly
   callable from the internet as well.
4. waits for the ELBs, `POST`s `/mesh/refresh`, asserts `{"discovered":3}`, and prints
   `http://<elb-hostname>/mesh-ui` (the catalog with the live Fleet plane merged in)
   plus each service's `http://<elb-hostname>/benzene/spec-ui` URL
   (all in the run summary) — open the Mesh UI to watch the mesh discover the pods, exactly like the
   AwsMesh example's Lambda catalog, or hit a service's `/benzene/spec-ui`, `/benzene/health`, or
   `POST /benzene/invoke` directly.

Same dogfooding, different substrate: discovery is still `Benzene.Mesh.Discovery.Kubernetes` listing
`benzene`-labelled Services via the cluster API — EKS needs no code or manifest changes, only images
it can pull and a route in.

**Costs & teardown:** an EKS control plane bills ~$0.10/hour plus two `t3.small` nodes and four
classic ELBs (mesh + the three services, one per LoadBalancer Service). Re-run the workflow with
**destroy = true** to tear it all down (it deletes the namespace first so Kubernetes releases the
ELBs, then `terraform destroy`). Note the services are exposed **unauthenticated** — fine for this
throwaway demo, not a pattern to copy for real workloads.

To deploy from a laptop instead of CI, run the same four steps by hand: `terraform apply` in
`deploy/`, push the images to the ECR repositories it outputs, `aws eks update-kubeconfig`, then
`kustomize edit set image` + `kubectl apply -k` in `deploy/eks` (the workflow is the reference
script for the exact commands).

## OpenTelemetry

Both the services and the mesh wire **full OpenTelemetry** (`AddOpenTelemetry` + Benzene traces/metrics
over OTLP, plus `UseW3CTraceContext`/`UseBenzeneEnrichment`/`UseBenzeneMetrics` on the pipeline). Set
`OTEL_EXPORTER_OTLP_ENDPOINT` (e.g. an in-cluster collector Service) to export; unset, it no-ops.

## Known first-deploy iteration points
- **.NET 10 base images** — the Dockerfiles use `mcr.microsoft.com/dotnet/{sdk,aspnet}:10.0`; pin to
  the exact tag your registry has if `10.0` doesn't resolve.
- **RBAC scope** — discovery is scoped to the `benzene-mesh` namespace (`MESH_NAMESPACE`); widen the
  Role to a ClusterRole if you point it across namespaces.
