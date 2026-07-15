# Benzene.GoogleCloud.Functions.Core

## What this package does
Foundation package for Benzene's Google Cloud Functions integration - Phase 0 of
`work/google-cloud-roadmap-1.0.md`. Provides the bootstrap steps every Google Cloud Functions
trigger-type package needs to run a platform-neutral `BenzeneStartUp` (constructing it, resolving
its configuration, preparing the `IServiceCollection`/`IBenzeneServiceContainer` pair its
`ConfigureServices`/`Configure` lifecycle runs against). Mirrors `Benzene.Aws.Lambda.Core`'s role as
a thin shared foundation - not a transport adapter itself.

## Key types/interfaces
- `GoogleCloudStartUpRunner.Bootstrap<TStartUp>()` - constructs `TStartUp`, calls
  `GetConfiguration()`, builds a `ServiceCollection` (with `.AddLogging()`) and a
  `MicrosoftBenzeneServiceContainer` wrapping it, returning all four. Every trigger-type package
  (`Benzene.GoogleCloud.Functions.Http` today; a future `Benzene.GoogleCloud.Functions.PubSub`)
  calls this once at cold start, then runs `startUp.ConfigureServices(services, configuration)` and
  `startUp.Configure(<its own IBenzeneApplicationBuilder>, configuration)` itself - this package
  doesn't own `Configure`'s application-builder shape, since that's trigger-type-specific (HTTP vs.
  CloudEvent).

## When to use this package
- Not directly - consumed by `Benzene.GoogleCloud.Functions.Http` and any future Google Cloud
  Functions trigger-type package.

## Dependencies on other Benzene packages
- **Benzene.Abstractions.Pipelines** - `IBenzeneApplicationBuilder` (referenced transitively by
  callers' `Configure` signature; this package itself only needs `IBenzeneServiceContainer`).
- **Benzene.Microsoft.Dependencies** - `BenzeneStartUp`, `MicrosoftBenzeneServiceContainer`.

## Important conventions
- Deliberately has **no** Google-specific NuGet dependency at all (no
  `Google.Cloud.Functions.Framework`) - it's Google-neutral bootstrap plumbing, matching
  `Benzene.Aws.Lambda.Core`'s minimal-dependency posture. The Functions Framework dependency lives
  in `Benzene.GoogleCloud.Functions.Http` (and will live in any future `.Functions.PubSub`), not
  here.
