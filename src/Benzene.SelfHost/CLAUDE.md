# Benzene.SelfHost

## What this package does
Provides self-hosted application infrastructure for Benzene. Enables running Benzene applications as standalone console apps or Windows services without external web servers. Foundation for testing and lightweight deployments.

## Key types/interfaces

### Self-Hosting Infrastructure
- Self-host application builders
- Standalone application runners
- Console application helpers

## When to use this package
- When running Benzene apps as console applications
- When building Windows services with Benzene
- For integration testing without external dependencies
- For lightweight microservices that don't need full web server

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.Middleware** - Middleware implementations

## Important conventions
- Self-hosted apps use Benzene's DI container directly
- Suitable for message-based workloads (queues, events)
- Can be combined with SelfHost.Http for HTTP endpoints
- Typically used for testing or lightweight deployments
