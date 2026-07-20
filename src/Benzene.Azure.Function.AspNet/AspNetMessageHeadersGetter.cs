using Benzene.Abstractions.Messages.Mappers;

namespace Benzene.Azure.Function.AspNet;

/// <summary>
/// Extracts message headers from the HTTP request, mapping a fixed set of well-known headers
/// (<c>x-user-id</c>, <c>x-correlation-id</c>) to shorter field names while passing all other headers
/// through unchanged.
/// </summary>
public class AspNetMessageHeadersGetter : IMessageHeadersGetter<AspNetContext>
{
    private readonly IDictionary<string, string> _headerMapping = new Dictionary<string, string>
    {
        {"x-user-id", "userId" },
        {"x-correlation-id", "correlationId" },
    };

    /// <summary>
    /// Gets the request's headers, with well-known headers mapped to shorter field names.
    /// </summary>
    /// <param name="context">The HTTP context to extract headers from.</param>
    /// <returns>A dictionary of (lower-cased) header/field names to their (verbatim) values.</returns>
    public IDictionary<string, string> GetHeaders(AspNetContext context)
    {
        // Header field NAMES are case-insensitive (lower-case them for lookup stability), but VALUES
        // are opaque and case-sensitive (RFC 9110) - lower-casing a value corrupts bearer tokens,
        // correlation IDs, base64, etc. Preserve the value verbatim.
        return context.HttpRequest.Headers
            .Select(x => _headerMapping.ContainsKey(x.Key.ToLowerInvariant())
                ? (_headerMapping[x.Key.ToLowerInvariant()], context.HttpRequest.Headers[x.Key].First())
                : (x.Key, x.Value.First())
            )
            .GroupBy(x => x.Item1)
            .Select(x => x.First())
            .ToDictionary(x => x.Item1.ToLowerInvariant(), x => x.Item2);
    }
}
