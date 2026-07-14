namespace Benzene.Mesh.Aggregator;

/// <summary>
/// Publishes (and reads back) the JSON artifacts a <see cref="MeshAggregator"/> produces.
/// </summary>
/// <remarks>
/// Kept as a small port so a blob-storage adapter (S3, Azure Blob) can be added later as a drop-in
/// implementation of this interface, without changing <see cref="MeshAggregator"/>'s own logic. Only
/// a local-disk implementation (<see cref="FileSystemMeshArtifactStore"/>) ships in this package.
/// </remarks>
public interface IMeshArtifactStore
{
    /// <summary>Writes <paramref name="content"/> to <paramref name="relativePath"/>, creating or overwriting it.</summary>
    /// <param name="relativePath">A path relative to the store's root, e.g. <c>"manifest.json"</c> or <c>"services/orders-api.json"</c>.</param>
    /// <param name="content">The content to write.</param>
    Task PublishAsync(string relativePath, string content);

    /// <summary>Reads the content previously published at <paramref name="relativePath"/>, if any.</summary>
    /// <param name="relativePath">A path relative to the store's root.</param>
    /// <returns>The content, or <c>null</c> if nothing has been published at that path.</returns>
    Task<string?> TryReadAsync(string relativePath);
}
