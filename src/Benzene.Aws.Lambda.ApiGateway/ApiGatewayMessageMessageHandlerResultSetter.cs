using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.MessageHandlers;

namespace Benzene.Aws.Lambda.ApiGateway;

public class ApiGatewayMessageMessageHandlerResultSetter: ResponseMessageMessageHandlerResultSetterBase<ApiGatewayContext>
{
    public ApiGatewayMessageMessageHandlerResultSetter(IResponseHandlerContainer<ApiGatewayContext> responseHandlerContainer)
        : base(responseHandlerContainer)
    {
    }
}