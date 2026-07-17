# Benzene.Schema.OpenApi

## What this package does
OpenAPI (Swagger) schema generation for Benzene HTTP endpoints. Generates OpenAPI 3.0 specifications from Benzene message handlers and HTTP endpoints, enabling Swagger UI and API documentation.

## Key types/interfaces

### OpenAPI Generation
- OpenAPI 3.0 schema generator
- Endpoint discovery and documentation
- Request/response schema generation
- Swagger/OpenAPI middleware

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
- When generating OpenAPI documentation
- For Swagger UI integration
- For API documentation
- For contract-first development

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Http** - HTTP abstractions
- **Benzene.JsonSchema** - JSON schema generation

## Important conventions
- Discovers HTTP endpoints automatically
- Generates request/response schemas
- Supports OpenAPI 3.0
- Integrates with Swagger UI
- Includes authentication schemes
