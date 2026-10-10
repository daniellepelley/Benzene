# Spec simplification — implementation plan

**Status:** PLAN — ready to execute, phase by phase, each phase one pull request to this repo.
**Written:** 2026-10-10. **Source:** [`spec-review-2026-10.md`](spec-review-2026-10.md) — the review
this plan actions; the reasoning lives there and is not repeated here.
**Goal:** a specification as simple as it can be while still offering the features and the
consistency — before any 1.0 tag, while every contract is still free to change.
**Resolves on the way:** `remaining-issues-plan.md` P1.3 (`mesh-service-version-cases.json`: claim
it — Phase 4), and the deferred `benzene-headers-plan.md` Phase A (the rename — Phase 3).

---

## 0. Rules every phase runs under

1. **Spec first, ports second, snapshot third.** A contract change lands in `docs/specification/**`
   here, then each port catches up, then each port re-vendors `conformance/*.json` and bumps its
   `SPEC_VERSION`. Between the first and third step every port's `conformance-drift-check` is
   **expected** to be red; nobody "fixes" it by editing a vendored snapshot. (The rule
   `benzene-headers-plan.md` §1 states; it applies to every phase here.)
2. **Pre-1.0 means clean breaks.** No dual-accept, no migration shims, no "tolerate the old
   spelling" clauses. There is no installed base (`remaining-issues-plan.md` P0: nothing is
   installable yet). The same ruling as the 2026-07-27 `topic` rename.
3. **A move fixes its inbound references in the same commit** — `docs/`, `work/`,
   `.claude/agents/`, the website generator's hard links (`MarketingPages.cs`), and the port repos'
   docs when a port catches up. The website's broken-link self-check runs in CI against all four
   ports' `main` and is the gate: a phase is not merged while it fails.
4. **File paths stay stable unless a phase says otherwise.** Sibling repos link into
   `docs/specification/<file>.md` by path; the website nav is `index.md`. Tiering is expressed by
   grouping in `index.md` and a status line in each document, not by a sub-folder.
5. **One phase, one PR, one logical change.** A phase lands on `main` here before the next
   starts. Port catch-up for phase *n* may overlap with phase *n+1*'s spec work; it may not lag
   past phase *n+2* — otherwise the drift-check stays red long enough to be ignored.
6. **Every phase ends with the lifecycle** (`docs/documentation-lifecycle.md`): capability
   matrices updated in the ports that changed (`capability-scribe`), `docs/capabilities.md`
   refreshed here (`port-aligner`), and the phase's checklist ticked below.

## 1. Decisions this plan assumes

Each row is the review's recommendation, taken as the default. **To change the plan, change the
row** — every phase below cites the decisions it depends on.

| # | Decision | Default this plan takes | Phase |
|---|---|---|---|
| D1 | Handler-version selection | **Exact match only.** Absent version → the unversioned handler; present and unmatched → `not-found`, with the requested version named in the error. "Highest available" is a replaceable selector a port MAY offer, never the default. | 1 |
| D2 | `isSuccessful` | The producer sets it; for a vocabulary status it MUST equal the table; the receiver trusts it. Stated once, in wire-contracts §1.2. | 1 |
| D3 | Trace context | Core: *if* propagated, verbatim W3C. Profile: MUST join inbound and forward outbound. The other two statements (core-concepts §10 "MUST behave identically", mesh §3 SHOULD) go. | 1 |
| D4 | Well-known HTTP defaults | `/benzene/invoke`, `/benzene/spec-ui`, `/benzene/mesh-ui` — change the defaults; delete design-principles §5.3. | 1 |
| D5 | `benzene:mesh:query:*` | Pin the three read models the collector fixture already asserts through (`fleet`, `service`, `topic`), subset-matched, as part of the mesh module. Two collectors implement them today. | 4 |
| D6 | MCP header names | `benzene-mcp-tool`, `benzene-mcp-session`, listed in the MCP module. | 2 |
| D7 | Problem-type registry | **Dropped.** Framework failures omit `type` (RFC 9457: absent = `about:blank`); `benzeneStatus` is the discriminator; applications MAY set `type`. | 3 |
| D8 | `errors[].field` | JSON Pointer (RFC 6901) against the serialized request, on every transport and from every validator. | 3 |
| D9 | Header tiers A–D | Replaced by Core / Module *name* / Binding *name*. Tier B is empty today. | 3 |
| D10 | Version signal | One header, `benzene-version`, configurable like every reserved name. The `version`/`x-version` fallback list goes. The HTTP `/v{n}` route segment is sugar that *writes* the header; the route-parameter name is the port's choice. | 3 |
| D11 | JSON reads | Writers MUST write camelCase; readers MUST accept camelCase; case-insensitive read is MAY. | 3 |
| D12 | Field spellings | Envelope `statusCode` → `status`; TraceEvent `topicVersion` → `version`; problem document keeps `benzeneStatus` (RFC 9457 owns `status` there). | 3 |
| D13 | Reserved topics | One table in wire-contracts. `benzene:healthcheck` → `benzene:health`; `benzene:spec` defined; `benzene:liveness`/`benzene:readiness` in, optional, same response shape; `benzene:ping` in, optional (the transport-reachability probe is a cross-port concept). `benzene:test-payloads`, `benzene:mesh:report`, `benzene:mesh:annotations:add` are not in the table: the .NET port either proposes a row (spec-first) or moves them out of the `benzene:` namespace. | 3 |
| D14 | `_benzeneHeaders` | → `benzene-headers`, kebab-case, EventBridge only. The design's second half (an optional packed-headers mode on every transport) is **rejected**: more modes on the wire is the opposite of consistency. | 3 |
| D15 | Profile | Two claims: **Cloud Service** (R1–R5, R7, R8 — pull-able by any tool, no collector needed) and **Meshed** (Cloud Service + R6). `profile.name` ∈ {`cloud-service`, `meshed`}. | 5 |
| D16 | Settlement on one-way bindings | A one-way binding MUST NOT acknowledge a message whose invocation ended in a failure-class result, no result, or an uncaught error; `ignored` is acknowledged; a stream-shaped binding whose retention blocks the partition MAY default to skip-and-continue and MUST document it. (The .NET 1.0 settlement contract, promoted.) | 2 |
| D17 | Envelope over HTTP | The HTTP status line carries the Benzene status mapped by wire-contracts §4.1; the response body is the envelope; the inner problem document omits `status` (it is not an HTTP response). | 2 |
| D18 | Self-description | One document: identity + placement + `transports[]` + `requests[]`/`events[]` + `components.schemas` in **JSON Schema 2020-12** with `$ref` and the closure walk; one hash, `contractHash`, JCS + SHA-256 over the contract part. Served as `benzene:spec` over the envelope and `GET /benzene/spec` over HTTP. `benzene:mesh` dropped; `benzene:mesh:register` carries the same document; heartbeat `descriptorHash` → `contractHash`. Reserved topics are never in the document. No `openapi`, `info`, `reserved`, `example`. | 6 |
| D19 | Proposals | Mechanism C's routing (versioning §5.4–§5.7) and the open questions (§7) → `work/`. Service-version identity and order (mesh §2.4–§2.5) and the issue feed (§4.1) stay as modules — implemented by .NET (identity, order, issue collection) and Go (issue emission). | 4 |
| D20 | Spec version | A `docs/specification/VERSION` file, `0.2.0-draft` after this plan lands; README's "Versioning" section points at it. Ports' `SPEC_VERSION` stays a commit hash — it is a snapshot pointer, a different thing. | 7 |

**Deferred — not in this plan, recorded so they are not re-argued:**

| Item | Recommendation | Why deferred |
|---|---|---|
| Drop `updated`/`deleted` from the vocabulary (both → 204; gRPC collapses all successes) | Keep for now | Touches every port's result type, examples and the envelope fixture for a marginal gain; revisit at the 1.0 cut |
| gRPC `conflict` → `Aborted` instead of `AlreadyExists` | Keep for now | Behaviour change in every gRPC binding; `AlreadyExists` is defensible for the common conflict |
| CloudEvents binding (`cloudevents-design.md`) | Out of scope | A new module, not a simplification |
| `CancellationToken` on handlers (`remaining-issues-plan.md`, extracted remainder) | Out of scope | Per-port API, not spec |

## 2. Phases

Sizes are relative (S/M/L/XL) and cover the spec change here plus the port catch-up.

### Phase 1 — Settle the contradictions *(S; doc-only here, two small port changes)*

Decisions: D1, D2, D3, D4.

**Spec (this repo):**

| File | Edit |
|---|---|
| `core-concepts.md` §2 | Keep "selected only by an exact version match"; add the unmatched-version rule (D1). |
| `core-concepts.md` §5 | `isSuccessful`: one sentence citing wire-contracts §1.2 (D2). Delete the mesh-fingerprint cross-reference in the `errors` paragraph. |
| `core-concepts.md` §10 | Trace context: "if an implementation propagates W3C trace context it MUST do so verbatim" — nothing more (D3). |
| `wire-contracts.md` §1.2 | The authoritative-signal paragraph becomes the only statement of D2 (trim the historical "sender that predates this field" clause — rule 2). |
| `wire-contracts.md` §2 | `traceparent`/`tracestate` row: Core-if-used wording per D3. |
| `mesh.md` §3 | "Outbound propagation … SHOULD" → cite the profile's R8 instead of restating a level. |
| `cloud-service-profile.md` R8 | Unchanged text; it is now the *only* MUST. |
| `versioning.md` §3 | Delete "(default: exact match, else highest available version)"; cite core-concepts §2. |
| `transport-bindings.md` §2 | Lift the three normative sentences out of the informative catalogue: EventBridge "MUST NOT require a `topic` attribute" and DynamoDB "derived" → `transport-metadata-cases.json`'s `topicSources` already pins them, so cite it from §1; the MCP MUSTs move with MCP in Phase 2 (hold here as a note). |
| `design-principles.md` §5.2–§5.3 | Defaults become `/benzene/invoke`, `/benzene/spec-ui`, `/benzene/mesh-ui`; delete §5.3 (D4). |

**Fixtures:** `envelope-cases.json` gains one case — a request carrying `benzene-version` for a
version no canonical handler has → `not-found` (pins D1 observationally; today nothing does).

**Ports:**

| Port | Owes |
|---|---|
| .NET | `VersionSelector` default → exact (strict selector becomes the default, "highest" the swap-in); `BenzeneMessageHttpOptions.DefaultPath` → `/benzene/invoke`; `MeshUiExtensions.DefaultPath` → `/benzene/mesh-ui`; `SpecUiExtensions.DefaultPath` → `/benzene/spec-ui`; `CloudServicePaths` / `CloudServiceProbePaths` and every example that spells a path; docs + matrix row (divergence note 8 closes). |
| Go | Already exact-only; verify the unmatched-version error names the version; verify UI/envelope defaults. |
| TypeScript, Python | Default selector → exact (Python: `exact_version` is already the default — verify); defaults verified. |

**Exit:** `docs/capabilities.md` divergence note 8 deleted; website build green; all four
drift-checks green on the new `envelope-cases.json`.

### Phase 2 — Complete the transport contract *(M)*

Decisions: D6, D16, D17.

**Spec:**

| File | Edit |
|---|---|
| `transport-bindings.md` §1 | Add obligation **8 — Settlement** (D16, the one paragraph) and a sentence under 5 for the envelope endpoint (D17). Add "a binding SHOULD state its unit of work and its cancellation source" to the existing SHOULD. |
| `transport-bindings.md` §2 | Replace the prose catalogue with **one table** — binding · topic source · metadata channel · reply channel · unit of work · settlement default · cancellation source — identical in membership to `transport-metadata-cases.json`'s `topicSources` plus HTTP, gRPC, Cosmos change feed, S3, Kinesis, Queue Storage, Event Grid, Blob (every binding a port ships). Per-binding narrative (EventBridge's `_benzeneHeaders` lifting, DynamoDB's image rule, RabbitMQ's routing-key fallback) moves to the port docs. HTTP's row says: topic from an explicit route table (`httpMappings`) or the envelope endpoint — there is no implicit `POST /{topic}`. |
| `transport-bindings.md` §2 "Outbound clients" | Moves to `core-concepts.md` as a new §"Outbound": the **sender** (`send(topic, headers, body, version?) → result`), the **routing table** (`topic → destination`, deployment configuration, not contract), and **declared produces** (the registry's outbound twin). mesh §2.3 then cites it. |
| `mcp.md` *(new, module)* | The MCP entry from transport-bindings §2, as its own document with its own status line and the `benzene-mcp-tool`/`benzene-mcp-session` headers (D6). The three MUSTs from Phase 1's note land here. |
| `wire-contracts.md` §2 | Two new header rows under "Module: MCP". |
| `index.md` | Add `mcp.md` under Modules (the grouping is formalised in Phase 4; add the bullet now). |

**Fixtures:**

- `transport-metadata-cases.json`: add a `settlementDefaults` section, the same shape as
  `topicSources` — per binding, `shape` (`queue` | `stream` | `one-shot` | `request-response`) and
  `onFailureResult` (`redeliver` | `skip-and-continue` | `n/a`). A port asserts its constants
  against it, the cheapest check and the one that catches a knob defaulting off.
- `problem-details-cases.json` `httpRules`: add `envelopeEndpoint` — a `not-found` envelope posted
  to `/benzene/invoke` → HTTP 404, body is the envelope, inner problem body excludes `status`.

**Ports:**

| Port | Owes |
|---|---|
| .NET | Rename the MCP headers; docs. Settlement already conforms (`SettlementContractDefaultsTest`); add the fixture assertion. |
| TypeScript | **The largest item in this phase:** the three adapters whose escalation knob defaults off (SNS, Service Bus trigger, Azure Kafka trigger) default on; the five with no knob (EventBridge, S3, MSK, Kinesis, Event Hub trigger) gain one, defaulting per D16. Its matrix already states the gap. |
| Go, Python | State settlement defaults in the matrix (both currently `unknown`); conform where they don't; MCP n/a. |

**Exit:** `docs/capabilities.md` divergence note 3 and the "settlement contract is not in the spec"
flag close; TS/Go/Python settlement cells are `yes`.

### Phase 3 — Reserved topics and the wire renames *(L; the one coordinated wire window)*

Decisions: D7–D14. Everything here changes bytes on the wire; it lands as one spec PR and one
catch-up PR per port, so the estate is inconsistent for one window rather than eight.

**Spec:**

| File | Edit |
|---|---|
| `wire-contracts.md` §1.2 | `statusCode` → `status` (D12). |
| `wire-contracts.md` §1.3 | Delete the two "previously described… withdrawn" paragraphs (rule 2). `type`: optional, application-owned, absent for framework failures (D7). `errors[].field`: JSON Pointer (D8). |
| `wire-contracts.md` §2 | Tier column → Core / Module / Binding (D9). `benzene-version` row: one name, no fallback list (D10). `_benzeneHeaders` row → `benzene-headers` (D14), with the sentence the headers plan prescribes ("a header name carried in a payload, so it keeps the header convention"). Delete the "`benzene-status` … appeared in this table historically" paragraph. "MUST expose them as a single injectable value" → "configurable, in one place, for both directions". |
| `wire-contracts.md` §2.1 | Claim check → `claim-check.md` *(new, module)*; the row stays, labelled "Module: claim-check". |
| `wire-contracts.md` §3.1 | **Deleted** (D7). The HTTP `status` member rule in §1.3 and the §4.1 table carry what survives. |
| `wire-contracts.md` §5 | Topic `benzene:health` (D13); `-2`/`-3` dedup → "keys are unique". |
| `wire-contracts.md` §6 | Case-insensitive read → MAY (D11). |
| `wire-contracts.md` *new §* "Reserved topics" | The table (D13): topic · request · response · default HTTP path, for `benzene:health`, `benzene:liveness`, `benzene:readiness`, `benzene:spec` (shape: "the self-description document" — until Phase 6, the Contract Document), `benzene:ping`, and the collector topics by reference to mesh §4. The rule: the `benzene:` prefix is reserved for this table; a port adds a row before it adds a topic. `/benzene/invoke` is the one well-known path that is not a topic. |
| `design-principles.md` §5.2 | Reduced to "every well-known HTTP surface is the HTTP binding of a reserved topic — see wire-contracts" plus the `/benzene/` prefix rule and the three UI paths. |
| `core-concepts.md` §10 | `benzene:healthcheck` → `benzene:health`. |
| `mesh.md` §3 | `topicVersion` → `version` (D12). |
| `versioning.md` §2 | Fallback-list rationale deleted (D10); the HTTP route segment described as sugar over the header. The §7 "route-parameter naming collision" open question closes with it. |
| `transport-bindings.md` §2 table | EventBridge row: `benzene-headers`. |
| `conformance/README.md` | Delete the `_benzeneHeaders` paragraph; `registry` group gone; `conformance:problem` handler's `field` is a JSON Pointer. |
| `contract-document.md` §5.1 | Unchanged this phase (deleted in Phase 6). |

**Fixtures:**

| File | Change |
|---|---|
| `envelope-cases.json` | `statusCode` → `status` in every `expected`; a health case uses `benzene:health`. |
| `problem-details-cases.json` | `registry` group removed; `envelopeCases`: `bodyExclude: ["type"]` on framework-produced documents, `field` values become pointers (`/name`); `httpRules` unchanged. |
| `transport-metadata-cases.json` | `defaultMetadataKeys` gains `headers: "benzene-headers"` (the EventBridge key, now pinned — the README's reason not to is gone); any fallback-name case removed; `version-travels-alongside-the-topic` unchanged. |
| `mesh-trace-cases.json` | `topicVersion` → `version`. |
| `mesh-collector-cases.json`, `mesh-issue-cases.json`, `mesh-service-version-cases.json` | Envelope `statusCode` → `status` in expected responses. |
| `status-vocabulary.json`, `http-`/`grpc-status-mapping.json` | Unchanged. |

**Ports (all four):** envelope and trace field renames; `benzene:health`; `benzene-headers`;
registry removal (no `type` on framework failures); JSON Pointer from every validation adapter (.NET:
FluentValidation/DataAnnotations property path → pointer; schema validators already emit one);
single version header; the .NET `BenzeneTopic` registry reconciled with the table (D13's three
port-private topics decided). Each port's docs and matrix.

**Exit:** all four drift-checks green on the new snapshot; `docs/capabilities.md` divergence note 2
(claim check) closes — the header is now in the spec.

### Phase 4 — Tiering, narrative removal, and the proposals *(M; doc moves, no wire change)*

Decisions: D5, D19.

**Spec:**

| Action | Detail |
|---|---|
| One index | `README.md` becomes the single index: the tiers, one table per tier, the conformance-language paragraph, the "one design rule", and a one-line pointer to `VERSION` (D20, Phase 7). `index.md` keeps only what the website nav builder needs — the nested bullet list, grouped **Core / Modules / Profile / Guidance** — with the one-line descriptions matching README's. |
| Status lines | Every document's header becomes one line: `**Tier:** Core` / `Module — implemented by: .NET, Go` / `Profile` / `Guidance (informative)`. The "DRAFT v0.1 — promoted from…/primary implementation…/covers the full contract…" prose goes. |
| Guidance moves | `design-principles.md`, `porting-guide.md`, `port-quality-standards.md` → `docs/guides/` (a website source already; add them to `docs/guides/index.md`). Inbound links fixed: `docs/patterns/*` (5), `docs/guides/mesh-ui.md`, `.claude/agents/ergonomics-champion.md`, `work/mesh-ui-product-vision.md`, `website/generator/MarketingPages.cs` (`design-principles.html` → `guides/…`). The normative residue of design-principles stays in the spec: §4's "every convention, both sides" is already wire-contracts §2's rule (cite it); the extension-point table becomes a six-line paragraph in `core-concepts.md` — *each of these is a replaceable component with a default* — without the DI vocabulary. |
| `core-concepts.md` §7–§8 | §7 → two sentences: one application definition mounts several transports; a mount the host cannot honour is a silent no-op. §8 → two sentences: a scope per invocation; framework defaults the application can replace. The three phases and the six container operations go. |
| `mesh.md` | Delete the status header, the revision note, §9. §2.4–§2.5 → `service-versions.md` *(new, module; implemented by .NET)*. §4.1 keeps the contract, loses the "Additive 2026-07… parity pending" prose. §4: pin the three query read models (D5) — the response shapes the collector fixture already asserts, subset-matched, no more. |
| `versioning.md` | Reduced to the wire part (~60 lines): the header, absence, dispatch (cites core-concepts §2), the HTTP route sugar, the per-port note that *how* a service absorbs a version difference (casting, forwarding handlers) is invisible on the wire and documented per port. §3.1, §4.1–§4.4 → benzene-dotnet docs (a cookbook; the port owns it). §5.1–§5.3, §5.5–§5.6 → `service-versions.md` where they are the identity layer's rationale, trimmed. §5.4, §5.7, §6, §7 → `work/side-by-side-service-versions.md` (proposal; D19). |
| `contract-document.md` | Delete the status header and §6.1; the `cross-language-clients-plan.md` references go. (Merged away in Phase 6.) |
| `cloud-service-profile.md` §5 | Reduced to: a port MAY offer a wiring-time self-check carried on `profile`, and an external black-box probe; a link to the .NET docs for both. |
| `wire-contracts.md` | Delete §1.1's `message`/`body` note. |
| `porting-guide.md` §5 | Deleted before the move (history). |
| `conformance/README.md` | Claims table re-cut: **Core** · **per binding** (`transport-metadata-cases` + `httpRules` for HTTP) · **Profile** (`mesh-descriptor-cases` + `mesh-trace-cases` until Phase 6 renames them) · **per module** (mesh collector; issues; service versions — one claim, both fixtures; client generation). Eight claims become four kinds. |

**Fixtures:** none change. **Ports:** link fixes in docs that cite moved files; .NET takes the
versioning cookbook; `SPEC_VERSION` bump.

**Exit:** website green with all four ports; `remaining-issues-plan.md` P1.3 struck through
("claimed").

### Phase 5 — The profile split *(S)*

Decision: D15.

**Spec:** `cloud-service-profile.md` §1.2 gains the second level; R6 moves under **Meshed**; §6's
ladder table drops (the ladder went to guidance in Phase 4); `profile.name` values stated.
`mesh.md` §2 `profile` field: the two names. `design-principles.md` (now guidance) §2's "the top of
the ladder has a name" paragraph updated.

**Fixtures:** none (`profile` is optional and unpinned).

**Ports:** .NET `CloudServiceProfileReport` reports the claim it satisfies (`cloud-service` without
R6, `meshed` with); `Benzene.CloudService.Probe` likewise. Go `cloudservice.New` the same. TS/Python
verify.

### Phase 6 — One self-description *(XL; touches every port and every collector)*

Decision: D18. Planned with each port's DX champion before the spec PR is opened
(`port-quality-standards.md` §1 applies in full); the four port PRs land within one window, because
a collector on the old register body cannot host a service on the new one.

**Spec:**

| Action | Detail |
|---|---|
| `self-description.md` *(new, module; required by the profile)* | §1 the document: `service`, `serviceVersion` (+ `versionScheme`, cited from `service-versions.md`), `instanceId`, `runtime`, `placement`, `transports[]`, `messageEndpoint`, `requests[]`, `events[]`, `components.schemas`, `contractHash`, `degraded`, `profile`. §2 derivation: mesh §2.1's type-mapping table (JSON Schema 2020-12 — which is OpenAPI 3.1's schema object, so OpenAPI tooling still reads it) plus `$ref` into `components.schemas`; recursive types by `$ref`, not cut to `{}`. §3 generation rules: contract-document §5.2 (include-list, fail-loud), §5.3 (closure walk), §5.4 (transport-agnostic output). §4 hash: contract-document §6.2's algorithm over the contract only — `requests`, `events`, `components`; `transports`, `messageEndpoint`, `instanceId`, `degraded`, `profile` excluded (they describe a deployment, not the contract); §6.4's projection-comparability rule kept. One canonicalization: JCS. §5 serving: `benzene:spec` over the envelope; `GET /benzene/spec` over HTTP; the same bytes. |
| `mesh.md` | §1 (`benzene:mesh`) deleted; §2 reduced to "the register body is the self-description document (self-description.md)"; §2.1–§2.3 deleted (derivation and outbound registration live in self-description.md and core-concepts §Outbound); §5 heartbeat `descriptorHash` → `contractHash`; §4 "re-registration replaces… `produces`" → "`events`". |
| `contract-document.md` | **Deleted.** `index.md`, README, porting-guide (now guidance), profile R5 re-pointed. |
| `cloud-service-profile.md` R5, R6 | R5: "derive and serve the self-description document at `benzene:spec` / `/benzene/spec`". R6: the `benzene:mesh` bullet goes (it is R5 now). |
| `wire-contracts.md` reserved-topic table | `benzene:spec` → "the self-description document"; `benzene:mesh` row never existed. |

**Fixtures:**

| File | Change |
|---|---|
| `self-description-cases.json` *(new)* | = `mesh-descriptor-cases.json` (canonical handlers + canonical outbound registration → `requests`/`events`, schema derivation, hash *properties*) + `contract-document-cases.json` (parse, include-list, closure) merged; one `documents` map. |
| `contract-hash-cases.json` | Kept as the exact-value fixture for the one hash, re-computed over the new shape and renamed `self-description-hash-cases.json`. |
| `mesh-descriptor-cases.json`, `contract-document-cases.json` | Removed. |
| `mesh-collector-cases.json`, `mesh-service-version-cases.json`, `mesh-issue-cases.json` | Register bodies and `descriptorHash` fields updated; `produces` → `events` in the graph cases. |
| `mesh-trace-cases.json` | Unchanged. |

**Ports (all four):** descriptor builder and spec endpoint become one builder; `benzene:mesh`
interception removed; codegen reads the new shape (TS/Python/Go/.NET generators); collectors ingest
the new register body and compare `contractHash`; the .NET `Benzene.Schema.OpenApi` package keeps
its OpenAPI 3.x/AsyncAPI *exports* as port-specific enrichments derived from the same builder (they
are not the spec document and are not served as it). Each port's docs and matrix.

**Exit:** all four ports pass `self-description-cases.json` and the hash fixture; a Go collector
hosts a .NET service and vice versa on the new body (the live-interop form); `docs/capabilities.md`
"Spec endpoint" and "Mesh — service side" rows re-read.

### Phase 7 — Close *(S)*

- `docs/specification/VERSION` = `0.2.0-draft` (D20); README's "Versioning" section rewritten to
  three lines: the file, semver-from-1.0, "a profile requirement added is a major".
- `docs/capabilities.md` full refresh (`port-aligner`) — the spec-gap flags (settlement, claim
  check, version selection) are gone; the "Spec-requirement flags" section is re-derived.
- `benzene-headers-design.md` and `benzene-headers-plan.md` → `work/archive/` (Phase A done; the
  packed-headers half rejected — stamped so).
- This plan and `spec-review-2026-10.md` → `work/archive/`, stamped with the phase commits;
  `work/README.md` updated.
- `remaining-issues-plan.md` P1.3 removed.

## 3. Per-repo view

| Repo | Phase 1 | 2 | 3 | 4 | 5 | 6 | 7 |
|---|---|---|---|---|---|---|---|
| **benzene** (spec) | contradictions; HTTP defaults | §1 obligations 8 + envelope; binding table; outbound §; `mcp.md` | reserved topics; renames; registry gone; `claim-check.md` | tiers; guidance to `docs/guides/`; narrative out; `service-versions.md`; proposals to `work/` | profile split | `self-description.md`; `contract-document.md` gone; fixtures merged | `VERSION`; archive |
| **benzene-dotnet** | selector default; three path defaults | MCP headers; fixture assert | all renames; JSON Pointer; `BenzeneTopic` reconciled | versioning cookbook; links | two-level report + probe | one builder; collector; codegen | matrix |
| **benzene-go** | verify | settlement stated; conform | all renames; registry gone | links | `cloudservice.New` levels | builder; collector (`meshd`); codegen | matrix |
| **benzene-typescript** | selector default | **settlement knobs** (8 adapters) | all renames; JSON Pointer | links | verify | builder; collector; codegen | matrix |
| **benzene-python** | verify | settlement stated; conform | all renames | links | verify | builder; collector; codegen | matrix |

Each port's catch-up PR: code, docs, `docs/capability-matrix.md`, re-vendored
`test/conformance-fixtures/` (or the port's equivalent), `SPEC_VERSION` = the benzene commit of
that phase.

## 4. What this plan deliberately does not do

- It does not touch `status-vocabulary.json` or the HTTP/gRPC mapping tables (the deferred table
  in §1).
- It does not reorganize `docs/specification/` into sub-folders (rule 4).
- It does not rewrite `blog/` or the website's marketing copy beyond fixing moved links; the copy
  cites `core-concepts` and `transport-bindings`, both of which keep their paths.
- It does not decide the three port-private `benzene:*` topics for the .NET port (D13) — the port
  proposes; the spec accepts a row or the port renames.

## 5. Checklist

```
Phase 1  [ ] spec PR merged   [ ] .NET   [ ] Go   [ ] TS   [ ] Python   [ ] capabilities.md
Phase 2  [ ] spec PR merged   [ ] .NET   [ ] Go   [ ] TS   [ ] Python   [ ] capabilities.md
Phase 3  [ ] spec PR merged   [ ] .NET   [ ] Go   [ ] TS   [ ] Python   [ ] capabilities.md
Phase 4  [ ] spec PR merged   [ ] .NET   [ ] Go   [ ] TS   [ ] Python   [ ] website green (4 ports)
Phase 5  [ ] spec PR merged   [ ] .NET   [ ] Go   [ ] TS   [ ] Python
Phase 6  [ ] champions consulted (4)   [ ] spec PR merged   [ ] .NET   [ ] Go   [ ] TS   [ ] Python   [ ] live interop both ways
Phase 7  [ ] VERSION   [ ] capabilities.md full refresh   [ ] archives + README
```
