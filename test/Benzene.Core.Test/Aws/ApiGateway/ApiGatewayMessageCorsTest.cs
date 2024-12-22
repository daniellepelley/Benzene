using System.Threading.Tasks;
using Benzene.Aws.ApiGateway;
using Benzene.Aws.Core;
using Benzene.Core.MessageHandling;
using Benzene.Http.Cors;
using Benzene.Test.Examples;
using Benzene.Tools;
using Benzene.Tools.Aws;
using Xunit;

namespace Benzene.Test.Aws.ApiGateway;

public class ApiGatewayMessageCorsTest
{
    private readonly AwsLambdaBenzeneTestHost _host;
    private const string AccessControlAllowHeaders = "Access-Control-Allow-Headers";
    private const string AccessControlAllowMethods = "Access-Control-Allow-Methods";
    private const string AccessControlAllowOrigin = "Access-Control-Allow-Origin";

    public ApiGatewayMessageCorsTest()
    {
        _host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
            )
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway
                    .UseProcessResponse()
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
        Assert.Null(response.Body);
        Assert.Equal(200, response.StatusCode);

        Assert.Equal("https://example.com", response.Headers[AccessControlAllowOrigin]);
        Assert.Equal("X-Query-Id,X-Tenant-Id,Authorization,Content-Type,X-Api-Key", response.Headers[AccessControlAllowHeaders]);
        Assert.Equal("OPTIONS,GET", response.Headers[AccessControlAllowMethods]);
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

        Assert.False(response.Headers.ContainsKey(AccessControlAllowOrigin));
        Assert.False(response.Headers.ContainsKey(AccessControlAllowHeaders));
        Assert.False(response.Headers.ContainsKey(AccessControlAllowMethods));
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
    }
}
