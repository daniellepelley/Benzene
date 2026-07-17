# Benzene — Enterprise Adoption Gap Analysis

**Status:** DRAFT for review — a decision-oriented backlog, not a commitment
**Last Updated:** 2026-07-17
**Purpose:** Identify the *features* Benzene needs for enterprise adoption by reasoning about a
representative enterprise system's requirements and finding where Benzene struggles. Deliberately
**not** about "more battle-hardening" or "more downloads" — every item is a buildable capability,
**or** a deliberate decision that it is *not* Benzene's job.

**Guiding principle (from review 2026-07-17):** Do not reinvent the wheel. Benzene should own
**cross-cutting concerns that live in the shared pipeline** and **reliability primitives its
transport model demands** — and stay out of **database access** and **domain-level** concerns,
which belong to the application inside the running process. A capability only belongs in Benzene
if it (a) fits the middleware/hexagonal model, (b) leverages the uniform topic+payload message
shape, or (c) adds genuine multi-cloud abstraction value. Otherwise it's a documented pattern the
user builds with existing extension points, or it's out of scope entirely.

**Method:** Every claim was checked against the codebase (package list, `src/**`) at the date
above, not assumed. Where an item says "absent" or "already partly there", it was verified by
inspection.

**Read alongside:** [`benzene-vision.md`](benzene-vision.md) — items are scored against its five
design tests (§2).

---

## 1. The reference enterprise system

Anchor on **Benzene's own origin** (see `benzene-vision.md` §1): a **multi-tenant B2B SaaS serving
thousands of businesses**, event-driven, multi-cloud. Its non-negotiables:

1. **Strict tenant isolation** — no cross-tenant data/config/load bleed.
2. **Fine-grained authorization** — "may *this* principal do *this* action on *this* resource".
3. **Reliable async processing** — at-least-once transports must not lose or double-apply events;
   multi-step distributed operations must not leave orphaned records on partial failure.
4. **Auditability & compliance** — who did what; PII handled to policy.
5. **Operational governance** — secrets out of config, safe config change, per-tenant quotas.

## 2. What Benzene already has (so we don't rebuild it)

- **Transport-agnostic handlers + one middleware pipeline** across AWS Lambda, Azure Functions,
  Azure self-hosted (Service Bus / Event Hub), Google Cloud, gRPC, ASP.NET Core, and self-hosted
  workers (Kafka / HTTP). This is the moat — most "gaps" are one middleware away *because* of it.
- **Scoped per-message state seam** — `PresetTopicHolder` + `PresetTopicMiddleware` demonstrate the
  canonical pattern: a scoped holder set by a middleware, read downstream, `null` when a pipeline
  doesn't opt in, never coupled to the transport context. **This is the seam tenancy (and similar
  cross-cutting state) reuses.**
- **Authentication**: `Benzene.Auth.OAuth2` (JWT/JWKS bearer), `Benzene.Auth.Basic`, `.Core`.
- **Authorization**: **scope-only** (`ScopeClaims` + `RequireScope`). No roles/policies/permissions.
- **Validation / serialization / content negotiation**: FluentValidation, DataAnnotations, JsonSchema;
  Json, Newtonsoft, Avro, MessagePack, Xml; `AddMediaFormatNegotiation`.
- **Caching**: `Benzene.Cache.Core` + `.Redis`.
- **Resilience**: `Benzene.Resilience` — **retry only** (`RetryMiddleware`).
- **Observability**: `Benzene.OpenTelemetry`, `Benzene.Diagnostics`, correlation IDs, W3C
  trace-context propagation on outbound `Clients`.
- **Health / ops**: `Benzene.HealthChecks` (+ EntityFramework, Http), k8s probes
  (`Benzene.CloudService.Probe`).
- **Outbound / integration**: `Benzene.Clients` (+ `.Aws`, `Client.Http`), mid-redesign
  (`benzene-clients-redesign-plan.md`).
- **Service visibility & contracts**: `Benzene.Mesh.*` — catalog, topology, **contract-drift**;
  `MeshManifest`/`MeshManifestEntry` (provider contract + `ContractDrift` flag); a `"schema"`
  health check exposing a contract `hashCode`; and consumer-side `ClientHealthCheckProcessor` /
  `ClientHashMatch` that compares a downstream service's hash against the hash a client was built
  with. **This is a half-built contract-testing substrate.**
- **API/spec/versioning**: `Benzene.Core.Versioning`, `Benzene.Schema.OpenApi`, `Benzene.Spec.Ui`,
  CORS.
- **Codegen / DX**: Terraform, OpenAPI, Client SDKs, templates, source generators.

---

## 3. Category A — Build it (on-model, first-class)

### A.1 — Sagas / distributed rollback (`Benzene.Saga`) ★ top priority

- **Requirement.** Multi-step distributed operations (each step calling another service) must either
  fully complete or fully roll back, leaving no orphaned records, and then be safely retryable.
- **Production precedent (this is proven on Benzene).** The production system used exactly this: a
  user-signup process fanned out to many lenders as Benzene-style services in **stages**, tasks
  within a stage running in **parallel**. Stage 1 creates a tenant + a company in Okta; stage 2
  uses the returned tenant ID to create a user in a microservice; stage 3 uses that user ID to
  create an RBAC role and related identity items. If **any** call fails, the saga runs a **take-down
  strategy per stage** — a compensation function handed the created record's ID that deletes it —
  rolling the whole operation back to its starting state, after which the entire process can be
  retried. Proved very reliable and orphan-free.
- **Current state.** None; Benzene is stateless-handler-shaped.
- **The gap.** No orchestration for staged, compensating, retryable distributed transactions.
- **Proposed work.** A saga package built **on the uniform topic+payload model**: a saga definition
  of ordered **stages**; each stage a set of **parallel tasks**; each task pairs a forward action
  (a message send via `Benzene.Clients`) with a **compensation** keyed by the ID(s) the forward
  action produced; a saga runner that executes stages in order, and on any failure runs
  compensations in reverse (per stage) to restore the starting state; idempotent, retry-safe state
  tracking of what completed. Because Benzene messages are uniform, wrapping this around them is
  natural — the runner speaks topic+payload, not bespoke per-step glue.
- **Vision fit.** Strong — leverages the uniform message shape and the outbound Clients path;
  orchestration logic stays generic, compensation logic stays in the app. (Supersedes the earlier
  "integrate an external orchestrator instead of building one" note — production experience shows a
  Benzene-native saga around uniform messages is the right call.)
- **Open design questions.** Where saga state lives (pluggable store, no DB opinion baked in);
  parallel-task failure semantics (fail-fast vs. await-all-then-compensate); retry/backoff of the
  whole saga vs. individual stages; timeout/heartbeat for long-running sagas.

### A.2 — Contract testing / breaking-change gate (extend `Benzene.Mesh` + `Benzene.Clients.HealthChecks`)

- **Requirement.** Catch a provider's breaking contract change **before** it reaches an environment
  a consumer depends on.
- **Current state (already half-there).** Providers expose a contract `hashCode` via a `"schema"`
  health check and a `MeshManifestEntry.ContractDrift` flag; consumers already compare a
  downstream hash against their built-in expectation (`ClientHealthCheckProcessor` → `ClientHashMatch`).
  But it's **runtime-only** (surfaces as a health-check warning) and **coarse** (whole-spec hash →
  can't tell breaking from additive).
- **The gap.** No build/CI-time check; no breaking-vs-non-breaking discrimination; consumer
  expectations aren't captured as CI artifacts.
- **Proposed work.** Capture each consumer's expected provider contracts (topics + hashes/specs)
  as an artifact; a CI check that compares them against providers' published `MeshManifest`/OpenAPI
  and **fails on breaking changes** (removed/renamed topic, incompatible payload) while allowing
  additive ones. Reuse the existing hashing + manifest + health-check plumbing rather than adding a
  Pact-style parallel system.
- **Vision fit.** Pure Benzene infrastructure reuse; no wheel reinvention.

### A.3 — Idempotency / exactly-once effect (`Benzene.Idempotency`)

- **Requirement.** A redelivered message (the norm on SQS/Service Bus/Event Hubs/Kafka) must not
  apply its effect twice.
- **Current state.** **Shipped** (`Benzene.Idempotency`). `IdempotencyMiddleware<TContext>` derives a
  key (default: `idempotency-key` header, else a deterministic SHA-256 of topic+body — swap via
  `IIdempotencyKeyStrategy<TContext>`), atomically claims it in a pluggable `IIdempotencyStore`, and
  invokes the handler only on the first sighting. Duplicates of a completed key short-circuit and
  replay a successful result so the transport acks them. A handler that throws or reports failure
  releases the claim so the redelivery reprocesses — a transient failure is never permanently
  swallowed. `InMemoryIdempotencyStore` ships as the single-instance/test default; a shared store
  (worked Redis `SET NX` example in the cookbook) is a three-method implementation. 20 unit tests.
- **Original proposal.** Idempotency-key middleware (key from a header/property or a deterministic
  topic+body hash) + a **pluggable** store contract; ship a Redis adapter over the existing
  `Benzene.Cache.Redis`, leave DynamoDb/SQL to the user. Duplicate keys short-circuit and replay the
  recorded outcome. **No bespoke DB layer** — Benzene owns the middleware + contract, not storage.
  (Delivered as designed; the Redis adapter is documented as a copy-paste `IIdempotencyStore` rather
  than a separate package, since the cache abstraction doesn't expose the atomic `SET NX` the claim
  needs and there is no Redis in CI to integration-test a shipped adapter.)
- **Vision fit.** Cross-cutting → shared pipeline; transport-agnostic reliability primitive.
- **Docs.** `src/Benzene.Idempotency/CLAUDE.md`; cookbook `docs/cookbooks/idempotency.md`.

### A.4 — Authorization depth (extend `Benzene.Auth`)

- **Requirement.** Roles, policies, and resource/attribute checks — declarative per handler.
- **Current state.** Scope-only (`ScopeClaims` + `RequireScope`).
- **Proposed work.** A policy/permission enforcement layer: `[RequirePermission]`/`[RequirePolicy]`
  handler attributes, role-claim support, and a resource-based hook
  (`IAuthorizationHandler<TResource>`), all transport-agnostic. Benzene owns the **enforcement
  mechanism**; what a permission *means* is the app's pluggable policy handler (keeps domain out).
- **Vision fit.** Cross-cutting → shared pipeline.

### A.5 — Secrets & multi-cloud configuration (`Benzene.Configuration.*`)

- **Requirement.** Secrets never in plaintext config; typed config validated at startup; portable
  across clouds.
- **Current state.** Raw `IConfiguration`/env vars only.
- **Proposed work.** A **neutral secrets/config abstraction** with provider adapters for Azure Key
  Vault, AWS Secrets Manager, SSM Parameter Store, and Azure App Configuration; startup
  fail-fast validation; optional reload. The value is the multi-cloud abstraction (a portability
  win, on-vision) — not re-implementing any provider.
- **Vision fit.** Portability (§2.7); neutral abstraction, provider adapters at the edge.

### A.6 — Schema registry integration (`Benzene.SchemaRegistry.*`)

- **Requirement.** Manage/validate event payload schemas centrally (Confluent / Azure Schema
  Registry) for event-driven contracts.
- **Current state.** `Benzene.Avro` handles schemas registry-less; Mesh tracks drift; no registry
  client.
- **Proposed work.** Registry client integration for (de)serialization + schema evolution checks,
  complementing Avro and the A.2 contract gate. Benzene "lends itself to this" — uniform messages +
  existing serialization seam.
- **Vision fit.** Fits the serialization/contract story.

---

## 4. Category B — Enable via existing seams + docs (thin or no new package)

### B.1 — Multi-tenancy (pattern, not machinery)

- **Requirement.** Tenant attribution + isolation for a multi-tenant B2B system.
- **Current state / seam.** The `PresetTopicHolder` pattern is exactly the mechanism: a **scoped
  `TenantHolder`** set by a **tenant-resolver middleware** (claim / header / subdomain), read by
  handlers and forwarded onto outbound `Clients`, `null`/guarded when absent — reusing per-message
  DI scope that already exists.
- **Decision.** Benzene does **not** need heavy isolation machinery. Ship a **cookbook** documenting
  the scoped-holder tenancy pattern; **optionally** a thin `Benzene.MultiTenancy` convenience that
  packages the holder + resolver strategies + a "tenant required" guard so teams don't rewrite it.
  Isolation of data/cache is achieved by the app using the tenant context (e.g. per-tenant cache
  key prefix, per-tenant connection string) — Benzene provides the context, not the storage policy.
- **Vision fit.** Passes all five, precisely *because* it's the existing seam.

---

## 5. Category C — Out of scope for Benzene (user-space)

### C.1 — Unit-of-work / data access / transactions

Database access is not something Benzene does or has an opinion on. A per-message DB transaction,
repository/UoW, EF/Dapper conventions — all belong to the application inside the running process,
using whatever libraries it prefers. **Benzene stays out of the database.** (Note: this is why A.1
saga state and A.3 idempotency stores are *pluggable contracts*, not built-in persistence.)

### C.2 — Audit trail

An audit trail is domain-level (what counts as an auditable action, what the record means, retention
policy). Benzene shouldn't own a domain audit model. A team that wants one writes an audit middleware
using the principal/tenant context Benzene already exposes — the same way any other app-specific
cross-cutting concern is added. **Out of scope as a Benzene feature.**

---

## 6. Category D — Boundary case (decide before building)

### D.1 — Transactional outbox

The outbox's forward half (a relay reading pending events and publishing via `Benzene.Clients`) is
Benzene's outbound domain; its other half (writing the outbox row **inside the business DB
transaction**) is squarely the application's ORM/DB concern, which §5/C.1 says Benzene stays out of.
**Options:** (i) Benzene ships only a relay + a store *interface*, users implement the store against
their own transaction; (ii) defer entirely and let A.1 (sagas) cover multi-step consistency, which
in practice addresses much of what teams reach to an outbox for. **Recommend deferring** pending a
decision, and revisiting after A.1.

---

## 7. Category E — Open / undecided (parked, low priority)

- **Pagination** — a uniform `IPagedResult<T>` convention *could* help, but paging is partly
  domain. Undecided.
- **Feature flags** — a thin OpenFeature evaluation hook in the pipeline (tenant-aware once B.1
  lands). Small, optional; undecided.
- **Resilience depth** — extend `Benzene.Resilience` (retry-only) with circuit breaker / timeout /
  bulkhead / fallback. Framework-cross-cutting; reasonable later.
- **Delegated identity / token exchange** — outbound `Clients` propagating caller identity (OBO).
  Fits Clients; lower priority than the above.

---

## 8. Status board

| ID | Item | Category | Vehicle | Status |
|----|------|----------|---------|--------|
| A.1 | Sagas / distributed rollback ★ | Build | `Benzene.Saga` | **Shipped** — engine + tests + example (v1: in-process, engine-only; see `saga-design.md` §7 fast-follows) |
| A.2 | Contract testing / CI gate | Build | `Benzene.HealthChecks.Schema` + `Clients.HealthChecks` + `Schema.OpenApi.Compatibility` | **Shipped** — A.2a runtime drift check (provider `SchemaHealthCheck` + hardened consumer processor) and A.2b CI gate (`SchemaCompatibility.EnsureBackwardCompatible`); cookbook `docs/cookbooks/contract-testing.md` |
| A.3 | Idempotency | Build | `Benzene.Idempotency` | **Shipped** — `IdempotencyMiddleware<TContext>` + pluggable `IIdempotencyStore` + in-memory store + header/body-hash key strategy; 20 tests; cookbook `docs/cookbooks/idempotency.md` (Redis store as copy-paste `SET NX`) |
| A.4 | Authorization depth | Build | extend `Benzene.Auth` | Not started |
| A.5 | Secrets & multi-cloud config | Build | `Benzene.Configuration.*` | Not started |
| A.6 | Schema registry | Build | `Benzene.SchemaRegistry.*` | Not started |
| B.1 | Multi-tenancy | Enable via seam + docs | cookbook (+ optional thin helper) | Not started |
| C.1 | Unit-of-work / data access | Out of scope | — | Won't do |
| C.2 | Audit trail | Out of scope | — | Won't do |
| D.1 | Transactional outbox | Boundary | relay + store interface, or defer | Deferred — decide after A.1 |
| E.1 | Pagination | Open | Core | Parked |
| E.2 | Feature flags | Open | `Benzene.FeatureFlags` (OpenFeature) | Parked |
| E.3 | Resilience depth | Open | extend `Benzene.Resilience` | Parked |
| E.4 | Delegated identity | Open | extend `Benzene.Clients` | Parked |

## 9. Recommended sequence

1. **A.1 Sagas** — highest-value, production-proven, uniquely well-suited to Benzene's uniform
   message model; unlocks reliable multi-step distributed operations without orphaned records.
2. **A.3 Idempotency** + **A.2 Contract testing** — make the at-least-once async model and the
   service-to-service contracts trustworthy; A.2 is largely reuse of existing plumbing.
3. **A.4 Authorization depth** — clears the enterprise security-review bar.
4. **A.5 Secrets/config** and **A.6 Schema registry** as multi-cloud/event maturity needs dictate.
5. **B.1 Multi-tenancy** cookbook can land any time (cheap; documents an existing seam).

Everything in Category A is a cross-cutting concern or reliability primitive that fits Benzene's
model directly; Categories C and D keep Benzene out of the database and the domain.
