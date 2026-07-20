using System.Collections.Generic;
using Benzene.AspNet.Core;
using Benzene.Http;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Benzene.Test.Asp;

/// <summary>
/// Characterization coverage for <see cref="AspNetMessageHeadersGetter"/> after it was rewritten from
/// a per-header LINQ/GroupBy chain to a single TryGetValue/TryAdd pass: unmapped headers pass through
/// under their own name, configured headers are renamed, and a name collision keeps the first entry
/// (the old <c>GroupBy(...).Select(g => g.First())</c> semantics).
/// </summary>
public class AspNetMessageHeadersGetterTest
{
    private sealed class FixedMappings : IHttpHeaderMappings
    {
        private readonly IDictionary<string, string> _mappings;
        public FixedMappings(IDictionary<string, string> mappings) => _mappings = mappings;
        public IDictionary<string, string> GetMappings() => _mappings;
    }

    private static AspNetContext ContextWithHeaders(params (string Key, string Value)[] headers)
    {
        var httpContext = new DefaultHttpContext();
        foreach (var (key, value) in headers)
        {
            httpContext.Request.Headers[key] = value;
        }

        return new AspNetContext(httpContext);
    }

    [Fact]
    public void GetHeaders_NoMappings_PassesEveryHeaderThroughUnderItsOwnName()
    {
        var getter = new AspNetMessageHeadersGetter(new DefaultHttpHeaderMappings());

        var result = getter.GetHeaders(ContextWithHeaders(("x-foo", "1"), ("x-bar", "2")));

        Assert.Equal("1", result["x-foo"]);
        Assert.Equal("2", result["x-bar"]);
    }

    [Fact]
    public void GetHeaders_MappedHeader_IsRenamedToItsMappedField()
    {
        var getter = new AspNetMessageHeadersGetter(
            new FixedMappings(new Dictionary<string, string> { { "x-user-id", "userId" } }));

        var result = getter.GetHeaders(ContextWithHeaders(("X-User-Id", "u-42"), ("x-other", "keep")));

        // The mapping key-matches case-insensitively (the getter lower-cases the header name).
        Assert.Equal("u-42", result["userId"]);
        Assert.False(result.ContainsKey("X-User-Id"));
        Assert.Equal("keep", result["x-other"]);
    }

    [Fact]
    public void GetHeaders_TwoHeadersMapToTheSameName_KeepsTheFirst()
    {
        var getter = new AspNetMessageHeadersGetter(
            new FixedMappings(new Dictionary<string, string> { { "a", "same" }, { "b", "same" } }));

        var result = getter.GetHeaders(ContextWithHeaders(("a", "first"), ("b", "second")));

        // Both map to "same"; first-wins, matching the old GroupBy(...).First().
        Assert.Equal("first", result["same"]);
    }
}
