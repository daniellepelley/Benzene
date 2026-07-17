using Benzene.Abstractions.Messages.Mappers;
using Benzene.Http.Routing;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Extracts the payload schema version from the matched route's <c>version</c> route parameter,
/// falling back to the header fallback list when the matched route declares no such parameter
/// (docs/specification/versioning.md §2.1).
/// </summary>
public class ApiGatewayMessageVersionGetter : HttpMessageVersionGetterBase<ApiGatewayContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiGatewayMessageVersionGetter"/> class.
    /// </summary>
    /// <param name="routeFinder">The route finder used to match the request to a registered topic.</param>
    /// <param name="headersGetter">Extracts the header dictionary from the context, for the fallback path.</param>
    public ApiGatewayMessageVersionGetter(IRouteFinder routeFinder, IMessageHeadersGetter<ApiGatewayContext> headersGetter)
        : base(routeFinder, headersGetter)
    {
    }

    /// <inheritdoc />
    protected override (string Method, string Path) GetMethodAndPath(ApiGatewayContext context)
    {
        return (context.ApiGatewayProxyRequest.HttpMethod, context.ApiGatewayProxyRequest.Path);
    }
}
