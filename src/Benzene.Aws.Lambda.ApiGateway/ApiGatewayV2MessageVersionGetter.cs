using System.Collections.Generic;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Http.Routing;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Extracts the payload schema version from the matched route's <c>version</c> route parameter,
/// falling back to the header fallback list when the matched route declares no such parameter
/// (docs/specification/versioning.md §2.1). The v2 counterpart of
/// <see cref="ApiGatewayMessageVersionGetter"/>.
/// </summary>
public class ApiGatewayV2MessageVersionGetter : HttpMessageVersionGetterBase<ApiGatewayV2Context>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiGatewayV2MessageVersionGetter"/> class.
    /// </summary>
    /// <param name="routeFinder">The route finder used to match the request to a registered topic.</param>
    /// <param name="headersGetter">Extracts the header dictionary from the context, for the fallback path.</param>
    /// <param name="headerNames">The header-name fallback list; defaults to <see cref="Benzene.Core.MessageHandlers.HeaderMessageVersionGetter{TContext}.DefaultHeaderNames"/> when null.</param>
    public ApiGatewayV2MessageVersionGetter(IRouteFinder routeFinder, IMessageHeadersGetter<ApiGatewayV2Context> headersGetter, IReadOnlyList<string>? headerNames = null)
        : base(routeFinder, headersGetter, headerNames)
    {
    }

    /// <inheritdoc />
    protected override (string Method, string Path) GetMethodAndPath(ApiGatewayV2Context context)
    {
        return (context.Method, context.Path);
    }
}
