# Benzene — Consolidated Cross-Port Capability Matrix

**Full refresh: 2026-08-20** (first build). **Column refresh: 2026-08-20 — .NET only**:
benzene-dotnet's matrix was extended with area rows (plus JWKS/OIDC and Azure Blob detail clauses)
and the .NET column re-read from it; the Go, TypeScript and Python columns are unchanged from the
full refresh (their matrices have not changed). Ports surveyed: **.NET**, **Go**,
**TypeScript**, **Python** — all four from their own `docs/capability-matrix.md` (the
capability-scribe's output in each port repo). This document is the **capability record** of the
estate (see [documentation-lifecycle.md](documentation-lifecycle.md)): descriptive, not normative.
The spec (`docs/specification/**`) defines what a port *must* do; this page records what each port's
own matrix *says* it does.

**How cells are filled.** A cell comes only from that port's matrix — never from its source code,
and never from another port's matrix (one port's claim about a sibling is noted, not trusted). A
matrix that doesn't address an area yields **unknown**, which is itself a finding: that repo owes a
capability-scribe run. Cell vocabulary:

| Cell | Meaning |
|---|---|
| **yes** | Shipped, with the package/module the port's matrix names |
| **no — deliberate** | Absent by stated design decision, with the port's own reason |
| **no — unbuilt** | Absent, and the port's matrix says so plainly with no design reason |
| **partial** | Shipped in part — one clause says which half |
| **unknown** | The port's matrix does not say |

## The matrix

| Capability | .NET (reference) | Go | TypeScript | Python |
|---|---|---|---|---|
| **Core pipeline & routing** | **yes** — topic-based dispatch to `[Message]` handlers, middleware pipeline (core package not named in matrix) | **yes** — root module (`Registry`, `Pipeline`, `App`); explicit registration by design, no attribute scanning | **yes** — `Benzene.Core*`, `Benzene.Abstractions*`, `Benzene.Results` (`@message` routing) | **yes** — `benzene-core` + `benzene-results` (`@message`, middleware, per-invocation DI) |
| **HTTP (inbound + outbound)** | **yes** — inbound HTTP/API Gateway (inbound package not named in matrix); outbound `Benzene.Clients.Http` | **yes** — `httpbinding` (mountable `http.Handler`) + `httpclient`; portable CORS | **yes** — `Benzene.Http` on Express / API Gateway / Azure Functions / GCF; outbound `Benzene.Clients.Http` (injectable `fetch`) | **yes** — `benzene-http` (ASGI app + status mapping + client) |
| **gRPC** | **yes** — `Benzene.Grpc` (all four RPC shapes incl. streaming, `[GrpcMethod]` routing) + `.AspNet`, `.Client`, `.Versioning`, test helpers; response-side payload downcasting over gRPC deliberately not shipped | **partial** — `grpcbinding` (unary interceptor, `benzene-status` trailer, client); streaming not implemented (documented gap) | **yes** — `Benzene.Grpc` (incl. streaming) + `Benzene.Grpc.Client` | **yes** — `benzene-grpc` (status mapping + server + client; transport SDK an optional extra) |
| **Kafka (self-hosted worker)** | **yes** — `Benzene.Kafka.Core` | **yes** — `kafka` module (consumer-group consumer + client) | **yes** — `Benzene.Kafka.Core` | **yes** — `benzene-kafka` (SDK duck-typed/optional) |
| **RabbitMQ** | **yes** — `Benzene.RabbitMq` (Explicit ack default) | **yes** — `rabbitmq` module (consumer + client) | **yes** — `Benzene.RabbitMq` (+ test helpers; no dedicated doc page — stated omission) | **yes** — `benzene-rabbitmq` (topology deliberately yours) |
| **AWS** | **yes** — Lambda SQS/SNS/EventBridge/Kafka(MSK)/S3/Kinesis/DynamoDB Streams; self-hosted SQS (`Benzene.Aws.Sqs`); Step Functions client | **yes** — `awslambda`, `awssqs`, `awssns`, `awseventbridge`, `awss3`, `awsdynamodb`, `awskinesis`, `awskafka`, `awslambdaclient`, `awsstepfunctions`; API Gateway custom authorizer and direct X-Ray SDK not implemented (OTel/ADOT is the stated route) | **yes** — `Benzene.Aws.Lambda.*` (incl. X-Ray), `Benzene.Aws.Sqs`, `Benzene.Clients.Aws.*` | **yes** — `benzene-aws` (Lambda host incl. all major triggers; outbound senders; self-hosted SQS consumer; test hosts) |
| **Azure** | **yes** — Function triggers Service Bus/Kafka/Event Grid/Event Hub/Queue Storage/Cosmos DB **+ Blob** (`Benzene.Azure.Function.BlobStorage`, deliberately non-routed: delivers blob name + content to `UseBlob(...)`); self-hosted Service Bus + Event Hub workers | **partial** — `azurefunctions` (HTTP, Queue/Service Bus, Cosmos, Timer, Event Grid, Event Hub) + self-hosted workers + clients; **Blob trigger deferred by design** (SDK-typed, no fabricated custom-handler shape), **Functions Kafka trigger not implemented** (payload unpinned) | **yes** — `Benzene.Azure.Function.*` (incl. Blob, Kafka, Timer), self-hosted workers, `Benzene.Clients.Azure.*` | **yes** — `benzene-azure` (incl. Blob inbound — a binding some sibling ports defer; the port ships it) |
| **GCP** | **partial** — matrix names only `Benzene.GoogleCloud.Functions.PubSub` | **yes** — `gcpfunctions` (HTTP + CloudEvent), `gcppubsub` (zero-dep push), `gcppubsubclient`; pull subscriptions deliberately off the zero-dep path | **yes** — `Benzene.GoogleCloud.Functions.*` + Pub/Sub client | **yes** — `benzene-gcp` (HTTP + Pub/Sub); Eventarc/Cloud Tasks plainly unbuilt |
| **Mesh — service side** | **yes** — `Benzene.Mesh.Wire` (descriptor from live registry incl. `consumes` + `descriptorHash`, trace middleware + batching HTTP exporter, deduplicating issue emitter) + `Benzene.Mesh.Reporting` push self-report; every feed opt-in by design | **yes** — `mesh/` (descriptor from live registry, trace middleware + exporters, issue emitter) | **yes** — `Benzene.Mesh.Wire/Contracts/Dispatch`, `MeshAnnouncer`, per-platform sources | **yes** — `benzene-mesh` (descriptor, feeds, trace middleware) |
| **Mesh — collector** | **yes** — `Benzene.Mesh.Collector` (in-memory store, bounded trace ring; durable storage deliberately out — the aggregator's published catalog artifacts are the durable output) + fleet/tracing/usage backends, aggregator + discovery + artifact stores, gated live dispatch, OIDC login gate | **yes** — `meshd` (in-memory, bounded trace ring; version-skew read model not implemented; durable storage deliberately out) | **yes** — `Benzene.Mesh.Collector` + aggregator/reporting/discovery/fleet/usage/storage packages | **yes** — `benzene-mesh` collector + poller + containerised deploy; in-memory store by design |
| **Mesh — UI** | **yes** — `Benzene.Mesh.Ui` serves the **canonical** `mesh-ui.html` (byte-identical embedded copy, CI drift-checked against the benzene-ui build) + `mesh-spec-ui.html` | **yes** — `meshd.ViewHandler` serves a **Go-native** `view.html` (deliberately *not* the canonical `mesh-ui.html`; the wire contract is the interop point) | **yes** — `Benzene.Mesh.Ui` (matrix does not say whether it serves the canonical `mesh-ui.html` or its own) | **yes** — serves the **canonical** `mesh-ui.html` vendored from the main repo, at `/mesh-ui/` |
| **Health checks** | **yes** — `Benzene.HealthChecks*` reserved-topic middleware (`healthcheck`/`liveness`/`readiness`/`contracts`, deliberately non-overlapping) + disk/HTTP/TCP/DynamoDB/EF/schema/Service Bus checks, client-side downstream checks, grpc.health.v1 bridge; a dedicated `/health` HTTP route deliberately not shipped (health is a topic) | **yes** — `healthcheck/` (reserved-topic middleware + TCP/HTTP/disk checks, category-only errors) + `clienthealthcheck` contract probe | **yes** — `Benzene.HealthChecks*` (disk/HTTP/TCP/DynamoDB/TypeORM/schema/Service Bus + transport-embedded + client-side) | **yes** — `benzene-core` `health.py` + `/benzene/health` |
| **Spec endpoint & Cloud Service Profile** | **yes** — `Benzene.Schema.OpenApi` (derived spec on the `spec` topic in native/OpenAPI 3.0/AsyncAPI 3.0) + `Benzene.Spec.Ui`; `Benzene.CloudService` wires R1–R8 in one call with an honest `CloudServiceProfileReport` (deliberately sugar-only); `Benzene.CloudService.Probe` (external black-box R1–R8 probe); `Benzene.Descriptor` tool | **yes** — `mesh.SpecHandler` (R5), `cloudservice.New` profile builder with honest R1–R8 report (R6/R8 not auto-wired, stated), `cloudserviceprobe` | **yes** — `Benzene.CloudService` + `Benzene.Spec.Ui` + `Benzene.CloudService.Probe` | **yes** — `spec.py` (registry-derived, `/benzene/spec`); R1–R8 mapping in the port's `cloud-service-profile.md` |
| **Codegen & typed clients** | **yes** — `Benzene.CodeGen.Core`/`.Client` (schema closure, canonical JSON + contract hash), the `benzene` CLI, MSBuild build-time generation (`Benzene.CodeGen.Build`), plus `Benzene.Clients` with HTTP/InProcess/AWS/Azure/GCP transport clients; `Benzene.CodeGen.Terraform` deliberately unshipped pending its grow-or-split call | **yes** — `codegen` module (contract-document clients) + `openapi`/`asyncapi` generators (AsyncAPI send side caller-declared; deriving it not implemented) | **yes** — `Benzene.CodeGen.Client` (CLI), `Benzene.Clients` (routing/retries/fan-out), `Benzene.Clients.InProcess` | **yes** — `benzene-codegen-client` (CLI, schema closure, RFC 8785 hashing) + `benzene-openapi` |
| **Settlement of returned failure results** | **yes** — the **1.0 settlement contract** (maintainer-approved 2026-07-21): every queue-shaped transport safe by default (`RaiseOnFailureStatus`/ack-mode defaults), stream workers deliberately at-most-once; guarded by `SettlementContractDefaultsTest` | **unknown/partial** — per-row hints only (SQS batch-item failures; SNS/EventBridge/S3 escalate as Go errors; Kafka skip-and-continue by design); no consolidated settlement statement for returned-failure-results across all transports | **partial** — safe on SQS, DynamoDB Streams, Queue Storage, Event Grid, Pub/Sub, RabbitMQ, Service Bus worker; **knob defaults off** on SNS / Service Bus trigger / Azure Kafka trigger; **no knob at all** on EventBridge / S3 / MSK / Kinesis / Event Hub trigger — matrix states plainly the .NET 1.0 contract "is not yet fully ported" | **unknown** — matrix has no settlement/ack-defaults section at all |
| **Outbox** | **yes** — `Benzene.Outbox` (+ `.DynamoDb`, `.EntityFramework` stores); at-least-once only, exactly-once and cross-envelope ordering deliberately out of scope | **no — unbuilt** (stated plainly; only trace is the `responseevents.Publisher` seam) | **no — deliberate** — "writing the outbox row inside *your* DB transaction is application territory"; documented seams (`IResponseEventPublisher`, `IUnitOfWork`) + step-by-step cookbook; matrix itself flags this as an honest divergence from .NET, not parity | **no — unbuilt** (stated plainly; `ResponseEventSink` can feed one you build) |
| **Claim check (oversized payloads)** | **yes** — `Benzene.ClaimCheck` + `.Aws.S3`, `.Azure.Blob` stores; `benzene-claim-check` header contract | **no — unbuilt** (no package, no seam; stated plainly) | **no — unbuilt** ("no stated design reason — it is simply unbuilt") | **no — unbuilt** (stated plainly) |
| **Idempotency** | **yes** — `IIdempotencyStore` + in-memory store + atomic-claim middleware | **yes** — `idempotency/` (same shape; fails open on store outage) | **yes** — `Benzene.Idempotency` (same shape) | **yes** — `benzene-resilience` `idempotency.py` (same shape) |
| **Resilience** | **yes** — `UseRetry` (jitter, max-delay cap) + `UseTimeout` + full Polly via `Benzene.Resilience.Polly` | **yes** — `resilience/` (retry, cooperative timeout, bulkhead, fallback) + `circuitbreaker` module; hedging not implemented (pipeline-contract question, stated) | **yes** — `RetryMiddleware` (no jitter/max-delay cap — stated as not implemented) + full Cockatiel via `Benzene.Cockatiel` | **yes** — `with_retry` decorators + `benzene-resilience` (circuit breaker, bulkhead) |
| **Rate limiting** | **no — deliberate** — listed with the policy-engine adapter as things Benzene deliberately does not ship | **yes** — `ratelimiting/` (per-instance; fleet-wide deliberately at the gateway) | **yes** — `Benzene.RateLimiting` (fixed-window, token-bucket, concurrency, payload-size; distributed deliberately out) | **yes** — in `benzene-resilience` |
| **Caching** | **yes** — `Benzene.Cache.Core` (cache-aside/write-through abstractions + cache health check) + `Benzene.Cache.Redis`; cache *middleware* deliberately not shipped (what/when to cache is a handler-level decision) | **yes** — `cache/` (pluggable `Store`, read-through, safe degradation) | **yes** — `Benzene.Cache.Core` + `Benzene.Cache.Redis` | **yes** — `benzene-cache` (cache-aside; memory + Redis) |
| **Validation** | **yes** — `Benzene.FluentValidation`/`.DataAnnotations`/`.JsonSchema` adapters over one shared short-circuit failure contract (`Benzene.Abstractions.Validation`); deliberately no validation DSL of its own | **yes** — `validation/` typed handler wrapper (deliberately not a reflection DSL or pipeline middleware) | **yes** — Zod/Joi/Yup/ajv adapter packages over a shared abstraction | **yes** — `benzene-pydantic` (optional adapter; core never imports it) |
| **Versioning (handler/payload)** | **yes** — both spec mechanisms: `(topic, version)` dispatch via pluggable `IVersionSelector`, default `VersionSelector` = **exact match, else highest available** (see divergence note 8); payload up- *and* downcasting with auto-composed chains (`Benzene.Core.Versioning`, serializer-agnostic) + request-side gRPC casting | **partial** — header-fallback + HTTP route-segment resolution, **exact-match-only** selection with unversioned fallback; payload casting and exact-else-highest deliberately not shipped, citing an unsettled spec self-contradiction (see divergence note 8) | **yes** — `Benzene.Core.Versioning` (payload casting for requests, responses, schemas) | **yes** — `(topic, version)` registration; pluggable selector, `exact_version` default, `highest_version` available |
| **Serialization beyond JSON** | **yes** — `Benzene.Xml`, `Benzene.MessagePack`, `Benzene.Avro` (+ `Benzene.NewtonsoftJson`) via content negotiation; binary formats Base64-armored (string-body wire contract — a stated boundary, not an accident) | **no — deliberate** — `encoding/json` is idiomatic; Avro/MessagePack would be new dependency decisions, not ports | **yes** — `Benzene.Xml`, `Benzene.MessagePack`, `Benzene.Avro` | **unknown** — matrix has no serialization row (JSON envelope implied) |
| **Schema registry / Confluent codec** | **yes** — Confluent wire-format codec + `ISchemaRegistryClient` seam (in-box compat checker textual-only, deliberate) | **unknown** — matrix does not address the registry codec (nearest statement: the deliberate no on alternate serializers) | **yes** — `Benzene.SchemaRegistry.Core` (same textual-only checker boundary) | **no — unbuilt** (stated plainly; Kafka binding carries plain envelopes) |
| **Sagas / workflows** | **yes** — `Benzene.Saga` (in-process, LIFO compensation; durable crash-resume deliberately out — use a real orchestrator) | **yes** — `saga/` (same shape and same stated boundary) | **yes** — `Benzene.Saga` (same shape and boundary) | **yes** — `benzene-resilience` `saga.py` (same shape and boundary) |
| **AuthN / AuthZ** | **yes** — `Benzene.Auth.OAuth2` (OAuth2 bearer JWT, strict algorithm allowlist, scope authorization); keys via **full OIDC discovery (`Authority`) or bare JWKS (`JwksUri`)**, cached with refresh-on-unrecognized-`kid` | **yes** — `auth/` (Basic + Bearer JWT: RFC 8725 allowlist, `StaticKeys` **or caching JWKS with OIDC discovery**; roles/scopes) | **yes** — `Benzene.Auth.OAuth2` (`jose`, required no-default allowlist, scopes) + `Benzene.Auth.Basic`; matrix does not say whether JWKS/OIDC discovery is included | **partial** — Basic + bearer JWT against a **configured static key only**; JWKS/OIDC discovery **not implemented** (stated as unbuilt, not declined) |
| **Distributed tracing / observability** | **yes** — W3C `traceparent` inbound on HTTP + async transports; OTel, exporter-agnostic | **yes** — `diagnostics` module (OTel API only) + `logging/` (`slog`); SDK/exporter deliberately app-owned | **yes** — `W3CTraceContextMiddleware` + `Benzene.Diagnostics`; OTel, exporter-agnostic | **yes** — `trace_middleware` (mesh) + `benzene-otel` exporter (SDK optional) |
| **Secrets & configuration** | **yes** — `ISecretStore` (env/files/composed/cached) + fail-fast validation; cloud adapters deliberately cookbook-only | **unknown** — matrix does not cover it | **yes** — `Benzene.Configuration.Core` (same shape and same cloud-adapter stance) | **unknown** — matrix does not cover it |
| **Database / state access** | **no — deliberate** — a database is not a transport; wrapping one hides its capabilities | **no — deliberate** — same reason, verbatim stance | **no — deliberate** — same | **no — deliberate** — same |
| **Conformance fixtures (vendored)** | **yes** — `test/Benzene.Conformance.Test` runs the vendored `SPEC_VERSION`-stamped snapshot (envelope, HTTP/gRPC status maps, status vocabulary, problem details, contract hash, mesh shapes); `conformance-drift-check.yml` re-verifies the snapshot against this repo | **yes** — `conformance/` runs the vendored fixtures from this repo | **unknown** — matrix does not mention them | **unknown** — matrix does not mention them |

## Divergence notes

Grades: *deliberate* (stated per-port design decision), *staged* (a later port hasn't got there —
normal), **drift** (contradiction about a shared contract — the finding that matters). Descriptive
only; decisions stay with the product owner.

1. **Outbox — mixed: deliberate (TS) + staged (Go, Python).** .NET ships `Benzene.Outbox` with
   store packages. TypeScript states a design position (outbox row belongs in the application's own
   transaction) with supported seams and a cookbook — and its own matrix flags this as "an honest
   divergence from the .NET port", not parity. Go and Python say "not implemented" plainly. No port
   papers over it, so this is not drift — but it is the largest capability fork in the estate.
2. **Claim check — staged.** .NET ships `Benzene.ClaimCheck` (+ S3/Blob stores); Go, TypeScript and
   Python all say unbuilt, plainly, with no design reason. The `benzene-claim-check` header contract
   currently exists only in .NET; it is documented in the .NET port, not in `docs/specification/`.
3. **Settlement of returned failure results — staged, safety-relevant.** .NET's matrix carries the
   1.0 settlement contract (queue-shaped transports safe by default, test-guarded). TypeScript
   states plainly it has not caught up: three adapters default the escalation knob off (SNS, Service
   Bus trigger, Azure Kafka trigger) and five have no knob at all (EventBridge, S3, MSK, Kinesis,
   Event Hub trigger) — on those, a returned failure result is silently settled unless the handler
   throws. Go's and Python's matrices don't state their settlement defaults at all (unknown — see
   scribe debts). The stream-worker exception (Kafka/Event Hub self-hosted default at-most-once) is
   *deliberate and aligned* everywhere it is stated (.NET, TS, Go's Kafka row). **Spec gap:** this
   maintainer-approved cross-port behavioral contract lives only in benzene-dotnet's docs; per
   AGENTS.md, an observable contract belongs in `docs/specification/**`.
4. **Auth — staged (Python) + one unknown (TS).** .NET and Go both state the full key-resolution
   surface: cached JWKS with full OIDC discovery (.NET: `Authority` or bare `JwksUri`, both with
   refresh-on-unrecognized-`kid`; Go: `StaticKeys` or caching JWKS with OIDC discovery). Python is
   explicitly partial (static-key JWT only; JWKS/OIDC stated as unbuilt — staged). Only the
   TypeScript matrix still doesn't say whether its OAuth2 package does JWKS/OIDC discovery — an
   unknown detail, not evidence either way.
5. **Mesh UI — deliberate (Go).** .NET and Python both serve the **canonical** `mesh-ui.html`
   (.NET as a byte-identical embedded copy with a CI drift-check against the benzene-ui build;
   Python vendored from this repo); Go deliberately serves its own zero-dependency `view.html` and
   documents that the shared `benzene:mesh:query:*` wire contract, not the page, is the interop
   point — a stance .NET's matrix states in the same terms. TypeScript ships `Benzene.Mesh.Ui` but
   its matrix still doesn't say which asset it serves.
6. **Schema registry — staged (Python) + unknown (Go).** .NET and TypeScript ship the Confluent
   codec + registry seam with the identical textual-only-checker boundary; Python says unbuilt
   plainly; Go's matrix doesn't address it directly.
7. **Rate limiting — deliberate (.NET).** The reference port deliberately does not ship
   rate-limiting middleware; Go, TypeScript and Python all do (each deliberately scoping out
   fleet-wide/distributed limiting). A rare case of the reference doing less than every other port,
   stated on both sides.
8. **Versioning selection — drift (at spec level), now with the reference implementation on one
   side.** Go ships exact-match-only selection and states why: `core-concepts.md` §2 ("selected
   only by an exact version match") and `versioning.md` §4 (default selector "exact match, else
   highest available version") disagree, and Go deliberately ships the conservative selector until
   it's settled. Verified against the spec at the full refresh: the contradiction is real. **New
   this refresh:** the .NET matrix now states its default — `VersionSelector` uses the requested
   version on an exact match, **else the highest available** (strict exact-only offered as a
   swap-in). So the reference implementation's default matches `versioning.md` §4 and contradicts
   `core-concepts.md` §2 — evidence for the record, not a resolution; which clause the spec keeps
   is still the product owner's call. Python defaults to exact with a pluggable `highest_version`;
   TypeScript ships payload casting. The port-level differences are deliberate/staged, but the
   spec contradicting itself about a shared contract remains drift — for the product owner to
   settle in `docs/specification/`.
9. **Azure Blob trigger & Functions Kafka trigger — deliberate (Go).** Go defers Blob (won't
   fabricate an unverifiable custom-handler shape) and hasn't pinned the Kafka trigger payload;
   TypeScript and Python ship Blob; .NET now states its position too: it **ships** the Blob
   trigger (`Benzene.Azure.Function.BlobStorage`) as a deliberately **non-routed** adapter —
   blob name + content delivered to `UseBlob(...)` rather than message routing, with
   throw-to-redeliver as the escape hatch. So three ports ship Blob (one non-routed by design)
   and Go's deferral is a deliberate solo position. gRPC streaming is similarly a documented Go
   gap (staged) where .NET (all four RPC shapes) and TypeScript ship it.
10. **.NET matrix coverage — scribe debt paid (resolved 2026-08-20).** At the full refresh the
    .NET matrix was production-concern-oriented and carried no area rows for gRPC, mesh (service
    side, collector, UI), health checks, the spec endpoint/profile, codegen, caching, validation,
    versioning, serialization, or conformance — twelve `unknown` cells in the reference column.
    The matrix has since been extended with area rows covering exactly those cells (plus the
    JWKS/OIDC and Blob-trigger detail clauses), and the resolution confirmed the earlier reading:
    every one was a **coverage gap, not a capability gap** — all twelve areas are **yes**, each
    with its packages named and its deliberate boundaries stated (e.g. sugar-only
    `Benzene.CloudService`, no `/health` HTTP route, no cache middleware, Base64-armored binary
    serialization). The only findings of substance the fill produced feed notes 8 (default
    version selector) and 9 (non-routed Blob trigger).

### Where the ports agree (worth recording)

- **Database/state access**: all four state the identical deliberate no ("a database is not a
  transport") — the core anti-pattern stance holds estate-wide.
- **Idempotency**: all four ship the same store-seam + atomic-claim shape and the same deliberate
  boundary (no cross-instance de-duplication).
- **Sagas**: all four ship in-process LIFO compensation with the same stated boundary (no durable
  crash-resume; use a real orchestrator).
- **Tracing**: all four join W3C trace context and export via the standard OTel API,
  exporter-agnostic.

## Spec-requirement flags (spec outranks)

Where `docs/specification/` requires a capability (Core, or the Cloud Service Profile R1–R8), a
non-**yes** cell is flagged here, not merely noted:

- **Profile surfaces (R3 health, R4 envelope, R5 derived spec, R6 mesh feeds, R8 propagation) —
  now attested by all four ports.** The former .NET flag is cleared: benzene-dotnet's extended
  matrix attests `Benzene.CloudService` (one-call R1–R8 wiring with an honest
  `CloudServiceProfileReport`) plus `Benzene.CloudService.Probe`, matching what
  [cloud-service-profile.md §5](specification/cloud-service-profile.md) already documented. No
  profile-surface flag remains open.
- **Core conformance (fixtures)** — .NET's matrix now attests running the vendored fixtures
  (`test/Benzene.Conformance.Test` + a drift-check workflow against this repo), joining Go. The
  TypeScript and Python matrices remain silent, so implementation-level Core conformance
  ([cloud-service-profile.md §5](specification/cloud-service-profile.md)) is still unattested for
  those two in this document.
- **Version selection** — the spec disagrees with itself (divergence note 8); until settled, no
  port's selector can be flagged against it.
- The settlement contract and the `benzene-claim-check` header are **not** in the spec today, so
  the TS/Go/Python gaps there are flagged only as divergence (notes 2–3), not spec violations —
  with the spec-gap observation recorded for the product owner.

## Unknown cells — who owes a capability-scribe run

| Repo | Unknown cells this run |
|---|---|
| benzene-dotnet | *(none — all entries from the full refresh resolved by the 2026-08-20 matrix extension; see divergence note 10)* |
| benzene-go | schema registry/Confluent codec; secrets & configuration; consolidated settlement defaults for returned failure results |
| benzene-typescript | conformance; which asset `Benzene.Mesh.Ui` serves; JWKS/OIDC discovery (detail) |
| benzene-python | settlement/ack defaults (no section at all); serialization; secrets & configuration; conformance |
