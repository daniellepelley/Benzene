using Benzene.Azure.Function.AspNet;
using Benzene.Azure.Function.AspNet.TestHelpers;
using Benzene.Azure.Function.Core;
using Benzene.Azure.Function.Core.TestHelpers;
using Benzene.Testing;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BenzeneStarter.Tests;

// A component test: it boots the SAME app your StartUp configures for a real deployment - both
// ConfigureServices and Configure run - then sends an HTTP request through the whole Benzene pipeline.
// Only the Azure Functions HTTP trigger is simulated. WithServices/WithConfiguration are the seams for
// overriding any dependency or setting the test needs. This handler returns a response, so the test
// asserts on it directly.
public class HelloWorldMessageHandlerTests
{
    private static IAzureFunctionApp BuildApp() =>
        BenzeneTestHost.Create<StartUp>()
            .WithConfiguration("Example:Setting", "test-value")
            .BuildAzureFunctionApp();

    [Fact]
    public async Task GET_hello_returns_a_greeting()
    {
        var app = BuildApp();
        // The name comes from the {name} route value; the body is an empty object (not null) so the
        // request object exists for the route value to bind onto.
        var request = HttpBuilder.Create<object>("GET", "/hello/world", new object()).AsAspNetCoreHttpRequest();

        var response = await app.HandleHttpRequest(request) as ContentResult;

        Assert.NotNull(response);
        Assert.Equal(200, response!.StatusCode);
        Assert.Contains("Hello world!", response.Content);
    }

    [Fact]
    public async Task An_unmapped_route_returns_NotFound()
    {
        var app = BuildApp();
        var request = HttpBuilder.Create<object>("GET", "/does-not-exist").AsAspNetCoreHttpRequest();

        var response = await app.HandleHttpRequest(request) as ContentResult;

        Assert.NotNull(response);
        Assert.Equal(404, response!.StatusCode);
    }
}
