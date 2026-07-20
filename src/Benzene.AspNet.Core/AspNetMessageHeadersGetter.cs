using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Http;

namespace Benzene.AspNet.Core;

/// <summary>
/// Extracts message headers from the request, mapping headers configured in <see cref="IHttpHeaderMappings"/>
/// to their mapped field names while passing all other headers through unchanged.
/// </summary>
public class AspNetMessageHeadersGetter : IMessageHeadersGetter<AspNetContext>
{
    private readonly IDictionary<string, string> _headerMapping;

    /// <summary>
    /// Initializes a new instance of the <see cref="AspNetMessageHeadersGetter"/> class.
    /// </summary>
    /// <param name="httpHeaderMappings">The configured header name mappings.</param>
    public AspNetMessageHeadersGetter(IHttpHeaderMappings httpHeaderMappings)
    {
        _headerMapping = httpHeaderMappings.GetMappings();
    }

    /// <summary>
    /// Gets the request's headers, with configured headers mapped to their mapped field names.
    /// </summary>
    /// <param name="context">The HTTP context to extract headers from.</param>
    /// <returns>A dictionary of header/field names to values.</returns>
    public IDictionary<string, string> GetHeaders(AspNetContext context)
    {
        // Single pass with TryGetValue/TryAdd instead of a per-header double dictionary lookup
        // (ContainsKey + indexer, each re-lowercasing the key) plus a GroupBy/First/ToDictionary just
        // to dedupe - this runs on every HTTP request. TryAdd keeps the first entry per resulting key,
        // matching the old GroupBy(...).Select(g => g.First()). When there are no mappings (the common
        // case) the key isn't lowercased at all.
        var headers = context.HttpContext.Request.Headers;
        var result = new Dictionary<string, string>(headers.Count);
        var hasMappings = _headerMapping.Count != 0;

        foreach (var header in headers)
        {
            var key = hasMappings && _headerMapping.TryGetValue(header.Key.ToLowerInvariant(), out var mapped)
                ? mapped
                : header.Key;

            result.TryAdd(key, header.Value.First());
        }

        return result;
    }
}
