using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Benzene.AspNet.Core;
using Benzene.Auth.OAuth2;
using Benzene.Core.MessageHandlers;
using Benzene.Test.Examples;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Auth;

/// <summary>
/// Exercises <c>RequireScope</c> chained after <c>UseOAuth2Bearer</c> in one real Kestrel-hosted
/// pipeline - the realistic composition shown in the auth-patterns cookbook. GET
/// <c>/example</c> (<see cref="ExampleMessageHandler"/>) is the scope-protected downstream route.
/// </summary>
public class RequireScopeTest
{
    private const string Issuer = "https://issuer.example.com";
    private const string Audience = "my-api";

    private static async Task<(WebApplication App, Uri BaseAddress)> StartHostAsync(FakeJwksServer jwks, params string[] requiredScopes)
    {
        var options = new OAuth2BearerOptions
        {
            JwksUri = jwks.JwksUri,
            ValidIssuers = new[] { Issuer },
            ValidAudiences = new[] { Audience },
            ValidAlgorithms = new[] { "RS256" },
            RequireHttpsMetadata = false
        };

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers();
        builder.Services.ConfigureServiceCollection();

        var app = builder.Build();
        app.UseRouting();
        app.UseBenzene(benzene => benzene
            .UseHttp(asp => asp
                .UseOAuth2Bearer(options)
                .RequireScope(requiredScopes)
                .UseMessageHandlers()
            )
        );
        app.UseEndpoints(_ => { });

        await app.StartAsync();
        return (app, new Uri(app.Urls.First()));
    }

    private static async Task<System.Net.HttpStatusCode> SendWithTokenAsync(
        Uri baseAddress, string token)
    {
        using var client = new HttpClient { BaseAddress = baseAddress };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(Defaults.Path);
        return response.StatusCode;
    }

    [Fact]
    public async Task NoAuthorizationHeaderAtAll_IsUnauthorized_NotForbidden()
    {
        using var jwks = new FakeJwksServer();
        jwks.AddKey("kid1");
        var (app, baseAddress) = await StartHostAsync(jwks, "admin");
        try
        {
            using var client = new HttpClient { BaseAddress = baseAddress };
            var response = await client.GetAsync(Defaults.Path);

            // No principal at all - the OAuth2 middleware itself already rejects this before
            // RequireScope even runs, and either way it must be Unauthorized, never Forbidden.
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task ValidTokenMissingRequiredScope_IsForbidden()
    {
        using var jwks = new FakeJwksServer();
        var key = jwks.AddKey("kid1");
        var (app, baseAddress) = await StartHostAsync(jwks, "admin");
        try
        {
            var token = FakeJwksServer.CreateToken(key, "kid1", Issuer, Audience,
                extraClaims: new Dictionary<string, object> { ["scope"] = "read write" });

            var status = await SendWithTokenAsync(baseAddress, token);

            // A valid, authenticated token that just lacks the scope - this must be Forbidden
            // (authenticated but not permitted), distinct from the no-principal Unauthorized case.
            Assert.Equal(System.Net.HttpStatusCode.Forbidden, status);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task ScopeClaim_SpaceDelimited_ContainingRequiredScope_PassesThrough()
    {
        using var jwks = new FakeJwksServer();
        var key = jwks.AddKey("kid1");
        var (app, baseAddress) = await StartHostAsync(jwks, "admin");
        try
        {
            var token = FakeJwksServer.CreateToken(key, "kid1", Issuer, Audience,
                extraClaims: new Dictionary<string, object> { ["scope"] = "read admin write" });

            var status = await SendWithTokenAsync(baseAddress, token);

            Assert.Equal(System.Net.HttpStatusCode.OK, status);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task ScpClaim_AsPlainString_ContainingRequiredScope_PassesThrough()
    {
        using var jwks = new FakeJwksServer();
        var key = jwks.AddKey("kid1");
        var (app, baseAddress) = await StartHostAsync(jwks, "admin");
        try
        {
            var token = FakeJwksServer.CreateToken(key, "kid1", Issuer, Audience,
                extraClaims: new Dictionary<string, object> { ["scp"] = "read admin" });

            var status = await SendWithTokenAsync(baseAddress, token);

            Assert.Equal(System.Net.HttpStatusCode.OK, status);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task ScpClaim_AsJsonArray_ContainingRequiredScope_PassesThrough()
    {
        using var jwks = new FakeJwksServer();
        var key = jwks.AddKey("kid1");
        var (app, baseAddress) = await StartHostAsync(jwks, "admin");
        try
        {
            var token = FakeJwksServer.CreateToken(key, "kid1", Issuer, Audience,
                extraClaims: new Dictionary<string, object> { ["scp"] = "[\"read\",\"admin\"]" });

            var status = await SendWithTokenAsync(baseAddress, token);

            Assert.Equal(System.Net.HttpStatusCode.OK, status);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task Composition_TokenWithScope_ReachesDownstreamHandler_TokenWithoutScope_DoesNot()
    {
        using var jwks = new FakeJwksServer();
        var key = jwks.AddKey("kid1");
        var (app, baseAddress) = await StartHostAsync(jwks, "admin");
        try
        {
            var goodToken = FakeJwksServer.CreateToken(key, "kid1", Issuer, Audience,
                extraClaims: new Dictionary<string, object> { ["scope"] = "admin" });
            var badToken = FakeJwksServer.CreateToken(key, "kid1", Issuer, Audience,
                extraClaims: new Dictionary<string, object> { ["scope"] = "read" });

            Assert.Equal(System.Net.HttpStatusCode.OK, await SendWithTokenAsync(baseAddress, goodToken));
            Assert.Equal(System.Net.HttpStatusCode.Forbidden, await SendWithTokenAsync(baseAddress, badToken));
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
