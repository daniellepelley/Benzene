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
    private const string AccessControlExposeHeaders = "Access-Control-Expose-Headers";
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
                        ExposedHeaders = new[]
                        {
                            "X-Total-Count"
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
        Assert.Equal("X-Total-Count", response.Headers[AccessControlExposeHeaders]);
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

    [Fact]
    public async Task Option_DisallowedRequestedHeader()
    {
        var request = HttpBuilder.Create("OPTIONS", "/example")
            .WithHeader("origin", "https://example.com")
            .WithHeader("access-control-request-headers", "X-Query-Id, X-Not-Allowed");
        var response = await _host.SendApiGatewayAsync(request);

        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);

        // A requested header outside the configured allow-list fails the preflight, just like
        // ASP.NET Core's CorsService - the browser will then block the actual request since no
        // CORS headers were returned.
        Assert.False(response.Headers.ContainsKey(AccessControlAllowOrigin));
        Assert.False(response.Headers.ContainsKey(AccessControlAllowHeaders));
        Assert.False(response.Headers.ContainsKey(AccessControlAllowMethods));
    }

    [Fact]
    public async Task Option_AllowedRequestedHeaders_Succeeds()
    {
        var request = HttpBuilder.Create("OPTIONS", "/example")
            .WithHeader("origin", "https://example.com")
            .WithHeader("access-control-request-headers", "X-Query-Id, Authorization");
        var response = await _host.SendApiGatewayAsync(request);

        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.Equal("https://example.com", response.Headers[AccessControlAllowOrigin]);
    }
}

public class ApiGatewayMessageCorsWildcardHeadersTest
{
    private const string AccessControlAllowHeaders = "Access-Control-Allow-Headers";
    private const string AccessControlAllowOrigin = "Access-Control-Allow-Origin";
    private readonly AwsLambdaBenzeneTestHost _host;

    public ApiGatewayMessageCorsWildcardHeadersTest()
    {
        _host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
            )
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway
                    .UseCors(new CorsSettings
                    {
                        AllowedDomains = new[] { "example.com" },
                        AllowedHeaders = new[] { "*" }
                    })
                    .UseMessageHandlers()
                )
            ).BuildHost();
    }

    [Fact]
    public async Task Option_EchoesRequestedHeaders()
    {
        var request = HttpBuilder.Create("OPTIONS", "/example")
            .WithHeader("origin", "https://example.com")
            .WithHeader("access-control-request-headers", "X-Anything, X-Something-Else");
        var response = await _host.SendApiGatewayAsync(request);

        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.Equal("https://example.com", response.Headers[AccessControlAllowOrigin]);
        // Wildcard AllowedHeaders echoes back exactly what was requested rather than a literal
        // "*", since browsers don't honor a literal "*" on credentialed requests.
        Assert.Equal("X-Anything, X-Something-Else", response.Headers[AccessControlAllowHeaders]);
    }
}
