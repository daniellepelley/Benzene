namespace Benzene.Configuration.Core;

/// <summary>
/// An in-process <see cref="ISecretStore"/> backed by a dictionary — for tests, local development,
/// and as the lowest layer of a <see cref="CompositeSecretStore"/> (e.g. hard-coded local defaults).
/// </summary>
public class InMemorySecretStore : ISecretStore
{
    private readonly IReadOnlyDictionary<string, string> _values;

    /// <summary>Initializes an empty store.</summary>
    public InMemorySecretStore()
        : this(new Dictionary<string, string>())
    {
    }

    /// <summary>Initializes a store from the given name/value pairs.</summary>
    /// <param name="values">The seed values.</param>
    public InMemorySecretStore(IReadOnlyDictionary<string, string> values)
    {
        _values = values;
    }

    /// <inheritdoc />
    public Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
        => Task.FromResult(_values.TryGetValue(name, out var value) ? value : null);
}
