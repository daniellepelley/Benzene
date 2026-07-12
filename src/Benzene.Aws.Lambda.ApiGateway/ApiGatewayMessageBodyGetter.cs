using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Extracts the raw body string from an API Gateway request.
/// </summary>
public class ApiGatewayMessageBodyGetter : IMessageBodyGetter<ApiGatewayContext>
{
    /// <summary>
    /// Gets the raw body from the API Gateway request.
    /// </summary>
    /// <param name="context">The API Gateway context to extract the body from.</param>
    /// <returns>The request body.</returns>
    public string GetBody(ApiGatewayContext context)
    {
        return context.ApiGatewayProxyRequest.Body;
    }
}
