# Benzene.Autofac

## What this package does
Adapts Benzene's DI abstractions onto Autofac. It is a **thin adapter**: `UsingBenzene(...)` on your own
Autofac `ContainerBuilder` gives Benzene an `IBenzeneServiceContainer` view over it, so Benzene's
registrations land in your Autofac container and Benzene resolves services through Autofac's
`ILifetimeScope`.

## Key types/interfaces
- `Extensions.UsingBenzene(this ContainerBuilder)` / `UsingBenzene(this ContainerBuilder, Action<IBenzeneServiceContainer>)` -
  the entry point.
- `AutofacBenzeneServiceContainer : IBenzeneServiceContainer` - maps Benzene lifetimes to Autofac:
  `AddScoped` → `InstancePerLifetimeScope`, `AddSingleton` → `SingleInstance`, `AddTransient` →
  `InstancePerDependency` (open generics via `RegisterGeneric`).
- `AutofacServiceResolverAdapter : IServiceResolver` - resolves from an Autofac `IComponentContext`/scope.
- `AutofacServiceResolverFactory : IServiceResolverFactory` - calls `containerBuilder.Build()` and opens a
  lifetime scope per Benzene scope.

## When to use this package
- When your application already uses Autofac and you want Benzene to register/resolve through it.

## Deliberate boundaries
- This package adds **no** Autofac module/decorator/interceptor wrappers of its own. Because
  `UsingBenzene` operates on your real `ContainerBuilder`, you keep full access to Autofac's native
  features (modules, decorators, interceptors) and use them directly — Benzene neither hides nor
  re-exposes them.

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - the DI abstractions (`IBenzeneServiceContainer`, `IServiceResolver`, factories)
- **Benzene.Core**
- **Autofac** (6.5.0)

## Important conventions
- Benzene `AddScoped`/`AddSingleton`/`AddTransient` map to Autofac lifetimes as above.
- `AutofacServiceResolverFactory` registers `NullLoggerFactory`/open-generic `Logger<>` fallbacks
  (via `IfNotRegistered`) so `ILogger<T>` always resolves; register your own `ILoggerFactory`
  instance (e.g. `LoggerFactory.Create(x => x.AddConsole())`) to enable real logging — user
  registrations always win over the fallbacks.
