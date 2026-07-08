# Benzene.AspNet.Core

## What this package does
Integrates Benzene with ASP.NET Core. Provides middleware for processing HTTP requests through Benzene pipelines, adapters for ASP.NET Core HttpContext, and configuration extensions for integrating with ASP.NET Core's IApplicationBuilder and IServiceCollection.

## Key types/interfaces

### Middleware & Application
- ASP.NET Core middleware for Benzene request processing
- Integration with ASP.NET Core request pipeline
- HttpContext adapter for Benzene's IHttpContext

### Configuration
- Extensions for `IApplicationBuilder` - adds Benzene middleware
- Extensions for `IServiceCollection` - registers Benzene services
- Integration with ASP.NET Core DI container

## When to use this package
- When building ASP.NET Core web applications with Benzene
- When you want Benzene's hexagonal architecture in ASP.NET Core
- When migrating existing ASP.NET Core apps to Benzene
- For web APIs that need middleware-based request processing

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.Middleware** - Middleware implementations
- **Benzene.Http** - HTTP abstractions
- **Microsoft.AspNetCore.Http** - ASP.NET Core integration

## Important conventions
- Register Benzene services in `ConfigureServices` or `Program.cs`
- Add Benzene middleware in ASP.NET Core pipeline
- Middleware should be added after routing but before endpoints
- HttpContext is adapted to Benzene's IHttpContext
- Uses ASP.NET Core's built-in DI container
- Async/await throughout for ASP.NET Core compatibility
