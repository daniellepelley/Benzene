# Benzene.Client.Http

## What this package does
HTTP client implementation for calling Benzene services. Provides HttpClient-based implementation of Benzene client abstractions, enabling type-safe HTTP calls to Benzene endpoints.

## Key types/interfaces

### HTTP Client
- HTTP implementation of Benzene clients
- HttpClient integration
- Request/response serialization
- URL routing and path parameters

## When to use this package
- When calling Benzene HTTP services
- For microservice-to-microservice communication
- When building API clients
- For type-safe HTTP requests

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Clients** - Client abstractions
- **Benzene.Http** - HTTP abstractions

## Important conventions
- Uses HttpClient from DI
- Automatic serialization/deserialization
- Base URL configuration
- Header propagation supported
- Follows Benzene routing conventions
