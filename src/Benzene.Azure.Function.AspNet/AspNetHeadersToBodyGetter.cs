using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Azure.Function.AspNet;

/// <summary>
/// Extracts a fixed set of HTTP headers (currently just <c>x-user-id</c>) and maps them to body-friendly
/// field names, for use by <see cref="AspNetContextRequestEnricher"/>.
/// </summary>
public class AspNetHeadersToBodyGetter : IMessageHeadersGetter<AspNetContext>
{
    private readonly IDictionary<string, string> _headerMapping = new Dictionary<string, string>
    {
        {"x-user-id", "userId" },
    };

    /// <summary>
    /// Gets the mapped headers present on the request.
    /// </summary>
    /// <param name="context">The HTTP context to extract headers from.</param>
    /// <returns>A dictionary of mapped field names to header values, for headers in the fixed mapping that are present on the request.</returns>
    public IDictionary<string, string> GetHeaders(AspNetContext context)
    {
        // Single pass with TryGetValue/TryAdd instead of a per-header double lookup + GroupBy/First/
        // ToDictionary - runs on every HTTP request. TryAdd keeps the first entry per mapped name,
        // matching the old GroupBy(...).Select(g => g.First()).
        var result = new Dictionary<string, string>();

        foreach (var header in context.HttpRequest.Headers)
        {
            if (_headerMapping.TryGetValue(header.Key.ToLowerInvariant(), out var mapped))
            {
                result.TryAdd(mapped, header.Value.First());
            }
        }

        return result;
    }
}
