# Benzene — Consolidated Cross-Port Capability Matrix

**Full refresh: 2026-10-11 (spec-1.0.0).** Every row, every column, every divergence note and every
spec-requirement flag on this page has been re-derived from scratch. The source is each port's
`docs/capability-matrix.md` on main, each pinned to `spec-1.0.0`: benzene-dotnet `1c828082`,
benzene-go `7fce3e2`, benzene-typescript `d8f26b0` and benzene-python `6499da6`. The canonical
mesh UI pages come from benzene-ui `0bde6b1`, also pinned to `spec-1.0.0`. Everything is read
against the specification as released at the tag `spec-1.0.0`:

- **Core and the Cloud Service Profile** (R1–R5, R7) are frozen under semver.
- **Stable modules:** the Contract Document and the Mesh. A meshed service is a Cloud Service plus
  M1 and M2.
- **Provisional modules (.NET):** Service Versions, MCP and Claim Check.

This refresh also records the Phase 5 Core-gap work, which every port closed before the tag
("Closed at this refresh", below).

**Row refresh: 2026-10-12 (spec-1.1.0).** The spec is read at the tag `spec-1.1.0` (`82923595`).
Its four changes are SHOULDs or a clarification:

- wire-contracts §4.1: a client that reads a problem document takes the status from its
  `benzeneStatus`;
- wire-contracts §4.2: an error's `code` rides in `FieldViolation.reason`, both directions;
- transport-bindings §1 "Outbound clients": a send that fails in transport, one-way included, yields a
  `timeout`/`service-unavailable` result rather than an exception;
- contract-document §1: a document without `schemaDialect` has an unknown dialect (parse case
  `missing-schema-dialect-is-rejected`).

The sources are each port's `docs/capability-matrix.md` on main, each now pinned to `spec-1.1.0`:
benzene-dotnet `e3892e28`, benzene-go `9a13703`, benzene-typescript `8464d3c` and benzene-python
`8a0bedc`. benzene-ui `bdcb581` is also pinned to `spec-1.1.0`. Rows re-derived:

- Outbound error fidelity, Envelope over HTTP (Python's client), gRPC;
- the new row Outbound transport failure;
- Spec endpoint, Codegen, Outbox (.NET), Distributed tracing, Mesh — service side;
- Conformance fixtures.

Notes 5 (the benzene-ui pin), 12, 15, 16 and 18 were also re-derived, and note 19 is new. So were
the spec-requirement flags, the agreement list and the unknown-cells table. Every other row stands as of the full refresh above.

**Row refresh: 2026-10-14 (spec-1.2.0).** The spec is read at the tag `spec-1.2.0` (`3bfe201f`).
Its two changes are both in the Mesh module, and neither adds a MUST for a service:

- mesh §4.1: an issue MAY carry `errors`, (`field`, `code`) pairs with delta counts, never the
  `message`. Both sides keep at most ten per issue, ordered by count, then `field`, then `code`. An
  empty member is the same as an absent one, and a malformed entry is skipped with the issue kept.
  The issue feed stays optional on both sides;
- mesh §4: a fleet view's trace summary carries the flow's `status`: the earliest-started failing
  event's when the flow failed, else the earliest-started event's. A collector whose trace store
  holds no Benzene status omits it, and readers MUST tolerate its absence.

`mesh-issue-cases.json` gained three error-detail cases and `mesh-collector-cases.json` two
flow-status cases. The sources are each port's `docs/capability-matrix.md` on main: benzene-dotnet
`4bf13aaa`, benzene-go `3b48fb8`, benzene-typescript `13f30ee` and benzene-python `563be93`, with
benzene-ui `c42694d`. Each matrix states the `spec-1.2.0` pin, and each snapshot is byte-identical
to the tag (aligner check). Rows re-derived:

- Mesh — service side, Mesh — collector;
- Conformance fixtures;
- Spec endpoint (.NET only: its M2 statement is no longer self-contradictory).

Notes 5, 14, 15 and 18 were also re-derived, and note 20 is new. So were the spec-requirement
flags, the agreement list and the unknown-cells table. Every other row stands as of the refreshes
above.

Earlier refreshes are in `git log -- docs/capabilities.md`. They were the first full build
(2026-08-20), the .NET column refreshes (2026-08-20, 2026-09-29) and the row refreshes for
spec-0.1.0 through spec-0.5.0 (2026-10-11).

Ports surveyed: **.NET**, **Go**, **TypeScript** and **Python**, each from its own
`docs/capability-matrix.md` (the capability-scribe's output in that port's repo). This document is
the **capability record** of the estate (see [documentation-lifecycle.md](documentation-lifecycle.md)).
It is descriptive, not normative. The spec (`docs/specification/**`) defines what a port *must* do;
this page records what each port's own matrix *says* it does.

**How cells are filled.** A cell comes only from that port's own matrix. It never comes from the
port's source code, or from another port's matrix: one port's claim about a sibling is noted, not
trusted. A matrix that doesn't address an area yields **unknown**. That is itself a finding: the
repo owes a capability-scribe run.

Sometimes a refresh checks a reported divergence, or a matrix claim that looks stale, against a
port's own source, config or commit titles. The text marks that as an **aligner check**. An aligner
check confirms a finding; it is never the source of a capability claim.

Cell vocabulary:

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
| **Outbound error fidelity** (core-concepts §12; wire-contracts §4.1 `benzeneStatus`, spec 1.1) | **yes** — `Benzene.Clients.Common.ProblemDocuments`: every client that receives a problem document rebuilds the result's `errors` from it, with `field` and `code` intact and in order, and attaches the document verbatim (`result.GetProblem()`). That covers the envelope clients, from a failure envelope's body (a `Void` caller included); the plain REST client (`UseHttp`/`HttpContextConverter`), from a non-success `application/problem+json` response; and the envelope-over-HTTP readers, from a non-envelope `application/problem+json` failure. **Status (spec 1.1):** inside an envelope it stays the envelope's (envelope-first). Outside one (the plain REST client, and the readers' non-envelope fallback), the problem document's `benzeneStatus` is the status, so an application-defined status survives the hop. The reverse HTTP table applies to every other failure, and the document's HTTP `status` member is never read. A failure body of any other content type is not read as a problem document. gRPC: `code` rides in `FieldViolation.reason` (gRPC row) | **yes** — `httpclient.Client` rebuilds `errors` (`message`, `field`, `code`) from the failure envelope (`envelope.ToResult`), whose `statusCode` is the status. When the response is not an envelope, it rebuilds them from an `application/problem+json` (or JSON) problem body. **Status (spec 1.1):** taken from the problem's `benzeneStatus`; a success-class `benzeneStatus` on a failure is ignored. Only a problem with none takes the reverse HTTP table. `awslambdaclient.Client` reads the failure envelope; `inprocess.Sender` passes the handler's errors through whole. Run end to end against `problem-details-cases.json`'s `envelopeCases` (`client_fidelity_conformance_test.go`, `problem_conformance_test.go`). One-way clients answer `accepted` and receive no document. **Deliberately not:** attaching the received document (the §12 MAY). `grpcbinding.Client` now keeps `code` too, read from `FieldViolation.reason` (gRPC row) | **yes** — `HttpClientMiddleware` (`Benzene.Clients.Http`) rebuilds `errors` from a non-success `application/problem+json` response, with `field` and `code` intact, and attaches the document (`ProblemTypes.from(result)`). The envelope reader behind `IBenzeneMessageSender` (`asBenzeneResult`) does the same for a failure envelope's body. **Status (spec 1.1):** both take it from the document's `benzeneStatus`, so a failure envelope whose `statusCode` the reader cannot map is still read from the document. A failure without a document, or a document without `benzeneStatus`, takes the reverse HTTP table (or the envelope's `statusCode`). An `application/json` failure body is not read as a problem document (the §4.1 MAY not taken). One-way clients receive no document; the mesh dispatchers relay the reply body verbatim. gRPC: `code` ↔ `FieldViolation.reason` (gRPC row) | **yes** — every reply-receiving client rebuilds `errors` in order with `message`, `field` and `code` through `benzene.results.problem_errors`. `HttpMessageSender` reads a route-mapped `application/problem+json` (or `application/json`) body. `InProcessMessageSender` and `LambdaMessageSender` (`RequestResponse`) read the failure envelope's body. **Status (spec 1.1):** `HttpMessageSender` takes the document's `benzeneStatus` when it states a failure, else the reverse HTTP table. The envelope clients take the envelope's `statusCode`; the document's `benzeneStatus` stands in only for an envelope with none. A non-problem failure body keeps the peer's status and never fails the call. `run_client_error_fidelity` runs every `envelopeCases` case through all three, and through `GrpcMessageSender` over loopback (`code` from `FieldViolation.reason`). The received document is not attached (the §12 MAY not taken) |
| **Envelope over HTTP (R4)** | **yes** — `Benzene.Http` `UseBenzeneMessage` at `/benzene/invoke`. HTTP 200 for every envelope it processed, whatever its status; 400 (problem+json) only for a body that is not an envelope. Same pipeline and auth as the other transports. `BenzeneMessageHttpOptions.TopicFilter` restricts the reachable topics (a rejected topic is a `not-found` envelope). The envelope-over-HTTP clients read the envelope whatever the HTTP status. **No asynchronous envelope entry point** (`envelope-async`): the Lambda direct-invoke envelope handler is request/response only (stated) | **yes** — `httpbinding.EnvelopeHandler` (`/benzene/invoke`): 200 per processed envelope (`not-found`, `deleted` and a pipeline `unauthorized` alike); 400 only for a non-envelope; the inner problem has no `status`. `WithTopicFilter`. `httpclient` reads the envelope whatever the status (`envelope.ToResult`). Async entry point: `awslambda.AsyncEnvelopeHandler` for `Event` invokes | **yes** — `useBenzeneMessage` (`src/Benzene.Http`) at `POST /benzene/invoke`: 200 per processed envelope; the inner problem has no `status`; 400 problem+json only for a malformed envelope. `topicFilter`. The envelope clients read the envelope whatever the status. Async entry point: `BenzeneMessageLambdaHandler` with `invocationType: 'Event'` | **yes** — `benzene-http` `POST /benzene/invoke`: 200 per processed envelope; the inner problem has no `status`; 400 only for a non-envelope. `StandardPaths(invoke_topics=...)` restricts the reachable topics. Async entry point: `AwsLambdaApp(invocation_type="Event")`. No envelope-over-HTTP client: `HttpMessageSender` posts to routes (stated "not built") |
| **Default `/benzene/` paths (R7)** | **yes** — `/benzene/invoke`, `/benzene/spec`, `/benzene/health` (`Benzene.CloudService`), `/benzene/spec-ui` (default spec URL `/benzene/spec?type=benzene`), `/benzene/mesh-ui`, `/benzene/mcp`; each configurable | **yes** — `/benzene/invoke`, `/benzene/spec`, `/benzene/health` (`httpbinding.EnvelopePath`/`SpecPath`/`HealthPath`), `/benzene/fleet-ui` (`meshd.ViewPath`); no spec-ui, mesh-ui or MCP surface to place | **yes** — `/benzene/invoke`, `/benzene/spec`, `/benzene/health` (`useBenzeneCloudService`), `/benzene/spec-ui` (reading `/benzene/spec?type=benzene`), `/benzene/mesh-ui`; no fleet-ui or MCP surface | **yes** — `/benzene/invoke`, `/benzene/spec`, `/benzene/health`, mesh UI at `/benzene/mesh-ui/` (`mount=` to move it); the matrix names no spec-ui surface |
| **Reserved topics** (wire-contracts §7) | **yes** — `benzene:healthcheck`/`liveness`/`readiness` (+ the .NET-only `contracts`). `benzene:spec` takes `{type?, format?}`, compared case-insensitively. Absent, empty, `null` or `{}` is the Contract Document; it also serves OpenAPI 3.0 and AsyncAPI 3.0, each as JSON or YAML. An unsupported `type` or `format` answers `not-implemented`. `GET /benzene/spec` is an ordinary HTTP mapping onto the topic, so it runs the pipeline and auth, with `type`/`format` taken from the query. `benzene:ping` is answered `ok` with no payload by the router, at any version (`DisablePing()` opts out). Also the collector topics. No `benzene:mesh` topic. Further .NET-only `benzene:*` topics are port extensions | **yes** — `benzene:healthcheck` (no `liveness`/`readiness`). `benzene:ping` (`RouterMiddleware`, `ok`, no payload). `benzene:spec` (`contract.Middleware`: `{type?, format?}`; anything but `benzene`/`json` is `not-implemented`; `WithDocumentType` adds port extensions such as AsyncAPI). `GET /benzene/spec` runs through the pipeline, with its `type`/`format` query parameters as the request, on every HTTP host: `httpbinding.SpecHandler`, and any `Route` mapped to `benzene:spec` through `httpbinding.Handler`, `awslambda.HTTPHandler` or `azurefunctions.Handler`. Also the collector topics. No `benzene:mesh` topic | **yes** — all three health topics. `benzene:spec`: absent, empty, `null` or `{}` is the Contract Document; `benzene`/`openapi`/`asyncapi` × `json`; anything else is `not-implemented` (HTTP 501); no YAML. `GET /benzene/spec` runs the pipeline, with its `type`/`format` query copied onto the request; a cloud service also answers it over `/benzene/invoke`. Also the collector topics. `benzene:ping` is answered `ok` with no payload by the router when no handler claims it. So is any other unclaimed reserved id, the retired `benzene:mesh` included | **yes** — `benzene:healthcheck` (no `liveness`/`readiness`). `benzene:spec` (`spec_interception`, installed by `application_from` for a declared `ContractInfo`): absent, empty, `null` or `{}` is `benzene`/`json`, the Contract Document; any other `type` or `format` answers `not-implemented` (HTTP 501); other document types come from `benzene-openapi`, not the topic. `GET /benzene/spec` copies `type`/`format` onto a `benzene:spec` request and runs it through the pipeline. Also the collector's `benzene:mesh:*` topics; there is no bare `benzene:mesh`. `benzene:ping` is answered `ok` with no payload by the router, on every transport, with no wiring |
| **Transport metadata — topic key and topic sources** (wire-contracts §2; transport-bindings §1 obligation 2) | **yes** — on every metadata transport the topic key (`topic` by default) is read case-insensitively. It is consumed as routing metadata and not surfaced again in the header dictionary. Kafka routes on the record's `topic` header, falling back to the consumed Kafka topic; RabbitMQ falls back to its routing key. Conformance runs every `metadataCases`/`overrideCases` entry, `headersExclude` and the case-insensitive key included, against every metadata binding: SQS (Lambda and self-hosted), SNS, Service Bus (Function and self-hosted), Event Hubs (Function and self-hosted), Pub/Sub, RabbitMQ and Kafka (self-hosted, Lambda, Function). It also runs the `topicSources` fallbacks | **yes** — `wire.ResolveMetadataTopic` and one injectable `ReservedNames`. `metadataCases`/`overrideCases` run through the real entry point of every native-metadata inbound binding, configured with `UseReservedNames`: `gcppubsub`, the Functions Service Bus and Event Hub handlers, `awskafka`, `awssqs` (Lambda and self-hosted), `awssns`, `azureservicebus.Worker`, `azureeventhub.Consumer`, `rabbitmq.Consumer` and `kafka.Consumer`. The consumed key is absent from the headers and read case-insensitively. Every `topicSources` entry is claimed and the non-metadata sources are driven: DynamoDB Streams' `{tableName}:{eventName}`, the HTTP route, EventBridge's `detail-type`, the gRPC method, RabbitMQ's routing-key fallback, and the Kafka-topic fallback on both Kafka bindings | **yes** — every `metadataCases` and `overrideCases` case runs through each native-metadata inbound binding: Lambda SQS and SNS, the self-hosted SQS consumer, Pub/Sub, both Service Bus bindings, the Event Hub consumer, RabbitMQ and the three Kafka bindings. Each reads the topic key case-insensitively and consumes it, so it never reappears as a header. The `topicSources` fallbacks (RabbitMQ's routing key, Kafka's own topic) also run. Kafka's header key is configurable (`topicHeaderKey`) | **yes** — `MetadataKeys` is configured once on the `AppDefinition`, and every binding and sender honours it. The `topic` key is used on SQS (Lambda and self-hosted), SNS, Pub/Sub, Service Bus, Event Hub, Kafka (self-hosted and Lambda MSK) and RabbitMQ. When a record has no `topic` header, these fall back to the consumed Kafka topic or the AMQP routing key; the header always wins. EventBridge uses `detail-type`; DynamoDB Streams uses `{tableName}:{eventName}`. Header keys are lower-cased and matched case-insensitively. The runner drives every `metadataCases` case (`headersExclude` included) and every `overrideCases` case through every host and client of each binding, and every `topicSources` entry from its native shape |
| **gRPC** (wire-contracts §4.2 `code` in `FieldViolation.reason`, spec 1.1) | **yes** — `Benzene.Grpc` (all four RPC shapes incl. streaming, `[GrpcMethod]` routing) + `.AspNet`, `.Client`, `.Versioning`, test helpers; response-side payload downcasting over gRPC deliberately not shipped. A failure's errors travel as `BadRequest` field violations whatever its status, each `code` in `FieldViolation.reason` (`GrpcFieldViolationReason`), and the client rebuilds `message`/`field`/`code` | **partial** — `grpcbinding` (unary interceptor, `benzene-status` trailer, client); streaming not implemented (documented gap). `errors[].code` is written to `FieldViolation.reason` and read back | **yes** — `Benzene.Grpc` (incl. streaming) + `Benzene.Grpc.Client`. Every errored status carries its errors as field violations: `message` → `description`, `field` → `field` (dotted), `code` → `reason`, all three read back. A call that fails before any reply is `service-unavailable` or `timeout`, not an exception | **yes** — `benzene-grpc` (status mapping + server + client; transport SDK an optional extra). Errors ride as `BadRequest` on `grpc-status-details-bin`, each `code` in `FieldViolation.reason`, read back into `code` |
| **Outbound transport failure** (transport-bindings §1 "Outbound clients", SHOULD, spec 1.1) | **yes** — every outbound route transport (`UseSqs`, `UseSns`, `UseEventBridge`, `UseServiceBus`, `UseEventHub`, `UseEventGrid`, `UseQueueStorage`, `UsePubSub`, `UseKafka`, `UseRabbitMq`, `UseBenzeneMessageOverHttp`, and the plain REST `UseHttp`) yields a result when a send fails before a reply or before the broker accepts it. That is `timeout` when the deadline elapsed (a `TimeoutException` in the chain, or the route's `UseTimeout()`) and `service-unavailable` otherwise, mapped by each converter's `ITransportFailureMapper`. The `IBenzeneMessageClient` clients, `StepFunctionsClient` and the mesh dispatchers classify the same way. The Service Bus / Event Hubs batch clients report a batch that never reached the broker as every entry failed. **Still throws:** request serialization errors and the caller's own (ambient) cancellation | **yes** for every reply-receiving client — `httpclient`, `awslambdaclient`, `inprocess.Sender` and `grpcbinding.Client` return `timeout` (deadline elapsed) or `service-unavailable` through one rule, `client.CallFailure`, never a Go error. **yes** for the one-way clients too (`awssqs`, `awssns`, `awseventbridge`, `gcppubsubclient`, the Azure clients, `rabbitmq`, `kafka`, `awsstepfunctions`): a send whose SDK or broker call errors before the broker accepts it goes through the same `client.CallFailure`, each binding with a deadline-exceeded test. The matrix does not say how a rejection the broker itself answered maps | **yes** — `HttpClientMiddleware` maps a rejected `fetch` to `timeout` (abort, timeout codes) or `service-unavailable` (`transportFailureResult`), and so do the mesh dispatchers and the gRPC client. A one-way send whose SDK call rejects is a result too (`failedSendResult`): `timeout` for a timed-out send, otherwise `service-unavailable`. A rejection the broker itself answered maps its status through the reverse HTTP table (`403` → `forbidden`, `429` → `too-many-requests`). The batch clients never throw for a transport failure: a failed chunk, or a Service Bus `createMessageBatch` / Event Hub `createBatch` that rejects, fails every entry that batch would have carried. **Still throws (stated):** an Event Hubs event too large for a batch, and the low-level `AwsLambdaClient`, because the result-returning Lambda message client is not ported | **yes** — one classifier, `benzene.core.failed_call_result`: `timeout` when the deadline elapsed (builtin and SDK timeout exceptions), otherwise `service-unavailable`. `HttpMessageSender` maps an `OSError` (`timeout=` bounds the call); `GrpcMessageSender` maps `DeadlineExceeded`, `Unavailable` and a closed channel. `LambdaMessageSender` and every one-way sender catch every SDK error, now `timeout` for a timeout. Tested over real sockets. **Deliberately not:** turning a defect into a result, so a non-`OSError` transport error still raises. The one-way senders' catch-all maps an SDK's own *reply* (access-denied, throttle) to `service-unavailable` (stated, unchanged) |
| **Kafka** | **yes** — `Benzene.Kafka.Core` (self-hosted worker), plus the Lambda MSK and Functions Kafka triggers (AWS and Azure rows); routes on the `topic` header, falling back to the consumed Kafka topic | **yes** — `kafka` module (consumer-group consumer + client) and the zero-dep `awskafka` Lambda handler; both route on the `topic` header, falling back to the Kafka topic | **yes** — `Benzene.Kafka.Core`, `Benzene.Aws.Lambda.Kafka`, `Benzene.Azure.Function.Kafka`; all three route on the `topic` header (`topicHeaderKey`), falling back to the consumed Kafka topic | **yes** — `benzene-kafka` (SDK duck-typed/optional); the `topic` header, else the consumed Kafka topic |
| **RabbitMQ** | **yes** — `Benzene.RabbitMq` (Explicit ack default); the `topic` key, falling back to the routing key | **yes** — `rabbitmq` module (consumer + client); the `topic` header, falling back to the routing key | **yes** — `Benzene.RabbitMq` (+ test helpers; no dedicated doc page — stated omission); routing-key fallback run by conformance | **yes** — `benzene-rabbitmq` (topology deliberately yours); the `topic` header, else the AMQP routing key |
| **AWS** | **yes** — Lambda SQS/SNS/EventBridge/Kafka(MSK)/S3/Kinesis/DynamoDB Streams; self-hosted SQS (`Benzene.Aws.Sqs`); Step Functions client | **yes** — `awslambda`, `awssqs`, `awssns`, `awseventbridge`, `awss3`, `awsdynamodb`, `awskinesis`, `awskafka`, `awslambdaclient`, `awsstepfunctions`. The API Gateway custom authorizer and a direct X-Ray SDK are not implemented; OTel/ADOT is the stated route | **yes** — `Benzene.Aws.Lambda.*` (incl. X-Ray), `Benzene.Aws.Sqs`, `Benzene.Clients.Aws.*` | **yes** — `benzene-aws`: Lambda host incl. all major triggers (DynamoDB Streams topic `{tableName}:{eventName}`); outbound senders; self-hosted SQS consumer; test hosts |
| **EventBridge header carrier (`benzene-headers`)** | **yes** — `Benzene.Aws.Lambda.EventBridge` reads wire headers from the reserved `benzene-headers` key inside `detail` and writes them there outbound (no dual-accept of the pre-0.2.0 `_benzeneHeaders`); asserted by the conformance suite | **unknown** — the matrix's EventBridge row does not name the header key | **yes** (attested through conformance) — the conformance row asserts the `benzene-headers` key from `transport-metadata-cases.json`; the EventBridge adapter and client ship in the AWS row | **yes** — `benzene-headers` is one of the reserved names in `MetadataKeys`, configured once and taken by the EventBridge decoder and sender |
| **Azure** | **yes** — Function triggers Service Bus/Kafka/Event Grid/Event Hub/Queue Storage/Cosmos DB **+ Blob** (`Benzene.Azure.Function.BlobStorage`, deliberately non-routed: delivers blob name + content to `UseBlob(...)`); self-hosted Service Bus + Event Hub workers | **partial** — `azurefunctions` (HTTP, Queue/Service Bus, Cosmos, Timer, Event Grid, Event Hub) + self-hosted workers + clients. **Blob trigger deferred by design** (SDK-typed; no fabricated custom-handler shape). **Functions Kafka trigger not implemented** (payload unpinned) | **yes** — `Benzene.Azure.Function.*` (incl. Blob, Kafka, Timer), self-hosted workers, `Benzene.Clients.Azure.*` | **yes** — `benzene-azure` (incl. Blob inbound); no self-hosted Service Bus, Event Hub or Cosmos DB worker and no Functions Kafka trigger (stated) |
| **GCP** | **partial** — matrix names only `Benzene.GoogleCloud.Functions.PubSub` (+ the `.GoogleCloud.PubSub` client) | **yes** — `gcpfunctions` (HTTP + CloudEvent), `gcppubsub` (zero-dep push), `gcppubsubclient`; pull subscriptions deliberately off the zero-dep path | **yes** — `Benzene.GoogleCloud.Functions.*` + Pub/Sub client | **yes** — `benzene-gcp` (HTTP + Pub/Sub); Eventarc/Cloud Tasks plainly unbuilt |
| **Mesh — service side** (Mesh module, Stable) | **yes** — `Benzene.Mesh.Wire`. The register body is the Contract Document `benzene:spec` serves (`Benzene.Schema.OpenApi`, spec row), with `runtime`/`placement`/`profile`/`degraded`, sent on `benzene:mesh:register`. Heartbeats (`MeshHeartbeat`) carry `serviceVersion` and the registered `registrationHash`. Sends come from `MeshOutboundRegistry` (`AddSendDeclarations` / the cloud service's `WithSends`). A generated client declares the topics it calls as sends of its consumer; a (topic, version) declared twice is listed once. Trace middleware (W3C join), batching HTTP exporter, deduplicating issue emitter (`UseMeshIssues` + `HttpMeshIssueExporter`); `Benzene.Mesh.Reporting` push self-report. **Issue `errors` (spec 1.2):** `UseMeshIssues` takes each failing result's (`field`, `code`) pairs, never the message; an error with neither, or with empty ones, is not counted. The exporter counts them per issue as deltas, holds at most 100 distinct pairs a window, and flushes the ten highest in the spec's order. Every feed is opt-in by design. **Meshed service:** given a collector, `UseBenzeneCloudService` wires M1 (trace feed, registration of the served document, heartbeats) in one call. With `WithHandlers`, a reduced document (`events`/`sends` named in `degraded`) registers at wire-up and the first invocation re-registers the complete one. **M2:** the inbound join is wired, and the joined `tracestate` is carried on `MeshSpan.TraceState`. **Outbound forwarding of the mesh span** is `UseMeshTraceContext()`, per route or on every route through the `OutboundRoutingBuilder`, on sends and published events alike. It stamps `MeshSpan.Current`'s `traceparent`/`tracestate` and wins over `UseW3CTraceContext()`'s `Activity.Current`. So a downstream TraceEvent's `parentSpanId` is the upstream TraceEvent's `spanId` (mesh §3). The matrix also says the cloud service's M2 self-check is satisfied only when the routing forwards it (or there are no outbound routes); the "deliberately not" column and the spec-endpoint row now say the same (note 15). Passes the producer groups (its register body) and `mesh-trace-cases.json` | **yes** — `mesh/`. `mesh.Announcer` registers the Contract Document `contract.Derive` builds, the one `benzene:spec` serves. `Registration` fills a detected `placement` (`DetectPlacement`) when the document carries none. `runtime` (`"go"`), `profile` (`cloudservice.New`'s self-check) and `degraded` (`events` and `sends` when no `OutboundRegistry` was supplied) come from the derivation; there is no `instanceId` in the body. Heartbeats carry `serviceVersion`, the registered `registrationHash` and `healthcheck.Run`'s aggregate; registration is retried until it lands. Also `TraceMiddleware`, outbound `WithTraceContext`, log/push trace exporters and an issue emitter (`IssueMiddleware`/`PushIssueExporter`), each feed independent. **Issue `errors` (spec 1.2):** each failing result's `field`/`code` pairs, never the message, counted per issue and bounded to the ten most frequent. **Meshed service, app-wired:** M1 is `Announcer` (register + heartbeats) plus `TraceMiddleware` with a `PushExporter`. M2: `TraceMiddleware` joins the inbound `traceparent` and adopts its `tracestate`, and `WithTraceContext` forwards this span's `traceparent`, `tracestate` unchanged, on every outbound message. That covers sends with a reply, one-way sends and published events (`responseevents.NewSenderPublisher`). `cloudservice.New` wires neither and reports them as not wired. **Deliberately not:** a language-native `exceptionType` (a panic becomes a result first); a `region` outside AWS Lambda | **yes** — the register body is the Contract Document `benzene:spec` serves, byte for byte. `MeshAnnouncer` (`Benzene.CloudService`) registers it at host boot (or on the first invocation, for a host that runs no start-up checks) and retries until the collector answers. It then heartbeats `{service, serviceVersion?, instanceId, registrationHash, sentAt, health}`. The document carries `runtime: "typescript"`, `placement` (configured with `withPlacement`, else `self-hosted`; `cloud` is not detected and `region` appears only when configured — stated), `profile` and `degraded`; no `instanceId`. `degraded` names `events` and `sends` whenever no outbound registry and no response-event/sender feed is registered. Built from `Benzene.Mesh.Wire` (`MeshRegistration`, `MeshHeartbeat`, `MeshOutboundRegistry`, `useMeshTrace`, `HttpMeshTraceExporter`), `.Contracts`, `.Dispatch` and per-platform sources. **M2:** `useMeshTrace` joins an inbound `traceparent` and carries the caller's `tracestate`. `useMeshTraceContext`, **opt-in per outbound route**, forwards both from `MeshSpan.current` on sends and published events alike. **Stated gap:** the cloud service cannot wire `useMeshTraceContext` (or see whether the app did). A raw `IBenzeneMessageClient` call outside the routes forwards nothing. `useW3CTraceContext` forwards the OpenTelemetry span, not the mesh span. **No issue emitter** (stated): the `benzene:mesh:issues` wire shapes ship (`MeshIssue`, `MeshIssueBatch`, `MeshIssueError`/`MeshIssueErrors`, classification, fingerprint), but not an emitter, so a service on this port sends no issues and no `errors`. The matrix gives no reason beyond the feed being optional (mesh §4.1) | **yes** — `benzene-mesh`. The register body is `registration_document(definition)`, identical to what `benzene:spec` serves (`runtime` `"python"`, `placement`, `profile`, `degraded`; no `instanceId`). `MeshFeedSender` sends registration, heartbeats, traces and issues; `Heartbeat.for_document` carries the document's `registrationHash` and `serviceVersion`. **Issues come from `IssueAggregator`, fed by hand; there is no issue middleware** (stated). **Issue `errors` (spec 1.2):** `record(..., errors=result.errors)` counts each error's (`field`, `code`), never `message`, per issue as a delta; an error with neither is not counted. A window holds at most 100 distinct pairs, and `flush()` bounds and orders them. A failure's detail reaches the feed only when the caller passes its errors. **Meshed service:** M1 is the three feeds through `MeshFeedSender`. M2: `trace_middleware` joins the inbound `traceparent`, keeping its `tracestate`; an invalid one starts a fresh trace and drops it. `with_trace_propagation` forwards `traceparent` + `tracestate` on whatever `MessageSender` it wraps. Sends and published events share that one port, so events are covered once the publisher is wrapped (tested). The registration is pushed by the host loop (example and deploy code), **not a library announcer** (stated). `degraded` is only what the composition root declares. `placement.cloud` is configured, never detected; `region` is never guessed |
| **Mesh — collector** (Mesh module, Stable) | **yes** — `Benzene.Mesh.Collector`. A register body is a Contract Document of which only `service` is required: no dialect check, and unmodelled members are kept. Consumers come from `requests`, providers from `events` ∪ `sends`; `benzene:` entries are ignored. An absent or `degraded` member is unknown: no edges, and a trace on it is never filed as undeclared. A trace of a reserved `benzene:` topic is never drift. The carried `registrationHash` is trusted. A heartbeat's hash is compared with the registration for the same `(service, serviceVersion)` (Service Versions row). No legacy body is read. Read models (Provisional): the fleet row's `topics` counts `requests`; `missingFeeds`' `descriptor` means never registered; `ServiceView.registration`, and `InstanceView.registrationHash` (+ `serviceVersion`) (note 14). **Spec 1.2:** an issue's `errors` decode leniently (a non-object entry, one with neither member, or a `count` that is not a positive integer is skipped, never failing the batch) and merge by (`field`, `code`) under the same bound and order. A fleet trace summary carries the flow's `status`: its earliest failing event's when it failed, else its earliest event's. The push store, the Jaeger source and enriched X-Ray rows set it; Tempo and X-Ray summary-plane rows hold no Benzene status and omit it (note 20). Also the fleet/tracing/usage backends, aggregator + discovery + artifact stores, gated live dispatch and the OIDC login gate. Durable storage is deliberately out; the aggregator's published catalog artifacts are the durable output. Passes `mesh-collector-cases.json`, `mesh-issue-cases.json` and `mesh-service-version-cases.json` | **yes** — `meshd`: register/heartbeat/traces/issues ingest, plus `benzene:mesh:query:*` read models over an in-memory store with a bounded trace ring. A register body is a Contract Document read loosely: only `service` is required (no dialect check; schemas never interpreted). `requests` are consumers, `events` ∪ `sends` providers; `benzene:` entries are ignored. An absent or `degraded` member is unknown. A reserved `benzene:` topic is never an undeclared edge (`contract.IsReserved`). Each declared edge reports `providerActivity`/`consumerActivity`, with `lastObservedAt` **absent when never observed**. A topic row's `lastSeen` is **absent for a topic with no traffic**. The carried `registrationHash` is trusted, and a heartbeat's is compared with the service's one registration (`hashMatches`). No legacy body is read. Read models (Provisional): the fleet row's `topics` counts `requests` and `produces` counts events and sends; the row carries `serviceVersion`, `runtime`, `transports` and `placement`. `missingFeeds`' `descriptor` means not registered; the service view's `descriptor` is the registered document as sent. **Spec 1.2:** an issue's `errors` (field/code pairs) merge by identity under the ten-entry bound (`mesh.MergeIssueErrors`), a malformed entry skipped, never failing the issue. A fleet flow carries its `status`, "its first failure's, else its first event's", from the in-memory store. Passes `mesh-collector-cases.json` and `mesh-issue-cases.json`. **Not implemented:** Service Versions, and the produced-vs-consumed version-skew read model. Durable storage is deliberately out | **yes** — `Benzene.Mesh.Collector`, plus aggregator, reporting, discovery, fleet, usage and storage packages. Reads a registration as a Contract Document: only `service` is required (no dialect check); `registrationHash` is trusted. `requests` are consumers, `events` ∪ `sends` providers; `benzene:` entries are ignored. An absent or `degraded` member is unknown. A `benzene:` topic is never an undeclared edge. A heartbeat's hash is compared with the service's one registration (keyed by `service`; no Service Versions). No legacy body is read. Read models (Provisional): the fleet/service rows' `binding` is now `transports`; the instance view has `registrationHash` (+ `serviceVersion`); the service view keeps `descriptor` (note 14). Trace `startedAt` and issue `firstSeen`/`lastSeen` ingest as ISO-8601 or epoch ms. **Spec 1.2:** an issue's `errors` merge by (`field`, `code`) (an empty member is an absent one) with `count += delta`, bounded to the ten highest in the spec's order, and appear on the fleet view's issue when it holds any; a malformed entry is skipped, the issue kept. A fleet flow (`TraceSummary`) carries `status`, the earliest-started failing event's or else the earliest-started event's. The in-memory store, the Jaeger source and enriched X-Ray rows set it, X-Ray omitting it when its error flag and the spans disagree; Tempo and X-Ray summary-plane rows omit it. The aggregator's drift flag reads the **served** `contractHash` | **yes** — `benzene-mesh` `MeshCollector`. The register body is a Contract Document: `requests` are consumers, `events` ∪ `sends` providers (a publisher is a provider), and `benzene:` entries are ignored. Only `service` is required (no `schemaDialect` or hash check; `registrationHash` trusted). An absent or `degraded` member is unknown. A reserved `benzene:` topic is exempt from undeclared drift (tested). `benzene:mesh:query:topic` reports `providerActivity`/`consumerActivity` per declared edge as `{service: {"lastObservedAt": …}}`, or `{}` when never observed. An undeclared trace files one `contract-drift` issue in the fleet query's `issues`. **Spec 1.2:** an issue's `errors` merge by (`field`, `code`) (an empty string is an absent member) with `count += delta`, bounded to ten in the spec's order, and appear on the fleet query's issue only when it holds any. An entry that is not an object, has neither member or a non-string one, or whose `count` is not a positive integer (JSON `true`, `1.5` and `2.0` included) is skipped, the issue kept (`decode_issue_errors`, `merge_issue_errors`). **New with this change:** the fleet query carries `traces`, the newest 20 flows, `{traceId, events, services, failed, startedAt, durationMs, status?}`. `status` is the earliest-started failing event's when the flow failed, else the earliest-started event's, omitted when the deciding event has no status. No legacy body is read. `MeshPoller` ingests each served `/benzene/spec` document as the registration, unchanged. Catalog store (snapshot v4, each document published as `specJson`), issue detection, inbox; containerised host + Terraform; in-memory store by design. **Service Versions not implemented**. Passes `mesh-collector-cases.json` and `mesh-issue-cases.json`. The matrix does not name its fleet-row, service-view or instance-view fields (note 14) |
| **Mesh — service versions** (Service Versions module, Provisional: .NET) | **yes** — `Benzene.Mesh.Collector` keys its catalog by `(service, serviceVersion)`. The fleet query keeps one row per service, headlined by the most recently registered version; the service query's `versions` lists every live version as `{serviceVersion, topics}`. A heartbeat's `registrationHash` is compared with the registration of its own `(service, serviceVersion)`, and is null for a version nothing registered. A topic counts as declared when **any** live version declares it (a stated approximation: a TraceEvent carries no `serviceVersion`). At most `maxVersionsPerService` (default 8) versions are retained. Runs `mesh-service-version-cases.json` and `mesh-version-order-cases.json` | **no — unbuilt** — "Service Versions is not implemented": the catalog keys by `service` alone; both service-version fixtures are listed opt-outs | **no — unbuilt** — "this port's collector has no service-version identity" (keys by `service`); neither service-version fixture runs | **no — unbuilt** — "Not implemented: Service Versions": the catalog is keyed by `service` alone (mesh §5's non-module rule); both fixtures in `UNRUN_FIXTURES`, with reasons |
| **Mesh — UI** | **yes** — `Benzene.Mesh.Ui` serves the **canonical** `mesh-ui.html` at `/benzene/mesh-ui` (a byte-identical embedded copy, CI drift-checked against the benzene-ui build), plus `mesh-spec-ui.html` (the per-service spec view) | **yes** — `meshd.ViewHandler` serves a **Go-native** `view.html` at `/benzene/fleet-ui`, polling the collector's fleet query. It is deliberately *not* the canonical `mesh-ui.html`; the wire contract is the interop point. No spec viewer | **yes** — `Benzene.Mesh.Ui`, default `/benzene/mesh-ui` (the matrix does not say whether it serves the canonical `mesh-ui.html` or its own); the spec viewer is `Benzene.Spec.Ui` (spec row) | **yes** — serves the **canonical** `mesh-ui.html` vendored from the main repo at `/benzene/mesh-ui/` (`mount=` to move it), projecting its catalog into the mesh-ui artifacts; the matrix names no separate spec viewer |
| **Health checks** | **yes** — `Benzene.HealthChecks*` reserved-topic middleware (`healthcheck`/`liveness`/`readiness`/`contracts`, deliberately non-overlapping). A health query whose checks ran answers `ok`, with the verdict in `isHealthy`. Every HTTP route mapped to a health topic answers **503** when `isHealthy` is false (a warning stays 200), pinned by `httpRules.healthEndpoint`. Disk/HTTP/TCP/DynamoDB/EF/schema/Service Bus checks, client-side downstream checks, grpc.health.v1 bridge. A dedicated `/health` route is deliberately not shipped: health is a topic, and `Benzene.CloudService` maps `GET /benzene/health` | **yes** — `healthcheck/`: `ok` whenever the checks ran, with `isHealthy` as the verdict (a warning does not flip it); `GET /benzene/health` answers 503 when it is false. TCP/HTTP/disk checks (category-only errors) + `clienthealthcheck` contract probe. No `liveness`/`readiness` topics or routes | **yes** — `ok` whenever the checks ran, with the verdict in `isHealthy`; a health-topic HTTP route answers 503 when it is false (a warning stays 200). `Benzene.HealthChecks*` (disk/HTTP/TCP/DynamoDB/TypeORM/schema/Service Bus + transport-embedded + client-side) | **yes** — `benzene-core` `health.py`: `benzene:healthcheck` answers `ok` with the verdict in `isHealthy`, per-check `ok`/`warning`/`failed` (a warning does not flip it). `GET /benzene/health` maps it to 200/503. Installed by `health_interception` or by `StandardPaths(health=...)`. No `liveness`/`readiness` topics or routes |
| **Spec endpoint & Cloud Service Profile** | **yes** — `Benzene.Schema.OpenApi` derives the **Contract Document**. Shape: `schemaDialect: "openapi-3.0"`; `service`/`serviceVersion`/`versionScheme`/`description` from `IApplicationInfo`; `requests` with no `benzene:` entries and no `response` for a no-payload handler. `events` come from event declarations (`AddResponseEventDeclarations`); `sends` from send declarations and generated clients. A (topic, version) in both fails the document. `contractHash` + `registrationHash` are always present (`ContractHash`, RFC 8785); examples are author-supplied only. `runtime` (`dotnet`), `placement` and `profile` come from a registered `ContractDocumentDeployment`, and `degraded` names `requests` when no handler registry is wired; all four sit outside both hashes. Schemas are Swashbuckle-derived in the OpenAPI 3.0 dialect, and `required` is every always-emitted property. Component names are the simple type name or `[SchemaName]`. A collision throws `ComponentNameCollisionException` **at start-up by default under `UseBenzeneCloudService`** (`WithoutContractDocumentStartUpCheck()` opts out, to spare a Lambda cold start). Under manual composition it throws at start-up only with the opt-in `AddContractDocumentStartUpCheck()`, otherwise on the first spec request, or at build time in `benzene-descriptor` (flagged below). A document without `schemaDialect` is rejected by the reader (Codegen row). Passes `derivationCases`, `namingCases`, `specTopicCases`, `specHttpCases` and every non-projection hash case. **`Benzene.CloudService`**: `UseBenzeneCloudService` wires **R1–R5 and R7** at the `/benzene/*` defaults. Given a collector, it also wires M1 and M2's inbound join (mesh row). Its honest `CloudServiceProfileReport` stamps only R1–R5/R7 on `profile.missing` (no collector needed). It reports `MeshRequirements`/`MeshMissing`/`IsMeshed` apart; `IsMeshed` requires the profile. Its M2 check needs the inbound join and every outbound route forwarding the mesh span with `UseMeshTraceContext()`, or no outbound routes. It cannot see a per-route `UseMeshTraceContext()` or a route registered after the cloud service is wired, and says so (note 15). Deliberately sugar-only. **`Benzene.CloudService.Probe`** audits R1–R5/R7, all observable: R5 is `/benzene/spec` **and** `benzene:spec` over the envelope. It reports M1/M2 as `Inconclusive`, never counted toward `--fail-on`. A stale `R6`/`R8` is carried, never interpreted. **`Benzene.Descriptor`** (`benzene-descriptor`) emits the `{name}.spec.json` Contract Document from a built service; `--service-version`/`--version-scheme`/`--cloud`/`--region` reach the document. **`Benzene.Spec.Ui`** | **yes** — the `contract` package (root module). `contract.Derive` builds the Contract Document at start-up: `schemaDialect: "openapi-3.0"`, `service`/`serviceVersion`/`versionScheme`/`description`; deployment-side `runtime`/`placement`/`profile`/`degraded` outside both hashes; `requests`/`events`/`sends`; no `benzene:` entries; no `response` for a handler returning `struct{}`; author examples only. Named structs are hoisted into `components`, named by `BenzeneSchemaName()` or the Go name; undeclared generic instances are inlined, and typed enums derive as their base type. A component-name collision **fails before the first message** (`*contract.NameCollisionError`). Events and sends come only from `contract.RegisterEvent`/`RegisterSend`; a (topic, version) in both is a start-up error. Deriving `responseevents` mappings into event declarations is not implemented. Both hashes are computed in the root module (`contract.HashDocument`, hand-written RFC 8785 `internal/jcs`). Served on `benzene:spec` and on `GET /benzene/spec` on every HTTP host. **`cloudservice.New`** wires **R1–R5 and R7**; placement is detected unless `WithPlacement`. It returns its derivation error or an honest `ProfileReport`: `Requirements` is the R1–R5/R7 checklist the document carries as its `profile` (`missing` omitted when none). `Mesh` reports M1/M2 apart (`Meshed()` requires the profile). `New` wires **neither M1 nor M2**, and `Mesh.Unsatisfied()` says so. No Go consumer reads `profile.missing`. **`cloudserviceprobe.Run`** gives tri-state verdicts for R1–R5/R7: R5 needs both halves, and R7 is `Inconclusive` on non-default paths. `Report.Mesh` carries M1/M2 as always `Inconclusive`. The matrix does not state the `required` rule for a defaulted field | **yes** — `Benzene.Schema.OpenApi` serves the **Contract Document**: `schemaDialect: "openapi-3.0"`; `service`/`serviceVersion`/`versionScheme`/`description` from the application info; `runtime`/`placement`/`profile` from `IServiceDeploymentInfo`; `degraded` for a member with no source. `requests` have no `benzene:` entries, no `response` for a no-payload handler, and carry `httpMappings`. `events` come from response events and `{ kind: 'event' }` declarations; `sends` from sender definitions, `{ kind: 'send' }` declarations, `MeshOutboundRegistry` and `withOutbound`. Both hashes come from `@benzenejs/codegen-core`, shared with the generator. `components` are hoisted from Zod/Joi/Yup/bring-your-own schemas, named by declaration (Zod `.meta({ id })`, Joi `.id()`, Yup `.meta({ id })`, `MapTypeJsonSchemaSource` `{ name }`) or else the class name. A collision, or a topic declared as both event and send, fails the start-up checks. A cloud service also answers `benzene:spec` over `/benzene/invoke`. The `asyncapi` document reads handlers, response events, sender definitions and `MeshOutboundRegistry` declarations. **Stated gaps:** no `messageEndpoint`; no YAML; Joi/Yup nested objects are inlined (Zod hoists); undeclared class names do not survive bundler renaming; no synthesized examples. `required` is every always-emitted property, a defaulted one included: Zod `.default()`/`.prefault()`/`.catch()`, Joi `.default()` and a declared Yup `.default()` all make a property required. **`Benzene.CloudService`**: the wiring-time `CloudServiceProfileReport` evaluates R1–R5/R7 into `profile.missing` (omitted when none). The meshed-service claim is a separate `MeshedServiceReport`: M1 needs a collector; M2 is graded on the inbound trace join, since the self-check cannot see outbound routes (stated); `isMeshed` requires the profile. **`Benzene.CloudService.Probe`** audits R1–R5/R7 over HTTP (R5 on both halves) and reports M1/M2 as `Inconclusive`. **`Benzene.Spec.Ui`** | **yes** — `benzene-core` derives the Contract Document **once at the composition root** (`AppDefinition(contract=ContractInfo(...))`, `application_from`). Sources: the registry, the `OutboundRegistry`'s event and send declarations, the router's routes and the envelope endpoint. Shape: `schemaDialect: "openapi-3.0"`, `service`/`serviceVersion`/`versionScheme`/`description`; deployment-side `runtime` (`"python"`)/`placement`/`profile`/`degraded` declared on `ContractInfo`, outside both hashes; `requests`/`events`/`sends`; no `benzene:` entries; no `response` for `response_type=NoPayload`; author examples only. `contractHash` + `registrationHash` are always present (`contract_hash.py`, RFC 8785 via `rfc8785`). Every dataclass, **pydantic v2 model** and `str`/`int` enum is hoisted. **`required` lists every dataclass field, a defaulted one included.** A pydantic model is duck-typed on its v2 API (no core dependency) and read as `model_dump(by_alias=True)` writes it: a property per field and `@computed_field`, under its serialization alias. Each is `required` unless `Field(exclude=True)` or `exclude_if`. `{}` is kept for a custom serializer; a `RootModel` is its root value. One marshaler (`to_jsonable`) writes standard JSON forms, and the derivation matches them: `UUID` (`format: uuid`), `date`, `datetime`, `timedelta` (ISO 8601 duration), `time` and `Path` as strings, and `Decimal` as a JSON number. `Literal[...]` takes §4.2's enum row, and is `{}` only when its values have no single JSON type. **Stated boundaries:** no validation constraints; a plain `Enum` or a mixed-type `Literal` is `{}`; a naive `datetime` is written as UTC (`+00:00`), so every `datetime` is RFC 3339; a naive `time` has no offset (note 12). A name collision fails building the app (`@schema_name("…")` is the remedy). Served on `benzene:spec`, answered wherever the transport carries a reply, and on `GET /benzene/spec` through the pipeline. The same document is the register body. Passes the four producer groups and every hash case. `derivationCases` run twice, from the canonical dataclasses and from the same types as pydantic models, to the same hashes. **The self-check** (`evaluate_cloud_service_profile`) grades R1–R5/R7 into `missing`. R3 is the topic intercepted, plus `/benzene/health` only where there is an HTTP surface; R7 is met trivially with no HTTP surface; R5 is the topic, plus `/benzene/spec` where there is HTTP. It grades M1/M2 apart on `report.mesh` (`is_meshed` requires the profile; M2 is graded on the propagation declared). `declare_cloud_service_profile` puts only the profile verdict on the document. A stale `R6`/`R8` is carried, never acted on. **`probe.py`** audits R1–R5/R7, with R5 and R3 each on both halves (`GET` path and topic over `/benzene/invoke`). It reports M1/M2 as `inconclusive`, never counted toward `is_clean`. Q11 (pydantic payloads deriving to `{}`) is closed |
| **Codegen & typed clients** (Contract Document module, client generator) | **yes** — `Benzene.CodeGen.Core`/`.Client` read the Contract Document. They reject an unknown `schemaDialect` (`UnknownSchemaDialectException`) **and a missing one** (spec 1.1; passes `missing-schema-dialect-is-rejected`). A legacy (spec-0.2.0) document is read only through the explicit opt-in `DeserializeAllowingLegacyShape`, with a warning. The conformant `Deserialize` that the CLI and the gate use never reads one (note 12). They ignore `benzene:` entries. Include-list and topic-scoped projections recompute both hashes, and a generated client's `HashCode` is its projection's `contractHash`. A generated client declares the topics it calls as sends of its consumer. `benzene diff` ignores `benzene:` entries and refuses a legacy baseline (`LegacyBaselineException`). `SchemaHealthCheck` publishes the **served** `contractHash`. Passes `parseCases`/`topicScopeCases`/`schemaClosureCases` and every hash case. Also the `benzene` CLI, MSBuild build-time generation (`Benzene.CodeGen.Build`), and `Benzene.Clients` with HTTP/InProcess/AWS/Azure/GCP transport clients. `Benzene.CodeGen.Terraform` is deliberately unshipped | **yes** — `codegen` module (`contractdoc` + `gengo` + `cmd/benzene-codegen`): typed clients from a Contract Document. Rejects an unknown `schemaDialect`, a missing one included (`missing-schema-dialect-is-rejected`), and ignores `benzene:` entries. Projections recompute both hashes with `contract.HashDocument`. An absent response generates `Result[struct{}]`; the nullable wrapper becomes a pointer. Passes every client-generator group and every hash case. `asyncapi/` and `openapi/` generate AsyncAPI 3.0 and OpenAPI 3.0 from the Contract Document. `clienthealthcheck` compares the served `contractHash` with a generated client's | **yes** — `Benzene.CodeGen.Client` (CLI): typed clients from a Contract Document. Rejects an unknown `schemaDialect` and a document with none (contract-document §1; `missing-schema-dialect-is-rejected`), ignores `benzene:` entries (no `--include-reserved`), reads `sends`, types an absent response `void`, and embeds its projection's `contractHash`. The hash is `@benzenejs/codegen-core`, shared with the runtime. The schema health check and the aggregator's drift flag read the **served** `contractHash`. Also `Benzene.Clients` (routing/retries/fan-out) and `Benzene.Clients.InProcess` | **yes** — `benzene-codegen-client` (`benzene-codegen` CLI): rejects an unknown **or missing** `schemaDialect`, ignores `benzene:` entries, and reads `sends`. Types an absent response `Result[None]` and the nullable wrapper `T \| None`. The embedded `CONTRACT_HASH` is `benzene-core`'s `contractHash` over the client's projection. Passes `parseCases`/`topicScopeCases`/`schemaClosureCases` and the projection hash cases. Also `benzene-openapi` (OpenAPI 3.1 from the registry) |
| **Settlement of returned failure results** (transport-bindings §1, obligation 8) | **yes** — on every queue-shaped and one-shot binding, a failure result and a missing result are redelivered. An unmatched topic is a `not-found` failure everywhere. **Every binding has an at-most-once override** (existing knobs, or an `AtMostOnce` value/flag), and `.UseUnmatchedTopicAsIgnored()` works on every routed binding. The stream allowance is used and documented: the self-hosted Kafka and Event Hub workers skip-and-continue by default, and the Lambda MSK, Functions Kafka and Functions Event Hub triggers acknowledge a *missing* result (a failure result still redelivers). Unit of settlement stated per binding; guarded by `SettlementContractDefaultsTest` + `SettlementDefaultsConformanceTest`. No `envelope-async` binding | **yes** — every one-way binding settles through `envelope.Settlement`. A failure result, a missing result, an unmatched topic, a middleware error and a recovered panic all redeliver. Only the self-hosted `kafka.Consumer` defaults to skip-and-continue (documented; `envelope.AtLeastOnce` opts in to redelivery). `envelope.AtMostOnce` + `IgnoreUnmatchedTopics` on every binding. `awslambda.AsyncEnvelopeHandler` for `Event` invokes. The self-hosted Event Hub consumer redelivers a failed event in place. Asserted against `settlementDefaults`. A middleware panic on a self-hosted worker escapes `Run` and is redelivered from the broker or the uncommitted position (stated divergence) | **yes** — one rule (`Settlement.ts`, `isSettledAsFailure`) on every one-way binding. Queue and one-shot bindings redeliver a failure result, a missing result, an unmatched topic and an uncaught error. `atMostOnce` + `ignoreUnmatchedTopics` on every binding. The stream bindings skip-and-continue a missing result and an unmatched topic by default (`ignoreUnmatchedTopics` defaults `true` there); the two self-hosted stream workers skip-and-continue failures too, "as .NET does". The Timer trigger acknowledges a tick that set no result (spec-permitted). `envelope-async` via `invocationType: 'Event'`. Asserted against `settlementDefaults` | **yes** — one rule (`dispatch_one_way`, `isSuccessful` authoritative) on every one-way binding. A failure, a missing result, an unmatched topic and an uncaught exception are redelivered. `Settlement(at_most_once=..., ignore_unmatched_topics=...)` on every host. Only the self-hosted Kafka loop skip-and-continues (`stop_on_failure=True` for at-least-once). `envelope-async` via `AwsLambdaApp(invocation_type="Event")`. `idempotency()` never remembers a missing result. Each host's `SETTLEMENT_DEFAULTS` is checked against the fixture |
| **Outbox** | **yes** — `Benzene.Outbox` (+ `.DynamoDb`, `.EntityFramework` stores); at-least-once only; exactly-once and cross-envelope ordering deliberately out of scope. The relay reschedules a send that returns a failed result (stated with the transport-failure rule, Outbound transport failure row) | **no — unbuilt** (stated plainly; the only trace is the `responseevents.Publisher` seam) | **no — deliberate** — "writing the outbox row inside *your* DB transaction is application territory". Documented seams (`IResponseEventPublisher`, `IUnitOfWork`) + a step-by-step cookbook; the matrix itself flags this as an honest divergence from .NET, not parity | **no — unbuilt** (stated plainly; `ResponseEventSink` can feed one you build) |
| **Claim check (oversized payloads)** (Claim Check module, Provisional: .NET) | **yes** — `Benzene.ClaimCheck` + `.Aws.S3`, `.Azure.Blob` stores; `benzene-claim-check` header contract | **no — unbuilt** (no package, no seam; stated plainly) | **no — unbuilt** ("no stated design reason — it is simply unbuilt") | **no — unbuilt** (stated plainly) |
| **Idempotency** | **yes** — `IIdempotencyStore` + in-memory store + atomic-claim middleware | **yes** — `idempotency/` (same shape; fails open on store outage) | **yes** — `Benzene.Idempotency` (same shape) | **yes** — `benzene-resilience` `idempotency.py` (same shape) |
| **Resilience** | **yes** — `UseRetry` (jitter, max-delay cap; `OutboundRetry` predicates retry a failed outbound send on its result) + `UseTimeout` + the sequential-attempt Polly subset via `Benzene.Resilience.Polly` (hedging/fallback not supported, stated) | **yes** — `resilience/` (retry, cooperative timeout, bulkhead, fallback) + `circuitbreaker` module; hedging not implemented (pipeline-contract question, stated) | **yes** — `RetryMiddleware` (no jitter/max-delay cap — stated as not implemented) + full Cockatiel via `Benzene.Cockatiel` | **yes** — `with_retry` decorators + `benzene-resilience` (circuit breaker, bulkhead) |
| **Rate limiting** | **yes** — `Benzene.RateLimiting` (`RateLimitingMiddleware` on `System.Threading.RateLimiting`: fixed/sliding window, token bucket, concurrency, payload-size, per-caller partitioning); authoritative fleet-wide limiting deliberately at the gateway | **yes** — `ratelimiting/` (per-instance; fleet-wide deliberately at the gateway) | **yes** — `Benzene.RateLimiting` (fixed-window, token-bucket, concurrency, payload-size; distributed deliberately out) | **yes** — in `benzene-resilience` |
| **Caching** | **yes** — `Benzene.Cache.Core` (cache-aside/write-through abstractions + cache health check) + `Benzene.Cache.Redis`; cache *middleware* deliberately not shipped (what/when to cache is a handler-level decision) | **yes** — `cache/` (pluggable `Store`, read-through, safe degradation) | **yes** — `Benzene.Cache.Core` + `Benzene.Cache.Redis` | **yes** — `benzene-cache` (cache-aside; memory + Redis) |
| **Validation** | **yes** — `Benzene.FluentValidation`/`.DataAnnotations`/`.JsonSchema` adapters over one shared short-circuit failure contract (`Benzene.Abstractions.Validation`); deliberately no validation DSL of its own. All three emit `errors[].field` as an RFC 6901 JSON Pointer into the request's JSON form, named by the service's serializer, and omitted for a whole-request rule. gRPC carries it as the dotted `FieldViolation.field`, and a Benzene gRPC client reads it back as a pointer | **yes** — `validation/` typed handler wrapper (deliberately not a reflection DSL or pipeline middleware); `validation.Pointer` builds the RFC 6901 pointer from the request's `json` names; runs `conformance:validate`; gRPC converts to the dotted path and back | **yes** — Zod/Joi/Yup/ajv adapter packages over a shared abstraction. Every adapter emits RFC 6901 pointers (Yup's paths converted, ajv's native), omitting `field` for a whole-request rule. Runs `conformance:validate` through the Zod adapter; gRPC converts to the dotted path and back | **yes** — `benzene-pydantic` (optional adapter; core never imports it): `field` is an RFC 6901 pointer using the model's aliases, omitted for a `@model_validator` rule; gRPC (`benzene-grpc` `details.py`) converts to the dotted path and back |
| **Versioning (handler/payload)** | **yes** — both mechanisms. Selection per core-concepts §2: exact match; else the handler the payload can be **cast** to (the default wins a tie, else the ordinal-greatest); else `not-found` naming topic + version. No version: the unversioned handler, else the declared default (`IsDefault`), else `not-found`. The `version-selection` start-up check fails on two defaults and warns on none. One header, `benzene-version`, renamed in one place and written outbound under the same name; HTTP `/v{version}` route segment. "Highest available" only as the opt-in `HighestAvailableVersionSelector`. Payload up- *and* downcasting with auto-composed chains (`Benzene.Core.Versioning`) + request-side gRPC casting | **partial** — selection per core-concepts §2: exact `(id, version)` or `not-found`. No version: the unversioned handler, else `benzene.AsDefault()`; start-up warning for a topic with no default. One `benzene-version` header (`ReservedNames.VersionKey`) or the HTTP `{version}` route segment; no highest-available selector, even opt-in. **Not implemented:** payload casting (an optional capability, so the cast step never applies), and writing `benzene-version` from outbound clients | **yes** — `VersionSelector` per core-concepts §2: exact, else cast-reachable (the default wins a tie), else `not-found`. Declared default via `@message(topic, { version, isDefault: true })`; start-up warning. One `benzene-version` header (`addMessageVersionHeaderName`); `HighestAvailableVersionSelector` opt-in only. Payload casting for requests, responses and schemas (`Benzene.Core.Versioning`). **No HTTP `/v{version}` route segment** — header only | **yes** — `(topic, version)` registration; one `benzene-version` header (renamed once via `MetadataKeys`) or an HTTP `/v{version}` segment. Exact, else `not-found`. Declared `default=True`; `NoDefaultHandlerWarning` at start-up. `highest_version` is an explicit opt-in, never the default. Casting is registration-level (`casting_handler`), so the cast step has no candidates |
| **Serialization beyond JSON** | **yes** — `Benzene.Xml`, `Benzene.MessagePack`, `Benzene.Avro` (+ `Benzene.NewtonsoftJson`) via content negotiation; binary formats Base64-armored (string-body wire contract — a stated boundary, not an accident) | **no — deliberate** — `encoding/json` is idiomatic; Avro/MessagePack would be new dependency decisions, not ports | **yes** — `Benzene.Xml`, `Benzene.MessagePack`, `Benzene.Avro` | **unknown** — matrix has no serialization row (JSON envelope implied) |
| **Schema registry / Confluent codec** | **yes** — Confluent wire-format codec + `ISchemaRegistryClient` seam (in-box compat checker textual-only, deliberate) | **unknown** — matrix does not address the registry codec (nearest statement: the deliberate no on alternate serializers) | **yes** — `Benzene.SchemaRegistry.Core` (same textual-only checker boundary) | **no — unbuilt** (stated plainly; Kafka binding carries plain envelopes) |
| **Sagas / workflows** | **yes** — `Benzene.Saga` (in-process, LIFO compensation; durable crash-resume deliberately out — use a real orchestrator) | **yes** — `saga/` (same shape and same stated boundary) | **yes** — `Benzene.Saga` (same shape and boundary) | **yes** — `benzene-resilience` `saga.py` (same shape and boundary) |
| **AuthN / AuthZ** | **yes** — `Benzene.Auth.OAuth2` (OAuth2 bearer JWT, strict algorithm allowlist, scope authorization); keys via **full OIDC discovery (`Authority`) or bare JWKS (`JwksUri`)**, cached with refresh-on-unrecognized-`kid`; policy-engine adapter deliberately not shipped | **yes** — `auth/` (Basic + Bearer JWT: RFC 8725 allowlist, `StaticKeys` **or caching JWKS with OIDC discovery**; roles/scopes) | **yes** — `Benzene.Auth.OAuth2` (`jose`, required no-default allowlist, scopes) + `Benzene.Auth.Basic`; matrix does not say whether JWKS/OIDC discovery is included | **partial** — Basic + bearer JWT against a **configured static key only**; JWKS/OIDC discovery **not implemented** (stated as unbuilt, not declined) |
| **MCP (AI tool access)** (MCP binding, Provisional: .NET) | **yes** — `Benzene.Mcp`: the Model Context Protocol as a transport binding. Curated tools (topic-bound, or an app-written body that asks the pipeline) over Streamable HTTP (default `/benzene/mcp`) on any Benzene HTTP host, or stdio on the worker. A topic-bound call carries `benzene-mcp-tool` and the session `benzene-mcp-session`, and goes through the same middleware/validation/mesh feeds/start-up checks as every other transport. **no — deliberate:** one tool per handler, server-side session state, the server-to-client stream, resources, prompts | **no — unbuilt** — "this port serves no … MCP surface" (no reason stated) | **no — unbuilt** — "this port has no … MCP binding" (no reason stated) | **unknown** — the matrix does not mention MCP |
| **Distributed tracing / observability** | **yes** — W3C `traceparent`/`tracestate` inbound on HTTP + async transports; OTel, exporter-agnostic. Outbound, `UseW3CTraceContext()` forwards OpenTelemetry's `Activity.Current`. **Mesh M2:** a meshed service forwards the mesh span instead with `routing.UseMeshTraceContext()` (`Benzene.Mesh.Wire`): the `traceparent` carries the span the TraceEvent records, and it wins over `UseW3CTraceContext()` on the same route (mesh §3) | **yes** — `diagnostics` module (OTel API only; one server span per invocation, W3C join, outbound `WithTraceContext`) + `logging/` (`slog`); SDK/exporter deliberately app-owned. **Mesh M2:** `mesh.WithTraceContext` forwards `traceparent` with `tracestate` unchanged on sends, one-way sends and published events — app-wired | **yes** — `W3CTraceContextMiddleware` (`traceparent`/`tracestate`, the OTel span) + `Benzene.Diagnostics`; OTel, exporter-agnostic. **Mesh M2:** `useMeshTraceContext` forwards `traceparent` + `tracestate` from `MeshSpan.current`, opt-in per outbound route (sends and published events) | **yes** — `trace_middleware` (mesh; keeps the inbound `tracestate`) + `benzene-otel` exporter (SDK optional). **Mesh M2:** `with_trace_propagation` forwards `traceparent` + `tracestate` on the wrapped `MessageSender`, events included once the publisher is wrapped (the matrix does not name which span it forwards) |
| **Secrets & configuration** | **yes** — `ISecretStore` (env/files/composed/cached) + fail-fast validation; cloud adapters deliberately cookbook-only | **unknown** — matrix does not cover it | **yes** — `Benzene.Configuration.Core` (same shape and same cloud-adapter stance) | **unknown** — matrix does not cover it |
| **Database / state access** | **no — deliberate** — a database is not a transport; wrapping one hides its capabilities | **no — deliberate** — same reason, verbatim stance | **no — deliberate** — same | **no — deliberate** — same |
| **Conformance fixtures (vendored)** | **yes** — `test/Benzene.Conformance.Test`, **pinned to `spec-1.2.0`** (stated; `SPEC_VERSION`, `conformance-drift-check.yml`). Every contract-document/hash/collector/service-version/transport-metadata case passing, `missing-schema-dialect-is-rejected` included. Runs envelope (incl. version selection via `conformance:versioned`), HTTP/gRPC status maps, status vocabulary, and problem details (incl. `httpRules.envelopeEndpoint`/`healthEndpoint` over a real host, and the `requires: validation` case through FluentValidation). Also contract document and hashes (every case passing), and mesh trace/issue/collector/**service-version/version-order**. **It is the only port that runs the two Service Versions fixtures.** `transport-metadata-cases.json` runs in full: every binding's topic key, the version key, the `benzene-headers` key, and every `metadataCases`/`overrideCases` entry (`headersExclude` and the case-insensitive key included) against every metadata binding, plus the `topicSources` fallbacks. `settlementDefaults` is in the settlement row | **yes** — `conformance/` runs the vendored fixtures. The contract-document producer groups and every non-projection hash case run through the runtime `contract` package. `mesh-trace-cases.json` runs; `mesh-collector-cases.json` and `mesh-issue-cases.json` run against `meshd`; `codegen/conformance` runs the client-generator groups and every hash case. `transport-metadata-cases.json` runs in full through every native-metadata binding (transport-metadata row). `settlementDefaults` is asserted, and `problem-details-cases.json`'s `envelopeCases` run through the outbound clients. **Not run:** the two Service Versions fixtures (listed opt-outs). **Pinned to `spec-1.2.0`** (stated; `conformance/SPEC_VERSION`, `conformance-drift-check`) | **yes** — **pinned to `spec-1.2.0`** (stated; `ConformanceDriftTest`; `missing-schema-dialect-is-rejected` passes). All 13 canonical files are vendored, and **11 are opened by a runner**: envelope (incl. version selection), HTTP/gRPC status maps, status vocabulary, problem details (`conformance:validate` via Zod, `httpRules`), and transport metadata. Transport metadata covers every `metadataCases`/`overrideCases` case through each native-metadata binding, the `topicSources` fallbacks, and `settlementDefaults` for every shipped one-way binding. Also **every contract-document group**, **every hash case**, and `mesh-trace`/`mesh-collector`/`mesh-issue`, the 1.2 flow-status and error-detail cases included. **Not run:** the two Service Versions fixtures (no service-version identity). `ConformanceCoverageTest` fails on a fixture with no runner | **yes** — the dependency-free runner (`python -m tests.conformance_runner`, also under pytest), **pinned to `spec-1.2.0`** (stated; `conformance-drift-check.yml`). It opens status vocabulary, HTTP/gRPC status maps, envelope, and problem details (all three groups). Transport metadata covers every `topicSources` entry from its native shape and every `metadataCases` case (`headersExclude` included) through every host of each metadata binding. It also covers every `overrideCases` case, every outbound client writing the metadata back, and `settlementDefaults`. Then the contract document (producer groups, `derivationCases` run from dataclasses and again from pydantic models; client-generator groups), every hash case, `mesh-trace`, `mesh-collector` and `mesh-issue`. `run_fixture_coverage` fails on an unopened fixture; the `requires: validation` case and the pydantic `derivationCases` run are skipped without pydantic, and the gRPC client's error-fidelity run without the `[transport]` extra. **Not run:** the two Service Versions fixtures, in `UNRUN_FIXTURES` with reasons |

## Divergence notes

Grades: *deliberate* (stated per-port design decision), *staged* (a later port hasn't got there —
normal), **drift** (contradiction about a shared contract — the finding that matters). Descriptive
only; decisions stay with the product owner.

1. **Outbox — mixed: deliberate (TS) + staged (Go, Python).** .NET ships `Benzene.Outbox` with
   store packages. TypeScript states a design position (the outbox row belongs in the application's
   own transaction), with supported seams and a cookbook. Its own matrix flags this as "an honest
   divergence from the .NET port", not parity. Go and Python say "not implemented" plainly. No port
   papers over it, so this is not drift — but it is the largest capability fork in the estate.
2. **Claim check — staged; the module is Provisional: .NET.** .NET ships `Benzene.ClaimCheck`
   (+ S3/Blob stores). Go, TypeScript and Python all say unbuilt, plainly, with no design reason.
   [claim-check.md](specification/claim-check.md) stays Provisional until a second port implements
   it, and it has no fixtures yet.
3. **Settlement of returned failure results — the shared contract holds; what differs is
   deliberate.** Obligation 8 of transport-bindings §1 states the rule, and `settlementDefaults`
   pins each binding's shape. All four matrices claim it. On every queue-shaped and one-shot
   binding, a failure result, a missing result and an unmatched topic are redelivered by default.
   Every one-way binding offers an at-most-once override and unmatched-topic-as-`ignored`. What
   still differs sits inside the spec's stream allowance or a binding's stated unit of settlement,
   each documented by its port:
   - **Stream defaults.** The self-hosted Kafka loop skip-and-continues in all four. The self-hosted
     Event Hub worker skip-and-continues in .NET and TypeScript, redelivers in place in Go, and does
     not exist in Python. On the Lambda MSK and Functions Kafka/Event Hub triggers, .NET and
     TypeScript acknowledge a missing result. TypeScript also defaults `ignoreUnmatchedTopics: true`
     on every stream binding, acknowledging an unmatched topic. Go and Python redeliver both; Go
     names `IgnoreUnmatchedTopics` as the opt-in to .NET's behaviour.
   - **Units of settlement.**
     - Kinesis: .NET runs a fan-in with handler checkpoints; Go stops at the first failed record;
       TypeScript settles the whole invocation and runs records concurrently (shard order not
       kept); Python reports every failed record.
     - DynamoDB Streams: Python runs every record; the others stop at the first failure.
     - Lambda MSK: Python fails the whole invocation; the others report per partition.
     - Cosmos DB change feed: .NET and TypeScript run a fan-in with no result; Go settles one
       result-bearing batch; Python routes per document.
   - **Asynchronous envelope** (`envelope-async`) — *staged (.NET)*. Go, TypeScript and Python
     ship a separately selected entry point for `Event` invocations. .NET's Lambda direct-invoke
     envelope handler is request/response only (stated).
   - **A middleware panic on Go's self-hosted workers** escapes `Run` and is redelivered from the
     broker or the uncommitted position. .NET's workers nack and carry on. Stated by Go.
4. **Auth key resolution — staged (Python) + one unknown (TS).** .NET states cached JWKS with full
   OIDC discovery (`Authority` or bare `JwksUri`, refresh-on-unrecognized-`kid`). Go states
   `StaticKeys` or caching JWKS with OIDC discovery. Python is explicitly partial: static-key JWT
   only, with JWKS/OIDC stated as unbuilt. The TypeScript matrix does not say whether its OAuth2
   package does JWKS/OIDC discovery — an unknown detail, not evidence either way.
5. **Mesh UI — deliberate (Go) + one unknown (TS).** .NET (`/benzene/mesh-ui`) and Python
   (`/benzene/mesh-ui/`) serve the **canonical** `mesh-ui.html` built by benzene-ui. .NET embeds a
   byte-identical copy with a CI drift-check; Python vendors it from this repo. benzene-ui itself is
   pinned to `spec-1.2.0` (`c42694d`). Its README states one reader for both the Contract Document and the legacy
   (≤ 0.2) shape, refusing an unknown `schemaDialect`. Its refusal of a dialect-less Contract Document
   (spec 1.1) shows only in a commit title (aligner check: `6420890`). Spec 1.2's reader side also
   shows only in a commit (aligner check: `b5692e7`). The issue page lists an issue's `errors`
   (field, code, count) in the collector's order, and a flow row names the flow's `status`, showing
   nothing when the collector carried none, as mesh §4 requires of a reader. `b5692e7` is the build
   that .NET, TypeScript, Python and this repo re-vendored; `c42694d` changes only its contracts.
   benzene-ui has no capability matrix. Go deliberately serves its own
   zero-dependency `view.html` at `/benzene/fleet-ui`. It documents that the shared
   `benzene:mesh:query:*` wire contract, not the page, is the interop point, a stance .NET's
   matrix states in the same terms. TypeScript ships `Benzene.Mesh.Ui` at `/benzene/mesh-ui`, but
   its matrix still does not say which asset it serves. Its commits have been titled as vendoring
   the benzene-ui build; that is a pointer for the scribe, not a cell.
6. **Schema registry — staged (Python) + unknown (Go).** .NET and TypeScript ship the Confluent
   codec + registry seam with the identical textual-only-checker boundary. Python says unbuilt
   plainly. Go's matrix doesn't address it directly.
7. **Version selection — no spec-level drift; what remains is staged or optional.** All four
   matrices state core-concepts §2:
   - an exact match;
   - else, where the port casts payloads, a handler the version can be cast to;
   - else `not-found` naming the version;
   - a message with no version goes to the unversioned or declared-default handler;
   - a start-up warning for a topic with only versioned handlers and no default;
   - one `benzene-version` header with no fallback list;
   - "highest available" only as an opt-in (.NET, TypeScript, Python) or not at all (Go).

   What still differs:
   - **The cast step.** .NET and TypeScript implement it over their payload casting. Go has no
     payload casting, which core-concepts §10 makes optional, so the step never applies. Python's
     casting is registration-level, so an exact match already finds a casting handler.
   - **The HTTP `/v{version}` route segment.** .NET, Go and Python supply the version from it.
     TypeScript reads the header only (stated).
   - **Outbound.** Go's clients do not write `benzene-version` from the topic's version (stated "not
     implemented"); the application sets the header itself.
8. **Azure Blob trigger, Functions Kafka trigger, gRPC streaming — deliberate or staged (Go).** Go
   defers the Blob trigger by design: it won't fabricate an unverifiable custom-handler shape. It
   hasn't pinned the Functions Kafka trigger payload. .NET (as a deliberately non-routed adapter),
   TypeScript and Python ship Blob. Python ships no Functions Kafka trigger either (stated). gRPC
   streaming is a documented Go gap (staged); .NET (all four RPC shapes) and TypeScript ship it.
9. **Reserved topics and extension documents — deliberate.** All four serve `benzene:spec` with the
   Contract Document for an absent, empty, `null` or `{}` request. All four answer an unsupported
   `type` or `format` with `not-implemented`, and run `GET /benzene/spec` through the pipeline with
   `type`/`format` from the query. The extra document types are unpinned port extensions
   (contract-document §7):
   - .NET serves OpenAPI 3.0 and AsyncAPI 3.0 as JSON or YAML.
   - TypeScript serves `openapi` and `asyncapi` as JSON only.
   - Go serves only `benzene`/`json` unless the application adds a type with `WithDocumentType`.
   - Python serves only `benzene`/`json`; other documents come from `benzene-openapi`.

   The `liveness`/`readiness` topics are optional: .NET and TypeScript serve them, and Go and
   Python state they don't. All four answer `benzene:ping` `ok` with no payload from the router.
   TypeScript's router answers any unclaimed reserved id, the retired `benzene:mesh` included, the
   same way. A port MAY serve unlisted reserved topics (wire-contracts §7), so this is not a
   divergence.
10. **MCP — Provisional: .NET; staged elsewhere, one unknown.** .NET ships `Benzene.Mcp` with the
    `benzene-mcp-tool`/`benzene-mcp-session` headers at `/benzene/mcp`. Go and TypeScript state
    they have no MCP surface, with no design reason (unbuilt). Python's matrix is silent.
11. **Service Versions — Provisional: .NET; staged elsewhere.** Only .NET keys its catalog by
    `(service, serviceVersion)` and runs both module fixtures. It scopes the heartbeat hash
    comparison to one `(service, serviceVersion)`, as mesh §5 requires of the module. Go,
    TypeScript and Python state that their collectors key by `service` alone and compare a heartbeat
    with the service's one registration (mesh §5's non-module rule). All three opt out of both
    fixtures with the reason "the module is not implemented". A collector without the module stays
    collector-conformant.
12. **The Contract Document (Stable module) — the shared shape holds; the residual differences are
    deliberate, staged or unstated, with one flag; the dialect drift is resolved by spec 1.1.** All
    four matrices state the same document:
    - `schemaDialect: "openapi-3.0"`;
    - `service`/`serviceVersion`/`versionScheme`;
    - `requests`/`events`/`sends` from explicit declarations, with no `benzene:` entries;
    - no `response` for a handler with no payload;
    - author examples only;
    - `contractHash` + `registrationHash` computed by the runtime, with no private hash anywhere;
    - the deployment-side `runtime`/`placement`/`profile`/`degraded` outside both hashes.

    Every port passes the producer groups and the hash cases. Where they differ:
    - **When a component-name collision fails** — **flagged (.NET, manual composition only)**. Go
      fails before the first message, TypeScript in its start-up checks, and Python when building
      the app. .NET fails at start-up under `UseBenzeneCloudService` by default. Under manual
      composition it fails on the first spec request unless the application opts in to
      `AddContractDocumentStartUpCheck()` or emits the spec at build time. That is a ruled,
      deliberate trade (`work/archive/self-description-design.md` §10.2 item 7), off the profile path.
    - **Hoisting** — *deliberate within a SHOULD* (§4.1 rule 1). TypeScript inlines Joi/Yup nested
      objects (Zod hoists), and Go inlines undeclared generic instances. Component names are outside
      the hashes, so the ports still agree on both hashes.
    - **`required` for a defaulted property** (§4.2, ruled "always emitted" for 1.0). .NET states
      "`required` is every always-emitted property". Python states that a defaulted dataclass field
      is `required`. TypeScript now states it per library: Zod, Joi and Yup defaults all make a
      property required. The Go matrix still does not state the rule. No fixture has a defaulted
      property, so this rests on the ports' own tests.
    - **Optional top-level fields.** TypeScript states it emits no `messageEndpoint`; the other
      matrices do not say. *Staged.*
    - **Where events come from.** .NET: event declarations. TypeScript: response events and
      `{ kind: 'event' }` declarations. Python: the `OutboundRegistry`. Go: only
      `contract.RegisterEvent`, because its `responseevents` mappings do not become declarations
      (stated "not implemented"). *Staged (Go).* .NET's generated clients also declare the topics
      they call as sends of their consumer.
    - **Derivation coverage.** Python now derives pydantic v2 models (Q11 closed) and writes and
      derives `UUID`, `date`, `Decimal`, `timedelta`, `time`, `Path` and `Literal` in standard
      forms. Go's typed enums derive as their base type, which §4.2 permits.
    - **Python's naive `datetime`** — **fixed** (benzene-python `78de3f6`). Python derives a
      `datetime` as `format: date-time`, and now takes a naive one as UTC and writes it `+00:00`, so
      every `datetime` it writes is RFC 3339 (§4.2's timestamp row). A naive `time` is still written
      without an offset; its schema is a plain string. No other matrix addresses offsets.
    - **A document without `schemaDialect`** — **resolved by spec 1.1.** contract-document §1 now
      says a document without `schemaDialect` has an unknown dialect, and the parse case
      `missing-schema-dialect-is-rejected` pins it. All four client generators state that they
      reject one:
      - .NET's conformant `Deserialize` rejects it. A legacy (spec-0.2.0) document is read only
        through the explicit opt-in `DeserializeAllowingLegacyShape`, with a warning, and
        `benzene diff` refuses one as a baseline. *Deliberate, opt-in.*
      - Go, TypeScript and Python reject a missing dialect like an unknown one.
      - benzene-ui refuses a dialect-less Contract Document. By design it still reads the legacy
        `openapi`/`info` shape, which is a different document, so an estate mid-upgrade stays
        visible. That comes from its README plus a commit title (aligner check: `6420890`).

      A collector is not a consumer in this sense: mesh §2 exempts it, and all four collectors state
      they do not check the dialect.
13. **The register body and the service side (Mesh, Stable) — the contract holds in all four; what
    differs is idiom or permitted.** All four matrices state the same service side:
    - the register body is the document `benzene:spec` serves, with `runtime`, `placement`,
      `profile` and `degraded`, and no `instanceId`;
    - heartbeats carry `registrationHash` and `serviceVersion`.

    And the same collector side:
    - only `service` is required, and the carried `registrationHash` is trusted;
    - providers are `events` ∪ `sends`, and consumers are `requests`;
    - `benzene:` entries are ignored, and an absent or `degraded` member is unknown;
    - a traced reserved topic is never drift, and no legacy body is read.

    What differs:
    - **Who registers** — *idiom* (mesh §8). .NET: `UseBenzeneCloudService`. Go: the library's
      `mesh.Announcer`. TypeScript: `MeshAnnouncer` at host boot. Python: the host loop in example
      and deploy code (no library announcer, stated), plus the collector-side `MeshPoller`.
    - **`placement`** — *permitted* (contract-document §1: detected or configured).
      - Go detects `cloud`, and `region` only on AWS Lambda.
      - TypeScript and Python are configured only; TypeScript defaults to `self-hosted`.
      - .NET takes it from a registered `ContractDocumentDeployment` or `benzene-descriptor`. Its
        matrix does not say whether anything is detected.
    - **`degraded`** — *permitted*.
      - Go names `events` and `sends` when no `OutboundRegistry` is supplied.
      - TypeScript does the same when no outbound registry and no response-event/sender feed is
        registered.
      - .NET names `requests` when no handler registry is wired, and `events`/`sends` on the
        `WithHandlers` wire-up registration.
      - Python names only what its composition root declares.
    - **Heartbeat hash scope** — *staged*, following each catalog key (note 11).
    - No `mesh-collector-cases.json` or `mesh-issue-cases.json` case traces a reserved topic, so
      the reserved-topic drift exemption rests on the four ports' own tests. That is a gap in the
      fixtures, not a divergence.
14. **Collector read-model field names — divergent, inside Provisional shapes.** The read models are
    Provisional and "asserted only as far as the fixtures need" (mesh §4). Each collector renamed
    different fields as the descriptor left:
    - .NET: `ServiceView.registration` and `InstanceView.registrationHash` (+ `serviceVersion`). It
      does not name the fleet row's transports field.
    - TypeScript: `transports` on the fleet/service rows and `registrationHash` on the instance
      view, but it keeps the service view's `descriptor`.
    - Go: `transports` (+ `serviceVersion`, `runtime`, `placement`) on the fleet row, and
      `descriptor` (the registered document) on the service view.
    - Python: names its topic query's activity maps and its fleet query's `issues` and (new at
      spec 1.2) `traces`, but no fleet-row, service-view or instance-view fields.

    Spec 1.2 pins one more fleet-view field, the trace summary's `status` (note 20).

    All four keep `descriptor` for the missing-feed marker, which mesh §6 itself uses. A view
    written against one collector's service or instance view will not read another's. The fixtures
    still pass because they do not assert these fields. Pinning them is the post-1.0 work mesh §4
    already defers.
15. **The meshed-service claim (mesh §7.1) — all four can meet M1 and M2; .NET's M2 drift is
    closed, and its matrix now states it consistently.**
    - **What wires it** — *idiom*.
      - .NET's `UseBenzeneCloudService` wires M1 and M2's inbound join, given a collector. Outbound,
        the app adds `routing.UseMeshTraceContext()`.
      - Go's `cloudservice.New` wires neither; the app runs `mesh.Announcer`, `TraceMiddleware`
        with a push exporter, and `WithTraceContext`. Its report says so.
      - TypeScript uses `MeshAnnouncer` and its trace feed, plus `useMeshTraceContext` added per
        outbound route.
      - Python uses `MeshFeedSender` (registration pushed by the host loop) and
        `with_trace_propagation`.
    - **How the self-check grades M2** — *divergent, stated by each port*.
      - TypeScript grades M2 on the inbound join only. The self-check cannot see outbound routes,
        which the cloud service does not own (stated).
      - .NET's mesh row and spec-endpoint row both say M2 needs the inbound join and every
        outbound route forwarding the mesh span, or no outbound routes. The check cannot see a
        per-route `UseMeshTraceContext()` or a route registered after the cloud service is wired
        (stated).
      - Go reports M1/M2 unwired for a `New`-only build.
      - Python grades the propagation as declared on the composition root.

      So `isMeshed` can read true in TypeScript for a service that forwards nothing outbound.
      .NET's `IsMeshed` cannot for a route it sees, but routes registered after wiring escape it.
    - **M2's outbound span** — *closed (.NET), was drift against mesh §3.* Mesh §3 makes the
      forwarded `traceparent`'s parent-id the `spanId` that the invocation's TraceEvent records,
      "whatever other tracing the port also runs".
      - .NET: `UseMeshTraceContext()`, per route or on every route, stamps `MeshSpan.Current`'s
        `traceparent`/`tracestate` on sends and published events. It wins over
        `UseW3CTraceContext()`'s `Activity.Current`. Stated in the mesh row and the distributed-tracing
        row.
      - Go: `mesh.WithTraceContext`, "this span's `traceparent`".
      - TypeScript: `useMeshTraceContext`, from `MeshSpan.current`.
      - Python: `with_trace_propagation`. Its matrix still does not name the span.
    - **.NET matrix remnants** — *fixed* (benzene-dotnet `fa138e10`). The mesh row's "deliberately
      not" column and the spec-endpoint row's M2 note now state `UseMeshTraceContext()`, not the
      retired gap. They add that the AwsMesh example forwards only `UseW3CTraceContext()` to keep
      its X-Ray traces whole, so it reports M2 missing.
16. **Error fidelity across a hop — Core §12 met by all four; both 1.1 SHOULDs met by all four.**
    All four state that their reply-receiving clients rebuild `errors` from a received problem
    document, with `field` and `code` intact, and all four run the problem-details `envelopeCases`
    through those clients.
    - **`benzeneStatus` over HTTP** (wire-contracts §4.1, SHOULD, spec 1.1) — *met by all four.*
      Outside an envelope, every HTTP client now takes the status from a problem document's
      `benzeneStatus`; the reverse table applies only to a failure without one. Go and Python ignore
      a success-class `benzeneStatus` on a failure. Inside an envelope the readers differ in
      precedence, but only when the two disagree:
      - .NET is envelope-first;
      - Go and Python take the envelope's `statusCode` (Python uses `benzeneStatus` only for an
        envelope with none);
      - TypeScript's envelope reader takes the document's `benzeneStatus`, and falls back to the
        envelope's `statusCode`.

      A conformant envelope carries the same status in both, so this is *permitted*.
    - **A problem error's `code` over gRPC** (wire-contracts §4.2, SHOULD, spec 1.1) — *met by all
      four.* Each server writes `code` to `FieldViolation.reason` and each client reads it back.
      TypeScript and .NET do so for every errored status, not only `validation-error`.
    - **Attaching the document** (the §12 MAY): .NET (`GetProblem()`) and TypeScript
      (`ProblemTypes.from`) do; Go and Python deliberately do not.
    - **Reading an `application/json` failure body as a problem document** (the §4.1 MAY): Go
      ("(or JSON)") and Python (`application/json`) do. .NET and TypeScript state they do not.
    - **Python has no envelope-over-HTTP client** (stated "not built"): `HttpMessageSender` posts to
      routes. That answers the R4 client-read-rule unknown of the last refresh.
17. **EventBridge `benzene-headers` — attested by three ports.** .NET states the key (no
    dual-accept). TypeScript asserts it through its conformance runner. Python names it among its
    reserved `MetadataKeys`. The Go matrix still ships EventBridge without naming the key. No
    evidence of divergence — a gap in the record.
18. **Matrices behind their own repos.**
    - The conformance pin is current in all four. Each matrix states `spec-1.2.0`, and none still
      calls the 1.2 changes a candidate or says the fixtures are "ahead of the pin".
    - .NET's M2 self-contradiction is fixed (note 15).
    - .NET's matrix does not say whether `placement` is ever detected (note 13), or what its fleet
      row calls the transports field (note 14). It does not say that its X-Ray rows omit a flow's
      `status` when X-Ray's error flag and the spans disagree, though its source does (note 20).
19. **Outbound transport failure (transport-bindings §1, SHOULD, spec 1.1) — met for reply-receiving
    clients and one-way clients in all four.** A send that fails before a
    reply, or before the broker accepts it, is a `timeout` result when the deadline elapsed and
    `service-unavailable` otherwise.
    - **Reply-receiving clients:** .NET (every route transport via `ITransportFailureMapper`, the
      message clients, the mesh dispatchers), Go (`client.CallFailure`), TypeScript
      (`transportFailureResult`, the mesh dispatchers, the gRPC client) and Python
      (`failed_call_result`) all state it.
    - **One-way clients:** all four state it. Go's one-way clients went through the shared
      `client.CallFailure` at this refresh (benzene-go `9a13703`); before, every transport failure
      was `service-unavailable`, never `timeout`.
    - **A rejection the broker itself answered** — *divergent within a SHOULD that does not reach
      it.* TypeScript maps the broker's status through the reverse HTTP table (`403` → `forbidden`,
      `429` → `too-many-requests`). Python maps it to `service-unavailable` (stated, unchanged). The
      .NET and Go matrices do not say.
    - **Batch clients:** .NET reports a Service Bus / Event Hubs batch that never reached the
      broker as every entry failed. TypeScript reports per entry: a failed chunk, or a
      `createBatch`/`createMessageBatch` that rejects, fails every entry that batch would have
      carried (per partition-key group for Event Hubs). The Go and Python matrices name no batch client.
    - **What still throws, by statement:**
      - .NET: request serialization errors and the caller's own (ambient) cancellation.
      - TypeScript: an Event Hubs event too large for a batch, which is not a transport failure.
        Also its low-level `AwsLambdaClient`, because the result-returning Lambda message client is
        not ported (*staged*).
      - Python: a transport error that is not an `OSError`, deliberately, since that is a defect.
      - Go: nothing stated for the reply-receiving clients.
    - **Knock-on (.NET):** the outbox relay reschedules a send that *returns* a failed result.
20. **Issue error detail and a flow's status (mesh §4.1, §4; spec 1.2) — the collector side holds
    in all four; the emitter side differs, within a feed that is optional on both sides.**
    - **Collector `errors`** — *met by all four*, and all four pass the three error-detail cases.
      Each merges an issue's `errors` by (`field`, `code`) with `count += delta`, keeps the ten
      highest in the spec's order, skips a malformed entry and keeps the issue, and reports `errors`
      on the fleet view's issue. TypeScript and Python state that an empty member is an absent one.
      .NET states only that its emitter does not count an error with empty members, and the Go
      matrix does not say. The fixture
      `invalid-error-detail-entries-are-skipped-and-the-issue-kept` pins it, so the silence is a gap
      in the record, not evidence of divergence.
    - **The emitter** — *divergent: idiom (Python), staged (TypeScript)*. No port sends `message`.
      - .NET: automatic. `UseMeshIssues` takes each failing result's pairs, and
        `HttpMeshIssueExporter` holds at most 100 distinct pairs a window and flushes ten.
      - Go: automatic. `IssueMiddleware` takes them, and the exporter bounds them to the ten most
        frequent. The matrix states no per-window cap.
      - Python: **hand-fed.** `IssueAggregator.record(..., errors=result.errors)` counts them (100
        pairs a window, ten at `flush()`), but there is no issue middleware (stated). So a failure's
        detail, and the issue itself, reach the feed only where the application records them.
      - TypeScript: **no emitter** (stated). The issue wire shapes ship, but a TypeScript service
        sends no issues, so no `errors`. Mesh §4.1 makes the feed optional on both sides, and §6
        calls such a service reduced, not broken. The matrix files this under "deliberately not"
        but gives no design reason beyond that optionality.
      - Mesh §4.1's SHOULD to drop `field` for a request type whose member names are data (a map
        keyed by id) is addressed by no matrix.
    - **A flow's `status`** — *met by all four from their own trace store; external sources omit it
      as the spec permits.* Each fleet view sets it from the earliest-started failing event when the
      flow failed, else from the earliest-started event, and passes the two flow-status cases.
      - Go and Python build flows only from their in-memory store. **Python's fleet view gained
        flows (`traces`, the newest 20) with this change**; before it had none. Go's matrix words the
        rule as "its first failure's, else its first event's". It passes the fixture, and
        `meshd/store.go` sorts by start time (aligner check: `b57033b`).
      - .NET and TypeScript also read external trace backends. The push store, the Jaeger source
        and enriched X-Ray rows (spans fetched) carry `status`. Tempo and X-Ray summary-plane rows
        hold no Benzene status and omit it, which mesh §4 permits. TypeScript states that an X-Ray row
        also omits it when X-Ray's error flag and the mapped spans disagree. .NET's matrix does not
        say so, but its `XRayTraceSource` does the same (aligner check: `ffe5c471`). This is a gap in
        .NET's record, not a divergence.
    - **Readers.** benzene-ui shows nothing for an absent `status` (note 5). Go's matrix does not
      say whether its own `view.html` shows `status` or `errors` (a detail, not a flag).

### Closed at the spec-1.2.0 row refresh (2026-10-14)

- **.NET's M2 self-contradiction (notes 15, 18) — closed.** benzene-dotnet `fa138e10` rewrote the
  mesh row's "deliberately not" column and the spec-endpoint row's M2 note to state
  `UseMeshTraceContext()`.
- **Python's fleet view now has flows.** Before this change it had none (not recorded here
  before). Its fleet query now carries `traces` with each flow's `status` (notes 14, 20).
- **Spec 1.2's two mesh changes** are met on the collector side by all four, and pass in all four
  snapshots (note 20).
- **The `spec-1.2.0` pin in the matrices — closed.** The pin commits had changed only
  `SPEC_VERSION`. Each matrix now states the pin (benzene-dotnet `4bf13aaa`, benzene-go `3b48fb8`,
  benzene-typescript `13f30ee`, benzene-python `563be93`).

### Closed at the spec-1.1.0 row refresh (2026-10-12)

- **.NET M2 outbound parent-id (mesh §3) — closed.** `UseMeshTraceContext()` forwards the mesh
  span on sends and published events (note 15). Stale passages remain in the .NET matrix (note 18).
- **Python Q11, pydantic derivation (contract-document §4.2) — closed.** Pydantic v2 models derive
  to object schemas, and the producer groups run from pydantic models as well as dataclasses.
- **A document without `schemaDialect` (contract-document §1) — resolved by the spec.** Spec 1.1
  says an absent dialect is unknown, and all four client generators reject it. .NET keeps an
  explicit legacy opt-in, and benzene-ui reads the legacy `openapi`/`info` shape by design (note 12).
- **`benzeneStatus` over HTTP and `code` over gRPC — the two 1.x spec gaps are closed.** Both are
  1.1 SHOULDs, and all four ports state both (note 16).
- **Unknown cells resolved by the scribes:**
  - Go and TypeScript: a missing `schemaDialect`.
  - TypeScript: the `required` rule, and the HTTP client's status source.
  - Python: the envelope client read rule (no such client) and the `application/json` problem body
    (read).
  - Go: the conformance pin.

### Closed at this refresh (spec-1.0.0, Phase 5)

Numbers in brackets are the note numbers of the previous version of this page (in git history).

- **Client error fidelity (core-concepts §12) — closed for .NET and TypeScript.** The P1 port
  work in `work/remaining-issues-plan.md` was .NET's `HttpStatusCode.Convert` and TypeScript's
  `HttpContextConverter`. Both clients now rebuild `errors` from a received problem document, and
  Go states the same. Python's matrix is silent (note 16).
- **Topic metadata reading [11] — closed in all four.** Every port now reads the topic key
  case-insensitively, keeps it out of the headers, and runs every `metadataCases`/`overrideCases`
  case through each metadata binding. That includes .NET's SQS bindings, whose case-sensitive read
  was recorded here. .NET's skipped `headersExclude` assertion and TypeScript's two-case subset
  are gone.
- **Python topic sources [12] — closed.** DynamoDB Streams routes on `{tableName}:{eventName}`, and
  RabbitMQ falls back to the AMQP routing key; both are stated and run from `topicSources`.
- **Kafka topic fallback — new at 1.0, met by all four.** The record's `topic` header, falling back
  to the Kafka topic it was consumed from (transport-bindings, `topicSources`).
- **Rate limiting [7] — closed; the row was stale.** The .NET matrix ships `Benzene.RateLimiting`,
  so all four ports ship per-instance rate limiting and scope out fleet-wide limiting.
- **`required` means always emitted [18, ruled for Phase 5].** .NET and Python state it. Go and
  TypeScript are unstated (note 12).
- **Python's R3/R7 self-check grading [22] — closed.** R3 is the topic intercepted, plus
  `/benzene/health` only where there is an HTTP surface. R7 is met trivially without one. The
  probe audits R3 on both halves. The capability and its self-assessment now agree.
- **Go's topic-summary `lastSeen` [22] — closed.** It is now absent, not the zero time, for a topic
  with no traffic (stated).
- **EventBridge `benzene-headers` [15] — Python now attests it.** Go remains unknown (note 17).
- **Retired as history:**
  - [10] .NET matrix coverage, paid 2026-08-20.
  - [13] Go's reserved-topic and spec-route gaps, closed at spec-0.3.0/0.4.0.
  - [17] and [19] matrix pins, closed but for Go (note 18).
  - [21] Go's `lastObservedAt` and the TypeScript k8s-example region flag, closed at spec-0.5.0.
  - TypeScript's UI-middleware scoping, an internal fix with no capability change.

### Where the ports agree (worth recording)

- **Database/state access**: all four state the identical deliberate no ("a database is not a
  transport"). The core anti-pattern stance holds estate-wide.
- **Idempotency**: all four ship the same store-seam + atomic-claim shape and the same deliberate
  boundary (no cross-instance de-duplication).
- **Sagas**: all four ship in-process LIFO compensation with the same stated boundary (no durable
  crash-resume; use a real orchestrator).
- **Rate limiting** (new at this refresh): all four ship per-instance limiting and leave
  authoritative fleet-wide limiting to the gateway.
- **Tracing**: all four join W3C trace context and export via the standard OTel API,
  exporter-agnostic. All four forward `traceparent` and `tracestate` outbound on sends and
  published events alike. .NET, Go and TypeScript state that they forward the mesh span; Python
  does not name the span (note 15).
- **The wire window**: all four state the same behaviour for:
  - settlement on queue-shaped and one-shot bindings;
  - the envelope endpoint (HTTP 200 per processed envelope, topic restriction);
  - health (`ok` + `isHealthy`, HTTP 503 when unhealthy);
  - one `benzene-version` header with §2 selection;
  - `benzene:ping`;
  - `errors[].field` as an RFC 6901 JSON Pointer (dotted over gRPC).
- **Transport metadata** (new at this refresh): all four:
  - read the topic key case-insensitively and consume it;
  - route Kafka on the `topic` header, falling back to the consumed Kafka topic;
  - fall back to RabbitMQ's routing key;
  - run every `transport-metadata-cases.json` case through each metadata binding.
- **The self-description**: all four:
  - derive one Contract Document from their registrations;
  - compute `contractHash` and `registrationHash` in the runtime, with no private hash;
  - serve it on `benzene:spec` and through the pipeline at `GET /benzene/spec`, leaving
    `benzene:` topics out of it;
  - ship a client generator that rejects an unknown or missing `schemaDialect`, ignores
    `benzene:` entries and embeds its projection's `contractHash`.
- **The register body**: all four:
  - register the document `benzene:spec` serves;
  - heartbeat its `registrationHash` and `serviceVersion`;
  - run a collector that requires only `service`, trusts the carried hash, builds providers from
    `events` ∪ `sends` and consumers from `requests`, treats an absent or `degraded` member as
    unknown and reads no legacy body.

  Live interop on that body passed between .NET, Go, Python and TypeScript at the Phase 3 exit.
- **The profile split**: all four self-checks put only R1–R5/R7 on `profile.missing` and report
  the meshed-service claim (M1/M2) apart, with "meshed" requiring the profile. All four probes
  audit R5 on both halves and report M1/M2 as inconclusive.
- **The pin**: all four fixture snapshots, and benzene-ui's contracts, are pinned to `spec-1.2.0`
  and byte-identical to the tag. All four matrices state it.
- **Spec 1.2's collector side** (new at this row refresh): all four collectors merge an issue's
  `errors` under the ten-entry bound and carry a flow's `status` on the fleet view (note 20).
- **Spec 1.1's client SHOULDs** (new at this row refresh): all four:
  - take a received problem document's status from its `benzeneStatus`;
  - carry `code` in `FieldViolation.reason` over gRPC, both directions;
  - return a `timeout`/`service-unavailable` result, not an exception, when a call fails in
    transport, one-way sends included (note 19).

## Spec-requirement flags (spec outranks)

Re-derived against `spec-1.0.0`; re-read against `spec-1.1.0` at the 2026-10-12 row refresh and
against `spec-1.2.0` at the 2026-10-14 row refresh. Spec 1.1 adds SHOULDs and one clarification,
but no new MUST capability. Spec 1.2 adds an optional issue member and a fleet-view field, which a
collector sets where its trace store holds Benzene status and a reader MUST tolerate missing. It
too adds no new MUST capability for a service. The spec requires capabilities at several levels:

- Core MUSTs;
- a binding's obligations, for each binding a port ships;
- the Cloud Service Profile's R1–R5 and R7;
- for a service claiming to be meshed, M1/M2 (mesh §7.1);
- for a module a port implements, that module's MUSTs.

Where the spec requires a capability, a non-**yes** cell or a stated contradiction is flagged here,
not merely noted. **Open** flags first.

**Open**

- **Contract Document §4.1 rule 4 — .NET, manual composition only (ruled).** A component-name
  collision MUST fail derivation "before any message is handled". Under `UseBenzeneCloudService`
  (the profile path) the start-up check is on by default. Under manual composition, .NET throws on
  the first spec request unless the application opts in or emits the spec at build time. That is
  the deliberate trade ruled in `work/archive/self-description-design.md` §10.2 item 7, and it stays
  flagged because the module is now Stable.

**Closed at the spec-1.1.0 row refresh**

- **Mesh §3 / M2 outbound parent-id — .NET — closed.** The .NET matrix now states
  `UseMeshTraceContext()`. Per route or on every route, it stamps `MeshSpan.Current`'s
  `traceparent`/`tracestate` on sends and published events, over `UseW3CTraceContext()`. The mesh
  row also says the cloud service's M2 check needs that forwarding. Two stale passages in the same
  matrix still describe the old gap, so benzene-dotnet owes a scribe pass on them (notes 15, 18).
  All four ports now state forwarding from their mesh trace middleware. Python's matrix does not
  name the span.
- **Contract Document §4.2 derivation — Python (Q11) — closed.** Pydantic v2 models derive to
  object schemas with `required` per §4.2. `derivationCases` run from pydantic models as well as
  dataclasses, to the same hashes.
- **Contract Document §1, an absent `schemaDialect`** — the spec question is resolved by 1.1 (an
  absent dialect is unknown, so a consumer MUST reject it). All four client generators state the
  rejection, and pass `missing-schema-dialect-is-rejected` (note 12). .NET's
  `DeserializeAllowingLegacyShape` is an explicit application opt-in outside the conformant
  reader. benzene-ui reads the legacy `openapi`/`info` shape by design. Both are recorded, not
  flagged; whether a viewer's legacy read is in scope of "consumer" is the product owner's call.
- **Core — core-concepts §12 error fidelity** (closed at 1.0). All four still run the
  problem-details `envelopeCases` through their reply-receiving clients.

**Not port flags — spec 1.1 SHOULDs**

- wire-contracts §4.1 (`benzeneStatus`) and §4.2 (`code` in `FieldViolation.reason`) are stated by
  all four (note 16).
- transport-bindings §1 (transport failure is a result) is stated by all four for reply-receiving
  clients and one-way clients alike (note 19).

**No flag — re-read at 1.0**

- **Core — version selection** (core-concepts §2). No matrix contradicts it. Go's missing payload
  casting is an optional capability (§10).
- **Core — wire-contracts §2.** Header keys are read case-insensitively, and the topic key is kept
  out of the headers, on every metadata binding in all four ports (closed at this refresh).
- **Core fixtures.** All four pin `spec-1.2.0` and run the status vocabulary, the mapping tables,
  `envelope-cases.json`, and problem-details `registry` + `envelopeCases`.
- **Binding obligations.**
  - *Obligation 2 (topic sources)*: all four state every `topicSources` entry for the bindings they
    ship, including the Kafka and RabbitMQ fallbacks and Python's DynamoDB topic.
  - *Obligation 8 (settlement)*: all four claim it on every one-way binding, with the at-most-once
    override.
  - *Per-binding fixtures*: all four run `transport-metadata-cases.json` per metadata binding, plus
    `settlementDefaults`, and run `httpRules` for their HTTP binding.
  - .NET's lack of `envelope-async` is a binding it does not ship, not a broken one.
- **Profile R1–R5 and R7.** All four wire the profile in one call or composition. All four
  self-assess only R1–R5/R7 in `profile.missing` (cloud-service-profile §5). All four pass the
  contract-document producer groups that profile support requires. R5 is answered wherever the
  transport carries a reply, plus `/benzene/spec`.
- **M1/M2 service side.** All four can meet M1. They pass `mesh-trace-cases.json` and the
  producer groups that mesh §7.2 requires of a meshed service's port. All four can now meet M2's
  outbound parent-id as well (closed above).
- **Mesh collector (Stable).** All four pass `mesh-collector-cases.json` and `mesh-issue-cases.json`,
  spec 1.2's flow-status and error-detail cases included. Mesh §4.2's "last observed at (or its
  absence)" is met by all four, and Go's topic-summary zero time is gone. Where .NET and TypeScript
  omit a flow's `status` (Tempo, X-Ray summary-plane rows, X-Ray disagreement), the trace store holds
  no Benzene status for the event that decides the flow. That is the external-backend case mesh §4
  permits; readers must tolerate it (note 20).
- **Issue feed (spec 1.2).** The feed is optional on both sides (mesh §4.1), so TypeScript's missing
  emitter and Python's hand-fed `IssueAggregator` are not flags (note 20).
- **Provisional modules** (Service Versions, MCP, Claim Check — all .NET only) carry no
  obligation for a port that does not implement them. A collector without Service Versions stays
  collector-conformant (conformance README).

## Unknown cells — who owes a capability-scribe run

| Repo | Unknown cells this run | Stale or missing in the matrix (not unknown) |
|---|---|---|
| benzene-dotnet | *(none)* | Unstated: whether `placement` is ever detected (note 13); what the fleet row calls its transports field (note 14); X-Ray rows omitting a flow's `status` when the flag and spans disagree (note 20, detail). The M2 self-contradiction is fixed (`fa138e10`) |
| benzene-go | Schema registry/Confluent codec; secrets & configuration; EventBridge `benzene-headers` | Still unstated: the `required` rule for a defaulted field (note 12). Also unstated (details, note 20): whether an empty `field`/`code` merges as absent, and whether `view.html` shows `status`/`errors` |
| benzene-typescript | Which asset `Benzene.Mesh.Ui` serves; JWKS/OIDC discovery (detail); whether a consumer ignores a stale `R6`/`R8` in `profile.missing` (detail) | — |
| benzene-python | serialization; secrets & configuration; MCP; which span `with_trace_propagation` forwards (detail, note 15) | Its collector's fleet/service/instance field names (note 14) |
