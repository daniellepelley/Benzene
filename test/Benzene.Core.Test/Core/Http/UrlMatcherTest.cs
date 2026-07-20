using Benzene.Http.Routing;
using Xunit;

namespace Benzene.Test.Core.Http;

public class UrlMatcherTest
{
    private readonly UrlMatcher _urlMatcher;

    public UrlMatcherTest()
    {
        _urlMatcher = new UrlMatcher();
    }
    
    
    [Theory]
    [InlineData("/example", "/example")]
    [InlineData("/example/", "/example/")]
    [InlineData("example", "example")]
    [InlineData("/example", "example")]
    [InlineData( "/example","example/")]
    [InlineData( "/EXample/", "/EXAMPLE")]
    [InlineData( "example", "/example")]
    [InlineData( "example/42", "/example/{id}")]
    [InlineData( "example/42_foo", "/example/{id}")]
    [InlineData( "example/42-foo", "/example/{id}")]
    [InlineData( "example/42-foo", "/example/{id}-foo")]
    [InlineData( "example-42-foo", "/example-{id}-foo")]
    public void MatchesPath(string path, string handlerPath)
    {
        var  parameters = _urlMatcher.MatchUrl(path, handlerPath);
        Assert.NotNull(parameters);
    }
    
    [Theory]
    [InlineData( "/example", "examples")]
    [InlineData( "/example","example/action")]
    [InlineData( "/EXample/", "/foo/EXAMPLE")]
    [InlineData( "example", "/example/{id}")]
    [InlineData( "example/34/action", "/example/{id}")]
    public void DoesNotMatchPath(string path, string handlerPath)
    {
        var parameters = _urlMatcher.MatchUrl(path, handlerPath);
        Assert.Null(parameters);
    }
    
    [Theory]
    [InlineData( "example/42", "/example/{id}")]
    [InlineData( "example/42/action", "/example/{id}/action")]
    [InlineData( "example-42-foo", "/example-{id}-foo")]
    [InlineData( "42-foo", "/{id}-foo")]
    [InlineData( "example-42foo", "/example-{id}foo")]
    public void FindWithParameters(string path, string handlerPath)
    {
        var parameters = _urlMatcher.MatchUrl(path, handlerPath);
        Assert.Equal("42", parameters["id"]);
    }

    [Theory]
    // The parameter value itself contains the segment's literal text. Anchoring the literal to the
    // ends (not a global String.Replace) is what extracts the full value: "dogss" against "/{slug}s"
    // is "dogs" (not "dog"), "xax" against "/x{id}" is "ax" (not "a").
    [InlineData("dogss", "/{slug}s", "slug", "dogs")]
    [InlineData("xax", "/x{id}", "id", "ax")]
    public void FindWithParameters_ValueOverlapsSegmentLiteral(string path, string handlerPath, string key, string expected)
    {
        var parameters = _urlMatcher.MatchUrl(path, handlerPath);

        Assert.NotNull(parameters);
        Assert.Equal(expected, parameters[key]);
    }
}
