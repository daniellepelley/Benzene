# Benzene.Mesh.Aws.Lambda

## What this package does
Fetches a service's spec/health via a synchronous AWS Lambda `Invoke` instead of HTTP - for
services with no public HTTP surface at all. Phase B of the multi-transport data collection work
in `work/service-mesh-roadmap-1.0.md`'s 2026-07-15 update. A focused adapter package, mirroring
`Benzene.Mesh.Tracing.Tempo`'s shape (depends on `Benzene.Mesh.Aggregator` for the
`IMeshServiceSource` port, keeps the AWS SDK out of the aggregator's own dependency graph).

## Key types/interfaces
- `LambdaMeshServiceSource : IMeshServiceSource` - sends a `BenzeneMessageClientRequest` with topic
  `"spec"`/`"healthcheck"` to `entry.SourceOptions["functionName"]` via
  `Benzene.Clients.Aws.Lambda.IAwsLambdaClient.SendMessageAsync` (`InvocationType.RequestResponse`),
  reading the response's `.Body` as the opaque raw JSON string - the exact same treatment
  `HttpMeshServiceSource` gives an HTTP response, no new typed models. Wraps the call in
  `Task.WaitAsync(cancellationToken)` since `IAwsLambdaClient.SendMessageAsync` has no
  `CancellationToken` parameter of its own - this is what makes `MeshAggregator`'s
  `PerServiceFetchTimeout` still bound this source's fetch time from the caller's point of view.
  Throws `InvalidOperationException` if `SourceOptions["functionName"]` is missing - caught by
  `MeshAggregator`'s existing per-entry try/catch, surfacing as that service's own error, not a
  run-wide failure.
- `Extensions.AddMeshLambdaSource()` - registers a default-credential-chain `IAmazonLambda`,
  `IAwsLambdaClient`, and this source as an additional `IMeshServiceSource`. **Deliberately does
  not register `IMeshArtifactStore`/`MeshAggregator`** - requires
  `Benzene.Mesh.Aggregator.Extensions.AddMeshAggregator(...)` already registered in the same
  container, same prerequisite shape as `Benzene.Mesh.Tracing.Tempo.AddTempoTopology`.
- `AwsLambdaMeshServiceDispatcher : Benzene.Mesh.Dispatch.IMeshServiceDispatcher` +
  `Extensions.AddMeshLambdaDispatcher()` - the **write-side** counterpart of `LambdaMeshServiceSource`,
  for the opt-in live-dispatch feature (`Benzene.Mesh.Dispatch`). Sends a caller-chosen topic/body to a
  service via the **same** `IAwsLambdaClient` / `lambda:InvokeFunction` grant this source already uses to
  read spec/health (the client registrations are shared, idempotent via `TryAdd`, so it's safe to call
  alongside `AddMeshLambdaSource()`). This is why direct-invoke reuses an existing access path rather
  than a new grant - the §10.7 premise. Requires `SourceOptions["functionName"]`, same as the source.

## Why this reuses `Benzene.Clients.Aws.Lambda.IAwsLambdaClient` instead of building a new client
`IAwsLambdaClient`/`AwsLambdaClient` already exist and already wrap `IAmazonLambda.InvokeAsync`
(`Event`/`RequestResponse`), and the **receiving** side already exists too:
`Benzene.Aws.Lambda.Core.BenzeneMessage.BenzeneMessageLambdaHandler` routes a raw Lambda invocation
whose payload has a `Topic` to the normal BenzeneMessage pipeline. Combined with
`Benzene.Schema.OpenApi`'s `"spec"` topic and `Benzene.HealthChecks`'s `"healthcheck"` topic, any
service already wired the normal Benzene way already answers a direct Lambda invoke for spec/health
with **zero target-side changes**. This package is a thin wrapper, not new invocation plumbing -
verified against the actual shipped code, not assumed, before building this.

Uses the lower-level `IAwsLambdaClient` directly, not the higher-level
`AwsLambdaBenzeneMessageClient` - that wrapper is built around typed `IBenzeneClientRequest`/
`IBenzeneResult` call sites a generated client would have; this ad-hoc "poll spec/health as an
opaque string" use case has neither, so the lower-level client is the better fit and avoids pulling
`Benzene.Schema.OpenApi` (for typed spec models) into this adapter at all.

## Important conventions
- The `"spec"`/`"healthcheck"` topic literals are hardcoded, not referenced from
  `Benzene.Schema.OpenApi.Constants.DefaultSpecTopic`/`Benzene.HealthChecks.Constants.DefaultHealthCheckTopic`
  - deliberate, to keep this package's dependency graph to just `Benzene.Mesh.Aggregator` +
  `Benzene.Clients.Aws`. This is coupling-by-convention: if either topic constant is ever renamed,
  this adapter silently breaks. `test/Benzene.Mesh.Test/LambdaMeshServiceSourceTest.cs`'s two
  `...MatchingBenzene...` tests capture the actual topic sent to a mocked `IAwsLambdaClient` and
  assert it against the real constants directly (same cross-check shape as `MeshHashingTest`'s
  against `CodeGenHelpers.GenerateHash`) so a rename fails loudly here instead.
- `IAmazonLambda` is registered via `new AmazonLambdaClient()` (default AWS credential chain) -
  the same default-construction pattern `Benzene.CodeGen.Cli.Core.AmazonLambdaClientFactory` uses.
  Override the `IAmazonLambda` registration yourself (register it before calling
  `AddMeshLambdaSource()`, or re-register after) for custom credentials/region.
- **The client is constructed lazily.** `MeshAggregator` resolves *every* `IMeshServiceSource`
  eagerly (via `IEnumerable<IMeshServiceSource>` constructor injection) as soon as it's built, so
  `AddMeshLambdaSource()` registers `LambdaMeshServiceSource` with a `Lazy<IAwsLambdaClient>` rather
  than the resolved client - the `AmazonLambdaClient` (which throws `No RegionEndpoint or ServiceURL
  configured` without a region) is only constructed the first time a service with
  `Source=AwsLambdaInvoke` is actually fetched. This is what lets a **pure-HTTP** mesh host (e.g.
  `deploy/Mesh/Benzene.Mesh.Host` or `examples/K8sMesh/compose`) that references this package start
  with no AWS region/credentials configured at all. `LambdaMeshServiceSource` keeps its original
  eager `IAwsLambdaClient` constructor too (used by the tests), delegating to the lazy one.

## When to use this package
- A monitored service is hosted on AWS Lambda with no public HTTP surface (no API Gateway/Function
  URL), but does accept a direct `Invoke` - wire `AddMeshAggregator(...)` then
  `AddMeshLambdaSource()` into the aggregator's host, and give each such service's
  `MeshServiceRegistryEntry` `Source = MeshServiceSource.AwsLambdaInvoke` and
  `SourceOptions = { ["functionName"] = "..." }`.
- Not for services with zero synchronous entry point at all (SQS/SNS/EventBridge-only Lambdas) -
  there is no `Invoke` this package (or any pull-based source) can make that returns a response for
  those; that case needs a push/self-report path (`Benzene.Mesh.Reporting`, Phase C).

## Dependencies on other Benzene packages
- **Benzene.Mesh.Aggregator** - `IMeshServiceSource`.
- **Benzene.Clients.Aws** - `IAwsLambdaClient`/`AwsLambdaClient`/`BenzeneMessageClientRequest`, and
  transitively `AWSSDK.Lambda` - no new external NuGet dependency needed in this package's own csproj.
