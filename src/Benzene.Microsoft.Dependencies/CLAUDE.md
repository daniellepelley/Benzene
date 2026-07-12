# Benzene.Microsoft.Dependencies

## What this package does
Microsoft.Extensions.DependencyInjection integration for Benzene. Adapts Benzene's DI abstractions to Microsoft's IServiceCollection and IServiceProvider, enabling use of Microsoft's built-in DI container.

## Key types/interfaces

### Microsoft DI Integration
- Adapter from `IBenzeneServiceContainer` to `IServiceCollection`
- Adapter from `IServiceResolver` to `IServiceProvider`
- Service lifetime mapping
- DI registration extensions

## When to use this package
- When using Benzene with ASP.NET Core
- For .NET Core/5+ applications
- When you want Microsoft's built-in DI container
- Standard choice for modern .NET applications

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions (DI)
- **Microsoft.Extensions.DependencyInjection** - Microsoft DI

## Important conventions
- Register Benzene services in `IServiceCollection`
- Scoped, Singleton, Transient lifetimes mapped
- Works seamlessly with ASP.NET Core DI
- No additional DI container needed
- `UsingBenzene` calls `services.AddLogging()` so `ILogger<T>`/`ILoggerFactory` always resolve;
  host logging configuration (before or after `UsingBenzene`) is respected because MEL's
  registration is TryAdd-based and composable
