using System.Collections.Generic;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.Helper;
using Benzene.Http;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Extracts headers from an API Gateway HTTP API v2 request, folding in the v2 cookies array and
/// applying the configured <see cref="IHttpHeaderMappings"/>.
/// </summary>
public class ApiGatewayV2MessageHeadersGetter : IMessageHeadersGetter<ApiGatewayV2Context>
{
    private readonly IHttpHeaderMappings _httpHeaderMappings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiGatewayV2MessageHeadersGetter"/> class.
    /// </summary>
    /// <param name="httpHeaderMappings">The header mappings used to rename headers to their mapped values.</param>
    public ApiGatewayV2MessageHeadersGetter(IHttpHeaderMappings httpHeaderMappings)
    {
        _httpHeaderMappings = httpHeaderMappings;
    }

    /// <summary>
    /// Gets the mapped headers from the API Gateway v2 request.
    /// </summary>
    /// <param name="context">The API Gateway v2 context to extract headers from.</param>
    /// <returns>The request's headers (cookies folded in), with mapped header names replaced according to <see cref="IHttpHeaderMappings"/>.</returns>
    public IDictionary<string, string> GetHeaders(ApiGatewayV2Context context)
    {
        return DictionaryUtils.Replace(context.CombinedHeaders(), _httpHeaderMappings.GetMappings());
    }
}
