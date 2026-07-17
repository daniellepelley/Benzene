using System.Globalization;

namespace Benzene.Configuration.Core;

/// <summary>
/// Ergonomic, typed reads over an <see cref="ISecretStore"/> for building configuration at startup.
/// <see cref="RequireAsync"/> throws <see cref="MissingSecretException"/> when a value is absent or
/// blank (fail fast); <see cref="GetAsync"/> returns a default. The typed overloads parse and throw a
/// clear <see cref="FormatException"/> when a present value is malformed.
/// </summary>
public class SecretResolver
{
    private readonly ISecretStore _store;

    /// <summary>Initializes the resolver over <paramref name="store"/>.</summary>
    public SecretResolver(ISecretStore store)
    {
        _store = store;
    }

    /// <summary>Returns the value for <paramref name="name"/>, throwing if it is absent or blank.</summary>
    public async Task<string> RequireAsync(string name, CancellationToken cancellationToken = default)
    {
        var value = await _store.GetSecretAsync(name, cancellationToken);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new MissingSecretException(new[] { name });
        }

        return value;
    }

    /// <summary>Returns the value for <paramref name="name"/>, or <paramref name="defaultValue"/> if absent/blank.</summary>
    public async Task<string?> GetAsync(string name, string? defaultValue = null, CancellationToken cancellationToken = default)
    {
        var value = await _store.GetSecretAsync(name, cancellationToken);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    /// <summary>Returns the value for <paramref name="name"/> parsed as an <see cref="int"/>.</summary>
    public async Task<int> RequireIntAsync(string name, CancellationToken cancellationToken = default)
    {
        var value = await RequireAsync(name, cancellationToken);
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            throw new FormatException($"Secret '{name}' is not a valid integer.");
        }

        return result;
    }

    /// <summary>Returns the value for <paramref name="name"/> parsed as a <see cref="bool"/>.</summary>
    public async Task<bool> RequireBoolAsync(string name, CancellationToken cancellationToken = default)
    {
        var value = await RequireAsync(name, cancellationToken);
        if (!bool.TryParse(value, out var result))
        {
            throw new FormatException($"Secret '{name}' is not a valid boolean.");
        }

        return result;
    }

    /// <summary>Returns the value for <paramref name="name"/> parsed as an absolute <see cref="Uri"/>.</summary>
    public async Task<Uri> RequireUriAsync(string name, CancellationToken cancellationToken = default)
    {
        var value = await RequireAsync(name, cancellationToken);
        if (!Uri.TryCreate(value, UriKind.Absolute, out var result))
        {
            throw new FormatException($"Secret '{name}' is not a valid absolute URI.");
        }

        return result;
    }
}
