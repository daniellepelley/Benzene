using System;
using Amazon.Lambda.APIGatewayEvents;
using Benzene.Aws.Lambda.ApiGateway;
using Xunit;

namespace Benzene.Test.Aws.ApiGateway;

/// <summary>
/// Covers the binary-response half of #25(B): the API Gateway response adapters (v1 and v2) base64-
/// encode a raw byte body and flag the response <c>IsBase64Encoded</c>, so API Gateway decodes it back
/// to bytes on the way out. A normal string body leaves <c>IsBase64Encoded</c> unset.
/// </summary>
public class ApiGatewayBinaryResponseTest
{
    private static readonly byte[] BinaryBody = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0xFF };

    [Fact]
    public void V1_SetBodyBytes_Base64EncodesAndFlagsIsBase64Encoded()
    {
        var context = new ApiGatewayContext(new APIGatewayProxyRequest());

        new ApiGatewayResponseAdapter().SetBody(context, BinaryBody);

        Assert.Equal(Convert.ToBase64String(BinaryBody), context.ApiGatewayProxyResponse.Body);
        Assert.True(context.ApiGatewayProxyResponse.IsBase64Encoded);
    }

    [Fact]
    public void V1_SetBodyString_LeavesIsBase64EncodedUnset()
    {
        var context = new ApiGatewayContext(new APIGatewayProxyRequest());

        new ApiGatewayResponseAdapter().SetBody(context, "{\"ok\":true}");

        Assert.Equal("{\"ok\":true}", context.ApiGatewayProxyResponse.Body);
        Assert.False(context.ApiGatewayProxyResponse.IsBase64Encoded);
    }

    [Fact]
    public void V2_SetBodyBytes_Base64EncodesAndFlagsIsBase64Encoded()
    {
        var context = new ApiGatewayV2Context(new APIGatewayHttpApiV2ProxyRequest());

        new ApiGatewayV2ResponseAdapter().SetBody(context, BinaryBody);

        Assert.Equal(Convert.ToBase64String(BinaryBody), context.ApiGatewayProxyResponse.Body);
        Assert.True(context.ApiGatewayProxyResponse.IsBase64Encoded);
    }

    [Fact]
    public void V2_SetBodyString_LeavesIsBase64EncodedUnset()
    {
        var context = new ApiGatewayV2Context(new APIGatewayHttpApiV2ProxyRequest());

        new ApiGatewayV2ResponseAdapter().SetBody(context, "{\"ok\":true}");

        Assert.Equal("{\"ok\":true}", context.ApiGatewayProxyResponse.Body);
        Assert.False(context.ApiGatewayProxyResponse.IsBase64Encoded);
    }
}
