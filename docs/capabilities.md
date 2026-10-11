# Benzene — Consolidated Cross-Port Capability Matrix

**Full refresh: 2026-08-20** (first build). **Row refresh: 2026-10-11 (spec-0.4.0, plan Phase 3b — the Contract Document is the register body), all four columns**: re-read from each port's matrix as committed on main at its `spec-0.4.0` pin (benzene-dotnet `e47eabf2`, benzene-go `da71d57`, benzene-typescript `171848c`, benzene-python `2b962de`; all four fixture snapshots pinned to `spec-0.4.0`). Rows re-read: reserved topics, mesh service side, mesh collector, mesh service versions, mesh UI, spec endpoint & profile, codegen & typed clients, conformance. The ServiceDescriptor, the `benzene:mesh` topic and `descriptorHash` are gone from all four matrices. Note 18 shrinks (Go's Lambda/Azure spec route and TypeScript's AsyncAPI gap close; TypeScript now emits `versionScheme`), note 19 closes but for Go's unstated pin, note 16 loses its two unknowns (Go and Python now state Service Versions unbuilt), the Go §7 spec-requirement flag closes, and notes 20–21 record the register-body differences and the 3b observations. **Phase 3 exit evidence** (reported by the 3b port work, not read from a matrix): live interop on the new register body passed both ways — a Go collector hosting .NET services and a .NET collector hosting Go services — plus Go↔Python and TypeScript→Go. **Row refresh: 2026-10-11 (spec-0.3.0, plan Phase 3a — one self-description), all four columns**: re-read from each port's matrix as committed on main at its `spec-0.3.0` pin (benzene-dotnet `b83e4fcc`, benzene-go `2a8ab4d`, benzene-typescript `897e2e3`, benzene-python `4fd59e8`; all four fixture snapshots pinned to `spec-0.3.0`). Rows re-read: reserved topics (`benzene:spec`), spec endpoint & profile, codegen & typed clients, conformance, and the mesh service-side, collector and UI cells that touch outbound declarations, `consumes`/`produces` or the spec viewer; the descriptor, `benzene:mesh` and `descriptorHash` facts are deliberately left as they were (they change in Phase 3b). Note 13 closes (Go now serves `benzene:spec` and the Contract Document) and the R5 flag against Go with it; note 18 records where the four Contract Document producers and readers still differ, note 19 the matrices that are behind their own repos. **Row refresh: 2026-10-11 (spec-0.2.0) — the Phase 1 rows, all four columns**: re-read from each port's matrix after its spec-0.2.0 catch-up (benzene-dotnet `9e48b35`, benzene-go `ef287d5`, benzene-typescript `c977776`, benzene-python `07b2bfe`; all four fixture snapshots pinned to `spec-0.2.0`). Rows re-read: settlement, versioning, health checks, spec endpoint & profile, validation, mesh UI, MCP, conformance; rows added: envelope over HTTP, default `/benzene/` paths, reserved topics, EventBridge `benzene-headers`, mesh service versions. Divergence notes 3 (settlement) and 8 (version selection) shrink to what still differs, the "settlement is not in the spec" flag closes, and notes 11–17 record the remaining known gaps. Every other row is unchanged since the refreshes below. **Row refresh: 2026-10-11 (spec-0.1.0, plan Phase 0.1) — settlement and conformance, all four columns**: the Go, TypeScript and Python matrices gained per-binding settlement audits read from source (spec-simplification-plan Phase 0.1), and the four ports' fixture snapshots were pinned to the `spec-0.1.0` release. **Column refresh: 2026-09-29 — .NET only**: the .NET port added
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
| **Reserved topics** (wire-contracts §7) | **yes** — `benzene:healthcheck`/`liveness`/`readiness` (+ the .NET-only `contracts`); `benzene:spec` (`{type?, format?}`, compared case-insensitively; absent/empty/`null`/`{}` = the Contract Document; the Contract Document, OpenAPI 3.0 and AsyncAPI 3.0 × JSON/YAML; an unsupported `type` or `format` answers `not-implemented`; `GET /benzene/spec` is an ordinary HTTP mapping onto the topic, so it runs the pipeline and auth with `type`/`format` taken from the query); `benzene:ping` answered `ok`, no payload, by the router at any version (`DisablePing()` opts out); the collector topics. **No `benzene:mesh`** — the topic and `UseMeshDescriptor` are gone (spec-0.4.0, mesh row). Further .NET-only `benzene:*` topics as port extensions | **yes** — `benzene:healthcheck` (no `liveness`/`readiness`), `benzene:ping` (`RouterMiddleware`, `ok`, no payload), `benzene:spec` (`contract.Middleware`: `{type?, format?}`; anything but `benzene`/`json` is `not-implemented`; `WithDocumentType` adds port extensions such as AsyncAPI). `GET /benzene/spec` runs through the pipeline with its `type`/`format` query parameters as the request on every HTTP host: `httpbinding.SpecHandler`, and any `Route` mapped to `benzene:spec` through `httpbinding.Handler`, `awslambda.HTTPHandler` or `azurefunctions.Handler` (the note-18 gap is closed). The collector topics; no `benzene:mesh` topic (spec-0.4.0) | **yes** — all three health topics; `benzene:spec` (absent/empty/`null`/`{}` = the Contract Document; `benzene`/`openapi`/`asyncapi` × `json`; anything else `not-implemented`, HTTP 501; no YAML; `GET /benzene/spec` runs the pipeline, its `type`/`format` query copied onto the request; a cloud service also answers it over `/benzene/invoke`); the collector topics; `benzene:ping` answered `ok`, no payload, by the router when no handler claims it — and so is any other unclaimed reserved id, the retired `benzene:mesh` included (stated: "there is no `benzene:mesh` topic since spec-0.4.0") | **yes** — `benzene:healthcheck` (no `liveness`/`readiness`); `benzene:spec` (`spec_interception`, installed by `application_from` for a declared `ContractInfo`: absent/empty/`null`/`{}` = `benzene`/`json`, the Contract Document; any other `type` or `format` answers `not-implemented`, HTTP 501 — other document types come from `benzene-openapi`, not the topic; `GET /benzene/spec` copies `type`/`format` onto a `benzene:spec` request and runs it through the pipeline); the collector's `benzene:mesh:*` topics — no bare `benzene:mesh` since spec 3b; `benzene:ping` answered `ok`, no payload, by the router on every transport with no wiring |
| **gRPC** | **yes** — `Benzene.Grpc` (all four RPC shapes incl. streaming, `[GrpcMethod]` routing) + `.AspNet`, `.Client`, `.Versioning`, test helpers; response-side payload downcasting over gRPC deliberately not shipped | **partial** — `grpcbinding` (unary interceptor, `benzene-status` trailer, client); streaming not implemented (documented gap) | **yes** — `Benzene.Grpc` (incl. streaming) + `Benzene.Grpc.Client` | **yes** — `benzene-grpc` (status mapping + server + client; transport SDK an optional extra) |
| **Kafka (self-hosted worker)** | **yes** — `Benzene.Kafka.Core` | **yes** — `kafka` module (consumer-group consumer + client) | **yes** — `Benzene.Kafka.Core` | **yes** — `benzene-kafka` (SDK duck-typed/optional) |
| **RabbitMQ** | **yes** — `Benzene.RabbitMq` (Explicit ack default) | **yes** — `rabbitmq` module (consumer + client) | **yes** — `Benzene.RabbitMq` (+ test helpers; no dedicated doc page — stated omission) | **yes** — `benzene-rabbitmq` (topology deliberately yours) |
| **AWS** | **yes** — Lambda SQS/SNS/EventBridge/Kafka(MSK)/S3/Kinesis/DynamoDB Streams; self-hosted SQS (`Benzene.Aws.Sqs`); Step Functions client | **yes** — `awslambda`, `awssqs`, `awssns`, `awseventbridge`, `awss3`, `awsdynamodb`, `awskinesis`, `awskafka`, `awslambdaclient`, `awsstepfunctions`; API Gateway custom authorizer and direct X-Ray SDK not implemented (OTel/ADOT is the stated route) | **yes** — `Benzene.Aws.Lambda.*` (incl. X-Ray), `Benzene.Aws.Sqs`, `Benzene.Clients.Aws.*` | **yes** — `benzene-aws` (Lambda host incl. all major triggers; outbound senders; self-hosted SQS consumer; test hosts) |
| **EventBridge header carrier (`benzene-headers`)** | **yes** — `Benzene.Aws.Lambda.EventBridge` reads wire headers from the reserved `benzene-headers` key inside `detail` and writes them there outbound (`_benzeneHeaders` before spec-0.2.0, no dual-accept); the key is asserted by the conformance suite | **unknown** — the matrix's EventBridge row does not name the header key | **yes** (attested through conformance) — the matrix's conformance row asserts the `benzene-headers` key from `transport-metadata-cases.json`; the EventBridge adapter and client ship in the AWS row | **unknown** — the matrix ships EventBridge inbound and outbound but does not name the header key |
| **Azure** | **yes** — Function triggers Service Bus/Kafka/Event Grid/Event Hub/Queue Storage/Cosmos DB **+ Blob** (`Benzene.Azure.Function.BlobStorage`, deliberately non-routed: delivers blob name + content to `UseBlob(...)`); self-hosted Service Bus + Event Hub workers | **partial** — `azurefunctions` (HTTP, Queue/Service Bus, Cosmos, Timer, Event Grid, Event Hub) + self-hosted workers + clients; **Blob trigger deferred by design** (SDK-typed, no fabricated custom-handler shape), **Functions Kafka trigger not implemented** (payload unpinned) | **yes** — `Benzene.Azure.Function.*` (incl. Blob, Kafka, Timer), self-hosted workers, `Benzene.Clients.Azure.*` | **yes** — `benzene-azure` (incl. Blob inbound — a binding some sibling ports defer; the port ships it) |
| **GCP** | **partial** — matrix names only `Benzene.GoogleCloud.Functions.PubSub` | **yes** — `gcpfunctions` (HTTP + CloudEvent), `gcppubsub` (zero-dep push), `gcppubsubclient`; pull subscriptions deliberately off the zero-dep path | **yes** — `Benzene.GoogleCloud.Functions.*` + Pub/Sub client | **yes** — `benzene-gcp` (HTTP + Pub/Sub); Eventarc/Cloud Tasks plainly unbuilt |
| **Mesh — service side** | **yes** — `Benzene.Mesh.Wire`: the register body is the Contract Document `benzene:spec` serves (`Benzene.Schema.OpenApi`, spec row), with `runtime`/`placement`/`profile`/`degraded`, on `benzene:mesh:register`; the `ServiceDescriptor`, `descriptorHash`, the `benzene:mesh` topic (`UseMeshDescriptor`) and `MeshSchemaGenerator` are gone (spec-0.4.0). Heartbeats (`MeshHeartbeat`) carry `serviceVersion` and the registered `registrationHash`. Sends come from `MeshOutboundRegistry` (`AddSendDeclarations` / the cloud service's `WithSends`), and **a generated client now declares the topics it calls as sends of its consumer** (every registered `IMessageDefinitionFinder<IMessageSenderDefinition>` reaches the document; a (topic, version) declared twice is listed once). Trace middleware (W3C join) + batching HTTP exporter, deduplicating issue emitter; `Benzene.Mesh.Reporting` push self-report; every feed opt-in by design. `UseBenzeneCloudService` wires `benzene:spec`, traces, registration and heartbeats in one call; with `WithHandlers` a reduced document (`events`/`sends` named in `degraded`) registers at wire-up and the first invocation re-registers the complete one. Passes the producer groups (its register body) and `mesh-trace-cases.json` | **yes** — `mesh/`: `mesh.Announcer` registers the Contract Document `contract.Derive` builds — the one `benzene:spec` serves; `Registration` fills a detected `placement` (`DetectPlacement`) when the document carries none; `runtime` (`"go"`), `profile` (`cloudservice.New`'s self-check) and `degraded` (`events` and `sends` when no `OutboundRegistry` was supplied) come from the derivation; no `instanceId` in the body. Heartbeats carry `serviceVersion` and the registered `registrationHash`, with `healthcheck.Run`'s aggregate; registration is retried until it lands. `TraceMiddleware` + outbound `WithTraceContext`, log/push trace exporters, issue emitter — every feed independent and optional. No `benzene:mesh` topic, descriptor or `descriptorHash` (spec-0.4.0). **Deliberately not:** a language-native `exceptionType` (the router converts a panic to a result first); a `region` outside AWS Lambda (none guessed) | **yes** — the register body is the Contract Document `benzene:spec` serves, byte for byte: `MeshAnnouncer` (`Benzene.CloudService`) registers it at host boot (on the first invocation for a host that runs no start-up checks), retrying until the collector answers, then heartbeats `{service, serviceVersion?, instanceId, registrationHash, sentAt, health}`. The document carries `runtime: "typescript"`, `placement` (configured with `withPlacement`, else `self-hosted`; `cloud` is not detected, `region` only when configured — stated), `profile` and `degraded` (`events` and `sends` whenever no outbound registry and no response-event/sender feed is registered), no `instanceId`. `Benzene.Mesh.Wire` (`MeshRegistration`, `MeshHeartbeat`, `MeshOutboundRegistry`, `useMeshTrace`, `HttpMeshTraceExporter`), `.Contracts`, `.Dispatch`, per-platform sources. No ServiceDescriptor, `benzene:mesh` topic or `descriptorHash` (removed). Its k8s example: note 21 | **yes** — `benzene-mesh`: the register body is `registration_document(definition)`, identical to what `benzene:spec` serves (`runtime` `"python"`, `placement`, `profile`, `degraded`; no `instanceId`); `MeshFeedSender` sends registration/heartbeat/traces/issues; `Heartbeat.for_document` carries the document's `registrationHash` and `serviceVersion`; `trace_middleware` + `with_trace_propagation`. The registration is pushed by the host loop (example and deploy code), **not a library announcer** (stated); `degraded` is only what the composition root declares, since its `OutboundRegistry` always determines `events`/`sends`; `placement.cloud` configured, never detected; `region` never guessed. No ServiceDescriptor, `benzene:mesh` topic or `descriptorHash` (removed in spec 3b) |
| **Mesh — collector** | **yes** — `Benzene.Mesh.Collector`: a register body is a Contract Document of which only `service` is required (no dialect check; unmodelled members kept); consumers from `requests`, providers from `events` ∪ `sends`, `benzene:` entries ignored; an absent or `degraded` member is unknown — no edges, and a trace on it is never filed as undeclared; **a trace of a reserved `benzene:` topic is never drift**; the carried `registrationHash` is trusted (never recomputed, never a reason to reject), and a heartbeat's is compared with the registration for the same `(service, serviceVersion)` (service-versions row). No legacy `topics`/`produces` body is read. Read models (Provisional): the fleet row's `topics` counts `requests`; `missingFeeds`' `descriptor` means never registered; `ServiceView.descriptor` renamed `registration`, `InstanceView.descriptorHash` renamed `registrationHash` (+ `serviceVersion`) (note 21). Plus the fleet/tracing/usage backends, aggregator + discovery + artifact stores, gated live dispatch, OIDC login gate; durable storage deliberately out (the aggregator's published catalog artifacts are the durable output). Passes the re-cut `mesh-collector-cases.json`, `mesh-issue-cases.json` and `mesh-service-version-cases.json` | **yes** — `meshd`: register/heartbeat/traces/issues ingest + `benzene:mesh:query:*` read models over an in-memory store with a bounded trace ring. A register body is a Contract Document read loosely: only `service` is required (no dialect check, schemas never interpreted); `requests` are consumers, `events` ∪ `sends` providers, `benzene:` entries ignored; an absent or `degraded` member is unknown — no edges, no undeclared-edge `contract-drift` against it; the carried `registrationHash` is trusted, and a heartbeat's is compared with the service's one registration (`hashMatches`). No legacy body is read. Read models (Provisional): the fleet row's `topics` counts `requests`, `produces` counts events and sends, and it carries `serviceVersion`, `runtime`, `transports` and `placement`; `missingFeeds`' `descriptor` means not registered; the service view's `descriptor` is the registered document as sent. Passes `mesh-collector-cases.json` and `mesh-issue-cases.json`. **Not implemented:** Service Versions, the produced-vs-consumed version-skew read model; durable storage deliberately out. Not in the matrix: that a reserved topic is never drift (aligner check: `meshd/store.go` skips `contract.IsReserved` topics), and the zero-time `lastObservedAt` of a never-observed edge (note 21) | **yes** — `Benzene.Mesh.Collector` + aggregator/reporting/discovery/fleet/usage/storage packages. Reads a registration as a Contract Document: only `service` is required (no dialect check; `registrationHash` trusted, never recomputed, never a reason to reject); `requests` are consumers, `events` ∪ `sends` providers, `benzene:` entries ignored; an absent or `degraded` member is unknown — no edges, no undeclared-edge `contract-drift`; **a `benzene:` topic is never an undeclared edge**; a heartbeat's hash is compared with the service's one registration (keyed by `service`; no Service Versions). No legacy body is read. Read models (Provisional): unchanged except the fleet/service rows' `binding`, now `transports`, and the instance view's `descriptorHash`, now `registrationHash` (+ `serviceVersion`); the service view keeps its `descriptor` field (note 21). Trace `startedAt` and issue `firstSeen`/`lastSeen` ingest as ISO-8601 or epoch ms. The aggregator's drift flag reads the **served** `contractHash` (codegen row) | **yes** — `benzene-mesh` `MeshCollector`: the register body is a Contract Document — `requests` consumers, `events` ∪ `sends` providers (a publisher is a provider), `benzene:` entries ignored; only `service` is required (no `schemaDialect` or hash check — the carried `registrationHash` trusted, never recomputed); an absent or `degraded` member is unknown (no edges, no undeclared-edge `contract-drift`). No legacy `topics`/`produces` body is read. `MeshPoller` ingests each served `/benzene/spec` document as the registration unchanged, with a heartbeat carrying its `registrationHash` and `serviceVersion`. Catalog store (snapshot v4, each registered document kept and published as `specJson`), issue detection, inbox; containerised host + Terraform; in-memory store by design; **Service Versions not implemented**. Passes `mesh-collector-cases.json` and `mesh-issue-cases.json`. Not in the matrix: the reserved-topic drift rule (aligner check: `c477907` skips the undeclared-edge check for `benzene:` topics) and its read-model field names (note 21) |
| **Mesh — service versions (D26)** | **yes** — `Benzene.Mesh.Collector` keys its catalog by `(service, serviceVersion)`: the fleet query keeps one row per service, headlined by the most recently registered version, and the service query's `versions` lists every live version as `{serviceVersion, topics}` (D26). spec-0.4.0: a heartbeat's `registrationHash` is compared with the registration of its own `(service, serviceVersion)` and is null (unknown) for a version nothing registered; a topic counts as declared when **any** live version declares it (a stated approximation — a TraceEvent carries no `serviceVersion`); at most `maxVersionsPerService` (default 8) versions are retained. Runs `mesh-service-version-cases.json` and `mesh-version-order-cases.json` | **no — unbuilt** — "Service Versions is not implemented": the catalog keys by `service` alone, so two side-by-side releases replace each other's registration, and a heartbeat is compared with the service's one registration; both service-version fixtures are listed opt-outs | **no — unbuilt** — "this port's collector has no service-version identity" (keys by `service`); neither service-version fixture runs | **no — unbuilt** — "Not implemented: Service Versions": the catalog is keyed by `service` alone, so two side-by-side releases overwrite each other's registration and a heartbeat's hash is compared with the service's one registration (mesh §5's non-module rule); both fixtures in `UNRUN_FIXTURES`, with reasons |
| **Mesh — UI** | **yes** — `Benzene.Mesh.Ui` serves the **canonical** `mesh-ui.html` at `/benzene/mesh-ui` (byte-identical embedded copy, CI drift-checked against the benzene-ui build) + `mesh-spec-ui.html` (the per-service spec view). The matrix no longer carries the pre-3a "until benzene-ui re-vendors" gap (note 19 closed) | **yes** — `meshd.ViewHandler` serves a **Go-native** `view.html` at `/benzene/fleet-ui`, polling the collector's fleet query (deliberately *not* the canonical `mesh-ui.html`; the wire contract is the interop point); no spec viewer (the port serves no spec-ui surface) | **yes** — `Benzene.Mesh.Ui`, default `/benzene/mesh-ui` (the matrix still does not say whether it serves the canonical `mesh-ui.html` or its own); the spec viewer is `Benzene.Spec.Ui` (spec row) | **yes** — serves the **canonical** `mesh-ui.html` vendored from the main repo, at `/benzene/mesh-ui/` (`mount=` to move it), projecting its catalog into the mesh-ui artifacts; the matrix names no separate spec viewer |
| **Health checks** | **yes** — `Benzene.HealthChecks*` reserved-topic middleware (`healthcheck`/`liveness`/`readiness`/`contracts`, deliberately non-overlapping): a health query whose checks ran answers `ok`, the verdict in `isHealthy`; every HTTP route mapped to a health topic answers **503** when `isHealthy` is false (a warning stays 200), pinned by `httpRules.healthEndpoint`; disk/HTTP/TCP/DynamoDB/EF/schema/Service Bus checks, client-side downstream checks, grpc.health.v1 bridge; a dedicated `/health` route deliberately not shipped (health is a topic; `Benzene.CloudService` maps `GET /benzene/health`) | **yes** — `healthcheck/`: `ok` whenever the checks ran, `isHealthy` the verdict (a warning does not flip it), `GET /benzene/health` answers 503 when it is false; TCP/HTTP/disk checks (category-only errors) + `clienthealthcheck` contract probe; no `liveness`/`readiness` topics or routes | **yes** — `ok` whenever the checks ran, the verdict in `isHealthy`; a health-topic HTTP route answers 503 when it is false (a warning stays 200); `Benzene.HealthChecks*` (disk/HTTP/TCP/DynamoDB/TypeORM/schema/Service Bus + transport-embedded + client-side) | **yes** — `benzene-core` `health.py`: `benzene:healthcheck` answers `ok` with the verdict in `isHealthy`, per-check `ok`/`warning`/`failed` (a warning does not flip it); `GET /benzene/health` maps it to 200/503; no `liveness`/`readiness` topics or routes |
| **Spec endpoint & Cloud Service Profile** | **yes** — `Benzene.Schema.OpenApi` derives the **Contract Document**: `schemaDialect: "openapi-3.0"`; `service`/`serviceVersion`/`versionScheme`/`description` from `IApplicationInfo`; `requests` with no `benzene:` entries and no `response` for a no-payload handler; `events` from event declarations (`AddResponseEventDeclarations`); `sends` from send declarations (`MeshOutboundRegistry` via `AddSendDeclarations` / `WithSends`, and generated clients — mesh row), a (topic, version) in both failing the document; `contractHash` + `registrationHash` always present (`ContractHash`, RFC 8785); author examples only. **spec-0.4.0:** the deployment-side `runtime` (`dotnet`), `placement` and `profile` from a registered `ContractDocumentDeployment` (`SetContractDocumentDeployment`; the cloud service registers one) and `degraded` (`requests` when no handler registry is wired), all outside both hashes; the document is the register body, so `derivationCases` (now carrying `serviceInfo`) cover the registration. Swashbuckle-derived schemas in the OpenAPI 3.0 dialect; component names the simple type name or `[SchemaName]`; a collision throws `ComponentNameCollisionException` **on the first spec request by default** — at start-up only with the opt-in `AddContractDocumentStartUpCheck()` ("to spare Lambda cold starts"), or at build time in `benzene-descriptor` (flagged below). Passes `derivationCases`, `namingCases`, `specTopicCases`, `specHttpCases` and every non-projection hash case. `Benzene.CloudService` — `UseBenzeneCloudService` wires R1–R8 at the `/benzene/*` defaults (incl. registration of the served document + heartbeats), with an honest `CloudServiceProfileReport` stamped on the document's `profile` (R6 = `benzene:spec` + the three feeds); deliberately sugar-only. `Benzene.CloudService.Probe` (external R1–R8 probe) evidences R6, and R2's topic count, through `benzene:spec` over the envelope. `Benzene.Descriptor` (`benzene-descriptor`) emits the `{name}.spec.json` Contract Document from a built service; the `.service.json` descriptor is retired (`--emit descriptor`/`both` fail); `--service-version`/`--version-scheme` now reach the document (#171 closed), `--cloud`/`--region` reach `placement`. `Benzene.Spec.Ui` | **yes** — `contract` package (root module): `contract.Derive` builds the Contract Document at start-up (spec-0.4.0 shape: `schemaDialect: "openapi-3.0"`, `service`/`serviceVersion`/`versionScheme`/`description`, the deployment-side `runtime`/`placement`/`profile`/`degraded` outside both hashes, `requests`/`events`/`sends`, no `benzene:` entries, no `response` for a handler returning `struct{}`, author examples only); named structs hoisted into `components` (named by `BenzeneSchemaName()` or the Go name; undeclared generic instances inlined; typed enums derive as their base type), and a component-name collision **fails before the first message** (`*contract.NameCollisionError`); events and sends only from `contract.RegisterEvent`/`RegisterSend` (a (topic, version) in both is a start-up error; deriving `responseevents` mappings into event declarations not implemented); both hashes in the root module (`contract.HashDocument`, hand-written RFC 8785 `internal/jcs`); served on `benzene:spec` and `GET /benzene/spec` on every HTTP host (reserved-topics row). `cloudservice.New` wires R1–R5/R7 and R6's self-description on `benzene:spec` (placement detected unless `WithPlacement`), returning an honest R1–R8 `ProfileReport` that the document carries as its `profile` (`{"name":"cloud-service","missing":[…]}`); it does **not** wire R6's three feeds or R8 — the report says so (stated). `cloudserviceprobe.Run` evidences R6's observable half by `benzene:spec` over the envelope endpoint | **yes** — `Benzene.Schema.OpenApi` serves the **Contract Document**: `schemaDialect: "openapi-3.0"`, `service`/`serviceVersion`/`versionScheme`/`description` from the application info, `runtime`/`placement`/`profile` from `IServiceDeploymentInfo`, `degraded` for a member with no source, `requests` (no `benzene:` entries; no `response` for a no-payload handler; `httpMappings`), `events` (response events and `{ kind: 'event' }` declarations), `sends` (sender definitions and `{ kind: 'send' }` declarations, `MeshOutboundRegistry`, `withOutbound`), both hashes (`@benzenejs/codegen-core`, shared with the generator), `components` hoisted from Zod/Joi/Yup/bring-your-own schemas; names declared (Zod `.meta({ id })`, Joi `.id()`, Yup `.meta({ id })`, `MapTypeJsonSchemaSource` `{ name }`) else the class name; a collision, or a topic declared as both an event and a send, fails the start-up checks. A cloud service also answers `benzene:spec` over `/benzene/invoke` with the same document (R6). **The `asyncapi` document now reads** handlers, response events, sender definitions and the `MeshOutboundRegistry` declarations (an event: a `send` on its own channel; a send: a `send` onto the other service's channel, its declared reply as AsyncAPI's `reply`) — the note-18 gap is closed. **Stated gaps:** no `messageEndpoint`; no YAML; Joi/Yup nested objects inlined, not hoisted (Zod hoists); undeclared class names do not survive bundler renaming; no synthesized examples. `Benzene.CloudService` (profile report, reserved paths) + `Benzene.Spec.Ui` + `Benzene.CloudService.Probe` (the matrix does not say how the probe evidences R6) | **yes** — `benzene-core` derives the Contract Document **once at the composition root** (`AppDefinition(contract=ContractInfo(...))`, `application_from`) from the registry, the `OutboundRegistry`'s event and send declarations, the router's routes and the envelope endpoint: `schemaDialect: "openapi-3.0"`, `service`/`serviceVersion`/`versionScheme`/`description`, the deployment-side `runtime` (`"python"`)/`placement`/`profile`/`degraded` declared on `ContractInfo` and outside both hashes (`placement.cloud` configured, never detected; `region` never guessed), `requests`/`events`/`sends`, no `benzene:` entries, no `response` for `response_type=NoPayload`, author examples only, `contractHash` + `registrationHash` always present (`contract_hash.py`, RFC 8785 via `rfc8785`); every dataclass and `str`/`int` enum hoisted; a name collision fails building the app (`@schema_name("…")` the remedy); served on `benzene:spec` and `GET /benzene/spec`, through the pipeline (reserved-topics row); the same document is the register body. Passes the four producer groups (`derivationCases` with its `serviceInfo`) and every hash case. The profile self-check's R6 is the three feeds plus the self-description (unmet without a `ContractInfo`); `declare_cloud_service_profile` puts the verdict on the document's `profile`; `probe.py` evidences R6's observable half by invoking `benzene:spec` over `/benzene/invoke`. **Known gap (Q11, deferred):** a pydantic `BaseModel` payload derives to `{}` (flagged below) |
| **Codegen & typed clients** | **yes** — `Benzene.CodeGen.Core`/`.Client` (schema closure, canonical JSON + contract hash) reading the Contract Document: rejects an unknown `schemaDialect` (`UnknownSchemaDialectException`) but **reads a legacy (spec-0.2.0) document with a warning** (note 18); ignores `benzene:` entries (`IncludeReservedTopics` gone); include-list and topic-scoped projections recompute both hashes, and a generated client's `HashCode` is its projection's `contractHash`; **a generated client declares the topics it calls as sends of its consumer**, so they reach that service's Contract Document and register body (mesh row); `benzene diff` ignores `benzene:` entries and refuses a legacy baseline (`LegacyBaselineException`); `SchemaHealthCheck` publishes the **served** `contractHash` (no private hash); passes `parseCases`/`topicScopeCases`/`schemaClosureCases` and every hash case, projections included. The `benzene` CLI, MSBuild build-time generation (`Benzene.CodeGen.Build`), plus `Benzene.Clients` with HTTP/InProcess/AWS/Azure/GCP transport clients; `Benzene.CodeGen.Terraform` deliberately unshipped pending its grow-or-split call | **yes** — `codegen` module (`contractdoc` + `gengo` + `cmd/benzene-codegen`): typed clients from a Contract Document — rejects an unknown `schemaDialect`, ignores `benzene:` entries, projections recompute both hashes with the root module's `contract.HashDocument`, an absent response generates `Result[struct{}]`, the nullable wrapper becomes a pointer; passes every client-generator group and every hash case. `asyncapi/` generates AsyncAPI 3.0 from the Contract Document (requests → `receive`, a reply only when a response is declared; events and sends → `send`); **`openapi/` now generates OpenAPI 3.0 from the Contract Document too** (its components and `$ref`s carried over) — no generator reads a mesh descriptor any more; `clienthealthcheck` compares the served `contractHash` with a generated client's. Deriving `responseevents` mappings into event declarations: not implemented | **yes** — `Benzene.CodeGen.Client` (CLI): typed clients from a Contract Document — rejects an unknown `schemaDialect`, ignores `benzene:` entries (no `--include-reserved`), reads `sends`, types an absent response `void`, embeds its projection's `contractHash`; the hash is `Benzene.CodeGen.Core` (`@benzenejs/codegen-core`), shared with the runtime; the schema health check and the mesh aggregator's drift flag read the **served** `contractHash` (no private hash); `Benzene.Clients` (routing/retries/fan-out), `Benzene.Clients.InProcess`. The `asyncapi` export reads the outbound (event and send) declarations (spec row) | **yes** — `benzene-codegen-client` (`benzene-codegen` CLI): rejects an unknown **or missing** `schemaDialect`, ignores `benzene:` entries (`--include-reserved` gone), reads `sends`, types an absent response `Result[None]` and the nullable wrapper `T \| None`; the embedded `CONTRACT_HASH` is `benzene-core`'s `contractHash` over the client's projection; passes `parseCases`/`topicScopeCases`/`schemaClosureCases` and the projection hash cases. `benzene-openapi` (OpenAPI 3.1 from the registry) |
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
| **Conformance fixtures (vendored)** | **yes** — `test/Benzene.Conformance.Test` runs the vendored snapshot, pinned to `spec-0.4.0` (stated; `conformance-drift-check.yml`): envelope (incl. version selection via `conformance:versioned`), HTTP/gRPC status maps, status vocabulary, problem details (incl. `httpRules.envelopeEndpoint`/`healthEndpoint` over a real host, and the `requires: validation` case through FluentValidation), contract document and hashes (every case passing), mesh trace/issue/collector/**service-version/version-order** — **the only port that runs `mesh-service-version-cases.json` and `mesh-version-order-cases.json`**; `mesh-descriptor-cases.json` and its runner were deleted with the descriptor. `transport-metadata-cases.json` for every binding's topic key, the version key, the `benzene-headers` key and the version metadata cases (`settlementDefaults` in the settlement row). **Not run:** the other `metadataCases`/`overrideCases` and `headersExclude` (note 11) | **yes** — `conformance/` runs the vendored fixtures: the contract-document producer groups (`derivationCases` with its `serviceInfo` identity and placement, `namingCases`, `specTopicCases`, `specHttpCases`) and every non-projection hash case through the runtime `contract` package; `mesh-trace-cases.json`; `mesh-collector-cases.json` and `mesh-issue-cases.json` against `meshd`; `codegen/conformance` runs the client-generator groups and every hash case; `settlement_conformance_test.go` asserts `settlementDefaults`; `conformance:validate` runs (Validation row). **Not run:** `mesh-service-version-cases.json` and `mesh-version-order-cases.json` (listed opt-outs — Service Versions not implemented). The conformance row still names no pin; aligner check: `conformance/SPEC_VERSION` is `spec-0.4.0` | **yes** — pinned to `spec-0.4.0` (stated; `ConformanceDriftTest`); all 13 canonical files vendored (`mesh-descriptor-cases.json` and its runner deleted with the descriptor), **11 opened by a runner**: envelope (incl. version selection), HTTP/gRPC status maps, status vocabulary, problem details (`conformance:validate` via Zod, `httpRules`), transport metadata (topic, version and `benzene-headers` keys; the two version metadata cases; `settlementDefaults` for every shipped one-way binding), **contract document — every group** (producer groups in `ContractDocumentProducerConformanceTest`, `derivationCases` on real Zod output; client-generator groups in `ContractDocumentConformanceTest`), **every hash case**, `mesh-trace-cases`, `mesh-collector-cases` (incl. the 3b cases: a publisher is a provider, `benzene:` entries ignored, a service-only body accepted), `mesh-issue-cases`. **Not run:** both service-version fixtures (no service-version identity), and the non-version `metadataCases` (`headersExclude` — note 11); `ConformanceCoverageTest` fails on a fixture with no runner | **yes** — **now a conformance row**: the dependency-free runner (`python -m tests.conformance_runner`, also under pytest) opens status vocabulary, HTTP/gRPC status maps, envelope, problem details (all three groups), transport metadata (+ `settlementDefaults` against every host's declared constants), contract document (producer groups `derivationCases`/`namingCases`/`specTopicCases`/`specHttpCases` and client-generator groups `parseCases`/`topicScopeCases`/`schemaClosureCases`), `contract-hash-cases.json` (every case, projections included), `mesh-trace-cases.json`, `mesh-collector-cases.json` and `mesh-issue-cases.json`; `run_fixture_coverage` fails the run on an unopened fixture; the `requires: validation` case is reported skipped without pydantic. Pinned to `spec-0.4.0` (stated; `conformance-drift-check.yml`). **Not run:** both service-version fixtures, in `UNRUN_FIXTURES` with reasons. The matrix does not say which `metadataCases` run (note 11) |

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
   specified as an optional add-on ([claim-check.md](specification/claim-check.md), Provisional); .NET is still its only implementation.
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
   still doesn't say which asset it serves (its commits `c977776` and, for spec-0.3.0, `bb18311` are
   titled as vendoring the benzene-ui build — a pointer for the scribe, not a cell). The canonical
   pages this repo vendored for spec-0.3.0 (`d1afc7b6`) read both the Contract Document and the
   pre-3a shape; the .NET and Python pin commits re-vendored that build, and .NET's matrix no longer
   describes the old one (note 19 closed). spec-0.4.0 does not reach the canonical pages, which read
   aggregator artifacts rather than register bodies (plan Phase 3b). Go's page polls only the
   `benzene:mesh:query:*` contract; the fleet-row field renames of note 21 are where a UI would feel
   3b, and no matrix says whether its page reads a renamed field.
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
13. **Reserved topics and the derived spec — Go's gaps resolved (spec-0.3.0, spec-0.4.0); what
    remains is deliberate.** All four ports serve `benzene:spec` with the Contract Document for an
    absent, empty, `null` or `{}` request, answer an unsupported `type` or `format` with
    `not-implemented`, and run `GET /benzene/spec` through the pipeline with `type`/`format` from the
    query — Go now on its Lambda and Azure Functions HTTP handlers too (stated in its matrix). No port
    serves `benzene:mesh` any longer (spec-0.4.0); TypeScript's router answers it, like any unclaimed
    reserved id, `ok` with no payload — a port MAY serve unlisted reserved topics (wire-contracts §7),
    and a tool MUST NOT depend on one, so not a divergence. The extra document types differ, and
    contract-document §7 leaves them as unpinned port extensions — *deliberate*: .NET serves
    OpenAPI 3.0 and AsyncAPI 3.0 as JSON or YAML; TypeScript `openapi` and `asyncapi` as JSON only;
    Go only `benzene`/`json` unless the application adds a type with `WithDocumentType`; Python only
    `benzene`/`json` (other documents from `benzene-openapi`). The `liveness`/`readiness` topics are
    optional: .NET and TypeScript serve them, Go and Python state they don't. All four answer
    `benzene:ping` `ok` with no payload from the router; the port work reports that the body's
    encoding still differs between ports, which wire-contracts §7 leaves unspecified (a caller reads
    only the status) — not a divergence.
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
    .NET claims it (and `mesh-version-order-cases.json`), and at spec-0.4.0 scopes the heartbeat
    hash comparison to one `(service, serviceVersion)` as mesh §5 requires of the module. Go,
    TypeScript and Python now all state their collectors key by `service` alone (Service Versions
    unbuilt) and compare a heartbeat with the service's one registration — mesh §5's non-module
    rule — and all three opt out of both fixtures. TypeScript's runner still gives the opt-out
    reason "awaits a cross-port claim-or-drop decision", which D26 has ruled (aligner check:
    `ConformanceCoverageTest.test.ts`); Go's runner now gives the not-implemented reason — stale
    wording in one runner, not a capability difference.
17. **Matrices behind their own code — closed the same day.** The .NET and TypeScript Conformance rows
    still named the `spec-0.1.0` pin, and Python's settlement footnote still called the Blob trigger
    unnamed by the fixture; all three were corrected in the ports (`6f68abbb`, `f88a346`, `3500f07`).
    At the spec-0.3.0 refresh both Conformance rows name the new pin (.NET's also the validation case).
18. **The Contract Document — the shared shape holds; the differences are mostly deliberate or
    staged, with one drift and two flags.** All four matrices state the same document:
    `schemaDialect: "openapi-3.0"`, `service`/`serviceVersion`, `requests`/`events`/`sends`
    from explicit declarations, no `benzene:` entries, no `response` for a handler with no payload,
    author examples only, `contractHash` + `registrationHash` computed by the runtime (no
    private or provider-side hash anywhere — .NET's `SchemaHealthCheck`, TypeScript's schema health
    check and aggregator, Go's `clienthealthcheck` and Python's poller all read the served value),
    and, at spec-0.4.0, the deployment-side `runtime`/`placement`/`profile`/`degraded` outside both
    hashes (note 20). Every port passes the producer groups and the hash cases (conformance row).
    Where they differ:
    - **When a component-name collision fails** — flagged below. Go (before the first message),
      TypeScript (start-up checks) and Python (building the app) fail at start-up; .NET fails on the
      first spec request unless the application opts in to `AddContractDocumentStartUpCheck()` or
      emits the spec at build time — *deliberate*, stated as sparing Lambda cold starts.
    - **Hoisting** — *deliberate within a SHOULD* (§4.1 rule 1: SHOULD hoist, MAY inline). TypeScript
      inlines Joi/Yup nested objects (Zod hoists); Go inlines undeclared generic instances; .NET and
      Python state they hoist every named type. Component names are outside the hashes, so the
      ports still agree on both hashes.
    - **Optional top-level fields.** All four now emit `versionScheme` (TypeScript's matrix gained it
      at spec-0.4.0). TypeScript states it emits no `messageEndpoint`; the other matrices do not say
      whether they emit one (Python's derivation reads the envelope endpoint). *Staged.*
    - **Where events come from.** .NET: event declarations (`AddResponseEventDeclarations`).
      TypeScript: its response events and `{ kind: 'event' }` declarations. Python: the
      `OutboundRegistry`'s declarations. Go: only `contract.RegisterEvent` — its `responseevents`
      mappings do not become event declarations (stated "not implemented"). *Staged (Go).* Where
      sends come from now includes .NET's generated clients, which declare the topics they call as
      sends of their consumer (spec-0.4.0).
    - **Derivation coverage.** Python: a pydantic `BaseModel` payload derives to `{}` — a stated
      known gap the product owner deferred past 3a (Q11, `work/self-description-design.md`);
      flagged below against §4.2. Go: typed enums derive as their base type, which §4.2 permits.
    - **Extension documents.** *Resolved (spec-0.4.0).* Go's `asyncapi/` and, now, `openapi/`
      generate from the Contract Document; TypeScript's `asyncapi` document reads the event and send
      declarations (its matrix states it), so it no longer disagrees with the Contract Document
      about what is sent. The Go HTTP spec-route gap of 3a is closed too (note 13).
    - **A document without `schemaDialect`** — **drift (unpinned).** Readers disagree: .NET's
      generator reads a legacy (spec-0.2.0) document with a warning (and its `benzene diff` refuses
      one as a baseline); Python's rejects a missing dialect like an unknown one; the Go and
      TypeScript matrices say only "rejects an unknown `schemaDialect`". §1 makes the field REQUIRED
      and says a consumer MUST reject a dialect it does not know, but `parseCases` pins only an
      unknown value, not an absent one. So the same pre-3a document generates a client in one
      language and fails in another. Whether absent counts as unknown is for the spec to say. (A
      collector is not a consumer in this sense: mesh §2 exempts it from the dialect rule, and all
      four collectors state they do not check it.)
19. **Matrices behind their own repos — closed but for one pin (spec-0.4.0).** .NET's spec row no
    longer lists the "UI pages still read the old shape" gap that its pin commit `b83e4fcc` had
    closed; Python's matrix now has a conformance row naming its fixture set and the `spec-0.4.0`
    pin. Go's conformance row still names no pin (its `SPEC_VERSION` reads `spec-0.4.0`; its spec
    and mesh rows name the spec-0.4.0 shape). New at this refresh, the reserved-topic drift rule is
    in the Go and Python collectors' source but not their matrices (note 20).
20. **The register body (spec-0.4.0) — the contract holds in all four; what differs is idiom or
    staged.** All four matrices state the same service side: the register body is the document
    `benzene:spec` serves, with `runtime`, `placement`, `profile` and `degraded` and no
    `instanceId`; heartbeats carry `registrationHash` and `serviceVersion`; no ServiceDescriptor,
    `benzene:mesh` topic or `descriptorHash` remains. And the same collector side: only `service` is
    required, the carried `registrationHash` is trusted, providers are `events` ∪ `sends` and
    consumers `requests`, `benzene:` entries are ignored, an absent or `degraded` member is unknown,
    and no legacy body is read. The Phase 3 exit's live interop (header) confirms the shapes across
    .NET, Go, Python and TypeScript. What differs:
    - **Who registers** — *idiom* (mesh §8). .NET: `UseBenzeneCloudService` (a reduced document at
      wire-up under `WithHandlers`, the complete one on the first invocation). Go: the library's
      `mesh.Announcer`. TypeScript: `MeshAnnouncer` at host boot. Python: the host loop in its
      example and deploy code — no library announcer (stated) — plus the collector-side `MeshPoller`,
      which ingests each served `/benzene/spec` as a registration.
    - **`placement`** — *permitted* (contract-document §1: detected or configured). Go detects
      `cloud`, and `region` only on AWS Lambda; TypeScript and Python are configured only
      (TypeScript defaults to `self-hosted`); .NET takes it from a registered
      `ContractDocumentDeployment` or `benzene-descriptor --cloud/--region`, and its matrix does not
      say whether anything is detected. TypeScript's k8s example: note 21.
    - **`degraded`** — *permitted*. Go names `events` and `sends` when no `OutboundRegistry` is
      supplied; TypeScript the same when no outbound registry and no response-event/sender feed is
      registered; .NET names `requests` when no handler registry is wired, and `events`/`sends` on
      the `WithHandlers` wire-up registration; Python names only what its composition root declares.
    - **Heartbeat hash scope** — *staged*, following each catalog key: .NET compares within one
      `(service, serviceVersion)` (the Service Versions module), the other three against the
      service's one registration (mesh §5's non-module rule). Note 16.
    - **A trace on a reserved topic is never drift** (mesh §4.2, added during 3b after the Python
      collector filed `contract-drift` for a traced `benzene:spec` in a live run). The .NET and
      TypeScript matrices state it; the Go and Python matrices do not, though their source does
      (aligner check: `meshd/store.go`'s `contract.IsReserved` guard; Python `c477907`). No
      `mesh-collector-cases.json` or `mesh-issue-cases.json` case traces a reserved topic, so the
      rule rests on the four ports' own tests — a gap in the record and in the fixtures, not a
      divergence.
21. **Observations from the 3b port work — one Provisional-shape divergence and two flags.**
    - **Collector read-model field renames differ** — *divergent, inside Provisional shapes*. The
      read models are Provisional and "asserted only as far as the fixtures need" (mesh §4), and each
      collector renamed different fields as the descriptor left: .NET renamed `ServiceView.descriptor`
      to `registration` and `InstanceView.descriptorHash` to `registrationHash` (+ `serviceVersion`)
      and does not name the fleet row's transports field; TypeScript renamed the fleet/service rows'
      `binding` to `transports` and the instance view's `descriptorHash` to `registrationHash`
      (+ `serviceVersion`) but kept the service view's `descriptor`; Go's fleet row carries
      `transports` (+ `serviceVersion`, `runtime`, `placement`) and its service view keeps
      `descriptor`, the registered document; Python's matrix names no read-model fields (its catalog
      publishes each document as `specJson`). All keep the `descriptor` name for the missing-feed
      marker, which mesh §6 itself uses. A view written against one collector's service or instance
      view will not read another's; the fixtures still pass because they do not assert these fields.
      Whether to pin them is the post-1.0 pinning mesh §4 already defers.
    - **Go reports a never-observed declared edge with a zero-time `lastObservedAt`** — flagged
      below against mesh §4.2, which requires "last observed at (or its absence)" per edge. Reported
      by the port work; not in Go's matrix. Aligner check: `meshd.EdgeActivity.LastObservedAt` is a
      `time.Time` tagged `omitempty`, which `encoding/json` does not omit for a struct value, so an
      unobserved edge carries `0001-01-01T00:00:00Z`, while `store.go`'s own comment says the edge
      is reported "with an absent LastObservedAt". A reader that does not special-case the zero time
      sees an ancient observation rather than none.
    - **TypeScript's k8s example sends `payment:take` without declaring it and reports
      `placement.region` `"in-cluster"`** — reported by the port work; example code, not the
      library, and not in TypeScript's matrix. Aligner check: `examples/k8s-mesh/src/serviceApp.ts`
      wires `.withPlacement('kubernetes', 'in-cluster')` and no `withOutbound`/`addMeshOutbound`.
      Per TypeScript's matrix such a document names `events` and `sends` in `degraded`, so a
      collector treats them as unknown and files no drift — the example's graph simply lacks the
      orders → payments edge. `"in-cluster"` is not a region: flagged below against
      contract-document §1 ("a producer MUST NOT guess"); `cloud: "kubernetes"` is an explicit
      override §1 allows.

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
- **The spec-0.3.0 self-description** (new 2026-10-11): all four derive one Contract Document from
  their registrations, compute `contractHash` and `registrationHash` in the runtime with no private
  hash, serve it on `benzene:spec` and through the pipeline at `GET /benzene/spec`, leave
  `benzene:` topics out of it, and ship a client generator that rejects an unknown
  `schemaDialect`, ignores `benzene:` entries and embeds its projection's `contractHash`.
- **The spec-0.4.0 register body** (new 2026-10-11): all four register the document `benzene:spec`
  serves, heartbeat its `registrationHash` and `serviceVersion`, and run a collector that requires
  only `service`, trusts the carried hash, builds providers from `events` ∪ `sends` and consumers
  from `requests`, treats an absent or `degraded` member as unknown and reads no legacy body
  (note 20). Live interop on that body passed between .NET, Go, Python and TypeScript (header).

## Spec-requirement flags (spec outranks)

Where `docs/specification/` requires a capability (Core, a binding obligation, or the Cloud Service
Profile R1–R8), a non-**yes** cell or a stated contradiction is flagged here, not merely noted:

- **R5 derived spec — closed (spec-0.3.0).** Go's matrix now states the
  [contract-document.md](specification/contract-document.md) document at `/benzene/spec` and on
  `benzene:spec`, so all four ports attest R5, alongside R3 health at `/benzene/health`, R4 the
  envelope at `/benzene/invoke` with its MUST of topic restriction, and R7 the `/benzene/` defaults.
- **Contract Document §4.1 rule 4 — .NET.** A component-name collision MUST fail derivation "before
  any message is handled". By default .NET throws on the first spec request; start-up failure needs
  the opt-in `AddContractDocumentStartUpCheck()`, or the spec emitted at build time (note 18).
  A deliberate trade for Lambda cold starts, ruled in `work/self-description-design.md` §10.2
  item 7 (build-time verification where the port has it, the start-up check opt-in); whether
  `Benzene.CloudService` should switch the check on by default is open for Phase 4 (the profile).
- **Contract Document §4.2 derivation — Python.** A pydantic `BaseModel` payload derives to `{}`,
  where §4.2 maps an object or record to an object schema and keeps `{}` for raw or unserializable
  values. The port states it as a known gap; the product owner deferred it past 3a (Q11). The
  port still passes `derivationCases`.
- **Contract Document §7 / transport-bindings §1 — Go, closed (spec-0.4.0).** Go's matrix now
  states that its Lambda and Azure Functions HTTP handlers copy `type`/`format` onto `benzene:spec`,
  as `httpbinding` does (note 13).
- **R6 mesh service-side feeds — all four can meet it (spec-0.4.0).** R6 is now `benzene:spec` plus
  registration (its body the R5 document), heartbeats and traces. .NET's `UseBenzeneCloudService`
  wires all four; Go's `cloudservice.New` wires only `benzene:spec` and its report lists the three
  feeds as unmet until the application runs `mesh.Announcer` and the trace exporters (stated,
  honest); TypeScript's `MeshAnnouncer` and trace feed, and Python's `MeshFeedSender` with
  registration pushed by the host loop, provide them. The `profile` self-assessment is on the
  document in all four. The .NET, Go and Python probes evidence R6's observable half through
  `benzene:spec` over the envelope; TypeScript's matrix does not say how its probe does. No flag.
- **Mesh §4.2 "last observed at (or its absence)" — Go, reported but not in its matrix.** A
  never-observed declared edge carries a zero-time `lastObservedAt` rather than none (note 21). The
  read-model shape is Provisional; the per-edge reporting rule is a MUST.
- **Contract Document §1 `placement.region` — TypeScript's k8s example.** It reports `"in-cluster"`
  as a region, which no platform documents; §1 says a producer MUST NOT guess (note 21). The
  library emits a region only when configured, so this is the example's configuration, not the
  port's behaviour.
- **Binding contracts.** Against wire-contracts' case-insensitive header read: .NET's SQS topic
  attribute (note 11). Against `transport-metadata-cases.json`: .NET's and TypeScript's
  `headersExclude` (note 11); Python's DynamoDB Streams topic, which transport-bindings §1
  obligation 2 also names, and its RabbitMQ routing-key fallback (note 12).
- **Core conformance (fixtures)** — all four snapshots are pinned to `spec-0.4.0` (the .NET,
  TypeScript and Python matrices state it; Go's by aligner check of `SPEC_VERSION`), and none
  vendors `mesh-descriptor-cases.json`, which the release deleted. All four claim the
  contract-document producer groups that contract-document §8 and mesh §7 require (the document is
  the register body), the client-generator groups plus every hash case required of a generator,
  `mesh-trace-cases.json`, and — as collectors — `mesh-collector-cases.json` and
  `mesh-issue-cases.json`. Python now attests these in a conformance row. Only .NET runs the
  conditional `mesh-service-version-cases.json` and `mesh-version-order-cases.json`.
- **Settlement** — now Core binding obligation 8 (transport-bindings §1). Every port's matrix claims
  it, including the MUST of an at-most-once override on every one-way binding; no flag. The former
  "settlement is not in the spec" observation is closed.
- **Version selection** — the spec no longer contradicts itself (note 8), and no port's matrix
  contradicts D1; no flag.

## Unknown cells — who owes a capability-scribe run

| Repo | Unknown cells this run | Stale or missing in the matrix (not unknown) |
|---|---|---|
| benzene-dotnet | *(none)* | The SQS case-sensitive topic read (note 11); whether `placement` is ever detected and what the fleet row calls its transports field (notes 20, 21). Outside this refresh: its matrix now has a `Benzene.RateLimiting` row (added in `b3e86703`) while its AuthN row still lists rate-limiting middleware among the things it does not ship — self-contradictory, and this page's Rate limiting row and note 7 were not re-read this run |
| benzene-go | schema registry/Confluent codec; secrets & configuration; EventBridge `benzene-headers` | Conformance row names no pin (note 19); the reserved-topic drift rule its collector implements (note 20); the zero-time `lastObservedAt` of a never-observed edge (note 21); whether a missing `schemaDialect` is rejected (note 18) |
| benzene-typescript | which asset `Benzene.Mesh.Ui` serves; JWKS/OIDC discovery (detail); how `Benzene.CloudService.Probe` evidences R6 (detail) | Whether a missing `schemaDialect` is rejected (note 18); the k8s example's undeclared send and guessed region (note 21); its runner's stale service-version opt-out reason (note 16) |
| benzene-python | serialization; secrets & configuration; MCP; EventBridge `benzene-headers` | The reserved-topic drift rule its collector implements (`c477907`, note 20); its collector's read-model field names (note 21); which `metadataCases` run (note 11); the DynamoDB and RabbitMQ topic sources (note 12) |
