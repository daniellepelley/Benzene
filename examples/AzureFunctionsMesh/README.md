# Azure Functions Mesh — end-to-end example (purely Azure Functions)

The all-**Azure Functions** counterpart of `examples/AzureMesh` (which hosts the same services as Web
Apps for Containers). Here **every** component is an isolated-worker Azure Function:

- **3 Cloud Services** (`orders` / `payments` / `shipping`) — one deployable, `MESH_SERVICE` selects
  the domain — each an HTTP-triggered Function exposing the full Cloud Service Profile
  (`/benzene/spec`, `/benzene/health`, `/benzene/invoke`, `/benzene/spec-ui`). Each Function App is
  tagged `benzene = "true"` for discovery.
- **1 mesh** — a Function App with **two** triggers: a catch-all **HTTP trigger** serving the Mesh UI
  (`/mesh-ui`) + catalog artifacts, and a **timer trigger** running the discovery + aggregation pass
  on a schedule. It discovers the tagged sites via Azure Resource Manager (managed identity),
  interrogates each over HTTPS, and persists the catalog to **Blob Storage**.

The point of this example is to prove Benzene's central promise across a second Azure hosting model:
**the same handlers, the same Cloud Service Profile, the same mesh — now on Functions** — and that the
mesh discovers and interrogates Function-hosted services identically to Web Apps.

## Why it works the same

Benzene's Azure discovery enumerates `Microsoft.Web/sites`, and a **Function App is a
`Microsoft.Web/sites`** (kind `functionapp`) — so the *exact same* `AzureAppServiceDiscoveryProvider`
finds it, builds `https://{name}.azurewebsites.net/benzene/spec|health`, and the aggregator's
`HttpMeshServiceSource` interrogates it. Two requirements make a Function interrogable, both handled
here:

1. **`/benzene/*` at the root.** Function HTTP triggers default to an `/api` route prefix; `host.json`
   sets `"routePrefix": ""` so the paths match the mesh's default discovery URLs. (Alternatively, the
   `benzene:spec-path` / `benzene:health-path` discovery tags can point at `/api/benzene/...`.)
2. **Anonymous auth** on the catch-all HTTP trigger (`AuthorizationLevel.Anonymous`) — the aggregator
   carries no function key. Same public-exposure posture as spec/health on the Web App example.

## How the pieces map to the Web App example

| Concern | Web App example (`AzureMesh`) | This example (Functions) |
|---|---|---|
| Cloud Service host | ASP.NET Core container, `app.UseHttp(...)` | HTTP-triggered Function, `app.UseHttp(...)` (identical pipeline) |
| Cloud Service Profile | `UseBenzeneCloudService(...)` | **the same** `UseBenzeneCloudService(...)` — it only needs an `IHttpContext` |
| Mesh UI + artifacts | ASP.NET Core container | HTTP-triggered Function (`UseMeshUi` + `UseMeshArtifacts`) |
| Periodic aggregation | `BackgroundService` (always-on) | **timer trigger** (`UseTimerTrigger`) — a Consumption Function has no always-on thread |
| On-demand refresh | `POST /mesh/refresh` | **the same** `POST /mesh/refresh` handler |
| Discovery | `Benzene.Mesh.Discovery.Azure` | **the same** package |
| Catalog store | `Benzene.Mesh.Azure.Blob` | **the same** package |

## Projects

| Path | What it is |
|---|---|
| `Service/` | the Cloud Service Function (one deployable, `MESH_SERVICE` selects the domain), published 3× |
| `Mesh/` | the mesh Function (HTTP: UI + artifacts + refresh; timer: aggregation), Azure discovery + Blob store |
| `deploy/` | Terraform: storage, Consumption plan, 4 Function Apps, managed identity + role assignments |

## Run it locally

Each project is a standard isolated-worker Function app (`func start`). The mesh needs a reachable Blob
endpoint (`MESH_BLOB_URI`) and, for discovery, Azure credentials the ARM client can use
(`DefaultAzureCredential` — e.g. `az login` locally). Discovery + live interrogation are only fully
exercised against real Azure (managed identity + deployed sites), like the sibling examples — locally
the value is that the whole thing **builds and wires up** and each Function starts.

```bash
# from examples/AzureFunctionsMesh
dotnet build Benzene.Example.AzureFunctionsMesh.sln
# service (pick a domain)
cd Service && MESH_SERVICE=orders func start
# mesh (needs MESH_BLOB_URI + az login); serves /mesh-ui
cd ../Mesh && func start
```

## Deploy it (GitHub Actions)

The **Deploy Azure Functions Mesh Example** workflow
(`.github/workflows/deploy-azure-functions-mesh-example.yml`) is manual-only
(**Actions → Deploy Azure Functions Mesh Example → Run workflow**). Put an Azure service principal in
the **`test`** GitHub Environment as `AZURE_CREDENTIALS` (the `azure/login` JSON), with rights to
create the resource group, storage, App Service plan, Function Apps, and **to assign roles** (Owner or
User Access Administrator). Supply a globally-unique storage-account name (it defaults to
`benzenefnmesh` and **must differ from other examples'** — the remote state is kept in a
`<name>tfstate` account, so reusing the AzureMesh example's `benzenemesh` collides on that globally-
unique name and fails `terraform init` with a 404); it runs `terraform apply`,
then `func azure functionapp publish` for each of the four apps, and prints the URLs
(`mesh_ui_url`, `mesh_refresh_url`, `service_spec_ui_urls`).

`deploy/` provisions a storage account (Functions runtime + the `mesh` blob container), a Linux
Consumption plan, the four Function Apps (3 tagged services + the mesh), and the mesh identity's role
assignments (**Reader** on the resource group to list sites, **Storage Blob Data Contributor** on the
storage account to read/write the catalog). The three services share one deployable with `MESH_SERVICE`
set per app; the mesh is scoped to this subscription + resource group via `MESH_SUBSCRIPTION_ID` /
`MESH_RESOURCE_GROUP`. To deploy by hand, `terraform apply` then
`func azure functionapp publish <name>` from each project.

## Known first-deploy iteration points

Live Azure behaviour is only verifiable on a real deploy (as with every mesh example); the likely
first-run tweaks:
- **Cold start vs the 10s fetch timeout** — an idle Consumption-plan Function may cold-start past the
  aggregator's per-service fetch timeout and flash *Unreachable* until warm. `RunOnStartup = true` on
  the timer warms the mesh; a Premium/dedicated plan avoids it for the services.
- **`routePrefix`** — if `/benzene/spec` 404s, confirm `host.json`'s `"routePrefix": ""` took effect
  (or set the `benzene:spec-path`/`benzene:health-path` tags to `/api/benzene/...`).
- **Managed-identity propagation** — a `time_sleep` gives the mesh identity time to propagate before
  its role assignments apply; the roles can still take a minute to take effect (an early pass returns
  `0`, the timer retries).
- **`.NET 10` isolated on Functions** — classic Linux Consumption (Y1) does not offer the newest
  .NET stacks (those land on Flex Consumption first), which made a framework-dependent .NET 10
  deploy fail its trigger sync on every app (the host couldn't start the worker). The workflow
  therefore publishes the apps **self-contained** (`dotnet publish -r linux-x64 --self-contained`) —
  the package carries its own .NET 10 runtime — and the Terraform stack pin (`var.dotnet_version`,
  default `8.0`) only needs to be a version the plan supports, not the version the apps target.
  If your region's plan does support the apps' .NET version natively, setting `dotnet_version` to
  it also works with a framework-dependent deploy.
