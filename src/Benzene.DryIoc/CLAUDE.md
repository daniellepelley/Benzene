# Benzene.DryIoc

## What this package does
Adapts Benzene's DI abstractions onto [DryIoc](https://github.com/dadhi/DryIoc). It is a **thin adapter**
in the same shape as `Benzene.Autofac`: `UsingBenzene(...)` on a DryIoc `IContainer` gives Benzene an
`IBenzeneServiceContainer` view over it, so Benzene's registrations land in your DryIoc container and
Benzene resolves services through DryIoc scopes.

## Key types/interfaces
- `Extensions.CreateContainer()` — returns a DryIoc `Container` configured the way Benzene expects (see
  "Container rules" below). Prefer it over `new Container()` when wiring Benzene.
- `Extensions.UsingBenzene(this IContainer)` / `UsingBenzene(this IContainer, Action<IBenzeneServiceContainer>)` -
  the entry point (registers `IBenzeneServiceContainer`/`IServiceResolver` self-services, like the Autofac adapter).
- `DryIocBenzeneServiceContainer : IBenzeneServiceContainer` - maps Benzene lifetimes to DryIoc `Reuse`:
  `AddScoped` → `Reuse.Scoped`, `AddSingleton` → `Reuse.Singleton`, `AddTransient` → `Reuse.Transient`.
  Open generics need no special case — DryIoc's `Register(Type, Type)` accepts an open generic
  implementation directly (unlike Autofac's separate `RegisterGeneric`). `AddSingleton(instance)` uses
  `RegisterInstance`; scoped/transient instance and factory-func registrations use `RegisterDelegate`.
- `DryIocServiceResolverAdapter : IServiceResolver` - resolves from a DryIoc `IResolverContext`
  (container or opened scope). `GetService<T>` uses `Resolve<T>()`; `TryGetService<T>` uses
  `Resolve<T>(IfUnresolved.ReturnDefault)` (null on unregistered, no throw); `GetServices<T>` uses
  `Resolve<IEnumerable<T>>()`. On a failed `GetService<T>()` it wraps DryIoc's exception in a
  `BenzeneException` enriched with a missing-registration hint via
  `RegistrationErrorHandler.Describe(typeof(T), ex)` — keyed on the requested type, throw-safe, original
  error preserved as `InnerException`. Shared diagnostic logic: `Benzene.Core.DI.RegistrationCheck`.
- `DryIocServiceResolverFactory : IServiceResolverFactory` - opens a DryIoc scope (`OpenScope()`) per
  Benzene scope, and registers `NullLoggerFactory`/open-generic `Logger<>` fallbacks (only when the
  consumer hasn't registered their own) so `ILogger<T>`/`ILoggerFactory` always resolve — same policy as
  the Autofac adapter.

## Container rules (important)
A Benzene container needs two DryIoc rules that MS DI/Autofac apply implicitly but DryIoc does not by
default — `Extensions.CreateContainer()` sets both; a bring-your-own `IContainer` must apply the same
(or DryIoc's `WithMicrosoftDependencyInjectionRules()`, a superset):
- **Last-registration wins** (`rules.WithFactorySelector(Rules.SelectLastRegisteredFactory())`) — Benzene
  re-registers services to override defaults (`AddSingleton`, `TryAdd`-then-override); DryIoc otherwise
  throws on multiple non-keyed defaults for a single resolve.
- **Greediest resolvable constructor** (`rules.With(FactoryMethod.ConstructorWithResolvableArguments)`) —
  Benzene registers types with more than one public constructor (e.g. `Benzene.Core.MessageHandlers`'s
  `JsonSerializer`: `.ctor()` and `.ctor(JsonSerializerOptions)`); DryIoc otherwise refuses to pick one
  and throws `UnableToSelectSinglePublicConstructorFromMultiple`.

## When to use this package
- When your application already uses DryIoc and you want Benzene to register/resolve through it.
- If you only want a fast third-party container behind Benzene without its native API, you can instead
  keep the default `Benzene.Microsoft.Dependencies` adapter and back it with DryIoc's MS DI integration
  (`DryIoc.Microsoft.DependencyInjection`) — Benzene resolves through the resulting `IServiceProvider`
  unchanged (see the BYO-provider test in `test/Benzene.Core.Test/Core/Core/DI`).

## Deliberate boundaries
- This package adds **no** DryIoc-specific decorators/interceptors of its own. `UsingBenzene` operates on
  your real `IContainer`, so you keep full access to DryIoc's native features and use them directly.

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - the DI abstractions (`IBenzeneServiceContainer`, `IServiceResolver`, factories)
- **Benzene.Core** - `RegistrationCheck` (shared resolve-failure diagnostics)
- **DryIoc.dll** (5.4.3)

## Status
Prototype (see `work/` DI-container investigation). Mirrors the `Benzene.Autofac` adapter's structure
and test shape; covered by the DryIoc cases in `test/Benzene.Core.Test/Core/Core/DI/*`.
