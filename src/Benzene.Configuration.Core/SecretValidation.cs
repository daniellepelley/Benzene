namespace Benzene.Configuration.Core;

/// <summary>
/// Startup fail-fast validation: verify every required secret resolves before the service starts
/// serving traffic, so a missing credential surfaces as an immediate, complete startup error rather
/// than a first-request failure deep in a handler.
/// </summary>
public static class SecretValidation
{
    /// <summary>
    /// Verifies every name in <paramref name="requiredNames"/> resolves to a non-blank value,
    /// throwing <see cref="MissingSecretException"/> listing <em>all</em> missing names at once.
    /// </summary>
    /// <param name="store">The store to validate against.</param>
    /// <param name="requiredNames">The names that must be present.</param>
    /// <param name="cancellationToken">A token to cancel the checks.</param>
    public static async Task EnsureRequiredAsync(
        ISecretStore store, IEnumerable<string> requiredNames, CancellationToken cancellationToken = default)
    {
        var missing = new List<string>();
        foreach (var name in requiredNames)
        {
            var value = await store.GetSecretAsync(name, cancellationToken);
            if (string.IsNullOrWhiteSpace(value))
            {
                missing.Add(name);
            }
        }

        if (missing.Count > 0)
        {
            throw new MissingSecretException(missing);
        }
    }

    /// <summary>
    /// Verifies every name in <paramref name="requiredNames"/> resolves to a non-blank value,
    /// throwing <see cref="MissingSecretException"/> listing all missing names at once.
    /// </summary>
    /// <param name="store">The store to validate against.</param>
    /// <param name="requiredNames">The names that must be present.</param>
    public static Task EnsureRequiredAsync(ISecretStore store, params string[] requiredNames)
        => EnsureRequiredAsync(store, (IEnumerable<string>)requiredNames);
}
