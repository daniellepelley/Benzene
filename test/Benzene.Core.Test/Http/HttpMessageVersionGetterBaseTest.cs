using System.Collections.Generic;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Http.Routing;
using Xunit;

namespace Benzene.Test.Http;

public class HttpMessageVersionGetterBaseTest
{
    private class TestContext
    {
        public TestContext(string method, string path)
        {
            Method = method;
            Path = path;
        }

        public string Method { get; }
        public string Path { get; }
    }

    private class FixedRouteFinder : IRouteFinder
    {
        private readonly HttpTopicRoute? _route;

        public FixedRouteFinder(HttpTopicRoute? route)
        {
            _route = route;
        }

        public HttpTopicRoute? Find(string method, string path) => _route;
    }

    private class FixedHeadersGetter : IMessageHeadersGetter<TestContext>
    {
        private readonly IDictionary<string, string> _headers;

        public FixedHeadersGetter(IDictionary<string, string> headers)
        {
            _headers = headers;
        }

        public IDictionary<string, string> GetHeaders(TestContext context) => _headers;
    }

    private class TestMessageVersionGetter : HttpMessageVersionGetterBase<TestContext>
    {
        public TestMessageVersionGetter(IRouteFinder routeFinder, IMessageHeadersGetter<TestContext> headersGetter)
            : base(routeFinder, headersGetter)
        {
        }

        protected override (string Method, string Path) GetMethodAndPath(TestContext context) => (context.Method, context.Path);
    }

    [Fact]
    public void GetVersion_RouteHasVersionParameter_ReturnsIt()
    {
        var route = new HttpTopicRoute("order:create", new Dictionary<string, object> { ["version"] = "V1" });
        var getter = new TestMessageVersionGetter(new FixedRouteFinder(route), new FixedHeadersGetter(new Dictionary<string, string>
        {
            ["benzene-version"] = "V2"
        }));

        // The route parameter wins over the header fallback when both are present.
        Assert.Equal("V1", getter.GetVersion(new TestContext("GET", "/v1/orders")));
    }

    [Fact]
    public void GetVersion_RouteMatchedButHasNoVersionParameter_FallsBackToHeaders()
    {
        var route = new HttpTopicRoute("order:create", new Dictionary<string, object> { ["id"] = "42" });
        var getter = new TestMessageVersionGetter(new FixedRouteFinder(route), new FixedHeadersGetter(new Dictionary<string, string>
        {
            ["benzene-version"] = "V2"
        }));

        Assert.Equal("V2", getter.GetVersion(new TestContext("GET", "/orders/42")));
    }

    [Fact]
    public void GetVersion_NoRouteMatched_FallsBackToHeaders()
    {
        var getter = new TestMessageVersionGetter(new FixedRouteFinder(null), new FixedHeadersGetter(new Dictionary<string, string>
        {
            ["version"] = "V1"
        }));

        Assert.Equal("V1", getter.GetVersion(new TestContext("GET", "/unknown")));
    }

    [Fact]
    public void GetVersion_NoRouteMatchAndNoHeaders_ReturnsNull()
    {
        var getter = new TestMessageVersionGetter(new FixedRouteFinder(null), new FixedHeadersGetter(new Dictionary<string, string>()));

        Assert.Null(getter.GetVersion(new TestContext("GET", "/unknown")));
    }
}
