using System.Collections.Generic;
using System.Linq;
using Benzene.Abstractions.Messages.Mappers;
using Benzene.Http;

namespace Benzene.AspNet.Core;

/// <summary>
/// Extracts only the headers configured in <see cref="IHttpHeaderMappings"/>, mapping them to their
/// mapped field names, for use by <see cref="AspNetRequestEnricher"/>.
/// </summary>
public class AspNetHeadersToBodyGetter : IMessageHeadersGetter<AspNetContext>
{
    private readonly IDictionary<string, string> _headerMapping;

    /// <summary>
    /// Initializes a new instance of the <see cref="AspNetHeadersToBodyGetter"/> class.
    /// </summary>
    /// <param name="httpHeaderMappings">The configured header name mappings.</param>
    public AspNetHeadersToBodyGetter(IHttpHeaderMappings httpHeaderMappings)
    {
        _headerMapping = httpHeaderMappings.GetMappings();
    }

    /// <summary>
    /// Gets the mapped headers present on the request.
    /// </summary>
    /// <param name="context">The HTTP context to extract headers from.</param>
    /// <returns>A dictionary of mapped field names to header values, for headers in the configured mapping that are present on the request.</returns>
    public IDictionary<string, string> GetHeaders(AspNetContext context)
    {
        // Single pass with TryGetValue/TryAdd instead of a per-header double lookup + GroupBy/First/
        // ToDictionary - runs on every HTTP request. TryAdd keeps the first entry per mapped name,
        // matching the old GroupBy(...).Select(g => g.First()).
        var result = new Dictionary<string, string>();

        foreach (var header in context.HttpContext.Request.Headers)
        {
            if (_headerMapping.TryGetValue(header.Key.ToLowerInvariant(), out var mapped))
            {
                result.TryAdd(mapped, header.Value.First());
            }
        }

        return result;
    }
}
