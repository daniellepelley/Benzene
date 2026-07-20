using System;
using System.Text;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Extracts the body string from an API Gateway HTTP API v2 request, decoding it when API Gateway
/// base64-encoded it.
/// </summary>
public class ApiGatewayV2MessageBodyGetter : IMessageBodyGetter<ApiGatewayV2Context>
{
    /// <summary>
    /// Gets the body from the API Gateway v2 request. When the request is flagged
    /// <see cref="Amazon.Lambda.APIGatewayEvents.APIGatewayHttpApiV2ProxyRequest.IsBase64Encoded"/>
    /// (API Gateway base64-encodes the body for binary media types, or any payload it can't treat as
    /// text), the body is decoded back to its real text so the handler never sees base64; a normal text
    /// body is returned unchanged.
    /// </summary>
    /// <param name="context">The API Gateway v2 context to extract the body from.</param>
    /// <returns>The request body, base64-decoded if it was base64-encoded.</returns>
    public string GetBody(ApiGatewayV2Context context)
    {
        var request = context.ApiGatewayProxyRequest;
        if (request.IsBase64Encoded && !string.IsNullOrEmpty(request.Body))
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(request.Body));
        }

        return request.Body;
    }
}
