using System;
using System.Text;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Core.MessageHandlers.Request;
using Benzene.Core.MessageHandlers.Serialization;
using Benzene.Core.Messages;
using Xunit;

namespace Benzene.Test.Aws.ApiGateway;

/// <summary>
/// Covers the binary-request half of #25(B): the API Gateway body-bytes getters (v1 and v2) return a
/// base64-encoded request body's real bytes (no lossy UTF-8 round-trip), and a <see cref="RawBytesRequest"/>
/// handler receives those bytes verbatim through the request mapper.
/// </summary>
public class ApiGatewayBinaryRequestTest
{
    private static readonly byte[] BinaryBody = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x00, 0xFF, 0x1A, 0x0A };

    [Fact]
    public void V1_BytesGetter_Base64Body_ReturnsDecodedBytes()
    {
        var request = new APIGatewayProxyRequest { Body = Convert.ToBase64String(BinaryBody), IsBase64Encoded = true };
        var bytes = new ApiGatewayMessageBodyBytesGetter().GetBodyBytes(new ApiGatewayContext(request));

        Assert.Equal(BinaryBody, bytes.ToArray());
    }

    [Fact]
    public void V1_BytesGetter_TextBody_ReturnsUtf8Bytes()
    {
        var request = new APIGatewayProxyRequest { Body = "{\"a\":1}", IsBase64Encoded = false };
        var bytes = new ApiGatewayMessageBodyBytesGetter().GetBodyBytes(new ApiGatewayContext(request));

        Assert.Equal(Encoding.UTF8.GetBytes("{\"a\":1}"), bytes.ToArray());
    }

    [Fact]
    public void V1_BytesGetter_EmptyBody_ReturnsEmpty()
    {
        var bytes = new ApiGatewayMessageBodyBytesGetter().GetBodyBytes(new ApiGatewayContext(new APIGatewayProxyRequest()));
        Assert.True(bytes.IsEmpty);
    }

    [Fact]
    public void V2_BytesGetter_Base64Body_ReturnsDecodedBytes()
    {
        var request = new APIGatewayHttpApiV2ProxyRequest { Body = Convert.ToBase64String(BinaryBody), IsBase64Encoded = true };
        var bytes = new ApiGatewayV2MessageBodyBytesGetter().GetBodyBytes(new ApiGatewayV2Context(request));

        Assert.Equal(BinaryBody, bytes.ToArray());
    }

    [Fact]
    public void V1_RawBytesRequestHandler_ReceivesTheDecodedBinaryBody_ThroughTheRequestMapper()
    {
        // The full v1 request-side vertical: a base64 binary body → the bytes getter decodes it → the
        // request mapper hands a RawBytesRequest carrying the exact bytes (no deserialization).
        var request = new APIGatewayProxyRequest { Body = Convert.ToBase64String(BinaryBody), IsBase64Encoded = true };
        var mapper = new RequestMapper<ApiGatewayContext>(
            new ApiGatewayMessageBodyGetter(), new JsonSerializer(), new ApiGatewayMessageBodyBytesGetter());

        var mapped = mapper.GetBody<RawBytesRequest>(new ApiGatewayContext(request));

        Assert.NotNull(mapped);
        Assert.Equal(BinaryBody, mapped.Content.ToArray());
    }

    [Fact]
    public void V2_RawBytesRequestHandler_ReceivesTheDecodedBinaryBody_ThroughTheRequestMapper()
    {
        var request = new APIGatewayHttpApiV2ProxyRequest { Body = Convert.ToBase64String(BinaryBody), IsBase64Encoded = true };
        var mapper = new RequestMapper<ApiGatewayV2Context>(
            new ApiGatewayV2MessageBodyGetter(), new JsonSerializer(), new ApiGatewayV2MessageBodyBytesGetter());

        var mapped = mapper.GetBody<RawBytesRequest>(new ApiGatewayV2Context(request));

        Assert.NotNull(mapped);
        Assert.Equal(BinaryBody, mapped.Content.ToArray());
    }
}
