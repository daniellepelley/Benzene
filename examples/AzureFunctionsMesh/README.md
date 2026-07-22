# Azure Functions Mesh — end-to-end example (purely Azure Functions)

The all-**Azure Functions** counterpart of `examples/AzureMesh` (which hosts the same services as Web
Apps for Containers). Here **every** component is an isolated-worker Azure Function:

- **6 Cloud Services** — `orders` / `payments` / `shipping` / `inventory` / `notifications` /
  `analytics`, each its own Function App project — that **call each other over Service Bus, Event Hub
  and Event Grid** (see "Interconnectivity" below). Each exposes the Cloud Service Profile as **JSON
  only** (`/benzene/spec`, `/benzene/health`, `/benzene/invoke`) over an HTTP trigger; it hosts **no
  HTML of its own** — the browsable spec view is served by the mesh, not the service. Each Function App
  is tagged `benzene = "true"` for discovery.
- **1 mesh** — a Function App with **two** triggers: a catch-all **HTTP trigger** serving the Mesh UI
  (`/mesh-ui`), the mesh-hosted per-service **Spec UI** (`/mesh-spec-ui.html`, the target of each
  service's *spec* link — it renders the spec the aggregator captured, so services need no UI), and the
  catalog artifacts; and a **timer trigger** running the discovery + aggregation pass
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
| Mesh UI + artifacts | ASP.NET Core container | HTTP-triggered Function (`UseMeshUi` + `UseMeshSpecUi` + `UseMeshArtifacts`) |
| Periodic aggregation | `BackgroundService` (always-on) | **timer trigger** (`UseTimerTrigger`) — a Consumption Function has no always-on thread |
| On-demand refresh | `POST /mesh/refresh` | **the same** `POST /mesh/refresh` handler |
| Discovery | `Benzene.Mesh.Discovery.Azure` | **the same** package |
| Catalog store | `Benzene.Mesh.Azure.Blob` | **the same** package |

## Interconnectivity — one transport per job

The six services form a live fulfilment flow, each Azure transport used for what it's good at (the
`Shared/` project holds the wiring identical across services; each service's `Triggers.cs` declares just
the triggers it uses, via the source generator):

| Transport | Idiomatic for | In this example |
|---|---|---|
| **Service Bus queue** | point-to-point **command**, one consumer | `orders → payments` (`payment:take`), `payments → shipping` (`shipment:book`) |
| **Event Hub** | high-throughput **event stream**, fan-out via consumer groups | `orders` streams `order:placed` → **inventory + notifications** (a consumer group each) |
| **Event Grid** | **routed integration events**, filtered by event type | `payments` publishes `payment:captured`, `shipping` publishes `shipment:dispatched` → **notifications / inventory / analytics** |

Every hop goes through the same Benzene `IBenzeneMessageSender` (`AddOutboundRouting` → `UseServiceBus`
/ `UseEventHub` / `UseEventGrid`), and the receiving side is the matching Benzene ingress
(`app.UseServiceBus` / `app.UseEventHub` / `app.UseEventGrid`) — the **same handler**, no per-transport
code. Each service also **declares** what it sends (spec `events`), so the mesh derives the structural
topology across all six.

> The Event Hub egress↔Functions-trigger round-trip needed a small framework addition (property-based
> Event Hub ingress, `UseEventHub(eh => eh.UseMessageHandlers())`) — see `Benzene.Azure.Function.EventHub`.

## Projects

| Path | What it is |
|---|---|
| `Shared/` | the common wiring (Cloud Service Profile + HTTP) + the lazy Service Bus/Event Hub/Event Grid client helpers |
| `Orders/` `Payments/` `Shipping/` | command-chain services (Service Bus) that also publish events (Event Hub / Event Grid) |
| `Inventory/` `Notifications/` `Analytics/` | pure event consumers (Event Hub and/or Event Grid) |
| `Mesh/` | the mesh Function (HTTP: UI + artifacts + refresh; timer: aggregation), Azure discovery + Blob store |
| `deploy/` | Terraform: storage, Consumption plan, 7 Function Apps, Service Bus + Event Hub + Event Grid, managed identity + roles |

## Run it locally

Each project is a standard isolated-worker Function app (`func start`). The mesh needs a reachable Blob
endpoint (`MESH_BLOB_URI`) and, for discovery, Azure credentials the ARM client can use
(`DefaultAzureCredential` — e.g. `az login` locally). Discovery + live interrogation are only fully
exercised against real Azure (managed identity + deployed sites), like the sibling examples — locally
the value is that the whole thing **builds and wires up** and each Function starts.

```bash
# from examples/AzureFunctionsMesh
dotnet build Benzene.Example.AzureFunctionsMesh.sln
# a service (each is its own project now — Orders/Payments/Shipping/Inventory/Notifications/Analytics)
cd Orders && func start
# mesh (needs MESH_BLOB_URI + az login); serves /mesh-ui
cd ../Mesh && func start
```

The inter-service sends are best-effort: with no Service Bus/Event Hub/Event Grid connection configured
locally, a send just logs and the caller continues — so each Function still starts and serves its Cloud
Service Profile.

## Deploy it (GitHub Actions)

The **Deploy Azure Functions Mesh Example** workflow
(`.github/workflows/deploy-azure-functions-mesh-example.yml`) is manual-only
(**Actions → Deploy Azure Functions Mesh Example → Run workflow**). Put an Azure service principal in
the **`test`** GitHub Environment as `AZURE_CREDENTIALS` (the `azure/login` JSON), with rights to
create the resource group, storage, App Service plan, Function Apps, and **to assign roles** (Owner or
User Access Administrator). Supply a globally-unique storage-account name (it defaults to
`benzenefnmesh` and **must differ from other examples'** — the remote state is kept in a
`<name>tfstate` account, so reusing the AzureMesh example's `benzenemesh` collides on that globally-
unique name and fails `terraform init` with a 404). The workflow runs `terraform apply`, publishes each
of the seven apps (zip deploy), then a **second `terraform apply`** to wire the Event Grid subscriptions
(they need their target functions to exist first), and prints the URLs.

`deploy/` provisions a storage account (Functions runtime + the `mesh` blob container), a Linux
Consumption plan, the seven Function Apps (6 tagged services + the mesh), the **Service Bus** namespace +
queues, the **Event Hub** namespace + hub + consumer groups, the **Event Grid** topic + subscriptions,
and the mesh identity's role assignments (**Reader** on the resource group to list sites, **Storage Blob
Data Contributor** on the storage account). Each service gets exactly the messaging connection strings it
uses; the mesh is scoped via `MESH_SUBSCRIPTION_ID` / `MESH_RESOURCE_GROUP`. To deploy by hand:
`terraform apply`, publish each project (`func azure functionapp publish <name>`), then
`terraform apply -var wire_eventgrid_subscriptions=true`.

## Known first-deploy iteration points

Live Azure behaviour is only verifiable on a real deploy (as with every mesh example); the likely
first-run tweaks:
- **Cold start vs the 10s fetch timeout** — an idle Consumption-plan Function may cold-start past the
  aggregator's per-service fetch timeout and flash *Unreachable* until warm. `RunOnStartup = true` on
  the timer warms the mesh; a Premium/dedicated plan avoids it for the services.
- **`routePrefix`** — if `/benzene/spec` 404s, confirm `host.json`'s `"routePrefix": ""` took effect
  (or set the `benzene:spec-path`/`benzene:health-path` tags to `/api/benzene/...`).
- **`sync-triggers` "Forbidden from extensions API" on first deploy** — after a fresh zip deploy the
  ARM `syncfunctiontriggers` call can fail with `Unauthorized … Encountered an error (Forbidden) from
  extensions API` until the isolated worker has cold-started at least once and the host can reach the
  app's extensions endpoint. This is a **host/worker cold-start race, not the declared-trigger
  codegen** — the generated `functions.metadata` is correct (proven by the *identical* `service.zip`
  succeeding on one service while a sibling times out, and by the mesh — which declares *more*
  triggers, HTTP **and** Timer — syncing first try). The workflow now **warms each app** (an anonymous
  HTTP GET forces the worker up) before every sync-triggers attempt and widens the retry window; a
  manual `func azure functionapp publish <name>`, or simply hitting any anonymous route once, has the
  same effect.
- **Event Grid subscriptions are wired via the extension webhook, after publish + warm** — Event Grid's
  `azure_function_endpoint` validates through an ARM control-plane lookup of the function, which is
  unreliable for isolated-worker functions on a Consumption plan (it fails with *"Destination endpoint
  not found … should pre-exist"* even once the code is deployed). So the subscriptions instead point at
  the Functions **Event Grid extension webhook**
  (`/runtime/webhooks/eventgrid?functionName=<fn>&code=<system-key>`), which is validated against the
  *live* running function — the deploy publishes first, then **warms** each consumer app (an anonymous
  HTTP hit cold-starts the worker so the extension answers the validation handshake and its system key
  exists), then does the second apply (`wire_eventgrid_subscriptions=true`). If a subscription still
  fails validation, the consumer app was likely cold — warm it (hit any route) and re-run.
  - **The second apply must not strip the deployed package.** On Linux Consumption the zip-deploy step
    sets `WEBSITE_RUN_FROM_PACKAGE` to the uploaded package URL. That setting is *not* declared in the
    Terraform `app_settings`, so without care the second apply treats it as drift and removes it —
    un-deploying every function *in the same apply* that then wires the subscriptions. Even with the
    webhook approach above that is fatal: an un-deployed app can't answer the Event Grid validation
    handshake, and its Event Grid extension system key can't be read (`azurerm_function_app_host_keys`
    fails). Both Function App resources therefore carry
    `lifecycle { ignore_changes = [app_settings["WEBSITE_RUN_FROM_PACKAGE"]] }` so the publish step owns
    that key and the post-publish apply leaves the deployed code in place. Re-running the whole workflow
    after this fix self-heals a previously stripped environment (publish re-sets it; the wire apply
    leaves it alone).
- **Event Hub / Service Bus / Event Grid connection strings** — each service gets the connection strings
  it needs as app settings from the namespaces Terraform creates (`ServiceBusConnection`,
  `EventHubConnection`, `EventGridEndpoint`/`EventGridKey`). A missing/blank one only fails the *send*
  (best-effort, logged), not startup — so an interrogable-but-not-yet-wired service still shows healthy.
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
