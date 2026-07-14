using Benzene.Core.Messages.Helper;
using Xunit;

namespace Benzene.Test.Core.Core.Helper;

public class MediaTypeTest
{
    [Theory]
    [InlineData("application/xml", "application/xml")]
    [InlineData("application/xml; charset=utf-8", "application/xml")]
    [InlineData("application/xml;charset=utf-8", "application/xml")]
    [InlineData("Application/XML", "application/xml")]
    [InlineData("APPLICATION/XML; CHARSET=UTF-8", "application/xml")]
    [InlineData("  application/xml  ", "application/xml")]
    public void Matches_True(string headerValue, string mediaType)
    {
        Assert.True(MediaType.Matches(headerValue, mediaType));
    }

    [Theory]
    [InlineData("application/json", "application/xml")]
    [InlineData(null, "application/xml")]
    [InlineData("", "application/xml")]
    [InlineData("application/xml-extended", "application/xml")]
    public void Matches_False(string headerValue, string mediaType)
    {
        Assert.False(MediaType.Matches(headerValue, mediaType));
    }
}
