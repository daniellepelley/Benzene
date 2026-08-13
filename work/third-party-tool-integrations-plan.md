# Third-Party Tool Integrations — work plan

**Date:** 2026-08-13
**Status:** plan; no implementation started.
**Source research:** [third-party-tool-integrations.md](third-party-tool-integrations.md) — read
it first; this document turns its ranking (§5) into pickup-cold work packages.

## How to use this document

Each work package (WP) below is written to be picked up by an agent with no other context:
goal, home repo, prerequisites, ordered tasks, acceptance criteria, and a do-not list. WPs are
independently shippable unless a prerequisite says otherwise. Where a WP touches code in
`benzene-dotnet` (or another repo this plan's author could not open), the first task is always
**verify the stated assumption against the actual code** — the research was grounded in this
repo's docs, not a benzene-dotnet checkout.

Suggested reviewer per WP names the matching product-owner agent persona from
`.claude/PRODUCT_OWNERS.md`; treat it as "whose design bar applies", not a gate.

### Sequencing

```
WP0 (metric-name convention) ──► WP4 (Grafana pack) ──► (later) WP6 (Datadog mesh source)
WP1 (Datadog cookbook)        — no dependencies; do first
WP2 (Roslyn analyzers)        — no dependencies
WP3 (profile-check Action)    — no dependencies
WP5 (Datadog Agent check)     — no dependencies (benefits from WP1 landing first for docs links)
WP7 (VS Code extension)       — no dependencies; after WP1–WP3 on value ranking
WP8 (Backstage provider)      — BLOCKED: wait for the enterprise-readiness collector/host slices
```

Recommended pickup order: **WP1 → WP2 → WP3 → WP0 → WP4 → WP5**, then WP6/WP7 as appetite
allows. WP8 stays parked until its trigger fires.

---

## WP0 — Settle the cross-port usage-metric convention

**Goal:** one documented, language-neutral name/shape for the Benzene OTel metrics (at minimum
the processed-messages counter currently emitted by .NET as `benzene.messages.processed`), so
dashboards and metrics-store readers don't fracture per port.
**Home:** this repo (the documented home is the deliverable — likely a short
`docs/specification/` or `docs/guides/` observability-conventions page; deliberately **not**
normative mesh-spec material, per [mesh-enterprise-readiness.md](mesh-enterprise-readiness.md) §5).
**Reviewer:** observability-product-owner (decision owner), mesh-product-owner (consumer).

Tasks:
1. Inventory what each port emits today: grep benzene-dotnet for `benzene.messages.processed`
   and any other `benzene.*` meter/counter names, and check go/typescript/python ports for any
   metric emission at all.
2. Decide the convention: metric names, units, and the attribute set (candidate attributes:
   `topic`, `version`, `status`, `transport`, `service`). Follow OTel semantic-convention
   naming rules (dot-separated, lowercase). Include the duration histogram if one exists.
3. Write the conventions page: name, instrument type, unit, required/optional attributes, and
   an explicit "additive changes only" stability note. State that it is a cross-port
   *convention*, not a wire contract, and why (spec tautness).
4. File follow-up issues (or notes) in each port repo to align emission with the page.

Acceptance: the page exists, is linked from the mesh enterprise-readiness note's §5 gap, and
names every metric the CloudWatch/App Insights usage sources currently read back.
Do NOT: put this in `mesh.md` as normative text; rename existing .NET metrics in the same
change (alignment is per-port follow-up work).

---

## WP1 — Datadog (and friends) OTLP cookbook

**Goal:** a "Benzene → Datadog" guide proving Benzene services light up Datadog APM/service
maps with zero Benzene-side code, establishing the "OTLP is the integration" position.
**Home:** language-neutral concept page in this repo (`docs/guides/` — sits beside
`code-generation.md`); the runnable per-port cookbook belongs in benzene-dotnet's docs
(mirroring its existing `distributed-tracing-opentelemetry` cookbook), which the website
already stitches in.
**Reviewer:** observability-product-owner; dx-champion for the walkthrough quality.

Tasks:
1. Read benzene-dotnet's `docs/cookbooks/distributed-tracing-opentelemetry.md` to match its
   structure and OTel wiring idiom.
2. Write the .NET cookbook: `Benzene.OpenTelemetry` setup → OTLP exporter → the three supported
   Datadog paths (OTLP ingest in the Datadog Agent; OTel Collector + Datadog exporter; direct
   OTLP intake), with one worked path end-to-end and the other two summarized with links.
   Show which Benzene span attributes (topic, version, status) to facet on in Datadog, with a
   screenshot or described expected result.
3. Write the short language-neutral guide page here: the position (OTel-native, exporter-
   agnostic, no per-vendor exporters), linking to each port's cookbook and noting the same
   recipe covers New Relic / Honeycomb / Dynatrace / Grafana Cloud by swapping the endpoint.
4. Add the guide to `docs/guides/index.md` nav; run the website generator
   (`dotnet run --project website/generator -- --out website/dist`) — the broken-link
   self-check must pass.

Acceptance: website builds green with the new guide reachable from the guides nav; the .NET
cookbook has been verified against a real or clearly-marked-untested Datadog endpoint (if
network egress blocks live verification, say so in the doc's status line — house precedent:
the Tempo caveat in `.claude/PRODUCT_OWNERS.md`).
Do NOT: add any Datadog-specific exporter code; promise vendor features not reachable via OTLP.

---

## WP2 — Roslyn analyzers in the Benzene NuGet packages

**Goal:** compile-time spec-conformance diagnostics that travel with the package reference and
run in Visual Studio, Rider, and CI — the correct answer to "tools inside Visual Studio /
ReSharper".
**Home:** benzene-dotnet (new `Benzene.Analyzers` project, packed into the core package as an
`analyzers/dotnet/cs` asset — verify against the repo's packing setup).
**Reviewer:** validation-product-owner (owns developer tooling), core-product-owner (registry
semantics).

Tasks:
1. Verify assumptions in benzene-dotnet: how handlers are registered (attribute scanning vs
   explicit calls — both are legal idioms per core-concepts §9), how topic ids are expressed
   (string literals? constants?), and whether the marketing-page's "Roslyn source generator for
   handler discovery" exists yet or is aspirational. Adjust diagnostics below to what the API
   actually looks like.
2. Scaffold the analyzer project (netstandard2.0, `Microsoft.CodeAnalysis.CSharp` — pin to the
   lowest Roslyn version the supported VS/SDK matrix requires) + a test project using
   `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing`.
3. Implement diagnostics, one PR-sized slice each, in this order:
   - **BZ0001** (error): application handler registered on a reserved `benzene:`-prefixed
     topic id.
   - **BZ0002** (warning): routable topic served outside the handler registry — breaks Cloud
     Service Profile R2. Only fire where detection is sound; prefer silence to false positives.
   - **BZ0003** (info): registered request/response type whose derived schema degrades to `{}`
     per mesh.md §2.1 (recursion, custom serializer, dynamic) — message explains the mesh
     consequence, not just the rule.
   - **BZ0004** (warning + code-fix): duplicate topic id/version registration.
4. Wire packing so the analyzer ships inside the existing package(s) — no separate install
   step — and document each diagnostic id (docs page per id, linked from the diagnostic's
   `helpLinkUri`).
5. Verify in all three hosts: `dotnet build` (diagnostics appear in build output), Visual
   Studio, and Rider with Roslyn analyzers enabled.

Acceptance: analyzer tests green; a sample project violating each rule produces the diagnostic
in `dotnet build`; packing verified by inspecting the produced `.nupkg`.
Do NOT: build a ReSharper (JetBrains SDK) plugin or a VSIX — explicitly declined in the
research (classic ReSharper won't run these analyzers; VS and Rider do, and that's the
audience); fire BZ0002 heuristically where the registration idiom makes it unsound.

---

## WP3 — GitHub Action wrapping `benzene profile-check`

**Goal:** a marketplace-publishable Action that runs the existing live-probe conformance
checker against a deployed service URL — a post-deploy Cloud Service Profile gate in one YAML
step.
**Home:** new repo (`benzene-profile-check-action`); the probe CLI lives in benzene-dotnet
(`Benzene.CloudService.Probe`, `benzene profile-check --url <url>` — cloud-service-profile.md §5).
**Reviewer:** dx-champion; infrastructure-product-owner.

Tasks:
1. Verify in benzene-dotnet how the `benzene` CLI is distributed (dotnet tool on NuGet? which
   package id/version?) and what `profile-check`'s exit codes and output look like, including
   the tri-state (Satisfied / NotSatisfied / Inconclusive) rendering.
2. Build a composite Action: inputs `url` (required), `fail-on` (`not-satisfied` default —
   Inconclusive must NOT fail the gate, per the probe's design), optional `paths-prefix`;
   installs the .NET SDK + tool, runs the probe, writes a per-requirement (R1–R8) markdown
   table to `$GITHUB_STEP_SUMMARY`, sets outputs (`verdict`, per-requirement JSON).
3. README with a copy-paste post-deploy job example; note R6/R8's structurally-Inconclusive
   halves so users aren't surprised (research §2 / profile §5).
4. Version tag `v1`; marketplace listing after at least one real workflow has exercised it.

Acceptance: a workflow in the Action's own repo runs the Action against a known-conformant
public/demo service (or a service spun up in the job) and the summary table renders; a
deliberately non-conformant target fails the job.
Do NOT: reimplement any probe logic in the Action (it is packaging only); default `fail-on` to
include Inconclusive.

---

## WP4 — Grafana dashboard pack

**Goal:** published dashboards-as-JSON (grafana.com + repo) for Benzene's OTel metrics: topic
throughput, status breakdown, duration histogram, health rollup — making the WP0 convention
observable.
**Home:** benzene-dotnet or a small new `benzene-grafana` repo (decide at pickup; prefer the
new repo if dashboards are language-neutral, which post-WP0 they should be).
**Prerequisite:** WP0 merged.
**Reviewer:** observability-product-owner.

Tasks:
1. Build dashboards against a local stack (OTel collector → Prometheus → Grafana) running any
   Benzene example service; use only WP0-conventional metric names/attributes.
2. Export JSON with templated datasource + `service` variable; commit with a README showing the
   scrape/export wiring.
3. Publish to grafana.com dashboards; record ids in the README; link from the WP1 guide page.

Acceptance: importing the JSON into a clean Grafana against the README's stack shows live data
from an example service; no panel references a non-convention metric name.
Do NOT: encode .NET-only metric names; block on Datadog work (this is the Prometheus/Grafana
lane).

---

## WP5 — Datadog Agent integration (catalog listing)

**Goal:** a `benzene` Datadog Agent check scraping the spec-pinned surfaces of profiled
services — health service-checks from `/benzene/health`, topic inventory + contract-drift
signal (`descriptorHash` change) from the descriptor — submitted to Datadog's community
`integrations-extra` catalog.
**Home:** new repo for development; upstream PR to `DataDog/integrations-extra`.
**Reviewer:** observability-product-owner; mesh-product-owner (descriptor semantics).

Tasks:
1. Read Datadog's agent-integration developer docs (`ddev` tooling, check structure, metadata
   files, community submission requirements) — current process, not from memory.
2. Implement the check (Python): config = list of base URLs (+ per-instance path overrides
   mirroring the probe's stance that non-default paths degrade guarantees); emit
   `benzene.can_connect` and per-health-check service checks, gauge/count metrics tagged
   `benzene_service`, `benzene_topic`, `runtime`, `cloud`; detect descriptor-hash change
   between runs and emit an event (drift signal). All HTTP calls per-instance-isolated: one
   unreachable service must not fail the others.
3. Conformance-fixture-driven tests: reuse shapes from `docs/specification/conformance/`
   (envelope, descriptor cases) as HTTP-mocked fixtures so the check is tested against the
   neutral truth, not one implementation.
4. Metadata for the catalog (manifest, dashboards optional at v1), then the upstream
   submission; track review turnaround as its own follow-up.

Acceptance: check passes its tests against fixture-derived mocks; a live run against a demo
profiled service shows the service checks/metrics in a Datadog sandbox org (or is explicitly
marked unverified if egress-blocked); submission PR opened upstream.
Do NOT: call `/benzene/invoke` (read-only integration — no dispatch surface); scrape
non-profiled services and report them as broken (absent surfaces = reduced picture, mesh.md §6
posture); hold v1 for the Marketplace (paid) track — community catalog first.

---

## WP6 — Datadog as a mesh data source

**Goal:** `IMeshTraceSource` (and, if the APIs support it cleanly, `IMeshUsageSource`)
adapters reading Datadog APM/metrics APIs, so Datadog-standardized estates can power the mesh
fleet view — the shape of enterprise-readiness roadmap slice 4.
**Home:** benzene-dotnet (`Benzene.Mesh.Tracing.Datadog`).
**Prerequisites:** WP0 (usage metric name); strongly prefer landing after benzene-dotnet's
enterprise slice-0/1 pre-work (composite read model taking `IEnumerable<IMeshTraceSource>`,
`TryAddSingleton` client registration, config-bindable options — see
[mesh-enterprise-readiness.md](mesh-enterprise-readiness.md) §6 engineering pre-work), so the
new source is born config-selectable rather than adding a sixth welded adapter.
**Reviewer:** mesh-product-owner; performance-champion (fetch isolation as sources multiply).

Tasks:
1. Study the existing Tempo and X-Ray source implementations for the options/adapter idiom;
   copy the idiom exactly.
2. Implement the trace source against Datadog's trace/span query API (verify current API +
   auth model from Datadog docs at pickup time); map spans back to TraceEvent-shaped fleet
   data the read model expects.
3. Options POCO must be config-bindable (no ctor-required args — the documented enterprise
   pre-work trap); register clients with `TryAddSingleton`.
4. If the enterprise config catalog (slice 1) has landed: register the source name
   (`"fleet": { "source": "datadog" }`) with fail-fast unknown-name behavior.

Acceptance: unit tests with recorded/mocked API responses; the AwsMesh-style example wired
with the Datadog source renders fleet data; a wrong API key degrades that source only
(fetch isolation), never the catalog.
Do NOT: hard-code metric names that WP0 hasn't settled; couple to `Benzene.Mesh.Collector`
internals — this is a read-path adapter behind an existing port.

---

## WP7 — VS Code extension

**Goal:** a spec-aware `benzene-vscode` extension serving the TypeScript/Python-port audience:
conformance-fixture JSON validation, topic-id completion from a running service's derived spec
or a collector, "invoke topic" against `/benzene/invoke`, descriptor rendering.
**Home:** new repo `benzene-vscode`.
**Reviewer:** dx-champion; validation-product-owner.

Tasks (v1 scope deliberately small):
1. Feature 1 — fixture tooling: JSON schema + diagnostics for the
   `docs/specification/conformance/*.json` fixture formats (read `conformance/README.md` for
   the formats), activated on those filenames.
2. Feature 2 — service explorer: configure base URLs; tree view of topics/versions from
   `/benzene/spec`; click-to-invoke via `/benzene/invoke` with a JSON body editor and the
   envelope pre-filled; render health from `/benzene/health`.
3. Completion from a live spec (topic ids inside string literals) is v2 — note it, don't build
   it.
4. Publish to the VS Code marketplace under the project's publisher; README screenshots.

Acceptance: extension runs against any profiled demo service (language-agnostic — test against
a non-.NET port's service to prove the point); fixture diagnostics fire on a deliberately
malformed fixture copy.
Do NOT: bind to any single port's project layout; embed the mesh UI in v1 (research §4.3 —
falls out later for free).

---

## WP8 — Backstage entity provider *(parked — do not start)*

**Trigger to unpark:** benzene-dotnet enterprise slices 1–2 shipped (config-driven mesh host +
auth), i.e. a deployable collector story exists for the provider to read from.
**Shape when unparked:** a Backstage backend module (opt-in entity provider, matching
Backstage's "discovery proposes, config disposes" posture already endorsed in
[mesh-enterprise-readiness.md](mesh-enterprise-readiness.md) §3) that maps mesh catalog data to
Backstage Components/APIs, attaching generated OpenAPI/AsyncAPI as API entities. Note from
research §6: this may constitute the "second consumer" of the `benzene:mesh:query:*` read
models that mesh.md §4 names as the trigger for pinning them in the spec — raise with
mesh-product-owner before building against them.

---

## Cross-cutting rules for every WP

- **Verify before building on an assumption** about benzene-dotnet or a vendor API — the
  research was written from this repo plus vendor docs, not from a port checkout.
- **Fixtures are the neutral truth** (AGENTS.md): integration tests mock services using
  `docs/specification/conformance/` shapes, never one implementation's quirks.
- **Read-only by default**: monitoring integrations (WP3, WP5) never touch `/benzene/invoke`;
  only WP7's explicit user-driven invoke feature does.
- **Reduced, not broken**: absent surfaces on a service render as a reduced picture (mesh.md
  §6 posture), never as integration failure.
- **No new spec obligations**: nothing here adds wire contracts. The two spec-adjacent
  touchpoints are WP0's conventions page (informative) and WP8's query-topic trigger.
- Keep commits scoped to one logical change; plan-first applies to WP2, WP5, WP6.
