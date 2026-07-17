namespace Benzene.Configuration.Core;

/// <summary>
/// An <see cref="ISecretStore"/> reading from environment variables — the twelve-factor default and
/// the natural local-development override in front of a cloud store. A logical name is mapped to an
/// environment-variable key by upper-casing it and replacing <c>:</c>, <c>.</c>, <c>-</c>, and spaces
/// with <c>_</c> (so <c>Db:Password</c> reads <c>DB_PASSWORD</c>), with an optional prefix.
/// </summary>
public class EnvironmentVariableSecretStore : ISecretStore
{
    private readonly string _prefix;

    /// <summary>Initializes the store.</summary>
    /// <param name="prefix">
    /// An optional prefix prepended to every logical name before mapping (e.g. <c>MyApp:</c> so
    /// <c>Db:Password</c> reads <c>MYAPP_DB_PASSWORD</c>). Defaults to none.
    /// </param>
    public EnvironmentVariableSecretStore(string prefix = "")
    {
        _prefix = prefix ?? "";
    }

    /// <inheritdoc />
    public Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        var key = ToEnvironmentVariableKey(_prefix + name);
        var value = Environment.GetEnvironmentVariable(key);
        return Task.FromResult(string.IsNullOrEmpty(value) ? null : value);
    }

    /// <summary>Maps a logical name to an environment-variable key.</summary>
    public static string ToEnvironmentVariableKey(string name)
    {
        var chars = new char[name.Length];
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            chars[i] = c is ':' or '.' or '-' or ' ' ? '_' : char.ToUpperInvariant(c);
        }

        return new string(chars);
    }
}
