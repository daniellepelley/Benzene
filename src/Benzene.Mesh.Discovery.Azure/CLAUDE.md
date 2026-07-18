# Benzene.Mesh.Discovery.Azure

## What this package does
Azure **self-discovery** for the Benzene mesh: implements `IMeshDiscoveryProvider`
(`Benzene.Mesh.Contracts`) by enumerating a subscription's App Service / Function App resources
(`Microsoft.Web/sites`) filtered by tag (default `benzene`) and emitting a mesh registry entry per
site. The Azure analogue of `Benzene.Mesh.Discovery.Aws` — discovery replaces a hand-written
`mesh.json`.

## Key types
- `AzureAppServiceDiscoveryProvider : IMeshDiscoveryProvider` (`Key = "Azure"`) — the discovery logic:
  filters the listed sites by the `MeshDiscoveryFilter` (tags + optional `Regions`) and emits an
  **HTTP** entry per site at `https://{host}/benzene/spec|health`. Host defaults to
  `{name}.azurewebsites.net`; override per-site with the `benzene:host` tag (custom domain) and the
  paths with `benzene:spec-path`/`benzene:health-path`. Because App Services are HTTP-addressable the
  entries use `MeshServiceSource.Http` — the aggregator's existing `HttpMeshServiceSource`
  interrogates each, so there's **no Azure-specific fetch source**.
- `IAzureResourceLister` / `AzureResourceInfo` — a thin port over Azure Resource Manager (list
  `Microsoft.Web/sites` → name/location/tags) so the provider's logic is unit-testable without the
  SDK. `AzureArmResourceLister` is the real implementation over `ArmClient`, kept logic-free.
- `Extensions.AddMeshAzureDiscovery()` — registers the provider over an `ArmClient` authenticated with
  `DefaultAzureCredential` (managed identity in Azure, dev credential locally) plus a
  `MeshDiscoveryRunner`, mirroring `AddMeshAwsLambdaDiscovery`'s DI shape.

## Deploying
- The mesh's managed identity needs **Reader** on the resources it enumerates. Scope discovery to a
  region with `MeshDiscoveryFilter(regions: […])`.
- Pair with `Benzene.Mesh.Azure.Blob` (the `BlobMeshArtifactStore`) so the discovered catalog +
  artifacts persist centrally where the Mesh UI can read them.

## Dependencies
- `Benzene.Abstractions`, `Benzene.Mesh.Contracts`.
- NuGet: `Azure.ResourceManager`, `Azure.Identity`.

## Tests
- `test/Benzene.Mesh.Test/Discovery/AzureAppServiceDiscoveryProviderTest.cs` — mocks
  `IAzureResourceLister`: default-host URL building, host/path tag overrides, region filtering,
  valued-tag filtering.
