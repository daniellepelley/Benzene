using System.Collections.Generic;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Diagnostics.Correlation;
using Moq;
using Xunit;

namespace Benzene.Test.Diagnostics;

public class CorrelationExtensionsTest
{
    private static Mock<IMessageHeadersGetter<string>> CreateHeadersGetter(IDictionary<string, string> headers)
    {
        var getter = new Mock<IMessageHeadersGetter<string>>();
        getter.Setup(x => x.GetHeaders("context")).Returns(headers);
        return getter;
    }

    [Fact]
    public void GetHeader_SingleKey_MatchesCaseInsensitively()
    {
        var getter = CreateHeadersGetter(new Dictionary<string, string> { { "TraceParent", "abc-123" } });

        var value = getter.Object.GetHeader("context", "traceparent");

        Assert.Equal("abc-123", value);
    }

    [Fact]
    public void GetHeader_SingleKey_NotPresent_ReturnsEmptyString()
    {
        var getter = CreateHeadersGetter(new Dictionary<string, string>());

        var value = getter.Object.GetHeader("context", "traceparent");

        Assert.Equal(string.Empty, value);
    }

    [Fact]
    public void GetHeader_MultipleKeys_ReturnsFirstPresentInGivenOrder()
    {
        var getter = CreateHeadersGetter(new Dictionary<string, string> { { "second-choice", "found-it" } });

        var value = getter.Object.GetHeader("context", new[] { "first-choice", "second-choice" });

        Assert.Equal("found-it", value);
    }

    [Fact]
    public void GetHeader_MultipleKeys_SkipsAnEmptyValueAndTriesTheNextKey()
    {
        var getter = CreateHeadersGetter(new Dictionary<string, string>
        {
            { "first-choice", "" },
            { "second-choice", "found-it" }
        });

        var value = getter.Object.GetHeader("context", new[] { "first-choice", "second-choice" });

        Assert.Equal("found-it", value);
    }
}
