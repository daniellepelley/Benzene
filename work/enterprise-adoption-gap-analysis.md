# Benzene — Enterprise Adoption Gap Analysis

**Status:** DRAFT for review — a backlog to work through, not a commitment
**Last Updated:** 2026-07-17
**Purpose:** Identify the concrete *features* Benzene needs to be adopted by an enterprise
business, by reasoning about a representative enterprise system's requirements and finding where
Benzene would struggle to meet them. Deliberately **not** about "more battle-hardening" or "more
downloads" — every item here is a buildable capability.

**Method:** Each gap was checked against the actual codebase (package list, `src/**`), not
assumed. Where an item says "absent", it was verified by inspection at the date above. This
document should be held to the same standard as it evolves — see the repo's other roadmaps for how
easily an unchecked "it's missing" claim turns out to be wrong.

**Read alongside:** [`benzene-vision.md`](benzene-vision.md) — every proposed item below is scored
against its five design tests (§2.1–2.7). The strongest candidates are cross-cutting concerns that
belong in the *one shared middleware pipeline*, which is exactly where the vision says work should
live.

---

## 1. The reference enterprise system

To find gaps that are sharp rather than generic, we anchor on a concrete system — and the most
credible one is **Benzene's own origin story** (see `benzene-vision.md` §1): a **multi-tenant B2B
SaaS platform serving thousands of businesses**, event-driven, deployed across more than one cloud.

That system's non-negotiables:

1. **Strict tenant isolation** — one tenant can never see or affect another's data, config, or load.
2. **Fine-grained authorization** — not just "is this a valid token" but "may *this* principal do
   *this* action on *this* resource".
3. **Reliable async processing** — at-least-once transports must not lose events or apply them
   twice; a DB write and its published event must not diverge.
4. **Auditability & compliance** — a tamper-evident record of who did what; PII handled to policy.
5. **Operational governance** — secrets never in config files, safe config change, quotas so one
   tenant can't starve others, predictable failure behaviour.

The rest of this document walks those requirements against what Benzene has today.

## 2. What Benzene already has (so we don't rebuild it)

Grounding the gaps requires being honest about the strong base:

- **Transport-agnostic handlers + one middleware pipeline** across AWS Lambda, Azure Functions,
  Azure self-hosted (Service Bus / Event Hub), Google Cloud, gRPC, ASP.NET Core, and self-hosted
  workers (Kafka / HTTP). This is the moat — most gaps below are *one middleware away* precisely
  because this exists.
- **Authentication**: `Benzene.Auth.OAuth2` (JWT bearer via JWKS, `OAuth2BearerMiddleware`),
  `Benzene.Auth.Basic`, `Benzene.Auth.Core`.
- **Authorization**: **scope-based only** — `ScopeClaims` + `RequireScope` (see
  `docs/cookbooks/auth-patterns.md`). No roles/policies/permissions/resource checks.
- **Validation**: `Benzene.FluentValidation`, `Benzene.DataAnnotations`, `Benzene.JsonSchema`.
- **Serialization**: Json (System.Text.Json), `Benzene.NewtonsoftJson`, `Benzene.Avro`,
  `Benzene.MessagePack`, `Benzene.Xml`; content negotiation via `AddMediaFormatNegotiation`.
- **Caching**: `Benzene.Cache.Core` + `Benzene.Cache.Redis`.
- **Resilience**: `Benzene.Resilience` — **retry only** (`RetryMiddleware`). No circuit breaker,
  timeout, bulkhead, or fallback.
- **Observability**: `Benzene.OpenTelemetry` (exporter-agnostic traces/metrics), `Benzene.Diagnostics`,
  correlation IDs, W3C trace-context propagation on outbound `Clients`.
- **Health / ops**: `Benzene.HealthChecks` (+ EntityFramework, Http), Kubernetes readiness/liveness
  via `Benzene.CloudService.Probe`.
- **API/versioning/spec**: `Benzene.Core.Versioning` (payload version casters), `Benzene.Schema.OpenApi`,
  `Benzene.Spec.Ui`, CORS via `Benzene.Http.Cors`.
- **Outbound / integration**: `Benzene.Clients` (+ `.Aws`, `Client.Http`) — currently mid-redesign
  (`benzene-clients-redesign-plan.md`).
- **Service visibility**: `Benzene.Mesh.*` — cross-service catalog, topology, contract-drift.
- **Codegen / DX**: Terraform, OpenAPI, Client SDKs, templates, source generators.

## 3. How to use this document

Each gap is a self-contained work package with a consistent template so we can pick them off one
at a time:

- **Requirement** — the enterprise need.
- **Current state** — what exists today, grounded.
- **The gap** — precisely what's missing.
- **Proposed work** — package name + shape + key types.
- **Vision fit** — which of `benzene-vision.md` §2's tests it passes.
- **Depends on / sequencing.**
- **Status** — tracked in the table in §7.

Tiering: **Tier 1** = enterprise-blocking and a natural pipeline fit (do first); **Tier 2** =
operational maturity that broadens fit; **Tier 3** = completeness/polish.

---

## Tier 1 — enterprise-blocking, natural fit for the pipeline

### T1.1 — Multi-tenancy (`Benzene.MultiTenancy`)

- **Requirement.** Every request/message is attributable to a tenant; data, config, and resources
  are isolated per tenant; a missing/ambiguous tenant is a hard failure, not a silent default.
- **Current state.** None. No `ITenantContext`, tenant resolver, or tenant middleware exists
  (verified). Tenant identity today can only be re-derived ad hoc inside each handler from a JWT
  claim or header.
- **The gap.** Tenant is a cross-cutting concern re-implemented (and liable to drift) per handler —
  the exact failure mode the vision exists to prevent — and there is no enforced isolation story.
- **Proposed work.** A tenant-resolution middleware (strategies: JWT claim, header, subdomain/host,
  explicit) that populates a scoped `ITenantContext`; the context flows through the pipeline and
  into outbound `Clients`; tenant-scoped DI and per-tenant configuration/connection-string
  resolution; a "tenant required" guard middleware. Optional: per-tenant cache key prefixing (ties
  to `Benzene.Cache.Core`) and per-tenant data-partition helpers.
- **Vision fit.** Passes all five — transport-agnostic, cross-cutting → shared pipeline, thin
  adapters, portable. This is the headline gap given Benzene's own origin.
- **Depends on / sequencing.** Foundational; several later items (rate limiting, audit, config,
  cache) become "per-tenant" once this exists. **Do first.**

### T1.2 — Idempotency / exactly-once effect (`Benzene.Idempotency`)

- **Requirement.** A message delivered more than once (the norm for every async transport Benzene
  supports) must not apply its effect twice; ideally the original result is replayed.
- **Current state.** None. SQS, Service Bus, Event Hubs, and Kafka are all at-least-once; the ack
  modes added to the consumer packages control *redelivery*, not *deduplication*. No idempotency
  key or dedup store exists (verified).
- **The gap.** Correct-once processing is left entirely to each handler, which is both hard and
  duplicated everywhere.
- **Proposed work.** Idempotency-key extraction (from a header/message property, or a
  deterministic hash of topic+body) + a pluggable idempotency store (`Redis`, `DynamoDb`, `SQL`)
  as middleware: first-seen keys proceed and record their result; duplicates short-circuit and
  replay the recorded outcome. Configurable retention and in-flight locking.
- **Vision fit.** Cross-cutting → shared pipeline; transport-agnostic (works for any at-least-once
  adapter). Passes all five.
- **Depends on / sequencing.** Independent; pairs with the async reliability work already shipped.
  **Do second.**

### T1.3 — Authorization depth (extend `Benzene.Auth`)

- **Requirement.** Enforce "may *this* principal perform *this* action on *this* resource" — roles,
  policies, and resource/attribute-based checks — declaratively per handler.
- **Current state.** Scope-only: `ScopeClaims` + `RequireScope`. No roles, no policy engine, no
  resource/ABAC, no per-handler permission attribute (verified — `Benzene.Auth.Core` is just
  `AuthResults` + `AuthenticationHolder`).
- **The gap.** Scope checks alone don't clear an enterprise security review; there's no
  `[RequirePermission("orders:write")]`-style declarative authZ, and no way to author reusable
  policies or resource-owner checks.
- **Proposed work.** A policy/permission model with pluggable policy handlers, a
  `[RequirePermission]`/`[RequirePolicy]` handler attribute set, role-claim support, and a
  resource-based check hook (`IAuthorizationHandler<TResource>`), transport-agnostic — the
  conceptual analogue of ASP.NET Core `[Authorize(Policy=...)]` without the HTTP coupling.
- **Vision fit.** Cross-cutting → shared pipeline; keeps handlers transport-agnostic. Passes all
  five.
- **Depends on / sequencing.** Independent of tenancy but far stronger with it (tenant-scoped
  permissions). **Do third.**

### T1.4 — Transactional outbox (`Benzene.Outbox`)

- **Requirement.** Persisting business data and publishing the resulting event must be atomic — no
  lost events, no phantom events (the dual-write problem).
- **Current state.** None. `Benzene.Clients` publishes outbound with no transactional tie to a DB
  write (verified — no outbox implementation exists; only a DynamoDb-streams *plan* doc).
- **The gap.** In an event-driven enterprise this is a correctness hole: a crash between DB commit
  and publish either drops an event or (with publish-first) emits one for work that rolled back.
- **Proposed work.** An outbox store written inside the same transaction as business data, plus a
  relay (polling and/or CDC — DynamoDb Streams, SQL CDC, Postgres logical replication) that
  publishes pending rows through `Benzene.Clients` and marks them sent. Ships with at least one
  store (EF/SQL) and one relay.
- **Vision fit.** A reliability primitive for the outbound side; complements the Clients redesign
  (`benzene-clients-redesign-plan.md`). Portable across stores.
- **Depends on / sequencing.** Best after the Clients redesign settles; pairs with T2.4
  (unit-of-work). **Do fourth.**

---

## Tier 2 — operational maturity, broadens fit

### T2.1 — Secrets & dynamic configuration (`Benzene.Configuration.*`)

- **Requirement.** Secrets never live in files/env in plaintext; config is validated at startup and
  can change safely at runtime.
- **Current state.** Relies on raw `IConfiguration`/environment variables; no first-class secret
  providers, startup validation, or reload (verified).
- **Proposed work.** Configuration-provider packages for Azure Key Vault, AWS Secrets Manager, AWS
  SSM Parameter Store, and Azure App Configuration; a startup config-validation hook (fail fast on
  missing/invalid settings); optional hot-reload with change notification into the pipeline.
- **Vision fit.** Cross-cutting; provider-per-cloud but behind a neutral abstraction, so portability
  holds.

### T2.2 — Rate limiting & quotas (`Benzene.RateLimiting`)

- **Requirement.** Per-client and **per-tenant** request/throughput limits so one consumer can't
  starve others.
- **Current state.** None (verified).
- **Proposed work.** A limiter middleware (token-bucket / fixed / sliding window) with a pluggable
  counter store (in-memory / Redis), keyed by tenant, client, or custom dimension; standard
  `429`/throttle result mapping across transports.
- **Vision fit.** Cross-cutting → shared pipeline; becomes "per-tenant" once T1.1 lands.
- **Depends on.** Strongly complements T1.1.

### T2.3 — Audit trail (`Benzene.Audit`)

- **Requirement.** A structured, queryable, tamper-evident "who did what, when, to what" record —
  distinct from application logging.
- **Current state.** None dedicated (verified). Logging/diagnostics exist but are not an audit
  trail.
- **Proposed work.** An audit middleware capturing principal + tenant + action + resource + outcome
  into a pluggable sink (append-only store / event stream); redaction-aware; opt-in per handler or
  policy.
- **Vision fit.** Cross-cutting → shared pipeline. Stronger with T1.1 (tenant) and T1.3 (principal
  detail).

### T2.4 — Unit-of-work / transaction boundary (`Benzene.Data` or extend Core)

- **Requirement.** Consistent commit/rollback semantics per message, and conventional data access
  so every service doesn't reinvent it.
- **Current state.** None. Only `Benzene.HealthChecks.EntityFramework` touches data; there's no
  transaction-scope middleware or repository/UoW convention (verified).
- **Proposed work.** A per-message transaction-scope middleware (begin on entry, commit on success,
  roll back on failure) with EF Core and Dapper adapters; a light repository/UoW convention. Pairs
  naturally with T1.4 (outbox written in the same transaction).
- **Vision fit.** Sits in the pipeline; keeps persistence a port behind the handler.

### T2.5 — Long-running workflows / sagas

- **Requirement.** Coordinate multi-step business processes spanning several messages/services with
  compensation on failure.
- **Current state.** None; Benzene is stateless-handler-shaped (verified).
- **Proposed work.** Two options to weigh: (a) a lightweight saga/process-manager package (state +
  correlation + compensation) or (b) **first-class integration** with an existing orchestrator
  (Temporal, Azure Durable Functions, AWS Step Functions) exposing steps as Benzene handlers.
  Recommendation: start with (b) — orchestration is a deep problem and integration respects the
  "thin adapter" principle better than building an engine.
- **Vision fit.** Needs care — a bespoke engine risks pushing logic out of handlers; integration is
  the safer read of the vision. Flag for an explicit design discussion.

---

## Tier 3 — completeness / polish

- **T3.1 — Pagination** (first-class result concern). Confirmed absent. A `IPagedResult<T>` +
  cursor/offset convention + transport response mapping, so list endpoints are consistent.
- **T3.2 — Feature flags** via **OpenFeature** — a provider-neutral flag evaluation hook in the
  pipeline (tenant-aware once T1.1 lands). None today.
- **T3.3 — Schema registry integration** for event contracts (Confluent / Azure Schema Registry) —
  complements `Benzene.Avro` and Mesh contract-drift; there's registry-less schema handling today
  but no registry client.
- **T3.4 — Consumer-driven contract testing (Pact)** + promote `Benzene.Mesh` contract-drift into a
  CI gate — turn the existing drift detection into an enforce-before-merge check.
- **T3.5 — PII redaction / data-governance middleware** — enforce the existing
  `docs/privacy-and-data-handling.md` policy (tag + redact PII in logs/audit/traces). Doc exists;
  enforcement doesn't.
- **T3.6 — Delegated identity / token exchange (on-behalf-of)** in `Benzene.Clients` — propagate the
  caller's identity to downstream service calls (OAuth2 token exchange / OBO). Outbound clients
  forward correlation + trace context today, but not identity.
- **T3.7 — Resilience depth** — extend `Benzene.Resilience` (retry-only today) with circuit
  breaker, timeout, bulkhead, and fallback, so the outbound/handler path degrades predictably.

---

## 7. Status board (tackle one by one)

| ID | Item | Tier | Proposed package | Status |
|----|------|------|------------------|--------|
| T1.1 | Multi-tenancy | 1 | `Benzene.MultiTenancy` | Not started |
| T1.2 | Idempotency / exactly-once | 1 | `Benzene.Idempotency` | Not started |
| T1.3 | Authorization depth | 1 | extend `Benzene.Auth` | Not started |
| T1.4 | Transactional outbox | 1 | `Benzene.Outbox` | Not started |
| T2.1 | Secrets & dynamic config | 2 | `Benzene.Configuration.*` | Not started |
| T2.2 | Rate limiting & quotas | 2 | `Benzene.RateLimiting` | Not started |
| T2.3 | Audit trail | 2 | `Benzene.Audit` | Not started |
| T2.4 | Unit-of-work / transactions | 2 | `Benzene.Data` | Not started |
| T2.5 | Workflows / sagas | 2 | integration (Temporal/Durable/Step Fns) | Not started — design first |
| T3.1 | Pagination | 3 | Core | Not started |
| T3.2 | Feature flags (OpenFeature) | 3 | `Benzene.FeatureFlags` | Not started |
| T3.3 | Schema registry | 3 | `Benzene.SchemaRegistry.*` | Not started |
| T3.4 | Contract testing (Pact) + Mesh CI gate | 3 | `Benzene.Mesh.*` / testing | Not started |
| T3.5 | PII redaction / governance | 3 | `Benzene.Privacy` | Not started |
| T3.6 | Delegated identity / token exchange | 3 | extend `Benzene.Clients` | Not started |
| T3.7 | Resilience depth | 3 | extend `Benzene.Resilience` | Not started |

## 8. Recommended sequence

**Multi-tenancy → Idempotency → Authorization depth → Transactional outbox**, then Tier 2 as
operational needs dictate. Rationale: tenancy unlocks the B2B-SaaS story and makes several Tier 2
items "per-tenant" for free; idempotency + outbox make the at-least-once async transports
trustworthy end to end; authorization depth clears the hard security-review bar. Everything in
Tier 1 is a cross-cutting concern that lives in the one shared pipeline — the highest-leverage,
most on-vision place to invest.

## 9. Notes on scope discipline

Two items deserve an explicit design conversation *before* building, because they can pull logic
out of handlers and work against the vision if done naively:

- **T2.5 (sagas)** — prefer integrating an orchestrator over building an engine.
- **T2.4 (unit-of-work)** — keep persistence a port; the middleware manages the *boundary*, not the
  data access itself.

Everything else in Tiers 1–3 is a clean cross-cutting-concern or reliability primitive that fits
Benzene's model directly.
