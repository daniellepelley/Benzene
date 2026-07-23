using Benzene.Aws.Lambda.ApiGateway.TestHelpers;
using Benzene.Aws.Lambda.Core.TestHelpers;
using Benzene.Testing;
using Xunit;

namespace BenzeneStarter.Tests;

// A component test: it boots the SAME app your StartUp configures for a real deployment - both
// ConfigureServices and Configure run - then sends an HTTP request through the whole Benzene pipeline.
// Only the API Gateway trigger is simulated. WithServices/WithConfiguration are the seams for
// overriding any dependency or setting the test needs. This handler returns a response, so the test
// asserts on it directly.
public class HelloWorldMessageHandlerTests
{
    private static AwsLambdaBenzeneTestHost BuildHost() =>
        new(BenzeneTestHost.Create<StartUp>()
            .WithConfiguration("Example:Setting", "test-value")
            .BuildAwsLambdaHost());

    [Fact]
    public async Task GET_hello_returns_a_greeting()
    {
        using var host = BuildHost();

        var response = await host.SendApiGatewayAsync(HttpBuilder.Create<object>("GET", "/hello/World"));

        Assert.Equal(200, response.StatusCode);
        Assert.Contains("Hello World!", response.Body);
    }

    [Fact]
    public async Task An_unmapped_route_returns_NotFound()
    {
        using var host = BuildHost();

        var response = await host.SendApiGatewayAsync(HttpBuilder.Create<object>("GET", "/does-not-exist"));

        Assert.Equal(404, response.StatusCode);
    }
}
