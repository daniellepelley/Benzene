# Azure Mesh Self-Discovery — end-to-end example

The Azure counterpart of `examples/AwsMesh`: three Benzene Cloud Services running as **Azure Web Apps
for Containers**, plus a **mesh Web App** that discovers them **by resource tag** via Azure Resource
Manager, interrogates each over HTTPS, and serves the Mesh UI — with the catalog persisted to **Blob
Storage**.

## Architecture

```
        Azure resource group: benzene-mesh-rg
  ┌──────────┬───────────┬────────────┐
  │ orders   │ payments  │ shipping   │   3 Web Apps for Containers (one image, MESH_SERVICE selects
  │ Web App  │ Web App   │ Web App    │   the domain); each tagged  benzene = "true"
  └────┬─────┴─────┬─────┴─────┬──────┘
       │  1. list Microsoft.Web/sites (tag benzene) via Azure Resource Manager (managed identity)
       │  2. GET https://<name>.azurewebsites.net/benzene/spec|health   (interrogate)
       ▼
   ┌────────┐  writes manifest.json / services/*.json / topics.json / registry.json
   │  mesh  │  to Blob Storage; serves the Mesh UI at /mesh-ui
   └────────┘
```

- Discovery is `Benzene.Mesh.Discovery.Azure` (`AzureAppServiceDiscoveryProvider`): it lists the
  subscription's `Microsoft.Web/sites` carrying the `benzene` tag and emits **HTTP** registry entries
  at their default hostnames — so the aggregator's existing `HttpMeshServiceSource` interrogates them.
- The store is `Benzene.Mesh.Azure.Blob` (`BlobMeshArtifactStore`).
- The mesh Web App has a **system-assigned managed identity** with **Reader** on the resource group
  (to list the sites) and **Storage Blob Data Contributor** on the storage account (to read/write the
  catalog). The mesh's own Web App is **not** `benzene`-tagged, so it never discovers itself.

## Projects

| Path | What it is |
|---|---|
| (shared) `examples/K8sMesh/Service` | the Cloud Service image, reused as-is for the 3 Azure Web Apps |
| `Mesh/` | the Azure mesh service (Azure discovery + Blob store + UI, 30s background pass + `POST /mesh/refresh`) |
| `deploy/` | Terraform: ACR, storage + container, App Service plan, 4 Web Apps, managed identity + role assignments |
| `.github/workflows/deploy-azure-mesh-example.yml` | build+push images to ACR → `terraform apply` |

## Deploy it (GitHub Actions)

1. Put an Azure service principal in the **`test`** GitHub Environment: `AZURE_CREDENTIALS`
   (the `azure/login` JSON) with rights to create the resource group, ACR, storage, App Service, and
   **to assign roles** (Owner or User Access Administrator on the subscription/RG).
2. **Actions → Deploy Azure Mesh Example → Run workflow** — supply a globally-unique ACR name and
   storage-account name. It creates the registry, builds + pushes both images, then applies the stack.
3. Grab the URLs from the final `terraform output`:
   - `mesh_ui_url` — the Mesh UI (service catalog + the cross-service **Topics** table).
   - `service_spec_ui_urls` — each service's Spec UI (with its **Benzene utilities** panel).
   - `mesh_refresh_url` — `POST` to force a discovery+aggregation pass (returns `201 {"discovered":3}`).

## Known first-deploy iteration points

Like the AWS example, the live Azure behaviour is only verifiable on a real deploy; the likely
first-run tweaks (all localized):
- **.NET 10 base images** — the Dockerfiles use `mcr.microsoft.com/dotnet/{sdk,aspnet}:10.0`; pin to
  the exact tag your registry has if `10.0` doesn't resolve.
- **Web App container port** — set via `WEBSITES_PORT=8080` (the container listens on `PORT=8080`);
  if a site returns 502 on cold start this is the first thing to check.
- **Role-assignment propagation** — the mesh identity's Reader/Blob roles can take a minute to take
  effect; an early discovery pass may return `0` until they propagate (the background pass retries).
- **Managed-identity blob auth** — `DefaultAzureCredential` uses the Web App's managed identity in
  Azure; ensure the identity has **Storage Blob Data Contributor**, not just control-plane access.
