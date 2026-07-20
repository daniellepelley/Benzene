using System.Collections.Generic;
using Benzene.Http.Routing;
using Xunit;

namespace Benzene.Test.Core.Http;

/// <summary>
/// Coverage for <see cref="MemoizingRouteFinder"/>: it returns the inner finder's result but only
/// calls it once for a repeated method+path (the topic getter, version getter, and enricher all
/// resolve the same route per request), and recomputes when either changes.
/// </summary>
public class MemoizingRouteFinderTest
{
    private sealed class CountingRouteFinder : IRouteFinder
    {
        public int Calls { get; private set; }

        public HttpTopicRoute? Find(string method, string path)
        {
            Calls++;
            return new HttpTopicRoute($"{method}:{path}", new Dictionary<string, object>());
        }
    }

    [Fact]
    public void Find_RepeatedMethodAndPath_CallsTheInnerFinderOnce()
    {
        var inner = new CountingRouteFinder();
        var memoizing = new MemoizingRouteFinder(inner);

        var first = memoizing.Find("GET", "/users/5");
        var second = memoizing.Find("GET", "/users/5");
        var third = memoizing.Find("GET", "/users/5");

        Assert.Equal(1, inner.Calls);
        Assert.Equal("GET:/users/5", first!.Topic);
        Assert.Same(first, second);
        Assert.Same(first, third);
    }

    [Fact]
    public void Find_DifferentPath_Recomputes()
    {
        var inner = new CountingRouteFinder();
        var memoizing = new MemoizingRouteFinder(inner);

        var a = memoizing.Find("GET", "/users/5");
        var b = memoizing.Find("GET", "/users/6");

        Assert.Equal(2, inner.Calls);
        Assert.Equal("GET:/users/5", a!.Topic);
        Assert.Equal("GET:/users/6", b!.Topic);
    }

    [Fact]
    public void Find_DifferentMethodSamePath_Recomputes()
    {
        var inner = new CountingRouteFinder();
        var memoizing = new MemoizingRouteFinder(inner);

        memoizing.Find("GET", "/users/5");
        var post = memoizing.Find("POST", "/users/5");

        Assert.Equal(2, inner.Calls);
        Assert.Equal("POST:/users/5", post!.Topic);
    }
}
