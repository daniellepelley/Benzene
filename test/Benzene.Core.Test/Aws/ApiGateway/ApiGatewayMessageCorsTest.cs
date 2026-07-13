using System.Threading.Tasks;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.ApiGateway.TestHelpers;
using Benzene.Aws.Lambda.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Http.Cors;
using Benzene.Test.Examples;
using Benzene.Testing;
using Benzene.Tools.Aws;
using Xunit;

namespace Benzene.Test.Aws.ApiGateway;

public class ApiGatewayMessageCorsTest
{
    private readonly AwsLambdaBenzeneTestHost _host;
    private const string AccessControlAllowHeaders = "Access-Control-Allow-Headers";
    private const string AccessControlAllowMethods = "Access-Control-Allow-Methods";
    private const string AccessControlAllowOrigin = "Access-Control-Allow-Origin";
    private const string AccessControlAllowCredentials = "Access-Control-Allow-Credentials";
    private const string AccessControlMaxAge = "Access-Control-Max-Age";
    private const string Vary = "Vary";

    public ApiGatewayMessageCorsTest()
    {
        _host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
            )
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway
                    .UseCors(new CorsSettings
                    {
                        AllowedDomains = new[]
                        {
                            "example.com"
                        },
                        AllowedHeaders = new[]
                        {
                            "X-Query-Id","X-Tenant-Id","Authorization","Content-Type","X-Api-Key"
                        },
                        AllowCredentials = true,
                        MaxAgeSeconds = 600
                    })
                    .UseMessageHandlers()
                )
            ).BuildHost();
    }


    [Fact]
    public async Task Option()
    {
        var request = HttpBuilder.Create("OPTIONS", "/example?key=value")
            .WithHeader("origin", "https://example.com");
        var response = await _host.SendApiGatewayAsync(request);

        Assert.NotNull(response);
        // Assert.Null(response.Body);
        Assert.Equal(200, response.StatusCode);

        Assert.Equal("https://example.com", response.Headers[AccessControlAllowOrigin]);
        Assert.Equal("X-Query-Id,X-Tenant-Id,Authorization,Content-Type,X-Api-Key", response.Headers[AccessControlAllowHeaders]);
        Assert.Equal("OPTIONS,GET", response.Headers[AccessControlAllowMethods]);
        Assert.Equal("true", response.Headers[AccessControlAllowCredentials]);
        Assert.Equal("600", response.Headers[AccessControlMaxAge]);
        Assert.Equal("Origin", response.Headers[Vary]);
    }


    [Fact]
    public async Task Option_UnknownOrigin()
    {
        var request = HttpBuilder.Create("OPTIONS", "/example")
            .WithHeader("origin", "unknown.com");
        var response = await _host.SendApiGatewayAsync(request);

        Assert.NotNull(response);
        // Assert.Null(response.Body);
        Assert.Equal(200, response.StatusCode);

        Assert.False(response.Headers.ContainsKey(AccessControlAllowOrigin));
        Assert.False(response.Headers.ContainsKey(AccessControlAllowHeaders));
        Assert.False(response.Headers.ContainsKey(AccessControlAllowMethods));
        Assert.False(response.Headers.ContainsKey(AccessControlAllowCredentials));
        Assert.False(response.Headers.ContainsKey(AccessControlMaxAge));
        // Still varies by Origin: an unknown origin today could be an allowed one tomorrow,
        // and a cache must not serve this rejection to a different, allowed origin.
        Assert.Equal("Origin", response.Headers[Vary]);
    }

    [Fact]
    public async Task Send()
    {
        var request = HttpBuilder.Create("GET", "/example", Defaults.MessageAsObject)
            .WithHeader("origin", "example.com");

        var response = await _host.SendApiGatewayAsync(request);

        Assert.NotNull(response);
        Assert.NotNull(response.Body);
        Assert.Equal(200, response.StatusCode);

        Assert.Equal("example.com", response.Headers[AccessControlAllowOrigin]);
        Assert.Equal("X-Query-Id,X-Tenant-Id,Authorization,Content-Type,X-Api-Key", response.Headers[AccessControlAllowHeaders]);
        Assert.Equal("OPTIONS,GET", response.Headers[AccessControlAllowMethods]);
        Assert.Equal("true", response.Headers[AccessControlAllowCredentials]);
        Assert.Equal("Origin", response.Headers[Vary]);
        // Access-Control-Max-Age is only meaningful on preflight (OPTIONS) responses.
        Assert.False(response.Headers.ContainsKey(AccessControlMaxAge));
    }

    [Fact]
    public async Task Send_NoOrigin()
    {
        var request = HttpBuilder.Create("GET", "/example", Defaults.MessageAsObject);

        var response = await _host.SendApiGatewayAsync(request);

        Assert.NotNull(response);
        Assert.NotNull(response.Body);
        Assert.Equal(200, response.StatusCode);

        Assert.False(response.Headers.ContainsKey(AccessControlAllowOrigin));
        Assert.False(response.Headers.ContainsKey(AccessControlAllowHeaders));
        Assert.False(response.Headers.ContainsKey(AccessControlAllowMethods));
        Assert.False(response.Headers.ContainsKey(Vary));
    }
}
