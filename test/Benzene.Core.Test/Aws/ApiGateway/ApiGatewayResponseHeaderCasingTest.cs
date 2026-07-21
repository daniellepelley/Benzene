using Amazon.Lambda.APIGatewayEvents;
using Benzene.Aws.Lambda.ApiGateway;
using Xunit;

namespace Benzene.Test.Aws.ApiGateway;

/// <summary>
/// The response header dictionary is case-insensitive, so a handler that sets <c>Content-Type</c>
/// updates the same entry the framework writes as the lowercase <c>content-type</c> (see
/// <see cref="Constants.ContentTypeHeader"/>) instead of producing a duplicate header that API
/// Gateway would emit twice. Covers both the v1 and v2 response adapters.
/// </summary>
public class ApiGatewayResponseHeaderCasingTest
{
    [Fact]
    public void V1_ContentTypeSetInBothCasings_CollapsesToSingleHeader()
    {
        var context = new ApiGatewayContext(new APIGatewayProxyRequest());
        var adapter = new ApiGatewayResponseAdapter();

        adapter.SetContentType(context, "application/json");
        adapter.SetResponseHeader(context, "Content-Type", "text/plain");

        Assert.Single(context.ApiGatewayProxyResponse.Headers);
        Assert.Equal("text/plain", context.ApiGatewayProxyResponse.Headers["content-type"]);
    }

    [Fact]
    public void V2_ContentTypeSetInBothCasings_CollapsesToSingleHeader()
    {
        var context = new ApiGatewayV2Context(new APIGatewayHttpApiV2ProxyRequest());
        var adapter = new ApiGatewayV2ResponseAdapter();

        adapter.SetContentType(context, "application/json");
        adapter.SetResponseHeader(context, "Content-Type", "text/plain");

        Assert.Single(context.ApiGatewayProxyResponse.Headers);
        Assert.Equal("text/plain", context.ApiGatewayProxyResponse.Headers["content-type"]);
    }
}
