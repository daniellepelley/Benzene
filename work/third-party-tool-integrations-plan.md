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
WP0 (metric-name convention) ✅ done here │ follow-through in benzene-go / -dotnet / -ts / -python
WP1 (Datadog cookbook)       ◐ guide done here │ .NET cookbook outstanding in benzene-dotnet
WP2 (Roslyn analyzers)        — no dependencies; benzene-dotnet
WP3 (profile-check Action)    — no dependencies; new repo
WP4 (Grafana pack)            — WP0 ratified first
WP5 (Datadog Agent check)     — no dependencies; new repo
WP6 (Datadog mesh source)     — after WP0 + benzene-dotnet enterprise slice-0/1 pre-work
WP7 (VS Code extension)       — no dependencies; after WP2/WP3 on value ranking
WP8 (Backstage provider)      — BLOCKED: wait for the enterprise-readiness collector/host slices
```

Remaining pickup order: **WP2 → WP3 → WP0-follow-through (benzene-go especially — it carries
three live defects) → WP1's .NET cookbook → WP4 → WP5**, then WP6/WP7 as appetite allows. WP8
stays parked until its trigger fires.

**Getting the other repos.** All four port repos clone cleanly from this environment, so the
"verify the assumption first" task in each WP is doable rather than aspirational:
`git clone --depth 1 https://github.com/daniellepelley/benzene-dotnet.git` (likewise
`-go`, `-typescript`, `-python`). The website generator also needs one:
`--dotnet-docs <benzene-dotnet>/docs` is **required**, not optional.

---

## WP0 — Settle the cross-port usage-metric convention ✅ DONE (2026-08-13, this repo's half)

**Delivered:** [`docs/guides/observability-conventions.md`](../docs/guides/observability-conventions.md),
linked from the guides nav. Status on the page is PROPOSED v0.1 pending
observability-product-owner ratification.

**What the inventory found** (verified against all four ports' `main`, 2026-08-13 — the audit is
§5 of the page): the fracture this WP was created to prevent has **already happened**, and is not
cosmetic.

- .NET and TypeScript agree exactly: `benzene.messages.processed` / `benzene.message.duration`,
  attributed `topic`/`transport`/`result`, spans stamped `benzene.topic`/`benzene.version`/
  `benzene.status`/`benzene.transport`.
- Go diverges on every axis: `benzene.invocations` / `benzene.invocation.duration`, attributed
  `benzene.topic`/`benzene.topic.version`/`benzene.status`, no transport attribute anywhere.
- Python emits nothing (no diagnostics module).
- .NET already had a written standard for this in its own `docs/mesh-usage-feed.md` §1 — so the
  convention page is a **promotion of an existing .NET-local standard to a cross-port home**,
  not an invention. That framing is what makes the .NET/TS vocabulary the natural winner.

**Three live defects surfaced** (all Go-side, all silent):
1. A Go service's topic version never reaches a .NET-hosted mesh — the Tempo/Jaeger/X-Ray trace
   sources read `benzene.version`; Go stamps `benzene.topic.version`. Cross-language fleets are a
   shipped scenario, so this is reachable today and shows as a blank column, not an error.
2. Go services cannot answer "over which transports" — no transport attribute on metrics or
   spans — making Mesh UI requirement 3 structurally unanswerable for them.
3. Go's counter is attributed by raw status rather than the collapsed `result` vocabulary, so Go
   and .NET services can't be summed into one usage series even after instrument names align.

**benzene-go: DONE (2026-08-13).** Branch `claude/observability-conventions-alignment` pushed to
`daniellepelley/benzene-go` (not merged — no PR opened per this session's no-unrequested-PR
policy; open one when ready). `diagnostics/diagnostics.go` now:
- renames the span attribute `benzene.topic.version` → `benzene.version` (the silent-data-loss
  fix — this is the key the shipped Tempo/Jaeger/X-Ray trace sources actually read);
- renames the metric instruments to `benzene.messages.processed`/`benzene.message.duration` and
  their attributes to `topic`/`transport`/`result`, implementing the `result` collapse rule
  (`success`/raw-status/`exception`/`<missing>`) via the same `resultSuccessful`-optional-
  interface idiom used elsewhere in this Go module;
- sets the span's `benzene.status` to `exception` when a pipeline error escapes past the
  middleware's position (previously left at whatever `ic.Result` happened to hold, or empty) —
  matching .NET's `ActivityMiddlewareDecorator` and closing a fourth divergence found while
  implementing, not in the original audit;
- done as a **clean rename, not a dual-emission migration** — pre-1.0, and the project's stated
  position is to avoid compat shims when the code can just change;
- `transport` is honestly emitted as `<missing>` rather than invented: **investigated and found
  that benzene-go has no `ICurrentTransport`-equivalent anywhere** — no transport binding
  records its identity anywhere `InvocationContext`-reachable code can read it back, not even on
  the mesh's own `TraceEvent`. Adding a real value needs new cross-cutting plumbing (every
  binding's `Use<Transport>` constructor would need to stamp identity somewhere resolvable),
  which is a separate, larger piece of work — flagged in the package doc comment rather than
  attempted here or faked with a placeholder that looks like real data.
- 100% test coverage maintained (new tests for the exception path, the `<missing>`-result path,
  and the narrow `SetResult(status:"", successful:false)` → `"failure"` edge case); `gofmt`/`go
  vet`/`go build`/`go test -race -cover` all clean on the `diagnostics` module and the root
  module.
- Four docs updated to match (`README.md`, `docs/middleware.md`, `docs/getting-started-aws.md`,
  `examples/opentelemetry-helloworld/README.md`).

**New follow-up surfaced, not in original scope**: a real `benzene.transport`/metric-`transport`
value needs a transport-identity concept added to benzene-go's core (`InvocationContext` or a
context-carried accessor, set by each `Use<Transport>` binding constructor) before any port can
close that gap. Worth its own WP if the mesh usage-by-transport view over Go services matters
before a wider core change is otherwise planned.

**Remaining WP0 follow-through (different repos):**
- **benzene-dotnet / benzene-typescript**: no behaviour change; point `Benzene.Diagnostics` at
  the shared page and retire the .NET-local copy of the standard in `docs/mesh-usage-feed.md` §1
  so the two cannot drift.
- **benzene-python**: implement to the page when a diagnostics module is added.
- Ratify the page (observability-product-owner) and drop its PROPOSED status.

Do NOT: put this in `mesh.md` as normative text (it isn't a wire contract and no fixture can
pin it); rename the .NET/TS instruments (they hold the install base — that is the whole
rationale for the direction of alignment).

---

## WP1 — Datadog (and friends) OTLP cookbook ◐ PARTLY DONE (2026-08-13, this repo's half)

**Delivered:** [`docs/guides/exporting-telemetry.md`](../docs/guides/exporting-telemetry.md),
linked from the guides nav. States the position ("OTLP is the integration; Benzene ships no
vendor exporters and intends to ship none"), tables the four supported Datadog routes with
links, says what to facet on once data lands (`benzene.topic`, `benzene.status`,
`benzene.transport`, `result`), and generalizes to every other backend. Website build green
(119 pages, broken-link self-check passed) — build it with
`dotnet run --project website/generator -- --out website/dist --dotnet-docs <benzene-dotnet>/docs`;
the generator **requires** a .NET docs path and errors without one.

**Remaining (benzene-dotnet, not done here):** the runnable .NET cookbook —
`docs/cookbooks/datadog-otlp.md` beside the existing `distributed-tracing-opentelemetry.md` and
`custom-metrics-opentelemetry.md` (both already present, and the structural model to copy).
One Datadog route worked end-to-end with real `Benzene.OpenTelemetry` wiring, the other three
summarized. If egress blocks live verification against a Datadog endpoint, mark it untested in
the doc's status line — house precedent is the standing Tempo caveat in
`.claude/PRODUCT_OWNERS.md`. Then link it from the guide page's per-port list.

Do NOT: add any Datadog-specific exporter code; promise vendor features not reachable via OTLP.

---

## WP2 — Roslyn analyzers in the Benzene NuGet packages ✅ DONE (2026-08-13, partially)

**Delivered:** pushed directly to `main` on `daniellepelley/benzene-dotnet`
(`dc458ec..d9565c9`, branch `claude/reserved-topic-and-schema-analyzers` — no PR, per this
session's direct-push instruction).

**What was verified first (task 1), which reshaped the plan:**
- The analyzer infrastructure **already existed**: `Benzene.CodeGen.SourceGenerators`
  (`src/Benzene.CodeGen.SourceGenerators/MessageHandlerSourceGenerator.cs`) is a working
  `IIncrementalGenerator`, already packed as `analyzers/dotnet/cs` (`IsRoslynComponent=true`),
  already referenced by `Benzene.Core.MessageHandlers` — so it already reaches every
  handler-carrying project with no separate install. The plan's task 2 ("scaffold the analyzer
  project") and most of task 4 ("wire packing") were **already done** before this session
  started.
- It already shipped two diagnostics with the ids **`BENZ0xx`**, not `BZ0xxx` as the plan
  guessed: `BENZ001` (duplicate topic — this **is** the plan's BZ0004, already implemented) and
  `BENZ002` (`[HttpEndpoint]` handler with no `[Message]` topic — not one of the plan's four,
  a real gap the plan didn't anticipate).
- Handler registration idiom: `[Message("topic", "version")]` attribute + `IMessageHandler<TReq,
  TRes>`/`IMessageHandler<TReq>` interface, discovered by the generator via Roslyn symbols
  (fully-qualified type names hardcoded as strings — an analyzer project deliberately doesn't
  take a runtime dependency on the library it inspects, confirmed as the established pattern
  throughout the file).

**Added `BENZ003` and `BENZ004`** (the plan's BZ0001 and BZ0003 — renumbered/reworded to fit
the existing scheme):
- **BENZ003** (error): a `[Message]` handler on one of Benzene's own reserved topic ids.
  **Deliberately narrower than "any `benzene:`-prefixed topic"**, unlike the plan's original
  BZ0001 spec: verification found a real, shipped exception —
  `examples/AwsMesh/Mesh/MeshAggregateHandler.cs` legitimately registers
  `[Message("benzene:mesh:aggregate")]`, because mesh.md §4 makes a collector an *ordinary*
  Benzene service serving the `benzene:mesh:*` ingest topics as handlers. A blanket prefix ban
  would have been a false positive on real code. BENZ003 fires only on the seven specific ids
  hand-copied from `Benzene.Abstractions.BenzeneTopic.All` (`benzene:spec`,
  `benzene:test-payloads`, `benzene:healthcheck`, `benzene:liveness`, `benzene:readiness`,
  `benzene:mesh`, `benzene:ping`) — confirmed none of those seven are ever legitimately declared
  via `[Message]` anywhere in the codebase, and confirmed `examples/AwsMesh` still builds clean
  with the new rule active.
- **BENZ004** (info): a handler's request/response type is one that
  `Benzene.Mesh.Wire.MeshSchemaGenerator` derives an unconstrained `{}` schema for — mirrored
  its exact top-level special-cases (`object`, `dynamic`, enum, `JsonElement`/`JsonDocument`/
  `JsonNode`) via Roslyn symbols instead of the runtime deriver's reflection. Deliberately
  shallow (top-level type only, no property-walking, no cycle detection) — the sound subset the
  plan itself called for ("fire only where detection is sound").
- **Plan's BZ0002 (registry-bypass) was not attempted** — investigated and confirmed it's
  inherently unsound for a source generator: it would need to detect handlers *not* using the
  `[Message]`/interface idiom at all, which by definition isn't visible to a generator built
  around that idiom. No safe detection strategy found; left undone rather than shipping
  something heuristic and noisy.
- **Plan's BZ0004 (duplicate topic) was already `BENZ001`** — nothing to add.

**Testing**: the plan's own testing framework choice (`Microsoft.CodeAnalysis.CSharp.Analyzer.
Testing`) turned out to already be in use for BENZ001/BENZ002 and confirmed broken in this
environment (`MessageHandlerSourceGeneratorTest.cs`'s two golden-file tests are `[Fact(Skip=
...)]`). A working alternative already existed too:
`test/Benzene.Core.Test/Autogen/CodeGen/SourceGenerator/MessageHandlerDiagnosticsTest.cs` drives
`CSharpGeneratorDriver` directly and asserts on `Diagnostic[]` — no golden-file comparison. Added
7 new tests to that file in the same idiom, including the negative case that pins the
mesh-collector exception (`Benz003_IsSilentOnAMeshCollectorExtendingTheReservedNamespace`). All
14 tests in the file pass; `Benzene.Core.MessageHandlers` and the real `examples/AwsMesh` both
build clean with zero false positives from the new rules.

CHANGELOG.md updated under `[Unreleased] → Added`.

**Remaining for a future pickup:**
- **BZ0002/registry-bypass**: no sound design found; revisit only if a concrete detectable
  pattern emerges (e.g. a specific anti-pattern worth naming rather than "anything not using
  `[Message]`").
- **IDE verification**: confirmed via `dotnet build` + the direct-driver tests only. Visual
  Studio and Rider verification (the plan's task 5) not done in this session (no GUI IDE
  available) — should still work automatically (`IsRoslynComponent=true` + `analyzers/dotnet/cs`
  packing is exactly what makes both pick it up), but worth a manual confirmation pass.
- **Per-diagnostic docs page + `helpLinkUri`**: not added. BENZ001/BENZ002 don't have one either
  (no existing convention to extend) — worth doing for all four together if/when this becomes a
  priority, rather than starting a new pattern for just the two new ids.
Do NOT: build a ReSharper (JetBrains SDK) plugin or a VSIX — confirmed still correct; classic
ReSharper doesn't execute Roslyn analyzers, VS and Rider both do.

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
