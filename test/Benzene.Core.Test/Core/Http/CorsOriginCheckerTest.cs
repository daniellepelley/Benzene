using System.Collections.Generic;
using Benzene.Http.Cors;
using Xunit;

namespace Benzene.Test.Core.Http;

public class CorsOriginCheckerTest
{
    private readonly CorsOriginChecker _corsOriginChecker;
    private string[] _allowedDomains = { "example.com", "foo.bar" };

    public CorsOriginCheckerTest()
    {
        _corsOriginChecker = new CorsOriginChecker();
    }


    [Theory]
    [InlineData("example.com", "example.com")]
    [InlineData("https://example.com", "https://example.com")]
    [InlineData("http://example.com", "http://example.com")]
    [InlineData("http://example.com/", "http://example.com/")]
    [InlineData("http://example.com/foo", "http://example.com/foo")]
    [InlineData("foo.bar", "foo.bar")]
    [InlineData("http://foo.bar/foo", "http://foo.bar/foo")]
    [InlineData("http://example1.com/foo", null)]
    [InlineData("http://example1.com1/foo", null)]
    public void MatchesPath(string origin, string expected)
    {
        var httpRequest = new HttpRequest2
        {
            Headers = new Dictionary<string, string>
            {
                { "origin", origin }
            }
        };
        
        var actual = _corsOriginChecker.MatchOrigin(_allowedDomains, httpRequest);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NoHeader()
    {
        var httpRequest = new HttpRequest2();
        
        var actual = _corsOriginChecker.MatchOrigin(_allowedDomains, httpRequest);
        Assert.Null(actual);
    }
}
