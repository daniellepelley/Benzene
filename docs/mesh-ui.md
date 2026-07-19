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
service's raw spec/health URLs, and (when its spec advertised any) a chip row of the transports
that service is wired to receive messages over (e.g. `http`, `sqs`) — lifted from the `benzene`
spec's document-level `transports` field (see [Spec](spec.md#transport-advertisement)), silently
omitted for a service whose spec predates the field or advertised none. Expanding a card lazily
fetches that service's per-service JSON and renders its health-check detail (type, status,
dependencies, data). When the aggregator also
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

**Clicking a topic id opens its topic page** — a full view swap (the service list, topic table,
and topology all step aside; a **Back** button returns to them), not a small popup, grouping every
version of that topic together with a section per version. Each version's **Producers** and
**Consumers** sections render the *real* service card for every service involved — the same
accordion, status/drift badges, owning-team label, transport chips, spec/health/spec-ui links, and
lazy health-check detail as the main service list, not just a name — so a producer or consumer can
be drilled straight into from the topic page itself, with no separate lookup, and its transport
chips answer "how would I actually reach this topic on this service" right there. A producer/
consumer name with no matching
entry in the manifest (e.g. a system outside this fleet feeding a `gap` topic) renders as a plain
placeholder instead of a broken card. Every embedded card's own **topics** link still works from
inside the topic page too — it returns to the main view and re-filters the topic table, so drilling
from a topic into one of its services and back into a *different* topic that service touches is a
few clicks, not a dead end.

Opening a topic updates the URL to `…#topic:<id>` — copy it straight out of the address bar to
share or bookmark a link to that exact topic; opening that URL later (or just refreshing) reopens
the same page once `topics.json` has loaded. Leaving the page, however you leave it (the Back
button, Escape, or a link inside it navigating elsewhere), clears the hash again. Browsing between
topics is real navigation — the browser's own Back/Forward buttons move through topic views the
same way clicking around the page does.

**The topic table has its own search box**, matching against the topic id *or* any producer/
consumer service name — useful once a fleet has more topics than fit on screen, and it doubles as
the reverse cross-link: every service card also has a **topics** link that pre-fills this search
with that service's name and scrolls to the table, answering "which topics does `orders-api`
touch" from the service side instead of hunting through rows by hand.

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
