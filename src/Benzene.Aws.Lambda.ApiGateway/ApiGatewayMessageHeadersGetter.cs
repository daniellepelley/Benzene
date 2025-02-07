using System.Collections.Generic;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.Helper;
using Benzene.Http;

namespace Benzene.Aws.Lambda.ApiGateway;

public class ApiGatewayMessageHeadersGetter : IMessageHeadersGetter<ApiGatewayContext>
{
    private readonly IHttpHeaderMappings _httpHeaderMappings;

    public ApiGatewayMessageHeadersGetter(IHttpHeaderMappings httpHeaderMappings)
    {
        _httpHeaderMappings = httpHeaderMappings;
    }

    public IDictionary<string, string> GetHeaders(ApiGatewayContext context)
    {
        return DictionaryUtils.Replace(context.ApiGatewayProxyRequest.Headers, _httpHeaderMappings.GetMappings());
    }
}