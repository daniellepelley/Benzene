using System.Linq;
using Benzene.Http.Routing;
using Xunit;

namespace Benzene.Test.Http;

/// <summary>
/// The path-based HTTP versioning policy (Slice 2): <c>AddHttpVersioning()</c> re-emits each route under a
/// <c>/v{version}/…</c> segment so <c>/v1/orders</c> reaches the same topic with the version captured, while
/// keeping the unversioned route resolving to latest. Verified through <see cref="VersionedHttpEndpointFinder"/>
/// and the real <see cref="RouteFinder"/>.
/// </summary>
public class VersionedHttpEndpointFinderTest
{
    private sealed class StubFinder : IHttpEndpointFinder
    {
        private readonly IHttpEndpointDefinition[] _definitions;
        public StubFinder(params IHttpEndpointDefinition[] definitions) => _definitions = definitions;
        public IHttpEndpointDefinition[] FindDefinitions() => _definitions;
    }

    private static VersionedHttpEndpointFinder Versioned(HttpVersioningOptions? options = null, params IHttpEndpointDefinition[] defs)
        => new(new StubFinder(defs), options ?? new HttpVersioningOptions());

    [Fact]
    public void EmitsBothTheUnversionedAndTheVersionedRoute_ByDefault()
    {
        var finder = Versioned(defs: new HttpEndpointDefinition("POST", "/orders", "order:create"));

        var paths = finder.FindDefinitions().Select(d => d.Path).ToArray();

        Assert.Contains("/orders", paths);
        Assert.Contains("/v{version}/orders", paths);
        Assert.All(finder.FindDefinitions(), d => Assert.Equal("order:create", d.Topic));
    }

    [Fact]
    public void KeepUnversionedRouteFalse_EmitsOnlyTheVersionedRoute()
    {
        var finder = Versioned(new HttpVersioningOptions { KeepUnversionedRoute = false },
            new HttpEndpointDefinition("POST", "/orders", "order:create"));

        var paths = finder.FindDefinitions().Select(d => d.Path).ToArray();

        Assert.Equal(new[] { "/v{version}/orders" }, paths);
    }

    [Fact]
    public void AHandVersionedRoute_IsPassedThroughUntouched()
    {
        // The app already declared the {version} parameter itself - respect it, don't double-prefix.
        var finder = Versioned(defs: new HttpEndpointDefinition("GET", "/v{version}/orders/{id}", "order:get"));

        var paths = finder.FindDefinitions().Select(d => d.Path).ToArray();

        Assert.Equal(new[] { "/v{version}/orders/{id}" }, paths);
    }

    [Fact]
    public void CustomSegment_IsApplied()
    {
        var finder = Versioned(new HttpVersioningOptions { VersionSegment = "version/{version}" },
            new HttpEndpointDefinition("GET", "/orders", "order:get"));

        Assert.Contains("/version/{version}/orders", finder.FindDefinitions().Select(d => d.Path));
    }

    [Fact]
    public void RouteFinder_MatchesTheVersionedPath_CapturingTheVersion()
    {
        var routeFinder = new RouteFinder(Versioned(defs:
            new HttpEndpointDefinition("GET", "/orders/{id}", "order:get")));

        var versioned = routeFinder.Find("GET", "/v2/orders/42");

        Assert.NotNull(versioned);
        Assert.Equal("order:get", versioned!.Topic);
        Assert.Equal("2", versioned.Parameters[HttpVersioningOptions.VersionParameterName]);
        Assert.Equal("42", versioned.Parameters["id"]);
    }

    [Fact]
    public void RouteFinder_StillMatchesTheUnversionedPath_WithNoVersionCaptured()
    {
        var routeFinder = new RouteFinder(Versioned(defs:
            new HttpEndpointDefinition("GET", "/orders/{id}", "order:get")));

        var bare = routeFinder.Find("GET", "/orders/42");

        Assert.NotNull(bare);
        Assert.Equal("order:get", bare!.Topic);
        Assert.False(bare.Parameters.ContainsKey(HttpVersioningOptions.VersionParameterName));
    }
}
