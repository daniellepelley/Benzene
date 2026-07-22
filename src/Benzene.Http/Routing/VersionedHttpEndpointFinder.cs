using System;
using System.Collections.Generic;
using System.Linq;

namespace Benzene.Http.Routing;

/// <summary>
/// Decorates an <see cref="IHttpEndpointFinder"/> to apply the path-based versioning policy
/// (<see cref="HttpVersioningOptions"/>): each discovered route is re-emitted under a version segment
/// (<c>/v{version}/…</c>) so path-versioned requests reach the same topic, optionally keeping the original
/// unversioned route (which resolves to the latest handler). A route the app already declared with the
/// <c>{version}</c> parameter is treated as a hand-authored override and passed through untouched.
/// <para>
/// Registered by <c>AddHttpVersioning()</c> in place of the composite finder, so both the route table
/// (<see cref="RouteFinder"/>) and the generated spec see the versioned paths — no per-transport change.
/// </para>
/// </summary>
public class VersionedHttpEndpointFinder : IHttpEndpointFinder
{
    private static readonly string VersionParamToken = "{" + HttpVersioningOptions.VersionParameterName + "}";

    private readonly IHttpEndpointFinder _inner;
    private readonly HttpVersioningOptions _options;

    /// <summary>Initializes a new instance wrapping <paramref name="inner"/> with <paramref name="options"/>.</summary>
    public VersionedHttpEndpointFinder(IHttpEndpointFinder inner, HttpVersioningOptions options)
    {
        _inner = inner;
        _options = options;
    }

    /// <inheritdoc />
    public IHttpEndpointDefinition[] FindDefinitions()
    {
        var result = new List<IHttpEndpointDefinition>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in _inner.FindDefinitions())
        {
            // A route the app authored with the {version} parameter is an explicit override: pass it through
            // as-is (do not also add an auto-versioned or unversioned twin).
            if (definition.Path.Contains(VersionParamToken, StringComparison.OrdinalIgnoreCase))
            {
                Add(result, seen, definition);
                continue;
            }

            if (_options.KeepUnversionedRoute)
            {
                Add(result, seen, definition);
            }

            Add(result, seen, new HttpEndpointDefinition(definition.Method, PrefixVersion(definition.Path), definition.Topic));
        }

        return result.ToArray();
    }

    private string PrefixVersion(string path)
        => "/" + _options.VersionSegment.Trim('/') + "/" + path.TrimStart('/');

    private static void Add(List<IHttpEndpointDefinition> result, HashSet<string> seen, IHttpEndpointDefinition definition)
    {
        // Dedup on (method, path) so a hand-versioned override plus an auto-versioned twin can't collide into
        // the reflection finder's duplicate-route error.
        if (seen.Add(definition.Method + " " + definition.Path))
        {
            result.Add(definition);
        }
    }
}
