using System;
using System.Text;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Aws.Lambda.ApiGateway;

/// <summary>
/// Byte-oriented companion to <see cref="ApiGatewayV2MessageBodyGetter"/>: returns the request body's
/// raw bytes, decoding base64 when API Gateway flagged
/// <see cref="Amazon.Lambda.APIGatewayEvents.APIGatewayHttpApiV2ProxyRequest.IsBase64Encoded"/> —
/// unlike the string getter, without the lossy UTF-8 round-trip, so a true binary upload reaches a
/// raw-bytes handler byte-identical.
/// </summary>
public class ApiGatewayV2MessageBodyBytesGetter : IMessageBodyBytesGetter<ApiGatewayV2Context>
{
    /// <summary>
    /// Gets the request body's raw bytes: the base64-decoded bytes when the request is
    /// <c>IsBase64Encoded</c>, otherwise the UTF-8 bytes of the text body.
    /// </summary>
    /// <param name="context">The API Gateway v2 context to extract the body bytes from.</param>
    /// <returns>The raw request body bytes, or empty when there is no body.</returns>
    public ReadOnlyMemory<byte> GetBodyBytes(ApiGatewayV2Context context)
    {
        var request = context.ApiGatewayProxyRequest;
        if (string.IsNullOrEmpty(request.Body))
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        return request.IsBase64Encoded
            ? Convert.FromBase64String(request.Body)
            : Encoding.UTF8.GetBytes(request.Body);
    }
}
