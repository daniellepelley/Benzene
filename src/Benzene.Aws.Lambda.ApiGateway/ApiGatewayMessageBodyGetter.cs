using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.ApiGateway;

public class ApiGatewayMessageBodyGetter : IMessageBodyGetter<ApiGatewayContext>
{
    public string GetBody(ApiGatewayContext context)
    {
        return context.ApiGatewayProxyRequest.Body;
    }
}
