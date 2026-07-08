# Benzene.Schema.OpenApi

## What this package does
OpenAPI (Swagger) schema generation for Benzene HTTP endpoints. Generates OpenAPI 3.0 specifications from Benzene message handlers and HTTP endpoints, enabling Swagger UI and API documentation.

## Key types/interfaces

### OpenAPI Generation
- OpenAPI 3.0 schema generator
- Endpoint discovery and documentation
- Request/response schema generation
- Swagger/OpenAPI middleware

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
