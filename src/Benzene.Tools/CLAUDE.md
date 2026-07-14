# Benzene.Tools

## What this package does
AWS Lambda-specific test-host helpers, built on top of `Benzene.Testing`'s
`IBenzeneTestHost`/`BenzeneTestHost` infrastructure. This is **not** a CLI or code
generation tool — it's a library referenced from test projects, providing an AWS
Lambda entry-point wrapper that Lambda stream events can be sent into.

## Key types/interfaces

### AWS test host (`Aws/`)
- `AwsLambdaBenzeneTestHost : IBenzeneTestHost, IDisposable` - wraps an
  `IAwsLambdaEntryPoint` (typically built via `Benzene.Testing`'s
  `BenzeneTestHost.Create<TStartUp>().BuildAwsLambdaHost()`, in
  `Benzene.Aws.Lambda.Core.TestHelpers`) and drives Lambda stream events/`BenzeneMessage`
  requests through it, deserializing the response back to a typed object. Opens an
  `AWSXRayRecorder` test segment around each invocation — an AWS SDK requirement for
  some client calls to work under test, unrelated to the deleted `Benzene.Aws.XRay`
  tracing package.
- `AwsLambdaBenzeneTestHostExtensions.BuildHost(...)` - builds an
  `AwsLambdaBenzeneTestHost` from the **older** `IStartUp<TContainer,...>` /
  `IAwsEntryPointBuilder` pattern (pre-`BenzeneStartUp` unification; see
  `AwsLambdaBenzeneTestStartUp<TStartUp, TContainer>`).
- `ThreadSafeTestLambdaLogger : ILambdaLogger` - buffers log lines for assertions
  instead of writing only to the console.

### Generic inline startup (`InlineStartUp.cs`)
- `InlineStartUp<TContext>` - fluent `ConfigureServices`/`Configure`/`Build` builder
  for constructing an `IEntryPointMiddlewareApplication<TRequest, TResponse>` inline
  in a test, without a named `StartUp` class.

### Dead code (do not use, do not extend)
- `MessageBuilder.cs`, `MessageBuilderExtensions.cs`, `HttpBuilder.cs` are **entirely
  commented out**. The duplication between these and `Benzene.Testing`'s
  `MessageBuilder`/`HttpBuilder` was resolved in favor of `Benzene.Testing`'s copies;
  these files were left as commented-out historical record rather than deleted.

## When to use this package
- Writing AWS Lambda tests that need to drive a real `Stream`/`ILambdaContext`
  invocation through `IAwsLambdaEntryPoint` (e.g. verifying the exact request/response
  serialization Lambda uses), rather than testing the middleware pipeline directly.
- For everything else (building AWS Lambda test hosts against the current
  `BenzeneStartUp` model, message/HTTP builders), prefer `Benzene.Testing` and
  `Benzene.Aws.Lambda.Core.TestHelpers` directly.

## Dependencies on other Benzene packages
- **Benzene.Aws.Lambda.Core** - `IAwsLambdaEntryPoint`, `AwsEventStreamContext`
- **Benzene.Testing** - `IBenzeneTestHost`, `IMessageBuilder<T>`

## Important conventions
- This package is referenced from test projects (`Benzene.Testing`/`Amazon.Lambda.TestUtilities`
  are direct dependencies), not shipped as a runtime/production dependency.
- Not a `dotnet tool` and has no CLI entry point, despite the package name.
