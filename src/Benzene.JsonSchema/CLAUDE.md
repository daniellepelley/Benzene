# Benzene.JsonSchema

## What this package does
Validates an incoming request body against a JSON Schema as Benzene pipeline middleware. For each
message it obtains a schema for the current topic (by default, generated from the registered
handler's request type), parses the raw request body, evaluates it against that schema, and
short-circuits with a `ValidationError` result when the body is missing or does not conform.
Schema generation here is a means to request validation only - it is not an API-documentation or
OpenAPI feature (that is `Benzene.Schema.OpenApi`).

## Key types/interfaces
- `JsonSchemaMiddleware<TContext> : IMiddleware<TContext>` (Name `"JsonSchema"`) - resolves the
  schema via `IJsonSchemaProvider<TContext>`; a `null` schema means "no validation" and passes
  through to `next()`. Reads the body via `IMessageBodyGetter<TContext>`; a `null` body, or a body
  that fails `schema.Evaluate(...)` (`OutputFormat.List`), is short-circuited with
  `BenzeneResult.Set(defaultStatuses.ValidationError, false)` via `IMessageHandlerResultSetter<TContext>`.
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
- The failure result is a bare `ValidationError` status with a `false` payload - it does **not**
  return per-property error detail (unlike `Benzene.FluentValidation`/`Benzene.DataAnnotations`).
- No registered handler/request type for the topic ⇒ no schema ⇒ validation is skipped (pass-through),
  not a rejection.
- Generated schemas use camelCase property names and are cached per request type across requests.
