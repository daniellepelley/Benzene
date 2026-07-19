using System.Collections.Generic;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Http.Routing;

namespace Benzene.SelfHost.Http;

/// <summary>
/// Extracts the payload schema version from the matched route's <c>version</c> route parameter,
/// falling back to the header fallback list when the matched route declares no such parameter
/// (docs/specification/versioning.md §2.1).
/// </summary>
public class HttpListenerMessageVersionGetter : HttpMessageVersionGetterBase<SelfHostHttpContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpListenerMessageVersionGetter"/> class.
    /// </summary>
    /// <param name="routeFinder">The route finder used to match the request's method and path to a route.</param>
    /// <param name="headersGetter">Extracts the header dictionary from the context, for the fallback path.</param>
    /// <param name="headerNames">The header-name fallback list; defaults to <see cref="Benzene.Core.MessageHandlers.HeaderMessageVersionGetter{TContext}.DefaultHeaderNames"/> when null.</param>
    public HttpListenerMessageVersionGetter(IRouteFinder routeFinder, IMessageHeadersGetter<SelfHostHttpContext> headersGetter, IReadOnlyList<string>? headerNames = null)
        : base(routeFinder, headersGetter, headerNames)
    {
    }

    /// <inheritdoc />
    protected override (string Method, string Path) GetMethodAndPath(SelfHostHttpContext context)
    {
        return (context.HttpListenerContext.Request.HttpMethod, context.HttpListenerContext.Request.RawUrl);
    }
}
