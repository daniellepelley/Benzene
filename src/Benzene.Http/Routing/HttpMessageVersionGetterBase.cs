using Benzene.Abstractions.Messages.Mappers;
using Benzene.Abstractions.MessageHandlers.Mappers;
using Benzene.Core.MessageHandlers;

namespace Benzene.Http.Routing;

/// <summary>
/// Base <see cref="IMessageVersionGetter{TContext}"/> for HTTP-shaped transports: reads the
/// payload schema version from the matched route's <c>version</c> route parameter (e.g.
/// <c>/v{version}/orders/{id}</c>), falling back to the header fallback list
/// (<see cref="HeaderMessageVersionGetter{TContext}"/>) when the matched route declares no such
/// parameter (docs/specification/versioning.md §2.1) - so an HTTP transport can support both a
/// path-versioned and a header-versioned surface for the same topic without duplicating routes.
/// </summary>
/// <typeparam name="TContext">The transport-specific HTTP context type.</typeparam>
public abstract class HttpMessageVersionGetterBase<TContext> : IMessageVersionGetter<TContext>
{
    /// <summary>The route parameter name checked before falling back to headers.</summary>
    public const string VersionRouteParameterName = "version";

    private readonly IRouteFinder _routeFinder;
    private readonly HeaderMessageVersionGetter<TContext> _headerFallback;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpMessageVersionGetterBase{TContext}"/> class.
    /// </summary>
    /// <param name="routeFinder">The route finder used to match the request's method and path to a route.</param>
    /// <param name="headersGetter">Extracts the header dictionary from the context, for the fallback path.</param>
    /// <param name="headerNames">The header-name fallback list; defaults to <see cref="HeaderMessageVersionGetter{TContext}.DefaultHeaderNames"/>.</param>
    protected HttpMessageVersionGetterBase(IRouteFinder routeFinder, IMessageHeadersGetter<TContext> headersGetter, IReadOnlyList<string>? headerNames = null)
    {
        _routeFinder = routeFinder;
        _headerFallback = new HeaderMessageVersionGetter<TContext>(headersGetter, headerNames);
    }

    /// <summary>Extracts the HTTP method and path this transport's context carries.</summary>
    /// <param name="context">The transport-specific context for the incoming request.</param>
    protected abstract (string Method, string Path) GetMethodAndPath(TContext context);

    /// <inheritdoc />
    public string? GetVersion(TContext context)
    {
        var (method, path) = GetMethodAndPath(context);
        var route = _routeFinder.Find(method, path);

        if (route != null
            && route.Parameters.TryGetValue(VersionRouteParameterName, out var value)
            && value is string { Length: > 0 } version)
        {
            return version;
        }

        return _headerFallback.GetVersion(context);
    }
}
