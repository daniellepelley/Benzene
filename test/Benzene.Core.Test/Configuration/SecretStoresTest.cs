using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Benzene.Configuration.Core;
using Xunit;

namespace Benzene.Test.Configuration;

// The store implementations (in-memory, env vars, files, composite, caching) - all BCL-only and
// exercisable without any cloud dependency.
public class SecretStoresTest
{
    [Fact]
    public async Task InMemory_ReturnsValue_OrNullWhenAbsent()
    {
        var store = new InMemorySecretStore(new Dictionary<string, string> { ["Db:Password"] = "s3cret" });

        Assert.Equal("s3cret", await store.GetSecretAsync("Db:Password"));
        Assert.Null(await store.GetSecretAsync("Missing"));
    }

    [Theory]
    [InlineData("Db:Password", "DB_PASSWORD")]
    [InlineData("Api.Key", "API_KEY")]
    [InlineData("feature-flag", "FEATURE_FLAG")]
    public void EnvironmentVariable_MapsLogicalNameToKey(string name, string expectedKey)
    {
        Assert.Equal(expectedKey, EnvironmentVariableSecretStore.ToEnvironmentVariableKey(name));
    }

    [Fact]
    public async Task EnvironmentVariable_ReadsMappedVariable()
    {
        // Unique name so parallel test classes can't collide on the process-wide env var.
        Environment.SetEnvironmentVariable("BENZENE_TEST_DB_PASSWORD", "from-env");
        try
        {
            var store = new EnvironmentVariableSecretStore("Benzene_Test_");

            Assert.Equal("from-env", await store.GetSecretAsync("Db:Password"));
            Assert.Null(await store.GetSecretAsync("Nope"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("BENZENE_TEST_DB_PASSWORD", null);
        }
    }

    [Fact]
    public async Task File_ReadsContent_TrimsTrailingNewline_NullWhenAbsent()
    {
        var dir = Path.Combine(Path.GetTempPath(), "benzene-secrets-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "db_password"), "file-secret\n");
            var store = new FileSecretStore(dir);

            // "Db:Password" sanitizes to "Db_Password"; also verify the direct file name.
            Assert.Equal("file-secret", await store.GetSecretAsync("db_password"));
            Assert.Null(await store.GetSecretAsync("absent"));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Composite_ReturnsFirstNonNull()
    {
        var store = new CompositeSecretStore(
            new InMemorySecretStore(new Dictionary<string, string> { ["Shared"] = "from-first" }),
            new InMemorySecretStore(new Dictionary<string, string> { ["Shared"] = "from-second", ["Only2"] = "v2" }));

        Assert.Equal("from-first", await store.GetSecretAsync("Shared")); // earliest store wins
        Assert.Equal("v2", await store.GetSecretAsync("Only2"));          // falls through
        Assert.Null(await store.GetSecretAsync("Nowhere"));
    }

    private sealed class CountingStore : ISecretStore
    {
        private readonly string? _value;
        public int Calls { get; private set; }
        public CountingStore(string? value) => _value = value;

        public Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(_value);
        }
    }

    [Fact]
    public async Task Caching_ServesFromCacheWithinTtl_RefetchesAfterExpiry_AndOnInvalidate()
    {
        var now = DateTimeOffset.UtcNow;
        var inner = new CountingStore("v");
        var cache = new CachingSecretStore(inner, timeToLive: TimeSpan.FromMinutes(5), now: () => now);

        Assert.Equal("v", await cache.GetSecretAsync("k"));
        Assert.Equal("v", await cache.GetSecretAsync("k"));
        Assert.Equal(1, inner.Calls); // second read served from cache

        now = now.AddMinutes(6); // TTL lapses
        Assert.Equal("v", await cache.GetSecretAsync("k"));
        Assert.Equal(2, inner.Calls); // re-fetched

        cache.Invalidate("k"); // explicit reload
        Assert.Equal("v", await cache.GetSecretAsync("k"));
        Assert.Equal(3, inner.Calls);
    }
}
