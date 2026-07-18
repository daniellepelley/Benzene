# Benzene.Testing

## What this package does
In-memory test-host and request-builder helpers for testing Benzene applications from a
platform-neutral `BenzeneStartUp`, without deploying to a cloud host. This package builds the
**configured services + configuration**; the concrete platform host is constructed by a `Build*`
extension supplied by a platform package (e.g. `BuildAwsLambdaHost` in `Benzene.Aws.Lambda.Core`,
`BuildAzureFunctionApp`) via the `Build<THost>(factory)` seam here.

## Key types/interfaces
- `BenzeneTestHost.Create<TStartUp>()` - static entry point returning a `BenzeneTestHostBuilder<TStartUp>`.
- `BenzeneTestHostBuilder<TStartUp>` - `WithServices(Action<IServiceCollection>)` (override real
  dependencies with fakes/mocks after the StartUp's own `ConfigureServices`), `WithConfiguration(...)`
  (layer in-memory config overrides), and `Build<THost>(Func<TStartUp, IServiceCollection, IConfiguration, THost>)`
  which runs the StartUp and hands the result to a platform factory.
- `MessageBuilder<T>` / static `MessageBuilder` + `MessageBuilderExtensions` - build a Benzene message
  (topic + typed body + headers) to drive through a test host.
- `HttpBuilder<T>` / static `HttpBuilder` - build an HTTP-shaped test request.
- The `IMessageBuilder<T>` / `IHttpBuilder<T>` / `IBenzeneTestHost` interfaces these implement live in
  `Benzene.Abstractions`, not this package.

## When to use this package
- Integration-testing a `BenzeneStartUp`'s handlers/middleware in-memory with real DI, mocking only the
  external edges via `WithServices(...)`.

## Dependencies on other Benzene packages
- **Benzene.Abstractions** / **Benzene.Abstractions.Middleware** / **Benzene.Abstractions.Pipelines** -
  the builder interfaces and pipeline/hosting abstractions
- **Benzene.Microsoft.Dependencies** - `BenzeneStartUp`, the MEL container the host builds on

## Important conventions
- `WithServices` overrides run **after** the StartUp's `ConfigureServices`, so last-registration-wins
  replaces the real dependency.
- No external process/network is started - suitable for unit and in-memory integration tests.
