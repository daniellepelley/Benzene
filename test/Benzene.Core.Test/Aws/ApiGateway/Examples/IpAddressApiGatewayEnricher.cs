using System.Collections.Generic;
using Benzene.Abstractions.Request;
using Benzene.Aws.ApiGateway;

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
