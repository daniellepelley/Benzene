# Mesh UI

The Benzene Mesh UI is a single, self-contained web page for **reviewing a Benzene estate** —
what every service does, the topics it consumes and produces, the payload schemas on those
topics, which versions are running, how contracts are drifting, who calls whom, how much traffic
each topic carries, and which services are healthy — **without reading any source code**.

Like [code generation](code-generation.md), the Mesh UI is **not a feature of any one language**.
It renders the language-neutral mesh catalog artifacts that every conforming aggregator produces
from the [mesh contracts](../specification/mesh.md) and the
[Cloud Service Profile](../specification/cloud-service-profile.md). The same page renders a fleet
of .NET services, a fleet of TypeScript services, a fleet of Go services, or a mixed fleet — it
never knows or cares which port produced the data.

Because it is a cross-language concern, there is exactly **one** Mesh UI. It lives in the
cross-language `benzene` repo at [`mesh-ui/mesh-ui.html`](https://github.com/daniellepelley/Benzene/blob/main/mesh-ui/mesh-ui.html) and is the
**canonical source of truth**. Every language port and the website demo *vendor a verbatim copy*
of it (see [Consumption](#consumption)); none maintains its own. This guide is the contract that
keeps every one of those copies rendering a consistent product.

## 1. What the Mesh UI is for

The reader is someone reviewing the estate to **make and defend a decision** about how it should
evolve — a developer, a business analyst, a product owner, an architect. The UI is an
**estate-comprehension** product first and a monitoring dashboard second. Every view must answer
a real question one of those readers has, and must be legible to a reader who cannot read C#, Go,
or TypeScript.

A conforming Mesh UI lets that reader, without reading source:

1. **See what each service does** — its topics consumed and produced, the request/response/message
   payload schemas on each, and the versions running. This is the estate's functional map and is
   the centerpiece.
2. **See the evolution and viability of the data** — how contracts are changing (drift, version
   mismatches, schema divergence, added/removed topics) and whether the platform as it stands
   right now is viable.
3. **See usage** — how often each topic is exercised and **over which transports**, so "wired up"
   is distinguishable from "actually used."
4. **Judge value and decide** — what is earning its keep versus what is a deprecation candidate,
   with the evidence on the same screen.
5. **Check current state** — service health, surfaced as a prioritised list of problems to act on.
   Health matters but is deliberately not the centerpiece.
6. **Understand the domain and its flows** — what services exist, what each topic represents, who
   calls whom, how events fan out, end to end.

If a reader cannot do one of these against a fully-populated set of artifacts, the UI is
incomplete.

## 2. The data contract

The Mesh UI is **data-driven** from a set of static JSON artifacts, all resolved by **relative
path** from the manifest it loads. These are the read-model artifacts an aggregator/collector
publishes; their shapes are fixed by `Benzene.Mesh.Contracts` in each port and are byte-compatible
across ports. The UI **must not invent fields** — it renders these and degrades gracefully when
any is absent ([mesh.md §6](../specification/mesh.md#6-degradation-normative)).

| Artifact | Answers | Key shape |
|---|---|---|
| `manifest.json` | Which services exist, their health status and contract-drift flag | `{ generatedAtUtc, services: [{ name, status, contractDrift, specUrl, healthUrl, owningTeam?, transports?, snapshotAtUtc? }] }` |
| `services/{name}.json` | One service's full detail: derived spec, spec hash + previous hash (drift), per-check health, fetch error | `{ name, fetchedAtUtc, specJson, specHash, previousSpecHash, contractDrift, health, error }` |
| `topics.json` | The functional map: every topic, its consumers/producers, request/response/message schemas, version, reserved flag, schema-mismatch, and structural changes | `{ generatedAtUtc, topics: [{ topic, version, reserved, consumers[], producers[], status, requestSchema, responseSchema, messageSchema, schemaMismatch, changes[] }], removedTopics[] }` |
| `topology.json` | Who calls whom, with rate/error/latency | `{ generatedAtUtc, edges: [{ client, server, source, requestsPerMinute, errorRate, p50LatencyMs, p95LatencyMs, p99LatencyMs }] }` |
| `usage.json` | How often each topic is exercised, per transport and status, over a window | `{ generatedAtUtc, windowStartUtc, windowEndUtc, entries: [{ topic, version, service, transport, status, count, avgDurationMs, source }] }` |
| `annotations.json` | Human discussion threads keyed to a service or topic (read-only when static) | `{ generatedAtUtc, annotations: [{ id, entity, author, text, createdAtUtc }] }` where `entity` is `service:<name>` or `topic:<topicId>` |
| `asyncapi.json` | An AsyncAPI export of the catalog (optional; surfaced as a download / Studio deep-link) | AsyncAPI document |

Reference fixtures for every one of these live next to the UI in the website demo
([`website/demos/mesh/`](../../website/demos/mesh/)) and are the schema-matched examples a new
port's aggregator output should look like.

### 2.1 Relationship to the wire contracts

These artifacts are **read models**, not wire shapes. A service self-describes on the wire with a
[ServiceDescriptor](../specification/mesh.md#2-servicedescriptor) (`service`, `topics[].id`,
`requestSchema`, `descriptorHash`, …) and reports over the collector topics; the aggregator/
collector projects those wire feeds (plus optional pull sources and a metrics feed) into the
artifacts above. The mapping is [mesh.md §9](../specification/mesh.md#9-relationship-to-the-existing-net-mesh-packages-informative).
The UI consumes the read models so that it is decoupled from how any port collects them — which is
exactly why one page serves every language.

### 2.2 Timestamp portability *(implementer note)*

Timestamp fields (`generatedAtUtc`, `fetchedAtUtc`, `createdAtUtc`, `snapshotAtUtc`,
`windowStartUtc`/`windowEndUtc`) are emitted as **ISO-8601 strings** by the .NET aggregator and as
**epoch-millisecond numbers** by the TypeScript aggregator (`DateTimeOffset` → `number` in the
port). A conforming UI **must accept both** for every timestamp field. The canonical UI already
normalises through `new Date(v)`, which handles either. This is the one known data-shape
divergence between ports at the artifact layer; it is a rendering concern, not a spec change.

## 3. Functional requirements

Every conforming Mesh UI (i.e. every vendored copy) MUST provide the following views, each
feature-detected from the artifacts above and each degrading independently when its artifact is
absent. Section numbers below are the product surface, not spec sections.

### 3.1 Estate overview *(the front door)*
- A headline count strip: total services, healthy / unhealthy / unreachable, and contract-drift
  count, all derived from `manifest.json`.
- A card or row per service showing name, health status, and contract-drift state, linking to the
  service's detail page.
- An **issue inbox**: the estate's scattered problems (unhealthy services, contract drift, schema
  mismatches, deprecation candidates, unreachable services) promoted into one severity-grouped,
  actionable worklist — "what do I need to act on now" — each row linking to the offending service
  or topic. This is how health is surfaced without making health the centerpiece.
- Empty and reduced states must be honest: a missing artifact says so and names what is missing,
  never renders a blank or a lie.

### 3.2 Functional map — topics catalog *(the centerpiece)*
- Every topic from `topics.json`, with its consumers and producers (by service), its
  request/response/message payload schemas rendered as human-readable structure (not raw JSON dump),
  its version, and its reserved flag.
- Contract-evolution signals inline: `schemaMismatch`, `changes[]` (schema-changed,
  consumers-changed, …), and `removedTopics`.
- A per-topic drill-in page: full schemas, who produces and consumes it, its version-compatibility
  picture, its observed usage, and its annotations.
- Benzene's own plumbing topics (`reserved` / the `benzene:*` utility topics) are **hidden by
  default** behind a single "show benzene topics" toggle — a business reader sees the domain, not
  the framework.

### 3.3 Contract drift & version compatibility
- Surface `contractDrift` per service (from `specHash` vs `previousSpecHash`) and `schemaMismatch`
  per topic, always with the "what changed" made legible.
- Reconcile which versions the fleet **produces** against which it **consumes**, so a producer that
  has moved ahead of a consumer is visible.

### 3.4 Usage / traffic
- Render `usage.json`: per-topic exercise counts, broken down by **transport** and status, over the
  feed's window, with the window labelled.
- Every number must reconcile: one number = one source + one window label wherever it appears. A
  count that cannot be re-windowed client-side says so rather than silently mislabelling.

### 3.5 Topology — who calls whom
- A traffic map / graph and a table from `topology.json`: client → server edges with
  requests-per-minute, error rate, and p50/p95/p99 latency where present.
- The per-service page shows that service's own callers and callees.

### 3.6 Value & deprecation
- A view that argues, from evidence, what is earning its keep versus what is a deprecation
  candidate: structural signals from `topics.json` (no declared consumers, removed topics) plus
  observed usage from `usage.json` (zero traffic since it was wired). It **argues from data; it
  never decides** — the reader decides, with the evidence on screen. This is what lets a product
  owner defend retiring, say, `order:legacy-export`.

### 3.7 Per-service detail page
- Health per check (type, status, data, dependencies) from `services/{name}.json`, the derived
  spec, the drift state, the service's slice of the functional map and topology, its usage, and its
  annotations — the full picture for one service on one page.

### 3.8 Annotations *(read on the static floor)*
- Render `annotations.json` threads against the service or topic they key to, so estate decisions
  carry their human context ("finance confirmed nothing reads this anymore"). Writing annotations
  is a backend-gated enhancement (§4).

### 3.9 Cross-cutting quality bars *(non-negotiable)*
- **Self-contained**: a single HTML file, inline CSS and JS, **no CDN, no external fonts, no build
  step, no network calls except the relative mesh JSON artifacts**. It must be statically hostable
  and must operate under a strict Content-Security-Policy.
- **Accessible**: keyboard-navigable, ARIA-labelled, and legible; light/dark parity;
  responsive down to a narrow viewport; honours `prefers-reduced-motion`.
- **Safe with untrusted data**: `specUrl`/`healthUrl` and any URL from a self-reported manifest are
  scheme-checked before becoming a link (no `javascript:`/`data:` execution).
- **Time-to-understanding is the quality bar**: first-run empty states, sample data, and "what am I
  looking at" cues so a reader who has never seen the estate gets oriented fast.

## 4. The static floor and the live plane

The Mesh UI has a **load-bearing floor** and an **optional enhancement layer**, and the boundary is
explicit by design.

- **Static floor (required, cross-language).** Everything in §3 renders from the committed JSON
  artifacts alone, resolved by relative path, with zero backend. This is the whole of the
  cross-language product and is what the website demo and every language example ship.
- **Live plane (optional, backend-gated progressive enhancement).** When — and only when — a live
  envelope endpoint is supplied (via `?fleet=<url>` or a `data-fleet-url` attribute on the document
  root), the UI mounts an additional live-traffic view and enriches the static rows with observed
  behaviour (live invocations, error rates, latency, recent flows, trace/correlation lookup).
  Absent that endpoint, **the live layer never mounts and leaves no trace** — the static floor is
  intact. Writing annotations is a second, independent backend-gated toggle of the same shape.

This is the vessel rule for anything needing a backend and state: name the boundary, feature-detect
it, and degrade to the static floor. A conforming UI MUST preserve the static floor — a deployment
that provides no live endpoint (the website demo, a plain S3-hosted catalog) must render the full
§3 product from artifacts alone.

The live plane's *wire query shape* (the envelope request/response the endpoint speaks) is a
**collector-side idiom, not part of the mesh spec** ([mesh.md §4](../specification/mesh.md#4-collector-topics)
defers the `mesh:query:*` read models). Any port that wants to offer the live plane implements a
compatible endpoint; a port that doesn't still ships the identical static UI.

## 5. Consumption

There is one canonical file, [`mesh-ui/mesh-ui.html`](https://github.com/daniellepelley/Benzene/blob/main/mesh-ui/mesh-ui.html) in this repo.
Every consumer **vendors a verbatim copy** with a provenance marker — the same discipline the
[conformance fixtures](../specification/conformance/README.md) use, and the reason
[git submodules were rejected](../../work/repo-split-plan.md): a copy is diffable, offline, and
cannot break a downstream build when this repo moves.

| Consumer | Vendored as | Notes |
|---|---|---|
| Website demo | `website/demos/mesh/index.html` | Copied verbatim next to the fixture JSON; the site's `CopyDemos` publishes the directory as-is. The demo has no live endpoint, so it renders the static floor. |
| `benzene-dotnet` | `src/Benzene.Mesh.Ui/mesh-ui.html` | The `.NET` host serves this file over `/benzene/mesh-ui`; it may additionally wire the live plane by pointing `data-fleet-url` at its own `/benzene/invoke`. |
| `benzene-typescript` | a `@benzene/mesh-ui` asset (not yet ported) | When the TS port adds a mesh-UI package, it vendors this file rather than authoring a new one. |
| `benzene-go` (future) | its mesh-UI asset | Same: vendor, do not fork. |

**The rule: never fork the copy.** Fixes and features land in the canonical here (with this guide
updated in the same change); consumers re-vendor. A copy that has drifted from the canonical is a
bug. A lightweight check — a byte/-hash comparison of each vendored copy against the canonical, in
each consumer's CI — keeps them honest; the provenance banner at the top of the file names the
canonical path so a reader of any copy knows where home is.

## 6. Reconciliation history *(informative)*

The canonical UI was consolidated from the divergent per-language versions in July 2026:

- **`benzene-dotnet`'s `Benzene.Mesh.Ui/mesh-ui.html`** was the fuller-featured implementation —
  the estate overview, issue inbox, topics catalog, contract-drift and version-compatibility views,
  usage/traffic, topology map, value/deprecation view, per-service/topic/issue drill-ins,
  annotations, AsyncAPI export, and the backend-gated live plane. It is pure, language-neutral
  HTML/CSS/JS that renders the cross-language artifact contract, so it was **promoted to the
  canonical** with a provenance banner and no behavioural change.
- **`benzene-typescript` had no Mesh UI HTML** at consolidation time — its
  `aws-lambda-mesh` example's deploy notes record that `@benzene/mesh-ui` "isn't ported yet." There
  was therefore no separate TypeScript page to harvest from; the TS port's contribution is its
  proof that the **artifact shapes are genuinely cross-language** (`Benzene.Mesh.Contracts` is
  ported field-for-field), which is what makes one UI legitimate.
- **Visual system**: the canonical keeps its established indigo/dark design system and the
  functional HTTP-method colour chips. Aligning the accent to the website's green
  (`site.css --accent`) is a deferred brand decision (see the open items below) — it is a
  one-token-block change and is intentionally not made unilaterally, because the accent is shared
  with the Spec UI family for cross-UI consistency.

## 7. Open items

- **Brand-accent alignment** with the website's green is deferred to a maintainer call (§6).
- **TypeScript mesh-UI package**: when `@benzene/mesh-ui` is ported, it vendors this canonical file;
  it should not re-author a page.
- **Windowable usage feed**: `usage.json` counts carry a baked window and cannot be re-windowed
  client-side; a windowable feed is tracked in `work/service-mesh-roadmap-1.0.md`.
- **Staleness signal**: there is no per-service freshness/`Stale` status in the artifacts yet; the
  UI shows staleness as explicitly deferred rather than inventing it.
- **Companion Spec UI deep-link**: the per-service page links to a `mesh-spec-ui.html` companion;
  where a consumer does not vendor that companion, the link is a best-effort outbound link.
</content>
</invoke>
