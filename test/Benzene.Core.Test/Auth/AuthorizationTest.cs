using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Benzene.AspNet.Core;
using Benzene.Abstractions.Middleware;
using Benzene.Auth.Core;
using Benzene.Auth.OAuth2;
using Benzene.Core.MessageHandlers;
using Benzene.Test.Examples;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Auth;

/// <summary>
/// Exercises the A.4 authorization layer (<c>RequireRole</c>/<c>RequirePolicy</c>/
/// <c>RequireAuthorization</c>, all in <c>Benzene.Auth.Core</c>) chained after <c>UseOAuth2Bearer</c>
/// in one real Kestrel-hosted pipeline — the same composition style as <see cref="RequireScopeTest"/>.
/// GET <c>/example</c> is the protected downstream route.
/// </summary>
public class AuthorizationTest
{
    private const string Issuer = "https://issuer.example.com";
    private const string Audience = "my-api";

    private static async Task<(WebApplication App, Uri BaseAddress)> StartHostAsync(
        FakeJwksServer jwks, Action<IMiddlewarePipelineBuilder<AspNetContext>> configureAuthz)
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
            .UseHttp(asp =>
            {
                asp.UseOAuth2Bearer(options);
                configureAuthz(asp);
                asp.UseMessageHandlers();
            })
        );
        app.UseEndpoints(_ => { });

        await app.StartAsync();
        return (app, new Uri(app.Urls.First()));
    }

    private static async Task<HttpStatusCode> SendAsync(Uri baseAddress, string? token)
    {
        using var client = new HttpClient { BaseAddress = baseAddress };
        if (token != null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await client.GetAsync(Defaults.Path);
        return response.StatusCode;
    }

    private static async Task RunWithHostAsync(
        Action<IMiddlewarePipelineBuilder<AspNetContext>> configureAuthz, Func<System.Security.Cryptography.RSA, Uri, Task> body)
    {
        using var jwks = new FakeJwksServer();
        var key = jwks.AddKey("kid1");
        var (app, baseAddress) = await StartHostAsync(jwks, configureAuthz);
        try
        {
            await body(key, baseAddress);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private static string Token(System.Security.Cryptography.RSA key, IDictionary<string, object> claims)
        => FakeJwksServer.CreateToken(key, "kid1", Issuer, Audience, extraClaims: claims);

    // ---- RequireRole ----------------------------------------------------------------------------

    [Fact]
    public async Task RequireRole_NoToken_IsUnauthorized()
    {
        await RunWithHostAsync(
            asp => asp.RequireRole("admin"),
            async (_, baseAddress) => Assert.Equal(HttpStatusCode.Unauthorized, await SendAsync(baseAddress, null)));
    }

    [Fact]
    public async Task RequireRole_AuthenticatedButMissingRole_IsForbidden()
    {
        await RunWithHostAsync(
            asp => asp.RequireRole("admin"),
            async (key, baseAddress) =>
            {
                var token = Token(key, new Dictionary<string, object> { ["roles"] = "reader" });
                Assert.Equal(HttpStatusCode.Forbidden, await SendAsync(baseAddress, token));
            });
    }

    [Fact]
    public async Task RequireRole_WithMatchingRoleClaim_PassesThrough()
    {
        await RunWithHostAsync(
            asp => asp.RequireRole("admin"),
            async (key, baseAddress) =>
            {
                var token = Token(key, new Dictionary<string, object> { ["roles"] = "admin" });
                Assert.Equal(HttpStatusCode.OK, await SendAsync(baseAddress, token));
            });
    }

    [Fact]
    public async Task RequireRole_WithRolesAsJsonArray_PassesThrough()
    {
        await RunWithHostAsync(
            asp => asp.RequireRole("admin"),
            async (key, baseAddress) =>
            {
                // Azure AD app-roles shape: a single "roles" claim whose value is a JSON array.
                var token = Token(key, new Dictionary<string, object> { ["roles"] = "[\"reader\",\"admin\"]" });
                Assert.Equal(HttpStatusCode.OK, await SendAsync(baseAddress, token));
            });
    }

    // ---- RequirePolicy --------------------------------------------------------------------------

    [Fact]
    public async Task RequirePolicy_Inline_SatisfiedByClaim_PassesThrough_OtherwiseForbidden()
    {
        Action<IMiddlewarePipelineBuilder<AspNetContext>> authz = asp =>
            asp.RequirePolicy("employees-only",
                principal => Task.FromResult(principal.HasClaim(c => c.Type == "department" && c.Value == "eng")));

        await RunWithHostAsync(authz, async (key, baseAddress) =>
        {
            var good = Token(key, new Dictionary<string, object> { ["department"] = "eng" });
            var bad = Token(key, new Dictionary<string, object> { ["department"] = "sales" });

            Assert.Equal(HttpStatusCode.OK, await SendAsync(baseAddress, good));
            Assert.Equal(HttpStatusCode.Forbidden, await SendAsync(baseAddress, bad));
        });
    }

    [Fact]
    public async Task RequirePolicy_ByName_ResolvesRegisteredPolicy()
    {
        Action<IMiddlewarePipelineBuilder<AspNetContext>> authz = asp =>
        {
            asp.Register(x => x.AddAuthorizationPolicy("employees-only",
                principal => principal.HasClaim(c => c.Type == "department" && c.Value == "eng")));
            asp.RequirePolicy("employees-only");
        };

        await RunWithHostAsync(authz, async (key, baseAddress) =>
        {
            var good = Token(key, new Dictionary<string, object> { ["department"] = "eng" });
            var bad = Token(key, new Dictionary<string, object> { ["department"] = "sales" });

            Assert.Equal(HttpStatusCode.OK, await SendAsync(baseAddress, good));
            Assert.Equal(HttpStatusCode.Forbidden, await SendAsync(baseAddress, bad));
        });
    }

    [Fact]
    public async Task RequirePolicy_NoToken_IsUnauthorized()
    {
        await RunWithHostAsync(
            asp => asp.RequirePolicy("always", _ => Task.FromResult(true)),
            async (_, baseAddress) => Assert.Equal(HttpStatusCode.Unauthorized, await SendAsync(baseAddress, null)));
    }

    // ---- RequireAuthorization (resource-based) --------------------------------------------------

    private record OrderResource(string Tenant);

    private class SameTenantAuthorizationHandler : IAuthorizationHandler<OrderResource>
    {
        public Task<bool> IsAuthorizedAsync(ClaimsPrincipal principal, OrderResource resource)
            => Task.FromResult(principal.HasClaim(c => c.Type == "tenant" && c.Value == resource.Tenant));
    }

    [Fact]
    public async Task RequireAuthorization_CallerInResourceTenant_PassesThrough_OtherwiseForbidden()
    {
        Action<IMiddlewarePipelineBuilder<AspNetContext>> authz = asp =>
        {
            asp.Register(x => x.AddScoped<IAuthorizationHandler<OrderResource>, SameTenantAuthorizationHandler>());
            asp.RequireAuthorization<AspNetContext, OrderResource>(_ => new OrderResource("acme"));
        };

        await RunWithHostAsync(authz, async (key, baseAddress) =>
        {
            var good = Token(key, new Dictionary<string, object> { ["tenant"] = "acme" });
            var bad = Token(key, new Dictionary<string, object> { ["tenant"] = "globex" });

            Assert.Equal(HttpStatusCode.OK, await SendAsync(baseAddress, good));
            Assert.Equal(HttpStatusCode.Forbidden, await SendAsync(baseAddress, bad));
        });
    }
}
