# Bring Your Own DI Container

Use a dependency-injection container other than the two Benzene ships adapters for
(Microsoft.Extensions.DependencyInjection and Autofac) — **without any Benzene-specific package**.

## Problem statement

Your team standardises on a particular DI container — Lamar, DryIoc, Grace, LightInject, Castle
Windsor, Simple Injector — and you want Benzene's handlers and middleware to register and resolve
through *that* container, not Microsoft's built-in one.

You don't need a `Benzene.<Container>` adapter for this. Benzene resolves services through your
**host's `IServiceProvider`**, and every one of those containers ships a
Microsoft.Extensions.DependencyInjection (MS DI) integration that *is* an `IServiceProvider`. Plug the
container into your host the standard .NET way and Benzene uses it unchanged.

## How it works

Benzene's `MicrosoftServiceResolverFactory` has a constructor that takes an **already-built
`IServiceProvider`** and does not own it. The host transports build it from the application's provider:

- ASP.NET Core / self-host: `new MicrosoftServiceResolverFactory(app.ApplicationServices)`
- Azure Functions (isolated worker): `new MicrosoftServiceResolverFactory(serviceProvider)`

So when you replace the host's provider factory with your container's — the standard
`IHostBuilder.UseServiceProviderFactory(...)` hook — `ApplicationServices` *is* your container's
provider, and Benzene resolves every service through it. No Benzene code changes; no new package.

```
 your container  ──MS DI integration──▶  IServiceProvider  ──▶  Benzene (MicrosoftServiceResolverFactory)
 (Lamar / DryIoc / …)                    (the host's provider)      resolves handlers + middleware here
```

## Prerequisites

- A Benzene app wired the normal MS DI way — `services.UsingBenzene(x => x.AddBenzene()…)` in your
  `ConfigureServices` (see [Hosting](../hosting.md)).
- The NuGet MS-DI integration package for your container (e.g. `Lamar.Microsoft.DependencyInjection`,
  `DryIoc.Microsoft.DependencyInjection`, `Autofac.Extensions.DependencyInjection`).

## Step-by-step (ASP.NET Core host)

Registration is unchanged — you still call `UsingBenzene`/`AddBenzene` against `IServiceCollection`.
The only new line swaps the provider factory on the host.

### Lamar

Lamar implements MS DI natively, so it's the cleanest drop-in:

```csharp
using Lamar.Microsoft.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseLamar();          // ← Lamar builds the IServiceProvider from now on

builder.UseBenzene<StartUp>();    // your usual Benzene startup; ConfigureServices still uses IServiceCollection

var app = builder.Build();
app.UseBenzene();
app.Run();
```

### DryIoc

```csharp
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;

builder.Host.UseServiceProviderFactory(
    // WithMicrosoftDependencyInjectionRules is required — see "Container requirements" below.
    new DryIocServiceProviderFactory(new Container(rules => rules.WithMicrosoftDependencyInjectionRules())));
```

### Autofac (via its MS DI integration)

```csharp
using Autofac.Extensions.DependencyInjection;

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
```

> Autofac is the one container Benzene *also* ships a **native** adapter for
> (`Benzene.Autofac`). Prefer the native adapter only if you configure Autofac through its own
> `ContainerBuilder`/modules; if you register everything through `IServiceCollection`, the provider
> path above is simpler.

That's the whole recipe. Because `ConfigureServices` still populates an `IServiceCollection`, all your
existing Benzene registrations (`AddBenzene`, `AddMessageHandlers`, `UseHttp`, clients, health checks)
are untouched.

## Container requirements

Benzene registers services the way MS DI behaves, so your container must apply the same two semantics.
Every container's *MS DI integration* applies them by design (that's what "MS DI conformance" means) —
this only bites if you hand-build a raw container:

1. **Last-registration wins** for a single resolve. Benzene re-registers services to override defaults
   (`AddSingleton`, `TryAdd`-then-override). A container that instead throws on multiple default
   registrations will fail at startup.
2. **Greediest resolvable constructor.** Benzene has types with more than one public constructor (e.g.
   its `JsonSerializer`: `.ctor()` and `.ctor(JsonSerializerOptions)`). The container must pick the
   constructor whose arguments it can satisfy, as MS DI and Autofac do.

For DryIoc specifically, both come from `WithMicrosoftDependencyInjectionRules()` (shown above) — a raw
`new Container()` has neither and will throw `Error.UnableToSelectSinglePublicConstructorFromMultiple`.

## Testing

Prove it end-to-end by building the provider from your container and running a message through a real
Benzene app — no host required:

```csharp
var services = new ServiceCollection();
services.UsingBenzene(x =>
{
    x.AddBenzene();
    x.AddMessageHandlers(typeof(MyHandler).Assembly);
});

// Build the IServiceProvider with your container instead of Microsoft's:
var container = new Container(rules => rules.WithMicrosoftDependencyInjectionRules());
IServiceProvider provider = container.WithDependencyInjectionAdapter(services);

// Drive Benzene through it via the existing factory — no Benzene changes:
using var factory = new MicrosoftServiceResolverFactory(provider);
var response = await app.HandleAsync(request, factory);
```

## Troubleshooting

- **`Error.UnableToSelectSinglePublicConstructorFromMultiple` (or similar "ambiguous constructor").**
  Your container isn't applying greedy-constructor selection. Use its MS DI rules
  (`WithMicrosoftDependencyInjectionRules()` for DryIoc); don't hand-build a raw container.
- **"Multiple default registrations" / "expected single factory".** Your container isn't applying
  last-registration-wins. Same fix — use the MS DI rules/integration, not a raw container.
- **`error CS0118: 'Example' is a namespace but is used like a type` (DryIoc only).** `DryIoc.dll`
  ships a stray public top-level `Example` namespace (leftover sample types). It only collides if your
  own code has a type named `Example` that you import via a `using` and use unqualified — a type named
  `Example` in the *same* namespace as its use is unaffected, and code with no `Example` type is
  unaffected. If you hit it, either fully-qualify your `Example` type or add
  `<PackageReference Include="DryIoc.dll" … Aliases="dryioc" />` to contain DryIoc's namespaces.

## Variations

- **Simple Injector** deliberately restricts some patterns Benzene relies on (resolving unregistered
  concrete types, certain constructor/lifestyle rules). Use its "cross-wire" MS DI integration and
  expect to loosen its verification for the Benzene-owned registrations, rather than treating it as a
  drop-in.
- **A native adapter** is only worth it when you configure the container through its *own* builder API
  (like Autofac modules). For everything registered through `IServiceCollection`, the provider path on
  this page is the supported route — that's why Benzene ships adapters only for MS DI and Autofac.

## Further reading

- [Hosting](../hosting.md) — how `UseBenzene`/`ConfigureServices` wire the pipeline
- `Benzene.Abstractions.DI` — `IServiceResolver`/`IServiceResolverFactory`/`IBenzeneServiceContainer`,
  the seams an adapter implements
- `Benzene.Autofac` — the reference native adapter, if you need one for your container
