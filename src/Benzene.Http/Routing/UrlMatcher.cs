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

            var parameterKeys = routeParts.Where(x => x.StartsWith("{")).ToArray();
            var nonParameterKeys = routeParts.Where(x => !x.StartsWith("{")).ToArray();

            var value = RemoveParts(pathParts[i], nonParameterKeys);

            if (value == null)
            {
                return null;
            }

            if (value == "")
            {
                continue;
            }

            if (parameterKeys.Any())
            {
                output[parameterKeys[0].Replace("{", "").Replace("}", "")] = value;
                continue;
            }

            return null;
        }


        return output;
    }

    private string RemoveParts(string input, string[] removeParts)
    {
        return removeParts.Aggregate(input, (s, s1) =>
        {
            if (s.Split(s1, StringSplitOptions.RemoveEmptyEntries).Length > 1)
            {
                return null;
            }

            return s.Replace(s1, "");
        });
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

