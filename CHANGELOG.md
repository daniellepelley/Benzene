# Changelog

All notable changes to Benzene will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Removed
- **BREAKING:** `Benzene.Clients` / `Benzene.Clients.Aws`: deleted the alpha-era outbound client
  mechanism (Step 4 of `work/benzene-clients-redesign-plan.md`), `[Obsolete]` for one release cycle
  before removal. Deleted: `ClientsBuilder`, `SingleClientsBuilder`, `IBenzeneMessageClientFactory`,
  `IClientMessageRouter`, `IDependencyWrapper<T>` (`Benzene.Abstractions`),
  `DependencyWrapperFactory<T>`, `ClientBuilder`, `BenzeneMessageClientFactory`,
  `RetryBenzeneMessageClient`, `HeaderBenzeneMessageClient`, `HeadersBenzeneMessageClient`,
  `CorrelationIdBenzeneMessageClient`, `TraceContextBenzeneMessageClient`,
  `RetryBenzeneMessageClientWrapper`, `CorrelationIdBenzeneMessageClientWrapper`,
  `TraceContextBenzeneMessageClientWrapper`, `ClientMessageSender<TRequest,TResponse>`,
  `ClientMapping`, `ClientMappingBuilder`, `TopicAndServiceKey`, `IClientHeaders`, `ClientHeaders`
  (all `Benzene.Clients`), and `SqsBenzeneMessageClientFactory`,
  `AwsLambdaBenzeneMessageClientFactory`, `SqsBenzeneMessageClientExtensions`,
  `AwsLambdaBenzeneMessageClientExtensions`, `Extensions.AddBenzeneMessageClients`/
  `AddBenzeneMessageClient`/`AddLambdaClients` (all `Benzene.Clients.Aws`). Replaced by
  `IBenzeneMessageSender`/`OutboundRoutingBuilder`/`AddOutboundRouting(...)` and outbound
  `IMiddleware<OutboundContext>` (`.UseRetry(...)`, `.UseCorrelationId(...)`,
  `.UseW3CTraceContext()`, `.UseSqs(...)`, `.UseSns(...)`) - see
  `docs/migration-alpha-to-1.0.md` for the full mapping. `IBenzeneMessageClient` itself and its
  concrete transport clients (`SqsBenzeneMessageClient`, `SnsBenzeneMessageClient`,
  `AwsLambdaBenzeneMessageClient`, `EventBridgeBenzeneMessageClient`, `GrpcBenzeneMessageClient`,
  `KafkaBenzeneMessageClient`) are **unaffected** - only the resolution/factory/decorator layer
  around them was removed.

### Added
- `Benzene.Clients.Aws` / `Benzene.Clients`: closed a gap in Step 1 of the outbound redesign
  (`work/benzene-clients-redesign-plan.md`) - the `Benzene.Clients.Aws`-side factory/extension
  layer built on the already-obsoleted `IBenzeneMessageClientFactory`/`ClientsBuilder` mechanism
  (`SqsBenzeneMessageClientFactory`, `AwsLambdaBenzeneMessageClientFactory`,
  `SqsBenzeneMessageClientExtensions`, `AwsLambdaBenzeneMessageClientExtensions`,
  `Extensions.AddBenzeneMessageClients`/`AddBenzeneMessageClient`/`AddLambdaClients`) was never
  itself marked `[Obsolete]` in Step 1. Also newly marked, confirmed reachable only through that
  same obsoleted layer: `ClientBuilder`, `BenzeneMessageClientFactory`, `RetryBenzeneMessageClient`,
  `HeaderBenzeneMessageClient`, `HeadersBenzeneMessageClient`, `CorrelationIdBenzeneMessageClient`,
  `TraceContextBenzeneMessageClient`, `RetryBenzeneMessageClientWrapper`,
  `CorrelationIdBenzeneMessageClientWrapper`, `TraceContextBenzeneMessageClientWrapper`,
  `ClientMessageSender<TRequest,TResponse>`, `ClientMapping`, `ClientMappingBuilder`,
  `TopicAndServiceKey`, `IClientHeaders`, `ClientHeaders` (all `Benzene.Clients`). Non-breaking -
  warning-level only. **`IBenzeneMessageClient` itself and its concrete transport implementations
  (`SqsBenzeneMessageClient`, `SnsBenzeneMessageClient`, `AwsLambdaBenzeneMessageClient`,
  `EventBridgeBenzeneMessageClient`, `GrpcBenzeneMessageClient`, `KafkaBenzeneMessageClient`)
  remain untouched** - they're load-bearing for `Benzene.Grpc.Client`/`Benzene.Kafka.Core`, entirely
  outside this redesign's scope, and were never obsoleted. See the design doc's dated update for the
  full verified-safe Step 4 deletion scope.
- `Benzene.Clients.Aws` / `Benzene.Clients`: Step 3 of the outbound redesign
  (`work/benzene-clients-redesign-plan.md`) - `.UseSqs(queueUrl, ...)`/`.UseSns(topicArn, ...)` now
  have `OutboundContext` overloads, so an `OutboundRoutingBuilder.Route(topic, pipeline => pipeline.UseSqs(...))`
  route can send via SQS/SNS through new `OutboundSqsContextConverter`/`OutboundSnsContextConverter`
  (the `OutboundContext` counterparts of the existing `SqsContextConverter<T>`/`SnsContextConverter<T>`,
  reusing the same `SqsClientMiddleware`/`SnsClientMiddleware`). Also lands the design's
  middleware-ification of the old `ClientBuilder` decorators: new `CorrelationIdMiddleware`
  (`Benzene.Clients.CorrelationId`, `.UseCorrelationId(...)`) and `W3CTraceContextMiddleware`
  (`Benzene.Clients.TraceContext`, `.UseW3CTraceContext()`) stamp headers onto `OutboundContext`
  directly - converted from `CorrelationIdBenzeneMessageClient`/`TraceContextBenzeneMessageClient`.
  Retry needed no new type: the existing, already-generic `Benzene.Resilience.RetryMiddleware<TContext>`/
  `.UseRetry<TContext>(...)` already works on `OutboundContext` unmodified. **Constraint worth
  knowing**: SQS/SNS have no request/response semantics beyond a send acknowledgement, so a topic
  routed through `.UseSqs(...)`/`.UseSns(...)` must be sent via
  `IBenzeneMessageSender.SendAsync<TRequest,Void>` - any other response type compiles but throws
  `InvalidCastException` at runtime (a real, documented trade-off of `IBenzeneMessageSender`'s
  unconstrained-generic shape, not something this change fixes). `.UseAwsLambda(...)` has no
  `OutboundContext` overload yet - explicitly deferred, see the design doc's dated update and
  `Benzene.Clients.Aws/CLAUDE.md`.
- `Benzene.CodeGen.Client` / `Benzene.Clients`: Step 2 of the outbound redesign
  (`work/benzene-clients-redesign-plan.md`) - generated `{Service}ServiceClient`s now target
  `IBenzeneMessageSender` instead of `IBenzeneMessageClientFactory`: the constructor takes
  `IBenzeneMessageSender sender`, and each generated method body is
  `_sender.SendAsync<TRequest,TResponse>("topic", message, headers)` - no more per-call
  `_clientFactory.Create(...)`. Each generated client also now emits a sibling
  `{Service}ServiceClientRouting.RequiredTopics` array. New `ValidateOutboundRouting()` (on
  `IServiceResolver`) reflects over every loaded assembly's `*Routing.RequiredTopics` and throws
  `MissingOutboundRoutesException` for anything unrouted - an opt-in startup safety net for a
  missing `OutboundRoutingBuilder.Route` call, catching it before the first real send. Also widens
  `IBenzeneMessageSender.SendAsync` with an optional third `headers` parameter (a correction to
  Step 1's design, discovered while migrating the generated client - see the design doc's dated
  update) so per-call headers, a real feature of the previous generated client, aren't silently
  dropped. The generated *interface* is unchanged; this is purely a generated-class-body and
  `IBenzeneMessageSender`-shape change. `Benzene.Clients.Aws`'s transport wiring still uses the old
  mechanism - Step 3.
- `Benzene.Clients`: `IBenzeneMessageSender`/`OutboundRoutingBuilder`/`AddOutboundRouting(...)` -
  Step 1 of the outbound redesign in `work/benzene-clients-redesign-plan.md`. A single topic-keyed
  `SendAsync<TRequest,TResponse>(topic, request)` call replaces resolving a client by service
  name/topic first; `AddOutboundRouting(routing => routing.Route("order:create", pipeline => ...))`
  builds one `IMiddlewarePipeline<OutboundContext>` per topic. New `DuplicateOutboundRouteException`
  (a repeated topic) and `UnroutedTopicException` (a topic with no registered route) replace the
  previous bare `ArgumentException`/`InvalidOperationException`. Purely additive - the previous
  `ClientsBuilder`/`SingleClientsBuilder`/`IBenzeneMessageClientFactory`/`IClientMessageRouter`/
  `IDependencyWrapper<T>`/`DependencyWrapperFactory<T>` mechanism is marked `[Obsolete]` (warning,
  not an error) but not yet removed - still fully functional. `Benzene.CodeGen.Client`'s generated
  clients and `Benzene.Clients.Aws`'s transport wiring still use the old mechanism; migrating them
  onto the new one is Steps 2-3 of the plan, with the old mechanism's removal as Step 4.
- `Benzene.Aws.Lambda.Kinesis`: real per-batch checkpointing and failure containment for the
  Kinesis event source mapping's `ReportBatchItemFailures`. `KinesisStreamApplication`'s
  `StreamContext<KinesisEventRecord>` now carries a real `KinesisStreamCheckpointer` (a new
  `IStreamCheckpointer<KinesisEventRecord>`) instead of the previous no-op
  `NullStreamCheckpointer` - call `context.Checkpointer.CheckpointAsync(record)` after your stream
  handler has safely processed (or windowed past) a record. A new `KinesisBatchResponse` (mirroring
  `Amazon.Lambda.SQSEvents.SQSBatchResponse`'s wire shape) is returned by `KinesisLambdaHandler`,
  naming the sequence number to resume from - Kinesis's shard-ordered retry contract only reads the
  *first* reported failure, unlike SQS's per-message list. A pipeline exception is caught (logged)
  rather than cascaded, so the response still reflects whatever was checkpointed before the
  failure. Also adds `StreamMiddlewareApplication<TEvent,TItem,TResult>`
  (`Benzene.Core.Middleware.Streaming`), the result-producing sibling of the existing
  `StreamMiddlewareApplication<TEvent,TItem>`, directly reusable for a future SQS-streaming
  equivalent. **Behavioral change worth knowing about**: a handler that never calls
  `CheckpointAsync` now gets a response naming the *first* record's sequence number on any
  exception (AWS retries the whole batch) instead of the previous silent no-op - purely
  additive/more-correct, no code changes required to adopt it. See
  `work/kinesis-batch-failure-handling-design.md` and `Benzene.Aws.Lambda.Kinesis/CLAUDE.md`.
- `Benzene.Kafka.Core`: live-broker test coverage for `BenzeneKafkaWorker<TKey,TValue>`'s own
  `Consume` loop (`test/Benzene.Integration.Test/Kafka/BenzeneKafkaWorkerLiveTest.cs`), the last
  self-hosted worker gap called out in the package's `CLAUDE.md` — a real worker, hosted via
  `Benzene.HostedService.BuildHostedService()`, now consumes a real message produced against the
  Event Hubs emulator's Kafka-compatible endpoint (the same emulator `Benzene.Azure.Function.Kafka`'s
  `KafkaConsumerPipelineTest` already exercises in CI) and dispatches it through the full
  message-handler pipeline. Test-only change, no production code touched.
- `Benzene.Azure.Function.ServiceBus`: `ServiceBusOptions.AckMode` (default `ServiceBusAckMode.AutoComplete`,
  reproducing today's Functions-host-auto-completes behavior exactly) - set `ServiceBusAckMode.Explicit`
  for true per-message `CompleteMessageAsync`/`AbandonMessageAsync` control tied to the handler's
  outcome. Requires the trigger's `AutoCompleteMessages = false` (Functions-runtime config, outside
  Benzene's control) and a new `HandleServiceBusMessages(IAzureFunctionApp, ServiceBusMessageActions,
  params ServiceBusReceivedMessage[])` overload that threads `ServiceBusMessageActions` through a new
  `ServiceBusTriggerBatch` request type (named to avoid colliding with the real
  `Azure.Messaging.ServiceBus.ServiceBusMessageBatch` SDK type) - `ServiceBusBatchApplication` now
  implements both `IMiddlewareApplication<ServiceBusReceivedMessage[]>` (unchanged) and
  `IMiddlewareApplication<ServiceBusTriggerBatch>` (new), sharing one instance. The plain
  `ServiceBusReceivedMessage[]` overload never touches message completion, even if `AckMode` is set to
  `Explicit` - there's nothing to act on without `ServiceBusMessageActions`. Purely additive. Closes
  the "candidates for future work" gap flagged in the package's own `CLAUDE.md` and
  `work/batch-failure-handling.md`'s deferred table.

### Changed
- **BREAKING:** `Benzene.CodeGen.Core`'s example payload generation moved to
  `Benzene.Schema.OpenApi` so examples can be generated at runtime during spec builds, not just at
  codegen time: `IPayloadBuilder`/`PayloadBuilder` are now
  `Benzene.Schema.OpenApi.Examples.IExamplePayloadBuilder`/`ExamplePayloadBuilder`, and
  `ISchemaGetter`/`SchemaGetter` moved namespace to `Benzene.Schema.OpenApi.Examples` (same names).
  `BuildAsJson`/`Build(Type)` extensions moved with them. The generator is also hardened: it now
  honours schema `example`/`default`/`enum` values, the `date`/`email`/`uri` formats, sizes strings
  within `minLength`/`maxLength`, clamps numbers into `minimum`/`maximum`, accepts known values
  keyed by property path (`order.customer.email`) as well as bare name, expands non-cyclic nested
  `$ref`s fully (previously any second-level object reference collapsed to `{}`), and terminates
  reference cycles — direct or mutual — as `{}`/`[]` with a maximum reference depth of 8. Output
  for previously-supported shapes is unchanged.

### Added
- `Benzene.Results`: two new statuses — `TooManyRequests` (throttled/rate limited; HTTP 429, gRPC
  `ResourceExhausted`) and `Timeout` (downstream deadline elapsed; HTTP 504, gRPC
  `DeadlineExceeded`) — with the full complement of factories (`BenzeneResult.TooManyRequests`/
  `Timeout`) and extensions (`IsTooManyRequests()`/`IsTimeout()`). `BenzeneResultStatus` becomes
  the single owner of status classification: new `IsSuccess`/`IsFailure`/`IsKnown`/`IsTransient`
  helpers, with `BenzeneResultHttpMapper`, the conformance status handler, and
  `RetryBenzeneMessageClient` rewired onto them. HTTP reverse mapping gains explicit
  408/422/429/500/501/502/503/504 rows in both `BenzeneResultHttpMapper` and
  `Convert(HttpStatusCode)` (422/501/503 previously fell to `UnexpectedError` through the
  latter). The portable spec (`wire-contracts.md` §3/§4) and its conformance fixtures are
  updated in lockstep. **Behavioral changes:** `BenzeneResult.Set(status)`/`Set(status, payload)`
  now derive `IsSuccessful` from the status class (a known failure status yields an unsuccessful
  result; application-defined statuses keep the successful default; `Set<T>(status, bool)` stays
  explicit, and a new `Set<T>(status, payload, isSuccessful)` overload covers
  failure-status-with-payload-body results — `HealthCheckProcessor` uses it to keep the
  unhealthy 503 + report-body behavior); `RetryBenzeneMessageClient` also retries `TooManyRequests` (not `Timeout` — retrying
  a possibly-applied operation is only safe when idempotent; opt in via the new `shouldRetry`
  constructor parameter) and returns the last inner result after exhausting retries instead of a
  synthesized `ServiceUnavailable`; gRPC reverse mapping `DeadlineExceeded` now yields `Timeout`
  (was `ServiceUnavailable`). Fixes two missing-`$` interpolation bugs in the unmapped-status
  error messages. See `docs/plans/results-taxonomy-plan.md` and `docs/reference/results.md`.
- `Benzene.CodeGen.Core`: `CodeGenHelpers.GenerateHash(EventServiceDocument)` — computes the
  contract hash over a normalized document with the non-contract decoration stripped (generated
  `example` payloads, `messageEndpoint`). Both the handler-array overload and
  `Benzene.CodeGen.Client`'s baked-in SDK `HashCode` now go through it, so contract hashes are
  **unchanged** by the new spec examples and existing deployed client SDKs don't falsely report
  contract drift against upgraded services.
- `Benzene.CodeGen.LambdaTestTool` (renamed from `Benzene.CodeGen.MockLambdaTool`, whose package
  name was out of sync with its `Benzene.CodeGen.LambdaTestTool` namespace — **breaking** for
  anyone referencing the old package id): productized test-payload-file generation.
  `DefaultExampleBuilders` provides the standard per-transport set (BenzeneMessage envelope, SNS,
  SQS, API Gateway), `LambdaTestFilesBuilder` gains a parameterless constructor using it, and the
  CodeGen CLI gains a `lambda-test-tool` command (`-profile`/`-lambda-name` to fetch the spec
  from a Lambda, or `-file` to read it from disk; `-directory` for output). See
  `docs/payload-testing.md`.
- `Benzene.Http`: BenzeneMessage-over-HTTP endpoint — `UseBenzeneMessage` (namespace
  `Benzene.Http.BenzeneMessage`) mounts a `BenzeneMessageHttpMiddleware<TContext>` on any Benzene
  HTTP pipeline (API Gateway, Azure Functions, ASP.NET Core, self-host): POST a
  `{ topic, headers, body }` envelope and it dispatches through the BenzeneMessage pipeline (the
  same `"benzene"` transport as direct Lambda invoke) and returns the response envelope as JSON
  with the HTTP status mapped from the envelope status. Same name and overload shapes as the
  Lambda adapter (inline pipeline action or shared pre-built builder), plus
  `BenzeneMessageHttpOptions` (`Path`, default `/benzene-message`; `TopicFilter` allowlist).
  Strictly opt-in — it exposes every routed topic over HTTP, so see `docs/payload-testing.md` for
  the security posture. Registers `IBenzeneMessageHttpEndpointInfo`, which the `benzene` spec
  advertises as a new optional top-level `messageEndpoint` field (additive; round-tripped by
  `EventServiceDocumentDeserializer`). Replaces the ad-hoc `UseHttpToBenzeneMessage` prototype in
  `examples/Aws`.
- `Benzene.Spec.Ui`: "Try it" panel — when the loaded spec advertises `messageEndpoint`, every
  topic/event card gains an editable payload (pre-filled from the spec `example`), a headers
  editor, and a Send/Dispatch button that POSTs the BenzeneMessage envelope and renders the
  response envelope inline (HTTP + Benzene status chips, headers, pretty-printed body, duration).
  The page stays fully self-contained (vanilla JS, no external requests) and degrades to the
  read-only viewer when no `messageEndpoint` is present.
- `Benzene.Schema.OpenApi`: the `benzene` spec format now carries a generated `example` payload on
  every request topic and broadcast event, produced during `EventServiceDocumentBuilder.Build()` by
  the hardened `ExamplePayloadBuilder` (deterministic, validation-aware — see the Changed entry).
  `RequestResponse.Example`/`Event.Example` are new optional members; the field is additive, and
  `EventServiceDocumentDeserializer` round-trips it. `Benzene.Spec.Ui` renders the example per
  topic/event with a copy button. See `docs/spec.md` / `docs/spec-ui.md`.
- `Benzene.Aws.Lambda.Sqs`: `SqsOptions.BatchFailureMode` (via a new `Action<SqsOptions>? configure`
  parameter on `UseSqs`) - defaults to `SqsBatchFailureMode.PartialBatchFailure`, reproducing
  today's per-message `SQSBatchResponse.BatchItemFailures` reporting exactly. Set to
  `SqsBatchFailureMode.FailWholeBatch` to instead throw the new `SqsBatchProcessingException` when
  any message in the batch fails, so SQS retries the whole batch instead of just the failed
  messages. Purely additive.
- `Benzene.Aws.Lambda.Sns`: `SnsOptions.CatchExceptions` and `SnsOptions.RaiseOnFailureStatus` (via
  a new `Action<SnsOptions>? configure` parameter on `UseSns`) - both default to `false`,
  reproducing today's implicit behavior exactly (a handler exception cascades to fail the Lambda
  invocation, triggering SNS's own subscription retry policy; a non-exception failure result is
  silently accepted, no retry). `CatchExceptions = true` catches and logs exceptions instead of
  cascading them; `RaiseOnFailureStatus = true` escalates a non-exception failure result into the
  new `SnsMessageProcessingException`, so SNS retries it too - `CatchExceptions` governs both real
  and escalated exceptions uniformly. Purely additive. See `work/batch-failure-handling.md` for the
  general containment/escalation vocabulary this establishes for other batch/event transports.
- `Benzene.Kafka.Core`: `BenzeneKafkaConfig.CatchHandlerExceptions` (default `true`, reproducing
  today's catch-log-continue behavior exactly) - set `false` to instead stop the whole worker on
  the first unhandled handler exception. Implemented in the shared
  `Benzene.SelfHost.BoundedConcurrentDispatcher<T>` as new optional `catchExceptions`/`onFault`
  constructor parameters (both back-compatible defaults, so `Benzene.SelfHost.Http.BenzeneHttpWorker`
  is unaffected). Purely additive.
- `Benzene.Kafka.Core`: `BenzeneKafkaConfig.CommitOnlyOnSuccess` (default `false`, reproducing
  Confluent.Kafka's own default of auto-storing an offset as soon as it's consumed) - set `true` for
  at-least-once processing, so a message whose handler fails (or whose worker crashes mid-handling)
  is redelivered instead of silently skipped. Sets `ConsumerConfig.EnableAutoOffsetStore = false` and
  calls `IConsumer.StoreOffset` only after the handler succeeds. Requires
  `CatchHandlerExceptions = false` and `PreserveOrderPerPartition = true` - `BenzeneKafkaWorker`
  throws `InvalidOperationException` at startup otherwise, since `StoreOffset` is a last-write-wins
  watermark with no gap tracking and either combination could silently commit past a failed message.
  Purely additive.
- `Benzene.Azure.Function.Kafka`: `KafkaOptions.CatchExceptions`/`RaiseOnFailureStatus` (via a new
  `Action<KafkaOptions>? configure` parameter on `UseKafka`) - same shape and defaults as
  `Benzene.Aws.Lambda.Sns`'s `SnsOptions`; `RaiseOnFailureStatus` escalates into a new
  `KafkaMessageProcessingException`. `KafkaMessageMessageHandlerResultSetter` now records the
  outcome onto `KafkaContext.MessageResult` (previously a true no-op) so `RaiseOnFailureStatus` has
  something to read. Purely additive.
- `Benzene.Azure.Function.ServiceBus`: `ServiceBusOptions.CatchExceptions`/`RaiseOnFailureStatus`
  (via a new `Action<ServiceBusOptions>? configure` parameter on `UseServiceBus`) - same shape as
  the Kafka entry above, escalates into a new `ServiceBusMessageProcessingException`.
  `ServiceBusMessageMessageHandlerResultSetter` likewise upgraded from a no-op to a real setter.
  Still not true per-message `ServiceBusMessageActions` completion (a larger, separate follow-up -
  see `work/batch-failure-handling.md`). Purely additive.
- `Benzene.Aws.Sqs`: `SqsConsumerOptions.AckMode` (via a new `Action<SqsConsumerOptions>? configure`
  parameter on `UseSqs`) for the standalone polling `SqsConsumer` - defaults to
  `SqsConsumerAckMode.WholeBatch`, reproducing today's all-or-nothing batch deletion exactly. Set to
  `SqsConsumerAckMode.PerMessage` to delete only the messages that actually succeeded instead.
  Required `SqsConsumerMessageContext` to gain `IHasMessageResult` (it had no result concept before)
  and `SqsConsumerMessageMessageHandlerResultSetter` to become a real setter instead of a no-op.
  Purely additive.
- `Benzene.Core.MessageHandlers`: `UsePresetTopic<TContext>(topicId, version)` pipeline extension,
  `PresetTopicMiddleware<TContext>`, and `PresetTopicMessageTopicGetter<TContext>` - lets a specific
  SQS queue or Service Bus subscription route every message to one fixed topic, for producers that
  aren't Benzene clients and never set the usual `topic` message attribute/property. The preset
  topic is carried in a new scoped `PresetTopicHolder` (one per message), resolved from the same DI
  scope by the middleware that sets it and the getter that reads it back - deliberately not a
  property on the transport context, so `SqsMessageContext` (`Benzene.Aws.Lambda.Sqs`),
  `SqsConsumerMessageContext` (`Benzene.Aws.Sqs`), and `ServiceBusContext`
  (`Benzene.Azure.Function.ServiceBus`) stay plain descriptions of their transport message with no
  new interface to implement. See `Benzene.Abstractions.Middleware/CLAUDE.md`'s "Context purity"
  convention for the general pattern. Purely additive - a pipeline that never calls
  `UsePresetTopic` behaves exactly as before.
- `Benzene.Mesh.Aggregator`: `IMeshServiceSource` port - the aggregator's spec/health fetch is no
  longer inlined HTTP; it's delegated per-`MeshServiceRegistryEntry.Source` (new additive field,
  defaults to `"Http"`) to a registered `IMeshServiceSource`. `HttpMeshServiceSource` (the original
  behavior, moved not rewritten) ships as the default. `MeshServiceRegistryEntry` also gains an
  additive `SourceOptions` (untyped string dictionary) for source-specific config. Foundation for
  upcoming non-HTTP sources (AWS Lambda Invoke) and a push/self-report ingestion path. **BREAKING:**
  `MeshAggregator`'s constructor changed from `(HttpClient, IMeshArtifactStore, Func<DateTimeOffset>?)`
  to `(IEnumerable<IMeshServiceSource>, IMeshArtifactStore, Func<DateTimeOffset>?)` - direct
  construction outside `AddMeshAggregator` (whose own signature is unchanged) needs
  `new[] { new HttpMeshServiceSource(httpClient) }` instead of a bare `HttpClient`.
- `Benzene.Mesh.Aws.Lambda`: `LambdaMeshServiceSource`, a second `IMeshServiceSource` for services
  reachable only via a synchronous AWS Lambda `Invoke` (no public HTTP surface) - sends a
  `"spec"`/`"healthcheck"` topic message to `SourceOptions["functionName"]` via the existing
  `Benzene.Clients.Aws.Lambda.IAwsLambdaClient`, no new Lambda-invocation plumbing. New
  `MeshServiceSource.AwsLambdaInvoke` constant. `AddMeshLambdaSource()` registers it alongside
  `AddMeshAggregator(...)`. No new external NuGet dependency (`AWSSDK.Lambda` already flows in
  transitively via `Benzene.Clients.Aws`).
- `Benzene.Mesh.Contracts`: `MeshServiceReport`/`IMeshReportPublisher` - the push/self-report shapes
  for services with no synchronous entry point at all (e.g. SQS/SNS/EventBridge-only Lambdas), a
  deliberate small widening of this package's role to "data shapes + zero-I/O port interfaces."
- `Benzene.Mesh.Aggregator`: `ArtifactStoreMeshReportPublisher` (direct-write `IMeshReportPublisher`)
  and `MeshReportMessageHandler` (`[HttpEndpoint("POST", "/mesh/report")]`/`[Message("mesh:report")]`,
  opt-in ingestion endpoint, not auto-registered by any existing wiring beyond the default publisher).
- `Benzene.Mesh.Reporting`: new lightweight package (depends on `Benzene.Mesh.Contracts` only) for
  services that self-report. `HttpMeshReportPublisher` posts to an ingestion endpoint;
  `MeshSelfReportMiddleware<TContext>` publishes opportunistically as a side effect of real
  requests/messages (throttled, never blocking, never propagating a failure) - no scheduled/cron
  reporting in v1, since that would defeat serverless on-demand billing. Spec/health are supplied
  as delegates, so this package stays free of a dependency on `Benzene.Schema.OpenApi`/
  `Benzene.HealthChecks`.
- `deploy/Mesh/Benzene.Mesh.Host`: new config-driven, Docker/Compose-deployable Benzene Mesh
  Aggregator+UI - for running the mesh dashboard against real services in local dev (distinct from
  `examples/Mesh/`'s fake-data demo). Reads `mesh.json` (bind-mounted, `MESH_CONFIG_PATH`) via
  `IConfiguration.Get<MeshHostConfig>()` - this repo's first binding of a list of config objects.
  Wires `AddMeshAggregator` + `AddMeshLambdaSource`, plus a new `MeshPollBackgroundService`
  (timer-triggered aggregation, since bare Compose has no external scheduler - local to this Host
  only). Own `Benzene.Mesh.Host.sln`, not part of `Benzene.sln`/`Benzene.Examples.sln`. New CI:
  `build-mesh-host.yml` (compiles on every push/PR) and `deploy-mesh-host.yml` (manual
  `workflow_dispatch`, publishes to GHCR - the first Docker image publish in this repo). Completes
  the four-phase multi-transport mesh data collection epic (Phases A-D) - see
  `work/service-mesh-roadmap-1.0.md`.
- RetryMiddleware component for exponential backoff retry logic
- FluentValidation extensions
- Source generators for message handlers
- Claude agent for architecture reviews
- `BenzeneException(string message, Exception innerException)` constructor overload,
  so wrapped exceptions preserve proper `.InnerException` chaining instead of being
  stringified into the message text
- Amazon EventBridge integration: inbound Lambda adapter (`Benzene.Aws.Lambda.EventBridge` —
  `detail-type` is the topic, `detail` is the body) and outbound `PutEvents` client
  (`EventBridgeBenzeneMessageClient` in `Benzene.Clients.Aws`), with test helpers
  (`AsEventBridge()`)
- Terraform EventBridge rule generation (`TerraformEventBridgeRuleBuilder` in
  `Benzene.CodeGen.Terraform`): `aws_cloudwatch_event_rule`/target/permission generated from a
  service's `[Message]` topics, discoverable via `ReflectionMessageHandlersFinder`
- DynamoDB Streams integration (`Benzene.Aws.Lambda.DynamoDb`): change-data-capture Lambda
  adapter — topic is `"{tableName}:{eventName}"`, body is the record image unmarshalled from
  AttributeValue format to plain JSON; ordered sequential processing with stop-at-first-failure
  partial-batch checkpointing; test helpers (`AsDynamoDb()`); no new NuGet dependencies
- `Benzene.MessagePack`: MessagePack support (`MessagePackMediaFormat<TContext>` +
  `AddMessagePack()`/`AddMessagePack<TContext>()`/`UseMessagePack<TContext>()`), the deferred
  binary-format follow-up named in Phase 4 of `docs/plans/request-response-improvements-plan.md`.
  New NuGet dependency: `MessagePack` (MessagePack-CSharp) 3.1.8. Since every Benzene transport's
  body is a `string` today, `MessagePackSerializer` Base64-armors the msgpack bytes rather than
  throwing from its string members, so it works unchanged through every existing transport's
  request/response pipeline while still exercising Phase 4's byte-oriented path on
  `BenzeneMessageContext`
- `benchmarks/Benzene.Benchmarks`: the repo's first BenchmarkDotNet suite, covering
  `MiddlewarePipeline<TContext>.HandleAsync` (chain-construction cost, isolated from and combined
  with DI scope creation, at 1/5/20 middlewares) and
  `MultiSerializerOptionsRequestMapper<TContext>.GetBody<T>` (first-call negotiation/cache-build
  cost vs. warmed-cache steady state). Included in `Benzene.sln` so CI compile-checks it, but not
  run as part of CI — see `benchmarks/Benzene.Benchmarks/README.md`. New NuGet dependency:
  `BenchmarkDotNet` 0.15.8. No recorded baseline numbers yet — this is the first-ever suite
- `templates/Benzene.Templates`: the repo's first `dotnet new` project-template pack, with six
  starter templates — `benzene.asp`, `benzene.aws.apigateway`, `benzene.aws.sqs`, `benzene.aws.sns`,
  `benzene.azure.http`, `benzene.kafka.worker` — each generating a complete, buildable project
  wired around the same `HelloWorldMessageHandler` demo handler (the Kafka worker's is a
  fire-and-forget variant by necessity). Generated projects reference Benzene packages with a
  floating `Version="*-*"` rather than a pinned version, so they always restore the latest
  published (prerelease) package. Verified by a new `.github/workflows/build-templates.yml` (packs,
  installs, generates, and builds every template — plus a daily schedule run, since a floating
  version can break a template with zero content changes to this repo). Published to nuget.org via
  a new manual `.github/workflows/deploy-templates.yml` (separate from `deploy-benzene.yml` since
  `Benzene.Templates` isn't part of `Benzene.sln` and versions independently) — see
  `templates/README.md` and [Project Templates](docs/getting-started-templates.md) for
  installing/generating a project with `dotnet new`.
  No new library dependency — the pack project uses only `PackageType=Template`/`IncludeContentInPack`,
  the standard `dotnet pack` mechanism for a template pack, no extra tooling package

### Fixed
- `Benzene.SelfHost.Http`: fixed the self-hosted HTTP transport never actually finishing a response.
  Its `IMessageHandlerResultSetter<SelfHostHttpContext>` (previously named `KafkaMessageHandlerResultSetter`
  — an apparent copy-paste artifact from `Benzene.Kafka.Core`) unconditionally forced
  `Response.StatusCode = 200` regardless of the real result and never ran the registered
  `IResponseHandler<SelfHostHttpContext>` chain, so response bodies were never written and the
  underlying `HttpListenerResponse` was never closed/finalized. Discovered while adding the package's
  first real end-to-end test coverage (`test/Benzene.Core.Test/SelfHost/Http/BenzeneHttpWorkerTest.cs`,
  a real `HttpListener` bound to a loopback port, driven by a real `HttpClient`). **BREAKING:**
  `KafkaMessageHandlerResultSetter` renamed to `HttpListenerMessageHandlerResultSetter` and now
  inherits `ResponseMessageMessageHandlerResultSetterBase<SelfHostHttpContext>` (same pattern as
  `AspMessageMessageHandlerResultSetter`/`ApiGatewayMessageMessageHandlerResultSetter`) — only affects
  code that referenced the old class name directly, not normal `AddHttp()`/`UseHttp()` usage. HTTP
  status codes returned by this transport now correctly reflect the actual handler result instead of
  always being 200.
- `Benzene.CodeGen.Cli.Core`: `HelpCommand` (`benzene help <command>`) threw a
  `NullReferenceException` instead of a usage error when given an unrecognized command name,
  discovered while adding test coverage for the CLI's help/routing/payload-mapping glue
  (`CommandRouter`, `CommandBase<TPayload>`, `PayloadMapper`, `HelpGenerator`). Now writes
  `Command <name> not found` to `Console.Error`, matching `CommandRouter`'s existing behavior for an
  unrecognized top-level command name.

### Removed
- **BREAKING:** `UseCorrelationId()` (`Benzene.Diagnostics.Correlation`) — the legacy inbound
  correlation-header pickup middleware, previously `[Obsolete]`. Use `UseW3CTraceContext()` for
  cross-service correlation; `ICorrelationId`/`AddCorrelationId()`/`WithCorrelationId()` and the
  outbound `WithCorrelationId()` client decorator remain. See the migration guide.
- **BREAKING:** `WithRequestId()`/`WithApplication()` (`Benzene.Aws.Lambda.Core.LogContextBuilderExtensions`),
  previously `[Obsolete]` — use the portable `UseBenzeneEnrichment()` instead. (The
  transport-agnostic `WithApplication()` in `Benzene.Core.MessageHandlers` is unaffected.)

### Added
- `Benzene.GoogleCloud.Functions.PubSub` (+ `.TestHelpers`) — real Pub/Sub push adapter for Google
  Cloud Functions Gen2, Phase 1 of `work/google-cloud-roadmap-1.0.md`. Replaces the old,
  non-functional `examples/Google` Pub/Sub stub with `GooglePubSubFunctionHost<TStartUp>` wired
  through `UseMessageHandlers()`, using the same `"topic"` message-attribute convention already
  established by `Benzene.Aws.Sqs`/`Benzene.Aws.Lambda.Sqs`/`Benzene.Aws.Lambda.Sns`/
  `Benzene.Azure.Function.ServiceBus`. `PubSubOptions.CatchExceptions`/`RaiseOnFailureStatus`
  (both default `false`) reuse the same containment/escalation shape as
  `Benzene.Azure.Function.Kafka`/`Benzene.Azure.Function.ServiceBus`'s options. Cloud Functions
  delivers exactly one Pub/Sub message per invocation, so there's no batch/fan-out loop involved.
  Preset-topic override and `examples/Google` wiring are deliberately not part of this pass — see
  the package's own `CLAUDE.md` and the roadmap doc's Phase 1 update note.
- `CONTRIBUTING.md` — dev setup, a pointer to `AGENTS.md`/package `CLAUDE.md` files, and PR
  expectations for external contributors. `README.md`'s inline "Contributing" section now points
  to it instead of duplicating a shorter version of the same content.

### Changed
- CI (`build-benzene.yml`): `Benzene.Grpc.Test`, `Benzene.Mesh.Test`, and `Benzene.Conformance.Test`
  now run as part of the main `build` job, alongside the existing `Benzene.Core.Test` run. All three
  were already part of `Benzene.sln` (so already compiled by CI) but were previously never actually
  executed anywhere except a developer's own machine.
- Updated to .NET 10
- IContextConverter is now async
- Cleaned up namespaces across AWS Lambda packages
- Renamed AWS Lambda projects for better clarity
- **BREAKING:** Renamed `BenzeneWorkerStartup2` (`Benzene.SelfHost`) to `BenzeneWorkerBuilder`,
  matching its file name and removing the versioned-name smell flagged in the 1.0 API review
- Request/response pipeline: Phase 1 of `docs/plans/request-response-improvements-plan.md` —
  correctness and hot-path performance fixes with no reshaping of the request/response design.
  Content-type matching (`ISerializerOption`/XML response selection) now tolerates `;`-delimited
  parameters and casing (`application/xml; charset=utf-8` now correctly selects XML instead of
  silently falling back to JSON) via a new `MediaType.Matches`/`MediaTypeHeaderContextPredicate`
  (`Benzene.Core.Messages`). `MessageHandlerFactory` dispatch, handler-definition lookup
  (new `MessageHandlerDefinitionIndex`, singleton-cached, topic-id-keyed), and
  `MultiSerializerOptionsRequestMapper`'s composed mapper no longer repeat reflection/allocation
  work on every message. `Benzene.Xml.XmlSerializer` caches its underlying
  `System.Xml.Serialization.XmlSerializer` per type instead of constructing one per call.
  **BREAKING:** `MessageHandlerDefinitionLookUp`'s constructor now takes a
  `MessageHandlerDefinitionIndex` instead of `IEnumerable<IMessageHandlersFinder>`;
  `JsonSerializationResponseHandler`/`XmlSerializationResponseHandler` now take their serializer
  via constructor injection instead of constructing one internally. All in-repo consumers go
  through DI and pick these up automatically.
- Request/response pipeline: Phase 2 of `docs/plans/request-response-improvements-plan.md` —
  media-format unification. Request-side `ISerializerOption<TContext>` and response-side
  `ISerializationResponseHandler<TContext>` are replaced by a single `IMediaFormat<TContext>`
  (content type + can-read + can-write + serializer), selected per message by a scoped, memoizing
  `IMediaFormatNegotiator<TContext>` (`content-type` for reads, `accept` for writes — tolerant of
  `;`-delimited parameters and casing via Phase 1's `MediaType.Matches`). One
  `SerializationResponseHandler<TContext>` now writes every response body, replacing the old
  per-format response stack, and **this fixes `Benzene.AspNet.Core`/`Benzene.Azure.Function.AspNet`/
  `Benzene.SelfHost.Http` silently not supporting XML** (response format now genuinely honors
  `Accept`, not just `Benzene.Aws.Lambda.ApiGateway`). `Benzene.Xml` becomes one
  `XmlMediaFormat<TContext>` + `AddXml()`/`AddXml<TContext>()`/`UseXml<TContext>()`.
  `Benzene.Extras` gains `InlineMediaFormat<TContext>` as the inline-registration replacement.
  **BREAKING:** `ISerializerOption<TContext>`, `SerializerOptionBase`, `InlineSerializerOption`,
  `IRequestMapBuilder<TContext>`, `RequestMapBuilder<TContext>` are removed (replaced by
  `IMediaFormat<TContext>`/`InlineMediaFormat<TContext>`); `ISerializationResponseHandler<TContext>`,
  `IBodySerializer`, `BodySerializer<TContext>`, `ResponseBodyHandler<TContext>`,
  `ResponseHandler<T,TContext>`, `JsonSerializationResponseHandler<TContext>`,
  `JsonDefaultMultiSerializerOptionsRequestMapper<TContext>` are removed (replaced by
  `SerializationResponseHandler<TContext>`); `MultiSerializerOptionsRequestMapper<TContext>` drops
  its `TDefaultSerializer` type parameter; `IResponseHandler<TContext>` is reshaped to a single
  `ValueTask HandleAsync(TContext, IMessageHandlerResult)` method, removing the
  `ISyncResponseHandler`/`IAsyncResponseHandler` split; `Benzene.Xml`'s mutable static `Settings`
  class, `XmlSerializationResponseHandler`, `XmlResponseHandler`, `XmlSerializerOption`, and
  `XmlContentTypeHeaderContextPredicate` are removed (replaced by `XmlMediaFormat<TContext>`). All
  in-repo consumers (the four HTTP-ish transports, `BenzeneMessage`, six AWS event-source
  transports) go through DI and were updated to register the new types.
- Request/response pipeline: Phase 3 of `docs/plans/request-response-improvements-plan.md` — the
  renderer seam. Response writing is no longer serializer-only: a new `IResponseRenderer<TContext>`
  (`CanRender`/`RenderAsync`) lets a handler's result be written in any representation, not just
  JSON/XML. Phase 2's `SerializationResponseHandler<TContext>` becomes
  `SerializerResponseRenderer<TContext>` (the catch-all renderer, registered last, unchanged
  JSON/XML behavior), wrapped by a new `RendererResponseHandler<TContext>` (the actual
  `IResponseHandler<TContext>` every transport registers) that short-circuits if a body is already
  set, then walks registered renderers in order and delegates to the first whose `CanRender`
  matches — a custom renderer (e.g. HTML, matched via `accept: text/html`) registers before the
  serializer renderer and owns its own error representation instead of `ErrorPayload` JSON. New
  `IRawContentMessage : IRawStringMessage` (`Benzene.Abstractions.Messages`) lets a handler's raw
  payload carry its own content type, honored by `SerializerResponseRenderer` in place of the
  negotiated format's content type. All in-repo consumers (the four HTTP-ish transports,
  `BenzeneMessage`) go through DI and were updated to register `SerializerResponseRenderer` +
  `RendererResponseHandler` in place of the deleted `SerializationResponseHandler`.
- Request/response pipeline: Phase 4 of `docs/plans/request-response-improvements-plan.md` —
  byte-oriented serialization (additive, no breaking changes). New `IPayloadSerializer : ISerializer`
  (`Benzene.Abstractions.Serialization`) adds `Serialize(Type, object, IBufferWriter<byte>)` and
  `Deserialize(Type, ReadOnlySpan<byte>)`; `Benzene.Core.MessageHandlers.Serialization.JsonSerializer`
  now implements it via `Utf8JsonWriter`/`Utf8JsonReader` (byte-path and string-path output are
  byte-identical). New optional `IMessageBodyBytesGetter<TContext>`
  (`Benzene.Abstractions.Messages.Mappers`) lets a transport expose its body as bytes;
  `RequestMapper<TContext>` prefers deserializing straight from those bytes (skipping the
  intermediate string) when the selected serializer implements `IPayloadSerializer` and the getter
  is registered, otherwise the string path is unchanged. `IBenzeneResponseAdapter<TContext>` gains
  a default-interface `SetBody(TContext, ReadOnlyMemory<byte>)` member (UTF-8-decodes and delegates
  to the string overload by default, so every existing adapter keeps compiling and behaving
  identically without any changes). `BenzeneMessageContext` is wired as the reference transport
  (`BenzeneMessageGetter` now also implements `IMessageBodyBytesGetter`); converting the other
  transports' body getters, a binary format package (Protobuf/MessagePack), and a byte-oriented
  response-writing path are explicitly deferred to future work once a consumer needs them.

### Fixed
- `EnrichingRequestMapper`'s XML docs corrected: enrichers fold onto the mapped request in
  registration order with earlier enrichers taking precedence (a later enricher can only fill in
  a property no earlier one has set) — the docs previously claimed the opposite
- `KafkaClientMiddleware` no longer silently swallows produce exceptions — failures now propagate
  (mapped to `ServiceUnavailable` by `KafkaBenzeneMessageClient`), and `KafkaContextConverter`
  maps the delivery result's `PersistenceStatus` (`Persisted` → `Accepted`, anything else →
  `ServiceUnavailable`) instead of unconditionally reporting success
- **CRITICAL:** Fixed bug in `BenzeneServiceContainerExtensions.TryAddSingleton(Type)` that was incorrectly calling `AddScoped` instead of `AddSingleton`
- **CRITICAL:** Fixed bug in `Extensions.Split()` method that was passing wrong variable to builder
- Fixed Kafka package compatibility with examples
- Fixed AWS Lambda example configuration
- Fixed enrichment values bug where values would fail if they were the wrong type
- Fixed service resolver factory issue
- Fixed build issues
- Fixed `Benzene.Examples.sln`, which referenced pre-restructure project paths/names
  and did not build; regenerated against the current project layout
- Fixed 6 example projects (`Benzene.Example.Azure`, `Benzene.Examples.CodeGen.Client`,
  `Benzene.Examples.Google`, `Benzene.Example.Grpc`, `Benzene.Examples.Aws`,
  `Benzene.Examples.Kakfa`) with pre-existing compile errors that had been masked by
  the broken solution file
- `MicrosoftServiceResolverAdapter`/`AutofacServiceResolverAdapter` now wrap DI
  resolution failures via `BenzeneException`'s new inner-exception constructor
  instead of stringifying the original exception into the message text
- `NullBenzeneServiceContainer`'s registration methods now throw
  `NotImplementedException` with a message explaining it's an intentional
  null-object placeholder, instead of the bare, contextless default message
- **Resource leak:** `MicrosoftServiceResolverAdapter`/`AutofacServiceResolverAdapter` now actually
  dispose the DI scope created by `IServiceResolverFactory.CreateScope()` — previously `Dispose()`
  was a no-op on both adapters, so every ASP.NET Core request, Lambda invocation, and SQS/DynamoDB
  batch record leaked its scope's `IDisposable`/`IAsyncDisposable` scoped services (DB
  connections/contexts, etc.), regardless of calling code correctly doing
  `using var scope = serviceResolverFactory.CreateScope();`
- `MiddlewarePipeline<TContext>` no longer re-reverses its middleware array on every single
  `HandleAsync` call — the order is precomputed once at construction instead. The `_cachedChain`
  field this replaces was dead code (declared and read but never assigned, so the "cache" branch
  was unreachable)
- `Benzene.Mesh.Aggregator.MeshAggregator.RunOnceAsync` now polls every registered service
  concurrently instead of one at a time (mirroring `HealthCheckProcessor.PerformHealthChecksAsync`),
  and each service's spec/health fetch is bounded by an explicit 10-second timeout instead of
  relying solely on the injected `HttpClient`'s own (much longer) default — a single slow/hung
  service could previously stall the whole run

### Removed
- Removed ToDelete folder - `IMessageResult` and `IHasMessageResult` moved to proper location in `Benzene.Abstractions.MessageHandlers`
- **BREAKING:** Removed `BenzeneServiceContainerExtensions.AddScoped<T>(T instance)`.
  It was unreachable dead code — `IBenzeneServiceContainer` declares its own
  `AddScoped<T>(T instance)` member with the same signature, so normal call syntax
  always resolved to that (unconditional) method instead of this "Try" extension's
  dedup logic. See [Migration Guide](docs/migration-alpha-to-1.0.md) for details.

See the [Migration Guide](docs/migration-alpha-to-1.0.md) for a full list of API
renames between alpha and 1.0, and notes on the two critical bug fixes above.

## [0.x.x-alpha] - Historical

### Added
- Initial alpha releases
- Core middleware pipeline infrastructure
- Hexagonal architecture (ports and adapters) support
- AWS Lambda adapters (API Gateway, SNS, SQS, Kafka, EventBridge)
- Azure adapters (AspNet, Kafka, EventHub)
- Message handling infrastructure
- Dependency injection abstractions
- Health checks
- Diagnostics and logging
- Serialization support (JSON, XML)
- Validation (DataAnnotations, FluentValidation)
- OpenAPI/AsyncAPI schema generation
- Code generation tools
- Testing utilities
- gRPC support
- Cache support (Core, Redis)
- Observability (XRay, Datadog, Zipkin, OpenTelemetry)

---

## Version History Notes

Benzene was in alpha (0.x.x-alpha) during initial development. This CHANGELOG was created retroactively as part of preparing for the 1.0.0 release.

For detailed commit history, see: https://github.com/daniellepelley/Benzene/commits/main
