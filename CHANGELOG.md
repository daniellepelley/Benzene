# Changelog

All notable changes to Benzene will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
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

### Fixed
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
