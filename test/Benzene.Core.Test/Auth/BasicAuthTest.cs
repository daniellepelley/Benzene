using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Benzene.AspNet.Core;
using Benzene.Auth.Basic;
using Benzene.Core.MessageHandlers;
using Benzene.Test.Examples;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Auth;

/// <summary>
/// Exercises <c>UseBasicAuth</c> end-to-end against a real Kestrel-hosted pipeline. GET
/// <c>/example</c> (<see cref="ExampleMessageHandler"/>, via <c>Benzene.Test.Examples</c>) is the
/// protected downstream route every case probes.
/// </summary>
public class BasicAuthTest
{
    private sealed class RecordingValidator : IBasicAuthCredentialValidator
    {
        public string? SeenUsername { get; private set; }
        public string? SeenPassword { get; private set; }
        public Func<string, string, ClaimsPrincipal?> Validate { get; set; } = (_, _) => null;

        public Task<ClaimsPrincipal?> ValidateAsync(string username, string password)
        {
            SeenUsername = username;
            SeenPassword = password;
            return Task.FromResult(Validate(username, password));
        }
    }

    private static async Task<(WebApplication App, Uri BaseAddress, RecordingValidator Validator)> StartHostAsync(string realm = "Benzene")
    {
        var validator = new RecordingValidator();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers();
        builder.Services.ConfigureServiceCollection();

        var app = builder.Build();
        app.UseRouting();
        app.UseBenzene(benzene => benzene
            .UseHttp(asp => asp
                .UseBasicAuth(validator, realm)
                .UseMessageHandlers()
            )
        );
        app.UseEndpoints(_ => { });

        await app.StartAsync();
        return (app, new Uri(app.Urls.First()), validator);
    }

    [Fact]
    public async Task MissingAuthorizationHeader_IsUnauthorized_WithChallenge()
    {
        var (app, baseAddress, _) = await StartHostAsync(realm: "test-realm");
        try
        {
            using var client = new HttpClient { BaseAddress = baseAddress };

            var response = await client.GetAsync(Defaults.Path);

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
            var challenge = response.Headers.WwwAuthenticate.Single();
            Assert.Equal("Basic", challenge.Scheme);
            Assert.Contains("test-realm", challenge.Parameter);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task MalformedBase64_IsUnauthorized_WithChallenge()
    {
        var (app, baseAddress, _) = await StartHostAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = baseAddress };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "not-valid-base64!!!");

            var response = await client.GetAsync(Defaults.Path);

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.NotEmpty(response.Headers.WwwAuthenticate);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task ColonInPassword_DecodesAndSplitsCorrectly()
    {
        var (app, baseAddress, validator) = await StartHostAsync();
        try
        {
            validator.Validate = (u, p) => u == "bob" && p == "pa:ss:word"
                ? new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, u) }))
                : null;

            using var client = new HttpClient { BaseAddress = baseAddress };
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("bob:pa:ss:word"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);

            var response = await client.GetAsync(Defaults.Path);

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("bob", validator.SeenUsername);
            Assert.Equal("pa:ss:word", validator.SeenPassword);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task ValidatorReturnsNull_IsUnauthorized_WithChallenge()
    {
        var (app, baseAddress, validator) = await StartHostAsync();
        try
        {
            validator.Validate = (_, _) => null;

            using var client = new HttpClient { BaseAddress = baseAddress };
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("user:wrong-password"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);

            var response = await client.GetAsync(Defaults.Path);

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.NotEmpty(response.Headers.WwwAuthenticate);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Fact]
    public async Task ValidatorReturnsPrincipal_PassesThrough()
    {
        var (app, baseAddress, validator) = await StartHostAsync();
        try
        {
            validator.Validate = (u, _) => new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, u) }));

            using var client = new HttpClient { BaseAddress = baseAddress };
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("user:correct-password"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);

            var response = await client.GetAsync(Defaults.Path);

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
