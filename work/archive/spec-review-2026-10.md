# Specification review — October 2026

> ARCHIVED 2026-10-11 — actioned. Evidence: the spec release `spec-1.0.0` (Benzene `5dd10f17`); Actioned by `spec-simplification-plan.md` (archived beside it); the spec released as `spec-1.0.0`.

**Status:** REVIEW — findings and recommendations, not decisions. Written before any 1.0 tag, while
every contract is still cheap to change.
**Scope:** every document under `docs/specification/`, with the weight on the higher levels (README,
design principles, core concepts, wire contracts, transport bindings) and the transport spec. The
mesh, versioning and contract-document documents are reviewed for what they cost the higher levels.
**Criterion:** as simple as the spec can be while still offering the features and the consistency.
**Method:** every claim below was checked against the spec text, the conformance fixtures, the
cross-port capability matrix (`docs/capabilities.md`) and, where a claim is about the reference
implementation, benzene-dotnet's source.
**Revised 2026-10-11** after a second-round review by the architecture reviewer and the core product
owner; §8 records what that round changed and why. The decisions taken from this review are in
[`spec-simplification-plan.md`](spec-simplification-plan.md).

---

## 1. The verdict

The spec's *ideas* are sound and worth keeping: a message is `{topic, headers, body}`; a result is a
string status plus `isSuccessful`; a failure body is an RFC 9457 problem document; a binding is seven
obligations; the profile is a named MUST-set over an open core; the mesh graph is declared, not
observed; "is this a concept or an idiom" is the one design rule. None of that needs to change.

What needs to change is the **amount of specification wrapped around those ideas**, a contract that
is missing entirely (settlement), and a handful of places where the spec disagrees with itself or
with the reference port. By the spec's own rule of thumb (porting-guide §1: *a concept is something
whose removal changes what is on the wire or what a handler observes*), well over half of the 3,470
lines is not concept: it is implementation narrative, revision history, .NET package mapping,
process guidance, and proposals. A porting author cannot find the lines that bind them, and the
duplicated parts have already drifted (two self-description documents, two hash canonicalizations,
a reserved-topic list that exists in the reference port but not in the spec).

The highest-leverage changes, in order:

1. **Finish the transport contract.** Settlement of a failure result on one-way bindings — the one
   safety-relevant rule — is not in the spec; nor is the HTTP status of the envelope endpoint (§4.5).
2. **Settle the contradictions** of §3 — version selection above all, since Go and .NET already ship
   different defaults because of it, and the health check, which the reference port implements by
   breaking the `isSuccessful` rule.
3. **One reserved-topic registry** of the topics that must work across ports (§2.5).
4. **Three tiers, by what binds whom** — Core, Modules, Profile — with guidance moved out and the
   narrative deleted (§2.2–§2.4). Costs the ports nothing but link fixes.
5. **One self-description document**, narrowed and designed by a short spike first (§2.1).

---

## 2. Structural findings (the higher levels)

### 2.1 Two self-descriptions of one service

A service derives, from one registry, two documents that describe the same thing:

| | ServiceDescriptor (mesh.md §2) | Contract Document (contract-document.md) |
|---|---|---|
| Reached via | reserved topic `benzene:mesh` | HTTP `/benzene/spec` (and a `benzene:spec` topic the spec never defines) |
| Consumed topics | `topics[] { id, version, requestSchema, responseSchema }` | `requests[] { topic, version, reserved, httpMappings, request, response, example }` |
| Outbound | `produces[]` — every topic the service *sends*, request/response calls included, with a `responseSchema` | `events[] { topic, version, message, example }` — fire-and-forget topics the service *publishes* |
| Transports | `binding` — one string | `transports[]` — an array |
| Schema dialect | "a subset of JSON Schema 2020-12", inline, no `$ref`, recursion cut to `{}` | OpenAPI 3.0 Schema Objects, `$ref` into `components.schemas`, a normative closure walk |
| Hash | `descriptorHash`: documented member order, "per-port by design" | `contractHash`: RFC 8785 (JCS), compared across ports |
| Pinned by | `mesh-descriptor-cases.json` | `contract-document-cases.json`, `contract-hash-cases.json` |

Each has a reason it looks the way it does (the descriptor came from Go, the contract document from
.NET's OpenAPI tooling). Neither reason is a reason to keep both: a port implements two schema
derivations and two canonicalizers, and a reader learns two vocabularies for one idea.

But the two are not simply two spellings of one thing, and a merge has to respect three differences:

- **`produces` and `events` mean different things.** `events` is what a client of this service can
  subscribe to; `produces` is everything this service depends on downstream, including RPCs it makes.
  Folding `produces` into `events` (which .NET's builder already half-does,
  `EventServiceDocumentBuilder.cs`) would mean adding a downstream dependency changes the hash every
  generated client embeds — false drift, since nothing a client can call changed — and the declared
  response type of outbound calls is lost.
- **Cross-port identical output needs a naming rule.** `mesh-descriptor-cases.json` asserts
  identical inline schemas across ports. Once schemas are `$ref`s into `components.schemas`, identical
  output and a cross-port hash need a normative rule for naming and hoisting components. Today the
  names are CLR type names. Neither document has such a rule.
- **`benzene:spec` serves more than one format.** In .NET it takes `{type, format}` and serves the
  Benzene document, OpenAPI 3.0 or AsyncAPI 3.0 (`SpecBuilder.cs`). The fleet-wide AsyncAPI document
  (`AsyncApiCompositor`) and the Spec UI's default URL (`/spec?type=benzene`) depend on that.

**Recommendation — narrow, and spike first.** One document, the contract document, gains the
identity fields (`service`, `serviceVersion`, `versionScheme`, `runtime`, `placement`, `profile`,
`degraded`) and becomes the `benzene:mesh:register` body; `benzene:mesh` and `descriptorHash` go.
Outbound splits into `events[]` (published) and `sends[]` (dependencies, with response schemas). One
algorithm (JCS + SHA-256), two projections: `contractHash` over `requests` + `events` + `components`
(what clients depend on), and a registration hash that adds `sends` (what the collector's graph
depends on) — contract-document §6.4 already establishes that a hash is per-projection. A
component-naming rule is specified. `benzene:spec` keeps an optional `{type, format}` request: the
contract document is the default and the only conformance-tested output; ports MAY serve others.
Moving `components` to JSON Schema 2020-12 (OpenAPI 3.1's schema object) is a separate decision,
after 1.0, behind a discriminator. A short design spike settles `events`/`sends`, the two
projections and the naming rule before any of this is specified.

### 2.2 "Core" is not the core

README §"Two levels": *Core = everything in this directory except the profile.* That makes mesh
(594 lines), versioning (616), contract-document (331), port-quality-standards (168) and
porting-guide (132) part of "the Benzene Core Specification". A porting author told to implement
Core is told to read 3,470 lines to find the few hundred that bind them. The conformance README's
claims table already knows better — it has eight distinct claims — but the document structure doesn't
follow it.

**Recommendation.** Tiers, and each document says which it is:

| Tier | Binds | Documents |
|---|---|---|
| **Core** | every port, every service | `core-concepts.md`, `wire-contracts.md`, `transport-bindings.md` (§1 + §3), `versioning.md` (its wire part — below) |
| **Modules** | a port that claims the module | `contract-document.md` (→ the self-description, §2.1), `mesh.md` (feeds, collector), `service-versions.md`, `claim-check.md`, `mcp.md` |
| **Profile** | a service that claims it | `cloud-service-profile.md` |
| **Guidance** *(informative)* | nobody | `design-principles.md` (most of it), `porting-guide.md`, `port-quality-standards.md` → `docs/guides/` |

Three refinements the second round added:

- **Versioning's wire part is Core, not a module.** Handler-version dispatch is in Core
  (core-concepts §2), and its only signal is the `benzene-version` header. A Core-only port that
  dispatches by version but has no defined way to receive a version is incoherent. The header,
  absence and dispatch go into Core (they are a few paragraphs); payload casting stays a port idiom.
- **Core contains optional capabilities.** Health checks, trace context and versioning are "optional,
  but if implemented, exactly this shape". That is a category; README names it once instead of each
  document improvising the wording.
- **A module with one implementation is Provisional.** MCP (.NET), claim-check (.NET) and service
  versions (.NET) stay in the spec, labelled Provisional with their implementing ports, until a
  second port implements them. That is the consistent line between them and Mechanism C, which has no
  implementation and goes to `work/`.

And a bucket that is not the spec at all: **proposals** — versioning §5's version-aware outbound
routing (no port has it) and versioning §7's open questions. The spec's rule that concepts land
"before or alongside" code is right; a proposal with no implementation is not "alongside".

Two status headers are stale: versioning.md says "no port implements" the service-version identity
layer, but benzene-dotnet's collector keys its catalog by `(service, serviceVersion)`
(`MeshCollectorStore`) and ships the order comparators (`MeshServiceVersion`). (No .NET test runs
`mesh-service-version-cases.json`, though — `remaining-issues-plan.md` P1.3.) A status line nobody
maintains is worse than none.

**What 1.0 freezes** is said nowhere. Each tier needs a stability label: Core and the profile frozen
under semver from 1.0; each module Stable or Provisional; guidance unversioned.

### 2.3 Five layering vocabularies for one idea

The spec explains "how much of Benzene you are using" five ways: Core/Profile (README), adoption
rungs 1–5 (design-principles §2), header tiers A–D (wire-contracts §2), requirement ids R1–R8
(profile), and eight conformance claims (conformance README). They overlap without mapping cleanly:
profile §6 maps R→rungs; header **tier B is defined and contains no header** (A: `topic`,
`content-type`; C: `benzene-version`, `traceparent`/`tracestate`, `x-correlation-id`,
`benzene-claim-check`; D: `_benzeneHeaders`). An empty tier is the sign the scheme was designed ahead
of its use.

**Recommendation.** One vocabulary — Core / Module *name* / Binding *name* / Profile — used
everywhere a level is named. The adoption ladder stays as an informative narrative in guidance, not
as a fifth level system.

### 2.4 History and implementation narrative inside normative text

Pre-1.0, git holds the history; the spec should hold the rule. Lines that are history, status, .NET
package narrative, or references into `work/` and port internals:

| Document | Lines (approx.) | What |
|---|---|---|
| wire-contracts.md | 35 | §1.1 `message`/`body` note; §1.3 "this document previously described two rules…"; §2 "`benzene-status` … appeared in this table historically"; §3.1's three-way `code`/`type`/`classification` comparison |
| mesh.md | 90 | status header; revision note; §4.1's "additive 2026-07… parity pending" prose; §9 (.NET package convergence table) |
| versioning.md | 400 | status header; §2.3 (.NET DI wiring); §3/§3.1 (C# samples); §4.1–§4.4 (decorator internals, BFS chain composition, "required redesign — done"); §5.4, §5.7 (proposal); §6–§7 |
| contract-document.md | 40 | status header; §6.1 "why a new algorithm"; references to `cross-language-clients-plan.md` and `spec-mesh-tooling-implementation-plan.md` |
| cloud-service-profile.md | 40 | §5's description of `Benzene.CloudService`, `Benzene.CloudService.Probe`, the CLI command and the tri-state verdict |
| porting-guide.md | 15 | §5 ".NET-isms that must not leak" (one entry already struck through) |
| conformance/README.md | 10 | the `_benzeneHeaders` paragraph that cites `work/benzene-headers-plan.md` |
| design-principles.md | 10 | §5.3 migration note (and it is wrong — see §3 row 5) |

About 650 lines, none of it binding. A spec that cites a `work/` plan or a C# class name is not
language-neutral, and those citations rot first.

### 2.5 The reserved-topic registry that does not exist

The spec defines `benzene:healthcheck` (wire §5), `benzene:mesh` (mesh §1) and the four collector
topics (mesh §4), mentions `benzene:mesh:query:*` as deliberately unpinned, and names
`benzene:spec` (contract-document §5.1, profile R5) **without ever defining it**.

The reference port serves at least twelve `benzene:*` topics the spec does not define: in its
`BenzeneTopic` registry, `benzene:spec`, `benzene:test-payloads`, `benzene:liveness`,
`benzene:readiness`, `benzene:ping`; in `MeshAggregatorTopics`, `benzene:mesh:report`,
`benzene:mesh:annotations:add`, `benzene:mesh:aggregate`; `benzene:mesh:dispatch`
(`Benzene.Mesh.Dispatch`); `benzene:mesh:topology` (Tempo); `benzene:mesh:query:trace` and
`benzene:mesh:query:correlation` (`Benzene.Mesh.Wire`). One of them is already cross-port in
practice: the canonical mesh UI, which Python also serves, calls `benzene:mesh:dispatch`.

Separately, design-principles §5.2 defines the well-known **HTTP paths** (`/benzene/health`,
`/benzene/spec`, `/benzene/invoke`, three UIs) as a second registry, with "reserved topics take no
prefix — the prefix is an HTTP-surface concern" as the only link between them.

**Recommendation.** One section in wire-contracts that lists the **interoperable** reserved topics —
the ones a tool may rely on across ports — each with its request and response shape: the health
topics, `benzene:spec`, `benzene:ping`, the collector topics and `benzene:mesh:dispatch` (by
reference to the mesh module). The rule: the `benzene:` prefix is reserved to the framework; a port
MAY serve other topics under it; a tool MUST NOT depend on a topic the section does not list. The
interception rule (match by topic id alone, ignoring version) — today stated only in mesh §1 —
moves here.

Default HTTP paths do not belong in that Core table: they are the HTTP binding's default
`httpMappings` of reserved topics, the same mechanism domain topics use (`GET /benzene/health` →
`benzene:healthcheck`, `GET /benzene/spec?type=…` → `benzene:spec` with that request). The UI paths
are not topics at all, and `/benzene/invoke` is the envelope carrier, not a topic. Keep
`benzene:healthcheck` as the name: the binding maps it to `/benzene/health` explicitly, and renaming
it would be the second topic rename since July for no behaviour.

### 2.6 The outbound half has no home

core-concepts is entirely inbound. The outbound model is scattered: the sender interface in
transport-bindings §2 (last subsection); the outbound routing table only by implication in
versioning §5.4 and porting-guide §4; outbound *registration* (declared `produces`) in mesh §2.3;
the generated client's dependency on the sender in contract-document §5.4; decorators in
transport-bindings §2. A reader cannot find out what "sending a message in Benzene" means without
reading four documents.

**Recommendation.** A `core-concepts` §"Outbound" with three concepts: the **sender**
(`send(topic, headers, body, version?) → result`), the **routing table** (`topic → destination`,
deployment configuration, not contract), and **declared sends** (the registry's outbound twin; the
name follows the §2.1 spike). It is also the place to settle what a caller sees when a peer fails —
whether a client rebuilds `errors` from a received problem document (`remaining-issues-plan.md`
P1.1/P1.2), which today differs by port.

---

## 3. Contradictions to settle

| # | Where | The two readings | Recommendation |
|---|---|---|---|
| 1 | core-concepts §2 vs versioning §3 | "selected **only** by an exact version match" vs default selector "exact match, **else highest available**". Go ships exact-only citing this; .NET ships exact-else-highest (`docs/capabilities.md` note 8) — and .NET's "highest" is the ordinal string maximum, so `v9` beats `v10`. | **Exact; else a handler the request can be cast to; else `not-found`** naming the version. Plain exact-only would break shipped payload casting: a `v1` request reaches the single `v2` handler and is upcast only because selection falls back today (`CastingRequestMapper`, `examples/K8sMesh`). **Absent version → the topic's default handler** — the unversioned one, or one explicitly declared default; none → `not-found`, with a start-up warning when a topic has versioned handlers and no default. The spec says plainly that one header drives both dispatch and casting. Ships together with dropping the `version`/`x-version` fallback headers (§4.4), or an unrelated `version` header a caller already sends turns every request into `not-found`. |
| 2 | core-concepts §5 vs wire §1.2 vs wire §3 | `isSuccessful` "derived from status class" / "the authoritative signal… a receiver MUST prefer this field" / "a status that collides with a known failure string… is still classified as that failure regardless of `isSuccessful`". The reference health check sits in the gap: unhealthy returns `service-unavailable` **with** `isSuccessful: true`, so HTTP probes get a 503 and the body is the health report (`HealthCheckProcessor.cs`). | `isSuccessful` MUST match the status table, no exceptions; the receiver trusts it; stated once, in wire §1.2. The health topics return `ok` whenever the checks ran, and `isHealthy` in the payload carries health; the HTTP binding's default mapping for the health topics returns 503 when `isHealthy` is false. The §1.3 "escape hatch" goes. |
| 3 | core-concepts §10 vs wire §2 vs profile R8 vs mesh §3 | trace context "MUST behave identically on every transport" / tier C "Benzene… does not require them" / MUST (R8) / SHOULD forward (mesh). | Core: *if* propagated, verbatim W3C (the interop rule). The mesh claim (R8, which moves with R6 — §4.6): MUST join and forward. Delete the other two statements. |
| 4 | transport-bindings §2 (informative) | Contains MUSTs: EventBridge "needs no bolted-on `topic`", MCP "MUST NOT auto-expose", "a start-up check MUST fail". | Normative rules move to §1, to `transport-metadata-cases.json`'s `topicSources` (which already pins the EventBridge and DynamoDB topic sources), or to the MCP document. |
| 5 | design-principles §5.3 vs benzene-dotnet | "`/benzene/invoke`… already follow[s] it". Half true: `CloudServicePaths.Invoke` is `/benzene/invoke`, but the envelope endpoint's own default (`BenzeneMessageHttpOptions.DefaultPath`) and the mesh dispatcher's (`HttpMeshServiceDispatcher`) are `/benzene-message` — so .NET's dispatcher misses .NET's own profiled services. `/mesh-ui`, `/spec-ui`, `/mcp` and the Spec UI's default spec URL (`/spec?type=benzene`) are unprefixed. | Change the defaults now and delete §5.3. The installed base is one .NET alpha and one application (§4.13), which sets its MCP path explicitly. |
| 6 | mesh §4 vs `mesh-collector-cases.json` | `benzene:mesh:query:*` "deliberately not part of this contract" — but every collector case asserts through them. | Label the three read models the fixture uses (`fleet`, `service`, `topic`) Provisional module content now, so the fixture asserts against something the spec acknowledges; pin them fully after 1.0. |
| 7 | wire §2 naming rule vs transport-bindings MCP | Invented names carry `benzene-`; MCP introduces `mcp-tool`, `mcp-session` headers, absent from the header table. | `benzene-mcp-tool` / `benzene-mcp-session`, listed under the MCP binding. |

---

## 4. Document by document

### 4.1 README.md and index.md

Two tables of contents for the same twelve files, with different one-line descriptions (index:
"Payload Schema Versioning — versioning a topic's request/response shape"; README: "three axes…
plus one specified-but-unbuilt mechanism"). Merge into one. README §"Versioning" says the spec is
versioned "as one unit" but no version exists beyond "DRAFT v0.1" stamped on each file, while the
ports' `SPEC_VERSION` is a commit hash — pick one: a `VERSION` file or a tag in this repo, and pin
each port's drift-check to it. Keep §"The one design rule" exactly as it is; it is the best paragraph
in the spec and the test this review applied.

### 4.2 design-principles.md

The status header says its defaults *and* its override points are normative; the body is mostly
strategy. Four kinds of content are interleaved:

- **Informative strategy** (§1, §2 the adoption ladder, the "examples are where this is proved" rule)
  — good writing, belongs in guidance.
- **Normative rules that must survive the move.** §3: every capability that depends on a steer MUST
  degrade gracefully when the steer is declined, and MUST state its degradation. §4.1: a shorthand
  MUST be composed from the public explicit form, and inference MUST be verified before any message
  is handled. These are the architectural backbone of "opinionated but optional"; they move into
  core-concepts, not into guidance.
- **A normative rule stated twice**: §4 "every convention, both sides" and wire-contracts §2
  "reserved names are defaults… an override applies to both directions" are the same rule. Keep
  the wire-contracts statement (it is where the names are) and cite it. The §4 extension-point table
  uses .NET DI vocabulary ("replace the topic-getter registration"); the concept is that each is a
  replaceable component with a default.
- **§5 well-known surfaces** → the reserved-topic section and the HTTP binding (§2.5). §5.3 is wrong
  (§3 row 5).

### 4.3 core-concepts.md

The best-shaped document. Keep §1–§6, §9, §10. Changes:

- **§3**: say how a handler reaches headers and invocation facts (via an accessor resolved from the
  scope — currently only implied by §4's "ride on the context… or an accessor").
- **§5**: `isSuccessful` per §3 row 2. The `errors` paragraph's cross-reference to the mesh
  fingerprint is noise here.
- **§7 Application lifecycle**: `getConfiguration / configureServices / configure` is
  `BenzeneStartUp` promoted to a concept, and two ports already have no configuration phase
  (`work/ceremony-parity-audit.md` §1). The concept is two sentences: one application definition can
  mount several transports, and a mount the host cannot honour is a silent no-op; and **registration
  completes, and is validated, before the first invocation** — the moment core-concepts §2's "startup
  error" and every start-up check need. transport-bindings §1 and §3 cite the `configure` phase and
  are reworded to "mount".
- **§8 Dependency registration**: `addSingleton/addScoped/addTransient/tryAdd*/getService/
  tryGetService` is MS DI. The concept is the three semantics §8 itself says must survive: a scope per
  invocation; framework defaults the application can replace; handlers and middleware constructed
  with their declared dependencies, per scope. Keep those; drop the operations.
- **Add**: a pipeline can be invoked in process with no binding at all. It is rung 1 of the ladder
  and the vision's "testable as a plain function call", and nothing normative says it once the
  porting guide and port-quality-standards move to guidance.
- **Add §"Outbound"** (§2.6), and the design-principles rules of §4.2.

### 4.4 wire-contracts.md

The genuine cross-language interop surface — envelope, problem document, reserved metadata keys,
status vocabulary + `isSuccessful`, HTTP/gRPC mappings, health response, JSON defaults — is about
150 lines. The document is 388. Where the rest goes:

- **§3.1 problem-type registry.** Eleven URIs under `https://benzene.app/problems/`, each equal to
  the base plus `benzeneStatus`. The table and its titles cost text, not behaviour: the bytes are
  already a mechanical rule, shipped in all four ports since August, and generic RFC 9457 clients
  discriminate on `type`. Shrink §3.1 to that one sentence and keep the bytes; delete the three-way
  `code`/`type`/`classification` paragraph.
- **`errors[].field`** is specified as "JSON Pointer for schema-based validators, the host
  language's property path for others — document which per integration": a per-validator
  convention baked into the wire (a TypeScript client reads `"Name"` from .NET and `"/name"` from
  Python). Pick **JSON Pointer (RFC 6901)**, with a defined target: the pointer addresses the
  handler's request type in its JSON form, using the serializer's naming policy (rename attributes
  included). A rule that cannot be resolved to a pointer omits `field` rather than emitting a
  non-pointer. The gRPC binding converts to the dotted path `FieldViolation.field` conventionally
  uses. After a payload cast the pointer addresses the handler's version of the type, not the
  caller's. Only ajv and JsonSchema.Net emit pointers today; FluentValidation, DataAnnotations, Zod,
  Joi, Yup and Pydantic adapters all need conversion.
- **§2 header table.** Replace tiers with Core / Module / Binding (§2.3). Five of the seven rows are
  not Core today. The "Naming" paragraph and "Reserved names are defaults" are right and stay — but
  "MUST expose them as a single injectable value" is an implementation shape; the concept is
  *configurable, in one place, both directions*.
- **§2.1 claim check** — 25 lines of normative security rules for an add-on one port ships. A
  Provisional module document.
- **`benzene-version` fallback list** (`benzene-version` → `version` → `x-version`): the document
  itself describes how it goes wrong (a foreign `version` header silently read as a schema
  version). One header, `benzene-version`, configurable like every other reserved name.
- **§3 vocabulary.** Seventeen statuses. `updated` and `deleted` both map to 204 and the reverse
  mapping can only return `deleted`; gRPC collapses all six successes to `OK`. Candidate for removal
  — a judgement call, flagged, not insisted on.
- **§4.2 gRPC**: `conflict → AlreadyExists` is specifically "entity already exists" in gRPC; the
  canonical code for a concurrency conflict is `Aborted`. Minor; flagged.
- **§5 health**: `healthChecks` key dedup with `-2`/`-3` suffixes is an implementation detail; "keys
  are unique" is the rule.
- **§6 serialization**: "Reading: property-name matching is case-insensitive" is a System.Text.Json
  default. JavaScript's `JSON.parse` and Python's `json` are exact-match; mandating it costs every
  non-.NET port a custom deserializer for no interop gain (writers already MUST emit camelCase). Make
  case-insensitive read a MAY.
- **Field names.** The same value is spelled four ways — result `status`, envelope `statusCode`,
  problem `benzeneStatus`, TraceEvent `status` — and `topicVersion` (TraceEvent) sits beside
  `version` elsewhere. Worth knowing, **not worth renaming**: handler authors never see the envelope,
  RFC 9457 owns an integer `status` one level down, `topicVersion` reads more clearly next to
  `serviceVersion`, and the rename would touch six repositories including the canonical mesh UI.

### 4.5 transport-bindings.md

**§1 (the binding contract)** is the right shape — seven obligations — and is missing the two that
matter most to a user:

1. **Settlement.** Item 5 says "queue/event transports define their acknowledge/reject behaviour
   instead", and nothing in the spec defines it. This is the safety-relevant observable: whether a
   `validation-error` result on an SQS message is retried or silently dropped. The reference port
   has a maintainer-approved rule (benzene-dotnet `docs/message-result.md`, "1.0 settlement
   contract"); TypeScript's matrix says plainly it has not caught up (three adapters default the knob
   off, five have no knob); Go's and Python's matrices do not state theirs. The rule, as an eighth
   obligation:
   - A one-way binding MUST NOT acknowledge a message whose invocation ended with
     `isSuccessful: false`, with no result, or with an uncaught error; any success-class result,
     `ignored` included, is acknowledged. One-way bindings are therefore at-least-once, and handlers
     on them must be idempotent.
   - A topic no handler matches is a `not-found` result and is not acknowledged. A binding SHOULD
     offer to treat unmatched topics as `ignored`, because a consolidated service subscribed to a
     shared bus would otherwise fill its dead-letter queue with events it was never meant to handle.
   - A middleware that ends the pipeline on a one-way binding MUST set a result; "no result" is a
     failure, so a middleware-only consumer that forgets is redelivered, visibly.
   - A binding MUST offer an at-most-once override. A stream-shaped binding whose retention blocks
     the partition MAY default to skip-and-continue and MUST document it.
   - Where the transport checkpoints a batch or a position rather than a message (a change feed, a
     stream shard), the binding MUST define its unit of settlement.
   - Deterministic failures reach a dead-letter destination only through the transport's own redrive
     configuration; the spec says so rather than implying the framework does it.
2. **The envelope over HTTP.** R4 makes `/benzene/invoke` the surface every tool uses, and the spec
   never says what HTTP status accompanies the envelope. The reference port maps the Benzene status
   onto the HTTP status line — which breaks for `updated`/`deleted`, mapped to 204, a status that
   cannot carry a body (RFC 9110 §15.3.5), so the envelope is lost. It also conflates the carrier
   with the cargo: a 404 can no longer distinguish "no envelope endpoint here" from "the envelope's
   topic was not found", and gateways rewrite 5xx bodies. **Every envelope the endpoint processed is
   returned with 200**; non-2xx is reserved for carrier failures (malformed envelope, authentication
   on the carrier). The inner problem document omits `status`.
3. **The envelope endpoint is a generic RPC** into every handler, and the spec says nothing about
   who may call it. It MUST run the same pipeline (and so the same authentication middleware) as the
   service's other transports, and MUST support restricting which topics are reachable through it.

**§2 (the catalogue)** is informative, uneven (HTTP: 4 lines; MCP: 27; Pub/Sub, Service Bus, Event
Hub, S3, Kinesis, Queue Storage, Event Grid, Blob: absent — though the ports ship them and
`transport-metadata-cases.json`'s `topicSources` already lists eleven bindings with their topic
source and metadata channel). Replace it with one table — binding · topic source · metadata channel ·
reply channel · unit of work · settlement default · cancellation source — and put per-binding detail
in each port's docs. HTTP's row should say how it resolves a topic: an explicit route table
(`httpMappings`) plus the envelope endpoint; there is no implicit `POST /{topic}`. This is additive
and can follow 1.0; lifting the MUSTs out of §2 (§3 row 4) cannot.

**MCP** is a binding with its own rules and reserved headers (§3 row 7): its own document, framed as
a binding that satisfies §1, labelled Provisional (one port, two weeks old).

**Outbound clients** → core-concepts (§2.6). **§3 multi-vendor rules** — keep, verbatim.

### 4.6 cloud-service-profile.md

The profile is the right idea. Three things:

- **R6 and R8 belong to the mesh, not the profile.** A service cannot claim "Cloud Service" today
  without pushing four feeds to a collector. And R8's rationale ("consumer edges… derived from trace
  parentage") has been stale since the 2026-08 mesh revision made the graph declared; its only
  remaining consumer is mesh §4.2. Move both into the mesh module's own conformance claim. The
  profile becomes R1–R5 and R7 — pull-able by any tool, no collector needed — with **one** name. A
  second profile level would be a sixth layering vocabulary (§2.3); a collector already knows who
  registered with it.
- **Where the claim lives.** `profile` sits on the descriptor, reachable only through `benzene:mesh`.
  A profiled service that is not meshed would have no carrier for its claim, so `profile` moves to
  the self-description served at `benzene:spec` — which is why the profile change follows the §2.1
  merge rather than preceding it.
- **§5** spends 40 lines describing `Benzene.CloudService`, `Benzene.CloudService.Probe`, a CLI
  command and a tri-state verdict. The spec-level content is one sentence: a port MAY offer a
  wiring-time self-check, carried on the `profile` field, and an external probe.

### 4.7 mesh.md

594 lines; about 300 is the contract a meshed service and a collector need: §1 (reserved topic, until
the §2.1 merge), §2 (descriptor), §3 (TraceEvent), §4 (collector topics, the declared-graph rule,
§4.1 issues, §4.2 declared vs observed), §5 (heartbeat), §6 (degradation). The rest:

- §2.4–§2.5 service-version identity and order — implemented by the .NET collector; 100 lines and
  two fixtures. → `service-versions.md`, a Provisional module, so mesh.md is the feeds and the
  collector.
- §4.1 issue feed — emitted by .NET and Go, collected by .NET. Stays; loses its status prose.
- `benzene:mesh:query:*` — Provisional labels on the three read models (§3 row 6).
- §9 .NET package convergence table → delete (a migration note for one port).
- The revision note and status header → delete; the declared-graph rule is simply the rule now.
- The interception rule in §1 → the reserved-topic section (§2.5), so it survives §1's removal.

### 4.8 versioning.md

616 lines, of which the **wire concept** is about 60, and that becomes a Core document at the same
path (§2.2): the version is
metadata, not payload; one header, `benzene-version`; absence selects the default handler; dispatch
per §3 row 1; one header drives both dispatch and casting; the HTTP `/v{n}` route segment is sugar
that writes the header (the route-parameter name is the port's business, which closes §7's collision
question). Everything else is one of:

- **Invisible on the wire.** Mechanism B (transparent payload casting, §4) — a caller cannot tell
  whether the service cast the payload or registered two handlers. §4.1–§4.4 (BFS chain composition,
  decorator wiring, "required redesign — done") are .NET internals → .NET docs.
- **The service-version identity layer** (§5.1–§5.3, §5.5–§5.6) → the rationale of
  `service-versions.md`, trimmed.
- **A proposal.** Version-aware routing (§5.4), the sharp edges (§5.7), §6 and the open questions
  (§7) → `work/`.

### 4.9 contract-document.md

Becomes the self-description (§2.1). What survives as is: §2 and §3 (entry shapes), §4
(components), §5.2 (include-list, fail-loud), §5.3 (closure walk), §5.4 (transport-agnostic
generated code), §6.2/§6.4 (hash and projection comparability). What goes regardless: the status
header, §6.1 (history). Candidates for the §2.1 spike to decide, with their consumers named:

- `openapi: "3.0.1"` — a document that claims to be OpenAPI and has no `paths` fails every OpenAPI
  validator; a Benzene discriminator would be honest.
- `reserved` and §5.1 detection, and §6.2's reserved-entry hashing rule — all of it disappears if
  reserved topics are never in the document. benzene-ui reads `requests[].reserved` today.
- `example` — generated decoration the hash already strips; the Spec UI displays it.
- `info.version` — empty in the example; `serviceVersion` is the field.

### 4.10 porting-guide.md and port-quality-standards.md

Both are guidance, not specification. The porting guide's §1 table and §4 (in-process transport)
are useful and should live in `docs/guides/`; §5 is history. Port-quality-standards describes a
DX-champion-agent workflow and GitHub Actions tiers — a repository process for this project's
contributors. Neither binds a port in the way the Core documents do.

### 4.11 conformance/

Eight conformance claims for a 0.1 spec is granularity creep (Core; HTTP binding; Profile; collector;
issue-feed collector; service-version collector; version-ordering port; client generator). After
§2.2: **Core**, **per binding** (`transport-metadata-cases` + `httpRules` for HTTP), **Profile**, and
**per module**. The canonical-handler scheme is good. Two loose ends: the README's `_benzeneHeaders`
paragraph citing a `work/` plan goes once the rename lands, and .NET implements service-version
identity but no test runs `mesh-service-version-cases.json`.

### 4.12 Housekeeping before a tag

- `_benzeneHeaders` → `benzene-headers` (`work/benzene-headers-plan.md`): do the rename; do **not**
  take the design's second half (an optional packed-headers mode on every transport) — more modes
  on the wire is the opposite of consistency, and EventBridge is the only transport that needs it.
- The HTTP defaults of §3 row 5.

### 4.13 Downstream consumers of the spec

The spec has more consumers than the four ports, and a change that touches what they read has to be
sequenced with them:

| Consumer | Reads |
|---|---|
| `benzene-ui` | vendors the fixtures under `contracts/` with its own `SPEC_VERSION`; reads envelope `statusCode`, descriptor `produces`, contract `requests[].reserved` |
| The canonical mesh UI (`mesh-ui/` here) | envelope `statusCode` (9 uses), TraceEvent `topicVersion` (2), calls `benzene:mesh:dispatch`; .NET embeds a byte-identical copy under a CI drift-check, Python vendors one |
| Website demos and the blog | envelope examples (6 files use `statusCode`) |
| `benzene-patterns` | runnable patterns against the ports |
| The Count | 11 .NET packages pinned at `0.0.3-alpha.4`, including `Benzene.Mcp` — a production application, and the only installed base |

Because .NET's embedded mesh UI is drift-checked against the benzene-ui build, any change the UI must
follow lands in benzene-ui first.

---

## 5. Proposed target shape

```
docs/specification/
  README.md                 the one index; tiers + stability labels; conformance language   ~70
  VERSION
  core-concepts.md          model, inbound + outbound, the normative principles,
                            lifecycle and scope in a few sentences                          ~200
  wire-contracts.md         envelope, problem document, headers, reserved topics,
                            status vocabulary + mappings, health, JSON                      ~240
  transport-bindings.md     the contract (incl. settlement, envelope-over-HTTP, envelope
                            security), the binding table, multi-vendor rules                ~140
  versioning.md             the version header, absence, dispatch (Core)                     ~80
  cloud-service-profile.md  R1–R5, R7                                                        ~90
  contract-document.md      → the self-description (module)                                 ~200
  mesh.md                   feeds, collector, declared graph, issues, degradation, R6/R8    ~300
  service-versions.md       identity + order (Provisional)                                  ~90
  claim-check.md            (Provisional)                                                    ~40
  mcp.md                    a binding (Provisional)                                          ~60
  conformance/              README ~150, claims re-cut by tier
docs/guides/                design-principles (strategy), porting-guide, port-quality-standards
work/                       version-aware routing (Mechanism C), versioning's open questions
```

File paths stay flat — sibling repositories and the website link into `docs/specification/<file>.md`
by path, and the tiering is carried by the index and each document's status line. Roughly 1,400
normative lines in place of 3,470 total, with nothing a port needs removed — only things a port does
not need, and the duplicates.

## 6. What this review would not change

The envelope (`topic`/`headers`/`body`, body pre-serialized as a string, `statusCode` as named);
statuses as strings plus `isSuccessful`; RFC 9457 for failures, including the registry's bytes; the
binding obligations; the no-op mount rule; the profile as a named MUST-set; the declared-graph mesh
decision and its degradation rules; the `/benzene/` prefix; `benzene:healthcheck`; "reserved names
are defaults, both directions"; the canonical-handler conformance scheme; the one design rule. These
are the spec.

## 7. Suggested order of work

1. Settlement and envelope-over-HTTP into transport-bindings §1, and the contradictions of §3 —
   behaviour users feel, in one wire window with the reserved-topic section and the
   `benzene-headers` rename.
2. The tiering, guidance move and narrative removal (§2.2–§2.4) — no contract change, so it runs while
   the ports catch up on step 1.
3. A short design spike for §2.1, then the narrowed self-description — the one change that touches
   every port and every collector, opened only when every port is green on step 1.
4. The profile re-cut (§4.6), once the claim has somewhere to live.

## 8. The second-round review, 2026-10-11

The first version of this review and its plan were reviewed by the **architecture reviewer** (is the
ports-and-adapters model kept sound, is the structure cleaner) and the **core product owner** (value
against four-port cost, users, release sequencing). Both returned *needs changes*; both endorsed the
diagnosis and the spec-first sequencing. Every factual claim either made that this revision relies on
was checked against benzene-dotnet's source. What changed:

| Topic | First version | Now | From |
|---|---|---|---|
| Version selection (§3 row 1) | exact only | exact, else cast-reachable, else `not-found`; declared default for absence; with the header-list drop | both |
| `isSuccessful` (§3 row 2) | MUST match the table | no exceptions — the health topics return `ok` and the HTTP binding maps `isHealthy: false` to 503 | architecture |
| Envelope over HTTP (§4.5) | status line carries the Benzene status | 200 for every processed envelope | architecture |
| Settlement (§4.5) | one paragraph | override, unmatched topics, no-result, unit of settlement, idempotency, redrive | both |
| Envelope endpoint security (§4.5) | — | same pipeline; topic restriction | product owner |
| Self-description (§2.1) | full merge, JSON Schema 2020-12, one hash | narrow merge after a spike; `events`/`sends`; two projections of one algorithm; naming rule; format selector kept; dialect after 1.0 | both |
| Reserved topics (§2.5) | seven unlisted; `benzene:health` rename | twelve unlisted; table of interoperable topics only; defaults are HTTP-binding `httpMappings`; no rename | both |
| Field renames (§4.4) | `statusCode`→`status`, `topicVersion`→`version` | not renamed | product owner (architecture agreed with the renames; the four-port and UI cost decided it) |
| Problem-type registry (§4.4) | dropped from the wire | bytes kept; §3.1 shrinks to one sentence | product owner (architecture agreed with dropping; the shipped August decision and RFC 9457 clients decided it) |
| `errors[].field` (§4.4) | JSON Pointer against the serialized request | JSON Pointer with a defined target, omission rule, gRPC conversion | both |
| Profile (§4.6) | two claims, Cloud Service + Meshed | one profile, R1–R5 and R7; R6 and R8 are the mesh module's claim; after the merge | both, combined |
| Tiers (§2.2) | versioning a module; one-implementation modules unqualified | versioning's wire part in Core; "optional capabilities" named; Provisional label; stability labels | both |
| design-principles (§4.2) | to guidance, one rule kept | the degradation and shorthand rules stay normative, in core-concepts | architecture |
| core-concepts §7/§8 (§4.3) | two sentences each | plus validated-before-first-invocation, construction per scope, in-process invocation | both |
| Downstream consumers (§4.13) | — | benzene-ui, mesh UI, website, patterns, The Count | product owner |
| Corrections | — | .NET's `CloudServicePaths` was already `/benzene/invoke`; `mesh:report`/`annotations:add` are in `MeshAggregatorTopics`; header table has five non-Core rows; the first version's §2.4 still called mesh §2.4–§2.5 unbuilt; "no installed base" was wrong | both |
