using System.Collections.Generic;
using Benzene.Abstractions.Mappers;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Core.Helper;
using Benzene.Http;
using Benzene.Results;

namespace Benzene.Aws.ApiGateway;

public class ApiGatewayMessageHeadersMapper : IMessageHeadersMapper<ApiGatewayContext>
{
    private readonly IHttpHeaderMappings _httpHeaderMappings;

    public ApiGatewayMessageHeadersMapper(IHttpHeaderMappings httpHeaderMappings)
    {
        _httpHeaderMappings = httpHeaderMappings;
    }

    public IDictionary<string, string> GetHeaders(ApiGatewayContext context)
    {
        return DictionaryUtils.Replace(context.ApiGatewayProxyRequest.Headers, _httpHeaderMappings.GetMappings());
    }
}