# Benzene — Consolidated Cross-Port Capability Matrix

**Full refresh: 2026-08-20** (first build). **Row refresh: 2026-10-11 (spec-0.2.0) — the Phase 1 rows, all four columns**: re-read from each port's matrix after its spec-0.2.0 catch-up (benzene-dotnet `9e48b35`, benzene-go `ef287d5`, benzene-typescript `c977776`, benzene-python `07b2bfe`; all four fixture snapshots pinned to `spec-0.2.0`). Rows re-read: settlement, versioning, health checks, spec endpoint & profile, validation, mesh UI, MCP, conformance; rows added: envelope over HTTP, default `/benzene/` paths, reserved topics, EventBridge `benzene-headers`, mesh service versions. Divergence notes 3 (settlement) and 8 (version selection) shrink to what still differs, the "settlement is not in the spec" flag closes, and notes 11–17 record the remaining known gaps. Every other row is unchanged since the refreshes below. **Row refresh: 2026-10-11 (spec-0.1.0, plan Phase 0.1) — settlement and conformance, all four columns**: the Go, TypeScript and Python matrices gained per-binding settlement audits read from source (spec-simplification-plan Phase 0.1), and the four ports' fixture snapshots were pinned to the `spec-0.1.0` release. **Column refresh: 2026-09-29 — .NET only**: the .NET port added
the MCP transport binding (`Benzene.Mcp`) and its matrix row; the other three columns for that row read
"unbuilt" because their matrices predate the binding (spec §2 entry added the same day). **Column refresh: 2026-08-20 — .NET only**:
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
capability-scribe run. Where a refresh confirms a reported divergence, or a matrix's claim that looks
stale, against a port's own source or config, the text says **aligner check** — a confirmation of a
finding, never the source of a capability claim. Cell vocabulary:

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
| **Envelope over HTTP (R4)** | **yes** — `Benzene.Http` `UseBenzeneMessage` at `/benzene/invoke`: HTTP 200 for every envelope it processed, whatever its status; 400 (problem+json) only for a body that is not an envelope; same pipeline and auth as the other transports; `BenzeneMessageHttpOptions.TopicFilter` restricts reachable topics (a rejected topic is a `not-found` envelope); the envelope-over-HTTP clients read the envelope whatever the HTTP status. **No asynchronous envelope entry point** (`envelope-async`) — the Lambda direct-invoke envelope handler is request/response only (stated) | **yes** — `httpbinding.EnvelopeHandler` (`/benzene/invoke`): 200 per processed envelope (`not-found`, `deleted`, pipeline `unauthorized` alike), 400 only for a non-envelope, inner problem without `status`; `WithTopicFilter`; `httpclient` reads the envelope whatever the status (`envelope.ToResult`). Async entry point: `awslambda.AsyncEnvelopeHandler` for `Event` invokes | **yes** — `useBenzeneMessage` (`src/Benzene.Http`) at `POST /benzene/invoke`: 200 per processed envelope, inner problem without `status`, 400 problem+json only for a malformed envelope; `topicFilter`; envelope clients read the envelope whatever the status. Async entry point: `BenzeneMessageLambdaHandler` with `invocationType: 'Event'` | **yes** — `benzene-http` `POST /benzene/invoke`: 200 per processed envelope, inner problem without `status`, 400 only for a non-envelope; `StandardPaths(invoke_topics=...)` restricts reachable topics. Async entry point: `AwsLambdaApp(invocation_type="Event")`. The matrix does not state the client-side read rule |
| **Default `/benzene/` paths (R7)** | **yes** — `/benzene/invoke`, `/benzene/spec`, `/benzene/health` (`Benzene.CloudService`), `/benzene/spec-ui` (default spec URL `/benzene/spec?type=benzene`), `/benzene/mesh-ui`, `/benzene/mcp`; each configurable | **yes** — `/benzene/invoke`, `/benzene/spec`, `/benzene/health` (`httpbinding.EnvelopePath`/`SpecPath`/`HealthPath`), `/benzene/fleet-ui` (`meshd.ViewPath`); no spec-ui, mesh-ui or MCP surface to place | **yes** — `/benzene/invoke`, `/benzene/spec`, `/benzene/health` (`useBenzeneCloudService`), `/benzene/spec-ui` (reading `/benzene/spec?type=benzene`), `/benzene/mesh-ui`; no fleet-ui or MCP surface | **yes** — `/benzene/invoke`, `/benzene/spec`, `/benzene/health`, mesh UI at `/benzene/mesh-ui/` (`mount=` to move it); the matrix names no spec-ui surface |
| **Reserved topics** (wire-contracts §7) | **yes** — `benzene:healthcheck`/`liveness`/`readiness` (+ the .NET-only `contracts`); `benzene:spec` (absent/empty/`{}` = the contract document; native, OpenAPI 3.0, AsyncAPI 3.0 × JSON/YAML; an unsupported `type` or `format` answers `not-implemented`); `benzene:ping` answered `ok`, no payload, by the router at any version (`DisablePing()` opts out); `benzene:mesh` + collector topics; further .NET-only `benzene:*` topics as port extensions | **partial** — `benzene:healthcheck` (no `liveness`/`readiness`), `benzene:ping` (`RouterMiddleware`, `ok`, no payload), `benzene:mesh` + collector topics; **`benzene:spec` not implemented** — `/benzene/spec` serves the mesh descriptor directly (see note 13) | **yes** — all three health topics; `benzene:spec` (`{type?, format?}`; `benzene`/`openapi`/`asyncapi` × `json`; anything else `not-implemented`; no YAML); `benzene:mesh`, the collector topics; `benzene:ping` answered `ok`, no payload, by the router when no handler claims it | **yes** — `benzene:healthcheck` (no `liveness`/`readiness`); `benzene:spec` (`spec_interception`: the contract document for an absent request or `type=benzene`, native `ServiceSpec` for `native`, otherwise `not-implemented`/HTTP 501); `benzene:mesh`, the collector topics; `benzene:ping` answered `ok`, no payload, by the router on every transport with no wiring |
| **gRPC** | **yes** — `Benzene.Grpc` (all four RPC shapes incl. streaming, `[GrpcMethod]` routing) + `.AspNet`, `.Client`, `.Versioning`, test helpers; response-side payload downcasting over gRPC deliberately not shipped | **partial** — `grpcbinding` (unary interceptor, `benzene-status` trailer, client); streaming not implemented (documented gap) | **yes** — `Benzene.Grpc` (incl. streaming) + `Benzene.Grpc.Client` | **yes** — `benzene-grpc` (status mapping + server + client; transport SDK an optional extra) |
| **Kafka (self-hosted worker)** | **yes** — `Benzene.Kafka.Core` | **yes** — `kafka` module (consumer-group consumer + client) | **yes** — `Benzene.Kafka.Core` | **yes** — `benzene-kafka` (SDK duck-typed/optional) |
| **RabbitMQ** | **yes** — `Benzene.RabbitMq` (Explicit ack default) | **yes** — `rabbitmq` module (consumer + client) | **yes** — `Benzene.RabbitMq` (+ test helpers; no dedicated doc page — stated omission) | **yes** — `benzene-rabbitmq` (topology deliberately yours) |
| **AWS** | **yes** — Lambda SQS/SNS/EventBridge/Kafka(MSK)/S3/Kinesis/DynamoDB Streams; self-hosted SQS (`Benzene.Aws.Sqs`); Step Functions client | **yes** — `awslambda`, `awssqs`, `awssns`, `awseventbridge`, `awss3`, `awsdynamodb`, `awskinesis`, `awskafka`, `awslambdaclient`, `awsstepfunctions`; API Gateway custom authorizer and direct X-Ray SDK not implemented (OTel/ADOT is the stated route) | **yes** — `Benzene.Aws.Lambda.*` (incl. X-Ray), `Benzene.Aws.Sqs`, `Benzene.Clients.Aws.*` | **yes** — `benzene-aws` (Lambda host incl. all major triggers; outbound senders; self-hosted SQS consumer; test hosts) |
| **EventBridge header carrier (`benzene-headers`)** | **yes** — `Benzene.Aws.Lambda.EventBridge` reads wire headers from the reserved `benzene-headers` key inside `detail` and writes them there outbound (`_benzeneHeaders` before spec-0.2.0, no dual-accept); the key is asserted by the conformance suite | **unknown** — the matrix's EventBridge row does not name the header key | **yes** (attested through conformance) — the matrix's conformance row asserts the `benzene-headers` key from `transport-metadata-cases.json`; the EventBridge adapter and client ship in the AWS row | **unknown** — the matrix ships EventBridge inbound and outbound but does not name the header key |
| **Azure** | **yes** — Function triggers Service Bus/Kafka/Event Grid/Event Hub/Queue Storage/Cosmos DB **+ Blob** (`Benzene.Azure.Function.BlobStorage`, deliberately non-routed: delivers blob name + content to `UseBlob(...)`); self-hosted Service Bus + Event Hub workers | **partial** — `azurefunctions` (HTTP, Queue/Service Bus, Cosmos, Timer, Event Grid, Event Hub) + self-hosted workers + clients; **Blob trigger deferred by design** (SDK-typed, no fabricated custom-handler shape), **Functions Kafka trigger not implemented** (payload unpinned) | **yes** — `Benzene.Azure.Function.*` (incl. Blob, Kafka, Timer), self-hosted workers, `Benzene.Clients.Azure.*` | **yes** — `benzene-azure` (incl. Blob inbound — a binding some sibling ports defer; the port ships it) |
| **GCP** | **partial** — matrix names only `Benzene.GoogleCloud.Functions.PubSub` | **yes** — `gcpfunctions` (HTTP + CloudEvent), `gcppubsub` (zero-dep push), `gcppubsubclient`; pull subscriptions deliberately off the zero-dep path | **yes** — `Benzene.GoogleCloud.Functions.*` + Pub/Sub client | **yes** — `benzene-gcp` (HTTP + Pub/Sub); Eventarc/Cloud Tasks plainly unbuilt |
| **Mesh — service side** | **yes** — `Benzene.Mesh.Wire` (descriptor from live registry incl. `consumes` + `descriptorHash`, trace middleware + batching HTTP exporter, deduplicating issue emitter) + `Benzene.Mesh.Reporting` push self-report; every feed opt-in by design | **yes** — `mesh/` (descriptor from live registry, trace middleware + exporters, issue emitter) | **yes** — `Benzene.Mesh.Wire/Contracts/Dispatch`, `MeshAnnouncer`, per-platform sources | **yes** — `benzene-mesh` (descriptor, feeds, trace middleware) |
| **Mesh — collector** | **yes** — `Benzene.Mesh.Collector` (in-memory store, bounded trace ring; durable storage deliberately out — the aggregator's published catalog artifacts are the durable output) + fleet/tracing/usage backends, aggregator + discovery + artifact stores, gated live dispatch, OIDC login gate | **yes** — `meshd` (in-memory, bounded trace ring; version-skew read model not implemented; durable storage deliberately out) | **yes** — `Benzene.Mesh.Collector` + aggregator/reporting/discovery/fleet/usage/storage packages | **yes** — `benzene-mesh` collector + poller + containerised deploy; in-memory store by design |
| **Mesh — service versions (D26)** | **yes** — `Benzene.Mesh.Collector` keys its catalog by `(service, serviceVersion)`: the fleet query keeps one row per service, headlined by the most recently registered version, and the service query's `versions` lists every live version as `{serviceVersion, topics}` (D26, spec-0.2.0); runs `mesh-service-version-cases.json` and `mesh-version-order-cases.json` | **unknown** — the matrix is silent on `serviceVersion` (it says only that the produced-vs-consumed version-skew read model is not implemented); aligner check: the runner opts out of both service-version fixtures | **no — unbuilt** — "this port's collector has no service-version identity"; neither service-version fixture runs | **unknown** — the matrix is silent; aligner check: the runner's `UNRUN_FIXTURES` says the collector keys by service name alone and does not claim mesh §2.4 |
| **Mesh — UI** | **yes** — `Benzene.Mesh.Ui` serves the **canonical** `mesh-ui.html` at `/benzene/mesh-ui` (byte-identical embedded copy, CI drift-checked against the benzene-ui build) + `mesh-spec-ui.html` | **yes** — `meshd.ViewHandler` serves a **Go-native** `view.html` at `/benzene/fleet-ui` (deliberately *not* the canonical `mesh-ui.html`; the wire contract is the interop point) | **yes** — `Benzene.Mesh.Ui`, default `/benzene/mesh-ui` (matrix still does not say whether it serves the canonical `mesh-ui.html` or its own) | **yes** — serves the **canonical** `mesh-ui.html` vendored from the main repo, at `/benzene/mesh-ui/` (`mount=` to move it) |
| **Health checks** | **yes** — `Benzene.HealthChecks*` reserved-topic middleware (`healthcheck`/`liveness`/`readiness`/`contracts`, deliberately non-overlapping): a health query whose checks ran answers `ok`, the verdict in `isHealthy`; every HTTP route mapped to a health topic answers **503** when `isHealthy` is false (a warning stays 200), pinned by `httpRules.healthEndpoint`; disk/HTTP/TCP/DynamoDB/EF/schema/Service Bus checks, client-side downstream checks, grpc.health.v1 bridge; a dedicated `/health` route deliberately not shipped (health is a topic; `Benzene.CloudService` maps `GET /benzene/health`) | **yes** — `healthcheck/`: `ok` whenever the checks ran, `isHealthy` the verdict (a warning does not flip it), `GET /benzene/health` answers 503 when it is false; TCP/HTTP/disk checks (category-only errors) + `clienthealthcheck` contract probe; no `liveness`/`readiness` topics or routes | **yes** — `ok` whenever the checks ran, the verdict in `isHealthy`; a health-topic HTTP route answers 503 when it is false (a warning stays 200); `Benzene.HealthChecks*` (disk/HTTP/TCP/DynamoDB/TypeORM/schema/Service Bus + transport-embedded + client-side) | **yes** — `benzene-core` `health.py`: `benzene:healthcheck` answers `ok` with the verdict in `isHealthy`, per-check `ok`/`warning`/`failed` (a warning does not flip it); `GET /benzene/health` maps it to 200/503; no `liveness`/`readiness` topics or routes |
| **Spec endpoint & Cloud Service Profile** | **yes** — `Benzene.Schema.OpenApi` (derived spec on the `benzene:spec` topic in native/OpenAPI 3.0/AsyncAPI 3.0, JSON or YAML; unsupported `type`/`format` → `not-implemented`) + `Benzene.Spec.Ui`; `Benzene.CloudService` wires R1–R8 in one call with an honest `CloudServiceProfileReport` (deliberately sugar-only); `Benzene.CloudService.Probe` (external black-box R1–R8 probe); `Benzene.Descriptor` tool | **partial** — `mesh.SpecHandler` serves the registry-derived **mesh descriptor** at `/benzene/spec` (R5), not a contract document and not through a `benzene:spec` topic (not implemented); `cloudservice.New` profile builder (R1–R5/R7 wired) with an honest R1–R8 report (R6/R8 not auto-wired, stated); `cloudserviceprobe` | **yes** — `Benzene.CloudService` (profile report, reserved paths) + `Benzene.Spec.Ui` + `Benzene.CloudService.Probe`; `benzene:spec` as in the reserved-topics row | **yes** — `ContractDocument.derive` (registry-derived contract document) at `GET /benzene/spec` and via `benzene:spec`; R1–R8 mapping in the port's `cloud-service-profile.md`; the HTTP route reads the document directly rather than dispatching the topic (stated) |
| **Codegen & typed clients** | **yes** — `Benzene.CodeGen.Core`/`.Client` (schema closure, canonical JSON + contract hash), the `benzene` CLI, MSBuild build-time generation (`Benzene.CodeGen.Build`), plus `Benzene.Clients` with HTTP/InProcess/AWS/Azure/GCP transport clients; `Benzene.CodeGen.Terraform` deliberately unshipped pending its grow-or-split call | **yes** — `codegen` module (contract-document clients) + `openapi`/`asyncapi` generators (AsyncAPI send side caller-declared; deriving it not implemented) | **yes** — `Benzene.CodeGen.Client` (CLI), `Benzene.Clients` (routing/retries/fan-out), `Benzene.Clients.InProcess` | **yes** — `benzene-codegen-client` (CLI, schema closure, RFC 8785 hashing) + `benzene-openapi` |
| **Settlement of returned failure results** (transport-bindings §1, obligation 8) | **yes** — the 1.0 settlement contract extended to spec-0.2.0: on every queue-shaped and one-shot binding a failure result and a missing result are redelivered; an unmatched topic is a `not-found` failure everywhere; an **at-most-once override on every binding** (existing knobs, or a new `AtMostOnce` value/flag); `.UseUnmatchedTopicAsIgnored()` on every routed binding; the stream allowance used and documented: the self-hosted Kafka and Event Hub workers skip-and-continue by default, and the Lambda MSK, Functions Kafka and Functions Event Hub triggers acknowledge a *missing* result (a failure result still redelivers); unit of settlement stated per binding; guarded by `SettlementContractDefaultsTest` + `SettlementDefaultsConformanceTest`; no `envelope-async` binding | **yes** — every one-way binding (22) settles through `envelope.Settlement`: a failure result, a missing result, an unmatched topic, a middleware error and a recovered panic all redeliver; only the self-hosted `kafka.Consumer` defaults to skip-and-continue (documented; `envelope.AtLeastOnce` opts in to redelivery); `envelope.AtMostOnce` + `IgnoreUnmatchedTopics` on every binding; `awslambda.AsyncEnvelopeHandler` for `Event` invokes; the self-hosted Event Hub consumer now redelivers a failed event in place; asserted against `settlementDefaults`. A middleware panic on a self-hosted worker escapes `Run` and is redelivered from the broker or uncommitted position (stated divergence) | **yes** — one rule (`Settlement.ts`, `isSettledAsFailure`) on every one-way binding: queue and one-shot bindings redeliver a failure result, a missing result, an unmatched topic and an uncaught error; `atMostOnce` + `ignoreUnmatchedTopics` on every binding; the stream bindings (Kinesis, Lambda MSK, Functions Kafka and Event Hub, both self-hosted workers) skip-and-continue a missing result and an unmatched topic by default (`ignoreUnmatchedTopics` defaults `true` there), and the two self-hosted stream workers skip-and-continue failures too — "as .NET does"; the Timer trigger acknowledges a tick that set no result (spec-permitted); `envelope-async` via `invocationType: 'Event'`; asserted against `settlementDefaults` | **yes** — one rule (`dispatch_one_way`, `isSuccessful` authoritative — the status-text defect is gone) on every one-way binding: a failure, a missing result, an unmatched topic and an uncaught exception are redelivered; `Settlement(at_most_once=..., ignore_unmatched_topics=...)` on every host; only the self-hosted Kafka loop skip-and-continues (`stop_on_failure=True` for at-least-once); `envelope-async` via `AwsLambdaApp(invocation_type="Event")`; `idempotency()` never remembers a missing result; each host's `SETTLEMENT_DEFAULTS` checked against the fixture. (The matrix's footnote that the Blob trigger is unnamed by the fixture is stale — note 17) |
| **Outbox** | **yes** — `Benzene.Outbox` (+ `.DynamoDb`, `.EntityFramework` stores); at-least-once only, exactly-once and cross-envelope ordering deliberately out of scope | **no — unbuilt** (stated plainly; only trace is the `responseevents.Publisher` seam) | **no — deliberate** — "writing the outbox row inside *your* DB transaction is application territory"; documented seams (`IResponseEventPublisher`, `IUnitOfWork`) + step-by-step cookbook; matrix itself flags this as an honest divergence from .NET, not parity | **no — unbuilt** (stated plainly; `ResponseEventSink` can feed one you build) |
| **Claim check (oversized payloads)** | **yes** — `Benzene.ClaimCheck` + `.Aws.S3`, `.Azure.Blob` stores; `benzene-claim-check` header contract | **no — unbuilt** (no package, no seam; stated plainly) | **no — unbuilt** ("no stated design reason — it is simply unbuilt") | **no — unbuilt** (stated plainly) |
| **Idempotency** | **yes** — `IIdempotencyStore` + in-memory store + atomic-claim middleware | **yes** — `idempotency/` (same shape; fails open on store outage) | **yes** — `Benzene.Idempotency` (same shape) | **yes** — `benzene-resilience` `idempotency.py` (same shape) |
| **Resilience** | **yes** — `UseRetry` (jitter, max-delay cap) + `UseTimeout` + full Polly via `Benzene.Resilience.Polly` | **yes** — `resilience/` (retry, cooperative timeout, bulkhead, fallback) + `circuitbreaker` module; hedging not implemented (pipeline-contract question, stated) | **yes** — `RetryMiddleware` (no jitter/max-delay cap — stated as not implemented) + full Cockatiel via `Benzene.Cockatiel` | **yes** — `with_retry` decorators + `benzene-resilience` (circuit breaker, bulkhead) |
| **Rate limiting** | **no — deliberate** — listed with the policy-engine adapter as things Benzene deliberately does not ship | **yes** — `ratelimiting/` (per-instance; fleet-wide deliberately at the gateway) | **yes** — `Benzene.RateLimiting` (fixed-window, token-bucket, concurrency, payload-size; distributed deliberately out) | **yes** — in `benzene-resilience` |
| **Caching** | **yes** — `Benzene.Cache.Core` (cache-aside/write-through abstractions + cache health check) + `Benzene.Cache.Redis`; cache *middleware* deliberately not shipped (what/when to cache is a handler-level decision) | **yes** — `cache/` (pluggable `Store`, read-through, safe degradation) | **yes** — `Benzene.Cache.Core` + `Benzene.Cache.Redis` | **yes** — `benzene-cache` (cache-aside; memory + Redis) |
| **Validation** | **yes** — `Benzene.FluentValidation`/`.DataAnnotations`/`.JsonSchema` adapters over one shared short-circuit failure contract (`Benzene.Abstractions.Validation`); deliberately no validation DSL of its own. All three emit `errors[].field` as an RFC 6901 JSON Pointer into the request's JSON form, named by the service's serializer (naming policy and renames applied), omitted for a whole-request rule; gRPC carries it as the dotted `FieldViolation.field` and a Benzene gRPC client reads it back as a pointer | **yes** — `validation/` typed handler wrapper (deliberately not a reflection DSL or pipeline middleware); `validation.Pointer` builds the RFC 6901 pointer from the request's `json` names; runs `conformance:validate`; gRPC converts to the dotted path and back | **yes** — Zod/Joi/Yup/ajv adapter packages over a shared abstraction; every adapter emits RFC 6901 pointers (Yup's paths converted, ajv's native), omitting `field` for a whole-request rule; runs `conformance:validate` through the Zod adapter; gRPC converts to the dotted path and back | **yes** — `benzene-pydantic` (optional adapter; core never imports it): `field` is an RFC 6901 pointer using the model's aliases, omitted for a `@model_validator` rule; gRPC (`benzene-grpc` `details.py`) converts to the dotted path and back |
| **Versioning (handler/payload)** | **yes** — both mechanisms. Selection per core-concepts §2 (spec-0.2.0): exact match, else the handler the payload can be **cast** to (the default wins a tie, else ordinal-greatest), else `not-found` naming topic + version; no version → the unversioned handler, else the declared default (`IsDefault`), else `not-found`; the `version-selection` start-up check fails on two defaults and warns on none; one header, `benzene-version`, renamed in one place and written outbound under the same name; HTTP `/v{version}` route segment; "highest available" only as the opt-in `HighestAvailableVersionSelector`. Payload up- *and* downcasting with auto-composed chains (`Benzene.Core.Versioning`) + request-side gRPC casting | **partial** — selection per core-concepts §2: exact `(id, version)` or `not-found`; no version → the unversioned handler, else `benzene.AsDefault()`; start-up warning for a topic with no default; one `benzene-version` header (`ReservedNames.VersionKey`) or the HTTP `{version}` route segment; no highest-available selector, even opt-in. **Not implemented:** payload casting (so the cast step never applies) and writing `benzene-version` from outbound clients | **yes** — `VersionSelector` per core-concepts §2: exact, else cast-reachable (the default wins a tie), else `not-found`; declared default via `@message(topic, { version, isDefault: true })`; start-up warning; one `benzene-version` header (`addMessageVersionHeaderName`); `HighestAvailableVersionSelector` opt-in only; payload casting for requests, responses, schemas (`Benzene.Core.Versioning`). **No HTTP `/v{version}` route segment** — header only | **yes** — `(topic, version)` registration; one `benzene-version` header (`version_header=`) or an HTTP `/v{version}` segment; exact, else `not-found`; declared `default=True`; `NoDefaultHandlerWarning` at start-up; `highest_version` an explicit opt-in, never the default; casting is registration-level (`casting_handler`), so the cast step has no candidates |
| **Serialization beyond JSON** | **yes** — `Benzene.Xml`, `Benzene.MessagePack`, `Benzene.Avro` (+ `Benzene.NewtonsoftJson`) via content negotiation; binary formats Base64-armored (string-body wire contract — a stated boundary, not an accident) | **no — deliberate** — `encoding/json` is idiomatic; Avro/MessagePack would be new dependency decisions, not ports | **yes** — `Benzene.Xml`, `Benzene.MessagePack`, `Benzene.Avro` | **unknown** — matrix has no serialization row (JSON envelope implied) |
| **Schema registry / Confluent codec** | **yes** — Confluent wire-format codec + `ISchemaRegistryClient` seam (in-box compat checker textual-only, deliberate) | **unknown** — matrix does not address the registry codec (nearest statement: the deliberate no on alternate serializers) | **yes** — `Benzene.SchemaRegistry.Core` (same textual-only checker boundary) | **no — unbuilt** (stated plainly; Kafka binding carries plain envelopes) |
| **Sagas / workflows** | **yes** — `Benzene.Saga` (in-process, LIFO compensation; durable crash-resume deliberately out — use a real orchestrator) | **yes** — `saga/` (same shape and same stated boundary) | **yes** — `Benzene.Saga` (same shape and boundary) | **yes** — `benzene-resilience` `saga.py` (same shape and boundary) |
| **AuthN / AuthZ** | **yes** — `Benzene.Auth.OAuth2` (OAuth2 bearer JWT, strict algorithm allowlist, scope authorization); keys via **full OIDC discovery (`Authority`) or bare JWKS (`JwksUri`)**, cached with refresh-on-unrecognized-`kid` | **yes** — `auth/` (Basic + Bearer JWT: RFC 8725 allowlist, `StaticKeys` **or caching JWKS with OIDC discovery**; roles/scopes) | **yes** — `Benzene.Auth.OAuth2` (`jose`, required no-default allowlist, scopes) + `Benzene.Auth.Basic`; matrix does not say whether JWKS/OIDC discovery is included | **partial** — Basic + bearer JWT against a **configured static key only**; JWKS/OIDC discovery **not implemented** (stated as unbuilt, not declined) |
| **MCP (AI tool access)** | **yes** — `Benzene.Mcp`: the Model Context Protocol as a transport binding; curated tools (topic-bound, or an app-written body that asks the pipeline) over Streamable HTTP (default `/benzene/mcp`) on any Benzene HTTP host or stdio on the worker, a topic-bound call carrying `benzene-mcp-tool` and the session `benzene-mcp-session` (spec-0.2.0), through the same middleware/validation/mesh feeds/start-up checks as every other transport; **no — deliberate**: one tool per handler, server-side session state, the server-to-client stream, resources, prompts | **no — unbuilt** — "this port serves no … MCP surface" (no reason stated) | **no — unbuilt** — "this port has no … MCP binding" (no reason stated) | **unknown** — the matrix does not mention MCP (it was rewritten after the binding was specified, so the silence is now a gap in the record) |
| **Distributed tracing / observability** | **yes** — W3C `traceparent` inbound on HTTP + async transports; OTel, exporter-agnostic | **yes** — `diagnostics` module (OTel API only) + `logging/` (`slog`); SDK/exporter deliberately app-owned | **yes** — `W3CTraceContextMiddleware` + `Benzene.Diagnostics`; OTel, exporter-agnostic | **yes** — `trace_middleware` (mesh) + `benzene-otel` exporter (SDK optional) |
| **Secrets & configuration** | **yes** — `ISecretStore` (env/files/composed/cached) + fail-fast validation; cloud adapters deliberately cookbook-only | **unknown** — matrix does not cover it | **yes** — `Benzene.Configuration.Core` (same shape and same cloud-adapter stance) | **unknown** — matrix does not cover it |
| **Database / state access** | **no — deliberate** — a database is not a transport; wrapping one hides its capabilities | **no — deliberate** — same reason, verbatim stance | **no — deliberate** — same | **no — deliberate** — same |
| **Conformance fixtures (vendored)** | **yes** — `test/Benzene.Conformance.Test` runs the vendored snapshot: envelope (incl. version selection via `conformance:versioned`), HTTP/gRPC status maps, status vocabulary, problem details (incl. `httpRules.envelopeEndpoint`/`healthEndpoint` over a real host), contract document/hash, mesh descriptor/trace/issue/collector/**service-version/version-order**; `transport-metadata-cases.json` for every binding's topic key, the version key, the `benzene-headers` key, the version metadata cases and `settlementDefaults`. **Not run:** the other `metadataCases`/`overrideCases` and `headersExclude` (skipped with a stated reason — note 11). Matrix stale on two points (note 17): it says `SPEC_VERSION` still names `spec-0.1.0` and the `requires: validation` case is unrun; aligner check: `SPEC_VERSION` is `spec-0.2.0` and `ProblemDetailsConformanceTest` claims `validation` | **yes** — `conformance/` runs the vendored fixtures; `settlement_conformance_test.go` asserts `settlementDefaults`; `conformance:validate` runs (Validation row). The matrix names no pin; aligner check: `conformance/SPEC_VERSION` is `spec-0.2.0`, and both service-version fixtures are opted out | **yes** — `test/Benzene.Core.Test/Conformance/` opens 12 of the 14 fixtures (all but `mesh-service-version-cases`/`mesh-version-order-cases`): incl. version selection, `conformance:validate` via Zod, `httpRules`, the `benzene-headers` key and `settlementDefaults` for every shipped one-way binding; **not run:** the non-version `metadataCases` (`headersExclude` — note 11); `ConformanceCoverageTest` fails on a fixture with no runner. Matrix stale on the pin (says `spec-0.1.0` until re-pinned — note 17); aligner check: `SPEC_VERSION` is `spec-0.2.0` | **yes** (thinly attested) — no conformance row; the settlement section names the dependency-free conformance runner checking every host's `SETTLEMENT_DEFAULTS` against `transport-metadata-cases.json`; fixture set and pin unstated. Aligner check: `conformance/SPEC_VERSION` is `spec-0.2.0`; both service-version fixtures are in `UNRUN_FIXTURES` |

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
   Python all say unbuilt, plainly, with no design reason. The `benzene-claim-check` header is now
   specified as an optional add-on (wire-contracts §2.1); .NET is still its only implementation.
3. **Settlement of returned failure results — the spec gap and the safety divergence are closed;
   what remains is deliberate.** Obligation 8 of transport-bindings §1 (spec-0.2.0, D16) states the
   rule, `settlementDefaults` pins each binding's shape, and all four matrices now claim it: on every
   queue-shaped and one-shot binding a failure result, a missing result and an unmatched topic are
   redelivered by default; every one-way binding offers an at-most-once override; every port offers
   unmatched-topic-as-`ignored` (.NET `.UseUnmatchedTopicAsIgnored()`, Go `IgnoreUnmatchedTopics`,
   TypeScript `ignoreUnmatchedTopics`, Python `Settlement(ignore_unmatched_topics=True)`). The defects
   the Phase 0.1 audits found (Go's Event Hub consumer, Python's status-text classification and
   `idempotency()` storing a missing result) are recorded as fixed by their own matrices. What still
   differs sits inside the spec's stream allowance or a binding's stated unit of settlement —
   *deliberate*, each documented by its port:
   - **Stream defaults.** The self-hosted Kafka loop skip-and-continues in all four. The self-hosted
     Event Hub worker skip-and-continues in .NET and TypeScript, redelivers in place in Go, and does
     not exist in Python. On the Lambda MSK and Functions Kafka/Event Hub triggers, .NET and
     TypeScript acknowledge a missing result, and TypeScript also defaults `ignoreUnmatchedTopics:
     true` on every stream binding (Kinesis included), acknowledging an unmatched topic; Go and Python
     redeliver both, Go naming `IgnoreUnmatchedTopics` as the opt-in to .NET's behaviour.
   - **Units of settlement.** Kinesis: .NET fan-in with handler checkpoints; Go stops at the first
     failed record; TypeScript settles the whole invocation and runs records concurrently (shard order
     not kept); Python reports every failed record. DynamoDB Streams: Python runs every record; the
     others stop at the first failure. Lambda MSK: Python fails the whole invocation; the others report
     per partition. Cosmos DB change feed: .NET and TypeScript fan-in with no result; Go settles one
     result-bearing batch; Python routes per document.
   - **Asynchronous envelope** (`envelope-async`) — *staged (.NET)*. Go, TypeScript and Python ship a
     separately selected entry point for `Event` invocations; .NET's Lambda direct-invoke envelope
     handler is request/response only, so .NET has no binding that settles an asynchronously invoked
     envelope.
   - **A middleware panic on Go's self-hosted workers** escapes `Run` and is redelivered from the
     broker or the uncommitted position, where .NET's workers nack and carry on — stated by Go.
4. **Auth — staged (Python) + one unknown (TS).** .NET and Go both state the full key-resolution
   surface: cached JWKS with full OIDC discovery (.NET: `Authority` or bare `JwksUri`, both with
   refresh-on-unrecognized-`kid`; Go: `StaticKeys` or caching JWKS with OIDC discovery). Python is
   explicitly partial (static-key JWT only; JWKS/OIDC stated as unbuilt — staged). Only the
   TypeScript matrix still doesn't say whether its OAuth2 package does JWKS/OIDC discovery — an
   unknown detail, not evidence either way.
5. **Mesh UI — deliberate (Go).** .NET (`/benzene/mesh-ui`) and Python (`/benzene/mesh-ui/`) both
   serve the **canonical** `mesh-ui.html` (.NET as a byte-identical embedded copy with a CI
   drift-check against the benzene-ui build; Python vendored from this repo); Go deliberately serves
   its own zero-dependency `view.html` at `/benzene/fleet-ui` and documents that the shared
   `benzene:mesh:query:*` wire contract, not the page, is the interop point — a stance .NET's matrix
   states in the same terms. TypeScript ships `Benzene.Mesh.Ui` at `/benzene/mesh-ui`, but its matrix
   still doesn't say which asset it serves (its pin commit, `c977776`, is titled as vendoring the
   spec-0.2.0 mesh UI build — a pointer for the scribe, not a cell).
6. **Schema registry — staged (Python) + unknown (Go).** .NET and TypeScript ship the Confluent
   codec + registry seam with the identical textual-only-checker boundary; Python says unbuilt
   plainly; Go's matrix doesn't address it directly.
7. **Rate limiting — deliberate (.NET).** The reference port deliberately does not ship
   rate-limiting middleware; Go, TypeScript and Python all do (each deliberately scoping out
   fleet-wide/distributed limiting). A rare case of the reference doing less than every other port,
   stated on both sides.
8. **Version selection — the spec-level drift is resolved; what remains is staged.**
   `core-concepts.md` §2 now carries decision D1 and `versioning.md` cites it; "exact match, else
   highest available" has left the spec. All four matrices state D1's selection: an exact match;
   else, where the port casts payloads, a handler the version can be cast to; else `not-found`
   naming the version; a message with no version goes to the unversioned or declared-default
   handler; a start-up warning for a topic with only versioned handlers and no default; one
   `benzene-version` header with no fallback list; "highest available" only as an opt-in (.NET,
   TypeScript, Python) or not at all (Go). What still differs:
   - **The cast step.** .NET and TypeScript implement it over their payload casting (default first,
     then the ordinally greatest cast target). Go has no payload casting (not implemented), so the
     step never applies; Python's casting is registration-level, so an exact match already finds a
     casting handler.
   - **The HTTP `/v{version}` route segment.** .NET, Go and Python supply the version from it;
     TypeScript reads the header only (stated).
   - **Outbound.** Go's clients do not write `benzene-version` from the topic's version (stated
     "not implemented"; aligner check: only the root package (`router.go`, `app.go`) and `wire` reference the version key);
     the application sets the header itself.
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
11. **Topic metadata reading — drift against `transport-metadata-cases.json` (.NET, TypeScript).**
    The fixture's `metadataCases` assert that the consumed `topic` key does not reappear as a header
    (`headersExclude`). Both ports' metadata bindings leave it in the header dictionary, and both
    matrices say so: .NET runs the case with that assertion skipped (aligner check: a
    `[Fact(Skip = "Known gap: …")]` in `TransportMetadataConformanceTest`), and TypeScript runs only
    the two version metadata cases. Separately, **.NET's SQS bindings read the topic attribute
    case-sensitively** (aligner check: `SqsMessageTopicGetter` and `SqsMessageTopicMapper` look the
    key up in the SDK's attribute dictionary directly, while the headers getter is case-insensitive),
    against wire-contracts' "header keys are case-insensitive on read" and the fixture's
    `topic-key-is-case-insensitive-on-read` case; .NET's matrix does not record this one. The Go and
    Python matrices do not say whether they run `metadataCases` at all.
12. **Python topic sources — drift against `topicSources` (predates Phase 1).** The DynamoDB
    Streams topic defaults to `dynamodb:<eventname>` (aligner check: `benzene-aws` `events.py`)
    where transport-bindings §1 obligation 2 and the fixture pin `{tableName}:{eventName}`; and the
    RabbitMQ consumer reads only the `topic` header, with no fallback to the AMQP routing key the
    fixture names (aligner check: `benzene-rabbitmq` `consumer.py`). Python's matrix states neither —
    it says only that envelope-less sources route on "a configured convention".
13. **Reserved topics and the derived spec — staged (Go).** .NET, TypeScript and Python serve
    `benzene:spec` and answer an unsupported `type` or `format` with `not-implemented`. Go serves no
    `benzene:spec` topic (stated; aligner check: no reference in source) and its `/benzene/spec`
    serves the mesh descriptor rather than the contract document — scheduled for the plan's Phase 3a,
    and flagged against R5 below. The `liveness`/`readiness` topics are optional: .NET and
    TypeScript serve them, Go and Python state they don't. All four answer `benzene:ping` `ok` with
    no payload from the router; the port work reports that the body's encoding still differs between
    ports, which wire-contracts §7 now leaves unspecified (a caller reads only the status) — not a
    divergence.
14. **MCP — Provisional, .NET only.** .NET ships `Benzene.Mcp` with the spec-0.2.0 headers
    (`benzene-mcp-tool`, `benzene-mcp-session`) at `/benzene/mcp`. Go and TypeScript state they have
    no MCP surface, with no design reason (unbuilt); Python's matrix is silent. *Staged* — the spec
    carries MCP as Provisional (.NET) until a second port implements it.
15. **EventBridge `benzene-headers` — attested by two ports.** .NET states the rename (no
    dual-accept) and TypeScript asserts the key through its conformance runner; the Go and Python
    matrices ship EventBridge without naming the header key. No evidence of divergence — a gap in the
    record.
16. **Mesh service versions (D26) — staged.** D26 ruled one fleet row per service, with the live
    versions listed on the service query; `mesh-service-version-cases.json` stays conditional. Only
    .NET claims it (and `mesh-version-order-cases.json`). TypeScript states its collector has no
    service-version identity; the Go and Python matrices are silent, and their runners opt out of
    both fixtures (aligner check). Go's and TypeScript's opt-out reasons still read "awaiting the
    cross-port claim-or-drop decision", which D26 has since ruled — stale wording in their runners,
    not a capability difference.
17. **Matrices behind their own code — closed the same day.** The .NET and TypeScript Conformance rows
    still named the `spec-0.1.0` pin, and Python's settlement footnote still called the Blob trigger
    unnamed by the fixture; all three were corrected in the ports (`6f68abbb`, `f88a346`, `3500f07`).
### Where the ports agree (worth recording)

- **Database/state access**: all four state the identical deliberate no ("a database is not a
  transport") — the core anti-pattern stance holds estate-wide.
- **Idempotency**: all four ship the same store-seam + atomic-claim shape and the same deliberate
  boundary (no cross-instance de-duplication).
- **Sagas**: all four ship in-process LIFO compensation with the same stated boundary (no durable
  crash-resume; use a real orchestrator).
- **Tracing**: all four join W3C trace context and export via the standard OTel API,
  exporter-agnostic.
- **The spec-0.2.0 wire window** (new 2026-10-11): all four state the same behaviour for settlement
  on queue-shaped and one-shot bindings, the envelope endpoint (HTTP 200 per processed envelope,
  topic restriction), health (`ok` + `isHealthy`, HTTP 503 when unhealthy), one `benzene-version`
  header with D1 selection, `benzene:ping`, and `errors[].field` as an RFC 6901 JSON Pointer
  (dotted over gRPC).

## Spec-requirement flags (spec outranks)

Where `docs/specification/` requires a capability (Core, a binding obligation, or the Cloud Service
Profile R1–R8), a non-**yes** cell or a stated contradiction is flagged here, not merely noted:

- **R5 derived spec — Go.** [cloud-service-profile.md](specification/cloud-service-profile.md) R5
  requires the document served at `/benzene/spec` to have the
  [contract-document.md](specification/contract-document.md) shape; Go's matrix states that it
  serves the mesh descriptor there, and no `benzene:spec` topic. Known and scheduled (Phase 3a: "Go
  builds a contract-document producer"). The other profile surfaces — R3 health at
  `/benzene/health`, R4 the envelope at `/benzene/invoke` with its MUST of topic restriction, R7 the
  `/benzene/` defaults — are attested by all four ports.
- **Binding contracts.** Against wire-contracts' case-insensitive header read: .NET's SQS topic
  attribute (note 11). Against `transport-metadata-cases.json`: .NET's and TypeScript's
  `headersExclude` (note 11); Python's DynamoDB Streams topic, which transport-bindings §1
  obligation 2 also names, and its RabbitMQ routing-key fallback (note 12).
- **Core conformance (fixtures)** — all four snapshots are pinned to `spec-0.2.0` (aligner check of
  each `SPEC_VERSION`). .NET, Go and TypeScript attest running the fixtures in their matrices
  (.NET's and TypeScript's still name the old pin — note 17); Python's attests its runner only
  through its settlement section.
- **Settlement** — now Core binding obligation 8 (transport-bindings §1). Every port's matrix claims
  it, including the MUST of an at-most-once override on every one-way binding; no flag. The former
  "settlement is not in the spec" observation is closed.
- **Version selection** — the spec no longer contradicts itself (note 8), and no port's matrix
  contradicts D1; no flag.

## Unknown cells — who owes a capability-scribe run

| Repo | Unknown cells this run | Stale or missing in the matrix (not unknown) |
|---|---|---|
| benzene-dotnet | *(none)* | Conformance row: the pin and the validation case (note 17); the SQS case-sensitive topic read (note 11) |
| benzene-go | schema registry/Confluent codec; secrets & configuration; EventBridge `benzene-headers`; mesh service versions | — |
| benzene-typescript | which asset `Benzene.Mesh.Ui` serves; JWKS/OIDC discovery (detail) | Conformance row: the pin (note 17) |
| benzene-python | serialization; secrets & configuration; MCP; EventBridge `benzene-headers`; mesh service versions | No conformance row (fixture set, pin); the Blob footnote (note 17); the DynamoDB and RabbitMQ topic sources (note 12) |
