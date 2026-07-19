# Mesh UI

**`Benzene.Mesh.Ui`** ships two self-contained, dependency-free HTML viewers over Benzene's
[service mesh](specification/mesh.md) — the same visual family as [Spec UI](spec-ui.md), styled
with the same design tokens, but one level up: catalog-of-services instead of catalog-of-topics.

| Page | Shows | Data source | Deployment |
|---|---|---|---|
| **Mesh Explorer** (`mesh-ui.html`) | A published mesh artifact snapshot: service health, contract drift, cross-service topics and topology | `manifest.json`/`services/*.json` (and optionally `topics.json`/`topology.json`) produced by `Benzene.Mesh.Aggregator` | Primarily a **static file host** — the realistic case is copying the HTML next to the aggregator's generated JSON |
| **Fleet view** (`mesh-fleet-ui.html`) | The **live** derived fleet: services with health and reduced-feed markers, the topic catalog with observed consumers, and recent trace flows | Polls a `Benzene.Mesh.Collector`'s `mesh:query:fleet` topic every 2 seconds, over the wire-envelope endpoint | Served by a running Benzene app — there is no static/offline mode, since it has nothing to render without a live collector to poll |

Both pages are theme-aware (light/dark) and render with no external requests, so they work
offline (Mesh Explorer) or behind strict CSPs (both).

## Mesh Explorer

Shows a stats bar (total/healthy/unhealthy/unreachable/drift counts) and a searchable list of
service cards — name, an optional owning-team label, status badge, drift badge, links to the
service's raw spec/health URLs. Expanding a card lazily fetches that service's per-service JSON
and renders its health-check detail (type, status, dependencies, data). When the aggregator also
published `topics.json` and `topology.json`, the page additionally renders the cross-service topic
catalog and a sortable edge table (client, server, source, req/min, error rate, p50/p95/p99
latency) — either section is silently hidden if its file is missing, since most deployments won't
have both wired up.

The topic catalog is keyed by **(topic, version)**, not topic id alone, and shows both directions
of exposure — **Producers** (services whose spec declares sending it) and **Consumers** (services
that handle it, with their HTTP mappings) — plus a computed **Status**: `deprecation candidate`
(produced somewhere, consumed nowhere — a prompt to check whether it's safe to retire, not proof
that it is) or `gap` (consumed somewhere with no HTTP mapping, produced nowhere in this fleet —
often a legitimate third-party or non-Benzene producer, worth confirming rather than an error).
Both are informational signals computed by the aggregator across every service's self-description
at once — no individual service is ever asked whether its own topics are still in use. See
`Benzene.Mesh.Aggregator/CLAUDE.md`'s "Aggregated topic catalog" section for the full computation,
and `work/service-mesh-roadmap-1.0.md` §10.8–§10.9 for the design rationale.

Every producer/consumer name is clickable — it scrolls to, opens, and briefly highlights that
service's card above, so "who's still consuming this" is never more than one click away from "is
that service actually healthy right now." A card hidden by an active search filter is revealed
automatically when you jump to it this way.

**Clicking a topic id opens its topic view** — a dialog grouping every version of that topic
together (the flat table is one row per (topic, version); the dialog is the "everything about
this topic, across every version" answer). Each version gets its own producers/consumers/status
block, so "is anything still consuming `shipping:booked` v1 while v2 is live" is answered at a
glance instead of scanning the table for every row that happens to share a topic id. Producer/
consumer chips inside the dialog are the same jump-to-service links as the table.

### Serving it

Transport-agnostic HTTP middleware — works on AWS Lambda API Gateway, Azure Functions, ASP.NET
Core, or the self-host server, though **the primary path is a plain static file host**: publish
`mesh-ui.html` into the same directory/bucket the aggregator writes its artifacts to.

```csharp
using Benzene.Mesh.Ui; // UseMeshUi

app.UseApiGateway(http => http
    .UseMeshUi()                                   // serves GET /mesh-ui, fetching ./manifest.json
    .UseMessageHandlers()
);

// Customise the path and/or the manifest URL it fetches:
app.UseApiGateway(http => http
    .UseMeshUi("/dashboard", "https://cdn.example.com/mesh/manifest.json")
    .UseMessageHandlers()
);
```

Browse to `/mesh-ui`. With no manifest reachable it falls back to `?url=` on the query string, a
`data-manifest-url` attribute, or (if none of those resolve) a built-in sample so the layout is
visible standalone.

### Serving it yourself

```csharp
var html = MeshUiPage.GetHtml("https://cdn.example.com/mesh/manifest.json");
// write `html` with content-type "text/html"
```

## Fleet view

The live counterpart: five summary tiles (services, topics, invocations, errors, unhealthy), then
three tables — **Services** (health, runtime, cloud, binding, topic/instance counts, reduced
feeds), **Topic catalog** (providers, observed consumers, invocations, errors, average duration,
status breakdown), and **Recent flows** (one row per trace: participating services in call order,
event count, duration, outcome). Everything here is *derived* — from registered descriptors,
heartbeats, and trace events the collector has actually ingested — never hand-declared, per
[mesh.md §2–§4](specification/mesh.md).

Because it polls live, an unreachable collector shows "collector unreachable — retrying" rather
than an empty or stale page, and keeps retrying every 2 seconds.

### Serving it

```csharp
using Benzene.Mesh.Ui; // UseMeshFleetUi

app.UseApiGateway(http => http
    .UseMeshFleetUi()                              // serves GET /benzene/fleet-ui
    .UseMessageHandlers()
);

// Point it at a collector reachable at a different URL (same-origin path or absolute):
app.UseApiGateway(http => http
    .UseMeshFleetUi("/benzene/fleet-ui", "https://collector.example.com/benzene/invoke")
    .UseMessageHandlers()
);
```

Add this to the pipeline that fronts your `Benzene.Mesh.Collector` — the page polls
`mesh:query:fleet` on the same wire-envelope endpoint (`/benzene/invoke` by default) that
services use to register, heartbeat, and export traces. See
[`examples/Mesh/README.md`](../examples/Mesh/README.md) for a runnable end-to-end demo
(`./run.sh`) with real services registering, heartbeating, and tracing into a live Fleet view.

### Serving it yourself

```csharp
var html = MeshFleetUiPage.GetHtml("https://collector.example.com/benzene/invoke");
// write `html` with content-type "text/html"
```

## Which one do I want?

- Publishing periodic snapshots from an aggregator that polls each service's `/spec` and
  `/healthcheck` — **Mesh Explorer**.
- Running a `Benzene.Mesh.Collector` that services actively register, heartbeat, and trace
  into — **Fleet view**. This is the [Cloud Service Profile](specification/cloud-service-profile.md)'s
  intended visibility surface (its R6 requirement provisions exactly the feeds this page reads).

Nothing stops running both against the same fleet — they read two different pipelines
([mesh.md §9](specification/mesh.md#9-relationship-to-the-existing-net-mesh-packages-informative)
covers how the two converge.
