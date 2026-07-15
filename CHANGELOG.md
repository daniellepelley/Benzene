# Changelog

All notable changes to Benzene will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
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

### Removed
- **BREAKING:** `UseCorrelationId()` (`Benzene.Diagnostics.Correlation`) — the legacy inbound
  correlation-header pickup middleware, previously `[Obsolete]`. Use `UseW3CTraceContext()` for
  cross-service correlation; `ICorrelationId`/`AddCorrelationId()`/`WithCorrelationId()` and the
  outbound `WithCorrelationId()` client decorator remain. See the migration guide.
- **BREAKING:** `WithRequestId()`/`WithApplication()` (`Benzene.Aws.Lambda.Core.LogContextBuilderExtensions`),
  previously `[Obsolete]` — use the portable `UseBenzeneEnrichment()` instead. (The
  transport-agnostic `WithApplication()` in `Benzene.Core.MessageHandlers` is unaffected.)

### Changed
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
