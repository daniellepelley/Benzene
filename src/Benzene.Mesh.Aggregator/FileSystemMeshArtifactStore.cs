namespace Benzene.Mesh.Aggregator;

/// <summary>
/// An <see cref="IMeshArtifactStore"/> that publishes artifacts to a directory on local disk.
/// </summary>
public class FileSystemMeshArtifactStore : IMeshArtifactStore
{
    private readonly string _rootDirectory;

    /// <summary>Initializes a new instance of the <see cref="FileSystemMeshArtifactStore"/> class.</summary>
    /// <param name="rootDirectory">The directory artifacts are written under. Created on first write if it doesn't exist.</param>
    public FileSystemMeshArtifactStore(string rootDirectory)
    {
        _rootDirectory = rootDirectory;
    }

    /// <inheritdoc />
    public async Task PublishAsync(string relativePath, string content)
    {
        var fullPath = ResolveWithinRoot(relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(fullPath, content);
    }

    /// <inheritdoc />
    public async Task<string?> TryReadAsync(string relativePath)
    {
        var fullPath = ResolveWithinRoot(relativePath);
        return File.Exists(fullPath) ? await File.ReadAllTextAsync(fullPath) : null;
    }

    /// <summary>
    /// Resolves <paramref name="relativePath"/> against the store root and asserts the result stays
    /// inside it. The relative path can carry a service name that originated in an untrusted push
    /// report (<c>services/{report.Name}.json</c>), so a value like <c>"../../etc/passwd"</c> or a
    /// rooted path would otherwise let <see cref="Path.Combine(string,string)"/> escape the root and
    /// read or overwrite an arbitrary file. Resolving to a full path and checking containment closes
    /// that traversal at the storage boundary, protecting every caller.
    /// </summary>
    private string ResolveWithinRoot(string relativePath)
    {
        var rootFull = Path.GetFullPath(_rootDirectory);
        var combined = Path.GetFullPath(Path.Combine(rootFull, relativePath));

        var rootWithSeparator = rootFull.EndsWith(Path.DirectorySeparatorChar)
            ? rootFull
            : rootFull + Path.DirectorySeparatorChar;

        if (!string.Equals(combined, rootFull, StringComparison.Ordinal) &&
            !combined.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new System.UnauthorizedAccessException(
                $"The artifact path '{relativePath}' resolves outside the store root and was rejected.");
        }

        return combined;
    }
}
