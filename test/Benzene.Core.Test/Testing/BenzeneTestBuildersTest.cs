using System.Collections.Generic;
using Benzene.Testing;
using Xunit;

namespace Benzene.Test.Testing;

public class BenzeneTestBuildersTest
{
    [Fact]
    public void MessageBuilder_WithHeader_SameKeyTwice_OverwritesLastWins()
    {
        // Real transports treat headers as an overwrite map; setting a default then an override must
        // be last-wins, not throw (Dictionary.Add threw ArgumentException on the duplicate key).
        var builder = MessageBuilder.Create("topic")
            .WithHeader("content-type", "application/json")
            .WithHeader("content-type", "application/xml");

        Assert.Equal("application/xml", builder.Headers["content-type"]);
    }

    [Fact]
    public void HttpBuilder_WithHeader_SameKeyTwice_OverwritesLastWins()
    {
        var builder = HttpBuilder.Create("GET", "/x")
            .WithHeader("accept", "application/json")
            .WithHeader("accept", "application/xml");

        Assert.Equal("application/xml", builder.Headers["accept"]);
    }

    [Fact]
    public void HttpBuilder_WithHeaders_IsAdditive_DoesNotDropPreviouslyAddedHeader()
    {
        // WithHeaders must merge like the sibling MessageBuilderExtensions.WithHeaders, not replace the
        // whole dictionary (which clobbered any earlier WithHeader values).
        var builder = HttpBuilder.Create("GET", "/x")
            .WithHeader("a", "1")
            .WithHeaders(new Dictionary<string, string> { ["b"] = "2" });

        Assert.Equal("1", builder.Headers["a"]);
        Assert.Equal("2", builder.Headers["b"]);
    }

    [Fact]
    public void HttpBuilder_WithHeaders_CopiesTheCallerDictionary_DoesNotAliasIt()
    {
        // WithHeaders must not alias the caller's collection: a later WithHeader must not mutate it,
        // and a read-only source dictionary must not throw.
        var source = new Dictionary<string, string> { ["b"] = "2" };
        var builder = HttpBuilder.Create("GET", "/x").WithHeaders(source);

        builder.WithHeader("c", "3");

        Assert.False(source.ContainsKey("c"));
    }
}
