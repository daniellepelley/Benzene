# Mesh UI

**`Benzene.Mesh.Ui`** ships a self-contained, dependency-free HTML viewer over Benzene's
[service mesh](specification/mesh.md) — the same visual family as [Spec UI](spec-ui.md), styled
with the same design tokens, but one level up: catalog-of-services instead of catalog-of-topics.

The **Mesh Explorer** (`mesh-ui.html`) is a published mesh **artifact snapshot** — service health,
contract drift, cross-service topics and topology — read from the `manifest.json`/`services/*.json`
(and optionally `topics.json`/`topology.json`/`usage.json`) produced by `Benzene.Mesh.Aggregator`.
Its realistic deployment is a **static file host**: copy the HTML next to the aggregator's generated
JSON. The page is theme-aware (light/dark) and renders with no external requests, so it works offline
and behind strict CSPs.

When a **live** [`Benzene.Mesh.Collector`](#the-live-fleet-plane) is reachable, the same page also
grows a **live Fleet plane** — the catalog is the spine and the live data (health, observed-vs-declared
consumers, recent trace flows, and a Fleet landing view) enriches it in place, rather than living on a
separate page. This is opt-in via a single wire-envelope endpoint (see below); with none configured the
page is the static explorer exactly as described above.

## Mesh Explorer

> **2026-07-22:** the explorer has grown into a three-entity product — **Estate**, **Topic**
> (`#topic:<id>`), and **Service** (`#service:<name>`) pages, all hash-deep-linkable — with a
> node-link topology graph above the edge table, per-entity **usage** sections fed by the
> [mesh usage feed](mesh-usage-feed.md) (`usage.json`), an estate-level **Value & deprecation**
> ranking (structural + observed-usage evidence, run-over-run change flags, topics removed since
> the previous run), and per-entity **Discussion** (below). Every artifact stays optional: a
> missing file hides its sections, never breaks the page. The sections below describe the
> original core; see `src/Benzene.Mesh.Ui/CLAUDE.md` for the full, current feature inventory.

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
`Benzene.Mesh.Aggregator/CLAUDE.md`'s "Aggregated topic catalog" section for the full computation.

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

### Discussion & annotations

Topic and service pages carry a **Discussion** section — the decisions the explorer's evidence
provokes ("retire this after finance signs off", "this drift is the planned v2 migration") get
recorded next to the evidence instead of evaporating into chat. It is split across the two data
planes so the static floor survives:

- **Reading is static.** Notes live in `annotations.json`, published into the same artifact store
  as `manifest.json` — any static host serves recorded discussion with zero backend. No artifact
  and no endpoint → the section doesn't exist at all.
- **Writing needs a live endpoint.** Posting goes through the aggregator host's
  `mesh:annotations:add` handler (`POST /mesh/annotations` over HTTP, or the topic on any
  transport the host runs — same dogfooded shape as `mesh:report`, and the same opt-in: a host
  that doesn't discover the handler has no write surface). The explorer feature-detects it via a
  `?annotations=<envelope-url>` query parameter or a `data-annotations-url` attribute, and shows
  the composer only then; otherwise the section is explicitly read-only.
- **Identity is self-declared by design.** A note records the display name the author typed.
  Authenticating who may post — and verifying who they are — belongs to the gateway in front of
  the annotations endpoint, the same boundary ruling as [rate limiting](rate-limiting.md): Benzene
  ships the mechanism, the deployment's edge owns access control. The handler enforces only
  shape (required fields, size bounds).

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

## The live Fleet plane

When you point the page at a live [`Benzene.Mesh.Collector`](specification/mesh.md), the catalog is
enriched with *derived* fleet data — from registered descriptors, heartbeats, and trace events the
collector has actually ingested, never hand-declared, per [mesh.md §2–§4](specification/mesh.md):

- a **Fleet landing view** (five summary tiles — services, topics, invocations, errors, unhealthy —
  a live Services table with health/reduced-feed markers, a topic catalog with observed consumers,
  and Recent flows: one row per trace, expandable to a waterfall);
- per-entity **live sections** on the Service and Topic pages — the declared catalog reconciled with
  what's observed (e.g. declared-vs-observed consumers, with the gap called out);
- correlation-id and trace-id lookups that pivot from a reported failure to every related flow.

Because these poll live, an unreachable collector degrades honestly ("collector unreachable —
retrying") rather than showing an empty or stale page; the static catalog underneath is unaffected.

### Serving it

The Fleet plane is folded into `UseMeshUi` — pass the wire-envelope `envelopeUrl` the page should poll:

```csharp
using Benzene.Mesh.Ui; // UseMeshUi

app.UseApiGateway(http => http
    // The catalog page, enriched with the live Fleet plane polling /benzene/invoke:
    .UseMeshUi("/mesh-ui", "manifest.json", "/benzene/invoke")
    // The collector behind that endpoint (queries only, or queries + ingestion):
    .UseBenzeneMessage(new BenzeneMessageHttpOptions { Path = "/benzene/invoke" },
        collector => collector.UseMessageHandlers(MeshCollectorHandlers.All))
    .UseMessageHandlers()
);
```

The `envelopeUrl` can be a same-origin path (the common case: the mesh host also fronts the collector)
or an absolute URL to a collector reachable elsewhere. Omit it (the default is `null`) and the page is
the static explorer with the Fleet plane dormant. The endpoint the page polls is the same wire-envelope
endpoint (`/benzene/invoke` by default, `MeshUiExtensions.DefaultEnvelopeUrl`) that services use to
register, heartbeat, and export traces. See [`examples/Mesh/README.md`](../examples/Mesh/README.md) for a
runnable end-to-end demo (`./run.sh`) with real services registering, heartbeating, and tracing into the
live Fleet plane, and `examples/AwsMesh/Mesh/Startup.cs` for the AWS wiring (X-Ray + CloudWatch behind
the envelope).

### Serving it yourself

```csharp
// Inject both the manifest URL and the live-fleet envelope URL onto the page:
var html = MeshUiPage.GetHtml("https://cdn.example.com/mesh/manifest.json",
                              "https://collector.example.com/benzene/invoke");
// write `html` with content-type "text/html"
```

This is the [Cloud Service Profile](specification/cloud-service-profile.md)'s intended visibility
surface (its R6 requirement provisions exactly the feeds the Fleet plane reads);
[mesh.md §9](specification/mesh.md#9-relationship-to-the-existing-net-mesh-packages-informative)
covers how the artifact and collector pipelines converge.
