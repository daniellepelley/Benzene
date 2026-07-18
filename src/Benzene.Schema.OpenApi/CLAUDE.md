# Benzene.Schema.OpenApi

## What this package does
Generates a machine-readable spec of a Benzene service's message contract from its registered
handlers, HTTP endpoints, and broadcast/sender message definitions, and serves it from a `spec`
topic. Despite the package name it produces **three** spec formats, selected per request:
- `benzene` - a Benzene-native "EventService" document (topics, request/response payloads, broadcast
  events, HTTP mappings, validation rules, generated example payloads). This is the **default** and
  the format `Benzene.Spec.Ui` renders.
- `openapi` - OpenAPI 3.0 (via `Microsoft.OpenApi`), for HTTP endpoints.
- `asyncapi` - AsyncAPI 2.0 (via `AsyncAPI.NET` / `LEGO.AsyncAPI`), for topic/event channels.

Each format can be emitted as JSON or YAML. The package also includes a backward-compatibility gate
for the `benzene` contract.

## Key types/interfaces

### Serving the spec
- `Extensions.UseSpec<TContext>()` / `UseSpec<TContext>(string topic)` - registers `SpecMessageHandler`
  on the `spec` topic (`Constants.DefaultSpecTopic`).
- `SpecMessageHandler : IMessageHandler<SpecRequest, RawStringMessage>` - builds the spec and returns
  it as a raw string.
- `SpecRequest { string Type; string Format }` - `Type` selects `benzene`/`openapi`/`asyncapi`
  (unknown/empty ⇒ `benzene`); `Format` selects `yaml` vs `json` (default `json`).
- `SpecBuilder` - dispatches a `SpecRequest` to the right document builder and calls
  `GenerateJson()`/`GenerateYaml()`.

### Document builders (one per format)
- `OpenApi/OpenApiDocumentBuilder` - OpenAPI 3.0 (`SerializeAsJson/Yaml(OpenApiSpecVersion.OpenApi3_0)`).
  Builds paths/operations from HTTP endpoint definitions, request/response schemas, and a fixed set of
  error responses (400/401/403/404/422/500/503) whose bodies reference `ErrorPayload`.
- `AsyncApi/AsyncApiDocumentBuilder` - AsyncAPI 2.0 (`AsyncApiVersion.AsyncApi2_0`); channels per topic
  plus `:benzeneResult` response channels, broadcast events, and message-sender definitions.
- `EventService/EventServiceDocumentBuilder` - the `benzene` `EventServiceDocument`; requests, events,
  HTTP mappings, an optional top-level `messageEndpoint`, and deterministic generated example payloads.
- Builders implement the "consumer" seam interfaces in `Abstractions/` -
  `IConsumesApplicationInfo`, `IConsumesMessageHandlerDefinitions`, `IConsumesHttpEndpointDefinitions`,
  `IConsumesBroadcastEventsDefinitions`, `IConsumesMessageSenderDefinitions`,
  `IConsumesMessageEndpoint` - and `SpecBuilder` feeds each builder only the definitions it consumes,
  resolved from DI (`IApplicationInfo`, `IMessageHandlersFinder`, `IHttpEndpointFinder`,
  `IMessageDefinitionFinder`, `IBenzeneMessageHttpEndpointInfo`). Output flows through `IProducesJson`/
  `IProducesYaml`.

### Schema building
- `ISchemaBuilder` / `SchemaBuilder` - generates `OpenApiSchema`s from CLR types using Swashbuckle's
  `SchemaGenerator` + a `System.Text.Json` contract resolver, and collects them into a components
  catalogue.
- `OpenApiValidationSchemaBuilder : ISchemaBuilder` - decorates generated schemas with validation
  constraints pulled from a registered `IValidationSchemaBuilder` (e.g. `Benzene.FluentValidation`'s):
  `minLength`/`maxLength`, `pattern`, `enum` (IsOneOf), `required` (NotEmpty/NotNull), and `uuid`/
  `email` formats. `SpecBuilder` uses this automatically when an `IValidationSchemaBuilder` is registered.
- `JsonOpenApiSchemaBuilder` - builds an `OpenApiSchema` set from a sample JSON document.
- `OpenApiSchemaComparer` - structural diff between two `OpenApiSchema`s (type/format/properties).

### Backward-compatibility gate (`Compatibility/`)
- `SchemaCompatibility` - `Compare(baseline, current)` and `EnsureBackwardCompatible(...)` (throws
  `SchemaCompatibilityException` on breaking changes; JSON-string overloads deserialize a committed
  baseline). Supporting types: `SchemaCompatibilityComparer`, `SchemaCompatibilityReport`,
  `SchemaCompatibilityRules`, `SchemaChange`/`SchemaChangeKind`, `ChangeCompatibility`,
  `SchemaDirection`. Drop `EnsureBackwardCompatible` into a test to fail CI when the `benzene`
  contract stops being backward compatible with a baseline `spec.json`.

### Example payload generation (`Examples/`)
- `IExamplePayloadBuilder` / `ExamplePayloadBuilder` — builds a deterministic example payload
  (camelCased property-name → value dictionary) from an `OpenApiSchema`. Honours, in precedence
  order: caller-supplied known values (keyed by property path `order.customer.email`, falling back
  to bare name), the schema's own `example` → `default` → first `enum` value, then a fixed value
  per type/format (`uuid`, `date-time`, `date`, `email`, `uri`), sized/clamped into
  `minLength`/`maxLength`/`minimum`/`maximum` so generated examples pass the validation rules the
  spec advertises. `pattern` is not reverse-generated. Reference cycles terminate as `{}`/`[]`
  (ancestry-tracked, max reference depth 8). Deterministic by design — no randomness — so spec
  output and golden-file tests stay stable.
- `ISchemaGetter` / `SchemaGetter` — resolves `$ref` schemas against a schema catalogue.
- `OpenApiAnyConverter` — converts between plain .NET values and `IOpenApiAny` trees (used to
  honour schema examples and to embed generated examples into spec documents).
- These types moved here from `Benzene.CodeGen.Core` (which now consumes them) so examples can be
  generated at runtime during spec builds, not just at codegen time. Tests:
  `test/Benzene.Core.Test/Autogen/Schema/OpenApi/Examples/ExamplePayloadBuilderTest.cs`
  (golden files alongside).

## When to use this package
- To expose a live, always-accurate spec of a service's topics/endpoints from its own handler
  registrations (`UseSpec`), consumed by `Benzene.Spec.Ui`, Swagger UI (openapi), or AsyncAPI tooling.
- To gate CI on backward-compatibility of the message contract (`SchemaCompatibility`).

## Dependencies on other Benzene packages
- **Benzene.Abstractions.Pipelines** / **Benzene.Core.MessageHandlers** / **Benzene.Core.Messages** /
  **Benzene.Core** - message-handler definitions, finders, `RawStringMessage`, pipeline seams.
- **Benzene.Abstractions.Validation** - `IValidationSchemaBuilder`/`IValidationSchema`/
  `ValidationConstants`, consumed by `OpenApiValidationSchemaBuilder`.
- **Benzene.Http** - HTTP endpoint definitions/routing and the BenzeneMessage endpoint info.
- **Benzene.Results** - `BenzeneResult`, `ErrorPayload`.
- NuGet: **Microsoft.OpenApi**(+**.Readers**), **AsyncAPI.NET**, **Swashbuckle.AspNetCore.SwaggerGen**
  (schema generation only - no Swagger UI is bundled), **Newtonsoft.Json**.
- Note: this package does **not** depend on `Benzene.JsonSchema`; schema generation is done via
  Swashbuckle's `SchemaGenerator`, not that package.

## Important conventions
- The `spec` topic is served like any other message handler; the default format is `benzene`, output
  is JSON unless `Format` is `yaml`.
- **Reserved utility topics** (`ReservedTopics`): the Cloud Service Profile's operational topics
  (`spec`/`healthcheck`/`liveness`/`readiness`/`mesh`/`invoke`/`report`) are flagged on each
  `benzene`-spec `RequestResponse` as `reserved: true` (emitted only when true, read back via
  `JsonProperty`). Consumers (`Benzene.Spec.Ui`, `Benzene.Mesh.Aggregator`) use it to separate a
  service's domain topics from its Benzene utility endpoints. Matched by default topic id — a
  renamed reserved topic isn't auto-flagged (it's a presentation aid, not a security boundary).
- Schemas are generated from handler request/response types and keyed by type name; validation
  constraints are merged in only when an `IValidationSchemaBuilder` is registered.
- OpenAPI output is version 3.0; AsyncAPI output is 2.0.

## Tests
- Example payload generation: `test/Benzene.Core.Test/Autogen/Schema/OpenApi/Examples/ExamplePayloadBuilderTest.cs`
  (with golden files).
