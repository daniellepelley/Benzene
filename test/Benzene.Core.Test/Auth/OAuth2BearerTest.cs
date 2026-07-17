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
/// Exercises <c>UseOAuth2Bearer</c> end-to-end against a real Kestrel-hosted pipeline and a real
/// loopback JWKS endpoint (<see cref="FakeJwksServer"/>) - not just the validation logic in
/// isolation. GET <c>/example</c> (<see cref="ExampleMessageHandler"/>, via
/// <c>Benzene.Test.Examples</c>) is the protected downstream route every case probes.
/// </summary>
public class OAuth2BearerTest
{
    private const string Issuer = "https://issuer.example.com";
    private const string Audience = "my-api";

    private static async Task<(WebApplication App, Uri BaseAddress)> StartHostAsync(OAuth2BearerOptions options)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers();
        builder.Services.ConfigureServiceCollection();

        var app = builder.Build();
        app.UseRouting();
        app.UseBenzene(benzene => benzene
            .UseHttp(asp => asp
                .UseOAuth2Bearer(options)
                .UseMessageHandlers()
            )
        );
        app.UseEndpoints(_ => { });

        await app.StartAsync();
        return (app, new Uri(app.Urls.First()));
    }

    private static OAuth2BearerOptions OptionsFor(FakeJwksServer jwks, params string[] validAlgorithms)
    {
        return new OAuth2BearerOptions
        {
            JwksUri = jwks.JwksUri,
            ValidIssuers = new[] { Issuer },
            ValidAudiences = new[] { Audience },
            ValidAlgorithms = validAlgorithms.Length > 0 ? validAlgorithms : new[] { "RS256" },
            // FakeJwksServer is a plain-HTTP loopback test double, not a real identity provider -
            // see OAuth2BearerOptions.RequireHttpsMetadata's remarks for why this is never done
            // in production (it stays true there).
            RequireHttpsMetadata = false
        };
    }

    [Fact]
    public async Task ValidToken_PassesThrough()
    {
        using var jwks = new FakeJwksServer();
        var key = jwks.AddKey("kid1");
        var (app, baseAddress) = await StartHostAsync(OptionsFor(jwks));
        try
        {
            using var client = new HttpClient { BaseAddress = baseAddress };
            var token = FakeJwksServer.CreateToken(key, "kid1", Issuer, Audience);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(Defaults.Path);

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task ExpiredToken_IsUnauthorized()
    {
        using var jwks = new FakeJwksServer();
        var key = jwks.AddKey("kid1");
        var (app, baseAddress) = await StartHostAsync(OptionsFor(jwks));
        try
        {
            using var client = new HttpClient { BaseAddress = baseAddress };
            var token = FakeJwksServer.CreateToken(key, "kid1", Issuer, Audience,
                expires: DateTime.UtcNow.AddMinutes(-5), notBefore: DateTime.UtcNow.AddMinutes(-10));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(Defaults.Path);

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task WrongIssuer_IsUnauthorized()
    {
        using var jwks = new FakeJwksServer();
        var key = jwks.AddKey("kid1");
        var (app, baseAddress) = await StartHostAsync(OptionsFor(jwks));
        try
        {
            using var client = new HttpClient { BaseAddress = baseAddress };
            var token = FakeJwksServer.CreateToken(key, "kid1", "https://a-different-issuer.example.com", Audience);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(Defaults.Path);

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task WrongAudience_IsUnauthorized()
    {
        using var jwks = new FakeJwksServer();
        var key = jwks.AddKey("kid1");
        var (app, baseAddress) = await StartHostAsync(OptionsFor(jwks));
        try
        {
            using var client = new HttpClient { BaseAddress = baseAddress };
            var token = FakeJwksServer.CreateToken(key, "kid1", Issuer, "a-different-audience");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(Defaults.Path);

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task TokenSignedWithDisallowedAlgorithm_IsUnauthorized()
    {
        // The algorithm-confusion test: options only allow RS256, but this token is HMAC-signed
        // (HS256). A validator that trusted the token's own "alg" claim would need something to
        // treat as the HMAC secret - the classic attack uses the service's own RSA public key,
        // which is exactly why an explicit allowlist (not the token's self-declared alg) is what
        // this package requires. This proves the allowlist is actually enforced, not just present.
        using var jwks = new FakeJwksServer();
        jwks.AddKey("kid1");
        var (app, baseAddress) = await StartHostAsync(OptionsFor(jwks, "RS256"));
        try
        {
            using var client = new HttpClient { BaseAddress = baseAddress };
            var token = FakeJwksServer.CreateHmacSignedToken(Issuer, Audience, "some-arbitrary-shared-secret-value");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(Defaults.Path);

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task MissingAuthorizationHeader_IsUnauthorized()
    {
        using var jwks = new FakeJwksServer();
        jwks.AddKey("kid1");
        var (app, baseAddress) = await StartHostAsync(OptionsFor(jwks));
        try
        {
            using var client = new HttpClient { BaseAddress = baseAddress };

            var response = await client.GetAsync(Defaults.Path);

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task MalformedAuthorizationHeader_IsUnauthorized()
    {
        using var jwks = new FakeJwksServer();
        jwks.AddKey("kid1");
        var (app, baseAddress) = await StartHostAsync(OptionsFor(jwks));
        try
        {
            using var client = new HttpClient { BaseAddress = baseAddress };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "dXNlcjpwYXNz");

            var response = await client.GetAsync(Defaults.Path);

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task MultipleKeysInJwks_BothValidate()
    {
        // Two keys present in the JWKS from the start (both known up front) - proves the middleware
        // correctly picks the signing key matching each token's "kid" rather than only ever trying
        // the first key in the set.
        using var jwks = new FakeJwksServer();
        var key1 = jwks.AddKey("kid1");
        var key2 = jwks.AddKey("kid2");
        var (app, baseAddress) = await StartHostAsync(OptionsFor(jwks));
        try
        {
            using var client = new HttpClient { BaseAddress = baseAddress };

            var token1 = FakeJwksServer.CreateToken(key1, "kid1", Issuer, Audience);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
            var response1 = await client.GetAsync(Defaults.Path);
            Assert.Equal(System.Net.HttpStatusCode.OK, response1.StatusCode);

            var token2 = FakeJwksServer.CreateToken(key2, "kid2", Issuer, Audience);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
            var response2 = await client.GetAsync(Defaults.Path);
            Assert.Equal(System.Net.HttpStatusCode.OK, response2.StatusCode);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    // A "live rotation" test - warm the cache with kid1, add kid2 to the JWKS server afterward,
    // then confirm a kid2-signed token still validates - was attempted here but proved unreliable:
    // Microsoft.IdentityModel.Protocols.ConfigurationManager<T> enforces a MinimumRefreshInterval
    // (several minutes) between fetches, which is correct, deliberate production behavior (it stops
    // a flood of unrecognized-kid tokens from hammering the JWKS endpoint) but makes a live re-fetch
    // within one fast unit test impractical to assert deterministically without reaching into
    // internal timing knobs this package doesn't (and shouldn't) expose just for a test. Per the
    // design doc's own fallback guidance, MultipleKeysInJwks_BothValidate above is the substitute:
    // it proves the middleware correctly resolves a signing key by "kid" out of a multi-key JWKS
    // document rather than only ever trying the first key - the part of key-rotation support that
    // is deterministically testable.
}
