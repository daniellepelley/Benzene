using Benzene.Abstractions.Response;
using Benzene.Core.MessageHandlers;

namespace Benzene.Aws.ApiGateway;

public class ApiGatewayMessageResultSetter: ResponseMessageResultSetterBase<ApiGatewayContext>
{
    public ApiGatewayMessageResultSetter(IResponseHandlerContainer<ApiGatewayContext> responseHandlerContainer)
        : base(responseHandlerContainer)
    {
    }
}