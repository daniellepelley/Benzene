using System.Collections.Generic;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Core.MessageHandlers;
using Xunit;

namespace Benzene.Test.Core.Core.MessageHandling;

public class HeaderMessageVersionGetterTest
{
    private class TestContext
    {
    }

    private class FixedHeadersGetter : IMessageHeadersGetter<TestContext>
    {
        private readonly IDictionary<string, string>? _headers;

        public FixedHeadersGetter(IDictionary<string, string>? headers)
        {
            _headers = headers;
        }

        public IDictionary<string, string> GetHeaders(TestContext context) => _headers;
    }

    [Fact]
    public void GetVersion_BenzeneVersionHeaderPresent_TakesPriorityOverFallbacks()
    {
        var headers = new Dictionary<string, string>
        {
            ["benzene-version"] = "V2",
            ["version"] = "V1",
            ["x-version"] = "V3"
        };
        var getter = new HeaderMessageVersionGetter<TestContext>(new FixedHeadersGetter(headers));

        Assert.Equal("V2", getter.GetVersion(new TestContext()));
    }

    [Fact]
    public void GetVersion_NoBenzeneVersionHeader_FallsBackToPlainVersionHeader()
    {
        var headers = new Dictionary<string, string>
        {
            ["version"] = "V1",
            ["x-version"] = "V3"
        };
        var getter = new HeaderMessageVersionGetter<TestContext>(new FixedHeadersGetter(headers));

        Assert.Equal("V1", getter.GetVersion(new TestContext()));
    }

    [Fact]
    public void GetVersion_OnlyXVersionHeaderPresent_FallsBackToIt()
    {
        var headers = new Dictionary<string, string> { ["x-version"] = "V3" };
        var getter = new HeaderMessageVersionGetter<TestContext>(new FixedHeadersGetter(headers));

        Assert.Equal("V3", getter.GetVersion(new TestContext()));
    }

    [Fact]
    public void GetVersion_NoneOfTheFallbackHeadersPresent_ReturnsNull()
    {
        var headers = new Dictionary<string, string> { ["content-type"] = "application/json" };
        var getter = new HeaderMessageVersionGetter<TestContext>(new FixedHeadersGetter(headers));

        Assert.Null(getter.GetVersion(new TestContext()));
    }

    [Fact]
    public void GetVersion_HeaderNameCasingDiffers_StillMatchesCaseInsensitively()
    {
        var headers = new Dictionary<string, string> { ["Benzene-Version"] = "V2" };
        var getter = new HeaderMessageVersionGetter<TestContext>(new FixedHeadersGetter(headers));

        Assert.Equal("V2", getter.GetVersion(new TestContext()));
    }

    [Fact]
    public void GetVersion_HeadersDictionaryIsNull_ReturnsNull()
    {
        var getter = new HeaderMessageVersionGetter<TestContext>(new FixedHeadersGetter(null));

        Assert.Null(getter.GetVersion(new TestContext()));
    }

    [Fact]
    public void GetVersion_CustomHeaderNameList_OnlyRecognizesConfiguredNames()
    {
        var headers = new Dictionary<string, string> { ["benzene-version"] = "V2", ["schema-version"] = "V9" };
        var getter = new HeaderMessageVersionGetter<TestContext>(new FixedHeadersGetter(headers), headerNames: ["schema-version"]);

        // benzene-version is ignored entirely because it isn't in the configured list - proves an
        // app with a conflicting pre-existing "version"-shaped header can narrow the fallback
        // (docs/specification/versioning.md §2.1).
        Assert.Equal("V9", getter.GetVersion(new TestContext()));
    }
}
