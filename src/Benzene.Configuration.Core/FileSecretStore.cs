using System.IO;

namespace Benzene.Configuration.Core;

/// <summary>
/// An <see cref="ISecretStore"/> that reads each value from a file named after it in a directory —
/// the Docker/Kubernetes secret-mount convention (e.g. <c>/run/secrets/db_password</c>). Keeps
/// secrets out of environment variables and image layers.
/// </summary>
/// <remarks>
/// The logical name is mapped to a file name by replacing <c>:</c>, <c>.</c>, <c>/</c>, and <c>\</c>
/// with <c>_</c>. A trailing newline (which editors and <c>echo</c> add) is trimmed; other
/// whitespace is preserved, since it may be significant in a secret.
/// </remarks>
public class FileSecretStore : ISecretStore
{
    private readonly string _directory;

    /// <summary>Initializes the store to read from <paramref name="directory"/>.</summary>
    /// <param name="directory">The directory holding one file per secret.</param>
    public FileSecretStore(string directory)
    {
        _directory = directory;
    }

    /// <inheritdoc />
    public async Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_directory, SanitizeFileName(name));
        if (!File.Exists(path))
        {
            return null;
        }

        var content = await File.ReadAllTextAsync(path, cancellationToken);
        return content.TrimEnd('\r', '\n');
    }

    private static string SanitizeFileName(string name)
        => name.Replace(':', '_').Replace('.', '_').Replace('/', '_').Replace('\\', '_');
}
