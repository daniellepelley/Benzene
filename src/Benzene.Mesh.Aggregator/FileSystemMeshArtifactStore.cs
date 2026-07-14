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
        var fullPath = Path.Combine(_rootDirectory, relativePath);
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
        var fullPath = Path.Combine(_rootDirectory, relativePath);
        return File.Exists(fullPath) ? await File.ReadAllTextAsync(fullPath) : null;
    }
}
