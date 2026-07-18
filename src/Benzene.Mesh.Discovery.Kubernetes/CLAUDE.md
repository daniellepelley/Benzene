# Benzene.Mesh.Discovery.Kubernetes

## What this package does
Kubernetes **self-discovery** for the Benzene mesh: implements `IMeshDiscoveryProvider`
(`Benzene.Mesh.Contracts`) by listing the cluster's Services filtered by label (default `benzene`)
and emitting a mesh registry entry per Service. This is the Kubernetes analogue of
`Benzene.Mesh.Discovery.Aws` — discovery replaces a hand-written `mesh.json`.

## Key types
- `KubernetesServiceDiscoveryProvider : IMeshDiscoveryProvider` (`Key = "Kubernetes"`) — the discovery
  logic: builds a Kubernetes label selector from the `MeshDiscoveryFilter`, lists matching Services,
  and emits an **HTTP** entry per Service at its in-cluster DNS
  (`http://{name}.{namespace}.svc.cluster.local[:port]/benzene/spec|health`). Because in-cluster
  Services are HTTP-addressable, entries use the default `MeshServiceSource.Http` — the aggregator's
  existing `HttpMeshServiceSource` interrogates each, so there's **no Kubernetes-specific fetch
  source**. Per-Service label overrides: `benzene.scheme` (default `http`), `benzene.spec-path`,
  `benzene.health-path`. `BuildLabelSelector` is public + unit-tested.
- `IKubernetesServiceLister` / `KubernetesServiceInfo` — a thin port over the K8s API (list Services →
  name/namespace/port/labels) so the provider's logic is unit-testable without the SDK.
  `KubernetesApiServiceLister` is the real implementation over the client SDK's `IKubernetes`
  (`CoreV1.ListService…`), kept logic-free.
- `Extensions.AddMeshKubernetesDiscovery()` — registers the provider over an **in-cluster**
  `IKubernetes` (`KubernetesClientConfiguration.InClusterConfig()`) plus a `MeshDiscoveryRunner`,
  mirroring `AddMeshAwsLambdaDiscovery`'s DI shape.

## Deploying
- The mesh pod's ServiceAccount needs RBAC to **list Services** (`get`/`list` on `services`) in the
  target namespace(s). Scope discovery to one namespace via `MeshDiscoveryFilter(@namespace: "…")`,
  or leave it null to list across all namespaces.
- Pair with the filesystem or a blob artifact store — a single mesh pod that both writes the catalog
  and serves the Mesh UI can use `FileSystemMeshArtifactStore` on its own volume.

## Dependencies
- `Benzene.Abstractions`, `Benzene.Mesh.Contracts`.
- NuGet: `KubernetesClient` (the official .NET Kubernetes client).

## Tests
- `test/Benzene.Mesh.Test/Discovery/KubernetesServiceDiscoveryProviderTest.cs` — mocks
  `IKubernetesServiceLister`: in-cluster DNS URL building, non-default port, scheme/path label
  overrides, valued-tag filtering, namespace+selector pass-through.
