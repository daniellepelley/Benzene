using Benzene.Http.Routing;
using BenchmarkDotNet.Attributes;

namespace Benzene.Benchmarks;

/// <summary>
/// Benchmarks <see cref="RouteFinder.Find"/> - the per-request HTTP route match. <see cref="RouteCount"/>
/// is parameterized because a miss scans every registered route, and the cost this suite targets is the
/// per-route pattern work: the finder now compiles each route's method (lower-cased) and path pattern
/// (split + regex) once at construction and splits only the incoming path per request, instead of
/// re-splitting and re-running <c>Regex.Split</c> over every route's pattern on every request. Both a
/// hit (first route) and a miss (scans all) are measured.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class RouteFindingBenchmarks
{
    private sealed class FakeEndpoint : IHttpEndpointDefinition
    {
        public string Method { get; init; } = "GET";
        public string Path { get; init; } = "/";
        public string Topic { get; init; } = "topic";
    }

    private sealed class FakeFinder : IHttpEndpointFinder
    {
        private readonly IHttpEndpointDefinition[] _definitions;
        public FakeFinder(IHttpEndpointDefinition[] definitions) => _definitions = definitions;
        public IHttpEndpointDefinition[] FindDefinitions() => _definitions;
    }

    [Params(5, 25, 100)]
    public int RouteCount { get; set; }

    private RouteFinder _routeFinder = null!;
    private string _hitPath = null!;
    private string _missPath = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var definitions = new IHttpEndpointDefinition[RouteCount];
        for (var i = 0; i < RouteCount; i++)
        {
            // A parameterized path per route so matching does the prefix/suffix + extraction work.
            definitions[i] = new FakeEndpoint { Method = "GET", Path = $"/resource{i}/{{id}}/detail", Topic = $"topic-{i}" };
        }

        _routeFinder = new RouteFinder(new FakeFinder(definitions));
        // A hit against the first-registered route, and a miss that forces a scan of every route.
        _hitPath = "/resource0/123/detail";
        _missPath = "/does/not/exist/anywhere";
    }

    [Benchmark(Description = "Find: hit (first route, extracts a parameter)")]
    public HttpTopicRoute? Find_Hit() => _routeFinder.Find("GET", _hitPath);

    [Benchmark(Description = "Find: miss (scans every route)")]
    public HttpTopicRoute? Find_Miss() => _routeFinder.Find("GET", _missPath);
}
