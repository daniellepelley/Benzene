using System.Threading.Tasks;
using Benzene.Aws.ApiGateway;
using Benzene.Aws.Core;
using Benzene.Core.MiddlewareBuilder;
using Benzene.Http.Cors;
using Benzene.Test.Examples;
using Benzene.Tools;
using Benzene.Tools.Aws;
using Xunit;

namespace Benzene.Test.Aws.ApiGateway;

public class ApiGatewayMessageCorsTest
{
    private AwsLambdaBenzeneTestHost _host;

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
                        }
                    })
                    .UseProcessResponse()
                    .UseMessageRouter()
                )
            ).BuildHost();
    }


    [Fact]
    public async Task Option()
    {
        var request = HttpBuilder.Create("OPTIONS", "/example?key=value")
            .WithHeader("origin", "example.com");
        var response = await _host.SendApiGatewayAsync(request);

        Assert.NotNull(response);
        Assert.Null(response.Body);
        Assert.Equal(200, response.StatusCode);

        Assert.Equal("example.com", response.Headers["access-control-allow-origin"]);
        Assert.Equal("X-Query-Id,X-Tenant-Id,Authorization,Content-Type,X-Api-Key", response.Headers["access-control-allow-headers"]);
        Assert.Equal("OPTIONS,GET", response.Headers["access-control-allow-methods"]);
    }


    [Fact]
    public async Task Option_UnknownOrigin()
    {
        var request = HttpBuilder.Create("OPTIONS", "/example")
            .WithHeader("origin", "unknown.com");
        var response = await _host.SendApiGatewayAsync(request);

        Assert.NotNull(response);
        Assert.Null(response.Body);
        Assert.Equal(200, response.StatusCode);

        Assert.False(response.Headers.ContainsKey("access-control-allow-origin"));
        Assert.False(response.Headers.ContainsKey("access-control-allow-headers"));
        Assert.False(response.Headers.ContainsKey("access-control-allow-methods"));
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

        Assert.Equal("example.com", response.Headers["access-control-allow-origin"]);
        Assert.Equal("X-Query-Id,X-Tenant-Id,Authorization,Content-Type,X-Api-Key", response.Headers["access-control-allow-headers"]);
        Assert.Equal("OPTIONS,GET", response.Headers["access-control-allow-methods"]);
    }

    [Fact]
    public async Task Send_NoOrigin()
    {
        var request = HttpBuilder.Create("GET", "/example", Defaults.MessageAsObject);

        var response = await _host.SendApiGatewayAsync(request);

        Assert.NotNull(response);
        Assert.NotNull(response.Body);
        Assert.Equal(200, response.StatusCode);

        Assert.False(response.Headers.ContainsKey("access-control-allow-origin"));
        Assert.False(response.Headers.ContainsKey("access-control-allow-headers"));
        Assert.False(response.Headers.ContainsKey("access-control-allow-methods"));
    }

}
