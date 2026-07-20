using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Benzene.Http.Routing;

/// <summary>
/// Provides URL pattern matching functionality for HTTP routing, including extraction of route parameters.
/// </summary>
/// <remarks>
/// This class compares incoming URL paths against route patterns and extracts parameter values
/// from parameterized route segments (e.g., extracting "123" from "/users/123" when matched
/// against "/users/{id}"). Matching is case-insensitive and supports complex patterns with
/// multiple parameters and literal text segments.
/// <para>
/// The per-request matching is delegated to <see cref="CompiledRoutePath"/>. On the hot path,
/// <see cref="RouteFinder"/> compiles each route once at construction; this convenience method
/// compiles the pattern per call, which is fine for one-off callers (e.g. CORS route matching).
/// </para>
/// </remarks>
public class UrlMatcher
{
    /// <summary>
    /// Matches a URL path against a route pattern and extracts route parameters.
    /// </summary>
    /// <param name="path">The incoming URL path to match.</param>
    /// <param name="routerPath">The route pattern to match against, which may contain parameters (e.g., "/users/{id}").</param>
    /// <returns>
    /// A dictionary containing the extracted route parameters if the path matches the pattern,
    /// or <c>null</c> if there is no match.
    /// </returns>
    public IDictionary<string, object>? MatchUrl(string path, string routerPath)
    {
        return new CompiledRoutePath(routerPath).Match(SplitPath(path));
    }

    /// <summary>Splits an incoming URL path (dropping any query string) into case-folded segments.</summary>
    internal static string[] SplitPath(string path)
    {
        return path
            .Split('?', StringSplitOptions.RemoveEmptyEntries)
            .First()
            .ToLowerInvariant()
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>Splits a single route-pattern segment into its literal and <c>{parameter}</c> parts.</summary>
    internal static string[] SplitRouterPath(string routerPath)
    {
        return Regex.Split(routerPath
                .Replace("/", ""), @"(?<=\})|(?=\{)")
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();
    }
}
