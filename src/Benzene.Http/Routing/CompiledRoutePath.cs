using System;
using System.Linq;

namespace Benzene.Http.Routing;

/// <summary>
/// A route pattern (e.g. <c>/users/{id}</c>) with its per-segment parsing done <em>once</em>, up
/// front, so matching an incoming path is a cheap set of string comparisons instead of re-splitting
/// the pattern and running <see cref="System.Text.RegularExpressions.Regex"/> over it on every
/// request. <see cref="UrlMatcher.MatchUrl"/> and <see cref="RouteFinder"/> both match through this;
/// <see cref="RouteFinder"/> compiles each route once at construction and reuses it for the process
/// lifetime.
/// </summary>
/// <remarks>
/// Semantics: literal segments (and a parameter's literal prefix/suffix) match case-insensitively,
/// while a parameter's extracted <em>value</em> is preserved verbatim (case-sensitive ids/slugs/tokens);
/// a single parameter per segment with an optional literal prefix/suffix; and a parameter that resolves
/// to an empty value is not a match. This only moves the constant, per-pattern work off the request path.
/// </remarks>
internal sealed class CompiledRoutePath
{
    private readonly Segment[] _segments;

    public CompiledRoutePath(string routerPath)
    {
        var routerPathParts = UrlMatcher.SplitPath(routerPath);
        _segments = new Segment[routerPathParts.Length];

        for (var i = 0; i < routerPathParts.Length; i++)
        {
            var routeParts = UrlMatcher.SplitRouterPath(routerPathParts[i]);
            var paramIndex = Array.FindIndex(routeParts, x => x.StartsWith("{"));

            _segments[i] = paramIndex < 0
                ? Segment.CreateLiteral(string.Concat(routeParts))
                : Segment.CreateParameter(
                    routeParts[paramIndex].Replace("{", "").Replace("}", ""),
                    string.Concat(routeParts.Take(paramIndex)),
                    string.Concat(routeParts.Skip(paramIndex + 1)));
        }
    }

    /// <summary>
    /// Matches already-split, already-case-folded incoming path segments (see
    /// <see cref="UrlMatcher.SplitPath"/>) against this pattern, extracting parameter values.
    /// </summary>
    /// <param name="pathParts">The incoming path split into segments.</param>
    /// <returns>The extracted route parameters if the path matches, otherwise <c>null</c>.</returns>
    public IDictionary<string, object>? Match(string[] pathParts)
    {
        if (pathParts.Length != _segments.Length)
        {
            return null;
        }

        var output = new Dictionary<string, object>();

        for (var i = 0; i < _segments.Length; i++)
        {
            var seg = _segments[i];
            var segment = pathParts[i];

            if (seg.IsLiteral)
            {
                // Case-insensitive literal matching, but against the original-case incoming segment
                // (not a pre-folded copy) so a parameter value in a sibling segment keeps its case.
                if (!string.Equals(segment, seg.Literal, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                continue;
            }

            if (segment.Length < seg.Prefix.Length + seg.Suffix.Length
                || !segment.StartsWith(seg.Prefix, StringComparison.OrdinalIgnoreCase)
                || !segment.EndsWith(seg.Suffix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var value = segment.Substring(seg.Prefix.Length, segment.Length - seg.Prefix.Length - seg.Suffix.Length);
            if (value == "")
            {
                // The literal prefix/suffix matched but the parameter itself is empty (e.g.
                // "/example--foo" against "/example-{id}-foo"). A required parameter with no value is
                // not a match - returning the dictionary here would dispatch to the handler with the
                // parameter absent (bound to null/default). Treat it as no match (404).
                return null;
            }

            output[seg.ParamName] = value;
        }

        return output;
    }

    private readonly struct Segment
    {
        private Segment(bool isLiteral, string literal, string paramName, string prefix, string suffix)
        {
            IsLiteral = isLiteral;
            Literal = literal;
            ParamName = paramName;
            Prefix = prefix;
            Suffix = suffix;
        }

        public bool IsLiteral { get; }
        public string Literal { get; }
        public string ParamName { get; }
        public string Prefix { get; }
        public string Suffix { get; }

        public static Segment CreateLiteral(string literal) => new(true, literal, string.Empty, string.Empty, string.Empty);

        public static Segment CreateParameter(string paramName, string prefix, string suffix)
            => new(false, string.Empty, paramName, prefix, suffix);
    }
}
