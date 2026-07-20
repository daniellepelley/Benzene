using Benzene.Abstractions.MessageHandlers.Response;
using Benzene.Core.MessageHandlers;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Sets the message handler result onto an <see cref="ApiGatewayV2Context"/>'s response, running the
/// registered <see cref="IResponseHandler{TContext}"/> chain (status code, JSON body, etc.).
/// </summary>
public class ApiGatewayV2MessageHandlerResultSetter : ResponseMessageHandlerResultSetterBase<ApiGatewayV2Context>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiGatewayV2MessageHandlerResultSetter"/> class.
    /// </summary>
    /// <param name="responseHandlerContainer">The container of response handlers to run for API Gateway v2 contexts.</param>
    public ApiGatewayV2MessageHandlerResultSetter(IResponseHandlerContainer<ApiGatewayV2Context> responseHandlerContainer)
        : base(responseHandlerContainer)
    {
    }
}
