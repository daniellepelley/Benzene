using System.Collections.Generic;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Http.Routing;

namespace Benzene.AspNet.Core;

/// <summary>
/// Extracts the payload schema version from the matched route's <c>version</c> route parameter,
/// falling back to the header fallback list when the matched route declares no such parameter
/// (docs/specification/versioning.md §2.1).
/// </summary>
public class AspNetMessageVersionGetter : HttpMessageVersionGetterBase<AspNetContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AspNetMessageVersionGetter"/> class.
    /// </summary>
    /// <param name="routeFinder">The route finder used to match the request's method and path to a route.</param>
    /// <param name="headersGetter">Extracts the header dictionary from the context, for the fallback path.</param>
    /// <param name="headerNames">The header-name fallback list; defaults to <see cref="Benzene.Core.MessageHandlers.HeaderMessageVersionGetter{TContext}.DefaultHeaderNames"/> when null.</param>
    public AspNetMessageVersionGetter(IRouteFinder routeFinder, IMessageHeadersGetter<AspNetContext> headersGetter, IReadOnlyList<string>? headerNames = null)
        : base(routeFinder, headersGetter, headerNames)
    {
    }

    /// <inheritdoc />
    protected override (string Method, string Path) GetMethodAndPath(AspNetContext context)
    {
        return (context.HttpContext.Request.Method, context.HttpContext.Request.Path);
    }
}
