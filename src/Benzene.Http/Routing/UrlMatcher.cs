using System.Text.RegularExpressions;

namespace Benzene.Http.Routing;

public class UrlMatcher
{
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

