namespace Benzene.Configuration.Core;

/// <summary>
/// The neutral "fetch a named value from somewhere" seam that decouples an application from where
/// its secrets and configuration actually live. An app depends on this interface; a provider adapter
/// implements it (environment variables, a mounted file, Azure Key Vault, AWS Secrets Manager, SSM
/// Parameter Store, Azure App Configuration, ...), so the same code runs against any backend and
/// ports across clouds without change.
/// </summary>
/// <remarks>
/// Values may be secrets (a database password) or plain configuration (a service endpoint) — both
/// are "a named value from a source", which is why one seam covers both a secret store and a config
/// store. Deliberately one method: a provider adapter is trivial to write, and composition
/// (<see cref="CompositeSecretStore"/>), caching (<see cref="CachingSecretStore"/>), validation, and
/// typed resolution are all layered on top in this package rather than pushed onto every adapter.
/// </remarks>
public interface ISecretStore
{
    /// <summary>
    /// Fetches the value for <paramref name="name"/>, or <c>null</c> if this store does not have it
    /// (so a <see cref="CompositeSecretStore"/> can fall through to the next store).
    /// </summary>
    /// <param name="name">The logical secret/config name.</param>
    /// <param name="cancellationToken">A token to cancel the lookup.</param>
    Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default);
}
