using Benzene.Http;
using Benzene.Test.Examples;
using Moq;
using Xunit;

namespace Benzene.Test.Core.Http;

public class RouteFinderTest
{
    public RouteFinder CreateRouteFinder(string handlerMethod, string handlerPath)
    {
        var mockHttpEndpointFinder = new Mock<IHttpEndpointFinder>();
        mockHttpEndpointFinder.Setup(x => x.FindDefinitions())
            .Returns(new[] { new HttpEndpointDefinition(handlerMethod, handlerPath, Defaults.Topic) });

        return new RouteFinder(mockHttpEndpointFinder.Object);
    }
    
    [Theory]
    [InlineData("GET", "/example", "/example")]
    [InlineData("GET", "/example/", "/example/")]
    [InlineData("GET", "example", "example")]
    [InlineData("GET", "/example", "example")]
    [InlineData("get", "/example","example/")]
    [InlineData("gET", "/EXample/", "/EXAMPLE")]
    [InlineData("GET", "example", "/example")]
    [InlineData("GET", "example/42", "/example/{id}")]
    [InlineData("GET", "example/42_foo", "/example/{id}")]
    [InlineData("GET", "example/42-foo", "/example/{id}")]
    [InlineData("GET", "example-42-foo", "/example-{id}-foo")]
    public void FindsTopic(string method, string path, string handlerPath)
    {
        var routeFinder = CreateRouteFinder("GET", handlerPath);

        var httpTopicRoute = routeFinder.Find(method, path);

        Assert.Equal(Defaults.Topic, httpTopicRoute.Topic);
    }
    
    [Theory]
    [InlineData("GET", "/example", "examples")]
    [InlineData("get", "/example","example/action")]
    [InlineData("gET", "/EXample/", "/foo/EXAMPLE")]
    [InlineData("GET", "example", "/example/{id}")]
    [InlineData("GET", "example/34/action", "/example/{id}")]
    public void DoesNotFindsTopic(string method, string path, string handlerPath)
    {
        var routeFinder = CreateRouteFinder("GET", handlerPath);

        var httpTopicRoute = routeFinder.Find(method, path);

        Assert.Null(httpTopicRoute);
    }
    
    [Theory]
    [InlineData("GET", "example/34", "/example/{id}")]
    [InlineData("GET", "example/34/action", "/example/{id}/action")]
    public void FindWithParameters(string method, string path, string handlerPath)
    {
        var routeFinder = CreateRouteFinder("GET", handlerPath);

        var httpTopicRoute = routeFinder.Find(method, path);

        Assert.Equal(Defaults.Topic, httpTopicRoute.Topic);
        Assert.Equal("34", httpTopicRoute.Parameters["id"]);
    }
}
