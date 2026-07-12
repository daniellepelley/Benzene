using System.Collections.Generic;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.Helper;
using Benzene.Http;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Extracts headers from an API Gateway request, applying the configured <see cref="IHttpHeaderMappings"/>.
/// </summary>
public class ApiGatewayMessageHeadersGetter : IMessageHeadersGetter<ApiGatewayContext>
{
    private readonly IHttpHeaderMappings _httpHeaderMappings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiGatewayMessageHeadersGetter"/> class.
    /// </summary>
    /// <param name="httpHeaderMappings">The header mappings used to rename headers to their mapped values.</param>
    public ApiGatewayMessageHeadersGetter(IHttpHeaderMappings httpHeaderMappings)
    {
        _httpHeaderMappings = httpHeaderMappings;
    }

    /// <summary>
    /// Gets the mapped headers from the API Gateway request.
    /// </summary>
    /// <param name="context">The API Gateway context to extract headers from.</param>
    /// <returns>The request's headers, with mapped header names replaced according to <see cref="IHttpHeaderMappings"/>.</returns>
    public IDictionary<string, string> GetHeaders(ApiGatewayContext context)
    {
        return DictionaryUtils.Replace(context.ApiGatewayProxyRequest.Headers, _httpHeaderMappings.GetMappings());
    }
}
