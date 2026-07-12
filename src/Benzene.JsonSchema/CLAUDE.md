# Benzene.JsonSchema

## What this package does
JSON Schema generation and validation for Benzene. Generates JSON schemas from request/response types and validates JSON against schemas. Useful for API documentation and contract validation.

## Key types/interfaces

### JSON Schema
- Schema generation from types
- JSON validation against schemas
- Schema metadata extraction

## When to use this package
- When generating API documentation
- For contract-first API design
- When validating JSON payloads
- For OpenAPI schema generation

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions

## Important conventions
- `DefaultJsonSchemaProvider<TContext>` generates a schema from the request type
  of the handler registered for the current topic (via `JsonSchema.Net.Generation`,
  draft 2020-12), using camelCase property names to match the default serializer
- Returns no schema (skips validation) when the topic has no registered handler
- Generated schemas are cached per request type
- Replace by registering a custom `IJsonSchemaProvider<TContext>`
