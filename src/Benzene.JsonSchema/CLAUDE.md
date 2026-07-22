# Benzene.JsonSchema

## What this package does
Validates an incoming request body against a JSON Schema as Benzene pipeline middleware — the
JSON-Schema member of the validation-library family (`Benzene.FluentValidation`,
`Benzene.DataAnnotations`), sharing their failure contract. For each message it obtains a schema
for the current topic (by default generated from the registered handler's request type; supply
hand-authored documents via `SuppliedJsonSchemaCatalog`), parses the raw request body, evaluates
it against that schema, and short-circuits with a `ValidationError` result carrying an array of
property-scoped messages when the body is missing, malformed, or non-conforming.
Schema generation here is a means to request validation only - it is not an API-documentation or
OpenAPI feature (that is `Benzene.Schema.OpenApi`).

## Key types/interfaces
- `JsonSchemaMiddleware<TContext> : IMiddleware<TContext>` (Name `"JsonSchema"`) - resolves the
  schema via `IJsonSchemaProvider<TContext>`; a `null` schema means "no validation" and passes
  through to `next()`. Reads the body via `IMessageBodyGetter<TContext>`; a `null`/malformed body,
  or one that fails `schema.Evaluate(...)` (`OutputFormat.List`), short-circuits with
  `BenzeneResult.Set(defaultStatuses.ValidationError, errors)` via
  `IMessageHandlerResultSetter<TContext>` - the messages travel as the result's **errors** (same
  as FluentValidation/DataAnnotations), so the response body is the serialized `ErrorPayload`
  (`{ status, errors }`). The topic's handler definition is attached to the result so the response
  payload mapper writes that body (it skips definition-less results).
- `JsonSchemaValidationErrors` - flattens a failed evaluation into one message per failed keyword,
  prefixed with the failing value's JSON Pointer (`"/name: Value is longer than 5 characters"`;
  root-level failures unprefixed); also the `MissingBody`/`MalformedBody` message constants.
- `IJsonSchemaProvider<TContext>` - `Json.Schema.JsonSchema? Get(TContext context)`. Return `null`
  to skip validation for a message.
- `DefaultJsonSchemaProvider<TContext>` - the default implementation. Resolves the topic
  (`IMessageTopicGetter<TContext>`), finds its handler (`IMessageHandlerDefinitionLookUp`), and
  generates a schema from the handler's `RequestType` using `JsonSchema.Net.Generation`
  (`JsonSchemaBuilder().FromType(...)`, draft 2020-12) with camelCase property names to match the
  default serializer. Returns `null` when the topic is empty or has no registered handler/request
  type. Generated schemas are cached per request type.
- `Extensions.UseJsonSchema<TContext>()` - pipeline entry point; registers dependencies and adds the
  middleware.
- `DependencyInjectionExtensions.AddJsonSchema()` - registers `DefaultJsonSchemaProvider<>`
  (`TryAddScoped`, so a provider registered in ConfigureServices - a user's own or the supplied
  one below - wins over the default) and `JsonSchemaMiddleware<>` (scoped).
- `SuppliedJsonSchemaCatalog` / `SuppliedJsonSchemaProvider<TContext>`
  (+ `AddSuppliedJsonSchemas`) - bring-your-own schemas for validation: topics whose request type
  is mapped in the catalog validate against the hand-authored schema; everything else falls back
  to the default generated one. Feed it from the same documents as `Benzene.Schema.OpenApi`'s
  `SuppliedSchemaCatalog` to keep the published contract and runtime validation aligned - see
  `work/complex-payloads-byo-schema-plan.md`. Tests: `SuppliedJsonSchemaProviderTest`.

## When to use this package
- To reject non-conforming request bodies early with a JSON Schema check, without hand-writing
  validators.
- When you want schema-based validation derived automatically from your handler request types.
- Replace `DefaultJsonSchemaProvider<TContext>` with your own `IJsonSchemaProvider<TContext>` to
  supply hand-authored or externally-sourced schemas.

## Dependencies on other Benzene packages
- **Benzene.Abstractions.Pipelines** - pipeline seams.
- **Benzene.Core.MessageHandlers** - `IMessageBodyGetter<>`, `IMessageTopicGetter<>`,
  `IMessageHandlerDefinitionLookUp`, `IMessageHandlerResultSetter<>`, `IDefaultStatuses`,
  `MessageHandlerResult`.
- **Benzene.Core.Middleware** - `IMiddlewarePipelineBuilder<TContext>`, `Use<TContext,TMiddleware>()`.
- **JsonSchema.Net** (NuGet, 9.2.2) - schema model and evaluation.
- **JsonSchema.Net.Generation** (NuGet, 7.3.10) - CLR-type → schema generation.

## Important conventions
- The failure result matches the other validation libraries: `ValidationError` status with the
  property-scoped messages as the result's errors, serialized in the response as `ErrorPayload`.
  (Until 2026-07 this was a bare `false` payload with no detail - flagged as a behavior change,
  along with `JsonSchemaMiddleware`'s constructor gaining the topic getter + definition lookup.)
- Validation runs on the **raw wire body**, before deserialization - unlike FluentValidation/
  DataAnnotations (which validate the mapped request object in the handler pipeline), so it also
  catches malformed and missing bodies.
- No registered handler/request type for the topic ⇒ no schema ⇒ validation is skipped (pass-through),
  not a rejection.
- Generated schemas use camelCase property names and are cached per request type across requests.
- `docs/README.md` is the packaged NuGet readme, mirroring the other validation packages.
