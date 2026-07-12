# Benzene.Autofac

## What this package does
Autofac DI container integration for Benzene. Adapts Benzene's DI abstractions to Autofac's ContainerBuilder and ILifetimeScope, enabling use of Autofac's advanced features (modules, decorators, interceptors).

## Key types/interfaces

### Autofac Integration
- Adapter from `IBenzeneServiceContainer` to `ContainerBuilder`
- Adapter from `IServiceResolver` to `ILifetimeScope`
- Autofac module support
- Advanced Autofac features

## When to use this package
- When you need Autofac's advanced features
- For applications using Autofac modules
- When you want decorator pattern support
- For complex DI scenarios

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - Core abstractions (DI)
- **Autofac** - Autofac DI container

## Important conventions
- Register Benzene services in `ContainerBuilder`
- Lifetime scopes mapped to Benzene scopes
- Autofac modules work with Benzene
- Decorators and interceptors supported
- `AutofacServiceResolverFactory` registers `NullLoggerFactory`/open-generic `Logger<>` fallbacks
  (via `IfNotRegistered`) so `ILogger<T>` always resolves; register your own `ILoggerFactory`
  instance (e.g. `LoggerFactory.Create(x => x.AddConsole())`) to enable real logging — user
  registrations always win over the fallbacks
