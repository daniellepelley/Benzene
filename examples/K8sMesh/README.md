# Kubernetes Mesh Self-Discovery — end-to-end example

The Kubernetes counterpart of `examples/AwsMesh`: three Benzene Cloud Services running as pods, plus a
**mesh service** that discovers them **by label** via the Kubernetes API, interrogates each over plain
in-cluster HTTP, and serves the Mesh UI. It runs two ways from the same manifests: credential-free on
a throwaway [`kind`](https://kind.sigs.k8s.io) cluster in CI, or on a real **AWS EKS** cluster with
the Mesh UI on the public internet (see "Deploy to AWS (EKS)" below).

## Architecture

```
        Kubernetes namespace: benzene-mesh
  ┌──────────┬───────────┬────────────┐
  │ orders   │ payments  │ shipping   │   3 Deployments (one image, MESH_SERVICE selects the domain)
  │ Service  │ Service   │ Service    │   each Service labelled  benzene: "true"
  └────┬─────┴─────┬─────┴─────┬──────┘
       │           │           │
       │   1. list Services (label benzene=true) via the Kubernetes API
       │   2. GET http://<svc>.<ns>.svc.cluster.local/benzene/spec|health  (interrogate)
       ▼
   ┌────────┐   writes manifest.json / services/*.json / topics.json / registry.json
   │  mesh  │   to /artifacts (pod volume) and serves the Mesh UI at /mesh-ui
   └────────┘   NodePort 30080 → open the UI in a browser
```

- Discovery is `Benzene.Mesh.Discovery.Kubernetes` (`KubernetesServiceDiscoveryProvider`): it lists
  Services carrying the `benzene` label and emits **HTTP** registry entries at their in-cluster DNS —
  so the aggregator's existing `HttpMeshServiceSource` interrogates them, no K8s-specific fetch source.
- The mesh's ServiceAccount has RBAC to **list Services** only (`k8s/mesh.yaml`). The mesh's own
  Service is **not** `benzene`-labelled, so it never discovers itself.
- The catalog lives on the mesh pod's own `emptyDir` volume (single writer + reader) — no blob store.

## Projects

| Path | What it is |
|---|---|
| `Service/` | one ASP.NET Core Cloud Service image; `MESH_SERVICE` picks the domain (orders/payments/shipping) |
| `Mesh/` | the discovery + aggregator + UI service (K8s discovery, filesystem store, 30s background pass + `POST /mesh/refresh`) |
| `k8s/` | manifests: namespace, the 3 Deployments+Services, and the mesh (SA + RBAC + Deployment + NodePort Service), with a kustomize base for target-specific overlays |
| `deploy/` | Terraform for the AWS leg: EKS cluster + node group + the two ECR repositories |
| `deploy/eks/` | kustomize overlay over `k8s/`: ECR images (set by the workflow) + a LoadBalancer mesh Service |
| `.github/workflows/deploy-k8s-mesh-example.yml` | build images → kind → deploy → assert 3 discovered |
| `.github/workflows/deploy-eks-mesh-example.yml` | terraform apply → push images to ECR → deploy → assert 3 discovered → print the public Mesh UI URL |

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
open http://localhost:8080/mesh-ui        # the discovered catalog + cross-service Topics table
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
   in the ECR images and turns the mesh's NodePort Service into an internet-facing **LoadBalancer**.
4. waits for the ELB, `POST`s `/mesh/refresh`, asserts `{"discovered":3}`, and prints
   `http://<elb-hostname>/mesh-ui` — open it to watch the mesh discover the pods, exactly like the
   AwsMesh example's Lambda catalog.

Same dogfooding, different substrate: discovery is still `Benzene.Mesh.Discovery.Kubernetes` listing
`benzene`-labelled Services via the cluster API — EKS needs no code or manifest changes, only images
it can pull and a route in.

**Costs & teardown:** an EKS control plane bills ~$0.10/hour plus two `t3.small` nodes and a classic
ELB. Re-run the workflow with **destroy = true** to tear it all down (it deletes the namespace first
so Kubernetes releases the ELB, then `terraform destroy`).

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
