using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.MessageHandlers;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Sets the message handler result onto an <see cref="ApiGatewayContext"/>'s response, running the
/// registered <see cref="IResponseHandler{TContext}"/> chain (status code, JSON body, etc.).
/// </summary>
public class ApiGatewayMessageHandlerResultSetter: ResponseMessageHandlerResultSetterBase<ApiGatewayContext>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiGatewayMessageHandlerResultSetter"/> class.
    /// </summary>
    /// <param name="responseHandlerContainer">The container of response handlers to run for API Gateway contexts.</param>
    public ApiGatewayMessageHandlerResultSetter(IResponseHandlerContainer<ApiGatewayContext> responseHandlerContainer)
        : base(responseHandlerContainer)
    {
    }
}
