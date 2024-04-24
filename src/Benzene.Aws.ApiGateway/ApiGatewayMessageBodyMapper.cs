using Benzene.Abstractions.Mappers;

namespace Benzene.Aws.ApiGateway;

public class ApiGatewayMessageBodyMapper : IMessageBodyMapper<ApiGatewayContext>
{
    public string GetBody(ApiGatewayContext context)
    {
        return context.ApiGatewayProxyRequest.Body;
    }
}
