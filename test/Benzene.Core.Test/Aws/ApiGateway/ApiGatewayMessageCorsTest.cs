using System.Threading.Tasks;
using Benzene.Aws.Lambda.ApiGateway;
using Benzene.Aws.Lambda.ApiGateway.TestHelpers;
using Benzene.Aws.Lambda.Core;
using Benzene.Core.MessageHandlers;
using Benzene.Http.Cors;
using Benzene.Test.Examples;
using Benzene.Testing;
using Benzene.Aws.Lambda.Core.TestHelpers;
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
        // A real CORS preflight carries Access-Control-Request-Method (Fetch standard); only then are
        // the Allow-Methods/Allow-Headers/Max-Age response headers emitted.
        var request = HttpBuilder.Create("OPTIONS", "/example?key=value")
            .WithHeader("origin", "https://example.com")
            .WithHeader("access-control-request-method", "GET");
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
    public async Task Option_BareOptionsWithoutRequestMethod_IsNotTreatedAsAPreflight()
    {
        // Without Access-Control-Request-Method this is not a preflight, so the preflight-only
        // headers (Allow-Methods/Allow-Headers/Max-Age) must NOT be emitted. The origin is still
        // acknowledged (Allow-Origin) and the response varies by Origin.
        var request = HttpBuilder.Create("OPTIONS", "/example")
            .WithHeader("origin", "https://example.com");
        var response = await _host.SendApiGatewayAsync(request);

        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);

        Assert.Equal("https://example.com", response.Headers[AccessControlAllowOrigin]);
        Assert.False(response.Headers.ContainsKey(AccessControlAllowMethods));
        Assert.False(response.Headers.ContainsKey(AccessControlAllowHeaders));
        Assert.False(response.Headers.ContainsKey(AccessControlMaxAge));
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
        Assert.Equal("true", response.Headers[AccessControlAllowCredentials]);
        Assert.Equal("X-Total-Count", response.Headers[AccessControlExposeHeaders]);
        Assert.Equal("Origin", response.Headers[Vary]);
        // Access-Control-Allow-Methods / Allow-Headers / Max-Age are preflight-only response headers
        // (the browser ignores them on an actual response), so they must NOT appear on a real request.
        Assert.False(response.Headers.ContainsKey(AccessControlAllowMethods));
        Assert.False(response.Headers.ContainsKey(AccessControlAllowHeaders));
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
            .WithHeader("access-control-request-method", "GET")
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
            .WithHeader("access-control-request-method", "GET")
            .WithHeader("access-control-request-headers", "X-Query-Id, Authorization");
        var response = await _host.SendApiGatewayAsync(request);

        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        Assert.Equal("https://example.com", response.Headers[AccessControlAllowOrigin]);
    }
}

public class ApiGatewayMessageCorsWildcardOriginCredentialsTest
{
    private const string AccessControlAllowOrigin = "Access-Control-Allow-Origin";
    private const string AccessControlAllowCredentials = "Access-Control-Allow-Credentials";
    private readonly AwsLambdaBenzeneTestHost _host;

    public ApiGatewayMessageCorsWildcardOriginCredentialsTest()
    {
        _host = new InlineAwsLambdaStartUp()
            .ConfigureServices(services => services
                .ConfigureServiceCollection()
            )
            .Configure(app => app
                .UseApiGateway(apiGateway => apiGateway
                    .UseCors(new CorsSettings
                    {
                        // Any-origin plus credentials is the dangerous combination.
                        AllowedDomains = new[] { "*" },
                        AllowCredentials = true
                    })
                    .UseMessageHandlers()
                )
            ).BuildHost();
    }

    [Fact]
    public async Task WildcardOrigin_WithCredentials_ReflectsOriginButOmitsCredentialsHeader()
    {
        var request = HttpBuilder.Create("OPTIONS", "/example")
            .WithHeader("origin", "https://attacker.example")
            .WithHeader("access-control-request-method", "GET");
        var response = await _host.SendApiGatewayAsync(request);

        Assert.NotNull(response);
        Assert.Equal(200, response.StatusCode);
        // The origin is still echoed (any-origin is allowed), but the credentials header is refused:
        // reflecting an arbitrary origin *with* Access-Control-Allow-Credentials: true is the
        // origin-reflection hole that ASP.NET Core forbids. A specific allow-list keeps credentials.
        Assert.Equal("https://attacker.example", response.Headers[AccessControlAllowOrigin]);
        Assert.False(response.Headers.ContainsKey(AccessControlAllowCredentials));
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
            .WithHeader("access-control-request-method", "GET")
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
