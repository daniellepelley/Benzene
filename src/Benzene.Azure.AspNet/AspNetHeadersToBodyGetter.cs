using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Azure.AspNet;

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
        return context.HttpRequest.Headers
            .Where(x => _headerMapping.ContainsKey(x.Key.ToLowerInvariant()))
            .Select(x => (_headerMapping[x.Key.ToLowerInvariant()], context.HttpRequest.Headers[x.Key].First()))
            .GroupBy(x => x.Item1)
            .Select(x => x.First())
            .ToDictionary(x => x.Item1, x => x.Item2);
    }
}
