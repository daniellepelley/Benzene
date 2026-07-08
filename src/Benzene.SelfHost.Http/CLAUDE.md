# Benzene.SelfHost.Http

## What this package does
Provides HTTP server capabilities for self-hosted Benzene applications. Enables running HTTP endpoints without ASP.NET Core or IIS. Built on HttpListener for lightweight HTTP hosting in console apps or Windows services.

## Key types/interfaces

### HTTP Self-Hosting
- HTTP listener integration
- HTTP context adapter for HttpListener
- HTTP server lifecycle management

## When to use this package
- When you need HTTP endpoints in console apps
- For integration testing with real HTTP without ASP.NET Core
- For lightweight microservices with minimal dependencies
- When deploying to environments without web server

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.Middleware** - Middleware implementations
- **Benzene.Http** - HTTP abstractions
- **Benzene.SelfHost** - Self-hosting infrastructure

## Important conventions
- Uses System.Net.HttpListener under the hood
- Requires admin privileges for port binding on Windows
- Suitable for development and testing
- Consider ASP.NET Core for production HTTP workloads
- Good for scenarios where IIS/Kestrel is not available
