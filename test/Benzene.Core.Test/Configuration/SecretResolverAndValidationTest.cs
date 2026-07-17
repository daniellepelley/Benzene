using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Configuration.Core;
using Xunit;

namespace Benzene.Test.Configuration;

// SecretResolver (typed, fail-fast reads) and SecretValidation (startup completeness check).
public class SecretResolverAndValidationTest
{
    private static SecretResolver Resolver(params (string Name, string Value)[] values)
    {
        var dict = new Dictionary<string, string>();
        foreach (var (name, value) in values)
        {
            dict[name] = value;
        }

        return new SecretResolver(new InMemorySecretStore(dict));
    }

    [Fact]
    public async Task Require_ReturnsValue_WhenPresent()
    {
        Assert.Equal("v", await Resolver(("Api:Key", "v")).RequireAsync("Api:Key"));
    }

    [Fact]
    public async Task Require_Throws_WhenMissing()
    {
        var ex = await Assert.ThrowsAsync<MissingSecretException>(() => Resolver().RequireAsync("Api:Key"));
        Assert.Contains("Api:Key", ex.MissingNames);
    }

    [Fact]
    public async Task Require_Throws_WhenBlank()
    {
        await Assert.ThrowsAsync<MissingSecretException>(() => Resolver(("Api:Key", "   ")).RequireAsync("Api:Key"));
    }

    [Fact]
    public async Task Get_ReturnsDefault_WhenMissing()
    {
        Assert.Equal("fallback", await Resolver().GetAsync("Api:Key", "fallback"));
        Assert.Equal("real", await Resolver(("Api:Key", "real")).GetAsync("Api:Key", "fallback"));
    }

    [Fact]
    public async Task TypedReads_ParseOrThrow()
    {
        var resolver = Resolver(
            ("Port", "8080"), ("Enabled", "true"), ("Endpoint", "https://api.example.com"),
            ("BadPort", "notanumber"));

        Assert.Equal(8080, await resolver.RequireIntAsync("Port"));
        Assert.True(await resolver.RequireBoolAsync("Enabled"));
        Assert.Equal(new Uri("https://api.example.com"), await resolver.RequireUriAsync("Endpoint"));

        await Assert.ThrowsAsync<FormatException>(() => resolver.RequireIntAsync("BadPort"));
    }

    [Fact]
    public async Task EnsureRequired_Passes_WhenAllPresent()
    {
        var store = new InMemorySecretStore(new Dictionary<string, string>
        {
            ["Db:Password"] = "p", ["Api:Key"] = "k"
        });

        await SecretValidation.EnsureRequiredAsync(store, "Db:Password", "Api:Key");
    }

    [Fact]
    public async Task EnsureRequired_Throws_ListingAllMissingAtOnce()
    {
        var store = new InMemorySecretStore(new Dictionary<string, string> { ["Present"] = "v" });

        var ex = await Assert.ThrowsAsync<MissingSecretException>(
            () => SecretValidation.EnsureRequiredAsync(store, "Present", "Missing1", "Missing2"));

        Assert.Contains("Missing1", ex.MissingNames);
        Assert.Contains("Missing2", ex.MissingNames);
        Assert.DoesNotContain("Present", ex.MissingNames);
    }
}
