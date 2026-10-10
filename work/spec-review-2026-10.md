# Specification review — October 2026

**Status:** REVIEW — findings and recommendations, not decisions. Written before any 1.0 tag, while
every contract is still free to change.
**Scope:** every document under `docs/specification/`, with the weight on the higher levels (README,
design principles, core concepts, wire contracts, transport bindings) and the transport spec. The
mesh, versioning and contract-document documents are reviewed for what they cost the higher levels.
**Criterion:** as simple as the spec can be while still offering the features and the consistency.
**Method:** every claim below was checked against the spec text, the conformance fixtures, the
cross-port capability matrix (`docs/capabilities.md`) and, where a claim is about the reference
implementation, benzene-dotnet's source.

---

## 1. The verdict

The spec's *ideas* are sound and worth keeping: a message is `{topic, headers, body}`; a result is a
string status plus `isSuccessful`; a failure body is an RFC 9457 problem document; a binding is seven
obligations; the profile is a named MUST-set over an open core; the mesh graph is declared, not
observed; "is this a concept or an idiom" is the one design rule. None of that needs to change.

What needs to change is the **amount of specification wrapped around those ideas**, and a handful of
places where the spec disagrees with itself or with the reference port. By the spec's own rule of
thumb (porting-guide §1: *a concept is something whose removal changes what is on the wire or what a
handler observes*), roughly 60% of the 3,470 lines is not concept: it is implementation narrative,
revision history, .NET package mapping, process guidance, and proposals for mechanisms no port has
built. That volume has two costs. A porting author cannot find the ~1,200 lines that bind them.
And the duplicated parts have already drifted (two self-description documents, two hash
canonicalizations, four spellings of "status", a reserved-topic list that exists in the reference
port but not in the spec).

The highest-leverage changes, in order:

1. **One self-description document.** The ServiceDescriptor (`benzene:mesh`) and the Contract
   Document (`/benzene/spec`) are two projections of the same handler registry with different field
   names, different schema dialects and different hash algorithms. Merge them (§2.1).
2. **Three tiers, by what binds whom.** Core (must), Modules (each optional, each with one
   conformance claim), Guidance (informative). Move proposals out of the spec until two ports
   implement them (§2.2).
3. **Finish the transport contract.** Settlement of a failure result on one-way bindings — the
   safety-relevant rule — is not in the spec; nor is the HTTP status of the envelope endpoint (§4.5).
4. **One reserved-topic registry.** Every `benzene:*` topic, its response shape and its default
   HTTP path, in one table. The reference port serves seven reserved topics the spec never defines
   (§2.5).
5. **Settle the contradictions** listed in §3 — version selection above all, since Go and .NET
   already ship different defaults because of it.

---

## 2. Structural findings (the higher levels)

### 2.1 Two self-descriptions of one service

A service derives, from one registry, two documents that describe the same thing:

| | ServiceDescriptor (mesh.md §2) | Contract Document (contract-document.md) |
|---|---|---|
| Reached via | reserved topic `benzene:mesh` | HTTP `/benzene/spec` (and a `benzene:spec` topic the spec never defines) |
| Consumed topics | `topics[] { id, version, requestSchema, responseSchema }` | `requests[] { topic, version, reserved, httpMappings, request, response, example }` |
| Produced topics | `produces[]` (same shape as `topics`) | `events[] { topic, version, message, example }` |
| Transports | `binding` — one string | `transports[]` — an array |
| Schema dialect | "a subset of JSON Schema 2020-12", inline, no `$ref`, recursion cut to `{}` | OpenAPI 3.0 Schema Objects, `$ref` into `components.schemas`, a normative closure walk |
| Hash | `descriptorHash`: documented member order, "per-port by design" | `contractHash`: RFC 8785 (JCS), compared across ports |
| Pinned by | `mesh-descriptor-cases.json` | `contract-document-cases.json`, `contract-hash-cases.json` |

Each has a reason it looks the way it does (the descriptor came from Go, the contract document from
.NET's OpenAPI tooling). Neither reason is a reason to keep both. The costs: a port implements two
schema derivations and two canonicalizers; a reader learns two vocabularies for one idea; and the
two have already diverged in meaning (`binding` is singular, `transports` is plural; `topicVersion`
on a TraceEvent, `version` everywhere else).

**Recommendation.** One document: the ServiceDescriptor *is* identity + placement + the contract.

- `service`, `serviceVersion`, `instanceId`, `runtime`, `placement`, `transports[]` (plural; drop
  `binding`).
- `requests[]` and `events[]` as the contract document defines them, minus `reserved` and minus
  `example` (see §4.9), with `httpMappings` kept.
- `components.schemas` in **JSON Schema 2020-12** — which is also what an OpenAPI 3.1 Schema Object
  is, so the OpenAPI heritage is preserved without the `openapi: "3.0.1"` marker that claims to be
  OpenAPI while having no `paths`. mesh.md §2.1's type-mapping table is the derivation rule, stated
  once. Keep `$ref` + the closure walk (codegen needs named types; the descriptor's inline-and-cut
  rule loses them).
- **One hash**, JCS + SHA-256 over the contract part (`requests`, `events`, `components`), with
  `instanceId`, `degraded`, `profile` and the hash itself excluded. mesh.md §2.2's "documented order,
  per-port by design" is a second canonicalizer for no benefit: JCS libraries exist in every port's
  ecosystem (contract-document.md §6.2 lists them). contract-document.md §6.3, which exists to
  justify the divergence, disappears with it.
- Served two ways: the reserved topic `benzene:spec` over the envelope and `GET /benzene/spec` over
  HTTP — the same bytes. `benzene:mesh` then either becomes an alias or is dropped.

Cost: every port changes its descriptor and its contract document once, and `mesh-descriptor-cases`
and `contract-document-cases` merge into one fixture. Pre-release is the only time this is cheap.

### 2.2 "Core" is not the core

README §"Two levels": *Core = everything in this directory except the profile.* That makes mesh
(594 lines), versioning (616), contract-document (331), port-quality-standards (168) and
porting-guide (132) part of "the Benzene Core Specification". A porting author told to implement
Core is told to read 3,470 lines to find the ~600 that bind them. The conformance README's claims
table already knows better — it has eight distinct claims — but the document structure doesn't
follow it.

**Recommendation.** Three tiers, and the folder says which is which:

| Tier | Binds | Documents |
|---|---|---|
| **Core** | every port, every service | `core-concepts.md`, `wire-contracts.md`, `transport-bindings.md` (§1 + §3) |
| **Modules** | a port that claims the module | `self-description.md` (§2.1), `mesh.md` (feeds + collector), `versioning.md` (the wire part only), `claim-check.md`, `mcp.md` |
| **Profile** | a service that claims it | `cloud-service-profile.md` |
| **Guidance** *(informative)* | nobody | `design-principles.md` (most of it), `porting-guide.md`, `port-quality-standards.md` → move to `docs/`, not `docs/specification/` |

And a fourth bucket that is not the spec at all: **proposals** — versioning §5 (Mechanism C, "no
port implements"), mesh §2.4–§2.5 (service-version identity and order, "no collector honours it"),
mesh §4.1 (issue feed, "Go parity pending"), versioning §7 (open questions). These belong in `work/`
until two ports implement them. The spec's rule that concepts land "before or alongside" code is
right; pinning *conformance fixtures* for mechanisms nobody has built
(`mesh-service-version-cases.json`, `mesh-version-order-cases.json`) spends complexity budget on
speculation and makes the fixture set look twice as large as the contract.

### 2.3 Five layering vocabularies for one idea

The spec explains "how much of Benzene you are using" five ways: Core/Profile (README), adoption
rungs 1–5 (design-principles §2), header tiers A–D (wire-contracts §2), requirement ids R1–R8
(profile), and eight conformance claims (conformance README). They overlap without mapping cleanly:
profile §6 maps R→rungs; header **tier B is defined and contains no header** (A: `topic`,
`content-type`; C: `benzene-version`, `traceparent`, `x-correlation-id`, `benzene-claim-check`;
D: `_benzeneHeaders`). An empty tier is the sign the scheme was designed ahead of its use.

**Recommendation.** One vocabulary — Core / Module *name* / Profile — used everywhere a level is
named. The header table's tier column becomes "Core", "Module: versioning", "Binding: EventBridge".
The adoption ladder stays as an informative narrative in guidance, not as a fifth level system.

### 2.4 History and implementation narrative inside normative text

Pre-1.0, git holds the history; the spec should hold the rule. Lines that are history, status, .NET
package narrative, or references into `work/` and port internals:

| Document | Lines (approx.) | What |
|---|---|---|
| wire-contracts.md | 35 | §1.1 `message`/`body` note; §1.3 "this document previously described two rules…"; §2 "`benzene-status` … appeared in this table historically"; §3.1's three-way `code`/`type`/`classification` comparison |
| mesh.md | 180 | status header; revision note; §2.4–§2.5 (unbuilt); §4.1 (one port); §9 (.NET package convergence table) |
| versioning.md | 480 | status header; §2.3 (.NET DI wiring); §3/§3.1 (C# samples); §4.1–§4.4 (decorator internals, BFS chain composition, "required redesign — done"); §5 (proposal); §7 (open questions) |
| contract-document.md | 40 | status header; §6.1 "why a new algorithm"; references to `cross-language-clients-plan.md` and `spec-mesh-tooling-implementation-plan.md` |
| cloud-service-profile.md | 40 | §5's description of `Benzene.CloudService`, `Benzene.CloudService.Probe`, the CLI command and the tri-state verdict |
| porting-guide.md | 15 | §5 ".NET-isms that must not leak" (one entry already struck through) |
| conformance/README.md | 10 | the `_benzeneHeaders` paragraph that cites `work/benzene-headers-plan.md` |
| design-principles.md | 10 | §5.3 migration note (and it is wrong — see §4.2) |

About 800 lines, none of it binding. A spec that cites a `work/` plan or a C# class name is not
language-neutral, and those citations rot first.

### 2.5 The reserved-topic registry that does not exist

The spec defines `benzene:healthcheck` (wire §5), `benzene:mesh` (mesh §1) and the four collector
topics (mesh §4), mentions `benzene:mesh:query:*` as deliberately unpinned, and names
`benzene:spec` (contract-document §5.1, profile R5) **without ever defining it**. The reference
port's `BenzeneTopic` registry carries, in addition: `benzene:spec`, `benzene:test-payloads`,
`benzene:liveness`, `benzene:readiness`, `benzene:ping`, `benzene:mesh:report`,
`benzene:mesh:annotations:add`. Seven reserved topics a Go or Python service will never answer, and
a reader of the spec cannot know exist.

Separately, design-principles §5.2 defines the well-known **HTTP paths** (`/benzene/health`,
`/benzene/spec`, `/benzene/invoke`, three UIs) as a second registry, with "reserved topics take no
prefix — the prefix is an HTTP-surface concern" as the only link between them.

**Recommendation.** One table in wire-contracts, and nothing else defines a `benzene:*` topic:

| Topic | Request | Response | Default HTTP path |
|---|---|---|---|
| `benzene:health` | none | health response (wire §5) | `GET /benzene/health` |
| `benzene:spec` | none | the self-description document (§2.1) | `GET /benzene/spec` |
| `benzene:mesh:register` / `heartbeat` / `traces` | mesh §4 | `{accepted}` | — (collector-side) |

Every well-known HTTP surface is then *the HTTP binding of a reserved topic*, and `/benzene/invoke`
is the one path that is not a topic (it is the envelope carrier). Rename `benzene:healthcheck` →
`benzene:health` so the topic and its path agree. Decide `liveness`/`readiness`/`ping`/
`test-payloads` in or out — in, with a one-line shape each, or out of the reference port's reserved
namespace. `benzene:mesh` either aliases `benzene:spec` or goes.

### 2.6 The outbound half has no home

core-concepts is entirely inbound. The outbound model is scattered: the sender interface in
transport-bindings §2 (last subsection); the outbound routing table only by implication in
versioning §5.4 and porting-guide §4; outbound *registration* (declared `produces`) in mesh §2.3;
the generated client's dependency on the sender in contract-document §5.4; decorators in
transport-bindings §2. A reader cannot find out what "sending a message in Benzene" means without
reading four documents.

**Recommendation.** A `core-concepts` §"Outbound" with three concepts: the **sender**
(`send(topic, headers, body, version?) → result`), the **routing table** (`topic → destination`,
deployment configuration, not contract), and **declared produces** (the registry's outbound twin,
which the self-description projects). mesh §2.3 then cites it instead of defining it.

---

## 3. Contradictions to settle

| # | Where | The two readings | Recommendation |
|---|---|---|---|
| 1 | core-concepts §2 vs versioning §3 | "selected **only** by an exact version match" vs default selector "exact match, **else highest available**". Go ships exact-only citing this; .NET ships exact-else-highest. (`docs/capabilities.md` note 8.) | **Exact.** Absent → the unversioned handler; present and unmatched → `not-found` naming the version. "Else highest" silently serves v3 behaviour to a v1 caller — the failure versioning exists to prevent — and contradicts versioning §2.2's own "absent means the *oldest* still accepted". Offer highest-available as a replaceable selector, not the default. |
| 2 | core-concepts §5 vs wire §1.2 | `isSuccessful` "derived from status class" vs "the authoritative signal… a receiver MUST prefer this field". | The producer sets it; for a vocabulary status it MUST equal the table; the receiver trusts it. Say it once, in wire §1.2, and have core-concepts cite it. |
| 3 | core-concepts §10 vs wire §2 vs profile R8 vs mesh §3 | trace context "MUST behave identically on every transport" / tier C "Benzene… does not require them" / MUST (R8) / SHOULD forward (mesh). | Core: *if* propagated, verbatim W3C (the interop rule). Profile: MUST join and forward. Delete the other two statements. |
| 4 | transport-bindings §2 (informative) | Contains MUSTs: EventBridge "needs no bolted-on `topic`", MCP "MUST NOT auto-expose", "a start-up check MUST fail". | Normative rules move to §1 or to the module; §2 becomes the table of §4.5. |
| 5 | design-principles §5.3 vs benzene-dotnet | "`/benzene/invoke`… already follow[s] it" — the reference default is `/benzene-message`; `/mesh-ui`, `/spec-ui` unchanged. | Change the three defaults now and delete §5.3. There is no installed base to migrate. |
| 6 | mesh §4 vs `mesh-collector-cases.json` | `benzene:mesh:query:*` "deliberately not part of this contract" — but every collector case asserts through them. | Either pin a minimal query contract (fleet, service, topic — the three the fixture uses) or assert the derivation rules another way. CI-pinning a shape while calling it non-contract is the worst of both. |
| 7 | wire §2 naming rule vs transport-bindings MCP | Invented names carry `benzene-`; MCP introduces `mcp-tool`, `mcp-session` headers, absent from the header table. | `benzene-mcp-tool` / `benzene-mcp-session`, listed under the module. |

---

## 4. Document by document

### 4.1 README.md and index.md

Two tables of contents for the same twelve files, with different one-line descriptions (index:
"Payload Schema Versioning — versioning a topic's request/response shape"; README: "three axes…
plus one specified-but-unbuilt mechanism"). Merge into one. README §"Versioning" says the spec is
versioned "as one unit" but no version exists beyond "DRAFT v0.1" stamped on each file, while the
ports' `SPEC_VERSION` is a commit hash — pick one: a `VERSION` file or a tag in this repo. Keep
§"The one design rule" exactly as it is; it is the best paragraph in the spec and the test this
review applied.

### 4.2 design-principles.md

The status header says its defaults *and* its override points are normative; the body is mostly
strategy. Three kinds of content are interleaved:

- **Informative strategy** (§1, §2, §4.1 the shorthand ladder, the "examples are where this is
  proved" rule) — good writing, belongs in guidance.
- **A normative rule stated twice**: §4 "every convention, both sides" and wire-contracts §2
  "reserved names are defaults… an override applies to both directions" are the same rule. Keep
  the wire-contracts statement (it is where the names are) and cite it.
- **The §4 extension-point table** describes overrides as "replace the topic-getter registration"
  — .NET DI vocabulary in a cross-language document. The concept is: each of these is a replaceable
  component with a default. The table can say that in one column.
- **§5 well-known surfaces** → the reserved-topic table (§2.5). §5.3 is wrong (§3 row 5).

### 4.3 core-concepts.md

The best-shaped document. Keep §1–§6, §9, §10. Changes:

- **§3**: say how a handler reaches headers and invocation facts (via an accessor resolved from the
  scope — currently only implied by §4's "ride on the context… or an accessor"). It is the one
  thing a handler author needs that the model doesn't state.
- **§5**: `isSuccessful` per §3 row 2. The `errors` paragraph's cross-reference to the mesh
  fingerprint is noise here.
- **§7 Application lifecycle**: `getConfiguration / configureServices / configure` is
  `BenzeneStartUp` promoted to a concept. Apply the rule of thumb: nothing on the wire or observed
  by a handler changes if a port has no configuration phase — and two ports already don't
  (`work/ceremony-parity-audit.md` §1: Python has none, Go's is app-defined). The *concept* is one
  sentence: one application definition can mount several transports, and a mount the host cannot
  honour is a silent no-op. Keep that; drop the phases.
- **§8 Dependency registration**: `addSingleton/addScoped/addTransient/tryAdd*/getService/
  tryGetService` is MS DI. The concept is two sentences the section already contains: a scope per
  invocation, and framework defaults the application can replace. Keep those; drop the operations.
- **Add §"Outbound"** (§2.6).

### 4.4 wire-contracts.md

The genuine cross-language interop surface — envelope, problem document, `topic` metadata key,
status vocabulary + `isSuccessful`, HTTP/gRPC mappings, health response, JSON defaults — is about
150 lines. The document is 388. Where the rest goes:

- **§1.3 problem document.** Adopting RFC 9457 is right. The **problem-type registry (§3.1)** is
  not earning its keep: eleven URIs under `https://benzene.app/problems/` that are "opaque, never
  dereferenced, not live pages", each encoding exactly the information `benzeneStatus` already
  carries one line below. Every port must ship the table; no reader gains anything from it.
  Recommend: framework failures omit `type` (RFC 9457: absent = `about:blank`), `benzeneStatus` is
  the discriminator, applications may set `type`. The registry, the `registry` fixture group, and
  the three-way `code`/`type`/`classification` paragraph all go. The HTTP `status` member rule
  stays (it is the one place the RFC and Benzene genuinely meet).
- **`errors[].field`** is specified as "JSON Pointer for schema-based validators, the host
  language's property path for others — document which per integration". That is a per-port,
  per-validator convention baked into the wire: a TypeScript client reading `"Name"` from .NET and
  `"/name"` from Python. Pick one. **JSON Pointer (RFC 6901)** against the serialized request is
  the standard, unambiguous for arrays, and what schema validators already emit; the non-schema
  adapters build it from the serialized property names.
- **§2 header table.** Replace tiers with Core / Module / Binding (§2.3). Four of seven rows are
  modules or bindings; Core is `topic` and `content-type`. The "Naming" paragraph and "Reserved
  names are defaults" are right and should stay — but "MUST expose them as a single injectable
  value" is an implementation shape; the concept is *configurable, in one place, both directions*.
- **§2.1 claim check** — 25 lines of normative security rules for a tier-C add-on one port ships.
  A module document.
- **`benzene-version` fallback list** (`benzene-version` → `version` → `x-version`): the document
  itself describes how it goes wrong (a foreign `version` header silently read as a schema
  version). One header, `benzene-version`, configurable like every other reserved name. The list
  is three names where one will do.
- **§3 vocabulary.** Seventeen statuses. `updated` and `deleted` both map to 204 and the reverse
  mapping can only return `deleted`; gRPC collapses all six successes to `OK`. They are REST
  niceties, lossy on both protocols. Candidate for removal (four success statuses: `ok`,
  `created`, `accepted`, `ignored`). A judgement call — flagged, not insisted on.
- **§4.2 gRPC**: `conflict → AlreadyExists` is specifically "entity already exists" in gRPC; the
  canonical code for a concurrency conflict is `Aborted` (or `FailedPrecondition`). Minor.
- **§5 health**: `healthChecks` key dedup with `-2`/`-3` suffixes is an implementation detail; "keys
  are unique" is the rule.
- **§6 serialization**: "Reading: property-name matching is case-insensitive" is a System.Text.Json
  default. JavaScript's `JSON.parse` and Python's `json` are exact-match; mandating
  case-insensitivity costs every non-.NET port a custom deserializer for no interop gain (writers
  already MUST emit camelCase). Make it: writers MUST write camelCase; readers MUST accept
  camelCase; case-insensitive read is MAY.
- **Four spellings of one value**: result `status`, envelope `statusCode`, problem `benzeneStatus`,
  TraceEvent/issue `status`; and `topicVersion` (TraceEvent) vs `version` (everywhere else). The
  envelope's `statusCode` carries a string status, not a code. Recommend `status` everywhere except
  the problem document, where RFC 9457 owns `status` and `benzeneStatus` is forced; `version`
  everywhere. Pre-release this is a find-and-replace; after, it is forever.

### 4.5 transport-bindings.md

**§1 (the binding contract)** is the right shape — seven obligations — and is missing the two that
matter most to a user:

1. **Settlement.** Item 5 says "queue/event transports define their acknowledge/reject behaviour
   instead", and nothing in the spec defines it. This is the safety-relevant observable: whether a
   `validation-error` result on an SQS message is retried or silently dropped. The reference port
   has a maintainer-approved rule (benzene-dotnet `docs/message-result.md`, "1.0 settlement
   contract"): *on a queue-shaped transport, a failure result or a missing result is not settled —
   it is redelivered, exactly like an uncaught exception; the self-hosted stream workers (Kafka,
   Event Hub) are the deliberate at-most-once exception.* TypeScript's matrix says plainly it has
   not caught up (three adapters default the knob off, five have no knob); Go and Python don't
   state theirs. One paragraph in §1 fixes the whole estate's divergence: **a one-way binding MUST
   NOT acknowledge a message whose invocation ended in a failure-class result, no result, or an
   uncaught error; `ignored` is acknowledged; a stream-shaped binding where retention blocks the
   partition MAY default to skip-and-continue and MUST document it.**
2. **The envelope over HTTP.** R4 makes `/benzene/invoke` the surface every tool uses, and the
   spec never says what HTTP status accompanies the envelope. The reference port maps the Benzene
   status onto the HTTP status line (so a `not-found` envelope arrives as HTTP 404) and leaves the
   inner problem document's `status` member absent. Two sentences in §1 (or wire §1).

**§2 (the catalogue)** is informative, uneven (HTTP: 4 lines; MCP: 27; Pub/Sub, Service Bus, Event
Hub, S3, Kinesis, Queue Storage, Event Grid, Blob: absent — though all four ports ship them and the
`transport-metadata-cases.json` `topicSources` table already lists eleven bindings with their topic
source and metadata channel). The fixture is the real registry; the prose is a partial copy. Replace
§2 with one table — binding · topic source · metadata channel · reply channel · unit of work ·
cancellation source — identical to `topicSources`, and put per-binding detail in each port's docs.
HTTP's row should finally say how it resolves a topic: an explicit route table (`httpMappings`) plus
the envelope endpoint; there is no implicit `POST /{topic}`.

**MCP** is a module (its own document, like mesh), not a catalogue entry: it has its own normative
rules, its own reserved headers (§3 row 7), and exists in one port as of two weeks ago.

**Outbound clients** → core-concepts (§2.6). **§3 multi-vendor rules** — keep, verbatim.

### 4.6 cloud-service-profile.md

The profile is the right idea and R1–R8 are the right eight. Three things:

- **R6 is the heaviest requirement and depends on the heaviest module.** A service cannot claim
  "Cloud Service" without pushing four feeds to a collector. Consider two claims: *Cloud Service*
  (R1–R5, R7, R8: pull-able by any tool, no collector needed) and *Meshed* (+R6). Fewer MUSTs for the
  common case; the mesh stays a module. Profile §6 already draws this line (rung 5 = R6).
- **§5** spends 40 lines describing `Benzene.CloudService`, `Benzene.CloudService.Probe`, a CLI
  command and a tri-state verdict. The spec-level content is one sentence: a port MAY offer a
  wiring-time self-check, carried on the descriptor's `profile` field, and an external probe.
- **§1.1, §6, §7** are informative and fine, but with §2.2's tiering the "bargain" and the ladder
  mapping are said three times across three documents.

### 4.7 mesh.md

594 lines; ~250 is the contract a profiled service and a collector need: §1 (reserved topic), §2
minus §2.4–§2.5 (descriptor, which becomes §2.1's document), §3 (TraceEvent), §4 minus §4.1
(collector topics and the declared-graph rule), §5 (heartbeat), §6 (degradation). The rest:

- §2.4–§2.5 service-version identity and order — "no port implements"; two fixtures. → `work/`.
- §4.1 issue feed — one port, "Go parity pending", 80 lines, its own fingerprint algorithm and
  classification table. → a module or `work/` until a second port.
- §4.2 declared vs observed — good, short, keep.
- §9 .NET package convergence table → delete (it is a migration note for one port).
- The revision note and status header → delete; the declared-graph rule is simply the rule now.
- Two canonicalizers → one (§2.1).

### 4.8 versioning.md

616 lines, of which the **wire concept** is about 60: the version is metadata, not payload;
`benzene-version` header; the HTTP `/v{n}` route convention as that header's HTTP form; absent =
unversioned; dispatch is exact (§3 row 1). Everything else is one of:

- **Invisible on the wire.** Mechanism B (transparent payload casting, §4) — a caller cannot tell
  whether the service cast the payload or registered two handlers. By the rule of thumb it is an
  idiom: port documentation, cookbook, not spec. §4.1–§4.4 (BFS chain composition, decorator
  wiring, "required redesign — done") are .NET internals.
- **A proposal.** Mechanism C (§5, 200 lines) and the open questions (§7). → `work/`.
- **A convenience that costs consistency.** The fallback list (§4.4 above) and the HTTP route
  parameter named `version`, whose collision risk §7 itself records. One header; the route segment
  is sugar that writes the header.

### 4.9 contract-document.md

Folds into the self-description document (§2.1). What survives: §2 and §3 (entry shapes), §4
(components), §5.2 (include-list, fail-loud), §5.3 (closure walk), §5.4 (transport-agnostic
generated code), §6.2/§6.4 (hash and projection comparability). What goes:

- `openapi: "3.0.1"` — a document that claims to be OpenAPI and has no `paths` fails every OpenAPI
  validator. Give it its own discriminator.
- `reserved` and §5.1 detection, and §6.2's reserved-entry hashing rule (strip the flag but detect
  before stripping, drop the entry for whole-service but keep it for topic-scoped if asked) — all
  of it disappears if **reserved topics are never in the document**. They are framework surfaces
  the spec documents; a service's contract is its domain topics.
- `example` — generated decoration the hash strips; a UI can generate one from the schema.
- `info.version` — empty in the example, meaning undefined; `serviceVersion` is the field.
- §6.1 — history.

### 4.10 porting-guide.md and port-quality-standards.md

Both are guidance, not specification. The porting guide's §1 table and §4 (in-process transport)
are useful and should live in `docs/`; §5 is history. Port-quality-standards describes a
DX-champion-agent workflow and GitHub Actions tiers — a repository process for this project's
contributors. Neither binds a port in the way the Core documents do, and putting them under
"Core Specification" is what inflates Core to 3,470 lines.

### 4.11 conformance/

Eight conformance claims for a 0.1 spec is granularity creep (Core; HTTP binding; Profile; collector;
issue-feed collector; service-version collector; version-ordering port; client generator). After
§2.2: **Core**, **per binding** (`transport-metadata-cases` + `httpRules` for HTTP), **Profile**, and
**per module** (one fixture file each). The canonical-handler scheme is good. The README's
`_benzeneHeaders` paragraph citing a `work/` plan goes once the rename lands.

### 4.12 Housekeeping that must happen before a tag

- `_benzeneHeaders` → `benzene-headers` (`work/benzene-headers-plan.md`): do the rename; do **not**
  take the design's second half (an optional packed-headers mode on every transport) — more modes
  on the wire is the opposite of consistency, and EventBridge is the only transport that needs it.
- The three HTTP defaults (§3 row 5).
- `benzene:healthcheck` → `benzene:health` (§2.5).
- The status/version field names (§4.4 last bullet).

---

## 5. Proposed target shape

```
docs/specification/
  README.md                 the one index; tiers; conformance language; spec version     ~60
  core-concepts.md          model, inbound + outbound, lifecycle in one rule              ~180
  wire-contracts.md         envelope, problem document, headers, reserved topics,
                            status vocabulary + mappings, health, JSON                   ~220
  transport-bindings.md     the contract (incl. settlement, envelope-over-HTTP),
                            the binding table, multi-vendor rules                        ~120
  cloud-service-profile.md  R1–R8 (or R1–R5/R7/R8 + Meshed)                              ~110
  modules/
    self-description.md     identity + contract + hash + generation rules               ~180
    mesh.md                 feeds, collector topics, declared graph, degradation        ~220
    versioning.md           the header, absence, dispatch                                ~60
    claim-check.md                                                                        ~40
    mcp.md                                                                                ~60
  conformance/              README ~150, fixtures regrouped by tier
docs/guides/                design-principles (strategy), porting-guide, port-quality-standards
work/                       Mechanism C, service-version identity/order, issue feed, open questions
```

Roughly 1,400 normative lines in place of 3,470 total, with nothing a port needs removed — only
things a port does not need, and the duplicates.

## 6. What this review would not change

The envelope (`topic`/`headers`/`body`, body pre-serialized as a string); statuses as strings plus
`isSuccessful`; RFC 9457 for failures (minus the URI registry); the seven binding obligations; the
no-op mount rule; the profile as a named MUST-set; the declared-graph mesh decision and its
degradation rules; the `/benzene/` prefix; "reserved names are defaults, both directions"; the
canonical-handler conformance scheme; the one design rule. These are the spec.

## 7. Suggested order of work

1. Settle §3 (one sitting; they are decisions, not design).
2. Lift settlement and envelope-over-HTTP into transport-bindings §1 (two paragraphs; closes the
   estate's largest behavioural divergence).
3. The reserved-topic table and the four renames (§4.12).
4. The tiering move (§2.2) — mostly `git mv` and deleting narrative; no contract changes.
5. The self-description merge (§2.1) — the one change that touches every port; plan it with each
   port's champion, land it once.
6. Re-cut the conformance claims to match (§4.11).
