# Third-Party Tool Integrations — research

**Date:** 2026-08-13
**Status:** ad-hoc research; no implementation started. Assesses whether Benzene should build
tools that link into useful third parties — observability platforms (Datadog and friends),
developer portals, and IDE tooling (ReSharper / Visual Studio / Rider) that assists with or
monitors Benzene.

## 1. The question

Is there a real opportunity for Benzene-branded integrations into third-party tools — e.g. a
Datadog plugin, or tooling inside ReSharper / Visual Studio that assists with Benzene
development or monitoring? Which are worth building, which are already effectively covered, and
where would each live?

## 2. Why Benzene is unusually well-positioned

The spec already defines every surface a third-party integration needs, language-neutrally:

- **OpenTelemetry-native**: services emit OTel traces/metrics and join W3C trace context
  ([cloud-service-profile.md](../docs/specification/cloud-service-profile.md) R8;
  [mesh.md](../docs/specification/mesh.md) §3). The .NET port ships `Benzene.OpenTelemetry` and
  is deliberately exporter-agnostic.
- **Well-known HTTP surfaces**: `/benzene/health`, `/benzene/spec`, `/benzene/invoke` at
  spec-pinned default paths (profile R3–R5, R7) — a scraper can probe any profiled service with
  zero per-service configuration.
- **Machine-readable self-description**: the ServiceDescriptor (topics, versions, JSON schemas,
  placement, `descriptorHash`) and the derived spec, from which OpenAPI/AsyncAPI/clients/infra
  are already generated ([code-generation.md](../docs/guides/code-generation.md)).
- **An external audit tool already exists**: `Benzene.CloudService.Probe` / `benzene
  profile-check --url` (profile §5) — BCL-only, language-neutral, tri-state verdicts.

So most integrations are *thin adapters over contracts that already exist*, not new machinery.
That is the core finding: the cost side of every row in §5 is low-to-moderate precisely because
the spec did the hard part.

## 3. Observability platforms

### 3.1 Datadog — three distinct plays, in ascending effort

**(a) Zero-code: OTLP export + a cookbook.** Datadog ingests OTLP through several supported
paths — [OTLP ingest in the Datadog Agent](https://docs.datadoghq.com/opentelemetry/setup/otlp_ingest_in_the_agent/),
the [Datadog Distribution of the OTel Collector](https://docs.datadoghq.com/opentelemetry/setup/agent/),
the upstream Collector's Datadog exporter, and a [direct OTLP intake endpoint](https://docs.datadoghq.com/opentelemetry/setup/otlp_ingest/).
Because Benzene is already OTel-native and exporter-agnostic, **Benzene services show up in
Datadog APM, service maps, and trace views today with no Benzene-side code at all.** The gap is
purely documentation: a "Benzene → Datadog" cookbook per port (mirroring the existing
`distributed-tracing-opentelemetry` cookbook) showing the OTLP endpoint config and which
Benzene semantic attributes (topic, version, status) to facet on. Near-zero effort, immediate
credibility. The same one-cookbook-per-vendor move covers New Relic, Honeycomb, Dynatrace,
Grafana Cloud — **do not build per-vendor exporters; OTLP is the integration.**

**(b) A Datadog Agent integration ("the plugin" proper).** Datadog supports
[custom Agent-based integrations](https://docs.datadoghq.com/extend/integrations/agent_integration/)
(a Python check, distributable via the community
[integrations-extra repo](https://docs.datadoghq.com/agent/guide/use-community-integrations/) or
the Marketplace). A `benzene` check would scrape the spec-pinned surfaces — `/benzene/health`
for per-check service health, the descriptor/derived spec for topic inventory and
contract-drift detection via `descriptorHash` — and emit Datadog service checks + tags
(`benzene_topic`, `benzene_status`, `runtime`, `placement.cloud`). This is exactly what the
Cloud Service Profile was designed to enable: the check works against *any* profiled service in
any language, unmodified. Moderate effort (Python, Datadog's dev tooling, a listing process),
and a genuine marketing artifact — "Benzene is in the Datadog integrations catalog" is an
adoption signal money can't easily buy.

**(c) Datadog as a mesh data source.** The mesh read path already has the ports for this —
`IMeshTraceSource` (X-Ray, Tempo, Jaeger today) and `IMeshUsageSource` (CloudWatch, App
Insights) per [mesh-enterprise-readiness.md](mesh-enterprise-readiness.md) §2.1. A
`Benzene.Mesh.Tracing.Datadog` / usage source reading Datadog's APM + metrics APIs would let an
estate that standardized on Datadog power the mesh fleet view from it, exactly as Tempo/X-Ray
estates do. This slots straight into the enterprise-readiness roadmap's slice 4 ("new sources")
and inherits its config-catalog story (`"fleet": { "source": "datadog" }`). Effort is one
adapter package in benzene-dotnet; the seam exists.

One caution on (c): it deepens the already-flagged dependency on per-backend metric/trace
naming conventions. The unsettled cross-port usage-counter name
(`benzene.messages.processed`, flagged in enterprise-readiness §5) should be settled **before**
a third backend hard-codes it.

### 3.2 Grafana / Prometheus

Partially built already (`Benzene.Mesh.Tracing.Tempo`, Prometheus-derived topology). The
missing third-party-facing artifact is cheap and visible: a **published Grafana dashboard pack**
(dashboards-as-JSON on grafana.com) for Benzene's OTel metrics — topic throughput, status
breakdown, duration histograms, health rollup. Requires the same metric-name convention
decision as above; arguably that decision's first deliverable *is* the dashboard pack, since it
makes the convention observable and testable.

### 3.3 Everything else

Covered by OTLP (§3.1a). Resist vendor-specific exporters; accept vendor-specific *cookbooks*
freely, and vendor-specific *mesh sources* only when an estate-fit case shows up (the
enterprise-readiness ranking: sources are a softer veto).

## 4. Developer-facing tooling

### 4.1 Roslyn analyzers + source generator (NuGet) — the right "Visual Studio/ReSharper tool"

The highest-leverage IDE play is not an extension at all: **ship analyzers inside the Benzene
NuGet packages.** Analyzers travel with the package reference, run in Visual Studio, in
`dotnet build`/CI, and in Rider (Rider [executes Roslyn analyzers](https://www.jetbrains.com/help/rider/Settings_Roslyn_Analyzers.html),
including [NuGet-delivered ones](https://blog.jetbrains.com/dotnet/2018/03/22/roslyn-analyzer-support-rider-2018-1-eap/));
no marketplace listing, no separate install, no version skew. Candidate diagnostics fall
straight out of the spec:

- **Reserved-prefix misuse**: registering an application handler on a `benzene:` topic id.
- **Registry bypass** (profile R2): routable topics served outside the handler registry — the
  service silently can't claim the profile; an analyzer makes the drop visible at compile time.
- **Schema-derivation traps** (mesh §2.1): registered request/response types that degrade to
  `{}` (dynamic values, custom serializers, recursion) — legal, but worth an info-level "this
  topic will publish an unconstrained schema".
- **Topic id/version literals**: typo-prone stringly-typed ids; an analyzer plus a code-fix
  suggesting the port's constants idiom.
- The existing Roslyn **source generator** direction (compile-time handler discovery, already
  noted on the website's production story) shares infrastructure with this.

Important nuance for the original question: **classic ReSharper (the VS add-in) does not
execute Roslyn analyzers — Visual Studio itself does, and Rider does.** So "a ReSharper
plugin" specifically would be a separate JetBrains-SDK plugin with its own inspections — high
maintenance, duplicating what analyzers give everywhere else. Verdict: analyzers yes
(benzene-dotnet repo), dedicated ReSharper/Rider plugin no. A VSIX Visual Studio extension is
likewise dominated by analyzers for assist-type features; revisit only if a *visual* feature
(see 4.3) proves wanted.

### 4.2 VS Code extension

Would serve the TypeScript/Python ports (whose users live in VS Code) and could be
spec-aware rather than language-aware: validate conformance-fixture JSON, complete topic ids
against a running service's derived spec or a mesh collector, "invoke this topic" via
`/benzene/invoke`, render a descriptor. Moderate cost, real DX upside, natural home in a new
`benzene-vscode` repo. Second priority behind analyzers.

### 4.3 "Monitor Benzene from the IDE"

The mesh UI already is the monitoring surface, deliberately a static single HTML page. An IDE
webview wrapper around it (VS Code panel / VS tool window pointing at a collector) is cheap
*because* of that design — but it's a convenience shell, not a product. Park it; it falls out
for free if 4.2 happens.

### 4.4 Developer portals & contract ecosystems

- **Backstage**: a Benzene catalog entity provider that feeds Backstage's software catalog from
  a mesh collector (services, APIs from derived OpenAPI/AsyncAPI, health) matches Backstage's
  opt-in entity-provider model and the mesh PO's own benchmark list (Datadog service maps,
  Backstage, AsyncAPI Studio). Worthwhile once a collector deployment story (enterprise slices
  1–2) is shipped; premature before it.
- **AsyncAPI / OpenAPI tooling**: already the strategy — generation *from the description*
  makes every OpenAPI/AsyncAPI consumer (gateways, doc portals, Studio) an integration Benzene
  gets for free. No new work; keep the generators healthy.
- **CI**: a **GitHub Action wrapping `benzene profile-check --url`** (post-deploy conformance
  gate) is a tiny, high-signal integration — the probe CLI exists; the Action is packaging. The
  conformance-drift workflow overlay in `work/repo-split/overlay/` shows the house already
  thinks this way.

## 5. Ranking

| # | Integration | Value | Effort | Where |
|---|---|---|---|---|
| 1 | OTLP vendor cookbooks (Datadog first) | High — instant "works with Datadog" | Trivial (docs) | each port's docs + website |
| 2 | Roslyn analyzers in Benzene NuGets | High — compile-time spec conformance, VS + Rider + CI | Low–moderate | benzene-dotnet |
| 3 | GitHub Action for `profile-check` | Medium — CI conformance gate, cheap credibility | Low | new tiny repo |
| 4 | Grafana dashboard pack | Medium — forces/ships the metric-name convention | Low (after convention settled) | benzene-dotnet or new repo |
| 5 | Datadog Agent integration (catalog listing) | Medium–high — discoverability in Datadog's catalog | Moderate | new repo (Python) |
| 6 | Datadog mesh trace/usage source | Medium — estate-fit; roadmap slice 4 shape | Moderate | benzene-dotnet |
| 7 | VS Code extension | Medium — TS/Python DX, spec-aware | Moderate | new repo |
| 8 | Backstage entity provider | Medium, later — needs shipped collector story | Moderate | new repo |
| — | ReSharper/Rider plugin, VSIX | Dominated by #2 | High | declined |
| — | Per-vendor OTel exporters | Dominated by #1 | — | declined |

## 6. Spec impact — almost none, deliberately

Every item above consumes existing wire contracts; none adds one. Two touchpoints:

- The **cross-port metric-name convention** (enterprise-readiness §5) becomes load-bearing for
  #4/#6 — settle it (observability-product-owner) before a third consumer hard-codes it.
- If a Datadog Agent check or Backstage provider becomes a real second consumer of the
  `benzene:mesh:query:*` read models, that is the trigger mesh.md §4 names for pinning them.

## 7. Sources

- This repo: `docs/specification/{cloud-service-profile,mesh}.md`,
  `docs/guides/code-generation.md`, `work/mesh-enterprise-readiness.md`,
  `.claude/PRODUCT_OWNERS.md` (existing package inventory incl. `Benzene.OpenTelemetry`,
  `Benzene.Mesh.*`), `website/generator/MarketingPages.cs` (OTel/exporter-agnostic positioning,
  Roslyn source-generator mention).
- Datadog: [OTLP ingest in the Agent](https://docs.datadoghq.com/opentelemetry/setup/otlp_ingest_in_the_agent/) ·
  [OpenTelemetry in Datadog](https://docs.datadoghq.com/opentelemetry/) ·
  [direct OTLP intake](https://docs.datadoghq.com/opentelemetry/setup/otlp_ingest/) ·
  [create an Agent-based integration](https://docs.datadoghq.com/extend/integrations/agent_integration/) ·
  [community/Marketplace integrations](https://docs.datadoghq.com/agent/guide/use-community-integrations/)
- JetBrains: [Rider Roslyn analyzer support](https://www.jetbrains.com/help/rider/Settings_Roslyn_Analyzers.html) ·
  [NuGet-delivered analyzers in Rider](https://blog.jetbrains.com/dotnet/2018/03/22/roslyn-analyzer-support-rider-2018-1-eap/)
