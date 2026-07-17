using System;
using Benzene.Abstractions.Middleware;
using Benzene.AspNet.Core;
using Benzene.Auth.OAuth2;
using Benzene.Core.Middleware;
using Benzene.Microsoft.Dependencies;
using Benzene.Test.Examples;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Auth;

/// <summary>
/// <see cref="OAuth2BearerOptions.Validate"/> runs at pipeline wire-up time (via
/// <c>UseOAuth2Bearer</c>), not on the first request - a misconfigured pipeline must fail fast.
/// These exercise it through the public <c>UseOAuth2Bearer</c> entry point rather than calling the
/// internal <c>Validate()</c> directly.
/// </summary>
public class OAuth2BearerOptionsValidationTest
{
    private static OAuth2BearerOptions ValidOptions()
    {
        return new OAuth2BearerOptions
        {
            JwksUri = "https://issuer.example.com/.well-known/jwks.json",
            ValidIssuers = new[] { "https://issuer.example.com" },
            ValidAudiences = new[] { "my-api" },
            ValidAlgorithms = new[] { "RS256" }
        };
    }

    private static IMiddlewarePipelineBuilder<AspNetContext> CreatePipelineBuilder()
    {
        var services = new ServiceCollection().ConfigureServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        return new MiddlewarePipelineBuilder<AspNetContext>(container);
    }

    [Fact]
    public void BothAuthorityAndJwksUriSet_Throws()
    {
        var options = ValidOptions();
        options.Authority = "https://issuer.example.com/.well-known/openid-configuration";

        var app = CreatePipelineBuilder();
        Assert.Throws<ArgumentException>(() => app.UseOAuth2Bearer(options));
    }

    [Fact]
    public void NeitherAuthorityNorJwksUriSet_Throws()
    {
        var options = ValidOptions();
        options.JwksUri = null;

        var app = CreatePipelineBuilder();
        Assert.Throws<ArgumentException>(() => app.UseOAuth2Bearer(options));
    }

    [Fact]
    public void EmptyValidIssuers_Throws()
    {
        var options = ValidOptions();
        options.ValidIssuers = Array.Empty<string>();

        var app = CreatePipelineBuilder();
        Assert.Throws<ArgumentException>(() => app.UseOAuth2Bearer(options));
    }

    [Fact]
    public void EmptyValidAudiences_Throws()
    {
        var options = ValidOptions();
        options.ValidAudiences = Array.Empty<string>();

        var app = CreatePipelineBuilder();
        Assert.Throws<ArgumentException>(() => app.UseOAuth2Bearer(options));
    }

    [Fact]
    public void EmptyValidAlgorithms_Throws()
    {
        // The one that directly guards against RFC 8725 §3.1 algorithm confusion - an empty
        // allowlist would trust whatever "alg" the token itself claims.
        var options = ValidOptions();
        options.ValidAlgorithms = Array.Empty<string>();

        var app = CreatePipelineBuilder();
        Assert.Throws<ArgumentException>(() => app.UseOAuth2Bearer(options));
    }

    [Fact]
    public void ValidOptions_DoesNotThrow()
    {
        var app = CreatePipelineBuilder();
        app.UseOAuth2Bearer(ValidOptions());
    }
}
