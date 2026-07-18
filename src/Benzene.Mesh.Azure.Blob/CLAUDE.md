# Benzene.Mesh.Azure.Blob

## What this package does
An Azure Blob Storage `IMeshArtifactStore` for `Benzene.Mesh.Aggregator` — the Azure analogue of
`Benzene.Mesh.Aws.S3`. It persists the aggregator's generated catalog (`manifest.json`,
`services/*.json`, `topology.json`, `topics.json`) and the discovery-generated `registry.json` as
blobs keyed by their relative path, so an Azure-hosted mesh writes its output centrally where the
Mesh UI reads it.

## Key types
- `BlobMeshArtifactStore : IMeshArtifactStore` — `PublishAsync`/`TryReadAsync` over a
  `BlobContainerClient`, with an optional blob-name prefix. `TryReadAsync` maps a 404
  (`RequestFailedException.Status == 404`) to `null`, same contract as the S3/filesystem stores.
- `Extensions.AddMeshAggregatorWithBlob(registry, container, prefix?)` — registers the aggregator
  backed by this store over a caller-supplied `BlobContainerClient`. A convenience overload takes a
  blob-service `Uri` + container name and authenticates with `DefaultAzureCredential` (managed
  identity in Azure, dev credential locally). Mirrors `AddMeshAggregatorWithS3`.

## Deploying
- The blob container must exist (create it in Terraform/Bicep, as with the S3 bucket — the store
  doesn't create it). The mesh's managed identity needs **Storage Blob Data Contributor** on it.

## Dependencies
- `Benzene.Abstractions`, `Benzene.Mesh.Aggregator` (the `IMeshArtifactStore` port + `AddMeshAggregator`).
- NuGet: `Azure.Storage.Blobs`, `Azure.Identity`.
