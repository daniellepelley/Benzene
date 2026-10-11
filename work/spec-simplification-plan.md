# Spec simplification — implementation plan

**Status:** PLAN — revised 2026-10-11 after review by the architecture reviewer and the core product
owner; ready to execute from Phase 0.
**Written:** 2026-10-10. **Source:** [`spec-review-2026-10.md`](spec-review-2026-10.md) — the review
this plan actions. The reasoning lives there (its §8 records what the second round changed) and is
not repeated here.
**Goal:** a specification as simple as it can be while still offering the features and the
consistency, settled before a 1.0 tag.
**Resolves on the way:** `remaining-issues-plan.md` P1.1–P1.3 and `benzene-headers-plan.md` Phase A.

---

## 0. Rules every phase runs under

1. **Spec first, ports second, snapshot third** — and the snapshot is pinned. A contract change
   lands in `docs/specification/**` here and is released as a spec tag (`spec-0.2.0`, …); each port
   catches up and re-vendors `conformance/*.json` at that tag. From Phase 0, a port's drift-check
   compares its snapshot with the ref its `SPEC_VERSION` names, not with `main`, and a separate
   non-blocking job reports "behind latest". "Expected red" no longer exists: a port is green on the
   tag it claims, and visibly behind until it moves.
2. **Pre-1.0 breaks are clean, but not invisible.** No dual-accept or compatibility shims. The
   installed base is one .NET alpha (`0.0.3-alpha.4`) and one application, **The Count**, which pins
   eleven Benzene packages and runs in production. The Count is the canary: each .NET release that
   implements a phase is taken up by The Count, with its tests run, before the release is announced.
   Every breaking change carries a migration note in the port's changelog.
3. **This plan runs alongside the releases, not ahead of them.** The P0 publishes in
   `remaining-issues-plan.md` proceed in parallel — installable betas are what will settle doubts
   about the spec. One condition: TypeScript `0.1.0-beta.2` either ships after its Phase 1 settlement
   work or names its unsafe settlement defaults in the release notes.
4. **Downstream consumers move with the ports.** benzene-ui, the canonical mesh UI (`mesh-ui/`), the
   website demos and blog, benzene-patterns and The Count all read the contract (review §4.13). A
   change the mesh UI must follow lands in **benzene-ui first**, because .NET's embedded copy is
   drift-checked against the benzene-ui build.
5. **File paths stay stable** unless a phase says otherwise; tiering is carried by `index.md`'s
   grouping and each document's status line, not by sub-folders. A move fixes every inbound
   reference in the same commit — `docs/`, `work/`, `.claude/agents/`, the website generator's hard
   links (`MarketingPages.cs`), and the ports' docs when they catch up. The website's broken-link
   self-check (run in CI against all four ports' `main`) gates every phase.
6. **One window for the wire.** Every change ports must implement before 1.0 lands in **Phase 1**,
   as back-to-back spec PRs released under one tag; each port does one catch-up and one re-vendor.
   Phase 2 changes no contract and runs while the ports catch up. Phase 3, the only other
   cross-port window, opens only when every port and benzene-ui are green on Phase 1's tag.
7. **Every phase ends with the lifecycle** (`docs/documentation-lifecycle.md`): each changed port's
   `docs/capability-matrix.md` updated (`capability-scribe`), `docs/capabilities.md` refreshed here
   (`port-aligner`), and the checklist (§5) ticked.

## 1. Decisions

Each row is the default this plan executes. **To change the plan, change the row** — the phases cite
the decisions they depend on. *Status* records what the second-round review did to the row (the
review's §8 has the reasons).

| # | Decision | Status | Phase |
|---|---|---|---|
| D1 | **Version selection:** an exact match; else a handler the request can be cast to (a registered cast from the requested version to that handler's version); else `not-found`, naming the requested version. A message with **no** version goes to the topic's default handler — the unversioned one, or one explicitly declared default; if there is none, `not-found`, and a start-up warning names any topic that has versioned handlers and no default. "Highest available" leaves the spec (a port MAY offer it as an opt-in selector). The spec states that one header drives both dispatch and casting. | amended | 1 |
| D2 | **`isSuccessful`** MUST match the status table, with no exceptions; the receiver trusts it; stated once, in wire-contracts §1.2. The health topics return `ok` whenever the checks ran, with `isHealthy` in the payload; the HTTP binding's default mapping for the health topics returns **503** when `isHealthy` is false. The §1.3 escape hatch goes. `grpc.health.v1` is unaffected. | amended | 1 |
| D3 | **Trace context:** Core — *if* propagated, verbatim W3C. The mesh claim (R8, D15) — MUST join inbound and forward outbound. core-concepts §10's "MUST behave identically" and mesh §3's SHOULD go. | amended | 1 (text), 4 (R8 moves) |
| D4 | **Well-known HTTP defaults** carry the `/benzene/` prefix: envelope `/benzene/invoke`, `/benzene/spec-ui` (and its default spec URL `/benzene/spec?type=benzene`), `/benzene/mesh-ui`, MCP `/benzene/mcp`. design-principles §5.3 is deleted. | amended (list corrected) | 1 |
| D5 | **`benzene:mesh:query:*`:** the three read models the collector fixture asserts through (`fleet`, `service`, `topic`) are labelled Provisional module content; full pinning follows 1.0. | amended | 2 |
| D6 | **MCP headers** `benzene-mcp-tool`, `benzene-mcp-session`. MCP gets its own document, framed as a binding, labelled Provisional (.NET). | kept | 1 (headers), 2 (doc) |
| D7 | **Problem-type registry:** the bytes stay (`type` = `https://benzene.app/problems/` + `benzeneStatus`); §3.1's table and titles shrink to that one sentence; the `code`/`type`/`classification` paragraph goes. | amended — no wire change | 2 |
| D8 | **`errors[].field`** is a JSON Pointer (RFC 6901) addressing the handler's request type in its JSON form, using the serializer's naming policy (rename attributes included). A rule that cannot be resolved to a pointer omits `field`. The gRPC binding converts to a dotted `FieldViolation.field`. After a payload cast the pointer addresses the handler's version of the type. | amended | 1 |
| D9 | **Header classification** Core / Module *name* / Binding *name* replaces tiers A–D. | kept | 2 |
| D10 | **One version header**, `benzene-version`, configurable like every reserved name; the `version`/`x-version` fallbacks go. The HTTP `/v{n}` route segment is sugar that writes the version; the parameter name is the port's business. | moved to Phase 1, with D1 | 1 |
| D11 | **JSON reads:** writers MUST write camelCase; readers MUST accept it; case-insensitive read is MAY. | kept | 2 |
| D12 | ~~Rename envelope `statusCode` → `status`, TraceEvent `topicVersion` → `version`.~~ | **dropped** | — |
| D13 | **Reserved topics:** one section in wire-contracts lists the **interoperable** reserved topics with request and response shapes: `benzene:healthcheck`, `benzene:liveness`, `benzene:readiness` (health response; the last two optional), `benzene:spec` (optional `{type, format}` request; the contract document by default), `benzene:ping` (optional reachability probe), and the mesh topics by reference (`benzene:mesh`, until Phase 3; the collector topics; `benzene:mesh:dispatch`, Provisional). The `benzene:` prefix is reserved to the framework; a port MAY serve other topics under it; a tool MUST NOT depend on an unlisted one. Interception matches the topic id alone, ignoring version. Default HTTP paths are the HTTP binding's default `httpMappings` for these topics, not a column of the table. **No rename** of `benzene:healthcheck`. | amended | 1 |
| D14 | `_benzeneHeaders` → **`benzene-headers`**, EventBridge only. The packed-headers mode for every transport (`benzene-headers-design.md` §2.2) is rejected. | kept | 1 |
| D15 | **Profile:** one profile, `cloud-service`, of R1–R5 and R7 — pull-able by any tool, no collector needed. R6 (feeds) and R8 (propagation) become the mesh module's own conformance claim. The `profile` field moves to the self-description served at `benzene:spec`. | amended | 4 |
| D16 | **Settlement** (transport-bindings §1, obligation 8): a one-way binding MUST NOT acknowledge a message whose invocation ended with `isSuccessful: false`, no result, or an uncaught error; success results, `ignored` included, are acknowledged — so handlers on one-way bindings must be idempotent. An unmatched topic is a `not-found` failure; a binding SHOULD offer to treat unmatched topics as `ignored` (shared-bus subscribers). A middleware that ends the pipeline on a one-way binding MUST set a result. A binding MUST offer an at-most-once override; a stream binding whose retention blocks the partition MAY default to skip-and-continue and MUST document it. A binding that checkpoints a batch or position MUST define its unit of settlement. Dead-lettering is the transport's redrive configuration, said explicitly. | amended | 1 |
| D17 | **Envelope over HTTP:** every envelope the endpoint processed returns HTTP **200**, whatever its status; non-2xx only for carrier failures (malformed envelope, authentication on the carrier). The inner problem document omits `status`. | amended (was: status line carries the status) | 1 |
| D18 | **One self-description, narrowed:** the contract document gains the identity fields and becomes the `benzene:mesh:register` body; `benzene:mesh` and `descriptorHash` go. Outbound splits into `events[]` (published) and `sends[]` (dependencies, with response schemas). One algorithm (JCS + SHA-256), two projections: `contractHash` (requests + events + components) and a registration hash (adds `sends`). A normative component-naming rule. `benzene:spec` keeps its format selector; the contract document is the only conformance-tested output. Details settled by the Phase 0 spike; the JSON Schema 2020-12 dialect is a separate decision after 1.0. *Spike done:* `work/self-description-design.md` — hashes over a dereferenced projection, so component names leave the hash (Q1); providers are events **and** sends; ten rulings plus one new (Q1–Q11, its §10.5), reviewed by all four port champions. | amended (was: full merge); **ruled 2026-10-11** — every §10.5 recommendation accepted | 0 (spike), 3 |
| D19 | **Proposals and Provisional modules:** version-aware routing (versioning §5.4, §5.7), §6 and §7 → `work/side-by-side-service-versions.md`. A module or binding with one implementation is labelled **Provisional** with its implementing ports — MCP, claim-check, service versions — until a second port implements it. | amended | 2 |
| D20 | **Spec version and stability:** a `docs/specification/VERSION` file and `spec-x.y.z` tags (Phase 0, needed by rule 1). Each document carries a stability label: Core and the profile frozen under semver from 1.0; each module Stable or Provisional; guidance unversioned. | amended | 0 (tags), 2 (labels) |
| D21 | **Envelope endpoint security:** the endpoint runs the same pipeline, and so the same authentication middleware, as the service's other transports, and MUST support restricting which topics are reachable through it. | new | 1 |
| D22 | **Tier content:** versioning's wire part (the header, absence, dispatch — D1, D10) is **Core**; `versioning.md` keeps its path as that Core document. Core names one category once: *optional capabilities, exactly this shape if implemented* (health, trace context, versioning). | new | 2 |
| D23 | **Normative principles stay normative:** design-principles §3 (a capability that depends on a steer degrades gracefully and states its degradation) and §4.1 (a shorthand is composed from the public explicit form; inference is verified before the first message) move into core-concepts, not into guidance. | new | 2 |
| D24 | **core-concepts §7/§8** keep: one definition mounts several transports, an unhonourable mount is a no-op; registration completes and is validated before the first invocation; a scope per invocation; replaceable defaults; handlers and middleware constructed with their declared dependencies per scope; **a pipeline can be invoked in process with no binding**. The phases and container operations go; transport-bindings' references to `configure` become "mount". | new | 2 |
| D25 | **Client error fidelity** (`remaining-issues-plan.md` P1.1/P1.2), stated in core-concepts §Outbound: a client that receives a problem document (a failure envelope, or `application/problem+json`) MUST rebuild `errors` from it with `field` and `code` intact; attaching the received document verbatim is MAY. Additive in every port. | new | 2 (spec); port work may follow 1.0 |

| D26 | **Fleet rows under service versions** — *open, for ruling*. `mesh-service-version-cases.json` expects the fleet query to return **one row per `(service, serviceVersion)`**; .NET's collector deliberately returns **one row per service**, headlined by the latest version, with every version kept underneath — and mesh.md §4 says the per-version breakdown "belongs to the service view". **Ruled 2026-10-11:** one row per service in the fleet; the per-version breakdown is asserted on the **service** query, which lists the service's live versions (the fixture is amended in Phase 1, and .NET's service view gains the list). Go's collector has no `serviceVersion` handling at all, and TypeScript runs neither service-version fixture; the fixture stays conditional. | ruled | 1 |

**Not at all:** D12; renaming `benzene:healthcheck`; dropping the problem-type bytes; a second profile
name.

**Deferred, unchanged:** dropping `updated`/`deleted` from the vocabulary; gRPC `conflict` →
`Aborted`; a CloudEvents binding (`cloudevents-design.md`); `CancellationToken` on handlers.

**After 1.0, additive:** the binding table replacing transport-bindings §2's prose catalogue (its
MUSTs are lifted out in Phase 1); full pinning of the query read models (D5); the JSON Schema
2020-12 dialect behind a discriminator (D18); D25's port work if not done; promoting a Provisional
module when a second port implements it.

## 2. Phases

### Phase 0 — Prerequisites *(no spec text changes)*

| # | Work | Where | Output |
|---|---|---|---|
| 0.1 | **Settlement audit.** `capability-scribe` states, per binding, the default on a failure result, a missing result and an unmatched topic, and the override knob — in Go and Python (both `unknown` today; Python has no settlement section) and confirming TypeScript's. | benzene-go, benzene-python, benzene-typescript | each port's matrix; sizes Phase 1's port work |
| 0.2 | **Self-description spike.** Rule on: `events[]`/`sends[]` (names and shapes); the two hash projections and which feeds each; the component-naming and hoisting rule (with cross-port examples); the `{type, format}` request; and, per candidate field (`openapi`, `info`, `reserved`, `example`), keep or drop with its consumers named (benzene-ui reads `requests[].reserved`; the Spec UI shows `example`). Plan with each port's DX champion (`port-quality-standards.md` §1). | here, `work/self-description-design.md` | a ruled design; Phase 2's Outbound section uses its vocabulary. *Done, pending ruling:* the design (6ae9da20) and its §10 port-champion review; every port's 3a cost is now **M** (none L); the rulings are §10.5 |
| 0.3 | **Spec tags and pinned drift-checks.** Tag `spec-0.1.0` at current `main`; add `docs/specification/VERSION`. Each port's `conformance-drift-check` compares against its `SPEC_VERSION` ref, plus a non-blocking "behind latest" job. The same for benzene-ui's `contracts/`. | here, four ports, benzene-ui | rule 1 in force. *Done:* `spec-0.1.0` cut by the new `release-spec` workflow (tags are cut from `VERSION`, never by hand); all five checks green on the pin; benzene-ui pinned to the commit its four fixtures match, behind by design until Phase 1 |
| 0.4 | **Claim the service-version fixture.** Run `mesh-service-version-cases.json` in `Benzene.Conformance.Test`. *Outcome:* .NET keys the catalog by version, but running the fixture failed 3 of 4 cases — one a real bug (re-registering one version erased an edge a sibling still declared; fixed, benzene-dotnet `174bde89`), two a read-model disagreement (fleet rows per name vs per version) that needs a ruling, D26. Not claimed yet. | benzene-dotnet | P1.3 narrowed to D26 |
| 0.5 | **Canary and release notes.** Add The Count to the .NET release checklist (rule 2); the TypeScript beta.2 condition (rule 3). | benzene-dotnet, the-count, benzene-typescript | — |

### Phase 1 — The wire window *(L; one tag, `spec-0.2.0`)*

Decisions: D1, D2, D3 (text), D4, D6 (headers), D8, D10, D13, D14, D16, D17, D21. Three spec PRs,
landed back to back, then tagged:

**1a — Behaviour.**

| File | Edit |
|---|---|
| `core-concepts.md` §2 | D1 in full; D10's "one header drives both". |
| `core-concepts.md` §5, §10 | `isSuccessful` cites wire §1.2 (D2); trace context per D3; delete the mesh-fingerprint aside in §5. |
| `wire-contracts.md` §1.2, §1.3 | D2 stated once; delete the escape-hatch carve-out and the historical "sender that predates this field" clause. |
| `wire-contracts.md` §2 | `benzene-version`: one name (D10); `traceparent` row per D3. |
| `wire-contracts.md` §5 | Health topics return `ok` with `isHealthy` (D2). |
| `versioning.md` §2–§3 | Delete the fallback list and "exact match, else highest available"; cite core-concepts §2. |
| `mesh.md` §3 | Outbound propagation cites the profile's R8 rather than restating a level. |
| `transport-bindings.md` §1 | Obligation 8, settlement (D16). Envelope over HTTP (D17) and envelope security (D21). The HTTP binding's default `httpMappings` for reserved topics, including the health 503 rule (D2, D13). |
| `transport-bindings.md` §2 | Lift the MUSTs out of the informative catalogue: EventBridge and DynamoDB topic sources cite `topicSources`; MCP's three MUSTs move to a holding note for Phase 2's `mcp.md`. |
| `design-principles.md` §5.2–§5.3 | D4's defaults; delete §5.3. |

**1b — Names.** `wire-contracts.md` gains the reserved-topic section (D13), defining `benzene:spec`
once — "the contract document (contract-document.md), or the format its request names" — so Phase 3
changes the document, never the definition. `_benzeneHeaders` → `benzene-headers` in wire §2 and
transport-bindings (D14), with the sentence `benzene-headers-plan.md` prescribes. MCP header rows
(D6). `conformance/README.md` loses its `_benzeneHeaders` paragraph.

**1c — Error field.** `wire-contracts.md` §1.3 `errors[].field` per D8; §4.2 states the gRPC
conversion.

**Fixtures (`spec-0.2.0`):**

| File | Change |
|---|---|
| `envelope-cases.json` | A new canonical handler `conformance:versioned`, registered at `v1` (declared default) and `v2`, each returning `{ "version": "<v>" }`. Cases: `benzene-version: v2` → v2; no version → v1 (the default); `v9` → `not-found`; `benzene-version: v2` to `conformance:greet` (unversioned only) → `not-found`. |
| `transport-metadata-cases.json` | `defaultMetadataKeys.headers` = `benzene-headers`. A case: a bare `version` attribute resolves no version and stays a header (D10). A `settlementDefaults` section shaped like `topicSources` — per binding `shape` (`queue` / `stream` / `one-shot` / `request-response`), `onFailure` (`redeliver` / `skip-and-continue` / `n/a`), `unmatchedTopic` (`redeliver` / `n/a`); ports assert their constants against it. |
| `problem-details-cases.json` | `httpRules.envelopeEndpoint`: a processed `not-found` envelope → HTTP 200, body is the envelope, inner problem document excludes `status`; a `deleted` envelope → HTTP 200 with its body (not 204). `httpRules.healthEndpoint` (ports with HTTP and health): one canonical failing check → `GET /benzene/health` returns 503 with the health report. `envelopeCases`: a new canonical handler `conformance:validate` (request `{ "name": string, "items": [{ "sku": string }] }`, rules: `name` non-empty, every `items[].sku` non-empty), wired through the port's own validation integration; `{ "name": "", "items": [{}] }` → `validation-error` with `field`s `/name` and `/items/0/sku` (messages and codes unasserted). |
| `conformance/README.md` | The two new canonical handlers, the failing health check, the new fixture groups. |
| everything else | Unchanged — the `registry` group included (D7 keeps the bytes). |

**Ports and consumers:**

| Repo | Owes for `spec-0.2.0` |
|---|---|
| benzene-dotnet | `VersionSelector` → D1 (exact, cast-reachable, declared default, start-up warning); version getters → `["benzene-version"]`; `HealthCheckProcessor` returns `Ok`, and the HTTP health response maps `isHealthy: false` → 503; `BenzeneMessageHttpMiddleware` → 200 for processed envelopes, and every envelope-over-HTTP client (`HttpMeshServiceDispatcher`, the HTTP sender) reads the envelope whatever the status; defaults: `BenzeneMessageHttpOptions.DefaultPath`, `HttpMeshServiceDispatcher` default invoke path, `MeshUiExtensions.DefaultPath`, `SpecUiExtensions.DefaultPath` and `DefaultSpecUrl`, `McpOptions.DefaultPath` (`CloudServicePaths`/`CloudServiceProbePaths` are already correct); the EventBridge key; MCP headers; FluentValidation and DataAnnotations adapters emit pointers via the serializer's naming policy; the unmatched-topic-as-ignored option on each one-way binding that lacks it; `TopicFilter` documented as D21's restriction; docs (payload testing, Lambda test tool, getting-started pages), examples, changelog, matrix; The Count canary run. |
| benzene-go | Per the 0.1 audit (22 bindings; failure, missing result and unmatched topic already redeliver almost everywhere): an at-most-once override on the 21 bindings without one; the unmatched-as-`ignored` option; an at-least-once mode for the self-hosted Kafka consumer; `awslambda.EnvelopeHandler` acknowledging failures when invoked asynchronously; the self-hosted Event Hub consumer skipping a failed event in a running process (a bug); selection per D1 (exact-only today: add the declared default and cast reachability if casting ships); health and envelope status; paths; `benzene-headers`; validation → pointers; the two canonical handlers in its runner. |
| benzene-typescript | **Settlement:** SNS, Service Bus trigger and Azure Kafka trigger default their escalation knob on; EventBridge, S3, MSK, Kinesis and Event Hub trigger gain one — plus, from the 0.1 audit, a knob for the Functions Timer trigger and the Kafka worker (which never reads the result), and **a missing result redelivered on the ten bindings that acknowledge it today** (the adapters escalate only on `isSuccessful === false`). Selection per D1 (it ships casting); health and envelope status; paths; `benzene-headers`; Zod, Joi and Yup adapters → pointers (ajv already emits them); runner handlers; its runner runs 12 of 14 fixtures (confirmed by the audit). |
| benzene-python | Per the 0.1 audit (18 bindings; redelivery is already the default on every trigger): an at-most-once override on every trigger (none today); the unmatched-as-`ignored` option; the self-hosted Kafka loop's skip-and-continue (documented as at-least-once); the self-hosted SQS, Kafka and RabbitMQ loops classifying by status text instead of `isSuccessful` (a wire §1.2 defect); `idempotency()` storing a missing result as `ok`; selection per D1 (`exact_version` is already the default); health and envelope status; paths; `benzene-headers`; Pydantic adapter → pointers; runner handlers. |
| benzene-ui | **First**, before .NET re-embeds: envelope dispatch reads `isSuccessful`/`statusCode` from a 200 response rather than treating non-2xx as the failure signal; re-vendor `contracts/` at `spec-0.2.0`; rebuild `mesh-ui.html` and `mesh-spec-ui.html`. |
| benzene (website, mesh UI) | Re-vendor the rebuilt pages into `mesh-ui/` and `website/demos/` (the mesh-ui drift check gates it); blog/demo examples that spell old paths. |
| the-count | Take the .NET release; it sets its MCP path explicitly, so D4's MCP default does not move it. |

**Landed 2026-10-11** (`1ac37823`, `41f86bbe`, `befdef01`, fixtures `1579790b`). Three things the
text settled that this table did not spell out, each now owed by every port: `benzene:spec` answers an
unsupported `type` or `format` with `not-implemented` (wire §7); a service consuming a one-way transport
SHOULD serve `benzene:ping`, because .NET's queue and Lambda health checks send it and settlement would
otherwise redeliver every probe until it dead-letters; and `settlementDefaults` pins each binding's
**shape** and the defaults a shape allows, not one value per binding — .NET's own stream workers default
to skip-and-continue while its Lambda-hosted streams redeliver, and both are conformant. VERSION moves to
`0.2.0` once the .NET reference passes the new fixtures, so a fixture mistake costs a commit, not a
`spec-0.2.1`.

**Released 2026-10-11 as `spec-0.2.0`** (`2d4fe4d8`), cut once the merged .NET reference passed every
new fixture. Every port and benzene-ui is pinned to it with CI green — benzene-ui `1e3e41c`, .NET
`9e48b356`, Go `ef287d5`, TypeScript `c977776`, Python `07b2bfe` — and the rebuilt mesh UI is
vendored everywhere it is embedded (which also turned TypeScript's and Python's mesh-ui drift checks,
already red, green). The port work raised four more spec points, all inside the tag: an envelope
without `isSuccessful` is unsuccessful; a scheduled tick with no result MAY be acknowledged;
`benzene:ping`'s body is unspecified; and `settlementDefaults` names `blob-storage` and `cloudevents`.
`docs/capabilities.md` is row-refreshed, notes 3 and 8 closed, and the remaining divergences recorded
as notes 11–16 (none blocks the exit). **Open:** The Count's canary run waits for a published .NET
release; .NET's `Build Mesh Host` workflow fails at its kubectl dry-run step, red on main before this
phase and unrelated to it.

**Exit:** every port and benzene-ui green on `spec-0.2.0`; `docs/capabilities.md` divergence notes 3
(settlement) and 8 (version selection) and the "settlement is not in the spec" flag close.

### Phase 2 — Structure *(M; doc-only, no fixture changes; runs during the ports' Phase 1 catch-up)*

Decisions: D5, D6 (doc), D7, D9, D11, D19, D20 (labels), D22, D23, D24, D25.

| Action | Detail |
|---|---|
| One index | `README.md` becomes the index: the tiers (Core, Modules, Profile, Guidance), the *optional capabilities* category (D22), one table per tier with stability labels (D20), the conformance-language paragraph, the one design rule, a pointer to `VERSION`. `index.md` keeps only the nested list the website nav needs, grouped by tier, with matching descriptions. |
| Status lines | Each document's header becomes one line: `Core` / `Module — Stable` / `Module — Provisional: .NET` / `Profile` / `Guidance (informative)`. The "DRAFT v0.1 — promoted from…" prose goes. |
| Guidance | `design-principles.md`, `porting-guide.md`, `port-quality-standards.md` → `docs/guides/` (already a website source; add them to `docs/guides/index.md`). **Before** the move, design-principles §3 and §4.1's rules go into core-concepts (D23) and §4's both-sides rule is replaced by a citation of wire §2. Inbound links: `docs/patterns/*` (5 files), `docs/guides/mesh-ui.md`, `.claude/agents/ergonomics-champion.md`, `work/mesh-ui-product-vision.md`, `website/generator/MarketingPages.cs`. Deep links to the old benzene.app URLs break; pre-1.0, accepted. `porting-guide.md` §5 is deleted first (history). |
| `core-concepts.md` | §7–§8 per D24; §3 says how a handler reaches headers and invocation facts; a new §"Outbound": the sender, the routing table, declared sends (vocabulary from 0.2), and D25; the extension-point paragraph without DI vocabulary. |
| `versioning.md` | Becomes the Core versioning document at the same path (D22), ~80 lines: version is metadata; one header; absence; dispatch (D1); one header drives casting too; the route sugar; one informative paragraph on absorbing a version difference. §2.3, §3.1 and §4 → benzene-dotnet docs (a cookbook). §5.1–§5.3 and §5.5–§5.6 → `service-versions.md`. §5.4, §5.7, §6, §7 → `work/side-by-side-service-versions.md` (D19). |
| `service-versions.md` *(new)* | mesh §2.4–§2.5 plus the rationale above. Module — Provisional: .NET. |
| `mcp.md` *(new)* | The MCP entry from transport-bindings §2 and Phase 1's holding note: a binding that satisfies §1. Provisional: .NET. |
| `claim-check.md` *(new)* | wire §2.1. Module — Provisional: .NET. The header row stays in wire §2, classified "Module: claim-check". |
| `mesh.md` | Delete the status header, the revision note, §9, §4.1's status prose; §2.4–§2.5 move out; Provisional labels on the three query read models (D5). |
| `wire-contracts.md` | §1.1's `message`/`body` note and §2's "`benzene-status` … historically" go. §2's tier column → D9; "single injectable value" → "configurable in one place, both directions". §3.1 → D7's one sentence. §5's dedup rule → "keys are unique". §6 per D11. |
| `contract-document.md` | Status header and §6.1 go; references into `work/` plans go. |
| `cloud-service-profile.md` | §5 → one paragraph: a port MAY offer a wiring-time self-check, carried on `profile`, and an external probe; link to the .NET docs. |
| `conformance/README.md` | Claims re-cut to Core · per binding · Profile · per module (Provisional modules marked). |

**Landed 2026-10-11** (`8e4be084`, `1f92d964`, and the trims commit), with the website building green
from all four ports. Two departures from the table: design-principles §5.2's path table was
normative (the profile's R7 cites it), so it moved into transport-bindings §1 rather than into
guidance; and contract-document §6's numbering is kept (§6.1 became a two-line "Portability" note)
because fixture descriptions cite §6.x and Phase 2 changes no fixture. The .NET material from
versioning is `docs/cookbooks/versioning-internals.md` in benzene-dotnet.

**Fixture-text debt for the next fixture window (Phase 3):** fixture descriptions are not edited in
Phase 2, so four now cite text that moved — `mesh-version-order-cases.json` and
`mesh-service-version-cases.json` cite mesh §2.4–§2.5 (now `service-versions.md`; mesh keeps stubs at
both numbers so the citations still resolve), and `transport-metadata-cases.json` cites "tier D" and
says names MUST be "a single injectable value" (the spec now says "configurable in one place, both
directions"). Re-word them when 3a re-cuts fixtures.

**Ports:** link fixes; .NET takes the versioning cookbook. **Exit:** the website builds green with all
four ports; `remaining-issues-plan.md` P1.1 and P1.2 move to "decided" (D25), port work tracked
there.

### Phase 3 — One self-description *(L; the second cross-port window)*

Decision: D18, as ruled by spike 0.2. Opens only when every port and benzene-ui are green on
`spec-0.2.0`. Two spec PRs, two tags.

**3a — The document (`spec-0.3.0`).** `contract-document.md` per the ruled design
(`self-description-design.md`, §10 winning over §0–§9): `events[]`/`sends[]`, the two hash
projections over a dereferenced, metadata-stripped projection, the naming rule (normative but
outside the hash), the field decisions, the `{type, format}` request, and **§2.1's derivation rule
with a `derivationCases` group** (it moves here from 3b: the cross-port derivation is first tested
on the contract document). Fixtures: `contract-document-cases.json` and `contract-hash-cases.json`
re-cut with recomputed hashes, plus `derivationCases`, `namingCases` and `specTopicCases`. Ports, all
**M**: .NET configures Swashbuckle to the rule rather than replacing it, and adds the upgrade path
for committed contracts and compatibility baselines; **Go builds a contract-document producer** (it
has none — its `/benzene/spec` serves the descriptor), inline-first, with naming trailing;
TypeScript normalises Zod output and ports an outbound registry; Python merges its three producer
paths into one. Go and Python route `GET /benzene/spec` through the pipeline before `sends` is
served (Q6). The hash moves into each runtime package. Consumers: benzene-ui — **first, reading
both document shapes during the transition**, since it never reads a register body this is all of
its Phase 3 work — and the Spec UI.

**3b — The register body (`spec-0.4.0`).** The contract document gains the identity fields and
becomes the `benzene:mesh:register` body; `benzene:mesh` (mesh §1) and `descriptorHash` (§2.2) go;
mesh §2 reduces to "the register body is the contract document"; §2.1's type mapping moves into
contract-document as the derivation rule; heartbeats carry the registration hash; the collector's
graph's providers are **events ∪ sends** (a published event's publisher is its provider). The interception rule already lives in the reserved-topic
section (D13). Fixtures: `mesh-descriptor-cases.json` folds into `contract-document-cases.json`;
`mesh-collector-cases.json`, `mesh-service-version-cases.json` and `mesh-issue-cases.json` take the
new register body. Ports: one builder for spec and register; the four collectors (`meshd`, .NET
`Benzene.Mesh.Collector`, TypeScript, Python). Consumers: none in benzene-ui (it reads aggregator
artifacts, not register bodies); the mesh UI only if the fleet read model's `binding` → `transports`
lands.

**Exit:** every port passes the re-cut fixtures; a Go collector hosts a .NET service and the reverse
on the new body (the live-interop form); the "Spec endpoint" and "Mesh — service side" rows of
`docs/capabilities.md` are re-read.

### Phase 4 — The profile *(S)*

Decisions: D15, D3 (R8 moves). After Phase 3, because the claim's carrier is the self-description.
Spec: `cloud-service-profile.md` is R1–R5 and R7; R6 and R8 move into `mesh.md` as the module's
conformance claim; the `profile` field is documented on the contract document. Fixtures: none
(`profile` is optional and unpinned). Ports: .NET `CloudServiceProfileReport` and
`Benzene.CloudService.Probe`; Go `cloudservice.New`; TypeScript and Python verify.

### Phase 5 — Close *(S)*

- `VERSION` and a release tag for the spec as 1.0 is cut; README's "Versioning" section reduced to
  three lines (the file, semver from 1.0, a profile requirement added is a major).
- `docs/capabilities.md` full refresh (`port-aligner`); the spec-gap flags are re-derived.
- To `work/archive/`, stamped with the phase commits: `benzene-headers-design.md`,
  `benzene-headers-plan.md` (Phase A done, the packed-headers half rejected), this plan,
  `spec-review-2026-10.md`, `self-description-design.md`. `work/README.md` updated.
- `remaining-issues-plan.md` P1.1–P1.3 removed.

**Minimum before 1.0:** Phases 0–4. If Phase 3 slips, freeze Core only, and mark the profile and
every module Provisional for the 1.0 tag.

## 3. Per-repo view

| Repo | 0 | 1 | 2 | 3 | 4 |
|---|---|---|---|---|---|
| **benzene** (spec) | tags, `VERSION`, spike | three spec PRs, `spec-0.2.0` | tiers, guidance move, new module docs | two spec PRs, `spec-0.3.0`, `spec-0.4.0` | profile |
| **benzene** (website, mesh UI, blog) | — | re-vendor UI pages; examples | link fixes | re-vendor UI pages | — |
| **benzene-dotnet** | pinned drift-check; run service-version fixture; canary checklist | selector, version header, health, envelope 200, paths, EventBridge, MCP headers, pointers, unmatched-as-ignored | links; versioning cookbook | generator, codegen, collector, one builder | profile report + probe |
| **benzene-go** | settlement audit; pinned drift-check | settlement, selection, health, envelope, paths, `benzene-headers`, pointers | links | generator, codegen, `meshd` | `cloudservice.New` |
| **benzene-typescript** | settlement audit; pinned drift-check; beta.2 condition | **settlement (8 adapters)**, selection, health, envelope, paths, `benzene-headers`, pointers (3 adapters) | links | generator, codegen, collector | verify |
| **benzene-python** | settlement audit; pinned drift-check | settlement (~15 triggers), selection, health, envelope, paths, `benzene-headers`, pointers | links | generator, codegen, collector | verify |
| **benzene-ui** | pinned `contracts/` | envelope status handling; re-vendor; rebuild — **first** | — | spec readers; register body — **first** | — |
| **benzene-patterns** | — | rebuild against the ports' releases | — | rebuild | — |
| **the-count** | canary | take the .NET release | — | take the .NET release | — |

**At risk:** TypeScript (a broken 129-package publish, the largest settlement item, three validator
adapters, unconfirmed fixture runner), then Python (settlement unknown across ~15 triggers).

## 4. What this plan deliberately does not do

- It does not touch `status-vocabulary.json` or the HTTP/gRPC mapping tables (the deferred items).
- It does not reorganize `docs/specification/` into sub-folders (rule 5).
- It does not rewrite `blog/` or the website's marketing copy beyond moved links and old paths.
- It does not decide the .NET-only `benzene:*` topics (`test-payloads`, `mesh:report`,
  `mesh:annotations:add`, `mesh:aggregate`, `mesh:topology`, `mesh:query:trace`,
  `mesh:query:correlation`): under D13 they may stay as port topics; a port proposes a row in the
  reserved-topic section when it wants one to be interoperable.

## 5. Checklist

```
Phase 0  [x] 0.1 audit (Go, Py, TS)  [x] 0.2 spike + champion review, ruled  [x] 0.3 tags + pinned checks (4 ports + ui)  [x] 0.4 .NET fixture (bug fixed; D26 ruled, lands in Phase 1)  [x] 0.5 canary
Phase 1  [x] 1a  [x] 1b  [x] 1c  [x] fixtures  [x] spec-0.2.0  [x] ui  [x] .NET  [x] Go  [x] TS  [x] Py  [ ] the-count  [x] capabilities.md
Phase 2  [x] spec PR  [x] website green (4 ports)  [x] .NET cookbook
Phase 3  [ ] 3a spec-0.3.0  [ ] 3b spec-0.4.0  [ ] ui  [ ] .NET  [ ] Go  [ ] TS  [ ] Py  [ ] live interop both ways
Phase 4  [ ] spec PR  [ ] .NET  [ ] Go  [ ] TS  [ ] Py
Phase 5  [ ] VERSION/tag  [ ] capabilities.md full refresh  [ ] archives + README
```
