using System.Collections.Generic;
using Benzene.Http;
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
        var httpRequest = new HttpRequest
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
        var httpRequest = new HttpRequest();

        var actual = _corsOriginChecker.MatchOrigin(_allowedDomains, httpRequest);
        Assert.Null(actual);
    }

    [Theory]
    [InlineData("https://anything.example")]
    [InlineData("https://another-origin.test")]
    public void Wildcard_MatchesAnyOrigin(string origin)
    {
        var httpRequest = new HttpRequest
        {
            Headers = new Dictionary<string, string>
            {
                { "origin", origin }
            }
        };

        var actual = _corsOriginChecker.MatchOrigin(new[] { "*" }, httpRequest);

        // The actual origin is always echoed back rather than a literal "*", so the header
        // stays valid even when Access-Control-Allow-Credentials is also set.
        Assert.Equal(origin, actual);
    }

    [Theory]
    // A config entry with an explicit scheme is an exact origin (scheme+host+port) match:
    // it must not also allow a different scheme or port on the same host.
    [InlineData("https://secure.example", "http://secure.example", null)]
    [InlineData("https://secure.example", "https://secure.example:8443", null)]
    [InlineData("https://secure.example", "https://secure.example", "https://secure.example")]
    [InlineData("https://secure.example:8443", "https://secure.example:8443", "https://secure.example:8443")]
    // A bare-hostname config entry is host-only (scheme/port agnostic), preserving the
    // documented shorthand form.
    [InlineData("secure.example", "http://secure.example", "http://secure.example")]
    [InlineData("secure.example", "https://secure.example:8443", "https://secure.example:8443")]
    public void ExactOriginMatch_RespectsSchemeAndPort(string allowedDomain, string origin, string expected)
    {
        var httpRequest = new HttpRequest
        {
            Headers = new Dictionary<string, string>
            {
                { "origin", origin }
            }
        };

        var actual = _corsOriginChecker.MatchOrigin(new[] { allowedDomain }, httpRequest);
        Assert.Equal(expected, actual);
    }
}
