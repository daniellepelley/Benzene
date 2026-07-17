# Changelog

All notable changes to Benzene will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
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
