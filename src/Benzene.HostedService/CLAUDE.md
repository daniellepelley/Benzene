# Benzene.HostedService

## What this package does
IHostedService integration for Benzene. Enables running Benzene applications as background services in ASP.NET Core, processing messages from queues, scheduled tasks, or long-running operations.

## Key types/interfaces

### Hosted Service Integration
- `IHostedService` implementation for Benzene
- Background message processing
- Graceful shutdown support
- Service lifecycle management

## When to use this package
- When processing background messages in ASP.NET Core
- For queue consumers as hosted services
- For scheduled tasks
- For long-running background operations

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions
- **Benzene.Abstractions.Middleware** - Middleware abstractions
- **Benzene.Core.Middleware** - Middleware implementations
- **Microsoft.Extensions.Hosting** - Hosted service abstractions

## Important conventions
- Registered as IHostedService in DI
- Starts with application
- Graceful shutdown on application stop
- Can run multiple hosted services
- Suitable for queue consumers
