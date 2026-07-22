using System.Collections.Generic;
using System.Threading.Tasks;
using Benzene.Abstractions.Messages;
using Benzene.Abstractions.Messages.BenzeneClient;
using Benzene.Abstractions.Results;
using Benzene.Clients;
using Benzene.Results;
using Xunit;

namespace Benzene.Test.Clients;

/// <summary>
/// The outbound version helpers (Slice 1 of the versioning finish): a producer declares a payload version
/// and it travels in the standard <c>benzene-version</c> header, which the inbound
/// <c>HeaderMessageVersionGetter</c> reads. Proves the send half of "send v1 → upcast to v2".
/// </summary>
public class VersionSendExtensionsTest
{
    private sealed class CapturingClient : IBenzeneMessageClient
    {
        public IBenzeneClientRequest<object>? Last;

        public Task<IBenzeneResult<TResponse>> SendMessageAsync<TRequest, TResponse>(IBenzeneClientRequest<TRequest> request)
        {
            Last = new BenzeneClientRequest<object>(request.Topic, request.Message!, request.Headers);
            return Task.FromResult(BenzeneResult.Ok<TResponse>(default!));
        }

        public void Dispose()
        {
        }
    }

    [Fact]
    public async Task SendMessageAsync_WithVersion_PutsItInTheStandardVersionHeader()
    {
        var client = new CapturingClient();

        await client.SendMessageAsync<string, string>("order:create", "payload", version: "2");

        Assert.Equal("2", client.Last!.Headers[MessageVersionHeaders.Default]);
    }

    [Fact]
    public async Task SendMessageAsync_WithVersion_PreservesOtherHeaders()
    {
        var client = new CapturingClient();

        await client.SendMessageAsync<string, string>("order:create", "payload", "2",
            new Dictionary<string, string> { { "tenant", "acme" } });

        Assert.Equal("2", client.Last!.Headers[MessageVersionHeaders.Default]);
        Assert.Equal("acme", client.Last.Headers["tenant"]);
    }

    [Fact]
    public void WithVersion_NullHeaders_ReturnsANonNullDictionaryCarryingTheVersion()
    {
        IDictionary<string, string>? headers = null;

        var result = headers.WithVersion("3");

        Assert.Equal("3", result[MessageVersionHeaders.Default]);
    }

    [Fact]
    public void WithVersion_EmptyVersion_LeavesHeadersUnchanged()
    {
        var headers = new Dictionary<string, string> { { "tenant", "acme" } };

        var result = headers.WithVersion("");

        Assert.False(result.ContainsKey(MessageVersionHeaders.Default));
        Assert.Equal("acme", result["tenant"]);
    }
}
