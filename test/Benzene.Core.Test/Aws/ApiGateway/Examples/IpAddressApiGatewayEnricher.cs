using System.Collections.Generic;
using Benzene.Abstractions.MessageHandlers.Request;
using Benzene.Aws.Lambda.ApiGateway;

namespace Benzene.Test.Aws.ApiGateway.Examples;

public class IpAddressApiGatewayEnricher : IRequestEnricher<ApiGatewayContext>
{
    public IDictionary<string, object> Enrich<TRequest>(TRequest request, ApiGatewayContext context)
    {
        return new Dictionary<string, object>
        {
            { "ipaddress", context.ApiGatewayProxyRequest?.RequestContext?.Identity?.SourceIp }
        };
    }
}
