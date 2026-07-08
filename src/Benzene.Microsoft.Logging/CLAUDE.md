# Benzene.Microsoft.Logging

## What this package does
Microsoft.Extensions.Logging integration for Benzene. Adapts Benzene's logging abstractions to Microsoft's ILogger interface, enabling integration with ASP.NET Core logging, Application Insights, and other Microsoft logging providers.

## Key types/interfaces

### Microsoft Logging Integration
- Adapter from `IBenzeneLogger` to `ILogger`
- Log context integration with log scopes
- Log level mapping
- Structured logging support

## When to use this package
- When using Benzene with ASP.NET Core
- For Application Insights integration
- When you want Microsoft logging providers
- Standard choice for .NET Core/5+ applications

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions (logging)
- **Microsoft.Extensions.Logging** - Microsoft logging abstractions

## Important conventions
- Register in DI container
- Log scopes map to Benzene log context
- Structured logging preserved
- Log levels mapped appropriately
- Works with all ILogger providers
