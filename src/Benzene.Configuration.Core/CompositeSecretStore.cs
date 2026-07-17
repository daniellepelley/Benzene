namespace Benzene.Configuration.Core;

/// <summary>
/// Tries an ordered list of stores and returns the first non-<c>null</c> value. Enables layering —
/// e.g. environment variables overriding a cloud store for local development, hard-coded defaults as
/// a last resort, or a fast in-process store in front of a remote one.
/// </summary>
public class CompositeSecretStore : ISecretStore
{
    private readonly IReadOnlyList<ISecretStore> _stores;

    /// <summary>Initializes the composite from an ordered set of stores (earliest wins).</summary>
    /// <param name="stores">The stores to try in order.</param>
    public CompositeSecretStore(params ISecretStore[] stores)
    {
        _stores = stores;
    }

    /// <inheritdoc />
    public async Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        foreach (var store in _stores)
        {
            var value = await store.GetSecretAsync(name, cancellationToken);
            if (value != null)
            {
                return value;
            }
        }

        return null;
    }
}
