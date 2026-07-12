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
        return context.HttpContext.Request.Headers
            .Select(x => _headerMapping.ContainsKey(x.Key.ToLowerInvariant())
                ? (_headerMapping[x.Key.ToLowerInvariant()], context.HttpContext.Request.Headers[x.Key].First())
                : (x.Key, x.Value.First())
            )
            .GroupBy(x => x.Item1)
            .Select(x => x.First())
            .ToDictionary(x => x.Item1, x => x.Item2);
    }
}
