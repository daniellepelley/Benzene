using Benzene.Schema.OpenApi;
using Xunit;

namespace Benzene.Test.Autogen.Schema.OpenApi;

/// <summary>
/// The spec is deterministic for a given (type, format), so <see cref="SpecCache"/> memoizes the
/// finished document and only the first request for each combination pays the full
/// schema-generation build - the caching that stops the mesh aggregator's repeated polling from
/// re-running that build every time.
/// </summary>
public class SpecCacheTest
{
    [Fact]
    public void GetOrBuild_SameTypeAndFormat_BuildsOnceThenReuses()
    {
        var cache = new SpecCache();
        var builds = 0;

        var first = cache.GetOrBuild(new SpecRequest("benzene", "json"), _ => { builds++; return "doc"; });
        var second = cache.GetOrBuild(new SpecRequest("benzene", "json"), _ => { builds++; return "doc"; });

        Assert.Equal("doc", first);
        Assert.Equal("doc", second);
        Assert.Equal(1, builds);
    }

    [Fact]
    public void GetOrBuild_DifferentTypeOrFormat_BuildsPerCombination()
    {
        var cache = new SpecCache();
        var builds = 0;
        string Build(SpecRequest r) { builds++; return $"{r.Type}-{r.Format}"; }

        cache.GetOrBuild(new SpecRequest("benzene", "json"), Build);
        cache.GetOrBuild(new SpecRequest("openapi", "json"), Build);
        cache.GetOrBuild(new SpecRequest("benzene", "yaml"), Build);
        cache.GetOrBuild(new SpecRequest("benzene", "json"), Build); // repeat of the first

        Assert.Equal(3, builds);
    }

    [Fact]
    public void GetOrBuild_TypeIsCaseInsensitive_MatchingTheBuilder()
    {
        // SpecBuilder lower-cases Type, so "Benzene" and "benzene" resolve to the same document.
        var cache = new SpecCache();
        var builds = 0;

        cache.GetOrBuild(new SpecRequest("Benzene", "json"), _ => { builds++; return "doc"; });
        cache.GetOrBuild(new SpecRequest("benzene", "json"), _ => { builds++; return "doc"; });

        Assert.Equal(1, builds);
    }

    [Fact]
    public void GetOrBuild_FormatIsCaseSensitive_SoYamlAndUppercaseDoNotCollide()
    {
        // SpecBuilder compares Format with `== "yaml"` exactly: "YAML" is treated as JSON, so it must
        // NOT share a cache entry with the real "yaml" document.
        var cache = new SpecCache();
        var builds = 0;

        cache.GetOrBuild(new SpecRequest("benzene", "yaml"), _ => { builds++; return "yaml-doc"; });
        var upper = cache.GetOrBuild(new SpecRequest("benzene", "YAML"), _ => { builds++; return "json-doc"; });

        Assert.Equal(2, builds);
        Assert.Equal("json-doc", upper);
    }
}
