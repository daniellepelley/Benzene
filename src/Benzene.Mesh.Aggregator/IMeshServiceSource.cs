using Benzene.Mesh.Contracts;

namespace Benzene.Mesh.Aggregator;

/// <summary>
/// Fetches a registered service's spec and health documents however fits that service's
/// <see cref="MeshServiceRegistryEntry.Source"/> - HTTP (<see cref="HttpMeshServiceSource"/>, the
/// only implementation this package ships), an AWS Lambda Invoke, or any other transport an
/// adapter package chooses to add. <see cref="MeshAggregator"/> is decoupled from any one
/// transport by depending on this port rather than an <see cref="HttpClient"/> directly.
/// </summary>
public interface IMeshServiceSource
{
    /// <summary>
    /// The <see cref="MeshServiceRegistryEntry.Source"/> value this source answers for (see
    /// <see cref="MeshServiceSource"/>). Matched case-insensitively.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Fetches <paramref name="entry"/>'s spec document, verbatim, as raw JSON text. Should throw
    /// on failure (connection error, timeout, non-success response) rather than returning
    /// <c>null</c> - <see cref="MeshAggregator"/> catches and records the exception's type name.
    /// </summary>
    Task<string> FetchSpecAsync(MeshServiceRegistryEntry entry, CancellationToken cancellationToken);

    /// <summary>
    /// Fetches <paramref name="entry"/>'s health check response, verbatim, as raw JSON text (to be
    /// deserialized by the caller). Should throw only on a genuine fetch failure - an
    /// unhealthy-but-successfully-fetched response (e.g. HTTP 503 with a valid body) must still be
    /// returned, not treated as a failure, so it can be distinguished from a truly unreachable
    /// service.
    /// </summary>
    Task<string> FetchHealthAsync(MeshServiceRegistryEntry entry, CancellationToken cancellationToken);

    /// <summary>
    /// Fetches <paramref name="entry"/>'s spec in an alternate representation (e.g.
    /// <c>"asyncapi"</c> — the same <c>spec</c> endpoint's other <c>type</c>), or <c>null</c> when
    /// this source can't serve that type. This is <em>best-effort and additive</em>: unlike
    /// <see cref="FetchSpecAsync"/> (whose failure marks the service unreachable), a source that
    /// doesn't support alternate spec types just returns <c>null</c> and the aggregator omits that
    /// service from whatever artifact is built on it. The default implementation returns
    /// <c>null</c>, so existing sources don't have to implement it.
    /// </summary>
    /// <param name="entry">The service to fetch for.</param>
    /// <param name="specType">The spec type to request (e.g. <c>"asyncapi"</c>).</param>
    /// <param name="cancellationToken">Cancels the fetch (bounded by the aggregator's per-service timeout).</param>
    Task<string?> TryFetchSpecAsync(MeshServiceRegistryEntry entry, string specType, CancellationToken cancellationToken)
        => Task.FromResult<string?>(null);
}
