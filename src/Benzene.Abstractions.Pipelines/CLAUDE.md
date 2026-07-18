# Benzene.Abstractions.Pipelines

## What this package does
Hosting- and pipeline-level abstractions that sit above the raw middleware: the platform-neutral
application-builder/startup seam a host uses to wire a Benzene app, the per-invocation context that
lets a portable handler reach native platform features, the long-running worker contract, and a few
result/message-shape markers. (Note: the assembly is `Benzene.Abstractions.Pipelines`, but the types
live in `Benzene.Abstractions.Hosting`, `Benzene.Abstractions.Clients`, and
`Benzene.Abstractions.Results` namespaces, not a `.Pipelines` namespace.)

## Key types/interfaces

### Hosting (`Benzene.Abstractions.Hosting`)
- `IBenzeneApplicationBuilder : IRegisterDependency` - platform-neutral app builder: exposes the
  `Platform` id and `Create<TContext>()` to make a middleware pipeline builder.
- `IStartUp<TContainer, TConfiguration, TAppBuilder>` - the startup contract:
  `GetConfiguration()`, `ConfigureServices(...)`, `Configure(...)`.
- `IBenzeneInvocation` - per-invocation context: `InvocationId`, `Platform`, and
  `GetFeature<T>()` to reach a native host feature (e.g. `ILambdaContext`, `HttpContext`) while
  handlers stay portable. Populated per pipeline by the host's `UseBenzeneInvocation()`; does not
  auto-flow into a nested DI scope.
- `IBenzeneInvocationAccessor` - scoped mutable holder host middleware uses to publish the
  `IBenzeneInvocation`; application code should depend on `IBenzeneInvocation` directly.
- `IBenzeneWorker` - long-running worker contract: `StartAsync` / `StopAsync`.

### Clients (`Benzene.Abstractions.Clients`)
- `IClientHeaders` - outbound header bag: `Set(key, value)` / `Get()`.

### Result/message markers (`Benzene.Abstractions.Results`)
- `IRawJsonMessage` (`Json`) / `IBase64JsonMessage` (`Base64Json`) - payload-shape markers used by
  renderers to emit pre-formed JSON or base64-armored JSON.

## When to use this package
- When implementing a hosting platform adapter (a new `IBenzeneApplicationBuilder` / startup / worker).
- When a handler needs to reach a native platform feature portably via `IBenzeneInvocation`.
- Rarely referenced directly by ordinary application code.

## Dependencies on other Benzene packages
- **Benzene.Abstractions** - core abstractions (`IRegisterDependency`, results).
- **Benzene.Abstractions.Middleware** - `IMiddlewarePipelineBuilder<TContext>`.

## Important conventions
- The startup/app-builder seam is platform-neutral so the same app wires onto Lambda, ASP.NET Core,
  or a self-hosted worker.
- `IBenzeneInvocation` is populated per-pipeline, not globally — add `UseBenzeneInvocation()` to any
  inner sub-pipeline that also needs it.

## Tests
Interfaces only; exercised through the hosting adapters' tests in `test/`.
