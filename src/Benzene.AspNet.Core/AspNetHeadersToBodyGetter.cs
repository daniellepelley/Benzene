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
        return context.HttpContext.Request.Headers
            .Where(x => _headerMapping.ContainsKey(x.Key.ToLowerInvariant()))
            .Select(x => (_headerMapping[x.Key.ToLowerInvariant()], context.HttpContext.Request.Headers[x.Key].First()))
            .GroupBy(x => x.Item1)
            .Select(x => x.First())
            .ToDictionary(x => x.Item1, x => x.Item2);
    }
}
