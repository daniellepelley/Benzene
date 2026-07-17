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
    public AspNetMessageVersionGetter(IRouteFinder routeFinder, IMessageHeadersGetter<AspNetContext> headersGetter)
        : base(routeFinder, headersGetter)
    {
    }

    /// <inheritdoc />
    protected override (string Method, string Path) GetMethodAndPath(AspNetContext context)
    {
        return (context.HttpContext.Request.Method, context.HttpContext.Request.Path);
    }
}
