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
        var routerPathParts = SplitPath(routerPath);
        var pathParts = SplitPath(path);

        if (routerPathParts.Length != pathParts.Length)
        {
            return null;
        }

        var output = new Dictionary<string, object>();

        for (int i = 0; i < routerPathParts.Length; i++)
        {
            var routeParts = SplitRouterPath(routerPathParts[i]);
            var segment = pathParts[i];

            var paramIndex = Array.FindIndex(routeParts, x => x.StartsWith("{"));

            if (paramIndex < 0)
            {
                // A literal-only segment must match exactly.
                if (segment != string.Concat(routeParts))
                {
                    return null;
                }

                continue;
            }

            // The one parameter's value is whatever sits between the anchored literal prefix (the
            // parts before it) and suffix (the parts after it). Anchoring to the ends - rather than
            // the old global String.Replace of each literal - is what stops a literal that also appears
            // inside the parameter value from corrupting it (e.g. "/{slug}s" matching "/dogss" gave
            // "dog" instead of "dogs") or wrongly failing the match.
            var prefix = string.Concat(routeParts.Take(paramIndex));
            var suffix = string.Concat(routeParts.Skip(paramIndex + 1));

            if (segment.Length < prefix.Length + suffix.Length
                || !segment.StartsWith(prefix)
                || !segment.EndsWith(suffix))
            {
                return null;
            }

            var value = segment.Substring(prefix.Length, segment.Length - prefix.Length - suffix.Length);
            if (value == "")
            {
                continue;
            }

            output[routeParts[paramIndex].Replace("{", "").Replace("}", "")] = value;
        }


        return output;
    }

    private string[] SplitPath(string path)
    {
        return path
            .Split('?', StringSplitOptions.RemoveEmptyEntries)
            .First()
            .ToLowerInvariant()
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
    }

    private string[] SplitRouterPath(string routerPath)
    {
        return Regex.Split(routerPath
                .Replace("/", ""), @"(?<=\})|(?=\{)")
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();
    }

}

